# 아키텍처

## 게임패드와 포함 폰트

BattleDirector.Start가 GamepadPointer를 생성한다. Gamepad 이벤트를 화면 포인터로 변환하고 EventSystem의 UI raycast/기존 클릭 콜백 또는 Board.Pick/State.Tile을 사용한다. 전투 규칙을 별도 구현하지 않는다. 메뉴 순환은 현재 활성·상호작용 가능한 버튼만 대상으로 하고, B 취소는 상태의 Cancel 또는 메뉴의 돌아가기/목록/출전 준비 버튼을 사용한다. 실행 순서 -100으로 기본 UI submit보다 먼저 게임패드 사용을 감지하고 sendNavigationEvents를 끄므로 A 입력이 두 번 실행되지 않는다. 마우스 전환·장치 해제·컴포넌트 비활성화 시 기존 설정을 복원한다. 커서 Canvas는 raycast를 막지 않는다. 가상 Mouse 장치를 만들거나 실제 OS 포인터를 이동하지 않는다.

BattleHud.Font가 미지정이면 Resources/TalesTactics/Korean의 정적 TMP 폰트를 선택한다. Noto Sans CJK KR 원본 OTF와 라이선스를 포함하고 같은 원본의 동적 fallback(1024, Clear Dynamic Data On Build)을 연결한다. 런타임 소유 OS 폰트와 공유 Resources 폰트의 수명을 구분하며 사용자 지정 Font는 덮어쓰지 않는다. 생성 도구와 배포 경로는 INPUT_DISTRIBUTION.md를 따른다.

## 아트·전투 연출

CharacterData.Poses는 Attack/Cast/Guard/Damage의 4방향 Sprite와 Walk/Dead/Skill/Ultimate의 DirectionalSpriteClip 참조다. CharacterMotion은 카메라 상대 방향으로 포즈를 고르고 공격 준비/복귀 및 피격 종료 뒤 기본 그림으로 돌아온다. Skill/Ultimate는 각 2개 준비/발동 프레임이며 BoardView.BeginSkill → ReleaseSkill로 실제 판정에 맞춰 전환한다. 일반 시전도 준비/발동 단계에서는 Skill 클립을 사용한다. 클립이 없으면 Attack/Cast 포즈를 대체 사용한다. 별도 Animator가 있으면 BoardView가 CharacterMotion을 추가하지 않는다. 포즈별 실제 사각 경계·발 pivot·렌더링 윤곽은 Tools의 CSV에서 Sprite Editor Data Provider API로 임포트한다. 원본 PNG와 기존 전투 수치는 유지한다.

Poses.Walk/Dead는 DirectionalSpriteClip의 방향별 프레임 배열과 FPS로 구성한다. Walk는 반복, Dead는 마지막 프레임 유지이며 상태 시간은 CharacterMotion이 관리한다. 경로 방향 전환과 동일 KO 상태 갱신은 시간을 초기화하지 않는다. 부활로 Idle에 들어가면 기존 그림·크기를 복구한다. 누락된 현재 프레임은 기본 그림과 기존 Transform 동작으로 대체한다. 전체 10명에 각 4방향 걷기 4프레임/쓰러짐 3프레임, 총 280개를 연결했다. 별도 프레임이 없는 적 캐릭터는 기존 동작을 사용한다.

CharacterMotion은 Animator가 없는 유닛에 방향 그림과 Transform 기반 기본 동작을 제공한다. 카메라 회전 기준으로 화면 방향을 선택하며 실제 전투 Facing은 바꾸지 않는다. BattleDirector가 판정 전에 HP와 대상 목록을 보관하고 BoardView.PresentImpact에 전달한다. KO 후 Targets에서 사라진 유닛도 해당 목록으로 연출한다. HP 소모 비용과 실제 회복/피격은 구분한다.

CombatEffect는 SkillData.Presentation.Pattern이 있으면 기술 패턴을, Automatic/CharacterStyle이면 기존 10가지 VisualStyle을 사용한다. 모든 기본 공격은 명시적 CharacterStyle 프로필로 무기별 표현을 유지한다. VisualStyle은 기술의 명시값 → 캐릭터 명시값 → 무기 기본값 순으로 선택한다. 회복/부활은 의미가 명확한 기존 전용 효과를 우선한다. Presentation.Pulses는 시각 반복만 제어하며 판정·비용·RNG에는 관여하지 않는다. 준비 시간 동안 축소 고리/기술명을 표시하고 판정 후 발동 프레임·효과를 시작한다. Windup/Recovery는 유한한 0.08–1.5초로 제한한다. 효과는 0.6초 뒤 소멸하며 Restart는 전장 하위 효과와 BoardView 코루틴을 함께 정리한다.

