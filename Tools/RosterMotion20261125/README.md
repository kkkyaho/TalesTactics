# 동료 전체 모션 검수 도구

실행 대상은 `Builds/SourceOnlyPublish20261118/Project` 공개 검증 checkout 하나다. ROOT의 Editor·빌드 대상·설정·음원은 변경하지 않는다. 모든 결과는 이번 `Logs/RosterMotion20261125`의 새 폴더/파일에 남기며 실패 증거도 보존한다. 외부 C#은 Assets에 복사하지 않는다.

## 기준과 실행 순서

1. `CaptureBaseline.ps1` 완료 기준은 `Baseline-bdadaad52bb0415ea7e0fe660d597031`. 원문 4,181개·사용자 트리 5,597개·실제 저장9개·이전 테스트 XML1개·원화946개·APK6개를 보호한다. 상세 경로/원문/음원은 로컬 전용이다.
2. Root가 확정한 파일 목록을 `SyncSelected.ps1 -Manifest ... -Output ...`로 먼저 검사한다. 실제 적용에는 `-Apply`를 사용한다. 사전 PUBLIC 사용자 수정23개와 음원/실행 파일은 거부하고 동일 해시는 복사하지 않는다. 복사 전 PUBLIC 원문과 해시를 보존하며, 정지된 Editor의 자동 새로 고침만 잠시 억제했다가 반환한다.
3. `InvokeUnity.ps1`의 ReadEditor/Recompile/CompileStatus를 사용해 새 코드의 실제 컴파일 완료를 확인한다. 외부 C#은 CompileScript로 실행 없이 검사한다. 신규타입 의존 도구는 동기화 전 구 버전 Editor로 컴파일하지 않는다.
4. `ReadSpriteCapabilities.cs`의 Main(assetPath)을 신규 PNG에 호출해 CreateAndDeleteSprite/EditSpriteName/EditSpriteRect/EditPivot 지원을 확인한다. 모두 지원한 뒤 제품 importer의 Import를 실행한다. 새 PNG·profile·folder .meta는 원문·GUID를 ROOT와 맞추고 명시 목록에 기록한다.
5. 신규 CharacterMotionProfile EditMode 8개와 기존 시범 회귀를 먼저 실행한다. 실제 개수와 통과 결과를 기록하고 예상 개수로 성공을 가정하지 않는다.
6. EnterEmptyCapture.cs → 정지/저장 격리를 유지한 SetupRosterBattle.Main(newStorage)로 실제6화 CommandState를 연다. 게임 Start를 억제하고 저장 경로는 새 Logs 아래로 제한한다. 생성 Scene과 Play는 검증이 끝나면 정리한다.
7. `RosterMotionCapture.Begin(newOutput, characterId, after)`를 캐릭터별 실행한다. before는 기존 프레젠테이션(알펜은 이미 적용된 시범 유지), after는 새 공통 registry 경로다. 네 방향 각각 실제6초 대기·기본공격·피격·회복을 PNG로 캡처한다. blink는 실제 Sprite를 관측하며 시간을 조작하지 않는다. 카메라/게임 상태/RNG/원본 데이터 보존을 확인한다. 개별 result.json 종료 후 다음 캐릭터를 시작한다. 최종 시퀀스는 BeforeFinal/AfterFinal이며, 메모리 컴파일 Frame 배열의 누락을 막기 위해 Newtonsoft 필드 resolver로 기록하고 frames 개수를 PNG 개수와 대조한다.
8. `RosterActualEpisode6Review.BeginEditor(newOutput, characterId)`는 각 동료의 실제 BoardView HP/머리 기준점/그림자/재질/픽킹/UI8장을 기록한다. 4회전·집중·피격·1280x720/2340x1080 가로 UI. 모든 동료를 같은 맵에서 비교하기 위해 임시 profile에 합류 플래그를 명시적으로 채운다. 자연 진행·합류 규칙 통과로 표현하지 않는다. 임시 Review 객체가 종료/삭제된 뒤 다음 run을 시작한다. 촬영용 Play는 계속 유지하고 마지막에 호출자가 정리한다.
9. `RosterStageMatrix.Begin(newResultJson)`은 훈련+모든56스테이지 실제 BoardView.Build × 10명 × 4방향=2,280개 연결을 관측한다. MaxDeployment에 따른 두 임시 세션으로 나누며, 렌더러 연결 확인을 위해 합류/배치 자격을 의도적으로 우회하는 검수이다. stage16에서 CrossoverReserve로 실제 비활성인5명을 먼저 확인하고 임시 session의 reserve만 풀어 검사한 뒤 원래 flag를 복원한다. 기존 실제 세션/캠페인/글로벌 RNG는 복원한다. 정규 Unity 테스트 수에 합산하지 않는다.
10. `BuildComparison.ps1`은 동일 캐릭터의 통과한 전후 result.json으로 원본 PNG 연속 비교 HTML을 만든다. 리사이즈/보간/가짜 프레임 생성은 하지 않는다. 최종 프레임을 직접 보고 제품 결함을 고친 뒤 소스를 고정한다.
11. 전체 EditMode/PlayMode를 실제 실행한다. InvokeUnity StartTests는 비동기 시작만 하며 TestStatus로 결과를 따로 수집한다. 0개 완료는 통과가 아니다. 이번 Play에서는 기존 DisableDomainReload 상태에서 테스트 provider의 빈 nonnull 어셈블리 캐시가 확인되었다. 패키지·제품·검사·프로젝트설정을 바꾸지 않고 stopped 검증 Editor의 해당 캐시만 null로 초기화한 뒤 RosterFullPlayTests의 공식 mode-wide TestRunnerApi를 사용했다. Play 진입 때 풀린 collector는 진행 중인 같은 run에 RegisterCallbacks로 재연결한다. 실제 발견321개와 최종 RunFinished 결과·XML의 모든 이름/판정을 대조하며 초기 callback 관측과 전체 권위 결과를 구분한다. 패키지 해시·캐시 전후·0개 회차도 기록한다. PlayMode는 일반 GameView와 정상 프레임 진행을 유지한다.
12. `RosterMotionBuilds.Windows()` 후 Android로 대상 전환하여 `Android()`를 실행한다. 새 `Builds/RosterMotion20261125/{Windows,Android}` 출력이며 원래6개 APK를 덮어쓰지 않는다. 실행 직전·직후 고정한83개 제품 파일의 ROOT/PUBLIC SHA를 비교하고, DetailedBuildReport에서 신규18장·9개 profile과 기존 알펜2장·profile 총30개 에셋의 실제 패키징을 확인한다. 출력 충돌은 덮어쓰기 없이 중단하며 Android 내부/외부 결과는 같은 runId로 연결한다. 다시 Win64로 전환한 뒤 설정·폰트·Play·임시 Scene·자동 프레임 상태를 복원한다.
13. CheckRosterApk.py / CheckRosterManaged.ps1에 해당 build의 runId와 시작 시각을 전달한다. CheckArtifacts.ps1은 실제 EXE/APK·BuildReport SHA와 크기, 고정 manifest 원문·83개 입력,30개 payload·동일 runId·오류0·설정 복원을 함께 검사한다. 이전 성공 보고서나 mtime만으로 성공을 주장하지 않는다.
14. `RestoreRegressionArtifacts.ps1` 기본은 계획만 기록하며 `-Apply`로 적용한다. `-FullPlayResult`에 실제 전체321건 원문을 지정하고, 이번처럼 전체320/321 후 동일 소스의 실패 항목 단독1/1을 별도 재검한 경우에만 `-RetestResult`로 재검 원문을 함께 지정한다. 실패 이름과 재검 이름·판정, 고정83파일의 양쪽 해시, 최신 진단 XML의 이름/개수/UTC를 대조하고 단일 전체통과로 합산하지 않는다. `-NotBeforeUtc`는 StorageTests를 생성한 EditMode 시작 시각부터 지정한다. 이번 회귀가 새로 만든 StorageTests GUID 파일만 보존·정리하고, 검수 CSV와 최신 XML은 Logs에 보존한 뒤 baseline으로 되돌린다. 전체 실패/재검 JSON과 각각의 실제 XML도 별도 보존한다. 기존 사용자 파일 삭제는 허용하지 않고 다른 변경이 있으면 중단한다.
15. `RestoreValidationState.ps1`은 보호한 PUBLIC 폰트/설정의 원문 복원과 검증에 사용한다. `ReadFinalEditorState.cs`는 시작 상태와 일치하는19조건을 읽는다.
16. `CheckFinalProtection.ps1 -Manifest final-source-manifest.json -Output newReport.json -RequireBuilds`는 이번 baseline과 제품 최종 SHA 목록을 엄격 비교한다. manifest 형식은 `{ "files": [{ "relative": "Assets/...", "sha256": "..." }] }`이며 ROOT/PUBLIC이 같은 최종 제품 해시를 가져야 한다. 개인 경로를 포함한 상세 JSON을 공개하지 않는다.

