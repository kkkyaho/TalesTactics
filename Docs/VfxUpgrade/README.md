# 전투 VFX 개선 — 구현 및 검수

상태: **구현, 최종 Editor 촬영, Windows 빌드와 검수 플레이어의 12개 시퀀스 검사를 완료했다. 일반 실행본은 숨김 시작·로그 검사까지 확인했으며 타이틀 화면 수동 검수는 하지 못했다.** EditMode는 358/358 통과했다. 2차 전체 PlayMode는 184/185 통과 후 실패한 궁극기 구분 문제를 수정해 해당 검사 1/1을 별도 통과했으며, 이를 단일 전체 실행의 185/185 통과로 표현하지 않는다.

[실제 전후 비교 페이지](review.html)에서 대표 기술 6종을 볼 수 있다. [Actual](Actual/)의 영상·PNG와 [촬영 출처 명세](Actual/manifest.json)는 수정 전 `before-visible`, 최종 수정 후 `after-final` 자료를 사용한다. 영상은 무음이며 소리 검증 자료는 아니다.

## 구현 범위

기존 기술 데이터의 속성, 무기 표현, `SkillVisualPattern`을 사용해 준비 → 이동 → 접촉 → 잔광의 흐름을 구분한다. 검기·화염/유성·얼음·번개·회복·궁극기를 대표 검수 대상으로 삼는다. 전투 규칙을 추가하거나 기술마다 새 피해 계산을 만들지 않는다.

- **공통 이미지 소재:** `Assets/TalesTactics/Resources/TalesTactics/CombatElements.png`는 4열 × 4행의 16프레임 아틀라스다. 행별로 검격/검기, 얼음, 번개, 회복 이미지를 4프레임씩 배치했다. native `imagegen`으로 생성한 뒤 같은 도구로 셀 안쪽 여백을 보완하는 편집을 수행했다. 기존 `CombatFire.png`의 화염·연기 소재도 함께 사용한다.
- **이미지와 움직임의 조합:** `ImpactArt.cs`가 셀 UV와 인접 프레임 보간을 담당한다. `ImpactChoreography.cs`와 `ImpactParticles.cs`가 무기 궤적, 지면 잔광, 파편, 연기와 준비 동작을 조합한다. 같은 속성이나 패턴의 다른 기술에도 공통 소재를 재사용한다. **모든 기술에 개별 전용 그림을 제작한 것은 아니다.**
- **접촉 시점:** `SkillVfxTiming.cs`가 해당 투사체의 이동 시간을 정하고, `BattleDirector.Execute`는 이동이 끝난 뒤 기존 해석기로 효과를 적용한다. 피해량·회복량 숫자와 접촉 연출은 이 적용 시점에 나타나도록 연결했다. 논리 격자 이동, 피해 공식, 비용 규칙을 새로 정의하지 않는다.
- **범위 공격:** 여러 실제 대상이 있으면 조준 칸에 큰 중심 효과를 한 번 표시한다. 중심 `CombatEffect`는 `Recipient=null`인 시각 전용 객체로 두어 Time Stop/Cage 등 기존 고유 패턴도 한 번 유지한다. 각 실제 대상에는 작은 접촉 효과와 개별 숫자를 남긴다. 기존 대상 판정과 피해·회복 결과는 유지한다.
- **궁극기:** `UltimateSkillCue.cs`가 명령 영역에 시전자 초상화와 기술명을 짧게 표시한다. 겹치는 직계 행동 안내 문구만 일시적으로 숨겼다가 복원하며, 입력을 가로채지 않고 모달이 열리면 표시를 숨긴다. `ShieldFinale`는 공통 원화 분기로 대체하지 않아 기존 방패 고유 문양과 새 화염층을 함께 유지한다. 전체 화면 컷인이나 카메라 전환을 추가한 방식은 아니다.
- **효과음:** `SkillAudioPalette.cs`가 검격, 화염, 얼음, 번개, 회복, 궁극기 6계열의 준비/접촉 소리, 총 12종을 로컬에서 합성한다. **녹음 음원이나 다운로드한 효과음이 아니다.** `BattleAudio.cs`가 필요한 클립을 생성·재사용하며 효과음 설정, 중복 재생 간격, 동시 재생 수와 출력 감쇠를 적용한다.

