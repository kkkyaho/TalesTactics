# 캐릭터·모션·기술 품질 개선 — 적용·검증 완료

2026-10-31 새 원화 **103시트·3,850프레임**을 실제 production 경로에 적용했다. 전체 기존 **159시트의 Sprite 식별자**와 보호 대상 Content·장비·폰트 파일537개의 바이트 보존을 확인했다. 적용 후 실제 전체 **EditMode435/435(30.03초)**와 **PlayMode247/247(608.92초)**를 통과했으며 실패·건너뜀·미결정은0이다. 크기 검사에서 발생한 앞선 실패와0건 실행은 아래에 구분해 남겼다. Windows 리뷰 플레이어의 **아군 기술176/176·자동 캠페인20화·성능 표본20/20**도 통과했다. 일반/리뷰 빌드와 **최종 일반 플레이어20초 응답·정상 종료0·타입 격리·최종 저장 일치**를 확인했다. 실행 중 변경된 창 설정1개는 검증 후 이번 원본 백업으로 복원했다. 구현과 열거된 검증은 완료했으며 버전 관리 반영은 develop 이력에서 확인한다. [Windows 결과 요약](Validation/windows-final-validation-summary.json). [적용 결과](Validation/production-application.json), [최종 EditMode](Validation/full-editmode-after-art.json), [최종 전체 PlayMode](Validation/full-playmode-after-art.json).

## 적용 범위

최초 밀집 모션 **39시트·2,826프레임**에 아군 8명의 기본/전용무기 대기·이동·보조·복귀 원화 **64시트·1,024프레임**을 더했다. 전체 자산 합계 159시트·4,898프레임은 유지하며, 159장 모두를 이번에 다시 그렸다는 뜻은 아니다. [선택 명세](selected-revisions.json)는 적용 전 입력을 고정한 기록이므로 그 안의 당시 상태 문구와 최신 적용 결과를 구별한다.

승인된 [캐릭터·모션 시안](Samples/character-motion-approved.png)과 [기술 이펙트 시안](Samples/skills-approved.png)을 바탕으로 built-in image_gen에서 새로 그렸다. 두 시안은 생성 이미지이며 실제 게임 캡처가 아니다. 생성 후보는 리사이즈·색/알파 보정 없이 native RGBA를 분리·정수 이동으로 배치했고, 원래 프레임 이름·ID·순서와 장비 참조를 보존했다. 실제 rect·pivot·outline·PPU는 측정값과 렌더 검수로 확인했다. 생성 프롬프트·재시도·선택 출처는 [Generation](Generation)에 기록한다. 최종 Packed PNG와 측정 자료는 보관하며 raw Generated·참조 시트·대량 PNG는 로컬에 남긴다.

공통 모션에는 포즈별 체류, 준비·타격·회수 흐름과 방향별 시전 포즈열을 연결했다. VFX는 검흔·접촉 강조와 주문 후면 레이어를 보완했다. Core 판정·기술 수치·Content 정의·저장 형식 변경은 이 작업의 범위가 아니다. [검수 도구와 재현 범위](quality-presentation-review.md).

## 실제 검증과 남은 확인

