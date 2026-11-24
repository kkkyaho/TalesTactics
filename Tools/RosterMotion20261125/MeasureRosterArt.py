"""Read-only native sprite measurements; never writes image or Unity metadata.

Connected alpha components define poses, rather than quarter-sheet rectangles.
Foot/head values are review candidates, not approved pivots or head anchors.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import io
import json
from pathlib import Path
import re
import runpy

import numpy as np
from PIL import Image

OWNERS = ("cless", "farah", "jade", "kisara", "mint", "natalia", "shionne", "tear", "velvet")
KNOWN_ROWS = {
    "cless": ["front", "back", "left", "right"],
    "farah": ["front", "back", "right", "left"],
    "jade": ["front", "back", "right", "left"],
    "kisara": ["front", "back", "right", "left"],
}
THRESHOLD = 64
HEAD_ALGORITHM_PATH = Path(__file__).resolve().parents[1] / "BuildUnitHeadAnchors.py"
# run_path avoids a __pycache__ write; do not invoke the source tool main.
HEAD_ALGORITHM = runpy.run_path(str(HEAD_ALGORITHM_PATH))["anchor"]


def sha(data):
    return hashlib.sha256(data).hexdigest()


def components(mask):
    """8-connected components using row runs and union-find; no scipy needed."""
    height, width = mask.shape
    parents, ranks, runs = [], [], []

    def find(label):
        while parents[label] != label:
            parents[label] = parents[parents[label]]
            label = parents[label]
        return label

    def union(first, second):
        first, second = find(first), find(second)
        if first == second:
            return
        if ranks[first] < ranks[second]:
            first, second = second, first
        parents[second] = first
        if ranks[first] == ranks[second]:
            ranks[first] += 1

    previous = []
    for y in range(height):
        row = mask[y].astype(np.int8)
        edges = np.flatnonzero(np.diff(np.pad(row, (1, 1))))
        current, pointer = [], 0
        for start, end in zip(edges[::2], edges[1::2]):
            start, end = int(start), int(end)
            label = len(parents)
            parents.append(label)
            ranks.append(0)
            runs.append((start, end, y, label))
            while pointer < len(previous) and previous[pointer][1] < start:
                pointer += 1
            index = pointer
            while index < len(previous) and previous[index][0] <= end:
                union(label, previous[index][2])
                index += 1
            current.append((start, end, label))
        previous = current
    groups = {}
    for start, end, y, label in runs:
        root = find(label)
        if root not in groups:
            groups[root] = {"x0": start, "y0": y, "x1": end, "y1": y + 1,
                            "area": 0, "sumX": 0.0, "sumY": 0, "runs": []}
        group = groups[root]
        group["x0"], group["x1"] = min(group["x0"], start), max(group["x1"], end)
        group["y0"], group["y1"] = min(group["y0"], y), max(group["y1"], y + 1)
        length = end - start
        group["area"] += length
        group["sumX"] += (start + end - 1) * length / 2
        group["sumY"] += y * length
        group["runs"].append((start, end, y))
    return sorted(groups.values(), key=lambda item: item["area"], reverse=True)


def bbox(group):
    return {"x": group["x0"], "y": group["y0"], "width": group["x1"] - group["x0"],
            "height": group["y1"] - group["y0"]}


def component_mask(group):
    mask = np.zeros((group["y1"] - group["y0"], group["x1"] - group["x0"]), dtype=bool)
    for start, end, y in group["runs"]:
        mask[y - group["y0"], start - group["x0"]:end - group["x0"]] = True
    return mask


def consecutive_edges(valid, count=3):
    indices = np.flatnonzero(np.convolve(valid.astype(np.int32), np.ones(count, np.int32), mode="valid") == count)
    return None if not len(indices) else (int(indices[0]), int(indices[-1]) + count - 1)


def body_candidates(group):
    mask = component_mask(group)
    height, width = mask.shape
    ink = mask.sum(axis=0)
    # A broad horizontal mass window resists a tall but thin staff/spear.
    window = max(8, min(width, int(round(width * 0.42))))
    weighted = np.convolve(ink, np.ones(window, np.int32), mode="valid")
    left = int(np.argmax(weighted)) if len(weighted) else 0
    right = min(width, left + window)
    band_ink = ink[left:right]
    mass = int(band_ink.sum())
    center_x = float(np.dot(np.arange(left, right), band_ink) / mass) if mass else (width - 1) / 2
    half_width = max(4, int(round(window * 0.40)))
    band_left, band_right = max(0, int(round(center_x)) - half_width), min(width, int(round(center_x)) + half_width + 1)
    row_ink = mask[:, band_left:band_right].sum(axis=1)
    min_row_ink = max(4, int(round((band_right - band_left) * 0.12)))
    edges = consecutive_edges(row_ink >= min_row_ink, count=min(3, height))
    head_y, foot_y = edges if edges else (0, height - 1)
    lower_rows = mask[max(0, foot_y - 3):min(height, foot_y + 1), band_left:band_right]
    foot_columns = np.flatnonzero(lower_rows.any(axis=0)) + band_left
    foot_x = float(np.median(foot_columns)) if len(foot_columns) else center_x
    absolute_x, absolute_y = group["x0"], group["y0"]
    # Keep literal silhouette bottom too: disagreement is evidence for weapon/cloth ambiguity.
    raw_bottom_columns = np.flatnonzero(mask[-1])
    raw_bottom_x = float(np.median(raw_bottom_columns)) if len(raw_bottom_columns) else center_x
    lower = mask[max(0, foot_y - 11):min(height, foot_y + 1), :]
    support = lower.sum(axis=0)
    support_edges = np.flatnonzero(np.diff(np.pad((support > 0).astype(np.int8), (1, 1))))
    lower_groups = []
    for start, end in zip(support_edges[::2], support_edges[1::2]):
        start, end = int(start), int(end)
        area = int(support[start:end].sum())
        if area <= 5:
            continue
        low_y, low_x = np.nonzero(lower[:, start:end])
        lower_groups.append({"x0": absolute_x + start, "x1Exclusive": absolute_x + end,
                             "xCenter": absolute_x + (start + end - 1) / 2,
                             "lowestY": absolute_y + max(0, foot_y - 11) + int(low_y.max()),
                             "areaPixels": area, "widthPixels": end - start,
                             "couldBeThinWeapon": end - start <= max(5, round(width * 0.04))})
    flags = ["heuristic_requires_visual_review"]
    if height - 1 - foot_y > 5:
        flags.append("silhouette_extends_below_body_foot_candidate")
    if head_y > 8:
        flags.append("silhouette_extends_above_body_head_candidate")
    return {
        "bodyCenterX": round(absolute_x + center_x, 3),
        "bodyBand": {"x0": absolute_x + band_left, "x1Exclusive": absolute_x + band_right,
                     "minimumOpaquePixelsPerRow": min_row_ink, "consecutiveRows": min(3, height)},
        "headTopCandidate": {"x": round(absolute_x + center_x, 3), "y": absolute_y + head_y},
        "footBottomCandidate": {"x": round(absolute_x + foot_x, 3), "y": absolute_y + foot_y},
        "literalSilhouetteBottom": {"x": round(absolute_x + raw_bottom_x, 3), "y": group["y1"] - 1},
        "lowerSilhouetteSupportGroups": lower_groups,
        "bodyHeightCandidatePixels": foot_y - head_y + 1,
        "headToLiteralBottomPixels": height - head_y,
        "flags": flags,
    }


def crown_candidate(alpha, group):
    """Reuse only the original broad-run algorithm, never its old-art windows."""
    x0, y0, x1, y1 = (group[key] for key in ("x0", "y0", "x1", "y1"))
    mask = alpha[y0:y1, x0:x1] >= 128
    try:
        u, v, details = HEAD_ALGORITHM(mask)
        return {"method": "BuildUnitHeadAnchors.anchor; alpha>=128; old HEAD_WINDOWS excluded",
                "headTopCandidate": {"x": round(x0 + u * (x1 - x0), 3), "y": y0 + details["crownTop"]},
                "normalizedInAlpha64BBox": {"u": u, "v": v},
                "detailsRelativeToAlpha64BBox": details, "requiresVisualReview": True}
    except ValueError as error:
        return {"error": str(error), "requiresVisualReview": True}


def edge_report(alpha, threshold):
    result = {}
    edges = {"top": alpha[0, :], "bottom": alpha[-1, :], "left": alpha[:, 0], "right": alpha[:, -1]}
    for name, edge in edges.items():
        positions = np.flatnonzero(edge >= threshold)
        result[name] = {"count": int(len(positions)), "first": int(positions[0]) if len(positions) else None,
                        "last": int(positions[-1]) if len(positions) else None}
    result["anyContact"] = any(result[name]["count"] > 0 for name in edges)
    return result


def load_direction_info(root, owner):
    path = root / "Docs" / "RosterMotion20261125" / ("art-" + owner + ".json")
    if not path.exists():
        return KNOWN_ROWS.get(owner), "task instructions" if owner in KNOWN_ROWS else None, None
    raw = path.read_bytes()
    record = json.loads(raw.decode("utf-8-sig"))
    rows = record.get("observedRowDirections") or record.get("layout", {}).get("rows") or KNOWN_ROWS.get(owner)
    valid = isinstance(rows, list) and len(rows) == 4 and set(rows) == {"front", "back", "left", "right"}
    if not valid:
        rows = None
    source = path.relative_to(root).as_posix() if rows else None
    return rows, source, {"path": path.relative_to(root).as_posix(), "sha256": sha(raw)}


def frame_name(owner, kind, direction, column):
    action = "idle" if kind == "idle" else "damage" if column == 3 else "attack"
    index = column if action != "damage" else 0
    return f"{owner}-roster-{action}-{direction or 'unconfirmed'}-{index}"


def measure_sheet(path, root, owner, kind, rows, direction_source):
    raw = path.read_bytes()
    with Image.open(io.BytesIO(raw)) as image:
        rgba = np.asarray(image.convert("RGBA"))
    alpha = rgba[:, :, 3]
    height, width = alpha.shape
    groups = components(alpha >= THRESHOLD)
    minimum_major_area = max(500, round(width * height * 0.0004))
    substantial = [item for item in groups if item["area"] >= minimum_major_area]
    majors = substantial[:16]
    order = sorted(majors, key=lambda item: item["sumY"] / item["area"])
    ordered = []
    for offset in range(0, len(order), 4):
        ordered.extend(sorted(order[offset:offset + 4], key=lambda item: item["sumX"] / item["area"]))
    frames = []
    for index, group in enumerate(ordered):
        row, column = divmod(index, 4)
        direction = rows[row] if rows else None
        measures = body_candidates(group)
        x0, y0, x1, y1 = group["x0"], group["y0"], group["x1"], group["y1"]
        frame = {
            "row": row, "column": column, "direction": direction,
            "name": frame_name(owner, kind, direction, column),
            "alpha64AreaPixels": group["area"], "alpha64BBoxTopLeft": bbox(group),
            "unityRectWithoutPadding": {"x": x0, "y": height - y1, "width": x1 - x0, "height": y1 - y0},
            "alpha64Centroid": {"x": round(group["sumX"] / group["area"], 3), "y": round(group["sumY"] / group["area"], 3)},
            "crossesNominalCell": x0 < column * width / 4 or x1 > (column + 1) * width / 4 or y0 < row * height / 4 or y1 > (row + 1) * height / 4,
            "sheetEdgeContact": {"left": x0 == 0, "top": y0 == 0, "right": x1 == width, "bottom": y1 == height},
            "bodyCandidates": measures,
            "broadRunHeadCandidate": crown_candidate(alpha, group),
        }
        frames.append(frame)
    extras = [item for item in groups if item not in majors and item["area"] > 5]
    after = sha(path.read_bytes())
    return {
        "path": path.relative_to(root).as_posix(), "characterId": owner, "kind": kind,
        "sha256": sha(raw), "bytes": len(raw), "width": width, "height": height, "nativeBytesUnchanged": after == sha(raw),
        "rowDirections": rows, "directionSource": direction_source,
        "alphaThreshold": THRESHOLD, "connectivity": 8, "componentCount": len(groups),
        "minimumMajorComponentArea": minimum_major_area, "substantialComponentCount": len(substantial),
        "majorPoseCount": len(majors), "hasExactly16MajorPoses": len(substantial) == 16,
        "smallNoiseComponentsAtMost5Pixels": sum(item["area"] <= 5 for item in groups),
        "lowAlphaPixelCounts": {"alpha1To5": int(((alpha >= 1) & (alpha <= 5)).sum()),
                                "alpha6To63": int(((alpha >= 6) & (alpha <= 63)).sum())},
        "outerEdgeContact": {"alpha64": edge_report(alpha, 64), "alpha128": edge_report(alpha, 128)},
        "frames": frames,
        "otherComponentsAbove5Pixels": [{"areaPixels": item["area"], "bboxTopLeft": bbox(item)} for item in extras[:64]],
        "otherComponentReportTruncated": len(extras) > 64,
        "warnings": ([] if len(substantial) == 16 else ["major_component_count_requires_review"]) +
                    ([] if rows else ["row_directions_unconfirmed"]) +
                    (["opaque_native_sheet_edge_contact_requires_review"] if edge_report(alpha, 64)["anyContact"] else []),
    }


def old_reference(root, owner, head_anchors):
    path = root / "Assets" / "TalesTactics" / "Art" / "PixelCampaign" / (owner + "-hq.png")
    meta = Path(str(path) + ".meta")
    if not path.exists() or not meta.exists():
        return {"characterId": owner, "available": False}
    raw, meta_raw = path.read_bytes(), meta.read_bytes()
    text = meta_raw.decode("utf-8-sig")
    ppu_match = re.search(r"spritePixelsToUnits:\s*([0-9.eE+-]+)", text)
    pattern = re.compile(r"name:\s*([^\r\n]+)\s+rect:\s+serializedVersion:\s*\d+\s+x:\s*([0-9.eE+-]+)\s+y:\s*([0-9.eE+-]+)\s+width:\s*([0-9.eE+-]+)\s+height:\s*([0-9.eE+-]+)\s+alignment:\s*\d+\s+pivot:\s*\{x:\s*([0-9.eE+-]+),\s*y:\s*([0-9.eE+-]+)\}")
    entries = list(pattern.finditer(text))
    if not entries or ppu_match is None:
        return {"characterId": owner, "available": False, "error": "Original sprite metadata could not be parsed"}
    first = next((item for item in entries if item.group(1).strip() == owner + "-hq_0_0"), entries[0])
    name = first.group(1).strip()
    x, y, w, h = [int(round(float(first.group(index)))) for index in (2, 3, 4, 5)]
    pivot_x, pivot_y = float(first.group(6)), float(first.group(7))
    with Image.open(io.BytesIO(raw)) as image:
        rgba = np.asarray(image.convert("RGBA"))
    height, width = rgba.shape[:2]
    top = height - y - h
    if x < 0 or top < 0 or x + w > width or top + h > height:
        return {"characterId": owner, "available": False, "error": "Native reference sprite rect out of bounds"}
    groups = components(rgba[top:top + h, x:x + w, 3] >= THRESHOLD)
    if not groups:
        return {"characterId": owner, "available": False, "error": "No opaque reference body component"}
    group = groups[0]
    measures = body_candidates(group)
    candidate_height = measures["bodyHeightCandidatePixels"]
    ppu = float(ppu_match.group(1))
    anchors = [item for item in head_anchors if item.get("sprite") == name and item.get("width") == w and item.get("height") == h]
    anchor = anchors[0] if len(anchors) == 1 else None
    anchor_height = (anchor["v"] - pivot_y) * h if anchor else None
    return {
        "characterId": owner, "available": True, "path": path.relative_to(root).as_posix(),
        "sha256": sha(raw), "metadataSha256": sha(meta_raw), "nativeWidth": width, "nativeHeight": height,
        "firstSpriteName": name, "metadataSpriteCount": len(entries), "pixelsPerUnit": ppu,
        "metadataRectUnity": {"x": x, "y": y, "width": w, "height": h},
        "nativeRectTopLeft": {"x": x, "y": top, "width": w, "height": h},
        "metadataPivotNormalized": {"x": pivot_x, "y": pivot_y},
        "metadataPivotPixelsInRect": {"x": pivot_x * w, "y": pivot_y * h},
        "metadataFootTopLeftInRect": {"x": pivot_x * w, "y": h - pivot_y * h},
        "existingHeadAnchorMatches": anchors,
        "existingHeadAnchorToFootPixels": anchor_height,
        "existingHeadAnchorToFootWorldUnitsBeforeCharacterScale": anchor_height / ppu if anchor_height is not None else None,
        "existingHeadAnchorToFootFormula": "(existingAnchor.v - metadataPivot.y) * metadataRect.height",
        "majorBodyComponentBBoxInRect": bbox(group),
        "bodyCandidatesInRect": measures,
        "broadRunHeadCandidateInRect": crown_candidate(rgba[top:top + h, x:x + w, 3], group),
        "bodyHeightCandidateWorldUnitsBeforeCharacterScale": candidate_height / ppu,
        "nativeBytesUnchanged": sha(path.read_bytes()) == sha(raw) and sha(meta.read_bytes()) == sha(meta_raw),
        "warning": "Body/head/foot candidates are alpha-density heuristics; weapons or clothing may still influence them. No PPU is approved by this report.",
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", type=Path, default=Path("Logs/RosterMotion20261125/art-measurements.json"))
    parser.add_argument("--character", action="append", choices=OWNERS)
    args = parser.parse_args()
    root = args.root.resolve()
    output = args.output if args.output.is_absolute() else root / args.output
    owners = args.character or list(OWNERS)
    sheets, references, missing, provenance = [], [], [], []
    anchor_path = root / "Assets/TalesTactics/Resources/TalesTactics/UnitHeadAnchors.json"
    anchor_bytes = anchor_path.read_bytes()
    head_algorithm_bytes = HEAD_ALGORITHM_PATH.read_bytes()
    head_anchors = json.loads(anchor_bytes.decode("utf-8-sig"))["sprites"]
    for owner in owners:
        rows, direction_source, record = load_direction_info(root, owner)
        if record:
            provenance.append(record)
        references.append(old_reference(root, owner, head_anchors))
        for kind in ("idle", "action"):
            path = root / "Assets" / "TalesTactics" / "Art" / "RosterMotion20261125" / owner / (kind + "-v1.png")
            if not path.exists():
                missing.append(path.relative_to(root).as_posix())
                continue
            result = measure_sheet(path, root, owner, kind, rows, direction_source)
            sheets.append(result)
            print(f"{owner}/{kind}: {result['majorPoseCount']} poses; substantial={result['substantialComponentCount']}; outerEdge={result['outerEdgeContact']['alpha64']['anyContact']}", flush=True)
    report = {
        "schemaVersion": 1, "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "purpose": "Read-only native measurements for human-reviewed layout, scale, feet and HP anchors",
        "coordinateSystem": "Image candidates use top-left pixel coordinates; Unity rect conversion is supplied separately",
        "method": "8-connected alpha>=64 components; select the 16 largest substantial components, cluster by centroid rows, never crop at nominal equal cells",
        "pixelsModified": False, "unityMetadataModified": False, "productionJsonModified": False,
        "approvedLandmarks": False, "allInputsPresent": not missing,
        "allReadInputsBytePreserved": all(item.get("nativeBytesUnchanged", True) for item in sheets + references) and sha(anchor_path.read_bytes()) == sha(anchor_bytes) and sha(HEAD_ALGORITHM_PATH.read_bytes()) == sha(head_algorithm_bytes),
        "existingHeadAnchorSource": {"path": anchor_path.relative_to(root).as_posix(), "sha256": sha(anchor_bytes), "entries": len(head_anchors)},
        "headCandidateAlgorithm": {"path": HEAD_ALGORITHM_PATH.relative_to(root).as_posix(), "sha256": sha(head_algorithm_bytes), "oldHeadWindowsApplied": False},
        "sheetsMeasured": len(sheets), "posesMeasured": sum(len(item["frames"]) for item in sheets),
        "missingSources": missing, "artRecordProvenance": provenance, "references": references, "sheets": sheets,
    }
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(str(output), flush=True)
    if not report["allReadInputsBytePreserved"]:
        raise SystemExit("A source changed while it was being measured; report is not valid.")


if __name__ == "__main__":
    main()
