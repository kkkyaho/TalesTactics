# 전장 가독성과 첫 사용 안내 — 2026-10-19

기술명·피해 숫자·HP 정보의 겹침을 줄이고, 맵 이동·적 기술 조회·방향 확정의 조작 방법을 게임 안에서 짧게 안내한다. 전투 규칙이나 성장·보상 수치를 바꾸지 않고 기존 전투 화면과 입력 흐름을 유지한다.

## 화면과 동작 변경

| 부분 | 변경한 동작 |
|---|---|
| 기술명 | 기존 머리 위에 표시하던 일반 기술명을 전장 상단의 일정한 위치에 표시한다. 시전자 이름과 아군/적군 구분을 함께 보여 준다. 기본 공격 표시는 추가하지 않고, 궁극기는 기존 초상화 알림을 유지해 중복을 피한다. |
| HP·이름 | 피격 대상, 시전자·마우스로 가리킨 유닛, 현재 행동자 순으로 상세 정보의 우선순위를 정한다. 위치가 옮겨진 HP바는 연결선으로 해당 유닛을 가리킨다. |
| 피해·회복 숫자 | 같은 프레임의 숫자끼리 자리를 나누고 HP·이름보다 먼저 공간을 확보한다. 기술명, 중앙 경험치, 조작 안내, 하단 메시지 영역도 피한다. |
| 지난 행동 결과 | 공격·회복·반격·지원 결과는 3.2초 뒤 정리한다. 다음 명령으로 이동하면 먼저 지우고, 새 결과가 나오면 그 결과의 표시 시간을 새로 시작한다. 메뉴·대화·타이밍 입력 중에는 읽기 시간을 소모하지 않는다. |
| 오류·대상 예측 | 자원 부족이나 유효하지 않은 대상 안내는 행동 결과 타이머로 지우지 않는다. 명령을 바꾸거나 선택 대상이 무효화되면 해당 문맥에 맞게 정리한다. |

기술명 알림은 정보를 읽을 짧은 표시 시간을 확보하되 전투 진행을 기다리게 하지 않는다. 새 기술이 나오면 이전 기술명을 교체하며, 과거 기술명을 큐에 쌓아 뒤늦게 재생하지 않는다. 모달·대화·타이밍 입력 중에는 숨긴다.

## 첫 사용 안내

| 안내 | 표시 상황 | 완료로 기억하는 시점 |
|---|---|---|
| 맵 둘러보기 | 아군 명령 대기 | 전장 위에서 시작한 가운데 버튼 드래그로 실제 카메라 위치가 달라졌을 때 |
| 적의 기술 확인 | 살아 있는 적이 있는 아군 명령 대기 | 실제 전투의 적 기술 목록을 열었을 때. 요약의 기술 보기와 일반 상태창 기술 탭을 모두 포함하며, 요약창만 열거나 거점의 미리보기 적을 조회한 경우는 제외한다. |
| 방향을 고르면 턴 완료 | 대기·방어의 방향 선택 | 유효한 방향 확정을 완료했을 때. 미리보기·취소는 제외한다. |

안내는 기존 프레임과 일러스트 X 버튼을 사용하고 한 번에 하나만 표시한다. 바로 나타나는 대신 잠깐의 대기 시간을 두며, 완료 또는 닫기 다음 안내까지는 8초 간격을 둔다. 글과 배경은 마우스 입력을 가로채지 않고 X 버튼만 클릭을 받는다. 포커스를 강제로 옮기거나 별도의 확인 단계를 추가하지 않는다.

직접 조작에 성공하거나 X로 닫으면 같은 안내를 다시 표시하지 않는다. 모달·대화·적 턴·행동 연출·타이밍 입력 중에는 숨기고, 기존 선택형 4단계 입문 연습 중에는 별도 안내를 끼워 넣지 않는다. 하단의 다른 메시지가 필요한 경우에는 맵·적 기술 안내가 기다린다. 방향 안내는 기존 방향 확정 문구 위에 배치한다.

기억한 항목은 기존 전역 `settings.json`의 `SeenContextHints` 비트에 저장한다. 이전 설정에 이 필드가 없으면 미확인 상태로 읽으며 설정 버전과 캠페인 형식은 바꾸지 않는다. 저장 슬롯 변경이나 전투 재시작으로 초기화하지 않고, 오래 열어 둔 설정 초안을 저장해도 이미 배운 항목을 합쳐 보존한다. 자동 기록은 현재 적용된 설정만 저장하며 화면 설정·자동 타이밍을 다시 적용하거나 캠페인을 저장하지 않는다. 보호된 설정 원본은 덮어쓰지 않고, 저장에 실패해도 실행 중에는 기억하여 반복 표시·반복 저장을 막는다.

