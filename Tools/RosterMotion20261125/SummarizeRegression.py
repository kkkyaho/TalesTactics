"""Read completed Unity test evidence and create one privacy-safe summary; never run Unity."""
from __future__ import annotations
import argparse
from collections import Counter
from datetime import datetime
from decimal import Decimal, ROUND_HALF_UP
import hashlib
import json
from pathlib import Path
import re
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
BASE = Path("Logs/RosterMotion20261125/Tests")
DESTINATION = ROOT / "Docs/RosterMotion20261125/Validation"
COUNT_FIELDS = ("total", "passed", "failed", "skipped", "inconclusive")


def require(condition, message):
    if not condition:
        raise ValueError(message)


def fingerprint(path, raw):
    return {"path": path.as_posix(), "sha256": hashlib.sha256(raw).hexdigest(), "bytes": len(raw)}


def build_summary(platform, result_raw, xml_raw, start_raw):
    require(platform in ("edit", "play"), "Expected edit or play platform")
    mode = "EditMode" if platform == "edit" else "PlayMode"
    result = json.loads(result_raw)
    require(result.get("status") == "completed", "Test run is not completed; no passing summary will be written")
    counts = result["summary"]
    require(all(type(counts[name]) is int and counts[name] >= 0 for name in COUNT_FIELDS), "Invalid result counts")
    if platform == "play":
        require(counts["total"] == 321, "The current full PlayMode suite must contain all 321 cases; zero or partial results are not completion")
    require(counts["total"] > 0 and counts["passed"] == counts["total"] and all(counts[name] == 0 for name in ("failed", "skipped", "inconclusive")), "Test run is not an entirely passing completed run")
    cases = result["results"]
    require(len(cases) == counts["total"] and all(case["Status"] == "Passed" for case in cases), "JSON individual results disagree with the passing summary")
    start = json.loads(start_raw)
    start_data = start["data"]
    start_result = start_data["result"]
    parameters = start_data["parameters"]
    require(start["success"] is True and not start.get("errors") and start_data["command"] == "run_tests", "Start evidence is not a successful test invocation")
    require(start_result["Mode"] == mode and parameters["mode"] == ("editor" if platform == "edit" else "playmode"), "Requested platform differs from test start evidence")
    require(start_result["FilterApplied"] in (None, "") and not any(value for key, value in parameters.items() if "filter" in key.lower()), "Filtered results cannot be summarized as the full suite")
    xml = ET.fromstring(xml_raw)
    require(xml.tag == "test-run" and xml.get("result") == "Passed", "XML does not report a completed passed test run")
    xml_counts = {name: int(xml.attrib[name]) for name in COUNT_FIELDS}
    require(xml_counts == counts and int(xml.attrib["testcasecount"]) == counts["total"], "XML aggregate counts differ from JSON")
    xml_cases = xml.findall(".//test-case")
    require(len(xml_cases) == counts["total"] and all(case.attrib["result"] == "Passed" for case in xml_cases), "XML individual cases do not all pass")
    json_cases = Counter((case["FullName"], case["Status"]) for case in cases)
    xml_case_counts = Counter((case.attrib["fullname"], case.attrib["result"]) for case in xml_cases)
    require(json_cases == xml_case_counts, "XML and JSON test names/status multiplicities differ")
    json_duration = Decimal(str(result["duration"]))
    xml_duration = Decimal(xml.attrib["duration"])
    require(json_duration.is_finite() and xml_duration.is_finite() and xml_duration >= 0 and json_duration >= 0, "Invalid test duration")
    precision = Decimal(1).scaleb(json_duration.as_tuple().exponent)
    require(xml_duration.quantize(precision, rounding=ROUND_HALF_UP) == json_duration, "XML duration differs at the precision reported by JSON")
    times = {}
    for key in ("start-time", "end-time"):
        value = xml.attrib[key]
        require(value.endswith("Z"), "XML timestamp must explicitly identify UTC")
        times[key] = datetime.fromisoformat(value.replace("Z", "+00:00"))
    window = (times["end-time"] - times["start-time"]).total_seconds()
    require(window >= 0 and abs(window - float(xml_duration)) <= 1.0, "XML UTC interval and duration are inconsistent at second timestamp precision")
    return {"schemaVersion": 1, "platform": mode, "scope": "Completed full Unity " + mode + " suite in the separate source-only validation checkout",
            "status": result["status"], "passed": True, "summary": {name: counts[name] for name in COUNT_FIELDS},
            "durationSecondsReportedJson": result["duration"], "durationSecondsXml": float(xml_duration),
            "startedUtc": times["start-time"].isoformat().replace("+00:00", "Z"),
            "finishedUtc": times["end-time"].isoformat().replace("+00:00", "Z"),
            "sources": {"resultJson": fingerprint(BASE / ("full-" + platform + "-result.json"), result_raw),
                        "resultXml": fingerprint(BASE / ("full-" + platform + "-result.xml"), xml_raw),
                        "startInvocation": fingerprint(BASE / ("full-" + platform + "-start.json"), start_raw)},
            "crossChecks": {"completedJson": True, "unfilteredStartModeMatches": True,
                            "aggregateCountsMatch": True, "individualNameStatusMultiplicitiesMatch": True,
                            "actualJsonCaseCount": len(cases), "actualXmlCaseCount": len(xml_cases),
                            "durationMatchesAtJsonPrecision": True, "utcReadFromXml": True},
            "countsAsAdditionalUnityTest": False,
            "limitations": ["This is a read-only summary of the existing Unity run, not another test execution.",
                            "Only the named test mode is covered; no result for another mode is implied.",
                            "The JSON duration is rounded; the XML duration and UTC interval are retained separately.",
                            "No standalone Windows player execution, APK installation or Android device gameplay is asserted.",
                            "Raw test names, output, stack traces, absolute checkout paths and user storage contents are omitted."]}



