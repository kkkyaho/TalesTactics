using System;
using System.Linq;
using NUnit.Framework;
using TalesTactics.Editor;
using UnityEngine;

namespace TalesTactics.Tests
{
    public sealed class QualityMotionAtlasToolsTests
    {
        const int Width = 256, Height = 64;
        static Color32[] Sheet(int figures = 4)
        {
            var pixels = new Color32[Width * Height];
            for (int cell = 0; cell < figures; cell++)
                Paint(pixels, cell * 64 + 20, 15, 22, 30, new Color32((byte)(50 + cell), 110, 170, 255));
            return pixels;
        }
        static void Paint(Color32[] pixels, int x, int y, int width, int height, Color32 color)
        { for (int yy = y; yy < y + height; yy++) for (int xx = x; xx < x + width; xx++) pixels[yy * Width + xx] = color; }

        [Test]
        public void CellAnalysisRetainsWeakAlphaFragmentsWithoutChangingNativeRgba()
        {
            var pixels = Sheet(); pixels[10 * Width + 10] = new Color32(211, 43, 98, 1);
            var before = pixels.ToArray();
            var cells = QualityMotionAtlasTools.ValidateGeneratedCells(pixels, Width, Height, 4, 1, 4);
            Assert.That(cells.Length, Is.EqualTo(4));
            Assert.That(cells[0].nontransparentPixels, Is.EqualTo(22 * 30 + 1));
            Assert.That(cells[0].support.x, Is.EqualTo(10));
            Assert.That(cells[0].largestStrongComponent, Is.EqualTo(22 * 30));
            CollectionAssert.AreEqual(before, pixels);
        }

        [Test]
        public void EvenNearlyTransparentCellBoundaryContactIsRejectedInsteadOfTrimmed()
        {
            var pixels = Sheet(); pixels[20 * Width + 63] = new Color32(1, 2, 3, 1);
            Assert.Throws<InvalidOperationException>(() => QualityMotionAtlasTools.ValidateGeneratedCells(pixels, Width, Height, 4, 1, 4));
            Assert.That(pixels[20 * Width + 63].a, Is.EqualTo(1));
        }

        [Test]
        public void ASecondMajorFigureInOneCellIsRejected()
        {
            var pixels = Sheet(); Paint(pixels, 3, 15, 13, 30, new Color32(255, 255, 255, 255));
            Assert.Throws<InvalidOperationException>(() => QualityMotionAtlasTools.ValidateGeneratedCells(pixels, Width, Height, 4, 1, 4));
        }

        [Test]
        public void UnusedLastCellMustRemainTransparentAndMissingRequiredCellsFail()
        {
            var pixels = Sheet(3);
            Assert.That(QualityMotionAtlasTools.ValidateGeneratedCells(pixels, Width, Height, 4, 1, 3).Length, Is.EqualTo(3));
            Assert.Throws<InvalidOperationException>(() => QualityMotionAtlasTools.ValidateGeneratedCells(pixels, Width, Height, 4, 1, 4));
            pixels[30 * Width + 220] = new Color32(1, 2, 3, 1);
            Assert.Throws<InvalidOperationException>(() => QualityMotionAtlasTools.ValidateGeneratedCells(pixels, Width, Height, 4, 1, 3));
        }

        [Test]
        public void AdaptiveRowsUseRealTransparentCorridorsWithoutCuttingStrongArt()
        {
            var pixels = new Color32[Width * 128];
            for (int cell = 0; cell < 4; cell++)
            {
                Paint(pixels, cell * 64 + 20, 55, 22, 31, new Color32(80, 120, 180, 255));
                Paint(pixels, cell * 64 + 20, 15, 22, 31, new Color32(90, 130, 190, 255));
            }
            var before = pixels.ToArray();
            var result = QualityMotionAtlasTools.AnalyzeAdaptiveSheet(pixels, Width, 128, 4, 2, 8);
            Assert.That(result.rowCutsFromTop[1], Is.Not.EqualTo(64));
            Assert.That(Math.Abs(result.rowCutsFromTop[1] - 64), Is.LessThanOrEqualTo(128f / 2 * .2f));
            Assert.That(result.frames.Length, Is.EqualTo(8));
            Assert.That(result.ownedPixelIndices.Sum(p => p.Length), Is.EqualTo(8 * 22 * 31));
            CollectionAssert.AreEqual(before, pixels);
        }

