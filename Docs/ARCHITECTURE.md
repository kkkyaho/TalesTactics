# 아키텍처

## 공통 프레임과 전장 식생

`OrnatePanel`은 가져온9-slice Sprite로 공통 창의 금속 테두리를 표시한다. `OrnateButtonGraphic`은 버튼·초상화·게이지에 크기 독립적인 이중 프레임을 그리며 입력을 받지 않는다. `HudButtonAccent`가 포커스/선택/비활성 상태를 연결한다. 상태창은 큰 캐릭터 배경과 얇은 능력치 행을 사용하며 기존 데이터·탭·페이지와 입력 경로를 유지한다.

`PixelBattlefield`의 `PixelLivingTerrain` 부분 클래스는 논리 GridMap 바깥에 연속 외곽 지형·나무·관목을 더한다. 좌표 해시로 변화를 만들며 Unity Random을 소비하지 않는다. 장식에는 Collider가 없고 전방 나무는 카메라 방향에 따라 숨긴다. `TerrainDetailBillboard`는 식생의 명시적 atlas UV 메시를 공유한다. 생성 메시·머티리얼·Sprite는 전장 파괴 시 해제한다. 공통 Terrain 셰이더에 지면 이끼·미세 명암·나뭇잎 사이 빛·물 반짝임을 추가했다. [실행 화면과 검증](OrnatePresentation/README.md).

## 타이틀과 저장 진입

`BattleDirector.Start`는 카탈로그 검증 후 `BattleHud.ShowTitle`을 연다. `BattleTitleHud`는 승인한 배경·타이틀·3슬롯 목록·새 게임 확인을 기존 uGUI로 구성한다. 타이틀 중 일반 HUD/단축키를 차단하고 기존 EventSystem과 GamepadPointer의 선택·확인·취소를 사용한다. 준비 화면으로 진입할 때 타이틀을 닫는다.

`BattleTitleFlow.ReadTitleSlots`는 별도 `CampaignFile`로 읽기만 한다. 새 게임은 선택 슬롯 저장 성공 후 현재 캠페인을 교체하며, 이전 정상 저장을 `.bak`으로 유지한다. 불러오기는 보호된 파일을 차단하고 백업 복구·구 캠페인 호환 경로를 유지한다. 중단 재개는 기존 `ResumeBattle`의 체크포인트 검증·소비를 사용하고 실패하면 이전 프로필을 복원한다. 마지막 슬롯 설정 실패는 이미 성공한 캠페인 저장을 실패로 보고하지 않는다.

일반 빌드에는 `TitleScreenReview`가 포함되지 않는다. 검수 도구는 별도 저장 폴더와 실제 엔진 화면을 사용한다. [타이틀 적용과 검증](TitleScreen/README.md).

## 현재 플레이 가이드

`BattleHelpHud`는 기존 0–7번 페이지와 조작 안내 바로가기를 유지하면서 캠페인·대화·장비·저장 안내를 추가한 12개 주제를 제공한다. `BattleSystemHud`의 임무 변형 설명과 `BattleIntermissionGrowthHud`의 확장 캠페인 성장 안내도 현재 규칙을 따른다. 게임 규칙·저장 형식·콘텐츠 에셋은 변경하지 않는다.

`HelpGuideReview`는 Editor/검수 define에서만 포함하며 15개 안내 화면의 해상도·글자 크기·폰트·잘림·버튼 접근·배경 입력 차단과 캠페인 불변을 검사한다. Windows 검수는 GUID 저장 경로를 격리하며, 고정 Game View 검수는 `HelpGuideBuild.EditorReview`로 시작한다. 일반 실행본에는 검수 타입이 없다. [검증 근거와 한계](HelpGuide/README.md).

## 전용무기 이미지와 장착 외형

`EquipmentData.Icon`은 장비 정비·편성 슬롯·상태창의 고유 이미지를 제공한다. `EquipmentAppearance`는 소유자/무기 ID 및 장착 호환성을 확인한 뒤 원본 Sprite를 전용무기 Sprite로 교체한다. 기존 프레임 순서·모션 시간·카메라 방향·스킬 준비/타격 구간은 그대로 사용한다. `CharacterMotion`, 전장 초기 표시, 이동 도착 미리보기, 편성 모델, 상태창의 회전 모델과 장비 초안 미리보기가 같은 해석 경로를 사용한다. 해제/일반 장비/잘못된 소유자/미등록 프레임은 원래 외형으로 돌아간다.

기존 캐릭터 그림에는 무기가 포함되어 있어 전용무기가 반영된 전체 프레임51장/1,404개를 별도 제작했다. 원본 그림·클립은 수정하지 않는다. `Tools/MeasureSignatureArt.py`는 투명 PNG를 읽기만 하고 연결된 그림 영역·기존 발 기준점/PPU를 측정한다. `SignatureWeaponImporter`는 Sprite Editor Data Provider로 프레임과 외곽선, 아이콘10개, 장비 연결을 명시적으로 가져온다. `.meta` 수동 수정이나 `DemoContent.Create` 실행은 필요 없다. 다시 가져올 때 기존 Sprite ID를 유지한다.

별도 외형 저장값은 없다. 현재 `EquipmentLoadout`과 기존 장비 ID 저장을 사용하므로 초안 취소·저장 실패·재접속·중단 전투에서 장비가 외형을 결정한다. `CharacterModelImage`는 스프라이트 외곽선 메시를 그리며 무기가 길어져도 원본 몸체 크기를 기준으로 표시한다. `SignatureWeaponReview`는 일반 빌드에 제외되는 격리 저장/화면 검수 도구다.

## 인터미션과 초상화 대화

`BattleIntermissionHub`는 기존 준비 패널을 거점으로 사용하며 `ShowDeployment`는 상세 편성 화면을 유지한다. 시작·재시작·슬롯 전환 시 거점으로 복귀한다. `BattleIntermissionEquipmentHud`는 기존 `EquipmentLoadout` 초안/저장 경로를 사용하면서 변경 수치를 우선 표시하고 전체 비교는 기존 모달 계층으로 연다.

`CampaignStory`는 대사의 진행·완료 콜백을 보존하고 읽은 대사의 복사본과 현재 이야기의 장을 제공한다. `BattleStoryHud`가 영웅 초상화, 자동 진행, 읽은 대화 기록과 건너뛰기 확인을 맡는다. 대화 중 준비 메뉴를 CanvasGroup으로 차단하고 확인창에서는 대화 조작도 차단한다. 자동 진행은 비활성 창에서 정지하고 기록/확인/종료 시 해제된다. 검수 코드 `IntermissionUxReview`는 일반 빌드에 포함하지 않는다.

## 전투 표시 계층

`ImpactChoreography`는 기존 `ImpactParticles` 풀에 준비/접촉 연출을 추가한다. 준비 효과는 움직이는 `CharacterMotion` 위치를 따르며, 불꽃 흐름·다중 화염·연기와 공격자 중심의 세 겹 검격 리본을 그린다. 준비 단계는 피해를 처리하지 않는다. `BoardActionPresentation`의 준비/복귀 시간과 `CharacterMotion`의 가속/접촉 정지/복귀를 함께 사용한다. `VfxQualityReview`는 별도 검수 빌드에서 실제 기술 실행을 촬영하며 일반 빌드에서는 제외한다. [범위와 근거](VfxQuality/README.md).

