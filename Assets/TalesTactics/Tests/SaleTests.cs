using System.IO;
using NUnit.Framework;

namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [Test] public void SaleFailureAndExceptionRestoreLastCopyAndInventoryOrder()
        {
            var save = new CampaignSave(); var item = Sword(); item.BuyPrice = 101;
            var entry = new OwnedEquipment { Id = item.Id, Count = 1 }; save.Inventory.Insert(0, entry);
            Assert.That(CampaignInventory.TrySell(save, item, _ => false), Is.False);
            Assert.That(save.Inventory[0], Is.SameAs(entry)); Assert.That(entry.Count, Is.EqualTo(1));
            Assert.Throws<IOException>(() => CampaignInventory.TrySell(save, item, _ => throw new IOException("test")));
            Assert.That(save.Gold, Is.EqualTo(300)); Assert.That(save.Inventory[0], Is.SameAs(entry));
            var file = new CampaignFile(SaveTestPath());
            Assert.That(CampaignInventory.TrySell(save, item, file.Save), Is.True);
            var loaded = file.Load(); Assert.That(loaded.Gold, Is.EqualTo(350));
            Assert.That(CampaignInventory.Owned(loaded, item.Id), Is.Zero);
            Assert.That(loaded.Inventory.Count, Is.EqualTo(1));
            Assert.That(CampaignInventory.TrySell(save, item, file.Save), Is.False);
        }

        [Test] public void SalePreservesEquippedCopiesAndBlocksOverflowAndUnpricedItems()
        {
            var save = new CampaignSave(); var item = Sword(); item.BuyPrice = 100;
            save.Inventory.Add(new OwnedEquipment { Id = item.Id, Count = 2 });
            save.Get("a").Equipment[0] = item.Id;
            Assert.That(CampaignInventory.TrySell(save, item, _ => true), Is.True);
            Assert.That(CampaignInventory.Owned(save, item.Id), Is.EqualTo(1));
            Assert.That(save.Get("a").Equipment[0], Is.EqualTo(item.Id));
            int writes = 0;
            System.Func<CampaignSave, bool> persist = _ => { writes++; return true; };
            Assert.That(CampaignInventory.TrySell(save, item, persist), Is.False);
            save.Get("a").Equipment[0] = null; save.Gold = CampaignInventory.MaxGold - 49;
            Assert.That(CampaignInventory.TrySell(save, item, persist), Is.False);
            item.BuyPrice = 1; Assert.That(CampaignInventory.TrySell(save, item, persist), Is.False);
            Assert.That(writes, Is.Zero);
            item.BuyPrice = 98;
            Assert.That(CampaignInventory.TrySell(save, item, persist), Is.True);
            Assert.That(save.Gold, Is.EqualTo(CampaignInventory.MaxGold));
        }
    }
}
