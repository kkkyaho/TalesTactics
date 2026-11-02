# 플레이 피드백 일괄 반영 — 구현·검증 현황

2026-11-02~03, Unity 6000.6.0f1 / `develop`. 전장 겹침, 캐릭터 대기·표정, 알펜 크기, 등장 시점·합류 레벨, 지정 음악과 기존 음악 배치를 함께 수정했다. **구현과 아래 Editor·Windows 검증을 완료했다.** 전체 PlayMode의 실패 한 건과 보정 후 별도 재검사 결과, 캠페인 검수 도구의 첫 실패도 구분해 보존한다. `develop` 반영 여부는 실제 커밋·푸시 이력을 확인하며, 이 문서는 공개 릴리즈를 선언하지 않는다.

## 적용 내용

| 사용자 피드백 | 구현한 동작 |
|---|---|
| 캐릭터가 뒤쪽 벽·지형에 잘못 잘리는 겹침 | 화면을 향하는 기존 스프라이트의 외형을 유지하고, 각 픽셀의 깊이를 발을 기준으로 선 몸체에 맞춘다. 앞쪽 벽은 계속 캐릭터를 가리며 카메라 회전에도 앞뒤 관계를 갱신한다. |
| 정적인 대기 자세와 후속 표정 요청 | 발을 고정한 상체 호흡·체중 이동·팔과 무기 흔들림, 캐릭터별 짧은 시선·미세한 눈매 긴장을 추가한다. 원화를 유지한 최종 표현의 실제 표본을 확인했다. |
| 타이틀·인터미션 음악 | 지정한 로컬 곡을 각각 타이틀과 거점에 연결한다. 같은 거점 메뉴를 다시 그릴 때 곡을 재시작하지 않는다. |
| 지정 전투곡과 기존 곡의 적절한 사용 | 장·지형·보스·개인 시나리오에 따라 새 곡과 기존 곡을 배치한다. 기존 전투·이야기·승리·캐릭터 테마를 보존한다. |
| 첫 등장 전 동료가 거점에 보이는 문제 | 해당 회차의 실제 등장 전에는 편성·장비·성장 목록에서 숨기고, 첫 등장 전투에는 전원을 강제 출전시킨다. 회차 완료 후 일반 편성으로 돌아간다. |
| 신규 동료의 합류 레벨 | 합류 직전 이미 합류한 동료 전체의 평균 레벨을 내림해 최초 한 번 적용한다. 동시 합류자는 같은 이전 명단으로 계산한다. |
| 알펜이 다른 동료보다 작은 문제 | 투명 여백 대신 실제 몸체 높이로 보정한다. 월드·이동 미리보기·UI 모델·기본/전용 장비에 같은 공통 배율을 사용한다. |

기존 원화와 행동 프레임을 재생성하거나 전면 교체하지 않는다. 원격 음악 다운로드, `DemoContent.Create`, 기존 Content 초기화도 수행하지 않는다. 이전 작업의 접촉·투영 그림자와 저장 형식은 유지한다.

## 깊이와 캐릭터 표현

기존 카메라 정면 빌보드는 위로 갈수록 실제 3D 공간에서 뒤쪽으로 기울어진다. 피치 57도에서는 화면상 앞에 선 캐릭터의 상체가 뒤쪽 벽보다 먼 깊이로 판정될 수 있다. `StandingSpriteDepth`와 공통 shader include는 화면 XY와 원화 비율을 유지하면서 clip 깊이만 발 위에 세운 몸체에 맞춘다. 정사영 카메라에서 적용하며, 실제 메시 모델을 새로 만들거나 스프라이트를 수직 회전하지 않는다.

불투명 지형은 깊이 검사로, 투명 스프라이트끼리는 발 pivot과 카메라의 수평 시선축(CustomAxis)에 따른 정렬로 앞뒤를 결정한다. 캐릭터·시민·서 있는 지형 소품·수목·이동 도착 미리보기에 연결했다. 바닥 오버레이와 바닥 그림자에는 이 보정을 켜지 않는다.

