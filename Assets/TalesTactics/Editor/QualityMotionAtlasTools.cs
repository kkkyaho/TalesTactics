using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TalesTactics.Editor
{
    // Explicit creation of NEW reference/candidate files only. Never imports, edits metadata,
    // assigns clips or approves art. Existing SdCharacterArtImporter owns production changes.
    public static class QualityMotionAtlasTools
    {
        const string ReferenceRoot = "Logs/QualityUpgrade20261031/References";
        const string CandidateRoot = "Assets/TalesTactics/Art/SdCharacters/QualityUpgrade20261031";
        const string DocumentRoot = "Docs/SdCharacters/QualityUpgrade20261031";
        const string ProductionManifest = "Docs/SdCharacters/production-manifest.json";
        const int Columns = 4, ReferenceGutter = 12, PackGutter = 2, MaximumSize = 4096;

        [Serializable] public sealed class Box
        {
            public int x, y, width, height;
            public Box() { }
            public Box(RectInt r) { x = r.x; y = r.y; width = r.width; height = r.height; }
            public RectInt Rect => new RectInt(x, y, width, height);
        }
        [Serializable] public sealed class Pair
        {
            public float x, y;
            public Pair() { }
            public Pair(float x, float y) { this.x = x; this.y = y; }
        }
        // Offline exporters may write integral pixel coordinates as 45.0. Deserialize
        // them explicitly as floats, then reject non-integral rectangles before use.
        [Serializable] public sealed class SourceBox
        {
            public float x, y, width, height;
            public SourceBox() { }
            public SourceBox(RectInt r) { x = r.x; y = r.y; width = r.width; height = r.height; }
            public RectInt Rect => IntegralRect(new Rect(x, y, width, height));
        }
        [Serializable] public sealed class ReferenceFrame
        {
            public int index, sheetIndex, cellIndex;
            public string name, originalSpriteId, originalInternalId;
            public SourceBox sourceRect;
            public Pair sourcePivot;
        }
        [Serializable] public sealed class ReferenceSheet
        {
            public string path, sha256;
            public int width, height, columns, rows, cellWidth, cellHeight;
            public int[] frameIndices;
        }
        [Serializable] public sealed class ReferenceManifest
        {
            public int schemaVersion = 1, sourceWidth, sourceHeight, maxFramesPerSheet, columns = Columns;
            public string source, sourceGuid, sourceSha256, currentSourceSha256, sourceMetaSha256;
            public float sourcePixelsPerUnit;
            public string coordinates = "Rectangles use bottom-left pixels. Cell order is left-to-right, then top-to-bottom. Source pivots are normalized.";
            public string scope = "Only currently imported SpriteRects. Omitted/unused drawings are never reconnected. Reference PNGs are exact source-rect crops translated by integer pixels, without resizing or pixel changes.";
            public ReferenceFrame[] frames;
            public ReferenceSheet[] sheets;
        }
        [Serializable] public sealed class CellScan
        {
            public Box cell, support, strongBody;
            public int nontransparentPixels, strongPixels, largestStrongComponent, detachedStrongPixels;
            public float bodyCenterX, lowerBodyCenterX;
        }
        [Serializable] public sealed class PackedPlacement
        {
            public int index, x, y, width, height;
        }
        [Serializable] public sealed class PackedLayout
        {
            public int width, height;
            public PackedPlacement[] frames;
        }
        [Serializable] public sealed class FrameEvidence
        {
            public string name, inputPath, nativeCropSha256;
            public Box inputCell, inputSupport, inputStrongBody;
            public int nontransparentPixels, translationX, translationY;
            public float sourceBodyHeight, candidateBodyHeight, heightRatio, sourceBodyCenterX, candidateBodyCenterX;
            public float pivotX, pivotY, lowerBodyCenterDeltaPixels;
        }
        [Serializable] public sealed class GeneratedInput
        {
            public string path, sha256, nontransparentRgbaHistogramSha256;
            public int width, height, frameCount, weakPixels, weakPixelsOutsideTheirAssignedCell, weakDistanceTies;
            public int[] columnCuts, rowCutsFromTop, rowColumnCuts, componentSeeds;
            public bool explicitLayout;
        }
        [Serializable] public sealed class ReviewedLayout
        {
            public string path, sha256;
            public int width, height;
            public string[] frameNames;
            public int[] columnCuts, rowCutsFromTop, rowColumnCuts, componentSeeds;
        }
        [Serializable] public sealed class ReviewedLayouts
        {
            public int schemaVersion = 1;
            public string scope = "Explicitly measured separation corridors only; not visual approval or production approval.";
            public ReviewedLayout[] sheets;
        }
        public sealed class AdaptiveSheetAnalysis
        {
            public int[] columnCuts, rowCutsFromTop, rowColumnCuts, componentSeeds;
            public CellScan[] frames;
            public int[][] ownedPixelIndices;
            public int weakPixels, weakPixelsOutsideTheirAssignedCell, weakDistanceTies;
            public string nontransparentRgbaHistogramSha256;
        }
        [Serializable] public sealed class RevisionMeasurement
        {
            // Names match SdCharacterArtImporter.Measurement. Extra fields document the revision.
            public int schemaVersion = 1, width, height, frameCount;
            public string source, candidate, sourceGuid, sourceSha256, candidateSha256;
            public float pixelsPerUnit, alphaZeroFraction, solidFraction;
            public bool visualApproved = false;
            public SdCharacterArtImporter.Frame[] frames;
            public string currentSourceSha256, sourceMetaSha256, referenceManifest, referenceManifestSha256;
            public string reviewedLayouts, reviewedLayoutsSha256;
            public bool nativeRgbaPreserved;
            public string outlinePolicy = "Full support rectangle, including all detached and weak-alpha pixels. Texture alpha retains transparent gaps; no polygon simplification clips the artwork.";
            public string packingPolicy = "Integer translations only. Smallest area found by deterministic height-sorted shelf search across candidate widths, not a claim of globally optimal bin packing. Two transparent pixels surround each frame; both dimensions <=4096.";
            public string generatedLayoutPolicy = "Automatic dividing corridors lie within 20% of nominal cell pitch. Optional explicit layouts are bound to the exact PNG hash, dimensions and ordered frame names. Horizontal corridors span the full image; vertical corridors span each entire row. rowColumnCuts records five boundaries per row; columnCuts is empty when rows differ. Every corridor is free of alpha>8 with two clear pixels on each side. Actual cuts and the sidecar hash are recorded. Alpha>8 figures keep their cell identity. Every alpha1..8 pixel belongs to the nearest alpha>8 figure pixel by exact squared Euclidean distance, with equal-distance owners resolved to the lower original frame index. No nontransparent RGBA pixel is dropped or changed. Optional reviewed componentSeeds instead identify separate alpha>128 eight-connected bodies (bottom-left X,Y pairs in original frame order). Duplicate/merged seeded bodies, unseeded major figures and opaque canvas-edge contact are rejected. Minor detached opaque pieces go to their nearest body; all alpha1..128 pixels then retain native RGBA with exact nearest opaque ownership. This supports interleaved silhouettes without straight cutting corridors; explicit seed identities are hash-bound and recorded.";
            public string anchorPolicy = "Draft only: PPU uses median native strong-body height ratio for pose-0 frames (all frames if absent), or an explicit override. X preserves the old pivot offset from the largest strong component's pixel-median body center. Y preserves the old pivot offset from the strong body's lower edge. Weapons, crouching and floating poses require human/continuous review; these are not semantic foot detections.";
            public string productionPolicy = "Update the matching approved production job only after visual/continuous review, then call ApplyApprovedRevision with currentSourceSha256. Never rerun initial inventory/application.";
            public float derivedPixelsPerUnit, pixelsPerUnitOverride;
            public GeneratedInput[] inputs;
            public FrameEvidence[] evidence;
        }
        sealed class Raster
        {
            public string path, hash;
            public int width, height;
            public Color32[] pixels;
        }
        sealed class Snapshot
        {
            public Raster raster;
            public string metaHash, guid;
            public float ppu;
            public ReferenceFrame[] frames;
        }
        sealed class Payload { public string path; public byte[] bytes; }
        sealed class DraftGeometry { public int width, height, left, bottom; public float pivotX, pivotY; }

        public static string Prepare(string source, string newOutputDirectory, int maxFramesPerSheet = 16)
        {
            if (maxFramesPerSheet != 16 && maxFramesPerSheet != 20)
                throw new ArgumentException("Choose 16 or 20 reference frames per sheet.");
            string project = Project(), input = Resolve(project, source), output = Resolve(project, newOutputDirectory);
            RequireInside(output, project, ReferenceRoot);
            if (Directory.Exists(output) || File.Exists(output)) throw new InvalidOperationException("Reference directory must be new: " + output);
            Snapshot original = ReadSource(project, input);
            var job = ProductionJob(project, Relative(project, input));
            RequireIdentity(job, original);
            var manifest = new ReferenceManifest
            {
                source = Relative(project, input), sourceGuid = original.guid, sourceSha256 = job.sourceSha256,
                currentSourceSha256 = original.raster.hash, sourceMetaSha256 = original.metaHash,
                sourceWidth = original.raster.width, sourceHeight = original.raster.height,
                sourcePixelsPerUnit = original.ppu, maxFramesPerSheet = maxFramesPerSheet, frames = original.frames
            };
            var sheets = new List<ReferenceSheet>(); var payloads = new List<Payload>();
            for (int start = 0; start < original.frames.Length; start += maxFramesPerSheet)
            {
                var chunk = original.frames.Skip(start).Take(maxFramesPerSheet).ToArray();
                int cw = chunk.Max(f => f.sourceRect.Rect.width) + ReferenceGutter * 2;
                int ch = chunk.Max(f => f.sourceRect.Rect.height) + ReferenceGutter * 2;
                // Keep the final reference's cell density consistent with the earlier sheets.
                // Remaining cells are transparent; they never create additional frame identities.
                int rows = maxFramesPerSheet / Columns, width = cw * Columns, height = ch * rows;
                if (width > MaximumSize || height > MaximumSize) throw new InvalidOperationException("Reference native crops exceed 4096; choose smaller batches.");
                var pixels = new Color32[checked(width * height)];
                for (int i = 0; i < chunk.Length; i++)
                {
                    var frame = chunk[i]; frame.sheetIndex = sheets.Count; frame.cellIndex = i;
                    RectInt cell = Cell(width, height, Columns, rows, i);
                    int dx = cell.x + (cell.width - frame.sourceRect.Rect.width) / 2;
                    int dy = cell.y + (cell.height - frame.sourceRect.Rect.height) / 2;
                    CopyRect(original.raster.pixels, original.raster.width, frame.sourceRect.Rect, pixels, width, dx, dy);
                }
                byte[] png = Encode(pixels, width, height);
                string path = Path.Combine(output, "reference-" + sheets.Count.ToString("00") + ".png");
                payloads.Add(new Payload { path = path, bytes = png });
                sheets.Add(new ReferenceSheet { path = Relative(project, path), sha256 = Hash(png), width = width, height = height,
                    columns = Columns, rows = rows, cellWidth = cw, cellHeight = ch, frameIndices = chunk.Select(f => f.index).ToArray() });
            }
            manifest.sheets = sheets.ToArray();
            string manifestPath = Path.Combine(output, "reference-manifest.json");
            payloads.Add(new Payload { path = manifestPath, bytes = Json(manifest) });
            AssertUnchanged(original);
            WriteNew(payloads);
            Debug.Log("Prepared " + manifest.frames.Length + " exact source crops in " + sheets.Count + " reference sheets. Production unchanged: " + manifestPath);
            return Relative(project, manifestPath);
        }

        public static string Pack(string manifestPath, string[] generatedPaths, string candidatePath, string measurementPath, float pixelsPerUnitOverride = 0, string reviewedLayoutsPath = null)
        {
            string project = Project(), referencePath = Resolve(project, manifestPath);
            RequireInside(referencePath, project, ReferenceRoot);
            byte[] referenceBytes = File.ReadAllBytes(referencePath);
            var reference = JsonUtility.FromJson<ReferenceManifest>(Encoding.UTF8.GetString(referenceBytes).TrimStart('\uFEFF'));
            ValidateReference(reference);
            if (generatedPaths == null || generatedPaths.Length != reference.sheets.Length)
                throw new ArgumentException("One generated sheet is required for each reference sheet, in the same order.");
            if (!Finite(pixelsPerUnitOverride) || pixelsPerUnitOverride < 0) throw new ArgumentException("Invalid PPU override.");
            string sourcePath = Resolve(project, reference.source), candidate = Resolve(project, candidatePath), measurement = Resolve(project, measurementPath);
            RequireNewOutput(candidate, ".png", project, CandidateRoot);
            RequireNewOutput(measurement, ".json", project, DocumentRoot);
            Snapshot original = ReadSource(project, sourcePath);
            var job = ProductionJob(project, reference.source); RequireIdentity(job, original);
            if (reference.currentSourceSha256 != original.raster.hash || reference.sourceMetaSha256 != original.metaHash
                || reference.sourceGuid != original.guid || reference.sourceSha256 != job.sourceSha256
                || reference.sourceWidth != original.raster.width || reference.sourceHeight != original.raster.height
                || Math.Abs(reference.sourcePixelsPerUnit - original.ppu) > .0001f)
                throw new InvalidOperationException("Source or original provenance changed after reference preparation.");
            CompareFrames(reference.frames, original.frames);
            var generated = generatedPaths.Select(p => ReadPng(Resolve(project, p))).ToArray();
            if (generated.Select(r => r.path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != generated.Length
                || generated.Any(r => r.path == sourcePath || r.path == candidate || r.path == measurement))
                throw new InvalidOperationException("Inputs and outputs must be distinct.");
            string layoutPath = string.IsNullOrEmpty(reviewedLayoutsPath) ? null : Resolve(project, reviewedLayoutsPath);
            byte[] layoutBytes = layoutPath == null ? null : File.ReadAllBytes(layoutPath);
            var explicitLayouts = new Dictionary<string, ReviewedLayout>(StringComparer.OrdinalIgnoreCase);
            if (layoutBytes != null)
            {
                var document = JsonUtility.FromJson<ReviewedLayouts>(Encoding.UTF8.GetString(layoutBytes).TrimStart('\uFEFF'));
                if (document == null || document.schemaVersion != 1 || document.sheets == null || document.sheets.Length == 0)
                    throw new InvalidOperationException("Invalid reviewed-layout sidecar.");
                foreach (var entry in document.sheets)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.path) || string.IsNullOrEmpty(entry.sha256))
                        throw new InvalidOperationException("Reviewed layout is missing its exact source identity.");
                    string inputPath = Resolve(project, entry.path);
                    if (!generated.Any(r => string.Equals(r.path, inputPath, StringComparison.OrdinalIgnoreCase)) || explicitLayouts.ContainsKey(inputPath))
                        throw new InvalidOperationException("Reviewed layout names a duplicate or unused input.");
                    explicitLayouts.Add(inputPath, entry);
                }
            }
            var scans = new CellScan[reference.frames.Length]; var owners = new Raster[scans.Length];
            var nativePixels = new Color32[scans.Length][]; var analyses = new AdaptiveSheetAnalysis[generated.Length];
            for (int s = 0; s < reference.sheets.Length; s++)
            {
                var sheet = reference.sheets[s]; string path = Resolve(project, sheet.path);
                RequireInside(path, project, ReferenceRoot);
                if (Hash(File.ReadAllBytes(path)) != sheet.sha256) throw new InvalidOperationException("Reference PNG changed: " + path);
                AdaptiveSheetAnalysis analysis;
                if (explicitLayouts.TryGetValue(generated[s].path, out var reviewed))
                {
                    bool components = reviewed.componentSeeds != null && reviewed.componentSeeds.Length > 0;
                    if (reviewed.sha256 != generated[s].hash || reviewed.width != generated[s].width || reviewed.height != generated[s].height
                        || reviewed.frameNames == null || !reviewed.frameNames.SequenceEqual(sheet.frameIndices.Select(i => reference.frames[i].name)))
                        throw new InvalidOperationException("Reviewed layout PNG/dimensions/frame order differs from its reference.");
                    if (components)
                        analysis = AnalyzeComponentSeparatedSheet(generated[s].pixels, generated[s].width, generated[s].height, reviewed.componentSeeds, sheet.frameIndices.Length);
                    else
                    {
                        if (reviewed.rowCutsFromTop == null || reviewed.rowCutsFromTop.Length != sheet.rows + 1
                            || (reviewed.rowColumnCuts != null && reviewed.rowColumnCuts.Length > 0
                                ? reviewed.rowColumnCuts.Length != (sheet.columns + 1) * sheet.rows
                                : reviewed.columnCuts == null || reviewed.columnCuts.Length != sheet.columns + 1))
                            throw new InvalidOperationException("Reviewed grid dimensions differ from the reference.");
                        analysis = reviewed.rowColumnCuts != null && reviewed.rowColumnCuts.Length > 0
                            ? AnalyzeRowSeparatedSheet(generated[s].pixels, generated[s].width, generated[s].height, reviewed.rowColumnCuts, reviewed.rowCutsFromTop, sheet.frameIndices.Length)
                            : AnalyzeReviewedSheet(generated[s].pixels, generated[s].width, generated[s].height, reviewed.columnCuts, reviewed.rowCutsFromTop, sheet.frameIndices.Length);
                    }
                }
                else analysis = AnalyzeAdaptiveSheet(generated[s].pixels, generated[s].width, generated[s].height, sheet.columns, sheet.rows, sheet.frameIndices.Length);
                analyses[s] = analysis;
                for (int i = 0; i < analysis.frames.Length; i++)
                {
                    int index = sheet.frameIndices[i]; scans[index] = analysis.frames[i]; owners[index] = generated[s];
                    nativePixels[index] = OwnedCrop(generated[s].pixels, generated[s].width, scans[index].support.Rect, analysis.ownedPixelIndices[i]);
                }
            }
            var sourceScans = reference.frames.Select(f => Scan(original.raster.pixels, original.raster.width, f.sourceRect.Rect, false)).ToArray();
            int[] standing = Enumerable.Range(0, scans.Length).Where(i => Regex.IsMatch(reference.frames[i].name, @"_0_\d+$")).ToArray();
            if (standing.Length == 0) standing = Enumerable.Range(0, scans.Length).ToArray();
            float derivedPpu = original.ppu * Median(standing.Select(i => (float)scans[i].strongBody.height / sourceScans[i].strongBody.height));
            float ppu = pixelsPerUnitOverride > 0 ? pixelsPerUnitOverride : derivedPpu;
            if (!Finite(ppu) || ppu <= 0) throw new InvalidOperationException("Cannot derive a finite positive PPU.");
            // Root pivots may lie just below the visible feet. Add transparent canvas around
            // the native support instead of clamping the pivot or moving/resizing the drawing.
            var geometry = new DraftGeometry[scans.Length];
            for (int i = 0; i < scans.Length; i++)
            {
                var s = scans[i]; var old = sourceScans[i]; var f = reference.frames[i]; float scale = ppu / original.ppu;
                float oldX = f.sourceRect.x + f.sourcePivot.x * f.sourceRect.width;
                float oldY = f.sourceRect.y + f.sourcePivot.y * f.sourceRect.height;
                float anchorX = s.bodyCenterX + (oldX - old.bodyCenterX) * scale - s.support.x;
                float anchorY = s.strongBody.y + (oldY - old.strongBody.y) * scale - s.support.y;
                if (!Finite(anchorX) || !Finite(anchorY)) throw new InvalidOperationException("Non-finite draft anchor: " + f.name);
                int left = Math.Max(PackGutter, Mathf.CeilToInt(-anchorX) + PackGutter);
                int bottom = Math.Max(PackGutter, Mathf.CeilToInt(-anchorY) + PackGutter);
                int right = Math.Max(PackGutter, Mathf.CeilToInt(anchorX - s.support.width) + PackGutter);
                int top = Math.Max(PackGutter, Mathf.CeilToInt(anchorY - s.support.height) + PackGutter);
                if (left > Math.Max(32, s.support.width / 4) || right > Math.Max(32, s.support.width / 4)
                    || bottom > Math.Max(32, s.support.height / 4) || top > Math.Max(32, s.support.height / 4))
                    throw new InvalidOperationException("Draft anchor needs excessive transparent padding: " + f.name + ". Review pose/PPU.");
                int w = s.support.width + left + right, h = s.support.height + bottom + top;
                geometry[i] = new DraftGeometry { width = w, height = h, left = left, bottom = bottom, pivotX = (anchorX + left) / w, pivotY = (anchorY + bottom) / h };
            }
            var layout = FindLayout(geometry.Select(g => g.width).ToArray(), geometry.Select(g => g.height).ToArray());
            var packed = new Color32[checked(layout.width * layout.height)];
            var frames = new SdCharacterArtImporter.Frame[scans.Length]; var evidence = new FrameEvidence[scans.Length];
            for (int i = 0; i < scans.Length; i++)
            {
                var s = scans[i]; var old = sourceScans[i]; var f = reference.frames[i]; var place = layout.frames[i]; var g = geometry[i];
                int nativeX = place.x + g.left, nativeY = place.y + g.bottom;
                CopyRect(nativePixels[i], s.support.width, new RectInt(0, 0, s.support.width, s.support.height), packed, layout.width, nativeX, nativeY);
                string nativeHash = CropHash(nativePixels[i], s.support.width, new RectInt(0, 0, s.support.width, s.support.height));
                if (nativeHash != CropHash(packed, layout.width, new RectInt(nativeX, nativeY, s.support.width, s.support.height)))
                    throw new InvalidOperationException("Packed native RGBA differs: " + f.name);
                float px = g.pivotX, py = g.pivotY;
                float hw = place.width * .5f, hh = place.height * .5f;
                frames[i] = new SdCharacterArtImporter.Frame { name = f.name, originalSpriteId = f.originalSpriteId, originalInternalId = f.originalInternalId,
                    x = place.x, y = place.y, width = place.width, height = place.height, pivotX = px, pivotY = py,
                    outlines = new[] { new SdCharacterArtImporter.Polygon { points = new[] { P(-hw, -hh), P(hw, -hh), P(hw, hh), P(-hw, hh) } } } };
                evidence[i] = new FrameEvidence { name = f.name, inputPath = owners[i].path, nativeCropSha256 = nativeHash, inputCell = s.cell,
                    inputSupport = s.support, inputStrongBody = s.strongBody, nontransparentPixels = s.nontransparentPixels,
                    translationX = nativeX - s.support.x, translationY = nativeY - s.support.y, sourceBodyHeight = old.strongBody.height,
                    candidateBodyHeight = s.strongBody.height, heightRatio = (float)s.strongBody.height / old.strongBody.height,
                    sourceBodyCenterX = old.bodyCenterX, candidateBodyCenterX = s.bodyCenterX, pivotX = px, pivotY = py,
                    lowerBodyCenterDeltaPixels = s.lowerBodyCenterX - (s.support.x + px * place.width - g.left) };
            }
            int nontransparent = packed.Count(c => c.a != 0), strong = packed.Count(c => c.a > 128);
            if (nontransparent != scans.Sum(s => s.nontransparentPixels)) throw new InvalidOperationException("Native pixel count changed.");
            if (HistogramHash(generated.SelectMany(r => r.pixels)) != HistogramHash(packed)) throw new InvalidOperationException("Native RGBA histogram changed.");
            float zeroFraction = 1f - (float)nontransparent / packed.Length, solidFraction = (float)strong / packed.Length;
            if (zeroFraction < .15f || solidFraction < .02f || solidFraction > .75f)
                throw new InvalidOperationException("Packed alpha distribution is incompatible with the production importer.");
            byte[] png = Encode(packed, layout.width, layout.height);
            var result = new RevisionMeasurement { source = reference.source, candidate = Relative(project, candidate), sourceGuid = reference.sourceGuid,
                sourceSha256 = reference.sourceSha256, currentSourceSha256 = original.raster.hash, sourceMetaSha256 = original.metaHash,
                candidateSha256 = Hash(png), referenceManifest = Relative(project, referencePath), referenceManifestSha256 = Hash(referenceBytes),
                reviewedLayouts = layoutPath == null ? null : Relative(project, layoutPath), reviewedLayoutsSha256 = layoutBytes == null ? null : Hash(layoutBytes),
                width = layout.width, height = layout.height, frameCount = frames.Length, pixelsPerUnit = ppu, derivedPixelsPerUnit = derivedPpu,
                pixelsPerUnitOverride = pixelsPerUnitOverride, alphaZeroFraction = zeroFraction, solidFraction = solidFraction,
                nativeRgbaPreserved = true, frames = frames, evidence = evidence,
                inputs = generated.Select((r, s) => new GeneratedInput { path = r.path, sha256 = r.hash, width = r.width, height = r.height,
                    explicitLayout = explicitLayouts.ContainsKey(r.path),
                    frameCount = reference.sheets[s].frameIndices.Length, columnCuts = analyses[s].columnCuts, rowCutsFromTop = analyses[s].rowCutsFromTop, rowColumnCuts = analyses[s].rowColumnCuts, componentSeeds = analyses[s].componentSeeds,
                    weakPixels = analyses[s].weakPixels, weakPixelsOutsideTheirAssignedCell = analyses[s].weakPixelsOutsideTheirAssignedCell,
                    weakDistanceTies = analyses[s].weakDistanceTies, nontransparentRgbaHistogramSha256 = analyses[s].nontransparentRgbaHistogramSha256 }).ToArray() };
            AssertUnchanged(original);
            if (Hash(File.ReadAllBytes(referencePath)) != Hash(referenceBytes) || generated.Any(r => Hash(File.ReadAllBytes(r.path)) != r.hash))
                throw new InvalidOperationException("A reference or generated input changed during packing.");
            if (layoutPath != null && Hash(File.ReadAllBytes(layoutPath)) != Hash(layoutBytes))
                throw new InvalidOperationException("Reviewed layout changed during packing.");
            WriteNew(new[] { new Payload { path = candidate, bytes = png }, new Payload { path = measurement, bytes = Json(result) } });
            Debug.Log("Packed " + frames.Length + " native frames; visualApproved=false, PPU=" + ppu + ". Production unchanged.");
            return Relative(project, measurement);
        }

        // Public pure-pixel validation is shared by the preparation pipeline and meaningful EditMode probes.
        public static CellScan[] ValidateGeneratedCells(Color32[] pixels, int width, int height, int columns, int rows, int usedCells)
        {
            if (width <= 0 || height <= 0 || width > MaximumSize || height > MaximumSize || pixels == null || pixels.Length != (long)width * height
                || columns != Columns || rows < 1 || rows > 5 || usedCells < 1 || usedCells > columns * rows)
                throw new ArgumentException("Invalid generated sheet dimensions or cell count.");
            var result = new CellScan[usedCells];
            for (int i = 0; i < columns * rows; i++)
            {
                RectInt cell = Cell(width, height, columns, rows, i);
                if (i < usedCells) result[i] = Scan(pixels, width, cell, true);
                else for (int y = cell.yMin; y < cell.yMax; y++) for (int x = cell.xMin; x < cell.xMax; x++)
                    if (pixels[y * width + x].a != 0) throw new InvalidOperationException("Unused generated cell contains artwork: " + i);
            }
            return result;
        }

        public static AdaptiveSheetAnalysis AnalyzeAdaptiveSheet(Color32[] pixels, int width, int height, int columns, int rows, int usedCells)
        {
            if (width <= 0 || height <= 0 || width > MaximumSize || height > MaximumSize || pixels == null || pixels.Length != (long)width * height
                || columns != Columns || rows < 1 || rows > 5 || usedCells < 1 || usedCells > columns * rows)
                throw new ArgumentException("Invalid adaptive sheet dimensions or cell count.");
            var occupiedColumns = new bool[width]; var occupiedRows = new bool[height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) if (pixels[y * width + x].a > 8)
            { occupiedColumns[x] = true; occupiedRows[height - 1 - y] = true; }
            int[] topCuts = SeparationCuts(occupiedRows, rows);
            int[] xCuts;
            try { xCuts = SeparationCuts(occupiedColumns, columns); }
            catch (InvalidOperationException)
            {
                // Animation silhouettes can be staggered between rows. A clear vertical
                // corridor need span its own row, not unrelated poses elsewhere on the sheet.
                var perRow = new int[rows * (columns + 1)];
                for (int row = 0; row < rows; row++)
                {
                    var occupied = new bool[width];
                    for (int y = height - topCuts[row + 1]; y < height - topCuts[row]; y++)
                        for (int x = 0; x < width; x++) if (pixels[y * width + x].a > 8) occupied[x] = true;
                    Array.Copy(SeparationCuts(occupied, columns), 0, perRow, row * (columns + 1), columns + 1);
                }
                return AnalyzeRowSeparatedSheet(pixels, width, height, perRow, topCuts, usedCells);
            }
            return AnalyzeReviewedSheet(pixels, width, height, xCuts, topCuts, usedCells);
        }

        // Explicit cuts change only where cells are assigned, never the pixel data or validation strength.
        public static AdaptiveSheetAnalysis AnalyzeReviewedSheet(Color32[] pixels, int width, int height, int[] columnCuts, int[] rowCutsFromTop, int usedCells)
        {
            if (columnCuts == null || columnCuts.Length != Columns + 1 || rowCutsFromTop == null || rowCutsFromTop.Length < 2 || rowCutsFromTop.Length > 6)
                throw new ArgumentException("Invalid explicit sheet cuts.");
            return AnalyzeRowSeparatedSheet(pixels, width, height,
                Enumerable.Range(0, rowCutsFromTop.Length - 1).SelectMany(_ => columnCuts).ToArray(), rowCutsFromTop, usedCells);
        }

        // Row-major flattened cuts: five integer X boundaries per row. No alpha, scale,
        // sprite order or edge safety rule changes; every rectangle partitions one full row.
        public static AdaptiveSheetAnalysis AnalyzeRowSeparatedSheet(Color32[] pixels, int width, int height, int[] rowColumnCuts, int[] rowCutsFromTop, int usedCells)
        {
            if (width <= 0 || height <= 0 || width > MaximumSize || height > MaximumSize || pixels == null || pixels.Length != (long)width * height
                || rowCutsFromTop == null || rowCutsFromTop.Length < 2 || rowCutsFromTop.Length > 6
                || rowColumnCuts == null || rowColumnCuts.Length != (Columns + 1) * (rowCutsFromTop.Length - 1)
                || usedCells < 1 || usedCells > Columns * (rowCutsFromTop.Length - 1))
                throw new ArgumentException("Invalid row-separated sheet dimensions or cell count.");
            int[] topCuts = (int[])rowCutsFromTop.Clone(), perRow = (int[])rowColumnCuts.Clone();
            ValidateCuts(topCuts, height);
            int columns = Columns, rows = topCuts.Length - 1;
            var occupiedRows = new bool[height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                if (pixels[y * width + x].a > 8) occupiedRows[height - 1 - y] = true;
            ValidateClearCuts(topCuts, occupiedRows);
            for (int row = 0; row < rows; row++)
            {
                int[] cuts = perRow.Skip(row * (columns + 1)).Take(columns + 1).ToArray();
                ValidateCuts(cuts, width);
                var occupied = new bool[width];
                for (int y = height - topCuts[row + 1]; y < height - topCuts[row]; y++)
                    for (int x = 0; x < width; x++) if (pixels[y * width + x].a > 8) occupied[x] = true;
                ValidateClearCuts(cuts, occupied);
            }
            int[] xCuts = perRow.Take(columns + 1).ToArray();
            if (Enumerable.Range(1, rows - 1).Any(row => !xCuts.SequenceEqual(perRow.Skip(row * (columns + 1)).Take(columns + 1)))) xCuts = new int[0];
            var cells = new RectInt[columns * rows]; var frames = new CellScan[usedCells];
            var owner = Enumerable.Repeat(-1, pixels.Length).ToArray(); var owned = new List<int>[usedCells];
            for (int i = 0; i < usedCells; i++) owned[i] = new List<int>();
            for (int i = 0; i < cells.Length; i++)
            {
                int col = i % columns, row = i / columns;
                int start = row * (columns + 1) + col;
                cells[i] = new RectInt(perRow[start], height - topCuts[row + 1], perRow[start + 1] - perRow[start], topCuts[row + 1] - topCuts[row]);
                if (i < usedCells) frames[i] = Scan(pixels, width, cells[i], true, 8);
                for (int y = cells[i].yMin; y < cells[i].yMax; y++) for (int x = cells[i].xMin; x < cells[i].xMax; x++)
                {
                    int point = y * width + x;
                    if (pixels[point].a <= 8) continue;
                    if (i >= usedCells) throw new InvalidOperationException("Unused adaptive cell contains alpha>8 artwork: " + i);
                    owner[point] = i; owned[i].Add(point);
                }
            }
            var nearest = new PixelLookup(width, height, owner);
            int weak = 0, crossings = 0, ties = 0;
            for (int point = 0; point < pixels.Length; point++) if (pixels[point].a > 0 && pixels[point].a <= 8)
            {
                int x = point % width, y = point / width;
                int assigned = nearest.NearestOwner(x, y, out bool tied);
                if (assigned < 0) throw new InvalidOperationException("A weak-alpha pixel has no figure owner.");
                owner[point] = assigned; owned[assigned].Add(point); weak++;
                if (!cells[assigned].Contains(new Vector2Int(x, y))) crossings++;
                if (tied) ties++;
            }
            for (int i = 0; i < usedCells; i++)
            {
                var points = owned[i];
                int l = points.Min(p => p % width), r = points.Max(p => p % width) + 1;
                int b = points.Min(p => p / width), t = points.Max(p => p / width) + 1;
                frames[i].support = new Box(new RectInt(l, b, r - l, t - b));
                frames[i].nontransparentPixels = points.Count;
            }
            if (owned.Sum(p => p.Count) != pixels.Count(p => p.a != 0)) throw new InvalidOperationException("Adaptive ownership lost native pixels.");
            return new AdaptiveSheetAnalysis { columnCuts = xCuts, rowCutsFromTop = topCuts, rowColumnCuts = perRow, frames = frames,
                ownedPixelIndices = owned.Select(p => p.ToArray()).ToArray(), weakPixels = weak,
                weakPixelsOutsideTheirAssignedCell = crossings, weakDistanceTies = ties,
                nontransparentRgbaHistogramSha256 = HistogramHash(pixels) };
        }

        // Explicit opaque-body seeds identify individual figures whose bounding boxes overlap.
        // Native pixels are assigned once, never erased, recolored, resized or duplicated.
        // This is opt-in and bound to exact PNG/frame identities by the reviewed sidecar.
        public static AdaptiveSheetAnalysis AnalyzeComponentSeparatedSheet(Color32[] pixels, int width, int height, int[] seeds, int usedCells)
        {
            if (pixels == null || width < 1 || height < 1 || width > MaximumSize || height > MaximumSize
                || pixels.Length != (long)width * height || usedCells < 1 || usedCells > 20
                || seeds == null || seeds.Length != usedCells * 2)
                throw new ArgumentException("Invalid explicit component dimensions or seed count.");
            var labels = Enumerable.Repeat(-1, pixels.Length).ToArray();
            var parts = new List<List<int>>();
            var queue = new int[pixels.Length];
            for (int p = 0; p < pixels.Length; p++)
            {
                if (pixels[p].a <= 128 || labels[p] >= 0) continue;
                var part = new List<int>(); int head = 0, tail = 0, label = parts.Count;
                queue[tail++] = p; labels[p] = label;
                while (head < tail)
                {
                    int point = queue[head++], x = point % width, y = point / width;
                    if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                        throw new InvalidOperationException("Opaque figure reaches canvas edge; cannot certify an unclipped source.");
                    part.Add(point);
                    for (int oy = -1; oy <= 1; oy++) for (int ox = -1; ox <= 1; ox++)
                    {
                        int nx = x + ox, ny = y + oy;
                        if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                        int next = ny * width + nx;
                        if (labels[next] < 0 && pixels[next].a > 128) { labels[next] = label; queue[tail++] = next; }
                    }
                }
                parts.Add(part);
            }
            var componentOwner = Enumerable.Repeat(-1, parts.Count).ToArray();
            var owner = Enumerable.Repeat(-1, pixels.Length).ToArray();
            var owned = Enumerable.Range(0, usedCells).Select(_ => new List<int>()).ToArray();
            for (int i = 0; i < usedCells; i++)
            {
                int x = seeds[i * 2], y = seeds[i * 2 + 1];
                if (x < 0 || y < 0 || x >= width || y >= height || labels[y * width + x] < 0)
                    throw new InvalidOperationException("A reviewed seed is not on an opaque body: " + i);
                int label = labels[y * width + x];
                if (componentOwner[label] >= 0 || parts[label].Count < 256)
                    throw new InvalidOperationException("Seeds share a connected body or identify a small fragment: " + i);
                componentOwner[label] = i;
                foreach (int point in parts[label]) { owner[point] = i; owned[i].Add(point); }
            }
            var seedBodySizes = owned.Select(points => points.Count).ToArray();
            var bodyLookup = new PixelLookup(width, height, owner);
            for (int label = 0; label < parts.Count; label++)
            {
                if (componentOwner[label] >= 0) continue;
                var part = parts[label]; int assigned = -1;
                // A detached piece stays intact only when every opaque pixel agrees
                // on the same unique seeded body. One BFS sample cannot prove this.
                foreach (int point in part)
                {
                    int candidate = bodyLookup.NearestOwner(point % width, point / width, out bool tied);
                    if (candidate < 0 || tied || assigned >= 0 && assigned != candidate)
                        throw new InvalidOperationException("Detached opaque piece has ambiguous nearest seeded bodies.");
                    assigned = candidate;
                }
                // Previously accepted fragments must not enlarge the body-size limit.
                if (assigned < 0 || part.Count >= Math.Max(64, seedBodySizes[assigned] * .12f))
                    throw new InvalidOperationException("Unseeded major opaque figure or detached artwork.");
                componentOwner[label] = assigned;
                foreach (int point in part) { owner[point] = assigned; owned[assigned].Add(point); }
            }
            var nearest = new PixelLookup(width, height, owner);
            int weak = 0, ties = 0;
            for (int p = 0; p < pixels.Length; p++) if (pixels[p].a > 0 && pixels[p].a <= 128)
            {
                int assigned = nearest.NearestOwner(p % width, p / width, out bool tied);
                if (assigned < 0) throw new InvalidOperationException("Nonopaque pixel has no native figure owner.");
                owner[p] = assigned; owned[assigned].Add(p); weak++; if (tied) ties++;
            }
            var frames = new CellScan[usedCells];
            for (int i = 0; i < usedCells; i++)
            {
                var points = owned[i];
                int l = points.Min(p => p % width), r = points.Max(p => p % width) + 1;
                int b = points.Min(p => p / width), t = points.Max(p => p / width) + 1;
                var bounds = new RectInt(l, b, r - l, t - b);
                var crop = OwnedCrop(pixels, width, bounds, points.ToArray());
                var frame = Scan(crop, bounds.width, new RectInt(0, 0, bounds.width, bounds.height), false);
                if (frame.largestStrongComponent < 256 || frame.detachedStrongPixels > frame.largestStrongComponent * .25f)
                    throw new InvalidOperationException("Reviewed component contains excessive detached artwork: " + i);
                frame.cell = new Box(bounds); frame.support = new Box(bounds);
                frame.strongBody.x += l; frame.strongBody.y += b;
                frame.bodyCenterX += l; frame.lowerBodyCenterX += l;
                frames[i] = frame;
            }
            if (owned.Sum(p => p.Count) != pixels.Count(p => p.a != 0))
                throw new InvalidOperationException("Component ownership lost or duplicated native pixels.");
            return new AdaptiveSheetAnalysis { frames = frames, ownedPixelIndices = owned.Select(p => p.ToArray()).ToArray(),
                columnCuts = new int[0], rowCutsFromTop = new int[0], rowColumnCuts = new int[0],
                componentSeeds = (int[])seeds.Clone(), weakPixels = weak, weakDistanceTies = ties,
                nontransparentRgbaHistogramSha256 = HistogramHash(pixels) };
        }

        static void ValidateCuts(int[] cuts, int dimension)
        {
            if (cuts[0] != 0 || cuts[cuts.Length - 1] != dimension)
                throw new ArgumentException("Explicit cuts must cover the entire input exactly.");
            for (int i = 1; i < cuts.Length; i++) if (cuts[i] <= cuts[i - 1] || cuts[i] > dimension)
                throw new ArgumentException("Explicit cuts must be strictly increasing pixel coordinates.");
        }
        static void ValidateClearCuts(int[] cuts, bool[] occupied)
        {
            for (int i = 1; i < cuts.Length - 1; i++)
            {
                if (cuts[i] < PackGutter || cuts[i] > occupied.Length - PackGutter)
                    throw new InvalidOperationException("Explicit cut lacks a transparent safety corridor.");
                for (int p = cuts[i] - PackGutter; p < cuts[i] + PackGutter; p++) if (occupied[p])
                    throw new InvalidOperationException("Explicit cut crosses alpha>8 artwork or its two-pixel safety margin.");
            }
        }

        static int[] SeparationCuts(bool[] occupied, int parts)
        {
            int n = occupied.Length; var result = new int[parts + 1]; result[parts] = n;
            for (int boundary = 1; boundary < parts; boundary++)
            {
                float nominal = (float)boundary * n / parts, tolerance = (float)n / parts * .2f;
                int lo = (int)Math.Ceiling(nominal - tolerance), hi = (int)Math.Floor(nominal + tolerance);
                int best = -1; float distance = float.PositiveInfinity;
                for (int x = 0; x < n;)
                {
                    if (occupied[x]) { x++; continue; }
                    int start = x; while (x < n && !occupied[x]) x++; int end = x;
                    int a = Math.Max(lo, start + PackGutter), b = Math.Min(hi, end - PackGutter);
                    if (a > b) continue;
                    int cut = Math.Max(a, Math.Min(b, (int)Math.Round(nominal, MidpointRounding.AwayFromZero)));
                    float d = Math.Abs(cut - nominal);
                    if (d < distance || d == distance && cut < best) { best = cut; distance = d; }
                }
                if (best <= result[boundary - 1] || best >= n)
                    throw new InvalidOperationException("No alpha>8-free separating corridor within 20% of nominal boundary " + boundary + ". Do not clip; regenerate the sheet.");
                result[boundary] = best;
            }
            return result;
        }

        // Exact nearest-seed distance using expanding spatial bins. Stop only after every
        // unvisited bin is farther away than the best distance, so cross-owner ties are seen.
        sealed class PixelLookup
        {
            const int BinSize = 16;
            readonly int width, height, columns, rows;
            readonly int[] owners;
            readonly List<int>[] bins;
            public PixelLookup(int width, int height, int[] owners)
            {
                this.width = width; this.height = height; this.owners = owners;
                columns = (width + BinSize - 1) / BinSize; rows = (height + BinSize - 1) / BinSize;
                bins = new List<int>[columns * rows];
                for (int i = 0; i < owners.Length; i++) if (owners[i] >= 0)
                {
                    int bin = i / width / BinSize * columns + i % width / BinSize;
                    if (bins[bin] == null) bins[bin] = new List<int>();
                    bins[bin].Add(i);
                }
            }
            public int NearestOwner(int x, int y, out bool tied)
            {
                int bx = x / BinSize, by = y / BinSize, best = -1, distance = int.MaxValue; tied = false;
                for (int radius = 0; radius <= Math.Max(columns, rows); radius++)
                {
                    int left = Math.Max(0, bx - radius), right = Math.Min(columns - 1, bx + radius);
                    int bottom = Math.Max(0, by - radius), top = Math.Min(rows - 1, by + radius);
                    for (int yy = bottom; yy <= top; yy++) for (int xx = left; xx <= right; xx++)
                    {
                        // Only the newly visited perimeter. A clipped edge was already scanned.
                        if (Math.Abs(xx - bx) != radius && Math.Abs(yy - by) != radius) continue;
                        var bin = bins[yy * columns + xx]; if (bin == null) continue;
                        foreach (int point in bin)
                        {
                            int dx = point % width - x, dy = point / width - y, d = dx * dx + dy * dy, candidate = owners[point];
                            if (d < distance) { distance = d; best = candidate; tied = false; }
                            else if (d == distance && candidate != best) { tied = true; best = Math.Min(best, candidate); }
                        }
                    }
                    int outside = int.MaxValue;
                    if (left > 0) outside = Math.Min(outside, x - (left * BinSize - 1));
                    if (right < columns - 1) outside = Math.Min(outside, (right + 1) * BinSize - x);
                    if (bottom > 0) outside = Math.Min(outside, y - (bottom * BinSize - 1));
                    if (top < rows - 1) outside = Math.Min(outside, (top + 1) * BinSize - y);
                    if (outside == int.MaxValue || best >= 0 && (long)outside * outside > distance) return best;
                }
                return best;
            }
        }

        static Color32[] OwnedCrop(Color32[] pixels, int sourceWidth, RectInt bounds, int[] indices)
        {
            var result = new Color32[checked(bounds.width * bounds.height)];
            foreach (int point in indices)
                result[(point / sourceWidth - bounds.y) * bounds.width + point % sourceWidth - bounds.x] = pixels[point];
            return result;
        }
        static string HistogramHash(IEnumerable<Color32> pixels)
        {
            var histogram = new Dictionary<uint, int>();
            foreach (var c in pixels)
            {
                if (c.a == 0) continue;
                uint key = (uint)c.r | (uint)c.g << 8 | (uint)c.b << 16 | (uint)c.a << 24;
                histogram.TryGetValue(key, out int count); histogram[key] = count + 1;
            }
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) foreach (var entry in histogram.OrderBy(p => p.Key)) { writer.Write(entry.Key); writer.Write(entry.Value); }
                return Hash(stream.ToArray());
            }
        }

        static CellScan Scan(Color32[] pixels, int width, RectInt cell, bool strict, int marginAlpha = 0)
        {
            var seen = new bool[cell.width * cell.height]; var queue = new int[seen.Length];
            var parts = new List<List<int>>(); int xmin = cell.xMax, ymin = cell.yMax, xmax = -1, ymax = -1, count = 0, strongCount = 0;
            for (int y = cell.yMin; y < cell.yMax; y++) for (int x = cell.xMin; x < cell.xMax; x++)
            {
                byte a = pixels[y * width + x].a;
                if (a == 0) continue;
                count++; xmin = Math.Min(xmin, x); ymin = Math.Min(ymin, y); xmax = Math.Max(xmax, x); ymax = Math.Max(ymax, y);
                if (strict && a > marginAlpha && (x - cell.xMin < PackGutter || cell.xMax - 1 - x < PackGutter || y - cell.yMin < PackGutter || cell.yMax - 1 - y < PackGutter))
                    throw new InvalidOperationException("Artwork touches the cell's transparent safety margin at " + x + "," + y + ". Regenerate; pixels are not trimmed.");
                if (a <= 128) continue;
                strongCount++; int local = (y - cell.yMin) * cell.width + x - cell.xMin;
                if (seen[local]) continue;
                int head = 0, tail = 0; queue[tail++] = local; seen[local] = true; var part = new List<int>();
                while (head < tail)
                {
                    int point = queue[head++], px = point % cell.width, py = point / cell.width; part.Add(point);
                    for (int oy = -1; oy <= 1; oy++) for (int ox = -1; ox <= 1; ox++)
                    {
                        int nx = px + ox, ny = py + oy;
                        if (nx < 0 || ny < 0 || nx >= cell.width || ny >= cell.height) continue;
                        int next = ny * cell.width + nx;
                        if (!seen[next] && pixels[(ny + cell.y) * width + nx + cell.x].a > 128) { seen[next] = true; queue[tail++] = next; }
                    }
                }
                parts.Add(part);
            }
            if (parts.Count == 0 || count == 0) throw new InvalidOperationException("Missing opaque figure in a required cell.");
            parts.Sort((a, b) => b.Count.CompareTo(a.Count)); var body = parts[0];
            int detached = parts.Skip(1).Sum(p => p.Count);
            if (strict && (body.Count < 256 || parts.Skip(1).Any(p => p.Count >= Math.Max(64, body.Count * .12f)) || detached > body.Count * .25f))
                throw new InvalidOperationException("Ambiguous/multiple major figures or detached artwork in a cell; review rather than auto-merge.");
            int bx = body.Min(p => p % cell.width) + cell.x, by = body.Min(p => p / cell.width) + cell.y;
            int br = body.Max(p => p % cell.width) + cell.x + 1, bt = body.Max(p => p / cell.width) + cell.y + 1;
            float bodyX = Median(body.Select(p => (float)(p % cell.width + cell.x) + .5f));
            float feetX = Median(body.Where(p => p / cell.width + cell.y < by + (bt - by) * .18f).Select(p => (float)(p % cell.width + cell.x) + .5f));
            return new CellScan { cell = new Box(cell), support = new Box(new RectInt(xmin, ymin, xmax - xmin + 1, ymax - ymin + 1)),
                strongBody = new Box(new RectInt(bx, by, br - bx, bt - by)), nontransparentPixels = count, strongPixels = strongCount,
                largestStrongComponent = body.Count, detachedStrongPixels = detached, bodyCenterX = bodyX, lowerBodyCenterX = feetX };
        }

        public static PackedLayout FindLayout(int[] widths, int[] heights)
        {
            if (widths == null || heights == null || widths.Length == 0 || widths.Length != heights.Length || widths.Length > 256
                || widths.Concat(heights).Any(v => v <= 0 || v > MaximumSize - PackGutter * 2)) throw new ArgumentException("Invalid native frame sizes.");
            int[] order = Enumerable.Range(0, widths.Length).OrderByDescending(i => heights[i]).ThenByDescending(i => widths[i]).ThenBy(i => i).ToArray();
            int minimum = widths.Max() + PackGutter * 2; PackedLayout best = null;
            for (int candidateWidth = minimum; candidateWidth <= MaximumSize; candidateWidth += 8)
                ConsiderLayout(candidateWidth, widths, heights, order, ref best);
            ConsiderLayout(MaximumSize, widths, heights, order, ref best);
            if (best == null) throw new InvalidOperationException("Native artwork cannot fit within 4096x4096 without resizing.");
            return best;
        }
        static void ConsiderLayout(int width, int[] widths, int[] heights, int[] order, ref PackedLayout best)
        {
            var positions = new PackedPlacement[widths.Length]; int x = 0, y = 0, shelfHeight = 0, occupiedWidth = 0;
            foreach (int i in order)
            {
                int w = widths[i] + PackGutter * 2, h = heights[i] + PackGutter * 2;
                if (x + w > width) { y += shelfHeight; x = 0; shelfHeight = 0; }
                if (y + h > MaximumSize) return;
                positions[i] = new PackedPlacement { index = i, x = x + PackGutter, y = y + PackGutter, width = widths[i], height = heights[i] };
                x += w; occupiedWidth = Math.Max(occupiedWidth, x); shelfHeight = Math.Max(shelfHeight, h);
            }
            int height = y + shelfHeight, area = occupiedWidth * height;
            if (best == null || area < best.width * best.height || area == best.width * best.height && Math.Max(occupiedWidth, height) < Math.Max(best.width, best.height))
                best = new PackedLayout { width = occupiedWidth, height = height, frames = positions };
        }

        static Snapshot ReadSource(string project, string path)
        {
            RequireInside(path, project, "Assets/TalesTactics/Art");
            var raster = ReadPng(path); string relative = Relative(project, path), metaHash = Hash(File.ReadAllBytes(path + ".meta"));
            var importer = AssetImporter.GetAtPath(relative) as TextureImporter;
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple) throw new InvalidOperationException("Source must already be imported as multiple sprites.");
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) throw new InvalidOperationException("Source sprite data provider is unavailable.");
            provider.InitSpriteEditorDataProvider(); var rects = provider.GetSpriteRects();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(relative).OfType<Sprite>().ToDictionary(s => s.name, StringComparer.Ordinal);
            string guid = AssetDatabase.AssetPathToGUID(relative);
            if (rects == null || rects.Length == 0 || rects.Length != sprites.Count || rects.Select(r => r.name).Distinct().Count() != rects.Length)
                throw new InvalidOperationException("Imported source frame coverage is inconsistent.");
            var frames = new ReferenceFrame[rects.Length];
            for (int i = 0; i < rects.Length; i++)
            {
                var r = rects[i]; RectInt box = IntegralRect(r.rect);
                if (box.x < 0 || box.y < 0 || box.xMax > raster.width || box.yMax > raster.height || box.width <= 0 || box.height <= 0
                    || !Finite(r.pivot.x) || !Finite(r.pivot.y) || r.pivot.x < 0 || r.pivot.x > 1 || r.pivot.y < 0 || r.pivot.y > 1
                    || !sprites.TryGetValue(r.name, out var sprite) || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string actualGuid, out long id) || actualGuid != guid)
                    throw new InvalidOperationException("Invalid source frame identity/bounds: " + r.name);
                frames[i] = new ReferenceFrame { index = i, name = r.name, originalSpriteId = r.spriteID.ToString(), originalInternalId = id.ToString(), sourceRect = new SourceBox(box), sourcePivot = new Pair(r.pivot.x, r.pivot.y) };
            }
            if (frames.Select(f => f.originalSpriteId).Distinct().Count() != frames.Length || frames.Select(f => f.originalInternalId).Distinct().Count() != frames.Length
                || !Finite(importer.spritePixelsPerUnit) || importer.spritePixelsPerUnit <= 0) throw new InvalidOperationException("Invalid source IDs/PPU.");
            return new Snapshot { raster = raster, metaHash = metaHash, guid = guid, ppu = importer.spritePixelsPerUnit, frames = frames };
        }
        static SdCharacterArtImporter.Job ProductionJob(string project, string source)
        {
            var manifest = JsonUtility.FromJson<SdCharacterArtImporter.Manifest>(File.ReadAllText(Path.Combine(project, ProductionManifest)));
            var jobs = manifest?.jobs?.Where(j => j.source == source).ToArray();
            if (jobs == null || jobs.Length != 1 || string.IsNullOrWhiteSpace(jobs[0].sourceSha256) || jobs[0].sourceSha256.Length != 64)
                throw new InvalidOperationException("Source has no unique original production provenance.");
            return jobs[0];
        }
        static void RequireIdentity(SdCharacterArtImporter.Job job, Snapshot original)
        {
            if (job.sourceGuid != original.guid || job.spriteNames == null || !job.spriteNames.OrderBy(n => n, StringComparer.Ordinal).SequenceEqual(original.frames.Select(f => f.name).OrderBy(n => n, StringComparer.Ordinal)))
                throw new InvalidOperationException("Original production GUID/frame names differ from current source.");
        }
        static void ValidateReference(ReferenceManifest m)
        {
            if (m == null || m.schemaVersion != 1 || m.columns != Columns || m.frames == null || m.sheets == null || m.frames.Length == 0 || m.sheets.Length == 0
                || m.maxFramesPerSheet != 16 && m.maxFramesPerSheet != 20 || m.frames.Select(f => f?.name).Distinct().Count() != m.frames.Length)
                throw new InvalidOperationException("Invalid reference manifest.");
            var indices = new List<int>();
            for (int s = 0; s < m.sheets.Length; s++)
            {
                var sheet = m.sheets[s];
                if (sheet == null || sheet.frameIndices == null || sheet.frameIndices.Length == 0 || sheet.frameIndices.Length > m.maxFramesPerSheet
                    || sheet.columns != Columns || sheet.rows < (sheet.frameIndices.Length + Columns - 1) / Columns
                    || sheet.rows > m.maxFramesPerSheet / Columns
                    || sheet.width != sheet.cellWidth * Columns || sheet.height != sheet.cellHeight * sheet.rows)
                    throw new InvalidOperationException("Invalid reference sheet layout.");
                for (int c = 0; c < sheet.frameIndices.Length; c++)
                {
                    int i = sheet.frameIndices[c];
                    if (i < 0 || i >= m.frames.Length || m.frames[i] == null || m.frames[i].index != i || m.frames[i].sheetIndex != s || m.frames[i].cellIndex != c)
                        throw new InvalidOperationException("Reference frame/cell assignment is ambiguous.");
                    indices.Add(i);
                }
            }
            if (!indices.SequenceEqual(Enumerable.Range(0, m.frames.Length))) throw new InvalidOperationException("Reference frames are missing, duplicated or reordered.");
        }
        static void CompareFrames(ReferenceFrame[] expected, ReferenceFrame[] actual)
        {
            if (expected.Length != actual.Length) throw new InvalidOperationException("Reference frame count changed.");
            for (int i = 0; i < expected.Length; i++)
            {
                var a = expected[i]; var b = actual[i];
                if (a.name != b.name || a.originalSpriteId != b.originalSpriteId || a.originalInternalId != b.originalInternalId || a.sourceRect == null || a.sourcePivot == null
                    || a.sourceRect.Rect != b.sourceRect.Rect || Math.Abs(a.sourcePivot.x - b.sourcePivot.x) > .000001f || Math.Abs(a.sourcePivot.y - b.sourcePivot.y) > .000001f)
                    throw new InvalidOperationException("Reference sprite metadata changed: " + b.name);
            }
        }
        static RectInt IntegralRect(Rect r)
        {
            if (new[] { r.x, r.y, r.width, r.height }.Any(v => !Finite(v) || Math.Abs(v - Mathf.Round(v)) > .001f))
                throw new InvalidOperationException("Native pixel extraction requires integral source rectangles.");
            return new RectInt(Mathf.RoundToInt(r.x), Mathf.RoundToInt(r.y), Mathf.RoundToInt(r.width), Mathf.RoundToInt(r.height));
        }
        static RectInt Cell(int width, int height, int columns, int rows, int index)
        {
            int col = index % columns, row = index / columns, x0 = col * width / columns, x1 = (col + 1) * width / columns;
            int y0 = height - (row + 1) * height / rows, y1 = height - row * height / rows;
            return new RectInt(x0, y0, x1 - x0, y1 - y0);
        }
        static void CopyRect(Color32[] source, int sourceWidth, RectInt rect, Color32[] target, int targetWidth, int x, int y)
        {
            for (int row = 0; row < rect.height; row++) Array.Copy(source, (rect.y + row) * sourceWidth + rect.x, target, (y + row) * targetWidth + x, rect.width);
        }
        static string CropHash(Color32[] pixels, int width, RectInt rect)
        {
            var bytes = new byte[checked(rect.width * rect.height * 4)]; int n = 0;
            for (int y = rect.yMin; y < rect.yMax; y++) for (int x = rect.xMin; x < rect.xMax; x++)
            { var c = pixels[y * width + x]; bytes[n++] = c.r; bytes[n++] = c.g; bytes[n++] = c.b; bytes[n++] = c.a; }
            return Hash(bytes);
        }
        static Raster ReadPng(string path)
        {
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Expected a PNG input.");
            byte[] bytes = File.ReadAllBytes(path), signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
            if (bytes.Length < 33 || !bytes.Take(8).SequenceEqual(signature) || bytes[12] != 73 || bytes[13] != 72 || bytes[14] != 68 || bytes[15] != 82 || bytes[24] != 8 || bytes[25] != 6)
                throw new InvalidOperationException("Native input must be an 8-bit RGBA PNG: " + path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!ImageConversion.LoadImage(texture, bytes, false) || texture.width > MaximumSize || texture.height > MaximumSize) throw new InvalidOperationException("Cannot decode supported native input: " + path);
                return new Raster { path = path, hash = Hash(bytes), width = texture.width, height = texture.height, pixels = texture.GetPixels32() };
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
        static byte[] Encode(Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true); Texture2D verify = null;
            try
            {
                texture.SetPixels32(pixels); texture.Apply(false, false); byte[] bytes = ImageConversion.EncodeToPNG(texture);
                verify = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                if (bytes == null || !ImageConversion.LoadImage(verify, bytes, false) || verify.width != width || verify.height != height) throw new InvalidOperationException("PNG round-trip dimensions changed.");
                var decoded = verify.GetPixels32();
                for (int i = 0; i < pixels.Length; i++) if (decoded[i].r != pixels[i].r || decoded[i].g != pixels[i].g || decoded[i].b != pixels[i].b || decoded[i].a != pixels[i].a)
                    throw new InvalidOperationException("PNG round-trip changed RGBA bytes.");
                return bytes;
            }
            finally { if (verify != null) UnityEngine.Object.DestroyImmediate(verify); UnityEngine.Object.DestroyImmediate(texture); }
        }
        static void AssertUnchanged(Snapshot s)
        {
            if (Hash(File.ReadAllBytes(s.raster.path)) != s.raster.hash || Hash(File.ReadAllBytes(s.raster.path + ".meta")) != s.metaHash)
                throw new InvalidOperationException("Production source changed while preparing candidate files.");
        }
        static void WriteNew(IEnumerable<Payload> source)
        {
            var payloads = source.ToArray(); var created = new List<string>();
            if (payloads.Select(p => p.path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != payloads.Length) throw new InvalidOperationException("Duplicate output paths.");
            foreach (var p in payloads) if (File.Exists(p.path) || File.Exists(p.path + ".meta") || Directory.Exists(p.path)) throw new InvalidOperationException("Output already exists: " + p.path);
            try
            {
                foreach (var p in payloads)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(p.path));
                    using (var stream = new FileStream(p.path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { created.Add(p.path); stream.Write(p.bytes, 0, p.bytes.Length); }
                }
            }
            catch { foreach (string path in created) File.Delete(path); throw; }
        }
        static void RequireNewOutput(string path, string extension, string project, string root)
        {
            RequireInside(path, project, root);
            if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) || File.Exists(path) || Directory.Exists(path) || File.Exists(path + ".meta"))
                throw new InvalidOperationException("Choose a NEW " + extension + " output under " + root);
        }
        static void RequireInside(string path, string project, string root)
        {
            string allowed = Path.GetFullPath(Path.Combine(project, root)).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(path).StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Path is outside " + root + ": " + path);
        }
        static string Project() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        static string Resolve(string project, string path)
        { if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A path is required."); return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(project, path)); }
        static string Relative(string project, string path)
        { return path.Substring(project.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1).Replace('\\', '/'); }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static float Median(IEnumerable<float> values)
        { var a = values.OrderBy(v => v).ToArray(); if (a.Length == 0) throw new InvalidOperationException("No body pixels for measurement."); return a.Length % 2 == 1 ? a[a.Length / 2] : (a[a.Length / 2 - 1] + a[a.Length / 2]) * .5f; }
        static SdCharacterArtImporter.Point P(float x, float y) => new SdCharacterArtImporter.Point { x = x, y = y };
        static byte[] Json(object value) => new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(value, true) + "\n");
        static string Hash(byte[] bytes)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    }
}