| 단계 | 확보한 근거 | 해석 범위 |
|---|---|---|
| 적용 전 회귀 | [EditMode433/433, 9.83초](Validation/full-editmode-atomic.json), [전체 PlayMode247/247, 596.49초](Validation/full-playmode-before-art.json) | 서로 다른 시점의 실행이며 새 원화 적용 후 전체 통과 결과가 아님 |
| 후보 모션 A/B | [크레스·민트·파라·티아96/96, 1,077PNG](Validation/final-candidate-motion-group-a-summary.json), [제이드·알펜·시온·벨벳104/104, 1,433PNG](Validation/final-candidate-motion-group-b-summary.json) | 기본/전용의 방향별 phase 검사와 선택 동작 연속 촬영. 전체 네이티브 프레임의 육안 승인 아님 |
| 후보 크기 후속 검사 | [크레스24/24, 210PNG](Validation/final-candidate-motion-cless-scale-summary.json), [티아24/24, 271PNG](Validation/final-candidate-motion-tear-scale-summary.json) | 각 당시 후보의 표현 fixture 통과. 별도 사후 장비 회귀 검사와 구분 |
| 적 후보 | [224행 수집 중 오리진 화면 밖 오류4건](Validation/candidate-enemy-motion-initial-failed-summary.json), 카메라 보완 후 [오리진12/12](Validation/candidate-origin-camera-recheck-summary.json) | 최초 전체 결과는 실패로 보존. 224개 단일 전체 재실행 성공이나 실제 적 AI 전투 검증으로 세지 않음 |
| 적용 전 VFX | [schema2 364/364, 1,340PNG](Validation/preart-vfx-schema2-summary.json) | 182기술×일반/축소의 상태·기하 표현 검사. 실제 Director 단일 판정이나 사후 새 원화 검증과 별개 |
| production 적용 | [103/103·3,850프레임, 전체159 식별자·보호 파일 보존](Validation/production-application.json) | 다섯 번째 적용 성공. 가져오기 성공을 시각 품질 전체 통과로 해석하지 않음 |
| 적용 후 EditMode | [435/435, 30.03초](Validation/full-editmode-after-art.json) | 초기432/433 실패를 보존하고 약한 알파 여백을 제외하는 동일 범위 검사·합성 회귀2개를 추가해 전체 재실행 |
| 적용 후 첫 PlayMode | [246/247, 624.07초·실패1건 추출 기록](Validation/full-playmode-after-art-attempt1-summary.json) | HQ 포즈의 사각형 높이 검사 실패. 원본 raw 손실을 명시하고 후속 통과와 분리 |
| 적용 후 최종 PlayMode | [전체247/247, 608.92초](Validation/full-playmode-after-art.json), [표적 포즈1/1, 8.21초](Validation/pose-playmode-recheck.json) | 각각 실제 Unity 실행. 전체 결과의 실패·건너뜀·미결정0 |
| 최종 Windows 빌드 | [리뷰44.729초·0오류/3경고](Validation/windows-review-build-final.txt), [일반61.394초·0오류/2경고](Validation/windows-normal-build-final.txt) | 빌드 성공과 플레이어 검증을 구분 |
| 실제 아군 기술 | [176/176·88기술×일반/축소](Validation/windows-roster-final.json) | 실제 Director·단일 판정·독립 Resolver/격자/RNG·아군6모델·저장 보존. 훈련 fixture이며 적AI·장비 전수 검수 아님 |
| 실제 자동 캠페인 | [20화·46,057검사·175저장/재로드](Validation/windows-campaign-final.json) | 필수 전후 이야기·전투·구매·보상·후일담 재로드·다음 화. 고정 자동 정책이며 사람 플레이/밸런스 승인 아님 |
| Windows 성능 표본 | [20/20·29,007프레임](Validation/windows-performance-final.json) | 10개 대표 표현×일반/축소, PNG0장. 모든182기술·순수GPU·장시간 성능 결과 아님 |
| 최종 일반 시작 | [20초·응답창·정상 종료0·오류0](Validation/windows-normal-startup-final.json) | 새 DLL의 review/editor/test 격리 확인. settings.json1개 변경 후 이번 백업 복원, 최종 전체 저장 일치 |

후보 자동 결과는 사용자/격리 저장·세션·원본 보존과 소유 복제본 정리를 기록한다. 실제 육안 판단 범위는 [제이드](Validation/final-jade-scale-visual.json), [민트·티아·크레스](Validation/final-continuous-mint-tear-cless.json), [파라](Validation/finalcontinuous-farah.json), [알펜](Validation/alphen-transition-visual.json), [시온](Validation/final-continuous-shionne.json), [벨벳](Validation/final-continuous-velvet.json), [보스](Validation/boss-optical-review.json)에서 확인한다. 각 기록의 대상·시점·미확인 동작을 유지한다. 위 후보/VFX 링크는 **전체 원본 보고서가 아닌 파생 요약**이며 [명세](Validation/candidate-fixture-summaries.json)에 원본 경로·SHA256·크기를 기록했다. 전체 result/timeline/PNG는 로컬에 보존하고 저장소에는 중복 복사하지 않았다.

Windows 결과의 [파생 요약](Validation/windows-final-validation-summary.json)은 원본 wrapper 경로·SHA와 저장 스냅샷 일치, 실제 프로세스 종료0을 기록한다. 캠페인은 Standard·timeScale4의 고정 구매 정책이며 반응/자동 타이밍/임무 이벤트/CT/utility AI를 끈 검수 조건이다. 실제 대화는 자동 진행했고 구매24건·7,320G를 사용했다. 이 결과로 사람의 난이도·경제 체감을 승인하지 않는다.

성능은 RTX3060·Direct3D11·1920×1080의 uncapped 리뷰 플레이어에서 관측한 값이다. 평균1.657ms, p95 2.189ms, p99 2.630ms, 최대72.829ms이며 OS/프레임 대기를 포함한 짧은 표본이다. 일반 출시 빌드·다른PC·장시간 성능 보증으로 바꾸어 해석하지 않는다.

최종 실제 캡처: [크레스](Evidence/cless.0-normal.png) · [민트](Evidence/mint.0-normal.png) · [파라](Evidence/farah.0-normal.png) · [제이드](Evidence/jade.6-normal.png) · [알펜](Evidence/alphen.ultimate-normal.png) · [키사라](Evidence/kisara.ultimate-reduced.png). [원본 해시·육안 검토 명세](Evidence/actual-capture-manifest.json)에 범위를 남겼다. 6장의 첫 접점 화면이며 최대 이펙트 장면이나 모든 동작 프레임이 아니다. 훈련 fixture에서 동료를 조밀하게 배치한 이름/HP 표시 겹침은 한계로 명시한다.

