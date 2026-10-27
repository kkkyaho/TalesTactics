# 실제 경로 자동 진행 및 보스 방향 검수

2026-10-24 추가한 검수 코드의 실행 계약이다. 실행 결과가 없는 상태에서는 완주·통과 증거로 사용하지 않는다.

## 실제 1~20화 흐름

`CampaignFlowReview`는 `UNITY_EDITOR || TALESTACTICS_REVIEW`에서만 포함된다. 기존 전투 씬을 Play Mode로 연 뒤 다음을 호출한다.

```csharp
TalesTactics.CampaignFlowReview.BeginEditor(newEmptyAbsoluteOutput, 4);
```

리뷰 define으로 빌드한 Windows 플레이어는 `--campaign-flow-review <32자리 GUID>`로 실행한다. 출력은 플레이어 옆 `CampaignFlowReviews/<GUID>`에 생성한다. 임의 사용자 저장 경로 또는 기존 결과 폴더를 출력으로 지정할 수 없다.

- 새 격리 슬롯에서 `StartTitleGame`으로 시작한다. 각 화 실제 합류 동료 중 정해진 우선순위 최대 6인을 편성한다.
- 소유 장비의 개선분을 장착한 뒤, 현재 골드와 해금 조건 안에서 호환 장비를 `CampaignInventory.TryBuy`와 `EquipmentLoadout.TrySave`로 구매·착용한다. 정책은 고정 능력치 점수의 개선량/가격 순이다. 무료 합류 훈련·매각·반복 보상은 사용하지 않으며, 조건을 만족한 승급은 실제 저장 API로 수행한다.
- 렌더링된 이야기 시작/다음/결과 계속 버튼을 통해 전후 대사를 모두 표시한다. 13화는 왼쪽 경로를 선택한다. 후일담 다음 줄 커서는 매 줄 디스크 재읽기로 확인하며, 매 화 후일담 완료 뒤 실제 타이틀로 돌아가 `LoadTitleGame`으로 재로드한다.
- 타이틀 왕복 **직전** `CampaignSave` 전체를 별도 JSON으로 보관하고, 재로드 뒤 전체 상태와 런타임 배치/선택장을 대조한다. 정상 갱신되는 `SavedAt`만 제외하며, 보상·성장·스킬·특성·장비·프리셋·배치·선택장·후일담·중단 상태는 모두 비교한다. `episode-XX-before-title-reload.json`과 `episode-XX-after-title-reload.json`, 각 SHA-256 및 `permanentStateRoundTripPreserved`를 남긴다. 로드 이후 메모리와 디스크만 비교한 것을 왕복 전 상태 보존 증거로 사용하지 않는다.
- 아군은 기존 `UtilityPlanner` 결과를 `BattleDirector.MoveUnit/Execute/Guard/WaitCommand/ChooseFacing`에 전달한다. 적은 실제 `BattleDirector.EnemyTurn` 코루틴을 사용한다. 생존 적이 없는 경우에만 기존 감사와 같은 합법적 노드/출구 경로 탐색을 사용한다.
- 레벨·HP·MP·승패를 주입하지 않고, 실제 `BattleEndState → CompleteBattle → SaveBattleReward`에서 보상을 얻는다. 보상 중복 호출, 실제 액션 EXP+임무 EXP, 합류 시점, 다음 화 및 최종 완료 상태, 저장 파일을 검증한다.
- 500 아군 명령 또는 900초 벽시계/화를 넘으면 실패 원인을 남기고 멈춘다. 패배도 그대로 실패 보고한다. 봇의 실패를 콘텐츠 진행 불가로 자동 판정하지 않는다.

출력은 `report.json`, 실제 화면 PNG, 격리 `Storage`다. 부분 결과도 진행 중 기록한다. Play Mode 종료 코드는 `SessionState["TalesTactics.CampaignFlowReviewExit"]` (0 통과, 1 중단)다.

11·16·20화에서는 다음 필드를 구분해서 해석한다.

| 항목 | 기록 | 해석 한계 |
| --- | --- | --- |
| MP | 유닛별 진입/최저/종료 MP, 기술 수, 명목 MP 비용 합 | 레벨업·회복이 있어 시작−종료와 소비 합이 같지 않을 수 있다. |
| 행동 기회 | 적/보스 시작 턴, 행동불가 시작 턴, 준비 관찰, 발동 기술, 보스 단계 | 준비/턴은 프레임 관찰이다. 원인 확정은 action 로그와 실제 전술 코드/화면을 함께 본다. |
| 성장 | 진입 EXP, 전투 내 최종 EXP, 보상 저장 후 최종 EXP, 행동 EXP | 보상 저장 후의 성장과 전투 내 성장을 분리한다. |
| 경제 | 구매 거래별 전후 골드, 지급 아이템, 인벤토리, 착용 장비 | 단일 구매 정책·단일 파티다. 최적 경제나 가격 균형을 인증하지 않는다. |
| 시간 | 실제 벽시계, 가속된 시뮬레이션 시간, 프레임 수, 기술 windup+recovery 합 | 사람 플레이 시간으로 환산하지 않는다. 명목 연출 합은 이동·발사체·대화/사고 시간을 포함하지 않는다. |

이것은 실제 엔진 경로를 사용한 **자동 진행**이다. 사람이 대사를 읽고 20화를 수동 완주한 검수와 구분한다. 중간 후일담의 프로세스 재시작은 이 실행에 포함하지 않는다. 사용자 저장 파일 해시 변화가 있으면 통과하지 않는다.

## 보스 상태 모델과 전장 4방향

```csharp
TalesTactics.CrossoverPlayerReview.BeginEditor(newEmptyAbsoluteOutput, true);
```

`true`는 방향 검수만, `false`는 기존 전체 프레젠테이션 검수다. 플레이어 인자는 `--crossover-directions-review <GUID>` 또는 기존 `--crossover-review <GUID>`다.

상태창을 한 번 연 뒤 실제 **모델 회전** 버튼을 3번 눌러 Front/Back/Left/Right 4방향을 캡처한다. 별도로 상태창을 닫고 전장 카메라를 90도씩 돌려 4방향을 캡처한다. `directions.json`에 표면 종류·월드 방향·뷰 방향·카메라 각도·예상/실제 Sprite·전장 가시성·캡처 이름을 남기고 Sprite 일치와 방향 고유성을 확인한다. 종전처럼 매번 상태창을 다시 열어 정면으로 초기화한 것을 4방향 검증으로 세지 않는다.

이 검수는 모든 화 해금/레벨 40, 예비 보스 활성화를 위한 기존 fixture 상태를 사용한다. **그림·방향·연출 증거이며 성장·밸런스·완주 증거가 아니다.** 종료 코드는 `SessionState["TalesTactics.CrossoverPlayerReviewExit"]` (0 통과, 2 오류)다. 화면 품질·잘림·방향별 차이는 생성된 실제 PNG를 별도로 확인한다.
