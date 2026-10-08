using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [TestCase(1)] [TestCase(3)]
        public void PhaseSurvivalCountsEnemyPhasesRegardlessOfPartySize(int count)
        {
            var b=new BattleSession(FreeTurnCatalog(),Enumerable.Range(0,count),teamTurns:true,phaseSurvival:true,objective:ObjectiveKind.Survive);b.Advance();
            var goal=(SurviveTurns)b.Victory;
            for(int round=0;round<4;round++)
            {
                Assert.That(b.EndPlayerPhase(),Is.True);Assert.That(goal.Completed,Is.EqualTo(round));b.Advance();
                for(int enemy=0;enemy<4;enemy++){b.EndTurn();b.EndTurn();if(enemy<3)b.Advance();}
                Assert.That(goal.Completed,Is.EqualTo(round+1));if(round<3)b.Advance();
            }
            Assert.That(b.Result,Is.EqualTo(BattleResult.Victory));
        }
        [Test] public void BulkEndDoesNotRestartSelectedUnitsOrChangeFacing()
        {
            var b=new BattleSession(FreeTurnCatalog(),new[]{0,1,2},teamTurns:true);b.Advance();var a=b.Active;
            a.Facing=Facing.Left;a.AddStatus(StatusKind.Song,3);a.Cooldowns["a"]=4;b.Select(b.Units[1]);
            Assert.That(b.EndPlayerPhase(),Is.True);Assert.That(b.EndPlayerPhase(),Is.False);
            Assert.That(a.TurnsStarted,Is.EqualTo(1));Assert.That(a.Cooldowns["a"],Is.EqualTo(4));Assert.That(a.Statuses.Single().Turns,Is.EqualTo(2));Assert.That(a.Facing,Is.EqualTo(Facing.Left));
            Assert.That(b.Units.Take(3).All(u=>!u.Has(StatusKind.Guard)),Is.True);b.Advance();Assert.That(b.Active.Team,Is.EqualTo(Team.Enemy));
        }
        [Test] public void NewRoundActionStateIgnoresPreviousRoundFlags()
        {
            var b=new BattleSession(FreeTurnCatalog(),new[]{0,1,2},teamTurns:true);b.Advance();b.Units[2].Moved=b.Units[2].Acted=true;
            Assert.That(b.ActionState(b.Units[2]),Is.EqualTo("이동·행동"));b.Select(b.Units[2]);b.Active.Moved=true;Assert.That(b.ActionState(b.Active),Is.EqualTo("행동"));b.Active.Moved=false;b.Active.Acted=true;b.Active.FlamingChain=true;Assert.That(b.ActionState(b.Active),Is.EqualTo("이동·행동"));
        }
        [Test] public void TacticalCheckpointRetainsNewRulesAndLegacyCheckpointsKeepOldRules()
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0,1,2},campaignStage:3,teamTurns:true,phaseSurvival:true,tacticalCombat:true);b.Advance();
            b.Active.Growth=GrowthPath.Assault;var cp=BattleCheckpoint.Capture(b,new[]{0,1,2});var restored=cp.Restore(c);
            Assert.That(restored.TacticalCombat,Is.True);Assert.That(((SurviveTurns)restored.Victory).Required,Is.EqualTo(4));Assert.That(restored.Active.Growth,Is.EqualTo(GrowthPath.Assault));
            cp.Version=3;restored=cp.Restore(c);Assert.That(restored.TacticalCombat,Is.False);Assert.That(((SurviveTurns)restored.Victory).Required,Is.EqualTo(12));
        }
        [Test] public void PincerRequiresOppositeLivingUnstunnedAllyAndForecastMatches()
        {
            var b=new BattleSession(FreeTurnCatalog(),new[]{0,1},teamTurns:true,tacticalCombat:true);b.Advance();var a=b.Active;var target=b.Units.First(u=>u.Team==Team.Enemy);var ally=b.Units[1];
            b.Grid.Place(a,new Vector2Int(1,1));b.Grid.Place(ally,new Vector2Int(3,1));b.Grid.Place(target,new Vector2Int(2,1));
            Assert.That(b.Resolver.HasPincer(a,target),Is.True);ally.AddStatus(StatusKind.Stun,1);Assert.That(b.Resolver.HasPincer(a,target),Is.False);ally.Statuses.Clear();
            var skill=Skill(EffectKind.Damage);skill.Effects[0].Power=1;
            var forecast=BattleForecast.Create(b,a,skill,target.Position);var predicted=forecast.Rows.Single(r=>r.Unit==target);
            Assert.That(predicted.Effects,Does.Contain("협공 +10%"));Assert.That(b.Resolver.Execute(a,skill,target.Position,out _),Is.True);Assert.That(target.CurrentHP,Is.EqualTo(predicted.AfterHP));
        }
        [Test] public void AgilityBonusIsCappedAndDoesNotChangeMagic()
        {
            var gear=New<EquipmentData>();gear.Bonus=new Stats{SPD=1000};player.Equipment[0]=gear;
            var modern=new SkillResolver(grid,new[]{player,enemy},rules,tacticalCombat:true);
            Assert.That(modern.AgilityMultiplier(player,enemy),Is.EqualTo(1.15f).Within(.0001));Assert.That(modern.AgilityMultiplier(enemy,player),Is.EqualTo(.85f).Within(.0001));
            var magic=new SkillEffect{Kind=EffectKind.Damage,Magic=true,Power=1};Assert.That(modern.DamagePreview(player,enemy,magic),Is.EqualTo(resolver.DamagePreview(player,enemy,magic)));
        }
        [Test] public void GrowthSaveFailureRollsBackAndLoadoutPreviewMatchesRuntime()
        {
            var save=new CampaignSave();var p=save.Get(data.Id);p.Level=10;
            Assert.That(GrowthPaths.Save(save,data,GrowthPath.Assault,_=>false),Is.False);Assert.That(p.Growth,Is.EqualTo(GrowthPath.Unselected));
            Assert.That(GrowthPaths.Save(save,data,GrowthPath.Assault,_=>true),Is.True);
            var u=new UnitRuntime(data,Team.Player,rules,10);TacticalDevelopment.Apply(u,save,0);
            var loadout=new EquipmentLoadout(data,p,new EquipmentData[0],save);var detail=new UnitRuntime(data,Team.Player,rules,10);loadout.Apply(detail);Assert.That(detail.Stats.STR,Is.EqualTo(u.Stats.STR));Assert.That(loadout.Preview(10,false).STR,Is.EqualTo(u.Stats.STR));Assert.That(loadout.Preview(10,false).DEF,Is.EqualTo(u.Stats.DEF));
        }
        [Test] public void PresetStoreAndApplyAreAtomicAndDoNotDuplicateEquipment()
        {
            var c=FreeTurnCatalog();var item=New<EquipmentData>();item.Id="test-weapon";item.Slot=EquipmentSlot.Weapon;item.Weapon=c.Characters[0].Weapon;c.Equipment=new[]{item};
            var save=new CampaignSave();save.Inventory.Add(new OwnedEquipment{Id=item.Id,Count=1});save.Get(c.Characters[0].Id).Equipment[0]=item.Id;
            Assert.That(FormationPresets.Store(save,c,new[]{0,1},0,_=>true),Is.True);save.Get(c.Characters[0].Id).Equipment[0]=null;save.Get(c.Characters[1].Id).Equipment[0]=item.Id;
            string before=JsonUtility.ToJson(save);Assert.That(FormationPresets.Apply(save,c,0,_=>false,out _),Is.Not.Null);Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
            Assert.That(FormationPresets.Apply(save,c,0,_=>true,out var members),Is.Null);Assert.That(members,Is.EqualTo(new[]{0,1}));Assert.That(CampaignInventory.Equipped(save,item.Id),Is.EqualTo(1));Assert.That(save.Get(c.Characters[0].Id).Equipment[0],Is.EqualTo(item.Id));
            save.Get(c.Characters[0].Id).Equipment[0]=null;save.Inventory.RemoveAll(e=>e.Id==item.Id);Assert.That(FormationPresets.Apply(save,c,0,_=>true,out _),Does.Contain("부족"));
        }
    }
}
