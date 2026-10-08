# 인터미션 메뉴 아트

장비 관리·장비 상점·성장/승급 승인 시안을 실제 uGUI로 구현한다. 기존 FormationPortraits와 FormationBackdrop을 재사용한다.

`Assets/TalesTactics/Resources/TalesTactics/EquipmentIcons.png`는 내장 image_gen 도구로 생성한 3×3 장비 아이콘 아틀라스다. 폰트·수치·버튼·클릭 영역은 이미지에 포함하지 않고 실제 게임 데이터와 uGUI로 그린다. 아이콘 선택은 카탈로그 순서가 아닌 ID를 사용한다. 치유 휘장은 수호의 메달 그림을 공유하되 실제 이름·효과는 해당 데이터에서 읽는다. 신규 미등록 아이템에는 부위별 벡터 아이콘을 사용한다.

셀 순서(왼쪽부터): 청동검 / 철검 / 가죽 갑옷; 강화 갑옷 / 생명의 부적 / 수호의 메달; 단련 갑옷 / 기동 장화 / 집중 부적.

원본: `C:/Users/USER-PC/.codex/generated_images/01a11681-0831-7cd3-8350-680670d8afd9/exec-e20390c0-daa3-4bb3-9aa9-989078d93f0d.png`

최종 생성 프롬프트:

> Use case: game asset sprite atlas. Create a perfectly regular 3 columns by 3 rows equipment icon atlas, square image, 9 equal square cells. Painted high-quality tactical JRPG inventory icons, silver/brass metals, restrained vivid jewels, navy background exactly solid #101b2b everywhere with NO borders, NO labels, NO lettering, NO stars. Each object isolated in exact center of its own cell, fits inside middle 75 percent with equal margins; each clearly legible at 100px. Top row left to right: bronze short sword diagonal up-left with brown leather hilt; steel longsword diagonal up-left with brass hilt; brown leather chest armor with shoulder plates. Middle row: reinforced silver chest armor over dark blue cloth with gold trim; red ruby life amulet on gold chain; round protective silver medallion with teal shield crest. Bottom row: heavy tempered dark steel armor with brass edges; pair brown leather movement boots with small teal wing accent; purple crystal focus charm in silver circular setting. Exact 3x3 equal grid, no crossing cell boundaries. Consistent hand-painted fantasy equipment style, subtle highlights, only objects and solid opaque navy background; no glow clouds, no texture behind objects. This is an actual game resource sheet, not a screenshot or presentation.

반환된 이미지의 알파 채널을 보존했다. Unity에서는 밉맵 없이 Bilinear/Uncompressed로 읽어 각 셀을 RawImage UV로 표시한다. 시안의 예시 골드·레벨·능력치와 미구현 수량 조절은 하드코딩하지 않는다. 거래는 기존과 같이 1개씩 처리한다.