def validated_run_directory(value):
    directory = Path(value)
    require(not directory.is_absolute() and directory.parent == BASE and re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_-]*", directory.name) is not None,
            "Run directory must be one explicit repository-relative Tests/<run> directory for this task")
    resolved = (ROOT / directory).resolve()
    require(resolved.parent == (ROOT / BASE).resolve() and resolved.is_dir(), "Run directory escaped task Tests scope or does not exist")
    return directory


def utc(value):
    require(isinstance(value, str) and value.endswith("Z"), "Run evidence must explicitly identify UTC")
    return datetime.fromisoformat(value.replace("Z", "+00:00"))


def build_api_play_summary(directory, result_raw, xml_raw, started_raw, queued_raw, discovery_raw, attached_raw=None):
    """Default public mode remains strict: every case in this one full run must pass."""
    return _build_api_play_run(directory, result_raw, xml_raw, started_raw, queued_raw, discovery_raw, attached_raw, expected_failed=0)


def _build_api_play_run(directory, result_raw, xml_raw, started_raw, queued_raw, discovery_raw, attached_raw, *, expected_failed):
    require(expected_failed in (0, 1), "Only strict all-pass or the explicit one-failure retry workflow is supported")
    result = json.loads(result_raw)
    require(result.get("status") == "completed", "PlayMode API run is not completed")
    counts = result["summary"]
    require(all(type(counts[name]) is int and counts[name] >= 0 for name in COUNT_FIELDS), "Invalid API result counts")
    require(counts == {"total": 321, "passed": 321 - expected_failed, "failed": expected_failed, "skipped": 0, "inconclusive": 0},
            "API run counts do not match this workflow: all 321 cases must finish, with exactly the explicit expected failure count; zero is not a pass")
    cases = result["results"]
    expected_statuses = Counter({"Passed": 321 - expected_failed})
    if expected_failed:
        expected_statuses["Failed"] = expected_failed
    require(result["expectedCount"] == result["actualLeafCount"] == len(cases) == 321 and Counter(case["Status"] for case in cases) == expected_statuses, "API leaf results disagree with expected counts")
    require(result["runner"] == "Unity TestRunnerApi official mode-wide PlayMode filter", "Unexpected PlayMode API runner")
    started = json.loads(started_raw) if started_raw is not None else None
    attached = json.loads(attached_raw) if attached_raw is not None else None
    queued, discovery = json.loads(queued_raw), json.loads(discovery_raw)
    require(queued["discovered"] == discovery["count"] == 321, "API discovered test counts differ from 321")
    if started is not None:
        require(started["declaredTests"] == started["expectedTests"] == 321, "API declared test count differs from 321")
    else:
        require(attached is not None, "Missing RunStarted callback requires the same-run attached.json evidence")
    require(queued["filter"] == "PlayMode; no testNames override", "Expected the official unfiltered PlayMode execution")
    discovered = Counter(discovery["names"])
    actual = Counter(case["FullName"] for case in cases)
    require(sum(discovered.values()) == len(discovered) == 321 and actual == discovered, "Discovery and completed JSON test names differ")
    run_id = queued["runId"]
    require(isinstance(run_id, str) and str(uuid.UUID(run_id)) == run_id.lower(), "Invalid official queued runId")
    for label, document in (("result", result), ("started", started), ("attached", attached)):
        if document is not None and "runId" in document:
            require(document["runId"] == run_id, label + " runId differs from the official queued runId")
    xml = ET.fromstring(xml_raw)
    require(xml.tag == "test-run" and xml.attrib["result"] in (("Failed", "Failed(Child)") if expected_failed else ("Passed",)), "API XML run status differs from expected counts")
    require({name: int(xml.attrib[name]) for name in COUNT_FIELDS} == counts and int(xml.attrib["testcasecount"]) == 321, "API XML aggregate counts differ")
    xml_cases = xml.findall(".//test-case")
    require(len(xml_cases) == 321 and Counter(case.attrib["result"] for case in xml_cases) == expected_statuses, "API XML leaf cases disagree with expected counts")
    require(Counter((case["FullName"], case["Status"]) for case in cases) == Counter((case.attrib["fullname"], case.attrib["result"]) for case in xml_cases), "API XML and JSON full name/status multiplicities differ")
    xml_begun, xml_finished = utc(xml.attrib["start-time"]), utc(xml.attrib["end-time"])
    authoritative_start = utc(result["startedUtc"]) if result.get("startedUtc") else xml_begun
    authoritative_finish = utc(result["finishedUtc"]) if result.get("finishedUtc") else xml_finished
    require(abs((xml_begun - authoritative_start).total_seconds()) < 1 and abs((xml_finished - authoritative_finish).total_seconds()) < 1,
            "API result UTC and XML second-resolution timestamps do not identify the same interval")
    discovered_at, queued_at = utc(discovery["utc"]), utc(queued["utc"])
    start_precision = 0 if result.get("startedUtc") else 1
    finish_precision = 0 if result.get("finishedUtc") else 1
    require(authoritative_start <= authoritative_finish and discovered_at <= queued_at and
            (discovered_at - authoritative_start).total_seconds() <= start_precision and
            (queued_at - authoritative_finish).total_seconds() <= finish_precision,
            "Discovery/queue/start/finish chronology is inconsistent")
    if started is not None:
        require(abs((utc(started["startedUtc"]) - authoritative_start).total_seconds()) < 1,
                "RunStarted callback and authoritative result/XML UTC differ beyond timestamp precision")
    if attached is not None:
        require(attached["scope"] == "Collector reattached after PlayMode lifecycle; authoritative RunFinished tree supplies every result, including earlier tests. No new test execution started.",
                "Attachment does not identify same-run callback recovery")
        attached_at = utc(attached["utc"])
        require(queued_at <= attached_at and (authoritative_start - attached_at).total_seconds() <= start_precision and
                (attached_at - authoritative_finish).total_seconds() <= finish_precision and type(attached["frame"]) is int and attached["frame"] >= 0,
                "Attachment timestamp is outside the selected run")
    if result.get("collectedUtc"):
        require(utc(result["collectedUtc"]) >= authoritative_finish, "Collection precedes authoritative result completion")
    duration_json, duration_xml = Decimal(str(result["duration"])), Decimal(xml.attrib["duration"])
    require(duration_json.is_finite() and duration_xml.is_finite() and duration_json >= 0 and duration_xml >= 0, "Invalid API duration")
    precision = Decimal(1).scaleb(max(duration_json.as_tuple().exponent, duration_xml.as_tuple().exponent))
    require(duration_json.quantize(precision, rounding=ROUND_HALF_UP) == duration_xml.quantize(precision, rounding=ROUND_HALF_UP), "API JSON/XML durations differ at common reported precision")
    require(xml_finished >= xml_begun and abs((xml_finished - xml_begun).total_seconds() - float(duration_xml)) <= 1,
            "API XML UTC interval and duration are inconsistent at second precision")
    source_items = [("resultJson", "full-play-result.json", result_raw), ("resultXml", "full-play-result.xml", xml_raw),
                    ("queued", "queued.json", queued_raw), ("discovery", "discovery.json", discovery_raw)]
    if started_raw is not None:
        source_items.append(("started", "started.json", started_raw))
    if attached_raw is not None:
        source_items.append(("attached", "attached.json", attached_raw))
    return {"schemaVersion": 2, "platform": "PlayMode", "scope": "Completed full Unity TestRunnerApi PlayMode suite in the separate source-only validation checkout",
            "status": result["status"], "passed": expected_failed == 0, "summary": counts,
            "runDirectory": directory.as_posix(), "runId": run_id, "runner": result["runner"], "xmlRunResult": xml.attrib["result"],
            "startedUtc": xml_begun.isoformat().replace("+00:00", "Z") if started is None else authoritative_start.isoformat().replace("+00:00", "Z"),
            "startTimestampSource": "XML test-run start-time; RunStarted callback missing" if started is None else "Authoritative result time cross-checked with RunStarted callback and XML",
            "finishedUtc": authoritative_finish.isoformat().replace("+00:00", "Z"),
            "resultStartedUtc": result.get("startedUtc"), "resultFinishedUtc": result.get("finishedUtc"), "collectedUtc": result.get("collectedUtc"),
            "discoveryUtc": discovery["utc"], "queuedUtc": queued["utc"],
            "xmlStartedUtc": xml_begun.isoformat().replace("+00:00", "Z"), "xmlFinishedUtc": xml_finished.isoformat().replace("+00:00", "Z"),
            "durationSecondsReportedJson": result["duration"], "durationSecondsXml": float(duration_xml),
            "sources": {label: fingerprint(directory / name, raw) for label, name, raw in source_items},
            "crossChecks": {"completedJson": True, "officialModeWideFilter": True, "declaredTests": started["declaredTests"] if started else None,
                            "discoveredTests": discovery["count"], "actualJsonCaseCount": len(cases), "actualXmlCaseCount": len(xml_cases),
                            "discoveryAndCompletedNamesMatch": True, "individualNameStatusMultiplicitiesMatch": True,
                            "aggregateCountsMatch": True, "resultAndXmlUtcIntervalMatch": True,
                            "durationMatchesAtCommonReportedPrecision": True, "runStartedCallbackPresent": started is not None},
            "callbackRecovery": {"attached": attached is not None, "attachedUtc": attached["utc"] if attached else None,
                                 "scope": attached["scope"] if attached else None,
                                 "limitation": "RunStarted was not captured after PlayMode lifecycle callback loss. The same run's authoritative RunFinished tree and XML supply all 321 results; this is not a new execution." if started is None else None},
            "runIdBinding": {"officialSource": "queued.json", "resultContainsRunId": "runId" in result, "startedContainsRunId": started is not None and "runId" in started,
                             "basis": "All available records come from one explicitly selected task-local execution directory; discovery and completed names, queue/attachment chronology, authoritative result UTC and XML UTC are cross-checked. The collector requires a fresh output directory.",
                             "limitation": "The current result/started schema does not carry the official runId. No missing runId is invented; same-folder chronology and the 321-name equality link those records to queued.json."},
            "countsAsAdditionalUnityTest": False,
            "limitations": ["This summary is not another Unity test run. Only the explicitly selected completed API attempt is covered.",
                            "The official runId is recorded in queued.json; the schema linkage and any missing start callback are disclosed above.",
                            "XML UTC fields have one-second precision. When RunStarted is missing, its start time is taken from XML, never fabricated.",
                            "No standalone Windows player execution, APK installation or Android-device gameplay is asserted.",
                            "Raw names, output, stack traces, absolute checkout paths and user storage contents are omitted."]}


