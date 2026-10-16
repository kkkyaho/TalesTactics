using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator RepresentativeVfxPoolReuseReducedEffectsAndOwnedResourcesStayIsolated()
        {
            director.BeginBattle();yield return null;
            var owner=new GameObject("VFX upgrade regression");var pool=owner.AddComponent<ImpactPool>();
            var random=JsonUtility.ToJson(Random.state);var campaign=JsonUtility.ToJson(director.Campaign);
            var health=director.Session.Units.Select(u=>u.CurrentHP).ToArray();var positions=director.Session.Units.Select(u=>u.Position).ToArray();
            float timeScale=Time.timeScale;ImpactParticles firstInstance=null;
            Mesh[] ownedMeshes=null;Material[] ownedMaterials=null;
            try
            {
                foreach(bool reduced in new[]{false,true})
                foreach(string id in new[]{"cless.0","jade.6","jade.5","jade.3","mint.0","cless.ultimate"})
                {
                    var actor=director.Catalog.Characters.Single(c=>c.Id==id.Split('.')[0]);
                    var skill=actor.Skills.Concat(new[]{actor.UltimateSkill}).Single(s=>s!=null&&s.Id==id);
                    var feedback=skill.Target==TargetType.Ally?CombatFeedback.Heal:CombatFeedback.Strike;
                    pool.Play(director.Board.BattleCamera,Vector3.zero,Vector3.right,skill.Element,CombatEffect.Resolve(actor,skill),feedback,skill.Presentation.Size,skill.IsUltimate,reduced,skill.Presentation.Pattern);
                    yield return null;
                    var instance=owner.GetComponentsInChildren<ImpactParticles>(true).Single();
                    if(firstInstance==null)firstInstance=instance;
                    Assert.That(instance,Is.SameAs(firstInstance),"Sequential effects must reuse an expired pool slot across elements and reduced mode");
                    Assert.That(pool.ActiveCount,Is.EqualTo(1));
                    var meshes=owner.GetComponentsInChildren<MeshFilter>().Select(m=>m.sharedMesh).ToArray();
                    Assert.That(meshes.Sum(m=>m.vertexCount),Is.GreaterThan(0),id);
                    foreach(var mesh in meshes)foreach(var vertex in mesh.vertices)
                        Assert.That(float.IsNaN(vertex.x)||float.IsNaN(vertex.y)||float.IsNaN(vertex.z)||float.IsInfinity(vertex.x)||float.IsInfinity(vertex.y)||float.IsInfinity(vertex.z),Is.False,id);
                    yield return new WaitForSeconds(ImpactParticles.Duration+.12f);
                    Assert.That(pool.ActiveCount,Is.Zero,id+" must finish before the next effect");
                }
                Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random),"Presentation must not consume gameplay RNG");
                Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(campaign));
                Assert.That(director.Session.Units.Select(u=>u.CurrentHP),Is.EqualTo(health));
                Assert.That(director.Session.Units.Select(u=>u.Position),Is.EqualTo(positions));
                Assert.That(Time.timeScale,Is.EqualTo(timeScale));
                ownedMeshes=owner.GetComponentsInChildren<MeshFilter>(true).Select(m=>m.sharedMesh).ToArray();
                ownedMaterials=owner.GetComponentsInChildren<Renderer>(true).Select(r=>r.sharedMaterial).Distinct().ToArray();
            }
            finally{Object.Destroy(owner);}
            yield return null;yield return null;
            Assert.That(ownedMeshes.All(m=>m==null),Is.True,"Pool meshes must be destroyed with their owner");
            Assert.That(ownedMaterials.All(m=>m==null),Is.True,"Pool materials must be destroyed with their owner");
        }
    }
}
