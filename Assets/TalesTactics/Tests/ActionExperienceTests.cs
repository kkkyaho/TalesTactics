using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [TestCase(-10,.2f)] [TestCase(-2,.6f)] [TestCase(0,1f)] [TestCase(2,1.4f)] [TestCase(10,2f)]
        public void ExperienceLevelDifferenceIsBounded(int difference,float expected)
        {Assert.That(ExperienceRules.LevelMultiplier(20,20+difference),Is.EqualTo(expected).Within(.001f));Assert.That(ExperienceRules.LevelMultiplier(1,40,true),Is.EqualTo(1.5f));}
        [Test] public void ExperienceCountsMultiHitOnceAndAwardsOnlyActualKiller()
        {
            var attack=Skill(EffectKind.Damage);data.BasicAttack=attack;
            var a=new ActionExperience(player,attack);a.Damage(enemy,enemy.Stats.HP/4,false);a.Damage(enemy,enemy.Stats.HP/4,false);Assert.That(a.Total(),Is.EqualTo(12));
            a.Damage(enemy,enemy.Stats.HP/2,true);Assert.That(a.Total(),Is.EqualTo(36));enemy.ExperienceBoss=true;Assert.That(a.Total(),Is.EqualTo(52));
            var skill=Skill(EffectKind.Damage);var b=new ActionExperience(player,skill);b.Damage(enemy,enemy.Stats.HP/2,false);Assert.That(b.Total(),Is.EqualTo(16));Assert.That(b.Total(.5f),Is.EqualTo(8));
            var empty=new ActionExperience(player,attack);empty.Damage(enemy,0,false);Assert.That(empty.Total(),Is.Zero);
        }
        [Test] public void ExperienceAreaDiminishesByRecipientAndTinyDamageIsReduced()
        {
            var s=Skill(EffectKind.Damage);var ledger=new ActionExperience(player,s);
            for(int i=0;i<4;i++){var t=new UnitRuntime(data,Team.Enemy,rules,25);ledger.Damage(t,t.Stats.HP/2,false);}
            Assert.That(ledger.Total(),Is.EqualTo(32));
            ledger=new ActionExperience(player,s);ledger.Damage(enemy,1,false);Assert.That(ledger.Total(),Is.InRange(1,3));
        }
        [Test] public void ExperienceHealingUsesActualRecoveryAndSupportNeedsStateChange()
        {
            var heal=Skill(EffectKind.Heal,TargetType.Self);heal.Effects[0].Power=100;
            var r=new SkillResolver(grid,new[]{player,enemy},rules,actionExperience:true);
            r.Execute(player,heal,player.Position,out _);Assert.That(player.BattleEXP,Is.Zero);
            player.Acted=false;player.CurrentHP-=player.Stats.HP/2;r.Execute(player,heal,player.Position,out _);Assert.That(player.BattleEXP,Is.EqualTo(18));
            player.Acted=false;player.CurrentHP--;r.Execute(player,heal,player.Position,out _);Assert.That(player.BattleEXP,Is.InRange(19,21));
            player.BattleEXP=0;player.EXP=0;player.Acted=false;
            var buff=Skill(EffectKind.Buff,TargetType.Self);buff.Effects[0].Status=StatusKind.Song;buff.Effects[0].Duration=2;
            r.Execute(player,buff,player.Position,out _);Assert.That(player.BattleEXP,Is.EqualTo(10));
            player.Acted=false;r.Execute(player,buff,player.Position,out _);Assert.That(player.BattleEXP,Is.EqualTo(10));
            player.Acted=false;player.Statuses.Clear();buff.Effects[0].Duration=0;r.Execute(player,buff,player.Position,out _);Assert.That(player.BattleEXP,Is.EqualTo(10));
            var cleanse=Skill(EffectKind.Cleanse,TargetType.Self);player.Acted=false;r.Execute(player,cleanse,player.Position,out _);Assert.That(player.BattleEXP,Is.EqualTo(10));
            player.AddStatus(StatusKind.Root,2);player.Acted=false;r.Execute(player,cleanse,player.Position,out _);Assert.That(player.BattleEXP,Is.EqualTo(24));
        }
        [Test] public void ExperienceReviveAndFailedChanceDoNotGrantPhantomCredit()
        {
            var ally=new UnitRuntime(data,Team.Player,rules,25);grid.Place(ally,new Vector2Int(3,1));ally.Damage(99999,grid);
            var r=new SkillResolver(grid,new[]{player,enemy,ally},rules,actionExperience:true);var s=Skill(EffectKind.Revive,TargetType.FallenAlly);s.Effects[0].Power=.5f;
            Assert.That(r.Execute(player,s,ally.Position,out _),Is.True);Assert.That(player.BattleEXP,Is.EqualTo(24));
            player.Acted=false;var status=Skill(EffectKind.Buff,TargetType.Self);status.Effects[0].Chance=0;status.Effects[0].Duration=2;
            r.Execute(player,status,player.Position,out _);Assert.That(player.BattleEXP,Is.EqualTo(24));
        }
        [Test] public void ExperienceGrowthOnlyRestoresStatIncreaseAndNeverRevives()
        {
            player.EXP=95;player.CurrentHP=30;player.CurrentMP=10;var before=player.Stats;
            ActionExperience.Grant(player,12);Assert.That(player.Level,Is.EqualTo(26));Assert.That(player.EXP,Is.EqualTo(7));
            Assert.That(player.CurrentHP,Is.EqualTo(30+player.Stats.HP-before.HP));Assert.That(player.CurrentMP,Is.EqualTo(10+player.Stats.MP-before.MP));
            player.CurrentHP=0;ActionExperience.Grant(player,100);Assert.That(player.CurrentHP,Is.Zero);
            player.Level=49;player.EXP=95;var gain=ActionExperience.Grant(player,300);Assert.That(gain.Amount,Is.EqualTo(5));Assert.That(player.Level,Is.EqualTo(50));Assert.That(player.EXP,Is.Zero);Assert.That(ActionExperience.Grant(player,100),Is.Null);
        }
        [Test] public void ExperienceIsDeferredUntilReactionsAndForecastDoesNotMutateProgress()
        {
            var b=ReactionBattle(out _);var a=b.Active;var t=b.Units[2];a.EXP=99;
            var before=JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1}));var f=BattleForecast.Create(b,a,a.Data.BasicAttack,t.Position);
            Assert.That(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1})),Is.EqualTo(before));
            int original=a.Level;b.Resolver.Execute(a,a.Data.BasicAttack,t.Position,out _);
            Assert.That(a.Level,Is.EqualTo(original+1));Assert.That(b.Units[1].BattleEXP,Is.GreaterThan(0));
            foreach(var row in f.Rows)Assert.That(row.AfterHP,Is.EqualTo(row.Unit.CurrentHP));
            Assert.That(b.Resolver.LastReactions.All(r=>r.Actor.Team!=Team.Player||r.Experience>0),Is.True);
        }
        [Test] public void ExperienceSuspendRewindAndRetryRestoreLevelAndUncommittedGains()
        {
            var b=ReactionBattle(out var c);b.Active.EXP=99;b.Opening.Units[0].EXP=99;
            b.RecordAction("공격");b.Resolver.Execute(b.Active,b.Active.Data.BasicAttack,b.Units[2].Position,out _);
            int level=b.Active.Level,exp=b.Active.EXP,earned=b.Active.BattleEXP;
            b=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1}))).Restore(c);
            Assert.That(b.Active.Level,Is.EqualTo(level));Assert.That(b.Active.EXP,Is.EqualTo(exp));Assert.That(b.Active.BattleEXP,Is.EqualTo(earned));
            var rewind=b.RewindLast(out _);Assert.That(rewind.Active.Level,Is.EqualTo(level-1));Assert.That(rewind.Active.EXP,Is.EqualTo(99));Assert.That(rewind.Active.BattleEXP,Is.Zero);
            var retry=b.Opening.Restore(c);Assert.That(retry.Units[0].EXP,Is.EqualTo(99));Assert.That(retry.Units[0].BattleEXP,Is.Zero);
        }
        [Test] public void ExperienceLegacyCheckpointKeepsOldBattleRulesAndRejectsCorruption()
        {
            var b=ReactionBattle(out var c);var cp=BattleCheckpoint.Capture(b,new[]{0,1});cp.Version=8;cp.Opening.Version=3;
            Assert.That(cp.Restore(c).Resolver.ActionExperienceEnabled,Is.False);Assert.That(cp.Opening.Restore(c).Resolver.ActionExperienceEnabled,Is.False);
            cp.Version=9;cp.Units[0].EXP=100;Assert.Throws<InvalidDataException>(()=>cp.Restore(c));
        }
        [TestCase(1)] [TestCase(2)]
        public void ExperienceMigrationPreservesLevelAndProgressRatioExactlyOnce(int version)
        {
            var path=SaveTestPath();var save=new CampaignSave{Version=version};var p=save.Get("hero");p.Level=5;p.EXP=250;
            string original=JsonUtility.ToJson(save);File.WriteAllText(path,original);var file=new CampaignFile(path);save=file.Load();
            Assert.That(save.Version,Is.EqualTo(3));Assert.That(save.Get("hero").Level,Is.EqualTo(5));Assert.That(save.Get("hero").EXP,Is.EqualTo(50));Assert.That(File.ReadAllText(path),Is.EqualTo(original));
            Assert.That(file.Save(save),Is.True);Assert.That(File.ReadAllText(path+".v"+version+".bak"),Is.EqualTo(original));Assert.That(file.Load().Get("hero").EXP,Is.EqualTo(50));
        }
        [Test] public void ExperienceVictoryCommitsPerUnitAndFailedSaveRollsBackBeforeRetry()
        {
            var save=new CampaignSave();var ally=New<CharacterData>();ally.Id="ally";
            var party=new[]{data,ally};var earned=new Dictionary<string,int>{{data.Id,136},{ally.Id,52}};var reward=CampaignEconomy.Prepare(save,0,9999);
            Assert.That(CampaignStages.TryReward(save,reward,party,_=>false,earned),Is.False);Assert.That(save.Characters,Is.Empty);
            Assert.That(CampaignStages.TryReward(save,reward,party,_=>true,earned),Is.True);
            Assert.That(save.Get(data.Id).Level,Is.EqualTo(2));Assert.That(save.Get(data.Id).EXP,Is.EqualTo(66));Assert.That(save.Get(ally.Id).EXP,Is.EqualTo(82));
            Assert.That(CampaignStages.TryReward(save,reward,party,_=>true,earned),Is.False);Assert.That(save.Get(data.Id).EXP,Is.EqualTo(66));
        }
    }
}
