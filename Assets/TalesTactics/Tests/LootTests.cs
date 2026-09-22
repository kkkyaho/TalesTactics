using System.IO;
using NUnit.Framework;

namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [Test] public void EquipmentRewardRollbackRetryReloadAndRepeatVictory()
        {
            var save = new CampaignSave(); data.Skills = new SkillData[0];
            var party = new[] { data }; string id = CampaignStages.EquipmentReward(0);
            Assert.That(CampaignStages.TryReward(save, 0, party, _ => false), Is.False);
            Assert.That(CampaignInventory.Owned(save, id), Is.Zero);
            Assert.That(save.Gold, Is.EqualTo(300)); Assert.That(save.StoryProgress, Is.Empty);
            Assert.Throws<IOException>(() => CampaignStages.TryReward(save, 0, party, _ => throw new IOException("test")));
            Assert.That(CampaignInventory.Owned(save, id), Is.Zero);
            var file = new CampaignFile(SaveTestPath());
            Assert.That(CampaignStages.TryReward(save, 0, party, file.Save), Is.True);
            save = file.Load(); Assert.That(CampaignInventory.Owned(save, id), Is.EqualTo(1));
            Assert.That(CampaignStages.TryReward(save, 0, party, _ => false), Is.False);
            Assert.That(CampaignInventory.Owned(save, id), Is.EqualTo(1));
            Assert.That(CampaignStages.TryReward(save, 0, party, file.Save), Is.True);
            Assert.That(CampaignInventory.Owned(file.Load(), id), Is.EqualTo(2));
        }

        [Test] public void FullRewardInventoryKeepsOtherRewardsAndEquippedCopies()
        {
            var save = new CampaignSave(); data.Skills = new SkillData[0];
            string id = CampaignStages.EquipmentReward(1);
            save.Inventory.Add(new OwnedEquipment { Id = id, Count = 99 });
            save.Get(data.Id).Equipment[0] = id;
            Assert.That(CampaignStages.TryReward(save, 1, new[] { data }, _ => true), Is.False);
            save.StoryProgress.Add("chapter1");
            Assert.That(CampaignStages.TryReward(save, 1, new[] { data }, _ => true), Is.True);
            Assert.That(CampaignInventory.Owned(save, id), Is.EqualTo(99));
            Assert.That(save.Get(data.Id).Equipment[0], Is.EqualTo(id));
            Assert.That(save.Gold, Is.EqualTo(480));
            Assert.That(save.StoryProgress, Does.Contain("chapter2"));
            CampaignInventory.Validate(save);
        }
    }
}
