using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        EquipmentData Signature(EquipmentEffect effect,string owner=null)
        {
            var item=New<EquipmentData>();item.Id="signature.test";item.DisplayName="시련 무기";item.Slot=EquipmentSlot.Weapon;
            item.Weapon=data.Weapon;item.ExclusiveCharacterId=owner??data.Id;item.Effect=effect;
            player.Equipment[0]=item;return item;
        }
        [Test] public void ExclusiveWeaponCannotBeEquippedByAnotherSwordUserOrSold()
        {
            var item=Signature(EquipmentEffect.EternalEdge,"cless");var hero=New<CharacterData>();hero.Id="alphen";hero.Weapon=item.Weapon;
            var progress=new CharacterProgress{Id="alphen",Equipment=new[]{item.Id,null,null}};
            var draft=new EquipmentLoadout(hero,progress,new[]{item});
            Assert.That(draft.Options(EquipmentSlot.Weapon),Is.Empty);Assert.That(draft.Get(EquipmentSlot.Weapon),Is.Null);
            Assert.That(draft.Equip(EquipmentSlot.Weapon,item),Is.False);
            var save=new CampaignSave();save.Inventory.Add(new OwnedEquipment{Id=item.Id,Count=1});
            Assert.That(CampaignInventory.TrySell(save,item,_=>true),Is.False);
            hero.Id="cless";Assert.That(new EquipmentLoadout(hero,null,new[]{item},save).Equip(EquipmentSlot.Weapon,item),Is.True);
            Assert.That(SignatureWeaponRules.Has(player,EquipmentEffect.EternalEdge),Is.False);
        }
        [Test] public void EternalSwordOnlyAmplifiesAnActuallyPreparedAttack()
        {
            var skill=Skill(EffectKind.Damage);int baseline=resolver.DamagePreview(player,enemy,skill.Effects[0],skill);
            Signature(EquipmentEffect.EternalEdge);Assert.That(resolver.DamagePreview(player,enemy,skill.Effects[0],skill),Is.EqualTo(baseline));
            enemy.IntentPhase=1;Assert.That(resolver.DamagePreview(player,enemy,skill.Effects[0],skill),Is.EqualTo(Mathf.RoundToInt(baseline*1.15f)).Within(1));
        }
        [Test] public void MercifulRodReducesHealingCostButNotTimeStop()
        {
            Signature(EquipmentEffect.MercifulRod);var heal=Skill(EffectKind.Heal);heal.MPCost=13;
            Assert.That(resolver.MPCost(player,heal),Is.EqualTo(10));var stop=TimeStop();stop.MPCost=30;
            Assert.That(resolver.MPCost(player,stop),Is.EqualTo(30));
        }
        [Test] public void BladeDrainsActualDamageInsteadOfOverkillDamage()
        {
            Signature(EquipmentEffect.UnboundBlade);player.CurrentHP=100;enemy.CurrentHP=10;
            var skill=Skill(EffectKind.Damage);skill.Effects[0].Power=20;
            Assert.That(resolver.Execute(player,skill,enemy.Position,out _),Is.True);Assert.That(player.CurrentHP,Is.EqualTo(101));
        }
        [Test] public void FistsAwardGaugeOncePerAttackRegardlessOfHitCount()
        {
            Signature(EquipmentEffect.ResoluteFists);var skill=Skill(EffectKind.Damage);
            skill.Effects=new[]{new SkillEffect{Kind=EffectKind.Damage,Power=.1f},new SkillEffect{Kind=EffectKind.Damage,Power=.1f}};
            Assert.That(resolver.Execute(player,skill,enemy.Position,out _),Is.True);Assert.That(player.SpecialGauge,Is.EqualTo(rules.GaugePerAttack+5));
        }
        [Test] public void HornExtendsSongButNeverNegativeStatuses()
        {
            Signature(EquipmentEffect.HarmonicHorn);var song=Skill(EffectKind.Buff,TargetType.Self);song.Effects[0].Status=StatusKind.Song;
            Assert.That(resolver.Execute(player,song,player.Position,out _),Is.True);Assert.That(player.Statuses.Single().Turns,Is.EqualTo(3));
            player.Acted=false;var stop=TimeStop();Assert.That(resolver.Execute(player,stop,enemy.Position,out _),Is.True);Assert.That(enemy.Statuses.Single().Turns,Is.EqualTo(2));
        }
        [Test] public void SpearPenetratesOnlyMagicDefense()
        {
            var skill=Skill(EffectKind.Damage);var effect=skill.Effects[0];int normal=resolver.DamagePreview(player,enemy,effect,skill);
            effect.Magic=true;int magic=resolver.DamagePreview(player,enemy,effect,skill);Signature(EquipmentEffect.FonicPiercer);
            Assert.That(resolver.DamagePreview(player,enemy,effect,skill),Is.GreaterThan(magic));effect.Magic=false;
            Assert.That(resolver.DamagePreview(player,enemy,effect,skill),Is.EqualTo(normal));
        }
        [Test] public void BowCleansesRootOnlyAfterEffectiveHealing()
        {
            Signature(EquipmentEffect.RoyalMercy);player.AddStatus(StatusKind.Root,2);var heal=Skill(EffectKind.Heal,TargetType.Self);
            Assert.That(resolver.Execute(player,heal,player.Position,out _),Is.True);Assert.That(player.Has(StatusKind.Root),Is.True);
            player.Acted=false;player.CurrentHP-=10;Assert.That(resolver.Execute(player,heal,player.Position,out _),Is.True);Assert.That(player.Has(StatusKind.Root),Is.False);
        }
        [Test] public void NebilimReducesHealthCostWithRoundingAndNoFreeSuicide()
        {
            Signature(EquipmentEffect.FlamingOath);var skill=Skill(EffectKind.Damage);skill.HPCost=11;
            Assert.That(resolver.HPCost(player,skill),Is.EqualTo(8));player.CurrentHP=8;
            Assert.That(resolver.Execute(player,skill,enemy.Position,out _),Is.False);Assert.That(player.CurrentHP,Is.EqualTo(8));
        }
        [Test] public void CoreAwardsGaugeOnlyForEffectiveHealing()
        {
            Signature(EquipmentEffect.ThornRelease);var heal=Skill(EffectKind.Heal,TargetType.Self);
            Assert.That(resolver.Execute(player,heal,player.Position,out _),Is.True);Assert.That(player.SpecialGauge,Is.Zero);
            player.CurrentHP-=10;player.Acted=false;Assert.That(resolver.Execute(player,heal,player.Position,out _),Is.True);Assert.That(player.SpecialGauge,Is.EqualTo(10));
        }
        [Test] public void ShieldRequiresGuardAndItsCorrectOwner()
        {
            var skill=Skill(EffectKind.Damage);var effect=skill.Effects[0];int baseline=resolver.DamagePreview(enemy,player,effect,skill);
            Signature(EquipmentEffect.UnbrokenShield);Assert.That(resolver.DamagePreview(enemy,player,effect,skill),Is.EqualTo(baseline));
            player.AddStatus(StatusKind.Guard,2);player.Equipment[0]=null;int guard=resolver.DamagePreview(enemy,player,effect,skill);
            Signature(EquipmentEffect.UnbrokenShield);Assert.That(resolver.DamagePreview(enemy,player,effect,skill),Is.EqualTo(Mathf.RoundToInt(guard*.85f)).Within(1));
        }
    }
}
