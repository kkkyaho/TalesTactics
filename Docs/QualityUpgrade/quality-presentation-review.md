# 실제 렌더·모션 검수 — 2026-10-31

새 `QualityPresentationReview`의 실행 계약과 확보한 후보 증거를 정리한다. 실제 production 원화103시트 적용 후 전체 **EditMode435/435(30.03초)**와 **PlayMode247/247(608.92초)**가 통과했다. 앞선 크기 검사 실패와0건 실행은 아래에 보존한다. 최종 Windows 리뷰 플레이어의 실제 아군 기술176회·자동 캠페인20화·대표 성능20회도 통과했다. **새 일반 플레이어20초 응답·정상 종료0·타입 격리·설정 복원 후 최종 저장 일치**까지 확인했고 후보 fixture와 구분한다. 구현과 열거된 검증은 완료했으며 버전 관리 반영은 develop 이력에서 확인한다. [Windows 파생 요약](Validation/windows-final-validation-summary.json). [최종 EditMode](Validation/full-editmode-after-art.json), [최종 전체 PlayMode](Validation/full-playmode-after-art.json), [현재 상태](README.md).

전용무기 크기 검사는 [native 픽셀1,404쌍 분석](Validation/signature-visible-scale-audit.json)에 따라 모든 alpha>128 픽셀의 높이/실제 PPU를 사용한다. 분리된 불투명 무기 조각도 포함하며 허용 범위0.65~1.65와 원화는 유지했다. 여백·빈 프레임·분리 조각·진짜2배 확대에 대한 합성 회귀2개를 추가했다. 자동 크기 검사는 해부학적 몸 비율이나 실제 연속 동작의 시각 승인이 아니다.

첫 사후 PlayMode의 [246/247·624.07초 실패 기록](Validation/full-playmode-after-art-attempt1-summary.json)은 **추출 요약이며 전체 raw 보고서가 아니다**. 원문은 후속0건 실행이 같은 파일을 덮어써 보관하지 못했고 당시 확인한 SHA·크기·단일 실패만 남겼다. [HQ native 분석](Validation/pose-visible-scale-audit.json)에서 실제 Unity geometry와 PNG/meta 해시를 고정하고 10명160포즈×5알파 기준800값을 재계산했다. 사각형 높이는41개가 범위를 벗어나지만 alpha≥16/64/128/129/200 높이는 모두 기존0.8~1.6 안이었다. 원화·PPU는 변경하지 않았으며 이 수치는 Unity 테스트 성공이나 전체 동작의 시각 승인이 아니다.

HQ 포즈 검사 보완 후 [표적1/1(8.21초)](Validation/pose-playmode-recheck.json), [전체247/247(608.92초)](Validation/full-playmode-after-art.json)을 실제 실행해 통과했다. 사이의 [0건 시도](Validation/playmode-empty-cache-attempt.json)는 성공 수치에 포함하지 않는다. [빈 UTF 캐시 초기화 기록](Validation/player-test-cache-reset.json)은 Editor 중지 상태에서 메모리 필드만 초기화했고 패키지·프로젝트 파일/설정을 수정하지 않았음을 명시한다. 전체 통과의 실패·건너뜀·미결정은0이며 Windows 결과와 구분한다.

## 최종 Windows 실행

[리뷰 빌드](Validation/windows-review-build-final.txt)는44.729초·0오류/3경고, [일반 빌드](Validation/windows-normal-build-final.txt)는61.394초·0오류/2경고로 성공했다. 기존 Inspect는 역사적 결과로 보존하며, [최종 일반 시작](Validation/windows-normal-startup-final.json)에서 새 DLL을 다시 검사해 review/editor/test 타입·참조0과 Director의 review bootstrap 미호출을 확인했다.

