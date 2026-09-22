using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [Test] public void DropBucketsExhaustivelyMatchPublishedProbabilities()
        {
            for(int stage=0;stage<2;stage++)
            {
                var rolls=Enumerable.Range(0,10000).Select(i=>CampaignEconomy.Drop(stage,i)).ToArray();
                Assert.That(rolls.Count(id=>id=="bronze-sword"),Is.EqualTo(2500));
                Assert.That(rolls.Count(id=>id==(stage==0?"leather-armor":"reinforced-armor")),Is.EqualTo(1500));
                Assert.That(rolls.Count(id=>id==null),Is.EqualTo(6000));
            }
            Assert.Throws<ArgumentOutOfRangeException>(()=>CampaignEconomy.Drop(0,-1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CampaignEconomy.Drop(0,10000));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CampaignEconomy.Drop(2,0));
        }
        [Test] public void RewardSnapshotRetriesSameDropAndCannotApplyTwice()
        {
            var save=new CampaignSave();var original=save.Inventory.ToArray();var reward=CampaignEconomy.Prepare(save,0,2500);
            Assert.That(CampaignStages.TryReward(save,reward,new[]{data},_=>false),Is.False);
            Assert.That(save.Inventory,Is.EqualTo(original));Assert.That(save.Characters,Is.Empty);
            Assert.Throws<IOException>(()=>CampaignStages.TryReward(save,reward,new[]{data},_=>throw new IOException()));
            Assert.That(save.Gold,Is.EqualTo(300));Assert.That(save.Inventory,Is.EqualTo(original));
            var file=new CampaignFile(SaveTestPath());Assert.That(CampaignStages.TryReward(save,reward,new[]{data},file.Save),Is.True);
            Assert.That(CampaignStages.TryReward(save,reward,new[]{data},file.Save),Is.False);
            var loaded=file.Load();Assert.That(CampaignInventory.Owned(loaded,"leather-armor"),Is.EqualTo(1));
            Assert.That(CampaignInventory.Owned(loaded,"vital-charm"),Is.EqualTo(1));Assert.That(loaded.Gold,Is.EqualTo(420));
        }
        [Test] public void FirstClearAndRepeatGrowthUseSeparateRewardsAndRespectLevelCap()
        {
            var save=new CampaignSave();var party=new[]{data,data};
            Assert.That(CampaignStages.TryReward(save,0,party,_=>true),Is.True);
            Assert.That(save.Get(data.Id).Level,Is.EqualTo(2));Assert.That(save.Get(data.Id).EXP,Is.EqualTo(20));
            Assert.That(CampaignStages.TryReward(save,1,party,_=>true),Is.True);
            Assert.That(save.Get(data.Id).Level,Is.EqualTo(3));Assert.That(save.Get(data.Id).EXP,Is.Zero);
            var repeated=CampaignEconomy.Prepare(save,1,9999);Assert.That(repeated.Gold,Is.EqualTo(90));Assert.That(repeated.Experience,Is.EqualTo(90));
            CampaignStages.TryReward(save,repeated,party,_=>true);Assert.That(save.Gold,Is.EqualTo(690));Assert.That(save.Get(data.Id).EXP,Is.EqualTo(90));
            save.Get(data.Id).Level=50;CampaignStages.TryReward(save,1,party,_=>true);Assert.That(save.Get(data.Id).EXP,Is.Zero);
        }
        [Test] public void FullBonusInventorySkipsOnlyThatItemAndRollbackPreservesReferences()
        {
            var save=new CampaignSave();var full=new OwnedEquipment{Id="leather-armor",Count=99};save.Inventory.Add(full);
            var reward=CampaignEconomy.Prepare(save,0,2500);var list=save.Inventory;save.Gold=CampaignInventory.MaxGold-5;
            Assert.That(CampaignStages.TryReward(save,reward,new[]{data},_=>false),Is.False);
            Assert.That(save.Inventory,Is.SameAs(list));Assert.That(save.Inventory[1],Is.SameAs(full));Assert.That(full.Count,Is.EqualTo(99));
            Assert.That(CampaignStages.TryReward(save,reward,new[]{data},_=>true),Is.True);
            Assert.That(full.Count,Is.EqualTo(99));Assert.That(CampaignInventory.Owned(save,"vital-charm"),Is.EqualTo(1));
            Assert.That(save.Gold,Is.EqualTo(CampaignInventory.MaxGold));
        }
        [Test] public void LockedAndStaleFirstClearRewardCannotBypassCompletionState()
        {
            var save=new CampaignSave();Assert.Throws<ArgumentException>(()=>CampaignEconomy.Prepare(save,1,0));
            var stale=CampaignEconomy.Prepare(save,0,0);CampaignStages.TryReward(save,0,new[]{data},_=>true);
            int gold=save.Gold;Assert.That(CampaignStages.TryReward(save,stale,new[]{data},_=>true),Is.False);Assert.That(save.Gold,Is.EqualTo(gold));
        }
    }
}
