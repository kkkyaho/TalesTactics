# 직업 아이콘 원화

승인된 8종 장비 실루엣을 하나의 투명 atlas로 제작했다. 게임용 파일은 `Assets/TalesTactics/Resources/TalesTactics/Ornate/ClassIcons.png` (1774×887 RGBA)이다. 내장 `image_gen` 도구의 참조 이미지 생성 모드를 사용했으며 CLI/API 우회는 사용하지 않았다. 생성 PNG의 alpha를 그대로 보존했다.

검사=검, 치유=날개 지팡이, 격투=권갑, 창술=창, 음유=리라, 궁술=활, 사격=장총, 수호=사자 방패. 크레스·벨벳·알펜은 검사 아이콘을 공유한다. 캐릭터별 정식 직업 이름과 능력은 기존 데이터를 유지한다.

`BattleClassArt`가 원화의 독립된 영역을 UV로 표시하고 비율을 보존한다. 총 개머리판이 규칙 격자 경계를 조금 넘으므로 개별 영역을 사용해 인접 아이콘이 섞이지 않게 했다. 8개 영역의 가장자리 alpha 최대값은 1/255로, 불투명한 윤곽이 잘리지 않는다. NPOT 크기 유지·mipmap 없음·무압축·bilinear·alpha transparency로 가져온다.

아래는 실제 생성에 사용한 최종 프롬프트다. 참조는 사용자가 승인한 직업 아이콘 시안이다.

```text
Create ONE production sprite atlas for a Unity fantasy tactics game, using the attached APPROVED CONCEPT BOARD as visual style and object-design reference only. Deliver a clean RGBA image with genuinely transparent background (alpha=0 outside objects). Exactly eight equipment role icons in a perfectly regular 4-column x 2-row grid, square cells, total image aspect ratio 2:1. Each icon wholly inside its cell with 12% transparent padding on every side; all objects have equal visual weight and crisp readable silhouettes suitable for 48-64 px UI. No labels, text, grid lines, panel frames, decorative border, small duplicates, character art, background, checkerboard painted into image, cast ground shadows or glow haze. One object per cell. Top row left to right: 1 engraved silver broad sword with aged gold crossguard and blue gem, angled bottom-left to top-right; 2 ivory and gold winged healing staff with jade orb, same diagonal; 3 bronze armored clenched fist/gauntlet with dark red wrist wrap; 4 silver spearhead and short haft with ornate gold mount and violet gem, diagonal. Bottom row left to right: 5 gold lyre with visible strings and turquoise jewel; 6 wooden/gold recurve bow with green-fletched nocked arrow; 7 antique fantasy walnut and brass long rifle, diagonally rising; 8 navy enamel shield with embossed gold lion and substantial gold rim. Match the approved objects closely. Premium hand-painted JRPG equipment illustrations, bright silver edges, restrained warm gold, deep recesses, no simplistic flat symbols. Each object is centered on exactly its cell center. This is the final game atlas, not another concept board.
```