BattleAudio.PlayBattle은 2장의 boss 또는 battle을 선택한다. BeginTheme은 실제 캐릭터 테마가 있을 때만 이전 AudioClip·샘플 위치·반복/재생 상태를 보관한다. EndTheme은 복귀하고 Play/StopAll은 예약 복귀를 취소한다. 음원이 없는 테마는 기존 음악을 끊지 않는다. 현재 프로젝트 오리지널 14곡이 연결돼 있다. MusicVolume은 기본 0.28이며 효과음 소스는 0.45다. 긴 BGM은 Streaming, 테마/승리는 CompressedInMemory를 사용한다. 제작법과 출처는 ORIGINAL_MUSIC.md를 따른다.

방향 선택은 카메라 pitch를 버리고 yaw의 4분면으로 양자화한다. 45도 경계는 일관된 방향을 선택해 기울어진 카메라에서 앞/뒤가 좌/우에 합쳐지는 문제를 막는다. 파라 타이밍 연출은 이 선택기에 시각 회전 720도를 전달하며 UnitRuntime.Facing은 바꾸지 않는다. 바닥 진행 고리 바깥에 성공 구간을 별도 표시한다. ClearTiming/ResetBoard는 두 고리와 회전 상태를 모두 정리한다.

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

기본 피해는 max(1, round((공격 능력치 × 배율 + 고정값 − 방어 × 방어계수) × 방향 × 가드)). 현재 명중 100%, 피해 확정값; 상태 효과만 확률이다. ElementalRules가 CharacterData.Affinities의 피해 배율을 적용한다. 무속성/미지정은 1, 무효는 0이며 유효 배율은 0~2로 제한한다. 물리 높이 보정은 단계당 10%, 최대 두 단계다. 마법·회복에는 높이 피해 보정을 적용하지 않는다.

기본 AoE는 맨해튼 거리 마름모이며 스킬별 직선/부채꼴, 시야 차단과 높이 차 제한도 지원한다. Effects의 AffectCaster로 가디언 필드의 적 피해와 자신의 회복을 분리한다.

## 고유 규칙

- 컨슘 클로: 발동 턴 종료는 소모하지 않고 이후 자신의 3턴에 유지. STR +20%, SPD +15%. 나이트메어/궁극기는 클로 상태 중 선행 공격 필요.
- 알펜: 일반 검술 뒤에만 같은 턴 Flaming Edge 추가 행동 허용. HP는 1 미만으로 내려가는 비용 지불 불가. 연계 후 추가 행동 불가.
- 파라: 일반 기술 모두 해금 시 사자전후의 입력 창. 입력은 한 번만 판정. 성공하면 사자전후 대신 궁극 효과를 실행하며 MP50만 사용한다. 현재 회전은 임시 Sprite 회전이다.
- 키사라: 가드 후 정면/측면 피해 감소로 Ignition 활성. 다음 공격 강화, 궁극기 조건 충족 후 소비. 방향 보정/가드 수치/클로/성가/케이지 보정은 BattleRules에서 편집.
- 자동 부활은 KO 판정 시 1회 소비. 일반 부활은 빈 원래 타일에만 배치.

## 저장과 성장

레벨 1–50, 고정 성장. EXP는 현재 레벨 ×100마다 레벨업, 최초 클리어 EXP는 1장120/2장180/3장300, 반복은60/90/150. 장비 3슬롯의 ID를 저장하고 Catalog에서 해결한다. EquipmentLoadout의 분리된 초안으로 장비를 미리보고 적용 시 저장한다. 슬롯 및 무기 종류를 검증하며 저장 실패 시 기존 장비로 복원한다. 훈련과 캠페인 모두 저장된 장비를 적용한다. 성장·승급 화면은 저장된 성장과 장비 기준으로 승급 전후 수치를 표시한다. CampaignPromotion이 조건 재검사와 1회 승급·저장 실패 복원을 담당한다. 캠페인 세 장의 완료 이벤트로 스토리 조건을 기록한다. 승급은 레벨 및 Story Flag 검사 API가 준비되어 있다. Battle Runtime은 저장하지 않는다.

