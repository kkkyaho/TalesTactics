"""Audit the task-owned Preview9 worktree; restore only with explicit --restore.

Run only after its Editor has closed. This script never edits the development
checkout, Git index/HEAD, build output, or an earlier build/restore receipt.
All receipts and byte backups go to a newly created directory outside the project.
No Unity process is started, stopped, or otherwise controlled.
"""
from __future__ import annotations

import argparse
import ctypes
from ctypes import wintypes
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import stat
import subprocess
import sys
import uuid
from datetime import datetime, timezone

PROJECT = Path("D:/TalesTacticsReleasePreview9")
EVIDENCE_ROOT = Path("D:/TalesTacticsBuildArchive/Preview9RestoreEvidence")
EXPECTED_HEAD = "e081a592d398aa0354b04a827fb3833a277552d2"
EXPECTED_TRACKED = 5715
EDITOR_PID = 46536
GLOBAL = "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset"
CONNECT = "ProjectSettings/UnityConnectSettings.asset"
KNOWN = {
    GLOBAL, CONNECT, "Assets/Settings/PC_RPAsset.asset",
    "Assets/TalesTactics/Resources/TalesTactics/KoreanFallback.asset",
    "ProjectSettings/GraphicsSettings.asset", "ProjectSettings/ProjectAuditorSettings.asset",
    "ProjectSettings/ShaderGraphSettings.asset",
    *("Assets/TalesTactics/Resources/TalesTactics/Ornate/" + name + ".png.meta"
      for name in ("CommonIcons", "CompactIcons", "FieldIcons", "Ground", "Panel", "Trees")),
}
ENV = os.environ.copy()
for _key in ("GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE", "GIT_COMMON_DIR"):
    ENV.pop(_key, None)
ENV["GIT_OPTIONAL_LOCKS"] = "0"


def utc():
    return datetime.now(timezone.utc).isoformat()


def digest(data):
    return hashlib.sha256(data).hexdigest()


def git(*args):
    return subprocess.run(["git", "-C", str(PROJECT), *args], env=ENV,
                          stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                          check=True, timeout=60, creationflags=subprocess.CREATE_NO_WINDOW).stdout


def reject_reparse(path, checked=None):
    """Check the leaf every time; cache already checked ancestors per scan only."""
    first = True
    for entry in (path, *path.parents):
        key = os.path.normcase(str(entry.absolute()))
        if not first and checked is not None and key in checked:
            break
        first = False
        if entry.exists() or entry.is_symlink():
            info = entry.lstat()
            if stat.S_ISLNK(info.st_mode) or getattr(info, "st_file_attributes", 0) & 0x400:
                raise RuntimeError(f"Reparse/symlink path refused: {entry}")
        if checked is not None:
            checked.add(key)


def source_path(relative):
    value = PurePosixPath(relative)
    if value.is_absolute() or any(p in ("", ".", "..") or ":" in p or "\\" in p for p in value.parts):
        raise RuntimeError(f"Unsafe tracked path: {relative!r}")
    result = PROJECT.joinpath(*value.parts)
    if os.path.commonpath([str(PROJECT.absolute()), str(result.absolute())]) != str(PROJECT.absolute()):
        raise RuntimeError("Tracked path escapes fixed worktree")
    return result


def require_editor_closed():
    if os.name != "nt":
        raise RuntimeError("This fixed Windows worktree audit must run on Windows")
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel.OpenProcess.argtypes = (wintypes.DWORD, wintypes.BOOL, wintypes.DWORD)
    kernel.OpenProcess.restype = wintypes.HANDLE
    kernel.GetExitCodeProcess.argtypes = (wintypes.HANDLE, ctypes.POINTER(wintypes.DWORD))
    kernel.GetExitCodeProcess.restype = wintypes.BOOL
    kernel.CloseHandle.argtypes = (wintypes.HANDLE,)
    handle = kernel.OpenProcess(0x1000, False, EDITOR_PID)
    if handle:
        try:
            code = wintypes.DWORD()
            if not kernel.GetExitCodeProcess(handle, ctypes.byref(code)) or code.value == 259:
                raise RuntimeError(f"PID {EDITOR_PID} is still alive or cannot be verified closed")
        finally:
            kernel.CloseHandle(handle)
    elif ctypes.get_last_error() != 87:  # ERROR_INVALID_PARAMETER: PID no longer exists.
        raise RuntimeError(f"Cannot verify PID {EDITOR_PID} is absent; Windows error {ctypes.get_last_error()}")
    # Also reject a replacement Editor or remaining import worker for this project.
    command = (
        "$ErrorActionPreference='Stop'; "
        "@(Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" | "
        "Where-Object {$_.CommandLine -and $_.CommandLine.Replace('\\','/')."
        "ToLowerInvariant().Contains('d:/talestacticsreleasepreview9')} | "
        "Select-Object -ExpandProperty ProcessId) | ConvertTo-Json -Compress"
    )
    result = subprocess.run(["powershell.exe", "-NoProfile", "-NonInteractive", "-Command", command],
                            stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True,
                            timeout=30, creationflags=subprocess.CREATE_NO_WINDOW)
    text = result.stdout.decode("utf-8-sig").strip()
    if text and json.loads(text):
        raise RuntimeError("A Unity Editor/import worker still references the release worktree")


