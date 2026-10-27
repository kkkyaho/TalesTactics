# 오리진 실제 방향 검수 — 2026-10-27

실제 Windows 검수 실행 `1d61159701a04402ae6dd6398810e582`에서 **보스 20명 × 상태창/전장 2종 × 4방향 = 160/160조합**을 완료했다. 모든 행의 실제 스프라이트가 예상 스프라이트와 일치하고 표시 상태가 참이며, 오류는 0건이다. [방향 결과 원본](Validation/origin/directions.json), [완료 결과 원본](Validation/origin/result.txt), [SHA-256 캡처 명세](Validation/origin/capture-manifest.json)를 보관했다.

상태창은 화면의 `모델 회전` 버튼에 Unity submit 이벤트를 전달해 네 방향을 전환하고 실제 `CharacterModelImage.sprite`를 비교한다. 전장은 기존 보드 카메라 회전 API를 90도씩 적용해 카메라 기준 네 방향의 실제 `SpriteRenderer.sprite`와 활성 상태·카메라 시야 내 표시를 확인한다. 사람의 마우스 입력 검수는 아니다.

## 오리진 실제 캡처

통합 담당자가 아래 실제 캡처 **8장 모두**를 표시된 크기로 확인했다. 다리, 네 팔의 구조와 칼날의 형태·연결이 방향에 맞게 보이며, 해당 화면에서 분리되거나 누락된 신체/무기는 관찰하지 못했다. 아래 PNG는 원본 그대로 복사했고 편집·재인코딩하지 않았다. JSON·TXT와 PNG 8장의 합계는 **30,473,526바이트**이며 각각 원본과 바이트/SHA-256 일치를 확인했다.

| 방향 | 상태창 실제 캡처 | 전장 실제 캡처 | 실제 스프라이트 |
| --- | --- | --- | --- |
| 정면 | [상태창](Validation/origin/origin-model-direction-Front.png) | [전장](Validation/origin/origin-battlefield-direction-Front.png) | `origin_0_0` |
| 후면 | [상태창](Validation/origin/origin-model-direction-Back.png) | [전장](Validation/origin/origin-battlefield-direction-Back.png) | `origin_0_1` |
| 좌측 | [상태창](Validation/origin/origin-model-direction-Left.png) | [전장](Validation/origin/origin-battlefield-direction-Left.png) | `origin_0_3` |
| 우측 | [상태창](Validation/origin/origin-model-direction-Right.png) | [전장](Validation/origin/origin-battlefield-direction-Right.png) | `origin_0_2` |

## 수정 이력과 한계

이전 방향 검수는 레그레타 검수 상태에서 전투 모델이 활성 상태로 동기화되지 않아 중단됐다. 검수 fixture의 전투 시작 뒤 `RefreshViews()` 호출과 활성 렌더러 확인을 보강한 뒤 위 실행을 다시 완료했다. 이 동기화 수정 때문에 오리진 원화 PNG를 다시 변경하지 않았다. 보관 시 적용 원화 SHA-256은 `5e61346d221c7a78c183c5befa8cc571b0ba7c9c9e28c17fec38ea1235060877`이며 [기존 제작 측정](../SdCharacters/2026-10-24/origin-clarity-v2-production.json)의 후보 SHA와 같다.

본 실행은 모든 장을 해금하고 레벨 40으로 구성한 방향 표시 fixture다. 전투 중 예비 보스를 활성화하기 위한 상태 조정도 사용하므로 실제 캠페인 진행·난이도·피해량 검증이 아니다. 이번 8장 확인은 정지된 네 방향 표시를 검수한 것으로, 오리진 72포즈 전체의 연속 동작을 새로 검수했다는 뜻이 아니다. 크레스 모션은 별도의 [최종12동작·표적 육안 검수](README.md#크레스-최종-연속-동작)에서 확인했다.
