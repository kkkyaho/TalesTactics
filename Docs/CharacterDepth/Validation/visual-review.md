# 캐릭터 입체감 시각 검토

이번 변경은 기존 SD 원화와 2.5D 전장의 접지·그림자·명암 보완이다. 전체 게임의 미술 완성도나 모든 네이티브 프레임을 평가한 점수가 아니다.

palette discipline: 4/5 — 장별 기존 환경색과 남색·금색 HUD를 유지하고 바닥 대비를 낮췄다. 실제 Play 중 재질 스캔에서 Default-Material0, 글꼴 Korean, 파티클0을 확인했다. 흰 Image tint는 원본 일러스트 색상을 유지하는 용도이며 무채색 기본 패널로 판정하지 않는다.

style uniformity: 4/5 — 기존 원화·윤곽·비율이 유지되고 추가 그림자에 검은 사각판이나 메시 경계가 드러나지 않는다.

lighting depth: 3/5 — 근접에서 진한 발 접촉 음영과 옆으로 드리운 옅은 몸 그림자가 구분된다. 전체 전장에서는 절제된 효과이며 실제3D 체적 조명은 아니다.

silhouette: 4/5 — 근접 인물과 무기 윤곽이 유지되고 반대 카메라에서도 그림자가 사라지지 않는다. 멀리 줌을 뺀 전장에서는 기존 작은 캐릭터 크기의 한계가 남는다.

UI intentionality: 4/5 — 남색·금색 명령/기술 창과 하단 고정 상태창이 유지된다. 확인한1024×768 기술창에서 글자·버튼 잘림이 보이지 않는다.

feedback visibility: 4/5 — 명령·기술 선택과 HP/MP 상태가 계속 읽히고 그림자가 선택·위험 표시보다 먼저 그려진다. 이번 정지 화면은 기술 발동 VFX의 평가가 아니다.

첫 화면 뒤 보완: 몸 그림자의 길이 `.28→.38`, alpha `.19→.22`로 약하게 보이던 부분을 보강했다. 사전 수학/GPU 검토에서 기본 카메라 투영의 폭 붕괴를 보완했고 표시 순서는 `-5`로 내려 기존 위험 범위를 가리지 않게 했다.

최종 Editor 실행에서 직접 열어 본7장:

- 전장 `00-episode-01-idle`, `02-episode-01-idle-r180`, `04-episode-01-character-close`, `56-episode-17-idle`, `82-training-six-character-close`, `89-training-six-skill-ui-1024x768`.
- HUD `31-zoom-text130-allies6-1280x720`.

전장 결과는 [editor-environment-final.json](editor-environment-final.json), HUD 결과는 [editor-hud-final.json](editor-hud-final.json), 실제 스캔은 [palette-scan.json](palette-scan.json)에 보존했다. 90+32개 자동 상태 검증과7장 육안 검토는 서로 다른 근거다. AI 목업을 실제 구현 증거로 세지 않는다. 최종 보관 화면은 아래 Windows 실행 캡처로 갱신했다. Editor 원시 보고서와 당시 로그 캡처는 별도로 유지한다.

최종 Windows 전장 실행 `e0c8448d840c4417bd9c45acbb7a7cb7`의 `00-episode-01-idle-r0`, `02-episode-01-idle-r180`, `04-episode-01-character-close`, `82-training-six-character-close`, `89-training-six-skill-ui-1024x768`와 HUD 실행 `cad70cc0014d436da4e214510059404f`의 `00-command-allies2-1920x1080`, `31-zoom-text130-allies6-1280x720` 총7장을 직접 열어 검토했다. 모두 정상 화면이며 근접 접촉 음영과 방향성 몸 그림자가 읽힌다. 검토한 반대 방향에서 실루엣 붕괴가 없고 사각판·거친 메시 경계·눈에 띄는 맵 밖 번짐이나 발의 부유감이 보이지 않았다. 작은 기술창과130%글자 명령/상태 패널에서도 잘림이 보이지 않았다. 90+32장 전수 육안 검토나 연속 모션 검수 결과는 아니다.

첫 Windows 환경 실행 `c7dde6e55fe2480a8a62cca24ea79101`의 초기10장은 검정이어서 통과 증거에서 제외했다. 검수 보고서의90/90과 별개로 래퍼의 비균일 PNG 검사가 실패 처리했다. 숨김 시작 크기를1024×768로 바꿔 첫1280×720 해상도 전환을 실제 수행하고, 실제 검수 창을 한 번 활성화한 별도 실행에서90장 전체 비균일 검사·정상 종료0·저장 불변을 확인했다. HUD32장도 별도 실행에서 같은 검사를 통과했다. 제품 원화·코드와 요구 캡처 해상도/판정 기준은 변경하지 않았다.

[Windows 전장 검증](windows-environment-validation.json) · [Windows HUD 검증](windows-hud-validation.json) · [실제 전후 비교](../comparison.html) · [대표 PNG4장 출처/SHA](../Evidence/actual-capture-manifest.json) · [전후 카메라 일치](../Evidence/before-after-camera.json).