def require_context():
    require_editor_closed()
    reject_reparse(PROJECT)
    reject_reparse(EVIDENCE_ROOT)
    top = git("rev-parse", "--show-toplevel").decode().strip()
    if os.path.normcase(os.path.abspath(top)) != os.path.normcase(str(PROJECT.absolute())):
        raise RuntimeError("Unexpected Git worktree root")
    head = git("rev-parse", "HEAD").decode().strip()
    if head != EXPECTED_HEAD:
        raise RuntimeError(f"HEAD must remain {EXPECTED_HEAD}; found {head}")
    if git("diff", "--cached", "--name-only", "-z", "HEAD", "--"):
        raise RuntimeError("Staged changes are refused; this tool never edits the Git index")
    if git("ls-files", "--others", "--exclude-standard", "-z"):
        raise RuntimeError("Untracked nonignored worktree files are refused")


def tree_entries():
    entries = []
    for record in git("ls-tree", "-r", "-z", "HEAD").split(b"\0"):
        if not record:
            continue
        meta, path = record.split(b"\t", 1)
        mode, kind, oid = meta.decode("ascii").split()
        relative = path.decode("utf-8")
        if kind != "blob" or mode not in ("100644", "100755"):
            raise RuntimeError(f"Unsupported tracked object: {relative}")
        source_path(relative)
        entries.append((relative, oid))
    if len(entries) != EXPECTED_TRACKED or len({p.casefold() for p, _ in entries}) != len(entries):
        raise RuntimeError("Pinned tracked-file count or unique Windows paths do not match")
    if not KNOWN.issubset({p for p, _ in entries}):
        raise RuntimeError("A required serializer input is absent from pinned HEAD")
    return entries


class HeadBlobs:
    """One read-only Git process avoids thousands of process launches."""
    def __enter__(self):
        self.process = subprocess.Popen(["git", "-C", str(PROJECT), "cat-file", "--batch"], env=ENV,
                                        stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                        stderr=subprocess.DEVNULL, creationflags=subprocess.CREATE_NO_WINDOW)
        return self

    def read(self, oid):
        self.process.stdin.write(oid.encode("ascii") + b"\n")
        self.process.stdin.flush()
        header = self.process.stdout.readline(200).decode("ascii").strip().split()
        if len(header) != 3 or header[0] != oid or header[1] != "blob":
            raise RuntimeError("Unexpected git cat-file response")
        remaining = int(header[2])
        chunks = []
        while remaining:
            chunk = self.process.stdout.read(min(remaining, 1024 * 1024))
            if not chunk:
                raise RuntimeError("Incomplete HEAD blob read")
            chunks.append(chunk)
            remaining -= len(chunk)
        if self.process.stdout.read(1) != b"\n":
            raise RuntimeError("Invalid HEAD blob delimiter")
        return b"".join(chunks)

    def __exit__(self, *_):
        self.process.stdin.close()
        self.process.stdout.close()
        try:
            self.process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            self.process.kill()  # Only this helper's own read-only git process.
            self.process.wait(timeout=10)


def normalized(data):
    text = data.decode("utf-8").replace("\r\n", "\n")
    if "\0" in text:
        raise UnicodeError("Binary content cannot be normalized")
    return re.sub(r"(?m)[ \t]+$", "", text)