def validated_test_file(value):
    path = Path(value)
    require(not path.is_absolute() and path.parent == BASE and re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_.-]*", path.name) is not None,
            "Retry evidence must be an explicit repository-relative file directly inside this task's Tests directory")
    resolved = (ROOT / path).resolve()
    require(resolved.parent == (ROOT / BASE).resolve() and resolved.is_file(), "Retry evidence escaped task Tests scope or does not exist")
    return path


def build_retry_summary(failed_name, result_path, result_raw, xml_path, xml_raw, start_path, start_raw, full_finished):
    result, invocation = json.loads(result_raw), json.loads(start_raw)
    expected_counts = {"total": 1, "passed": 1, "failed": 0, "skipped": 0, "inconclusive": 0}
    require(result.get("status") == "completed" and result["summary"] == expected_counts,
            "The isolated retry must complete exactly one passed test with no failures, skips or inconclusive results")
    require(all(type(result["summary"][field]) is int for field in COUNT_FIELDS), "Invalid retry count types")
    cases = result["results"]
    require(len(cases) == 1 and cases[0]["FullName"] == failed_name and cases[0]["Status"] == "Passed",
            "Retry test must be exactly the full run's sole failed test and must actually pass")
    data = invocation["data"]
    parameters, started = data["parameters"], data["result"]
    short_name = failed_name.rsplit(".", 1)[-1]
    require(invocation["success"] is True and not invocation.get("errors") and data["command"] == "run_tests",
            "Retry invocation is not a successful run_tests call")
    require(parameters["mode"] == "playmode" and started["Mode"] == "PlayMode" and
            parameters.get("filter") == short_name and started["FilterApplied"] == "testname: " + short_name and
            not any(value for key, value in parameters.items() if "filter" in key.lower() and key != "filter"),
            "Retry invocation mode/filter does not select exactly the failed method")
    require(started.get("success") is True and started.get("result") == "running", "Retry start must show an accepted asynchronous invocation")
    xml = ET.fromstring(xml_raw)
    require(xml.tag == "test-run" and xml.attrib["result"] == "Passed" and int(xml.attrib["testcasecount"]) == 1 and
            {field: int(xml.attrib[field]) for field in COUNT_FIELDS} == expected_counts, "Retry XML does not report exactly one passing case")
    xml_cases = xml.findall(".//test-case")
    require(len(xml_cases) == 1 and xml_cases[0].attrib["fullname"] == failed_name and xml_cases[0].attrib["result"] == "Passed",
            "Retry XML and JSON do not name the same single passing case")
    duration_json, duration_xml = Decimal(str(result["duration"])), Decimal(xml.attrib["duration"])
    require(duration_json.is_finite() and duration_xml.is_finite() and duration_json >= 0 and duration_xml >= 0, "Invalid retry duration")
    precision = Decimal(1).scaleb(duration_json.as_tuple().exponent)
    require(duration_xml.quantize(precision, rounding=ROUND_HALF_UP) == duration_json, "Retry JSON/XML duration differs at JSON precision")
    begun, finished = utc(xml.attrib["start-time"]), utc(xml.attrib["end-time"])
    require(begun > utc(full_finished) and finished >= begun and abs((finished - begun).total_seconds() - float(duration_xml)) <= 1,
            "Retry XML interval is inconsistent or does not follow the failed full run")
    return {"status": "completed", "passed": True, "summary": expected_counts, "testFullName": failed_name,
            "startedUtc": begun.isoformat().replace("+00:00", "Z"), "finishedUtc": finished.isoformat().replace("+00:00", "Z"),
            "timestampSource": "Actual Unity XML test-run UTC fields; the CLI start invocation contains no timestamp or runId",
            "durationSecondsReportedJson": result["duration"], "durationSecondsXml": float(duration_xml),
            "sources": {"resultJson": fingerprint(result_path, result_raw), "resultXml": fingerprint(xml_path, xml_raw), "startInvocation": fingerprint(start_path, start_raw)},
            "crossChecks": {"soleFailedFullRunNameMatches": True, "actualJsonCaseCount": 1, "actualXmlCaseCount": 1,
                            "individualNameStatusMatch": True, "exactMethodFilterMatches": True, "aggregateCountsMatch": True,
                            "durationMatchesAtJsonPrecision": True, "xmlIntervalFollowsFullRun": True},
            "limitation": "The asynchronous invocation's initial zero counts are not results. Its selected name/mode and the actual completed JSON/XML case link the retry; no missing timestamp or runId is fabricated."}