## 화면과 검증

BattleHud가 Canvas 배율을 반영한 전장 viewport를 제공한다. BoardView는 해당 영역에 카메라를 배치하고 전장 bounds를 투영해 전체 타일이 UI에 가리지 않도록 맞춘다.

실제 Unity EditMode 92개, PlayMode 51개 및 Windows 빌드/기본 입력 검수 완료. 결과와 남은 한계는 VALIDATION.md를 따른다.

화면 비율이 1.2 미만이면 전투 중 하단 2열 HUD를 사용한다. viewport와 패널 배치는 동일 조건으로 계산하며, 카메라의 회전/배율은 화면 크기 변경 시 유지하고 전투 시작 시 초기화한다. 카메라 조작은 전투 상태를 변경하지 않는다. 타이밍 회전 연출 중에는 카메라 회전/초기화를 잠근다.

ActionSelection → SkillDetails → TargetSelection 순서로 스킬을 조회한다. 상세 조회는 CanUse 결과를 보여 주며 전투 상태/자원을 소모하지 않는다. SkillResolver.Describe와 Preview가 실제 SkillData 효과를 표시하고, AffectCaster 효과는 시전자 대상으로 구분한다.

CampaignFile은 경로별 저장 I/O를 분리한다. Version 1의 누락 컬렉션·장비 슬롯과 성장 범위를 정규화하며, 중복/빈 캐릭터 ID는 유효한 저장으로 취급하지 않는다. 기본 파일 손상 시 .bak를 읽고, 다음 저장 시 손상 원본을 .corrupt-GUID로 보존한 뒤 기본 파일을 교체한다. 복구 불가 또는 미래 버전은 세션 저장을 차단한다. 출전 화면에 상태를 표시하며 원본 파일을 고친 뒤 재실행해야 한다.

## 캠페인 진행 프로토타입

CampaignStages는 chapter1 → chapter2 → chapter3 완료 플래그와 순차 해금을 정의한다. CampaignContent의 전용 맵을 사용하며 CampaignStages.EnemyLevel로 1장 Lv1, 2장 Lv3, 3장 Lv4를 명시한다. 훈련만 기존 TestStage를 사용한다. 훈련은 Lv25를 유지하고 완료 기록·EXP를 저장하지 않는다. 반복 클리어는 출전 전원에게 장별 EXP60/90/150을 주며, 장 완료 플래그는 중복 추가하지 않는다. 전투 전후 대사·다시 읽기와 세 장 전용 맵을 제공한다. 두 장 최초 완료 시 Lv3/EXP0, 세 장 최초 완료 시 Lv4/EXP0이 된다. 기존 V2 저장은 이관 없이 3장을 해금하며 Lv20 승급 조건은 그대로다.

BattleDirector는 시작 시 선택 장과 훈련 여부를 고정한다. 승리에만 CampaignStages.TryReward를 호출하며, 실패 시 레벨/EXP/스킬/완료 플래그를 복원한다. 결과 화면에서 보상 저장을 재시도할 수 있고 성공 후에는 중복 지급하지 않는다. 저장 실패 후 출전 화면으로 돌아가면 해당 보상을 포기한다. PersistCampaign 콜백은 기본 CampaignStorage.Save이며 PlayMode 테스트에서는 사용자 파일을 건드리지 않는 콜백으로 교체한다.

## 개발 빌드의 캠페인 검수

CampaignPlayerReview는 UNITY_EDITOR 또는 DEVELOPMENT_BUILD에서만 컴파일되며 에디터에서는 자동 실행하지 않는다. 개발 플레이어의 `--campaign-review <GUID> <chapter1|chapter2|resume>` 인자로만 활성화된다. 실제 전투 상태/버튼 submit/이동·공격 코루틴을 사용하고, 별도 CampaignFile을 BattleDirector.PersistCampaign에 연결하여 사용자 저장을 쓰지 않는다. 일반 배포 DLL에는 해당 타입이 없다. RunCampaignReview.ps1이 세 번의 프로세스 실행, 종료 코드·보고서·사용자 저장 해시 검사를 담당한다. 세부 제한과 재실행은 PLAYER_REVIEW.md를 따른다.

## 장비 재고와 상점

