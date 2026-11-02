using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator IdlePresentationClearsOnActualDisableAndHiddenRendererCallbacks()
        {
            var go=new GameObject("Idle callback fixture");go.transform.SetParent(director.transform);
            try
            {
                var renderer=go.AddComponent<SpriteRenderer>();var motion=go.AddComponent<CharacterMotion>();var block=new MaterialPropertyBlock();
                var data=director.Catalog.Characters.Single(c=>c.Id=="alphen");var unit=new UnitRuntime(data,Team.Player,director.Catalog.Rules);
                motion.Initialize(renderer,unit,director.Board.BattleCamera);
                yield return new WaitForEndOfFrame();renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion").w,Is.EqualTo(1));
                motion.enabled=false;renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion"),Is.EqualTo(Vector4.zero),"Real MonoBehaviour.OnDisable must restore the authored art");
                motion.enabled=true;yield return new WaitForEndOfFrame();renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion").w,Is.EqualTo(1));
                renderer.forceRenderingOff=true;yield return new WaitForEndOfFrame();renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion"),Is.EqualTo(Vector4.zero));
                renderer.forceRenderingOff=false;yield return new WaitForEndOfFrame();go.SetActive(false);
                renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion"),Is.EqualTo(Vector4.zero),"Inactive GameObjects must also clear the effect");
            }
            finally{Object.Destroy(go);}
        }
        // This is a real SpriteRenderer + production shader readback. Captures are direct
        // URP render-request output: no source PNG or generated expression bitmap is modified.
        [UnityTest] public IEnumerator IdleFacesRenderOnAllHeroDirectionsAndWeaponsWithoutChangingTheRestOfTheArt()
        {
            var output=Path.Combine(Application.dataPath,"../Logs/PlaytestFeedback/CharacterIdle",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(output);
            var go=new GameObject("Idle GPU actor");var cameraObject=new GameObject("Idle GPU camera");
            var target=RenderTexture.GetTemporary(384,384,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var material=new Material(Shader.Find("TalesTactics/UnitTone"));var presentation=new CharacterIdlePresentation();
            var failures=new List<string>();
            void Check(bool passed,string message){if(!passed)failures.Add(message);}
            try
            {
                go.layer=31;go.transform.position=new Vector3(1000,0,0);var renderer=go.AddComponent<SpriteRenderer>();renderer.sharedMaterial=material;
                var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(.24f,.29f,.32f,1);camera.targetTexture=target;camera.nearClipPlane=.1f;camera.farClipPlane=30;
                var block=new MaterialPropertyBlock();
                foreach(var data in director.Catalog.Characters.Where(CharacterVisualSizing.IsHero))
                foreach(bool equipped in new[]{false,true})
                {
                    var weapon=equipped?director.Catalog.Equipment.Single(e=>e.ExclusiveCharacterId==data.Id&&e.Slot==EquipmentSlot.Weapon):null;
                    foreach(var view in new[]{Facing.Front,Facing.Left,Facing.Right,Facing.Back})
                    {
                        renderer.sprite=EquipmentAppearance.Resolve(data,weapon,data.Sprites.Get(view));
                        IdleFrame(camera,renderer,false);presentation.Apply(renderer,data,view,new CharacterIdleSample(Vector3.zero,0,Vector2.zero,0));
                        yield return null;
                        var neutral=IdleReadback(camera);renderer.GetPropertyBlock(block);var a=block.GetVector("_IdleEyeA");var b=block.GetVector("_IdleEyeB");
                        if(view!=Facing.Back)Check(a.z>0,data.Id+" "+view+" missing landmark, equipped="+equipped);
                        presentation.Apply(renderer,data,view,new CharacterIdleSample(Vector3.zero,1,Vector2.zero,0));var tension=IdleReadback(camera);
                        int inside=0,outside=0;
                        for(int i=0;i<neutral.Length;i++)if(IdleDifference(neutral[i],tension[i])>5)
                        {
                            var world=camera.ScreenToWorldPoint(new Vector3((i%384)+.5f,(i/384)+.5f,10));var local=go.transform.InverseTransformPoint(world);
                            bool eye=IdleInside(local,a)||IdleInside(local,b);if(eye)inside++;else outside++;
                        }
                        if(view==Facing.Back)Check(inside+outside==0,"Back-view face opt-out: "+data.Id);
                        else
                        {
                            // A subtle 16% UV squeeze has a smaller signal than a closed-eye
                            // replacement. Require changed samples in 2% of the projected eye
                            // ellipses (minimum two), scaling with each authored iris and view.
                            float pixelsPerUnit=384/(2*camera.orthographicSize);
                            float eyeArea=Mathf.PI*(a.z*a.w+b.z*b.w)*pixelsPerUnit*pixelsPerUnit;
                            int required=Mathf.Max(2,Mathf.CeilToInt(eyeArea*.02f));
                            Check(inside>=required,data.Id+" "+view+" eye pixel signal "+inside+" < "+required+", equipped="+equipped);
                            Check(outside<=2,"Eye tension changed non-eye art: "+data.Id+" "+view);
                        }
                        presentation.Clear(renderer);Check(IdlePixelChanges(neutral,IdleReadback(camera))==0,"Clear must restore exact source art: "+data.Id+" "+view);
                        if(view==Facing.Front)
                        {
                            string prefix=data.Id+(equipped?"-signature":"-base");
                            IdleSave(target,Path.Combine(output,prefix+"-body-neutral.png"));
                            Color32[] neutralFace=null;
                            foreach(var state in new[]{"neutral","tension","focus"})
                            {
                                var sample=state=="tension"?new CharacterIdleSample(Vector3.zero,1,Vector2.zero,0):
                                    state=="focus"?new CharacterIdleSample(Vector3.zero,0,new Vector2(.18f,.025f),data.Id=="velvet"||data.Id=="alphen"||data.Id=="shionne"?.55f:.25f):default;
                                presentation.Apply(renderer,data,view,sample);IdleFrame(camera,renderer,true);var face=IdleReadback(camera);
                                if(state=="neutral")neutralFace=face;else Check(IdlePixelChanges(neutralFace,face)>15,prefix+" "+state+" must change sampled eye pixels");
                                IdleSave(target,Path.Combine(output,prefix+"-face-"+state+".png"));
                            }
                            IdleFrame(camera,renderer,false);presentation.Apply(renderer,data,view,new CharacterIdleSample(new Vector3(.014f,.008f,.006f),0,Vector2.zero,0));
                            var settled=IdleReadback(camera);renderer.GetPropertyBlock(block);float footLine=block.GetVector("_IdleShape").x*.04f;int feet=0,feetChanges=0,upperChanges=0;
                            for(int i=0;i<neutral.Length;i++)
                            {
                                var world=camera.ScreenToWorldPoint(new Vector3((i%384)+.5f,(i/384)+.5f,10));float y=go.transform.InverseTransformPoint(world).y;
                                if(y<footLine&&y>-.04f){if(IdleDifference(neutral[i],neutral[0])>10)feet++;if(IdleDifference(neutral[i],settled[i])>3)feetChanges++;}
                                else if(IdleDifference(neutral[i],settled[i])>5)upperChanges++;
                            }
                            Check(feet>50,"Foot negative control is empty: "+prefix);Check(feetChanges==0,"Foot pixels moved: "+prefix);Check(upperChanges>30,"Upper body/weapon did not settle: "+prefix);
                            IdleSave(target,Path.Combine(output,prefix+"-body-settle.png"));
                        }
                    }
                }
                Debug.Log("Character idle direct GPU review: "+Path.GetFullPath(output));
                Assert.That(failures,Is.Empty,string.Join("\n",failures));
            }
            finally{Object.Destroy(go);Object.Destroy(cameraObject);Object.Destroy(material);RenderTexture.ReleaseTemporary(target);}
        }
        // Includes the eye mask feather and one filtered pixel at its boundary.
        static bool IdleInside(Vector3 p,Vector4 eye)=>eye.z>0&&Mathf.Abs(p.x-eye.x)<eye.z*1.12f&&Mathf.Abs(p.y-eye.y)<eye.w*1.12f;
        static int IdleDifference(Color32 a,Color32 b)=>Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b);
        static int IdlePixelChanges(Color32[] a,Color32[] b)=>a.Zip(b,(x,y)=>IdleDifference(x,y)).Count(d=>d>2);
        static void IdleFrame(Camera camera,SpriteRenderer renderer,bool face)
        {
            var bounds=renderer.sprite.bounds;var center=bounds.center;float size=Mathf.Max(bounds.size.y,bounds.size.x)*.56f;
            if(face)
            {
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);var a=block.GetVector("_IdleEyeA");var b=block.GetVector("_IdleEyeB");
                center=new Vector3(b.z>0?(a.x+b.x)*.5f:a.x,b.z>0?(a.y+b.y)*.5f:a.y,0);size=.22f;
            }
            camera.orthographicSize=size;camera.transform.position=renderer.transform.position+center+Vector3.back*10;camera.transform.rotation=Quaternion.identity;
        }
        static Color32[] IdleReadback(Camera camera)
        {
            var request=new RenderPipeline.StandardRequest{destination=camera.targetTexture};
            Assert.That(RenderPipeline.SupportsRenderRequest(camera,request),Is.True);
            RenderPipeline.SubmitRenderRequest(camera,request);
            var old=RenderTexture.active;var texture=new Texture2D(384,384,TextureFormat.RGBA32,false,true);
            try{RenderTexture.active=camera.targetTexture;texture.ReadPixels(new Rect(0,0,384,384),0,0);texture.Apply();return texture.GetPixels32();}
            finally{RenderTexture.active=old;Object.DestroyImmediate(texture);}
        }
        static void IdleSave(RenderTexture target,string path)
        {
            var old=RenderTexture.active;var texture=new Texture2D(384,384,TextureFormat.RGBA32,false,true);
            try{RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,384,384),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
            finally{RenderTexture.active=old;Object.DestroyImmediate(texture);}
        }
    }
}

