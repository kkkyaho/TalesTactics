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
        // Inspect the alpha actually imported for rendering, without requiring readable assets
        // or UnityEditor APIs. Count every strong-alpha piece, including detached weapons.
        sealed class PoseAlphaPixels
        {
            public readonly int Width,Height;
            public readonly byte[] Alpha;
            public PoseAlphaPixels(int width,int height,Color32[] pixels)
            {
                Width=width;Height=height;Alpha=new byte[pixels.Length];
                for(int i=0;i<pixels.Length;i++)Alpha[i]=pixels[i].a;
            }
        }
        static float VisiblePoseHeight(Sprite art,Dictionary<Texture2D,PoseAlphaPixels> cache)
        {
            var texture=art.texture;
            if(!cache.TryGetValue(texture,out var image))
            {
                var previous=RenderTexture.active;RenderTexture target=null;Texture2D readback=null;
                try
                {
                    target=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
                    target.filterMode=FilterMode.Point;
                    Graphics.Blit(texture,target);RenderTexture.active=target;
                    readback=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false,true){hideFlags=HideFlags.HideAndDontSave};
                    readback.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0,false);
                    image=new PoseAlphaPixels(texture.width,texture.height,readback.GetPixels32());cache.Add(texture,image);
                }
                finally
                {
                    RenderTexture.active=previous;
                    if(target!=null)RenderTexture.ReleaseTemporary(target);
                    if(readback!=null)Object.Destroy(readback);
                }
            }
            var r=art.rect;
            var rect=new RectInt(Mathf.RoundToInt(r.x),Mathf.RoundToInt(r.y),Mathf.RoundToInt(r.width),Mathf.RoundToInt(r.height));
            Assert.That(rect.width>0&&rect.height>0&&rect.xMin>=0&&rect.yMin>=0&&rect.xMax<=image.Width&&rect.yMax<=image.Height,Is.True,art.name+" imported rect");
            Assert.That(art.pixelsPerUnit>0&&!float.IsNaN(art.pixelsPerUnit)&&!float.IsInfinity(art.pixelsPerUnit),Is.True,art.name+" PPU");
            int first=rect.yMax,last=rect.yMin-1;
            for(int y=rect.yMin;y<rect.yMax;y++)for(int x=rect.xMin;x<rect.xMax;x++)
                if(image.Alpha[y*image.Width+x]>128){first=Mathf.Min(first,y);last=Mathf.Max(last,y);}
            Assert.That(last,Is.GreaterThanOrEqualTo(first),art.name+" has no alpha>128 pixels");
            return (last-first+1)/art.pixelsPerUnit;
        }
        [UnityTest] public IEnumerator AllCharacterPosesRenderInFourCameraDirections()
        {
            var camera=director.Board.BattleCamera;
            var motions=new List<CharacterMotion>();
            var owners=new List<UnitRuntime>();
            var actions=new[]{AnimationKind.Attack,AnimationKind.Cast,AnimationKind.Guard,AnimationKind.Damage};
            foreach(var data in director.Catalog.Characters)
            {
                var unique=new HashSet<Sprite>();
                var pixels=new Dictionary<Texture2D,PoseAlphaPixels>();
                try
                {
                    foreach(var action in actions)foreach(Facing facing in System.Enum.GetValues(typeof(Facing)))
                    {
                        var art=data.Poses.Get(action,facing);Assert.That(art,Is.Not.Null,data.Id+action+facing);
                        Assert.That(unique.Add(art),Is.True,"Each pose/direction needs its own sprite: "+data.Id);
                        Assert.That(VisiblePoseHeight(art,pixels),Is.InRange(0.8f,1.6f),"Visible pose scale (alpha>128): "+art.name);
                    }
                }
                finally{pixels.Clear();}
                // Release the temporary readback texture before loading the next hero atlas.
                yield return null;
                var owner=new UnitRuntime(data,Team.Player,director.Catalog.Rules);owners.Add(owner);
                var go=new GameObject("Pose check "+data.Id);go.transform.SetParent(director.transform);
                var renderer=go.AddComponent<SpriteRenderer>();var motion=go.AddComponent<CharacterMotion>();
                motion.Initialize(renderer,owner,camera);motions.Add(motion);
            }
            var seen=new HashSet<Facing>();
            for(int rotation=0;rotation<4;rotation++)
            {
                camera.transform.rotation=Quaternion.Euler(45,rotation*90,0);
                var facing=CharacterMotion.ViewFacing(Facing.Front,camera.transform.rotation);seen.Add(facing);
                foreach(var action in actions)
                {
                    foreach(var motion in motions)motion.Set(action);
                    yield return new WaitForSeconds(0.12f);yield return new WaitForEndOfFrame();
                    for(int i=0;i<motions.Count;i++)
                        Assert.That(motions[i].GetComponent<SpriteRenderer>().sprite,Is.EqualTo(owners[i].Data.Poses.Get(action,facing)),owners[i].Data.Id+action+facing);
                }
            }
            Assert.That(seen.Count,Is.EqualTo(4));
        }

        [UnityTest] public IEnumerator AttackRecoversGuardPersistsAndMissingPoseFallsBack()
        {
            director.BeginBattle();yield return null;
            var unit=director.Session.Units.First();
            var motion=Object.FindObjectsByType<CharacterMotion>().Single(x=>x.Owner==unit);
            var renderer=motion.GetComponent<SpriteRenderer>();
            var facing=CharacterMotion.ViewFacing(unit.Facing,director.Board.BattleCamera.transform.rotation);
            director.Board.SetAnimation(unit,AnimationKind.Attack);yield return new WaitForEndOfFrame();
            Assert.That(renderer.sprite,Is.EqualTo(unit.Data.Sprites.Get(facing)),"Attack starts in the ready pose");
            yield return new WaitForSeconds(0.12f);yield return new WaitForEndOfFrame();
            Assert.That(renderer.sprite,Is.EqualTo(unit.Data.Poses.Attack.Get(facing)));
            yield return new WaitForSeconds(0.35f);yield return new WaitForEndOfFrame();
            Assert.That(renderer.sprite,Is.EqualTo(unit.Data.Sprites.Get(facing)),"Attack recovers without requiring a state refresh");
            director.Board.SetAnimation(unit,AnimationKind.Guard);yield return new WaitForSeconds(0.5f);yield return new WaitForEndOfFrame();
            Assert.That(renderer.sprite,Is.EqualTo(unit.Data.Poses.Guard.Get(facing)));
            Assert.That(motion.transform.localScale.y,Is.EqualTo(1),"Authored crouch must not be squashed again");
            var copy=Object.Instantiate(unit.Data);
            try
            {
                copy.Poses=null;motion.Initialize(renderer,new UnitRuntime(copy,Team.Enemy,director.Catalog.Rules){Facing=unit.Facing},director.Board.BattleCamera);
                motion.Set(AnimationKind.Cast);yield return new WaitForEndOfFrame();
                Assert.That(renderer.sprite,Is.EqualTo(copy.Sprites.Get(facing)),"Legacy/enemy assets do not require pose art");
            }
            finally{motion.Initialize(renderer,unit,director.Board.BattleCamera);Object.Destroy(copy);}
        }
    }
}
