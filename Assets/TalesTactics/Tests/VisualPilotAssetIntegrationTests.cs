using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TalesTactics.Tests
{
    public sealed class VisualPilotAssetIntegrationTests
    {
        const string ArtRoot="Assets/TalesTactics/Art/VisualPilot/Alphen/";
        const string MotionPath="Assets/TalesTactics/Resources/TalesTactics/VisualPilot/AlphenMotion.asset";
        [Serializable] sealed class Point {public float x,y;}
        [Serializable] sealed class Frame {public string name;public int x,y,width,height;public Point pivot;}
        [Serializable] sealed class Sheet {public string path;public int width,height;public Frame[] frames;}
        [Serializable] sealed class Manifest {public int schemaVersion;public float pixelsPerUnit;public Sheet[] sheets;}
        sealed class Expected {public Sheet Sheet;public Frame Frame;}

        [Test] public void ImportedProductionFramesLoadThroughResourcesAndDriveOnlyOptedInMotion()
        {
            var pilot=Resources.Load<AlphenVisualPilotMotion>(AlphenVisualPilotMotion.ResourcePath);
            Assert.That(pilot,Is.Not.Null,"Import the production pilot asset before running this test; synthetic clips cannot prove the resource connection.");
            Assert.That(AssetDatabase.GetAssetPath(pilot),Is.EqualTo(MotionPath));
            Assert.That(pilot,Is.SameAs(AssetDatabase.LoadAssetAtPath<AlphenVisualPilotMotion>(MotionPath)));
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.GetFullPath("Docs/VisualPilot20261124/art-layout.json")));
            Assert.That(manifest,Is.Not.Null);Assert.That(manifest.schemaVersion,Is.EqualTo(1));
            Assert.That(manifest.pixelsPerUnit,Is.EqualTo(188.5f).Within(.001f));
            Assert.That(manifest.sheets,Has.Length.EqualTo(2));
            Assert.That(manifest.sheets.Select(s=>s.path),Is.EquivalentTo(new[]{ArtRoot+"idle-v1.png",ArtRoot+"action-v1.png"}));
            var expected=manifest.sheets.SelectMany(s=>s.frames.Select(f=>new Expected{Sheet=s,Frame=f})).ToDictionary(e=>e.Frame.name);
            Assert.That(expected,Has.Count.EqualTo(32));
            var seen=new HashSet<Sprite>();
            VerifyClip(pilot.Idle,"idle",4,ArtRoot+"idle-v1.png",manifest.pixelsPerUnit,expected,seen);
            VerifyClip(pilot.Attack,"attack",3,ArtRoot+"action-v1.png",manifest.pixelsPerUnit,expected,seen);
            VerifyClip(pilot.Damage,"damage",1,ArtRoot+"action-v1.png",manifest.pixelsPerUnit,expected,seen);
            Assert.That(seen,Has.Count.EqualTo(32),"Every directional pose must reference its own imported sprite.");
            Assert.That(pilot.Attack.ReleaseFrame,Is.EqualTo(1));

            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            Assert.That(catalog,Is.Not.Null);
            var data=catalog.Characters.Single(c=>c.Id=="alphen");
            var owner=new UnitRuntime(data,Team.Player,catalog.Rules);
            var tick=typeof(CharacterMotion).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That(tick,Is.Not.Null);
            GameObject actor=null,cameraObject=null;
            try
            {
                actor=new GameObject("Production pilot connection test");
                var renderer=actor.AddComponent<SpriteRenderer>();var motion=actor.AddComponent<CharacterMotion>();
                cameraObject=new GameObject("Production pilot connection camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
                var directions=new HashSet<Facing>();
                foreach(float yaw in new[]{-39f,51f,141f,231f})
                {
                    camera.transform.rotation=Quaternion.Euler(57,yaw,0);
                    var facing=CharacterMotion.ViewFacing(owner.Facing,camera.transform.rotation);directions.Add(facing);
                    // No private asset injection: Initialize must find the production Resources asset itself.
                    motion.Initialize(renderer,owner,camera,true);tick.Invoke(motion,null);
                    Assert.That(pilot.Idle.Frames.Select(f=>f.Get(facing)),Does.Contain(renderer.sprite),"No imported idle pose at camera yaw "+yaw);
                    Assert.That(AssetDatabase.GetAssetPath(renderer.sprite),Is.EqualTo(ArtRoot+"idle-v1.png"));
                    motion.Initialize(renderer,owner,camera,false);tick.Invoke(motion,null);
                    Assert.That(renderer.sprite,Is.SameAs(EquipmentAppearance.Resolve(owner,data.Sprites.Get(facing))),"Opting out must restore existing art at yaw "+yaw);
                    Assert.That(seen.Contains(renderer.sprite),Is.False,"A prior pilot must not leak into another stage.");
                }
                Assert.That(directions,Has.Count.EqualTo(4));
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);
                if(cameraObject!=null)Object.DestroyImmediate(cameraObject);
            }
        }

        static void VerifyClip(DirectionalSpriteClip clip,string kind,int count,string source,float ppu,
            Dictionary<string,Expected> expected,HashSet<Sprite> seen)
        {
            Assert.That(clip,Is.Not.Null,kind);Assert.That(clip.Frames,Has.Length.EqualTo(count),kind);
            foreach(Facing direction in Enum.GetValues(typeof(Facing)))for(int i=0;i<count;i++)
            {
                string name="alphen-pilot-"+kind+"-"+direction.ToString().ToLowerInvariant()+"-"+i;
                Assert.That(clip.Frames[i],Is.Not.Null,name);
                var sprite=clip.Frames[i].Get(direction);Assert.That(sprite,Is.Not.Null,name);
                Assert.That(seen.Add(sprite),Is.True,"Duplicate sprite reference: "+name);
                Assert.That(sprite.name,Is.EqualTo(name));Assert.That(expected.ContainsKey(name),Is.True,name);
                var entry=expected[name];var frame=entry.Frame;
                Assert.That(entry.Sheet.path,Is.EqualTo(source),name);Assert.That(AssetDatabase.GetAssetPath(sprite),Is.EqualTo(source),name);
                Assert.That(sprite.pixelsPerUnit,Is.EqualTo(ppu).Within(.001f),name);
                Assert.That(sprite.rect,Is.EqualTo(new Rect(frame.x,frame.y,frame.width,frame.height)),name);
                Assert.That(sprite.texture.width,Is.EqualTo(entry.Sheet.width),name);Assert.That(sprite.texture.height,Is.EqualTo(entry.Sheet.height),name);
                Assert.That(frame.pivot,Is.Not.Null,name);Assert.That(frame.pivot.x,Is.InRange(0f,1f),name);Assert.That(frame.pivot.y,Is.InRange(0f,1f),name);
                var expectedPivot=new Vector2(frame.pivot.x*frame.width,frame.pivot.y*frame.height);
                Assert.That(Vector2.Distance(sprite.pivot,expectedPivot),Is.LessThan(.005f),"Imported foot pivot: "+name);
                Assert.That(UnitHeadAnchors.TryGetLocal(sprite,out var head),Is.True,"Authored head anchor: "+name);
                var normalizedHead=new Vector2((head.x*sprite.pixelsPerUnit+sprite.pivot.x)/sprite.rect.width,
                    (head.y*sprite.pixelsPerUnit+sprite.pivot.y)/sprite.rect.height);
                Assert.That(float.IsNaN(normalizedHead.x)||float.IsInfinity(normalizedHead.x),Is.False,name);
                Assert.That(float.IsNaN(normalizedHead.y)||float.IsInfinity(normalizedHead.y),Is.False,name);
                Assert.That(normalizedHead.x,Is.InRange(0f,1f),"Head anchor horizontal bounds: "+name);
                Assert.That(normalizedHead.y,Is.InRange(0f,1f),"Head anchor vertical bounds: "+name);
            }
        }
    }
}
