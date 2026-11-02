# 캐릭터와 전장의 입체감 개선

2026-11-02 `develop` 작업이다. 발이 지면을 딛고 몸이 그 위로 서 있는 인상을 강화하기 위해 **발 접촉 그림자에 짧은 몸 실루엣 그림자를 더하고, 캐릭터와 바닥의 광원 방향·명암을 맞췄다.** 기존 캐릭터 원화와 모션, 전투 격자·판정·저장 형식을 유지하는 표현 계층의 변경이다.

최종 Editor와 Windows 각각 전장 **90/90**·HUD **32/32** 상태 검수, 실제 전체 **EditMode452/452 · 34.07초**, 일반·검수 Windows 빌드를 확인했다. 전체 PlayMode **269/270**과 오디오 복구 후 별도 **1/1** 결과는 아래에 분리해 기록한다. [실제 화면 전후 비교](comparison.html).

구현과 이 문서에 열거한 검증을 완료했다. 버전 관리 반영 상태는 `develop` 이력에서 확인한다. 첫 빌드 오류와 검은 화면 캡처는 성공에 합산하지 않고 아래에 보존한다. 초기 세션 연결 해제 상태는 [당시 보존 기록](Validation/handoff-state.json)에 남기며, 현재 결과와 구분한다. `main` 반영과 공개 릴리즈는 이번 작업의 범위가 아니다.

## 적용한 표현

| 영역 | 구현과 목적 |
|---|---|
| 발과 몸의 그림자 | 기존 두 겹 발 접촉 그림자를 유지하고, 현재 표시 중인 스프라이트의 정점·UV·alpha를 사용하는 짧은 몸 그림자를 추가했다. 무기·옷의 실루엣과 원화의 투명 부분을 함께 반영한다. |
| 모션과 카메라 | 스프라이트 교체, 좌우 반전, 실제 크기와 회전, 들림을 투영에 반영한다. 들린 몸의 그림자는 지면에 남으며 광원 반대 방향으로 이동하고 농도가 낮아진다. 카메라를 돌려도 월드 광원 방향은 고정된다. |
| 지형 경계 | 발 아래 마른 타일을 기준으로 같은 높이의 육지에만 삼각형을 잘라 그린다. 물·맵 밖·다른 높이로 이어지는 경계에서 그림자를 끊고, 발보다 높은 면이나 수면만 있으면 몸 그림자를 숨긴다. |
| 몸의 명암 | 장별 주광·반사광을 유지하면서 환경광 가중치를 `.78`, 약한 측면·발 부근 명암 계수를 `.065`로 설정했다. 원화에 그려진 얼굴·옷·갑옷 명암을 보존하며 큰 밝기 흐름을 보완한다. 행동 종료의 채도·밝기 표시는 별도 속성을 유지한다. |
| 바닥 대비 | 바닥 재질의 세부 대비를 `.76`으로 낮춰 인물과 그림자가 먼저 읽히도록 조정했다. 바닥 PNG를 다시 그리거나 타일 구조를 변경하지 않았다. |
| 광원 일치 | 지형과 캐릭터가 같은 월드 주광 방향 `(-.45, .85, -.35)`를 사용하며 몸 그림자는 그 수평 반대 방향으로 뻗는다. 장별 환경색은 기존 팔레트를 따른다. |

`GroundedUnitVisual`이 기존 `CharacterMotion` 뒤에 몸 그림자를 갱신한다. 구조·호위 민간인도 같은 투영을 사용한다. 표시를 숨기거나 유닛이 사망하면 몸 그림자가 함께 숨겨지고, 전장을 정리할 때 소유 메시와 전장 공유 재질을 해제한다. 새 충돌체나 전투 난수를 추가하지 않는다.

최종 몸 그림자의 투영 배율은 높이의 `.38`, 폭은 `.88`, 기본 alpha는 `.22`이다. 최초 실제 화면에서 `.28`/`.19`가 약하게 보여 길이·농도를 보강했다. 그림자는 위험 범위(-4), 접촉 그림자(-3), 선택(-2)보다 아래(-5)에 그린다. 전장마다 재질 하나를 공유하고 유닛마다 메시 하나와 최대128개 Sprite geometry 캐시를 유지한다. 작업 목록·배열은 재사용하지만 새 Sprite의 최초 캐시와 퇴출 후 재생성에는 할당이 생긴다.