CampaignSave Version 2는 Gold와 Inventory(ID/Count)를 저장한다. Count는 장착분을 포함한 전체 보유량이며 Equipped를 제외한 수량이 미장착 수량이다. EquipmentLoadout은 초안 캐릭터를 제외한 다른 모든 캐릭터의 장착분을 예약으로 계산하고 적용 직전 다시 검사한다. 구매/장착은 PersistCampaign을 통해 저장하며 실패/예외 시 소지금·수량·슬롯을 복원한다. HUD의 장비/승급/타이밍 설정도 동일 저장 콜백을 사용한다.

CampaignInventory는 초기 300G/청동검 1개, 최대 9,999,999G/장비별 99개를 정의한다. EquipmentData.BuyPrice가 양수인 장비만 상점에 표시하며 가격/보너스는 Content 에셋에서 조정한다. 캠페인 보상은 EXP와 완료 플래그에 골드(최초 120/180, 반복60/90)를 묶어 원자적으로 저장/복원한다. 훈련·패배에는 보상을 지급하지 않는다. 구매·매각·확정/확률 드롭을 지원한다.

Version 1을 로드할 때 기존 장착 ID별 개수와 최소 청동검 1개를 이관한다. 원본은 즉시 수정하지 않으며 다음 저장 시 .v1.bak를 1회 보관한다. Version 2의 중복 ID/잘못된 수량/보유 초과 장착은 손상 저장으로 취급해 기존 백업 복구 또는 덮어쓰기 차단 경로를 따른다. Version 3 이상은 저장을 차단한다.

ShopContent.AddSamples는 기존 카탈로그에 가죽 갑옷/생명의 부적을 추가하는 명시적 에디터 메뉴이며 DemoContent/Scene을 재생성하지 않는다. 기존 샘플 에셋의 능력치는 보존한다. 메뉴를 다시 실행하면 가격 0인 청동검만 100G로 설정하므로, 사용자가 청동검 판매를 막으려고 가격을 0으로 바꾼 뒤에는 이 메뉴를 재실행하지 않는다.

장비 매각은 CampaignInventory.TrySell이 담당한다. 구매가/2(정수 버림)가 양수인 미장착 장비만 1개씩 매각하며 골드 상한을 초과하면 거래 전체를 거부한다. 마지막 1개 매각 시 재고 항목을 제거하고 저장 실패/예외 시 원래 항목 순서·수량·골드를 복원한다. Version 2 형식은 그대로 유지한다. BattleSellHud는 상점의 보유 장비 목록·상세·훈련/저장 보호 안내를 담당한다.

상점 단계 해금: EquipmentData.RequiredStoryFlag가 비어 있으면 기본 상품이다. CampaignInventory.PurchaseUnlockRequirement/PurchaseUnavailable이 실제 완료 기록을 검사하므로 UI 외 구매 호출도 잠긴 상품을 구매할 수 없다. 기존 보유 장비의 사용/매각은 해금 조건과 독립적이다. ChapterShopContent는 기존 에셋을 덮어쓰지 않고 철검/강화 갑옷을 추가하는 명시적 메뉴다. 저장 형식은 Version 2를 유지한다.

확정 장비 보상은 CampaignStages.EquipmentReward로 장별 ID를 정의한다(1장 vital-charm, 2장 iron-sword). TryReward가 장비 수량과 EXP·골드·완료 기록을 함께 저장하고 실패/예외 시 복원한다. 상한에서는 장비만 건너뛰며 다른 보상은 정상 지급한다. BattleDirector의 RewardPending이 동일 전투의 성공 이후 중복 지급을 막는다. 전투 자체와 미저장 보상은 디스크에 저장하지 않는다. 기존 Version 2 형식을 유지한다.


CampaignContent는 1장 11×9 물길/돌다리/우회로, 2장 12×10 계단/제단 맵과 6명 아군·4명 적 출전 좌표, 장별 전후 대사를 제공한다. BattleSession의 선택적 campaignStage는 기본 -1로 기존 테스트 맵/엔진 독립 테스트를 유지하며 캠페인에서만 전용 맵을 선택한다.
CampaignStory는 전체 화면 uGUI 이야기와 진행/건너뛰기를 담당한다. RequestBattle은 캠페인 도입 후 BeginBattle을 호출하고 훈련은 즉시 시작한다. 승리/보상 저장 성공 이후에만 후일담을 읽을 수 있으며 다시 읽기는 저장을 호출하지 않는다. 대화 중 전장 입력은 차단한다. 대화 위치는 저장하지 않으며 완료된 장의 대사는 언제든 다시 읽는다. 대사는 이 프로젝트용 창작 시나리오다.
BoardView는 전장 부분 viewport 아래에서 전체 화면을 지우는 cullingMask=0 배경 카메라를 생성하여 대사 패널 종료 뒤 화면 가장자리의 잔상을 방지한다.