[Roster176/176](Validation/windows-roster-final.json)은10명·88기술에 각각 일반/축소1회씩 실제 Director 경로를 실행했다. 모든 행에서 접점 전 상태 불변, 단일 판정, 독립 Resolver와 state hash/격자/RNG 일치,6모델 및 정리를 확인했다. `profileSeen`은170행이며 크레스0·제이드3/6의6행은 별도 signature 경로라 해당 공통 profile 검사의 대상이 아니다. 사용자/격리 저장은 불변, 오류0이며1920×1080 접점 PNG176장이 존재한다. 시뮬레이션30fps는 성능 FPS가 아니다.

[Campaign20화](Validation/windows-campaign-final.json)는46,057검사·175저장/재로드를 통과했다. 전후 대사94/91줄, 구매24건·7,320G, 적 기술58회·보스 준비36회·보스 기술20회를 관측했다. 모든20화에서 보상·후일담 저장/재로드·중복보상 방지·다음 화와 영구 상태 hash 왕복을 확인했다. Standard·고정 구매 정책·timeScale4이며 reactions/autoTiming/missionEvents/CT/utilityEnemyAI는false다. 일반 적 coroutine과 공개 Director/UI를 사용한 자동 검수이며 수동 완주·개인 이야기·밸런스 검증으로 세지 않는다.

[Performance20/20](Validation/windows-performance-final.json)은6전장 모델과 별도 표현 배우로10대표 기술×일반/축소를 워밍업한 뒤 PNG 없이 측정했다. RTX3060·D3D11·1920×1080, VSync0/targetFrameRate−1에서29,007표본의 평균1.657ms/p95 2.189ms/p99 2.630ms/최대72.829ms를 기록했다. 짧은 uncapped 리뷰 플레이어의 wall-frame 관측이며 모든182기술·순수GPU·일반 출시 빌드·장시간 누수/다른PC 성능을 보장하지 않는다.

[대표 실제 캡처6장](Evidence/actual-capture-manifest.json)은 원본 해시를 보관했다. 첫 접점 화면이며 최대 효과나 전프레임 검수가 아니다. 조밀한 훈련 배치에서 이름/HP가 겹치는 한계를 유지한다. 원본 player JSON3개와 build txt는 바이트 그대로 보존했고 [작은 wrapper 요약](Validation/windows-final-validation-summary.json)에 로컬 원본 경로·SHA·4,322개 저장 스냅샷 항목의 일치를 기록했다. 수천 개 개별 파일명은 다시 복사하지 않는다.

최종 [일반 시작](Validation/windows-normal-startup-final.json)은20초 응답과 정상 종료0, 새 DLL의 review/editor/test 제외를 확인했다. `settings.json`의 `WindowMaximized` True→False, 없던 `SeenContextHints`→0만 바뀌었고 허용 범위·해시·잠금 확인 뒤 이번 실행의 원본 백업으로 복원해 최종 전체 저장이 일치했다. 실행 중 무변경이나0복원으로 표현하지 않는다. 런타임 오류는0이며 DOF/Panini의 stripped 경고4줄은 남아 있다. root가 최신 창의 타이틀을 연속2회 확인했지만 게임 입력은 하지 않았고 별도 타이틀 이미지 파일은 보관하지 않았다.

## 도구별 범위

| 도구 | 확인하는 것 | 보장하지 않는 것 |
|---|---|---|
| `SkillRosterVfxReview` | 아군88기술×일반/축소176회 실제 Director 실행, 독립 Resolver·단일 판정·MP/HP·격자/RNG·6모델·저장 보존 | 적 AI 전체, 전용무기 장착 전수, 모든 기술182개의 실제 전투 실행 |
| `QualityPresentationReview` vfx | 고유 ID182개×일반/축소364회, 준비·이동·접점의 실제 렌더와 기하, 전장6명과 별도 배우1명 | 수동 배치 표현 fixture이므로 실제 피해 판정·다중 피격·음향·AI 검증 아님 |
| motion | 실제 카탈로그의 아군·보스·일반 적/변이·전용무기에 대해 존재하는 공격/물리기/시전기/궁극기×4방향 | 30fps 표본이며 미관측 네이티브 포즈를 모두 검토했다고 주장하지 않음 |
| performance | 효과 유형별 대표 기술의 일반/축소를 워밍업한 뒤 캡처 없이 프레임 간격·메모리·GC·효과 peak 수집 | Editor 결과는 일반 Windows 플레이어나 순수 GPU 성능이 아님 |
| 기존 방향·장비 검수기 | `CrossoverPlayerReview`의 보스 상태창/전장4방향, `SignatureWeaponReview`의 장착·UI, `SdMotionDirectionReview`의 크레스12동작 | 서로 다른 fixture이며 새 전체 후보 연속성이나 최종 모든 게임 화면을 대체하지 않음 |

