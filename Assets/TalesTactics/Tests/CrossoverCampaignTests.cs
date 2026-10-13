using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [Test] public void SixthCrossoverEpisodeUnlocksPromotionWithoutCompletingLegacyCampaign()
        {
            data.PromotionJob="상급";data.PromotionStoryFlag="chapter2";var save=new CampaignSave();save.Get(data.Id).Level=data.PromotionLevel;
            save.StoryProgress.Add(CrossoverCampaign.Flag(5));Assert.That(CampaignPromotion.TrySave(save,data,_=>true),Is.False);
            save.StoryProgress.Add(CrossoverCampaign.Flag(6));Assert.That(CampaignPromotion.TrySave(save,data,_=>false),Is.False);Assert.That(save.Get(data.Id).Promoted,Is.False);
            Assert.That(CampaignPromotion.TrySave(save,data,_=>true),Is.True);Assert.That(save.StoryProgress,Does.Not.Contain("chapter2"));
            Assert.That(new CharacterProgress{Level=data.PromotionLevel}.TryPromote(data,save.StoryProgress),Is.True);
            data.PromotionStoryFlag="custom-story";Assert.That(CampaignPromotion.StorySatisfied(data,save.StoryProgress),Is.False);
        }
        BattleCatalog CrossoverTestCatalog()
        {
            var c=FreeTurnCatalog();c.CrossoverReady=true;
            var ids=CrossoverCampaign.Episodes.Select(e=>e.Boss).Concat(CrossoverCampaign.Episodes.Select(e=>e.SecondBoss)).Concat(new[]{"origin","maxwell","boundary-device","sentinel","mage","archer"}).Where(id=>id.Length>0).Distinct();
            c.Enemies=ids.Select(id=>
            {
                var enemyData=New<CharacterData>();enemyData.Id=id;enemyData.DisplayName=id;enemyData.BaseStats=data.BaseStats;enemyData.BasicAttack=Skill(EffectKind.Damage);
                enemyData.Skills=Enumerable.Range(0,3).Select(i=>{var skill=Skill(EffectKind.Damage);skill.Id="boss."+id+"."+i;skill.Range=20;skill.MaxHeightDifference=-1;skill.RequiresLineOfSight=false;return skill;}).ToArray();
                return enemyData;
            }).ToArray();return c;
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void CrossoverObjectiveStateRestoresInAllTurnModes(int mode)
        {
            var c=CrossoverTestCatalog();var b=new BattleSession(c,new[]{0,1,2},campaignStage:14,useCT:mode==1,teamTurns:mode==2);
            b.Opening=BattleOpening.Capture(b,c);b.Advance();b.Crossover.State.Nodes=3;b.Crossover.State.Choice=1;
            b.RecordAction("구조 전");b.Crossover.State.Nodes=7;
            var restored=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1,2}))).Restore(c);
            Assert.That(restored.Crossover.State.Nodes,Is.EqualTo(7));Assert.That(restored.Crossover.State.Choice,Is.EqualTo(1));
            Assert.That(restored.RewindLast(out _).Crossover.State.Nodes,Is.EqualTo(3));Assert.That(restored.Opening.Restore(c).Crossover.State.Nodes,Is.Zero);
        }
        [Test] public void ArtoriusFirstEncounterWinsThroughSealsRescueAndEscapeWhileBossLives()
        {
            var b=new BattleSession(CrossoverTestCatalog(),new[]{0},campaignStage:14);b.Advance();
            Assert.That(b.Crossover.Boss.Data.Id,Is.EqualTo("artorius"));
            b.Crossover.State.Nodes=7;b.Grid.Place(b.Active,CrossoverBattle.Exit);
            Assert.That(b.Result,Is.EqualTo(BattleResult.Ongoing),"Seals still active");
            foreach(var device in b.Units.Where(u=>u.Data.Id=="boundary-device"))device.Damage(99999,b.Grid);
            Assert.That(b.Result,Is.EqualTo(BattleResult.Victory));Assert.That(b.Crossover.Boss.Alive,Is.True);
        }
        [Test] public void ShizelDefeatDoesNotSkipCompanionRescue()
        {
            var b=new BattleSession(CrossoverTestCatalog(),new[]{0},campaignStage:23);var boss=b.Crossover.Boss;
            boss.Damage(99999,b.Grid);boss.BossPhaseLock=false;boss.Damage(99999,b.Grid);
            Assert.That(boss.Alive,Is.False);Assert.That(b.Result,Is.EqualTo(BattleResult.Ongoing));
            b.Crossover.State.Nodes=7;Assert.That(b.Result,Is.EqualTo(BattleResult.Victory));
        }
        [Test] public void SplitFrontNeverActivatesBothBossesTogether()
        {
            var b=new BattleSession(CrossoverTestCatalog(),new[]{0},campaignStage:16);var second=b.Units.Single(u=>u.Data.Id=="legretta");
            Assert.That(second.Alive,Is.False);b.Crossover.Boss.Damage(99999,b.Grid);b.Crossover.Update();
            Assert.That(second.Alive,Is.True);Assert.That(b.Crossover.Boss.Alive,Is.False);Assert.That(b.Result,Is.EqualTo(BattleResult.Ongoing));
            second.Damage(99999,b.Grid);Assert.That(b.Result,Is.EqualTo(BattleResult.Victory));
        }
        [Test] public void OrdoRequiresThreeActualPhasesAndAllReturnConnections()
        {
            var b=new BattleSession(CrossoverTestCatalog(),new[]{0},campaignStage:25);var boss=b.Crossover.Boss;
            for(int phase=2;phase<=3;phase++)
            {
                boss.Damage(99999,b.Grid);Assert.That(boss.BossPhase,Is.EqualTo(phase));Assert.That(boss.Alive,Is.True);
                int hp=boss.CurrentHP;boss.Damage(99999,b.Grid);Assert.That(boss.CurrentHP,Is.EqualTo(hp));
                boss.BeginTurn();boss.Acted=true;boss.EndTurn();
            }
            boss.Damage(99999,b.Grid);Assert.That(b.Result,Is.EqualTo(BattleResult.Ongoing));
            foreach(var u in b.Units.Where(u=>u.Data.Id=="boundary-device"))u.Damage(99999,b.Grid);
            b.Crossover.State.Nodes=7;Assert.That(b.Result,Is.EqualTo(BattleResult.Victory));Assert.That(CampaignStages.HasNext(25),Is.False);
        }
        [Test] public void TwentyEpisodeRewardsProgressIndependentlyFromLegacyAndOptionalWeapons()
        {
            var save=new CampaignSave();var store=new CampaignFile(SaveTestPath());
            for(int stage=6;stage<=25;stage++)
            {
                Assert.That(CampaignStages.Unlocked(save,stage),Is.True);
                Assert.That(CampaignStages.TryReward(save,stage,new[]{data},store.Save),Is.True);save=store.Load();
                Assert.That(save.StoryProgress,Does.Contain(CampaignStages.Id(stage)));
            }
            Assert.That(save.StoryProgress.Count,Is.EqualTo(20));Assert.That(save.StoryProgress.Any(f=>f.StartsWith("personal.")),Is.False);
            Assert.That(save.StoryProgress,Does.Not.Contain("chapter6"));
        }
        [Test] public void ThirtyPersonalRewardsAreAtomicAndGrantTenExclusiveWeaponsOnce()
        {
            var save=new CampaignSave();var store=new CampaignFile(SaveTestPath());
            for(int n=1;n<=16;n++)save.StoryProgress.Add(CrossoverCampaign.Flag(n));
            for(int stage=26;stage<56;stage++)
            {
                Assert.That(PersonalScenarios.Unlocked(save,stage),Is.True);
                var reward=CampaignEconomy.Prepare(save,stage,0);string before=JsonUtility.ToJson(save);
                Assert.That(CampaignStages.TryReward(save,reward,Array.Empty<CharacterData>(),_=>false),Is.False);Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
                Assert.That(CampaignStages.TryReward(save,reward,Array.Empty<CharacterData>(),store.Save),Is.True);save=store.Load();
                Assert.That(CampaignStages.TryReward(save,stage,Array.Empty<CharacterData>(),store.Save),Is.False);
            }
            foreach(var arc in PersonalScenarios.Arcs)Assert.That(CampaignInventory.Owned(save,arc.WeaponId),Is.EqualTo(1));
            Assert.That(save.StoryProgress.Count(f=>f.StartsWith("personal.")),Is.EqualTo(30));
        }
        [Test] public void PersonalTiersUnlockInSixToEightTenToTwelveAndFourteenToSixteen()
        {
            for(int stage=26;stage<56;stage++)
            {
                var save=new CampaignSave();int gate=PersonalScenarios.RequiredEpisode(stage);
                Assert.That(gate,Is.InRange(new[]{6,10,14}[PersonalScenarios.Tier(stage)],new[]{8,12,16}[PersonalScenarios.Tier(stage)]));
                if(PersonalScenarios.Tier(stage)>0)save.StoryProgress.Add(PersonalScenarios.Flag(stage-1));
                Assert.That(PersonalScenarios.Unlocked(save,stage),Is.False);save.StoryProgress.Add(CrossoverCampaign.Flag(gate));Assert.That(PersonalScenarios.Unlocked(save,stage),Is.True);
            }
        }
        [Test] public void TimeStopCannotConsumePhaseProtectionOrSkipTheNewTelegraph()
        {
            var b=new BattleSession(CrossoverTestCatalog(),new[]{0},25,25,25);b.Advance();var boss=b.Crossover.Boss;
            boss.Damage(99999,b.Grid);boss.AddStatus(StatusKind.Stun,1);boss.BeginTurn();boss.EndTurn();Assert.That(boss.BossPhaseLock,Is.True);
            boss.BeginTurn();var plan=CrossoverBossTactics.Plan(b,boss);Assert.That(plan.PrepareSkill,Is.Not.Null);Assert.That(plan.Skill,Is.Null);
            EnemyTactics.Commit(boss,plan);boss.Acted=true;boss.EndTurn();Assert.That(boss.BossPhaseLock,Is.False);Assert.That(boss.IntentPhase,Is.EqualTo(1));
        }
    }
}
