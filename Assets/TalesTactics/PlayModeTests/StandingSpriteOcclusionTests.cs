using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace TalesTactics.PlayModeTests
{
    public sealed class StandingSpriteOcclusionTests
    {
        readonly List<Object> owned=new List<Object>();
        MeshRenderer obstacle;
        T Own<T>(T value) where T:Object {owned.Add(value);return value;}
        [UnityTearDown] public IEnumerator Cleanup()
        {for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)Object.Destroy(owned[i]);owned.Clear();yield return null;}

        [UnityTest] public IEnumerator UprightDepthKeepsRearWallsBehindBodyButAllowsFrontWallsToHideIt()
        {
            var camera=Own(new GameObject("Occlusion fixture camera")).AddComponent<Camera>();camera.enabled=false;
            camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.allowHDR=false;camera.allowMSAA=false;
            camera.orthographic=true;camera.orthographicSize=1.8f;camera.aspect=1;camera.nearClipPlane=.1f;camera.farClipPlane=30;
            StandingSpriteDepth.Configure(camera);
            var texture=Own(new Texture2D(24,56,TextureFormat.RGBA32,false,true));
            texture.SetPixels(Enumerable.Repeat(Color.white,24*56).ToArray());texture.Apply();
            var sprite=Own(Sprite.Create(texture,new Rect(0,0,24,56),new Vector2(.5f,0),40,0,SpriteMeshType.FullRect));
            var actor=Own(new GameObject("Upright actor")).AddComponent<SpriteRenderer>();actor.gameObject.layer=31;actor.sprite=sprite;actor.color=Color.red;
            var material=Own(new Material(Resources.Load<Shader>("TalesTactics/UnitTone")));actor.sharedMaterial=material;
            StandingSpriteDepth.Configure(actor);Assert.That(actor.spriteSortPoint,Is.EqualTo(SpriteSortPoint.Pivot));
            var cube=Own(GameObject.CreatePrimitive(PrimitiveType.Cube));cube.layer=31;var wall=cube.GetComponent<MeshRenderer>();obstacle=wall;
            var opaque=Own(new Material(Shader.Find("Unlit/Color")));opaque.color=Color.green;wall.sharedMaterial=opaque;
            Assert.That(material.shader.isSupported&&opaque.shader.isSupported,Is.True);
            yield return null;
            var block=new MaterialPropertyBlock();
            var prop=Own(new GameObject("Transparent pillar fixture")).AddComponent<SpriteRenderer>();prop.gameObject.layer=31;prop.sprite=sprite;prop.color=Color.green;prop.sharedMaterial=material;StandingSpriteDepth.Configure(prop);prop.enabled=false;
            foreach(float yaw in new[]{-39f,51f,141f,231f})
            {
                var rotation=Quaternion.Euler(57,yaw,0);camera.transform.SetPositionAndRotation(new Vector3(0,.7f,0)-rotation*Vector3.forward*10,rotation);
                StandingSpriteDepth.Configure(camera);actor.transform.rotation=rotation;var horizontal=Quaternion.Euler(0,yaw,0);
                cube.transform.rotation=horizontal;cube.transform.localScale=new Vector3(3,1.5f,.15f);
                cube.transform.position=horizontal*new Vector3(0,.75f,.65f);
                var reference=Render(camera,actor,null);int visible=Red(reference);Assert.That(visible,Is.GreaterThan(1500),"Actor reference render is absent. Colors: "+string.Join(";",reference.GroupBy(c=>c).OrderByDescending(g=>g.Count()).Take(5).Select(g=>g.Key+":"+g.Count())));
                actor.GetPropertyBlock(block);block.SetFloat("_StandingDepth",0);actor.SetPropertyBlock(block);
                int broken=Red(Render(camera,actor,wall));
                Assert.That(broken,Is.LessThan(visible*.85f),"Negative control did not reproduce rear-wall torso clipping at yaw "+yaw);
                StandingSpriteDepth.Configure(actor);int corrected=Red(Render(camera,actor,wall));
                Assert.That(corrected,Is.GreaterThan(visible*.97f),"Rear wall clipped an upright body at yaw "+yaw+" reference="+visible+" old="+broken+" new="+corrected);
                cube.transform.localScale=new Vector3(3,4,.15f);cube.transform.position=horizontal*new Vector3(0,2,-.65f);
                int hidden=Red(Render(camera,actor,wall));
                Assert.That(hidden,Is.LessThan(visible*.1f),"A wall physically in front must still hide the body at yaw "+yaw);
                prop.enabled=true;prop.transform.rotation=rotation;prop.transform.localScale=new Vector3(2,2,1);
                foreach(float height in new[]{-.5f,0,.5f})
                {
                    prop.transform.position=horizontal*new Vector3(0,height,.65f);
                    if(height>.4f)
                    {
                        camera.transparencySortMode=TransparencySortMode.Orthographic;
                        Assert.That(Red(Render(camera,actor,null)),Is.LessThan(visible*.85f),"Negative control must reproduce the elevated rear pillar covering the actor.");
                        StandingSpriteDepth.Configure(camera);
                    }
                    Assert.That(Red(Render(camera,actor,null)),Is.GreaterThan(visible*.97f),"Rear pillar must stay behind at yaw "+yaw+" height "+height);
                    prop.transform.position=horizontal*new Vector3(0,height,-.65f);
                    if(height<-.4f)
                    {
                        camera.transparencySortMode=TransparencySortMode.Orthographic;
                        Assert.That(Red(Render(camera,actor,null)),Is.GreaterThan(visible*.97f),"Negative control must reproduce the lower front pillar sorting behind.");
                        StandingSpriteDepth.Configure(camera);
                    }
                    Assert.That(Red(Render(camera,actor,null)),Is.LessThan(visible*.1f),"Front pillar must stay in front at yaw "+yaw+" height "+height);
                }
                prop.enabled=false;
            }
            yield return null;
        }
        static int Red(Color32[] pixels)=>pixels.Count(c=>c.r>180&&c.g<30&&c.b<30);
        Color32[] Render(Camera camera,SpriteRenderer actor,MeshRenderer wall)
        {
            const int size=256;var target=RenderTexture.GetTemporary(size,size,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var previous=RenderTexture.active;Texture2D readback=null;bool visible=obstacle.enabled;
            try
            {
                obstacle.enabled=wall!=null;
                var request=new RenderPipeline.StandardRequest{destination=target};
                Assert.That(RenderPipeline.SupportsRenderRequest(camera,request),Is.True,"Production URP must support render requests.");
                RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=target;readback=new Texture2D(size,size,TextureFormat.RGBA32,false,true);
                readback.ReadPixels(new Rect(0,0,size,size),0,0);readback.Apply();return readback.GetPixels32();
            }
            finally {obstacle.enabled=visible;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);if(readback!=null)Object.DestroyImmediate(readback);}
        }
    }
}