## 축소 효과와 상태 보존

기존 `ReducedEffects` 설정은 이미지 소재의 식별성을 남기면서 파편·발광·중첩 횟수와 불투명도를 줄인다. 예를 들어 유성 수, 검격 겹침 수, 회복 입자 수를 줄이고 궁극기 알림의 이동을 생략한다. 효과음 출력도 낮춘다. 이 설정 때문에 대상, 피해·회복량, 기술 비용이 바뀌어서는 안 된다.

이번 변경은 표현 계층의 시간 순서와 연출을 다룬다. Core 규칙과 저장 형식 변경, 전투 RNG 소비 추가, 전역 카메라 줌/흔들림이나 `Time.timeScale` 조절은 포함하지 않는다. 로컬 음향 합성은 자체 결정적 노이즈 상태를 사용한다. 상태 보존을 확인한 실행 범위와 증거는 아래 검증 기록에 구분했다.

풀은 `ImpactPool.Capacity` 안에서 효과 객체를 재사용하고, 소유 메시와 재질을 해제하도록 구성했다. 준비·이동·축소 접촉 상태가 다음 기술에 남지 않는지, 연속 사용과 재시작 후 리소스가 정리되는지는 별도 검수 항목이다.

## 실제 기술 비교

검수는 아래 기존 Content 기술을 `BattleDirector.Execute`로 실행한다. 임의의 가짜 기술이나 정적인 시안 이미지를 실제 플레이 캡처로 사용하지 않는다.

| 분류 | 실제 기술 | Content 에셋 |
| --- | --- | --- |
| 검기 | 크레스 — 마신검 (`cless.0`) | `Assets/TalesTactics/Content/Skills/cless.0.asset` |
| 화염·유성 | 제이드 — 메테오 스톰 (`jade.6`) | `Assets/TalesTactics/Content/Skills/jade.6.asset` |
| 얼음 | 제이드 — 앱솔루트 (`jade.5`) | `Assets/TalesTactics/Content/Skills/jade.5.asset` |
| 번개 | 제이드 — 썬더 블레이드 (`jade.3`) | `Assets/TalesTactics/Content/Skills/jade.3.asset` |
| 회복 | 민트 — 퍼스트 에이드 (`mint.0`) | `Assets/TalesTactics/Content/Skills/mint.0.asset` |
| 궁극기 | 크레스 — 명공참상검 (`cless.ultimate`) | `Assets/TalesTactics/Content/Skills/cless.ultimate.asset` |

`VfxUpgradeReview.cs`는 기존 5화 전장, 아군 6명, 기본 줌, 1920×1080에서 일반 효과 6회와 축소 효과 6회를 같은 보드/풀에서 연속 실행한다. 각 시퀀스 전후의 아군 수와 실제 모델 수, HP 변화, 격자 위치, 카메라 크기, 풀 사용량, 효과 종료 상태를 기록한다. 기술이 보이는 위치에서 사용되도록 런타임의 배치·레벨·HP·MP·게이지를 준비하고 반격/임무 이벤트를 끄므로, 캠페인 자연 진행이나 밸런스 테스트를 대신하지 않는다.

현재 검수기는 전투를 시작하기 전에 타이틀을 닫고, 각 촬영 직전에 `Hud.TitleOpen`이 false인지 검사한다. 전체 결과와 시퀀스별 `titleClosed`를 기록하며 타이틀이 다시 열리면 촬영을 중단한다. contact/follow 표본은 실제 HP 변화 또는 `CombatEffect` 생성으로 감지한 접촉 프레임을 기준으로 잡는다.