        [Test]
        public void AdaptiveWeakPixelsUseNearestStrongOwnerAndPreserveEdgeRgba()
        {
            var pixels = Sheet(); Paint(pixels, 20, 15, 31, 30, new Color32(51, 110, 170, 255));
            int crossing = 30 * Width + 65, edge = 0;
            pixels[crossing] = new Color32(211, 43, 98, 8); pixels[edge] = new Color32(41, 92, 182, 1);
            var before = pixels.ToArray();
            var result = QualityMotionAtlasTools.AnalyzeAdaptiveSheet(pixels, Width, Height, 4, 1, 4);
            Assert.That(result.weakPixels, Is.EqualTo(2)); Assert.That(result.weakPixelsOutsideTheirAssignedCell, Is.EqualTo(1));
            CollectionAssert.Contains(result.ownedPixelIndices[0], crossing); CollectionAssert.Contains(result.ownedPixelIndices[0], edge);
            var all = result.ownedPixelIndices.SelectMany(p => p).ToArray();
            Assert.That(all.Length, Is.EqualTo(pixels.Count(p => p.a != 0)));
            Assert.That(all.Distinct().Count(), Is.EqualTo(all.Length));
            CollectionAssert.AreEqual(before, pixels);
        }

        [Test]
        public void AdaptiveLayoutRejectsStrongArtWhenNoCorridorExists()
        {
            var pixels = Sheet(); Paint(pixels, 50, 50, 29, 1, new Color32(255, 255, 255, 9));
            Assert.Throws<InvalidOperationException>(() => QualityMotionAtlasTools.AnalyzeAdaptiveSheet(pixels, Width, Height, 4, 1, 4));
        }

        [Test]
        public void PaddedLastSheetKeepsTwelveFramesAndRejectsArtInEightBlankCells()
        {
            const int paddedHeight = 320;
            var pixels = new Color32[Width * paddedHeight];
            for (int row = 0; row < 3; row++) for (int column = 0; column < 4; column++)
                Paint(pixels, column * 64 + 20, paddedHeight - (row + 1) * 64 + 15, 22, 30, new Color32(80, 120, 180, 255));
            var original = pixels.ToArray();
            var result = QualityMotionAtlasTools.AnalyzeAdaptiveSheet(pixels, Width, paddedHeight, 4, 5, 12);
            Assert.That(result.frames.Length, Is.EqualTo(12));
            Assert.That(result.ownedPixelIndices.Sum(p => p.Length), Is.EqualTo(12 * 22 * 30));
            CollectionAssert.AreEqual(original, pixels);
            // One alpha>8 pixel is enough to reject invented art in an explicitly unused cell.
            pixels[30 * Width + 30] = new Color32(70, 80, 90, 9);
            Assert.Throws<InvalidOperationException>(() => QualityMotionAtlasTools.AnalyzeAdaptiveSheet(pixels, Width, paddedHeight, 4, 5, 12));
        }

        [Test]
        public void ExplicitCutsCannotCrossStrongArtworkOrChangeCanvasCoverage()
        {
            var pixels = Sheet();
            Assert.Throws<InvalidOperationException>(() => QualityMotionAtlasTools.AnalyzeReviewedSheet(pixels, Width, Height, new[] { 0, 30, 128, 192, Width }, new[] { 0, Height }, 4));
            Assert.Throws<ArgumentException>(() => QualityMotionAtlasTools.AnalyzeReviewedSheet(pixels, Width, Height, new[] { 0, 64, 64, 192, Width }, new[] { 0, Height }, 4));
            Assert.Throws<ArgumentException>(() => QualityMotionAtlasTools.AnalyzeReviewedSheet(pixels, Width, Height, new[] { 1, 64, 128, 192, Width }, new[] { 0, Height }, 4));
            Assert.Throws<ArgumentException>(() => QualityMotionAtlasTools.AnalyzeReviewedSheet(pixels, Width, Height, new[] { 0, 64, 128, 192, Width - 1 }, new[] { 0, Height }, 4));
        }