실제 URP 렌더 표적 검사는 피치 57도, yaw -39/51/141/231도의 네 방향을 사용한다. 보정을 끈 음성 대조군은 뒤쪽 벽으로 인해 몸체가 잘리는 상황을 재현해야 한다. 보정을 켜면 뒤쪽 불투명 벽 앞에서 기준 픽셀의 97% 이상이 남고, 실제 앞쪽 벽에는 10% 미만만 남아야 한다. 투명 기둥은 같은 높이와 ±0.5 단차의 앞뒤를 검사한다. 이전 Orthographic 정렬이 높은 뒤 기둥·낮은 앞 기둥에서 반대로 그려지는 음성 대조군도 포함한다. 참조 배우가 없는 검은 이미지는 통과하지 않는다. 이는 실제 GPU 깊이·정렬 검사이며 모든 맵의 수동 시각 승인이나 프레임률 측정은 아니다.

대기 동작은 기존 Idle 원화의 UV를 작게 변형한다. 파라의 빠른 움직임·간헐적인 두 번의 짧은 눈 긴장, 민트·티아의 느린 호흡, 키사라의 절제한 움직임, 알펜·벨벳·시온의 시선과 눈 좁히기를 구분한다. 게임 RNG를 사용하지 않고 HP·MP·행동 순서·방향을 바꾸지 않는다. 행동·피격·사망·비활성화 시 효과를 해제하고 기존 환경광·깊이·행동 완료 색상의 MaterialPropertyBlock 값을 보존한다.

표정은 기본/전용 장비의 전면·좌·우 Idle 원화별 눈 위치를 사용한다. 뒷모습과 등록하지 않은 원화는 얼굴 효과를 끈다. 새 입 모양·표정 원화·얼굴 리깅을 만든 것은 아니다. UV 기저는 실제 스프라이트에서 읽으며, 프레임마다 Sprite나 재질을 생성하지 않는다. 완전한 눈 감기는 구현하지 않는다. 기존 피부·머리카락·안경을 다시 그리지 않고 현재 눈만 미세하게 재표본한다. flipX/Y renderer는 대기 효과 전체를 끄며 회전 아틀라스의 별도 GPU 전수 검증은 수행하지 않았다. 자세한 구현과 표현 한계는 [캐릭터 작업 기록](notes-character.md)을 따른다.

알펜의 기본 전면 프레임은 319px이지만 alpha > 128의 실제 몸체는 242px이다. 몸체 비율을 `.80`으로 보정해 약 0.926에서 **1.157 월드 단위**로 키웠다. 긴 무기나 투명 패딩을 키로 계산하지 않고 동료들의 실제 몸체 중앙값과 비교한다. 원화·PPU 자체는 바꾸지 않는다.

## 합류와 이전 저장

크레스·민트는 1화 시작 전부터 동행 중인 시작 멤버로 유지한다. 이후 새 동료는 2~5화의 첫 등장 전투에서 합류하며, 해당 전투 전 거점에는 표시하지 않는다.

새 레벨은 `floor(이미 합류한 동료들의 레벨 합 / 동료 수)`다. 대기 동료도 포함하며 신규 본인·동시 합류자·미래 동료의 기존 기록은 제외한다. 레벨 기록이 없으면 Lv1로 본다. 예를 들어 크레스 Lv4·민트 Lv7 뒤에 함께 합류하는 알펜·시온은 둘 다 Lv5다. 이후 재시도·이어하기 때 평균을 다시 계산하지 않는다.

첫 등장 회차의 시작 대화가 끝나 실제 전투에 들어갈 때만 신규 합류를 저장한다. 최대 6인 안에서 기존 선택 순서의 앞쪽을 유지하고 필요한 자리를 확보한다. 해당 회차 재시도에도 등장 인원은 유지하며, 완료한 회차의 재플레이와 이후 회차는 자유 편성이다. 과거 프리셋에 미합류 동료가 들어 있으면 이름을 숨기고 적용을 거절한다.

V3 저장 버전과 기존 완료 플래그·스테이지 인덱스를 유지한다. 합류 사실은 `StoryProgress`의 `crossover.recruited.<회차>`에 기록한다. 기존의 더 높은 레벨·EXP·승급·장비·특성·해금 기술은 보존한다. 이미 등장 회차를 완료한 이전 저장의 성장치는 재계산하지 않는다.

