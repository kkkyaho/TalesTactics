using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [Test] public void CompactForecastRetainsImmunityAndChanceWithoutApplyingThem()
        {
            var b=new BattleSession(FreeTurnCatalog(),new[]{0},teamTurns:true);b.Advance();var actor=b.Active;var foe=b.Units.First(u=>u.Team==Team.Enemy);
            b.Grid.Place(foe,actor.Position+Vector2Int.right);foe.Data.Affinities=new[]{new ElementAffinity{Element=Element.Fire,Multiplier=0}};
            var skill=Skill(EffectKind.Damage);skill.Element=Element.Fire;skill.Effects=new[]{new SkillEffect{Kind=EffectKind.Damage},new SkillEffect{Kind=EffectKind.Status,Status=StatusKind.Stun,Chance=.5f}};
            var row=BattleForecast.Create(b,actor,skill,foe.Position).Rows.Single(r=>r.Unit==foe);
            Assert.That(row.Immune,Is.True);Assert.That(row.DirectDamage,Is.True);Assert.That(row.AfterHP,Is.EqualTo(row.BeforeHP));
            Assert.That(row.ImportantEffects.Replace(" ",""),Does.Contain("50%"));Assert.That(row.ImportantEffects,Does.Contain("기절"));Assert.That(foe.Has(StatusKind.Stun),Is.False);
        }
        [Test] public void CompactHealingForecastReportsRecoveryWithoutChangingHealth()
        {
            var b=new BattleSession(FreeTurnCatalog(),new[]{0},teamTurns:true);b.Advance();var actor=b.Active;actor.CurrentHP=10;
            var skill=Skill(EffectKind.Heal,TargetType.Ally);skill.Effects[0].Flat=30;
            var row=BattleForecast.Create(b,actor,skill,actor.Position).Rows.Single(r=>r.Unit==actor);
            Assert.That(row.AfterHP,Is.GreaterThan(row.BeforeHP));Assert.That(row.DirectDamage,Is.False);Assert.That(row.Immune,Is.False);Assert.That(actor.CurrentHP,Is.EqualTo(10));
        }
        [Test] public void AreaSummaryCountsActualTargetsAndKnockoutsWithoutChangingBattle()
        {
            var b=ReactionBattle(out _);var actor=b.Active;var target=b.Units[2];var extra=b.Units[3];
            b.Grid.Place(extra,new Vector2Int(4,2));target.CurrentHP=extra.CurrentHP=1;
            var skill=Skill(EffectKind.Damage);skill.Area=1;skill.Range=5;
            string before=JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1}));
            var forecast=BattleForecast.Create(b,actor,skill,target.Position);
            Assert.That(forecast.TargetCount,Is.EqualTo(2));Assert.That(forecast.EnemyKnockouts,Is.EqualTo(2));
            Assert.That(forecast.AllyHealthLoss,Is.Zero);Assert.That(forecast.HostileReactions,Is.Zero);
            Assert.That(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1})),Is.EqualTo(before));
            Assert.That(b.Resolver.Execute(actor,skill,target.Position,out _),Is.True);
            Assert.That(new[]{target,extra}.Count(u=>!u.Alive),Is.EqualTo(forecast.EnemyKnockouts));
        }
        [Test] public void ForecastRiskSeparatesHostileReactionDamageFromHpCostAndFriendlySupport()
        {
            var b=ReactionBattle(out _);var actor=b.Active;var target=b.Units[2];
            var skill=Skill(EffectKind.Damage);skill.HPCost=4;skill.Range=1;
            var forecast=BattleForecast.Create(b,actor,skill,target.Position);
            Assert.That(forecast.HostileReactions,Is.EqualTo(1));Assert.That(forecast.TargetCount,Is.EqualTo(1));
            Assert.That(forecast.ReactionDamage,Is.GreaterThan(0));
            Assert.That(forecast.AllyHealthLoss,Is.EqualTo(forecast.ReactionDamage));
            Assert.That(b.Resolver.Execute(actor,skill,target.Position,out _),Is.True);
            Assert.That(forecast.ReactionDamage,Is.EqualTo(b.Resolver.LastReactions.Where(r=>r.Target.Team==actor.Team).Sum(r=>r.Damage)));
            Assert.That(b.Resolver.LastReactions.Count,Is.GreaterThan(forecast.HostileReactions),"Friendly support must not appear as hostile risk.");
        }
        [Test] public void HealingSummaryDoesNotMislabelHealingAsFriendlyDamageOrKnockout()
        {
            var b=ReactionBattle(out _);var actor=b.Active;actor.CurrentHP=10;
            var skill=Skill(EffectKind.Heal,TargetType.Ally);skill.Effects[0].Flat=30;
            var forecast=BattleForecast.Create(b,actor,skill,actor.Position);
            Assert.That(forecast.TargetCount,Is.EqualTo(1));Assert.That(forecast.EnemyKnockouts,Is.Zero);
            Assert.That(forecast.AllyHealthLoss,Is.Zero);Assert.That(forecast.HostileReactions,Is.Zero);Assert.That(forecast.ReactionDamage,Is.Zero);
        }
    }
}