## 좁은 화면과 검증 범위

표시 위치는 명령창과 상단 메뉴를 제외한 실제 전장 영역 안에서 계산한다. 비중이 낮은 HP바를 배치할 공간이 부족하면 작은 바로 줄이며, 그래도 자리가 없으면 해당 바를 잠시 숨긴다. 공간이 생기면 다시 표시한다. 화면 밖의 비선택 유닛을 가장자리의 HP바로 빽빽하게 쌓지 않는다. 매우 작은 화면이나 임의의 긴 콘텐츠까지 모든 표시를 동시에 유지한다는 보장은 하지 않는다.

전용 검수는 실제 전투 세션의 아군 6명과 적을 가깝게 배치한 격리 저장 fixture를 사용한다. 피해·회복 숫자 일부는 배치 검사용 합성 값이며 실제 피해·성장·보상을 지급하는 완주 테스트와 구별한다. 캡처는 Unity가 렌더링한 화면이며 생성형 시안이 아니다. 장시간 성능, 다른 PC/DPI, 모든 캠페인의 수동 완주 검증을 대신하지 않는다.

## 검증 기록과 실제 캡처

Unity 6000.6.0f1의 실제 EditMode **363/363**(15.73초), 전체 PlayMode 최초 **231/233**(557.50초)를 실행했다. 실패 2건은 모달을 연 뒤 `LateUpdate`의 숨김 처리보다 먼저 상태를 읽던 테스트 대기 조건이었다. 렌더 시점 검사로 보완한 뒤 관련 안내 **6/6**(20.66초), 기술명 **3/3**(20.36초)를 통과했다. 이후 방향 화살표 영역 예약을 보완하고 밀집 배치 **1/1**(11.39초)을 추가 통과했다. 최종 단일 전체 233/233 실행으로 표현하지 않는다.

Editor 전장 검수는 **48/48**, 기존 UI 검수는 **1,200/1,200**에서 문제·수집 게임 오류 0을 기록했다. 전장 검수는 1024×768·1920×1080·1080×1440, 글자 100/130%, 회전 4방향의 밀집 표시와 안내·메시지 시나리오를 포함한다. 기존 UI 검수는 80상태×5해상도×글자 100/115/130%다. 초기 전장 검수 35행은 1화 출전 인원 정규화로 실제 아군이 2명이 되어 6인 fixture 전제를 충족하지 못했다. 방향 확정 뒤 행동 처리 단계에서 다음 안내를 찾던 흐름도 닫기 버튼 탐색 예외로 중단되었다. 원시 기록을 보존하고, 6인 출전이 가능한 임무 및 안내 검사의 단계 복원을 적용한 뒤 48행을 완료했다.

마지막으로 일반 적 상태창의 기술 탭에도 안내 완료 기록을 연결했다. 이 변경은 UI 배치나 전투 판정을 바꾸지 않으며, 해당 경로를 포함한 최종 안내 검사는 **7/7**(27.30초) 통과했다. 현재 PlayMode 테스트 정의는 234개이며, 최종 단일 전체 234/234 실행은 아니다. 최종 Windows 전장 검수는 실제 창 크기 1902×991, 글자 100/130%, 회전 4방향과 문맥 시나리오 **32/32**, 문제·수집 오류 0, 정상 종료를 확인했다. 최종 일반·검수 빌드도 성공했으며 일반 DLL에서 검수 코드가 제외됨을 확인했다.

