using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [Test] public void CTPreviewMatchesNextWithoutMutatingAndFasterUnitsActMore()
        {
            var fast=New<CharacterData>();fast.BaseStats=data.BaseStats;fast.BaseStats.SPD=40;
            var quick=new UnitRuntime(fast,Team.Player,rules);var list=new[]{quick,enemy};var ct=new CTTurnScheduler();
            var preview=ct.Preview(list).ToArray();Assert.That(ct.Preview(list),Is.EqualTo(preview));
            foreach(var expected in preview)Assert.That(ct.Next(list),Is.SameAs(expected));
            int count=0;for(int i=0;i<90;i++)if(ct.Next(list)==quick)count++;
            Assert.That(count,Is.EqualTo(60));
            fast.BaseStats.SPD=5;count=0;for(int i=0;i<50;i++)if(ct.Next(list)==quick)count++;
            Assert.That(count<15,Is.True,"Speed changes must affect subsequent CT ticks");
        }
        [Test] public void CTHandlesKORevivalZeroSpeedAndDynamicSpeed()
        {
            var ct=new CTTurnScheduler();var list=new[]{player,enemy};
            Assert.That(ct.Next(list),Is.SameAs(player));enemy.CurrentHP=0;
            for(int i=0;i<3;i++)Assert.That(ct.Next(list),Is.SameAs(player));
            enemy.CurrentHP=100;data.BaseStats.SPD=0;
            Assert.That(ct.Preview(list).Count,Is.EqualTo(8));
            Assert.That(ct.Preview(list).Contains(enemy),Is.True);
            player.CurrentHP=enemy.CurrentHP=0;Assert.That(ct.Next(list),Is.Null);
        }
        [Test] public void BossVictoryIgnoresAddsButPartyDefeatHasPriority()
        {
            var adds=new UnitRuntime(data,Team.Enemy,rules);var condition=new DefeatBoss(enemy);var list=new[]{player,enemy,adds};
            Assert.That(condition.Evaluate(list),Is.EqualTo(BattleResult.Ongoing));enemy.CurrentHP=0;
            Assert.That(condition.Evaluate(list),Is.EqualTo(BattleResult.Victory));player.CurrentHP=0;
            Assert.That(condition.Evaluate(list),Is.EqualTo(BattleResult.Defeat));
        }
        [Test] public void ReachAndEscortRequireLivingCorrectUnitAndDoNotUseElimination()
        {
            var goal=new Vector2Int(8,8);var ally=new UnitRuntime(data,Team.Player,rules);var list=new[]{player,ally,enemy};
            enemy.CurrentHP=0;var reach=new ReachDestination(goal);var escort=new ReachDestination(goal,ally);
            Assert.That(reach.Evaluate(list),Is.EqualTo(BattleResult.Ongoing));player.Position=goal;
            Assert.That(reach.Evaluate(list),Is.EqualTo(BattleResult.Victory));Assert.That(escort.Evaluate(list),Is.EqualTo(BattleResult.Ongoing));
            ally.Position=goal;Assert.That(escort.Evaluate(list),Is.EqualTo(BattleResult.Victory));ally.CurrentHP=0;
            Assert.That(escort.Evaluate(list),Is.EqualTo(BattleResult.Defeat));
        }
        [Test] public void SurvivalCountsOnlyLivingPlayerTurnsAndDefeatWinsTies()
        {
            var survive=new SurviveTurns(2);var list=new[]{player,enemy};survive.OnTurnEnded(enemy);
            Assert.That(survive.Completed,Is.Zero);survive.OnTurnEnded(player);
            Assert.That(survive.Evaluate(list),Is.EqualTo(BattleResult.Ongoing));survive.OnTurnEnded(player);
            Assert.That(survive.Evaluate(list),Is.EqualTo(BattleResult.Victory));player.CurrentHP=0;
            Assert.That(survive.Evaluate(list),Is.EqualTo(BattleResult.Defeat));
        }
        BattleSession TacticalSession(params SkillData[] skills)
        {
            data.BasicAttack=Skill(EffectKind.Damage);data.BasicAttack.Id="basic";data.BasicAttack.Range=1;data.Skills=skills;
            var catalog=New<BattleCatalog>();catalog.Rules=rules;catalog.Characters=new[]{data};catalog.Enemy=data;
            var session=new BattleSession(catalog,new[]{0},25,25,-1,false,true);
            session.Active=session.Units[0];session.Active.Moved=true;return session;
        }
        [Test] public void UtilityChoosesAreaDamageAndPlanningPreservesWorldAndResources()
        {
            var skill=Skill(EffectKind.Damage);skill.Area=1;skill.MPCost=8;
            var session=TacticalSession(skill);var actor=session.Active;var origin=actor.Position;int mp=actor.CurrentMP;
            session.Grid.Place(session.Units[1],new Vector2Int(2,1));session.Grid.Place(session.Units[2],new Vector2Int(3,1));
            var plan=new EnemyPlanner().Plan(session,actor);
            Assert.That(plan.Skill,Is.SameAs(skill));Assert.That(plan.Aim.HasValue,Is.True);
            Assert.That(actor.Position,Is.EqualTo(origin));Assert.That(session.Grid[origin].Occupant,Is.SameAs(actor));Assert.That(actor.CurrentMP,Is.EqualTo(mp));
            Assert.That(session.Resolver.Targets(actor,plan.Skill,plan.Aim.Value).Count(),Is.EqualTo(2));
            Assert.That(session.Resolver.Execute(actor,plan.Skill,plan.Aim.Value,out _),Is.True);
        }
        [Test] public void UtilityHealsCriticalAllyAndAvoidsFullHealthHealing()
        {
            var heal=Skill(EffectKind.Heal,TargetType.Ally);heal.Effects[0].Flat=150;heal.MPCost=10;
            var session=TacticalSession(heal);session.Active.CurrentHP=10;
            var plan=new EnemyPlanner().Plan(session,session.Active);Assert.That(plan.Skill,Is.SameAs(heal));
            session.Active.CurrentHP=session.Active.Stats.HP;
            Assert.That(new EnemyPlanner().Plan(session,session.Active).Skill,Is.Not.SameAs(heal));
        }
        [Test] public void UtilityRevivesOnlyUnoccupiedTilesAndRespectsCostsAndGates()
        {
            var revive=Skill(EffectKind.Revive,TargetType.FallenAlly);revive.Effects[0].Power=0.5f;revive.MPCost=12;
            var session=TacticalSession(revive);var ally=new UnitRuntime(data,Team.Player,rules);session.Units.Add(ally);
            session.Grid.Place(ally,new Vector2Int(2,1));ally.Damage(99999,session.Grid);
            Assert.That(new EnemyPlanner().Plan(session,session.Active).Skill,Is.SameAs(revive));
            session.Grid.Place(session.Units[1],ally.Position);
            Assert.That(new EnemyPlanner().Plan(session,session.Active).Skill,Is.Not.SameAs(revive));
            session.Grid.Place(session.Units[1],new Vector2Int(3,1));session.Active.CurrentMP=0;
            Assert.That(new EnemyPlanner().Plan(session,session.Active).Skill,Is.Not.SameAs(revive));
            session.Active.CurrentMP=120;revive.Gate=SkillGate.Claw;
            Assert.That(new EnemyPlanner().Plan(session,session.Active).Skill,Is.Not.SameAs(revive));
        }
        [Test] public void UtilityCannotAttackThroughBlockedSightAndGuardsWhenRooted()
        {
            var shot=Skill(EffectKind.Damage);shot.RequiresLineOfSight=true;
            var session=TacticalSession(shot);foreach(var t in session.Grid.Tiles.Values)if(t.Coordinate!=session.Active.Position)t.Walkable=false;
            var plan=new EnemyPlanner().Plan(session,session.Active);Assert.That(plan.Skill,Is.Null);Assert.That(plan.Guard,Is.True);
        }
        [Test] public void SurvivalSessionEndTurnIsIdempotent()
        {
            data.BasicAttack=Skill(EffectKind.Damage);var catalog=New<BattleCatalog>();catalog.Rules=rules;catalog.Characters=new[]{data};catalog.Enemy=data;
            var session=new BattleSession(catalog,new[]{0},1,1,-1,true,false,ObjectiveKind.Survive);
            session.Advance();session.EndTurn();session.EndTurn();
            Assert.That(((SurviveTurns)session.Victory).Completed,Is.EqualTo(1));
        }
    }
}
