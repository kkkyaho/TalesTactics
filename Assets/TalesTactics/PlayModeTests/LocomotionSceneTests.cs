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
        static readonly string[] LocomotionRoster={"cless","mint","velvet","farah","tear","jade","natalia","alphen","shionne","kisara"};
        [UnityTest] public IEnumerator WalkCyclesAllFramesInEveryViewAndStopsAtIdle()
        {
            var camera=director.Board.BattleCamera;
            foreach(var id in LocomotionRoster)
            {
                var data=director.Catalog.Characters.Single(x=>x.Id==id);
                Assert.That(data.Poses.Walk.Frames.Length,Is.EqualTo(4));
                Assert.That(data.Poses.Dead.Frames.Length,Is.EqualTo(3));
                var unit=new UnitRuntime(data,Team.Player,director.Catalog.Rules);
                var go=new GameObject("Walk cycle check");go.transform.SetParent(director.transform);
                var renderer=go.AddComponent<SpriteRenderer>();var motion=go.AddComponent<CharacterMotion>();motion.Initialize(renderer,unit,camera);
                for(int rotation=0;rotation<4;rotation++)
                {
                    camera.transform.rotation=Quaternion.Euler(45,rotation*90,0);
                    var facing=CharacterMotion.ViewFacing(unit.Facing,camera.transform.rotation);
                    var expected=data.Poses.Walk.Frames.Select(x=>x.Get(facing)).ToArray();
                    Assert.That(expected.All(x=>x!=null),Is.True);
                    Assert.That(expected.Distinct().Count(),Is.EqualTo(4));
                    // Observe multiple cycles under a busy Editor without relaxing the four-frame assertion.
                    motion.Set(AnimationKind.Walk);var observed=new HashSet<Sprite>();float until=Time.realtimeSinceStartup+1.5f;
                    while(observed.Count<expected.Length&&Time.realtimeSinceStartup<until){yield return new WaitForEndOfFrame();observed.Add(renderer.sprite);}
                    CollectionAssert.AreEquivalent(expected,observed,id+facing);
                    motion.Set(AnimationKind.Idle);yield return new WaitForEndOfFrame();
                    Assert.That(renderer.sprite,Is.EqualTo(data.Sprites.Get(facing)));
                }
            }
        }
        [UnityTest] public IEnumerator CollapseHoldsLastFrameAcrossSyncAndRevivalRestoresIdle()
        {
            for(int start=0;start<LocomotionRoster.Length;start+=5)
            {
                director.Restart();yield return null;
                director.Deployment.Clear();
                foreach(var id in LocomotionRoster.Skip(start).Take(5))director.Deployment.Add(System.Array.FindIndex(director.Catalog.Characters,x=>x.Id==id));
                director.BeginBattle();yield return null;
                Assert.That(director.Session.Units.Count(x=>x.Team==Team.Player),Is.EqualTo(5));
                foreach(var unit in director.Session.Units.Where(x=>x.Team==Team.Player))
                {
                    var motion=Object.FindObjectsByType<CharacterMotion>().Single(x=>x.transform.parent.name==unit.Data.DisplayName);
                    var renderer=motion.GetComponent<SpriteRenderer>();
                    var facing=CharacterMotion.ViewFacing(unit.Facing,director.Board.BattleCamera.transform.rotation);
                    unit.CurrentHP=0;director.Board.Sync();yield return new WaitForEndOfFrame();
                    Assert.That(renderer.sprite,Is.EqualTo(unit.Data.Poses.Dead.Frames[0].Get(facing)));
                    yield return new WaitForSeconds(0.2f);yield return new WaitForEndOfFrame();
                    Assert.That(renderer.sprite,Is.EqualTo(unit.Data.Poses.Dead.Frames[1].Get(facing)));
                    yield return new WaitForSeconds(0.2f);yield return new WaitForEndOfFrame();
                    var final=unit.Data.Poses.Dead.Frames[2].Get(facing);Assert.That(renderer.sprite,Is.EqualTo(final));
                    director.Board.Sync();yield return new WaitForEndOfFrame();
                    Assert.That(renderer.sprite,Is.EqualTo(final),"Repeated KO refresh must not restart collapse");
                    Assert.That(renderer.transform.localScale.y,Is.EqualTo(1),"Authored collapse must not be squashed");
                    unit.CurrentHP=unit.Stats.HP;director.Board.Sync();yield return new WaitForEndOfFrame();
                    Assert.That(renderer.sprite,Is.EqualTo(unit.Data.Sprites.Get(facing)));
                    Assert.That(renderer.transform.localScale.y,Is.InRange(0.99f,1.01f));
                }
            }
        }
    }
}