| 기록 | 실제 결과 / 현재 상태 |
|---|---|
| [소스 동기화](Validation/source-sync.json) | 개발 소스와 Unity 검증 사본의 파일별 SHA-256 대응 |
| [Unity EditMode](Validation/tests-editmode.json) | 363/363, 15.73초 |
| [전체 Unity PlayMode 최초 실행](Validation/tests-playmode.json) | 231/233, 557.50초. 실패 2건의 원시 메시지와 스택 보존 |
| [안내 최종 재검사](Validation/tests-context-final.json) | 7/7, 27.30초. 일반 적 상태창 기술 탭의 완료 기록 및 아군·미리보기 제외 포함 |
| [기술명 재검사](Validation/tests-skill-cue-final.json) | 3/3, 20.36초 |
| [방향 선택·밀집 배치 재검사](Validation/tests-crowded-final.json) | 1/1, 11.39초 |
| [Editor 전장 초기 기록](Validation/editor-clarity-initial-fixture.json) | 35행 모두 6인 전제 불충족(아군 2명), 이후 안내 단계 전제로 중단. 통과로 계산하지 않음 |
| [Editor 전장 가독성](Validation/editor-clarity.json) | 48/48, 문제·수집 오류 0. 각 행에 실제 캡처 파일명 포함 |
| [Editor 기존 UI](Validation/editor-ui.json) | 1,200/1,200, 문제·수집 오류 0 |
| [Windows 전장 가독성](Validation/windows-clarity.json), [실행 요약](Validation/windows-review-run.json) | 1902×991, 32/32. 문제·수집 오류 0, 종료 코드 0 |
| [일반 Windows 빌드](Validation/windows-build.json) | 최종 소스 빌드 성공, 21.57초·오류 0·경고 2 |
| [검수 Windows 빌드](Validation/review-build.json) | 최종 소스 빌드 성공, 16.54초·오류 0·경고 2 |
| [일반·검수 DLL 분리](Validation/build-isolation.json) | 일반 DLL의 검수 타입 제외, 검수 DLL의 검수 타입 포함 확인 |
| [일반 빌드 시작·저장 보존](Validation/windows-startup.json) | 12.16초 실행 유지·응답, 로그 오류 0, 사용자 8파일 해시 불변, 기존 실행 중 플레이어 보존 |
| [최종 검증 요약](Validation/verification-summary.json) | 소스 39개 동기화, 작업 전후 사용자 8파일 불변, 기존 플레이어 보존, 선별 이미지 3개 원본 해시 일치 |

최종 두 빌드의 경고 2건은 Pipeline 런타임 설정 미지정과 기존 `DEVELOPMENT_BUILD` 전처리 사용이다. 경고가 없었다고 표현하지 않는다.

실제 캡처에서는 밀집 전투, 기술명과 피해 숫자, 세 종류 안내, 모달에서 안내가 숨겨지는 상태, 결과 문구가 만료된 상태를 확인한다. 원시 캡처 이름은 각 보고서의 `capture` 필드에 있다. 일반 실행본은 숨김 시작·응답·로그만 확인했으며 타이틀 화면 수동 관찰이나 게임 입력은 수행하지 않았다. 작업 시작 전후 사용자 8파일 해시가 같고 기존 플레이어도 유지됨을 최종 확인했다. 이번 작업은 `develop` 개발이며 공개 preview.8 릴리즈 갱신을 포함하지 않는다.

### 선별한 실제 화면

- [Windows 밀집 전투의 기술명·피해 숫자](Screenshots/windows-crowded-feedback.png)
- [Windows 적 기술 조회 안내](Screenshots/windows-enemy-hint.png)
- [Editor 1024×768 방향 확정 안내](Screenshots/editor-facing-1024.png)

## 주요 구현 위치

- [기술명 표시](../../Assets/TalesTactics/Runtime/Presentation/BattleSkillCue.cs), [표시 공간 계산](../../Assets/TalesTactics/Runtime/Presentation/CombatLabelPlacement.cs), [HP 배치](../../Assets/TalesTactics/Runtime/Presentation/UnitHealthLayout.cs), [피해·경험치 표시](../../Assets/TalesTactics/Runtime/Presentation/CombatFeedbackHud.cs)
- [행동 메시지 수명](../../Assets/TalesTactics/Runtime/Presentation/BattleActionFeedback.cs), [반격·지원 표시](../../Assets/TalesTactics/Runtime/Presentation/BattleRewindControls.cs)
- [안내 표시](../../Assets/TalesTactics/Runtime/Presentation/BattleContextHintHud.cs), [안내 기억과 저장](../../Assets/TalesTactics/Runtime/Presentation/BattleContextHints.cs), [일반 상태창 기술 탭](../../Assets/TalesTactics/Runtime/Presentation/BattleCharacterStatusHud.cs), [설정 데이터](../../Assets/TalesTactics/Runtime/Core/PlayerPreferences.cs)
- [전장 검수기](../../Assets/TalesTactics/Runtime/Presentation/CombatClarityReview.cs), [안내 PlayMode 검사](../../Assets/TalesTactics/PlayModeTests/ContextHintSceneTests.cs), [메시지 PlayMode 검사](../../Assets/TalesTactics/PlayModeTests/ActionFeedbackSceneTests.cs), [설정 EditMode 검사](../../Assets/TalesTactics/Tests/ContextHintPreferencesTests.cs)