이번 구현은 기존 PNG·atlas 참조·Sprite 식별자를 교체하지 않는다. AI 목업은 방향을 정하는 참고였으며, 목업의 재해석된 인물이나 UI 이미지를 게임 원화로 사용한 것은 아니다.

## 표적 검사의 근거와 범위

[CharacterDepthTests.cs](../../Assets/TalesTactics/PlayModeTests/CharacterDepthTests.cs)의 신규 5개를 실제 Unity PlayMode에서 실행했다. 앞의 3개는 제품의 `ProjectedUnitShadow`와 셰이더를 `CommandBuffer.DrawRenderer`로 256×256 RenderTexture에 그린 뒤 픽셀을 읽는다. 나머지 2개는 실제 `TestBattle` 장면과 Director를 사용한다.

1. **원화 alpha와 프레임:** 비대칭 모양과 내부 구멍을 가진 합성 원화를 같은 크기의 불투명 원화와 비교한다. 실제 구멍 위치가 대조군에서는 채워지고 원본에서는 투명한지, 같은 atlas의 다른 rect로 바꾸면 실루엣이 달라지는지, 좌우 반전과 원복이 반영되는지 확인한다.
2. **크기·회전·들림:** 크기 확대, roll, 카메라 yaw에 따라 실제 투영 픽셀이 달라지는지 확인한다. 실제 카메라 방향에 해당하는 yaw **-39/51/141/231도**에서는 기본 방향 대비 면적 **65% 초과**, 각 방향의 alpha 구멍과 좌우 반전을 확인한다. 들림은 픽셀 중심의 이동·농도 감소와 메시의 지면 유지로 검사한다.
3. **수면·단차·맵 경계:** 먼저 그림자가 서 있는 타일 밖으로 충분히 뻗는 대조군을 확인한다. 이웃을 물·높은 지형·맵 밖으로 바꾼 세 조건에서 실제 렌더가 받는 면의 경계에서 잘리는지, 발 아래 유효 면이 없을 때 숨겨지는지 검사한다.
4. **전투 비간섭:** 실제 전장에서 네 방향 카메라 회전과 피격 포즈만 실행한다. 선택 유닛과 명령 상태, `CheckpointUnit` 상태, 지형·점유, 캠페인, Resolver 난수 횟수와 Unity 난수 상태가 그대로인지 확인한다. 화면 안 이동 가능 타일 전체의 raycast 결과도 확인한다.
5. **표시 수명과 정리:** 실제 배우의 renderer 숨김·복구, HP 0·복구에 몸 그림자가 따라가는지 확인한다. 전장 재시작 후 투영 컴포넌트·메시·공유 재질이 해제되는지도 검사한다.

투명한 RenderTexture의 alpha는 표준 blending에 따라 출력 alpha의 제곱으로 저장된다. 낮은 농도의 들림 그림자를 잘못 빈 화면으로 판단하지 않도록 가시 면적은 alpha 1 byte 초과를 사용한다. 경계 밖 픽셀 검사는 2 byte 초과, 내부 구멍 검사는 불투명 대조군과의 별도 비교를 사용한다. 이 계측 허용치는 제품 그림자의 농도를 올리는 대신 읽기 방식에 맞춘 것이다.

독립 검토에서 기본 카메라의 수평축과 빛 방향이 거의 평행하면 평면 스프라이트의 투영 면적이 선처럼 줄어드는 문제가 발견됐다. 전체 실루엣의 폭 축에 빛과 수직인 성분을 보강하고 실제 카메라 4방향 회귀를 추가해 표적 **5/5 · 15.04초**를 통과했다. 이는 마지막 표시 순서·길이·농도 조정 전 결과이며 최종 전체 PlayMode와 합산하지 않는다. [초기 표적 결과](Validation/targeted-before-visual-tuning.json).