SkillGeometry는 사거리/영향 타일/시야를 공유 계산한다. Shape=Diamond는 기존 목표 중심 마름모, Line/Cone은 시전자에서 선택한 상하좌우 방향으로 Range까지 뻗는다. Line은 폭0 관통, Cone은 전방거리>측면거리이면서 맨해튼 거리<=Range인 타일이다. 방향형 스킬은 대각선/자기 타일을 조준할 수 없다. MaxHeightDifference=-1은 제한 없음, RequiresLineOfSight=false는 기존 시야 무시 동작이다.
시야는 supercover 격자 광선으로 두 모서리 옆칸까지 검사한다. 비보행 타일/맵 구멍/시전자와 목표의 지면+1 높이를 잇는 선 이상인 중간 지형은 차단하며 유닛은 차단하지 않는다. 범위 내 각 영향 타일에도 높이/시야 규칙을 적용한다. Targets/Execute/ShowArea가 AreaTiles를 공유하고 EnemyPlanner의 기본 공격은 InRange를 사용한다.
대표 적용 메뉴 SkillGeometryContent.Apply는 피어싱 라인(Line/Range4/높이차2), 쌍장저파(Cone/Range3/높이차1), 나탈리아/시온 기본 공격(기존 사거리/높이차2)에 시야 필요를 지정한다. 명시적 적용 메뉴이며 이후 사용자 기하 규칙을 수정한 뒤 무조건 재실행하지 않는다. 비용·위력·해금 레벨은 변경하지 않는다. 이 대표 설정은 현재 CombatTuning.Apply의 전체 89개 기술 기준선으로 대체되었다. 전체 적용표는 COMBAT_BALANCE.md를 따른다.

CombatTuning은 기존 에셋의 기하·속성·저항을 명시적으로 적용하며 DemoContent를 재생성하지 않는다. 사용자 튜닝 이후 재실행하면 해당 필드를 덮어쓰므로 자동 실행하지 않는다. SkillGeometry.EffectiveRange는 활/총 물리 기술에 시전자-대상 높이 차를 최대 ±2칸 반영한다. DamagePreview의 선택적 SkillData 인자를 실제 실행과 EnemyPlanner가 공유한다. 속성은 피해에만 적용하며 상태 확률·회복에는 영향을 주지 않는다. 비용·위력·해금·게이트는 기존 값을 보존한다.

CTTurnScheduler는 ITurnScheduler의 선택 가능한 구현체로 CT1000/행동·SPD/틱·잔여 CT·순서 미리보기 복사본을 사용한다. BattleDirector.UseCT/UseUtilityAI를 출전 전 설정하고 세션 생성 시 고정한다. UtilityPlanner는 사용 가능한 기술과 도달 위치/목표를 점수화하며 실제 실행은 기존 SkillResolver와 EnemyTurn 코루틴을 사용한다. 기본 모드는 기존 SPD/기본 AI다.

BattleObjectives의 DefeatBoss, ReachDestination(일반/호위), SurviveTurns가 IVictoryCondition을 구현한다. BattleSession.EndTurn이 생존 카운터와 상태 종료를 한 번만 처리한다. 특수 목표는 훈련 맵만 허용하며 캠페인 생성자로 특수 목표를 요청하면 거부한다. 목표 설명은 HUD 하단에 표시하고 금색 타일/HP 막대로 목표를 표시한다. 세부 조건과 한계는 TACTICAL_SYSTEMS.md를 따른다.

CampaignEconomy.Prepare는 승리 시 최초/반복 EXP·골드·추가 장비 추첨 결과를 CampaignReward에 고정한다. BattleDirector가 이 객체를 실패/예외 후에도 보관하며 TryReward는 재추첨 없이 모든 수량·골드·성장·완료 플래그를 원자적으로 저장/복원한다. 성공한 객체의 Applied는 중복 적용을 차단한다. 최초/반복 상태가 달라진 오래된 객체도 거부한다. 기존 stage 인자 TryReward 오버로드는 추가 추첨 없는 결정적 호환 경로이며 실제 게임은 준비한 CampaignReward 경로를 사용한다.

