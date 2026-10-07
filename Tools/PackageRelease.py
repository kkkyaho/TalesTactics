"""Versioned, local-only Windows distribution. Python 3.10+, standard library."""
import argparse
import hashlib
import json
import re
import shutil
import subprocess
import tempfile
import zipfile
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REQUIRED = {'TalesTactics.exe', 'UnityPlayer.dll',
            'TalesTactics_Data/globalgamemanagers', 'TalesTactics_Data/boot.config',
            'TalesTactics_Data/Managed/TalesTactics.Runtime.dll',
            'TalesTactics_Data/StreamingAssets/Licenses/NotoSansCJK-OFL.txt',
            'TalesTactics_Data/StreamingAssets/Licenses/NotoSansCJK-NOTICE.txt'}


def digest(path):
    with Path(path).open('rb') as stream:
        return stream_digest(stream)


def stream_digest(stream):
    result = hashlib.sha256()
    for chunk in iter(lambda: stream.read(1024 * 1024), b''):
        result.update(chunk)
    return result.hexdigest()


def read_json(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def safe_name(name):
    if (not name or re.search(r'[\\:<>"|?*]', name) or
            any(p in ('', '.', '..') or p.endswith((' ', '.')) for p in name.split('/'))):
        raise ValueError(f'Unsafe archive path: {name}')
    return name


def excluded_file(path):
    return (any('BackUpThisFolder_ButDontShipItWithYourGame' in p for p in path.parts)
            or path.suffix.lower() in ('.pdb', '.mdb', '.log'))


def verify_build_files(build, report):
    if not report.get('files'):
        raise ValueError('Build report has no file inventory')
    checked = 0
    for entry in report['files']:
        path = Path(entry['path']).resolve()
        if build not in path.parents:
            raise ValueError('Build report file is outside the build directory')
        if excluded_file(path):
            continue
        if not path.is_file() or path.stat().st_size != entry['sizeBytes']:
            raise ValueError(f'Build report file missing or changed: {path.name}')
        checked += 1
    return checked


def verify(archive):
    with zipfile.ZipFile(archive) as z:
        infos = z.infolist()
        names = [safe_name(i.filename) for i in infos]
        if len({n.casefold() for n in names}) != len(names):
            raise ValueError('Duplicate archive entries')
        manifest = json.loads(z.read('manifest.json'))
        if manifest.get('schema') != 1 or manifest.get('kind') != 'release':
            raise ValueError('Unsupported release manifest')
        expected = {'manifest.json'}
        for entry in manifest['files']:
            name = safe_name(entry['path'])
            if name.casefold() in {n.casefold() for n in expected}:
                raise ValueError('Duplicate manifest entries')
            expected.add(name)
            info = z.getinfo(name)
            with z.open(info) as stream:
                if info.file_size != entry['bytes'] or stream_digest(stream) != entry['sha256']:
                    raise ValueError(f'Content mismatch: {name}')
        if expected != set(names) or not REQUIRED.issubset(expected):
            raise ValueError('Missing required or unexpected package files')
    return {'passed': True, 'version': manifest['version'], 'files': len(expected)-1,
            'sha256': digest(archive), 'bytes': Path(archive).stat().st_size}


def package(build, evidence, version, output, notes):
    if not re.fullmatch(r'\d+\.\d+\.\d+(?:-[a-zA-Z0-9]+(?:[.-][a-zA-Z0-9]+)*)?', version):
        raise ValueError('Use a version such as 0.1.0-preview.1')
    build, evidence, output = Path(build).resolve(), Path(evidence).resolve(), Path(output).resolve()
    if output == build or build in output.parents:
        raise ValueError('Output must be outside the input build')
    source = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    dirty = subprocess.check_output(['git', 'status', '--porcelain', '--', 'Assets', 'Packages', 'ProjectSettings'], cwd=ROOT, text=True)
    if dirty.strip():
        raise ValueError('Game source/settings must be clean before packaging')
    report = json.loads(read_json(evidence/'windows-build.json')['data']['result'])
    if report['result'] != 'Succeeded' or report['totalErrors'] or report['platform'] != 'StandaloneWindows64':
        raise ValueError('Successful Windows build evidence is required')
    if Path(report['outputPath']).resolve() != build/'TalesTactics.exe':
        raise ValueError('Build report belongs to a different directory')
    verify_build_files(build, report)
    hashes = read_json(evidence/'build-hashes.json')
    for name in ('TalesTactics.exe', 'TalesTactics_Data/Managed/TalesTactics.Runtime.dll'):
        recorded = [r['Hash'].lower() for r in hashes if Path(r['Path']).resolve() == build/name]
        if recorded != [digest(build/name)]:
            raise ValueError(f'Build differs from verified evidence: {name}')
    runtime = (build/'TalesTactics_Data/Managed/TalesTactics.Runtime.dll').read_bytes()
    if any(name in runtime for name in (b'CampaignPlayerReview', b'ManualPlayerReview', b'PersistencePlayerReview')):
        raise ValueError('Development review code must not ship')
    output.mkdir(parents=True, exist_ok=True)
    archive = output/f'TalesTactics-{version}-windows-x64.zip'
    checksum = archive.with_suffix('.zip.sha256')
    if archive.exists() or checksum.exists():
        raise FileExistsError('Release version already exists; choose a new version')
    excluded, files = [], []
    for path in sorted(build.rglob('*')):
        if path.is_symlink() or (hasattr(path, 'is_junction') and path.is_junction()):
            raise ValueError(f'Linked build entry: {path}')
        if not path.is_file():
            continue
        name = safe_name(path.relative_to(build).as_posix())
        if excluded_file(path):
            excluded.append(name)
            continue
        if re.search(r'(^|/)(campaign(?:-slot[23])?\.json|settings\.json)(\.|$)', name, re.I):
            raise ValueError('User data must not ship')
        files.append((name, path))
    if not REQUIRED.issubset({name for name, _ in files}):
        raise ValueError('Incomplete Unity build')
    files += [(p.name, p) for p in sorted((ROOT/'Tools/Distribution').glob('*.ps1'))]
    files += [('START-HERE.txt', ROOT/'Docs/Distribution/START-HERE.txt'), ('CHANGELOG.txt', Path(notes))]
    manifest = {'schema': 1, 'kind': 'release', 'version': version,
                'sourceCommit': source, 'unity': '6000.6.0f1', 'playerVersion': '0.1.0',
                'platform': 'windows-x64', 'createdUtc': datetime.now(timezone.utc).isoformat(),
                'saveReadVersions': [1, 2], 'saveWriteVersion': 2, 'settingsVersion': 1,
                'buildEvidenceSha256': digest(evidence/'windows-build.json'),
                'excluded': excluded, 'files': []}
    # Stage and validate before publishing either final output name.
    with tempfile.TemporaryDirectory(prefix='release-', dir=output) as temp:
        staged = Path(temp)/archive.name
        with zipfile.ZipFile(staged, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
            for name, path in files:
                manifest['files'].append({'path': name, 'bytes': path.stat().st_size, 'sha256': digest(path)})
                z.write(path, name)
            z.writestr('manifest.json', json.dumps(manifest, ensure_ascii=False, indent=2))
        result = verify(staged)
        # Exclusive create preserves any concurrent release with the same version.
        with archive.open('xb') as dest, staged.open('rb') as src:
            shutil.copyfileobj(src, dest)
        with checksum.open('x', encoding='ascii') as dest:
            dest.write(f"{result['sha256']}  {archive.name}\n")
    return dict(result, archive=str(archive), sourceCommit=source, excluded=len(excluded))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    p = sub.add_parser('pack')
    for name in ('build', 'evidence', 'version', 'output', 'notes'):
        p.add_argument('--'+name, required=True)
    v = sub.add_parser('verify')
    v.add_argument('archive')
    args = vars(parser.parse_args())
    command = args.pop('command')
    try:
        result = package(**args) if command == 'pack' else verify(**args)
        print(json.dumps(result, ensure_ascii=False, indent=2))
    except (ValueError, OSError, KeyError, zipfile.BadZipFile, subprocess.CalledProcessError) as error:
        parser.exit(1, f'FAILED: {error}\n')


if __name__ == '__main__':
    main()