def validate_source_identity(identity_path, identity_raw, freeze_path, freeze_raw, full_raw, retry_raw, full_start, retry_finish):
    identity, freeze = json.loads(identity_raw), json.loads(freeze_raw)
    sha = lambda raw: hashlib.sha256(raw).hexdigest()
    require(identity.get("passed") is True and identity["fileCountPerCheckout"] == 83 and identity["sourceFreezeSHA256"] == sha(freeze_raw),
            "Post-run source identity does not match the fixed 83-file manifest")
    require(identity["fullResultSHA256"] == sha(full_raw) and identity["retestResultSHA256"] == sha(retry_raw),
            "Post-run source identity does not bind these exact full/retry JSON bytes")
    require(utc(identity["checkedUtc"]) >= utc(retry_finish) and utc(identity["sourceFreezeLastWriteUtc"]) < utc(full_start),
            "Source identity chronology does not describe a post-retry check of the earlier manifest")
    files = freeze["files"]
    require(len(files) == 83 and len({item["relative"] for item in files}) == 83, "Frozen source list must contain 83 unique files")
    expected = {}
    for item in files:
        relative = Path(item["relative"])
        require(not relative.is_absolute() and ".." not in relative.parts and (relative.parts[0] == "Assets" or relative.as_posix() == "Docs/RosterMotion20261125/art-layout.json") and
                re.fullmatch(r"[0-9a-f]{64}", item["sha256"]) and type(item["bytes"]) is int and item["bytes"] >= 0,
                "Unsafe or invalid frozen source entry")
        expected[item["relative"]] = item
    rows = identity["rows"]
    required_keys = {(checkout, relative) for checkout in ("root", "validation") for relative in expected}
    actual_keys = [(row["checkout"], row["relative"]) for row in rows]
    require(len(rows) == 166 and len(set(actual_keys)) == 166 and set(actual_keys) == required_keys, "Source identity must cover the exact 83 files in both checkouts")
    for row in rows:
        item = expected[row["relative"]]
        require(row.get("passed") is True and row["sha256"] == row["expectedSHA256"] == item["sha256"], "Source identity has a mismatched source hash")
    checkout_roots = {"root": ROOT, "validation": ROOT / "Builds/SourceOnlyPublish20261118/Project"}
    for checkout, relative in sorted(required_keys):
        path = checkout_roots[checkout] / relative
        require(path.resolve().is_relative_to(checkout_roots[checkout].resolve()), "Frozen source resolved outside its checkout")
        current = path.read_bytes()
        require(sha(current) == expected[relative]["sha256"] and len(current) == expected[relative]["bytes"], "Current source differs from the validated freeze: " + checkout + "/" + relative)
    return {"passed": True, "evidenceKind": "Post-run source identity check and a fresh read-only rehash during summary generation",
            "sourceFreezeSHA256": sha(freeze_raw), "fileCountPerCheckout": 83, "checkoutCount": 2, "recordedHashComparisons": 166,
            "currentHashAndByteCountComparisons": 166, "checkedUtc": identity["checkedUtc"],
            "sourceFreezeLastWriteUtcReported": identity["sourceFreezeLastWriteUtc"],
            "sources": {"identity": fingerprint(identity_path, identity_raw), "freezeManifest": fingerprint(freeze_path, freeze_raw)},
            "coordinatorRecord": "The fixed manifest was established before regression. The coordinator and validation agent report no product or test edits between the full run and isolated retry; only documentation and tools were edited.",
            "limitations": ["The run-result JSON files do not contain execution-time source hashes. No retrospective before/after snapshot at each execution boundary is claimed.",
                            "The manifest file timestamp is supporting filesystem metadata, not independent proof of historical source contents. The recorded and freshly rehashed source identity is reported separately from the coordinator's unchanged-source record."]}