`FacingArrowGraphic`은 베이지색 화살표 윗면과 화면 아래쪽으로 돌출된 얇은 측면을 UI 메시로 그린다. 방향은 메시 좌표에서 회전하여 측면 방향을 일정하게 유지한다. 버튼 Image는 완전 투명이며 `cullTransparentMesh=false`와 raycast를 유지한다. 사각 배경 없이 기존 클릭 영역을 보존한다.

`ActionExperience.Grant`는 변경 전후 레벨/EXP 스냅샷을 반환한다. `CombatFeedbackHud`가 순서대로 초상화 EXP 게이지를 재생하고 HP 숫자를 화면 Canvas에 표시한다. 모달에서는 숨기고 전장 재설정 시 큐를 비운다. 피해 표시 중 상세 HP 글자는 잠시 숨기며 막대는 유지한다.

`BattleFacingHud`는 `BoardView.HeadWorld`를 화면 좌표로 투영해 작은 4방향 UI를 그린다. 기존 지면 화살표는 제거했으며 실제 선택/취소 상태 경로를 유지한다. `ImpactPool`은 전장별 최대16개의 동적 메시 효과를 재사용하고 `Impact.shader`가 생성된 화염/연기 아틀라스와 속성/무기 도형을 렌더링한다. 효과는 전투 난수를 소비하지 않는다. `PlayerPreferences.ReducedEffects`는 표시량만 줄인다. `CombatMotionProfile`과 `CharacterMotion`은 기존 방향별 프레임의 전진·반동·기울기를 강화한다. [상세와 검증](CombatPresentation/README.md).

`CombatPresentationReview`는 검수 define/Editor에서만 컴파일하며 GUID 전용 저장 경로와 엔진 캡처를 사용한다. 일반 실행본에는 포함하지 않는다.

현재 활성 캠페인은 메인20화/개인30단계다. BattleDirector.CanPlayCampaignStage가 출전·대사·중단 재개의 구 캠페인 진입을 막고, RestorePreparation은 구 장 선택을 새 메인1화로 바꾼다. 저장된 장 인덱스·성장·장비·기존 완료 이력 및 호환 코어는 유지한다. 레거시 PlayMode 검사는 복제된 비활성 확장 카탈로그로 분리한다. [범위](PresentationProposal/README.md).

## 경계를 잇는 의지 확장

`CampaignStages`는 기존 인덱스0–5를 보존하고 `CrossoverCampaign` 메인6–25, `PersonalScenarios` 개인26–55를 라우팅한다. 메인 시작/순차 합류와 개인 해금은 별도 완료 플래그를 사용한다. `CampaignPromotion.StorySatisfied`는 기존2장 또는 새6화 완료를 공통 승급 조건으로 확인한다.

`CrossoverSkillRules`의60기술 정의와 실제 Content를 비교 검사한다. `CrossoverBossTactics`는 고정 예고/빈틈/단계/소환/장치 복구를, `CrossoverTerrain`은 지속 위험과 지연 붕괴를, `CrossoverFronts`는 서·동 부대의 예약/전환을 처리한다. 예약 유닛은 `CanTakeTurn`과 대상 선정에서 제외되며 HP는 유지한다. `BossTimeStop`의 저항은 실제 수행한 행동에 따라 감소한다.

`CrossoverBattleState`가 구조 신호·선택·기록 범위·시련·위험·파괴 장치 순서를 보관한다. `CrossoverTrials`와 `SignatureWeaponRules`가 고유 조건/소유자 제한/효과를 정의한다. 중단V11·시작V6·되돌리기 복사본은 이 상태를 함께 검증하며 옛 중단 전투의 당시 규칙을 유지한다. 캠페인 보상은 완료/선택 플래그와 함께 저장 실패 시 롤백한다.

`BattlePersonalScenarios`, `BoardCrossoverView`, `CrossoverCivilianView`는 이야기/목표/구조된 비전투 행렬을 표현한다. 카탈로그의 `CrossoverVariants`는 기존 다오스/골렘을 바꾸지 않는 확장용 복사본과 인간형 히아데스이고, `Civilians`는 네 종류의 방향 스프라이트다. 생산 아트1708rect의 출처와 선택 규칙은 `Tools/MeasureCrossoverArt.py`, 연결은 `CrossoverArtImporter`에 있다. 공개된 카탈로그에는 생성/설치 도구를 무조건 재실행하지 않는다.

`CrossoverBalanceAudit`는 실제 명령의 독립 전투 시뮬레이션이다. `CrossoverPlayerReview`는 별도 저장/연출 fixture이며 수동 플레이나 밸런스 검사가 아니다. 최종 근거와 한계는 [CrossoverStatus](CrossoverStatus/README.md)를 따른다.

## 그래픽·UI/UX 표현 계층

`HudButtonAccent`는 주 행동/탭/보조 버튼의 배경·테두리와 비입력 밑줄을 관리한다. 포커스는 금색, 선택과 마우스 호버는 청록색으로 표시하며 실제 Button 콜백·입력 모달 차단 경로를 유지한다. 상단 아군 초상은 기존 팀 턴 선택 API를 사용하고 이동/행동 아이콘 또는 완료 체크를 보여 준다.

`UnitHealthLayout`는 각 `UnitHealthDisplay.LateUpdate`가 산출한 화면 좌표를 받은 뒤 선택/호버 우선순위로 겹침을 검사하여 Y 위치를 보정한다. 상세 이름/수치 영역을 포함한 크기를 사용하며 모델 위치·피격 판정에는 관여하지 않는다. `SkillVisualLanguage`는 기술 데이터에서 모양·속성·사거리 표시를 공통 계산한다.

`MissionMapPreview`는 실제 캠페인 맵을 별도로 생성하여 CanvasRenderer 메시로 그린다. 도착/호위 목표는 금색이며 플레이 중 GridMap이나 난수를 변경하지 않는다. `BattlePurchasePreview`는 저장 객체를 생성·수정하는 CampaignSave.Get 대신 기존 진척 또는 임시 기본값을 읽고, 재고를 연결하지 않은 EquipmentLoadout 초안으로 구매 예정 장비의 능력치를 비교한다. 실제 거래와 저장은 기존 CampaignInventory 경로를 사용한다.

`Panorama.shader`는 원경의 대비·채도·대기색을 조정하며, 바닥 대비는 Terrain.shader의 `_DetailContrast`, 캐릭터 환경색은 UnitTone.shader의 `_EnvironmentTint` 속성으로 분리한다. 기존 접지 그림자와 모션 클립을 유지한다. 이번 변경은 저장 스키마·전투 규칙·Content 재생성을 포함하지 않는다. [실제 화면과 검증](VisualUx/README.md).


## 전장 지형 아트

`BoardView`는 기존 타일 좌표·높이·충돌체를 만들고 `PixelBattlefield.Complete`에서 장식 메시를 마무리한다. 연속 바닥은 월드 XZ 좌표로 장별 텍스처를 샘플링하며 물 타일에는 별도의 흐름 재질을 사용한다. `TerrainGeometry`는 외벽 구조·계단·낙수·협곡 바위를 재질별로 합친다. 외벽의 조각 아치는 연속 UV로 연결된 회화 텍스처이며 기둥과 천문기구 등은 카메라를 향하는 2.5D 스프라이트다. 장식에는 Collider가 없다.

