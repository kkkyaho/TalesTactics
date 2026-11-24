using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        static readonly string[] LocomotionRoster={"cless","mint","velvet","farah","tear","jade","natalia","alphen","shionne","kisara"};
        [UnityTest, Category("EarlyBattlePolish")] public IEnumerator AcceleratedMovementAdvancesTheAuthoredWalkAtTheSameRate()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();var unit=director.Session.Active;
            var motion=director.Board.GetComponentsInChildren<CharacterMotion>().Single(m=>m.Owner==unit);
            var renderer=motion.GetComponent<SpriteRenderer>();
            var path=director.Session.Grid.Reachable(unit,out _).Keys.Select(p=>director.Session.Grid.Path(unit,p)).First(p=>p.Count>=3);
            var started=typeof(CharacterMotion).GetField("started",BindingFlags.Instance|BindingFlags.NonPublic);
            var render=typeof(CharacterMotion).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
            var position=unit.Position;var facing=unit.Facing;int hp=unit.CurrentHP,mp=unit.CurrentMP,draws=director.Session.Resolver.RandomDraws;
            float timeScale=Time.timeScale;
            foreach(float speed in new[]{1f,2f,4f})
            {
                motion.Set(AnimationKind.Idle);
                var movement=director.Board.AnimateMove(unit,path,speed);
                try
                {
                    Assert.That(movement.MoveNext(),Is.True);
                    started.SetValue(motion,Time.time-.14f);render.Invoke(motion,null);
                    var view=CharacterMotion.ViewFacing(SkillResolver.Toward(path[0],path[1]),director.Board.BattleCamera.transform.rotation);
                    var expected=EquipmentAppearance.Resolve(unit,unit.Data.Poses.Walk.Sample(view,.14f*speed,true));
                    Assert.That(renderer.sprite,Is.SameAs(expected),"Walk frames must follow the explicit "+speed+"x movement rate.");
                    Assert.That(motion.transform.localPosition,Is.EqualTo(Vector3.up*.04f),"Authored walking must keep the same foot offset.");
                }
                finally{(movement as System.IDisposable)?.Dispose();director.Board.Sync();}
            }
            Assert.That(unit.Position,Is.EqualTo(position));Assert.That(unit.Facing,Is.EqualTo(facing));
            Assert.That(unit.CurrentHP,Is.EqualTo(hp));Assert.That(unit.CurrentMP,Is.EqualTo(mp));
            Assert.That(director.Session.Resolver.RandomDraws,Is.EqualTo(draws));Assert.That(Time.timeScale,Is.EqualTo(timeScale));
        }
        [UnityTest, Category("EarlyBattlePolish")] public IEnumerator MovementCarriesElapsedTimeAcrossTileBoundariesAtEveryEnemySpeed()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();var unit=director.Session.Active;
            var motion=director.Board.GetComponentsInChildren<CharacterMotion>().Single(m=>m.Owner==unit);
            var grounding=motion.GetComponentInParent<GroundedUnitVisual>();
            var path=director.Session.Grid.Reachable(unit,out _).Keys.Select(p=>director.Session.Grid.Path(unit,p)).OrderByDescending(p=>p.Count).First();
            Assert.That(path.Count,Is.GreaterThanOrEqualTo(5));
            var position=unit.Position;var facing=unit.Facing;int draws=director.Session.Resolver.RandomDraws;
            float captureDelta=Time.captureDeltaTime,timeScale=Time.timeScale;
            try
            {
                // A fixed 50 ms frame exposes the lost fractional time at each 130 ms tile.
                // Capture timing is restored below; the global gameplay time scale is untouched.
                Time.captureDeltaTime=.05f;yield return null;
                foreach(float speed in new[]{1f,2f,4f})
                {
                    double began=Time.timeAsDouble;int frames=0;
                    var movement=director.Board.AnimateMove(unit,path,speed);
                    while(movement.MoveNext()){frames++;Assert.That(frames,Is.LessThan(100));yield return null;}
                    double elapsed=Time.timeAsDouble-began;
                    float authoredDuration=(path.Count-1)*director.Catalog.Rules.StepSeconds/speed;
                    Assert.That(elapsed,Is.InRange(authoredDuration-.001,authoredDuration+.051),
                        "Only the final rendered frame may round duration up; tile boundaries must retain fractional elapsed time at "+speed+"x.");
                    var end=director.Session.Grid[path.Last()].WorldPosition(director.Catalog.Rules.TileHeight);
                    Assert.That(Vector3.Distance(grounding.transform.position,end),Is.LessThan(.001f));
                    director.Board.Sync();yield return new WaitForEndOfFrame();
                    Assert.That(motion.Action,Is.EqualTo(AnimationKind.Idle));
                    Assert.That(motion.transform.localPosition,Is.EqualTo(Vector3.up*.04f));
                }
                Assert.That(unit.Position,Is.EqualTo(position));Assert.That(unit.Facing,Is.EqualTo(facing));
                Assert.That(director.Session.Resolver.RandomDraws,Is.EqualTo(draws));Assert.That(Time.timeScale,Is.EqualTo(timeScale));
            }
            finally{Time.captureDeltaTime=captureDelta;director.Board.Sync();}
        }
        [UnityTest] public IEnumerator WalkCyclesAllFramesInEveryViewAndStopsAtIdle()
        {
            var camera=director.Board.BattleCamera;
            foreach(var id in LocomotionRoster)
            {
                var data=director.Catalog.Characters.Single(x=>x.Id==id);
                Assert.That(data.Poses.Walk.Frames.Length,Is.EqualTo(8));
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
                    Assert.That(expected.Distinct().Count(),Is.EqualTo(8));
                    // Observe multiple cycles under a busy Editor without relaxing the eight-frame assertion.
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
                    var motion=Object.FindObjectsByType<CharacterMotion>().Single(x=>x.Owner==unit);
                    var renderer=motion.GetComponent<SpriteRenderer>();
                    var authored=motion.AuthoredProfile;Assert.That(authored,Is.Not.Null,"Actual board allies use authored motion");
                    Assert.That(authored.OwnerId,Is.EqualTo(unit.Data.Id));
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
                    Assert.That(renderer.sprite,Is.SameAs(authored.Idle.Frames[0].Get(facing)),"Revival immediately returns to authored rest");
                    Assert.That(renderer.transform.localScale.y,Is.InRange(0.99f,1.01f));
                }
            }
        }
    }
}
