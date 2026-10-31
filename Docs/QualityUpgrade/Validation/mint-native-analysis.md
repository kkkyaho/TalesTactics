# Mint native candidate analysis — 2026-10-31

These are independent pixel measurements and pure-managed code probes. Unity was not run. No candidate atlas was imported, no production PNG/meta was changed, and these results are not visual or production approval.

The four candidate PNGs each measure 1122 × 1402. The original 72 imported SpriteRects and their ordered names were used for matching. Original/source provenance is recorded in [mint-original-provenance.json](mint-original-provenance.json); each candidate hash, layout, nontransparent pixel count, ownership count and RGBA histogram hash is recorded in [mint-native-analysis.json](mint-native-analysis.json).

## Separation results

- Sheets 00 and 02 pass the existing automatic ±20% cell-pitch corridor search.
- Sheet 01 fails automatic search at the fourth row boundary: the last safe cut is y=1064, while the automatic interval begins at y=1066. Its real gap is alpha>8-free at top-down rows 1046..1065. Explicit measured cuts preserve that gap without enlarging the automatic tolerance.
- Sheet 03 has three unevenly spaced populated rows. It fails the requested uniform five-row automatic search. Explicit measured cuts place the 12 ordered figures in the first three rows and leave eight cells without alpha>8 artwork.
- All 72 figures pass the same major-component and safety-margin checks using automatic or explicit layouts. Every nontransparent native pixel, including alpha1..8, has exactly one owner. No PNG was rewritten or resized during this analysis.
- Sheet 03 contains distant alpha1..8 pixels in its lower empty-looking area. Keeping those pixels makes the last four native support boxes 728..771 pixels tall. They must not be silently trimmed. Support bounds differ from the visible strong-component bounds.

The optional input [mint-layouts.json](../Generation/mint-layouts.json) supplies only sheets 01 and 03. Each entry is bound to the exact PNG hash, dimensions, ordered frame names, and full-image monotone cuts. It does not authorize production application. `Pack` still defaults to automatic separation; use its final optional `reviewedLayoutsPath` argument to supply this sidecar. Every explicit cut must also have two alpha>8-free pixels on each side. The existing native per-frame RGBA, total count and histogram checks remain mandatory.

## Corresponding original-pose height ratios

The measurement is the bounding-box height of the largest connected alpha>128 component, including connected weapons and raised limbs. It is not a semantic head/torso measurement. Every ratio below compares the candidate with the **same original frame name and index**, so original pose-height changes are already represented.

| Sheet | Frames | Candidate/original minimum | Q25 | Median | Q75 | Maximum | Median after common pose-0 PPU |
|---|---:|---:|---:|---:|---:|---:|---:|
| 00 | 20 | 2.1574 | 2.2591 | 2.3251 | 2.3655 | 2.4510 | 1.0119 |
| 01 | 20 | 2.0748 | 2.1377 | 2.2009 | 2.2756 | 2.3846 | 0.9578 |
| 02 | 20 | 1.9434 | 1.9906 | 2.0239 | 2.0511 | 2.1650 | 0.8808 |
| 03 | 12 | 1.8027 | 1.8296 | 1.8628 | 2.0339 | 2.0882 | 0.8107 |

The median pose-0 native height scale is **2.2978389185**. With that common PPU, sheet 02's corresponding-pose height ratios span 0.8457..0.9422, and sheet 03's span 0.7845..0.9088. This is a continuity risk to inspect before application; separating the figures safely does not resolve it. Do not compensate by silently resizing or assigning unrelated per-frame scales. Full per-frame measurements: [mint-native-frame-ratios.csv](mint-native-frame-ratios.csv).

## Evidence and limits

- [mint-final-probe.log](mint-final-probe.log): preserved initial automatic failures and exploratory explicit-cell diagnostics.
- [mint-ratio-probe.log](mint-ratio-probe.log): final matched-frame distributions using the explicit-layout analysis implementation.
- [explicit-layout-tests.log](explicit-layout-tests.log): 17 pure-managed probe invocations passed, including cut-through-art rejection, invalid coverage rejection, 12/20-figure nonuniform layouts, blank-cell rejection, native ownership, and 18/54/72/80-frame packing geometry. These are **not Unity EditMode results**.
- [explicit-layout-tests-initial-fixture-failure.log](explicit-layout-tests-initial-fixture-failure.log): preserved first test-fixture failure. Its original synthetic spacing still allowed automatic separation; the fixture was corrected to exercise a truly out-of-range safe explicit corridor. The production automatic policy was not relaxed.

Actual Unity `Prepare`/`Pack`, importer application, pivot/PPU continuity, sprite references, and game rendering remain unverified for these four candidates.