        [TestCase(12)] [TestCase(20)]
        public void ExplicitSafeUnequalRowsPreserveEveryNativePixelAndFrameOrder(int used)
        {
            const int imageHeight = 320;
            int[] columns = { 0, 64, 128, 192, Width }, rows = { 0, 50, 90, 152, 230, imageHeight };
            var pixels = new Color32[Width * imageHeight];
            for (int i = 0; i < used; i++)
                Paint(pixels, i % 4 * 64 + 20, imageHeight - rows[i / 4 + 1] + 5, 22, 30, new Color32((byte)(50 + i), 110, 180, 255));
            pixels[0] = new Color32(11, 22, 33, 1); // Preserve even a remote weak pixel instead of trimming it.
            var before = pixels.ToArray();
            Assert.Throws<InvalidOperationException>(() => QualityMotionAtlasTools.AnalyzeAdaptiveSheet(pixels, Width, imageHeight, 4, 5, used));
            var result = QualityMotionAtlasTools.AnalyzeReviewedSheet(pixels, Width, imageHeight, columns, rows, used);
            Assert.That(result.frames.Length, Is.EqualTo(used));
            var all = result.ownedPixelIndices.SelectMany(p => p).ToArray();
            Assert.That(all.Length, Is.EqualTo(used * 22 * 30 + 1));
            Assert.That(all.Distinct().Count(), Is.EqualTo(all.Length));
            for (int i = 0; i < used; i++)
            {
                int[] strong = result.ownedPixelIndices[i].Where(p => pixels[p].a > 128).ToArray();
                Assert.That(strong.Length, Is.EqualTo(22 * 30));
                Assert.That(strong.All(p => pixels[p].r == 50 + i), Is.True, "Ordered frame " + i);
            }
            CollectionAssert.AreEqual(before, pixels);
        }

        [Test]
        public void WeakPixelSpatialSearchMatchesBruteForceIncludingOwnerTies()
        {
            var pixels = Sheet(); Paint(pixels, 20, 15, 23, 30, new Color32(51, 110, 170, 255));
            for (int y = 0; y < Height; y += 9) for (int x = 0; x < Width; x += 17)
                if (pixels[y * Width + x].a == 0) pixels[y * Width + x] = new Color32(5, 70, 110, 1);
            pixels[30 * Width + 63] = new Color32(23, 90, 111, 8);
            var result = QualityMotionAtlasTools.AnalyzeAdaptiveSheet(pixels, Width, Height, 4, 1, 4);
            int[] seeds = Enumerable.Range(0, pixels.Length).Where(i => pixels[i].a > 8).ToArray();
            int[] owners = Enumerable.Repeat(-1, pixels.Length).ToArray();
            for (int owner = 0; owner < 4; owner++) foreach (int point in result.ownedPixelIndices[owner]) owners[point] = owner;
            for (int point = 0; point < pixels.Length; point++) if (pixels[point].a > 0 && pixels[point].a <= 8)
            {
                int best = int.MaxValue, expected = int.MaxValue, x = point % Width, y = point / Width;
                foreach (int seed in seeds)
                {
                    int dx = seed % Width - x, dy = seed / Width - y, distance = dx * dx + dy * dy, owner = seed % Width / 64;
                    if (distance < best) { best = distance; expected = owner; }
                    else if (distance == best) expected = Math.Min(expected, owner);
                }
                Assert.That(owners[point], Is.EqualTo(expected), "Weak pixel " + point);
            }
            Assert.That(result.weakDistanceTies, Is.GreaterThanOrEqualTo(1));
            Assert.That(owners[30 * Width + 63], Is.EqualTo(0));
        }

        [TestCase(18)] [TestCase(54)] [TestCase(72)] [TestCase(80)]
        public void NativePackingKeepsEveryFrameIdentitySizeAndTransparentGutter(int count)
        {
            int[] widths = Enumerable.Range(0, count).Select(i => 170 + i % 7 * 13).ToArray();
            int[] heights = Enumerable.Range(0, count).Select(i => 210 + i % 11 * 9).ToArray();
            var layout = QualityMotionAtlasTools.FindLayout(widths, heights);
            Assert.That(layout.width, Is.LessThanOrEqualTo(4096)); Assert.That(layout.height, Is.LessThanOrEqualTo(4096));
            Assert.That(layout.frames.Length, Is.EqualTo(count));
            for (int i = 0; i < count; i++)
            {
                var f = layout.frames[i]; Assert.That(f.index, Is.EqualTo(i));
                Assert.That(f.width, Is.EqualTo(widths[i])); Assert.That(f.height, Is.EqualTo(heights[i]));
                Assert.That(f.x, Is.GreaterThanOrEqualTo(2)); Assert.That(f.y, Is.GreaterThanOrEqualTo(2));
                Assert.That(f.x + f.width + 2, Is.LessThanOrEqualTo(layout.width));
                Assert.That(f.y + f.height + 2, Is.LessThanOrEqualTo(layout.height));
                var padded = new RectInt(f.x - 2, f.y - 2, f.width + 4, f.height + 4);
                for (int j = 0; j < i; j++)
                {
                    var other = layout.frames[j];
                    Assert.That(padded.Overlaps(new RectInt(other.x - 2, other.y - 2, other.width + 4, other.height + 4)), Is.False);
                }
            }
        }

