using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        Vector2 CameraDragPoint()
        {
            var v=director.Hud.BattlefieldViewport;
            return new Vector2(v.center.x*Screen.width,v.center.y*Screen.height);
        }
        Vector2 CameraDragStep()
        {
            var v=director.Hud.BattlefieldViewport;
            return new Vector2(Mathf.Min(64,v.width*Screen.width*.16f),Mathf.Min(32,v.height*Screen.height*.12f));
        }
        void DragCameraOnce(Vector2 from,Vector2 to)
        {
            Assert.That(director.HandleCameraDrag(from,true,true),Is.True);
            Assert.That(director.Board.CameraDragging,Is.True);
            director.HandleCameraDrag(to,false,true);director.HandleCameraDrag(to,false,false);
        }
        [UnityTest] public IEnumerator CameraPanSurvivesZoomRotationAndHudFitsWithoutDrifting()
        {
            director.BeginBattle();yield return null;yield return new WaitForEndOfFrame();
            var board=director.Board;var camera=board.BattleCamera;var active=director.Session.Active;
            var originalPosition=active.Position;var rotation=camera.transform.rotation;float size=camera.orthographicSize;
            string combat=string.Join("|",director.Session.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u))));
            int draws=director.Session.Resolver.RandomDraws,remaining=director.Session.RemainingAllies;
            DragCameraOnce(CameraDragPoint(),CameraDragPoint()+CameraDragStep());
            var offset=board.CameraPanOffset;Assert.That(offset.sqrMagnitude,Is.GreaterThan(.01f));Assert.That(offset.y,Is.Zero);
            board.ZoomCamera(.75f);board.RotateCamera(90);
            Assert.That(Vector3.Distance(offset,board.CameraPanOffset),Is.LessThan(.001f));
            var stable=camera.transform.position;
            for(int i=0;i<30;i++)board.ZoomCamera(1);
            Assert.That(Vector3.Distance(stable,camera.transform.position),Is.LessThan(.01f),"Repeated fitting must not accumulate pan translation.");
            director.Preferences.TextScale=1.3f;director.Hud.Refresh();yield return new WaitForEndOfFrame();
            Assert.That(Vector3.Distance(offset,board.CameraPanOffset),Is.LessThan(.001f));
            Assert.That(string.Join("|",director.Session.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u)))),Is.EqualTo(combat),"Camera exploration is read-only.");
            Assert.That(director.Session.Active,Is.SameAs(active));Assert.That(director.Session.RemainingAllies,Is.EqualTo(remaining));Assert.That(director.Session.Resolver.RandomDraws,Is.EqualTo(draws));
            director.Preferences.TextScale=1;director.Hud.Refresh();yield return new WaitForEndOfFrame();board.ResetCamera();
            Assert.That(board.CameraPanOffset,Is.EqualTo(Vector3.zero));Assert.That(board.CameraDragging,Is.False);
            Assert.That(Quaternion.Angle(rotation,camera.transform.rotation),Is.LessThan(.01f));Assert.That(camera.orthographicSize,Is.EqualTo(size).Within(.01f));
            Assert.That(active.Position,Is.EqualTo(originalPosition));Assert.That(active.Moved||active.Acted,Is.False);
        }
        [UnityTest] public IEnumerator CameraPanClampsAtMapEdgesAndRestartsAtOverview()
        {
            director.BeginBattle();yield return null;yield return new WaitForEndOfFrame();
            var board=director.Board;board.ZoomCamera(.55f);
            var positions=director.Session.Grid.Tiles.Keys.ToArray();
            float maxX=(positions.Max(p=>p.x)-positions.Min(p=>p.x)+1)*.5f,maxZ=(positions.Max(p=>p.y)-positions.Min(p=>p.y)+1)*.5f;
            for(int rotation=0;rotation<4;rotation++)
            {
                for(int i=0;i<80;i++)DragCameraOnce(CameraDragPoint(),CameraDragPoint()+CameraDragStep());
                var offset=board.CameraPanOffset;
                Assert.That(Mathf.Abs(offset.x),Is.LessThanOrEqualTo(maxX+.001f));Assert.That(Mathf.Abs(offset.z),Is.LessThanOrEqualTo(maxZ+.001f));
                bool visible=director.Session.Grid.Tiles.Values.Any(t=>
                {
                    var p=board.BattleCamera.WorldToViewportPoint(t.WorldPosition(director.Catalog.Rules.TileHeight));
                    return p.z>0&&director.Hud.BattlefieldViewport.Contains(new Vector2(p.x,p.y));
                });
                Assert.That(visible,Is.True,"Clamped exploration must retain playable map content.");board.RotateCamera(90);
            }
            director.Restart();yield return null;director.BeginBattle();yield return null;
            Assert.That(board.CameraPanOffset,Is.EqualTo(Vector3.zero));
        }
        [UnityTest] public IEnumerator CameraMiddleDragIgnoresUiAndConsumesSelectionHoverAndCancel()
        {
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                director.BeginBattle();yield return null;director.MoveCommand();yield return new WaitForEndOfFrame();
                var active=director.Session.Active;var position=active.Position;var state=director.State;
                Vector2 point=CameraDragPoint(),step=CameraDragStep();
                var command=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="취소");
                var r=(RectTransform)command.transform;Vector2 ui=r.TransformPoint(r.rect.center);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=ui}.WithButton(MouseButton.Middle));yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Middle));yield return null;
                Assert.That(director.Board.CameraPanOffset,Is.EqualTo(Vector3.zero),"A drag started over UI must not take over the map.");
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Middle));yield return null;
                Assert.That(director.Board.CameraDragging,Is.True);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point+step,delta=step}.WithButton(MouseButton.Middle).WithButton(MouseButton.Left).WithButton(MouseButton.Right));yield return null;
                Assert.That(director.Board.CameraPanOffset.sqrMagnitude,Is.GreaterThan(.01f));
                Assert.That(director.State,Is.SameAs(state));Assert.That(active.Position,Is.EqualTo(position));Assert.That(director.HealthHoveredUnit,Is.Null);
                var offset=director.Board.CameraPanOffset;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=ui}.WithButton(MouseButton.Middle));yield return null;
                Assert.That(director.Board.CameraDragging,Is.False);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Middle));yield return null;
                Assert.That(director.Board.CameraPanOffset,Is.EqualTo(offset),"Leaving the field ends the drag until the button is released.");
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;
                director.State.Cancel();yield return null;
                Assert.That(director.IsPlayerCommand,Is.True,"The system menu is available only after leaving movement selection.");
                director.Hud.ShowSystemMenu();yield return null;
                Assert.That(director.Hud.SystemMenuOpen,Is.True,"The modal precondition must be established before testing input blocking.");
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Middle));yield return null;
                Assert.That(director.Board.CameraDragging,Is.False);
                Assert.That(director.Board.CameraPanOffset,Is.EqualTo(offset));
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;director.Hud.CloseSystemMenu();
            }
            finally{InputSystem.RemoveDevice(mouse);}
        }
        [UnityTest] public IEnumerator CameraMiddleButtonDoesNotBlockModalEscapeOrTimingInput()
        {
            var mouse=InputSystem.AddDevice<Mouse>();var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                director.BeginBattle();yield return null;yield return new WaitForEndOfFrame();
                Vector2 point=CameraDragPoint();
                director.Hud.ShowSystemMenu();yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Middle));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;
                Assert.That(director.Hud.SystemMenuOpen,Is.False,"An ignored middle press must not consume modal Escape.");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                Assert.That(director.Board.CameraDragging,Is.False,"Holding after a modal closes must not begin a new map gesture.");
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Middle));yield return null;
                Assert.That(director.Board.CameraDragging,Is.True);
                director.Hud.ShowSystemMenu();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));yield return null;
                Assert.That(director.Hud.SystemMenuOpen,Is.False,"Opening a modal must release an existing drag before handling Escape.");
                Assert.That(director.Board.CameraDragging,Is.False);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;
                director.SetState(new ActionExecutionState(director));director.TimingActive=true;director.TimingSuccess=false;
                director.TimingProgress=(director.Catalog.Rules.TimingWindowStart+director.Catalog.Rules.TimingWindowEnd)*.5f;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Middle));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));yield return null;
                Assert.That(director.TimingSuccess,Is.True,"A middle press during timing must not swallow Space.");
                Assert.That(director.Board.CameraDragging,Is.False);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;
                director.TimingActive=false;director.SetState(new CommandState(director));
            }
            finally{director.TimingActive=false;InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);}
        }
    }
}
