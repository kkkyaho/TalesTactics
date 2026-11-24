"""Publish allowlisted summaries of completed roster build evidence, never run builds.

All inputs are read-only. Missing/failed/mismatched evidence refuses publication.
Only the two new public JSON summaries are written, with exclusive creation.
Raw logs, tool stdout, absolute paths, music and build bytes are never copied.
"""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TASK = 'RosterMotion20261125'
LOG = ROOT / 'Logs' / TASK
BUILDS = LOG / 'Builds'
DOCS = ROOT / 'Docs' / TASK / 'Validation'
VALIDATION = ROOT / 'Builds/SourceOnlyPublish20261118/Project'
FROZEN_SHA = 'bd115915a0a6dc064591de2b0ebd455e0ab95dcdba6e97731795d52d62887214'
EXCLUDED = (
    'reviewFixtureTypes', 'reviewTypeReferences', 'reviewMemberReferences',
    'reviewIlReferences', 'editorOrTestTypes', 'editorOrTestAssemblies',
    'editorOrTestReferences', 'editorOrTestTypeReferences',
    'editorOrTestMemberReferences', 'editorOrTestIlReferences', 'testAttributes',
)
PRODUCT_TYPES = {
    'TalesTactics.BattleDirector', 'TalesTactics.BattleHud',
    'TalesTactics.MobileUiLayout', 'TalesTactics.AlphenVisualPilotMotion',
    'TalesTactics.CharacterMotionProfile', 'TalesTactics.CharacterMotionRegistry',
}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest()


def relative(path):
    return path.resolve().relative_to(ROOT).as_posix()


def public_safe(value):
    """Fail closed instead of accidentally publishing raw local/tool strings."""
    if isinstance(value, dict):
        for child in value.values():
            public_safe(child)
    elif isinstance(value, list):
        for child in value:
            public_safe(child)
    elif isinstance(value, str):
        require(not re.search(r'[A-Za-z]:[\\/]|file://|\\\\|/Users/|/home/', value),
                'Public document contains an absolute/private path')
        require('USER-PC' not in value, 'Public document contains a private host name')