이전 중단 전투만으로 합류가 인정되는 저장은 이어하기뿐 아니라 준비 화면의 장비·특성·구매 등 공통 저장 경로에서도 합류 기록과 체크포인트 소비를 한 번에 저장한다. 실패·예외 시 추가 기록, 체크포인트, 저장 시각·편성·선택을 복원한다. 신규 합류 저장에 실패하면 전투 입장도 진행하지 않는다. 실제 이전 저장 로드 후 `resume=false` 상태에서 특성 저장을 실패시킨 뒤 성공·재로드하는 회귀도 추가했다. [합류 작업 기록](notes-recruitment.md).

## 음악 배치

사용자가 지정한 로컬 MP3 여섯 개는 원본 바이트를 그대로 복사하고 SHA-256을 확인했다. 기존 오리지널 음악 14개와 효과음 4개를 보존한다. 새 곡은 Streaming·Vorbis 품질 0.85·원본 샘플레이트·stereo·preload·Load In Background로 가져오며, Unity 빌드 데이터의 재인코딩과 원본 MP3 보존을 구분한다.

| 곡 | 사용 위치 |
|---|---|
| Yume wa Owaranai ~ Psx Version | 타이틀 |
| Tales of Destiny 2 - Wilderness | 인터미션 거점 |
| Fighting of The Spirit | 주요 보스전·전용무기 시련 |
| Threatening Sky | 협곡·문서고의 일반 전투, 주요 보스전 전 대화 |
| Good Wine Makes A Good Blood | 성소·수문의 일반 전투 |
| Decisive | 마지막 20화 전투와 전투 전 대화 |

기존 `battle`은 훈련·유적 일반전, `boss`는 일반 중계핵 전투와 호환 카탈로그의 보스전, `story`는 보통 대화와 결전 이후, `victory`는 승리 결과에 남긴다. 캐릭터 10명의 기존 테마는 비오의와 개인 이야기에 사용한다. 경로 선택을 취소하면 인터미션 음악으로 돌아오며, 실행 중인 전투의 선곡은 메뉴 선택장이 아닌 실제 Session의 장을 따른다.

음악·효과음 음량은 기존 설정과 별도 소스를 사용한다. 비오의 후에는 이전 곡의 클립·샘플 위치·루프·재생 상태를 복원한다. 기존 ID의 개인 음악 대체 기능은 유지하며 새 지정 곡은 덮어쓰지 않는다. 음악 선택은 저장과 게임 RNG를 변경하지 않는다. 메타데이터·원본 SHA·검증 범위는 [음악 작업 기록](notes-audio.md)에 있다. 전곡 청취·음량 정규화·새 루프 편집·크로스페이드 검수는 별도 범위다.

## 현재 검증 상태

아래 결과는 버전 관리용 `Validation` 증거에 연결한다. 서로 다른 표적 실행을 합산해 단일 전체 실행으로 표현하지 않는다. Windows의 `*-summary.json`은 사용자 저장 트리의 개별 파일·디렉터리 이름을 생략한 파생 요약이며, 정확한 로컬 원문의 경로와 SHA-256을 포함한다. Player 보고서·캡처 manifest·로그는 별도 파일로 보존한다.

