# VFX 이미지 제작 기록

2026-10-16, built-in `image_gen` 도구를 사용했다. CLI/API fallback은 사용하지 않았다. 생성 결과를 게임 원화로 연결했으며 아래 이미지는 실행 화면이 아니다.

- 채택: `Assets/TalesTactics/Resources/TalesTactics/CombatElements.png`
- 원본 생성: `exec-e6affad1-15a3-4141-a7a3-cb865cd31faf.png`
- 여백 보완 후 채택: `exec-19dd1fdb-893f-4ab5-bf7f-397065fb76d2.png`
- 네 행: 검파·얼음·번개·회복. 각 행 4프레임, 16셀. 실제 출력은 1254×1254 RGBA이며 생성 도구의 출력 크기를 그대로 보존했다. import는 NPOT rescale 없이 사용한다.
- 불꽃·연기는 기존 `CombatFire.png`를 재사용했다. 원화 사이 보간, 위치·회전·투명도, 지형 깊이 페이드와 파편은 Unity 코드·셰이더에서 적용한다.

## 최초 생성 프롬프트

```text
Use case: stylized-concept. Asset type: production transparent VFX sprite atlas for a painterly 2.5D fantasy tactics game. Create a square 2048x2048 RGBA transparent-background sprite sheet, exact 4 columns by 4 rows of equal 512x512 cells, NO visible grid, NO text, NO labels. Each row has FOUR consecutive frames of one effect, read left to right: emerging, strong contact, expansion, dissipating. Row 1: brilliant silver-blue sword energy crescent with textured layered edge, an organic sharp sweeping slash, spark fragments. Row 2: faceted turquoise ice crystals erupting upward, refractive crystalline shards, vapor wisps, breaking into fragments at end. Row 3: branching electric blue lightning impact with a bright violet-white central fork and finer irregular branches, fading glowing sparks. Row 4: emerald-gold healing magic with wispy ascending ribbons, petal-like light fragments and luminous soft bloom, no plus signs or medical symbols. Painterly sharp illustrated JRPG effects, sophisticated authored texture detail, dramatic luminosity controlled to retain colored detail, NOT simple vector shapes, NOT 3D render. Each effect fits fully within its own cell with at least 12% transparent padding on all sides, all four stages aligned at the same center/ground contact, no spill between cells. Genuine transparent alpha, no checkerboard drawn, no backdrop, no character, no scenery, no interface. Asset for runtime animation, consistent camera and scale within each row.
```

## 여백 보완 프롬프트

위 원본을 먼저 확인하고 해당 파일을 edit target으로 전달했다. 채택 원본의 알파를 보존하여 복사했으며 별도 이미지 가공은 하지 않았다.

```text
Edit target: the attached transparent 4x4 fantasy VFX atlas. Keep exactly all 16 existing effects, their four row identities (sword wave, ice, lightning, healing), their color, polished painterly texture and animation ordering. Critical production correction: each equal cell must have a generous fully transparent border on ALL four sides. Scale every individual effect down to occupy only the central 68 percent of its equal cell width and height, keeping its ground point consistently at 80 percent of cell height. Keep grid exactly 4 columns x 4 rows, square overall canvas, no grid lines. Separate every effect from adjacent cells by wide transparent gutters. No colored pixel or glow may touch a cell boundary. Entire effect silhouette including smoke/sparks must fit its own cell. Real transparent alpha background, no colored backdrop or checkerboard. Do not add text, labels, borders, characters or UI.
```

요청한 비율·여백이 모든 픽셀에서 정확히 보장되는 수작업 아틀라스는 아니다. 런타임은 동일 셀 내부 UV inset과 마지막 프레임 clamp를 사용하고 실제 전장 렌더링으로 확인한다.
