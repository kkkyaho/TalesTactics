# Cless native quadrant review — 2026-10-24

## Scope and disposition

This review compared four generated native quadrant PNGs with the existing production motion atlas. It did not operate Unity, replace production art, edit metadata or approve a final animation.

- Source: C:/dev/TalesTactics/Assets/TalesTactics/Art/MotionPremium/cless.png (1254 × 1254, 80 SpriteRects).
- Inputs: C:/dev/TalesTactics/Temp/SdPolish20261024/ClessQuadrants/q0.png through q3.png (each 1122 × 1402, 20 figures).
- Quadrant order: q0 top-left; q1 top-right; q2 bottom-left; q3 bottom-right.
- Input rows and columns below are one-based, from the top-left. Animation clip indices are zero-based.
- Frame names and current clip indices were verified against cless.png.meta and Assets/TalesTactics/Content/Characters/cless.asset.

The new art has visibly clearer eyes, hair locks, armor boundaries, hands and blade planes. No obvious second sword, floating arm or detached head was found in the visible parts of the 80 figures. Cape occlusion prevents verification of every hidden limb. This is an improvement in readable native illustration detail, not proof of perfect frame-by-frame pose preservation or smooth animation.

The source alpha>128 bbox height has median 79 pixels (range 71–103). The new full strong-component median heights are q0 208, q1 221, q2 192 and q3 215 pixels. These are native texture measurements, not rendered world-size measurements. Earlier per-cell measurements clipped some long blades and are not used as full-figure measurements.

## Concrete pose changes and animation checkpoints

| Input / frame | Difference from the reviewed production atlas | Current clip checkpoint |
|---|---|---|
| q2 r4c2 / cless_16_1 | Raised empty hand moves from screen-right to screen-left in the back view. | Skill[12], Ultimate[17], Back; inspect transitions to cless_17_1. |
| q2 r5c2 / cless_18_1 | Exposed shoulder/arm and cape sweep change sides. | Ultimate[19→20], Back. |
| q2 r1c3 / cless_10_2 | Horizontal rightward thrust becomes a sword lowered toward screen-right. | Ultimate[8], Right. |
| q1 r5c3 / cless_9_2 | Blade angle changes from the left side near/above the head to upper-right. | Skill[5], Ultimate[7], Right. |
| q3 r1c3 / cless_11_2 | Blade angle changes from the left side above the head to upper-right. | Ultimate[11], Right. |
| q2 r3c3 / cless_14_2 | Recovery pose becomes more upright/front-facing than the prior leaning pose. | AttackMotion[8→9], Right; also Skill[10→11] and Ultimate[15→16]. |
| q0 r4c2 / cless_6_1 and q1 r4c2 / cless_7_1 | A downward blade is now visible beside the rear cape where the old pose largely concealed it. This is not an extra sword. | AttackMotion[2→3], Back. |

All 20 back-facing slots remain back-facing; no obvious back/front slot swap was found. Subtle side-view orientation, changing hand positions, limb proportions and weapon arcs still require continuous playback. Right corresponds to the current asset's _2 sprites, Left to _3.

Recommended actual-animation checks:

1. Demon Fang / AttackMotion in Front and Right, through anticipation, release, thrust and recovery; inspect body size/grounding and weapon continuity.
2. Back-facing Skill late frames and Ultimate[17→20], for the raised-hand/shoulder-side changes.
3. Right-facing Ultimate[7→12], for the reinterpreted sword angles and changes between quadrant art and existing release/cast frames.
4. Idle-to-motion transitions at actual game resolution, since a sharper native action sheet may reveal style or size differences against the other sheets.

## Input layout and transparency evidence

Measurements use eight-connected components and Euclidean distance between pixel centers. A major component contains at least 1000 alpha>threshold pixels.

Each input has exactly 20 major alpha>128 figures. Figures remain distinct at alpha>8. Many blades/capes cross the nominal equal 4×5 input grid even though the strong figures remain separated.

| Input | Major components alpha≥1 | alpha>8 | alpha>16 | alpha>32 | alpha>64 | alpha>128 | Minimum gap between alpha>128 figures |
|---|---:|---:|---:|---:|---:|---:|---:|
| q0 | 20 | 20 | not independently counted | not independently counted | not independently counted | 20 | 47.0000 px |
| q1 | 19 | 20 | 20 | 20 | 20 | 20 | 18.0000 px |
| q2 | 15 | 20 | 20 | 20 | 20 | 20 | 8.2462 px |
| q3 | 20 | 20 | not independently counted | not independently counted | not independently counted | 20 | 23.0000 px |

Minimum-gap pairs: q0 cless_6_2 / cless_8_2; q1 cless_7_2 / cless_9_2; q2 cless_16_2 / cless_18_2; q3 cless_13_0 / cless_13_1.

Nominal cell boundary touch/overflow at alpha>128: q0 6 figures, q1 7, q2 8, q3 7. Examples, using top-left coordinates and exclusive maxima:

- cless_12_0, q2 r2c1: bbox x=23..316 while nominal cell ends at x=280.
- cless_13_0, q3 r2c1: bbox x=30..327 while nominal cell ends at x=280.
- cless_18_2, q2 r5c3: bbox y=1088..1350 while nominal row starts at y=1121.

The alpha>0 connections below disappear at alpha>8; their bridges therefore depend on alpha 1–8 pixels:

| Input | Connected pair | Minimum strong-silhouette distance |
|---|---|---:|
| q1 | cless_7_2 / cless_9_2 | 18.0000 px |
| q2 | cless_10_0 / cless_10_1 | 14.0357 px |
| q2 | cless_12_0 / cless_12_1 | 17.6918 px |
| q2 | cless_14_0 / cless_14_1 | 19.7990 px |
| q2 | cless_16_2 / cless_18_2 | 8.2462 px |
| q2 | cless_18_0 / cless_18_1 | 12.1655 px |

Read-only input alpha>0 pixel counts: q0 644167, q1 745008, q2 667628, q3 670881; total 2727684. Detached alpha>8 small components: q0 64 components / 87 pixels; q1 77 / 104; q2 12 / 14; q3 22 / 43. These pixels must not be silently discarded.

Canvas-edge nontransparent pixels: q0 0, q1 13, q2 31, q3 0. Every q1/q2 edge pixel has alpha exactly 1; there are no alpha>8 edge pixels. The packer preserves all 44 edge pixels through the same nearest-owner assignment and includes them in each figure's complete support bbox. Only alpha>8 artwork touching the input canvas edge is rejected as a potential clipped figure; weak edge pixels are counted in inputCanvasEdgeWeakPixels. No edge pixels are removed.

## Authorized technical packing policy and verification limits

The initial strict equal-cell packer correctly could not accept these layouts. The subsequent strict alpha>0 component rule also rejected q1/q2 because of their faint bridges. After the measured thresholds and gaps were reported, the parent explicitly authorized relaxing only the alpha 1–8 ownership rule:

- Read the original 80 SpriteRects and preserve their names, Sprite IDs and local IDs in the report.
- Match the 20 alpha>128 figures in each quadrant one-to-one to source positions using the median of the figure pixels, not the blade-extreme bbox center.
- Require exactly 20 major alpha>8 regions, each containing one alpha>128 major figure. A region containing multiple major figures is still rejected.
- Assign isolated alpha>8 fragments only when the closest main region is unambiguous.
- Assign every alpha 1–8 pixel to the nearest final alpha>8 figure pixel using exact Euclidean pixel-center distance. Resolve different-owner ties by the original frame index and log the count.
- Preserve every nontransparent pixel's RGBA value and copy it exactly once by integer translation. Do not draw, resize, sharpen, remove noise, alter alpha or cut the image at arbitrary cell boundaries.
- Permit nominal input-cell crossing, and record it. The output must place each complete owned figure within its original normalized 8×10 cell with a transparent gutter, on a square canvas no larger than 4096.
- Record SharedWeakComponents, splitWeakPixelCount, weak tie counts, minimum strong gap, frame bounds/ratios and source/output pixel counts. splitWeakPixelCount means all alpha 1–8 pixels in an alpha>0 component that originally contained multiple major figures.
- Verify the encoded PNG by decoding it and comparing every output RGBA byte with the pre-encoding copy buffer. This proves technical pixel preservation, not visual pose or animation correctness.

SdMotionAtlasPacking.cs compiles against the installed Unity 6000.6 API. Its actual C# nearest-owner method was independently executed outside the Unity Editor and compared with exhaustive distance calculations for 22399 queries, including 1150 multiple-owner ties in the dense probes; all passed. This probe covered sparse regions, image edges and dimensions not divisible by the spatial-bin size.

This review did not execute the complete Unity packing API or import the result. Actual packed PNG/report, downstream Python measurement, Sprite ID/rect/PPU audit and animation playback remain the integration owner's checks.

## Input hashes

SHA-256 of reviewed files:

- Existing cless.png: 16ececda2c0b75a8aacf0fdd7d7a3455161418c6628e82eff500c4768f2908d0
- q0.png: a7351a5dbd48d97695791e64282d128c6d185e7d00bfa1ec0aa79f9cb2d9cc88
- q1.png: f1e4479092503c65c90b8bab6be60a2db15d3afd71a2ee3888a1a7e21858e4ec
- q2.png: 928a7b029c9a7783213f899c3046f00211a2a5dbf4127ec2e9e8d7db467a24e5
- q3.png: 6b52134de6bad6e5155203ab071475b6cea904bab7e0e3c55263f527912b905e

