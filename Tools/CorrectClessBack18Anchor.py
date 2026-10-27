"""Apply one explicit, capture-derived Cless Back18 pivot correction.

Usage: python Tools/CorrectClessBack18Anchor.py INPUT SPEC OUTPUT REPORT
Only new JSON files are written. No PNG/meta, production manifest or Unity writes.
This bounded v5 tool does not apply its offset automatically to another atlas.
"""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image

from MeasureSdCharacterArt import digest, read_sprites
from RefineSdMotionAnchors import (BASELINE, BASELINE_SHA, BASELINE_META_SHA,
                                 GUID, project_path, require)

METHOD = 'cless-v5-back18-observed-body-anchor-v1'
INPUT_SHA = '02e7482b5595ba718f866c47b193b02cbecbfc246a3aa48c3a12466d6704ca94'
CANDIDATE_SHA = '0ffd2c058365aa5fb3d2659a837053c950d84083704973baf94ba16d1a221d86'
RESULT_SHA = '189a2c699feebc9a6f1e8d8af1f191c931d06a599fd2b0a7a5473049f1b50bfa'
HQ_META_SHA = '6ea06fc52d9eaf159e1a4ad32629eeaa32a50f47d624bf010a71f0d12348a736'
ROIS = {58: ((432, 315, 487, 359), (522, 316, 581, 359)),
        59: ((467, 321, 523, 365), (568, 320, 626, 365)),
        61: ((459, 330, 516, 374), (560, 329, 619, 374)),
        62: ((389, 330, 447, 377), (480, 332, 542, 378))}


def boot_point(path, rois):
    """Measure only; never save, crop to a file, resize, or edit any image."""
    with Image.open(path) as image:
        require(image.size == (960, 540), 'Capture dimensions changed')
        rgb = np.array(image.convert('RGB')).astype(int)
    points = []
    for x0, y0, x1, y1 in rois:
        mask = np.max(abs(rgb[y0:y1, x0:x1] - rgb[0, 0]), axis=2) > 12
        y, x = np.nonzero(mask)
        require(len(x) > 100, 'Missing boot in explicit ROI')
        bottom = int(y.max())
        y, x = np.nonzero(mask & (np.arange(mask.shape[0])[:, None] > bottom - 10))
        require(x.min() > 0 and x.max() < x1 - x0 - 1 and bottom < y1 - y0 - 1,
                'Boot ROI clips its lower-band support')
        points.append(dict(x=(int(x.min()) + int(x.max())) / 2 + x0,
                           y=bottom + y0, roi=[x0, y0, x1, y1]))
    return np.mean([[p['x'], p['y']] for p in points], axis=0), points


def fit_transform(frame, sample, body_scale):
    """Recover billboard Z tilt and origin from exact mesh extents, not rect width."""
    points = np.array([[p['x'] + frame['width'] * (.5 - frame['pivotX']),
                        p['y'] + frame['height'] * (.5 - frame['pivotY'])]
                       for polygon in frame['outlines'] for p in polygon['points']])
    scale = body_scale / 220 * 540 / (2 * sample['orthographicSize'])
    bounds = sample['viewportBounds']
    observed = np.array([bounds['width'] * 960, bounds['height'] * 540])

    def transformed(degrees):
        angle = math.radians(degrees)
        c, s = math.cos(angle), math.sin(angle)
        return np.column_stack((points[:, 0] * c - points[:, 1] * s,
                                points[:, 0] * s + points[:, 1] * c)) * scale

    def error(degrees):
        q = transformed(degrees)
        return float(((q.max(axis=0) - q.min(axis=0) - observed) ** 2).sum())

    # Cless ultimate is Blink, Tilt=10; released scaleY=1 and Back sign=+1.
    angles = np.linspace(-10, 0, 401)
    best = int(np.argmin([error(a) for a in angles]))
    require(0 < best < len(angles) - 1, 'Recovered tilt falls at search boundary')
    low, high = angles[best - 1], angles[best + 1]
    for _ in range(70):
        a, b = (2 * low + high) / 3, (low + 2 * high) / 3
        if error(a) < error(b):
            high = b
        else:
            low = a
    angle = float((low + high) / 2)
    q = transformed(angle)
    maximum_error = math.sqrt(error(angle))
    require(maximum_error < .01, 'Recorded mesh bounds do not fit the expected transform')
    return dict(frame=sample['frame'], sprite=frame['name'], tiltDegrees=angle,
                screenPixelsPerNativePixel=scale,
                screenXPerAnchorPixel=scale * math.cos(math.radians(angle)),
                rootScreenX=bounds['x'] * 960 - float(q[:, 0].min()),
                rootScreenY=(1 - bounds['y'] - bounds['height']) * 540 + float(q[:, 1].max()),
                meshExtentFitErrorPixels=maximum_error)


