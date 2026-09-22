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
        [UnityTest] public IEnumerator AllCharacterPosesRenderInFourCameraDirections()
        {
            var camera=director.Board.BattleCamera;
            var motions=new List<CharacterMotion>();
            var owners=new List<UnitRuntime>();
            var actions=new[]{AnimationKind.Attack,AnimationKind.Cast,AnimationKind.Guard,AnimationKind.Damage};
            foreach(var data in director.Catalog.Characters)
            {
                var unique=new HashSet<Sprite>();
                foreach(var action in actions)foreach(Facing facing in System.Enum.GetValues(typeof(Facing)))
                {
                    var art=data.Poses.Get(action,facing);Assert.That(art,Is.Not.Null,data.Id+action+facing);
                    Assert.That(unique.Add(art),Is.True,"Each pose/direction needs its own sprite: "+data.Id);
                    Assert.That(art.bounds.size.y,Is.InRange(0.8f,1.6f),"Pose scale: "+art.name);
                }
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
            var motion=Object.FindObjectsByType<CharacterMotion>(FindObjectsSortMode.None).Single(x=>x.transform.parent.name==unit.Data.DisplayName);
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