## Integration failure: source-offset distance versus original slot

The integration owner attempted Pack in the Unity Editor and supplied this error for read-only diagnosis:

- D:/TalesTacticsHelpGuide/Temp/SdPolish20261024/cless-pack-error.txt
- Initial exception: Ambiguous major-figure position in quadrant 2 at (991.00, 680.00), AssignInput.
- No packed PNG was produced by this failed attempt.

That median belongs unambiguously to q2 r3c4, original frame cless_14_3. The initial algorithm used the original illustration's exact within-cell body offset as part of its assignment-confidence gate:

| Initial comparison | Expected center in q2 pixels, bottom-left | Distance in cell units |
|---|---|---:|
| Correct frame cless_14_3 | (969.8947, 796.0319) | 0.4205933 |
| Runner-up cless_16_3 | (986.0000, 520.9984) | 0.5673329 |

The runner-up distance margin was 0.1467395, just below the initial 0.15 gate. This was not a collision or a wrong nearest frame. The native redraw re-centered the body within its logical cell. Its median (991,680) lies inside the actual integer cell x=[841,1122), y=[561,842), at least 119 pixels from an edge (0.42349 cell on the limiting axis).

The revised mapping does not simply lower that distance threshold. Original source quadrant/row/column slots are authoritative. Each major figure median must:

1. Occupy a distinct 4×5 slot, giving exactly 20 occupied slots per quadrant.
2. Remain at least 0.15 cell inside the slot boundary on both axes.
3. Agree with the nearest original source-body frame name.
4. Retain the existing maximum source distance of 0.75 cell.

The source distance and runner-up margin remain in the JSON as diagnostics; assignmentCellInteriorMargin is added. This changes only identity assignment confidence. Source Sprite IDs, frame order, PNG pixels, weak-pixel ownership, integer placement and output validation are unchanged.

Independent read-only measurements of all 80 actual source/input figures passed these revised conditions:

| Quadrant | Unique correct slots | Minimum interior margin | Maximum source distance | Frames rejected by the old runner-up gate |
|---|---:|---:|---:|---:|
| q0 | 20 | 0.342857 | 0.120482 | 0 |
| q1 | 20 | 0.353571 | 0.085269 | 0 |
| q2 | 20 | 0.275000 | 0.474112 | 5 |
| q3 | 20 | 0.271429 | 0.429089 | 5 |

Every slot's original frame name independently matched the nearest-source name. The corrected C# file compiled against the installed Unity API. A successful full Unity Pack after this correction was not part of this review; the integration owner runs it after current PlayMode testing.

## Second integration failure: fixed within-cell centers — 2026-10-27

The integration owner's second actual Unity Pack passed figure assignment but failed in FindSize:

- Evidence: D:/TalesTacticsHelpGuide/Temp/SdPolish20261024/cless-pack-2-error.txt.
- Exception: Native pixels cannot fit the source-normalized 8x10 layout at or below 4096. No resizing is permitted.
- No PNG was produced by that attempt.

The actual C# AssignInput and FindSize methods were executed in an external .NET process against all four unchanged native inputs and the source metadata. This is an engine-independent algorithm probe, not a Unity Pack or import result. Python only decoded read-only inputs into temporary measurement buffers and read original sprite rectangles; it did not create or alter any image. The old FindSize failure was reproduced after AssignInput retained all 2,727,684 nontransparent pixels.

The complete per-frame alpha>0 support boxes have maximum width 323 and maximum height 324 pixels. Every support box fits the corresponding 4096-square output cell (512 wide, 409 or 410 high) with a one-pixel transparent gutter. The blocker was the exact original normalized alpha>128 bbox center, not excessive native image dimensions. Forty-three frames were placed too high within their correct cells; no frame needed a horizontal correction at 4096.

For the worst frame, cless_17_3 (q3 r4c4), all coordinates below use bottom-left origin and exclusive right/top edges:

- Native strong bbox: (897, 299, 189, 257).
- Native full support bbox: (864, 271, 227, 294).
- Original normalized strong bbox center: (0.9342105, 0.1909888), giving (3826.5263, 782.2903) on the 4096 output.
- Original cell: x=[3584,4096), y=[410,820).
- Preferred integer translation: (2835,355). It would place support y=[626,920), 101 pixels beyond the one-pixel-gutter limit yMax=819.
- Allowed translations: x=[2721,3004], y=[140,254]. The nearest allowed translation is (2835,254), a correction of (0,-101).

Largest required corrections at 4096:

| Frame | Native full support bbox x,y,w,h | Cell x,y,w,h | Preferred Y translation | Allowed Y translations | Integer correction x,y |
|---|---|---|---:|---|---|
| cless_17_3 | 864,271,227,294 | 3584,410,512,410 | 355 | 140..254 | 0,-101 |
| cless_16_0 | 15,273,297,276 | 0,410,512,410 | 367 | 138..270 | 0,-97 |
| cless_14_1 | 305,574,280,251 | 512,820,512,409 | 498 | 247..403 | 0,-95 |
| cless_17_0 | 35,292,224,265 | 2048,410,512,410 | 355 | 119..262 | 0,-93 |
| cless_17_1 | 325,262,246,286 | 2560,410,512,410 | 359 | 149..271 | 0,-88 |
| cless_17_2 | 570,260,236,297 | 3072,410,512,410 | 348 | 151..262 | 0,-86 |
| cless_18_2 | 592,46,252,272 | 1024,0,512,410 | 171 | -45..91 | 0,-80 |
| cless_15_0 | 36,556,233,277 | 2048,820,512,409 | 473 | 265..395 | 0,-78 |
| cless_14_0 | 17,550,306,271 | 0,820,512,409 | 484 | 271..407 | 0,-77 |
| cless_16_1 | 302,252,257,290 | 512,410,512,410 | 353 | 159..277 | 0,-76 |

The parent authorized relaxing the exact center constraint while preserving each original cell, frame name, Sprite ID and every native RGBA pixel. The revised packer first retains the existing centered-layout search. Only if no such layout fits at or below 4096 does it use the maximum 4096 canvas and clamp each preferred integer translation independently to its permitted X/Y range. This is the minimum displacement for each frame on that canvas. Choosing a smaller atlas would require additional displacement: a 3253-square layout is dimensionally possible but would adjust 62 frames by up to 109 pixels, versus 43 frames by up to 101 pixels at 4096. The policy therefore keeps the extra cell space instead of minimizing the canvas at the cost of more placement changes.

The output report schema is now 2. Each frame records preferredTranslationX/Y and cellFitAdjustmentX/Y alongside actual translationX/Y, full support bounds, original identities and normalizedCenterErrorPixels. The report also records adjustedFrameCount and maximumCellFitAdjustmentPixels. A full native support that cannot fit even after integer translation still fails with its frame name, support bbox and cell dimensions. No resampling, cropping, alpha cleanup, color change, source/meta write or slot reassignment was added.

Verification performed outside the Unity Editor:

- Revised source compiles against installed Unity 6000.6 API references.
- Actual AssignInput plus FindSize returns 4096 for the four native inputs; exactly 43 corrections, all vertical, maximum 101 pixels. The other 37 preserve their preferred integer centers.
- All 2,727,684 nontransparent RGBA pixels were copied once into an in-memory atlas and byte-compared against their original input pixels. Counts, non-overlap, all 80 cell gutters and minimum per-axis displacement passed. No PNG was emitted by this probe.
- A synthetic already-centered layout retained the old behavior and selected size 220 without a correction.
- A synthetic support wider than the maximum cell's gutter capacity was rejected with the specific-frame diagnostic.

Full Unity Pack PNG encoding/decoding, downstream measurement/import, pivot/PPU and real animation checks remain unperformed by this reviewer. Corrected texture positions must be consumed by the new measurement rather than assuming old normalized centers were preserved. Actual grounding and motion continuity are not established by this technical pixel-preservation result. The earlier visual pose-change checkpoints remain applicable.

## Actual Windows motion visual review — 2026-10-27

Disposition: **technical execution passed; visual consistency requires further work**. The new MotionPremium art is clearer, but the mixed-sheet animation is not visually approved as a completed style conversion.

Evidence root: D:/TalesTacticsHelpGuide/Builds/SdPolishReview20261027/SdMotionReviews/1a672154897b4949a66be2ab97eb300a/.

The supplied result.json reports 12/12 completed actions, 684 continuous frames at 30 fps, 12 battlefield references, all six watched sprites observed, and no recorded errors. The per-run counts are 4×45 attack + 4×54 skill + 4×72 ultimate = 684; including battlefield references gives 696 PNGs in total. Its scope is actual Director attacks/skills with six simulated allies using the existing staged SkillRosterVfxFixture, not campaign or balance testing. The 960×540 close-ups render only Cless against a plain background; other units and effects remain in the simulation. A technical passed result does not evaluate body-proportion/style consistency. The inspected result.json SHA-256 is 4c7c4869dbd9212d8ced2f6dc6db08ea8430a5423a0c93b05ca66b4c47ec521d.