`Terrain.shader`는 방향 명암·깊이 음영·최대24개의 접지 그림자를 계산한다. 실시간 그림자 맵이나 GI를 추가한 것은 아니다. 원경은 카메라 뒤의 화면 비율 보존 평면으로, 회전 가능한 3D 배경과 구별한다. 생성한 재질·메시·스프라이트는 보드 해제 시 함께 파괴한다. `Resources/TalesTactics/TerrainArt`의 원본 PNG와 임포트 설정을 사용하며 전투 RNG·이동 비용·저장 데이터에는 관여하지 않는다. [아트와 실제 검증](TerrainArt/README.md).

## 확인창과 메뉴 문구

되돌리기·재도전·아군 턴 종료 확인은 560×270 기준의 작은 창과 취소/실행 버튼으로 표시한다. 기본 화면에는 실행 대상·남은 횟수·진행 포기처럼 결정에 필요한 정보만 남긴다. BattleSystemHud의 저장 도움말(page6)과 전투 규칙(page7)에 자세한 규칙을 분리하며, 규칙 조회 후 돌아가도 설정 초안을 유지한다. 기존 버튼 오브젝트 이름/클릭 연결은 유지하고 CaptionLastButton으로 표시 문구만 줄인다. 시스템/능력창은 다른 화면을 열 때 크기를 다시 설정해 작은 확인창 크기가 상세창으로 전파되지 않도록 한다.

## 반응 공격과 행동 기록

BattleReactions는 SkillResolver의 선택형 단일 대상 추가 피해를 처리한다. 본 공격 뒤 인접 지원50%, 살아 있는 대상 반격75% 순이며 순환 반응은 없다. CounterUsed/SupportUsed는 UnitRuntime.BeginTurn에서 초기화하고 CheckpointUnit과 BattleProjection이 보존한다. 기본 꺼짐의 PlayerPreferences.Reactions는 새 전투에 전달한다. UI 예측과 실행이 같은 ResolveReactions를 사용하고 PrimaryHealth 및 ReactionStrike의 HP 스냅샷으로 연출 숫자를 중복 없이 표시한다.

BattleRewind는 캠페인 아군 명령 직전 체크포인트를 최근12개까지 보관한다. 이동/이동 취소/기술/방어/대기/일괄 턴 종료를 기록하고 최대3회 복구한다. 다음 적 페이즈와 증원까지 복원할 수 있다. BattleDirector는 연출·적 턴·승리 뒤 입력을 막고 복원 검증 뒤에만 Session을 교체한다. 패배 뒤에는 보상이 없으므로 복원이 가능하다.

BattleCheckpoint V8은 반응 규칙·소모 플래그·난수 소비 수·되감기 횟수·RewindFrame 목록을, BattleOpening V3은 출전 당시 반응 규칙을 보존한다. 각 RewindFrame은 History가 비어 있는 체크포인트 JSON 문자열이므로 Unity 직렬화 깊이 제한을 피한다. HasOpening=false일 때 Unity가 생성하는 빈 인라인 Opening 객체는 무시한다. 목록/프레임 크기·중첩·캠페인/편성/규칙 일치를 검증한다. V1–V7 중단/V1–V2 시작 기록은 기존 반응 비활성 규칙으로 읽는다. 실제 전투 RNG는 기존 System.Random 알고리즘과 시드를 유지하며 소비 횟수를 복원하고, 예측은 별도 그래프에서 확률 효과를 제외해 실전 난수를 소비하지 않는다.

## 난이도와 임무 변형

PlayerPreferences의 Difficulty(표준=0/여유/도전)와 MissionEvents(기본 꺼짐)는 다음 출전에만 반영한다. DifficultyRules는 캠페인 적의 시작 레벨에 −2/0/+2를 적용하고1–50 범위를 유지한다. 아군 성장·골드·EXP·장비 보상은 변경하지 않는다. 훈련은 기존 레벨과 규칙을 유지한다.

MissionOptions는 BattleSession의 선택형 임무 상태를 관리한다. 자유 선택 턴의3장은 거점에 아군이 있는 상태로 아군 페이즈를2회 연속 끝내면 승리하며, 비어 있는 페이즈 종료에는 진행을 초기화한다.3·5장은3번째 아군 페이즈 시작에2명 증원한다. 지정 적 출발점에 가까운 비점유 이동 가능 칸을 결정적으로 선택하고, 둘 다 배치할 공간이 없으면 보류한다. 증원은 그 다음 적군 페이즈부터 행동한다. BoardView.Sync는 새 유닛의 시각 오브젝트만 추가해 카메라와 기존 맵을 유지한다.

BattleCheckpoint V7에 난이도·변형 여부·증원 발생·점령 진행을 보관한다. 기존 V1–V6은 표준/변형 없음으로 복원한다. BattleOpening V2는 출전 당시 옵션을 보관하고 재도전 시 점령·증원을 초기 상태로 되돌린다. V1 시작 기록도 계속 읽는다. 설정과 임무를 조회하는 동작은 게임 저장을 바꾸지 않는다.

## 컴파일 경고 관리

객체 검색은 정렬 인자가 없는 FindObjectsByType API를 사용한다. 개발 검수 도구는 UNITY_EDITOR 또는 DEBUG에서만 컴파일하며 일반 배포의 검수 코드 제외를 빌드 산출물로 확인한다. Target/InspectedUnit/HoveredUnit은 전투 중 임시 참조이므로 NonSerialized로 명시한다. 영구 저장은 기존 CampaignFile/BattleCheckpoint가 맡는다. Pipeline 패키지의 플레이어 기능 비활성화 안내는 프로젝트 코드 경고와 별도로 기록하며, 경고 제거 목적으로 원격 제어를 켜거나 패키지 캐시를 수정하지 않는다.

## 간결한 임무·전투 예측

BattleMissionHud는 오른쪽 드로어에서 목표/적 정보 탭을 전환한다. 기본 목표는 승리·패배·진행 상황 또는 호위 방벽만 표시하고 지형과 세부 규칙은 펼쳐 본다. 적 초상화 선택은 조회 상태만 바꾸며 실제 전투 선택·턴·자원을 변경하지 않는다. 전투 예고가 있으면 일반 방벽 설명보다 우선 표시한다.

BattleTacticalHud는 기존 BattleForecast의 복제 계산 결과를 하단 오른쪽 카드로 표시한다. ForecastRow의 BeforeWard/AfterWard, DirectDamage/Immune, ImportantEffects는 계산 결과를 보여주기 위한 정보이며 실제 피해·상태 계산을 대체하지 않는다. 확률·상태·위치 변경은 기본 카드에 유지하고 전술 보정과 전체 계산 설명은 상세창에 둔다. HP 막대는 최대 HP 대비 잔존분과 증감분을 분리한다. 범위 대상 순환은 카드의 조회 인덱스만 바꾸며 조준 위치를 변경하지 않는다. 임무창과 예측 상세창이 열리면 뒤의 명령 카드를 숨기고 닫을 때 복원한다.

## 전신 모델 편성 화면

BattleModelDeploymentHud는 기존 BattleHud의 준비 렌더러를 확장한다. 모델 조회와 출전 토글은 별도 버튼이며 기존 Deployment 인덱스를 사용한다. 필터/정렬은 표시 인덱스 배열만 바꾸고 카탈로그·편성을 재정렬하지 않는다. 최대 인원은 Catalog.Rules.MaxDeployment로 검사한다. 임무 목록은 별도 준비 화면으로 옮겼으며 장비·성장·상점·모달·출전은 기존 경로로 연결한다.

