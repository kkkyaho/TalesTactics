# Cless motion direction review

2026-10-27 검수기 추가. 실행 결과를 생성하기 전에는 아래를 통과 증거로 사용하지 않는다.

## 실행

기존 전투 씬의 Play Mode에서 빈 절대 출력 경로를 지정한다.

```csharp
TalesTactics.SdMotionDirectionReview.BeginEditor(newEmptyAbsoluteOutput);
```

기본 연속 캡처는 **960×540, 시뮬레이션 30fps**다. 필요한 경우 두 번째 인자 `1280`으로 1280×720을 선택할 수 있다. 별도 검수 define을 포함한 Windows 실행 인자는 `--sd-motion-review <32자리 GUID>`이며, 플레이어 옆 `SdMotionReviews/<GUID>`에 기록한다. Windows 실행에는 별도 `-logFile` 경로를 사용한다.

검수기는 `UNITY_EDITOR || TALESTACTICS_REVIEW`에서만 포함된다. 일반 배포 빌드에는 넣지 않는다. Unity 실행·D 동기화는 통합 검증 담당자가 수행한다.

## 실제 검수 범위

기존 `SkillRosterVfxFixture.BeginActor`와 `Scenario`를 그대로 재사용하여 다음 **12회**를 실행한다.

- `cless.attack` 기본 공격 × Front / Back / Left / Right
- `cless.2` 추사우, 일반 Skill 클립 × 4방향
- `cless.ultimate` 명공참상검 × 4방향

카메라를 90도씩 돌려 `CharacterMotion.ViewFacing`의 실제 결과가 요구 방향인지 확인한다. 캐릭터 스킬, 전투 판정, 클립 타이밍 또는 원화를 바꾸지 않는다. 훈련 레벨·자원·6인 배치·표적은 기존 fixture의 준비 상태다. 캠페인 성장이나 밸런스를 검증하는 실행은 아니다.

각 회차에는 1920×1080 **원래 전장의 행동 전 실제 화면** 1장이 있다. 연속 모션은 별도 검수 카메라가 실제 크레스 SpriteRenderer만 렌더링한다. 배경은 단색이며 HUD·다른 유닛·VFX는 이 근접 화면의 렌더링에서 제외한다. 원래 카메라는 계속 실제 방향과 전투 표시를 구동하고, 6개 아군 모델과 VFX는 시뮬레이션에서 유지한다. 이 방식은 손·무기·몸체 연결을 보기 위한 화면이며, 근접 화면을 전체 전장 또는 VFX 가시성 검수로 표현하지 않는다.

PNG는 모두 `ScreenCapture.CaptureScreenshotAsTexture`로 받은 실제 Game View 렌더다. 크롭·리사이즈·합성·원화 편집은 하지 않는다. 프레임마다 Sprite 이름, 월드/카메라 상대 방향, 카메라 각도, Unity 프레임 번호, 시뮬레이션 시간, 모션 종류, 연출 중 여부, 실제 판정 횟수, 6인 모델 수, 화면 내 실루엣 범위를 기록한다.

## 출력과 판정

- `result.json` / `progress.json`: 전체 결과, 완료 수, 저장 보존, 관찰한 중요 프레임.
- `<skill>-<Facing>/timeline.json`: 프레임별 데이터와 독립 resolver 비교 결과.
- 각 회차의 `battlefield.png`: 1920×1080 원래 전장 참고 화면.
- 각 회차의 `0000.png`부터: 대기 → 실제 행동 → 접촉/회복 → 대기로 돌아오는 연속 근접 렌더.
- `representativeFrame`: 실제 판정 뒤 처음 기록한 프레임의 경로이며 별도 합성 이미지를 만들지 않는다.
- `isolated-save`: 새 격리 저장. 사용자 저장은 읽기 해시만 취한다. 검수 전후 campaign/settings 파일의 해시와 격리 파일 불변을 확인한다. 해상도 변경이 설정 저장으로 남지 않도록 검수 중 창 설정 기록 컴포넌트만 일시 중지하고 복구한다.

기존 fixture 검사로 실제 판정 1회, 비용·HP/MP·상태·점유·난수의 독립 resolver 일치, 접촉 전 판정 불변, 효과 정리와 Command 복귀를 확인한다. 추가로 전 프레임의 방향/6인 모델 수/화면 밖 실루엣 여부, 모션 종료를 확인한다. `SessionState["TalesTactics.SdMotionDirectionReviewExit"]`는 0 통과, 1 실패다.

방향 검사는 계산된 카메라 방향만 비교하지 않는다. 각 회차의 해당 방향 대기 Sprite와 실제 선택된 AttackMotion/Skill/Ultimate 클립 Sprite를 `EquipmentAppearance.Resolve`에 통과시켜 기대 집합을 만든다. 매 프레임 실제 `SpriteRenderer.sprite`의 객체가 이 집합에 포함되는지 검사하고 이름과 결과를 기록한다. 회차별로 해당 방향의 모든 고유 클립/대기 포즈가 관찰됐는지도 검사한다. `authoredDirectionSprites`, `requiredPoseSprites`, `missingRequiredPoseSprites`, `requiredWatchedSprites`와 프레임별 `authoredDirectionSprite`를 확인한다. 다른 방향에서 관찰한 중요 포즈로 해당 회차의 누락을 대신하지 않는다. 최종 결과 쓰기에 실패해도 `finally`에서 카메라·Canvas·레이어·시간·화면 설정과 종료 상태를 복구한다.

다음 6개 이름이 실제 `SpriteRenderer.sprite.name`에 나타나야 전체 통과로 표시한다.

| 프레임 | 주요 확인 위치 |
| --- | --- |
| `cless_16_1` | Back 일반기/궁극기의 들린 손과 다음 프레임 연결 |
| `cless_18_1` | Back 궁극기의 어깨·망토 방향 연결 |
| `cless_10_2` | Right 궁극기의 칼 방향 변화 |
| `cless_9_2` | Right 일반기/궁극기의 머리 근처 검 각도 |
| `cless_11_2` | Right 궁극기의 검 궤적 |
| `cless_14_2` | Right 공격/일반기/궁극기의 회복 자세 |

각 회차는 최대 150행동 프레임(+대기 1장), 실시간 120초다. 전체 상한은 연속 PNG 1,812장과 전장 참고 PNG 12장이고, 정상 실행 예상량은 약 700~1,000장이다. 느린 PNG 인코딩이 시뮬레이션 연속 프레임을 건너뛰지 않도록 `Time.captureFramerate=30`을 사용한다. 실제 사람 체감 재생 시간이나 GPU 성능 측정으로 해석하지 않는다. 30fps에서 중요 프레임이 관찰되지 않으면 누락을 보고하고 실패시키며, 임의로 포즈를 주입해서 채우지 않는다.

최종 부드러움·팔다리·검 궤적의 자연스러움은 PNG 연속 재생을 별도로 확인한다. 자동 비교 통과만으로 미술 품질이나 모든 동작의 완성을 주장하지 않는다.