This reviewer directly opened 18 unmodified close-up PNGs and one unmodified 1920×1080 battlefield reference with view_image. This was comparison of selected still frames, not playback of all 684 motion frames. The complete personally inspected set, relative to the evidence root:

- cless.attack-Right/0023.png, 0024.png.
- cless.2-Back/0037.png, 0038.png.
- cless.ultimate-Back/0058.png, 0059.png, 0061.png, 0062.png.
- cless.ultimate-Right/0000.png, 0016.png, 0017.png, 0020.png, 0022.png, 0030.png, 0031.png, 0034.png, 0065.png, 0066.png, battlefield.png.

Watched-pose timeline mapping (zero-based capture frame numbers):

| Sprite | Recorded occurrence ranges | Personally inspected occurrence |
|---|---|---|
| cless_16_1 | cless.2-Back 38–41; cless.ultimate-Back 52–54 | cless.2-Back/0038.png, preceded by 0037 |
| cless_18_1 | cless.ultimate-Back 59–61 | 0059 and 0061, with 0058/0062 neighbors |
| cless_10_2 | cless.ultimate-Right 20–21 | 0020, with 0017 and 0022 comparison |
| cless_9_2 | cless.2-Right 10–11; cless.ultimate-Right 17–19 | cless.ultimate-Right/0017.png, preceded by 0016 |
| cless_11_2 | cless.ultimate-Right 31–33 | 0031, with 0030/0034 neighbors |
| cless_14_2 | cless.attack-Right 24–27; cless.2-Right 30–33; cless.ultimate-Right 45–47 | cless.attack-Right/0024.png, preceded by 0023 |

### Confirmed visible issues

1. **Body proportions and outline style change abruptly between sheets.** In cless.ultimate-Right, 0016 (cless-hq_2_2) is a slender, small-headed preparation pose; 0017 (cless_9_2) changes to a much larger head, broader boots/body and thick ink contours. The same mismatch reverses between 0020 (cless_10_2) and 0022 (cless-extra_2_2), returns between 0030 (cless-extra_3_2) and 0031 (cless_11_2), and reverses again at the directly adjacent 0065 (cless_19_2) → 0066 (cless-polish_3_2). The head-to-body proportions change, so a uniform world-scale change alone would not make these illustrations consistent. Frame 0000 establishes the older slender idle style. Blade-inclusive sprite bounds are not measurements of anatomical height.
2. **Right-facing recovery reverses the visible blade direction.** cless.attack-Right/0023.png (cless_13_2) shows a low lunge with the blade extending to screen-right; 0024.png (cless_14_2) becomes a near-front-facing, more upright pose with the blade extending down-left. This is a conspicuous pose transition in adjacent frames. The timeline still names the expected _2 sprite and Right direction; the observations do not establish a wrong Sprite ID or import mapping. The source target is q2 row 3, column 3 (one-based), cless_14_2.
3. **Back-facing late recovery has a lateral visual jump and reverses the exposed shoulder/cape silhouette.** cless.ultimate-Back/0058.png (cless_17_1) has an overhead sword; 0059.png (cless_18_1) shifts the body to the right and exposes the opposite-side shoulder, then 0061→0062 (cless_18_1→cless_19_1) shifts visibly left again. The recorded sprite-bounds center X changes about +49.4 pixels at 58→59 and -80.8 pixels at 61→62 while camera yaw/orthographic size stay fixed. These bounds include cape/weapon shape and ongoing action motion; the figures are diagnostic evidence of the visible transition, not proof of an incorrect foot pivot or an importer-scale bug. This transition should be rechecked after the pose revisions.

The enlarged native action drawings show clear hair/armor/blade outlines. No obvious detached head, extra sword or missing visible arm was found in the selected frames. In cless.2-Back 0037→0038, the left-side arm raises coherently and both visible boots remain near the same screen baseline; the earlier source-pose reinterpretation is not itself a visible break in this inspected pair. Hidden hands/arms behind the cape cannot be certified from these views.

### Grounding and verification limits

The close-up camera excludes the battlefield surface and a usable ground-contact reference. Consequently these images do not establish whether the action feet slide, float or penetrate the ground. The single inspected battlefield.png is an idle reference with the surrounding party; it is not a ground-contact capture of the watched action poses. A body/sword silhouette crossing the viewport was not seen in the selected close-ups. No code, animation, image, metadata or Unity state was changed by this visual review.

### Cless source-sheet inventory and selected consistency correction

The following paths and .meta sprite counts were read directly from the C project. Manifest indices below are zero-based and refer to Docs/SdCharacters/production-manifest.json at this review; the source path is the stable job identifier. All listed original Sprite IDs and local IDs must be retained by subsequent art updates.

| Manifest job | Current production source under C:/dev/TalesTactics/ | PNG size | Sprite count | Referenced role / current measurement |
|---:|---|---:|---:|---|
| 7 | Assets/TalesTactics/Art/PixelCampaign/cless-hq.png | 1254×1254 | 16 | Idle/base sprites, Attack/AttackMotion, Cast, Guard, Skill, Ultimate; Docs/SdCharacters/cless-hq-measurement.json |
| 6 | Assets/TalesTactics/Art/PixelCampaign/cless-extra.png | 1254×1254 | 16 | Walk and Ultimate; Docs/SdCharacters/PixelCampaign-cless-extra-measurement.json |
| 9 | Assets/TalesTactics/Art/PixelCampaign/cless-polish.png | 1254×1254 | 16 | Ultimate entry/recovery; Docs/SdCharacters/PixelCampaign-cless-polish-measurement.json |
| 8 | Assets/TalesTactics/Art/PixelCampaign/cless-motion.png | 1254×1254 | 16 | Walk, Dead and Damage; Docs/SdCharacters/PixelCampaign-cless-motion-measurement.json |
| 63 | Assets/TalesTactics/Art/MotionPremium/cless.png | 4096×4096 | 80 | Newly packed Walk/AttackMotion/Skill/Ultimate; Docs/SdCharacters/2026-10-24/cless-native-v3-production.json |

The three existing sheets involved in the inspected cross-sheet transitions total 48 frames. PixelCampaign/cless-motion.png supplies another 16 Walk/Dead/Damage frames, so the existing ordinary-weapon reference family is four sheets / 64 frames. Original GUIDs: hq 1ed9e9d0946bec441b492ad3d089a515; extra 095a10fa5dd13cb409ff7ea4f046f4fa; polish c3a095f75a23401429bffc0e59b7ac50; motion e4e4c44ad7752524e8a16c9605524cdb; MotionPremium 40e5b31d6d64b0a48b8b17f727aebee9.

Historical manifest candidate paths for these four 1254-square sheets currently do not exist at their recorded Assets/SdCharacters locations; the production paths above exist and were measured. The newly packed candidate Assets/TalesTactics/Art/SdCharacters/cless-native-v3.png does exist. This is a path inventory, not a request to regenerate old candidates or modify the manifest.

The eternal-sword equipment override is a separate reference family for checking that the character retains the same style when equipped. Assets/TalesTactics/Art/SignatureWeapons/cless-appearance.asset contains 144 original→equipped frame mappings. Its five production sheets are all 1254×1254:

| Manifest job | Production source under C:/dev/TalesTactics/Assets/TalesTactics/Art/SignatureWeapons/ | Sprite count | Measurement under Docs/SdCharacters/ |
|---:|---|---:|---|
| 112 | cless-extra.png | 16 | SignatureWeapons-cless-extra-measurement.json |
| 113 | cless-hq.png | 16 | SignatureWeapons-cless-hq-measurement.json |
| 114 | cless-motion.png | 16 | SignatureWeapons-cless-motion-measurement.json |
| 115 | cless-polish.png | 16 | SignatureWeapons-cless-polish-measurement.json |
| 116 | cless-premium.png | 80 | SignatureWeapons-cless-premium-measurement.json |

All 144 signature frames are active in the manifest. This capture set does not visually validate the equipped-weapon variants. Ordinary plus equipped source inventory totals ten sheets / 288 frames, of which the current new native MotionPremium atlas is one sheet / 80 frames. The remaining nine production sheets account for 208 frames. No source sheet was edited during this inventory.

After this comparison, the integration owner confirmed that the existing Cless family already uses a consistent approximately three-head SD proportion. The selected correction is to align the **new four native MotionPremium quadrants** to PixelCampaign/cless-hq.png's existing body/head proportions, eye scale and line weight while retaining improved native detail. It is not a redesign of the other nine production sheets. The inventory above establishes the reference family and identity scope, not a requirement to regenerate 208 established frames. The owner also reported a targeted cless_14_2 blade-direction candidate to use as the next q2 input; that new candidate was not part of these 19 reviewed captures. After style/pose correction and repacking, the 12-direction/action capture suite must be repeated, with direct visual comparison at the transitions listed above. The current v3 result remains a technically passing run with unresolved visual consistency, not final visual acceptance of the upcoming candidate.

## 2026-10-27 — Native-motion X-anchor analysis and v4 metadata refinement

