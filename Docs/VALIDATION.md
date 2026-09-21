# 검증 기록 — 2026-09-22

## 통과한 검증

- Unity 6000.6.0f1, 실제 TestBattle/Content/TMP 에셋 import 및 생성 확인.
- 실제 Unity EditMode 34/34 통과: unity-test-run.json.
- 실제 Unity PlayMode 5/5 통과: unity-playmode-results.json.
- 전장 카메라 수정 후 PlayMode 5/5 재실행 통과: unity-playmode-viewport.xml. 출전 버튼, 참조/폰트, 전체 타일 viewport 포함, 이동/취소, 공격 미리보기/실행/턴 종료, 승리/패배/재시작 검증.
- Windows x64 플레이어 빌드 성공. 카메라 수정 후 재빌드 성공: windows-build-results.json에 Unity 빌드 결과와 실행 파일/런타임 DLL SHA256 보관.
- Computer Use로 Windows 실행 파일 검수: 출전, Lv25 훈련 전투 시작, 한글과 파라 궁극기의 일본어 한자, 전장 전체 표시, 실제 타일 클릭 이동, 이동 취소, 스킬 목록과 취소, 방향 선택과 다음 유닛 턴 전환, Restart 출전 화면 복귀.
- 엔진 독립 규칙 테스트 34/34 통과: managed-test-results.txt. 이는 Unity 실행과 별개의 .NET 대체 API 검사다.
- 실제 프로젝트 Library DLL 참조 API 컴파일: Runtime/Editor/EditMode/PlayMode 4개 통과 (api-compile-results.txt).

## 이번에 수정한 문제

- 출전 전 Catalog, Rules, roster, 공격 및 스킬 참조를 검증하고 누락 시 원인이 드러나는 오류 화면을 표시한다.
- TMP Essentials 비동기 import가 끝난 뒤 Scene을 생성한다. 미완성 Scene으로 빌드하지 않는다.
- Windows 맑은 고딕과 Yu Gothic 대체 폰트를 사용하고 소유한 동적 폰트 리소스를 정리한다.
- 최초 PlayMode 검증에서 fallbackFontAssetTable null로 5개 모두 초기화 실패했다. 목록 초기화 후 모두 통과했다. 실패 근거는 unity-playmode-initial-failure.json.
- 좁은 창에서 UI가 전장을 가리는 현상을 확인했다. UI 바깥 viewport에 카메라를 배치하고 전장 bounds 전체를 맞추도록 수정했다. 수정본에서 클릭 이동과 취소를 다시 확인했다.
- Unity가 Scene에 추가한 URP 카메라/조명 컴포넌트와 빌드 시 직렬화한 URP 설정을 보존했다.

## 한계 및 후속 검수

- 완성 게임이 아닌 임시 아트 전투 프로토타입이다. 최종 캐릭터 아트/애니메이션/음원은 포함하지 않는다.
- 실행 파일에서 전투를 끝까지 수동 플레이한 승리/패배 검수는 아직 없다. 승패와 재시작은 실제 Unity PlayMode 테스트에서 검증했다.
- 세로 창 HUD 재배치와 확대 조작은 아래 후속 검증에서 완료했다.
- 장시간 성능/메모리/저장 마이그레이션, 10명 전체 기술의 수동 검수는 미완료다.
- 빌드에는 Pipeline RuntimePipelineConfig 미지정, URP 디버그 셰이더 stripping, TMP 셰이더 pragma, Unity UAC1001 직렬화 분석기 경고가 있었다. 빌드는 성공했으며 런타임 Pipeline 원격 제어는 이번 검수에 사용하지 않았다.

## 작업 도구

CLI는 codexsandboxonline 계정에서 Pipeline 연결 파일을 읽지 못한다. 파일 읽기 승인만으로 Windows ACL이 바뀌지는 않았다. Computer Use 플러그인의 node_repl + @oai/sky를 통해 Unity 메뉴, Test Runner, 빌드 및 플레이어를 직접 제어할 수 있다. 브라우저 cua 도구의 native 비활성화는 이 별도 경로의 비활성화를 의미하지 않는다.

실행 파일은 Builds/Windows/TalesTactics.exe. Builds, Library, Logs 등은 Git에서 제외한다. 배포는 폴더 전체가 필요하며 exe 하나만 복사하지 않는다.

## 후속 단계 — 반응형 전투 HUD와 카메라

- 세로 창(화면 비율 1.2 미만)에서 유닛 정보와 명령을 전장 아래 2열로 배치했다. 넓은 화면에서는 기존 양쪽 패널을 유지한다.
- 회전/확대/축소/초기화 버튼 및 전장 위 휠 확대를 추가했다. 전투 시작과 재시작 시 기본 구도로 복원한다.
- 실제 Unity PlayMode 6/6 통과: unity-playmode-camera-buttons.xml. 카메라 버튼으로 네 방향 회전 후 전체 타일 표시, 확대/초기화, 전투 상태 보존과 재시작을 확인한다. 중간 결과는 unity-playmode-camera.xml.
- Runtime/Editor/EditMode/PlayMode 실제 API 컴파일 4개 통과. 최종 Windows 빌드 성공(9.102초); windows-build-results.json 갱신.
- Windows 실행 파일 604×932 창에서 하단 HUD, 전체 전장, 스킬 목록/취소 표시, 우회전 버튼, 회전한 전장 타일 클릭 이동/취소, 확대 버튼 및 초기화 복원을 직접 확인했다. 전장 위 휠 확대도 확인했다.
- Q/E/Home 단축키 코드도 포함했지만 Computer Use의 문자 키 입력에서는 회전 반응을 확인하지 못했다. 물리 키보드 입력 검증은 남아 있으며, 마우스 버튼으로 모든 카메라 기능을 사용할 수 있다.
- 최종 방향별 아트의 카메라 각도별 선택, 카메라 이동(pan), 매우 작은 창/초광폭의 모든 조합 검수는 후속 작업이다.
