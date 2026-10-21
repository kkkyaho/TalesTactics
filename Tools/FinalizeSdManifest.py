"""Finalize the explicitly approved SD replacement manifest; never edits image pixels."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DIRECTORY = ROOT / "Docs/SdCharacters"

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def finalize(check_only=False):
    path = DIRECTORY / "production-manifest.json"
    manifest = json.loads(path.read_text(encoding="utf-8-sig"))
    declared = {job["source"] for job in manifest["jobs"]}
    if len(declared) != len(manifest["jobs"]):
        raise ValueError("Duplicate manifest sources")
    approved = {}
    stale = []
    for report in DIRECTORY.glob("*measurement.json"):
        data = json.loads(report.read_text(encoding="utf-8-sig"))
        if data.get("source") not in declared or not data.get("visualApproved"):
            continue
        source, candidate = ROOT / data["source"], ROOT / data["candidate"]
        if digest(source) != data["sourceSha256"] or digest(candidate) != data["candidateSha256"]:
            stale.append(report.name)
            continue
        if data["source"] in approved:
            raise ValueError("Duplicate approved measurements: " + data["source"])
        approved[data["source"]] = (report, data)
    pending = sorted(declared - approved.keys())
    summary = dict(sheets=len(declared), approved=len(approved), pending=pending, stale=stale)
    if pending or stale:
        print(json.dumps(summary, indent=2))
        return False
    for job in manifest["jobs"]:
        report, data = approved[job["source"]]
        if data["sourceGuid"] != job["sourceGuid"] or data["sourceSha256"] != job["sourceSha256"]:
            raise ValueError("Original identity changed: " + job["source"])
        names = [f["name"] for f in data["frames"]]
        if len(names) != len(set(names)) or sorted(names) != sorted(job["spriteNames"]):
            raise ValueError("Frame coverage differs: " + job["source"])
        job.update(candidate=data["candidate"], measurement=report.relative_to(ROOT).as_posix(),
                   status="approved", visualApproved=True)
    manifest["description"] = "Complete measured and visually approved SD art replacements; Unity application is validated separately."
    summary["spriteNames"] = sum(len(j["spriteNames"]) for j in manifest["jobs"])
    summary["written"] = not check_only
    if not check_only:
        path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(summary, indent=2))
    return True

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--check-only", action="store_true")
    args = parser.parse_args()
    raise SystemExit(0 if finalize(args.check_only) else 1)
