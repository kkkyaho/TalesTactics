using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public sealed class VfxArtSceneTests
    {
        GameObject fixture, effects;
        Camera camera;
        ImpactPool pool;

        [SetUp] public void CreateFixture()
        {
            fixture=new GameObject("VFX art regression");
            var cameraObject=new GameObject("VFX regression camera");cameraObject.transform.SetParent(fixture.transform);
            camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;
            camera.transform.position=new Vector3(0,3,-6);camera.transform.LookAt(Vector3.zero);
            effects=new GameObject("VFX regression pool");effects.transform.SetParent(fixture.transform);
            pool=effects.AddComponent<ImpactPool>();
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(fixture!=null)Object.Destroy(fixture);
            yield return null;yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [Test] public void AuthoredAtlasesLoadWithRealTransparentAndVisiblePixels()
        {
            var shader=Resources.Load<Shader>("TalesTactics/Impact");
            Assert.That(shader,Is.Not.Null);Assert.That(shader.isSupported,Is.True);
            foreach(string name in new[]{"CombatFire","CombatElements"})
            {
                var texture=Resources.Load<Texture2D>("TalesTactics/"+name);
                Assert.That(texture,Is.Not.Null,name);Assert.That(texture.width,Is.GreaterThan(256),name);
                Assert.That(texture.height,Is.GreaterThan(256),name);
                // Imported atlases need not be CPU-readable. Sample their actual GPU alpha instead.
                var target=RenderTexture.GetTemporary(128,128,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
                var previous=RenderTexture.active;Texture2D readback=null;
                try
                {
                    Graphics.Blit(texture,target);RenderTexture.active=target;
                    readback=new Texture2D(128,128,TextureFormat.RGBA32,false,true);
                    readback.ReadPixels(new Rect(0,0,128,128),0,0);readback.Apply();
                    var pixels=readback.GetPixels32();
                    Assert.That(pixels.Count(p=>p.a<16),Is.GreaterThan(32),name+" needs transparent space");
                    Assert.That(pixels.Count(p=>p.a>128),Is.GreaterThan(32),name+" needs visible artwork");
                }
                finally
                {
                    RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
                    if(readback!=null)Object.Destroy(readback);
                }
            }
        }

        [Test] public void CelShaderAddsInkWithoutFillingTransparentArtworkSpace()
        {
            var shader=Resources.Load<Shader>("TalesTactics/Impact");Assert.That(shader,Is.Not.Null);Assert.That(shader.isSupported,Is.True);
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false,true){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var material=new Material(shader);var mesh=new Mesh();
            try
            {
                var pixels=new Color[64*64];
                for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                {
                    float distance=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f));
                    float alpha=Mathf.Clamp01((25-distance)/4f)*Mathf.Clamp01((distance-8)/3f),value=.4f+x/63f*.6f;
                    pixels[y*64+x]=new Color(value*.3f,value*.65f,value,alpha);
                }
                texture.SetPixels(pixels);texture.Apply();material.mainTexture=texture;
                material.SetFloat("_UseTexture",1);material.SetFloat("_AtlasGrid",1);
                mesh.vertices=new[]{new Vector3(0,0,0),new Vector3(0,1,0),new Vector3(1,1,0),new Vector3(1,0,0)};
                mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};
                mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};mesh.triangles=new[]{0,1,2,0,2,3};
                material.SetFloat("_CelStyle",0);var soft=RenderArtwork(mesh,material);
                material.SetFloat("_CelStyle",1);var cel=RenderArtwork(mesh,material);
                int softInk=soft.Count(c=>c.a>220&&c.r<60&&c.g<60&&c.b<80);
                int celInk=cel.Count(c=>c.a>220&&c.r<60&&c.g<60&&c.b<80);
                Assert.That(celInk,Is.GreaterThan(softInk+20),"Opaque dark contours must be rendered around the painted silhouette");
                Assert.That(cel.Count(c=>c.a>220&&c.b>120),Is.GreaterThan(300),"The colored body must survive the cel conversion");
                Assert.That(cel[64*128+64].a,Is.LessThan(4),"A transparent hole must not turn into a solid billboard");
                Assert.That(cel[0].a,Is.LessThan(4),"The atlas background must remain transparent");
            }
            finally{Object.DestroyImmediate(mesh);Object.DestroyImmediate(material);Object.DestroyImmediate(texture);}
        }

        static Color32[] RenderArtwork(Mesh mesh,Material material)
        {
            var target=RenderTexture.GetTemporary(128,128,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var previous=RenderTexture.active;Texture2D readback=null;
            var commands=new UnityEngine.Rendering.CommandBuffer();
            try
            {
                commands.SetRenderTarget(target);commands.ClearRenderTarget(false,true,Color.clear);
                commands.SetViewProjectionMatrices(Matrix4x4.identity,GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(0,1,0,1,-1,1),true));
                commands.DrawMesh(mesh,Matrix4x4.identity,material);Graphics.ExecuteCommandBuffer(commands);
                RenderTexture.active=target;readback=new Texture2D(128,128,TextureFormat.RGBA32,false,true);
                readback.ReadPixels(new Rect(0,0,128,128),0,0);readback.Apply();return readback.GetPixels32();
            }
            finally
            {
                commands.Release();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
                if(readback!=null)Object.DestroyImmediate(readback);
            }
        }

        [UnityTest] public IEnumerator FullAndReducedImpactMeshesHaveFiniteCompleteUvStreams()
        {
            var elements=new[]{Element.Fire,Element.Ice,Element.Lightning,Element.None,Element.Light};
            var patterns=new[]{SkillVisualPattern.Meteor,SkillVisualPattern.Ice,SkillVisualPattern.Lightning,SkillVisualPattern.Wave,SkillVisualPattern.Heal};
            foreach(bool reduced in new[]{false,true})
            for(int i=0;i<elements.Length;i++)
            {
                var feedback=i==4?CombatFeedback.Heal:CombatFeedback.Strike;
                Assert.That(ImpactParticles.HasAuthoredArt(patterns[i],elements[i],feedback),Is.True);
                pool.Play(camera,Vector3.left*2,Vector3.zero,elements[i],CombatVisualStyle.Sacred,feedback,1,true,reduced,patterns[i]);
                var effect=ActiveEffect();AssertGeometry(effect);
                yield return null;
                AssertGeometry(effect);effect.gameObject.SetActive(false);
            }
            // Travel starts between authored frames; this verifies that UV1 carries a real blend,
            // rather than merely duplicating the current UV with a constant zero weight.
            pool.Travel(camera,Vector3.left*2,Vector3.zero,Element.None,CombatVisualStyle.Sword,1,false,false,.5f,SkillVisualPattern.Wave);
            var travel=ActiveEffect();AssertGeometry(travel);
            var blended=new List<Vector3>();
            foreach(var filter in travel.GetComponentsInChildren<MeshFilter>())
            {
                var values=new List<Vector3>();filter.sharedMesh.GetUVs(1,values);blended.AddRange(values);
            }
            Assert.That(blended.Any(uv=>uv.z>0&&uv.z<1),Is.True,"Travel must interpolate adjacent atlas frames");
        }

        [Test] public void ReducedEffectsKeepAuthoredArtWhileReducingGeometry()
        {
            foreach(var element in new[]{Element.Fire,Element.Ice,Element.Light})
            {
                var feedback=element==Element.Light?CombatFeedback.Heal:CombatFeedback.Strike;
                var pattern=element==Element.Light?SkillVisualPattern.Heal:SkillVisualPattern.Automatic;
                pool.Play(camera,Vector3.left*2,Vector3.zero,element,CombatVisualStyle.Sacred,feedback,1,true,false,pattern);
                var effect=ActiveEffect();int full=VertexCount(effect);effect.gameObject.SetActive(false);
                pool.Play(camera,Vector3.left*2,Vector3.zero,element,CombatVisualStyle.Sacred,feedback,1,true,true,pattern);
                Assert.That(ActiveEffect(),Is.SameAs(effect));AssertGeometry(effect);
                Assert.That(VertexCount(effect),Is.LessThan(full),element+" reduced geometry");
                Assert.That(TexturedVertexCount(effect),Is.GreaterThan(0),element+" retains authored feedback");
                effect.gameObject.SetActive(false);
            }
        }

        [Test] public void ReusedEffectsClearTravelPreparationCompactAndPreviousAtlasGeometry()
        {
            pool.Travel(camera,Vector3.left*2,Vector3.zero,Element.Fire,CombatVisualStyle.Sacred,1,true,false,.6f,SkillVisualPattern.Meteor);
            var effect=ActiveEffect();Assert.That(effect.Travelling,Is.True);Assert.That(effect.Preparing,Is.False);
            effect.gameObject.SetActive(false);
            pool.Prepare(camera,fixture.transform,Vector3.right,Element.Fire,CombatVisualStyle.Sacred,1,false,false,.5f,SkillVisualPattern.Meteor);
            Assert.That(ActiveEffect(),Is.SameAs(effect));Assert.That(effect.Preparing,Is.True);
            Assert.That(effect.Travelling,Is.False);effect.gameObject.SetActive(false);
            pool.Play(camera,Vector3.left*2,Vector3.zero,Element.Fire,CombatVisualStyle.Sacred,CombatFeedback.Strike,1,true,false,SkillVisualPattern.Meteor,true);
            Assert.That(ActiveEffect(),Is.SameAs(effect));Assert.That(effect.Compact,Is.True);
            Assert.That(effect.Preparing,Is.False);Assert.That(effect.Travelling,Is.False);AssertGeometry(effect);
            Assert.That(TexturedVertexCount(effect),Is.Zero,"Compact replay must clear every old atlas mesh");
            effect.gameObject.SetActive(false);
            pool.Play(camera,Vector3.left*2,Vector3.zero,Element.Ice,CombatVisualStyle.Sacred,CombatFeedback.Strike,1,false,false,SkillVisualPattern.Ice);
            Assert.That(ActiveEffect(),Is.SameAs(effect));Assert.That(effect.Compact,Is.False);
            Assert.That(effect.Preparing,Is.False);Assert.That(effect.Travelling,Is.False);
            Assert.That(effect.Pattern,Is.EqualTo(SkillVisualPattern.Ice));AssertGeometry(effect);
            var fire=Resources.Load<Texture2D>("TalesTactics/CombatFire");
            var art=Resources.Load<Texture2D>("TalesTactics/CombatElements");
            Assert.That(VertexCountForTexture(effect,fire),Is.Zero,"Ice cannot retain old flame or back-atlas geometry");
            Assert.That(VertexCountForTexture(effect,art),Is.GreaterThan(0));
            Assert.That(pool.GetComponentsInChildren<ImpactParticles>(true).Length,Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator PoolLimitAndDestructionReleaseEveryOwnedMeshAndMaterial()
        {
            var fire=Resources.Load<Texture2D>("TalesTactics/CombatFire");
            var art=Resources.Load<Texture2D>("TalesTactics/CombatElements");
            for(int i=0;i<ImpactPool.Capacity+5;i++)
                pool.Play(camera,Vector3.left,Vector3.zero,i%2==0?Element.Fire:Element.Ice,CombatVisualStyle.Sacred,CombatFeedback.Strike,1,false,false);
            var instances=pool.GetComponentsInChildren<ImpactParticles>(true);
            Assert.That(instances.Length,Is.EqualTo(ImpactPool.Capacity));Assert.That(pool.ActiveCount,Is.EqualTo(ImpactPool.Capacity));
            var meshes=pool.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).Distinct().ToArray();
            var materials=pool.GetComponentsInChildren<MeshRenderer>(true).Select(r=>r.sharedMaterial).Distinct().ToArray();
            Assert.That(meshes.Length,Is.EqualTo(ImpactPool.Capacity*4));Assert.That(materials.Length,Is.EqualTo(3));
            Assert.That(materials.All(m=>m!=null&&m.shader.isSupported),Is.True);
            Object.Destroy(effects);yield return null;yield return null;
            Assert.That(instances.All(p=>p==null),Is.True);
            Assert.That(meshes.All(m=>m==null),Is.True,"Pool teardown must destroy every owned dynamic mesh");
            Assert.That(materials.All(m=>m==null),Is.True,"Pool teardown must destroy all shared runtime materials");
            Assert.That(fire!=null&&art!=null,Is.True,"Resource atlases are shared assets, not owned runtime textures");
        }

        ImpactParticles ActiveEffect()=>pool.GetComponentsInChildren<ImpactParticles>().Single();
        static int VertexCount(ImpactParticles effect)=>effect.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.vertexCount);
        static int TexturedVertexCount(ImpactParticles effect)=>effect.GetComponentsInChildren<MeshFilter>()
            .Where(f=>f.GetComponent<MeshRenderer>().sharedMaterial.GetFloat("_UseTexture")>0)
            .Sum(f=>f.sharedMesh.vertexCount);
        static int VertexCountForTexture(ImpactParticles effect,Texture texture)=>effect.GetComponentsInChildren<MeshFilter>()
            .Where(f=>f.GetComponent<MeshRenderer>().sharedMaterial.mainTexture==texture).Sum(f=>f.sharedMesh.vertexCount);
        static void AssertGeometry(ImpactParticles effect)
        {
            var meshes=effect.GetComponentsInChildren<MeshFilter>();Assert.That(meshes.Length,Is.EqualTo(4));
            Assert.That(meshes.Sum(f=>f.sharedMesh.vertexCount),Is.GreaterThan(0));
            foreach(var filter in meshes)
            {
                var mesh=filter.sharedMesh;var next=new List<Vector3>();mesh.GetUVs(1,next);
                Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount),filter.name+" UV0 length");
                Assert.That(next.Count,Is.EqualTo(mesh.vertexCount),filter.name+" UV1 length");
                foreach(var v in mesh.vertices)AssertFinite(v,filter.name);
                foreach(var uv in mesh.uv){Assert.That(uv.x,Is.InRange(0f,1f));Assert.That(uv.y,Is.InRange(0f,1f));}
                foreach(var uv in next)
                {
                    AssertFinite(uv,filter.name+" next UV/blend");
                    Assert.That(uv.x,Is.InRange(0f,1f));Assert.That(uv.y,Is.InRange(0f,1f));Assert.That(uv.z,Is.InRange(0f,1f));
                }
                foreach(int index in mesh.triangles)Assert.That(index,Is.InRange(0,mesh.vertexCount-1));
            }
        }
        static void AssertFinite(Vector3 value,string context)
        {
            Assert.That(float.IsNaN(value.x)||float.IsNaN(value.y)||float.IsNaN(value.z)||
                float.IsInfinity(value.x)||float.IsInfinity(value.y)||float.IsInfinity(value.z),Is.False,context);
        }
    }
}