FormationProgress는 없는 성장 기록을 임시 기본값으로 읽는다. 따라서 준비 화면 조회가 CampaignSave.Characters를 늘리지 않는다. 상세 능력치는 EquipmentLoadout.Preview와 같은 장비·특성을 적용한다. 새 초상화 아틀라스는 캐릭터 ID로 UV를 선택하며 기존 CharacterData.Portrait/전투 스프라이트를 덮어쓰지 않는다. FormationRing은 uGUI 메시로 그리는 고리이고 색상 외에도 출전 문구와 현재 선택 화살표로 상태를 구분한다. 배치·검수 기록은 [ModelFormation](ModelFormation/README.md)을 따른다.

## 로컬 배포와 업데이트

Tools/PackageRelease.py는 검증된 일반 Windows 빌드와 이전 빌드 보고서/실행 파일·Runtime DLL 해시를 확인하고 전체 배포 파일 해시를 manifest에 기록한다. 게임 소스 변경 상태에서는 패키징하지 않는다. 실행 파일의 제품 버전과 별도로 배포 ZIP 버전을 관리하며 같은 번호를 덮어쓰지 않는다. Unity 백업/심볼/로그와 사용자 저장을 배포에서 제외하고 기존 게임 폴더를 변경하지 않는다.

포함된 Verify-Package.ps1은 파일 목록·크기·SHA256 및 예상치 못한 파일을 검사한다. Backup-Saves.ps1은 게임 종료 후3슬롯/설정과 백업·이관/손상 보관본만 새 날짜/GUID 폴더로 복사한다. 원본 삭제·자동 복원·원격 업로드는 하지 않는다. 업데이트는 새 폴더에 전체 ZIP을 풀어 공유 persistentDataPath를 사용하는 방식이며, 하위 버전 복귀는 별도 저장 백업이 필요하다. 소비자에게 Python 설치는 필요하지 않다. 상세 범위는 Docs/Distribution/README.md를 따른다.

## 전투 피드백과 효과음 중첩

BoardView.Number는 숫자/배경을 같은 루트에 생성해0.65초 후 함께 정리한다. 카메라의 현재 up/right로 표시 위치를 매 프레임 계산하고 MaterialPropertyBlock으로 배경 투명도를 적용한다. 전투 수치·판정은 변경하지 않는다.

PrepareSkill의 기술명은 밝은 글자/어두운 배경과 별도의 속성색 준비 고리를 사용한다. 생성 때 preferred width를 한 번 측정해 최대5.6월드 단위 안에 맞추며 표시 중 AutoSize를 사용하지 않는다. 배경은 기술명의 자식이므로 발동/Restart 시 함께 제거된다.

BattleAudio의 음악 소스와 효과음 소스는 분리되어 있다. 효과음은45ms 동일ID 중복 방지, 최대4개 동시 재생, 새 음성의1/√n 게인을 적용한다. Time.unscaledTime으로 클립 수명을 추적하므로 전투 배속과 별개이며 StopAll에서 추적 상태를 비운다. 현재 효과음 소스의 pitch=1/비루프 재생을 전제로 한다. 효과음 pitch나 AudioListener.pause를 도입하면 이 수명 추적도 함께 변경해야 한다.

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

BoardView의 rangeBorders는 타일별 LineRenderer를 필요할 때 생성해 전장 안에서 재사용한다. 경계는 실제 타일 Renderer.bounds 위에 놓고 공유 HighlightMaterial의 _BaseColor/_Color를 PropertyBlock으로 지정한다. ClearHighlights는 선을 숨기고 경로를 지우며, ResetBoard는 전장과 사전을 정리한다. 타일 색상과 함께 쓰므로 바닥 그림을 유지하면서 경계를 식별할 수 있다. 이동 가능성/사거리/효과 범위는 기존 Grid와 SkillResolver 결과를 그대로 사용한다.

## 2026-10-04 — 단계별 중간 동작

CharacterPoses.AttackMotion은 준비/기존 타격/후속/복귀 4프레임이다. Skill은 준비/기존 Cast/Attack/후속/복귀 5프레임, Ultimate는 준비/기존 전용 charge/release/복귀 4프레임이다. DirectionalSpriteClip.ReleaseFrame은 준비와 발동 구간을 나누며 SamplePhase는 각 구간의 시간 비율을 사용한다. CharacterMotion.BeginSkill과 ReleaseSkill은 기존 전투 코루틴의 준비/회복 시간을 받아 발동 전에 타격 그림이 노출되는 것을 막고 회복 끝에 대기로 돌아간다. Set 호출은 현재 연출을 중단한다. Walk/Dead는 기존 FPS 기반 Sample을 유지한다.

AnimationPolishImporter는 BattleCatalog의 영웅/적20종에만 보충 시트를 연결한다. 기존 핵심 그림과 전투 데이터는 보존하며, 재실행 시 이미 확장된 Ultimate에서도 기존 핵심2장을 유지한다. AssetPostprocessor나 자동 생성 메뉴는 추가하지 않는다. Tools/MeasureAnimationPolish.py는 원본 이미지 픽셀을 수정하지 않고 경계/윤곽 CSV와 감사 JSON을 만든다. 검수 갤러리 스크립트는 Play Mode 임시 오브젝트만 만든다.

## 2026-10-04 — 전투 HUD 공간 분리

BattleCompactHud는 기존 BattleHud의 전투 레이아웃을 보완하는 partial이다. 좌하단 Unit 패널은344×174 Canvas 단위의 요약, 상세 정보는 별도 UnitDetails 패널이다. 상세창은 CanvasGroup.interactable로 기존 버튼을 잠그고 BattleDirector/GamepadPointer의 전장·카메라 입력을 차단한다. 닫기/취소는 전투 State와 HP/MP를 변경하지 않는다. Refresh 또는 Restart에서 상세창을 닫고 원래 상호작용을 복원한다.

CommandState의 세로 메뉴는 활성 캐릭터의 화면 좌표 바깥쪽에 제한 배치한다. Skill 목록/상세는 오른쪽 패널로 전장 viewport와 분리하고 TargetSelection은 하단으로 이동한다. 기존 Button 오브젝트 이름을 유지해 게임패드/자동 검수 경로를 보존하며 표시 문구만 간결하게 바꾼다. 기존 데이터·Scene·출전/상점/장비 구조는 재생성하지 않는다.

## 전장 가독성과 출전 준비 UI

BoardReadability는 행동자/대상/이동 목적지의 테두리와 집중 보기 상태를 관리한다. BoardView.FitBattlefield는 각 타일의 높이/유닛 여유를 포함한 카메라 공간 경계를 투영한다. ResetCamera는 집중 상태를 해제하고 ResetBoard는 지연 파괴 전에 런타임 참조를 비운다. BattleTargetNavigation은 기존 Resolver의 InRange/Targets 결과로 유효한 위치를 순환하고 실행 판정은 기존 Confirm 경로에 남긴다.

BattlePreparationHud는 기존 header/left/commands/footer를 재사용하고 준비 화면에만 중앙 PreparationContent 패널을 표시한다. 카드 선택과 Deployment 변경은 분리한다. EquipmentLoadout의 분리된 초안을 슬롯/후보 목록으로 수정하며 TrySave로 확정한다. BattleMarketHud는 구매/매각 공통 목록과 종류 필터·선택 상세·페이지를 렌더링하고 기존 CampaignInventory 거래/보호 검사를 재사용한다. 구매 장비 장착은 호환 캐릭터를 선택한 뒤 장비 초안 비교로 연결하며 저장 전 원본 장비를 변경하지 않는다.

