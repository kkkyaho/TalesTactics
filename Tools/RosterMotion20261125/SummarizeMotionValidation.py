"""Summarize existing Unity capture evidence; never run Unity or count these checks as tests."""
from __future__ import annotations
import argparse
from collections import Counter
import hashlib
import json
import os
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
BASE = Path("Logs/RosterMotion20261125")
OUTPUT = ROOT / "Docs/RosterMotion20261125/Validation"
IDS = ("cless", "mint", "farah", "tear", "jade", "natalia", "velvet", "alphen", "shionne", "kisara")
DIRECTIONS = ("Front", "Back", "Left", "Right")
PHASES = ("idle", "attack-prepare", "attack-release", "attack-recovery", "damage", "damage-recovery")
PRESERVATION = ("sessionUnchanged", "gridUnchanged", "campaignUnchanged", "randomUnchanged", "contentUnchanged", "cameraRestored", "temporaryObjectsRemoved")


def read(relative: Path):
    raw = (ROOT / relative).read_bytes()
    return json.loads(raw), {"path": relative.as_posix(), "sha256": hashlib.sha256(raw).hexdigest(), "bytes": len(raw)}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def write_new(name, payload):
    text = json.dumps(payload, ensure_ascii=False, indent=2) + "\n"
    require(not re.search(r"[A-Za-z]:[/\\]|/Users/|/home/", text), "Absolute path found in public summary")
    OUTPUT.mkdir(parents=True, exist_ok=True)
    with (OUTPUT / name).open("x", encoding="utf-8", newline="\n") as handle:
        handle.write(text)


