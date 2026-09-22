using NUnit.Framework;

namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [Test] public void ChapterStockUnlockRequiresSavedCompletionAndSurvivesReload()
        {
            var save = new CampaignSave(); var item = Sword(); item.BuyPrice = 240;
            item.RequiredStoryFlag = CampaignStages.Id(0); int writes = 0;
            Assert.That(CampaignInventory.TryBuy(save, item, _ => { writes++; return true; }), Is.False);
            Assert.That(writes, Is.Zero);
            data.Skills = new SkillData[0];
            Assert.That(CampaignStages.TryReward(save, 0, new[] { data }, _ => false), Is.False);
            Assert.That(CampaignInventory.PurchaseUnavailable(save, item), Is.EqualTo("해금 조건: 1장 완료"));
            var file = new CampaignFile(SaveTestPath());
            Assert.That(CampaignStages.TryReward(save, 0, new[] { data }, file.Save), Is.True);
            var loaded = file.Load();
            Assert.That(CampaignInventory.TryBuy(loaded, item, file.Save), Is.True);
            loaded = file.Load();
            Assert.That(loaded.Gold, Is.EqualTo(180));
            Assert.That(CampaignInventory.Owned(loaded, item.Id), Is.EqualTo(1));
        }

        [Test] public void UnknownUnlockFlagStaysLockedAndDoesNotRestrictOwnedEquipmentSale()
        {
            var save = new CampaignSave(); var item = Sword(); item.BuyPrice = 100;
            item.RequiredStoryFlag = "future-chapter";
            Assert.That(CampaignInventory.PurchaseUnavailable(save, item), Is.EqualTo("해금 조건: future-chapter"));
            save.Inventory.Add(new OwnedEquipment { Id = item.Id, Count = 1 });
            Assert.That(CampaignInventory.TrySell(save, item, _ => true), Is.True);
            item.RequiredStoryFlag = "";
            Assert.That(CampaignInventory.PurchaseUnavailable(save, item), Is.Null);
        }
    }
}