## 최종 검증 결과

| 검증 | 현재 상태 |
|---|---|
| 신규 표적 PlayMode | 시각 보강 전 **5/5 · 15.04초** |
| 전체 EditMode | **452/452 · 34.07초**, 실패·건너뜀·미결정0. [원본](Validation/full-editmode-final.json) |
| 전체 PlayMode와 오디오 재검사 | 첫 전체 **269/270 · 743.61초**, 음악1건 실패. 동일 소스에서 Editor 오디오 복구 후 해당 검사 **1/1 · 1.54초**. 아래 실패·재검사 근거 참조. 한 번의 전체270/270 실행으로 표현하지 않는다. |
| Editor 실제 전장과 HUD 캡처 | 최종 **90/90**·**32/32**, errors·issues0, 사용자 파일·fixture 캠페인 불변. [전장](Validation/editor-environment-final.json) · [HUD](Validation/editor-hud-final.json) |
| 일반·검수 Windows 빌드 | 일반 **64.8602659초·오류0·경고1**, 검수 **57.0643018초·오류0·경고6**, 두 helper 모두 통과. [일반](Validation/windows-normal-build-final.json) · [검수](Validation/windows-review-build-final.json) |
| 일반·검수 DLL 분리 | 통과. 일반 DLL에 Review/Fixture 타입·호출과 Editor/테스트 참조가 없고, 검수 DLL은 최초 저장 접근 전 격리 진입을 포함한다. [전장 검수 시 검사](Validation/windows-environment-type-isolation.json) · [HUD 검수 시 검사](Validation/windows-hud-type-isolation.json) |
| Windows 실제 전장 캡처 | **90/90**, 오류·issues0, 타일 피킹 **1,276/1,276**, 정상 종료0·강제 종료/정리 요청 없음. [원본](Validation/windows-environment-final.json) · [외부 검증](Validation/windows-environment-validation.json) |
| Windows 실제 HUD 캡처 | **32/32**, 오류·issues0, 정상 종료0·강제 종료/정리 요청 없음. [원본](Validation/windows-hud-final.json) · [외부 검증](Validation/windows-hud-validation.json) |
| Windows 실행 전후 사용자 저장 트리 | 각 실행 전후 **4,797파일·2,296디렉터리** 동일. 최상위 `Player.log`·`Player-prev.log`만 제외하며 기존 실행 게임의 PID·시작 시각·실행 경로도 유지됐다. |
| 작업 전 수정 파일과 최종 설정 | 초기 수정 **12파일**·사용자 저장/설정 **8파일**·검증 대상 소스/메타 **16파일** SHA 일치. Editor 중지·TestBattle clean·background=false·auto-tick true/16ms와 영속 설정 초기값을 확인했다. [최종 대조](Validation/final-state.json) |

환경 검수는 메인 1/11/12/15/16/17/20화와 6인 훈련으로 6개 지형 테마, 네 방향 카메라, 합법 이동, 피격 포즈, 인물 근접과 두 해상도의 명령·기술 UI를 수집하는 기존 `EnvironmentIntegrationReview` 경로를 사용한다. HUD는 2인·6인, 작은 창·큰 창, 카메라 이동·확대와 글자 확대를 다루는 기존 `BattleHudRedesignReview`를 재사용한다.

첫 Editor 환경 검수는 **잠긴 `Player.log`**를 읽으며 중단됐다. 두 검수기는 이제 최상위 모든 파일 중 정확히 `Player.log`·`Player-prev.log`만 제외한다. 알 수 없는 파일·백업·`.tmp`는 검사하고, Windows 래퍼는 하위 트리까지 검사한다. 잠긴 두 로그 허용과 다른 파일의 추가·동일 길이 수정·삭제를 다루는 EditMode4개 회귀를 추가했다. [초기 오류 원본](Validation/initial-review-console.json).