`boundary-device`는 공격 클립이 없는 고정 임무 오브젝트라 제외한다. 없는 물리기·시전기·궁극기를 새로 만들지 않는다. `expectedRuns`, `completedRuns`, 행별 owner/equipment/skill/facing으로 실제 범위를 확인한다. 검수기는 `UNITY_EDITOR || TALESTACTICS_REVIEW`에만 포함하며 일반 빌드에서 제외 여부를 따로 검사한다.

## 확보한 실행의 파생 요약

| 실행 | 자동 결과 | 요약 |
|---|---|---|
| 최종 후보 A: cless,mint,farah,tear | 96/96,1,077PNG, 연속16행 | [result](Validation/final-candidate-motion-group-a-summary.json) |
| 최종 후보 B: jade,alphen,shionne,velvet | 104/104,1,433PNG, 연속20행 | [result](Validation/final-candidate-motion-group-b-summary.json) |
| 크레스 크기 후속 후보 | 24/24,210PNG, 연속4행 | [result](Validation/final-candidate-motion-cless-scale-summary.json) |
| 티아 크기 후속 후보 | 24/24,271PNG, 연속4행 | [result](Validation/final-candidate-motion-tear-scale-summary.json) |
| 초기 적 후보 전체 | 224행/348PNG 수집, 오리진 화면 밖4행으로 **실패** | [실패 result](Validation/candidate-enemy-motion-initial-failed-summary.json) |
| 오리진 카메라 후속 | 12/12,12PNG, 연속0행 | [재검수 result](Validation/candidate-origin-camera-recheck-summary.json) |
| 원화 교체 전 schema2 VFX | 364/364,1,340PNG | [result](Validation/preart-vfx-schema2-summary.json) |

이들은 별도 실행이다. A/B 이후 PPU 보완과 크레스·티아 재검수를 구분하고 합계를 단일 전체 실행처럼 쓰지 않는다. 각 자동 pass는 실행·상태·화면/기하 검사를 뜻한다. `visualApproval`은 자동 보고서가 시각 품질을 승인하지 않는다고 명시한다. 위 파일은 핵심 결과·실패행을 추출한 **파생 요약이며 전체 원본 JSON이 아니다**. [출처 명세](Validation/candidate-fixture-summaries.json)에 로컬 원본 경로·SHA256·크기와 생략 범위를 남겼다. 예를 들어 [그룹 B 전체 원본](../../Logs/QualityUpgrade20261031/CandidateMotion/final-b/result.json)은 이 작업 PC의 로컬 링크이며 새 클론에는 없다. 원본 result/timeline/PNG는 변경하지 않았고 대량 사본을 저장소에 추가하지 않는다.

보관 범위: 위 최신 vfx2·CandidateMotion 원본은 유지한다. 이전 smoke/smoke2/smoke3·구 vfx 네 실행의 오래된 PNG676개(989,254,213바이트)는 디스크 확보를 위해 정리했고 원시 결과 JSON은 보존했다. 따라서 과거 보고서에 열거된 이미지가 모두 남아 있다는 뜻은 아니다. [로컬 정리 명세](../../Logs/QualityUpgrade20261031/obsolete-capture-cleanup.json)의 SHA256은 `c3c3d2e0ffa2f4021517275c77264a74f6b63f43d35f341a55420afbc1fde2f5`다.