        [Test]
        public void StaggeredRowsKeepNativePixelsAndOrderedOwnershipWithoutOneGlobalColumnCut()
        {
            var pixels = new Color32[Width * 128];
            for (int col = 0; col < 4; col++)
            {
                Paint(pixels, col * 64 + 4, 80, 50, 30, new Color32((byte)(40 + col), 80, 100, 255));
                Paint(pixels, col * 64 + 18, 15, col == 3 ? 40 : 50, 30, new Color32((byte)(44 + col), 80, 100, 255));
            }
            pixels[64 * Width + 64] = new Color32(1, 2, 3, 1);
            var before = pixels.ToArray();
            var result = QualityMotionAtlasTools.AnalyzeAdaptiveSheet(pixels, Width, 128, 4, 2, 8);
            Assert.That(result.columnCuts.Length, Is.EqualTo(0));
            Assert.That(result.rowColumnCuts.Length, Is.EqualTo(10));
            var all = result.ownedPixelIndices.SelectMany(p => p).ToArray();
            Assert.That(all.Length, Is.EqualTo(pixels.Count(p => p.a != 0)));
            Assert.That(all.Distinct().Count(), Is.EqualTo(all.Length));
            for (int i = 0; i < 8; i++) Assert.That(result.ownedPixelIndices[i].Where(p => pixels[p].a > 128).All(p => pixels[p].r == 40 + i), Is.True);
            CollectionAssert.AreEqual(before, pixels);
        }

        [Test]
        public void RowSpecificCutStillRejectsTouchingFiguresAndMalformedPartitions()
        {
            var pixels = Sheet();
            Assert.Throws<InvalidOperationException>(() => QualityMotionAtlasTools.AnalyzeRowSeparatedSheet(pixels, Width, Height,
                new[] {0,30,128,192,Width}, new[] {0,Height}, 4));
            Assert.Throws<ArgumentException>(() => QualityMotionAtlasTools.AnalyzeRowSeparatedSheet(pixels, Width, Height,
                new[] {0,64,128,192,Width,0}, new[] {0,Height}, 4));
            Assert.Throws<ArgumentException>(() => QualityMotionAtlasTools.AnalyzeRowSeparatedSheet(pixels, Width, Height,
                new[] {0,64,64,192,Width}, new[] {0,Height}, 4));
        }

        [Test]
        public void ComponentSeedsPreserveInterleavedNativeSilhouettesAndAllAlpha()
        {
            var pixels = Sheet();
            // Neighboring silhouettes interleave vertically, so no vertical empty cut exists.
            Paint(pixels, 40, 38, 40, 7, new Color32(50, 110, 170, 255));
            Paint(pixels, 61, 15, 25, 7, new Color32(51, 110, 170, 255));
            pixels[0] = new Color32(99, 22, 33, 48);
            var before = pixels.ToArray();
            var result = QualityMotionAtlasTools.AnalyzeComponentSeparatedSheet(pixels, Width, Height,
                new[] {25,25,89,25,153,25,217,25},4);
            var all = result.ownedPixelIndices.SelectMany(p => p).ToArray();
            Assert.That(all.Length, Is.EqualTo(pixels.Count(p => p.a > 0)));
            Assert.That(all.Distinct().Count(), Is.EqualTo(all.Length));
            for (int i = 0; i < 4; i++)
                Assert.That(result.ownedPixelIndices[i].Where(p => pixels[p].a > 128).All(p => pixels[p].r == 50+i), Is.True);
            CollectionAssert.Contains(result.ownedPixelIndices[0],0);
            CollectionAssert.AreEqual(before,pixels);
        }