## 입문 연습과 도움말

BattleTutorial은 캠페인과 분리된 Lv1 BattleSession을 만든다. PracticeTurns가 크레스→민트만 순환시키고 Session.Advance/EndTurn으로 실제 턴 시작·종료를 처리한다. PracticeObjective는 자동 승패를 막으며 네 단계의 실제 이동/Resolver 실행/방향 선택으로 완료한다. 허용 명령·타일·기술·대상을 제한하고 취소 시 단계를 유지한다. 캠페인 저장·보상은 호출하지 않으며 Restart로 기존 편성과 옵션을 복원한다.

BattleHelpHud는 준비/전투에서 호출하는 8페이지 모달이다. InputModalOpen을 상세창/도움말 공통 입력 차단 조건으로 사용하며 중앙 편성 영역도 잠근다. 도움말은 선택 상태를 바꾸지 않는다. 전투 수치와 궁극기 조건은 Catalog를 읽고 별도 복사본으로 유지하지 않는다. CampaignPlayerReview의 chapter1은 격리 저장에서 입문 연습을 완료한 후 기존 6장 검증을 진행한다.

## 저장 슬롯·전투 중단·설정

BattlePersistence는 Director별 CampaignProfiles/PreferenceFile을 소유한다. 기본 슬롯1은 기존 campaign.json이며 슬롯2/3은 campaign-slot2/3.json이다. 테스트·개발 검수는 ConfigureStorage로 별도 루트를 사용한다. 기존 PersistCampaign 콜백은 SaveProgress에 연결해 자동 저장 시각/준비 편성을 기록하고 오래된 중단 기록을 해제한다. 실패 시 추가 메타데이터를 복원하고 기존 거래/보상 실패 복원 경로를 유지한다.

BattleCheckpoint는 캠페인 아군 CommandState에서만 만든다. 콘텐츠 ID, 단계/유닛 수, 위치/점유, 수치, 상태, 큐 인덱스를 검증해 임시 BattleSession을 구성한 뒤 저장 성공 후 Director에 연결한다. SPD는 남은 queue/Round, CT는 유닛별 charge를 내보내고 복원한다. Active.BeginTurn을 다시 호출하지 않아 행동 사용 여부/쿨다운을 보존한다. Random.State는 검증/소비 저장이 성공한 뒤 복원한다. CampaignSave.HasSuspendedBattle은 Unity JsonUtility가 null인 인라인 객체를 재생성하는 동작과 실제 중단 기록을 구분한다. 이전 V2 파일의 누락된 필드는 기본값으로 읽고 미래 중단 버전은 원본 보호 상태로 처리한다.

시스템 메뉴는 기존 Canvas 위 별도 모달이며 InputModalOpen을 재사용한다. PlayerPreferences 초안은 성공적으로 저장한 후 복사해 적용하고 추가 편집이 적용된 설정을 바꾸지 않게 한다. 화면 설정은 Windows에서만 Screen.SetResolution으로 적용하며 음악/효과음 소스와 패드 커서 배율은 즉시 반영한다. CT/AI는 진행 중 Session을 재구성하지 않는다. PreferenceFile은 미지원/손상 파일을 덮어쓰지 않는다.

## 전투 속도와 결과 흐름

BattleFlow는 캐릭터 ID별 마지막 SkillData와 결과 직전 성장/재고/골드 기준을 보관한다. TargetSelection 취소는 일반 공격만 CommandState로, 기술은 SkillDetailsState로 복귀한다. 목록은 GamepadPointer.FocusButton으로 선택을 복원하되 실행하지 않는다.

EnemyTurn 시작 시 PlayerPreferences의 속도/간략 설정을 읽는다. 이동 보간·준비/복귀 대기만 조정하고 SkillResolver.Execute와 턴 종료는 모든 모드에서 동일하게 한 번 실행한다. 전역 Time.timeScale은 변경하지 않는다. 기존 설정 파일의 누락 필드는 정상 속도/전체 연출 기본값이다.

BattleResultHud는 BattleEndState 전용 패널이며 시스템/도움말 모달의 입력 차단에 포함된다. 보상 성공 후 실제 캠페인 차이로 결과를 표시하고 실패 시 성장 행을 감춘다. 기존 pendingReward와 저장 롤백을 사용해 재시도 추첨/중복 지급을 막는다. 다음 장은 준비 화면으로, 재도전은 같은 편성/장으로 BeginBattle을 호출해 이야기를 생략한다.

## 임무 정보와 적 역할

MissionBriefing은 Session의 실제 목표/생존 적 수를 읽는다. BattleMissionHud는 별도 모달이며 출전 전에는 선택 장의 CampaignEnemies를, 진행 중에는 Session.Units를 읽어 같은 적 구성을 표시한다. InputModalOpen 및 키보드/패드 취소와 시스템·도움말 전환에 연결하고 배경 CanvasGroup 상호작용을 차단한다. 세이브를 쓰거나 AI 예측을 실행하지 않는다.

EnemyRoles는 콘텐츠 ID를 전열/돌격/사격/마법/보스로 분류한다. EnemyPlanner와 UtilityPlanner의 점수에 캠페인 적 전용 선호를 더한다. 사격/마법은 가장 가까운 아군과 기본 사거리(최대3칸) 안에서 거리를 확보하고 돌격은 공격 가능한 부상자에 추가 점수를 준다. 실제 이동/사거리/비용/효과 판정은 GridMap/SkillResolver에 남겨 두며 훈련·아군 플래너에는 역할 보정을 적용하지 않는다. 다오스의 기존 Utility 선택은 유지한다.

## 화면 비율과 메뉴 키보드 선택

BattleHud.Initialize는 ScaleWithScreenSize CanvasScaler에 Expand를 적용해 논리 영역을 기준1440×900 이상으로 유지한다. 고정 높이의 편성 카드/상점/하단 출전 영역이21:9에서 서로 침범하던 문제를 해결하며, 임의 ConstantPixelSize 설정은 바꾸지 않는다. GamepadPointer는 메뉴/모달/이야기/결과에서만 Tab 순환을 처리하고 기존 EventSystem Submit을 그대로 사용한다. 활성 Button.IsInteractable 검사로 모달 뒤 CanvasGroup을 제외한다. 선택 Outline의 원래 색과 두께는 선택 변경/비활성화 때 복원한다. 전투 대상 Tab 처리는 BattleDirector에 남아 있다.

## 아이콘 전투 HUD

BattleIconHud는 전투 상단 초상 순서, 4행 단위 사용 가능 기술 목록, 선택 기술 요약과 상세 창을 담당한다. TacticalIcon은 CanvasRenderer를 가진 MaskableGraphic으로 직접 도형을 그리며 클릭을 가로채지 않는다. 사용 조건과 비용은 SkillResolver가 판정한다. 페이지 전환은 ActionSelectionState로 돌아가 이전 페이지 기술의 실행 버튼을 남기지 않는다. 전체 설명은 기존 입력 차단을 사용하는 상세 모달에서 제공한다.

BattleCompactHud는 유닛 옆 세로 5줄 명령, 작은 캐릭터 요약, 대상 가까이의 결과 패널과 화면 경계 처리를 담당한다. BoardView는 전체 화면 카메라로 배경을 렌더링하되 BattlefieldViewport를 타일 배치의 안전 영역으로 사용한다. 카메라 크기와 중심 오프셋에 안전 영역 비율을 반영하므로 UI 때문에 검은 띠가 생기지 않는다. 기존 전투 상태/저장 형식/규칙 데이터는 유지한다.