최종 [일반 시작](Validation/windows-normal-startup-final.json)은20초 응답과 정상 종료0, 새 DLL의 review/editor/test 제외를 확인했다. `settings.json`의 `WindowMaximized` True→False, 없던 `SeenContextHints`→0만 바뀌었고 허용 범위·해시·잠금 확인 뒤 이번 실행의 원본 백업으로 복원해 최종 전체 저장이 일치했다. 실행 중 무변경이나0복원으로 표현하지 않는다. 런타임 오류는0이며 DOF/Panini의 stripped 경고4줄은 남아 있다. root가 최신 창의 타이틀을 연속2회 확인했지만 게임 입력은 하지 않았고 별도 타이틀 이미지 파일은 보관하지 않았다.

## 적용 실패와 보완

첫 네 차례 적용은 모두 실패했고 **전체 롤백 확인**을 보존했다. [1차](Validation/production-application-attempt1.json)는 시온 전용 복귀의 bounds/PPU, [2차](Validation/production-application-attempt2.json)는 시온 보조 원화의 mapped-section 파일 잠금, [3차](Validation/production-application-attempt3.json)는 벨벳 전용 대기의 frame coverage, [4차](Validation/production-application-attempt4.json)는 시온 복귀 원화의 파일 잠금에서 중단했다. 부분 성공을 완료로 세지 않는다.

importer는 처음과 마지막 가져오기를 동기로 수행하고 실제/기대 geometry를 오류에 남긴다. 원본 파일을 제자리에서 덮어쓰는 대신 같은 폴더에 새 바이트를 기록·flush한 뒤 `File.Replace`로 교체한다. 지원하지 않거나 공유 잠금이 남으면 삭제/복사 우회 없이 실패한다. 자동 가져오기 중단은 finally에서 복구하고, 복원 실패도 원래 오류와 함께 보고한다. 외부 전체 적용 트랜잭션과 백업/해시 검증은 별도로 유지한다. 이 보완 뒤 5차 적용은 성공했지만 특정 native 캐시 원인까지 확정했다는 뜻은 아니다.

적용 후 첫 크레스 실패값2.22172117는 실제 몸 높이가 아니라 프레임 사각형의 비율이었다. [native 알파 분석](Validation/signature-visible-scale-audit.json)에서 전용무기 매핑1,404쌍을 조사했으며 alpha≥16/64/128/129/200 모두 기존0.65~1.65 범위 밖이0이었다. 최종 검사는 모든 **alpha>128 픽셀**의 높이와 실제 PPU를 사용하며 떨어진 무기 조각도 포함한다. 여백 제외·분리 조각 포함·빈 프레임 거부와 실제2배 확대 거부를 합성 테스트2개로 확인했다. 픽셀·import 설정·원화는 그대로다. 이 높이는 머리·옷·무기까지 포함하므로 해부학적 몸 비율이나 동작의 부드러움을 자동 승인하지 않는다.

첫 사후 PlayMode의 `cless-hq_3_0` 높이1.87923837도 `Sprite.bounds` 측정이었다. [native HQ 분석](Validation/pose-visible-scale-audit.json)은 정확한 Unity rect/PPU와 원본 PNG10장·meta10개의 SHA를 고정하고, 10명160포즈를 다시 읽어 검사했다. 사각형 기준41개가 기존0.8~1.6 범위 밖이지만 alpha≥16/64/128/129/200 기준 위반은 모두0이었다. 이는 픽셀 분석이며 Unity 재실행 성공을 의미하지 않는다.

첫 PlayMode 전체 raw JSON은 보관 전에 후속0건 실행이 같은 경로를 덮어써 남지 않았다. [추출 실패 기록](Validation/full-playmode-after-art-attempt1-summary.json)에 당시 읽은 요약·단일 실패·원본 SHA와61,039바이트 크기, 원문 부재를 명시했다. 전체 원본이나 복원본으로 표시하지 않으며 0건 실행도 통과로 세지 않는다.

HQ 크기 검사를 같은 허용 범위의 실제 알파 높이로 보완한 뒤 [표적 포즈1/1(8.21초)](Validation/pose-playmode-recheck.json)을 통과했다. 후속 [0건 실행](Validation/playmode-empty-cache-attempt.json)은 통과로 처리하지 않았다. 중지 상태에서 패키지·프로젝트 파일이나 설정을 바꾸지 않고 [메모리의 빈 UTF 테스트 캐시만 초기화](Validation/player-test-cache-reset.json)한 뒤 [전체247/247(608.92초)](Validation/full-playmode-after-art.json)을 실제 재실행해 통과했다. 이 결과는 Unity 회귀이며 Windows 플레이어 검증을 대신하지 않는다.

초기 라이선스·빈 테스트 캐시·분리 실패·민트 보류와 최초 적용 전 자동 승인 거절은 이전 단계의 이력으로 남긴다. [재연결 기록](Validation/Reconnect20261031), [민트 초기 수치](Validation/mint-native-analysis.md), [민트 보완 출처](Generation/mint-final-refinement.json). 요청 범위의 원화 적용과 열거된 검증을 완료했다. 사람의 전체 수동 완주·모든 프레임 평가·다른 PC/DPI·장시간 성능·공개 릴리즈는 이번 결과로 보장하지 않는다.