def correct(input_path, spec_path):
    input_path, spec_path = project_path(input_path), project_path(spec_path)
    watched = {}

    def verify(path, expected):
        path = Path(path).resolve()
        require(digest(path) == expected, 'Evidence SHA256 differs: ' + str(path))
        watched[str(path)] = expected
        return path

    verify(input_path, INPUT_SHA)
    spec = json.loads(spec_path.read_text(encoding='utf-8-sig'))
    watched[str(spec_path)] = digest(spec_path)
    require(spec['method'] == METHOD and spec['inputMeasurementSha256'] == INPUT_SHA and
            spec['candidateSha256'] == CANDIDATE_SHA and spec['targetSprite'] == 'cless_18_1',
            'This explicit correction is only for the reviewed v5 Back18')
    data = json.loads(input_path.read_text(encoding='utf-8-sig'))
    require('poseAnchorCorrection' not in data, 'Already corrected')
    require(data['pixelsPerUnit'] == 220 and data['width'] == data['height'] == 4096 and
            data['sourceGuid'] == GUID and data['frameCount'] == len(data['frames']) == 80,
            'Unexpected candidate metadata identity or scale')
    baseline = verify(project_path(BASELINE), BASELINE_SHA)
    verify(baseline.with_suffix('.png.meta'), BASELINE_META_SHA)
    _, _, old = read_sprites(baseline)
    for frame, original in zip(data['frames'], old):
        require((frame['name'], frame['originalSpriteId'], str(frame['originalInternalId'])) ==
                (original['name'], original['spriteId'], original['internalId']), 'Changed frame identity/order')
    candidate = verify(project_path(data['candidate']), CANDIDATE_SHA)
    atlas_evidence = []
    with Image.open(candidate) as image:
        require(image.mode == 'RGBA' and image.size == (4096, 4096), 'Unexpected atlas mode/size')
        rgba = np.array(image)
    for frame in data['frames']:
        if frame['name'] not in ('cless_17_1', 'cless_18_1', 'cless_19_1'):
            continue
        x, y, width, height = [frame[k] for k in ('x', 'y', 'width', 'height')]
        pixels = rgba[4096-y-height:4096-y, x:x+width]
        yy, xx = np.nonzero(pixels[:, :, 3] > 128)
        bbox = [int(xx.min()), int(yy.min()), int(xx.max()) + 1, int(yy.max()) + 1]
        support = pixels[bbox[1]:bbox[3], bbox[0]:bbox[2]]
        atlas_evidence.append(dict(name=frame['name'], rect=[x, y, width, height],
                                   rectRgbaSha256=hashlib.sha256(pixels.tobytes()).hexdigest(),
                                   alpha128BboxInRectTopOrigin=bbox,
                                   alpha128BboxRgbaSha256=hashlib.sha256(support.tobytes()).hexdigest()))
    hq = project_path(spec['scaleReference'])
    verify(hq.with_suffix('.png.meta'), HQ_META_SHA)
    _, hq_ppu, hq_frames = read_sprites(hq)
    front = next(f for f in hq_frames if f['internalId'] == '-628865702')
    require(front['name'] == 'cless-hq_0_0', 'Wrong body-size reference')
    body_scale = 1.22 / (front['rect'][3] / hq_ppu)
    result_path = verify(spec['captureResult'], RESULT_SHA)
    result = json.loads(result_path.read_text(encoding='utf-8-sig'))
    require(result['passed'] and result['width'] == 960 and result['height'] == 540 and result['fps'] == 30,
            'Unexpected capture evidence')
    run = next(r for r in result['runs'] if r['name'] == 'cless.ultimate-Back')
    frames = {f['name']: f for f in data['frames']}
    rows = []
    for sample in run['timeline']:
        number = sample['frame']
        if number not in range(57, 64):
            continue
        expected = 'cless_17_1' if number <= 58 else 'cless_18_1' if number <= 61 else 'cless_19_1'
        require(sample['sprite'] == expected and sample['presenting'] and sample['action'] == 'Ultimate' and
                sample['cameraYaw'] == 231.07244873046875 and sample['orthographicSize'] == 1.4335191249847412,
                'Unexpected action/frame/camera at evidence sample')
        path = verify(result_path.parent / sample['file'], spec['captureSha256'][str(number)])
        row = fit_transform(frames[expected], sample, body_scale)
        if number in ROIS:
            point, boots = boot_point(path, ROIS[number])
            angle = math.radians(row['tiltDegrees'])
            u = (point[0] - row['rootScreenX']) / row['screenPixelsPerNativePixel']
            v = (row['rootScreenY'] - point[1]) / row['screenPixelsPerNativePixel']
            row.update(boots=boots, stanceScreenX=float(point[0]), stanceScreenY=float(point[1]),
                       bodyXRelativeAnchorPixels=math.cos(angle) * u + math.sin(angle) * v,
                       bodyYRelativeAnchorPixels=-math.sin(angle) * u + math.cos(angle) * v)
        rows.append(row)
    require([r['frame'] for r in rows] == list(range(57, 64)), 'Incomplete evidence timeline')
    by_number = {r['frame']: r for r in rows}
    neighbor = (by_number[58]['bodyXRelativeAnchorPixels'] + by_number[62]['bodyXRelativeAnchorPixels']) / 2
    current = (by_number[59]['bodyXRelativeAnchorPixels'] + by_number[61]['bodyXRelativeAnchorPixels']) / 2
    delta = current - neighbor
    require(abs(delta - spec['expectedAnchorDeltaPixels']) < .0001, 'Derived correction differs from explicit proposal')
    output = copy.deepcopy(data)
    changed = next(f for f in output['frames'] if f['name'] == 'cless_18_1')
    before = frames['cless_18_1']
    changed['pivotX'] = round(before['pivotX'] + delta / before['width'], 6)
    require(0 < changed['pivotX'] < 1, 'Correction requires an unsupported rect expansion')
    applied = (changed['pivotX'] - before['pivotX']) * before['width']
    changed['footAnchorDeltaPixels'] = round(before['footAnchorDeltaPixels'] - applied, 3)
    for old_frame, new_frame in zip(data['frames'], output['frames']):
        for key in old_frame:
            if old_frame['name'] == 'cless_18_1' and key in ('pivotX', 'footAnchorDeltaPixels'):
                continue
            require(old_frame[key] == new_frame[key], 'Unintended frame field change')
    for row in rows:
        shift = -applied * row['screenXPerAnchorPixel'] if row['sprite'] == 'cless_18_1' else 0
        row['predictedScreenShiftX'] = shift
        row['predictedScreenShiftY'] = (applied * row['screenPixelsPerNativePixel'] * math.sin(math.radians(row['tiltDegrees']))
                                       if row['sprite'] == 'cless_18_1' else 0)
        if 'stanceScreenX' in row:
            row['predictedStanceScreenX'] = row['stanceScreenX'] + shift
    transitions = []
    for a, b in ((58, 59), (61, 62)):
        left, right = by_number[a], by_number[b]
        transitions.append(dict(fromFrame=a, toFrame=b,
                                rootDeltaPixels=right['rootScreenX'] - left['rootScreenX'],
                                beforeBodyDeltaPixels=right['stanceScreenX'] - left['stanceScreenX'],
                                predictedBodyDeltaPixels=right['predictedStanceScreenX'] - left['predictedStanceScreenX']))
    provenance = dict(method=METHOD, inputMeasurement=str(input_path), inputMeasurementSha256=INPUT_SHA,
                      specification=str(spec_path), specificationSha256=watched[str(spec_path)],
                      candidateSha256=CANDIDATE_SHA, captureResult=str(result_path), captureResultSha256=RESULT_SHA,
                      targetSprite='cless_18_1', pixelsPerUnit=220, bodyScale=body_scale,
                      scaleReferenceMetaSha256=HQ_META_SHA, oldPivotX=before['pivotX'], newPivotX=changed['pivotX'],
                      requestedAnchorDeltaPixels=delta, appliedAnchorDeltaPixels=applied,
                      targetBodyXRelativeAnchorPixels=neighbor, measuredBodyXRelativeAnchorPixels=current,
                      policy='Back18 body midpoint moves to the mean of unchanged Back17/19 body midpoints after removing recovered root translation and billboard tilt.',
                      supersedes='The old bbox-relative world-X invariant is intentionally overridden only for cless_18_1.',
                      unchanged='All PNG pixels, PPU, 80 names/IDs, every rect, pivotY, and outline coordinates; 79 entire frame records.',
                      inputVisualApproved=data.get('visualApproved', False), inputVisualReview=data.get('visualReview', ''),
                      limitation='Body ROI is a measured boot-stance proxy. Neighbors remain unchanged; residual pose steps are predicted, and actual playback must be reviewed. PivotY is unchanged, but an X change under tilt also changes projected screen Y.')
    output['poseAnchorCorrection'] = provenance
    output['visualApproved'] = False
    output.pop('visualReview', None)
    report = dict(schemaVersion=1, **provenance, evidenceHashes=watched, frames=rows, transitions=transitions,
                  atlasFrameEvidence=atlas_evidence,
                  maximumMeshExtentFitErrorPixels=max(r['meshExtentFitErrorPixels'] for r in rows))
    for path, expected in watched.items():
        require(digest(Path(path)) == expected, 'Evidence changed during processing')
    return output, report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('input', 'spec', 'output', 'report'):
        parser.add_argument(name)
    args = parser.parse_args()
    paths = [project_path(args.output), project_path(args.report)]
    require(paths[0] != paths[1] and all(p.suffix.lower() == '.json' and not p.exists() for p in paths),
            'Output/report must be distinct new JSON files')
    data, report = correct(args.input, args.spec)
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
    print(json.dumps({k: report[k] for k in ('oldPivotX', 'newPivotX', 'appliedAnchorDeltaPixels',
                                           'maximumMeshExtentFitErrorPixels', 'transitions')}, indent=2))


if __name__ == '__main__':
    main()