## 간결한 방향 UI와 아군 경로 통과

BattleFacingHud는 월드 Facing을 CharacterMotion.ViewFacing으로 화면 방향에 대응시킨다. 캐릭터 투영 위치 주위에20×20 꺾쇠와40×40 투명 클릭 영역을 놓고 매 프레임 카메라와 화면 경계에 맞춘다. 기존 ChooseFacing 경로로 방향을 확정하고 턴을 종료한다. Commands 패널 배경/레이캐스트는 방향 선택 중에만 숨기며 상태 전환 시 복구한다.

GridMap.Reachable은 내부 탐색에 CanTraverse를 사용해 같은 팀 점유 칸을 통과한다. 전체 탐색 후 점유된 다른 유닛의 칸을 도착 후보에서 제외하며 parents에는 통과 경로를 남긴다. CanEnter와 Place의 빈 칸 제약은 유지해 강제 이동 및 최종 점유가 겹치지 않는다. 애니메이션의 중간 통과는 시각 위치만 움직인다.

## 기술 목록에서 바로 대상 선택

BattleIconHud.VisibleSkills는 CanUse(현재 유닛, 기술, IsFollowup)와 입문 연습 허용 조건으로 목록을 필터링한다. 페이지 범위를 보정하고 빈 목록에서도 취소를 제공한다. 기술 행은 SelectSkill을 직접 호출하며 비용은 실행 시에만 사용한다. PointerEnter/Select는 짧은 설명만 갱신하고 상태를 전환하지 않는다. 상세 모달은 필요할 때 따로 연다.

SelectSkill은 마지막 기술을 기억하고 이전 Target을 지운 후 TargetSelectionState로 진입한다. CancelTarget은 일반 공격이면 CommandState, 기술이면 ActionSelectionState로 돌아간다. SkillDetailsState는 별도 상세 검수 경로로 유지되지만 일반 사용 흐름에 포함되지 않는다.

## 캐릭터 옆 컨텍스트와 외부 클릭

BattleCompactHud는 기술 목록과 대상 확인창에 PlaceBesideUnits를 적용하며 기준 위치는 항상 Session.Active.Position이다. 피해 미리보기는 Commands 안으로 합치고 별도 Message 대상 패널은 숨긴다. 일반 대상 전환은 키보드 Tab/Shift+Tab과 기존 패드 트리거 경로를 사용한다.

BattleDirector.HandleBattleClick은 마우스와 GamepadPointer.Submit의 전장 클릭을 공통 처리한다. UI 레이캐스트/모달/이야기/행동 연출/결과를 먼저 제외하고, 유효 이동 경로나 타겟이면 선택을 수행한다. 나머지는 State.Cancel로 한 단계만 되돌린다. CommandState에서는 CanUndoMove일 때에만 Undo하며 튜토리얼은 기존 제한을 유지한다. 메뉴 선택과 이동 취소를 한 클릭으로 연쇄 실행하지 않는다.

## 대상 클릭 즉시 발동

HandleBattleClick은 유효한 대상에 SelectTarget 후 Confirm을 즉시 호출한다. Update는 마우스 이동 시 유효 대상의 미리보기만 갱신한다. Tab/Shift+Tab은 기존 CycleTarget을 유지하며 Enter/숫자패드 Enter로 확정한다. 행동 상태가 즉시 ActionExecutionState로 바뀌므로 반복 클릭/확인은 중복 사용하지 않는다. 대상 컨텍스트는 실행 버튼 대신 짧은 조작 안내와 미리보기만 표시한다.

## 시스템·전술 UI 확장

BattleProjection은 GridMap·유닛·상태·쿨다운·전술 필드의 독립 복제본이다. BattleForecast는 복제본의 실제 SkillResolver를 forecast 모드로 실행해 자원/HP/상태/이동/보호 사용 변화를 비교한다. 확률 효과는 실행하거나 난수를 소비하지 않고 별도 확률 설명으로 보여 준다. ThreatMap도 복제본에서 다음 턴 이동·유효 적대 기술 범위를 계산하며 확정 AI 선택과 구분한다. 준비된 공격은 고정 IntentAim만 표시한다.

EnemyTactics.Plan은 순수 조회이고 Commit이 예고 상태를 변경한다. Prepare → 고정 위치 발동 → 휴식은 TurnsStarted/IntentTurn/IntentPhase로 구분하며 BeginTurn에서 기절·수면 취소와 휴식 해제를 처리한다. 캠페인의 역할 기술 4개는 선택적 Catalog 필드라 기존 테스트·훈련 카탈로그에도 호환된다. Utility 옵션은 역할 기술 외의 고유 기술까지 고려한다.

CampaignMissions가 장별 목표를 정하고 TacticalDevelopment가 특성·합류 훈련·기술 숙련·궁극기 체험을 관리한다. 특성의 능력치 계산은 UnitRuntime과 EquipmentLoadout이 공유하며 비용·회복·보호는 SkillResolver에 모은다. 저장 실패 시 변경을 되돌린다. BattleCheckpoint V2는 전술 필드·생존 경과·기존 임무 여부를 보존하고 V1 중단 기록은 기존 전멸 목표로 복원한다.

BattleTacticalControls/Hud와 BoardThreatView는 기존 작은 컨텍스트 창, 직접 클릭 발동, 공통 모달 입력 차단을 확장한다. 위험·예고·선택/행동자 표시는 별도 도형으로 구분하고, 전체 예측은 페이지 모달이다. AccessibleText는 작성된 최소 크기를 유지하며 PlayerPreferences.TextScale에 따라 최대 크기를 늘리고 제한 공간에서 자동 축소한다. 신규 에셋은 TacticalContent.Add가 없는 항목만 추가하며 DemoContent.Create를 재실행하지 않는다.

## 인터미션 공통 UI

BattleIntermissionHud는 기존 준비 화면 패널과 FormationText/Button을 재사용하며 공통 상단 이동, ID 기반 초상/장비 그림, 캐릭터 목록,9개 능력치 비교와 하단 확정 버튼을 제공한다. Clear(header)는 인터미션 상태/색상도 초기화해 전투·임무·기존 모달로 복귀할 수 있다. 이미지 비율 조정은 배치 컨테이너 안의 별도 자식에만 적용하며, 초상 목록의 제목/이름에는 고정 높이를 확보한다.

BattleIntermissionEquipmentHud는 EquipmentLoadout 초안과 저장된 장비를 따로 읽는다. 캐릭터/메뉴 전환 시 초안을 버리고 적용 시 기존 수량 검사와 TrySave를 사용한다. BattleIntermissionMarketHud는 카탈로그를6개씩 표시하며 기존 구매/매각 차단·트랜잭션을 그대로 호출한다. 거래 후 필터/페이지/선택을 유지하고 마지막 미장착 수량을 매각한 경우 상세를 남겨 거래 불가 사유를 보여 준다.

BattleIntermissionGrowthHud는 실제 저장 성장과 승급 조건을 표시한다. 특성 선택은 nullable 인수로 전달하는 UI 초안이고 적용 버튼에서만 SaveTrait를 호출한다. SaveTrait 실패 시 기존 특성을 복구하며 이번 호출에서 추가한 새 성장 기록도 제거한다. 기술 숙련은 기존 UnitDetails 모달의 입력 차단/닫기를 재사용한다. 단순 조회에는 Campaign.Get 대신 FormationProgress를 사용하므로 없는 성장 기록을 생성하지 않는다.