        [Test]
        public void ComponentSeedsRejectMergedMissingExtraAndClippedFigures()
        {
            var seeds=new[] {25,25,89,25,153,25,217,25};
            var pixels=Sheet();
            Assert.Throws<InvalidOperationException>(()=>QualityMotionAtlasTools.AnalyzeComponentSeparatedSheet(pixels,Width,Height,new[]{25,25,26,25,153,25,217,25},4));
            Assert.Throws<InvalidOperationException>(()=>QualityMotionAtlasTools.AnalyzeComponentSeparatedSheet(pixels,Width,Height,new[]{0,0,89,25,153,25,217,25},4));
            Paint(pixels,3,15,13,30,new Color32(1,2,3,255));
            Assert.Throws<InvalidOperationException>(()=>QualityMotionAtlasTools.AnalyzeComponentSeparatedSheet(pixels,Width,Height,seeds,4));
            pixels=Sheet(); Paint(pixels,0,15,21,30,new Color32(50,110,170,255));
            Assert.Throws<InvalidOperationException>(()=>QualityMotionAtlasTools.AnalyzeComponentSeparatedSheet(pixels,Width,Height,seeds,4));
            pixels=Sheet(); Paint(pixels,41,25,44,4,new Color32(50,110,170,255));
            Assert.Throws<InvalidOperationException>(()=>QualityMotionAtlasTools.AnalyzeComponentSeparatedSheet(pixels,Width,Height,seeds,4));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ComponentSeedsRejectWholeDetachedPieceWhenOwnersDisagreeOrTie(bool singleTiedPixel)
        {
            var pixels = new Color32[Width * Height];
            Paint(pixels, 3, 3, 16, 16, new Color32(10, 20, 30, 255));
            Paint(pixels, singleTiedPixel ? 40 : 39, 3, 16, 16, new Color32(40, 50, 60, 255));
            // The 18-pixel part's BFS middle prefers the right body, but its left
            // end prefers the left. A lone exactly equidistant pixel is ambiguous too.
            Paint(pixels, singleTiedPixel ? 29 : 20, 20, singleTiedPixel ? 1 : 18, 1, new Color32(70, 80, 90, 255));
            var before = pixels.ToArray();
            var error = Assert.Throws<InvalidOperationException>(() => QualityMotionAtlasTools.AnalyzeComponentSeparatedSheet(
                pixels, Width, Height, new[] {10,10,46,10}, 2));
            StringAssert.Contains("ambiguous nearest seeded bodies", error.Message);
            CollectionAssert.AreEqual(before, pixels);
        }

        [Test]
        public void ComponentFragmentSizeLimitUsesOriginalSeedBodyInsteadOfEarlierFragments()
        {
            var pixels = Sheet(1);
            // The seeded body has 660 pixels. This earlier 60-pixel part must not
            // raise its 12% limit from 79.2 to 86.4 and admit the later 80-pixel part.
            Paint(pixels, 20, 3, 10, 6, new Color32(80, 90, 100, 255));
            Paint(pixels, 20, 50, 10, 8, new Color32(110, 120, 130, 255));
            var before = pixels.ToArray();
            var error = Assert.Throws<InvalidOperationException>(() => QualityMotionAtlasTools.AnalyzeComponentSeparatedSheet(
                pixels, Width, Height, new[] {25,25}, 1));
            StringAssert.Contains("major opaque figure", error.Message);
            CollectionAssert.AreEqual(before, pixels);
        }

        [Test]
        public void ComponentSeedsKeepUnambiguousSmallPieceIntactAndPreserveAllNativeAlpha()
        {
            var pixels = Sheet();
            Paint(pixels, 24, 6, 3, 3, new Color32(220, 30, 90, 255));
            pixels[0] = new Color32(123, 45, 67, 1);
            var before = pixels.ToArray();
            var result = QualityMotionAtlasTools.AnalyzeComponentSeparatedSheet(pixels, Width, Height,
                new[] {25,25,89,25,153,25,217,25}, 4);
            for (int y = 6; y < 9; y++) for (int x = 24; x < 27; x++)
                CollectionAssert.Contains(result.ownedPixelIndices[0], y * Width + x);
            CollectionAssert.Contains(result.ownedPixelIndices[0], 0);
            var all = result.ownedPixelIndices.SelectMany(points => points).ToArray();
            Assert.That(all.Length, Is.EqualTo(pixels.Count(pixel => pixel.a != 0)));
            Assert.That(all.Distinct().Count(), Is.EqualTo(all.Length));
            CollectionAssert.AreEqual(before, pixels);
        }

        [Test]
        public void ImpossibleNativePackingFailsWithoutResizing()
        {
            Assert.Throws<InvalidOperationException>(() => QualityMotionAtlasTools.FindLayout(new[] { 4090, 4090 }, new[] { 4090, 4090 }));
            Assert.Throws<ArgumentException>(() => QualityMotionAtlasTools.FindLayout(new[] { 4096 }, new[] { 256 }));
        }
    }
}