그다음90장 실행은 비활성 `FramedContent`의 낡은 `ContentRect`를 검사해 모든 행에서 글자 경계 오류가 발생했다. 제품 HUD를 바꾸거나 허용치를 늘리지 않고, 기존 HUD 검수와 동일하게 실제 프레임 사각형과 Unit/Commands14px 여백·±2px 허용치를 사용하도록 보완했다. 정상 프레임은 통과하고 HP 글자를 실제 트랙 밖으로 옮기면 실패하는 PlayMode 회귀를 추가했다. [실패 원본](Validation/editor-environment-frame-failure.json). 최종90/90은 이 수정 이후의 별도 실행이다.

전체 PlayMode에서 새 그림자5개와 UI 경계1개를 포함한269개는 통과했고, 기존 `OriginalMusicCatalogPlaysEveryRegisteredTrack`만 재생 샘플0으로 실패했다. [전체 실행 원본](Validation/full-playmode-attempt1.json). 첫 단독 재검사는 Test Framework의 반복 실행 캐시 문제로0개였고 성공으로 세지 않는다. 메모리 캐시를 null로 초기화한 뒤 실제1개를 재실행해 같은 실패를 확인했다. [0개 실행](Validation/audio-empty-cache-attempt.json) · [실제 단독 실패](Validation/audio-rerun-failure.json).

추가 진단에서 게임 프레임은 **1→5366**으로 증가했으나 DSP 시간은 **6511.466666666666**에 고정되고, `isPlaying=true`·listener pause=false·pitch1·실제 음성 상태에서도 샘플0이었다. [시점A](Validation/audio-probe-a.json) · [시점B](Validation/audio-probe-b.json). 현재와 동일한 Stereo/48kHz/1024buffer/32real/512virtual 설정을 [AudioSettings.Reset](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/audiosettings/reset)에 전달하자 성공했고 DSP가 진행했다. [동일 설정 재초기화](Validation/audio-reset.json) · [후속 DSP](Validation/audio-probe-after-reset.json). 제품 오디오 코드·음원·검사 기준·AudioManager 파일은 바꾸지 않았다. 원래 검사를 다시 실행해14곡의 샘플 진행을 확인하는 **1/1 · 1.54초**를 통과했다. [복구 후 원본](Validation/audio-final.json). 전체 재실행 결과와 단독 재검사 결과를 합쳐 단일270/270 실행으로 주장하지 않는다.

## Windows 첫 빌드와 재개 이력

첫 일반 빌드 중 Unity PID44632가 속한 Windows 세션1의 `Disc` 상태와 최종 응답 지연을 확인해 작업을 중단했다. 당시에는 생성된 EXE만으로 성공을 판정하지 않았으며, 사용자 저장·기존 실행 게임·초기 수정 파일의 보존 상태를 남겼다. 사용자 재개 후 세션이 `Active`인 상태에서 같은 빌드의 완료 결과를 확인했다. 첫 보고서의 summary는 **`Succeeded`·오류2·경고4**였고, 오류0을 요구하는 helper는 **`status=Failed`, `passed=false`**로 종료했다. [첫 helper 결과 원본](Validation/windows-normal-build-attempt1.json).

회수한 BuildReport에서 오류2건은 `Call Post-Process callbacks` 단계의 Pipeline 요청 처리 메시지다. 하나는 잘못된 `editor status` 명령에 대한 `No command named 'editor status' is available`, 다른 하나는 `Failed to handle /api/exec request: Main thread operation timed out after 4000ms`다. 이 오류를 지우거나 예외 처리해 통과로 바꾸지 않았다. 경고4건은 Pipeline 런타임 설정 없음, 기존 `DEVELOPMENT_BUILD` 지시문 안내, URP 내부 `DebugOccluder`·`DebugOcclusionTest` 셰이더 stripping이다. 보고 시간 **34680.50초**에는 세션 연결 해제와 응답 지연 구간이 포함되므로 정상 빌드 소요 시간으로 사용하지 않는다.

최초 helper의 `JsonUtility` 출력에는 BuildSummary 속성·step/message·보호 파일 snapshot·별도 Editor 로그의 배열 필드가 누락됐다. 원본 JSON을 덮어쓰지 않고 동일 Build GUID와 출력 경로의 완료된 BuildReport에서 summary 속성과 **32단계·10메시지**를 별도 JSON으로 회수했다. 이 회수는 재빌드가 아니며, 누락된 당시 보호 파일 snapshot 배열과 독립 Editor 로그 배열까지 복원한 것은 아니다. [회수한 보고서와 오류 원문](Validation/windows-normal-report-attempt1.json).

