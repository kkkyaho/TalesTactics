using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        BattleCatalog TacticsCatalog(){var c=ValidCatalog();c.Equipment=new EquipmentData[0];data.Skills=new SkillData[0];return c;}
        [Test] public void ForecastMatchesActualAndNeverMutatesBattle()
        {
            var b=new BattleSession(TacticsCatalog(),new[]{0});var a=b.Units[0];var t=b.Units[1];b.Grid.Place(t,a.Position+Vector2Int.right);
            var s=Skill(EffectKind.Damage);s.MPCost=13;s.HPCost=4;s.Effects=new[]{new SkillEffect{Kind=EffectKind.Damage,Power=2},new SkillEffect{Kind=EffectKind.Damage,Power=3}};
            var before=b.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u))).ToArray();var rng=JsonUtility.ToJson(Random.state);
            var forecast=BattleForecast.Create(b,a,s,t.Position);BattleForecast.Create(b,a,s,t.Position);
            Assert.That(b.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u))),Is.EqualTo(before));Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(rng));
            Assert.That(b.Grid[t.Position].Occupant,Is.SameAs(t));Assert.That(b.Resolver.Execute(a,s,t.Position,out _),Is.True);
            foreach(var row in forecast.Rows)Assert.That(row.AfterHP,Is.EqualTo(row.Unit.CurrentHP));Assert.That(a.CurrentMP,Is.EqualTo(a.Stats.MP-13));
        }
        [Test] public void ForecastCapsHealingAndSeparatesChance()
        {
            var b=new BattleSession(TacticsCatalog(),new[]{0});var a=b.Units[0];a.CurrentHP-=7;var s=Skill(EffectKind.Heal,TargetType.Self);s.Effects[0].Power=100;
            var f=BattleForecast.Create(b,a,s,a.Position);Assert.That(f.Rows.Single().AfterHP-f.Rows.Single().BeforeHP,Is.EqualTo(7));
            s.Effects[0].Chance=.5f;f=BattleForecast.Create(b,a,s,a.Position);Assert.That(f.Rows.Single().AfterHP,Is.EqualTo(a.CurrentHP));Assert.That(f.Rows.Single().Effects,Does.Contain("50"));
        }
        [Test] public void ProtectionDoesNotStackAndConsumesOneCharge()
        {
            var cover=new UnitRuntime(data,Team.Enemy,rules){Trait=TacticalTrait.Protector};grid.Place(cover,new Vector2Int(2,2));cover.AddStatus(StatusKind.Guard,2);
            var second=new UnitRuntime(data,Team.Enemy,rules){Trait=TacticalTrait.Protector};grid.Place(second,new Vector2Int(3,1));second.AddStatus(StatusKind.Guard,2);
            var r=new SkillResolver(grid,new[]{player,enemy,cover,second},rules);var s=Skill(EffectKind.Damage);int ordinary=resolver.DamagePreview(player,enemy,s.Effects[0],s);
            Assert.That(r.DamagePreview(player,enemy,s.Effects[0],s),Is.EqualTo(Mathf.RoundToInt(ordinary*.7f)).Within(1));Assert.That(r.Execute(player,s,enemy.Position,out _),Is.True);
            Assert.That(cover.ProtectionUsed,Is.True);Assert.That(second.ProtectionUsed,Is.False);cover.BeginTurn();Assert.That(cover.ProtectionUsed,Is.False);Assert.That(cover.Has(StatusKind.Guard),Is.False);
        }
        [Test] public void TraitGearCostAndHealingUseActualModifiers()
        {
            player.Trait=TacticalTrait.Focus;var gear=New<EquipmentData>();gear.Effect=EquipmentEffect.EfficientCasting;player.Equipment[2]=gear;
            var s=Skill(EffectKind.Damage);s.MPCost=13;int cost=resolver.MPCost(player,s);Assert.That(cost,Is.EqualTo(9));player.CurrentMP=cost;
            Assert.That(resolver.Execute(player,s,enemy.Position,out _),Is.True);Assert.That(player.CurrentMP,Is.Zero);
            player.Trait=TacticalTrait.Healer;gear.Effect=EquipmentEffect.Restorative;Assert.That(resolver.Healing(player,new SkillEffect{Power=1}),Is.EqualTo(Mathf.RoundToInt(player.Stats.MAG*1.5f)));
        }
        [Test] public void MasteryUnlocksWithoutLevelChangeAndTrialIsOnce()
        {
            var save=new CampaignSave();save.StoryProgress.Add("chapter2");player.Level=1;TacticalDevelopment.Apply(player,save,5);
            var s=Skill(EffectKind.Damage);s.UnlockLevel=13;Assert.That(player.Unlocked(s),Is.True);s.UnlockLevel=14;Assert.That(player.Unlocked(s),Is.False);
            s.IsUltimate=true;s.UnlockLevel=20;s.GaugeCost=50;Assert.That(resolver.Execute(player,s,enemy.Position,out _),Is.True);player.BeginTurn();Assert.That(player.Unlocked(s),Is.False);Assert.That(player.Level,Is.EqualTo(1));
        }
        [Test] public void CatchUpAndTraitsRollbackFailedPersistence()
        {
            data.Skills=new SkillData[0];var save=new CampaignSave();save.StoryProgress.AddRange(new[]{"chapter1","chapter2","chapter3","chapter4","chapter5"});var p=save.Get(data.Id);p.EXP=55;
            Assert.That(TacticalDevelopment.CatchUp(save,data,5,_=>false),Is.False);Assert.That(p.Level,Is.EqualTo(1));Assert.That(p.EXP,Is.EqualTo(55));
            Assert.That(TacticalDevelopment.SaveTrait(save,data,TacticalTrait.Focus,_=>false),Is.False);Assert.That(p.Trait,Is.EqualTo(TacticalTrait.Balanced));
            Assert.That(TacticalDevelopment.CatchUp(save,data,5,_=>true),Is.True);Assert.That(p.Level,Is.EqualTo(7));Assert.That(TacticalDevelopment.CatchUp(save,data,0,_=>true),Is.False);
        }
        [TestCase(0,ObjectiveKind.Eliminate)] [TestCase(1,ObjectiveKind.Boss)] [TestCase(2,ObjectiveKind.Reach)]
        [TestCase(3,ObjectiveKind.Survive)] [TestCase(4,ObjectiveKind.Escort)] [TestCase(5,ObjectiveKind.Boss)]
        public void CampaignMissionUsesChapterObjective(int stage,ObjectiveKind expected)
        {var b=new BattleSession(TacticsCatalog(),new[]{0},campaignStage:stage);Assert.That(b.Objective,Is.EqualTo(expected));if(expected==ObjectiveKind.Reach||expected==ObjectiveKind.Escort)Assert.That(b.Grid[b.Destination].Walkable,Is.True);}
        [Test] public void CheckpointPreservesMissionAndLegacyVictory()
        {
            var c=TacticsCatalog();var b=new BattleSession(c,new[]{0},campaignStage:3);b.Advance();Assert.That(b.Active.Team,Is.EqualTo(Team.Player));b.Active.Trait=TacticalTrait.Focus;
            ((SurviveTurns)b.Victory).OnTurnEnded(b.Active);var cp=BattleCheckpoint.Capture(b,new[]{0});var read=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(cp)).Restore(c);
            Assert.That(((SurviveTurns)read.Victory).Completed,Is.EqualTo(1));Assert.That(read.Active.Trait,Is.EqualTo(TacticalTrait.Focus));
            cp.Version=1;cp.SurvivalTurns=0;Assert.That(cp.Restore(c).Objective,Is.EqualTo(ObjectiveKind.Eliminate));
        }
        [Test] public void TelegraphIsPureAndDoesNotFollowEvadingTarget()
        {
            var c=TacticsCatalog();data.Id="dhaos";var b=new BattleSession(c,new[]{0},campaignStage:5);var u=b.Units[1];var target=b.Units[0];
            foreach(var x in b.Units.Skip(2))x.Damage(99999,b.Grid);b.Grid.Place(u,new Vector2Int(3,3));b.Grid.Place(target,new Vector2Int(3,2));u.BeginTurn();
            var before=JsonUtility.ToJson(new CheckpointUnit(u));var plan=new EnemyPlanner().Plan(b,u);Assert.That(plan.PrepareSkill,Is.Not.Null);Assert.That(JsonUtility.ToJson(new CheckpointUnit(u)),Is.EqualTo(before));
            EnemyTactics.Commit(u,plan);b.Grid.Place(u,plan.Destination);var aim=u.IntentAim;b.Grid.Place(target,new Vector2Int(1,1));u.EndTurn();u.BeginTurn();
            var strike=new EnemyPlanner().Plan(b,u);Assert.That(strike.Aim,Is.EqualTo(aim));EnemyTactics.Commit(u,strike);int hp=target.CurrentHP;Assert.That(b.Resolver.Execute(u,strike.Skill,aim,out _),Is.True);Assert.That(target.CurrentHP,Is.EqualTo(hp));
            u.EndTurn();u.BeginTurn();Assert.That(new EnemyPlanner().Plan(b,u).Skill,Is.Null);Assert.That(u.IntentPhase,Is.EqualTo(2));u.EndTurn();u.BeginTurn();Assert.That(u.IntentPhase,Is.Zero);
        }
        [Test] public void ThreatQueryIncludesMovementWithoutMutatingUnits()
        {
            var b=new BattleSession(TacticsCatalog(),new[]{0});var snapshots=b.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u))).ToArray();var map=ThreatMap.Calculate(b);
            Assert.That(map.Count,Is.EqualTo(4));Assert.That(map.Values.All(c=>c.Count>0),Is.True);Assert.That(b.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u))),Is.EqualTo(snapshots));
            foreach(var u in b.Units)Assert.That(b.Grid[u.Position].Occupant,Is.SameAs(u));
        }
        [Test] public void OldSettingsDefaultToNormalTextSize()
        {var p=JsonUtility.FromJson<PlayerPreferences>("{\"Version\":1}");Assert.That(p.TextScale,Is.EqualTo(1));p.Validate();p.TextScale=float.NaN;Assert.Throws<System.IO.InvalidDataException>(()=>p.Validate());}
    }
}