def prepare():
    records = {}

    def ref(path):
        path = path.resolve()
        row = {'path': relative(path), 'bytes': path.stat().st_size, 'sha256': sha(path)}
        records[path] = row
        return dict(row)

    def read(path):
        reference = ref(path)
        value = json.loads(path.read_text(encoding='utf-8-sig'))
        require(sha(path) == reference['sha256'], 'Input changed during read')
        return value, reference

    freeze, freeze_ref = read(LOG / 'validation-product-freeze.json')
    require(freeze_ref['sha256'] == FROZEN_SHA and len(freeze['files']) == 83,
            'Unexpected frozen product input')
    require(len({row['relative'] for row in freeze['files']}) == 83, 'Duplicate frozen source')
    public_freeze_ref = ref(DOCS / 'product-source-freeze.json')
    require(public_freeze_ref['sha256'] == FROZEN_SHA, 'Public freeze differs from build freeze')
    proof, proof_ref = read(BUILDS / 'artifact-proof.json')
    require(proof['schema'] == 2 and proof['passed'] is True and
            proof['currentSourceMatched'] is True and proof['sourceFileCount'] == 83 and
            proof['sourceFreezeSHA256'] == FROZEN_SHA, 'Artifact proof has not passed')
    require(len(proof['builds']) == 2 and {r['platform'] for r in proof['builds']} == {'windows', 'android'},
            'Expected exactly two verified platforms')

    expected_payload = set()
    for character in ('cless', 'farah', 'jade', 'kisara', 'mint', 'natalia', 'shionne', 'tear', 'velvet'):
        expected_payload.update({f'Assets/TalesTactics/Art/{TASK}/{character}/idle-v1.png',
                                 f'Assets/TalesTactics/Art/{TASK}/{character}/action-v1.png',
                                 f'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/{character}.asset'})
    expected_payload.update({'Assets/TalesTactics/Art/VisualPilot/Alphen/idle-v1.png',
                             'Assets/TalesTactics/Art/VisualPilot/Alphen/action-v1.png',
                             'Assets/TalesTactics/Resources/TalesTactics/VisualPilot/AlphenMotion.asset'})
    platforms, isolation, context = [], [], {}
    for platform, folder, filename in (('windows', 'Windows', 'TalesTactics.exe'),
                                       ('android', 'Android', 'TalesTactics.apk')):
        build, build_ref = read(BUILDS / f'{platform}-build-final.json')
        queued, queued_ref = read(BUILDS / f'{platform}-queued.json')
        managed, managed_ref = read(BUILDS / f'{platform}-managed-isolation.json')
        artifact = VALIDATION / f'Builds/{TASK}/{folder}/{filename}'
        inner, inner_ref = read(artifact.parent / 'build-result.json')
        binary_ref = ref(artifact)
        report_ref = ref(BUILDS / f'{platform}.buildreport')
        row = next(r for r in proof['builds'] if r['platform'] == platform)
        run_id = build['runId']
        require(re.fullmatch(r'[0-9a-f]{32}', run_id) is not None and
                all(r['runId'] == run_id for r in (queued, inner, row, managed['buildContext'])),
                'Run identity mismatch')
        require(build['passed'] is True and build['result'] == inner['result'] == 'Succeeded' and
                build['errors'] == inner['errors'] == row['errors'] == 0 and
                build['warnings'] == row['warnings'] == inner['warnings'], 'Build did not succeed consistently')
        require(build['settingsEqual'] is True and build['sourceOnly'] is True and
                build['localMusicChecked'] is True and build['containsLocalMusic'] is False and
                inner['sourceOnly'] is True and inner['localMusicChecked'] is True and
                inner['containsLocalMusic'] is False, 'Source-only/settings check failed')
        require(build['sourceFreezeMatched'] is True and build['sourceFileCount'] == queued['sourceFileCount'] == 83 and
                build['sourceFreezeSHA256'] == queued['sourceFreezeSHA256'] == FROZEN_SHA,
                'Source-freeze identity mismatch')
        for phase in ('sourceBefore', 'sourceAfter'):
            snapshot = build[phase]
            require(snapshot['passed'] is True and snapshot['sha256'] == FROZEN_SHA and
                    snapshot['fileCount'] == len(snapshot['files']) == 83 and
                    all(entry['passed'] is True for entry in snapshot['files']), 'Source snapshot failed')
        require(build['payloadVerified'] is True and build['requiredPayloadCount'] == 30 and
                build['missingPayload'] == [] and len(build['packedPayload']) == 30 and
                {r['path'] for r in build['packedPayload']} == expected_payload and
                all(r['occurrences'] > 0 for r in build['packedPayload']), 'Incomplete actual packed payload')
        for flag in ('passed', 'artifactMatched', 'runMatched', 'sourceMatched', 'payloadMatched',
                     'buildReportMatched', 'settingsRestored', 'sourceOnly'):
            require(row[flag] is True, f'Artifact proof check failed: {flag}')
        require(row['sha256'] == build['artifactSHA256'] == binary_ref['sha256'] and
                row['bytes'] == build['artifactBytes'] == binary_ref['bytes'] and
                row['buildReportSHA256'] == build['buildReportSHA256'] == report_ref['sha256'] and
                row['reportSHA256'] == build_ref['sha256'] and row['resultSHA256'] == inner_ref['sha256'] and
                row['queuedSHA256'] == queued_ref['sha256'], 'Artifact/proof SHA or byte mismatch')
        require(managed['passed'] is True and managed['failures'] == [] and managed['checks'] and
                all(c['passed'] is True for c in managed['checks']), 'Managed inspection failed')
        require(all(managed[field] == [] for field in EXCLUDED), 'Excluded type/reference found')
        required = PRODUCT_TYPES | ({'TalesTactics.AndroidRuntimeSettings'} if platform == 'android' else set())
        require(set(managed['requiredProductionTypes']) == required and
                {r['name'] for r in managed['productionTypes']} == required, 'Required production types missing')
        mc = managed['buildContext']
        require(mc['finalSHA256'] == build_ref['sha256'] and mc['resultSHA256'] == inner_ref['sha256'] and
                mc['artifactSHA256'] == binary_ref['sha256'] and mc['artifactBytes'] == binary_ref['bytes'] and
                mc['resultAndArtifactUnchangedDuringInspection'] is True and
                managed['runtimeSHA256'] == managed['runtimeSHA256After'] and
                managed['methodBodiesInspected'] > 0 and managed['instructionsInspected'] > 0,
                'Managed proof is not bound to the current artifact')
        if platform == 'android':
            require(mc['runtimeWrittenDuringThisBuild'] is True and managed['androidInitializer']['hasBody'] is True,
                    'Fresh Android IL2CPP input or initializer not verified')
        platforms.append({
            'platform': folder, 'runId': run_id, 'result': build['result'], 'passed': True,
            'startedUtc': build['startedUtc'], 'finishedUtc': build['finishedUtc'],
            'target': build['target'], 'seconds': build['seconds'],
            'durationScope': 'seconds는 BuildReport.summary.totalTime 값입니다. 빌드 전후 원화 변환·설정 복원 및 대기 시간은 이 수치에 모두 포함되지 않습니다.',
            'queuedUtc': queued['queuedUtc'],
            'queuedToFinishedSeconds': round((dt.datetime.fromisoformat(build['finishedUtc'].replace('Z', '+00:00')) - dt.datetime.fromisoformat(queued['queuedUtc'].replace('Z', '+00:00'))).total_seconds(), 6),
            'queuedToFinishedScope': 'queuedUtc부터 finishedUtc까지의 벽시계 차이로 계산한 전체 요청 구간입니다. 빌드 전후 처리와 대기가 포함됩니다.',
            'warnings': build['warnings'], 'errors': build['errors'],
            'artifact': {'validationCheckoutRelativePath': artifact.relative_to(VALIDATION).as_posix(),
                         'bytes': binary_ref['bytes'], 'sha256': binary_ref['sha256'],
                         'scope': '실행 파일 단일 파일 크기이며 전체 Windows 폴더 크기가 아닙니다.' if platform == 'windows' else 'APK 단일 파일 크기입니다.'},
            'sourceFreezeMatchedBeforeAndAfter': True, 'sourceFileCount': 83,
            'requiredPayloadCount': 30, 'packedPayloadVerified': True,
            'settingsRestored': True, 'sourceOnly': True, 'localMusicChecked': True,
            'containsLocalMusic': False, 'standalonePlayerExecutedByThisVerification': False,
            'physicalDeviceGameplayTested': False,
            'evidence': {'final': build_ref, 'queued': queued_ref, 'inner': inner_ref, 'buildReport': report_ref},
        })
        isolation.append({
            'platform': folder, 'passed': True, 'runId': run_id,
            'inputScope': 'Windows 플레이어의 Managed 어셈블리' if platform == 'windows' else '현재 Android 빌드의 IL2CPP ManagedStripped 입력 어셈블리',
            'runtimeSHA256': managed['runtimeSHA256'], 'runtimeBytes': managed['runtimeBytes'],
            'runtimeUnchangedDuringInspection': True,
            'runtimeWrittenDuringThisBuild': mc['runtimeWrittenDuringThisBuild'],
            'freshnessScope': '새 출력 폴더와 runId로 확인하며 DLL 복사 시각은 이전일 수 있습니다.' if platform == 'windows' else 'ManagedStripped 수정 시각이 현재 빌드 시작 이후임을 확인했습니다.',
            'typeCount': managed['typeCount'], 'methodBodiesInspected': managed['methodBodiesInspected'],
            'instructionsInspected': managed['instructionsInspected'],
            'requiredProductionTypes': sorted(required), 'knownExcludedReviewTypeCount': len(managed['knownExcludedReviewTypes']),
            'excludedFindings': {field: 0 for field in EXCLUDED},
            'checkCount': len(managed['checks']), 'failedCheckCount': 0, 'evidence': managed_ref,
        })
        context[platform] = {'build': build, 'buildRef': build_ref, 'innerRef': inner_ref, 'artifactRef': binary_ref}

    apk, apk_ref = read(BUILDS / 'apk-verification.json')
    android = context['android']
    require(apk['passed'] is True and apk['hashStable'] is True and apk['buildResultStable'] is True and
            apk['build']['runId'] == android['build']['runId'] and
            apk['sha256'] == android['artifactRef']['sha256'] and apk['sizeBytes'] == android['artifactRef']['bytes'] and
            apk['buildResultSHA256'] == android['innerRef']['sha256'] and
            apk['finalBuilderReport']['sha256'] == android['buildRef']['sha256'], 'APK verification is not bound to build')
    tools = apk['toolResults']
    require(set(tools) == {'badging', 'manifest', 'signature', 'zipAlignment'} and
            all(value['exitCode'] == 0 for value in tools.values()), 'APK tool check failed')
    metadata = apk['metadata']
    require(metadata['applicationId'] == 'com.talestactics.game' and metadata['minSdk'] == 26 and
            metadata['targetSdk'] == 36 and metadata['architectures'] == ['arm64-v8a'] and
            apk['landscapeOnly'] is True and apk['zip']['allMemberCrcsPassed'] is True and
            apk['zip']['rawLocalMusicEntries'] == [], 'APK metadata or ZIP check failed')
    require('lib/arm64-v8a/libil2cpp.so' in apk['zip']['nativeLibraries'] and
            all(name.startswith('lib/arm64-v8a/') for name in apk['zip']['nativeLibraries']), 'Unexpected APK ABI')
    signature_text = tools['signature']['stdout']
    verified_schemes = re.findall(r'^Verified using (.+?): true\s*$', signature_text, re.M)
    stamp = dt.datetime.now(dt.timezone.utc).isoformat()
    common_scope = [
        '이 문서는 완료된 빌드와 읽기 전용 산출물 검사의 공개 요약입니다.',
        '83파일은 이번 제품·가져오기 배치 동결 범위이며 저장소 전체 파일 목록이 아닙니다.',
        'Windows 별도 플레이어 실행이나 Android 설치·실기기 플레이를 수행했다는 의미가 아닙니다.',
        '원시 로그의 절대경로·도구 출력과 음원·빌드 바이너리는 이 문서에 복사하지 않았습니다.',
    ]
    summary = {
        'schemaVersion': 1, 'passed': True, 'generatedUtc': stamp, 'scope': common_scope,
        'sourceFreeze': {'sha256': FROZEN_SHA, 'fileCount': 83, 'publicManifest': public_freeze_ref},
        'packedPayload': {'countPerPlatform': 30, 'characters': 10, 'spriteSheets': 20, 'motionProfiles': 10,
                          'verification': '각 BuildReport.packedAssets에서 정확한 30개 경로의 포함을 확인했습니다.'},
        'platforms': platforms, 'artifactProof': proof_ref,
        'warningsScope': '경고/오류 수는 실제 BuildReport 값입니다. 경로가 포함될 수 있는 원문 메시지는 로컬 근거에 남깁니다.',
    }
    artifact_summary = {
        'schemaVersion': 1, 'passed': True, 'generatedUtc': stamp, 'scope': common_scope,
        'sourceFreezeSHA256': FROZEN_SHA, 'sourceFileCount': 83,
        'managedIsolation': isolation,
        'androidPackage': {
            'passed': True, 'runId': apk['build']['runId'],
            'bytes': apk['sizeBytes'], 'sha256': apk['sha256'],
            'applicationId': metadata['applicationId'], 'minSdk': metadata['minSdk'], 'targetSdk': metadata['targetSdk'],
            'architectures': metadata['architectures'], 'landscapeOnly': True,
            'signatureVerified': True, 'signerSHA256': apk['signerSha256'],
            'verifiedSignatureSchemes': verified_schemes,
            'signatureScope': '검사 도구에 고정된 기존 테스트 인증서와 SHA-256이 일치합니다.',
            'zipAlignmentBytes': 4, 'zipAlignmentPassed': True, 'zipMemberCount': apk['zip']['entryCount'],
            'allZipMemberCrcsPassed': True, 'nativeLibraries': apk['zip']['nativeLibraries'],
            'rawLocalMusicZipEntries': 0, 'artifactAndReportsUnchangedDuringInspection': True,
            'toolExitCodes': {key: value['exitCode'] for key, value in tools.items()}, 'evidence': apk_ref,
        },
        'limitations': [
            'Managed 검사는 메타데이터·타입/멤버 참조·메서드 IL 및 테스트 속성을 검사합니다. Android 네이티브 바이너리 실행 검사가 아닙니다.',
            '리뷰 코드 제외는 검사 도구가 명시한 타입 목록과 Editor/Test 네임스페이스·어셈블리 규칙의 범위입니다.',
            'APK 정렬 검사는 zipalign -c 4이며 ELF 페이지 정렬 검증을 의미하지 않습니다.',
            '음원 제외는 빌더 사전 검사와 ZIP 파일명 검사의 근거입니다. Unity 직렬화 오디오를 디코딩한 검사는 아닙니다.',
            '설치·성능·터치·음원 재생·실기기 플레이는 이 산출물 검사 범위에 포함되지 않습니다.',
        ],
        'artifactProof': proof_ref,
    }
    result = {'build-summary.json': summary, 'artifact-verification.json': artifact_summary}
    for value in result.values():
        public_safe(value)
    # Bind generated prose to the exact input/report bytes observed above.
    for path, record in records.items():
        require(path.stat().st_size == record['bytes'] and sha(path) == record['sha256'], 'Input changed before publication')
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check-only', action='store_true', help='Validate all inputs without writing summaries')
    args = parser.parse_args()
    documents = prepare()
    encoded = {name: (json.dumps(value, ensure_ascii=False, indent=2) + '\n').encode('utf-8')
               for name, value in documents.items()}
    require(all(not (DOCS / name).exists() for name in encoded), 'Refusing to overwrite an existing summary')
    if not args.check_only:
        require(DOCS.is_dir(), 'Expected existing public Validation folder')
        for name, data in encoded.items():
            with (DOCS / name).open('xb') as stream:
                stream.write(data)
    print(json.dumps({'passed': True, 'checkOnly': args.check_only, 'outputs': [
        {'path': relative(DOCS / name), 'bytes': len(data), 'sha256': hashlib.sha256(data).hexdigest()}
        for name, data in encoded.items()]}, ensure_ascii=False))


if __name__ == '__main__':
    main()