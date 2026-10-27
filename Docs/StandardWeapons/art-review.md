# Standard weapon icon art review

Date: 2026-10-24

## Result and scope

PASS for the reviewed source illustration, seven named sprite rectangles and equipment identity mapping. The icons satisfy the user's direction to show recognizable illustrated equipment instead of basic geometric placeholders.

This was a read-only visual/source review. It did not run Unity, modify pixels or metadata, or verify the icons at the actual shop/inventory UI size.

Reviewed files:

- C:/dev/TalesTactics/Assets/TalesTactics/Art/StandardWeapons/icons.png
- C:/dev/TalesTactics/Assets/TalesTactics/Art/StandardWeapons/icons.png.meta
- C:/dev/TalesTactics/Tools/standard-weapon-icons.csv
- C:/dev/TalesTactics/Assets/TalesTactics/Editor/StandardWeaponContent.cs

## Visual findings

The seven icons use consistent dark outlines, broad metal highlights and brown wood/leather surfaces. Their silhouettes are distinct. Silver fittings and restrained gold details give them a coherent ordinary-equipment appearance; the staff's blue crystal provides a readable magical exception.

| Equipment ID | Visible construction and identity | Finding |
|---|---|---|
| training-staff | Blue crystal in a silver socket, shaped wooden shaft, wrapped grip and metal end cap. | Clearly a magical staff; no floating crystal or broken shaft. |
| steel-claw | Three curved steel blades with separate riveted bases, leather cuff and buckle. | Clearly a claw weapon, distinct from the fist icon; blades remain separated. |
| steel-fist | Articulated knuckle/hand plates, visible thumb, leather cuff and strap. | Clearly an armored gauntlet; no obvious detached plate or extra hand. |
| training-spear | Faceted metal spearhead, socket, straight wooden haft, wrapped grip and metal butt. | Simple spear construction fits the training-weapon identity; both ends are present. |
| reinforced-bow | Curved wooden limbs, metal reinforcement, grip and continuous string. | Clearly a bow; both tips and the string are visible in the source. |
| infantry-rifle | Wooden stock, trigger guard/lock, long metal barrel, front muzzle and stock cap. | Coherent fantasy-era firearm; no detached barrel or missing stock. |
| steel-shield | Bordered metal face, rivets, central boss and pointed lower end. | A designed shield rather than a plain polygon; the entire border and tip are present. |

No obvious weapon-body clipping, unintended duplicate equipment, embedded text or UI decoration was found inside the reviewed sprite frames. The full atlas contains faint cell-edge residue, discussed below.

## Rectangles, alpha and identity checks

- PNG: 1774 × 887, RGBA; fully transparent pixels comprise 72.7226% of the atlas.
- Source GUID: 97227cce3af8ec9459f70a3e9e5fa25d.
- Metadata contains exactly seven sprite records, seven unique Sprite IDs and seven unique local IDs.
- Every metadata name/rect exactly matches its row in standard-weapon-icons.csv.
- All seven CSV source dimensions are 1774 × 887, matching the PNG.
- The seven expected IDs match StandardWeaponContent's equipment definitions and imported-icon lookup.
- Metadata PPU is 100; NPOT scaling is disabled. This is a source-setting observation, not an engine-rendering result.

Minimum margins between each illustration's alpha bbox and its sprite rectangle:

| ID | Minimum margin, alpha>128 | Minimum margin, alpha>8 | Maximum alpha on rectangle boundary |
|---|---:|---:|---:|
| training-staff | 5 px | 3 px | 1 |
| steel-claw | 5 px | 3 px | 1 |
| steel-fist | 5 px | 4 px | 1 |
| training-spear | 5 px | 4 px | 1 |
| reinforced-bow | 5 px | 3 px | 1 |
| infantry-rifle | 5 px | 4 px | 1 |
| steel-shield | 5 px | 4 px | 1 |

All alpha>128 pixels in the complete atlas lie inside the union of the seven sprite rectangles. This supports the absence of cut strong silhouettes. The visible/main antialiasing also has space inside each rect at alpha>8.

The entire atlas is not perfectly clean at very low alpha: 25,392 alpha>0 pixels lie outside the seven rectangles, including faint grid-edge residue visible in the full-sheet preview. Only six of those pixels exceed alpha 8, and none exceed alpha 128. Each sprite rectangle also has some alpha=1 boundary pixels, so its border is not mathematically all zero. These are faint residues, not cropped solid equipment. No source cleanup was performed.

## Remaining verification

Actual shop/inventory rendering was outside this review. At the intended UI size, check that the bowstring, claw gaps, rifle muzzle and staff crystal remain readable and that no low-alpha fringe becomes conspicuous against the UI background. This limitation does not change the source-art/rect review result.

## Reviewed hashes

- icons.png SHA-256: 9bea2acad54d57da8fbb24423f2d52df2233e210443a1ceb26ea858811f333b0
- icons.png.meta SHA-256: 6d2e2f12e5d7cddced8b411e2f83a4521d8c3a3085a49326e3405a91f372d218
