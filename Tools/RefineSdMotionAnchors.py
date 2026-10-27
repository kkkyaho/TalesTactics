"""Refine only the 80 Cless native-motion X pivots, without editing image pixels.

Usage: python Tools/RefineSdMotionAnchors.py INPUT.json OUTPUT.json REPORT.json
Both outputs must be new files. The output requires a fresh visual approval.
Optional --pixels-per-unit explicitly calibrates native anatomical size against
the established Cless sheets after validating the untouched raw measurement PPU.
The immutable cless-before PNG/meta is the geometric baseline, even after the
production texture changes. MeasureSdCharacterArt's default behavior is untouched.
"""
import argparse
import copy
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image

from MeasureSdCharacterArt import PROJECT, components, digest, read_sprites

BASELINE = "Logs/SdPolish20261027/cless-before.png"
BASELINE_SHA = "16ececda2c0b75a8aacf0fdd7d7a3455161418c6628e82eff500c4768f2908d0"
BASELINE_META_SHA = "e29b617628a8a373a694a0659400c255a6ff50ef8f812892b1fe4aff68a6f5de"
GUID = "40e5b31d6d64b0a48b8b17f727aebee9"
METHOD = "cless-native-bbox-world-offset-v1"


def require(condition, message):
    if not condition:
        raise ValueError(message)


def project_path(value):
    return (PROJECT / value).resolve()


def assign_parts(alpha, records, scale):
    # Use the same component definition/assignment as the original measurement.
    expected = np.array([[(s['rect'][0] + s['rect'][2] / 2) * scale,
                          (1254 - s['rect'][1] - s['rect'][3] / 2) * scale]
                         for s in records])
    groups = [[] for _ in records]
    majors = [0 for _ in records]
    for part in components(alpha > 128):
        center = (part.min(axis=0) + part.max(axis=0)) / 2
        distance = ((expected - center) ** 2).sum(axis=1)
        order = np.argsort(distance)
        index = int(order[0])
        if len(part) > 1000:
            require(distance[order[1]] - distance[index] >= 25,
                    "Ambiguous major component: " + records[index]['name'])
            majors[index] += 1
        groups[index].append(part)
    require(all(majors), "Missing source/candidate major figure")
    return [np.concatenate(group) for group in groups], majors


def geometry(part, canvas_height):
    lo = part.min(axis=0)
    hi = part.max(axis=0) + 1
    left, top = (lo - 2).tolist()
    right, bottom = (hi + 2).tolist()
    width, height = right - left, bottom - top
    feet = part[part[:, 1] > bottom - height * .18]
    return dict(rect=[left, canvas_height - bottom, width, height],
                bbox=[int(lo[0]), int(canvas_height - hi[1]),
                      int(hi[0] - lo[0]), int(hi[1] - lo[1])],
                centerX=float((lo[0] + hi[0]) / 2),
                footX=float(np.median(feet[:, 0])) if len(feet) else (left + right) / 2)


def finite_number(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)


