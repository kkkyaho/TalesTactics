using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TalesTactics.PlayModeTests
{
    public sealed class VfxReadabilitySceneTests
    {
        GameObject fixture;
        Camera camera;
        ImpactPool pool;
        static readonly FieldInfo AgeField=typeof(ImpactParticles).GetField("age",BindingFlags.Instance|BindingFlags.NonPublic);
        static readonly MethodInfo DrawMethod=typeof(ImpactParticles).GetMethod("Draw",BindingFlags.Instance|BindingFlags.NonPublic);

        [SetUp] public void CreateFixture()
        {
            fixture=new GameObject("VFX readability regression");
            var view=new GameObject("Readability camera");view.transform.SetParent(fixture.transform);
            camera=view.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;
            pool=fixture.AddComponent<ImpactPool>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Object.Destroy(fixture);yield return null;yield return null;
            LogAssert.NoUnexpectedReceived();
        }
        static void AtTime(ImpactParticles effect,float seconds)
        {
            AgeField.SetValue(effect,seconds);DrawMethod.Invoke(effect,null);
        }
        ImpactParticles Active()=>fixture.GetComponentsInChildren<ImpactParticles>().Single();

        [Test] public void DemonFangKeepsGroundLeadingEdgeAndTrailingWakeInEveryDirection()
        {
            string random=JsonUtility.ToJson(UnityEngine.Random.state);float timeScale=Time.timeScale;
            foreach(bool reduced in new[]{false,true})
            foreach(float yaw in new[]{45f,135f,225f,315f})
            foreach(var direction in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
            {
                camera.transform.rotation=Quaternion.Euler(42,yaw,0);
                var origin=-direction*2;
                pool.Travel(camera,origin,Vector3.zero,Element.None,CombatVisualStyle.Sword,1,false,reduced,.3f,SkillVisualPattern.Wave,SkillSignature.DemonFang);
                var effect=Active();AtTime(effect,.15f);
                var meshes=effect.GetComponentsInChildren<MeshFilter>();Assert.That(meshes.Length,Is.EqualTo(4));
                var front=meshes.Single(m=>m.name=="Impact volume").sharedMesh;
                var head=Vector3.Lerp(origin,Vector3.zero,SkillVisualLanguage.Snap(.5f));
                Assert.That(front.vertexCount,Is.GreaterThan(100));
                foreach(var v in front.vertices)
                {
                    Assert.That(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z),Is.False);
                    Assert.That(v.y,Is.InRange(.05f,.13f),"Front and wake must stay on the ground instead of tilting with camera yaw");
                }
                Assert.That(front.vertices.Max(v=>Vector3.Dot(v-head,direction)),Is.GreaterThan(.45f),"Pale leading edge must be in front of the moving head");
                Assert.That(front.vertices.Min(v=>Vector3.Dot(v-head,direction)),Is.LessThan(-.8f),"Tapered wake must extend backward along the true travel direction");
                var illustration=meshes.Single(m=>m.name=="Authored elemental silhouette").sharedMesh.vertices;
                Assert.That(illustration.Length,Is.EqualTo(4));
                Assert.That(Vector3.Dot(illustration[3]-illustration[0],direction),Is.GreaterThan(.1f),"The illustrated front must not flip against travel");
                effect.gameObject.SetActive(false);
            }
            Assert.That(fixture.GetComponentsInChildren<ImpactParticles>(true).Length,Is.EqualTo(1));
            Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(random));
            Assert.That(Time.timeScale,Is.EqualTo(timeScale));
        }

        [Test] public void FirstAidKeepsFaceSpaceClearAndItsSealLowWithReducedEffects()
        {
            var profile=ArteProfiles.All.Single(p=>p.Id=="mint.0");
            string random=JsonUtility.ToJson(UnityEngine.Random.state);float timeScale=Time.timeScale;
            int fullCount=0,reducedCount=0;
            foreach(bool reduced in new[]{false,true})
            foreach(float yaw in new[]{45f,135f,225f,315f})
            {
                camera.transform.rotation=Quaternion.Euler(42,yaw,0);
                pool.Play(camera,Vector3.left*2,Vector3.zero,Element.Light,CombatVisualStyle.Sacred,CombatFeedback.Heal,1,false,reduced,SkillVisualPattern.Heal,false,SkillSignature.None,profile);
                var effect=Active();
                foreach(float time in new[]{.06f,.30f,.60f,.85f})
                {
                    AtTime(effect,time);
                    var meshes=effect.GetComponentsInChildren<MeshFilter>();Assert.That(meshes.Length,Is.EqualTo(4));
                    int count=meshes.Sum(m=>m.sharedMesh.vertexCount);
                    Assert.That(count,Is.GreaterThan(100));
                    if(reduced)reducedCount=Math.Max(reducedCount,count);else fullCount=Math.Max(fullCount,count);
                    foreach(var filter in meshes)AssertFaceSpace(filter.sharedMesh,camera);
                    var seal=meshes.Single(m=>m.name=="Depth flame and mist").sharedMesh;
                    Assert.That(seal.vertexCount,Is.GreaterThan(0),"The existing authored seal remains visible");
                    Assert.That(seal.vertices.All(v=>v.y>=0&&v.y<.10f),Is.True,"The seal must lie flat below the recipient");
                    Assert.That(meshes.Single(m=>m.name=="Depth flame and mist").GetComponent<Renderer>().sortingOrder,Is.LessThan(0),"Painted spell volume belongs behind the recipient");
                }
                effect.gameObject.SetActive(false);
            }
            Assert.That(reducedCount,Is.LessThan(fullCount),"Reduced effects retain the composition with fewer petals and segments");
            Assert.That(fixture.GetComponentsInChildren<ImpactParticles>(true).Length,Is.EqualTo(1));
            Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(random));
            Assert.That(Time.timeScale,Is.EqualTo(timeScale));
        }
        static void AssertFaceSpace(Mesh mesh,Camera view)
        {
            var points=mesh.vertices.Select(v=>new Vector2(Vector3.Dot(v,view.transform.right),Vector3.Dot(v,view.transform.up))).ToArray();
            var colors=mesh.colors;var triangles=mesh.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];
                if(Mathf.Max(colors[a].a,Mathf.Max(colors[b].a,colors[c].a))<.015f)continue;
                float minX=Mathf.Min(points[a].x,Mathf.Min(points[b].x,points[c].x));
                float maxX=Mathf.Max(points[a].x,Mathf.Max(points[b].x,points[c].x));
                float minY=Mathf.Min(points[a].y,Mathf.Min(points[b].y,points[c].y));
                float maxY=Mathf.Max(points[a].y,Mathf.Max(points[b].y,points[c].y));
                Assert.That(maxX<-.24f||minX>.24f||maxY<.62f||minY>1.30f,Is.True,
                    "Visible triangle bounds enter the reserved central face area: "+mesh.name+" triangle "+i/3);
            }
        }
    }
}
