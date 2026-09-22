using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator ConsecutiveAttackMotionRestartsWithoutRestartingTimingEveryFrame()
        {
            director.BeginBattle();yield return null;
            var caster=director.Session.Active;
            var motion=Object.FindObjectsByType<CharacterMotion>(FindObjectsSortMode.None).Single(m=>m.transform.parent.name==caster.Data.DisplayName);
            director.Board.SetAnimation(caster,AnimationKind.Attack);yield return new WaitForSeconds(0.5f);
            Assert.That(Mathf.Abs(motion.transform.localPosition.x),Is.LessThan(0.01f));
            director.Board.SetAnimation(caster,AnimationKind.Attack);yield return new WaitForSeconds(0.12f);
            Assert.That(Mathf.Abs(motion.transform.localPosition.x),Is.GreaterThan(0.03f));
            for(float t=0;t<0.15f;t+=Time.deltaTime){director.Board.ShowTimingSpin(caster,t);yield return null;}
            Assert.That(Mathf.Abs(motion.transform.localPosition.x),Is.GreaterThan(0.03f));
            director.Board.ClearTiming();
        }
        SkillData PresentationSkill(TargetType target,params SkillEffect[] effects)
        {
            var skill=ScriptableObject.CreateInstance<SkillData>();skill.Id="presentation-test";
            skill.Target=target;skill.MinRange=0;skill.Range=30;skill.Area=0;skill.Effects=effects;return skill;
        }
        [UnityTest] public IEnumerator TenCharacterStylesHaveDistinctGeometryAndExpire()
        {
            Assert.That(director.Catalog.Characters.Select(c=>c.VisualStyle).Distinct().Count(),Is.EqualTo(10));
            Assert.That(director.Catalog.Characters.All(c=>c.VisualStyle!=CombatVisualStyle.Automatic),Is.True);
            director.BeginBattle();yield return null;
            var target=director.Session.Units.First();
            foreach(var data in director.Catalog.Characters)
            {
                var g=new GameObject("Style test");
                var effect=g.AddComponent<CombatEffect>();
                effect.Initialize(director.Board.SpriteMaterial,director.Board.BattleCamera,CombatEffect.Resolve(data,data.BasicAttack),
                    CombatFeedback.Strike,target,Vector3.zero,Vector3.one,Color.cyan,true);
                Assert.That(g.GetComponentsInChildren<LineRenderer>().Count(x=>x.positionCount>0),Is.GreaterThanOrEqualTo(3));
            }
            yield return new WaitForSeconds(0.75f);
            Assert.That(Object.FindObjectsByType<CombatEffect>(FindObjectsSortMode.None),Is.Empty);
        }
        [UnityTest] public IEnumerator LethalAreaEffectsRetainAllRecipientsAfterResolution()
        {
            director.BeginBattle();yield return null;
            var caster=director.Session.Active;
            var skill=PresentationSkill(TargetType.Enemy,new SkillEffect{Kind=EffectKind.Damage,Flat=99999,Power=0,IgnoreDefense=true});
            try
            {
                skill.Area=30;
                var aim=director.Session.Units.First(u=>u.Team==Team.Enemy).Position;
                var recipients=director.Session.Resolver.Targets(caster,skill,aim).ToArray();var before=director.Board.CaptureHealth();
                Assert.That(recipients.Length,Is.EqualTo(4));
                Assert.That(director.Session.Resolver.Execute(caster,skill,aim,out var error),Is.True,error);
                director.Board.PresentImpact(caster,skill,aim,before,recipients);yield return null;
                var effects=Object.FindObjectsByType<CombatEffect>(FindObjectsSortMode.None);
                Assert.That(effects.Select(e=>e.Recipient),Is.EquivalentTo(recipients));
                Assert.That(effects.All(e=>e.Feedback==CombatFeedback.Strike&&!e.Recipient.Alive),Is.True);
                director.Restart();yield return null;yield return null;
                Assert.That(Object.FindObjectsByType<CombatEffect>(FindObjectsSortMode.None),Is.Empty);
            }
            finally{Object.Destroy(skill);}
        }
        [UnityTest] public IEnumerator HealingRevivalAndHealthCostsUseSeparateFeedback()
        {
            director.BeginBattle();yield return null;
            var caster=director.Session.Active;
            var ally=director.Session.Units.First(u=>u.Team==Team.Player&&u!=caster);
            var enemy=director.Session.Units.First(u=>u.Team==Team.Enemy);
            var heal=PresentationSkill(TargetType.Ally,new SkillEffect{Kind=EffectKind.Heal,Flat=25,Power=0});
            var revive=PresentationSkill(TargetType.FallenAlly,new SkillEffect{Kind=EffectKind.Revive,Power=0.5f});
            var attack=PresentationSkill(TargetType.Enemy,new SkillEffect{Kind=EffectKind.Damage,Flat=10,Power=0,IgnoreDefense=true});
            try
            {
                ally.CurrentHP-=30;
                var before=director.Board.CaptureHealth();
                Assert.That(director.Session.Resolver.Execute(caster,heal,ally.Position,out var error),Is.True,error);
                director.Board.PresentImpact(caster,heal,ally.Position,before,new[]{ally});yield return null;
                Assert.That(Object.FindAnyObjectByType<CombatEffect>().Feedback,Is.EqualTo(CombatFeedback.Heal));
                yield return new WaitForSeconds(0.7f);
                ally.Damage(99999,director.Session.Grid);caster.Acted=false;before=director.Board.CaptureHealth();
                Assert.That(director.Session.Resolver.Execute(caster,revive,ally.Position,out error),Is.True,error);
                director.Board.PresentImpact(caster,revive,ally.Position,before,new[]{ally});yield return null;
                Assert.That(Object.FindAnyObjectByType<CombatEffect>().Feedback,Is.EqualTo(CombatFeedback.Revive));
                yield return new WaitForSeconds(0.7f);
                caster.Acted=false;attack.HPCost=10;before=director.Board.CaptureHealth();
                director.Board.SetAnimation(caster,AnimationKind.Ultimate);
                Assert.That(director.Session.Resolver.Execute(caster,attack,enemy.Position,out error),Is.True,error);
                director.Board.PresentImpact(caster,attack,enemy.Position,before,new[]{enemy});yield return null;
                Assert.That(Object.FindObjectsByType<CombatEffect>(FindObjectsSortMode.None).Any(e=>e.Recipient==caster),Is.False);
                var motion=Object.FindObjectsByType<CharacterMotion>(FindObjectsSortMode.None).Single(m=>m.transform.parent.name==caster.Data.DisplayName);
                Assert.That(motion.Action,Is.EqualTo(AnimationKind.Ultimate));
                Assert.That(Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None).Any(t=>t.text=="HP -10"),Is.True);
            }
            finally{Object.Destroy(heal);Object.Destroy(revive);Object.Destroy(attack);}
        }
    }
}
