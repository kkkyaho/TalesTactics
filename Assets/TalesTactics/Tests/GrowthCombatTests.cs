using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        BattleSession GrowthBattle(string owner,GrowthPath path,out BattleCatalog catalog)
        {
            catalog=FreeTurnCatalog();var character=catalog.Characters[0];character.Id=owner;
            character.BasicAttack=Skill(EffectKind.Damage);character.BasicAttack.Id=owner+".basic";character.BasicAttack.Range=1;character.BasicAttack.Effects[0].Power=.5f;
            catalog.Enemy.BasicAttack.Range=1;
            var b=new BattleSession(catalog,new[]{0,1},level:10,campaignStage:0,teamTurns:true,reactions:true,actionExperience:false);b.Advance();
            b.Active.Growth=path;b.Active.Facing=Facing.Right;
            b.Grid.Place(b.Units[1],new Vector2Int(1,3));b.Grid.Place(b.Units[2],new Vector2Int(2,1));
            return b;
        }
        void MarkGrowthMove(UnitRuntime actor){actor.Moved=true;actor.MoveOrigin=actor.Position+Vector2Int.left;actor.CanUndoMove=true;}
        SkillData GrowthHeal(UnitRuntime actor)
        {
            var skill=Skill(EffectKind.Heal,TargetType.Ally);skill.Id="mint.growth-heal";skill.MPCost=12;skill.Effects[0].Power=0;skill.Effects[0].Flat=20;
            actor.Data.Skills=new[]{skill};return skill;
        }
        [Test,Category("GrowthCombat")]
        public void ClessAssaultPushesOnceAfterAllHitsAndForecastAndRewindMatch()
        {
            var b=GrowthBattle("cless",GrowthPath.Assault,out var catalog);var actor=b.Active;var target=b.Units[2];var skill=actor.Data.BasicAttack;
            skill.Effects=new[]{new SkillEffect{Kind=EffectKind.Damage,Power=.3f},new SkillEffect{Kind=EffectKind.Damage,Power=.3f}};
            var origin=target.Position;MarkGrowthMove(actor);b.RecordAction("돌파 공격");
            string before=JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1}));int expected=b.Resolver.DamagePreview(actor,target,skill.Effects[0],skill)*2;
            var forecast=BattleForecast.Create(b,actor,skill,target.Position);var row=forecast.Rows.Single(r=>r.Unit==target);
            Assert.That(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1})),Is.EqualTo(before));
            Assert.That(row.BeforeHP-row.AfterHP,Is.EqualTo(expected));Assert.That(row.ImportantEffects,Does.Contain("위치"));Assert.That(forecast.Reactions,Is.Empty,"The push removes the adjacent enemy's counter opportunity.");
            Assert.That(b.Resolver.Execute(actor,skill,target.Position,out var message),Is.True);
            Assert.That(target.Position,Is.EqualTo(origin+Vector2Int.right));Assert.That(target.CurrentHP,Is.EqualTo(row.AfterHP));Assert.That(message,Does.Contain("밀치기 1칸"));
            Assert.That(actor.CanUndoMove,Is.False);Assert.That(actor.SpecialGauge,Is.EqualTo(rules.GaugePerAttack));Assert.That(b.Resolver.LastReactions,Is.Empty);
            var restored=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1}))).Restore(catalog);
            Assert.That(restored.Active.Growth,Is.EqualTo(GrowthPath.Assault));Assert.That(restored.Units[2].Position,Is.EqualTo(target.Position));
            var rewind=restored.RewindLast(out _);Assert.That(rewind.Units[2].Position,Is.EqualTo(origin));Assert.That(rewind.Active.Moved,Is.True);Assert.That(rewind.Active.Acted,Is.False);
            Assert.That(rewind.Resolver.Execute(rewind.Active,skill,rewind.Units[2].Position,out _),Is.True);Assert.That(rewind.Units[2].Position,Is.EqualTo(target.Position));Assert.That(rewind.Units[2].CurrentHP,Is.EqualTo(target.CurrentHP));
        }
        [TestCase("unmoved"),TestCase("undo"),TestCase("boss"),TestCase("objective-boss"),TestCase("device"),TestCase("wall"),TestCase("cliff"),TestCase("occupied"),TestCase("level"),TestCase("unselected"),TestCase("other-owner"),TestCase("skill"),TestCase("followup"),Category("GrowthCombat")]
        public void ClessAssaultRespectsMovementUnlockTerrainAndActionBoundaries(string condition)
        {
            var b=GrowthBattle("cless",GrowthPath.Assault,out _);var actor=b.Active;var target=b.Units[2];var skill=actor.Data.BasicAttack;MarkGrowthMove(actor);var before=target.Position;
            if(condition=="unmoved")actor.Moved=false;
            if(condition=="undo")actor.MoveOrigin=actor.Position;
            if(condition=="boss")target.CrossoverBoss=true;
            if(condition=="objective-boss")target.ExperienceBoss=true;
            if(condition=="device")target.Data.Id="boundary-device";
            if(condition=="wall")b.Grid[before+Vector2Int.right].Walkable=false;
            if(condition=="cliff")b.Grid[before+Vector2Int.right].Height=10;
            if(condition=="occupied")b.Grid.Place(b.Units[1],before+Vector2Int.right);
            if(condition=="level")actor.Level=9;
            if(condition=="unselected")actor.Growth=GrowthPath.Unselected;
            if(condition=="other-owner")actor.Data.Id="alphen";
            if(condition=="skill")skill=Skill(EffectKind.Damage);
            Assert.That(b.Resolver.Execute(actor,skill,target.Position,out _,condition=="followup"),Is.True);
            Assert.That(target.Position,Is.EqualTo(before));
        }
        [Test,Category("GrowthCombat")]
        public void GrowthDoesNotTriggerOnSupportReactionOrOnZeroDamage()
        {
            var b=GrowthBattle("cless",GrowthPath.Assault,out _);var cless=b.Active;var ally=b.Units[1];var enemy=b.Units[2];MarkGrowthMove(cless);
            b.Grid.Place(ally,new Vector2Int(2,2));var before=enemy.Position;
            Assert.That(b.Resolver.Execute(ally,ally.Data.BasicAttack,enemy.Position,out _),Is.True);
            Assert.That(b.Resolver.LastReactions.Any(r=>r.Actor==cless&&r.Kind=="지원"),Is.True);Assert.That(enemy.Position,Is.EqualTo(before));
            cless.Acted=false;cless.Data.BasicAttack.Effects[0].Chance=0;
            Assert.That(b.Resolver.Execute(cless,cless.Data.BasicAttack,enemy.Position,out _,forecast:true),Is.True);Assert.That(enemy.Position,Is.EqualTo(before));
            cless.Acted=false;cless.Data.BasicAttack.Effects[0].Chance=1;cless.Data.BasicAttack.Element=Element.Fire;enemy.Data.Affinities=new[]{new ElementAffinity{Element=Element.Fire,Multiplier=0}};
            Assert.That(b.Resolver.Execute(cless,cless.Data.BasicAttack,enemy.Position,out _),Is.True);Assert.That(enemy.Position,Is.EqualTo(before));
        }
        [Test,Category("GrowthCombat")]
        public void ClessBulwarkUsesOneSharedProtectionAndForecastPreservesIt()
        {
            var b=GrowthBattle("cless",GrowthPath.Bulwark,out var catalog);var cless=b.Active;var ally=b.Units[1];var enemy=b.Units[2];
            b.Grid.Place(ally,new Vector2Int(1,2));b.Grid.Place(enemy,new Vector2Int(2,2));cless.AddStatus(StatusKind.Guard,2);
            var skill=enemy.Data.BasicAttack;var effect=skill.Effects[0];cless.Statuses.Clear();int unprotected=b.Resolver.DamagePreview(enemy,ally,effect,skill);cless.AddStatus(StatusKind.Guard,2);
            int protectedDamage=b.Resolver.DamagePreview(enemy,ally,effect,skill);Assert.That(protectedDamage,Is.LessThan(unprotected));Assert.That(b.Resolver.Protector(ally),Is.SameAs(cless));
            cless.Trait=TacticalTrait.Protector;Assert.That(b.Resolver.DamagePreview(enemy,ally,effect,skill),Is.EqualTo(protectedDamage),"Trait and growth must not stack.");
            var facingBefore=enemy.Facing;var forecast=BattleForecast.Create(b,enemy,skill,ally.Position);Assert.That(cless.ProtectionUsed,Is.False);Assert.That(enemy.Facing,Is.EqualTo(facingBefore));Assert.That(forecast.Rows.Single(r=>r.Unit==cless).Effects,Does.Contain("보호 1회"));
            // BattleDirector turns the actor toward its aim before Execute, as the forecast does on its private copy.
            enemy.Facing=SkillResolver.Toward(enemy.Position,ally.Position);
            Assert.That(b.Resolver.Execute(enemy,skill,ally.Position,out _),Is.True);foreach(var row in forecast.Rows)Assert.That(row.AfterHP,Is.EqualTo(row.Unit.CurrentHP),row.Unit.Data.Id);
            Assert.That(cless.ProtectionUsed,Is.True);Assert.That(b.Resolver.Protector(ally),Is.Null);
            var restored=BattleCheckpoint.Capture(b,new[]{0,1}).Restore(catalog);Assert.That(restored.Active.ProtectionUsed,Is.True);Assert.That(restored.Active.Growth,Is.EqualTo(GrowthPath.Bulwark));
            cless.BeginTurn();Assert.That(cless.ProtectionUsed,Is.False);Assert.That(cless.Has(StatusKind.Guard),Is.False);Assert.That(b.Resolver.Protector(ally),Is.Null);
        }
        [TestCase("unguarded"),TestCase("far"),TestCase("stun"),TestCase("sleep"),TestCase("used"),TestCase("level"),Category("GrowthCombat")]
        public void ClessBulwarkRequiresAnAvailableAdjacentGuard(string condition)
        {
            var b=GrowthBattle("cless",GrowthPath.Bulwark,out _);var cless=b.Active;var ally=b.Units[1];b.Grid.Place(ally,new Vector2Int(1,2));cless.AddStatus(StatusKind.Guard,2);
            if(condition=="unguarded")cless.Statuses.Clear();if(condition=="far")b.Grid.Place(ally,new Vector2Int(1,3));
            if(condition=="stun")cless.AddStatus(StatusKind.Stun,1);if(condition=="sleep")cless.AddStatus(StatusKind.Sleep,1);
            if(condition=="used")cless.ProtectionUsed=true;if(condition=="level")cless.Level=9;
            Assert.That(b.Resolver.Protector(ally),Is.Null);
        }
        [TestCase(GrowthPath.Assault,StatusKind.Song),TestCase(GrowthPath.Bulwark,StatusKind.Guard),Category("GrowthCombat")]
        public void MintSingleHealShowsSupportInForecastAndRestoresItThroughSaveRewind(GrowthPath path,StatusKind status)
        {
            var b=GrowthBattle("mint",path,out var catalog);var mint=b.Active;var ally=b.Units[1];var heal=GrowthHeal(mint);ally.CurrentHP-=40;
            b.RecordAction("지원 회복");string before=JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1}));
            var forecast=BattleForecast.Create(b,mint,heal,ally.Position);var row=forecast.Rows.Single(r=>r.Unit==ally);
            Assert.That(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1})),Is.EqualTo(before));Assert.That(row.ImportantEffects,Does.Contain(status==StatusKind.Guard?"다음 자기 턴까지":"성가 1턴"));
            Assert.That(b.Resolver.Describe(mint,heal),Does.Contain(GrowthPaths.Role(mint.Data,path)));
            Assert.That(b.Resolver.Execute(mint,heal,ally.Position,out _),Is.True);Assert.That(ally.CurrentHP,Is.EqualTo(row.AfterHP));Assert.That(ally.Has(status),Is.True);
            Assert.That(mint.SpecialGauge,Is.Zero);Assert.That(b.Resolver.LastReactions,Is.Empty);Assert.That(b.Resolver.LastExperience,Is.Empty);
            var restored=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1}))).Restore(catalog);
            Assert.That(restored.Units[1].Has(status),Is.True);Assert.That(restored.Active.Growth,Is.EqualTo(path));
            var rewind=restored.RewindLast(out _);Assert.That(rewind.Units[1].Has(status),Is.False);Assert.That(rewind.Active.CurrentMP,Is.EqualTo(mint.CurrentMP+heal.MPCost));
            Assert.That(rewind.Resolver.Execute(rewind.Active,heal,rewind.Units[1].Position,out _),Is.True);Assert.That(rewind.Units[1].Has(status),Is.True);
            ally.BeginTurn();Assert.That(ally.Has(status),Is.EqualTo(status==StatusKind.Song));ally.EndTurn();Assert.That(ally.Has(status),Is.False);
        }
        [TestCase("full"),TestCase("self"),TestCase("area"),TestCase("revive"),TestCase("level"),TestCase("unselected"),TestCase("other-owner"),TestCase("chance"),TestCase("followup"),Category("GrowthCombat")]
        public void MintGrowthDoesNotBuffIneligibleHealing(string condition)
        {
            var b=GrowthBattle("mint",GrowthPath.Assault,out _);var mint=b.Active;var ally=b.Units[1];var heal=GrowthHeal(mint);ally.CurrentHP-=40;
            if(condition=="full")ally.CurrentHP=ally.Stats.HP;
            if(condition=="self"){ally=mint;ally.CurrentHP-=40;}
            if(condition=="area")heal.Area=1;
            if(condition=="revive"){ally.Damage(99999,b.Grid);heal.Target=TargetType.FallenAlly;heal.Effects[0].Kind=EffectKind.Revive;heal.Effects[0].Power=.5f;}
            if(condition=="level")mint.Level=9;if(condition=="unselected")mint.Growth=GrowthPath.Unselected;if(condition=="other-owner")mint.Data.Id="tear";
            if(condition=="chance")heal.Effects[0].Chance=0;
            Assert.That(b.Resolver.Execute(mint,heal,ally.Position,out _,condition=="followup",condition=="chance"),Is.True);
            Assert.That(ally.Has(StatusKind.Song)||ally.Has(StatusKind.Guard),Is.False);
        }
        [Test,Category("GrowthCombat")]
        public void MintSupportPreservesLongerSongAndDoesNotAddExperienceOrDuplicateWeaponDiscount()
        {
            var b=GrowthBattle("mint",GrowthPath.Assault,out _);var mint=b.Active;var ally=b.Units[1];var heal=GrowthHeal(mint);ally.AddStatus(StatusKind.Song,3);ally.CurrentHP-=40;
            var weapon=New<EquipmentData>();weapon.Id="merciful-test";weapon.Slot=EquipmentSlot.Weapon;weapon.Weapon=mint.Data.Weapon;weapon.ExclusiveCharacterId="mint";weapon.Effect=EquipmentEffect.MercifulRod;mint.Equipment[0]=weapon;
            var resolverWithExp=new SkillResolver(b.Grid,b.Units,b.Rules,actionExperience:true);int beforeMP=mint.CurrentMP;
            var ledger=new ActionExperience(mint,heal);int oldHP=ally.CurrentHP;ally.CurrentHP+=20;ledger.Effect(ally,heal.Effects[0],oldHP,ally.Statuses.Count,false,ally.SpecialGauge,ally.Position);int expected=ledger.Total();ally.CurrentHP=oldHP;
            Assert.That(resolverWithExp.Execute(mint,heal,ally.Position,out _),Is.True);Assert.That(mint.CurrentMP,Is.EqualTo(beforeMP-9));
            Assert.That(ally.Statuses.Count(s=>s.Kind==StatusKind.Song),Is.EqualTo(1));Assert.That(ally.Statuses.Single(s=>s.Kind==StatusKind.Song).Turns,Is.EqualTo(3));
            Assert.That(resolverWithExp.LastExperience.Single().Amount,Is.EqualTo(expected));Assert.That(mint.SpecialGauge,Is.Zero);
        }
        [Test,Category("GrowthCombat")]
        public void GrowthChoiceKeepsLegacyEnumUnlockAndTransactionalSave()
        {
            Assert.That((int)GrowthPath.Unselected,Is.Zero);Assert.That((int)GrowthPath.Assault,Is.EqualTo(1));Assert.That((int)GrowthPath.Bulwark,Is.EqualTo(2));
            var save=new CampaignSave();var p=save.Get(data.Id);p.Level=9;string before=JsonUtility.ToJson(save);
            Assert.That(GrowthPaths.Save(save,data,GrowthPath.Assault,_=>true),Is.False);Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
            p.Level=10;p.Growth=GrowthPath.Bulwark;before=JsonUtility.ToJson(save);
            Assert.That(GrowthPaths.Save(save,data,GrowthPath.Assault,_=>throw new InvalidOperationException()),Is.False);Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
            Assert.That(GrowthPaths.Save(save,data,GrowthPath.Assault,_=>true),Is.True);var restored=JsonUtility.FromJson<CampaignSave>(JsonUtility.ToJson(save));Assert.That(restored.Get(data.Id).Growth,Is.EqualTo(GrowthPath.Assault));
            Assert.That(GrowthPaths.Save(save,data,GrowthPath.Unselected,_=>true),Is.True);
        }
    }
}