새 실행은 배열 필드도 기록하는 JSON 직렬화를 사용하고, 첫 산출물 폴더를 보존한 채 **`D:/TalesTacticsBuildArchive/CharacterDepthWindows20261102Final`**에 다시 빌드했다. 최종 일반 빌드는64.8602659초·오류0·경고1, 검수 빌드는57.0643018초·오류0·경고6으로 통과했다. 검수 실행본은 **`D:/TalesTacticsBuildArchive/CharacterDepthReview20261102/TalesTactics.exe`**이다. 일반 경고는 Pipeline 런타임 설정 없음이며, 검수의 추가5건은 기존 `FindObjectsByType` 계열 API4건과 `DEVELOPMENT_BUILD` 지시문1건의 사용 중단 안내다. 오류·경고 원문은 최종 BuildReport JSON에 남긴다.

첫 Windows 환경 실행은 검수기 내부에서90행을 작성하고 종료0·저장 불변을 확인했으나, 처음10장이 검은 화면이었다. 외부 래퍼가 첫 PNG의 단색 검사를 실패시켜 이 실행 전체를 시각 검증 성공으로 세지 않았다. [첫 실행 실패 원본](Validation/windows-environment-attempt1.json). 시작 창 크기를1024×768로 바꿔 검수기의 첫1280×720 전환이 실제로 일어나게 하고, 새 실행에서 네이티브 게임창을 한 번 활성화했다. 제품 소스를 바꾸지 않고 다시 촬영한 최종 전장90장은 모두 유효한 PNG로 확인됐고, 이후 HUD32장은 추가 네이티브 활성화 없이 정상 완료됐다. 두 최종 실행 모두 종료0이며 강제 종료나 래퍼의 종료 정리 요청이 없었다. 검은 화면을 보정하거나 이전 이미지로 대체하지 않았다.

두 Windows 실행에서 원래 사용자 저장 트리의4,797파일·2,296디렉터리가 각각 시작 전후 동일했다. 정확히 최상위 `Player.log`와 `Player-prev.log`만 제외하며 다른 로그·알 수 없는 파일·백업·하위 경로는 포함한다. 기존 사용자 게임 PID30016의 실행 정체성도 유지했다. 이 결과는 각 Windows 실행 구간의 보존 근거이며, 작업 전체 동안 테스트 산출물이 전혀 생기지 않았다는 의미는 아니다.

Windows 재개와 빌드·캡처 보완 뒤에도 [검증 대상 소스·메타16개](Validation/source-manifest.json)의 SHA가 같음을 확인해 Unity 테스트를 다시 실행하지 않았다. 빌드 helper와 외부 실행 조건의 보완을 제품 코드 변경으로 세지 않는다. 일반 DLL의 검수 코드 제외 확인과 실제 검수 실행본의90+32상태 실행은 서로 다른 근거이며, 일반 실행본의 수동 플레이를 수행했다는 뜻은 아니다.

## 실제 화면과 최종 보존 확인

최종 Editor7장에 이어 Windows7장을 직접 열어 확인했다. Windows 전장은 `00-episode-01-idle`, `02-episode-01-idle-r180`, `04-episode-01-character-close`, `82-training-six-character-close`, `89-training-six-skill-ui-1024x768`의5장이고, HUD는 `00-command-allies2-1920x1080`과 `31-zoom-text130-allies6-1280x720`이다. 이 표본에서 새 몸 그림자의 사각 배경·카메라 반대 방향의 소실·UI 잘림 등 추가 결함을 발견하지 못했다. 자동122상태 확인과7장 육안 검토를 구분하며 전체 원화 프레임의 미술 승인을 뜻하지 않는다. [시각 검토 범위](Validation/visual-review.md).

