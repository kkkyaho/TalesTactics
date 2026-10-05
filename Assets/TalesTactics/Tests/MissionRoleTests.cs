using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [TestCase(false)] [TestCase(true)]
        public void RangedEnemyKeepsAttackDistanceWithoutChangingWorld(bool utility)
        {
            var catalog=New<BattleCatalog>();catalog.Rules=rules;catalog.Characters=new[]{data};
            var ranged=New<CharacterData>();ranged.Id="archer";ranged.BaseStats=data.BaseStats;ranged.Skills=new SkillData[0];ranged.BasicAttack=Skill(EffectKind.Damage);ranged.BasicAttack.Range=3;catalog.Enemy=ranged;
            data.BasicAttack=Skill(EffectKind.Damage);data.BasicAttack.Range=1;
            var b=new BattleSession(catalog,new[]{0},campaignStage:0,utilityAI:utility);
            foreach(var tile in b.Grid.Tiles.Values){tile.Height=0;tile.Walkable=true;tile.MovementCost=1;}
            var actor=b.Units.First(u=>u.Team==Team.Enemy);foreach(var other in b.Units.Where(u=>u.Team==Team.Enemy&&u!=actor))other.Damage(99999,b.Grid);
            var target=b.Units[0];Assert.That(b.Grid.Place(target,new Vector2Int(4,3)),Is.True);Assert.That(b.Grid.Place(actor,new Vector2Int(5,3)),Is.True);
            var before=JsonUtility.ToJson(new CheckpointUnit(actor));var rng=JsonUtility.ToJson(Random.state);var plan=new EnemyPlanner().Plan(b,actor);
            Assert.That(plan.Skill,Is.SameAs(ranged.BasicAttack));Assert.That(GridMap.Distance(plan.Destination,target.Position),Is.EqualTo(3));
            Assert.That(JsonUtility.ToJson(new CheckpointUnit(actor)),Is.EqualTo(before));Assert.That(b.Grid[actor.Position].Occupant,Is.SameAs(actor));Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(rng));
            actor.AddStatus(StatusKind.Root,2);plan=new EnemyPlanner().Plan(b,actor);Assert.That(plan.Destination,Is.EqualTo(actor.Position));
        }
        [Test] public void RoleBonusesExcludeTrainingAndPlayerAndFavorWoundedTargets()
        {
            var catalog=New<BattleCatalog>();catalog.Rules=rules;catalog.Characters=new[]{data};catalog.Enemy=data;data.Id="wolf";data.BasicAttack=Skill(EffectKind.Damage);
            var campaign=new BattleSession(catalog,new[]{0},campaignStage:0);var actor=campaign.Units.First(u=>u.Team==Team.Enemy);var target=campaign.Units[0];
            float full=EnemyRoles.TargetScore(campaign,actor,target);target.CurrentHP/=2;
            Assert.That(EnemyRoles.TargetScore(campaign,actor,target),Is.GreaterThan(full));Assert.That(EnemyRoles.TargetScore(campaign,target,actor),Is.Zero);
            var training=new BattleSession(catalog,new[]{0});Assert.That(EnemyRoles.TargetScore(training,actor,target),Is.Zero);
        }
        [Test] public void MissionProgressUsesLivingEnemiesAndObjectiveRules()
        {
            var b=Session();Assert.That(MissionBriefing.Progress(b),Is.EqualTo("적 잔존 4 / 4"));b.Units.Last().Damage(99999,b.Grid);
            Assert.That(MissionBriefing.Progress(b),Is.EqualTo("적 잔존 3 / 4"));Assert.That(MissionBriefing.Victory(b),Is.EqualTo("모든 적 격파"));
            var catalog=New<BattleCatalog>();catalog.Rules=rules;catalog.Characters=new[]{data};catalog.Enemy=data;
            foreach(ObjectiveKind kind in System.Enum.GetValues(typeof(ObjectiveKind)))
            {var s=new BattleSession(catalog,new[]{0},objective:kind);Assert.That(MissionBriefing.Victory(s),Is.Not.Empty);Assert.That(MissionBriefing.Defeat(s),Does.Contain("전투불능"));Assert.That(MissionBriefing.Progress(s),Is.Not.Empty);}
        }
    }
}