The geometry baseline is the saved pre-native production pair, **C:/dev/TalesTactics/Logs/SdPolish20261027/cless-before.png** and its `.png.meta`, not the now-replaced production texture. The PNG is 1254×1254, PPU 64.01166, GUID `40e5b31d6d64b0a48b8b17f727aebee9`, with 80 sprite names/IDs. Its PNG SHA256 is `16ececda2c0b75a8aacf0fdd7d7a3455161418c6628e82eff500c4768f2908d0`; meta SHA256 is `e29b617628a8a373a694a0659400c255a6ff50ef8f812892b1fe4aff68a6f5de`. These match the packing baseline.

### Distinguishing atlas spacing, sprite registration and pose changes

The raw measurement sets `newAnchorX = oldAbsolutePivotX * (newCanvasWidth / oldCanvasWidth)`. For v3, the canvas ratio is 4096/1254 = **3.2663476874**, while PPU 152.252453 / 64.01166 = **2.3785112431**. Thus the source bbox-center-to-pivot offset is magnified 1.373274 times in world units when each new drawing is packed around the source-normalized bbox center. This is a technical registration error for independently generated/packed native figures; it is not evidence that the PNG was resampled or that Unity changed sprite IDs.

The appropriate X anchor for preserving the source bbox-relative world offset is:

```text
newAnchorX = newBboxCenterX + (oldAbsolutePivotX - oldBboxCenterX) * newPPU / oldPPU
newPivotX  = (newAnchorX - newRectX) / newRectWidth
```

Bboxes use alpha > 128 and the same >=8-pixel component inclusion as MeasureSdCharacterArt.py. Pixel bboxes use the half-open center `(minX + maxX + 1) / 2`; this makes integer packing/rounding visible in the report. Canvas layout is not used as the character-size multiplier. The X change does not alter the existing character grounding policy `pivotY=.01`.

