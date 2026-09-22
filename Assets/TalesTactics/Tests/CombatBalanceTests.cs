using NUnit.Framework;
using UnityEngine;

namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [Test] public void ElementResistanceWeaknessAndImmunityMatchActualDamage()
        {
            var s=Skill(EffectKind.Damage);s.Element=Element.Fire;
            var baseline=resolver.DamagePreview(player,enemy,s.Effects[0],s);
            data.Affinities=new[]{new ElementAffinity{Element=Element.Fire,Multiplier=0.5f}};
            int resistant=resolver.DamagePreview(player,enemy,s.Effects[0],s);
            Assert.That(resistant,Is.EqualTo(Mathf.Max(1,Mathf.RoundToInt(baseline*0.5f))));
            data.Affinities[0].Multiplier=1.5f;
            Assert.That(resolver.DamagePreview(player,enemy,s.Effects[0],s)>baseline,Is.True);
            int hp=enemy.CurrentHP;int predicted=resolver.DamagePreview(player,enemy,s.Effects[0],s);
            Assert.That(resolver.Execute(player,s,enemy.Position,out _),Is.True);
            Assert.That(hp-enemy.CurrentHP,Is.EqualTo(predicted));
            player.Acted=false;data.Affinities[0].Multiplier=0;hp=enemy.CurrentHP;
            Assert.That(resolver.DamagePreview(player,enemy,s.Effects[0],s),Is.Zero);
            Assert.That(resolver.Execute(player,s,enemy.Position,out _),Is.True);
            Assert.That(enemy.CurrentHP,Is.EqualTo(hp));
            s.Element=Element.None;Assert.That(resolver.DamagePreview(player,enemy,s.Effects[0],s)>0,Is.True);
        }

        [Test] public void HeightDamageCapsAndDoesNotModifyMagic()
        {
            var s=Skill(EffectKind.Damage);s.UsesHeightDamage=true;
            int neutral=resolver.DamagePreview(player,enemy,s.Effects[0],s);
            grid[player.Position].Height=2;int high=resolver.DamagePreview(player,enemy,s.Effects[0],s);
            Assert.That(high>neutral,Is.True);
            grid[player.Position].Height=20;Assert.That(resolver.DamagePreview(player,enemy,s.Effects[0],s),Is.EqualTo(high));
            grid[player.Position].Height=-2;Assert.That(resolver.DamagePreview(player,enemy,s.Effects[0],s)<neutral,Is.True);
            s.Effects[0].Magic=true;int magic=resolver.DamagePreview(player,enemy,s.Effects[0],s);
            grid[player.Position].Height=10;Assert.That(resolver.DamagePreview(player,enemy,s.Effects[0],s),Is.EqualTo(magic));
        }

        [Test] public void HeightRangeUsesSignedDeltaAndAppliesToArea()
        {
            var s=Skill(EffectKind.Damage);s.Range=3;s.HeightRangeLimit=2;s.Shape=SkillAreaShape.Line;
            grid.Place(enemy,new Vector2Int(6,1));grid[player.Position].Height=2;
            Assert.That(resolver.InRange(player,s,enemy.Position),Is.True);
            Assert.That(resolver.Targets(player,s,new Vector2Int(2,1)),Does.Contain(enemy));
            grid[player.Position].Height=0;Assert.That(resolver.InRange(player,s,enemy.Position),Is.False);
            grid.Place(enemy,new Vector2Int(3,1));grid[enemy.Position].Height=2;
            Assert.That(resolver.InRange(player,s,enemy.Position),Is.False);
            grid.Place(enemy,new Vector2Int(2,1));grid[enemy.Position].Height=2;
            Assert.That(resolver.InRange(player,s,enemy.Position),Is.True);
        }

        [Test] public void InvalidAffinityValuesAreBoundedAndMissingElementsAreNeutral()
        {
            data.Affinities=new[]{new ElementAffinity{Element=Element.Fire,Multiplier=20}};
            Assert.That(ElementalRules.Multiplier(data,Element.Fire),Is.EqualTo(2));
            data.Affinities[0].Multiplier=float.NaN;Assert.That(ElementalRules.Multiplier(data,Element.Fire),Is.EqualTo(1));
            data.Affinities[0].Multiplier=-1;Assert.That(ElementalRules.Multiplier(data,Element.Fire),Is.Zero);
            Assert.That(ElementalRules.Multiplier(data,Element.Water),Is.EqualTo(1));
        }
    }
}