| 검사 | 현재 결과와 범위 |
|---|---|
| 전체 EditMode | **469/469, 17.97초**, 실패·건너뜀·미결정 0. 합류 규칙·저장 호환 회귀 12개를 포함한다. [실제 원문](Validation/editmode-final.json). |
| 전체 PlayMode | **281/282, 712.14초**, 실패 1·건너뜀·미결정 0. 신규 합류 5개, 대기 lifecycle·GPU 및 음악 회귀는 통과했다. 실패는 기존 상점 fixture 한 건이며 아래에 원인과 후속 검사를 기록한다. [전체 실행 원문](Validation/playmode-full-attempt1.json). |
| 상점 fixture 보정 후 재검사 | **1/1, 9.10초**. 실제로 전원이 합류한 저장을 준비한 뒤 기존 7종 무기 구매·장착·골드·저장 재로드 검사를 그대로 실행했다. [재검사 원문](Validation/playmode-shop-final.json). 전체 282/282를 한 번에 통과한 결과가 아니다. |
| 깊이·가림 GPU 표적 | **1/1, 0.29초**. 네 yaw의 불투명 벽 양성/음성 대조군과 같은 높이·±0.5 단차 투명 기둥의 앞뒤 정렬, 이전 정렬 방식의 실패 재현을 포함한다. [실제 원문](Validation/depth-final.json). |
| 최종 대기·표정 GPU 표적 | **1/1, 5.64초**. DX11 경고 보정 후 GPU6의 100PNG가 표현 확정 GPU5와 파일 SHA-256·디코딩 RGBA 모두 일치했다. [실제 원문](Validation/idle-gpu-final.json) · [100장 비교](Validation/idle-gpu5-gpu6-comparison.json). |
| 대기·표정 실제 시각 검수 | 100PNG 중 실제 열어 본 **42장 표본에서 합격**. 기본 10명의 전면 얼굴, 알펜·제이드 전용 장비 전면 얼굴, 알펜·파라 전신 표본이다. 전체 100장의 육안 승인이 아니다. [검수 범위와 한계](Validation/visual-review.md) · [대표 이미지 갤러리](Evidence/gallery.html). |
| 일반 Windows 최종 빌드 | **Succeeded, 78.01초, 오류 0·경고 4**, 일반 Final2 경로. 추가 define 없음·보호 파일 불변을 확인했다. [전체 BuildReport](Validation/build-normal-final.json). |
| REVIEW Windows 최종 빌드 | **Succeeded, 33.51초, 오류 0·경고 7**, REVIEW Final3 경로. `TALESTACTICS_REVIEW`만 추가·보호 파일 불변을 확인했다. [전체 BuildReport](Validation/build-review-final.json). |
| Windows 전장 | **90/90 통과**, 실제 PNG 90개 디코딩·비균일성 확인, wrapper `passed=true`, 오류 0·종료 코드 0·강제 정리 없음. [원문 경로·SHA를 포함한 파생 요약](Validation/windows-environment-summary.json) · [Player 보고서](Validation/windows-environment-player-report.json) · [PNG manifest](Validation/windows-environment-capture-manifest.json). |
| Windows HUD | **32/32 통과**, 실제 PNG 32개 디코딩·비균일성 확인, wrapper `passed=true`, 오류 0·종료 코드 0·강제 정리 없음. [원문 경로·SHA를 포함한 파생 요약](Validation/windows-hud-summary.json) · [Player 보고서](Validation/windows-hud-player-report.json) · [PNG manifest](Validation/windows-hud-capture-manifest.json). |
| Windows 음악 | **14/14 통과**. 지정 6곡과 타이틀·거점·이야기·전투·테마 복귀 구간에서 선택 클립·샘플/DSP 진행·비영점 출력 관측, 음량 복구와 StopAll을 확인했다. 오류 0·종료 코드 0·강제 정리 없음. [파생 요약](Validation/windows-music-summary.json) · [Player 보고서](Validation/windows-music-player-report.json) · [오디오 manifest](Validation/windows-music-audio-manifest.json). |
| Windows 20화 캠페인 | **20/20 통과, 271.47초**, 24,626 checks·175 save/reload·48PNG·40개 저장 비교 JSON을 검증했다. 새 GUID의 wrapper `passed=true`, 오류 0·종료 코드 0·강제 정리 없음. [최종 파생 요약](Validation/windows-campaign-summary.json) · [Player 보고서](Validation/windows-campaign-player-report.json) · [단계·캡처 manifest](Validation/windows-campaign-stage-manifest.json) · [최종 V3 저장](Validation/windows-campaign-isolated-campaign.json). |
| Windows 실제 시각 표본 | 전장 **10장·HUD 6장**을 직접 확인했고 대표 원본 6PNG를 바이트 변경 없이 보존했다. 별도 대화 2장도 확인했으나 전장 점수나 알펜 월드 크기 비교에 합산하지 않았다. [검수 기록과 한계](Validation/windows-visual-review.md) · [Windows 갤러리](Evidence/Windows/gallery.html). |