def build_play_retry_summary(directory, named, retry_paths, retry_raw, identity_path, identity_raw, freeze_path, freeze_raw):
    full = _build_api_play_run(directory, named["full-play-result.json"], named["full-play-result.xml"], named.get("started.json"),
                               named["queued.json"], named["discovery.json"], named.get("attached.json"), expected_failed=1)
    failed = [case for case in json.loads(named["full-play-result.json"])["results"] if case["Status"] == "Failed"]
    require(len(failed) == 1, "Retry workflow requires exactly one failed full-run case")
    retry = build_retry_summary(failed[0]["FullName"], retry_paths[0], retry_raw[0], retry_paths[1], retry_raw[1], retry_paths[2], retry_raw[2], full["finishedUtc"])
    identity = validate_source_identity(identity_path, identity_raw, freeze_path, freeze_raw, named["full-play-result.json"], retry_raw[0], full["startedUtc"], retry["finishedUtc"])
    return {"schemaVersion": 3, "platform": "PlayMode", "status": "completed-with-successful-isolated-retry",
            "scope": "Full 321-case Unity PlayMode run followed by one unchanged-source isolated retry; the initial full run remains failed",
            "fullRunPassed": False, "retryPassed": True, "allCasesCoveredByPassingResults": True,
            "coverage": {"distinctTestCases": 321, "testExecutionsAcrossRuns": 322, "passedInFullRun": 320, "passedOnlyInIsolatedRetry": 1},
            "fullRun": full, "isolatedRetry": retry, "sourceIdentity": identity,
            "failureReview": {"testFullName": failed[0]["FullName"], "originalFailure": "The full run contains one failed case. Its original failure details remain in the linked JSON and XML evidence.",
                              "finding": "The coordinator reports unchanged test and product code. These artifacts do not establish why the original assertion failed, and a passing isolated retry does not prove that cause is resolved.",
                              "productOrTestChangesForRetry": False},
            "countsAsAdditionalUnityTest": False,
            "limitations": ["The full run had 320 passes and one failure; the successful isolated retry does not convert it into a 321/321 passing full run.",
                            "All 321 distinct cases have a passing result across two executions, not in one execution. The original failed result remains linked and retained.",
                            "No source hashes recorded at execution time are invented; the post-run identity evidence and coordinator record have separate scopes.",
                            "The original timing-related failure cause remains unconfirmed and may represent a flaky fixture or another transient issue.",
                            "This read-only summary is not another Unity test, Windows player run, APK installation or Android-device gameplay check.",
                            "Only the affected test's identifier is included; raw stack traces, absolute checkout paths and user storage contents are omitted."]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--platform", required=True, choices=("edit", "play"))
    parser.add_argument("--run-directory", type=Path, help="Official API PlayMode attempt under Logs/RosterMotion20261125/Tests/<run>")
    parser.add_argument("--retry-json", type=Path, help="Explicit completed isolated retry JSON under this task's Tests directory")
    parser.add_argument("--retry-xml", type=Path, help="Actual Unity XML for the same isolated retry")
    parser.add_argument("--retry-start", type=Path, help="Actual filtered CLI start invocation for the isolated retry")
    parser.add_argument("--source-identity", type=Path, help="Post-run 83-file identity evidence; default Tests/full-retest-source-identity.json")
    args = parser.parse_args()
    retry_options = (args.retry_json, args.retry_xml, args.retry_start)
    has_retry = any(value is not None for value in retry_options)
    require(not has_retry or (all(value is not None for value in retry_options) and args.platform == "play" and args.run_directory is not None),
            "Explicit retry mode requires --platform play, --run-directory and all three --retry-* evidence files")
    require(args.source_identity is None or has_retry, "--source-identity is valid only with the explicit isolated retry workflow")
    output = DESTINATION / ("editmode.json" if args.platform == "edit" else "playmode.json")
    require(not output.exists(), "Summary already exists; it will not be overwritten")
    if args.run_directory is not None:
        require(args.platform == "play", "--run-directory is reserved for official API PlayMode evidence")
        directory = validated_run_directory(args.run_directory)
        relatives = [directory / name for name in ("full-play-result.json", "full-play-result.xml", "queued.json", "discovery.json")]
        require(all((ROOT / path).is_file() for path in relatives), "API attempt is incomplete: result JSON/XML, queued and discovery must all exist")
        relatives += [directory / name for name in ("started.json", "attached.json") if (ROOT / directory / name).is_file()]
        raw = [(ROOT / path).read_bytes() for path in relatives]
        named = {path.name: value for path, value in zip(relatives, raw)}
        if has_retry:
            retry_paths = [validated_test_file(value) for value in retry_options]
            identity_path = validated_test_file(args.source_identity or BASE / "full-retest-source-identity.json")
            freeze_path = Path("Logs/RosterMotion20261125/validation-product-freeze.json")
            added_paths = retry_paths + [identity_path, freeze_path]
            added_raw = [(ROOT / path).read_bytes() for path in added_paths]
            summary = build_play_retry_summary(directory, named, retry_paths, added_raw[:3], identity_path, added_raw[3], freeze_path, added_raw[4])
            relatives += added_paths
            raw += added_raw
        else:
            summary = build_api_play_summary(directory, named["full-play-result.json"], named["full-play-result.xml"], named.get("started.json"), named["queued.json"], named["discovery.json"], named.get("attached.json"))
    else:
        relatives = [BASE / ("full-" + args.platform + "-" + suffix) for suffix in ("result.json", "result.xml", "start.json")]
        require(all((ROOT / path).is_file() for path in relatives), "Completed result JSON/XML and start invocation must all exist")
        raw = [(ROOT / path).read_bytes() for path in relatives]
        summary = build_summary(args.platform, *raw)
    require(all((ROOT / path).read_bytes() == original for path, original in zip(relatives, raw)), "Source evidence changed during summarization")
    text = json.dumps(summary, ensure_ascii=False, indent=2) + "\n"
    require(not re.search(r"[A-Za-z]:[/\\]|/Users/|/home/", text), "Absolute path found in public summary")
    DESTINATION.mkdir(parents=True, exist_ok=True)
    with output.open("x", encoding="utf-8", newline="\n") as handle:
        handle.write(text)
    report = {"platform": summary["platform"], "output": output.relative_to(ROOT).as_posix()}
    if has_retry:
        report.update({key: summary[key] for key in ("fullRunPassed", "retryPassed", "allCasesCoveredByPassingResults", "coverage")})
    else:
        report.update({"passed": summary["passed"], "summary": summary["summary"]})
    print(json.dumps(report))


if __name__ == "__main__":
    main()
