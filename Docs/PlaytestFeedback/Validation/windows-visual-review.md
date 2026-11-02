# Windows 실제 화면 — 표본 시각 검수

2026-11-03 KST. 최종 검수 플레이어 `D:/TalesTacticsBuildArchive/PlaytestFeedbackReview20261102Final3/TalesTactics.exe`가 새로 출력한 PNG를 직접 열어 확인했다. 이전 Editor 캡처를 이번 Windows 실행의 증거로 대신하지 않았다. 원본 6장을 보존한 [Windows 갤러리](../Evidence/Windows/gallery.html)와 [원본·복사본 SHA-256 manifest](../Evidence/Windows/manifest.json)를 제공한다.

## 실행 증거와 범위

| 검사 | 원본 실행 ID | 자동 검증 |
| --- | --- | --- |
| 전장 | `f517cd317b93446085feca7a91a6b6ca` | 90/90 PNG, passed=true, player errors 0, 종료 0, 강제 종료 없음, 사용자 저장 트리 불변 |
| HUD | `504eb11b980e4101b55ee0be8274cded` | 32/32 PNG, passed=true, player errors 0, 종료 0, 강제 종료 없음, 사용자 저장 트리 불변 |

각 원문은 `Logs/PlaytestFeedback20261102/Windows-<전장 ID>/validation.json`과 `WindowsHud-<HUD ID>/validation.json`에서 직접 읽었다. 사용자 저장 트리 비교는 루트의 Unity 실시간 로그 `Player.log`·`Player-prev.log`만 제외한다. 런타임 로그의 미사용 URP DOF/Panini 셰이더 제거 경고 4건을 오류 0과 구분한다. 이 검수에서는 사용자 게임 창을 활성화·중단·종료하지 않았다.

일반 최종 빌드 [BuildReport](build-normal-final.json)는 78.0067608초, 오류 0·경고 4이며 새 UnitTone 경고가 없다. 검수 최종 빌드 [BuildReport](build-review-final.json)는 33.5064796초, 오류 0·경고 7이며 새 UnitTone 경고가 없다. 남은 경고는 해당 원문에 보존한다. 빌드 성공과 아래 시각 점수를 분리한다.

전장 원본 폴더: `D:/TalesTacticsBuildArchive/PlaytestFeedbackReview20261102Final3/EnvironmentIntegrationReviews/f517cd317b93446085feca7a91a6b6ca/`.

직접 확인한 전장 **10장**:

- `00-episode-01-idle-1280x720-r0.png`
- `04-episode-01-character-close-1280x720-r0.png`
- `25-episode-12-idle-1280x720-r180.png`
- `45-episode-16-idle-1280x720-r0.png`
- `78-training-six-idle-1280x720-r0.png`
- `79-training-six-idle-1280x720-r90.png`
- `80-training-six-idle-1280x720-r180.png`
- `81-training-six-idle-1280x720-r270.png`
- `82-training-six-character-close-1280x720-r0.png`
- `89-training-six-skill-ui-1024x768-r0.png`

HUD 원본 폴더: `D:/TalesTacticsBuildArchive/PlaytestFeedbackReview20261102Final3/BattleHudRedesignReviews/504eb11b980e4101b55ee0be8274cded/`.

직접 확인한 HUD **6장**:

- `00-command-allies2-1920x1080.png`
- `24-command-allies6-1280x720.png`
- `25-skills-allies6-1280x720.png`
- `26-facing-allies6-1280x720.png`
- `27-target-forecast-allies6-1280x720.png`
- `31-zoom-text130-allies6-1280x720.png`

## 여섯 축 평가

기존 2.5D 원화·3D 지형·황금 장식의 남색 UI를 유지하는 범위의 **Windows 16장 표본 시각 점수**다. 새 팔레트 자동 스캔, 기본 재질 전수 조사, 검정 실루엣 이미지 생성, 연속 영상, 프레임률 측정을 수행한 점수가 아니다. 스타일·윤곽은 스프라이트 기준으로, 깊이는 실제 입체 지형과 발 접지·그림자의 관계로 평가한다. 전체 게임의 사운드·캠페인·저장 품질 점수도 아니다.

Palette discipline: 4/5 — 녹색 유적·푸른 밤·황토 협곡의 지면과 배경이 같은 계열이며 남색·금색 HUD가 일관된다. 신규 재질 자동 스캔은 실시하지 않았다.