def disabled_migration(head):
    return head.replace(
        "  m_TestInitMode: 0\n",
        "  m_DashboardUrl: https://dashboard.unity3d.com\n  m_TestInitMode: 0\n"
        "  InsightsSettings:\n    m_EventUrl: https://engine-data.unity3d.com\n"
        "    m_StackTraceUrl: https://engine-data.services.api.unity.com/v1/stack-traces\n"
        "    m_EngineDiagnosticsEnabled: 0\n    m_Enabled: 0\n"
    ).replace(
        "  CrashReportingSettings:\n    m_EventUrl: https://perf-events.cloud.unity3d.com\n    m_Enabled: 0\n",
        "  CrashReportingSettings:\n    serializedVersion: 2\n    m_EventUrl: https://perf-events.cloud.unity3d.com\n"
        "    m_EnableCloudDiagnosticsReporting: 0\n"
    ).replace(
        "  UnityAnalyticsSettings:\n    m_Enabled: 0\n    m_TestMode: 0\n    m_InitializeOnStartup: 1\n",
        "  UnityAnalyticsSettings:\n    m_Enabled: 0\n    m_TestMode: 0\n    m_InitializeOnStartup: 1\n"
        "    m_PackageRequiringCoreStatsPresent: 0\n"
    )


def classify(path, head, current):
    if head == current:
        return "Identical"
    try:
        a, b = normalized(head), normalized(current)
    except UnicodeError:
        return "Rejected"
    if a == b:
        return "EOLOrTrailingWhitespaceOnly"
    if path == GLOBAL:
        pattern = r"(?m)^    m_RuntimeSettings:\n      m_List:(?: \[\]\n|\n(?:      - rid: [0-9]+\n)+)"
        old, new = re.findall(pattern, a), re.findall(pattern, b)
        empty = "    m_RuntimeSettings:\n      m_List: []\n"
        if len(old) == len(new) == 1 and old[0].count("rid:") == 16 and new[0] == empty and a.replace(old[0], empty) == b:
            return "DerivedRuntimeSettingsClearedByEditorSerialization"
    if path == CONNECT and disabled_migration(a) == b:
        return "KnownDisabledUnityConnectSchemaMigration"
    return "Rejected"