## 명확한 한계

준비와 구문 검사, dry-run 컴파일은 제품 테스트 성공이 아니다. Editor 가로 화면은 실제 Android 기기 결과가 아니다. 기기 미연결 시 설치·터치·발열·음원 재생 확인을 수행했다고 표현하지 않는다. 모든 새 빌드는 원격 업로드하지 않는다. 이번 범위의 원화/모션 확대는 동료10명이며 적·보스나 다른 지형을 변경하지 않는다.
## 원화 입력 재현

MeasureRosterArt.py는 새18장의 원본 alpha 연결 영역과 기존 캐릭터 높이를 읽어 Logs/RosterMotion20261125/art-measurements.json을 만든다. 최종 방향은 공개 provenance의 observedRowDirections를 사용한다. BuildRosterLayout.py는 그 측정값과 Docs의 확정 landmarks-reviewed-five.json / landmarks-reviewed-four.json을 사용해 art-layout.json, scale-calibration.json, UnitHeadAnchors.json만 작성한다. DraftLandmarks.py는 검수 전 후보/HTML 생성 도구이며 확정 좌표를 자동 승인하지 않는다.

Sprite meta는 위 Python 도구가 작성하지 않는다. 실제 Unity RosterMotionArtImporter가 capability 확인 후 Sprite Data Provider로 작성한다. 생성 요청 크기와 실제 PNG 크기는 다를 수 있으며 정확한 SHA/실제 크기를 기준으로 검증한다. 원화 파일의 픽셀을 확대·자르기·보정하여 저장하지 않는다.

공개 반영 검사는 AuditPublication.ps1을 사용한다. 최종 명시 파일 목록과 선택 커밋의 blob을 바이트 비교하고 승인된 origin 주소와 최신 원격 develop을 시작·종료 시 다시 읽어 공개 커밋의 단일 부모와 대조한다. 고정83파일의 원문 SHA/크기와18개 원본 PNG의 layout/provenance SHA·실제 크기도 대조한다. 선택 파일은 두 checkout에서 명령 한 번에 한정한 core.autocrlf=false로 stage해 검증한 원문 바이트를 보존하며 Git 전역 설정은 변경하지 않는다. 원본 로컬 develop의 음원 포함 이력은 원격에 푸시하지 않는다.
