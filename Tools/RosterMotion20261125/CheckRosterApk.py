"""Read-only APK verification after the requested source-only Android build finishes.
No Unity execution, upload, download, installation, APK edits, or user-save access.
Run identity and freshness are mandatory; earlier verification evidence is preserved.
"""
import argparse
import datetime
import json
import hashlib
from pathlib import Path
import re
import subprocess
import sys
import zipfile

ROOT = Path('C:/dev/TalesTactics')
PROJECT = ROOT / 'Builds/SourceOnlyPublish20261118/Project'
EVIDENCE = ROOT / 'Logs/RosterMotion20261125/Builds'
APK = PROJECT / 'Builds/RosterMotion20261125/Android/TalesTactics.apk'
BUILD_REPORT = APK.with_name('build-result.json')
OUTPUT = EVIDENCE / 'apk-verification.json'
ANDROID = Path('C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data/PlaybackEngines/AndroidPlayer')
BUILD_TOOLS = ANDROID / 'SDK/build-tools/36.0.0'
JAVA = ANDROID / 'OpenJDK/bin/java.exe'
EXPECTED_CERT = 'bf8d1d00c3179bcb98c205a66050b27ef759c2938ea4f015368f955e04f06cda'

def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def run_tool(command):
    # Never invoke a shell or expose inherited signing/authentication settings.
    result = subprocess.run([str(item) for item in command], capture_output=True,
                            text=True, encoding='utf-8', errors='replace',
                            timeout=60, shell=False,
                            creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
    return {'exitCode': result.returncode, 'stdout': result.stdout,
            'stderr': result.stderr}


def package_info(text):
    package = re.search(r"^package: name='([^']+)' versionCode='([^']+)' versionName='([^']+)'", text, re.M)
    sdk = re.search(r"^sdkVersion:'(\d+)'", text, re.M)
    target = re.search(r"^targetSdkVersion:'(\d+)'", text, re.M)
    native = re.search(r'^native-code: (.+)$', text, re.M)
    require(all((package, sdk, target, native)), 'Incomplete aapt package metadata')
    return {'applicationId': package[1], 'versionCode': int(package[2]),
            'versionName': package[3], 'minSdk': int(sdk[1]),
            'targetSdk': int(target[1]),
            'architectures': re.findall(r"'([^']+)'", native[1])}


def signer_digest(text):
    matches = re.findall(r'^Signer #\d+ certificate SHA-256 digest: ([0-9a-fA-F]{64})$', text, re.M)
    require(len(matches) == 1, 'Expected exactly one APK certificate')
    return matches[0].lower()


def zip_layout(path):
    with zipfile.ZipFile(path) as archive:
        names = archive.namelist()
        require(len(names) == len(set(names)), 'Duplicate ZIP member names')
        libraries = [name for name in names if name.startswith('lib/') and name.endswith('.so')]
        resources = [{'name': row.filename, 'bytes': row.file_size}
                     for row in archive.infolist()
                     if row.filename.startswith('assets/bin/Data/') and
                     (row.filename.endswith(('.resource', '.resS')) or
                      re.search(r'\.(?:resource|resS)\.split\d+$', row.filename))]
        return {'entryCount': len(names), 'nativeLibraries': libraries,
                'streamResourceEntries': resources,
                'streamResourceBytes': sum(row['bytes'] for row in resources)}



def utc(value):
    parsed = datetime.datetime.fromisoformat(value.replace('Z', '+00:00'))
    require(parsed.tzinfo is not None, 'Timestamp needs an explicit UTC offset')
    return parsed.astimezone(datetime.timezone.utc)


def activity_orientations(xmltree):
    rows, current, depth = [], None, None
    for line in xmltree.splitlines():
        element = re.match(r'^(\s*)E: ([\w.-]+)', line)
        if element:
            indent = len(element[1])
            if current is not None and indent <= depth:
                rows.append(current)
                current, depth = None, None
            if element[2] in ('activity', 'activity-alias'):
                current, depth = {'element': element[2], 'name': None, 'orientation': None}, indent
        if current is None:
            continue
        if re.search(r'\bA: android:name\(', line) and current['name'] is None:
            value = re.search(r'=\s*"([^"]+)"', line)
            if value:
                current['name'] = value[1]
        if re.search(r'\bA: android:screenOrientation\(', line):
            value = re.search(r'\(type 0x10\)(0x[0-9a-fA-F]+|-?\d+)', line)
            require(value, 'Cannot parse screenOrientation integer')
            current['orientation'] = int(value[1], 0)
    if current is not None:
        rows.append(current)
    return rows


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--expected-run-id', required=True)
    parser.add_argument('--not-before-utc', required=True,
                        help='UTC timestamp recorded immediately before queuing this build')
    args = parser.parse_args()
    require(re.fullmatch(r'[0-9a-f]{32}', args.expected_run_id), 'Expected build run ID must be 32 lower-case hex digits')
    not_before = utc(args.not_before_utc)
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    require(not OUTPUT.exists(), 'Refusing to overwrite earlier APK verification evidence')
    report = {'checkedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
              'apk': str(APK), 'expectedRunId': args.expected_run_id,
              'notBeforeUtc': not_before.isoformat(), 'passed': False,
              'networkUsed': False, 'uploaded': False, 'deviceTested': False,
              'scope': 'Current source-only build identity/freshness, Android metadata and landscape manifest, test signature, ZIP CRC/alignment, ABI and SHA-256 stability. This is not an installation, native execution, ELF page-alignment, performance, touch or audio-playback test. Music exclusion is the explicit successful builder preflight plus ZIP filename check; no claim of decoding Unity serialized audio.'}
    try:
        require(APK.is_file() and BUILD_REPORT.is_file(), 'New APK or build report missing')
        final_path = EVIDENCE / 'android-build-final.json'
        require(final_path.is_file(), 'Final builder/settings restoration report missing')
        final_sha = sha(final_path)
        final = json.loads(final_path.read_text(encoding='utf-8-sig'))
        require(final.get('passed') is True and final.get('settingsEqual') is True,
                'Builder did not complete with restored settings')
        require(final.get('result') == 'Succeeded' and final.get('errors') == 0 and
                final.get('sourceOnly') is True, 'Final builder report did not pass')
        result_sha = sha(BUILD_REPORT)
        build = json.loads(BUILD_REPORT.read_text(encoding='utf-8-sig'))
        require(build.get('runId') == args.expected_run_id, 'Build run identity mismatch')
        require(build.get('result') == 'Succeeded' and build.get('errors') == 0,
                'Requested build did not finish successfully with zero errors')
        require(build.get('sourceOnly') is True and build.get('containsLocalMusic') is False and
                build.get('localMusicChecked') is True, 'Expected verified source-only build without supplied music')
        require(Path(build['outputPath']).resolve() == APK.resolve(), 'Build output path mismatch')
        started = utc(build['startedUtc'])
        require(started >= not_before, 'Build started before the current queued request')
        require(started <= datetime.datetime.now(datetime.timezone.utc), 'Build start is in the future')
        apk_time = datetime.datetime.fromtimestamp(APK.stat().st_mtime, datetime.timezone.utc)
        result_time = datetime.datetime.fromtimestamp(BUILD_REPORT.stat().st_mtime, datetime.timezone.utc)
        require(apk_time >= started and result_time >= started, 'APK or result predates this build')
        require(result_time >= apk_time, 'Final result report predates APK; build may still be running')
        report['build'] = {key: build.get(key) for key in ('runId', 'startedUtc', 'result', 'errors',
                           'warnings', 'durationSeconds', 'sourceOnly', 'containsLocalMusic', 'localMusicChecked')}
        finished = utc(final['finishedUtc'])
        require(started <= finished <= datetime.datetime.now(datetime.timezone.utc) and
                final_path.stat().st_mtime >= result_time.timestamp(),
                'Final builder report predates this build result')
        require(Path(final['output']).resolve() == APK.resolve(), 'Final builder output path mismatch')
        report['finalBuilderReport'] = {'path': str(final_path), 'sha256': final_sha, 'finishedUtc': final['finishedUtc'], 'passed': final['passed'], 'settingsEqual': final['settingsEqual']}
        report['buildResultSHA256'] = result_sha
        report['buildResultLastWriteUtc'] = result_time.isoformat()
        report['apkLastWriteUtc'] = apk_time.isoformat()
        report['sizeBytes'] = APK.stat().st_size
        report['sha256'] = sha(APK)
        require(report['sizeBytes'] > 0, 'Empty APK')
        tools = {'aapt': BUILD_TOOLS / 'aapt.exe', 'apksignerJar': BUILD_TOOLS / 'lib/apksigner.jar',
                 'zipalign': BUILD_TOOLS / 'zipalign.exe', 'java': JAVA}
        require(all(path.is_file() for path in tools.values()), 'Installed Android verifier tool missing')
        report['toolPaths'] = {key: str(value) for key, value in tools.items()}
        results = report['toolResults'] = {}
        results['badging'] = run_tool([tools['aapt'], 'dump', 'badging', APK])
        results['manifest'] = run_tool([tools['aapt'], 'dump', 'xmltree', APK, 'AndroidManifest.xml'])
        results['signature'] = run_tool([JAVA, '-jar', tools['apksignerJar'], 'verify', '--verbose', '--print-certs', APK])
        results['zipAlignment'] = run_tool([tools['zipalign'], '-c', '4', APK])
        require(all(row['exitCode'] == 0 for row in results.values()), 'An APK verification tool failed')
        metadata = report['metadata'] = package_info(results['badging']['stdout'])
        require(metadata['applicationId'] == 'com.talestactics.game' and metadata['minSdk'] == 26 and
                metadata['targetSdk'] == 36 and metadata['architectures'] == ['arm64-v8a'],
                'Unexpected Android package/SDK/ABI metadata')
        activities = report['activities'] = activity_orientations(results['manifest']['stdout'])
        launchable = re.search(r"^launchable-activity: name='([^']+)'", results['badging']['stdout'], re.M)
        require(launchable, 'No launchable activity')
        launch = [row for row in activities if row['name'] == launchable[1]]
        require(len(launch) == 1, 'Launchable activity was not uniquely found in manifest')
        # Android ActivityInfo: 0 landscape, 6 sensorLandscape, 8 reverseLandscape, 11 userLandscape.
        require(launch[0]['orientation'] in (0, 6, 8, 11), 'Launchable activity does not enforce landscape')
        report['landscapeOnly'] = True
        certificate = report['signerSha256'] = signer_digest(results['signature']['stdout'])
        require(certificate == EXPECTED_CERT, 'Test signing certificate changed from the previously shipped APK')
        report['zip'] = zip_layout(APK)
        require(report['zip']['nativeLibraries'] and
                all(name.startswith('lib/arm64-v8a/') for name in report['zip']['nativeLibraries']) and
                'lib/arm64-v8a/libil2cpp.so' in report['zip']['nativeLibraries'], 'Unexpected native ABI files')
        with zipfile.ZipFile(APK) as archive:
            names = archive.namelist()
            require('AndroidManifest.xml' in names and 'classes.dex' in names, 'Required APK members missing')
            require(archive.testzip() is None, 'ZIP member CRC verification failed')
            report['zip']['allMemberCrcsPassed'] = True
            report['zip']['rawLocalMusicEntries'] = [name for name in names if
                '/localmusic/' in name.lower() or name.lower().endswith('.mp3')]
            require(not report['zip']['rawLocalMusicEntries'], 'Unexpected raw MP3 or LocalMusic ZIP member')
        report['hashStable'] = sha(APK) == report['sha256']
        report['buildResultStable'] = sha(BUILD_REPORT) == result_sha and sha(final_path) == final_sha
        require(report['hashStable'] and report['buildResultStable'], 'APK or build result changed during verification')
        report['passed'] = True
    except Exception as error:
        report['error'] = type(error).__name__ + ': ' + str(error)
    with OUTPUT.open('x', encoding='utf-8') as stream:
        json.dump(report, stream, indent=2, ensure_ascii=False)
        stream.write('\n')
    print(json.dumps({'passed': report['passed'], 'report': str(OUTPUT),
                      'sizeBytes': report.get('sizeBytes'), 'sha256': report.get('sha256'),
                      'error': report.get('error')}))
    return 0 if report['passed'] else 1


if __name__ == '__main__':
    sys.exit(main())
