using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [Test] public void SixChapterProgressionPersistsAndFailedExpansionRewardsAreAtomic()
        {
            var file=new CampaignFile(SaveTestPath());var save=new CampaignSave();var party=new[]{data};
            int[] levels={1,1,1,2,2,2},exp={30,60,90,20,50,80},gold={420,600,840,1160,1580,2140};
            for(int stage=0;stage<6;stage++)
            {
                Assert.That(CampaignStages.Unlocked(save,stage),Is.True);
                if(stage<CampaignStages.LegacyCount-1)Assert.That(CampaignStages.Unlocked(save,stage+1),Is.False);
                var reward=CampaignEconomy.Prepare(save,stage,9999);
                int beforeGold=save.Gold,beforeLevel=save.Get(data.Id).Level,beforeEXP=save.Get(data.Id).EXP;
                Assert.That(CampaignStages.TryReward(save,reward,party,_=>false),Is.False);
                Assert.That(save.Gold,Is.EqualTo(beforeGold));Assert.That(save.Get(data.Id).Level,Is.EqualTo(beforeLevel));Assert.That(save.Get(data.Id).EXP,Is.EqualTo(beforeEXP));
                Assert.That(save.StoryProgress.Count,Is.EqualTo(stage));
                Assert.That(CampaignStages.TryReward(save,reward,party,file.Save),Is.True);
                Assert.That(CampaignStages.TryReward(save,reward,party,file.Save),Is.False);
                save=file.Load();Assert.That(save.Get(data.Id).Level,Is.EqualTo(levels[stage]));Assert.That(save.Get(data.Id).EXP,Is.EqualTo(exp[stage]));Assert.That(save.Gold,Is.EqualTo(gold[stage]));
            }
            Assert.That(save.Version,Is.EqualTo(3));Assert.That(CampaignInventory.Owned(save,"guardian-medal"),Is.EqualTo(2));
            Assert.That(CampaignInventory.Owned(save,"tempered-armor"),Is.EqualTo(1));
            Assert.That(CampaignStages.HasNext(5),Is.False);Assert.That(CampaignStages.Unlocked(save,6),Is.True);
            Assert.That(CampaignStages.TryReward(save,5,party,file.Save),Is.True);
            save=file.Load();Assert.That(save.Gold,Is.EqualTo(2420));Assert.That(save.Get(data.Id).EXP,Is.EqualTo(90));
            Assert.That(save.StoryProgress.Count,Is.EqualTo(6));
        }
        [Test] public void ThirdChapterContinuesExistingSaveAndRollsBackFailedReward()
        {
            var save=new CampaignSave();var party=new[]{data};
            Assert.That(CampaignStages.Unlocked(save,2),Is.False);
            CampaignStages.TryReward(save,0,party,_=>true);
            Assert.That(CampaignStages.Unlocked(save,2),Is.False);
            CampaignStages.TryReward(save,1,party,_=>true);
            var file=new CampaignFile(SaveTestPath());Assert.That(file.Save(save),Is.True);
            save=file.Load();Assert.That(save.Version,Is.EqualTo(3));
            Assert.That(CampaignStages.Unlocked(save,2),Is.True);
            var reward=CampaignEconomy.Prepare(save,2,2500);
            Assert.That(CampaignStages.TryReward(save,reward,party,_=>false),Is.False);
            Assert.That(save.Get(data.Id).Level,Is.EqualTo(1));Assert.That(save.Gold,Is.EqualTo(600));
            Assert.That(save.StoryProgress.Count,Is.EqualTo(2));
            Assert.That(CampaignInventory.Owned(save,"reinforced-armor"),Is.Zero);
            Assert.That(CampaignStages.TryReward(save,reward,party,file.Save),Is.True);
            Assert.That(CampaignStages.TryReward(save,reward,party,file.Save),Is.False);
            save=file.Load();Assert.That(save.Get(data.Id).Level,Is.EqualTo(1));Assert.That(save.Get(data.Id).EXP,Is.EqualTo(90));
            Assert.That(save.Gold,Is.EqualTo(840));Assert.That(save.StoryProgress.Count,Is.EqualTo(3));
            Assert.That(CampaignInventory.Owned(save,"reinforced-armor"),Is.EqualTo(2));
            Assert.That(CampaignStages.TryReward(save,2,party,file.Save),Is.True);
            save=file.Load();Assert.That(save.Get(data.Id).EXP,Is.Zero);Assert.That(save.Gold,Is.EqualTo(960));
            Assert.That(save.StoryProgress.Count,Is.EqualTo(3));Assert.That(CampaignStages.Unlocked(save,3),Is.True);
        }
        [Test] public void DropBucketsExhaustivelyMatchPublishedProbabilities()
        {
            for(int stage=0;stage<PersonalScenarios.FirstStage;stage++)
            {
                var rolls=Enumerable.Range(0,10000).Select(i=>CampaignEconomy.Drop(stage,i)).ToArray();
                Assert.That(rolls.Count(id=>id=="bronze-sword"),Is.EqualTo(2500));
                Assert.That(rolls.Count(id=>id==(stage==0?"leather-armor":"reinforced-armor")),Is.EqualTo(1500));
                Assert.That(rolls.Count(id=>id==null),Is.EqualTo(6000));
            }
            Assert.Throws<ArgumentOutOfRangeException>(()=>CampaignEconomy.Drop(0,-1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CampaignEconomy.Drop(0,10000));
            Assert.Throws<ArgumentOutOfRangeException>(()=>CampaignEconomy.Drop(CampaignStages.Count,0));
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
            Assert.That(save.Get(data.Id).Level,Is.EqualTo(1));Assert.That(save.Get(data.Id).EXP,Is.EqualTo(30));
            Assert.That(CampaignStages.TryReward(save,1,party,_=>true),Is.True);
            Assert.That(save.Get(data.Id).Level,Is.EqualTo(1));Assert.That(save.Get(data.Id).EXP,Is.EqualTo(60));
            var repeated=CampaignEconomy.Prepare(save,1,9999);Assert.That(repeated.Gold,Is.EqualTo(90));Assert.That(repeated.Experience,Is.EqualTo(10));
            CampaignStages.TryReward(save,repeated,party,_=>true);Assert.That(save.Gold,Is.EqualTo(690));Assert.That(save.Get(data.Id).EXP,Is.EqualTo(70));
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
