# 캐릭터 대기 표현 및 알펜 표시 비율

## 최종 구현 범위

- 기존 Idle 스프라이트/전용 장비 매핑을 그대로 사용한다. 원본 PNG, Content 에셋, 이동·스킬·피격·사망 프레임은 편집하지 않았다.
- 전체 몸체 대기 bob을 제거하고 발과 하단 부츠를 고정했다. `CharacterIdle.cginc`에서 상체 체중 이동·호흡·옆 팔과 무기 부분의 작은 흔들림을 표현한다. 전체 몸체 확대/축소를 표정으로 사용하지 않는다.
- 게임 RNG를 쓰지 않는 결정적 시간 표본을 사용한다. 파라는 조금 빠르고 활기 있는 체중 이동, 민트·티아는 느린 호흡, 키사라는 절제한 움직임, 벨벳·알펜·시온은 짧은 시선 이동과 눈을 조금 좁히는 표현을 사용한다.
- 얼굴은 현재 원화의 눈 영역만 재표본하는 미세 시선 이동·눈 긴장 표현이다. 새 피부를 채우거나 눈꺼풀을 그리지 않는다. 완전히 눈을 감는 애니메이션은 포함하지 않는다. 기본/전용 장비 10명 × 전면/좌/우 60개 얼굴을 원본 PNG 진단 갤러리에서 각각 확인하여 눈 위치를 기록했다. 제이드 안경과 머리카락에 가린 눈은 작은 내부 영역만 지정했다.
- 뒤쪽 방향이나 등록하지 않은 대체 원화에서는 얼굴 처리를 끈다. 행동·스킬·피격·사망·비활성화에서는 대기 효과를 해제한다. 기존 환경광·행동 완료 색상 property block을 보존한다.
- 실제 Sprite 로컬 좌표/UV 기저로 아틀라스 패킹과 UV 회전을 반영한다. 변형 좌표가 프레임을 벗어나면 투명 처리하여 인접 프레임을 읽지 않는다. 현재 UnitTone은 UnitySprites의 flip vertex 경로를 쓰지 않으므로 flipX/flipY가 켜진 renderer에서는 대기 처리를 안전하게 끈다. 새 Material/Sprite를 프레임마다 생성하지 않는다.

## 알펜 비율

기본 전면 프레임은 319px이며 강한 알파(alpha > 128)의 실제 몸체 높이는 242px다. 기존 실제 표시 높이는 약 0.926 월드 단위로 다른 SD 영웅보다 작았다. `CharacterVisualSizing.BodyFraction`을 알펜에 대해 .80으로 지정하여 약 1.157 월드 단위로 맞췄다. 공통 sizing 경로로 월드 캐릭터·이동 미리보기·UI 모델 및 모든 장비/행동 방향에 같은 배율이 적용된다.

## 검증 설계와 상태

EditMode 검사는 표본의 재현성·범위·눈 긴장 주기·RNG 보존, 네 카메라 방향과 장비의 기존 Sprite 참조/발 transform/행동 해제/HP·MP·방향 보존, 실제 네이티브 PNG 강한 알파 높이를 사용한 알펜과 동료의 비율을 확인한다. 별도 PlayMode 검사가 실제 MonoBehaviour 비활성화 및 숨김 renderer에서 대기 효과 해제를 확인한다.

GPU 검사는 실제 SpriteRenderer와 production UnitTone을 URP `RenderPipeline.StandardRequest`로 렌더한다. 10명 × 기본/전용 장비 × 네 방향에서 눈 긴장 표현이 눈 영역만 변경하는지, 뒷모습 유지, 해제 후 정확한 원화 복원, 발 픽셀 고정과 상체/무기 움직임을 검사한다. 미세 눈 긴장은 투영된 두 눈 타원 면적의 2% 이상(최소 2px)에서 RGB 차이가 있어야 하며, 눈 밖 변경은 필터 경계 허용 2px 이하여야 한다. 전면 neutral/tension/focus 얼굴 확대와 neutral/settle 전신 PNG 100장을 `Logs/PlaytestFeedback/CharacterIdle/<run-id>/`에 직접 저장한다. 의미 검사 실패는 누적하여 모든 캐릭터의 캡처 후 보고한다.

2026-11-02 표현 확정 GPU5 표적 검사는 1/1, 15.44초 통과했다. 증거 폴더는 `Logs/PlaytestFeedback/CharacterIdle/8a00a36dbe2d46039b3efdc124d0d8a4/`이며 직접 렌더 PNG 100개를 확인했다. 이후 DX11 반환값 경고를 수정한 GPU6도 **1/1, 5.64초** 통과했다. GPU6 폴더는 `Logs/PlaytestFeedback/CharacterIdle/e5f5f8feb712492889383b7d2683bddf/`다. 두 실행의 **100/100 PNG 파일 SHA-256과 디코딩한 RGBA가 모두 정확히 일치**하며 채널 최대 차이는 0이다. [100장 비교 원문](Validation/idle-gpu5-gpu6-comparison.json). 따라서 아래 GPU5의 실제 시각 표본과 저장소 증거 7장은 경고 수정 후 화면과도 동일하다.