## 저장 격리와 후보의 한계

검수 Windows 진입은 `ReviewStorageBootstrap`이 Director의 최초 저장 접근 전에 새32자리 GUID와 지정 검수 루트를 확인해 격리 경로를 설정한다. `SkillRosterVfxReview`도 같은 경로를 사용한다. `QualityPresentationReview`는 standalone에서 창 설정이 안정되도록 2초 기다린 후 격리 저장의 불변 기준을 잡는다. 일반 빌드와 Editor의 기본 동작은 이 진입점으로 바꾸지 않는다.

Editor 전용 `BeginEditorCandidates`는 선택 파일의 source/candidate 해시·GUID/local ID·이름과 측정 geometry를 확인하고 `CharacterData`, `EquipmentData`, `EquipmentAppearance`만 메모리에 복제한다. 전용무기의 Original/Equipped 참조를 함께 치환한다. **전장6명은 당시 원본이고 별도 배우1명만 후보**다. production PNG/meta·manifest·Content를 교체하지 않는다. 종료 시 원본 Content JSON/dirty 상태와 파일 해시를 확인하고 소유 복제본을 정리한다. 결과의 `candidateOriginalsUnchanged`, `candidateCleanupComplete`, clones 생성/파괴 수가 근거다.

선택 JSON은 적용 전 입력의 해시를 고정한 자료다. production 적용 후 그 옛 source 해시를 현재와 일치시키려고 기록을 덮어쓰지 않는다. 사후 검증은 적용된 에셋을 사용하는 일반 검수 진입점에서 수행한다. 이전 후보 결과는 사후 Sprite/장비 비교 테스트까지 통과했다는 뜻이 아니다.

## 실행 방법

Editor 실행은 Battle 씬의 새 Play Mode에서 시작하며, 출력은 새 빈 폴더여야 한다. 사용자 저장 경로와 하위는 거절한다. 실행이 끝나면 Play Mode를 종료하므로 다음 검수 전에 다시 시작한다.

```csharp
string output = System.IO.Path.Combine("C:/dev/TalesTactics/Logs/QualityUpgrade20261031/Presentation", "motion-" + System.Guid.NewGuid().ToString("N"));
TalesTactics.QualityPresentationReview.BeginEditor(output, "motion", "cless,mint,farah,tear", true);
return output;
```

`mode`는 `vfx`, `motion`, `performance`다. 큰 모션 검수는 owner filter로 나눈다. **적용 전** 후보 검수에는 같은 인수와 선택 파일을 받는 `BeginEditorCandidates(output, "Docs/QualityUpgrade/selected-revisions.json", "motion", owners, true)`를 사용했다. 새로운 현재 source 해시가 필요한 재검수는 별도의 명시된 선택 기록을 만들어야 하며 과거 고정 자료를 수정하지 않는다.

```csharp
string output = System.IO.Path.Combine("C:/dev/TalesTactics/Logs/QualityUpgrade20261031/Presentation", "actual-director-" + System.Guid.NewGuid().ToString("N"));
TalesTactics.SkillRosterVfxReview.BeginEditor(output, "after", false, "", "", true, true);
return output;
```

위 Director 검수는 아군176회 접점 촬영이며 별도 presentation fixture와 구분한다. Windows 전용 review 빌드의 인수는 다음과 같다. 일반 사용자용 실행 파일에는 이 진입점이 없어야 한다.

```text
TalesTactics.exe --quality-presentation-review <new32hexguid> vfx -logFile <external-log-path>
TalesTactics.exe --quality-presentation-review <new32hexguid> motion -logFile <external-log-path>
TalesTactics.exe --quality-presentation-review <new32hexguid> performance -logFile <external-log-path>
TalesTactics.exe --skill-roster-vfx-review <new32hexguid> --roster-phase after -logFile <external-log-path>
```

