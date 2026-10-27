using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TalesTactics.Editor
{
    // Pixel translation only. This utility never imports assets or edits sprite metadata.
    public static class SdMotionAtlasPacking
    {
        const int Columns = 8, Rows = 10, QuadrantColumns = 4, QuadrantRows = 5;
        const int FrameCount = Columns * Rows, MaximumSize = 4096;
        const int MajorPixels = 1000, WeakAlphaMaximum = 8;
        const int MaximumTinyDetachedTiePixels = 4;

        [Serializable] public sealed class Bounds
        {
            public int x, y, width, height;
            public Bounds(RectInt rect) { x = rect.x; y = rect.y; width = rect.width; height = rect.height; }
        }
        [Serializable] public sealed class FloatRect
        {
            public float x, y, width, height;
            public FloatRect(Rect rect) { x = rect.x; y = rect.y; width = rect.width; height = rect.height; }
        }
        [Serializable] public sealed class Pair
        {
            public float x, y;
            public Pair(float x, float y) { this.x = x; this.y = y; }
        }
        [Serializable] public sealed class InputReport
        {
            public string path, sha256, quadrant;
            public int width, height;
            public int majorStrongComponents, majorAlpha8Components, inputNontransparentPixels, assignedNontransparentPixels, inputCanvasEdgeWeakPixels;
            public int alpha1To8Pixels, weakTiePixelCount, detachedAlpha8Components, detachedAlpha8Pixels, splitWeakPixelCount;
            public int tinyDetachedTieComponents, tinyDetachedTiePixels;
            public float minStrongGap;
            public string minStrongGapFrameA, minStrongGapFrameB;
            public WeakComponentReport[] SharedWeakComponents;
            public DetachedTieAssignmentReport[] TinyDetachedTieAssignments;
        }
        [Serializable] public sealed class DetachedTieAssignmentReport
        {
            public int pixelCount, nearestSquaredDistance;
            public Bounds bounds, inputCell;
            public string assignedFrame;
            public string[] tiedNearestFrames;
        }
        [Serializable] public sealed class WeakComponentReport
        {
            public int pixelCount, splitWeakPixelCount;
            public Bounds bounds;
            public string[] frames;
        }
        [Serializable] public sealed class FrameReport
        {
            public string name, originalSpriteId, originalLocalId;
            public int row, column, quadrantIndex, quadrantRow, quadrantColumn;
            public FloatRect originalSpriteRect;
            public Pair originalPivot, nativeToSourceBboxRatio, normalizedCenterErrorPixels, inputBodyCenter;
            public float assignmentDistanceCells, assignmentRunnerUpMarginCells, assignmentCellInteriorMargin;
            public bool inputCrossesNominalCell;
            public Bounds sourceBbox, inputCell, inputBbox, inputSupportBbox, outputCell, outputBbox, outputSupportBbox;
            public int preferredTranslationX, preferredTranslationY, cellFitAdjustmentX, cellFitAdjustmentY;
            public int translationX, translationY, copiedNontransparentPixels;
        }
        [Serializable] public sealed class PackingReport
        {
            public int schemaVersion = 3, sourceWidth, sourceHeight, width, height, frameCount, adjustedFrameCount;
            public float maximumCellFitAdjustmentPixels;
            public string source, sourceGuid, sourceSha256, sourceMetaSha256, output, outputSha256, createdUtc;
            public float sourcePixelsPerUnit;
            public string coordinates = "Pixel bounds use bottom-left coordinates with exclusive right/top edges; row/column are zero-based from top-left.";
            public string quadrantOrder = "0 top-left, 1 top-right, 2 bottom-left, 3 bottom-right; each input is 4 columns by 5 rows.";
            public string assignment = "Original quadrant row/column slot is authoritative. Each major pixel median must occupy a unique slot, remain at least 0.15 cell inside its boundary, and agree with the nearest source-body frame. Source distance and runner-up margin remain diagnostics.";
            public string placement = "Integer translation only. Prefer alpha>128 bbox centers at normalized source bbox centers, rounded to the nearest pixel. If no centered layout fits at or below 4096, use a 4096 square and clamp each preferred integer translation to the nearest allowed translation inside its original cell with a one-pixel gutter. Report every correction; native RGBA pixels and frame identities are unchanged.";
            public string limitations = "No resizing, redrawing, filtering, color/alpha correction, PPU assignment, slicing or visual approval. Separate measurement and visual review are required.";
            public bool nativeNontransparentRgbaPixelsPreserved;
            public string weakOwnershipPolicy = "Eight-connected alpha>128 major figures and alpha>8 major regions must each number 20 per input and correspond one-to-one. Alpha 1..8 pixels use the nearest alpha>8 figure pixel (Euclidean pixel-center distance); equal-distance owners resolve to the lowest original frame index. No RGBA values or nontransparent pixels are removed.";
            public string detachedTiePolicy = "A detached alpha>8 component tied between nearest main-region owners may use its original grid-cell owner only when it contains at most 4 pixels, its entire bounds fit exactly one original cell, and that cell owner is among the tied nearest owners. Every such assignment is reported. All other detached ties and joined major figures are rejected.";
            public InputReport[] inputs;
            public FrameOverrideReport frameOverride;
            public string[] originalSpriteIds;
            public FrameReport[] frames;
        }
        [Serializable] public sealed class FrameOverrideReport
        {
            public string targetFrame, replacementPath, replacementSha256;
            public InputReport replacementInput;
            public int selectedFrameCount, rejectedFrameCount, originalFrameNontransparentPixels;
            public int selectedNontransparentPixels, rejectedNontransparentPixels, baseInputNontransparentPixels, effectiveNontransparentPixels;
            public int baseOnlyWidth, baseOnlyHeight, unchangedFrameCount;
            public bool unchangedFrameGeometryAndRgbaPreserved;
            public string policy = "Validate all 20 replacement figures using unchanged AssignInput rules, then select only the named frame. The other 19 replacement figures are excluded. Preserve the base-only atlas size and the other 79 frames' placement and native RGBA pixels. No resizing or pixel editing.";
        }
        sealed class Raster
        {
            public string path, hash;
            public int width, height;
            public Color32[] pixels;
        }
        sealed class Frame
        {
            public SpriteRect sprite;
            public string localId;
            public int row, column, quadrant, quadrantRow, quadrantColumn;
            public RectInt sourceBbox, inputCell, inputBbox, supportBbox, outputCell;
            public int offsetX, offsetY, preferredOffsetX, preferredOffsetY;
            public Vector2 sourceBodyCenter, inputBodyCenter;
            public float assignmentDistance, assignmentMargin, cellInteriorMargin;
            public int[] ownedPixels;
            public Component strong;
            public Raster replacementRaster;
        }

        /// <summary>
        /// Packs TL, TR, BL, BR 4x5 sheets into an 8x10 square PNG without resampling.
        /// Source must be an already imported square PNG with exactly 80 SpriteRects.
        /// Outputs must be new paths in project Temp or the SD candidate/document folders.
        /// Throws before writing for ambiguous major/alpha>8 ownership, strong-region bridges, or output overflow.
        /// Tied detached fragments of at most four pixels may use a unique containing original cell; every exception is reported.
        /// Native figures may cross nominal input cells; output frames must fit their original cells with a gutter.
        /// </summary>
        public static void Pack(string[] quadrants, string source, string output, string report)
        {
            PackCore(quadrants, source, output, report, null, null);
        }

        /// <summary>
        /// Validates a complete additional 4x5 sheet but selects exactly one named frame from it.
        /// The other 79 frames retain the base-only pack's geometry and RGBA pixels.
        /// No input image, importer, metadata or production asset is modified.
        /// </summary>
        public static void PackWithFrameOverride(string[] quadrants, string source, string output, string report,
            string replacementInput, string targetFrame)
        {
            if (string.IsNullOrWhiteSpace(replacementInput) || string.IsNullOrWhiteSpace(targetFrame))
                throw new ArgumentException("A replacement input path and exact target frame name are required.");
            PackCore(quadrants, source, output, report, replacementInput, targetFrame);
        }

        static void PackCore(string[] quadrants, string source, string output, string report,
            string replacementInput, string targetFrame)
        {
            if (quadrants == null || quadrants.Length != 4)
                throw new ArgumentException("Exactly four inputs are required, in TL, TR, BL, BR order.");
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string sourcePath = Resolve(project, source), outputPath = Resolve(project, output), reportPath = Resolve(project, report);
            string[] inputPaths = quadrants.Select(p => Resolve(project, p)).ToArray();
            string replacementPath = replacementInput == null ? null : Resolve(project, replacementInput);
            RequirePng(sourcePath);
            foreach (string path in inputPaths) RequirePng(path);
            if (replacementPath != null) RequirePng(replacementPath);
            RequireNewOutput(outputPath, ".png", project, "Temp", "Assets/TalesTactics/Art/SdCharacters", "Assets/SdCharacters");
            RequireNewOutput(reportPath, ".json", project, "Temp", "Docs/SdCharacters", "Docs/SdArtDirection");
            if (!Within(sourcePath, Path.Combine(project, "Assets")) || !File.Exists(sourcePath + ".meta"))
                throw new InvalidOperationException("Source must be an existing imported Assets PNG with its .meta file.");
            if (inputPaths.Concat(new[] { sourcePath, outputPath, reportPath }).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 7)
                throw new InvalidOperationException("All inputs, source and outputs must be distinct.");
            if (replacementPath != null && inputPaths.Concat(new[] { sourcePath, outputPath, reportPath })
                .Contains(replacementPath, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("The replacement input must be distinct from every base input, source and output.");
            string sourceAsset = sourcePath.Substring(project.Length + 1).Replace('\\', '/');
            string metaHash = Hash(File.ReadAllBytes(sourcePath + ".meta"));
            Raster original = ReadPng(sourcePath);
            if (original.width != original.height)
                throw new InvalidOperationException("A square output cannot preserve the aspect of this non-square source.");
            Raster[] inputs = inputPaths.Select(ReadPng).ToArray();
            var importer = AssetImporter.GetAtPath(sourceAsset) as TextureImporter;
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple)
                throw new InvalidOperationException("Source must already have a multiple-sprite importer.");
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Source has no sprite data provider.");
            provider.InitSpriteEditorDataProvider();
            SpriteRect[] rects = provider.GetSpriteRects();
            string guid = AssetDatabase.AssetPathToGUID(sourceAsset);
            if (string.IsNullOrEmpty(guid) || rects == null || rects.Length != FrameCount
                || rects.Any(r => string.IsNullOrEmpty(r.name) || r.spriteID == default(GUID))
                || rects.Select(r => r.name).Distinct().Count() != FrameCount
                || rects.Select(r => r.spriteID.ToString()).Distinct().Count() != FrameCount)
                throw new InvalidOperationException("Source must have exactly 80 unique, named SpriteRects and Sprite IDs.");
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(sourceAsset).OfType<Sprite>().ToArray();
            if (sprites.Length != FrameCount || sprites.Select(s => s.name).Distinct().Count() != FrameCount
                || !sprites.Select(s => s.name).OrderBy(n => n).SequenceEqual(rects.Select(r => r.name).OrderBy(n => n)))
                throw new InvalidOperationException("Imported sprite names/count differ from the source metadata.");
            var localIds = new Dictionary<string, string>();
            foreach (Sprite sprite in sprites)
            {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string spriteGuid, out long localId) || spriteGuid != guid)
                    throw new InvalidOperationException("Cannot verify original local ID: " + sprite.name);
                localIds.Add(sprite.name, localId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            if (localIds.Values.Distinct().Count() != FrameCount)
                throw new InvalidOperationException("Duplicate original sprite local IDs.");

            var frames = new Frame[FrameCount];
            foreach (SpriteRect sprite in rects)
            {
                Rect rect = sprite.rect;
                if (!(rect.width > 0 && rect.height > 0 && rect.x >= 0 && rect.y >= 0
                    && rect.xMax <= original.width && rect.yMax <= original.height))
                    throw new InvalidOperationException("Invalid source rect: " + sprite.name);
                int column = Mathf.FloorToInt(rect.center.x / original.width * Columns);
                int row = Mathf.FloorToInt((original.height - rect.center.y) / original.height * Rows);
                if (column < 0 || column >= Columns || row < 0 || row >= Rows || frames[row * Columns + column] != null)
                    throw new InvalidOperationException("Source SpriteRect centers do not map one-to-one to an 8x10 grid: " + sprite.name);
                int quadrant = row / QuadrantRows * 2 + column / QuadrantColumns;
                int qr = row % QuadrantRows, qc = column % QuadrantColumns;
                var sourceCell = new RectInt(Mathf.FloorToInt(rect.x), Mathf.FloorToInt(rect.y),
                    Mathf.CeilToInt(rect.xMax) - Mathf.FloorToInt(rect.x), Mathf.CeilToInt(rect.yMax) - Mathf.FloorToInt(rect.y));
                frames[row * Columns + column] = new Frame
                {
                    sprite = sprite, localId = localIds[sprite.name], row = row, column = column,
                    quadrant = quadrant, quadrantRow = qr, quadrantColumn = qc,
                    sourceBbox = AlphaBounds(original, sourceCell, 128, sprite.name),
                    sourceBodyCenter = AlphaMedian(original, sourceCell)
                };
            }
            if (frames.Any(f => f == null)) throw new InvalidOperationException("Source grid is incomplete.");
            var inputReports = new InputReport[4];
            for (int q = 0; q < 4; q++)
                inputReports[q] = AssignInput(inputs[q], frames.Where(f => f.quadrant == q).OrderBy(f => f.row * Columns + f.column).ToArray(), original.width, original.height, q);
            FrameOverrideReport replacementReport = null;
            Frame[] baseFrames = null;
            Raster replacement = null;
            int baseSize = 0;
            if (replacementPath != null)
            {
                int selectedIndex = Array.FindIndex(frames, f => f.sprite.name == targetFrame);
                if (selectedIndex < 0) throw new InvalidOperationException("Unknown replacement target frame: " + targetFrame);
                baseSize = FindSize(frames, original.width, original.height);
                baseFrames = frames.ToArray();
                Frame selected = frames[selectedIndex];
                replacement = ReadPng(replacementPath);
                Frame[] candidates = frames.Where(f => f.quadrant == selected.quadrant)
                    .OrderBy(f => f.row * Columns + f.column).Select(CloneForInput).ToArray();
                InputReport replacementAudit = AssignInput(replacement, candidates, original.width, original.height, selected.quadrant);
                Frame chosen = candidates.Single(f => f.sprite.name == targetFrame);
                chosen.replacementRaster = replacement;
                frames[selectedIndex] = chosen;
                int basePixels = inputReports.Sum(r => r.inputNontransparentPixels);
                replacementReport = new FrameOverrideReport
                {
                    targetFrame = targetFrame, replacementPath = replacement.path, replacementSha256 = replacement.hash,
                    replacementInput = replacementAudit, selectedFrameCount = 1, rejectedFrameCount = candidates.Length - 1,
                    originalFrameNontransparentPixels = selected.ownedPixels.Length,
                    selectedNontransparentPixels = chosen.ownedPixels.Length,
                    rejectedNontransparentPixels = replacementAudit.inputNontransparentPixels - chosen.ownedPixels.Length,
                    baseInputNontransparentPixels = basePixels,
                    effectiveNontransparentPixels = basePixels - selected.ownedPixels.Length + chosen.ownedPixels.Length,
                    baseOnlyWidth = baseSize, baseOnlyHeight = baseSize, unchangedFrameCount = FrameCount - 1
                };
            }
            int size = FindSize(frames, original.width, original.height);
            if (replacementReport != null && size != baseSize)
                throw new InvalidOperationException("A one-frame override must preserve the base-only atlas dimensions.");
            var packed = new Color32[checked(size * size)];
            var entries = new List<FrameReport>(FrameCount);
            foreach (Frame frame in frames)
            {
                Raster input = frame.replacementRaster ?? inputs[frame.quadrant];
                int count = 0;
                foreach (int pixelIndex in frame.ownedPixels)
                {
                    int x = pixelIndex % input.width, y = pixelIndex / input.width;
                    Color32 pixel = input.pixels[pixelIndex];
                    int target = (y + frame.offsetY) * size + x + frame.offsetX;
                    if (pixel.a == 0 || packed[target].a != 0) throw new InvalidOperationException("Invalid ownership or overlapping frame pixels.");
                    packed[target] = pixel;
                    count++;
                }
                RectInt outputBox = Translate(frame.inputBbox, frame.offsetX, frame.offsetY);
                entries.Add(new FrameReport
                {
                    name = frame.sprite.name, originalSpriteId = frame.sprite.spriteID.ToString(), originalLocalId = frame.localId,
                    row = frame.row, column = frame.column, quadrantIndex = frame.quadrant,
                    quadrantRow = frame.quadrantRow, quadrantColumn = frame.quadrantColumn,
                    originalSpriteRect = new FloatRect(frame.sprite.rect),
                    originalPivot = new Pair(frame.sprite.pivot.x, frame.sprite.pivot.y),
                    inputBodyCenter = new Pair(frame.inputBodyCenter.x, frame.inputBodyCenter.y),
                    assignmentDistanceCells = frame.assignmentDistance, assignmentRunnerUpMarginCells = frame.assignmentMargin,
                    assignmentCellInteriorMargin = frame.cellInteriorMargin,
                    inputCrossesNominalCell = !FitsWithGutter(frame.supportBbox, frame.inputCell),
                    sourceBbox = new Bounds(frame.sourceBbox), inputCell = new Bounds(frame.inputCell),
                    inputBbox = new Bounds(frame.inputBbox), inputSupportBbox = new Bounds(frame.supportBbox),
                    outputCell = new Bounds(frame.outputCell), outputBbox = new Bounds(outputBox),
                    outputSupportBbox = new Bounds(Translate(frame.supportBbox, frame.offsetX, frame.offsetY)),
                    preferredTranslationX = frame.preferredOffsetX, preferredTranslationY = frame.preferredOffsetY,
                    cellFitAdjustmentX = frame.offsetX - frame.preferredOffsetX, cellFitAdjustmentY = frame.offsetY - frame.preferredOffsetY,
                    translationX = frame.offsetX, translationY = frame.offsetY, copiedNontransparentPixels = count,
                    nativeToSourceBboxRatio = new Pair((float)frame.inputBbox.width / frame.sourceBbox.width, (float)frame.inputBbox.height / frame.sourceBbox.height),
                    normalizedCenterErrorPixels = new Pair(outputBox.center.x - frame.sourceBbox.center.x * size / original.width,
                        outputBox.center.y - frame.sourceBbox.center.y * size / original.height)
                });
            }
            int inputPixelCount = replacementReport == null ? inputReports.Sum(r => r.inputNontransparentPixels)
                : replacementReport.effectiveNontransparentPixels;
            if (entries.Sum(f => f.copiedNontransparentPixels) != inputPixelCount || packed.Count(p => p.a > 0) != inputPixelCount)
                throw new InvalidOperationException("Nontransparent pixel count changed during packing.");
            if (replacementReport != null)
            {
                // Recalculate the independent base-only placements; the selected candidate is not in this array.
                if (FindSize(baseFrames, original.width, original.height) != size)
                    throw new InvalidOperationException("Base-only layout changed during replacement verification.");
                for (int i = 0; i < frames.Length; i++)
                {
                    Frame frame = frames[i], baseline = baseFrames[i];
                    if (frame.sprite.name == targetFrame) continue;
                    FrameReport actual = entries[i];
                    if (actual.translationX != baseline.offsetX || actual.translationY != baseline.offsetY
                        || !SameBounds(actual.outputCell, baseline.outputCell)
                        || !SameBounds(actual.outputBbox, Translate(baseline.inputBbox, baseline.offsetX, baseline.offsetY))
                        || !SameBounds(actual.outputSupportBbox, Translate(baseline.supportBbox, baseline.offsetX, baseline.offsetY)))
                        throw new InvalidOperationException("Override moved an unchanged frame: " + frame.sprite.name);
                    Raster input = inputs[baseline.quadrant];
                    foreach (int index in baseline.ownedPixels)
                    {
                        int target = (index / input.width + baseline.offsetY) * size + index % input.width + baseline.offsetX;
                        if (!SamePixel(input.pixels[index], packed[target]))
                            throw new InvalidOperationException("Override changed RGBA in an unchanged frame: " + frame.sprite.name);
                    }
                }
                replacementReport.unchangedFrameGeometryAndRgbaPreserved = true;
            }
            byte[] png = EncodePng(packed, size);
            var document = new PackingReport
            {
                source = sourcePath, sourceGuid = guid, sourceSha256 = original.hash, sourceMetaSha256 = metaHash,
                sourceWidth = original.width, sourceHeight = original.height, sourcePixelsPerUnit = importer.spritePixelsPerUnit,
                output = outputPath, outputSha256 = Hash(png), width = size, height = size, frameCount = FrameCount,
                adjustedFrameCount = entries.Count(f => f.cellFitAdjustmentX != 0 || f.cellFitAdjustmentY != 0),
                maximumCellFitAdjustmentPixels = entries.Max(f => Mathf.Sqrt((float)f.cellFitAdjustmentX * f.cellFitAdjustmentX
                    + (float)f.cellFitAdjustmentY * f.cellFitAdjustmentY)),
                createdUtc = DateTime.UtcNow.ToString("O"), nativeNontransparentRgbaPixelsPreserved = true,
                originalSpriteIds = entries.Select(f => f.originalSpriteId).ToArray(), frames = entries.ToArray(),
                inputs = inputReports
            };
            if (replacementReport != null) { document.schemaVersion = 4; document.frameOverride = replacementReport; }
            if (Hash(File.ReadAllBytes(sourcePath)) != original.hash || Hash(File.ReadAllBytes(sourcePath + ".meta")) != metaHash
                || inputs.Any(input => Hash(File.ReadAllBytes(input.path)) != input.hash)
                || (replacement != null && Hash(File.ReadAllBytes(replacement.path)) != replacement.hash))
                throw new InvalidOperationException("An input/source changed while packing. No output was written.");
            WritePair(outputPath, png, reportPath, Encoding.UTF8.GetBytes(JsonUtility.ToJson(document, true) + "\n"));
            Debug.Log("SD native pixel packing complete: " + size + "x" + size + ", 80 frames. No import or metadata changes. " + outputPath);
        }

        static Frame CloneForInput(Frame source)
        {
            return new Frame
            {
                sprite = source.sprite, localId = source.localId, row = source.row, column = source.column,
                quadrant = source.quadrant, quadrantRow = source.quadrantRow, quadrantColumn = source.quadrantColumn,
                sourceBbox = source.sourceBbox, sourceBodyCenter = source.sourceBodyCenter
            };
        }
        static bool SameBounds(Bounds bounds, RectInt rect)
        {
            return bounds.x == rect.x && bounds.y == rect.y && bounds.width == rect.width && bounds.height == rect.height;
        }
        static bool SamePixel(Color32 a, Color32 b) { return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a; }


        sealed class Component
        {
            public int id;
            public int[] pixels;
            public RectInt bounds;
        }
        sealed class Components
        {
            public int[] labels;
            public List<Component> parts;
        }
        sealed class Seed
        {
            public int x, y, owner;
        }
        // Exact Euclidean queries over pixel centers. Spatial bins only accelerate
        // the search; it stops after even equal-distance seeds outside are excluded.
        sealed class PixelIndex
        {
            const int BinSize = 8;
            readonly int width, height, columns, rows;
            readonly List<Seed>[] bins;
            public PixelIndex(int width, int height)
            {
                this.width = width; this.height = height;
                columns = (width + BinSize - 1) / BinSize; rows = (height + BinSize - 1) / BinSize;
                bins = new List<Seed>[columns * rows];
            }
            public void Add(int pixel, int owner)
            {
                int x = pixel % width, y = pixel / width, bin = y / BinSize * columns + x / BinSize;
                if (bins[bin] == null) bins[bin] = new List<Seed>();
                bins[bin].Add(new Seed { x = x, y = y, owner = owner });
            }
            public uint Nearest(int pixel, out int squaredDistance)
            {
                int x = pixel % width, y = pixel / width, bx = x / BinSize, by = y / BinSize;
                int best = int.MaxValue; uint owners = 0;
                for (int radius = 0; radius < Math.Max(columns, rows); radius++)
                {
                    int left = Math.Max(0, bx - radius), right = Math.Min(columns - 1, bx + radius);
                    int bottom = Math.Max(0, by - radius), top = Math.Min(rows - 1, by + radius);
                    for (int yy = bottom; yy <= top; yy++)
                    for (int xx = left; xx <= right; xx++)
                    {
                        if (radius > 0 && Math.Abs(xx - bx) != radius && Math.Abs(yy - by) != radius) continue;
                        List<Seed> seeds = bins[yy * columns + xx];
                        if (seeds == null) continue;
                        foreach (Seed seed in seeds)
                        {
                            int dx = seed.x - x, dy = seed.y - y, distance = dx * dx + dy * dy;
                            if (distance < best) { best = distance; owners = 1u << seed.owner; }
                            else if (distance == best) owners |= 1u << seed.owner;
                        }
                    }
                    int outside = int.MaxValue;
                    if (left > 0) outside = Math.Min(outside, x - left * BinSize + 1);
                    if (right < columns - 1) outside = Math.Min(outside, (right + 1) * BinSize - x);
                    if (bottom > 0) outside = Math.Min(outside, y - bottom * BinSize + 1);
                    if (top < rows - 1) outside = Math.Min(outside, (top + 1) * BinSize - y);
                    if (owners != 0 && (outside == int.MaxValue || (long)outside * outside > best))
                    { squaredDistance = best; return owners; }
                }
                throw new InvalidOperationException("No alpha>8 owner seed found.");
            }
        }
        static int FirstOwner(uint mask)
        {
            for (int i = 0; i < 20; i++) if ((mask & (1u << i)) != 0) return i;
            throw new InvalidOperationException("Missing pixel owner.");
        }
        static bool MultipleOwners(uint mask) { return (mask & (mask - 1)) != 0; }

        static int TinyDetachedTieOwner(int pixelCount, RectInt bounds, RectInt[] cells, uint nearestOwners)
        {
            if (pixelCount < 1 || pixelCount > MaximumTinyDetachedTiePixels || !MultipleOwners(nearestOwners)) return -1;
            int owner = -1;
            for (int i = 0; i < cells.Length; i++)
            {
                RectInt cell = cells[i];
                if (bounds.xMin < cell.xMin || bounds.xMax > cell.xMax || bounds.yMin < cell.yMin || bounds.yMax > cell.yMax) continue;
                if (owner >= 0) return -1;
                owner = i;
            }
            return owner >= 0 && (nearestOwners & (1u << owner)) != 0 ? owner : -1;
        }

        static InputReport AssignInput(Raster input, Frame[] frames, int sourceWidth, int sourceHeight, int quadrant)
        {
            Components strong = Extract(input, 128), alpha8 = Extract(input, WeakAlphaMaximum);
            Component[] majors = strong.parts.Where(p => p.pixels.Length >= MajorPixels).ToArray();
            Component[] regions = alpha8.parts.Where(p => p.pixels.Length >= MajorPixels).ToArray();
            if (majors.Length != 20 || regions.Length != 20)
                throw new InvalidOperationException("Expected exactly 20 major figures at alpha>128 and alpha>8 in quadrant " + quadrant
                    + "; found " + majors.Length + "/" + regions.Length + ".");
            var strongOwners = new Dictionary<int, int>();
            var assigned = new bool[20];
            foreach (Component major in majors)
            {
                Vector2 center = Median(input.width, major.pixels);
                int owner = -1; float best = float.PositiveInfinity, runner = float.PositiveInfinity;
                for (int i = 0; i < frames.Length; i++)
                {
                    Frame frame = frames[i];
                    float expectedX = (frame.sourceBodyCenter.x / sourceWidth * 2 - quadrant % 2) * input.width;
                    float expectedY = (frame.sourceBodyCenter.y / sourceHeight * 2 - (quadrant < 2 ? 1 : 0)) * input.height;
                    float dx = (center.x - expectedX) / (input.width / 4f), dy = (center.y - expectedY) / (input.height / 5f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance < best) { runner = best; best = distance; owner = i; }
                    else if (distance < runner) runner = distance;
                }
                // Original source slot order is authoritative. Redrawn figures can sit
                // differently within their cells, so a source-body runner-up distance
                // alone must not override an unambiguous grid slot.
                int slotOwner = -1; float cellMargin = 0;
                for (int i = 0; i < frames.Length; i++)
                {
                    RectInt cell = Cell(input.width, input.height, frames[i].quadrantColumn, frames[i].quadrantRow, 4, 5);
                    if (center.x < cell.xMin || center.x >= cell.xMax || center.y < cell.yMin || center.y >= cell.yMax) continue;
                    slotOwner = i;
                    cellMargin = Mathf.Min(Mathf.Min((center.x - cell.xMin) / cell.width, (cell.xMax - center.x) / cell.width),
                        Mathf.Min((center.y - cell.yMin) / cell.height, (cell.yMax - center.y) / cell.height));
                    break;
                }
                if (owner < 0 || slotOwner != owner || assigned[owner] || best > .75f || cellMargin < .15f)
                    throw new InvalidOperationException("Ambiguous major-figure slot in quadrant " + quadrant + " at " + center
                        + ": slot=" + (slotOwner < 0 ? "none" : frames[slotOwner].sprite.name)
                        + ", nearest=" + (owner < 0 ? "none" : frames[owner].sprite.name)
                        + ", sourceDistance=" + best + ", cellInteriorMargin=" + cellMargin + ".");
                assigned[owner] = true; strongOwners.Add(major.id, owner);
                frames[owner].strong = major; frames[owner].inputBodyCenter = center;
                frames[owner].assignmentDistance = best; frames[owner].assignmentMargin = runner - best;
                frames[owner].cellInteriorMargin = cellMargin;
            }
            if (assigned.Any(value => !value)) throw new InvalidOperationException("Incomplete one-to-one figure assignment.");
            var ownership = Enumerable.Repeat(-1, input.pixels.Length).ToArray();
            var index = new PixelIndex(input.width, input.height);
            var detached = new List<Component>();
            foreach (Component region in alpha8.parts)
            {
                uint owners = 0;
                foreach (int pixel in region.pixels)
                    if (strongOwners.TryGetValue(strong.labels[pixel], out int owner)) owners |= 1u << owner;
                if (MultipleOwners(owners))
                    throw new InvalidOperationException("Alpha>8 region joins multiple major figures in quadrant " + quadrant + ": "
                        + string.Join(", ", Enumerable.Range(0, 20).Where(i => (owners & (1u << i)) != 0).Select(i => frames[i].sprite.name)));
                if (owners == 0) { detached.Add(region); continue; }
                int assignedOwner = FirstOwner(owners);
                foreach (int pixel in region.pixels) { ownership[pixel] = assignedOwner; index.Add(pixel, assignedOwner); }
            }
            // Assign isolated alpha>8 fragments against the unchanged main regions.
            // Only tiny, wholly single-cell ties may use that original cell's tied owner.
            var detachedOwners = new List<int>();
            var detachedTies = new List<DetachedTieAssignmentReport>();
            RectInt[] inputCells = frames.Select(f => Cell(input.width, input.height, f.quadrantColumn, f.quadrantRow, 4, 5)).ToArray();
            foreach (Component fragment in detached)
            {
                int best = int.MaxValue; uint owners = 0;
                foreach (int pixel in fragment.pixels)
                {
                    uint near = index.Nearest(pixel, out int distance);
                    if (distance < best) { best = distance; owners = near; }
                    else if (distance == best) owners |= near;
                }
                int assignedOwner;
                if (MultipleOwners(owners))
                {
                    assignedOwner = TinyDetachedTieOwner(fragment.pixels.Length, fragment.bounds, inputCells, owners);
                    if (assignedOwner < 0)
                        throw new InvalidOperationException("Ambiguous detached alpha>8 fragment in quadrant " + quadrant + " at " + fragment.bounds
                            + " (" + fragment.pixels.Length + " pixels): no unique original-cell owner among tied nearest owners within the four-pixel limit.");
                    detachedTies.Add(new DetachedTieAssignmentReport
                    {
                        pixelCount = fragment.pixels.Length, nearestSquaredDistance = best,
                        bounds = new Bounds(fragment.bounds), inputCell = new Bounds(inputCells[assignedOwner]),
                        assignedFrame = frames[assignedOwner].sprite.name,
                        tiedNearestFrames = Enumerable.Range(0, frames.Length).Where(i => (owners & (1u << i)) != 0).Select(i => frames[i].sprite.name).ToArray()
                    });
                }
                else assignedOwner = FirstOwner(owners);
                detachedOwners.Add(assignedOwner);
            }
            for (int i = 0; i < detached.Count; i++)
                foreach (int pixel in detached[i].pixels) { ownership[pixel] = detachedOwners[i]; index.Add(pixel, detachedOwners[i]); }

            int weakPixels = 0, ties = 0, nontransparent = 0, edgeWeakPixels = 0;
            for (int pixel = 0; pixel < input.pixels.Length; pixel++)
            {
                int alpha = input.pixels[pixel].a;
                if (alpha == 0) continue;
                nontransparent++;
                if (alpha > WeakAlphaMaximum)
                {
                    if (ownership[pixel] < 0) throw new InvalidOperationException("Unassigned alpha>8 pixel.");
                    continue;
                }
                uint nearest = index.Nearest(pixel, out _);
                if (MultipleOwners(nearest)) ties++;
                ownership[pixel] = FirstOwner(nearest); weakPixels++;
                int x = pixel % input.width, y = pixel / input.width;
                if (x == 0 || y == 0 || x == input.width - 1 || y == input.height - 1) edgeWeakPixels++;
            }
            var owned = Enumerable.Range(0, 20).Select(_ => new List<int>()).ToArray();
            for (int pixel = 0; pixel < input.pixels.Length; pixel++)
                if (input.pixels[pixel].a > 0) owned[ownership[pixel]].Add(pixel);
            for (int i = 0; i < frames.Length; i++)
            {
                Frame frame = frames[i];
                frame.ownedPixels = owned[i].ToArray();
                frame.supportBbox = PixelBounds(input.width, frame.ownedPixels);
                frame.inputBbox = PixelBounds(input.width, frame.ownedPixels.Where(p => input.pixels[p].a > 128).ToArray());
                frame.inputCell = Cell(input.width, input.height, frame.quadrantColumn, frame.quadrantRow, 4, 5);
                RectInt visibleBbox = PixelBounds(input.width, frame.ownedPixels.Where(p => input.pixels[p].a > WeakAlphaMaximum).ToArray());
                if (visibleBbox.xMin == 0 || visibleBbox.yMin == 0
                    || visibleBbox.xMax == input.width || visibleBbox.yMax == input.height)
                    throw new InvalidOperationException("Alpha>8 artwork touches the input canvas edge: " + frame.sprite.name);
            }
            Components all = Extract(input, 0);
            var shared = new List<WeakComponentReport>();
            foreach (Component component in all.parts)
            {
                uint owners = 0;
                foreach (int pixel in component.pixels)
                    if (strongOwners.TryGetValue(strong.labels[pixel], out int owner)) owners |= 1u << owner;
                if (!MultipleOwners(owners)) continue;
                shared.Add(new WeakComponentReport
                {
                    pixelCount = component.pixels.Length, bounds = new Bounds(component.bounds),
                    splitWeakPixelCount = component.pixels.Count(p => input.pixels[p].a <= WeakAlphaMaximum),
                    frames = Enumerable.Range(0, 20).Where(i => (owners & (1u << i)) != 0).Select(i => frames[i].sprite.name).ToArray()
                });
            }
            int assignedCount = owned.Sum(p => p.Count);
            if (assignedCount != nontransparent) throw new InvalidOperationException("Pixel assignment lost or duplicated pixels.");
            float gap = StrongGap(input, frames, out string gapA, out string gapB);
            return new InputReport
            {
                path = input.path, sha256 = input.hash, width = input.width, height = input.height,
                quadrant = new[] { "top-left", "top-right", "bottom-left", "bottom-right" }[quadrant],
                majorStrongComponents = majors.Length, majorAlpha8Components = regions.Length,
                inputNontransparentPixels = nontransparent, assignedNontransparentPixels = assignedCount, inputCanvasEdgeWeakPixels = edgeWeakPixels,
                alpha1To8Pixels = weakPixels, weakTiePixelCount = ties,
                detachedAlpha8Components = detached.Count, detachedAlpha8Pixels = detached.Sum(c => c.pixels.Length),
                tinyDetachedTieComponents = detachedTies.Count, tinyDetachedTiePixels = detachedTies.Sum(t => t.pixelCount),
                TinyDetachedTieAssignments = detachedTies.ToArray(),
                SharedWeakComponents = shared.ToArray(), splitWeakPixelCount = shared.Sum(c => c.splitWeakPixelCount),
                minStrongGap = gap, minStrongGapFrameA = gapA, minStrongGapFrameB = gapB
            };
        }
        static Components Extract(Raster raster, int threshold)
        {
            var labels = new int[raster.pixels.Length];
            var queue = new int[raster.pixels.Length];
            var parts = new List<Component>();
            for (int start = 0; start < raster.pixels.Length; start++)
            {
                if (labels[start] != 0 || raster.pixels[start].a <= threshold) continue;
                int id = parts.Count + 1, head = 0, tail = 1;
                queue[0] = start; labels[start] = id;
                while (head < tail)
                {
                    int pixel = queue[head++], x = pixel % raster.width, y = pixel / raster.width;
                    for (int yy = Math.Max(0, y - 1); yy <= Math.Min(raster.height - 1, y + 1); yy++)
                    for (int xx = Math.Max(0, x - 1); xx <= Math.Min(raster.width - 1, x + 1); xx++)
                    {
                        int neighbor = yy * raster.width + xx;
                        if (labels[neighbor] != 0 || raster.pixels[neighbor].a <= threshold) continue;
                        labels[neighbor] = id; queue[tail++] = neighbor;
                    }
                }
                var pixels = new int[tail]; Array.Copy(queue, pixels, tail);
                parts.Add(new Component { id = id, pixels = pixels, bounds = PixelBounds(raster.width, pixels) });
            }
            return new Components { labels = labels, parts = parts };
        }
        static RectInt PixelBounds(int width, int[] pixels)
        {
            if (pixels.Length == 0) throw new InvalidOperationException("Empty owned figure.");
            int xmin = int.MaxValue, ymin = int.MaxValue, xmax = -1, ymax = -1;
            foreach (int pixel in pixels)
            {
                int x = pixel % width, y = pixel / width;
                xmin = Math.Min(xmin, x); ymin = Math.Min(ymin, y); xmax = Math.Max(xmax, x); ymax = Math.Max(ymax, y);
            }
            return new RectInt(xmin, ymin, xmax - xmin + 1, ymax - ymin + 1);
        }
        static Vector2 Median(int width, int[] pixels)
        {
            int[] xs = pixels.Select(p => p % width).ToArray(), ys = pixels.Select(p => p / width).ToArray();
            Array.Sort(xs); Array.Sort(ys);
            int lower = (pixels.Length - 1) / 2, upper = pixels.Length / 2;
            return new Vector2((xs[lower] + xs[upper]) * .5f, (ys[lower] + ys[upper]) * .5f);
        }
        static Vector2 AlphaMedian(Raster raster, RectInt rect)
        {
            var pixels = new List<int>();
            for (int y = rect.yMin; y < rect.yMax; y++)
            for (int x = rect.xMin; x < rect.xMax; x++)
                if (raster.pixels[y * raster.width + x].a > 128) pixels.Add(y * raster.width + x);
            if (pixels.Count == 0) throw new InvalidOperationException("Empty source body.");
            return Median(raster.width, pixels.ToArray());
        }
        static int RectGapSquared(RectInt a, RectInt b)
        {
            int dx = Math.Max(0, Math.Max(a.xMin - b.xMax + 1, b.xMin - a.xMax + 1));
            int dy = Math.Max(0, Math.Max(a.yMin - b.yMax + 1, b.yMin - a.yMax + 1));
            return dx * dx + dy * dy;
        }
        static float StrongGap(Raster raster, Frame[] frames, out string frameA, out string frameB)
        {
            var boundaries = frames.Select(f => f.strong.pixels.Where(pixel =>
            {
                int x = pixel % raster.width, y = pixel / raster.width;
                return x == 0 || y == 0 || x == raster.width - 1 || y == raster.height - 1
                    || raster.pixels[pixel - 1].a <= 128 || raster.pixels[pixel + 1].a <= 128
                    || raster.pixels[pixel - raster.width].a <= 128 || raster.pixels[pixel + raster.width].a <= 128;
            }).ToArray()).ToArray();
            var pairs = new List<Tuple<int, int, int>>();
            for (int a = 0; a < frames.Length; a++)
            for (int b = a + 1; b < frames.Length; b++)
                pairs.Add(Tuple.Create(a, b, RectGapSquared(frames[a].strong.bounds, frames[b].strong.bounds)));
            int best = int.MaxValue; frameA = ""; frameB = "";
            foreach (var pair in pairs.OrderBy(p => p.Item3))
            {
                if (pair.Item3 >= best) break;
                foreach (int a in boundaries[pair.Item1])
                foreach (int b in boundaries[pair.Item2])
                {
                    int dx = a % raster.width - b % raster.width;
                    if (dx * dx >= best) continue;
                    int dy = a / raster.width - b / raster.width, distance = dx * dx + dy * dy;
                    if (distance >= best) continue;
                    best = distance; frameA = frames[pair.Item1].sprite.name; frameB = frames[pair.Item2].sprite.name;
                }
            }
            if (best == int.MaxValue) throw new InvalidOperationException("Cannot measure strong-figure separation.");
            return Mathf.Sqrt(best);
        }

        static int FindSize(Frame[] frames, int sourceWidth, int sourceHeight)
        {
            int minimum = Math.Max(Columns, Rows);
            foreach (Frame frame in frames)
                minimum = Math.Max(minimum, Math.Max((frame.supportBbox.width + 1) * Columns + 1, (frame.supportBbox.height + 1) * Rows + 1));
            for (int size = minimum; size <= MaximumSize; size++)
            {
                bool fits = true;
                foreach (Frame frame in frames)
                {
                    SetPreferredPlacement(frame, size, sourceWidth, sourceHeight);
                    if (!FitsWithGutter(Translate(frame.supportBbox, frame.offsetX, frame.offsetY), frame.outputCell)) { fits = false; break; }
                }
                if (fits) return size;
            }
            // A larger native drawing can fit its original slot but not its old
            // within-slot center. Use the available maximum canvas to retain room,
            // then choose the nearest legal integer offset on each independent axis.
            // Clamping does not move any frame to another slot or change its pixels.
            foreach (Frame frame in frames)
            {
                SetPreferredPlacement(frame, MaximumSize, sourceWidth, sourceHeight);
                int minX = frame.outputCell.xMin + 1 - frame.supportBbox.xMin;
                int maxX = frame.outputCell.xMax - 1 - frame.supportBbox.xMax;
                int minY = frame.outputCell.yMin + 1 - frame.supportBbox.yMin;
                int maxY = frame.outputCell.yMax - 1 - frame.supportBbox.yMax;
                if (minX > maxX || minY > maxY)
                    throw new InvalidOperationException("Native pixel support cannot fit its original cell at 4096: " + frame.sprite.name
                        + ", support=" + frame.supportBbox + ", cell=" + frame.outputCell
                        + ". A one-pixel gutter is required; no resizing or pixel removal is permitted.");
                frame.offsetX = Mathf.Clamp(frame.preferredOffsetX, minX, maxX);
                frame.offsetY = Mathf.Clamp(frame.preferredOffsetY, minY, maxY);
                if (!FitsWithGutter(Translate(frame.supportBbox, frame.offsetX, frame.offsetY), frame.outputCell))
                    throw new InvalidOperationException("Cell-constrained integer placement failed: " + frame.sprite.name);
            }
            return MaximumSize;
        }
        static void SetPreferredPlacement(Frame frame, int size, int sourceWidth, int sourceHeight)
        {
            frame.outputCell = Cell(size, size, frame.column, frame.row, Columns, Rows);
            frame.preferredOffsetX = Mathf.RoundToInt(frame.sourceBbox.center.x * size / sourceWidth - frame.inputBbox.center.x);
            frame.preferredOffsetY = Mathf.RoundToInt(frame.sourceBbox.center.y * size / sourceHeight - frame.inputBbox.center.y);
            frame.offsetX = frame.preferredOffsetX;
            frame.offsetY = frame.preferredOffsetY;
        }

        static RectInt Cell(int width, int height, int column, int row, int columns, int rows)
        {
            int x0 = column * width / columns, x1 = (column + 1) * width / columns;
            int y0 = height - (row + 1) * height / rows, y1 = height - row * height / rows;
            return new RectInt(x0, y0, x1 - x0, y1 - y0);
        }
        static RectInt Translate(RectInt rect, int x, int y) { return new RectInt(rect.x + x, rect.y + y, rect.width, rect.height); }
        static bool FitsWithGutter(RectInt rect, RectInt cell)
        {
            return rect.xMin > cell.xMin && rect.yMin > cell.yMin && rect.xMax < cell.xMax && rect.yMax < cell.yMax;
        }
        static RectInt AlphaBounds(Raster raster, RectInt cell, int threshold, string name)
        {
            int xmin = cell.xMax, ymin = cell.yMax, xmax = -1, ymax = -1;
            for (int y = cell.yMin; y < cell.yMax; y++)
            for (int x = cell.xMin; x < cell.xMax; x++)
            {
                if (raster.pixels[y * raster.width + x].a <= threshold) continue;
                xmin = Math.Min(xmin, x); ymin = Math.Min(ymin, y);
                xmax = Math.Max(xmax, x); ymax = Math.Max(ymax, y);
            }
            if (xmax < xmin) throw new InvalidOperationException("Empty alpha>" + threshold + " frame: " + name + " in " + raster.path);
            return new RectInt(xmin, ymin, xmax - xmin + 1, ymax - ymin + 1);
        }
        static Raster ReadPng(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
            if (bytes.Length < 33 || !bytes.Take(8).SequenceEqual(signature)
                || bytes[12] != 73 || bytes[13] != 72 || bytes[14] != 68 || bytes[15] != 82
                || bytes[24] != 8 || bytes[25] != 6)
                throw new InvalidOperationException("Lossless packing requires an 8-bit RGBA PNG: " + path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!ImageConversion.LoadImage(texture, bytes, false)) throw new InvalidOperationException("Cannot decode PNG: " + path);
                return new Raster { path = path, hash = Hash(bytes), width = texture.width, height = texture.height, pixels = texture.GetPixels32() };
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
        static byte[] EncodePng(Color32[] pixels, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                byte[] bytes = ImageConversion.EncodeToPNG(texture);
                if (bytes == null || bytes.Length == 0) throw new InvalidOperationException("PNG encoding failed.");
                // Verify the actual encoded result, not only the pre-encoding copy buffer.
                var verify = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                try
                {
                    if (!ImageConversion.LoadImage(verify, bytes, false) || verify.width != size || verify.height != size)
                        throw new InvalidOperationException("Encoded PNG dimensions changed.");
                    Color32[] decoded = verify.GetPixels32();
                    for (int i = 0; i < pixels.Length; i++)
                        if (decoded[i].r != pixels[i].r || decoded[i].g != pixels[i].g || decoded[i].b != pixels[i].b || decoded[i].a != pixels[i].a)
                            throw new InvalidOperationException("PNG round-trip changed RGBA bytes.");
                }
                finally { UnityEngine.Object.DestroyImmediate(verify); }
                return bytes;
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
        static string Resolve(string project, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A path is required.");
            return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(project, path));
        }
        static bool Within(string path, string root)
        {
            string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
        static void RequirePng(string path)
        {
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                throw new InvalidOperationException("Missing PNG input: " + path);
        }
        static void RequireNewOutput(string path, string extension, string project, params string[] roots)
        {
            if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) || !roots.Any(root => Within(path, Path.Combine(project, root))))
                throw new InvalidOperationException("Output must use " + extension + " inside: " + string.Join(", ", roots));
            if (File.Exists(path) || Directory.Exists(path) || File.Exists(path + ".meta"))
                throw new InvalidOperationException("Output or its metadata already exists; choose a new candidate path: " + path);
        }
        static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        static void WritePair(string output, byte[] png, string report, byte[] json)
        {
            bool createdOutput = false, createdReport = false;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                Directory.CreateDirectory(Path.GetDirectoryName(report));
                using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { createdOutput = true; stream.Write(png, 0, png.Length); }
                using (var stream = new FileStream(report, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { createdReport = true; stream.Write(json, 0, json.Length); }
            }
            catch
            {
                if (createdReport) File.Delete(report);
                if (createdOutput) File.Delete(output);
                throw;
            }
        }
    }
}