전장·HUD·음악·최종 캠페인 각각의 실행 전후 사용자 트리 **4,959파일·2,383디렉터리 전체가 일치**했다. 제외 항목은 사용자 루트의 `Player.log`·`Player-prev.log`뿐이며, 하위 폴더의 같은 이름 파일과 다른 로그·임시 파일도 검사에 포함했다. 실행 직전 기존 게임 프로세스는 없었다. 일반/REVIEW DLL·IL 분리도 통과했으며 일반 빌드의 검수 fixture·검수 IL 참조·Editor 타입은 0이었다. [분리 검사](Validation/player-type-isolation.json). 로그 오류 검색 결과도 각각 비어 있다. [전장 검색 결과](Validation/windows-environment-log-findings.json) · [전장 로그](Validation/windows-environment-player.log) · [HUD 검색 결과](Validation/windows-hud-log-findings.json) · [HUD 로그](Validation/windows-hud-player.log) · [음악 검색 결과](Validation/windows-music-log-findings.json) · [음악 로그](Validation/windows-music-player.log) · [캠페인 검색 결과](Validation/windows-campaign-log-findings.json) · [캠페인 로그](Validation/windows-campaign-player.log). 이는 자동 조건·파일 보존 검증이며, 전체 PNG의 육안 승인·모든 맵·성능 측정은 아니다. 캠페인은 기존 합법 명령 플래너와 timeScale 4를 사용한 자동 진행으로, 사람의 자연 진행 시간이나 밸런스 평가가 아니다.

음악 검사는 재생 시작·전환 직후의 짧은 관측이다. 전환 직후 출력 버퍼에 직전 클립의 샘플이 포함될 가능성이 있어 각 새 곡의 PCM만 분리한 출력 증명은 아니며, 전곡 청취·루프 연결부·음질 평가도 수행하지 않았다. 직접 재생 중 캠페인과 게임 RNG 불변, 저장된 음량 복구는 실제 Player 보고서에서 확인했다.

일반 Final2의 남은 경고는 Pipeline 런타임 설정 부재, 기존 `DEVELOPMENT_BUILD` 지시문 사용, URP 디버그용 shader 두 개의 stripping이다. REVIEW Final3는 Pipeline 설정 경고·기존 `SdMotionDirectionReview`의 obsolete 경고 네 건·기존 지시문 경고와 Editor 미컴파일 코드 경고 한 건이다. 마지막 경고는 REVIEW 전용 런타임 한 줄을 수정한 뒤 Editor 재컴파일 전에 Player를 빌드해 발생했다. 변경된 import·postprocess 코드는 없으며 Player compiler가 새 API 호출을 컴파일해 이전 `LocalMusicReview` 경고 두 건은 사라졌다. 이 변경은 일반 빌드에서 컴파일 제외되므로 일반 Final2를 유지한다. 두 최종 BuildReport에 `CharacterIdle.cginc`의 DX11 잠재적 미초기화 경고는 없다. 빌드 성공을 경고 0으로 표현하지 않는다.

## 실패와 수정 이력

초기 `MaterialPropertyBlock`을 managed 필드 생성 시점에 만드는 문제를 발견해 실제 메인 스레드 `Apply`에서 지연 생성하도록 수정했다. 첫 전체 EditMode는 **464/465**였고 모션 표적에서 예외가 발생했다. [초기 전체 원문](Validation/editmode-attempt1.json). EditMode에서 실제 비활성화 콜백을 기대하던 별도 표적은 **2/3**이었으며, `OnDisable`·GameObject 비활성화·`forceRenderingOff`는 실제 PlayMode 콜백 회귀로 분리했다. [초기 표적 원문](Validation/idle-edit-attempt1.json). 최종 전체 EditMode와 해당 PlayMode 회귀는 통과했다. 0개 발견 실행은 성공 횟수에 포함하지 않는다.

깊이 검사 초기 fixture는 참조 화면이 검거나 뒤쪽 벽 가림 판정이 실패했다. [첫 실행](Validation/depth-attempt1.json) · [두 번째](Validation/depth-attempt2.json) · [세 번째](Validation/depth-attempt3.json). 최종 fixture는 production URP 렌더 요청으로 바꿔 실제 카메라·불투명·투명 패스와 기존 판정 문턱을 확인했다.

