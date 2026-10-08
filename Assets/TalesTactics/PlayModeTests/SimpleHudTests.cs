using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator ThinFacingArrowsMatchCameraAndAcceptPointerClicks()
        {
            director.BeginBattle();yield return null;var unit=director.Session.Active;
            for(int rotation=0;rotation<4;rotation++)
            {
                director.Board.RotateCamera(90);
                foreach(Facing facing in System.Enum.GetValues(typeof(Facing)))
                {
                    director.StopAllCoroutines();director.Session.Active=unit;director.SetState(new FacingSelectionState(director));yield return null;
                    var panel=(RectTransform)director.Hud.transform.Find("Commands");Assert.That(panel.GetComponent<UnityEngine.UI.Image>().enabled,Is.False);
                    var button=panel.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name==facing.ToString());
                    var rect=(RectTransform)button.transform;Assert.That(rect.rect.width,Is.EqualTo(40));
                    var icon=button.GetComponentInChildren<TacticalIcon>();Assert.That(icon.rectTransform.rect.width,Is.EqualTo(20));
                    var view=CharacterMotion.ViewFacing(facing,director.Board.BattleCamera.transform.rotation);
                    var p=rect.anchoredPosition;
                    Assert.That(view==Facing.Front?p.y<0:view==Facing.Back?p.y>0:view==Facing.Left?p.x<0:p.x>0,Is.True);
                    PointAt(director.GetComponent<GamepadPointer>(),button.name);director.GetComponent<GamepadPointer>().Submit();
                    Assert.That(unit.Facing,Is.EqualTo(facing));Assert.That(director.State,Is.Not.TypeOf<FacingSelectionState>());
                }
            }
            director.StopAllCoroutines();
        }
        [UnityTest] public IEnumerator WalkingThroughAllyKeepsOccupancyAndAllowsUndoFromSmallMenu()
        {
            director.BeginBattle();yield return null;var unit=director.Session.Units.First(u=>u.Team==Team.Player);var ally=director.Session.Units.Where(u=>u.Team==Team.Player).Skip(1).First();
            director.Session.Active=unit;unit.BeginTurn();
            foreach(var tile in director.Session.Grid.Tiles.Values)tile.Walkable=tile.Coordinate.y==1&&tile.Coordinate.x>=1&&tile.Coordinate.x<=3;
            director.SetState(new CommandState(director));yield return null;
            var panel=(RectTransform)director.Hud.transform.Find("Commands");Assert.That(panel.rect.width,Is.EqualTo(104));Assert.That(panel.rect.height,Is.EqualTo(168));
            Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.Button>().Length,Is.EqualTo(6));
            var origin=unit.Position;var occupied=ally.Position;var destination=new Vector2Int(3,1);
            Assert.That(director.Session.Move(occupied),Is.False);Assert.That(unit.Moved,Is.False);
            Assert.That(director.Session.Grid.Path(unit,destination),Does.Contain(occupied));
            Click("Move / 이동");director.State.Tile(destination);
            for(int frame=0;frame<240&&director.State is ActionExecutionState;frame++)
            {Assert.That(director.Session.Grid[occupied].Occupant,Is.SameAs(ally));yield return null;}
            Assert.That(unit.Position,Is.EqualTo(destination));Assert.That(ally.Position,Is.EqualTo(occupied));
            Click("Undo Move / 이동 취소");yield return null;
            Assert.That(unit.Position,Is.EqualTo(origin));Assert.That(director.Session.Grid[occupied].Occupant,Is.SameAs(ally));Assert.That(unit.Moved,Is.False);
        }
    }
}