def motion_summary():
    captures = []
    for character in IDS:
        for variant in ("BeforeFinal", "AfterFinal"):
            relative = BASE / "Captures" / character / variant / "result.json"
            data, source = read(relative)
            require(data["characterId"] == character, f"Capture owner differs: {relative}")
            require(data["complete"] is True and data["passed"] is True and not data["errors"], f"Capture incomplete or failed: {relative}")
            frames = data["frames"]
            names = [frame["file"] for frame in frames]
            require(all(not Path(name).is_absolute() and ".." not in Path(name).parts and "\\" not in name for name in names), "Unsafe PNG reference")
            directory = ROOT / relative.parent
            actual_pngs = {Path(folder, name).relative_to(directory).as_posix()
                           for folder, _, files in os.walk(directory)
                           for name in files if name.lower().endswith(".png")}
            require(len(names) == len(set(names)) == data["pngCount"] and set(names) == actual_pngs, f"Actual PNG/frame inventory differs: {relative}")
            flags = {name: data[name] for name in PRESERVATION}
            require(all(value is True for value in flags.values()), f"Preservation failed: {relative}")
            snapshot_matches = {name: data[name + "Before"] == data[name + "After"]
                                for name in ("session", "grid", "campaign", "random")}
            require(all(snapshot_matches.values()), f"Before/after snapshot mismatch: {relative}")
            require({frame["viewFacing"] for frame in frames} == set(DIRECTIONS), f"Missing view direction: {relative}")
            directions = []
            observed_closed = set()
            for direction in DIRECTIONS:
                samples = [frame for frame in frames if frame["viewFacing"] == direction]
                phase_counts = Counter(frame["phase"] for frame in samples)
                require(set(phase_counts) == set(PHASES), f"Missing motion phase: {relative}/{direction}")
                closed = [frame for frame in samples if frame["sprite"] == f"{character}-{'pilot' if character == 'alphen' else 'roster'}-idle-{direction.lower()}-2"]
                if closed:
                    observed_closed.add(direction)
                directions.append({"viewFacing": direction, "frameCount": len(samples),
                                   "phaseCounts": dict(sorted(phase_counts.items())),
                                   "closedIdlePoseObserved": bool(closed), "closedIdlePoseSampleCount": len(closed),
                                   "blinkSupplementFrameCount": sum(frame["blinkSupplement"] is True for frame in samples),
                                   "fullBoardFrameCount": sum(frame["fullBoard"] is True for frame in samples)})
            require(observed_closed == set(data["closedIdleDirections"]), f"Closed idle observations disagree: {relative}")
            if variant == "AfterFinal":
                require(observed_closed == set(DIRECTIONS), f"New closed idle pose missing: {relative}")
            captures.append({"characterId": character, "variant": variant, "source": source,
                             "complete": data["complete"], "passed": data["passed"], "status": data["status"],
                             "unityVersion": data["unityVersion"], "campaignStageIndex": data["campaignStage"],
                             "pilot": data["pilot"], "authoredIdleOffsetSeconds": data["authoredIdleOffsetSeconds"],
                             "recordedFrameCount": len(frames), "reportedPngCount": data["pngCount"],
                             "actualPngCount": len(actual_pngs), "pngReferencesMatchPhysicalFiles": True,
                             "discardedColdFrames": data["discardedColdFrames"],
                             "targetFramesPerSecond": data["targetFramesPerSecond"],
                             "captureFramerate": data["captureFramerate"], "timeScale": data["timeScale"],
                             "statePreservation": flags, "snapshotEquality": snapshot_matches,
                             "closedIdleDirections": data["closedIdleDirections"], "directions": directions, "errorCount": len(data["errors"])})
    total_frames = sum(row["recordedFrameCount"] for row in captures)
    total_pngs = sum(row["actualPngCount"] for row in captures)
    require(len(captures) == 20 and total_frames == total_pngs == 5539, "Expected final 20 captures and 5,539 matching PNG records")
    result = {"schemaVersion": 1, "passed": True, "summaryKind": "Read-only aggregation of existing Unity capture evidence",
              "countsAsUnityTest": False, "captureRunCount": len(captures), "characterCount": len(IDS),
              "totalRecordedFrames": total_frames, "totalReportedPngs": sum(row["reportedPngCount"] for row in captures),
              "totalActualPngFiles": total_pngs,
              "beforeRecordedFrames": sum(row["recordedFrameCount"] for row in captures if row["variant"] == "BeforeFinal"),
              "afterRecordedFrames": sum(row["recordedFrameCount"] for row in captures if row["variant"] == "AfterFinal"),
              "afterClosedIdleDirectionObservations": sum(len(row["closedIdleDirections"]) for row in captures if row["variant"] == "AfterFinal"),
              "allStatePreservationChecksPassed": True, "captures": captures,
              "scope": "Ten isolated base-weapon actors, before and after presentation, on the existing episode-six battlefield; four directions each, idle/basic-attack/damage/recovery, using actual advancing Unity frames.",
              "limitations": ["This summary is not an additional EditMode or PlayMode test run.",
                              "PNG capture cadence includes Editor/readback/encoding overhead and is not a performance measurement.",
                              "Closed idle pose means the authored third idle sprite was observed; the back pose has no visible eyelids. Missing before-capture observations do not prove a pre-existing blink never occurred.",
                              "BeforeFinal retains the pre-existing Alphen pilot. The other nine use their earlier presentation.",
                              "No natural campaign progression, damage calculation, save write, Android installation or physical-device gameplay is asserted.",
                              "This mechanical summary does not replace visual review; original result JSON and PNGs remain local."]}
    write_new("motion-summary.json", result)
    return {"motionCaptures": len(captures), "recordedFrames": total_frames, "actualPngs": total_pngs}


