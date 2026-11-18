using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest, Category("CommercialSlice")]
        public IEnumerator OpeningStoriesUseChapterArtWithoutChangingCampaignProgress()
        {
            director.Catalog.CrossoverReady = true; // Shared fixture owns a cloned legacy catalog.
            director.Restart(); director.TrainingMode = false;
            director.Campaign = new CampaignSave();
            for (int n = 1; n <= 3; n++) director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(n));
            int writes = 0; director.PersistCampaign = _ => { writes++; return true; };
            string before = JsonUtility.ToJson(director.Campaign);
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "CommercialSlice20261117", "Story-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(output);
            float originalScale = director.Preferences.TextScale;
            try
            {
                foreach (float scale in new[] { 1f, 1.3f })
                {
                    director.Preferences.TextScale = scale;
                    for (int stage = CrossoverCampaign.FirstStage; stage < CrossoverCampaign.FirstStage + 3; stage++)
                    {
                        director.SelectedStage = stage;
                        director.ReplayStory(false);
                        yield return null;
                        Assert.That(director.StoryActive, Is.True);
                        while (director.StoryActive)
                        {
                            yield return null; Canvas.ForceUpdateCanvases();
                            var panel = director.Hud.transform.Find("CampaignStory");
                            var backdrop = panel.Find("StoryBackdrop").GetComponent<RawImage>();
                            Assert.That(backdrop.texture, Is.SameAs(Resources.Load<Texture2D>(StorySceneArt.Resource(stage))));
                            Assert.That(backdrop.raycastTarget, Is.False);
                            Assert.That(panel.Find("StoryLocation").GetComponent<TMP_Text>().text, Does.Contain("·"));
                            var rect = ((RectTransform)backdrop.transform).rect;
                            float sampledAspect = backdrop.texture.width * backdrop.uvRect.width / (backdrop.texture.height * backdrop.uvRect.height);
                            Assert.That(sampledAspect, Is.EqualTo(rect.width / rect.height).Within(.001f), "Chapter art keeps its aspect ratio.");
                            foreach (var text in panel.GetComponentsInChildren<TMP_Text>())
                            {
                                text.ForceMeshUpdate();
                                Assert.That(text.isTextOverflowing, Is.False, stage + " / " + scale + " / " + text.text);
                            }
                            if (director.StoryIndex == 1)
                            {
                                yield return new WaitForEndOfFrame();
                                var image = ScreenCapture.CaptureScreenshotAsTexture();
                                try { File.WriteAllBytes(Path.Combine(output, "episode-" + (stage - 5) + "-scale-" + scale.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".png"), image.EncodeToPNG()); }
                                finally { UnityEngine.Object.Destroy(image); }
                            }
                            director.AdvanceStory();
                        }
                    }
                }
                Assert.That(writes, Is.Zero, "Story replay must not grant rewards or save progress.");
                Assert.That(JsonUtility.ToJson(director.Campaign), Is.EqualTo(before));
            }
            finally { director.Preferences.TextScale = originalScale; }
        }
    }
}