The v3 captures re-inspected for this analysis were `cless.ultimate-Back/0058.png`, `0059.png` and `0062.png` under **D:/TalesTacticsHelpGuide/Builds/SdPolishReview20261027/SdMotionReviews/1a672154897b4949a66be2ab97eb300a/**, together with the baseline PNG. The timeline's fixed-camera sprite-bounds centers move +49.4px at 58→59 and -80.8px at 61→62. CharacterMotion's presentation travel/tilt is continuous, with no sprite-specific X correction; its final local X scale is 1. These observations isolate the discontinuity to changing sprite geometry/registration plus continuous action motion, rather than establishing an unrelated runtime scale switch.

The original and new lower-18%-alpha footprint measurements reproduce the v3 JSON within 0.000556px. They separate two effects:

| v3 sprite | Original footprint offset, world | Raw v3 footprint offset, world | X anchor correction, world | Footprint offset after bbox correction, world |
|---|---:|---:|---:|---:|
| cless_17_1 | +0.015622 | -0.121221 | +0.002329 | -0.123550 |
| cless_18_1 | +0.015622 | +0.518916 | +0.098991 | +0.419925 |
| cless_19_1 | -0.249954 | -0.123828 | -0.040745 | -0.083083 |

Positive anchor correction moves the rendered picture left by that world distance. For cless_18_1, the raw source-relative footprint change is +0.503295 world: about +0.098991 comes from the canvas/PPU mismatch, while +0.404303 remains after correcting that mismatch. The new drawing also reverses the exposed shoulder/cape side relative to the nearby recovery poses. A pivot change cannot repair that reinterpretation.

The footprint median is diagnostic, not an anatomical anchor. In v3 cless_18_1 its bottom band contains two boots **and a 511-pixel cape component**; the unequal boot areas also bias the median. cless_17_1 and cless_19_1 have cape-connected bottom-band silhouettes. Consequently the tool does **not** force feet onto the measured median or apply the larger foot-preserving pivot formula. The raw maximum 121.250px in cless_10_2 is +0.796378 world relative to its raw pivot, versus +0.015623 in the source; bbox correction removes only about 0.167240 world. The remaining pose/shape variation still needs visual inspection.

### Narrowly scoped metadata tool and validation

Added **C:/dev/TalesTactics/Tools/RefineSdMotionAnchors.py**, SHA256 `a3c538effc15451a43ad653fa8942c24b720f86ee180937965ebb940cf4b6239`. It is restricted to this immutable Cless baseline and its exact 80 names, original Sprite IDs/local IDs and order. It validates both baseline hashes/GUID, candidate SHA/dimensions, component coverage, measured rects, measured PPU, raw canvas-scaled X pivots and `pivotY=.01`. It rechecks input hashes before returning. It does not change MeasureSdCharacterArt.py or the existing 159-sheet production workflow's default measurement behavior.

Only `pivotX`, the dependent `footAnchorDeltaPixels` diagnostic and explicit refinement/approval provenance change in the output JSON. PNG pixels, metadata source files, sprite identity, rects, outlines and PPU remain unchanged. A refinement marker and raw-pivot guard reject repeated application. Outputs must be two distinct **new** `.json` files; exclusive creation avoids overwriting an existing report or measurement. Output `visualApproved` is reset to false, and any prior review is retained only as historical provenance. An independent read-only code review found no actionable issue in the formula, identity checks, repeat protection or file-writing path.

Offline validation on the final C tool and actual v4 measurement rejected **10/10 deliberately invalid inputs**:

| Probe | Deliberate alteration | Result |
|---|---|---|
| source_sha | Replace original PNG SHA with 64 zeroes | Rejected before geometry processing |
| candidate_sha | Replace candidate PNG SHA with 64 zeroes | Rejected before geometry processing |
| sprite_id | Replace frame 0's original Sprite ID | Rejected |
| local_id | Replace frame 0's original local ID | Rejected |
| frame_count | Set declared count to 79 | Rejected |
| changed_ppu | Increase PPU by 1 | Rejected |
| changed_rect | Shift frame 0 rect X by 1px | Rejected |
| changed_pivot_x | Add 0.1 to frame 0 X pivot | Rejected |
| changed_pivot_y | Change frame 0 Y pivot to .02 | Rejected |
| double_refinement | Supply the already refined output | Rejected |

All mutations were temporary JSON copies in the writable workspace. Before/after SHA256 comparison confirmed the raw v4 measurement, baseline PNG/meta, candidate PNG, production PNG/meta and production manifest were unchanged during these probes. Frame-by-frame comparison confirmed every output field except the two stated X-anchor fields was unchanged inside the 80 frame records. These are Python metadata checks, **not Unity tests or animation-quality acceptance**.

### v4 output and remaining actual-motion gate

Executed from C:/dev/TalesTactics:

```text
python -B Tools/RefineSdMotionAnchors.py Docs/SdCharacters/2026-10-24/cless-native-v4-measurement.json Docs/SdCharacters/2026-10-24/cless-native-v4-anchored.json Docs/SdCharacters/2026-10-24/cless-native-v4-anchor-report.json
```

The v4 candidate is **Assets/TalesTactics/Art/SdCharacters/cless-native-v4.png**, SHA256 `1735ae05d8b5d2060017e072ebf144cd8a40684e9549dba0f16908830342f1d7`. The raw measurement SHA256 is `af632d005394f362a349607b5e43186badc43bf964d0632e8e17b3a98f1439b3`. The tool produced:

- **cless-native-v4-anchored.json**, initial unapproved SHA256 `84f362277266011a1d1d0922c544596facd2c23d8a557c18e168d2b4a66ba726`.
- **cless-native-v4-anchor-report.json**, SHA256 `38fdf05e5f937ac0e38ec4bdc004492b91678b1c68ec0dcfd224df228a2c58f6`; all 80 before/after values and identities are recorded here.

All 80 frames were measured/refined. PPU stays **166.42948**, giving actual art scale **2.5999869399** versus canvas scale **3.2663476874**. The largest X anchor change is **24.752612px / 0.148727329 world**. The maximum error in preserving `(pivotAbsoluteX-bboxCenterX)/PPU` after six-decimal JSON pivot rounding is **0.000000838 world**. The largest source-relative bottom-band footprint difference changes from 0.718926316 to 0.603857375 world; this remaining diagnostic is not corrected automatically.

| v4 frame | Raw pivotX | Refined pivotX | Rendered image X shift, world |
|---|---:|---:|---:|
| cless_17_1 | .493403 | .494445 | -.001465 |
| cless_18_1 | .260842 | .307827 | -.064932 |
| cless_19_1 | .598473 | .580176 | +.024956 |
| cless_10_2 | .072245 | .160093 | -.115069 |

This pass did not import art, modify the production manifest, operate Unity or write to D. Integration and fresh Unity/Windows verification belong to the integration owner. The earlier v3 capture findings remain historical; no v4 Windows motion frames were available to this analysis. After import, specifically recheck Back 58→59 and 61→62, Right 23→24 blade recovery, and the HQ/extra/polish↔MotionPremium transitions documented above. Bbox-relative registration preservation does not prove anatomical pose consistency or foot contact with the battlefield.


## 2026-10-27 — Actual Windows v4 anchored motion visual review

This review inspected the newly aligned native quadrants **C:/dev/TalesTactics/Logs/SdPolish20261027/ClessAligned/q0.png** through **q3.png**, the existing **Assets/TalesTactics/Art/PixelCampaign/cless-hq.png**, and 19 actual Windows captures from:

**D:/TalesTacticsHelpGuide/Builds/SdPolishReview20261027Final/SdMotionReviews/29b5d49e6e5e4a83a8a7e6540c254f06/**

`result.json` SHA256 is `6297092c962c50bb7b20c7d7acea1cde40e236d121548daa196e08d2d969ee89`. The run reports **12/12 passed**, no errors, all six watched sprite names observed, six allied models in the simulation, and unchanged user/isolated saves. It contains **684 continuous PNGs plus 12 battlefield references, 696 total PNGs**. This automated action/state result is separate from the visual findings below. It is not campaign, balance, full-battlefield action-grounding or full-animation visual acceptance.

The 19 files actually opened with view_image were:

| Relative directory | PNGs viewed |
|---|---|
| cless.attack-Right | 0023.png, 0024.png |
| cless.2-Back | 0037.png, 0038.png |
| cless.ultimate-Back | 0058.png, 0059.png, 0061.png, 0062.png |
| cless.ultimate-Right | 0000.png, 0016.png, 0017.png, 0020.png, 0022.png, 0030.png, 0031.png, 0034.png, 0065.png, 0066.png, battlefield.png |

For reproducibility, compute SHA256 of each listed PNG, create a dictionary keyed by relative path, and serialize with Python `json.dumps(mapping, sort_keys=True, separators=(',', ':'))`. SHA256 of that UTF-8 serialization at review is `e672bea0b68be1ff7b7850dabe600803e738113251163571da2e5468a86ee1f0`.

### Confirmed improvements

The native illustrations now use warm brown eyes, gold hair shading, and silver/gold armor rendering closer to the existing HQ family. Hair, glove, blade and armor contours are legible in the actual close-ups. The sampled images do not show a detached head, extra sword or missing visible limb. Hidden limbs behind the cape remain outside this visual assurance.

The specific **Right blade reversal is corrected**: cless.attack-Right/0023.png (`cless_13_2`) and 0024.png (`cless_14_2`) both show a single connected sword extending screen-right. The earlier down-left blade in the second pose is gone. The hand/guard/blade connection is visually coherent in these two images.

cless.2-Back/0037.png→0038.png (`cless_15_1`→`cless_16_1`) still reads as raising the visible left-side arm. Both boots remain visible, with no obvious detached segment. This sampled pair does not show the strong opposing-side shoulder flip seen in late ultimate recovery.

### Remaining visual discontinuities

**The body-part size change between native motion and the established sheets remains visible.** In the fixed-camera cless.ultimate-Right sequence, 0016.png (HQ)→0017.png (native), 0030.png (extra)→0031.png (native), and 0065.png (native)→0066.png (polish) change the apparent size of the head/face and boots. The last pair is the clearest recovery example: the native head and boots become visibly smaller on the following polish frame while the foot baseline is similar. Color/line treatment is closer than v3, but these samples do not support claiming that anatomical size/proportion continuity is fully matched.

This finding is based on body parts, not on the total blade/cape-inclusive bbox height. A crouching pose naturally shortens the body, and an overhead sword increases the full sprite bounds; neither observation alone is a scalar scale or pivot defect. This review does not prescribe a global PPU change or another numerical pivot adjustment.

**The Back late-recovery lateral jump and silhouette reversal also remain.** At 0058.png (`cless_17_1`)→0059.png (`cless_18_1`), the head/torso moves right and the exposed shoulder/cape side reverses. At 0061.png (`cless_18_1`)→0062.png (`cless_19_1`), the head/torso returns left and the shoulder/cape side reverses again. In the aligned source art, cless_18_1 is q2 row 5, column 2, and the opposite-side silhouette is still present. It is therefore an authored pose difference, not evidence of an incorrect Sprite ID or uncorrected canvas/PPU multiplication.

| Actual v4 interval | Sprite-bounds center X, pixels | Change | Same-pose neighboring change |
|---|---|---:|---:|
| ultimate Back 58→59 | 509.468 → 547.962 | +38.494px | 57→58: -4.460px |
| ultimate Back 61→62 | 540.296 → 474.687 | -65.609px | 62→63: -3.274px |
| attack Right 23→24 | 611.229 → 544.820 | -66.409px | 22→23: -8.055px; 24→25: -9.969px |

The Back camera remains yaw 231.072449°, orthographic size 1.571351; the Right camera remains yaw 51.072445°, orthographic size 1.770217. All listed frames remain in their respective Attack/Ultimate action with `presenting=true`. These are renderer-geometry bounds rather than anatomical tracking. The observed head/body displacement agrees with the visible lateral transition, but the numbers also include changing sword/cape silhouettes and continuous action movement. Camera framing differs between the earlier v3 and v4 runs, so raw pixel deltas across the two runs are not a normalized anatomical comparison.

The Right 23→24 row records a separate remaining lateral recovery step; it does not negate the confirmed blade-direction fix. It should be judged as pose/registration continuity in playback rather than reported as the old wrong-way sword defect. The bbox-relative world-offset preservation check can pass while such body-relative changes remain, because the bbox center itself depends on cape and weapon composition.

### Visual acceptance and scope

This pass confirms clearer native rendering, closer color/line treatment, and the targeted blade-direction correction. **It does not grant complete visual continuity acceptance for the current v4 transition set**: the cross-sheet body-part size changes and Back recovery side-to-side pose jump remain concrete review points. Numerical import/ID/pivot checks and the 12 successful action simulations do not override these visual observations.

The close-ups are actual 960×540 Game View captures at fixed 30fps, with Cless isolated on a plain background. That framing does not expose a ground plane or valid contact reference, so foot sliding, hovering and penetration cannot be certified here. The one 1920×1080 battlefield image viewed is an idle reference; it does not verify action grounding. Full-scene VFX/occlusion, every intervening animation frame, and the equipped eternal-sword appearance are outside this 19-image review.

No PNG, metadata, source code, production manifest or Unity state was changed by this review. Only this document was appended; the review result and screenshots remain original engine output.


## 2026-10-27 — Native anatomy-size diagnosis and v5 255 PPU trial metadata

This section follows the actual v4 visual review above. It records a subsequent sizing investigation and metadata-only trial, not acceptance of a new rendered animation. The actual v4 capture root remains **D:/TalesTacticsHelpGuide/Builds/SdPolishReview20261027Final/SdMotionReviews/29b5d49e6e5e4a83a8a7e6540c254f06/**. In addition to the 19 images listed earlier, **cless.ultimate-Right/0064.png** was inspected for the recovery baseline comparison.

### Why test global scale before another illustration revision

The measured PPU uses the median bbox-height ratio of names whose pose token is `0` (`Tools/MeasureSdCharacterArt.py:134`). In this MotionPremium family, `MotionPremiumImporter.cs:63` inserts poses 0 through 3 into the walk clip. Pose 0 is therefore not an anatomical standing reference merely because the measurement variable is named `standing`. Native walking/crouching artwork can have a shorter overall body while retaining larger head/boot anatomy; matching its total bbox height to the old art can enlarge these body parts. This observation does not justify changing the default measurement policy for the other 158 sheets.

The following approximate screen measurements use only the original v4 captures. Within this ultimate-Right sequence the camera is fixed at yaw 141.072449 degrees and orthographic size 1.7702173. Head widths measure the largest gold/skin-colored region inside a manually bounded head ROI; boot widths measure the last ten occupied screen rows of a manually selected boot ROI. Color adjacency, antialiasing and pose foreshortening limit these proxies: they are not bone or anatomical segmentation. The 0030/0031 head comparison was excluded because an overhead hand/sword joins the head-color region.

| Actual ultimate-Right pair | Established sheet measurement | Native measurement | Native / established |
|---|---:|---:|---:|
| 0016 HQ -> 0017 native, head width | 79px | 113px | 1.43 |
| 0016 -> 0017, left/right boot width | 34 / 41px | 56 / 61px | 1.65 / 1.49 |
| 0020 native -> 0022 extra, head width | 70px | 107px | 1.53 |
| 0020 -> 0022, left boot width | 34px | 49px | 1.44 |
| 0030 extra -> 0031 native, left/right boot width | 37 / 40px | 53 / 51px | 1.43 / 1.28 |
| 0065 native -> 0066 polish, head width | 69px | 124px | 1.80 |
| 0065 -> 0066, left/right boot width | 37 / 37px | 54 / 64px | 1.46 / 1.73 |

The head-color condition was `r>55, g>28, b<r*.85, g>b*1.15, r>g*1.05, r<g*2`, within these top-origin ROIs: frame16 `(440,190,570,282)`, 17 `(440,165,620,300)`, 20 `(495,225,650,345)`, 22 `(540,260,660,352)`, 65 `(425,140,595,280)`, 66 `(425,170,540,278)`. The frame17/65 color component reaches the ROI bottom, so only horizontal extent was used. Boot occupancy used a background-color difference greater than 12. These are approximate sizing diagnostics, not an automated visual-quality score.

The integration owner selected a **255 PPU actual-render trial**. Relative to the verified raw 166.42948 PPU, the predicted screen-size multiplier is **0.6526646275**. For example, the native frame17 head width predicts 73.75px against HQ 79px, and its two boot widths predict 36.55/39.81px against HQ 34/41px. Frame20 predicts 69.84px head width against extra 70px. Frame65 still predicts an approximately 80.93px head against polish 69px; this recovery pose remains a specific visual check. These are arithmetic predictions, not generated or resized comparison images. Crouching total height may legitimately become shorter than a standing pose after anatomy is matched.

At frames 0064/0065/0066, the observed boot-bottom rows were approximately **368/367/369** (top-origin; the separate right boot at frame66 ended at 364). Renderer-bound bottoms were **371.273/370.599/370.213**. Thus this sample supports preserving the existing Y policy rather than inferring a new vertical pivot correction. Boot stance midpoint X was approximately **499.5/497.5/476.0**, head-region center X **518/515/479.5**, and renderer-bounds center X **492.527/489.737/457.653**. Pose/body-relative lateral continuity remains an actual-playback question even when bbox-relative world offsets are preserved.

### Bounded calibration helper and unchanged raw validation

`Tools/RefineSdMotionAnchors.py` now accepts optional `--pixels-per-unit`. Its default is unchanged. The original measurement PPU is independently recomputed and validated before accepting an override; supplying a different raw PPU still fails. An explicit selected value must be positive, finite and within 0.5 to 2 times the verified raw PPU. Both values and the visual-calibration reason are recorded.

The X formula remains:

```text
newAnchorX = newBboxCenterX + (oldPivotAbsoluteX - oldBboxCenterX) * selectedPPU / oldPPU
```

The immutable reference remains **Logs/SdPolish20261027/cless-before.png** and its original meta, with the PNG/meta SHA256 recorded in the report. All 80 names, sprite IDs and local IDs must match. PNG pixels, pivotY `.01`, rect Y/height and the absolute atlas-pixel outline geometry remain unchanged. The output remains `visualApproved=false` pending actual rendered review.

At the selected PPU, two X anchors require a minimally wider sprite rect to keep normalized pivotX within 0..1. This is a metadata extension, not a crop, repaint or resampling. Each extension must remain within its own original 8-by-10 atlas cell and must not overlap another sprite rect. Added strips may contain alpha 0 through 8 only and must remain outside the unchanged absolute outline; they are **not claimed to be strictly transparent**. Any alpha greater than 8 or neighboring-rect overlap is rejected. Outline local X is translated by the rect-center change so its absolute atlas-pixel position is unchanged; selecting a new PPU intentionally changes its world size.

### Actual v5 outputs and proof

The integration owner's v5 candidate is **Assets/TalesTactics/Art/SdCharacters/cless-native-v5.png**, SHA256 `0ffd2c058365aa5fb3d2659a837053c950d84083704973baf94ba16d1a221d86`. The raw measurement SHA256 is `683a6d162e80d0ada18b543696bed8f2f17eb8118ca8962feafbf30233bc956f`. This tool did not generate or modify that PNG.

Executed from C:/dev/TalesTactics:

```text
python -B Tools/RefineSdMotionAnchors.py Docs/SdCharacters/2026-10-24/cless-native-v5-measurement.json Docs/SdCharacters/2026-10-24/cless-native-v5-calibrated.json Docs/SdCharacters/2026-10-24/cless-native-v5-calibration-report.json --pixels-per-unit 255
```

- Tool SHA256: `2875224578fdf4b98dba0479c875997dda0fb1d9150d2317d90a70dbd27113d5`.
- Initial unapproved **cless-native-v5-calibrated.json** SHA256: `fa77c6d7a22282d8fea96b32e5b9a3075ae6712ce8fb95cc31a400f7a4c13f2f`.
- **cless-native-v5-calibration-report.json** SHA256: `ed31a98fd8bc8152821222a4ac0a971a9c248fd87dbaa53506010566e8ab8acd`.

| Frame | Raw measured rect x,y,w,h | Adjusted rect x,y,w,h | Added strip | Weak-alpha pixels / maximum alpha |
|---|---|---|---|---|
| cless_10_2 | 1182,1799,220,201 | 1178,1799,224,201 | 4px left, 804 pixels | 36 / 3 |
| cless_12_2 | 1200,1393,276,213 | 1192,1393,284,213 | 8px left, 1704 pixels | 90 / 4 |

Only these **2 of 80** rects expand. Their total added area is 2,508 atlas pixels, including **126 alpha-positive pixels, maximum alpha 4**, with no alpha greater than 8. Both remain inside their cells; all adjusted rects are nonoverlapping. Maximum absolute outline-coordinate error is **0 pixels**. No source pixel is altered or deleted.

All 80 X anchors preserve the old bbox-relative world offset with maximum rounding error **0.0000005063 world units**. The maximum anchor-only image shift at selected PPU is **0.100984220 world**, while the maximum bbox-center shift including raw-to-selected scale and anchor changes is **0.148728684 world**. The report separates these two diagnostics. The maximum old-relative bottom-band footprint difference is **0.706908793 before, 0.535491464 after**. Before values use raw PPU; after values use selected PPU. This reduction does not constitute anatomical foot anchoring or ground-contact proof: the bottom-band proxy can include boots, cape and weapon pixels.

### Offline checks and remaining render gate

The earlier ten raw-input negative probes remain documented above. Additional bounded checks for this calibration confirmed:

- Default v4 output frame records and PPU exactly match the earlier default refinement output.
- Raw PPU +1 is rejected even with an explicit selected PPU of 255.
- Selected NaN, infinity, zero, 80 and 340 are rejected; the last two lie outside the allowed range for raw 166.42948.
- Synthetic numeric alpha arrays confirm alpha 9 in an added strip is rejected; alpha 8 outside the unchanged outline is accepted.
- Cell-boundary crossing, an original outline extending into the padding, and neighboring-rect overlap are rejected.
- The actual v5 positive run preserves all 80 names/IDs, rect Y/height and pivotY; input PNG/meta/measurement hashes are checked again before output.

These are offline Python metadata checks, not Unity tests. No PNG was created or edited for these probes. Read-only independent code review found no additional actionable defect in the bounded formula, raw validation, padding guards or output handling. The global measurement default, previous v4 reports, production metadata and manifest were not changed by this helper run.

Actual v5 at 255 PPU must still be reviewed at HQ/extra/polish transitions, **ultimate Back 58->59 and 61->62**, **attack Right 23->24**, and late **ultimate Right 65->66**. The integration owner reports a new v5 Back pose18 illustration; its intended shoulder/cape correction is not certified by this metadata proof. This section approves reproducible metadata preparation only, not final anatomical continuity, foot contact or the full 12-animation visual result.

### Follow-up: preserve equipped-appearance contract, trial 220 PPU

After preparing the 255 candidate, the integration owner reported an actual EditMode failure in the unchanged `SignatureAppearanceTests`: equipped sprite bounds height / default sprite bounds height reached **1.911957 for cless_16_2**, **1.8328 for cless_16_3**, and approximately 1.65 to 1.69 for seven other poses, against the existing allowed range **0.65 to 1.65**. This reviewer did not execute that Unity test. Its reported result blocks accepting 255 PPU merely because some unarmed anatomy examples would match more closely. Neither equipped artwork nor test assertions were changed for this trial.

At the integration owner's request, the same validated raw v5 input was processed separately with **220 PPU**. The raw measurement, PNG, 255 outputs and earlier reports were preserved. Outputs are:

- **cless-native-v5-calibrated220.json**, initial unapproved SHA256 `02e7482b5595ba718f866c47b193b02cbecbfc246a3aa48c3a12466d6704ca94`.
- **cless-native-v5-calibration220-report.json**, SHA256 `6a9392b5dd295d7b2ea0a2430738fca0fb8b0907e98fdb1449efc8b87dff0837`.

The command is identical to the 255 run above, with those two distinct output names and `--pixels-per-unit 220`. **All 80 frame rects remain exactly as measured: zero rect expansions, zero added pixels, and zero absolute outline-coordinate error.** This trial therefore does not alter frame bounds through padding to satisfy the equipped-height check. Names, IDs and Y policy remain unchanged. Maximum source bbox-relative world-offset rounding error is **0.0000005720 world units**. The maximum anchor-only shift is **0.030678045 world**, and maximum bbox-center shift including the scale change is **0.148729153 world**. The old-relative bottom-band footprint diagnostic changes from 0.706908793 to 0.552336697 world; its anatomical limitations remain as stated above.

The predicted render-size multiplier at 220 PPU is **166.42948 / 220 = 0.7564976364**, approximately 24.35% smaller than the raw native rendering. Using the earlier approximate v4 observations, frame17 head width predicts **85.48px vs HQ 79px (+8.2%)**, frame20 **80.95px vs extra 70px (+15.6%)**, and frame65 **93.81px vs polish 69px (+36.0%)**. Thus 220 is a plausible partial size correction compatible with testing the existing equipped-appearance contract; it is not evidence of a full anatomy match. Pose angle and the approximate ROI can account for some differences, but the frame65 prediction is too large to dismiss without actual inspection.

The integration owner estimated the worst equipped/default height ratio at 220 to be about **1.6495**; actual Unity verification owns whether that remains within the unchanged test limit. The next visual gate is the **220 PPU** actual capture, especially late recovery 65->66 and the corrected Back pose18 transitions. The 255 metadata remains historical trial evidence, not the currently recommended acceptance candidate.


## 2026-10-27 — Actual Windows v5 at 220 PPU: targeted visual continuity review

This read-only pass inspected **17 original Windows PNGs** from **D:/TalesTacticsHelpGuide/Builds/SdPolishReview20261027V5/SdMotionReviews/d450f0d5da8447fc82e0564cdee1f7fa/**. No screenshot was composited, resampled or otherwise edited. The result JSON SHA256 is `189a2c699feebc9a6f1e8d8af1f191c931d06a599fd2b0a7a5473049f1b50bfa`. It reports **12/12 runs passed**, no errors, 684 continuous close-ups plus 12 battlefield references, and unchanged user/isolated saves. The simulation result does not by itself approve visual continuity.

| Directory | Original PNGs opened with view_image |
|---|---|
| cless.attack-Right | 0023.png, 0024.png |
| cless.ultimate-Right | 0016.png, 0017.png, 0020.png, 0022.png, 0030.png, 0031.png, 0065.png, 0066.png |
| cless.ultimate-Back | 0057.png, 0058.png, 0059.png, 0060.png, 0061.png, 0062.png, 0063.png |

Using the same sorted-relative-path dictionary of per-PNG SHA256 values described in the v4 review, the 17-image aggregate SHA256 is `0fe84bce8851331a1cc3f650f16a000b0a638f125c7fd0d1f815bdc54c23e4b1`.

### Improvements visible in the actual 220 rendering

The extreme cross-sheet scale change at ultimate Right **16 -> 17** is substantially reduced. The current pair reads as an upright guard changing to a crouched overhead strike rather than the previously much larger body appearing. Native head, gauntlet and boot contours remain sharp. The native figure is still stockier, but total crouched height is not evidence of a scale error.

Attack Right **23 -> 24** retains one connected sword pointing screen-right, and the hand/guard/blade join is coherent. There is no recurrence of the old wrong-way blade. This pair alone does not certify the full return-motion position curve.

The corrected Back pose18 now keeps the exposed shoulder and flowing cape on the same side as the subsequent Back pose19. The previous shoulder/cape reversal is gone. All four targeted Back frames show the back of the head; this is not a rear-to-front-facing flip. No extra sword, detached head or detached visible limb was found in these samples.

### Head height and remaining shape difference

The head assessment used visible hair-top to chin as well as width, rather than the total sword/cape-inclusive sprite bounds. In Right **20 -> 22**, the extra-sheet forearm/guard partly covers the face/head, so a precise head-height ratio would be misleading. Right **30 -> 31** also changes overhead sword/arm occlusion; native hair mass remains larger, but no precise anatomical ratio is claimed from that pair.

Right **65 -> 66** remains the clearest shape comparison. At 65 (`cless_19_2`), the crouched three-quarter native pose has a visibly larger hair/head mass; at 66 (`cless-polish_3_2`), the upright more side-on pose has the established smaller head and slimmer body. Approximate manually observed hair-top-to-chin height is **88-92px versus 70-73px**, a roughly **20-30%** difference. Head width also differs, but the change is not solely a side-angle width effect. The estimates include hairstyle and tilt and are not rigid skull or face segmentation. The skin/color masks were not accepted as precise head-height measurements where they joined a guard, glove or collar.

This late recovery transition still has a visible shape change. The 220 trial is a substantial improvement and some remaining variation is attributable to separately illustrated poses; this review does not equate a crouch-to-stand transition with a scalar pivot defect or demand another global PPU change. Full anatomical matching should not be claimed from these samples.

### Concrete remaining continuity defect: Back pose18 position step

The Back lateral movement is confirmed using **head and both boot positions**, not inferred from a changing sprite bbox. To quantify the boot stance, each boot was isolated by a manually chosen ROI in the unedited screenshot. Pixels differing from the plain background by more than 12 in any RGB channel were considered occupied; the X midpoint of the last ten occupied rows was calculated for each boot, then averaged. Top-origin ROI coordinates are:

- Frame58: left `(432,315,487,359)`, right `(522,316,581,359)`.
- Frame59: left `(467,321,523,365)`, right `(568,320,626,365)`.
- Frame61: left `(459,330,516,374)`, right `(560,329,619,374)`.
- Frame62: left `(389,330,447,377)`, right `(480,332,542,378)`.

| Ultimate Back frame | Sprite | Left boot center X | Right boot center X | Stance midpoint X |
|---|---|---:|---:|---:|
| 58 | cless_17_1 | 459.5px | 552.0px | 505.75px |
| 59 | cless_18_1 | 494.5px | 597.0px | 545.75px |
| 61 | cless_18_1 | 486.5px | 589.0px | 537.75px |
| 62 | cless_19_1 | 416.0px | 515.5px | 465.75px |

Both boots move right on **58 -> 59 (+40px stance midpoint)** and left on **61 -> 62 (-72px)**. The visible head follows the same directions, approximately +57px and -77px from the color-region center estimate. The 59 -> 61 interval, which retains the same pose18 for two frame steps, moves only -8px total. Viewing 57/58, 59/60/61 and 62/63 confirms smooth movement within each held pose and the larger change at the sprite switch. The camera is fixed within this Back run at yaw **231.072449 degrees**, orthographic size **1.4335191**, and all these frames remain `Ultimate`, `presenting=true` at 30fps.

The 61 -> 62 shift is approximately one and a half visible boot widths in one captured frame. It reads as a body-position jump rather than only cape/sword redistribution or the intended arm-lowering motion. The observation establishes a remaining playback-continuity issue; it does **not** alone identify whether the cause is frame metadata, pose-relative body placement, or an action-position curve. Existing source bbox-relative offset preservation is a different invariant and can pass while new artwork places the body differently inside its bbox.

The concrete next correction target is therefore **Back pose18's connection to poses17/19**, with body landmarks and the actual motion position considered together. Another full-art regeneration or global scale change is not established as necessary by this evidence. The current targeted sample is **not granted final smooth-motion visual acceptance** while this larger position step remains. Right65 -> 66 is a secondary shape-continuity review point after that correction.

The plain-background close-up has no ground plane, so actual floor contact, hovering and penetration remain unverified here. These 17 images also do not establish every intervening pose, equipped-weapon motion, full-scene VFX readability or campaign behavior. No source, PNG, metadata, production manifest, player save or Unity state was changed in this review; only this document was appended.


## 2026-10-27 — Explicit Back18 X-anchor continuity candidate

The integration owner authorized correcting the remaining Back18 body-position step through metadata. **No illustration or production import was changed by this operation.** The new helper is **Tools/CorrectClessBack18Anchor.py**; its explicit specification is **Docs/SdCharacters/2026-10-24/cless-native-v5-back18-correction.json**. It accepts only the exact reviewed v5/220 input and capture hashes. It does not add an automatic correction to the global measurement or refinement policy.

### Separate actual root movement from pose-relative body placement

Simply dividing the recorded screen bbox width by the sprite rect width is invalid: the recorded bbox comes from transformed silhouette mesh vertices, and the sprite rect includes padding. The implementation instead uses every original outline vertex, the unchanged 220 PPU, and the existing HQ front-body sizing rule. The HQ front is `cless-hq_0_0`, rect height 281, PPU 224.96513, yielding `BodyScale = 1.22 / (281 / 224.96513) = 0.9767169345`. Its meta SHA256 is `6ea06fc52d9eaf159e1a4ad32629eeaa32a50f47d624bf010a71f0d12348a736`.

The runtime uses the camera-facing billboard plus a Z tilt. For this released Cless ultimate, the Blink profile has Tilt 10 and scaleY is 1. The tool searches the known tilt interval -10 to 0 degrees and fits the transformed mesh width/height to each actual captured mesh bbox. It then recovers the screen origin from the transformed minima/maxima. Across frames 57 through 63, the maximum mesh-extent residual is **0.000093711 screen pixels**. This agreement validates the bounded reconstruction for these samples; it is not a general-purpose transform logger.

The inferred root X is **508.333 at frame58, 504.048 at59, 496.184 at61, 492.683 at62**. Thus the root moves smoothly left by 4.285px and 3.501px at the two sprite boundaries. The previous same-pose observed drift included a small rotation effect; it was not treated as pure root translation. The recovered screen scale is **0.8361925803 pixels per native atlas pixel**, with the horizontal pivot coefficient additionally multiplied by the fitted tilt cosine.

The measured two-boot stance point is inverse-transformed into each sprite's local atlas-pixel coordinates. Its X relative to the existing anchor is:

| Pose | Samples | Body midpoint relative to anchor |
|---|---|---:|
| Back17 | frame58 | -3.178179px |
| Back18 | mean of frames59 and61 | +49.803832px |
| Back19 | frame62 | -32.217203px |

The selected policy leaves Back17 and Back19 unchanged and places Back18 at their mean **-17.697691px**. This bridges the existing neighboring pose offsets rather than forcing every body point onto one arbitrary screen coordinate. The derived anchor delta is **+67.501523 native pixels**; six-decimal normalized-pivot rounding applies **+67.501504px**. `cless_18_1` pivotX changes from **.239166 to .540512**. The old bbox-relative world-offset invariant is explicitly superseded for this one frame only.

The predicted screen movement of the corrected pose18 is approximately **56.4px left**. The two observed stance transitions change from **+40/-72px** to approximately **-16.403/-15.574px**, including the continuous root movement. Residual pose differences remain because the neighbors are retained. This is a prediction requiring actual playback confirmation, not a claim that every transition is now visually accepted. PivotY and Y geometry remain unchanged; because the sprite is tilted, moving its X anchor also predicts a small screen-Y change of about **-2.16 to -1.42px** over frames59 to61.

### Outputs, invariants and bounded checks

Executed from C:/dev/TalesTactics:

```text
python -B Tools/CorrectClessBack18Anchor.py Docs/SdCharacters/2026-10-24/cless-native-v5-calibrated220.json Docs/SdCharacters/2026-10-24/cless-native-v5-back18-correction.json Docs/SdCharacters/2026-10-24/cless-native-v5-calibrated220-back18.json Docs/SdCharacters/2026-10-24/cless-native-v5-back18-report.json
```

- Tool SHA256: `05f555ff32e561476d2004f192bae700b8229a0dda31e7b29c2f2c0c64bb627d`.
- Explicit specification SHA256: `b1182e33254e31df63ea77b76d65af4b8b12bdb0e4e76943e98426b6b2b6d407`.
- Initial unapproved **cless-native-v5-calibrated220-back18.json** SHA256: `c1bff0b1b58b222eb2e7850f0157d5d7be0710c6b9b5c9f2333611cd0884144a`.
- **cless-native-v5-back18-report.json** SHA256: `a9d84da51504ececc6a21e65df7c0345bb43a01e2a165bfa4259122791bfa86e`.

Frame comparison confirms **79 entire frame records remain identical**. On Back18, only `pivotX` and its dependent `footAnchorDeltaPixels` diagnostic change. PNG bytes, PPU, all 80 names and IDs, rects, pivotY and outline coordinates remain unchanged. Every input/evidence file is rehashed before output. Both outputs must be distinct new JSON paths; the result is `visualApproved=false` and requires a new import/render review. Wrong target sprite, wrong candidate SHA, wrong capture SHA and a changed requested offset were rejected in four temporary-spec probes. The positive comparison and rejection details are in **cless-native-v5-back18-offline-probes.json**. These are offline checks, not Unity tests.

The report records each Back17/18/19 rect, strong-alpha bbox and RGBA hashes for both the full rect and strong-alpha-bbox crop. These are read-only pixel measurements. A future v6 atlas must be separately compared against that evidence before transferring an offset; the current tool rejects a different input atlas. If regenerated pixels or body placement differ, this v5 screenshot-derived correction cannot be assumed valid solely because sprite names are preserved.

### Reproduction prerequisites for a clean clone

The historical baseline PNG and meta can be reconstructed byte-for-byte from commit **48721270c0f76f14bfb9b18f62774a7172adece2**, paths **Assets/TalesTactics/Art/MotionPremium/cless.png** and **cless.png.meta**, using binary-safe `git show` output. The required baseline SHA256 values remain those already recorded above. Shell text transcoding must not alter the PNG bytes.

The native candidates, historical baseline copies, generated quadrants and actual capture directories are generation history/local review archives. Preparing those exact files and matching hashes is required to rerun these optional measurement/correction tools from a clean clone. Ordinary project builds use the committed production assets and do not require rerunning image generation or this historical review pipeline. Candidate availability and provenance should not be confused with a new network dependency in the game build.


## 2026-10-27 — Exact-content Back18 transfer to v6

The v6 packing changes only the selected Right19 illustration (`cless_19_2`). Before transferring any Back18 correction, this review compared **all other 79 frames**, not only the three Back poses. In the v5 and v6 calibrated220 inputs, all 79 frame records are exactly equal, including names/IDs, rects, pivotX/Y, outline coordinates and diagnostics. Each frame's complete rect-sized RGBA pixel array also matches exactly. Back17/18/19 therefore preserve the same measured body landmarks and local coordinate system; their atlas translations in this actual output are **0px**, despite translation having been anticipated as a possibility. The separate Right19 illustration is outside this exact-content comparison and still requires actual visual review.

The fixed-evidence helper **Tools/TransferClessBack18AnchorV6.py** validates the v5 original220/report hashes and v6 raw/calibrated220/PNG hashes, performs those 79 comparisons, and transfers only the reviewed Back18 anchor delta **+67.501504 native pixels**. It accepts no arbitrary atlas or offset. On v6, `cless_18_1` pivotX again changes **.239166 -> .540512**; only that field and the dependent `footAnchorDeltaPixels` change. PNG pixels, PPU220, Y policy, IDs, all rects and outlines remain unchanged. The output contains explicit transfer provenance and `visualApproved=false`. No production import or manifest mutation was performed here.

Executed from C:/dev/TalesTactics:

```text
python -B Tools/TransferClessBack18AnchorV6.py Docs/SdCharacters/2026-10-24/cless-native-v6-calibrated220-back18.json Docs/SdCharacters/2026-10-24/cless-native-v6-back18-report.json
```

- v6 candidate PNG SHA256: `c6bd896f69d75e1f9df5d8d22af72bb265693777181b3e94c081e5f902c2be44`.
- Raw v6 measurement SHA256: `e6970dd47368d468c78af41669461772da7d2c095a5e3590e404fff61dea62e5`.
- Input v6 calibrated220 SHA256: `1aaf7eb515a29fec930ebec5e572884dd1fbf7d0d1fc3608e2d123f06add6a4f`.
- Transfer tool SHA256: `3fb372db42f4659d4def8896cc93c5fc4400d3bd08d5031af6d68df372360f76`.
- Initial unapproved **cless-native-v6-calibrated220-back18.json** SHA256: `e19314a5f713844a86dd595cd27a3df2bcac8d7d665ab6a90bec5b0f7700e331`.
- **cless-native-v6-back18-report.json** SHA256: `f8125864a9e54c35f03acae4fd7ad17b170e9528b77574aba1943dac74dde565`.

The report includes all 79 rect RGBA hashes and frame-field equality results, plus the earlier v5 predicted transition values identified as historical predictions. Input hashes are checked again before output, outputs must be distinct new files, and a before/after field assertion excludes unintended changes. These checks establish safe content-identical metadata transfer; they do not replace the integration owner's Unity import, tests or fresh v6 motion capture.


## 2026-10-27 — Final v6 actual Windows targeted visual acceptance

The final read-only review opened these **six original captures sequentially**, with filenames attached to each image, from **D:/TalesTacticsHelpGuide/Builds/SdPolishReview20261027V6/SdMotionReviews/dc941b4abfa7411f9a1717c86d9a847e/**:

- `cless.ultimate-Back/0058.png`, `0059.png`, `0061.png`, `0062.png`.
- `cless.ultimate-Right/0065.png`, `0066.png`.

The run result reports **12/12 passed**, no errors, 684 continuous PNGs plus 12 battlefield references, and unchanged user/isolated save files. Its SHA256 is `c6ce99592ec014c0495b0672bd29e99ac8f8796c7e7df6f1f005255975c990fe`. The six-image aggregate SHA256 using the previously documented sorted-path/per-file-hash method is `43a9e1be2610a78527bb997d0dcffed3d315d0c1798991c9c407be7d5fa7bbee`.

**Back18 position correction is visibly effective.** Both reviewed boundaries now continue toward screen-left during recovery. The previous body movement right followed by a much larger left return is gone; head, shoulders and both boots agree with that observation. Using the same lower-ten-row boot-band method, stance midpoint X is **505.75 -> 490.00px** at 58->59 and **481.75 -> 465.75px** at 61->62: **-15.75/-16.00px**, compared with the previous **+40/-72px**. The small difference from the predicted -16.40/-15.57px is consistent with the thresholded raster-foot proxy and subpixel rendering. Camera yaw/orthographic size remain 231.072449 degrees / 1.4335191. Shoulder/cape facing remains consistent; the remaining discrete pose change reads as arm lowering and weight recovery rather than a body-position snap.

For reproducibility, the updated frame59 boot ROIs are `(410,319,467,363)` and `(511,318,570,363)`; frame61 uses `(402,328,460,372)` and `(503,327,563,373)`. Frame58/62 use the earlier ROIs. These are measurements of original capture pixels, not image edits.

**Right19's head-size transition is also improved.** At 65->66, the revised native head no longer reads as the previous large-head character switching to a smaller-head character. Visible hair-top-to-chin height is approximately **75-79px versus 70-73px**, rather than the previous roughly 88-92px versus 70-73px. This remains a manual visual estimate including hairstyle/tilt, not an anatomical segmentation. The remaining difference in crouch, three-quarter head angle and upright posture is acceptable variation between the illustrated poses. No detached head, extra sword, broken visible hand/weapon join or large new body-size pop was observed in this pair. Its camera remains yaw 141.072449 degrees / orthographic size 1.5858397.

**The two specific continuity blockers documented in the v5 review are accepted as resolved in this final six-frame sample. No further corrective change is required by these inspected transitions.** This conclusion supersedes the earlier non-acceptance for these targets only. It does not claim every intervening frame was visually inspected, perfect anatomical matching, equipped-weapon animation coverage or ground-plane contact: the close-up still uses a plain background. No screenshot, art, metadata, source, manifest, save or Unity state was changed during this final review; only this document was appended.
