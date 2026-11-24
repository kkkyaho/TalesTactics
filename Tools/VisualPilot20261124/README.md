# 시범 검증 도구

이 도구들은 2026-11-24 시범 적용의 로컬 검증을 재현하기 위한 보조 자료다. 제품 코드는 Assets/TalesTactics 아래에 있다.

- CaptureBaseline.ps1: 현재 파일과 저장/설정의 바이트·해시를 새 Logs 폴더에 보존한다.
- EnterEmptyCapture.cs, SetupPilotBattle.cs: 별도 공개 검증 checkout에서 실제 6화와 격리된 프로필을 준비한다.
- VisualPilotBaselineEpisode6.cs / VisualPilotActualEpisode6FinalV2.cs: 전후 실제 전장 검수. 개선 전 결과는 이전 제품 코드에서 얻어야 한다.
- PilotPaletteScan.cs: 실제 전장의 재질·UI 색·글자 상태를 읽는다.
- InspectSheet.ps1: 원본 이미지의 alpha 영역을 읽는다. 원본을 변경하지 않는다.
- BuildComparison.ps1: 통과한 기존/새 모션 result.json 두 개를 받아 Logs 아래의 새 오프라인 HTML을 만든다. 원본 PNG를 링크하며 중간 프레임을 만들지 않는다.
- BuildEvidence.py: 이 작업의 최종 결과에서 선택한 PNG17장을 바이트 그대로 복사해 공개 갤러리와 해시 목록을 만든다. 이미 결과 폴더가 있으면 중단한다.
- VisualPilotBuilds.cs: 별도 검증 checkout에서 Windows와 Android 빌드를 새 경로에 생성하고 설정 복원을 확인한다. Android 부분은 기존 AndroidBuild의 경로와 클래스명만 조정한 검증용 사본이다.
- CheckPilotApk.py / CheckPilotManaged.ps1: 해당 run ID의 실제 APK·빌드 보고서·managed 입력을 검사한다. 기존 파일의 수정 시각만으로 성공을 판단하지 않는다.
- RestoreValidationState.ps1: 검증용 Editor의 배경 실행과 폰트 상태를 작업 전 바이트로 복구하고 기록한다.
- ReadFinalEditorState.cs: 빌드 대상·자동 프레임·Play·장면·임시 오브젝트·복구 대기 상태를 읽는다.
- CheckFinalProtection.ps1: 원본/검증본의 보호 파일과 실제 저장·기존 APK의 보존을 대조한다. 사용자 폴더 전체 목록은 로컬에 두고 공개용 요약을 분리한다. 기본 StrictBaseline 검사와, 최초 실패 보고서·출처·정확한 해시를 확인해 이번 회귀 산출물만 분리하는 RecordedRegressionArtifacts 검사를 구분한다. 후자는 전체 사용자 트리 불변을 의미하지 않는다.
- AuditPublication.ps1: 명시한 파일 목록과 실제 Git blob을 대조하며 사용자 음원·새 실행파일·비공개 부모가 공개 커밋에 추가되지 않도록 확인한다. Git 상태를 변경하지 않는다.

기본 경로는 C:/dev/TalesTactics 및 그 아래 Builds/SourceOnlyPublish20261118/Project로 제한되어 있다. 실행 순서·실제 결과·실패 이력은 Docs/VisualPilot20261124에 기록했다. C# 검수 도구는 Unity CLI의 외부 script 실행으로 사용하며 Assets에 복사하지 않는다. 일반 실행본에는 포함하지 않는다.
