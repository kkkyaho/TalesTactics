# Q3 recovery head candidate

- Date: 2026-10-27.
- Tool: built-in `image_gen.imagegen`, not API/CLI, Python, or programmatic image editing.
- Candidate: `C:/dev/TalesTactics/Logs/SdPolish20261027/ClessAligned/q3-recovery-refined.png`.
- Candidate SHA256: `4A190C581522A058488C7AD9788F929B8B86BAEAD200B8FFF3F123003ED5CB57`.
- Generated original: `C:/Users/USER-PC/.codex/generated_images/01a15ecb-f85b-74d0-999d-ed7070bf6f9f/exec-bfcbdc27-b06a-4715-82b5-68b1cec30adc.png` (copied byte for byte).
- Input target: `C:/dev/TalesTactics/Logs/SdPolish20261027/ClessAligned/q3.png`, SHA256 `9ADB818D583CF0554A718C8AAEA43B6304F9A07B2260606190AA52AE15762292`.
- Reference-only game captures: `D:/TalesTacticsHelpGuide/Builds/SdPolishReview20261027V5/SdMotionReviews/d450f0d5da8447fc82e0564cdee1f7fa/cless.ultimate-Right/0065.png` and `0066.png`.

## Scope and audit

Intended edit: the head and golden hair of bottom-row, third-column `cless_19_2`, with approximately 18% less vertical extent. Visual inspection finds a smaller head/hair silhouette, brown eyes, red headband, and the same recovery direction/pose. The percentage is a prompt target, not a measured anatomical result. Actual game scale and the transition into idle still require the parent's Unity render review.

The generator did not preserve the other 19 cells pixel for pixel: **759,977 RGBA pixels outside the selected cell changed**. Therefore this sheet must not replace all of q3. Parent authorized selecting only this one generated frame through an explicit native-pixel atlas packing override; original sources supply the other 79 atlas frames. The generated target's body pixels also cannot be claimed pixel-identical to the source.

Read-only `System.Drawing.LockBits` comparison, without image modifications:

| Measurement | Original q3 | Candidate |
|---|---:|---:|
| Dimensions | 1122 × 1402 | 1122 × 1402 |
| Fully transparent pixels | 788,276 | 782,551 |
| Fully opaque pixels | 1,612 | 2,108 |
| Partial-alpha pixels | 783,156 | 788,385 |
| Selected cell nonzero-alpha pixels | 37,214 | 34,843 |

The alpha is genuine, not a drawn checkerboard. Partial alpha was already present in the source. Target-cell RGBA changes: 37,638, including 37,526 pixels visible in either input. No original asset, import metadata, production atlas, or Unity state was modified by generation.

A second tool-only restoration attempt was completed before the parent's stop message arrived. It is not selected: `C:/Users/USER-PC/.codex/generated_images/01a15ecb-f85b-74d0-999d-ed7070bf6f9f/exec-4f524dad-315f-434e-b8a4-028017fb1c2c.png`. No further generation was performed. The first candidate makes the requested head reduction clearer.

## Explicit atlas selection implementation

`SdMotionAtlasPacking.PackWithFrameOverride(quadrants, source, output, report, replacementInput, targetFrame)` validates all 20 figures of the additional image with the existing component/ownership rules, then uses only the named frame's owned native pixels. The original 4 inputs supply the other 79 frames. No raster editing or resampling is introduced. The existing `Pack` entry point still uses the original path with no override.

Override reports use schema 4 and record the complete replacement input audit, source path/hash, selected/rejected frame counts (1/19), selected/rejected pixel counts, and `base pixels - original target pixels + selected target pixels`. Unknown target names, reused paths, any existing component ambiguity, changed base-only canvas size, moved unchanged frames, lost pixels, and RGBA mismatches stop before output. Replacement files also participate in the final source-hash race check.

The unchanged-frame comparison is to the base-only pack calculated with the **current** source metadata. An earlier v5 report may have been packed with older source metadata; comparing those reports can legitimately show translation differences. Actual v5/v6 pixel-content comparison and Unity import/render verification remain the parent's integration step.

Implementation SHA256: `789E6A2E0C4621312DEFB9A7BF01B19E8CC7C05AC9A67DB2D5AE0D6A73B63A3F`. Source: `C:/dev/TalesTactics/Assets/TalesTactics/Editor/SdMotionAtlasPacking.cs`. Standalone Roslyn compilation against the installed Unity 6000.6.0f1 Core/Editor/ImageConversion/JSONSerialize and existing Unity.2D.Sprite.Editor assemblies succeeded with no diagnostics. This was a compile check, not a Unity test or a completed pack. No Unity instance was called by this subtask.