전체 EditMode는 **469/469, 17.97초** 통과했다. 전체 PlayMode는 **281/282, 712.14초**였으며 이 안의 대기 GPU 검사와 실제 disable/hidden callback 검사는 모두 통과했다. 남은 실패는 기존 상점 테스트 `SevenStandardWeaponCardsPurchaseEquipAndReloadThroughActualShop`이며 해당 fixture 수정 뒤 **별도 1/1, 9.10초** 재검사가 통과했다. 이 별도 결과를 합쳐 단일 전체 실행이 282/282였다고 표현하지 않는다. 원문은 `Logs/PlaytestFeedback20261102/full-play-final1.json`, `shop-fixture-final.json`, `idle-gpu6-final.json`이다.

이 실행의 실제 PNG 중 다음 42장을 직접 열어 비교했다. 대표 7장만 보존한 [실제 캡처 갤러리](Evidence/gallery.html)와 [시각 검수 기록](Validation/visual-review.md)을 함께 제공한다. 원본 PNG를 수정하거나 합성하지 않는다.

| 육안 확인 범위 | 파일 상태 | 수 |
| --- | --- | ---: |
| 크레스, 민트, 파라, 제이드, 티아, 나탈리아, 벨벳, 알펜, 시온, 키사라 기본 장비 전면 | `*-base-face-neutral/tension/focus.png` | 30 |
| 알펜·제이드 전용 장비 전면 | `*-signature-face-neutral/tension/focus.png` | 6 |
| 알펜 기본/전용 장비, 파라 기본 장비 전신 | `*-body-neutral/settle.png` | 6 |

확인한 얼굴 표본에서는 작은 시선 이동과 눈매 변화가 보이고 피부의 명암, 머리카락, 제이드 안경 테두리, 알펜의 가려진 눈 주변이 유지된다. 이전 방식의 평평한 피부 패치나 새 눈꺼풀 선은 없다. 전신 표본은 발 위치를 유지하면서 상체·팔·무기가 작게 움직이며 눈에 띄는 경계 찢김을 발견하지 않았다. 이는 확대 정지 표본의 육안 확인이며, 전장 크기에서 표정이 크게 읽히는 애니메이션이나 연속 영상 검수를 뜻하지 않는다. 측면·후면은 GPU 수치 검사 범위에 포함되지만 이 42장의 육안 확인 범위에는 포함되지 않는다.

완전히 감은 눈을 만드는 이전 3개 GPU 실험은 수치 검사를 통과했지만 확대 검수에서 피부 패치가 보여 채택하지 않았다. 기존 Cast/Damage 원화의 감은 눈도 확인했으나 머리 기울기·표정·자세가 달라 Idle 전체 프레임 교환이나 얼굴 조각 붙이기를 하지 않았다. 최종 방식은 원화 보존을 우선하여 자연스러운 미세 눈 긴장과 시선 변화로 범위를 정했다. 초기 MonoBehaviour 생성자에서 MPB를 만들던 문제는 실제 active Apply 시점에 지연 생성하도록 수정했다.

첫 Windows 일반 BuildReport(`Build-f60267e5b72b42d097d90ddbb77d391d/build-result.json`)에는 DX11 `CharacterIdlePixel` 반환값의 잠재적 미초기화 경고가 실제 `Packaging assets - resources.assets` 단계에 기록됐다. 과거 Console 잔재로 취급하지 않았다. `CharacterIdlePixel`과 `IdleEye`의 반환값을 명시 초기화·단일 return으로 정리하고, 사용하지 않는 눈의 반지름이 0인 분모를 보호했다. 유효 눈 수식은 유지했으며 GPU6의 100장 동일성으로 확인했다. [수정 후 일반 Windows BuildReport](Validation/build-normal-final.json)는 run `5507700f6c3b43bdba372dadf230e1b3`, **78.0067608초 성공, 오류 0, 기존 경고 4**이며 UnitTone 경고가 없다. 검수 최종 빌드도 **33.5064796초 성공, 오류 0·경고 7**, UnitTone 경고 없음이다. 새 Windows 전장 10장·HUD 6장을 직접 열어 평가한 범위·여섯 축 점수·대표 무보정 PNG는 [Windows 시각 기록](Validation/windows-visual-review.md)에 있다. 이 Windows 6인 명단에는 알펜이 없으므로 알펜 월드 크기의 Windows 직접 비교로 표현하지 않는다.

## 표현상의 한계

눈 표현은 작은 2D 재표본 근사이며 완전한 눈 감기·새 입 모양·대사 표정·정교한 얼굴 리깅을 포함하지 않는다. 상체 UV 변형은 기존 실루엣 그림자 메시까지 다시 변형하지 않으므로 화면상 약 1px 이하의 차이가 생길 수 있다. 눈 위치는 현재 Idle 원화의 이름과 프레임 크기에 대응하며 새로운 원화에는 별도 측정이 필요하다. renderer flip은 지원을 주장하지 않으며 안전하게 opt-out한다.
