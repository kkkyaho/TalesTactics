using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        SkillData TimeStop()
        {
            var skill=Skill(EffectKind.Status);skill.Id="mint.ultimate";
            skill.Effects[0].Status=StatusKind.Stun;skill.Effects[0].Duration=2;
            return skill;
        }
        void CastTimeStop(SkillData skill)
        {
            player.Acted=false;
            Assert.That(resolver.Execute(player,skill,enemy.Position,out _),Is.True);
        }
        [TestCase("dhaos")] [TestCase("shizel")] [TestCase("van")]
        [TestCase("artorius")] [TestCase("vholran")] [TestCase("ordo")]
        public void MajorTimeStopCancelsIntentAndRequiresTwoRealActions(string id)
        {
            data.Id=id;enemy.BossTimeRules=true;enemy.IntentPhase=1;enemy.IntentSkill="queued";
            var skill=TimeStop();CastTimeStop(skill);
            Assert.That(enemy.Statuses.Single().Turns,Is.EqualTo(1));
            Assert.That(enemy.IntentPhase,Is.Zero);Assert.That(enemy.IntentSkill,Is.Null);
            Assert.That(enemy.TimeStopResistance,Is.EqualTo(2));
            enemy.BeginTurn();enemy.EndTurn(); // The skipped turn is not an action.
            Assert.That(enemy.Has(StatusKind.Stun),Is.False);Assert.That(enemy.TimeStopResistance,Is.EqualTo(2));
            CastTimeStop(skill);Assert.That(enemy.Has(StatusKind.Stun),Is.False);
            enemy.BeginTurn();enemy.EndTurn(); // An empty recovery does not count either.
            Assert.That(enemy.TimeStopResistance,Is.EqualTo(2));
            enemy.AddStatus(StatusKind.Sleep,1);enemy.BeginTurn();enemy.EndTurn();
            Assert.That(enemy.TimeStopResistance,Is.EqualTo(2));
            for(int i=1;i>=0;i--){enemy.BeginTurn();enemy.Acted=true;enemy.EndTurn();Assert.That(enemy.TimeStopResistance,Is.EqualTo(i));}
            CastTimeStop(skill);Assert.That(enemy.Has(StatusKind.Stun),Is.True);
        }
        [TestCase("demitel")] [TestCase("golem")] [TestCase("asch")]
        [TestCase("origin")] [TestCase("maxwell")]
        public void IntermediateAndTrialEnemiesKeepTwoStoppedActions(string id)
        {
            data.Id=id;enemy.BossTimeRules=true;CastTimeStop(TimeStop());
            Assert.That(enemy.Statuses.Single().Turns,Is.EqualTo(2));
            enemy.BeginTurn();enemy.EndTurn();Assert.That(enemy.Has(StatusKind.Stun),Is.True);
            enemy.BeginTurn();enemy.EndTurn();Assert.That(enemy.Has(StatusKind.Stun),Is.False);
        }
        [Test] public void LegacyBattleAndUnrelatedStunKeepTheirOriginalRules()
        {
            data.Id="dhaos";CastTimeStop(TimeStop());Assert.That(enemy.Statuses.Single().Turns,Is.EqualTo(2));
            enemy.Statuses.Clear();enemy.BossTimeRules=true;var other=TimeStop();other.Id="other.stun";
            CastTimeStop(other);Assert.That(enemy.Statuses.Single().Turns,Is.EqualTo(2));Assert.That(enemy.TimeStopResistance,Is.Zero);
        }
        [TestCase(false,false)] [TestCase(true,false)] [TestCase(false,true)]
        public void TimeStopForecastCheckpointAndOpeningAgree(bool ct,bool team)
        {
            var c=FreeTurnCatalog();c.Enemy.Id="dhaos";
            var b=new BattleSession(c,new[]{0},25,25,5,useCT:ct,teamTurns:team);
            b.Opening=BattleOpening.Capture(b,c);b.Advance();
            var target=b.Units.First(u=>u.Team==Team.Enemy);var actor=b.Active;
            var skill=TimeStop();skill.Range=30;skill.Area=30;skill.RequiresLineOfSight=false;skill.MaxHeightDifference=-1;
            var before=JsonUtility.ToJson(new CheckpointUnit(target));
            var forecast=BattleForecast.Create(b,actor,skill,target.Position);
            Assert.That(JsonUtility.ToJson(new CheckpointUnit(target)),Is.EqualTo(before));
            Assert.That(forecast.Rows.Single(r=>r.Unit==target).Effects,Does.Contain("준비 공격 취소"));
            Assert.That(b.Resolver.Execute(actor,skill,target.Position,out _),Is.True);
            var cp=BattleCheckpoint.Capture(b,new[]{0});
            var restored=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(cp)).Restore(c);
            var copy=restored.Units.First(u=>u.Team==Team.Enemy);
            Assert.That(copy.TimeStopResistance,Is.EqualTo(2));Assert.That(copy.Statuses.Single().Turns,Is.EqualTo(1));
            var opening=restored.Opening.Restore(c);
            Assert.That(opening.Units.First(u=>u.Team==Team.Enemy).TimeStopResistance,Is.Zero);
            cp.Version=10;foreach(var u in cp.Units){u.TimeStopResistance=0;u.BossTimeRules=false;}
            Assert.That(cp.Restore(c).Units.Any(u=>u.BossTimeRules),Is.False);
        }
        [Test] public void InvalidResistanceCannotLoad()
        {
            data.Id="dhaos";enemy.BossTimeRules=true;var saved=new CheckpointUnit(enemy);saved.TimeStopResistance=3;
            Assert.Throws<InvalidDataException>(()=>saved.Apply(enemy,grid,FreeTurnCatalog()));
        }
    }
}
