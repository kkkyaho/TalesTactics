using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public sealed class EndingProgressTests
    {
        [Test] public void OlderSaveWithoutEndingFieldsDoesNotStartAnUnrelatedStory()
        {
            var save=JsonUtility.FromJson<CampaignSave>("{\"Version\":3,\"Gold\":300,\"Inventory\":[],\"StoryProgress\":[\"chapter1\"]}");
            CampaignFile.Normalize(save);
            Assert.That(save.HasPendingAfterStory,Is.False);Assert.That(save.PendingAfterStoryStage,Is.EqualTo(-1));
            Assert.That(save.StoryProgress,Has.Member("chapter1"));Assert.That(save.Gold,Is.EqualTo(300));
        }
        [Test] public void PendingEndingRoundTripsAndInvalidStageCannotBlockLoading()
        {
            var save=new CampaignSave{HasPendingAfterStory=true,PendingAfterStoryStage=0,PendingAfterStoryLine=2};save.StoryProgress.Add("chapter1");
            var restored=JsonUtility.FromJson<CampaignSave>(JsonUtility.ToJson(save));CampaignFile.Normalize(restored);
            Assert.That(restored.HasPendingAfterStory,Is.True);Assert.That(restored.PendingAfterStoryLine,Is.EqualTo(2));
            restored.PendingAfterStoryStage=CampaignStages.Count;CampaignFile.Normalize(restored);
            Assert.That(restored.HasPendingAfterStory,Is.False);Assert.That(restored.PendingAfterStoryStage,Is.EqualTo(-1));
        }
    }
}
