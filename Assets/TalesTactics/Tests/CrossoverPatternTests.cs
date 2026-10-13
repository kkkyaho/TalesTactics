using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        public static string[] AuthoredBossIds=>CrossoverBossSources.Entries.Select(e=>e[0]).ToArray();
        [Test] public void RassiusProtectsUnrescuedCiviliansFromBothLineAttacks()
        {
            var b=new BattleSession(AuthoredCrossoverCatalog(),new[]{0},campaignStage:12);var boss=b.Crossover.Boss;var hero=b.Units.First(u=>u.Team==Team.Player);
            b.Grid.Place(hero,CrossoverBattle.Nodes[0]);boss.BeginTurn();
            for(int pattern=0;pattern<2;pattern++){boss.PatternStep=pattern;Assert.That(CrossoverBossTactics.Plan(b,boss).PrepareSkill,Is.Null);}
            b.Crossover.State.Nodes=7;boss.PatternStep=1;b.Grid.Place(boss,new Vector2Int(2,8));
            Assert.That(CrossoverBossTactics.Plan(b,boss).PrepareSkill,Is.SameAs(boss.Data.Skills[1]));
        }
        BattleCatalog AuthoredCrossoverCatalog()
        {
            var c=CrossoverTestCatalog();
            foreach(var e in c.Enemies.Where(e=>AuthoredBossIds.Contains(e.Id)))
                for(int i=0;i<3;i++)CrossoverSkillRules.Configure(e.Id,i,e.Skills[i]);
            return c;
        }
        [TestCaseSource(nameof(AuthoredBossIds))]
        public void EveryBossCanPrepareAllThreeAuthoredPatternsWithoutMutatingPlanningState(string id)
        {
            var c=AuthoredCrossoverCatalog();int episode=Array.FindIndex(CrossoverCampaign.Episodes,e=>e.Boss==id||e.SecondBoss==id);
            int stage=id=="golem"?31:episode>=0?episode+6:id=="origin"?28:34;
            var b=new BattleSession(c,new[]{0},campaignStage:stage);var boss=b.Units.First(u=>u.Data.Id==id);var hero=b.Units.First(u=>u.Team==Team.Player);
            boss.CurrentHP=boss.Stats.HP;boss.CrossoverBoss=true;boss.CrossoverReserve=false;boss.BossPhase=1;b.Grid.Place(boss,new Vector2Int(6,11));b.Grid.Place(hero,new Vector2Int(5,11));boss.BeginTurn();
            for(int i=0;i<3;i++)
            {
                boss.PatternStep=i;var before=boss.Position;var plan=CrossoverBossTactics.Plan(b,boss);
                Assert.That(plan.PrepareSkill,Is.SameAs(boss.Data.Skills[i]),id+" pattern "+i);
                Assert.That(boss.Position,Is.EqualTo(before));Assert.That(boss.PatternStep,Is.EqualTo(i));Assert.That(boss.IntentPhase,Is.Zero);
            }
        }
        [Test] public void GanabeltClonesAppearAtPreparationAndDefeatingAllCancelsNova()
        {
            var b=new BattleSession(AuthoredCrossoverCatalog(),new[]{0},campaignStage:13);var boss=b.Crossover.Boss;boss.BeginTurn();boss.PatternStep=2;
            b.Grid.Place(b.Units[0],new Vector2Int(5,11));var plan=CrossoverBossTactics.Plan(b,boss);EnemyTactics.Commit(boss,plan,b);
            Assert.That(b.Units.Count(u=>u.Summoned&&u.Alive),Is.EqualTo(3));
            foreach(var clone in b.Units.Where(u=>u.Summoned))clone.Damage(99999,b.Grid);
            boss.IntentPhase=2;Assert.That(b.Resolver.CanUse(boss,plan.PrepareSkill),Does.Contain("취소"));
            boss.IntentPhase=0;Assert.That(CrossoverBossTactics.Plan(b,boss).PrepareSkill,Is.Not.Null);
        }
        [Test] public void SummonCirclesCanBeOccupiedAndDoNotMoveAfterMinionDeath()
        {
            var b=new BattleSession(AuthoredCrossoverCatalog(),new[]{0},campaignStage:11);var boss=b.Crossover.Boss;var skill=boss.Data.Skills[1];
            var circle=CrossoverCampaign.EnemySpawn(6);b.Grid.Place(b.Units[0],circle);
            CrossoverBossTactics.Resolve(boss,skill,boss.Position,b.Grid,b.Units);
            Assert.That(b.Units.Count(u=>u.Summoned&&u.Alive),Is.EqualTo(1));
            var minion=b.Units.First(u=>u.Summoned&&u.Alive);b.Grid.Place(minion,new Vector2Int(1,5));minion.Damage(99999,b.Grid);
            CrossoverBossTactics.Resolve(boss,skill,boss.Position,b.Grid,b.Units);
            Assert.That(minion.Position,Is.EqualTo(CrossoverCampaign.EnemySpawn(7)));
        }
        [Test] public void OrdoReplayLocksPatternAtPreparationAndRestoresIt()
        {
            var c=AuthoredCrossoverCatalog();var b=new BattleSession(c,new[]{0},campaignStage:25);b.Opening=BattleOpening.Capture(b,c);b.Advance();var boss=b.Crossover.Boss;
            b.Crossover.State.LastShape=(int)SkillAreaShape.Cross;b.Crossover.State.LastArea=2;
            EnemyTactics.Commit(boss,new EnemyPlan{PrepareSkill=boss.Data.Skills[0],PrepareAim=new Vector2Int(6,8)},b);
            var area=b.Resolver.AreaTiles(boss,boss.Data.Skills[0],boss.IntentAim).ToArray();
            b.Crossover.State.LastShape=(int)SkillAreaShape.Line;b.Crossover.State.LastArea=0;
            Assert.That(b.Resolver.AreaTiles(boss,boss.Data.Skills[0],boss.IntentAim),Is.EquivalentTo(area));
            var restored=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0}))).Restore(c);
            Assert.That(restored.Crossover.State.ReplayShape,Is.EqualTo((int)SkillAreaShape.Cross));Assert.That(restored.Crossover.State.ReplayArea,Is.EqualTo(2));
        }
        [Test] public void HazardPersistenceKeepsRoutesOpenAndRejectsInvalidOwners()
        {
            var c=AuthoredCrossoverCatalog();var b=new BattleSession(c,new[]{0},campaignStage:20);b.Opening=BattleOpening.Capture(b,c);b.Advance();var boss=b.Crossover.Boss;
            var skill=boss.Data.Skills[0];b.Crossover.OnResolved(boss,skill,new Vector2Int(6,8));
            Assert.That(b.Crossover.State.Hazards.Count,Is.GreaterThan(0));
            Assert.That(CrossoverBattle.Nodes.All(p=>b.Grid[p].Walkable),Is.True);Assert.That(b.Grid[CrossoverBattle.Exit].Walkable,Is.True);
            var cp=BattleCheckpoint.Capture(b,new[]{0});var restored=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(cp)).Restore(c);
            Assert.That(restored.Crossover.State.Hazards.Count,Is.EqualTo(b.Crossover.State.Hazards.Count));
            cp.Crossover.Hazards[0].Owner=999;Assert.Throws<System.IO.InvalidDataException>(()=>cp.Restore(c));
        }
        [Test] public void VholranWaveLeavesRaisedTilesSafeAndShizelSignalsCreateSanctuaries()
        {
            var b=new BattleSession(AuthoredCrossoverCatalog(),new[]{0},campaignStage:22);var boss=b.Crossover.Boss;var hero=b.Units[0];b.Grid.Place(hero,new Vector2Int(5,10));
            Assert.That(b.Resolver.Targets(boss,boss.Data.Skills[0],hero.Position),Has.No.Member(hero));
            var skill=New<SkillData>();CrossoverSkillRules.Configure("shizel",2,skill);
            Assert.That(b.Resolver.Targets(boss,skill,hero.Position),Has.Member(hero));b.Crossover.State.Nodes=4;
            Assert.That(b.Resolver.Targets(boss,skill,hero.Position),Has.No.Member(hero));
        }
    }
}
