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

레벨 1–50, 고정 성장. EXP는 현재 레벨 ×100마다 레벨업, 전투 승리 기본 EXP120. 장비 3슬롯의 ID를 저장하고 Catalog에서 해결한다. EquipmentLoadout의 분리된 초안으로 장비를 미리보고 적용 시 저장한다. 슬롯 및 무기 종류를 검증하며 저장 실패 시 기존 장비로 복원한다. 훈련과 캠페인 모두 저장된 장비를 적용한다. 성장·승급 화면은 저장된 성장과 장비 기준으로 승급 전후 수치를 표시한다. CampaignPromotion이 조건 재검사와 1회 승급·저장 실패 복원을 담당한다. 캠페인 두 장의 완료 이벤트로 스토리 조건을 기록한다. 승급은 레벨 및 Story Flag 검사 API가 준비되어 있다. Battle Runtime은 저장하지 않는다.

## 화면과 검증

BattleHud가 Canvas 배율을 반영한 전장 viewport를 제공한다. BoardView는 해당 영역에 카메라를 배치하고 전장 bounds를 투영해 전체 타일이 UI에 가리지 않도록 맞춘다.

실제 Unity EditMode 57개, PlayMode 12개 및 Windows 빌드/기본 입력 검수 완료. 결과와 남은 한계는 VALIDATION.md를 따른다.

화면 비율이 1.2 미만이면 전투 중 하단 2열 HUD를 사용한다. viewport와 패널 배치는 동일 조건으로 계산하며, 카메라의 회전/배율은 화면 크기 변경 시 유지하고 전투 시작 시 초기화한다. 카메라 조작은 전투 상태를 변경하지 않는다. 타이밍 회전 연출 중에는 카메라 회전/초기화를 잠근다.

ActionSelection → SkillDetails → TargetSelection 순서로 스킬을 조회한다. 상세 조회는 CanUse 결과를 보여 주며 전투 상태/자원을 소모하지 않는다. SkillResolver.Describe와 Preview가 실제 SkillData 효과를 표시하고, AffectCaster 효과는 시전자 대상으로 구분한다.

CampaignFile은 경로별 저장 I/O를 분리한다. Version 1의 누락 컬렉션·장비 슬롯과 성장 범위를 정규화하며, 중복/빈 캐릭터 ID는 유효한 저장으로 취급하지 않는다. 기본 파일 손상 시 .bak를 읽고, 다음 저장 시 손상 원본을 .corrupt-GUID로 보존한 뒤 기본 파일을 교체한다. 복구 불가 또는 미래 버전은 세션 저장을 차단한다. 출전 화면에 상태를 표시하며 원본 파일을 고친 뒤 재실행해야 한다.

## 캠페인 진행 프로토타입

CampaignStages는 chapter1 → chapter2 완료 플래그와 순차 해금을 정의한다. 동일 TestStage를 재사용하며 1장 적 Lv1, 2장 적 Lv3이다. 훈련은 Lv25를 유지하고 완료 기록·EXP를 저장하지 않는다. 반복 클리어도 출전 전원 EXP120을 주며, 장 완료 플래그는 중복 추가하지 않는다. 상세 스토리/대사/전용 맵 및 성장 속도 밸런스는 미구현이다.

BattleDirector는 시작 시 선택 장과 훈련 여부를 고정한다. 승리에만 CampaignStages.TryReward를 호출하며, 실패 시 레벨/EXP/스킬/완료 플래그를 복원한다. 결과 화면에서 보상 저장을 재시도할 수 있고 성공 후에는 중복 지급하지 않는다. 저장 실패 후 출전 화면으로 돌아가면 해당 보상을 포기한다. PersistCampaign 콜백은 기본 CampaignStorage.Save이며 PlayMode 테스트에서는 사용자 파일을 건드리지 않는 콜백으로 교체한다.

## 개발 빌드의 캠페인 검수

