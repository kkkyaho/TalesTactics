# 0.1.0-preview.9 Windows 공개 테스트 배포

릴리즈 준비 중이다. **첫 빌드·ZIP은 실행 검증에서 실패하여 배포에서 제외했다.** 새 일반 빌드 `WindowsPreview9Final2`, 최종 ZIP과 압축 해제 후 시작·정상 종료 검증은 통과했다. 소스 태그와 `main` 통합·원격 반영을 완료했다. **공개 ZIP 업로드는 자동 승인 검토에서 거부되어 사용자 승인 대기 중**이다. 최신 타이틀 육안 검수·공개 게시·공개 다운로드 검증은 미완료이며, 최신 공개 다운로드는 Preview8을 유지한다.

## 소스와 검증 범위

게임 소스 `e081a592d398aa0354b04a827fb3833a277552d2`에 태그 `v0.1.0-preview.9`를 생성·원격 반영했다. 태그와 `main` 통합·원격 반영은 GitHub Release ZIP 공개 게시와 구분한다. 이 커밋에서 만든 별도 작업 폴더 `D:/TalesTacticsReleasePreview9`만 사용했다. 빌드 준비·재import·직렬화 복원은 이 폴더에 한정했으며 원래 `C:/dev/TalesTactics`의 사용자 수정 12개 파일은 건드리지 않았다.

Unity 6000.6.0f1, Windows x64 Mono, TestBattle, `BuildOptions.None` 일반 빌드다. 제품 버전은 0.1.0, 배포 버전은 manifest의 0.1.0-preview.9다. 기능과 저장 호환은 [릴리즈 노트](release-notes.md)를 따른다.

소스 기능 검증은 실제 Unity EditMode **469/469**, 전체 PlayMode **281/282** 후 기존 상점 fixture 전제 보정·별도 **1/1**이다. 단일 전체 282/282로 합산하지 않는다. 개발 검수용 Windows의 전장 90/90·HUD 32/32·음악 14/14구간·자동 캠페인 20/20화(24,626검사·175저장 재로드)와 시각·음악 검증 한계는 [플레이 피드백 기록](../../PlaytestFeedback/README.md)에 있다. 이 결과는 이번 배포 ZIP의 실행 성공을 대신하지 않는다. 배포 도구 [14/14 검사](Validation/packaging-tests.txt)도 Unity 테스트 수와 구분한다.

## 첫 시도 — 실패 보존, 배포 제외

첫 시도의 근거는 **[Validation/Attempt1](Validation/Attempt1)**에 보존했다. `Validation` 바로 아래의 같은 이름 파일을 최종 성공 증거로 해석하면 안 된다.

| 단계 | 첫 시도의 실제 결과 | 근거 |
| --- | --- | --- |
| Unity 빌드 | `Succeeded`, 오류 0개·경고 2개, 258.0426474초 | [BuildReport](Validation/Attempt1/build-report.json) |
| 엄격한 파일 보존 검사 | `status: Failed`, `passed: false`, `protectedFilesUnchanged: false` | [원본 결과](Validation/Attempt1/build-result.json) |
| 별도 직렬화 감사 | 추적 파일 5,715개 원래 바이트로 복원·재해시, 소스 clean | [감사 원문](Validation/Attempt1/serialization-audit.json) |
| 코드 분리·ZIP 생성 | 각각 PASS. ZIP 825,497,917 bytes, manifest 대상 202개 파일 | [코드 분리](Validation/Attempt1/code-isolation.json), [패키지](Validation/Attempt1/package.json) |
| 압축 해제 후 실행 | **FAIL**. 검은 화면과 URP 초기화 예외, 정상 종료 0 | [실행 실패](Validation/Attempt1/startup-attempt1-failed.json), [첫 오류](Validation/Attempt1/startup-attempt1-first-error.log), [화면 관측](Validation/Attempt1/visual-startup.json) |

첫 실행은 강제 종료 없이 끝났고, 해상도 변경으로 쓰인 설정 2개를 원래 바이트로 복원한 뒤 사용자 파일 보존을 확인했다. 빌드 성공·코드 분리·ZIP 생성 PASS는 이 실행 실패를 상쇄하지 않는다. 실패 산출물의 [EXE·런타임 해시](Validation/Attempt1/build-hashes.json)와 [ZIP manifest](Validation/Attempt1/package-manifest.json)는 이력 식별용이며 공개 배포 대상이 아니다.

