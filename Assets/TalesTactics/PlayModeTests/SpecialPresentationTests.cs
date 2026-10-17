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
                foreach(var clip in new[]{data.Poses.Skill,data.Poses.Ultimate})
                {
                    var unique=new HashSet<Sprite>();
                    Assert.That(clip.Frames.Length,Is.GreaterThanOrEqualTo(4),data.Id);
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
                    foreach(var skill in new[]{data.Skills[0],data.UltimateSkill}.Concat(data.Skills.Where(s=>s.Id=="jade.3"||s.Id=="jade.6"||s.Id=="natalia.6")).Distinct())
                    {
                        // Healing/earth spells use the authored directional casting pose;
                        // Consume Claw and Revive's protective stance use Guard. Ultimates
                        // keep their separate authored phase clips in every camera direction.
                        bool casting=new[]{"mint.0","tear.0","jade.0","jade.3","jade.6"}.Contains(skill.Id);
                        bool guarding=skill.Id=="velvet.0"||skill.Id=="natalia.6";
                        var clip=skill.IsUltimate?data.Poses.Ultimate:skill.Id=="cless.0"?data.Poses.AttackMotion:data.Poses.Skill;
                        float previousScale=Time.timeScale;
                        try
                        {
                            // Inspect phase starts without a slow editor frame advancing past them.
                            Time.timeScale=0;
                            motion.BeginSkill(skill,0.4f,0.3f);yield return new WaitForEndOfFrame();
                            Assert.That(renderer.sprite,Is.EqualTo(casting?data.Poses.Cast.Get(facing):guarding?data.Poses.Guard.Get(facing):clip.Frames[0].Get(facing)),skill.Id+facing+" preparation");
                            motion.ReleaseSkill();yield return new WaitForEndOfFrame();
                            Assert.That(renderer.sprite,Is.EqualTo(casting?data.Poses.Cast.Get(facing):guarding?data.Poses.Guard.Get(facing):clip.Frames[clip.ReleaseFrame].Get(facing)),skill.Id+facing+" release");
                            Time.timeScale=previousScale;
                            yield return new WaitForSeconds(0.35f);yield return new WaitForEndOfFrame();
                            Assert.That(renderer.sprite,Is.EqualTo(data.Sprites.Get(facing)));
                        }
                        finally {Time.timeScale=previousScale;}
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
            var parents=new List<GameObject>();var contact=new List<string>();
            try
            {
                // Separate parents make every signature include only this character's owned pool.
                // Initialize all ten in one frame so animation clock differences cannot fake uniqueness.
                foreach(var data in catalog)
                {
                    var parent=new GameObject("Finale shape container");parent.transform.SetParent(director.transform);parents.Add(parent);
                    var g=new GameObject("Finale shape test");g.transform.SetParent(parent.transform);
                    var skill=data.UltimateSkill;var effect=g.AddComponent<CombatEffect>();
                    effect.Initialize(director.Board.SpriteMaterial,director.Board.BattleCamera,CombatEffect.Resolve(data,skill),
                        CombatFeedback.Strike,unit,Vector3.zero,Vector3.one,Color.cyan,true,skill.Presentation,skill.Element,false,false,SkillSignatures.Resolve(skill),ArteProfiles.Resolve(skill));
                    Assert.That(parent.GetComponentsInChildren<ImpactParticles>().Length,Is.EqualTo(1),data.Id);
                    contact.Add(Signature(parent));
                }
                yield return new WaitForSeconds(.1f);
                var shapes=new HashSet<string>();
                for(int i=0;i<catalog.Length;i++)
                {
                    string later=Signature(parents[i]);
                    Assert.That(contact[i].Length,Is.GreaterThan(0),catalog[i].Id+" must render at contact");
                    Assert.That(later.Length,Is.GreaterThan(0),catalog[i].Id+" must render after contact");
                    Assert.That(shapes.Add(contact[i]+"|after contact|"+later),Is.True,catalog[i].Id+" needs a distinct finale");
                }
                Assert.That(shapes.Count,Is.EqualTo(10),"All ten rendered finales must remain distinct");
                yield return new WaitForSeconds(.75f);
                Assert.That(Object.FindObjectsByType<CombatEffect>(),Is.Empty);
                Assert.That(unit.CurrentHP,Is.EqualTo(hp),"Visual pulses cannot apply gameplay damage");
            }
            finally{foreach(var parent in parents)if(parent!=null)Object.Destroy(parent);}

            string Signature(GameObject parent)
            {
                var result=new System.Text.StringBuilder();
                foreach(var line in parent.GetComponentsInChildren<LineRenderer>().Where(l=>l.enabled&&l.positionCount>0))
                {
                    result.Append("line:").Append(line.positionCount).Append(':').Append(line.sharedMaterial.name);
                    for(int i=0;i<line.positionCount;i++)result.Append(line.GetPosition(i).ToString("F3"));
                    result.Append(line.startColor.ToString("F3")).Append(line.endColor.ToString("F3"));
                    result.Append(line.startWidth.ToString("F3")).Append(line.endWidth.ToString("F3"));
                }
                foreach(var impact in parent.GetComponentsInChildren<ImpactParticles>())
                foreach(var filter in impact.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer=filter.GetComponent<MeshRenderer>();var mesh=filter.sharedMesh;
                    if(!renderer.enabled||mesh.vertexCount==0||mesh.colors.All(c=>c.a<=.001f))continue;
                    var material=renderer.sharedMaterial;
                    result.Append("mesh:").Append(mesh.vertexCount).Append(':').Append(material.name)
                        .Append(':').Append(material.mainTexture!=null?material.mainTexture.name:"none");
                    foreach(var vertex in mesh.vertices)result.Append(filter.transform.TransformPoint(vertex).ToString("F3"));
                    foreach(var uv in mesh.uv)result.Append(uv.ToString("F3"));
                    var next=new List<Vector3>();mesh.GetUVs(1,next);
                    foreach(var uv in next)result.Append(uv.ToString("F3"));
                    foreach(var color in mesh.colors)result.Append(color.ToString("F3"));
                    foreach(int index in mesh.triangles)result.Append(index).Append(',');
                }
                return result.ToString();
            }
        }

        [UnityTest] public IEnumerator SkillWindupDefersSingleResolutionAndRestartClearsCues()
        {
            director.BeginBattle();yield return null;
            var caster=director.Session.Active;var enemy=director.Session.Units.First(u=>u.Team==Team.Enemy);
            var skill=PresentationSkill(TargetType.Enemy,new SkillEffect{Kind=EffectKind.Damage,Flat=17,Power=0,IgnoreDefense=true});
            try
            {
                skill.MPCost=3;skill.Presentation=new SkillPresentation{Pattern=SkillVisualPattern.Burst,Pulses=3,Windup=0.3f,Recovery=0.4f};
                int hp=enemy.CurrentHP,mp=caster.CurrentMP;int damage=director.Session.Resolver.DamagePreview(caster,enemy,skill.Effects[0],skill);
                director.StartCoroutine(director.Execute(skill,enemy.Position));
                yield return new WaitForSeconds(0.1f);
                Assert.That(enemy.CurrentHP,Is.EqualTo(hp));Assert.That(caster.CurrentMP,Is.EqualTo(mp));
                Assert.That(Object.FindObjectsByType<ImpactParticles>().Any(p=>p.Preparing&&p.GetComponentsInChildren<MeshFilter>().Any(m=>m.sharedMesh.vertexCount>0)),Is.True);
                yield return new WaitForSeconds(0.75f);
                Assert.That(enemy.CurrentHP,Is.EqualTo(hp-damage));Assert.That(caster.CurrentMP,Is.EqualTo(mp-3));
                Assert.That(GameObject.Find("Skill preparation"),Is.Null);Assert.That(GameObject.Find("Skill name"),Is.Null);
                director.Board.BeginSkill(caster,skill,enemy.Position);director.Restart();yield return null;yield return null;
                Assert.That(GameObject.Find("Skill preparation"),Is.Null);Assert.That(GameObject.Find("Skill name"),Is.Null);
            }
            finally{Object.Destroy(skill);}
        }

        [UnityTest] public IEnumerator LongSkillNameFitsContrastPlateAndRestartClearsIt()
        {
            director.BeginBattle();yield return null;
            var caster=director.Session.Active;
            var skill=Object.Instantiate(caster.Data.Skills[0]);
            try
            {
                skill.DisplayName="빛과 어둠을 가르는 최후의 심판";skill.Animation=AnimationKind.Cast;
                skill.Presentation=new SkillPresentation{Windup=1.5f};
                director.Board.BeginSkill(caster,skill,caster.Position);
                yield return null;
                var label=GameObject.Find("Skill name").GetComponent<TMPro.TextMeshPro>();label.ForceMeshUpdate();
                Assert.That(label.text,Is.EqualTo(skill.DisplayName));Assert.That(label.isTextOverflowing,Is.False);
                Assert.That(label.textInfo.lineCount,Is.EqualTo(1));Assert.That(label.color.r,Is.GreaterThan(.9f));
                Assert.That(label.enableAutoSizing,Is.False);
                var backing=label.transform.Find("Skill name backing");Assert.That(backing,Is.Not.Null);
                Assert.That(label.preferredWidth,Is.LessThanOrEqualTo(backing.localScale.x));
                director.Board.RotateCamera(90);yield return null;
                Assert.That(Quaternion.Angle(label.transform.rotation,director.Board.BattleCamera.transform.rotation),Is.LessThan(.1f));
                director.Restart();yield return null;yield return null;
                Assert.That(GameObject.Find("Skill name backing"),Is.Null);Assert.That(GameObject.Find("Skill name"),Is.Null);
            }
            finally {Object.Destroy(skill);}
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
