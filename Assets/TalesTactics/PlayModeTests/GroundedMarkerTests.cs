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
        [UnityTest] public IEnumerator GroundedThreatUnionHidesInternalEdgesAndPreservesCommittedAttacks()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();
            var b=director.Session;var enemy=b.Units.First(u=>u.Team==Team.Enemy);
            var a=b.Grid.Tiles.Values.First(t=>b.Grid[t.Coordinate+Vector2Int.right]!=null);
            var other=b.Grid[a.Coordinate+Vector2Int.right];other.Height=a.Height;
            director.Threats.Clear();director.Threats[enemy]=new HashSet<Vector2Int>{a.Coordinate,other.Coordinate};
            director.InspectUnit(enemy);director.CycleThreat();yield return null;
            var area=director.Board.GetComponentsInChildren<GroundOverlay>().Single(x=>x.name=="Threat region");
            Assert.That(area.enabled,Is.True);Assert.That(area.QuadCount,Is.EqualTo(8),"Two fills and six outside edges; no shared edge");
            var mesh=area.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.colors.Count(c=>Mathf.Approximately(c.a,.1f)),Is.EqualTo(8));
            other.Height++;director.Board.DrawThreats();Assert.That(area.QuadCount,Is.EqualTo(10),"Height changes retain a contour on both ledges");
            other.Height=a.Height;enemy.IntentPhase=1;
            director.CycleThreat();director.CycleThreat();Assert.That(director.ThreatMode,Is.Zero);
            var intent=director.Board.GetComponentsInChildren<GroundOverlay>().Single(x=>x.name=="Committed attack region");
            Assert.That(area.enabled,Is.False);Assert.That(intent.enabled,Is.True);Assert.That(intent.QuadCount,Is.EqualTo(8));
            enemy.CurrentHP=0;director.Board.DrawThreats();Assert.That(intent.enabled,Is.False);
            director.Restart();yield return null;Assert.That(director.Board.GetComponentsInChildren<GroundOverlay>(),Is.Empty);
        }
        [UnityTest] public IEnumerator GroundedSelectionAndCompletionToneFollowPhaseAndReset()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();
            var b=director.Session;var ally=b.Active;var team=(TeamTurnScheduler)b.Scheduler;
            var sprite=director.Board.GetComponentsInChildren<CharacterMotion>().Single(x=>x.Owner==ally).GetComponent<SpriteRenderer>();
            var properties=new MaterialPropertyBlock();
            yield return new WaitForEndOfFrame();
            Assert.That(director.Board.GetComponentsInChildren<Transform>().Any(t=>t.name=="Faction Ring"||t.name=="Facing indicator"),Is.False);
            var marker=director.Board.GetComponentsInChildren<GroundOverlay>().Single(x=>x.name=="Active unit");
            Assert.That(marker.enabled,Is.True);Assert.That(marker.QuadCount,Is.EqualTo(1));Assert.That(marker.GetComponentsInChildren<SpriteRenderer>().Length,Is.EqualTo(4));
            Assert.That(director.Board.GetComponentsInChildren<GroundOverlay>().Count(x=>x.name=="Contact shadow"),Is.EqualTo(b.Units.Count));
            Assert.That(sprite.sharedMaterial.shader.isSupported,Is.True);
            team.Complete(ally);b.Select(team.Preview(b.Units).First());director.SetState(new CommandState(director));
            yield return new WaitForEndOfFrame();sprite.GetPropertyBlock(properties);Assert.That(properties.GetFloat("_Saturation"),Is.EqualTo(.38f));
            foreach(var u in b.Units.Where(u=>u.Team==Team.Player))team.Complete(u);
            team.Next(b.Units);foreach(var u in b.Units.Where(u=>u.Team==Team.Enemy))team.Complete(u);team.Next(b.Units);
            yield return new WaitForEndOfFrame();sprite.GetPropertyBlock(properties);Assert.That(properties.GetFloat("_Saturation"),Is.EqualTo(1));
            director.Hud.ShowSystemMenu();yield return new WaitForEndOfFrame();Assert.That(marker.enabled,Is.False);
            director.Hud.CloseSystemMenu();director.Board.RotateCamera(90);yield return new WaitForEndOfFrame();Assert.That(marker.enabled,Is.True);
            Assert.That(marker.transform.up,Is.EqualTo(Vector3.up));
        }
    }
}