def write_new(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    reject_reparse(path)
    with path.open("xb") as stream:
        stream.write(data)
        stream.flush()
        os.fsync(stream.fileno())


def write_json(path, value):
    write_new(path, (json.dumps(value, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))


def changed_paths():
    paths = {p.decode("utf-8") for p in git("diff", "--no-ext-diff", "--no-textconv", "--name-only", "-z", "HEAD", "--").split(b"\0") if p}
    raw_status = git("status", "--porcelain=v1", "-z", "--untracked-files=all")
    for record in raw_status.split(b"\0"):
        if not record:
            continue
        if record[:3] != b" M ":
            raise RuntimeError("Only unstaged modified tracked files may be restored")
        paths.add(record[3:].decode("utf-8"))
    return paths, raw_status.decode("utf-8")


def scan(entries, candidates, evidence=None):
    rows, targets, checked = [], [], set()
    with HeadBlobs() as blobs:
        for path, oid in entries:
            source = source_path(path)
            reject_reparse(source, checked)
            if not source.is_file():
                raise RuntimeError(f"Tracked file missing: {path}")
            head, current = blobs.read(oid), source.read_bytes()
            category = classify(path, head, current)
            row = {"path": path, "headObject": oid, "headSHA256": digest(head),
                   "currentSHA256": digest(current), "headBytes": len(head), "currentBytes": len(current),
                   "classification": category, "rawEqual": head == current}
            if evidence is not None and (path in candidates or path in KNOWN):
                base = evidence / "versions" / Path(*PurePosixPath(path).parts)
                head_file, current_file = Path(str(base) + ".head"), Path(str(base) + ".before")
                write_new(head_file, head)
                write_new(current_file, current)
                row.update(headEvidence=str(head_file), beforeEvidence=str(current_file))
                if head != current:
                    targets.append(row)
            rows.append(row)
    return rows, targets


def atomic_restore(row, run_id):
    source = source_path(row["path"])
    reject_reparse(source)
    if digest(source.read_bytes()) != row["currentSHA256"]:
        raise RuntimeError(f"Concurrent source change refused: {row['path']}")
    original = Path(row["headEvidence"]).read_bytes()
    if digest(original) != row["headSHA256"]:
        raise RuntimeError("HEAD evidence was changed; refusing restore")
    temporary = source.with_name(source.name + ".preview9-restore-" + run_id + ".tmp")
    try:
        write_new(temporary, original)
        if digest(source.read_bytes()) != row["currentSHA256"]:
            raise RuntimeError("Source changed while preparing atomic restore")
        os.replace(temporary, source)
        if digest(source.read_bytes()) != row["headSHA256"]:
            raise RuntimeError("Restored bytes differ from HEAD backup")
    finally:
        if temporary.exists():
            temporary.unlink()  # Exact temporary file owned by this invocation only.


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--restore", action="store_true", help="Perform the audited restoration; default is audit only")
    args = parser.parse_args(argv)
    receipt = {"schema": 1, "runId": uuid.uuid4().hex, "startedUtc": utc(), "project": str(PROJECT),
               "expectedHead": EXPECTED_HEAD, "restoreRequested": args.restore, "passed": False,
               "restored": [], "scope": "Post-close source restoration only; earlier raw build flags and evidence are untouched."}
    evidence = None
    try:
        require_context()
        entries = tree_entries()
        evidence = EVIDENCE_ROOT / ("Restore-" + receipt["runId"])
        reject_reparse(evidence)
        evidence.mkdir(parents=True, exist_ok=False)
        receipt["evidence"] = str(evidence)
        candidates, receipt["statusBefore"] = changed_paths()
        if not candidates.issubset({p for p, _ in entries}):
            raise RuntimeError("Change inventory includes a path outside pinned tracked files")
        rows, targets = scan(entries, candidates, evidence)
        receipt["trackedCount"] = len(rows)
        receipt["restoreCandidates"] = len(targets)
        write_json(evidence / "audit-before.json", rows)
        write_json(evidence / "restore-plan.json", targets)
        rejected = [row["path"] for row in rows if row["classification"] == "Rejected"]
        if rejected:
            raise RuntimeError("Unapproved semantic changes: " + ", ".join(rejected))
        receipt["semanticAuditPassed"] = True
        if not args.restore:
            receipt.update(status="AuditOnly", passed=True, restorationComplete=False)
        else:
            require_context()
            # Check every candidate before the first write, then each again during replacement.
            for row in targets:
                if digest(source_path(row["path"]).read_bytes()) != row["currentSHA256"]:
                    raise RuntimeError("Source changed after the audit; no restoration begun")
            for index, row in enumerate(targets):
                atomic_restore(row, receipt["runId"])
                receipt["restored"].append({"path": row["path"], "sha256": row["headSHA256"]})
                write_json(evidence / "restored" / (f"{index:04d}.json"), receipt["restored"][-1])
            require_context()
            after, _ = scan(entries, set())
            write_json(evidence / "audit-after.json", after)
            receipt["rawAllTrackedEqualHead"] = all(row["rawEqual"] for row in after)
            receipt["headEquivalentTrackedCount"] = sum(row["classification"] in ("Identical", "EOLOrTrailingWhitespaceOnly") for row in after)
            receipt["statusAfter"] = git("status", "--porcelain=v1", "-z", "--untracked-files=all").decode("utf-8")
            receipt["sourceCleanAfter"] = receipt["statusAfter"] == ""
            receipt["allRestoredFilesExactHeadBytes"] = all(digest(source_path(row["path"]).read_bytes()) == row["headSHA256"] for row in targets)
            receipt["passed"] = (receipt["sourceCleanAfter"] and receipt["headEquivalentTrackedCount"] == EXPECTED_TRACKED and receipt["allRestoredFilesExactHeadBytes"])
            receipt.update(status="Restored" if receipt["passed"] else "VerificationFailed", restorationComplete=receipt["passed"])
    except Exception as error:
        receipt.update(status="RejectedOrFailed", error=f"{type(error).__name__}: {error}", passed=False, restorationComplete=False)
    receipt["finishedUtc"] = utc()
    if evidence is not None:
        write_json(evidence / "restore-result.json", receipt)
    print(json.dumps({key: receipt.get(key) for key in ("status", "passed", "restorationComplete", "evidence", "error")}, ensure_ascii=False))
    return 0 if receipt["passed"] else 2


if __name__ == "__main__":
    raise SystemExit(main())
