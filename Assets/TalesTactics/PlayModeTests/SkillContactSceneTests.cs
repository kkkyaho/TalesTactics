using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator ProjectileArrivesBeforeDamageCostsAndNumbersAreAppliedOnce()
        {
            director.Preferences.Reactions=false;director.BeginBattle();yield return null;
            var actor=director.Session.Active;var target=director.Session.Units.First(u=>u.Team==Team.Enemy);
            var skill=PresentationSkill(TargetType.Enemy,new SkillEffect{Kind=EffectKind.Damage,Flat=10,Power=0,IgnoreDefense=true});
            skill.Element=Element.Fire;skill.Animation=AnimationKind.Cast;skill.VisualStyle=CombatVisualStyle.Sacred;skill.Presentation.Pattern=SkillVisualPattern.Meteor;
            skill.MPCost=5;skill.HPCost=3;
            try
            {
                int hp=target.CurrentHP,mp=actor.CurrentMP,ownHP=actor.CurrentHP;var cell=actor.Position;
                string random=JsonUtility.ToJson(Random.state);float timeScale=Time.timeScale;
                director.StartCoroutine(director.Execute(skill,target.Position));
                float deadline=Time.realtimeSinceStartup+5;
                while(!director.Board.GetComponentsInChildren<ImpactParticles>().Any(p=>p.Travelling)&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(director.Board.GetComponentsInChildren<ImpactParticles>().Any(p=>p.Travelling),Is.True,"Release flight never appeared");
                Assert.That(target.CurrentHP,Is.EqualTo(hp));Assert.That(actor.CurrentMP,Is.EqualTo(mp));Assert.That(actor.CurrentHP,Is.EqualTo(ownHP));
                Assert.That(director.Hud.HasHealthFeedback(target),Is.False);Assert.That(actor.Position,Is.EqualTo(cell));
                Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random));Assert.That(Time.timeScale,Is.EqualTo(timeScale));
                deadline=Time.realtimeSinceStartup+5;
                while(target.CurrentHP==hp&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(target.CurrentHP,Is.LessThan(hp));Assert.That(director.Hud.HasHealthFeedback(target),Is.True);
                int resolvedHP=target.CurrentHP;
                Assert.That(actor.CurrentMP,Is.EqualTo(mp-5));Assert.That(actor.CurrentHP,Is.EqualTo(ownHP-3));
                deadline=Time.realtimeSinceStartup+5;
                while(director.State is ActionExecutionState&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(director.State,Is.TypeOf<CommandState>());Assert.That(target.CurrentHP,Is.EqualTo(resolvedHP));
                Assert.That(actor.CurrentMP,Is.EqualTo(mp-5));Assert.That(actor.CurrentHP,Is.EqualTo(ownHP-3));
            }
            finally{Object.Destroy(skill);}
        }
        [UnityTest] public IEnumerator AreaImpactHasOneCenterpieceAndKeepsEveryRecipientContact()
        {
            director.BeginBattle();yield return null;var actor=director.Session.Active;
            var skill=PresentationSkill(TargetType.Enemy,new SkillEffect{Kind=EffectKind.Damage,Flat=99999,Power=0,IgnoreDefense=true});
            skill.Area=30;skill.Element=Element.Ice;skill.Presentation.Pattern=SkillVisualPattern.Ice;
            try
            {
                var aim=director.Session.Units.First(u=>u.Team==Team.Enemy).Position;
                var recipients=director.Session.Resolver.Targets(actor,skill,aim).ToArray();var before=director.Board.CaptureHealth();
                Assert.That(recipients.Length,Is.GreaterThan(1));
                Assert.That(director.Session.Resolver.Execute(actor,skill,aim,out var message),Is.True,message);
                director.Board.PresentImpact(actor,skill,aim,before,recipients);
                var impacts=director.Board.GetComponentsInChildren<ImpactParticles>();
                Assert.That(impacts.Count(p=>!p.Compact),Is.EqualTo(1),"Area size must not multiply the large effect");
                Assert.That(impacts.Count(p=>p.Compact),Is.EqualTo(recipients.Length));
                var effects=director.Board.GetComponentsInChildren<CombatEffect>();
                Assert.That(effects.Count(e=>e.Recipient==null),Is.EqualTo(1));
                Assert.That(effects.Where(e=>e.Recipient!=null).Select(e=>e.Recipient),Is.EquivalentTo(recipients));
                foreach(var recipient in recipients)Assert.That(director.Hud.HasHealthFeedback(recipient),Is.True);
                director.Restart();yield return null;yield return null;
                Assert.That(director.Board.GetComponentsInChildren<ImpactParticles>(),Is.Empty);
            }
            finally{Object.Destroy(skill);}
        }
        [UnityTest] public IEnumerator AreaTimeStopAndCageKeepOneSignatureAndAllActualContacts()
        {
            foreach(string id in new[]{"mint","jade"})
            {
                var character=director.Catalog.Characters.Single(c=>c.Id==id);
                director.Deployment.Clear();director.Deployment.AddRange(new[]{0,System.Array.IndexOf(director.Catalog.Characters,character)});
                director.BeginBattle();yield return null;
                var actor=director.Session.Units.Single(u=>u.Team==Team.Player&&u.Data.Id==id);
                var skill=Object.Instantiate(character.UltimateSkill);
                // Expand only fixture reach/cost to exercise several real recipients in the initial map.
                skill.Area=30;skill.Range=30;skill.MinRange=0;skill.MPCost=0;skill.GaugeCost=0;
                try
                {
                    var aim=director.Session.Units.First(u=>u.Team==Team.Enemy).Position;
                    var recipients=director.Session.Resolver.Targets(actor,skill,aim).ToArray();var before=director.Board.CaptureHealth();
                    Assert.That(recipients.Length,Is.GreaterThan(1));
                    Assert.That(director.Session.Resolver.Execute(actor,skill,aim,out var message),Is.True,message);
                    var health=director.Session.Units.Select(u=>u.CurrentHP).ToArray();var random=JsonUtility.ToJson(Random.state);
                    director.Board.PresentImpact(actor,skill,aim,before,recipients,director.Session.Resolver.PrimaryHealth);
                    var effects=director.Board.GetComponentsInChildren<CombatEffect>();var main=effects.Single(e=>e.Recipient==null);
                    Assert.That(main.Pattern,Is.EqualTo(id=="mint"?SkillVisualPattern.TimeStop:SkillVisualPattern.Cage));
                    Assert.That(main.GetComponentsInChildren<LineRenderer>().Count(l=>l.positionCount>0),Is.GreaterThanOrEqualTo(7),id+" lost its clock/cage signature");
                    var contacts=effects.Where(e=>e.Recipient!=null).ToArray();
                    Assert.That(contacts.Select(e=>e.Recipient),Is.EquivalentTo(recipients));
                    Assert.That(contacts.All(e=>e.GetComponentsInChildren<LineRenderer>().Length==0),Is.True,"Do not duplicate the large signature at every target");
                    var particles=director.Board.GetComponentsInChildren<ImpactParticles>();
                    Assert.That(particles.Count(p=>!p.Compact),Is.EqualTo(1));Assert.That(particles.Count(p=>p.Compact),Is.EqualTo(recipients.Length));
                    Assert.That(director.Session.Units.Select(u=>u.CurrentHP),Is.EqualTo(health));Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random));
                }
                finally{Object.Destroy(skill);director.Restart();}
                yield return null;yield return null;
                Assert.That(director.Board.GetComponentsInChildren<CombatEffect>(),Is.Empty);
            }
        }
        [UnityTest] public IEnumerator SkillCuesRespectMuteOverlapAndMusicWithoutCombatRandomness()
        {
            var audio=director.Audio;var skill=PresentationSkill(TargetType.Enemy,new SkillEffect{Kind=EffectKind.Damage});skill.Element=Element.Fire;
            var music=audio.GetComponent<AudioSource>();var previous=music.clip;var random=JsonUtility.ToJson(Random.state);
            try
            {
                audio.StopAll();audio.SetVolumes(.28f,0);audio.PlaySkillCue(skill,CombatVisualStyle.Sacred,true);
                Assert.That(audio.ActiveEffectVoices,Is.Zero);
                audio.SetVolumes(.28f,.45f);audio.PlaySkillCue(skill,CombatVisualStyle.Sacred,true);audio.PlaySkillCue(skill,CombatVisualStyle.Sacred,true);
                Assert.That(audio.ActiveEffectVoices,Is.EqualTo(1));Assert.That(audio.LastEffectGain,Is.EqualTo(1));
                audio.StopAll();audio.PlaySkillCue(skill,CombatVisualStyle.Sacred,true,true);
                Assert.That(audio.LastEffectGain,Is.LessThan(1));Assert.That(music.clip,Is.EqualTo(previous));
                Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random));
                audio.StopAll();Assert.That(audio.GetComponents<AudioSource>().All(s=>!s.isPlaying),Is.True);
                yield return null;
            }
            finally{audio.StopAll();Object.Destroy(skill);}
        }
    }
}
