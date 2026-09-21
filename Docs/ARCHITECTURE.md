# 아키텍처

## 레이어

- Runtime/Data: ScriptableObject와 직렬화 가능한 정의. 원본 에셋은 전투 중 수정하지 않음.
- Runtime/Core: GridMap/Dijkstra, UnitRuntime, SkillResolver, SpeedTurnScheduler, EnemyPlanner, BattleSession, CampaignStorage.
- Runtime/Presentation: BattleDirector는 코루틴/연출 조정. BattleStates의 상태별 클래스가 명령과 타일 선택을 담당. BoardView는 시각화, BattleHud는 uGUI 입력 화면, BattleAudio는 ID 기반 음원 재생.
- Editor: DemoContent는 최초 캐릭터/스킬/규칙 에셋 생성. ProjectSetup은 Camera/Canvas/EventSystem/Board/Director가 연결된 테스트 Scene 생성과 빌드 메뉴 제공.
- Tests: NUnit 규칙 테스트. Tools/ManagedChecks는 동일 테스트를 Unity 밖에서 실행하기 위한 한정된 대체 API 환경이며 게임 빌드에 포함되지 않음.

## 전투 상태

BattleStart → TurnStart → Command → MoveSelection / ActionSelection → TargetSelection → ActionExecution → Command → FacingSelection → TurnEnd. 결과 감지 시 BattleEnd. 적은 동일 이동·스킬 규칙을 이용하며 코루틴 동안 사용자 명령을 잠금.

SpeedTurnScheduler는 매 라운드 시작 시 살아 있는 모든 팀 유닛을 SPD 내림차순으로 정렬한다. 라운드 중 SPD 변화는 다음 라운드부터 순서에 반영된다. 동률은 배치 순서. 인터페이스를 바꾸지 않고 CT 방식으로 교체 가능.

이동은 4방향 Dijkstra, 이동비용 합산, 개별 계단 차이 <= JMP. 타일 점유는 살아 있는 유닛만. KO 위치는 런타임에 보존하여 부활 대상으로 사용한다. 부활 위치에 다른 유닛이 있으면 실행하지 않는다.

기본 피해는 max(1, round((공격 능력치 × 배율 + 고정값 − 방어 × 방어계수) × 방향 × 가드)). 현재 명중 100%, 피해 확정값; 상태 효과만 확률이다. 속성은 메타데이터이며 저항 시스템은 후속 작업.

현재 AoE는 맨해튼 거리 다이아몬드. 시야 차단/높이별 사거리/직선 관통 형태는 아직 없다. Effects의 AffectCaster로 가디언 필드의 적 피해와 자신의 회복을 분리한다.

## 고유 규칙

- 컨슘 클로: 발동 턴 종료는 소모하지 않고 이후 자신의 3턴에 유지. STR +20%, SPD +15%. 나이트메어/궁극기는 클로 상태 중 선행 공격 필요.
- 알펜: 일반 검술 뒤에만 같은 턴 Flaming Edge 추가 행동 허용. HP는 1 미만으로 내려가는 비용 지불 불가. 연계 후 추가 행동 불가.
- 파라: 일반 기술 모두 해금 시 사자전후의 입력 창. 입력은 한 번만 판정. 성공하면 사자전후 대신 궁극 효과를 실행하며 MP50만 사용한다. 현재 회전은 임시 Sprite 회전이다.
- 키사라: 가드 후 정면/측면 피해 감소로 Ignition 활성. 다음 공격 강화, 궁극기 조건 충족 후 소비. 방향 보정/가드 수치/클로/성가/케이지 보정은 BattleRules에서 편집.
- 자동 부활은 KO 판정 시 1회 소비. 일반 부활은 빈 원래 타일에만 배치.

## 저장과 성장

레벨 1–50, 고정 성장. EXP는 현재 레벨 ×100마다 레벨업, 전투 승리 기본 EXP120. 장비 3슬롯의 ID를 저장하고 Catalog에서 해결한다. 장비 교체 UI와 스토리 승급 UI는 후속 작업이다. 승급은 레벨 및 Story Flag 검사 API가 준비되어 있다. Battle Runtime은 저장하지 않는다.

## 화면과 검증

BattleHud가 Canvas 배율을 반영한 전장 viewport를 제공한다. BoardView는 해당 영역에 카메라를 배치하고 전장 bounds를 투영해 전체 타일이 UI에 가리지 않도록 맞춘다.

실제 Unity EditMode 34개, PlayMode 6개 및 Windows 빌드/기본 입력 검수 완료. 결과와 남은 한계는 VALIDATION.md를 따른다.

화면 비율이 1.2 미만이면 전투 중 하단 2열 HUD를 사용한다. viewport와 패널 배치는 동일 조건으로 계산하며, 카메라의 회전/배율은 화면 크기 변경 시 유지하고 전투 시작 시 초기화한다. 카메라 조작은 전투 상태를 변경하지 않는다. 타이밍 회전 연출 중에는 카메라 회전/초기화를 잠근다.
