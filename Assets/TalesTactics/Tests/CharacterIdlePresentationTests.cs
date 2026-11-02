using System;
using System.Linq;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace TalesTactics.Tests
{
    public sealed class CharacterIdlePresentationTests
    {
        BattleCatalog Catalog=>AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
        static void Step(CharacterMotion motion)=>typeof(CharacterMotion).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(motion,null);
        [Test] public void IdleSamplesHaveDistinctBoundedCadenceWithoutConsumingRandom()
        {
            var before=UnityEngine.Random.state;var calm=CharacterIdlePresentation.Sample("mint",2.5f);var lively=CharacterIdlePresentation.Sample("farah",2.5f);
            Assert.That(calm.Motion,Is.Not.EqualTo(lively.Motion));
            foreach(var c in Catalog.Characters.Where(CharacterVisualSizing.IsHero))
            {
                int tightened=0,open=0;float focus=0,gaze=0;
                for(int i=0;i<2000;i++)
                {
                    var a=CharacterIdlePresentation.Sample(c.Id,i*.01f);var b=CharacterIdlePresentation.Sample(c.Id,i*.01f);
                    Assert.That(a.Motion,Is.EqualTo(b.Motion));Assert.That(a.EyeTension,Is.EqualTo(b.EyeTension));
                    Assert.That(a.Motion.magnitude,Is.LessThan(.022f));
                    if(a.EyeTension>.98f)tightened++;if(a.EyeTension==0)open++;focus=Mathf.Max(focus,a.Focus);gaze=Mathf.Max(gaze,a.Gaze.magnitude);
                }
                Assert.That(tightened,Is.GreaterThan(4),c.Id+" never produces an eye-tension pulse");Assert.That(open,Is.GreaterThan(1700),c.Id+" pulses too often");
                Assert.That(focus,Is.GreaterThan(.08f));Assert.That(gaze,Is.GreaterThan(.035f));
            }
            Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(JsonUtility.ToJson(before)));
        }
        [Test] public void MotionKeepsFeetAndSourceArtStableAndClearsIdleOnActionsAndDeath()
        {
            var data=Catalog.Characters.Single(c=>c.Id=="alphen");var unit=new UnitRuntime(data,Team.Player,Catalog.Rules);
            var go=new GameObject("Idle source contract");var cameraObject=new GameObject("Idle contract camera");
            try
            {
                var renderer=go.AddComponent<SpriteRenderer>();var motion=go.AddComponent<CharacterMotion>();var camera=cameraObject.AddComponent<Camera>();
                motion.Initialize(renderer,unit,camera);var block=new MaterialPropertyBlock();block.SetFloat("_Brightness",.71f);renderer.SetPropertyBlock(block);
                int hp=unit.CurrentHP,mp=unit.CurrentMP;var position=unit.Position;var facing=unit.Facing;
                foreach(var weapon in new[]{(EquipmentData)null,Catalog.Equipment.Single(e=>e.ExclusiveCharacterId==data.Id&&e.Slot==EquipmentSlot.Weapon)})
                foreach(float yaw in new[]{-39f,51f,141f,231f})
                {
                    unit.Equipment[(int)EquipmentSlot.Weapon]=weapon;camera.transform.rotation=Quaternion.Euler(57,yaw,0);motion.Set(AnimationKind.Idle);Step(motion);
                    var view=CharacterMotion.ViewFacing(unit.Facing,camera.transform.rotation);
                    Assert.That(renderer.sprite,Is.SameAs(EquipmentAppearance.Resolve(unit,data.Sprites.Get(view))));
                    Assert.That(go.transform.localPosition,Is.EqualTo(new Vector3(0,.04f,0)));Assert.That(go.transform.localScale,Is.EqualTo(Vector3.one));
                    renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion").w,Is.EqualTo(1));Assert.That(block.GetFloat("_Brightness"),Is.EqualTo(.71f));
                    foreach(var action in new[]{AnimationKind.Walk,AnimationKind.Attack,AnimationKind.Guard,AnimationKind.Damage,AnimationKind.Dead})
                    {motion.Set(action);Step(motion);renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion"),Is.EqualTo(Vector4.zero),action.ToString());}
                }
                motion.Set(AnimationKind.Idle);unit.CurrentHP=0;Step(motion);renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion"),Is.EqualTo(Vector4.zero));
                unit.CurrentHP=hp;Step(motion);
                renderer.flipX=true;Step(motion);renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion"),Is.EqualTo(Vector4.zero));renderer.flipX=false;
                renderer.flipY=true;Step(motion);renderer.GetPropertyBlock(block);Assert.That(block.GetVector("_IdleMotion"),Is.EqualTo(Vector4.zero));renderer.flipY=false;
                Assert.That(unit.CurrentHP,Is.EqualTo(hp));Assert.That(unit.CurrentMP,Is.EqualTo(mp));Assert.That(unit.Position,Is.EqualTo(position));Assert.That(unit.Facing,Is.EqualTo(facing));
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(cameraObject);}
        }
        [Test] public void AlphenVisibleBodyMatchesRosterAndKeepsOneScaleForEveryWeaponAndPose()
        {
            var c=Catalog.Characters.Single(x=>x.Id=="alphen");var sprite=c.Sprites.Front;
            float measured=NativeVisibleHeight(sprite)*CharacterVisualSizing.Scale(c);
            Assert.That(measured,Is.InRange(1.13f,1.18f),"Native alpha>128 body height, excluding transparent support padding");
            var peers=Catalog.Characters.Where(x=>CharacterVisualSizing.IsHero(x)&&x.Id!="alphen"&&x.Id!="jade")
                .Select(x=>NativeVisibleHeight(x.Sprites.Front)*CharacterVisualSizing.Scale(x)).OrderBy(x=>x).ToArray();
            Assert.That(measured/peers[peers.Length/2],Is.InRange(.95f,1.05f),"Alphen must no longer be visibly shorter than the roster median");
        }
        static float NativeVisibleHeight(Sprite sprite)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                Assert.That(ImageConversion.LoadImage(texture,File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite))),Is.True);
                float sx=(float)texture.width/sprite.texture.width,sy=(float)texture.height/sprite.texture.height;var rect=sprite.rect;
                int x0=Mathf.RoundToInt(rect.xMin*sx),x1=Mathf.RoundToInt(rect.xMax*sx),y0=Mathf.RoundToInt(rect.yMin*sy),y1=Mathf.RoundToInt(rect.yMax*sy);
                int first=y1,last=y0-1;var pixels=texture.GetPixels32();
                for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(pixels[y*texture.width+x].a>128){first=Mathf.Min(first,y);last=Mathf.Max(last,y);}
                Assert.That(last,Is.GreaterThanOrEqualTo(first));return (last-first+1)/(sprite.pixelsPerUnit*sy);
            }
            finally{Object.DestroyImmediate(texture);}
        }
    }
}