def stage_summary(relative: Path):
    require(relative.parent == BASE / "StageMatrix" and relative.suffix == ".json", "Stage result must be an explicit task-local StageMatrix JSON")
    data, source = read(relative)
    rows = data["rows"]
    require(data["passed"] is True and data["restored"] is True and not data["errors"], "Stage matrix has not completed successfully")
    require(data["stageCount"] == 57 and set(data["roster"]) == set(IDS), "Unexpected stage or roster coverage")
    require(data["observedRows"] == data["expectedRows"] == len(rows) == 2280, "Incomplete stage matrix")
    keys = {(row["stage"], row["characterId"], row["view"]) for row in rows}
    expected = {(stage, character, direction) for stage in range(-1, 56) for character in IDS for direction in DIRECTIONS}
    require(keys == expected and len(keys) == len(rows) and all(row["passed"] is True for row in rows), "Missing, duplicate or failed board linkage")
    check_names = ("correct", "anchor", "projected", "grounded", "healthPresent", "finiteHead", "enemyFallback")
    check_counts = {name: sum(row[name] is True for row in rows) for name in check_names}
    require(all(count == 2280 for count in check_counts.values()), "Board linkage subcheck failed")
    history = []
    for name in ("final-result.json", "final-v2-result.json"):
        prior = BASE / "StageMatrix" / name
        if prior == relative or not (ROOT / prior).exists():
            continue
        old, old_source = read(prior)
        require(old["passed"] is False, "Earlier matrix unexpectedly passed; review history selection")
        record = {"source": old_source, "passed": old["passed"], "expectedRows": old["expectedRows"],
                  "observedRows": old["observedRows"], "actualRowCount": len(old["rows"]),
                  "restored": old["restored"], "errorCount": len(old["errors"]),
                  "errorTypes": sorted({message.split(":", 1)[0] for message in old["errors"]})}
        if name == "final-result.json":
            record["diagnosis"] = {"basis": "Validation coordinator investigation; not a field in the original result",
                                   "cause": "The fixture changed model facing without calling the real Board.Sync presentation path; only the verification fixture was corrected.",
                                   "productFilesChangedForCorrection": False}
        else:
            record["errorSummary"] = "The fixture's Single selection found no matching element; this incomplete attempt is not counted as passing coverage."
        history.append(record)
    result = {"schemaVersion": 1, "passed": True, "summaryKind": "Read-only aggregation of an existing actual BoardView matrix",
              "countsAsUnityTest": False, "source": source, "startedUtc": data["startedUtc"], "finishedUtc": data["finishedUtc"],
              "stageCount": 57, "trainingStageCount": 1, "campaignStageCount": 56, "characterCount": 10, "directions": list(DIRECTIONS),
              "expectedRows": data["expectedRows"], "observedRows": data["observedRows"], "actualRowCount": len(rows),
              "uniqueStageCharacterDirectionCount": len(keys), "restored": data["restored"], "errorCount": len(data["errors"]),
              "passedSubcheckCounts": check_counts, "earlierAttempts": history,
              "scope": "Actual BoardView.Build for training plus all 56 campaign stages, ten characters, four view directions: 2,280 unique connections. Temporary sessions bypass progression/deployment eligibility in separate roster batches.",
              "limitations": ["This is renderer linkage verification with forced roster eligibility, not natural campaign traversal, recruitment, unlock, combat damage or save validation.",
                              "This summary and matrix are not added to EditMode/PlayMode test counts.",
                              "No Android installation or physical-device gameplay is claimed.",
                              "Failure evidence is retained separately; only the explicitly selected successful result supplies complete coverage."]}
    write_new("stage-matrix-summary.json", result)
    return {"stageRows": len(rows), "earlierAttempts": len(history)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--stage-result", type=Path, help="Exact repository-relative successful StageMatrix result")
    parser.add_argument("--stage-only", action="store_true", help="Do not recreate the already-written motion summary")
    args = parser.parse_args()
    require(not args.stage_only or args.stage_result is not None, "--stage-only requires --stage-result")
    result = {}
    if not args.stage_only:
        result.update(motion_summary())
    if args.stage_result is not None:
        result.update(stage_summary(args.stage_result))
    print(json.dumps(result, ensure_ascii=False))


if __name__ == "__main__":
    main()
