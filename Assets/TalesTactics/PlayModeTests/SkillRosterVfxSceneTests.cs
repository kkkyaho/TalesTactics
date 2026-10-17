#if UNITY_EDITOR || TALESTACTICS_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [Test] public void SkillRosterProfilesCoverExactCatalogIdsWithoutReplacingTrioOrChangingAssets()
        {
            var entries=SkillRosterVfxFixture.Entries(director.Catalog);var allies=entries.Where(e=>e.Ally).ToArray();
            var source=entries.Select(e=>JsonUtility.ToJson(e.Skill)).ToArray();
            Assert.That(entries.Length,Is.EqualTo(182));Assert.That(allies.Length,Is.EqualTo(88));
            Assert.That(ArteProfiles.All.Count,Is.EqualTo(179));
            Assert.That(ArteProfiles.All.Select(p=>p.Id).Distinct(StringComparer.Ordinal).Count(),Is.EqualTo(179));
            CollectionAssert.AreEquivalent(entries.Where(e=>SkillSignatures.Resolve(e.Skill)==SkillSignature.None).Select(e=>e.Skill.Id),ArteProfiles.All.Select(p=>p.Id));
            foreach(var entry in entries)
            {
                var profile=ArteProfiles.Resolve(entry.Skill);string id=entry.Skill.Id;
                if(SkillSignatures.Resolve(entry.Skill)!=SkillSignature.None){Assert.That(profile.Enabled,Is.False,id+" retains its separately authored trio signature");continue;}
                Assert.That(profile.Enabled,Is.True,id);Assert.That(profile.Id,Is.EqualTo(id));
                Assert.That(profile.Count,Is.GreaterThan(0),id);Assert.That(profile.Variant,Is.GreaterThanOrEqualTo(0),id);
                Assert.That(profile.Windup,Is.GreaterThanOrEqualTo(0),id);Assert.That(profile.Travel,Is.GreaterThanOrEqualTo(0),id);Assert.That(profile.Recovery,Is.GreaterThan(0),id);
                foreach(float value in new[]{profile.Windup,profile.Travel,profile.Recovery,profile.Spread,profile.Angle,profile.Tint.r,profile.Tint.g,profile.Tint.b,profile.Tint.a})Assert.That(Finite(value),Is.True,id+" finite profile");
            }
            Assert.That(ArteProfiles.Resolve(null).Enabled,Is.False);
            var copy=Object.Instantiate(allies.Single(e=>e.Skill.Id=="cless.1").Skill);
            try
            {
                foreach(string id in new[]{"cless.1.variant","CLess.1","unknown.skill",""})
                {copy.Id=id;Assert.That(ArteProfiles.Resolve(copy).Enabled,Is.False,"Unknown IDs must use the existing fallback, even with the same pattern: "+id);}
            }
            finally{Object.Destroy(copy);}
            Assert.That(entries.Select(e=>JsonUtility.ToJson(e.Skill)),Is.EqualTo(source),"Profile selection must not rewrite authored combat data");
        }

        // Separate character cases keep a failed gate or effect identifiable and retryable.
        [UnityTest] public IEnumerator SkillRosterClessActualActionsMatchIndependentResolver()=>AlliedRosterActions("cless");
        [UnityTest] public IEnumerator SkillRosterMintActualActionsMatchIndependentResolver()=>AlliedRosterActions("mint");
        [UnityTest] public IEnumerator SkillRosterVelvetActualActionsMatchIndependentResolver()=>AlliedRosterActions("velvet");
        [UnityTest] public IEnumerator SkillRosterFarahActualActionsMatchIndependentResolver()=>AlliedRosterActions("farah");
        [UnityTest] public IEnumerator SkillRosterTearActualActionsMatchIndependentResolver()=>AlliedRosterActions("tear");
        [UnityTest] public IEnumerator SkillRosterJadeActualActionsMatchIndependentResolver()=>AlliedRosterActions("jade");
        [UnityTest] public IEnumerator SkillRosterNataliaActualActionsMatchIndependentResolver()=>AlliedRosterActions("natalia");
        [UnityTest] public IEnumerator SkillRosterAlphenActualActionsMatchIndependentResolver()=>AlliedRosterActions("alphen");
        [UnityTest] public IEnumerator SkillRosterShionneActualActionsMatchIndependentResolver()=>AlliedRosterActions("shionne");
        [UnityTest] public IEnumerator SkillRosterKisaraActualActionsMatchIndependentResolver()=>AlliedRosterActions("kisara");

        IEnumerator AlliedRosterActions(string owner)
        {
            int captureRate=Time.captureFramerate;Time.captureFramerate=30;
            var saveBefore=RosterFiles(testStorage);var source=SkillRosterVfxFixture.Entries(director.Catalog,true);
            var assets=source.Select(e=>JsonUtility.ToJson(e.Skill)).ToArray();
            try
            {
                SkillRosterVfxFixture.BeginActor(director,owner);yield return null;
                var selected=source.Where(e=>e.Owner.Id==owner).ToArray();Assert.That(selected.Length,Is.EqualTo(owner=="velvet"?6:owner=="jade"?10:9));
                foreach(var entry in selected)
                {
                    string normalResult=null;int normalDraws=-1;
                    foreach(bool reduced in new[]{false,true})
                    using(var scenario=new SkillRosterVfxFixture.Scenario(director,entry.Skill,reduced))
                    {
                        yield return null;string label=entry.Skill.Id+(reduced?" reduced":" normal");
                        Assert.That(director.Hud.TitleOpen,Is.False,label);
                        int hp=0,mp=0,previousHP=scenario.Actor.CurrentHP,previousMP=scenario.Actor.CurrentMP;
                        scenario.Start();int frames=0;
                        do
                        {
                            yield return null;scenario.Observe();frames++;
                            if(previousHP!=scenario.Actor.CurrentHP){hp++;previousHP=scenario.Actor.CurrentHP;}
                            if(previousMP!=scenario.Actor.CurrentMP){mp++;previousMP=scenario.Actor.CurrentMP;}
                        }while(!scenario.Finished&&frames<600);
                        Assert.That(scenario.Finished,Is.True,label+" did not complete within 20 seconds of simulated animation time");
                        Assert.That(scenario.Problems(),Is.Empty,label);
                        Assert.That(mp,Is.EqualTo(scenario.ExpectedMPCost>0?1:0),label+" MP must be charged once at contact");
                        // HP-cost skills may drain life in the same resolution; observing one combined
                        // state transition and matching the oracle prevents visual pulses charging again.
                        Assert.That(hp,Is.LessThanOrEqualTo(1),label+" repeated self HP changes from visual accents");
                        string actual=SkillRosterVfxFixture.State(director.Session.Units);
                        if(!reduced){normalResult=actual;normalDraws=director.Session.Resolver.RandomDraws;}
                        else{Assert.That(actual,Is.EqualTo(normalResult),label+" accessibility mode changed simulation");Assert.That(director.Session.Resolver.RandomDraws,Is.EqualTo(normalDraws),label);}
                        director.Hud.ClearCombatFeedback();
                    }
                }
                Assert.That(source.Select(e=>JsonUtility.ToJson(e.Skill)),Is.EqualTo(assets),"Actual actions must not mutate content assets");
                CollectionAssert.AreEquivalent(saveBefore,RosterFiles(testStorage),"Training presentation may not write a campaign or settings file");
                var particles=director.Board.GetComponentsInChildren<ImpactParticles>(true);
                var meshes=particles.SelectMany(p=>p.GetComponentsInChildren<MeshFilter>(true)).Select(f=>f.sharedMesh).ToArray();
                var materials=particles.SelectMany(p=>p.GetComponentsInChildren<Renderer>(true)).Select(r=>r.sharedMaterial).Distinct().ToArray();
                director.Restart();yield return null;yield return null;
                Assert.That(meshes.All(m=>m==null),Is.True,owner+" restart leaked owned meshes");
                Assert.That(materials.All(m=>m==null),Is.True,owner+" restart leaked owned materials");
            }
            finally{Time.captureFramerate=captureRate;}
        }

        [UnityTest] public IEnumerator SkillRosterAll182PresentationsRenderFiniteGeometryReusePoolAndReleaseResources()
        {
            SkillRosterVfxFixture.BeginActor(director,"cless");yield return null;
            var entries=SkillRosterVfxFixture.Entries(director.Catalog);Assert.That(entries.Length,Is.EqualTo(182));
            string units=SkillRosterVfxFixture.State(director.Session.Units),random=JsonUtility.ToJson(UnityEngine.Random.state),campaign=JsonUtility.ToJson(director.Campaign);
            int draws=director.Session.Resolver.RandomDraws;float timeScale=Time.timeScale;int rate=Time.captureFramerate;
            var holder=new GameObject("All catalog VFX render regression");var pool=holder.AddComponent<ImpactPool>();
            ImpactParticles[] firstBatch=null;Mesh[] ownedMeshes=null;Material[] ownedMaterials=null;
            try
            {
                Time.captureFramerate=30;
                foreach(bool reduced in new[]{false,true})
                for(int offset=0;offset<entries.Length;offset+=ImpactPool.Capacity)
                {
                    var batch=entries.Skip(offset).Take(ImpactPool.Capacity).ToArray();
                    foreach(var entry in batch)
                    {
                        var skill=entry.Skill;var feedback=RosterFeedback(skill);
                        pool.Play(director.Board.BattleCamera,Vector3.zero,Vector3.right,skill.Element,CombatEffect.Resolve(entry.Owner,skill),feedback,skill.Presentation?.Size??1,skill.IsUltimate,reduced,
                            skill.Presentation?.Pattern??SkillVisualPattern.Automatic,false,SkillSignatures.Resolve(skill),ArteProfiles.Resolve(skill));
                    }
                    Assert.That(pool.ActiveCount,Is.EqualTo(batch.Length));
                    var active=holder.GetComponentsInChildren<ImpactParticles>();
                    if(firstBatch==null)firstBatch=active;
                    else Assert.That(active.All(p=>firstBatch.Contains(p)),Is.True,"Expired entries must be reused across characters and reduced mode");
                    for(int i=0;i<active.Length;i++)
                    {
                        var skill=batch[i].Skill;
                        Assert.That(active[i].Arte.Id,Is.EqualTo(ArteProfiles.Resolve(skill).Id),skill.Id);
                        Assert.That(active[i].Signature,Is.EqualTo(SkillSignatures.Resolve(skill)),skill.Id);
                        RosterGeometry(active[i],skill.Id+" at contact");
                    }
                    yield return new WaitForSeconds(.2f);
                    foreach(var particle in active)RosterGeometry(particle,(particle.Arte.Id??particle.Signature.ToString())+" after contact");
                    yield return new WaitForSeconds(ImpactParticles.Duration+.1f);
                    Assert.That(pool.ActiveCount,Is.Zero,"Every bounded batch must finish naturally");
                    Assert.That(holder.GetComponentsInChildren<ImpactParticles>(true).Length,Is.LessThanOrEqualTo(ImpactPool.Capacity));
                }
                Assert.That(SkillRosterVfxFixture.State(director.Session.Units),Is.EqualTo(units));
                Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(random));Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(campaign));
                Assert.That(director.Session.Resolver.RandomDraws,Is.EqualTo(draws));Assert.That(Time.timeScale,Is.EqualTo(timeScale));
                ownedMeshes=holder.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();
                ownedMaterials=holder.GetComponentsInChildren<Renderer>(true).Select(r=>r.sharedMaterial).Distinct().ToArray();
            }
            finally{Time.captureFramerate=rate;Object.Destroy(holder);}
            yield return null;yield return null;
            Assert.That(ownedMeshes.All(m=>m==null),Is.True);Assert.That(ownedMaterials.All(m=>m==null),Is.True);
        }

        [UnityTest] public IEnumerator SkillRosterTwelveAudioFamiliesHaveFiniteReleaseContactClipsAndDestroyCleanly()
        {
            var families=SkillRosterVfxFixture.Entries(director.Catalog).Select(e=>SkillAudioPalette.Family(e.Skill,CombatEffect.Resolve(e.Owner,e.Skill))).Where(s=>s!=null).Distinct().ToArray();
            var expected=new[]{"arte-arrow","arte-rifle","arte-shield","arte-earth","arte-water","arte-wind","arte-holy","arte-song","arte-gravity","arte-claw","arte-hammer","arte-martial"};
            foreach(string family in expected)Assert.That(families,Does.Contain(family));
            string random=JsonUtility.ToJson(UnityEngine.Random.state);var clips=new List<AudioClip>();
            try
            {
                foreach(string family in expected)foreach(bool contact in new[]{false,true})
                {
                    var clip=SkillAudioPalette.Create(family,contact);clips.Add(clip);
                    Assert.That(clip,Is.Not.Null,family);Assert.That(clip.length,Is.InRange(.05f,3f),family);Assert.That(clip.channels,Is.EqualTo(1));
                    var samples=new float[clip.samples*clip.channels];Assert.That(clip.GetData(samples,0),Is.True,family);
                    Assert.That(samples.All(Finite),Is.True,family);Assert.That(samples.Max(Mathf.Abs),Is.InRange(.001f,1f),family);
                }
                Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(random),"Procedural audio may not consume gameplay/global RNG");
            }
            finally{foreach(var clip in clips)if(clip!=null)Object.Destroy(clip);}
            yield return null;Assert.That(clips.All(c=>c==null),Is.True);
        }

        [UnityTest] public IEnumerator SkillRosterWoundedVelvetDrainsOnceWithHealingSilhouetteAndKeepsTargetClaw()
        {
            SkillRosterVfxFixture.BeginActor(director,"velvet");yield return null;
            var skill=director.Catalog.Characters.Single(c=>c.Id=="velvet").Skills.Single(s=>s.Id=="velvet.1");
            using(var scenario=new SkillRosterVfxFixture.Scenario(director,skill,false,woundedActor:true))
            {
                int hp=scenario.Actor.CurrentHP;bool sawRecovery=false;scenario.Start();int frames=0;
                do
                {
                    yield return null;scenario.Observe();frames++;
                    if(!sawRecovery&&scenario.Resolutions>0)
                    {
                        Assert.That(scenario.Actor.CurrentHP,Is.GreaterThan(hp),"The fixture must exercise real life drain");
                        var effects=director.Board.GetComponentsInChildren<CombatEffect>();
                        Assert.That(effects.Single(e=>e.Recipient==scenario.Actor).Feedback,Is.EqualTo(CombatFeedback.Heal));
                        Assert.That(effects.Single(e=>e.Recipient==scenario.Recipient).Feedback,Is.EqualTo(CombatFeedback.Strike));
                        var particles=director.Board.GetComponentsInChildren<ImpactParticles>().Where(p=>!p.Preparing&&!p.Travelling).ToArray();
                        Assert.That(particles.Count(p=>p.Arte.Id==skill.Id),Is.EqualTo(1),"Only the enemy contact may use the claw attack profile");
                        var heal=particles.Single(p=>!p.Arte.Enabled&&p.Signature==SkillSignature.None);
                        var art=heal.GetComponentsInChildren<MeshFilter>().Single(m=>m.name=="Authored elemental silhouette");
                        Assert.That(art.sharedMesh.vertexCount,Is.GreaterThan(0));
                        Assert.That(art.GetComponent<Renderer>().sharedMaterial.mainTexture,Is.SameAs(Resources.Load<Texture2D>("TalesTactics/CombatElements")),"Drain must use the healing atlas, not the attack atlas");
                        Assert.That(art.sharedMesh.uv.All(uv=>uv.y>0&&uv.y<.25f),Is.True,"Drain must sample the authored healing row");
                        sawRecovery=true;
                    }
                }while(!scenario.Finished&&frames<600);
                Assert.That(sawRecovery,Is.True);Assert.That(scenario.Problems(),Is.Empty);Assert.That(scenario.Resolutions,Is.EqualTo(1));
            }
        }

        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        static CombatFeedback RosterFeedback(SkillData skill)=>skill.Effects.Any(e=>e.Kind==EffectKind.Damage&&!e.AffectCaster)?CombatFeedback.Strike:
            skill.Effects.Any(e=>e.Kind==EffectKind.Revive)?CombatFeedback.Revive:skill.Effects.Any(e=>e.Kind==EffectKind.Heal)?CombatFeedback.Heal:CombatFeedback.Support;
        static string[] RosterFiles(string path)=>!Directory.Exists(path)?new string[0]:Directory.GetFiles(path,"*",SearchOption.AllDirectories)
            .Select(file=>file+":"+Convert.ToBase64String(File.ReadAllBytes(file))).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
        static void RosterGeometry(ImpactParticles particle,string message)
        {
            var filters=particle.GetComponentsInChildren<MeshFilter>();Assert.That(filters.Length,Is.EqualTo(4),message+" must retain bounded four-mesh particles");
            int visible=0;
            foreach(var filter in filters)
            {
                var mesh=filter.sharedMesh;var renderer=filter.GetComponent<Renderer>();Assert.That(mesh,Is.Not.Null,message);Assert.That(renderer.sharedMaterial.shader.isSupported,Is.True,message);
                foreach(var vertex in mesh.vertices)Assert.That(Finite(vertex.x)&&Finite(vertex.y)&&Finite(vertex.z),Is.True,message);
                foreach(var uv in mesh.uv)Assert.That(Finite(uv.x)&&Finite(uv.y),Is.True,message);
                foreach(var color in mesh.colors)Assert.That(Finite(color.r)&&Finite(color.g)&&Finite(color.b)&&Finite(color.a),Is.True,message);
                if(renderer.enabled&&mesh.colors.Any(c=>c.a>.001f))visible+=mesh.vertexCount;
            }
            Assert.That(visible,Is.GreaterThan(0),message+" must have visible geometry");
        }
    }
}
#endif