## Actual engine packing result

The parent subsequently completed Unity compilation and `PackWithFrameOverride`. The resulting report was read independently: `C:/dev/TalesTactics/Docs/SdCharacters/2026-10-24/cless-v6-packing.json`, schema 4, created `2026-10-27T08:55:54.2152647Z`.

- Output: `C:/dev/TalesTactics/Assets/TalesTactics/Art/SdCharacters/cless-native-v6.png`, 4096 × 4096, 80 frames.
- SHA256: `c6bd896f69d75e1f9df5d8d22af72bb265693777181b3e94c081e5f902c2be44`; independently hashed PNG matches the report.
- Replacement input: 20 major components at both alpha thresholds; complete ownership audit passed. Exactly `cless_19_2` selected, other 19 replacement frames excluded (755,660 nontransparent pixels).
- Owned native-pixel count: **2,974,440 - 37,209 + 34,833 = 2,972,064**. These component-ownership counts can differ from the nominal rectangular cell counts above because detached/weak pixels use the packer's audited ownership assignment.
- `nativeNontransparentRgbaPixelsPreserved = true`; `unchangedFrameGeometryAndRgbaPreserved = true` for 79 frames against the base-only packing.
- A second agent's read-only review found no blocking error in the override path, and independently compared Back poses 17/18/19 to v5: frame metadata, pivot, outline and rect RGBA were exact matches, including zero translation change for those three frames. That three-frame comparison is narrower than a full v5/v6 comparison.

This proves the native-pixel packing contract for the selected 80 frames. Actual imported motion appearance and final build/startup checks are separate validation steps; no human-play or final visual approval is inferred from packing alone.

## Selected generation prompt

> Use case: precise-object-edit. Asset: production sprite-sheet candidate with real transparent alpha. Image 1 is the ONLY edit target, an existing 1122 x 1402 RGBA sprite sheet with 4 columns and 5 rows. Images 2 and 3 are reference-only actual game screenshots: image 2 shows the oversized recovery head and image 3 shows the existing brown-eyed Cless proportion immediately afterward. Return the complete image-1 sprite sheet at exactly 1122x1402 with genuine transparent background, no matte, no checkerboard, no text or cell lines. Make ONE surgical localized change: ONLY in the LAST (fifth) ROW, THIRD COLUMN from the left, the down-sloping-sword recovery pose cless_19_2. This cell lies x=561..840, y=1121..1401 in top-left image coordinates. Reduce the vertical extent of this pose's head AND golden spiky hair by approximately 18%, to harmonize with the smaller head-to-body proportion in reference image 3. Preserve the brown eyes, red headband, facial identity, expression, viewing direction, pixel-illustration finish, and original lighting. Keep the chin/neck attachment and horizontal head center fixed so the body is unchanged. Do not enlarge the face or add hair spikes. Do NOT alter the shoulders, armor, cape, hands, sword, legs, boots, body pose, baseline, or position of this character. All pixels outside this one head-and-hair area, including the other 19 sprites, must remain unchanged. Do not redraw or recompose the sheet, do not shift any figure, do not scale the canvas or whole sprite, do not change the other three directions in the last row. This is only a smaller head-and-hair correction in the bottom-row third-column sprite, preserving the original sheet and its transparent padding.

## Unselected restoration prompt

> Precise object edit, repair pass. Image1 is the immutable ORIGINAL 1122x1402 RGBA sprite sheet. Image2 is a prior candidate whose bottom-row THIRD-column recovery sprite cless_19_2 has the desired 18% shorter head and hair, BUT it incorrectly redrew pixels in all other cells. Return an exact copy of ORIGINAL image1 with only that smaller head/hair from image2 used in the bottom-row third-column sprite. This is a local composite/repair, not repainting or regenerating the sheet. Preserve image1's original RGB and alpha pixels everywhere outside that single head-and-hair patch. In particular all of the OTHER19 characters must be pixel-identical to image1, and that character's shoulders, armor, body, red cape, arms, sword, legs, boots and baseline must also be pixel-identical to image1. Keep the chin/neck join and horizontal head center fixed. Restrict the edit to the head-and-hair silhouette in bottom row (y1121..1401), third column (x561..840). Restore all original opacity from image1 everywhere outside this patch; no translucent repaint of opaque surfaces, no halo, no changes to poses. Output exactly1122x1402 and genuine transparent background alpha, no matte/checkerboard/captions/grid. Do not modify any other sprite or any original sheet spacing.
