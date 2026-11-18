using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        public static string[] AuthoredBossIds=>CrossoverBossSources.Entries.Select(e=>e[0]).ToArray();
        static void PlaceCrossoverPatternTarget(BattleSession b,UnitRuntime anchor,UnitRuntime target)
        {
            var tile=b.Grid.Neighbors(anchor.Position).FirstOrDefault(t=>t.Walkable&&(t.Occupant==null||t.Occupant==target)
                &&Math.Abs(t.Height-b.Grid[anchor.Position].Height)<=1&&!b.Crossover.SignalPositions.Contains(t.Coordinate)&&t.Coordinate!=b.Crossover.ExitPosition);
            Assert.That(tile,Is.Not.Null,"No adjacent non-objective tile for the pattern fixture");
            Assert.That(b.Grid.Place(target,tile.Coordinate),Is.True);
        }
        [Test] public void RassiusProtectsUnrescuedCiviliansFromBothLineAttacks()
        {
            var b=new BattleSession(AuthoredCrossoverCatalog(),new[]{0},campaignStage:12);var boss=b.Crossover.Boss;var hero=b.Units.First(u=>u.Team==Team.Player);
            Assert.That(b.Grid.Place(hero,b.Crossover.SignalPositions[0]),Is.True);boss.BeginTurn();
            for(int pattern=0;pattern<2;pattern++){boss.PatternStep=pattern;Assert.That(CrossoverBossTactics.Plan(b,boss).PrepareSkill,Is.Null);}
            b.Crossover.State.Nodes=b.Crossover.RequiredNodeMask;boss.PatternStep=1;PlaceCrossoverPatternTarget(b,hero,boss);
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
            boss.CurrentHP=boss.Stats.HP;boss.CrossoverBoss=true;boss.CrossoverReserve=false;boss.BossPhase=1;
            Assert.That(b.Grid.Place(boss,boss.Position),Is.True);PlaceCrossoverPatternTarget(b,boss,hero);
            if(id=="rassius")b.Crossover.State.Nodes=b.Crossover.RequiredNodeMask; // Civilian protection has its own negative test above.
            boss.BeginTurn();
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
            PlaceCrossoverPatternTarget(b,boss,b.Units[0]);var plan=CrossoverBossTactics.Plan(b,boss);EnemyTactics.Commit(boss,plan,b);
            Assert.That(b.Units.Count(u=>u.Summoned&&u.Alive),Is.EqualTo(3));
            foreach(var clone in b.Units.Where(u=>u.Summoned))clone.Damage(99999,b.Grid);
            boss.IntentPhase=2;Assert.That(b.Resolver.CanUse(boss,plan.PrepareSkill),Does.Contain("취소"));
            boss.IntentPhase=0;Assert.That(CrossoverBossTactics.Plan(b,boss).PrepareSkill,Is.Not.Null);
        }
        [Test] public void SummonCirclesCanBeOccupiedAndDoNotMoveAfterMinionDeath()
        {
            var b=new BattleSession(AuthoredCrossoverCatalog(),new[]{0},campaignStage:11);var boss=b.Crossover.Boss;var skill=boss.Data.Skills[1];
            var circle=b.Crossover.EnemySpawnPositions[6];Assert.That(b.Grid.Place(b.Units[0],circle),Is.True);
            boss.BeginTurn();Assert.That(b.Resolver.Execute(boss,skill,boss.Position,out _),Is.True);
            Assert.That(b.Units.Count(u=>u.Summoned&&u.Alive),Is.EqualTo(1));
            var minion=b.Units.First(u=>u.Summoned&&u.Alive);Assert.That(b.Grid.Place(minion,new Vector2Int(0,0)),Is.True);minion.Damage(99999,b.Grid);
            boss.BeginTurn();Assert.That(b.Resolver.Execute(boss,skill,boss.Position,out _),Is.True);
            Assert.That(minion.Position,Is.EqualTo(b.Crossover.EnemySpawnPositions[7]));
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
            var skill=boss.Data.Skills[0];b.Crossover.OnResolved(boss,skill,b.Crossover.SignalPositions[0]);
            Assert.That(b.Crossover.State.Hazards.Count,Is.GreaterThan(0));
            Assert.That(b.Crossover.SignalPositions.All(p=>b.Grid[p].Walkable),Is.True);Assert.That(b.Grid[b.Crossover.ExitPosition].Walkable,Is.True);
            var cp=BattleCheckpoint.Capture(b,new[]{0});var restored=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(cp)).Restore(c);
            Assert.That(restored.Crossover.State.Hazards.Count,Is.EqualTo(b.Crossover.State.Hazards.Count));
            cp.Crossover.Hazards[0].Owner=999;Assert.Throws<System.IO.InvalidDataException>(()=>cp.Restore(c));
        }
        [Test] public void VholranWaveLeavesRaisedTilesSafeAndShizelSignalsCreateSanctuaries()
        {
            var b=new BattleSession(AuthoredCrossoverCatalog(),new[]{0},campaignStage:22);var boss=b.Crossover.Boss;var hero=b.Units[0];
            Assert.That(b.Grid.Place(hero,new Vector2Int(9,10)),Is.True);Assert.That(b.Grid[hero.Position].Height,Is.GreaterThan(0));
            Assert.That(b.Resolver.Targets(boss,boss.Data.Skills[0],hero.Position),Has.No.Member(hero));
            Assert.That(b.Grid.Place(hero,new Vector2Int(7,10)),Is.True);Assert.That(b.Grid[hero.Position].Height,Is.Zero);
            Assert.That(b.Resolver.Targets(boss,boss.Data.Skills[0],hero.Position),Has.Member(hero),"Low-ground control must be exposed to the same wave");
            var sanctuary=new BattleSession(AuthoredCrossoverCatalog(),new[]{0},campaignStage:23);var resident=sanctuary.Units[0];var shizel=sanctuary.Crossover.Boss;
            Assert.That(sanctuary.Grid.Place(resident,sanctuary.Crossover.SignalPositions[2]),Is.True);
            var skill=shizel.Data.Skills.Single(s=>s.BossMechanic==BossMechanic.Sanctuary);
            Assert.That(sanctuary.Resolver.Targets(shizel,skill,resident.Position),Has.Member(resident));sanctuary.Crossover.State.Nodes=1;
            Assert.That(sanctuary.Resolver.Targets(shizel,skill,resident.Position),Has.Member(resident),"A distant connected signal does not protect this resident");
            sanctuary.Crossover.State.Nodes=4;Assert.That(sanctuary.Resolver.Targets(shizel,skill,resident.Position),Has.No.Member(resident));
        }
    }
}