def expand_x_rect(frame, target_anchor, alpha):
    """Add only necessary X padding, keeping all absolute outline vertices fixed."""
    x, y, w, h = [frame[k] for k in ('x', 'y', 'width', 'height')]
    left, right = min(x, math.floor(target_anchor)), max(x + w, math.ceil(target_anchor))
    new_rect = [left, y, right - left, h]
    canvas_height, canvas_width = alpha.shape
    _, pose, direction = frame['name'].rsplit('_', 2)
    row, column = int(pose) // 2, (int(pose) % 2) * 4 + int(direction)
    cell = [column * canvas_width // 8, canvas_height - (row + 1) * canvas_height // 10,
            (column + 1) * canvas_width // 8, canvas_height - row * canvas_height // 10]
    require(left >= cell[0] and right <= cell[2] and y >= cell[1] and y + h <= cell[3],
            'Expanded rect leaves its original atlas cell: ' + frame['name'])
    outlines = copy.deepcopy(frame['outlines'])
    original_points = [point for poly in frame['outlines'] for point in poly['points']]
    require(original_points and all(finite_number(p['x']) and finite_number(p['y']) for p in original_points),
            'Missing/nonfinite outline: ' + frame['name'])
    absolute_x = [x + w / 2 + p['x'] for p in original_points]
    require(min(absolute_x) >= x and max(absolute_x) <= x + w,
            'Existing outline intersects the added X padding: ' + frame['name'])
    strips = []
    if left < x:
        strips.append(alpha[canvas_height - y - h:canvas_height - y, left:x])
    if right > x + w:
        strips.append(alpha[canvas_height - y - h:canvas_height - y, x + w:right])
    added = np.concatenate([strip.reshape(-1) for strip in strips]) if strips else np.array([], dtype=np.uint8)
    max_alpha = int(added.max()) if added.size else 0
    require(max_alpha <= 8, 'Added X padding contains alpha > 8: ' + frame['name'])
    shift = x + w / 2 - (left + (right - left) / 2)
    for poly in outlines:
        for point in poly['points']:
            point['x'] = round(point['x'] + shift, 3)
    changed_points = [point for poly in outlines for point in poly['points']]
    error = max(abs(left + (right - left) / 2 + after['x'] - before)
                for before, after in zip(absolute_x, changed_points))
    require(error <= .000001 and all(a['y'] == b['y'] for a, b in zip(original_points, changed_points)),
            'Absolute outline geometry changed: ' + frame['name'])
    proof = dict(leftPixels=x - left, rightPixels=right - x - w, addedPixels=int(added.size),
                 weakPixelCount=int((added > 0).sum()), maximumAlpha=max_alpha, alphaAbove8Count=int((added > 8).sum()),
                 allowedAlphaMaximum=8, atlasCell=[cell[0], cell[1], cell[2] - cell[0], cell[3] - cell[1]],
                 outlineLocalXShift=shift, absoluteOutlineMaximumErrorPixels=error,
                 extensionOutsideAbsoluteOutline=True)
    return new_rect, outlines, proof


def require_no_rect_overlap(frames):
    for i, a in enumerate(frames):
        for b in frames[i + 1:]:
            overlap = (max(a['x'], b['x']) < min(a['x'] + a['width'], b['x'] + b['width']) and
                       max(a['y'], b['y']) < min(a['y'] + a['height'], b['y'] + b['height']))
            require(not overlap, 'Adjusted sprite rects overlap: ' + a['name'] + '/' + b['name'])


def refine(measurement, source=BASELINE, pixels_per_unit=None):
    """Read/validate inputs and return two JSON objects; perform no writes."""
    measurement, source = project_path(measurement), project_path(source)
    source_meta = source.with_suffix(source.suffix + '.meta')
    require(digest(source) == BASELINE_SHA, "Unexpected Cless baseline PNG SHA256")
    require(digest(source_meta) == BASELINE_META_SHA, "Unexpected Cless baseline meta SHA256")
    guid, old_ppu, records = read_sprites(source)
    require(guid == GUID and len(records) == 80, "Expected the original 80-sprite Cless GUID")
    require({r['name'] for r in records} == {f'cless_{p}_{d}' for p in range(20) for d in range(4)},
            "Unexpected Cless frame names")
    for key in ('spriteId', 'internalId'):
        require(len({r[key] for r in records}) == 80, "Duplicate baseline " + key)
    input_sha = digest(measurement)
    data = json.loads(measurement.read_text(encoding='utf-8-sig'))
    require('anchorRefinement' not in data, "Already refined; use the raw measurement")
    require(data.get('sourceGuid') == guid and data.get('sourceSha256') == BASELINE_SHA,
            "Measurement baseline identity/hash differs")
    frames = data.get('frames', [])
    require(data.get('frameCount') == 80 and len(frames) == 80, "Expected exactly 80 measured frames")
    for frame, old in zip(frames, records):
        require((frame.get('name'), frame.get('originalSpriteId'), str(frame.get('originalInternalId')))
                == (old['name'], old['spriteId'], old['internalId']),
                "Frame order/name/sprite ID/local ID changed")
    candidate = project_path(data['candidate'])
    require(candidate != source and digest(candidate) == data.get('candidateSha256'),
            "Candidate PNG SHA256 differs")
    with Image.open(source) as old_image, Image.open(candidate) as new_image:
        require(old_image.size == (1254, 1254) and old_image.mode == new_image.mode == 'RGBA',
                "Unexpected baseline canvas or image mode")
        width, height = new_image.size
        require(0 < width == height <= 4096 and [width, height] == [data.get('width'), data.get('height')],
                "Candidate dimensions differ or exceed 4096 square")
        old_alpha = np.array(old_image)[:, :, 3]
        new_alpha = np.array(new_image)[:, :, 3]
    canvas_scale = width / 1254
    old_parts, _ = assign_parts(old_alpha, records, 1)
    new_parts, major_counts = assign_parts(new_alpha, records, canvas_scale)
    require(data.get('majorComponentsPerFrame') == major_counts and
            data.get('majorComponentCount') == sum(major_counts) and
            data.get('unusedSourceFigureCount') == 0, "Component coverage differs")
    old_geometry = [geometry(part, 1254) for part in old_parts]
    new_geometry = [geometry(part, height) for part in new_parts]
    standing = [i for i, r in enumerate(records) if r['name'].rsplit('_', 2)[-2] == '0']
    expected_ppu = round(old_ppu * float(np.median([
        new_geometry[i]['bbox'][3] / records[i]['rect'][3] for i in standing])), 6)
    raw_ppu = data.get('pixelsPerUnit')
    require(finite_number(raw_ppu) and raw_ppu > 0 and abs(raw_ppu - expected_ppu) <= .000001,
            "Candidate PPU differs from the original measurement algorithm")
    new_ppu = raw_ppu if pixels_per_unit is None else pixels_per_unit
    require(finite_number(new_ppu) and .5 * raw_ppu <= new_ppu <= 2 * raw_ppu,
            "Selected PPU must be finite and between 0.5x and 2x the verified raw PPU")
    art_scale = new_ppu / old_ppu
    output = copy.deepcopy(data)
    output['pixelsPerUnit'] = new_ppu
    rows = []
    for old, frame, changed, old_g, new_g in zip(records, frames, output['frames'], old_geometry, new_geometry):
        name = old['name']
        rect = [frame.get(k) for k in ('x', 'y', 'width', 'height')]
        require(rect == new_g['rect'], "Measured rect differs from candidate alpha: " + name)
        x, y, w, h = rect
        require(x > 0 and y > 0 and x + w < width and y + h < height,
                "Candidate touches canvas edge: " + name)
        require(finite_number(frame.get('pivotX')) and frame.get('pivotY') == .01,
                "Unexpected pivot or grounding policy: " + name)
        old_anchor = old['rect'][0] + old['rect'][2] * old['pivot'][0]
        prior_anchor = x + w * frame['pivotX']
        require(abs(prior_anchor - old_anchor * canvas_scale) <= .001,
                "Input X pivot is not a raw canvas-scaled measurement: " + name)
        require(finite_number(frame.get('footAnchorDeltaPixels')) and
                abs(new_g['footX'] - prior_anchor - frame['footAnchorDeltaPixels']) <= .001,
                "Input foot diagnostic differs: " + name)
        target_anchor = new_g['centerX'] + (old_anchor - old_g['centerX']) * art_scale
        adjusted_rect, adjusted_outlines, padding = expand_x_rect(frame, target_anchor, new_alpha)
        require(pixels_per_unit is not None or adjusted_rect == rect,
                'Raw-PPU refinement unexpectedly requires rect expansion: ' + name)
        adjusted_x, _, adjusted_width, _ = adjusted_rect
        new_pivot = round((target_anchor - adjusted_x) / adjusted_width, 6)
        require(0 <= new_pivot <= 1, "World-offset-preserving pivot falls outside rect: " + name)
        rounded_anchor = adjusted_x + adjusted_width * new_pivot
        old_offset_world = (old_anchor - old_g['centerX']) / old_ppu
        new_offset_world = (rounded_anchor - new_g['centerX']) / new_ppu
        require(abs(new_offset_world - old_offset_world) <= .00001,
                "World offset not preserved within JSON rounding tolerance: " + name)
        changed['pivotX'] = new_pivot
        changed['x'], changed['width'] = adjusted_x, adjusted_width
        changed['outlines'] = adjusted_outlines
        changed['footAnchorDeltaPixels'] = round(new_g['footX'] - rounded_anchor, 3)
        rows.append(dict(name=name, originalSpriteId=old['spriteId'], originalInternalId=old['internalId'],
                         oldRect=old['rect'], oldPivotX=old['pivot'][0], oldBbox=old_g['bbox'],
                         rawMeasuredRect=rect, newRect=adjusted_rect, newBbox=new_g['bbox'], oldPivotAbsoluteX=old_anchor,
                         rectExpansion=padding,
                         oldBboxCenterX=old_g['centerX'], newBboxCenterX=new_g['centerX'],
                         priorPivotX=frame['pivotX'], pivotX=new_pivot,
                         pivotDeltaFraction=new_pivot - frame['pivotX'],
                         anchorDeltaPixels=rounded_anchor - prior_anchor,
                         imageShiftWorld=-(rounded_anchor - prior_anchor) / new_ppu,
                         rawBboxCenterOffsetWorld=(new_g['centerX'] - prior_anchor) / raw_ppu,
                         newBboxCenterOffsetWorld=(new_g['centerX'] - rounded_anchor) / new_ppu,
                         bboxCenterImageShiftWorld=(new_g['centerX'] - rounded_anchor) / new_ppu - (new_g['centerX'] - prior_anchor) / raw_ppu,
                         oldBboxToPivotWorld=old_offset_world, newBboxToPivotWorld=new_offset_world,
                         worldOffsetRoundingError=new_offset_world - old_offset_world,
                         oldFootOffsetWorld=(old_g['footX'] - old_anchor) / old_ppu,
                         priorFootOffsetWorld=(new_g['footX'] - prior_anchor) / raw_ppu,
                         newFootOffsetWorld=(new_g['footX'] - rounded_anchor) / new_ppu))
    require_no_rect_overlap(output['frames'])
    summary = dict(frameCount=80, adjustedFrames=sum(r['pivotDeltaFraction'] != 0 for r in rows),
                   expandedRectFrames=sum(r['rawMeasuredRect'] != r['newRect'] for r in rows),
                   addedRectPixels=sum(r['rectExpansion']['addedPixels'] for r in rows),
                   addedWeakAlphaPixels=sum(r['rectExpansion']['weakPixelCount'] for r in rows),
                   maximumAddedAlpha=max(r['rectExpansion']['maximumAlpha'] for r in rows),
                   maximumAbsoluteOutlineErrorPixels=max(r['rectExpansion']['absoluteOutlineMaximumErrorPixels'] for r in rows),
                   adjustedRectsNonoverlapping=True,
                   oldPixelsPerUnit=old_ppu, rawPixelsPerUnit=raw_ppu, newPixelsPerUnit=new_ppu,
                   canvasScale=canvas_scale, artScale=art_scale,
                   maximumAnchorDeltaPixels=max(abs(r['anchorDeltaPixels']) for r in rows),
                   maximumImageShiftWorld=max(abs(r['imageShiftWorld']) for r in rows),
                   maximumBboxCenterImageShiftWorld=max(abs(r['bboxCenterImageShiftWorld']) for r in rows),
                   maximumFootOffsetChangeWorldBefore=max(abs(r['priorFootOffsetWorld'] - r['oldFootOffsetWorld']) for r in rows),
                   maximumFootOffsetChangeWorldAfter=max(abs(r['newFootOffsetWorld'] - r['oldFootOffsetWorld']) for r in rows),
                   maximumWorldOffsetRoundingError=max(abs(r['worldOffsetRoundingError']) for r in rows))
    require(digest(source) == BASELINE_SHA and digest(source_meta) == BASELINE_META_SHA and
            digest(candidate) == data['candidateSha256'] and digest(measurement) == input_sha,
            "An input changed during measurement; no output written")
    calibration = dict(requested=pixels_per_unit is not None, rawPixelsPerUnit=raw_ppu,
                       selectedPixelsPerUnit=new_ppu, renderedSizeMultiplier=raw_ppu / new_ppu,
                       reason=('Explicit visual anatomical-size calibration against existing Cless HQ/extra/polish head and boot sizes; crouched total bbox height is not the size reference.'
                               if pixels_per_unit is not None else 'No PPU calibration requested; preserve the verified raw measurement PPU.'))
    provenance = dict(method=METHOD, baseline=str(source), baselineSha256=BASELINE_SHA,
                      baselineMetaSha256=BASELINE_META_SHA, inputMeasurement=str(measurement),
                      inputMeasurementSha256=input_sha, candidateSha256=data['candidateSha256'],
                      formula='newBboxCenterX + (oldPivotAbsoluteX - oldBboxCenterX) * newPPU / oldPPU',
                      bboxDefinition='alpha > 128, components >= 8 pixels; half-open pixel bbox center',
                      footDiagnostic='Bottom 18% alpha median; diagnostic only, not an anatomical foot guarantee',
                      imageShiftDiagnostic='imageShiftWorld is the anchor-only translation at selected PPU; bboxCenterImageShiftWorld includes both raw-to-selected scale and anchor changes. Before footprint diagnostics use raw PPU, after diagnostics use selected PPU.',
                      unchanged='PNG pixels, source/meta, frame names/IDs, absolute outline geometry, rect Y/height and pivotY=.01',
                      rectPaddingPolicy='Minimum integer X extension within original cell, no rect overlap; added pixels may have alpha 0..8 only and lie outside the unchanged absolute outline. This is not a claim of strictly zero alpha.',
                      calibration=calibration,
                      inputVisualApproved=data.get('visualApproved', False),
                      inputVisualReview=data.get('visualReview', ''), summary=summary)
    output['anchorRefinement'] = provenance
    output['visualApproved'] = False
    output.pop('visualReview', None)
    report = dict(schemaVersion=1, **provenance, frames=rows,
                  limitation='Preserves source bbox-relative world offsets; anatomical pose changes still need actual motion review.')
    return output, report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('measurement')
    parser.add_argument('output')
    parser.add_argument('report')
    parser.add_argument('--source', default=BASELINE, help='Only an exact copy of the fixed baseline PNG/meta is accepted')
    parser.add_argument('--pixels-per-unit', type=float, help='Explicit visual size calibration; 0.5x..2x verified raw PPU, default unchanged')
    args = parser.parse_args()
    paths = [project_path(args.output), project_path(args.report)]
    require(paths[0] != paths[1] and all(p.suffix.lower() == '.json' and not p.exists() for p in paths),
            'Output and report must be two distinct, new .json files')
    data, report = refine(args.measurement, args.source, args.pixels_per_unit)
    created = []
    try:
        for path, value in zip(paths, (data, report)):
            path.parent.mkdir(parents=True, exist_ok=True)
            with path.open('x', encoding='utf-8', newline='\n') as stream:
                created.append(path)
                json.dump(value, stream, ensure_ascii=False, indent=2, allow_nan=False)
                stream.write('\n')
    except BaseException:
        for path in created:
            path.unlink()
        raise
    print(json.dumps(report['summary'], indent=2))


if __name__ == '__main__':
    main()
