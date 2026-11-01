using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator GroundFacingKeyboardPreviewCancelsAndConfirmsAfterRotation()
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
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator GroundFacingArrowsMatchCameraAndAcceptPointerClicks()
        {
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                director.BeginBattle();yield return null;var unit=director.Session.Active;
                for(int rotation=0;rotation<4;rotation++)
                {
                    director.Board.RotateCamera(90);
                    foreach(Facing facing in System.Enum.GetValues(typeof(Facing)))
                    {
                        InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(1,Screen.height-1)});yield return null;
                        director.StopAllCoroutines();director.Session.Active=unit;director.SetState(new FacingSelectionState(director));yield return null;
                        var panel=(RectTransform)director.Hud.transform.Find("Commands");Assert.That(panel.GetComponent<UnityEngine.UI.Image>().enabled,Is.False);
                        Assert.That(panel.GetComponentsInChildren<FacingArrowGraphic>().Length,Is.EqualTo(4));
                        var button=panel.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name==facing.ToString());
                        var rect=(RectTransform)button.transform;Assert.That(rect.rect.width,Is.GreaterThanOrEqualTo(32));
                        var arrow=button.GetComponentInChildren<FacingArrowGraphic>();Assert.That(arrow,Is.Not.Null);
                        Assert.That(arrow.rectTransform.rect.width,Is.EqualTo(30));
                        Assert.That(arrow.GetComponent<CanvasRenderer>(),Is.Not.Null,"A hit target without a renderer leaves the direction choice invisible.");
                        yield return new WaitForEndOfFrame();
                        var arrowMesh=arrow.canvasRenderer.GetMesh();
                        Assert.That(arrowMesh.vertexCount,Is.EqualTo(4),"The direction artwork must submit a visible textured quad.");
                        Assert.That(arrowMesh.colors32.All(c=>c.a>0),Is.True);
                        Assert.That(arrow.mainTexture,Is.SameAs(IllustratedIcons.Get("direction",true).Texture));
                        Assert.That(arrow.canvasRenderer.materialCount,Is.GreaterThan(0));
                        Assert.That(arrow.canvasRenderer.cull,Is.False);
                        var activeMarker=director.Board.GetComponentsInChildren<GroundOverlay>().Single(a=>a.name=="Active unit");
                        Assert.That(activeMarker.enabled,Is.True,"Ground ornament identifies whose facing is being confirmed in a crowded party.");
                        var corners=activeMarker.GetComponentsInChildren<SpriteRenderer>();
                        Assert.That(corners.Length,Is.EqualTo(4),"The active marker owns four illustrated corners.");
                        Assert.That(corners.All(r=>r.enabled),Is.True,"Facing selection keeps each corner renderer enabled.");
                        Assert.That(corners.All(r=>r.sprite!=null),Is.True,"Each corner must hold a live native Sprite, including after earlier scene teardown.");
                        Assert.That(corners.All(r=>r.sprite.texture!=null),Is.True);
                        var ground=director.Session.Grid[unit.Position].WorldPosition(director.Catalog.Rules.TileHeight);
                        Assert.That(Vector3.Distance(activeMarker.transform.position,ground),Is.LessThan(.05f),"The selection ornament stays on the active actor's tile.");
                        Assert.That(director.Board.GetComponentsInChildren<GroundOverlay>().Any(a=>a.name.StartsWith("Ground facing ")),Is.False);
                        var head=director.Board.BattleCamera.WorldToScreenPoint(director.Board.HeadWorld(unit));
                        Assert.That(rect.position.y,Is.LessThan(head.y),"Directions must surround the feet, not float above the head.");
                        var view=CharacterMotion.ViewFacing(facing,director.Board.BattleCamera.transform.rotation);
                        var p=rect.anchoredPosition;
                        Assert.That(view==Facing.Front?p.y<0:view==Facing.Back?p.y>0:view==Facing.Left?p.x<0:p.x>0,Is.True);
                        var original=unit.Facing;
                        var screen=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
                        // Use the same real UI input path for hover and click. A manually
                        // injected enter can be replaced by the mouse's actual hover.
                        InputSystem.QueueStateEvent(mouse,new MouseState{position=screen,delta=screen-mouse.position.ReadValue()});
                        yield return null;yield return new WaitForEndOfFrame();
                        var hits=new System.Collections.Generic.List<RaycastResult>();
                        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},hits);
                        Assert.That(hits.Count,Is.GreaterThan(0));
                        Assert.That(hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>(),Is.SameAs(button),"The rendered arrow center must belong to its own hit target.");
                        Assert.That(director.PendingFacing,Is.EqualTo(facing));Assert.That(unit.Facing,Is.EqualTo(original));
                        Assert.That(arrow.color.g,Is.GreaterThan(arrow.color.r),"The pending direction uses a teal accent.");
                        InputSystem.QueueStateEvent(mouse,new MouseState{position=screen}.WithButton(MouseButton.Left));yield return null;
                        InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});yield return null;
                        Assert.That(unit.Facing,Is.EqualTo(facing));Assert.That(director.State,Is.Not.TypeOf<FacingSelectionState>());
                    }
                }
                director.StopAllCoroutines();
            }
            finally{InputSystem.RemoveDevice(mouse);}
        }
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator GroundFacingSelectionArtRecoversAfterCachedSpriteLifetimeEnds()
        {
            director.BeginBattle();yield return new WaitForEndOfFrame();
            var retired=Enumerable.Range(0,4).Select(i=>IllustratedIcons.Sprite("selected",i)).ToArray();
            Assert.That(retired.All(s=>s!=null),Is.True);
            director.Restart();yield return null;yield return null;
            // Static managed references can outlive their native Unity objects.
            // Reproduce that lifetime boundary without destroying shared textures.
            foreach(var sprite in retired)Object.Destroy(sprite);
            yield return null;
            Assert.That(retired.All(s=>s==null),Is.True);
            director.BeginBattle();yield return null;director.WaitCommand();yield return new WaitForEndOfFrame();
            Assert.That(director.State,Is.TypeOf<FacingSelectionState>());
            var marker=director.Board.GetComponentsInChildren<GroundOverlay>().Single(a=>a.name=="Active unit");
            Assert.That(marker.enabled,Is.True);
            var corners=marker.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(corners.Length,Is.EqualTo(4));
            for(int i=0;i<4;i++)
            {
                Assert.That(corners[i].enabled,Is.True);
                Assert.That(corners[i].sprite!=null,Is.True,"Re-entering battle must recreate an invalid cached corner sprite.");
                Assert.That(corners[i].sprite.texture!=null,Is.True);
                Assert.That(corners[i].sprite.vertices.Length,Is.EqualTo(4));
                Assert.That(corners[i].sprite,Is.SameAs(IllustratedIcons.Sprite("selected",i)));
            }
        }
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator WalkingThroughAllyKeepsOccupancyAndAllowsUndoFromSmallMenu()
        {
            director.BeginBattle();yield return null;var unit=director.Session.Units.First(u=>u.Team==Team.Player);var ally=director.Session.Units.Where(u=>u.Team==Team.Player).Skip(1).First();
            director.Session.Active=unit;unit.BeginTurn();
            foreach(var tile in director.Session.Grid.Tiles.Values)tile.Walkable=tile.Coordinate.y==1&&tile.Coordinate.x>=1&&tile.Coordinate.x<=3;
            director.SetState(new CommandState(director));yield return null;
            var panel=(RectTransform)director.Hud.transform.Find("Commands");var canvas=(RectTransform)director.Hud.transform;
            Assert.That(panel.rect.width,Is.LessThan(canvas.rect.width*.3f));Assert.That(panel.rect.height,Is.LessThan(canvas.rect.height*.6f));
            foreach(var button in panel.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                Assert.That(corners.All(p=>panel.rect.Contains(panel.InverseTransformPoint(p))),Is.True,"Command buttons must fit inside the ornate frame");
            }
            Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.Button>().Length,Is.EqualTo(5));
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
