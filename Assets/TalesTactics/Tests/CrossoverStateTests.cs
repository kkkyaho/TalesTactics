using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void SplitFrontReservesKeepHealthAndDoNotEnterTurnOrder(int mode)
        {
            var c=AuthoredCrossoverCatalog();var b=new BattleSession(c,new[]{0,1,2},campaignStage:16,useCT:mode==1,teamTurns:mode==2);
            b.Opening=BattleOpening.Capture(b,c);b.Advance();var east=b.Units[2];
            Assert.That(east.Alive,Is.True);Assert.That(east.CrossoverReserve,Is.True);Assert.That(b.Grid[east.Position].Occupant,Is.Not.SameAs(east));
            Assert.That(b.Scheduler.Preview(b.Units),Has.No.Member(east));
            var restored=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1,2}))).Restore(c);
            Assert.That(restored.Units[2].CrossoverReserve,Is.True);Assert.That(restored.Units[2].CurrentHP,Is.EqualTo(east.CurrentHP));
            b.Crossover.Boss.Damage(99999,b.Grid);b.Crossover.Update();b.EndTurn();
            Assert.That(b.Crossover.State.Segment,Is.EqualTo(1));Assert.That(east.CanTakeTurn,Is.True);Assert.That(b.Units[0].CrossoverReserve,Is.True);
            Assert.That(b.Opening.Restore(c).Units[2].CrossoverReserve,Is.True);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void StaticDevicesRemainTargetableButNeverTakeTurns(int mode)
        {
            var b=new BattleSession(AuthoredCrossoverCatalog(),new[]{0},campaignStage:6,useCT:mode==1,teamTurns:mode==2);
            var devices=b.Units.Where(u=>u.Data.Id=="boundary-device").ToArray();Assert.That(devices.All(u=>u.Alive),Is.True);
            for(int i=0;i<60;i++){b.Advance();Assert.That(b.Active.Data.Id,Is.Not.EqualTo("boundary-device"));b.Active.Acted=true;b.EndTurn();}
            Assert.That(b.Resolver.ValidTarget(b.Units[0],b.Units[0].Data.BasicAttack,devices[0]),Is.True);
        }
        [Test] public void ChosenPersonalRouteRollsBackOnSaveFailureAndPersistsOnce()
        {
            var save=new CampaignSave();int stage=27;
            save.StoryProgress.Add(CrossoverCampaign.Flag(PersonalScenarios.RequiredEpisode(stage)));save.StoryProgress.Add(PersonalScenarios.Flag(stage-1));
            var reward=CampaignEconomy.Prepare(save,stage,9999,1);string flag=CampaignStages.Id(stage)+".route.right";
            Assert.That(CampaignStages.TryReward(save,reward,Array.Empty<CharacterData>(),_=>false),Is.False);Assert.That(save.StoryProgress,Has.No.Member(flag));
            Assert.That(CampaignStages.TryReward(save,reward,Array.Empty<CharacterData>(),_=>true),Is.True);Assert.That(save.StoryProgress.Count(x=>x==flag),Is.EqualTo(1));
            Assert.That(CampaignStages.TryReward(save,reward,Array.Empty<CharacterData>(),_=>true),Is.False);
        }
        [Test] public void TractorBeamReducesMovementWithoutStoppingActionsAndCanBeCleansed()
        {
            var c=AuthoredCrossoverCatalog();var b=new BattleSession(c,new[]{0},campaignStage:16);var boss=b.Crossover.Boss;var hero=b.Units[0];
            b.Grid.Place(hero,new Vector2Int(4,11));int movement=hero.Stats.MOV;boss.BeginTurn();
            Assert.That(b.Resolver.Execute(boss,boss.Data.Skills[0],hero.Position,out _),Is.True);Assert.That(hero.Has(StatusKind.Slow),Is.True);
            Assert.That(hero.Stats.MOV,Is.EqualTo(Math.Max(1,movement-2)));hero.BeginTurn();Assert.That(hero.Acted,Is.False);
            var cleanse=Skill(EffectKind.Cleanse);cleanse.Target=TargetType.Self;cleanse.MinRange=cleanse.Range=0;
            Assert.That(b.Resolver.Execute(hero,cleanse,hero.Position,out _),Is.True);Assert.That(hero.Has(StatusKind.Slow),Is.False);
        }
        [Test] public void ClessContractRequiresCombatCareGuardAndReturnSealInsteadOfBossDeath()
        {
            var c=AuthoredCrossoverCatalog();c.Characters[0].Id="cless";var b=new BattleSession(c,new[]{0},campaignStage:28);var hero=b.Units[0];
            b.Crossover.State.Nodes=7;b.Grid.Place(hero,CrossoverBattle.ContractSeal);
            Assert.That(b.Result,Is.EqualTo(BattleResult.Ongoing));
            b.Crossover.State.TrialProgress=7;Assert.That(b.Result,Is.EqualTo(BattleResult.Victory));Assert.That(b.Crossover.Boss.Alive,Is.True);
        }
        [Test] public void DeviceRepairUsesDestructionOrderAndCannotRepairAnOccupiedLatestDevice()
        {
            var c=AuthoredCrossoverCatalog();var b=new BattleSession(c,new[]{0},campaignStage:25);var devices=b.Units.Where(u=>u.Data.Id=="boundary-device").ToArray();
            devices[2].Damage(99999,b.Grid);b.Crossover.Update();devices[0].Damage(99999,b.Grid);b.Crossover.Update();
            var restored=b.Crossover.State.Copy();Assert.That(restored.BrokenDevices.Last(),Is.EqualTo(b.Units.IndexOf(devices[0])));
            var skill=b.Crossover.Boss.Data.Skills.Single(s=>s.BossMechanic==BossMechanic.Mend);
            b.Grid.Place(b.Units[0],devices[0].Position);
            CrossoverBossTactics.Resolve(b.Crossover.Boss,skill,b.Crossover.Boss.Position,b.Grid,b.Units,b.Crossover.State);
            Assert.That(devices[0].Alive||devices[2].Alive,Is.False);
            b.Grid.Place(b.Units[0],new Vector2Int(0,0));
            CrossoverBossTactics.Resolve(b.Crossover.Boss,skill,b.Crossover.Boss.Position,b.Grid,b.Units,b.Crossover.State);
            Assert.That(devices[0].Alive,Is.True);Assert.That(devices[2].Alive,Is.False);
            Assert.That(b.Crossover.State.BrokenDevices.Last(),Is.EqualTo(b.Units.IndexOf(devices[2])));
        }
        [Test] public void CheckpointCannotResumeAReservedPlayerAsTheActiveUnit()
        {
            var c=AuthoredCrossoverCatalog();var b=new BattleSession(c,new[]{0,1,2},campaignStage:16);b.Advance();
            var cp=BattleCheckpoint.Capture(b,new[]{0,1,2});cp.Active=2;
            Assert.Throws<System.IO.InvalidDataException>(()=>cp.Restore(c));
        }
        [Test] public void OriginContractRequiresWestEastNorthSignalsInOrder()
        {
            var c=AuthoredCrossoverCatalog();c.Characters[0].Id="cless";var b=new BattleSession(c,new[]{0},campaignStage:28);var hero=b.Units[0];
            b.Grid.Place(hero,CrossoverBattle.Nodes[1]);b.Crossover.Update();Assert.That(b.Crossover.State.Nodes,Is.Zero);
            b.Grid.Place(hero,CrossoverBattle.Nodes[0]);b.Crossover.Update();Assert.That(b.Crossover.State.Nodes,Is.EqualTo(1));
            b.Grid.Place(hero,CrossoverBattle.Nodes[1]);b.Crossover.Update();Assert.That(b.Crossover.State.Nodes,Is.EqualTo(3));
            var device=b.Grid[CrossoverBattle.Nodes[2]].Occupant;device.Damage(99999,b.Grid);
            b.Grid.Place(hero,CrossoverBattle.Nodes[2]);b.Crossover.Update();Assert.That(b.Crossover.State.Nodes,Is.EqualTo(7));
        }
        [Test] public void CollapseWarnsThenChangesMovementCostAndRestoresThroughCheckpoint()
        {
            var c=AuthoredCrossoverCatalog();var b=new BattleSession(c,new[]{0},campaignStage:21);b.Advance();var hero=b.Units[0];var boss=b.Crossover.Boss;
            b.Grid.Place(hero,new Vector2Int(5,11));boss.BeginTurn();var skill=boss.Data.Skills.Single(s=>s.BossMechanic==BossMechanic.Collapse);
            Assert.That(b.Resolver.Execute(boss,skill,hero.Position,out _),Is.True);int hp=hero.CurrentHP;int cost=b.Grid[hero.Position].MovementCost;
            b.Crossover.OnTurnEnded(hero);Assert.That(hero.CurrentHP,Is.EqualTo(hp));Assert.That(b.Grid[hero.Position].MovementCost,Is.EqualTo(cost));
            boss.BeginTurn();b.Crossover.OnTurnEnded(boss);Assert.That(hero.CurrentHP,Is.EqualTo(hp-hero.Stats.HP/8));Assert.That(b.Grid[hero.Position].MovementCost,Is.EqualTo(2));
            var restored=BattleCheckpoint.Capture(b,new[]{0}).Restore(c);Assert.That(restored.Grid[hero.Position].MovementCost,Is.EqualTo(2));
            for(int i=0;i<2;i++){boss.BeginTurn();b.Crossover.OnTurnEnded(boss);}Assert.That(b.Grid[hero.Position].MovementCost,Is.EqualTo(cost));
        }
    }
}