추가 드롭은 청동검25%/장별 방어구15%/없음60%의 상호 배타적 한 번 추첨이다. 장비99개 상한은 해당 아이템만 건너뛰고 재추첨하지 않는다. 재고 객체 참조와 순서까지 실패 시 복원한다. 저장 버전2 및 기존 성장/재고를 보존하며 기존 완료 기록이 있는 장에는 반복 보상을 적용한다. 미저장 보상은 출전 화면으로 나가거나 앱을 종료하면 포기한다.

ManualPlayerReview는 DEVELOPMENT_BUILD/UNITY_EDITOR 조건부 검수 도구다. 명시적 --manual-review GUID로만 활성화하고 CampaignFile 저장 경로를 ManualReviews/GUID로 분리한다. 프레임시간·메모리·상태를 수집하며 전투 명령을 선택하지 않는다. MeasurePlayerProcess.ps1의 Windows 프로세스 메모리 기록과 함께 사용한다. 결과 및 재실행 파일 보존 절차는 FINAL_REVIEW.md를 따른다.


## 6장 확장

CampaignStages.Chapter의 추가 전용 배열이 제목·적/진입 레벨·최초/반복EXP/골드·확정 장비·보스 음악을 관리한다. 기존 stage0~2의 값과 chapter1~3 ID는 유지하며 새 장은 배열 뒤에 붙인다. Count는 배열 길이다. ExpansionCampaignContent는4~6장의 맵/스폰/전후 대사를 제공한다. 모든 캠페인 목표는 기존 적 전멸이다.

BattleHud.ChapterPage는3장 단위로 목록을 탐색하는 일시적UI 상태이며 저장하지 않는다. 목록 탐색 자체는 SelectedStage를 바꾸지 않는다. 장 버튼을 눌러야 실제 선택이 바뀐다. 장비/이야기 화면에서 돌아와도 페이지를 유지한다.

ExpansionShopContent.Add는 GuardianMedal/TemperedArmor가 없을 때만 새 에셋을 만들고 기존 카탈로그에 추가한다. 기존 에셋/사용자 튜닝은 덮어쓰지 않는다. 수호의 메달은chapter4, 단련 갑옷은chapter5로 판매를 해금한다. 보유 장비의 장착/매각은 기존 해금 독립 정책을 유지한다.

신규 캠페인의 동일 출전자는6장 최초 완료 시 Lv9/EXP0이 된다. 기존V2 저장 스키마·Lv20 승급·Lv50 상한은 유지하며 기존 저장의 경험치나 레벨을 다시 계산하지 않는다.

## 픽셀 캠페인 개편

BattleCatalog.Enemies는 ID로 조회하는 새 적 목록이다. CampaignEnemies가 장/슬롯별ID를 결정하며, 목록이 없는 레거시 카탈로그와 훈련은 기존 Enemy를 사용한다. 목록이 있는데 필수ID가 없으면 검증/세션 생성에서 실패한다. 캠페인 적은 자신의 기술만 사용하며, 플레이어 기술을 빌리는 훈련 UtilityAI 동작은 유지한다. 다오스는 기본AI에서도 UtilityPlanner를 사용한다.

PixelCampaignContent.CreateEnemies는 Content/PixelCampaign에 없는 새 캐릭터/기술만 생성한다. PixelArtImporter.Import는 alpha 읽기 분석 CSV를 사용해 Sprite Editor API로 경계·pivot·윤곽과 CharacterData의 아트 참조를 명시적으로 교체한다. 기존 캐릭터 능력치/스킬/성장 필드는 변경하지 않는다. 사용자 아트 참조 변경 후 자동 재실행하지 않는다. Tools/pixel-final-generation.json은 채택 원본/프롬프트 이력이며 실행 의존 경로가 아니다.

BoardView가 PixelBattlefield를 전장 수명에 맞춰 생성/삭제한다. PixelTerrain의16개 atlas구역을 타일 소재로 쓰고, PixelProps의16개 Sprite로 비보행칸/외곽을 장식한다. PixelBackdrops의2×3패널을 장별 원경으로 사용한다. 장식 충돌체는 제거하고 모든 소유 Material은 OnDestroy에서 정리한다. 카메라를 따라 도는 billboard와 수면 흔들림은 전투 RNG/좌표를 바꾸지 않는다. Point/무압축/밉맵 없음 설정이며 3D 회전/비정수 확대에서 정수 픽셀 배율을 보장하지 않는다.