최초 보존 검사 실패는 Unity의 빌드 중 직렬화 10개 파일 변경과 관련됐다. 종료 시 줄바꿈 변경 2개까지 감사했으며, `UnityConnectSettings.asset`의 비활성 서비스 설정을 유지하는 스키마 전환 외에는 줄바꿈·행 끝 공백 차이였다. 5,715개 복원은 소스 출처 감사 결과이고, 빌드 도중 바이트가 불변이었다거나 산출물이 재현 가능하다는 뜻은 아니다. raw strict 실패는 그대로 남겼다. 경고 2개는 Pipeline RuntimePipelineConfig 미지정과 기존 `DEVELOPMENT_BUILD` 지시자 UAC0009다.

## 셰이더 복구와 새 빌드

첫 실행의 오류는 null Shader에서 `LocalKeyword` → `Blitter.Initialize` → URP 생성자로 이어졌다. 정상 실행됐던 Final2 산출물에는 `Hidden/Universal/CoreBlit`·`CoreBlitColorAndDepth`가 있었지만 첫 릴리즈 산출물에는 없었다. 새 Library import가 파생 `m_RuntimeSettings` 목록을 비운 뒤, 디스크 원본을 복원하고 자동 갱신을 막은 준비 경로에서 Unity의 import 상태와 디스크 상태가 어긋난 것으로 판단했다. 초기 변경은 [import 기록](Validation/import-normalization.json)에 남겼다.

릴리즈 전용 Editor에서 [동기 Refresh·강제 재import](Validation/RefreshPreview9v2.cs)를 수행한 뒤 CoreBlit, CoreBlitColorAndDepth, CoreCopy 참조가 다시 해석되는 것을 [실제 응답](Validation/refresh-runtime-settings-final.json)으로 확인했다. 이는 Editor 참조 복구 확인이며 새 플레이어의 렌더 성공 판정은 아니다. 제품 소스나 Always Included Shaders 목록을 바꾸지 않았다.

새 [Build-Preview9Final.cs](Build-Preview9Final.cs)는 원본 바이트 보존과 제한된 의미 동등성 검사를 구분한다.

- HEAD는 `e081a592…`와 정확히 일치해야 하며, 미추적 제품 파일을 거부한다. 변경 후보와 알려진 직렬화 입력의 HEAD·현재 바이트를 모두 보관한다.
- 허용 차이는 줄바꿈·행 끝 공백, 정확히 알려진 파생 runtime 목록 16개 항목의 비움, 서비스 비활성 값을 유지하는 정확한 UnityConnect 스키마 전환뿐이다. 그 밖의 변경은 실패한다.
- `sourceCleanBefore/After`, `protectedFilesUnchanged`는 관측값 그대로 보존한다. `semanticSourceMatchesBefore/After`의 PASS로 raw clean·바이트 동일성 값을 바꾸지 않는다.
- 빌드 전에 필수 셰이더 3개의 이름·GUID를 확인하고, helper 안에서는 원복·SaveAssets·Refresh·자동 갱신 차단을 하지 않는다. 최종 PASS에는 성공 BuildReport와 빌드 전후 출처 검증이 모두 필요하다.

run `15a757f5b89a470a9fa3f156318d6f04`는 **Succeeded / passed: true**로 완료했다. 새 출력 위치는 `D:/TalesTacticsBuildArchive/WindowsPreview9Final2`이며, 원문은 [Validation/Final2](Validation/Final2)에 보존했다.

| Final2 검사 | 실제 결과 | 근거 |
| --- | --- | --- |
| 일반 Windows 빌드 | 오류 0개·경고 3개, 137.8605326초 | [BuildReport](Validation/Final2/build-report.json), [완료 결과](Validation/Final2/build-result.json) |
| 소스 출처 | 전후 HEAD 동일, `semanticSourceMatchesBefore/After: true` | [빌드 전 감사](Validation/Final2/semantic-source-before.json), [빌드 후 감사](Validation/Final2/semantic-source-after.json) |
| raw 보존 상태 | `sourceCleanBefore/After: false`, `protectedFilesUnchanged: false`를 그대로 기록 | [완료 결과](Validation/Final2/build-result.json) |
| 셰이더 사전 검사 | CoreBlit·CoreBlitColorAndDepth·CoreCopy 이름과 GUID 모두 일치 | [preflight](Validation/Final2/runtime-shader-preflight.json) |
| 산출물 셰이더 포함 | 1,305,832-byte globalgamemanagers.assets에서 Blit 계열 이름 4개 확인. 실행 검증과 별개 | [포함 검사](Validation/Final2/serialized-shader-inclusion.json) |
| 제품 코드 분리 | PASS, 검수 타입·IL 참조 없음, 필수 제품 타입 포함 | [기본 검사](Validation/Final2/code-isolation-base.json), [추가 검사](Validation/Final2/code-isolation.json) |
| 릴리즈 Editor 종료 | `closed: true`, `method: graceful` | [종료 결과](Validation/Final2/editor-close.json) |
| 종료 후 소스 복원 | PASS, 추적 파일 5,715개 의미 동등, 최종 소스 clean | [복원 결과](Validation/Final2/serialization-restore.json) |
| 최종 ZIP | PASS, 826,022,703 bytes, manifest 대상 202개 파일 | [패키지 검사](Validation/Final2/package.json), [manifest](Validation/Final2/package-manifest.json) |