대표 적용 후4장은 보정하지 않은 최종 Windows PNG로 보관했고 원본 SHA와 일치한다. [PNG 출처·SHA](Evidence/actual-capture-manifest.json). 6인 확대 전후 비교는 기존 Windows HUD와 최종 Windows HUD의 화면 크기·인원·orthographicSize·글자 배율·viewport·배우 화면 좌표 일치를 확인했다. [동일 비교 조건](Evidence/before-after-camera.json) · [전후 비교](comparison.html).

동적 폰트·검수 CSV2개·임시 background 설정은 이번 작업 시작 시의 바이트 백업으로 복원했다. 최종 대조에서 작업 전 수정12파일과 원래 사용자 저장/설정8파일이 초기 SHA와 같고, 검증 대상 소스·메타16파일도 같았다. 기존 사용자 변경을 HEAD로 되돌리지 않았으며 사용자 게임을 종료하지 않았다. Editor는 Play 중지·`TestBattle` dirty=false·runInBackground=false, auto-tick true/16ms로 복원됐고 영속 auto-tick 값도 초기값(-1, false)과 같다. [최종 상태와 파일별 근거](Validation/final-state.json).

## 표현과 검증의 한계

몸 그림자는 원화 alpha를 바닥에 투영하는 **2.5D 평면 표현**이다. 카메라를 따라 도는 원화에 최소 폭을 보강한 미술적 근사이며, 실제 3D 몸체나 물리적인 체적 그림자를 생성하지 않는다. 다른 높이의 면·벽·물 위로 연속 투영하거나 인접 유닛과 소품에 그림자를 드리우지 않는다. 발 접촉 그림자는 기존 방식을 유지한다.

캐릭터 명암도 스프라이트의 높이·폭과 월드 빛 방향을 이용한 약한 보정이다. 얼굴·갑옷·옷의 표면별 법선이나 움직이는 실제 광원에 반응하는 3D 조명은 아니다. 전체 스프라이트의 투명 여백까지 포함한 좌표를 사용하므로 모든 포즈에서 해부학적 광원 정합성을 보증하지 않는다.

표적5개는 모든 캐릭터·무기·네이티브 프레임을 육안 검수한 결과가 아니다. 현재 90도 단위 카메라를 확인했으며 연속 회전에서 최소 폭 축의 부호 전환을 부드럽게 보간하는 기능은 없다. 상하 반전·음수Y스케일·경사진 수신 지면은 이번 지원/검증 범위가 아니다. 장시간 CPU/GPU 성능, 다른 PC·DPI, 모든 유닛 밀집 배치, 전체 캠페인의 수동 플레이와 일반 실행본의 수동 플레이·공개 릴리즈는 별도 범위다. ManagedChecks나 독립 API 컴파일은 Unity 실행 테스트로 집계하지 않는다.

## 관련 구현

- [몸 실루엣 투영과 지형 clipping](../../Assets/TalesTactics/Runtime/Presentation/ProjectedUnitShadow.cs) · [alpha 그림자 셰이더](../../Assets/TalesTactics/Resources/TalesTactics/ProjectedUnitShadow.shader)
- [모션 뒤 접지 갱신](../../Assets/TalesTactics/Runtime/Presentation/GroundedUnitVisual.cs) · [구조·호위 민간인](../../Assets/TalesTactics/Runtime/Presentation/CrossoverCivilianView.cs)
- [공통 광원과 환경광](../../Assets/TalesTactics/Runtime/Presentation/BattleEnvironmentPalette.cs) · [캐릭터 명암 셰이더](../../Assets/TalesTactics/Resources/TalesTactics/UnitTone.shader)
- [전장 공유 재질과 바닥 대비](../../Assets/TalesTactics/Runtime/Presentation/PixelBattlefield.cs) · [지형 셰이더](../../Assets/TalesTactics/Resources/TalesTactics/TerrainArt/Terrain.shader)
- [이전 전장 환경 통합](../EnvironmentIntegration/README.md) · [이전 바닥과 외곽 개선](../TerrainRefinement/README.md) · [이전 전투 HUD 검증](../BattleHudRedesign/README.md)