`--quality-owners cless,mint`는 대상을 제한하고 `--quality-no-continuous`는 연속 캡처를 끈다. 결과는 `QualityPresentationReviews/<guid>/` 또는 `SkillRosterVfx/<guid>/` 아래에 생성된다. `Validate-QualityWindows.ps1`은 기본 Hidden이며 명시적 `-InteractiveWindow`일 때만 실제 창을 보인다. 숨김 실행에서 `WaitForEndOfFrame`이 정지한 실패 이력과 창을 보이는 재시도를 구별한다. 기존 프로세스를 종료하거나 사용자 저장을 자동 복원하지 않는다.

Editor 완료 코드는 `SessionState.GetInt("TalesTactics.QualityPresentationReviewExit", -1)`의 -1실행 중/0자동 검사 통과/1실패로 읽는다. 실행0건은 통과가 아니다. 초기 UTF 빈 캐시 문제의 [재연결 증거](Validation/Reconnect20261031)는 역사로 보존하며 패키지·프로젝트 설정을 바꾼 성공으로 표현하지 않는다.

## 캡처와 판정 읽기

- `result.json`: 예상/완료 수, 오류, 사용자/격리 저장·세션·원본 보존, 각 실행의 제한.
- 행별 `timeline.json`: 실제 Sprite·rect/pivot/PPU·방향·phase·화면 포함과 관측/미관측 포즈.
- motion: 연속 행은 `0000.png`부터 실제 프레임을 저장하고 나머지는 대표 접점1장과 phase telemetry를 남긴다. 현재 연속 대상은 크레스0·민트0·알펜/파라 궁극기·제이드0·티아0·시온0·벨벳 궁극기 및 다오스/바르바토스의 선택 동작 Front/Back이다. 후보는 해당 전용무기 Front/Back, 벨벳 기본 궁극기와 알펜 전용 궁극기 all4도 포함한다. 실제 `continuous` 행을 기준으로 읽는다.
- vfx: `prepare.png`, 실제 이동이 있을 때 `travel.png`, 접점 후0.12초 `contact.png`, 같은 접점의6명 전장 `field-contact.png`를 구분한다.

새 도구의 PNG는 960×540 Unity Camera.Render 원본이다. 실제 BattleCamera/URP 컴포넌트를 유지하고 시점·크기·화면비·culling만 일시 변경한 뒤 finally에서 복구한다. 프레이밍은 방향별 실제 클립 정점과 기존 모션의 이동·높이·기울기를 포함한다. 이것이 원화의 스케일 오류 자체를 고치는 것은 아니다. UI 오버레이와 사람의 조작은 포함하지 않는다.

schema1의 단일 접점은 이동 궤적·깊이 설정 증거가 부족했다. schema2는 mesh bounds·정점 알파·재질/셰이더·카메라/URP 깊이·시전자/수신 좌표를 기록한다. `hasInFramePositiveAlphaGeometry`는 화면에 겹치는 양의 정점 알파 기하만 뜻하며 텍스처 알파·가림·최종 픽셀 가독성의 자동 승인이 아니다. 시각 판단은 실제 PNG와 별도 검토 기록을 함께 사용한다.

한 실행은 최대2500PNG이며 초과하면 부분 결과와 실패를 남긴다. performance는 PNG0장이다. 무필터 성능 실행은 대표20회의 일반/축소 표본이며 owner filter를 주면 표본 범위가 달라지므로 결과를 섞지 않는다. `Time.captureFramerate=0`, VSync0, targetFrameRate−1에서 워밍업 뒤 측정하며 전환 구간은 제외한다. 평균/p50/p95/p99/max 벽시계 간격·Unity/managed 메모리·GC·효과 peak에는 OS·계측 비용이 포함된다. 장시간 누수·다른 PC 성능·음향·수동 캠페인 완주는 별도다.