## 아군 자유 선택 턴

실제 게임의 기본값은 `TeamTurnScheduler`다. 아군 페이즈에서 살아 있는 미완료 아군을 선택하고, 전원 종료 후 적군이 속도순으로 한 번씩 행동한다. `BattleSession.Select`는 행동을 소비하지 않고 `BeginActive`는 페이즈당 각 유닛의 첫 선택에만 `BeginTurn`을 호출한다. 이동·행동·이동 취소·쿨다운·연계·상태·적 예고의 턴 카운터는 선택 전환으로 초기화되지 않는다. `EndTurn`만 해당 유닛을 대기 목록에서 제거하고 생존 목표를 진행한다. 생존 목표의 기존 ‘아군 개별 턴 종료 횟수’ 의미는 유지한다.

`BattleDirector.SelectPlayerUnit`은 명령 대기·아군 페이즈·전투 진행 중에만 허용한다. 맵 클릭, 상단 최대6명 버튼, PgUp/PgDn이 이 경로를 공유한다. 타깃 선택·연출·방향 확정·모달·튜토리얼 도중에는 바꾸지 않는다. 기절/수면 유닛은 선택 후 기존 행동 불가 턴을 한 번 소비한다. 이미 종료한 유닛의 부활은 추가 행동을 주지 않는다.

설정의 `FixedSpeedOrder`와 기존 `CT`로 SPD/CT도 선택할 수 있다. 기본 설정은 둘 다 false이며, 코어의 기존 `BattleSession` 호출은 명시적으로 `teamTurns:true`를 넘긴 경우에만 새 규칙을 사용한다. 튜토리얼의 안내 순서 스케줄러는 유지한다. `BattleCheckpoint` V3는 미완료 인덱스·시작/처리된 인덱스·라운드를 저장하고, 이어하기에서 `BeginTurn`을 다시 호출하지 않는다. V1/V2 중단 전투는 저장 당시 SPD/CT로 복구한다. 캠페인 외부 저장 버전은2를 유지한다.

## 전술 시스템·UI 후속 개선

신규 전투의 자유 선택 턴에 맞춰 생존 목표, 전체 턴 종료, 남은 행동 표시, 전체 기술 조회, 민첩·협공 예측을 연결했다. Lv10 성장 방향과 편성·장비 3슬롯 프리셋을 추가했다. 규칙과 저장 호환성은 [TacticalPolish/README.md](TacticalPolish/README.md)를 따른다. 난이도 프리셋·증원·반격·전투 되감기는 별도 후속 범위다.

## 보스전 후속 조정

`BattleSession.BossEncounters`는 새 자유 턴 2장·6장 캠페인에만 활성화되는 규칙이다. 초기 적 위치를 후방·분산 배치로 바꾸고 목표 유닛에 `BossWard`를 부여한다. `SkillResolver.BossWardPercent`는 같은 편의 생존 유닛 수로 감소율을 계산한다. `BattleProjection`도 이 플래그를 복제하여 호위가 범위 공격 중 쓰러지는 경우까지 동일 순서로 예측한다. 목표 밖 보스라도 방벽이 바뀌면 ForecastRow에 포함한다.

V5 중단 기록의 `BossEncounters`로 규칙 적용을 결정하고 위치·HP·예고는 기존 CheckpointUnit에서 복원한다. 구 V1–V4는 새 규칙을 강제로 끈다. 호위 방벽 수치는 별도 저장하지 않고 생존 상태에서 파생한다. [범위와 자동 전투 비교](BossBalance/README.md).

## 전투 시작 상태 재도전

`BattleOpening`은 첫 `TurnStartState` 진입 전의 유닛 기록과 생성 옵션을 보관한다. 진행 중 상태를 저장하는 `BattleCheckpoint`와 구분하여 스케줄러·목표 카운터를 처음부터 생성한다. 새 세션을 완전히 검증한 뒤 `BattleDirector.RetryOpening`이 현재 세션을 교체한다. 저장된 캠페인 성장/보상에는 쓰지 않는다.

V6 중단 기록의 `HasOpening`이 참일 때만 시작 기록을 복원한다. inline null을 재생성하는 JsonUtility 특성 때문에 별도 플래그를 사용한다. 시작 기록의 장·편성과 중단 기록이 다르거나 검증에 실패하면 중단 저장을 소비하지 않는다. V1–V5의 진행 복원은 유지하고 시작 재도전만 제공하지 않는다. [조작·호환성](BattleRetry/README.md).

- TMP Essentials의 Mobile/SpaceWarp 셰이더 디버그 지시문은 Unity 6000.6의 enable_debug_symbols로 갱신했다. Essentials 재임포트로 덮어쓸 경우 이 호환 수정도 보존한다. URP 자동 디버그 리소스와 Pipeline 패키지 캐시는 변경하지 않는다.

## 행동 경험치 (2026-10-12)

- `ActionExperience`는 한 행동의 실제 피해/회복/상태 변화를 대상별로 합산한다. 다단/범위/레벨 차이/반응 행동 배율을 적용한 후 `SkillResolver`가 본 행동과 모든 지원·반격이 끝난 시점에 지급한다. 전투 예측은 동일 규칙의 복제 세션을 사용한다.
- `UnitRuntime.EXP`는 현재 레벨 내 경험치, `BattleEXP`는 캠페인에 아직 저장하지 않은 전투 획득량이다. `ExperienceRules`는 고정 100 기준 성장과 상한을 공유한다. 레벨업 시 생존자의 HP와 MP에는 최대치 증가분만 더한다.
- 승리 시 `CampaignStages.TryReward`가 캐릭터별 `BattleEXP`와 최초 30/반복 10을 기존 원자적 저장 경로에서 확정한다. 실패·중복 보상 방지는 기존 트랜잭션을 유지한다.
- Campaign v3은 v1/v2의 `EXP / (Level * 100)` 비율을 100 기준으로 한 번 변환한다. 구버전 파일의 첫 저장에 버전별 원본 백업을 남긴다. 체크포인트 v9·출전 기록 v4는 행동 경험치 사용 여부와 레벨/EXP/BattleEXP를 보존한다. 그 이전 기록은 행동 경험치를 비활성화해 복원한다.
- `BoardView.PresentExperience`는 짧은 월드 텍스트를 표시한다. 성장 화면은 100 기준 막대, 결과 화면은 실제 총 획득량과 변경된 경우의 레벨 화살표만 표시한다. 새 확인창은 추가하지 않는다.
- 수치/호환성/검수 재현 방법: `Docs/ActionExperience/README.md`.


## 교전 구성과 성장 기회 (2026-10-12)

