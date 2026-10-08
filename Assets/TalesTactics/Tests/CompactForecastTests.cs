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
    }
}