완전한 눈 감기를 흉내 낸 GPU1~3은 수치 검사를 통과했지만, 실제 얼굴 확대에서 피부 패치·눈 경계가 어색해 **시각 불합격으로 폐기**했다. [GPU1](Validation/idle-gpu-attempt1.json) · [GPU2](Validation/idle-gpu-attempt2.json) · [GPU3](Validation/idle-gpu-attempt3.json). 피부 재구성을 제거하고 원화 눈의 시선·긴장만 미세하게 바꾼 GPU4는 작은 눈에 적용한 고정 변경 픽셀 문턱에서 실패했다. [GPU4](Validation/idle-gpu-attempt4.json). 표현 강도를 높이지 않고 실제 눈의 투영 면적에 비례하는 검사로 바꾼 GPU5가 수치·42장 시각 표본을 통과했으며, DX11 경고를 수정한 GPU6의 100PNG도 동일했다. [GPU5](Validation/idle-gpu-attempt5.json). 정확한 판정 문턱과 시각 한계는 [시각 검수 기록](Validation/visual-review.md)에 남긴다.

전체 PlayMode의 유일한 실패는 `SevenStandardWeaponCardsPurchaseEquipAndReloadThroughActualShop`이었다. 기존 fixture가 5화 첫 등장 전 상태를 만들면서 전원이 합류한 상점을 기대했다. 이미 합류한 5화 상태로 fixture를 보정하고, 모든 캐릭터의 합류·5화 미완료·각 무기의 실제 장착 가능자를 먼저 확인하도록 했다. 원래의 7종 카드·아이콘·넘침·실제 버튼 클릭·구매·장착·잔액·파일 재로드 검사는 유지했다. 제품의 미합류 동료 숨김과 구매 조건을 완화하지 않았으며, 보정 후 상점 한 건의 별도 재검사가 통과했다.

첫 일반 빌드는 오류 0·경고 5로 성공했지만 `CharacterIdle.cginc`의 DX11 잠재적 미초기화 경고를 포함했다. [첫 일반 BuildReport](Validation/build-normal-attempt1.json). 함수 결과 초기화와 단일 return, 비활성 눈의 0 반지름 보호 후 GPU6을 실행하고 일반 Final2를 다시 빌드했다. 첫 REVIEW와 Final2 REVIEW는 오류 0·경고 8로 성공했으며, 새 `LocalMusicReview`의 obsolete 경고 두 건을 제거한 Final3가 최종 검수 대상이다. [첫 REVIEW BuildReport](Validation/build-review-attempt1.json) · [Final2 REVIEW BuildReport](Validation/build-review-attempt2.json). 최종 REVIEW의 남은 경고는 위에서 별도로 설명한다.

첫 Windows 캠페인 wrapper는 게임이 정상 종료한 뒤 보고서 파싱에서 실패했다. 첫 게임 실행은 20/20화·24,652 checks·175 save/reload·오류 0·종료 코드 0이었으나 wrapper 전체 결과는 실패로 남겼다. [첫 시도 파생 요약](Validation/windows-campaign-attempt1-summary.json). Unity가 쓴 BOM 없는 UTF-8 JSON을 Windows PowerShell 5가 기본 CP949로 읽어 한국어 화자 이름 주변의 따옴표를 잘못 해석한 것이 원인이었다. 실제 PS5에서 기본 읽기의 동일 오류와 명시적 UTF-8 읽기의 정상 파싱을 재현했다. wrapper의 보고서·최종 저장 JSON 읽기 두 곳에 `-Encoding UTF8`만 추가했다. 기존 원본 93파일과 실패 validation을 그대로 둔 독립 `Assert-Report` 재검사에서는 20화·48PNG·40개 저장 비교 JSON·최종 저장 조건이 통과했다. 이 독립 재검사를 전체 wrapper 성공으로 대신하지 않고 새 GUID `eaa2e9b6b9dc49ef8d3d86294088f12b`로 캠페인 전체를 다시 실행해 최종 통과했다. 런타임 코드는 바꾸지 않았으며 기존 실패 결과도 덮어쓰지 않았다.

