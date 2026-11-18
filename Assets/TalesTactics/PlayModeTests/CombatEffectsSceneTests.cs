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
            float captureDelta=Time.captureDeltaTime,timeScale=Time.timeScale;
            try
            {
                // Fixed simulation steps keep the short attack sample inside its motion window.
                // Observe only after LateUpdate, which owns the sprite's transform and idle shader.
                Time.timeScale=1;Time.captureDeltaTime=1f/60f;yield return null;
                director.BeginBattle();yield return new WaitForEndOfFrame();
                var caster=director.Session.Active;
                Assert.That(caster.Team,Is.EqualTo(Team.Player),"This motion fixture must not run an automatic enemy turn.");
                var motion=Object.FindObjectsByType<CharacterMotion>().Single(m=>m.Owner==caster);
                var renderer=motion.GetComponent<SpriteRenderer>();var block=new MaterialPropertyBlock();
                var clock=typeof(CharacterMotion).GetField("started",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                Assert.That(clock,Is.Not.Null);
                float Started()=>(float)clock.GetValue(motion);
                string Describe(string phase)=>$"{phase}: frame={Time.frameCount}, time={Time.time:F6}, started={Started():F6}, elapsed={Time.time-Started():F6}, delta={Time.deltaTime:F6}, action={motion.Action}, presenting={motion.Presenting}, x={motion.transform.localPosition.x:F6}";
                void AssertIdleShader(bool active,string phase)
                {
                    renderer.GetPropertyBlock(block);
                    Assert.That(block.GetVector("_IdleMotion").w,Is.EqualTo(active?1f:0f),Describe(phase));
                }

                director.Board.SetAnimation(caster,AnimationKind.Attack);float firstStarted=Started();
                Assert.That(firstStarted,Is.EqualTo(Time.time),Describe("First attack starts now"));
                yield return new WaitForSeconds(0.5f);yield return new WaitForEndOfFrame();
                Assert.That(motion.Action,Is.EqualTo(AnimationKind.Attack),Describe("First attack remains selected"));
                Assert.That(Started(),Is.EqualTo(firstStarted),Describe("First attack clock must not restart itself"));
                Assert.That(Mathf.Abs(motion.transform.localPosition.x),Is.LessThan(0.01f),Describe("First attack returns to its planted position"));
                AssertIdleShader(false,"Attack must not apply idle deformation");

                director.Board.SetAnimation(caster,AnimationKind.Attack);float secondStarted=Started();
                Assert.That(secondStarted,Is.EqualTo(Time.time),Describe("Repeated attack starts now"));
                Assert.That(secondStarted,Is.GreaterThan(firstStarted),Describe("Repeated attack must restart the clock"));
                yield return new WaitForSeconds(0.12f);yield return new WaitForEndOfFrame();
                Assert.That(motion.Action,Is.EqualTo(AnimationKind.Attack),Describe("Repeated attack remains selected"));
                Assert.That(Started(),Is.EqualTo(secondStarted),Describe("Repeated attack clock remains stable"));
                Assert.That(Mathf.Abs(motion.transform.localPosition.x),Is.GreaterThan(0.03f),Describe("Repeated attack performs a new lunge"));
                AssertIdleShader(false,"Repeated attack must not apply idle deformation");

                director.Board.ShowTimingSpin(caster,0);float spinStarted=Started();
                Assert.That(spinStarted,Is.EqualTo(Time.time),Describe("Timing spin starts now"));
                Assert.That(spinStarted,Is.GreaterThan(secondStarted),Describe("Timing spin starts a new motion"));
                for(int frame=0;frame<=9;frame++)
                {
                    director.Board.ShowTimingSpin(caster,frame/9f);yield return new WaitForEndOfFrame();
                    Assert.That(motion.Action,Is.EqualTo(AnimationKind.Skill),Describe("Timing spin stays selected"));
                    Assert.That(Started(),Is.EqualTo(spinStarted),Describe("Repeated timing updates must not restart the clock"));
                    Assert.That(Mathf.Abs(motion.transform.localPosition.x),Is.LessThan(0.01f),Describe("Spin stays grounded instead of lunging every update"));
                    AssertIdleShader(false,"Timing spin must not apply idle deformation");
                }
                director.Board.ClearTiming();yield return new WaitForEndOfFrame();
                Assert.That(motion.Action,Is.EqualTo(AnimationKind.Idle),Describe("Clearing timing restores idle"));
                Assert.That(Mathf.Abs(motion.transform.localPosition.x),Is.LessThan(0.01f),Describe("Idle keeps the feet planted"));
                AssertIdleShader(true,"Clearing timing restores idle presentation");
            }
            finally
            {
                Time.captureDeltaTime=captureDelta;Time.timeScale=timeScale;
                if(director!=null&&director.Board!=null)director.Board.ClearTiming();
            }
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
            // Destroy is deferred to the frame boundary; allow the busy Editor to render it.
            float expiryDeadline=Time.realtimeSinceStartup+1.5f;
            while(Object.FindObjectsByType<CombatEffect>().Length>0&&Time.realtimeSinceStartup<expiryDeadline)yield return new WaitForEndOfFrame();
            Assert.That(Object.FindObjectsByType<CombatEffect>(),Is.Empty);
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
                var effects=Object.FindObjectsByType<CombatEffect>();
                Assert.That(effects.Count(e=>e.Recipient==null),Is.EqualTo(1));
                var contacts=effects.Where(e=>e.Recipient!=null).ToArray();
                Assert.That(contacts.Select(e=>e.Recipient),Is.EquivalentTo(recipients));
                Assert.That(contacts.All(e=>e.Feedback==CombatFeedback.Strike&&!e.Recipient.Alive),Is.True);
                director.Restart();yield return null;yield return null;
                Assert.That(Object.FindObjectsByType<CombatEffect>(),Is.Empty);
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
                Assert.That(Object.FindObjectsByType<CombatEffect>().Any(e=>e.Recipient==caster),Is.False);
                var motion=Object.FindObjectsByType<CharacterMotion>().Single(m=>m.Owner==caster);
                Assert.That(motion.Action,Is.EqualTo(AnimationKind.Ultimate));
                Assert.That(Object.FindObjectsByType<TMPro.TMP_Text>().Any(t=>t.text=="HP -10"),Is.True);
            }
            finally{Object.Destroy(heal);Object.Destroy(revive);Object.Destroy(attack);}
        }
    }
}