각 실행은 새로운 격리 저장 경로를 사용한다. 초기 저장과 화면 크기 설정을 마친 뒤 Windows에서는 2초간 초기화가 안정되기를 기다려 격리 저장의 기준 SHA256을 기록한다. 원래 사용자 campaign/settings 파일은 초기화 전부터 감시하고, 이후 기술 실행 동안 사용자/격리 저장의 변화를 비교한다. 기존 사용자 저장을 캡처용 상태로 덮어쓰지 않는다. 검수 중 별도 실행 중인 게임에서 저장하면 외부 변경도 감지될 수 있다.

**캡처의 30fps는 `Time.captureFramerate=30`으로 정한 영상 표본 간격이다. 실제 처리 성능, 유지 프레임률 또는 GPU/CPU 벤치마크 수치가 아니다.** 실행 방법, 출력 파일과 개발 빌드 조건은 [REVIEW_WORKFLOW.md](REVIEW_WORKFLOW.md)를 따른다.

## 검증 상태

| 항목 | 현재 기록 |
| --- | --- |
| 변경 전 실제 촬영 | [before-result.json](Actual/before-result.json), 원본 `Logs/VfxUpgrade/before-visible`: 12개 시퀀스, 오류 0개. 실행/격자 유지/정리/타이틀 닫힘 true, 아군/모델 전후 6개, 기본 줌 9.9297 유지, 일반/축소 HP 변화 6종 일치, 사용자/격리 저장 불변. |
| 최종 변경 후 실제 촬영 | [after-result.json](Actual/after-result.json), 원본 `Logs/VfxUpgrade/after-final`: 12개 시퀀스, 오류 0개. 위와 동일한 정리·상태 보존 검사 통과. 초기 `after-visible` 대신 마지막 범위 효과·궁극기 수정까지 반영한 자료를 비교 페이지에 사용함. |
| EditMode | [editmode-final.json](Validation/editmode-final.json): 358/358 통과, 29.94초. 마지막 표현 코드 보완 전 실행이며 이후 Core 변경 없음. |
| 1차 전체 PlayMode | [playmode-first.json](Validation/playmode-first.json): 183/184 통과. 이후 궁극기 구분 검사를 보완하고 2차 전체 실행을 수행함. |
| 2차 전체 PlayMode | [playmode-second.json](Validation/playmode-second.json): 184/185 통과, 390.38초. 실패는 `CatalogProfilesRenderDistinctFinalesWithoutChangingHealth`의 키사라/시온 궁극기 형상 구분 검사. |
| 실패 항목 수정 후 재검사 | [finales-final.json](Validation/finales-final.json): 위 검사 1/1 통과, 4.89초. `ShieldFinale`의 방패 고유 문양을 보존하도록 수정했으며, **단일 전체 실행 185/185 통과 기록은 없음**. |
| Windows 일반 빌드 | `Builds/VfxUpgradeWindows/build-result.txt`: Succeeded, 오류 0개, 경고 4개, 70.05초. |
| Windows 검수용 빌드 | 초기 빌드 Succeeded, 오류 0개, 경고 2개, 30.79초. 검수기 보완 후 최종 `Builds/VfxUpgradeReview/build-result.txt`: Succeeded, 오류 0개, 경고 3개, 10.44초. |
| Windows 일반 시작 검사 | `Logs/VfxUpgrade/windows-startup.json`: 87.20초 유지, 응답 true, 로그 오류 0개, 사용자 저장/설정 8파일 SHA256 불변, 기존 게임 실행 유지. 숨김 시작·로그 검사이며 native 화면 캡처 승인 시간 초과로 일반 타이틀의 수동 시각 검수는 수행하지 못함. |
| Windows 최종 검수 실행 | `Builds/VfxUpgradeReview/VfxUpgrade/c07bbc17a23941188f5ca72ab24be170/result.json`: 1920×1080, 12개 시퀀스, 오류 0개, 실제 PNG 48개. 모든 실행/격자/정리/타이틀 닫힘 검사, 아군/모델 전후 6개, 일반/축소 HP 변화 일치, 사용자/격리 저장 불변을 확인함. |