- `CampaignEncounters`는 기존 맵에 장별 6/7/8/8/10/10명의 적 종류·좌표를 정의한다. `BattleDirector.BeginBattle`은 신규 캠페인에 `ExpandedEncounters`를 사용하며 훈련/구버전 복원은 기존 구성이다.
- 자유 턴 3·5장은 마지막 2명을 `MissionOptions`의 기존 단발 증원 경로로 3턴에 생성한다. 임무 옵션과 기본 증원이 겹쳐도 총량은 같다. SPD/CT는 전체 인원을 처음부터 배치한다.
- 체크포인트 v10/출전 기록 v5가 확장 구성 여부를 보존한다. 구 v9/v4 이하는 확장 구성을 끈 채 복원하고, 되감기/재도전에서도 이를 유지한다. 캠페인 v3과 행동 경험치 공식은 동일하다.
- 임무 적 목록은 4명씩 페이지를 나누며 새로 열면 첫 페이지부터 시작한다. 준비 단계에서는 증원 예정 적까지 표시하고, 전투에서는 실제 등장한 적만 표시한다.
- 자동 성장 비교와 한계는 `EncounterBalance/README.md`에 기록한다. 느린 전열의 성장 격차가 완전히 해소된 것은 아니다.

## 캐릭터·전투 표시와 Windows 창 (2026-10-12)

- `CharacterVisualSizing`은 기존 전면 스프라이트의 몸체 비율(제이드 .82, 민트 .96, 나머지 1)과 1.22 월드 높이를 공유한다. `CharacterModelImage`는 무기까지 포함한 폭에 맞춰 몸체를 축소하지 않고 발 기준으로 그린다. 전투는 `Body Scale` 부모를 두어 자식 `CharacterMotion`의 애니메이션 스케일을 보존한다. 몬스터 크기는 유지한다.
- `UnitHealthDisplay`/`HealthBarGraphic`은 전장 카메라 위치를 Overlay Canvas에 투영하여 최대 HP 트랙·잔여 HP·손실 잔상을 그린다. 선택/호버에만 숫자를 보여주고 HUD보다 아래에서 렌더링한다. 사망/스토리/모달에는 숨긴다.
- `BoardMovementPreview`는 이동 영역 외곽, 가는 경로와 화살촉, 목적지 유령 모델을 관리한다. 상태 취소와 보드 재생성 시 함께 정리한다. `PendingFacing`은 실제 방향과 분리하며 `FacingPointer` 호버/방향키는 미리보기, 클릭/Enter는 확정이다.
- `CanOpenSkills`와 행동 상태는 `SkillResolver.CanUse`의 실제 연계 가능 조건을 사용한다. 일반 행동 이후 연계 플래그만 남은 상태에서 빈 기술 탭이 열리지 않는다.
- `WindowDisplayController`는 크기 변경 안정화 후 `SavePreferences(..., false)`로 창 크기를 기록한다. Windows `IsZoomed`/`IsIconic`을 사용해 최대화/최소화를 구분하고 `WindowMaximized`와 복원용 Width/Height를 따로 보관한다. PlayerSettings의 resizableWindow를 켰다. 기존 v1 설정은 새 bool 필드가 false로 복원된다. 해상도 프리셋을 적용하면 최대화 기억을 해제한다.
- 개발 전용 `ManualPlayerReview`는 격리 설정을 디스플레이에도 적용하여 재실행 검수가 사용자 설정을 건드리지 않게 한다. 일반 배포 빌드에서는 컴파일되지 않는다. 상세 증거는 `BattleVisualPolish/README.md`.

## 지면 선택·위험 영역 (2026-10-12)

- `GroundOverlay`는 알파가 있는 지면 메쉬를 그린다. 선택 커서는 작은 모서리8선분과 옅은 내부 면, 접지 그림자는 중심에서 가장자리로 알파가 사라지는 부채꼴이다. 렌더러는 콜라이더/그림자 투사 없이 캐릭터 뒤에 정렬하며 소유 메쉬는 종료 시 해제한다.
- `BoardThreatView.Region`은 위험 셀의 합집합을 한 메쉬로 만들고 같은 높이의 내부 경계를 생략한다. 확정 예고는 별도 황색 영역이며 위험 표시 꺼짐과 독립적이다. 죽은 적을 제외하고 재도전/보드 교체 시 영역을 정리한다.
- `UnitTone.shader`는 Resources로 일반 빌드에 포함된다. 캐릭터 텍스처의 채도/밝기만 MaterialPropertyBlock으로 제어해 CharacterMotion의 피격/시전 tint를 보존한다. TeamTurnScheduler의 실제 미완료 목록을 사용하므로 이동/행동 여부 플래그만으로 완료 처리하지 않으며, 다음 아군 페이즈에서 색을 복원한다. MaterialPropertyBlock은 Unity 객체 생성이 허용되는 실행 시점에 초기화한다.
- 기존 전투 데이터/저장 버전 변경은 없다. 실제 캡처와 검증은 `GroundedMarkers/README.md`.

## 지면 방향 선택 (2026-10-13)

- `GroundOverlay.Arrow`는 사각 몸통과 삼각 화살촉으로 채워진 평면 화살표를 만든다. `BoardMovementPreview`는 방향 선택 상태에서 네 방향 메쉬를 캐릭터 뒤에 표시하고, `PendingFacing`이 바뀔 때만 선택색을 갱신한다. 모달/스토리/상태 종료 때 숨기며 보드 리셋 때 캐시를 함께 정리한다.
- `BattleFacingHud`에는 투명한 버튼만 남긴다. 카메라로 화살표의 지면 범위를 투영해 위치·크기를 맞추므로 화면 비율이나 회전이 바뀌어도 시각 표시와 클릭 위치가 분리되지 않는다. 최소 클릭 크기는 Canvas 좌표32×24이다. 별도의 화면 방향 꺾쇠와 선형 미리보기는 제거했다.
- `FacingPointer`·방향키의 미리보기, 클릭/Enter 확정, Esc 취소는 기존 경로를 유지한다. 실제 전투 방향은 확정할 때만 변경한다. 검증과 실제 화면은 `GroundFacing/README.md`.

## 캐릭터 모션과 상태창 (2026-10-13)

- `MotionPremiumImporter`는10명 원화의 Sprite Editor Data Provider 이름/영역/발 피벗/외곽선과 참조를 한 번 연결한다. 이미 확장된 클립이면 중단하며 Content 초기화는 호출하지 않는다. 방향별760장의 알파 측정은 `Tools/MeasureMotionPremium.py`와 CSV를 따른다.
- `DirectionalSpriteClip`의 준비/접촉 이후 구간을 이용해 보행8·공격10·일반 기술14·궁극기20~22프레임을 재생한다. 캐릭터별 기술 클립은 공유하며 전투 판정은 기존 ReleaseSkill/Resolver 시점에 유지한다.
- `CombatMotionProfile`은 무기별 전진/반동/도약/기울기/접촉 유지시간을 정의한다. `CharacterMotion`은 실제 facing 방향의 변위와 피격 방향 반동을 적용하고, `CombatEffect`는 접촉 그림을 짧게 유지한다. 글로벌 timeScale과 저장 데이터는 변경하지 않는다.
- `BattleCharacterStatusHud`는 상태창 전용 대상/탭/페이지/조회 방향을 관리한다. 동일 진영의 실제 유닛 또는 준비 단계의 장비 적용 미리보기를 조회하며 BattleSession.Active와 실제 facing을 수정하지 않는다. 능력/기술/장비·상태 효과·특성/적 전술을 보여준다.
- 기존 보조 팝업은 `PrepareUnitModal`을 사용한다. `ShowUnitDetails`는 큰 상태창과 입력 차단 배경을 열고, `CloseUnitDetails`는 모두 숨긴다. Q/E 입력은 이 모달이 열렸을 때만 처리한다.
- 아트 출처, 시안/실제 화면 및 검증은 `MotionStatus/README.md`를 따른다.