## 최종 Windows 실행 검수

빌드 대상은 TestBattle 씬, StandaloneWindows64, BuildOptions.None이다. 일반 빌드는 추가 define 없이, 검수 빌드는 `TALESTACTICS_REVIEW`만 추가한다. [빌드 도구](Validation/Build-PlaytestFeedback.cs)는 아래 정확한 두 경로만 허용하며, 예약 영수증이 아니라 뒤에 생성되는 `build-result.json`이 성공 판정이다.

- 일반: `D:/TalesTacticsBuildArchive/PlaytestFeedbackWindows20261102Final2`
- 검수: `D:/TalesTacticsBuildArchive/PlaytestFeedbackReview20261102Final3`

| 재현 도구 | 실행 검수 범위 |
|---|---|
| [전장 검수](Validation/Validate-PlaytestFeedbackWindows.ps1) | 일반/검수 DLL·IL 분리, 전장 90개 조건과 실제 PNG, 오류 0·정상 종료·저장 보존 |
| [HUD 검수](Validation/Validate-PlaytestFeedbackHud.ps1) | HUD 32개 조건과 실제 PNG, 오류 0·정상 종료·저장 보존 |
| [음악 검수](Validation/Validate-LocalMusicWindows.ps1) | 지정 6곡을 포함한 14개 재생·전환 구간의 커서/DSP 진행·비영점 출력·음량 복구·정상 종료 |
| [캠페인 검수](Validation/Validate-PlaytestCampaignWindows.ps1) | 실제 20화 전투·이야기·보상·저장 재로드, 40개 전후 JSON과 48개 PNG, 오류 0·정상 종료 |

각 검수는 새 GUID와 검수 빌드 하위 전용 Storage를 사용한다. 캠페인은 `CampaignFlowReviews/<GUID>`, 음악은 `LocalMusicReviews/<GUID>` 아래에 기록한다. 래퍼는 사용자 저장의 전체 재귀 파일 해시와 디렉터리 목록을 비교하되 루트 `Player.log`·`Player-prev.log` 두 이름만 제외한다. 기존 실행 게임은 현재 실행 직전의 PID·시작 시각·실행 파일 기준으로 보존하며 과거 PID를 필수로 요구하지 않는다. 제한 시간 초과나 소유 프로세스 정리가 필요했던 실행은 통과하지 않는다. 검수 래퍼의 성공은 일반 실행본 수동 플레이나 공개 릴리즈의 성공을 대신하지 않는다.

기존 dirty 12파일은 최종 SHA-256이 작업 시작 백업과 모두 일치했다. [보호 파일 최종 대조](Validation/preexisting-files-final.json). 사용자 원화는 편집하지 않았다. 사용자 저장·설정·백업 8파일도 작업 시작 해시를 유지했다. 별도로 바뀐 `TestResults.xml`은 생성된 테스트 산출물로 구분한다. [사용자 파일 최종 대조](Validation/user-files-final.json). Unity 검사가 생성한 동적 폰트와 기존 CSV 두 개는 시작 전 사용자 바이트로 복원했다. [폰트 복원](Validation/font-restoration.json) · [CSV 복원](Validation/generated-csv-restoration.json). 최종 관련 소스 70파일의 해시도 기록했다. [소스 SHA-256](Validation/source-final-sha256.json). 마지막 작업 공간 검사에서도 소스 70파일·보호 12파일·사용자 저장/설정의 변경이 없고 실행 중 게임 프로세스가 0임을 확인했다. [최종 대조](Validation/final-workspace-check.json).

Editor는 TestBattle 씬·Play 중지·씬 dirty=false·runInBackground=false 상태이며 AutoTick은 원래의 활성·16ms 설정으로 복원했다. [Editor 최종 상태](Validation/editor-final-state.json) · [AutoTick 최종 상태](Validation/autotick-final.json). 현재 Console 오류는 0이며 이전 실패 로그는 지우지 않았다. [Console 증거](Validation/console-final.json). 구현·검증 완료와 저장 보존을 확인했으며, `develop` 커밋·푸시 완료 여부는 버전 관리 이력을 따른다.
