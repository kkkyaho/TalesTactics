using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator GroundFacingKeyboardPreviewCancelsAndConfirmsAfterRotation()
        {
            var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            try
            {
                director.BeginBattle();yield return null;var unit=director.Session.Active;
                for(int rotation=0;rotation<4;rotation++)
                {
                    director.Board.RotateCamera(90);director.StopAllCoroutines();director.Session.Active=unit;
                    var original=unit.Facing;director.SetState(new FacingSelectionState(director));yield return null;
                    yield return KeyboardPress(keyboard,UnityEngine.InputSystem.Key.RightArrow);
                    var pending=director.PendingFacing;
                    Assert.That(pending,Is.EqualTo(director.ScreenFacing(Vector2.right)));Assert.That(unit.Facing,Is.EqualTo(original));
                    yield return KeyboardPress(keyboard,UnityEngine.InputSystem.Key.Escape);
                    Assert.That(director.State,Is.TypeOf<CommandState>());Assert.That(unit.Facing,Is.EqualTo(original));
                    director.WaitCommand();yield return KeyboardPress(keyboard,UnityEngine.InputSystem.Key.RightArrow);
                    yield return KeyboardPress(keyboard,UnityEngine.InputSystem.Key.Enter);
                    Assert.That(unit.Facing,Is.EqualTo(pending));Assert.That(director.State,Is.Not.TypeOf<FacingSelectionState>());
                }
                director.StopAllCoroutines();
            }
            finally {UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);}
        }
        [UnityTest] public IEnumerator GroundFacingArrowsMatchCameraAndAcceptPointerClicks()
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
                    var rect=(RectTransform)button.transform;Assert.That(rect.rect.width,Is.GreaterThanOrEqualTo(32));
                    var arrow=button.GetComponentInChildren<FacingArrowGraphic>();Assert.That(arrow,Is.Not.Null);
                    Assert.That(arrow.rectTransform.rect.width,Is.EqualTo(22));
                    Assert.That(director.Board.GetComponentsInChildren<GroundOverlay>().Any(a=>a.name.StartsWith("Ground facing ")),Is.False);
                    var head=director.Board.BattleCamera.WorldToScreenPoint(director.Board.HeadWorld(unit));
                    Assert.That(rect.position.y,Is.GreaterThan(head.y));
                    var view=CharacterMotion.ViewFacing(facing,director.Board.BattleCamera.transform.rotation);
                    var p=rect.anchoredPosition;
                    Assert.That(view==Facing.Front?p.y<0:view==Facing.Back?p.y>0:view==Facing.Left?p.x<0:p.x>0,Is.True);
                    var original=unit.Facing;
                    PointAt(director.GetComponent<GamepadPointer>(),button.name);
                    UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current),UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
                    yield return null;
                    Assert.That(director.PendingFacing,Is.EqualTo(facing));Assert.That(unit.Facing,Is.EqualTo(original));
                    director.GetComponent<GamepadPointer>().Submit();
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
            var panel=(RectTransform)director.Hud.transform.Find("Commands");Assert.That(panel.rect.width,Is.EqualTo(120));Assert.That(panel.rect.height,Is.EqualTo(180));
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