최초 촬영은 타이틀 오버레이로 시각 비교 자료가 무효였고, 검수기가 지형·캐릭터 공유 재질까지 VFX 소유로 수집해 마지막 해제 검사에 오탐이 있었다. 타이틀 닫힘 검사, 실제 접촉 감지, `ImpactParticles` 하위만의 소유 리소스 검사를 적용해 위 `before-visible` 기준 자료를 다시 촬영했으며 최초 이미지는 비교에 사용하지 않는다.

Windows 첫 검수 실행도 12개 기술의 연출·HP·격자·모델·정리 검사는 완료했으나, 창 크기 초기화가 격리 설정을 자동 저장해 `isolatedFilesUnchanged=false`로 실패했다(사용자 파일은 불변). 격리 기준 스냅샷을 화면 설정과 2초 안정화 뒤로 옮기고 종료 직전까지 `runInBackground`를 유지하도록 검수기를 보완한 뒤 위 최종 실행에서 저장 불변까지 통과했다.

저장소 사본: [Windows 최종 실행](Validation/windows-final.json), [일반 시작](Validation/windows-startup.json), [저장·종료·셰이더 확인](Validation/windows-checks.json), [빌드 코드 분리](Validation/build-isolation.json), [소스 50개 비교](Validation/source-parity.json). 최종 검수 빌드 경고는 Pipeline 런타임 설정 부재, 빌드 호출 시 임포트/후처리 코드 갱신 안내, `DEVELOPMENT_BUILD` 조건의 사용 중단 안내다. 마지막 변경은 검수 런타임 코드이며 빌드 메소드·임포터 변경은 없다.

표의 `Logs/`와 `Builds/` 원본 경로는 `D:/TalesTacticsHelpGuide/` 기준이다. 저장소의 `Actual/`과 `Validation/`에는 비교 자료와 테스트 결과 사본을 보관한다.

### 추가한 집중 검사

- `VfxUpgradeSceneTests`: 대표 6종 × 일반/축소 효과의 순차 풀 재사용, 메시 좌표, 종료, 전투 상태/RNG/timeScale 불변과 소유 리소스 해제.
- `VfxArtSceneTests`: 아틀라스 투명/가시 픽셀, 메시 UV, 축소 시 이미지 유지 및 형상 감소, 재사용 시 이전 준비/이동/접촉 상태 제거, 풀 한도와 소유 리소스 해제.
- `SkillContactSceneTests`: 투사체 도착 후 효과 적용과 비용/숫자 단일 처리, 범위 공격 중심 효과 한 번 및 대상별 접촉 유지, 효과음 설정/중첩/음악/RNG 보존.
- `UltimateCueSceneTests`: 궁극기 알림의 명령 영역 배치, 입력 통과와 재시작 시 정리.

## 알려진 한계

- 기존 머리 위 기술명과 선택 유닛 HP 라벨이 일부 동작에서 겹친다. 하단의 직전 기술 메시지도 새 동작이 끝날 때까지 남을 수 있다. 새 궁극기 카드 뒤 행동 안내 문구가 겹치던 문제는 수정했지만 이 두 기존 표시 문제까지 해결한 것은 아니다.
- 비교 영상은 실제 Unity 화면의 무음 캡처다. 효과음은 합성 및 재생 제어 검사와 별도로 실제 청취 평가가 필요하며, 영상으로 청각 품질을 입증하지 않는다.
- 일반 Windows 실행본의 타이틀 화면을 수동 검수한 기록은 없다. 별도 검수 플레이어가 만든 실제 전장 PNG 48개와 일반 실행본의 숨김 시작·로그 확인을 구분한다.
- 대표 기술·고정 배치 검수와 고정 30fps 촬영은 모든 기술의 독자적인 미술 완성도, 전체 캠페인 자연 진행 또는 기기 성능을 보장하지 않는다.

## 이미지 제작 출처 기록

원본 생성·여백 보완 프롬프트, 채택 파일과 생성 결과 식별자, 실제 출력 크기 및 제작 한계는 [ART_PROVENANCE.md](ART_PROVENANCE.md)에 기록했다. 이 원화 파일과 생성 결과는 실제 플레이 캡처와 구분한다.