경고 3개는 Pipeline RuntimePipelineConfig 미지정과 URP 디버그 셰이더 `Hidden/Core/DebugOccluder`·`Hidden/Core/DebugOcclusionTest`의 stripping 안내다. EXE·런타임 DLL의 정확한 SHA-256은 [빌드 해시](Validation/Final2/build-hashes.json)를 따른다. 이 빌드 PASS는 위에서 정한 의미 동등성 기준의 통과이며 raw 바이트 불변 PASS가 아니다.

Editor 종료 뒤의 첫 복원에서는 HEAD 바이트를 되돌렸지만 Git의 파일 상태·줄바꿈 처리로 modified 표시가 남아 [첫 복원 영수증](Validation/Final2/serialization-restore-initial.json)은 false였다. 감사 대상 설정 13개에 한정해 정상 Windows checkout을 적용한 후, HEAD와의 차이가 CRLF/LF뿐임과 추적 파일 5,715개의 의미 동등성·소스 clean을 재확인했다. [최종 복원 PASS](Validation/Final2/serialization-restore.json)는 이 후속 결과이며, 앞선 false나 빌드 중 raw flags를 덮어쓰지 않는다.

최종 ZIP은 `D:/TalesTacticsBuildArchive/ReleasesPreview9Final2/TalesTactics-0.1.0-preview.9-windows-x64.zip`이다. SHA-256은 `5781b06d4020863a89c4af6c86360475f441f7dde397bcee1fc482621fbd9151`이며 manifest의 sourceCommit은 `e081a592…`와 일치한다. 배포 대상 202개 파일과 manifest를 담고 Unity의 배포 제외용 백업 파일 1개를 뺐다.

## 압축 해제 실행과 남은 단계

최종 ZIP을 새 폴더 `Preview9ExtractedFinal2`에 풀어 실행했다. **60.1474727초 검사와 추가 120.1229641초 검사 모두 PASS**이며 초기화·응답·로그 오류 후보0·정상 종료0을 확인했다. 강제 종료와 설정 복원은 없었다. 각 실행 전후 사용자 **4,960파일·2,383디렉터리**가 동일했고 최상위 Unity 로그2개만 비교에서 제외했다. 배포 파일 202개도 실행 전후 검사를 통과했다. [60초 실행 원문](Validation/Final2/startup-summary.json), [추가 실행 원문](Validation/Final2/startup-visual-observation-summary.json).

육안 검수는 미완료다. 첫 관측은 검수 시작 전에 창이 종료됐고, 추가 관측에서는 검은 캡처2회와 창 활성화 실패가 있었다. 이후 WTS 조회에서 세션 잠김(`SessionFlags: 0`)을 확인했다. 이 조회는 당시 잠금 상태를 확인한 것이며 이전 캡처 시점의 상태나 게임 렌더 실패를 단독으로 확정하지 않는다. 최신 타이틀·한글 메뉴가 정상 표시된 것으로 세지 않는다. [첫 관측](Validation/Final2/visual-startup.json), [추가 관측](Validation/Final2/visual-startup-observed.json), [세션 잠금 근거](Validation/Final2/session-lock-state.json).

소스 태그·로컬 패키지 검증과 검증된 `develop` 소스의 `main` fast-forward 통합·원격 반영을 완료했다. 후속 문서 커밋은 두 브랜치의 이력에서 확인한다. 대용량 ZIP 공개 업로드가 자동 승인 검토에서 거부되어 사용자 승인 대기 중이며, 공개 Release 게시·서버 asset 검증·익명 다운로드는 아직 하지 않았다. 검토 사유는 정확한 ZIP·체크섬과 공개 GitHub 목적지에 대한 명시적 승인이 필요하다는 것이다. [태그·게시 상태와 검토 사유](Validation/publication-state.json)를 보존했고, 기존 최신 공개 다운로드 Preview8 안내를 유지한다.
