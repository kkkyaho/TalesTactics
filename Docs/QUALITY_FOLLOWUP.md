# 남은 품질 검토 — 2026-09-28

전체 완료가 아니다. 기술 검토와 성장 방향 결정은 완료했으며 물리 장치 조작·사람의 평가는 대기 중이다. 2026-09-28 사용자 선택으로 장편 확장형을 확정했다. 게임 수치·저장·그림·음악은 변경하지 않았다.

## 실물 입력

실제 Editor의 InputSystem.Gamepad.all.Count=0. 모델명과 USB/Bluetooth 연결이 필요하다. 가상 장치 회귀 검사는 물리 기기 검수를 대신하지 않는다.

연결 후 확인: 스틱 커서, 방향키 메뉴, A 선택/B 취소, 전장 대상 선택, LB/RB 회전, 우스틱 줌/R3 복구, 파라 타이밍, 연결 해제 후 마우스 전환, 재연결. 현재 모두 실물 미검수다.

## 장기 성장

Tools/ReviewLongProgression.cs.txt를 실제 Editor에서 실행했다. 에셋을 읽고 메모리상의 CampaignSave에 실제 보상 규칙을 적용했다. 사용자 저장과 에셋은 쓰지 않았다. 10명 각각 항상 출전, 1장 초회→2장 초회→2장 반복, 구매/매각/추가 드롭 없음. 결과는 long-progression-review.json의10명×49개 레벨 상승 기록이다. 전투 승률·소요 시간을 계산한 시뮬레이션은 아니다.

| 목표 | 총 승리 | 초회 두 장 이후 2장 반복 |
|---|---:|---:|
| Lv3 | 2 | 0 |
| Lv10 | 49 | 47 |
| Lv20 승급 자격 | 210 | 208 |
| Lv25 | 332 | 330 |
| Lv50 | 1360 | 1358 |

2026-09-28 사용자 결정: **장편 확장형으로 현재 성장 곡선을 유지한다.** 레벨별 필요 EXP=현재 레벨×100, 승급 Lv20, 상한 Lv50 및 기존 두 장의 보상을 유지한다. 기존 저장의 레벨·EXP는 소급 재산정하지 않는다.

208회는 후속 장 없이 2장 EXP90만 반복하는 제한된 시나리오의 수치다. 장편 전체의 목표 플레이 횟수나 권장 반복량이 아니다. 후속 장을 추가할 때 해당 장의 적 레벨·초회/반복 EXP·기술 해금·장비 가격을 함께 설계해 새 콘텐츠로 성장하도록 한다. 3장 협곡의 봉화를 첫 확장으로 구현했다(초회 EXP300/240G, 반복 EXP150/120G, 강화 갑옷). 후속 사용자 지시로6장까지 범위를 확정하고4~6장 및 첫 여정 결말을 추가했다. 같은 출전자가 초회 진행하면 Lv9/EXP0에 도달한다. 7장 이후는 이번 범위 밖이다. 두 장 완성형을 위한 EXP1870/3740 상향안은 채택하지 않는다.

초회6인 승리·1인 패배는 FINAL_REVIEW.md에 있다. 사람의 재미·학습 난이도나 모든 편성의 승률을 증명하지 않는다.

## 아트

기존 갤러리4개를 직접 열어10명×9상태×4방향을 다시 비교했다. 새 잘림·이웃 그림 혼입은 발견하지 못했고, 머리색·의상색·무기 실루엣은 식별 가능했다. 기존 캡처의 추가 시각 검토이며 새 엔진 테스트가 아니다.

[앞](polish-front-preview.png) / [뒤](polish-back-preview.png) / [왼쪽](polish-left-preview.png) / [오른쪽](polish-right-preview.png)

후속 작화 검토 후보: Idle/Walk 머리·몸 비율 변화, 큰 방패·긴 머리의 자세별 면적 변화, 어두운 캐릭터의 저대비 배경 가독성. 확정 오류나 원작 외형 불일치 판정은 아니다. 고밀도 중간 프레임 추가·전문가 장신구 감수는 수행하지 않았다.

## 음악

Tools/ReviewMusicSignals.py 실행:14곡 모두 기존 manifest 해시와 동일,44.1kHz/16bit/스테레오, 포화 샘플0, 처음/마지막 PCM 샘플 차이0. music-signal-review.json에 피크·RMS·DC 기록. PCM 경계 검사이며 압축 재생의 청감·멜로디 품질·사람의 만족도 평가는 아니다. 이 세션에서 실제 청음을 했다고 주장하지 않는다.

- [전투](../Assets/TalesTactics/Audio/Original/battle.wav), [보스](../Assets/TalesTactics/Audio/Original/boss.wav), [이야기](../Assets/TalesTactics/Audio/Original/story.wav), [승리](../Assets/TalesTactics/Audio/Original/victory.wav)
- [크레스](../Assets/TalesTactics/Audio/Original/cless.theme.wav), [민트](../Assets/TalesTactics/Audio/Original/mint.theme.wav), [벨벳](../Assets/TalesTactics/Audio/Original/velvet.theme.wav), [파라](../Assets/TalesTactics/Audio/Original/farah.theme.wav), [티아](../Assets/TalesTactics/Audio/Original/tear.theme.wav)
- [제이드](../Assets/TalesTactics/Audio/Original/jade.theme.wav), [나탈리아](../Assets/TalesTactics/Audio/Original/natalia.theme.wav), [알펜](../Assets/TalesTactics/Audio/Original/alphen.theme.wav), [시온](../Assets/TalesTactics/Audio/Original/shionne.theme.wav), [키사라](../Assets/TalesTactics/Audio/Original/kisara.theme.wav)

피드백에는 곡/캐릭터명·거슬리는 구간·원하는 변화(타악기/음색/반복 피로)를 적는다. 원작 음원 다운로드나 다른 서비스 업로드는 하지 않는다.