Style uniformity: 3/5 — 기존 SD 캐릭터의 뚜렷한 선과 회화형 지면의 질감 차이는 남지만 원화·환경·장식 UI의 정체성을 유지하며 새 피부 패치나 임시 도형이 보이지 않는다.

Lighting depth: 4/5 — 근접 발밑 접촉 그림자와 짧은 몸 그림자가 지면에 붙고 네 yaw에서 큰 평면 절단·사각 그림자판 없이 단차와 서 있는 몸체가 구분된다.

Silhouette: 4/5 — 기본 거리와 근접, 측면·후면에서 머리·옷·무기 윤곽으로 6인을 구분할 수 있다. 밀집한 인접 동료끼리 일부 겹치는 것은 남는다.

UI intentionality: 4/5 — 1024×768 기술 목록, 1280×720의 명령·예측·방향 선택·130% 글자, 1920×1080 상태 카드가 정렬되고 표본에서 버튼·문자 잘림이 없다.

Feedback visibility: 4/5 — 예측 화면의 선택 대상·범위 타일·HP/MP 변화와 방향 선택 화살표가 입력에 대한 응답을 보여 준다. 공격 충돌·피격 영상의 평가로 확장하지 않는다.

보정 전 Windows 화면을 같은 조건으로 채점한 기록은 없으므로 전후 점수 차이를 만들지 않았다. 이번 작업의 보정은 발 고정 대기 표현, 피부 재구성 제거 후 미세 눈매·시선 변화, 서 있는 몸체의 깊이 보정, 알펜 공통 표시 비율이다. 얼굴의 초기 세 실험은 [별도 시각 기록](visual-review.md)에 불합격으로 남겼고, 최종 GPU5→GPU6은 100PNG가 완전히 같다. 새 Windows 표본에서 추가로 수정해야 할 화면 결함은 발견하지 않았다.

## 관찰과 한계

- 기본 거리·근접·훈련 네 방향에서 뒤쪽 소품이 캐릭터 몸통을 크게 평면 절단하는 현상, 그림자 사각판, 발 주변의 눈에 띄는 경계 찢김을 발견하지 않았다. 모든 벽·높이·자세 조합을 육안 확인한 것은 아니며 깊이 음성·양성 대조군은 별도 GPU 테스트가 담당한다.
- 이번 전장·HUD 6인 명단은 크레스·민트·벨벳·파라·티아·제이드다. **알펜의 Windows 전장 크기를 직접 비교했다고 주장하지 않는다.** 알펜 비율은 네이티브 강한 알파 측정과 공통 sizing 테스트, 알펜 원본/전용 장비의 직접 GPU 전신 캡처로 확인했다.
- 캠페인 실행 `ca3e0c0b576a4526a64209185b749905`의 `episode-02-before-story.png`·`episode-02-after-story.png` 두 장도 직접 확인했다. 전자는 서술, 후자는 알펜 초상이 있는 대화 화면이며 대사·버튼 잘림이 없다. 월드 알펜은 나오지 않으므로 위 16장 전장/HUD 점수와 알펜 키 비교 증거에 합산하지 않는다. 당시 캠페인 전체 완료도 주장하지 않는다.
- 이 문서의 육안 범위는 전장 10장·HUD 6장·별도 대화 2장이다. 자동 90/90·32/32 성공을 전체 122장 육안 승인으로 표현하지 않는다. 애니메이션 타이밍·입체 그림자의 모든 픽셀·음악 청취·성능 검수는 포함하지 않는다.
- 대표 6PNG는 크기 변경·보정·잘라내기·합성 없이 복사했다. manifest의 원본·복사본 SHA-256이 6/6 일치한다.

## 적용한 검수 지침

`unity:craft-game-floors`의 `SKILL.md`, `references/character-motion.md`, `references/visual-scorecard.md`를 읽었다. 기존 컨트롤러·카메라·팔레트를 교체하지 않고, 2.5D 원화 보존과 이번 화면 변경 범위로 평가했다. 새 팔레트 전수 스캔과 Editor 조작은 이번 위임 범위에서 제외했음을 위에 명시했다.

Unity 6000.6의 [Application.runInBackground 공식 문서](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/application/runinbackground)를 확인했다. Windows 전장/HUD harness는 기존 값을 기록하고 background 실행을 켜며, 화면 상태를 안정시킨 뒤 `WaitForEndOfFrame`과 실제 `ScreenCapture`로 PNG를 만든다. 이번 독립 검수자는 Editor/기존 플레이어의 상태를 바꾸지 않았으며 실행·로그·저장 보존 판정은 wrapper 원문으로 확인했다.
