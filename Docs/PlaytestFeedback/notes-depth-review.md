# 서 있는 스프라이트 깊이 검토

2026-11-02 소스 검토와 후속 수정·검증 기록이다. 아래 실행 결과는 실제 Unity 검증 JSON과 최종 테스트 소스를 확인하여 기록했다.

## 적용 범위

`StandingSpriteDepth.Configure(SpriteRenderer)`는 캐릭터, 이동 목적지 미리보기, 시민, 건축 소품과 수목의 서 있는 `SpriteRenderer`에만 깊이 보정을 켠다. 화면상의 원화 위치는 유지하고, orthographic 카메라에서 발 위치에 서 있는 몸체의 깊이를 계산한다. 스프라이트 정렬 기준도 중심에서 pivot으로 바뀐다.

바닥 오버레이, 평면 foliage mesh, 전투 VFX와 바닥 효과는 기존 월드 깊이 처리를 유지한다. 공유 `UnitTone` 재질의 `_StandingDepth` 기본값은 0이며, 적용 여부는 각 renderer의 `MaterialPropertyBlock`에 저장된다. 캐릭터 환경광·대기 연출·전술 강조와 시민 환경광의 후속 갱신은 기존 block을 읽어 수정하므로 이 값을 보존한다.

현재 고정 카메라 pitch 57°에서 yaw 회전은 view matrix를 통해 깊이 보정에 반영된다. 투명 renderer 정렬은 `CustomAxis`를 사용하며 카메라 forward의 Y를 0으로 만든 수평 방향을 정규화한다. `BoardView` 초기화·회전·카메라 복원 시 축을 갱신한다.

## 단차 투명 소품 수정과 검증

이전 `TransparencySortMode.Orthographic`는 pivot의 전체 view-depth를 사용하여 높은 뒤 소품이 앞 캐릭터를 덮거나 낮은 앞 소품이 캐릭터 뒤로 정렬되는 문제가 있었다. pitch 57°에서 수평으로 .65 뒤, 높이 .5 위인 prop의 거리 차이는 `cos(57°) * .65 - sin(57°) * .5 ≈ -.065`다. 두 `UnitTone` renderer 모두 `ZWrite Off`이므로 서로의 per-pixel 깊이만으로는 정렬을 보완할 수 없었다. 수평 발 위치를 기준으로 하는 `CustomAxis`로 수정했다.

실제 URP 렌더를 사용하는 `StandingSpriteOcclusionTests` 최종 실행은 **1/1 통과, 전체 0.39초**다. yaw -39°·51°·141°·231° 각각에서 앞·뒤 투명 prop과 높이 -.5·0·+.5 조합을 검증했다. 이전 Orthographic 정렬로 되돌리는 대조 조건은 높은 뒤 prop과 낮은 앞 prop의 잘못된 정렬을 모두 재현했다. 같은 테스트에서 뒤 opaque 벽의 몸통 잘림 수정과 앞 opaque 벽의 정상 가림도 확인했다. [실제 실행 결과](../../Logs/PlaytestFeedback20261102/depth-height-final.json)

바닥 geometry의 깊이와 재질은 바꾸지 않았다. 명시된 `sortingOrder`가 우선하므로 projected shadow -5, threat -4, contact -3, selection -2와 ornament -1, actor 0, 화살표 6, field icon 7, VFX 20/23의 기존 순서는 유지한다. 같은 순서인 objective beacon surface·기본 경로선과 actor의 겹침은 전체 화면 검수에서도 확인할 항목이다.

## 별도 재현이 필요한 항목

- **전투 VFX와 몸체의 깊이 차이:** 일반 타격·회복 contact 위치는 발 위치에서 `camera.up * .65`만큼 올리고, `ImpactParticles.Plane()`도 카메라 평면 좌표를 사용한다. `Impact` shader에는 새 upright 보정이 없다. 따라서 뒤 벽 근처에서 몸체가 보이더라도 VFX 상단이 벽 깊이에 가려지는 차이는 남을 수 있다. 기존 월드 메시 경로에 대한 기하 추론이며, 이번 검토에서 실제 GPU 재현을 확인하지 않았다. `sortingOrder`만 높여서는 opaque 벽의 depth test를 우회하지 못한다. 실제 액션 검수에서 문제가 재현되면 contact billboard와 바닥·입체 효과를 구분하여 대응해야 한다.

단차 정렬 수정은 `StandingSpriteDepth.cs`와 `BoardView.cs`의 카메라 정렬 설정에 한정했다. 확인되지 않은 VFX 현상을 이유로 이번에 월드 효과의 깊이 처리 전체를 확장하지 않았다.
