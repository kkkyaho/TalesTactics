using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator OriginalMusicCatalogPlaysEveryRegisteredTrack()
        {
            var music=director.Audio.Library.Entries.Where(e=>e.Id=="battle"||e.Id=="boss"||e.Id=="story"||e.Id=="victory"||e.Id.EndsWith(".theme")).ToArray();
            Assert.That(music.Length,Is.EqualTo(14));
            foreach(var entry in music)
            {
                Assert.That(entry.Clip,Is.Not.Null,entry.Id);Assert.That(entry.Clip.channels,Is.EqualTo(2));
                director.Audio.Play(entry.Id);yield return new WaitForSeconds(0.15f);
                var source=director.Audio.GetComponent<AudioSource>();
                Assert.That(source.isPlaying,Is.True,entry.Id);Assert.That(source.timeSamples,Is.GreaterThan(0),entry.Id);
                Assert.That(source.volume,Is.EqualTo(director.Audio.MusicVolume));
            }
            director.Audio.StopAll();
        }

        [UnityTest] public IEnumerator TimingSpinCyclesFourViewsWithoutChangingCombatFacing()
        {
            director.BeginBattle();yield return null;
            var owner=director.Session.Units.First(u=>u.Data.Id=="farah");var original=owner.Facing;
            var frames=new HashSet<Sprite>();
            var motions=Object.FindObjectsByType<CharacterMotion>();
            var motion=motions.Single(m=>(UnitRuntime)typeof(CharacterMotion).GetField("unit",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(m)==owner);
            for(int i=0;i<4;i++)
            {
                director.Board.ShowTimingSpin(owner,i/8f);yield return new WaitForEndOfFrame();
                frames.Add(motion.GetComponent<SpriteRenderer>().sprite);
                Assert.That(owner.Facing,Is.EqualTo(original));
            }
            Assert.That(frames.Count,Is.EqualTo(4));
            Assert.That(GameObject.Find("Timing success window"),Is.Not.Null);
            director.Board.ClearTiming();yield return null;
            Assert.That(motion.Action,Is.EqualTo(AnimationKind.Idle));
            Assert.That(GameObject.Find("Timing success window"),Is.Null);
            director.Board.ShowTimingSpin(owner,0.5f);director.Restart();yield return null;yield return null;
            Assert.That(GameObject.Find("Timing arc"),Is.Null);
            Assert.That(GameObject.Find("Timing success window"),Is.Null);
        }

        [UnityTest] public IEnumerator SpecialPosesPrepareReleaseAndRecoverForEntireRoster()
        {
            var camera=director.Board.BattleCamera;
            foreach(var data in director.Catalog.Characters)
            {
                var unique=new HashSet<Sprite>();
                foreach(var clip in new[]{data.Poses.Skill,data.Poses.Ultimate})
                {
                    Assert.That(clip.Frames.Length,Is.EqualTo(2),data.Id);
                    foreach(var frame in clip.Frames)foreach(Facing f in System.Enum.GetValues(typeof(Facing)))
                    {Assert.That(frame.Get(f),Is.Not.Null);Assert.That(unique.Add(frame.Get(f)),Is.True);}
                }
                var go=new GameObject("Special pose test");go.transform.SetParent(director.transform);
                var renderer=go.AddComponent<SpriteRenderer>();var motion=go.AddComponent<CharacterMotion>();
                var owner=new UnitRuntime(data,Team.Player,director.Catalog.Rules);motion.Initialize(renderer,owner,camera);
                for(int rotation=0;rotation<4;rotation++)
                {
                    camera.transform.rotation=Quaternion.Euler(45,rotation*90,0);
                    var facing=CharacterMotion.ViewFacing(owner.Facing,camera.transform.rotation);
                    foreach(var skill in new[]{data.Skills[0],data.UltimateSkill})
                    {
                        var clip=skill.IsUltimate?data.Poses.Ultimate:data.Poses.Skill;
                        motion.BeginSkill(skill,0.2f,0.1f);yield return new WaitForEndOfFrame();
                        Assert.That(renderer.sprite,Is.EqualTo(clip.Frames[0].Get(facing)),data.Id+facing);
                        motion.ReleaseSkill();yield return new WaitForEndOfFrame();
                        Assert.That(renderer.sprite,Is.EqualTo(clip.Frames[1].Get(facing)),data.Id+facing);
                        yield return new WaitForSeconds(0.12f);yield return new WaitForEndOfFrame();
                        Assert.That(renderer.sprite,Is.EqualTo(data.Sprites.Get(facing)));
                    }
                }
                Object.Destroy(go);
            }
        }

        [UnityTest] public IEnumerator CatalogProfilesRenderDistinctFinalesWithoutChangingHealth()
        {
            var catalog=director.Catalog.Characters;
            var all=catalog.SelectMany(c=>c.Skills.Concat(new[]{c.BasicAttack,c.UltimateSkill})).Distinct().ToArray();
            Assert.That(all.Length,Is.EqualTo(88));
            Assert.That(all.All(s=>s.Presentation!=null&&s.Presentation.Pattern!=SkillVisualPattern.Automatic),Is.True);
            Assert.That(catalog.All(c=>c.BasicAttack.Presentation.Pattern==SkillVisualPattern.CharacterStyle),Is.True,"Basic attacks retain each character's weapon effects");
            Assert.That(catalog.Select(c=>c.UltimateSkill.Presentation.Pattern).Distinct().Count(),Is.EqualTo(10));
            director.BeginBattle();yield return null;var unit=director.Session.Active;int hp=unit.CurrentHP;
            var shapes=new HashSet<string>();
            foreach(var data in catalog)
            {
                var g=new GameObject("Finale shape test");g.transform.SetParent(director.transform);
                var effect=g.AddComponent<CombatEffect>();
                effect.Initialize(director.Board.SpriteMaterial,director.Board.BattleCamera,data.VisualStyle,
                    CombatFeedback.Strike,unit,Vector3.zero,Vector3.one,Color.cyan,true,data.UltimateSkill.Presentation);
                var signature=string.Join(";",g.GetComponentsInChildren<LineRenderer>().Select(l=>
                    l.positionCount+":"+string.Join(",",Enumerable.Range(0,l.positionCount).Select(i=>l.GetPosition(i).ToString("F3")))));
                Assert.That(shapes.Add(signature),Is.True,data.Id+" needs a distinct finale");
            }
            yield return new WaitForSeconds(0.75f);
            Assert.That(Object.FindObjectsByType<CombatEffect>(),Is.Empty);
            Assert.That(unit.CurrentHP,Is.EqualTo(hp),"Visual pulses cannot apply gameplay damage");
        }

        [UnityTest] public IEnumerator SkillWindupDefersSingleResolutionAndRestartClearsCues()
        {
            director.BeginBattle();yield return null;
            var caster=director.Session.Active;var enemy=director.Session.Units.First(u=>u.Team==Team.Enemy);
            var skill=PresentationSkill(TargetType.Enemy,new SkillEffect{Kind=EffectKind.Damage,Flat=17,Power=0,IgnoreDefense=true});
            try
            {
                skill.MPCost=3;skill.Presentation=new SkillPresentation{Pattern=SkillVisualPattern.Burst,Pulses=3,Windup=0.3f,Recovery=0.4f};
                int hp=enemy.CurrentHP,mp=caster.CurrentMP;
                director.StartCoroutine(director.Execute(skill,enemy.Position));
                yield return new WaitForSeconds(0.1f);
                Assert.That(enemy.CurrentHP,Is.EqualTo(hp));Assert.That(caster.CurrentMP,Is.EqualTo(mp));
                Assert.That(GameObject.Find("Skill preparation"),Is.Not.Null);
                yield return new WaitForSeconds(0.75f);
                Assert.That(enemy.CurrentHP,Is.EqualTo(hp-17));Assert.That(caster.CurrentMP,Is.EqualTo(mp-3));
                Assert.That(GameObject.Find("Skill preparation"),Is.Null);Assert.That(GameObject.Find("Skill name"),Is.Null);
                director.Board.BeginSkill(caster,skill,enemy.Position);director.Restart();yield return null;yield return null;
                Assert.That(GameObject.Find("Skill preparation"),Is.Null);Assert.That(GameObject.Find("Skill name"),Is.Null);
            }
            finally{Object.Destroy(skill);}
        }

        [UnityTest] public IEnumerator MusicThemeRestoresPositionAndMissingThemePreservesBattle()
        {
            var original=director.Audio.Library;var library=ScriptableObject.CreateInstance<AudioLibrary>();
            var battleClip=AudioClip.Create("test battle",441000,1,44100,false);
            var themeClip=AudioClip.Create("test theme",441000,1,44100,false);
            var bossClip=AudioClip.Create("test boss",441000,1,44100,false);
            try
            {
                library.Entries=new[]{new AudioEntry{Id="battle",Clip=battleClip},new AudioEntry{Id="boss",Clip=bossClip},new AudioEntry{Id="test.theme",Clip=themeClip}};
                director.Audio.Library=library;director.Audio.PlayBattle(false);
                var source=director.Audio.GetComponent<AudioSource>();source.timeSamples=44100;
                director.Audio.BeginTheme("missing.theme");Assert.That(source.clip,Is.EqualTo(battleClip));
                director.Audio.BeginTheme("test.theme");Assert.That(source.clip,Is.EqualTo(themeClip));
                director.Audio.EndTheme();Assert.That(source.clip,Is.EqualTo(battleClip));Assert.That(source.timeSamples,Is.InRange(44100,50000));
                director.Audio.PlayBattle(true);Assert.That(source.clip,Is.EqualTo(bossClip));
                director.Audio.BeginTheme("test.theme");director.Audio.StopAll();director.Audio.EndTheme();
                Assert.That(source.isPlaying,Is.False);yield return null;
            }
            finally{director.Audio.StopAll();director.Audio.Library=original;Object.Destroy(library);Object.Destroy(battleClip);Object.Destroy(themeClip);Object.Destroy(bossClip);}
        }
    }
}