CampaignPlayerReview는 UNITY_EDITOR 또는 DEVELOPMENT_BUILD에서만 컴파일되며 에디터에서는 자동 실행하지 않는다. 개발 플레이어의 `--campaign-review <GUID> <chapter1|chapter2|resume>` 인자로만 활성화된다. 실제 전투 상태/버튼 submit/이동·공격 코루틴을 사용하고, 별도 CampaignFile을 BattleDirector.PersistCampaign에 연결하여 사용자 저장을 쓰지 않는다. 일반 배포 DLL에는 해당 타입이 없다. RunCampaignReview.ps1이 세 번의 프로세스 실행, 종료 코드·보고서·사용자 저장 해시 검사를 담당한다. 세부 제한과 재실행은 PLAYER_REVIEW.md를 따른다.

## 장비 재고와 상점

CampaignSave Version 2는 Gold와 Inventory(ID/Count)를 저장한다. Count는 장착분을 포함한 전체 보유량이며 Equipped를 제외한 수량이 미장착 수량이다. EquipmentLoadout은 초안 캐릭터를 제외한 다른 모든 캐릭터의 장착분을 예약으로 계산하고 적용 직전 다시 검사한다. 구매/장착은 PersistCampaign을 통해 저장하며 실패/예외 시 소지금·수량·슬롯을 복원한다. HUD의 장비/승급/타이밍 설정도 동일 저장 콜백을 사용한다.

CampaignInventory는 초기 300G/청동검 1개, 최대 9,999,999G/장비별 99개를 정의한다. EquipmentData.BuyPrice가 양수인 장비만 상점에 표시하며 가격/보너스는 Content 에셋에서 조정한다. 캠페인 보상은 EXP와 완료 플래그에 골드(1장 120/2장 180)를 묶어 원자적으로 저장/복원한다. 훈련·패배에는 보상을 지급하지 않는다. 현재는 구매만 지원하고 매각/드롭은 후속 범위다.

Version 1을 로드할 때 기존 장착 ID별 개수와 최소 청동검 1개를 이관한다. 원본은 즉시 수정하지 않으며 다음 저장 시 .v1.bak를 1회 보관한다. Version 2의 중복 ID/잘못된 수량/보유 초과 장착은 손상 저장으로 취급해 기존 백업 복구 또는 덮어쓰기 차단 경로를 따른다. Version 3 이상은 저장을 차단한다.

ShopContent.AddSamples는 기존 카탈로그에 가죽 갑옷/생명의 부적을 추가하는 명시적 에디터 메뉴이며 DemoContent/Scene을 재생성하지 않는다. 기존 샘플 에셋의 능력치는 보존한다. 메뉴를 다시 실행하면 가격 0인 청동검만 100G로 설정하므로, 사용자가 청동검 판매를 막으려고 가격을 0으로 바꾼 뒤에는 이 메뉴를 재실행하지 않는다.

장비 매각은 CampaignInventory.TrySell이 담당한다. 구매가/2(정수 버림)가 양수인 미장착 장비만 1개씩 매각하며 골드 상한을 초과하면 거래 전체를 거부한다. 마지막 1개 매각 시 재고 항목을 제거하고 저장 실패/예외 시 원래 항목 순서·수량·골드를 복원한다. Version 2 형식은 그대로 유지한다. BattleSellHud는 상점의 보유 장비 목록·상세·훈련/저장 보호 안내를 담당한다.

상점 단계 해금: EquipmentData.RequiredStoryFlag가 비어 있으면 기본 상품이다. CampaignInventory.PurchaseUnlockRequirement/PurchaseUnavailable이 실제 완료 기록을 검사하므로 UI 외 구매 호출도 잠긴 상품을 구매할 수 없다. 기존 보유 장비의 사용/매각은 해금 조건과 독립적이다. ChapterShopContent는 기존 에셋을 덮어쓰지 않고 철검/강화 갑옷을 추가하는 명시적 메뉴다. 저장 형식은 Version 2를 유지한다.

확정 장비 보상은 CampaignStages.EquipmentReward로 장별 ID를 정의한다(1장 vital-charm, 2장 iron-sword). TryReward가 장비 수량과 EXP·골드·완료 기록을 함께 저장하고 실패/예외 시 복원한다. 상한에서는 장비만 건너뛰며 다른 보상은 정상 지급한다. BattleDirector의 RewardPending이 동일 전투의 성공 이후 중복 지급을 막는다. 전투 자체는 저장하지 않으며 확률 추첨은 없다. 기존 Version 2 형식을 유지한다.
