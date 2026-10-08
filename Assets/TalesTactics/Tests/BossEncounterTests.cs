using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [TestCase(false)] [TestCase(true)]
        public void BossWardReducesPhysicalAndMagicAndFallsWithLivingEscorts(bool magic)
        {
            var b=new BattleSession(FreeTurnCatalog(),new[]{0,1,2},campaignStage:1,teamTurns:true,bossEncounters:true);
            var boss=b.ObjectiveUnit;var actor=b.Units[0];var escorts=b.Units.Where(u=>u.Team==Team.Enemy&&u!=boss).ToArray();
            var effect=new SkillEffect{Kind=EffectKind.Damage,Power=4,Magic=magic};
            boss.BossWard=false;int full=b.Resolver.DamagePreview(actor,boss,effect);boss.BossWard=true;
            for(int i=0;i<=3;i++)
            {
                Assert.That(b.Resolver.BossWardPercent(boss),Is.EqualTo(75-i*25));
                Assert.That(b.Resolver.DamagePreview(actor,boss,effect),Is.EqualTo(Mathf.RoundToInt(full*(.25f+i*.25f))).Within(1));
                if(i<3)escorts[i].Damage(999999,b.Grid);
            }
            Assert.That(b.Resolver.BossWardPercent(actor),Is.Zero);
        }
        [TestCase(1)] [TestCase(5)]
        public void BossEncounterCheckpointRetainsFormationAndVersionFourOptsOut(int stage)
        {
            var catalog=FreeTurnCatalog();var b=new BattleSession(catalog,new[]{0,1,2},campaignStage:stage,teamTurns:true,bossEncounters:true);b.Advance();
            Assert.That(b.Units.All(u=>b.Grid[u.Position].Occupant==u),Is.True);
            var cp=BattleCheckpoint.Capture(b,new[]{0,1,2});var restored=cp.Restore(catalog);
            Assert.That(restored.BossEncounters,Is.True);Assert.That(restored.ObjectiveUnit.BossWard,Is.True);
            Assert.That(restored.Units.Select(u=>u.Position),Is.EqualTo(b.Units.Select(u=>u.Position)));
            cp.Version=4;restored=cp.Restore(catalog);
            Assert.That(restored.BossEncounters,Is.False);Assert.That(restored.Resolver.BossWardPercent(restored.ObjectiveUnit),Is.Zero);
            Assert.That(restored.Units.Select(u=>u.Position),Is.EqualTo(b.Units.Select(u=>u.Position)));
        }
        [Test] public void BossRulesDoNotChangeTrainingOrFixedOrderOrOtherMissions()
        {
            var c=FreeTurnCatalog();
            foreach(var b in new[]{new BattleSession(c,new[]{0},objective:ObjectiveKind.Boss,teamTurns:true,bossEncounters:true),new BattleSession(c,new[]{0},campaignStage:1,bossEncounters:true),new BattleSession(c,new[]{0},campaignStage:0,teamTurns:true,bossEncounters:true),new BattleSession(c,new[]{0},campaignStage:5,useCT:true,teamTurns:true,bossEncounters:true)})
            {Assert.That(b.BossEncounters,Is.False);Assert.That(b.Units.Any(u=>u.BossWard),Is.False);}
        }
        [Test] public void UltimateAreaForecastMatchesDamageAndWardAfterEscortDeaths()
        {
            var b=new BattleSession(FreeTurnCatalog(),new[]{0},campaignStage:1,teamTurns:true,bossEncounters:true);b.Advance();
            var actor=b.Active;var boss=b.ObjectiveUnit;var skill=Skill(EffectKind.Damage);skill.IsUltimate=true;skill.Range=20;skill.Area=20;skill.Effects[0].Power=1;
            foreach(var escort in b.Units.Where(u=>u.Team==Team.Enemy&&u!=boss))escort.CurrentHP=1;
            var f=BattleForecast.Create(b,actor,skill,boss.Position);Assert.That(f.Rows.Single(r=>r.Unit==boss).Effects,Does.Contain("75% → 0%"));
            Assert.That(b.Resolver.Execute(actor,skill,boss.Position,out _),Is.True);
            foreach(var row in f.Rows)Assert.That(row.Unit.CurrentHP,Is.EqualTo(row.AfterHP));
        }
        [Test] public void KillingEscortForecastReportsWardLossWithoutMutatingBattle()
        {
            var b=new BattleSession(FreeTurnCatalog(),new[]{0,1},campaignStage:1,teamTurns:true,bossEncounters:true);b.Advance();
            var actor=b.Active;var escort=b.Units.Last();b.Grid.Place(actor,new Vector2Int(6,6));escort.CurrentHP=1;
            var skill=Skill(EffectKind.Damage);skill.Effects[0].Power=4;
            var f=BattleForecast.Create(b,actor,skill,escort.Position);
            Assert.That(f.Rows.Single(r=>r.Unit==b.ObjectiveUnit).Effects,Does.Contain("75% → 50%"));
            Assert.That(b.Resolver.BossWardPercent(b.ObjectiveUnit),Is.EqualTo(75));Assert.That(escort.Alive,Is.True);
            Assert.That(b.Resolver.Execute(actor,skill,escort.Position,out _),Is.True);
            Assert.That(b.Resolver.BossWardPercent(b.ObjectiveUnit),Is.EqualTo(50));
        }
    }
}
