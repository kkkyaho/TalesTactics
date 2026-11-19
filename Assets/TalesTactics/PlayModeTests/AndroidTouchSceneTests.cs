using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using InputTouchPhase=UnityEngine.InputSystem.TouchPhase;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        void EnableTouchFixture()
        {
            typeof(BattleDirector).GetField("touchControls",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(director,true);
            director.Hud.Refresh();Canvas.ForceUpdateCanvases();
        }
        bool TouchMapPoint(Vector2Int tile,out Vector2 point)
        {
            if(!OpeningMapPoint(tile,out point))return false;
            return director.Hud.BattlefieldViewport.Contains(new Vector2(point.x/Screen.width,point.y/Screen.height));
        }
        static void QueueTouch(Touchscreen screen,int id,InputTouchPhase phase,Vector2 point)
        {
            InputSystem.QueueStateEvent(screen,new TouchState{touchId=id,phase=phase,position=point,pressure=phase==InputTouchPhase.Ended||phase==InputTouchPhase.Canceled?0:1});
        }
        IEnumerator TouchFrame(Touchscreen screen,int id,InputTouchPhase phase,Vector2 point)
        {QueueTouch(screen,id,phase,point);yield return null;yield return null;}
        IEnumerator TouchTap(Touchscreen screen,int id,Vector2 point)
        {yield return TouchFrame(screen,id,InputTouchPhase.Began,point);yield return TouchFrame(screen,id,InputTouchPhase.Ended,point);}

        [UnityTest, Category("AndroidFoundation")] public IEnumerator AndroidTouchMoveNeedsExplicitConfirmationAndCancelClearsTheOldDestination()
        {
            var touch=InputSystem.AddDevice<Touchscreen>();
            try
            {
                BeginOpeningControlsFixture();EnableTouchFixture();yield return new WaitForEndOfFrame();
                var unit=director.Session.Active;var origin=unit.Position;int history=director.Session.History.Count;
                director.MoveCommand();yield return new WaitForEndOfFrame();
                var destination=director.Session.Grid.Reachable(unit,out _).Keys.First(p=>p!=origin&&TouchMapPoint(p,out _));
                Assert.That(TouchMapPoint(destination,out var point),Is.True);
                yield return TouchTap(touch,1,point);yield return TouchTap(touch,2,point);
                Assert.That(director.UsesTouchControls,Is.True);Assert.That(director.TouchTileSelection,Is.EqualTo(destination));
                Assert.That(director.CanConfirmTouchSelection,Is.True);Assert.That(unit.Position,Is.EqualTo(origin));
                Assert.That(unit.Moved,Is.False);Assert.That(director.Session.History.Count,Is.EqualTo(history));
                director.CancelTouchSelection();Assert.That(director.TouchTileSelection,Is.Null);
                Assert.That(director.State,Is.TypeOf<CommandState>());director.ConfirmTouchSelection();
                Assert.That(unit.Position,Is.EqualTo(origin));Assert.That(director.Session.History.Count,Is.EqualTo(history));
                director.MoveCommand();yield return new WaitForEndOfFrame();
                Assert.That(TouchMapPoint(destination,out point),Is.True);yield return TouchTap(touch,3,point);
                director.ConfirmTouchSelection();director.ConfirmTouchSelection();
                float until=Time.realtimeSinceStartup+8;
                while(director.State is ActionExecutionState&&Time.realtimeSinceStartup<until)yield return null;
                Assert.That(unit.Position,Is.EqualTo(destination));Assert.That(unit.Moved,Is.True);
                Assert.That(director.Session.History.Count,Is.EqualTo(history+1));Assert.That(director.TouchTileSelection,Is.Null);
            }
            finally{InputSystem.RemoveDevice(touch);}
        }

        [UnityTest, Category("AndroidFoundation")] public IEnumerator AndroidTouchAttackPreviewsAndInvalidTileDisarmsBeforeExplicitExecution()
        {
            var touch=InputSystem.AddDevice<Touchscreen>();SkillData skill=null;
            try
            {
                BeginOpeningControlsFixture();EnableTouchFixture();yield return new WaitForEndOfFrame();
                skill=Object.Instantiate(director.Session.Active.Data.BasicAttack);skill.Range=99;
                director.SelectSkill(skill);yield return new WaitForEndOfFrame();
                var tile=director.AvailableTargets().First(p=>TouchMapPoint(p,out _));
                Assert.That(TouchMapPoint(tile,out var point),Is.True);
                string units=string.Join("|",director.Session.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u))));
                int history=director.Session.History.Count;
                yield return TouchTap(touch,1,point);
                Assert.That(director.Target,Is.EqualTo(tile));Assert.That(director.Forecast,Is.Not.Null);
                Assert.That(director.CanConfirmTouchSelection,Is.True);Assert.That(director.State,Is.TypeOf<TargetSelectionState>());
                Assert.That(string.Join("|",director.Session.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u)))),Is.EqualTo(units));
                var empty=director.Session.Grid.Tiles.Keys.First(p=>director.Session.Grid[p].Occupant==null&&TouchMapPoint(p,out _));
                Assert.That(TouchMapPoint(empty,out var emptyPoint),Is.True);yield return TouchTap(touch,2,emptyPoint);
                Assert.That(director.Target,Is.Null);Assert.That(director.Forecast,Is.Null);Assert.That(director.TouchTileSelection,Is.Null);
                director.ConfirmTouchSelection();Assert.That(director.Session.History.Count,Is.EqualTo(history));
                Assert.That(TouchMapPoint(tile,out point),Is.True);yield return TouchTap(touch,3,point);
                director.ConfirmTouchSelection();director.ConfirmTouchSelection();
                Assert.That(director.State,Is.TypeOf<ActionExecutionState>());Assert.That(director.Session.History.Count,Is.EqualTo(history+1));
                Assert.That(director.TouchTileSelection,Is.Null);
            }
            finally{director.StopAllCoroutines();InputSystem.RemoveDevice(touch);if(skill!=null)Object.Destroy(skill);}
        }

        [UnityTest, Category("AndroidFoundation")] public IEnumerator AndroidTouchBeginningOnUiCannotBecomeAMapTapOrDrag()
        {
            var touch=InputSystem.AddDevice<Touchscreen>();GameObject blocker=null;
            try
            {
                BeginOpeningControlsFixture();EnableTouchFixture();yield return new WaitForEndOfFrame();
                director.MoveCommand();yield return new WaitForEndOfFrame();
                var origin=director.Session.Active.Position;
                var destination=director.Session.Grid.Reachable(director.Session.Active,out _).Keys.First(p=>p!=origin&&TouchMapPoint(p,out _));
                Assert.That(TouchMapPoint(destination,out var point),Is.True);
                blocker=new GameObject("Touch ownership test UI",typeof(RectTransform),typeof(Image));
                var rect=(RectTransform)blocker.transform;rect.SetParent(director.Hud.transform,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)director.Hud.transform,point,null,out var local);
                rect.anchoredPosition=local;rect.sizeDelta=new Vector2(100,100);blocker.GetComponent<Image>().raycastTarget=true;
                Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
                var uiHits=new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},uiHits);
                Assert.That(uiHits.Any(hit=>hit.gameObject==blocker),Is.True,"The UI blocker must own the initial touch point before queuing input.");
                var camera=director.Board.CameraPanOffset;
                yield return TouchFrame(touch,1,InputTouchPhase.Began,point);
                blocker.SetActive(false);
                yield return TouchFrame(touch,1,InputTouchPhase.Moved,point+CameraDragStep());
                yield return TouchFrame(touch,1,InputTouchPhase.Ended,point);
                Assert.That(director.TouchTileSelection,Is.Null);Assert.That(director.Board.CameraPanOffset,Is.EqualTo(camera));
                Assert.That(director.Session.Active.Position,Is.EqualTo(origin));
                yield return TouchTap(touch,2,point);Assert.That(director.TouchTileSelection,Is.EqualTo(destination),"The next independent map tap must work after every finger was released.");
            }
            finally{if(blocker!=null)Object.Destroy(blocker);InputSystem.RemoveDevice(touch);}
        }

        [UnityTest, Category("AndroidFoundation")] public IEnumerator AndroidTouchDragAndPinchNeverCommitAndPinchReleaseCannotTurnIntoATap()
        {
            var touch=InputSystem.AddDevice<Touchscreen>();
            try
            {
                BeginOpeningControlsFixture();EnableTouchFixture();yield return new WaitForEndOfFrame();
                director.MoveCommand();yield return new WaitForEndOfFrame();
                var origin=director.Session.Active.Position;int history=director.Session.History.Count;
                var point=CameraDragPoint();var step=CameraDragStep();
                yield return TouchFrame(touch,1,InputTouchPhase.Began,point);
                yield return TouchFrame(touch,1,InputTouchPhase.Moved,point+step);
                Assert.That(director.Board.CameraPanOffset.sqrMagnitude,Is.GreaterThan(.01f));
                yield return TouchFrame(touch,1,InputTouchPhase.Ended,point+step);
                Assert.That(director.TouchTileSelection,Is.Null);Assert.That(director.Board.CameraDragging,Is.False);
                director.Board.ResetCamera();yield return new WaitForEndOfFrame();point=CameraDragPoint();
                float size=director.Board.BattleCamera.orthographicSize;
                QueueTouch(touch,2,InputTouchPhase.Began,point);QueueTouch(touch,3,InputTouchPhase.Began,point+step*.5f);
                yield return null;yield return null;
                yield return TouchFrame(touch,3,InputTouchPhase.Moved,point+step);
                Assert.That(director.Board.BattleCamera.orthographicSize,Is.LessThan(size),"Separating fingers zooms in.");
                yield return TouchFrame(touch,3,InputTouchPhase.Ended,point+step);
                var destination=director.Session.Grid.Reachable(director.Session.Active,out _).Keys.First(p=>p!=origin&&TouchMapPoint(p,out _));
                Assert.That(TouchMapPoint(destination,out var destinationPoint),Is.True);
                yield return TouchFrame(touch,2,InputTouchPhase.Moved,destinationPoint);
                yield return TouchFrame(touch,2,InputTouchPhase.Ended,destinationPoint);
                Assert.That(director.TouchTileSelection,Is.Null);Assert.That(director.Session.Active.Position,Is.EqualTo(origin));
                Assert.That(director.Session.History.Count,Is.EqualTo(history));
                yield return TouchTap(touch,4,destinationPoint);Assert.That(director.TouchTileSelection,Is.EqualTo(destination));
            }
            finally{InputSystem.RemoveDevice(touch);}
        }

        [UnityTest, Category("AndroidFoundation")] public IEnumerator AndroidTouchCancellationModalAndTimingNeverReuseHeldContacts()
        {
            var touch=InputSystem.AddDevice<Touchscreen>();
            try
            {
                BeginOpeningControlsFixture();EnableTouchFixture();yield return new WaitForEndOfFrame();
                director.MoveCommand();yield return new WaitForEndOfFrame();
                var origin=director.Session.Active.Position;
                var destination=director.Session.Grid.Reachable(director.Session.Active,out _).Keys.First(p=>p!=origin&&TouchMapPoint(p,out _));
                Assert.That(TouchMapPoint(destination,out var point),Is.True);
                yield return TouchTap(touch,5,point);Assert.That(director.CanConfirmTouchSelection,Is.True);
                yield return TouchFrame(touch,1,InputTouchPhase.Began,point);
                yield return TouchFrame(touch,1,InputTouchPhase.Canceled,point);
                Assert.That(director.TouchTileSelection,Is.Null);
                yield return TouchFrame(touch,2,InputTouchPhase.Began,point);
                // System menus intentionally cannot open during movement selection; help can.
                director.Hud.ShowHelp();Assert.That(director.Hud.InputModalOpen,Is.True);
                yield return null;yield return null;director.Hud.CloseHelp();
                Assert.That(director.Hud.InputModalOpen,Is.False);
                yield return TouchFrame(touch,2,InputTouchPhase.Ended,point);Assert.That(director.TouchTileSelection,Is.Null);
                director.TimingActive=true;yield return TouchFrame(touch,3,InputTouchPhase.Began,point);
                director.TimingActive=false;yield return TouchFrame(touch,3,InputTouchPhase.Ended,point);
                Assert.That(director.TouchTileSelection,Is.Null);Assert.That(director.Session.Active.Position,Is.EqualTo(origin));
                yield return TouchTap(touch,4,point);Assert.That(director.CanConfirmTouchSelection,Is.True);
                director.ResetTouchInput();Assert.That(director.CanConfirmTouchSelection,Is.False);
            }
            finally{director.TimingActive=false;InputSystem.RemoveDevice(touch);}
        }

        [UnityTest, Category("AndroidFoundation")] public IEnumerator AndroidTouchTutorialKeepsItsDestinationGateAndRequiresConfirmation()
        {
            var touch=InputSystem.AddDevice<Touchscreen>();
            try
            {
                director.Hud.HideTitle();director.BeginTutorial();EnableTouchFixture();director.Board.ResetCamera();yield return new WaitForEndOfFrame();
                var unit=director.Session.Active;var origin=unit.Position;director.MoveCommand();yield return new WaitForEndOfFrame();
                var wrong=director.Session.Grid.Reachable(unit,out _).Keys.First(p=>p!=origin&&p!=BattleDirector.TutorialDestination&&TouchMapPoint(p,out _));
                Assert.That(TouchMapPoint(wrong,out var wrongPoint),Is.True);yield return TouchTap(touch,1,wrongPoint);
                Assert.That(director.CanConfirmTouchSelection,Is.False);Assert.That(unit.Position,Is.EqualTo(origin));
                Assert.That(TouchMapPoint(BattleDirector.TutorialDestination,out var point),Is.True);yield return TouchTap(touch,2,point);
                Assert.That(director.TouchTileSelection,Is.EqualTo(BattleDirector.TutorialDestination));Assert.That(director.Tutorial,Is.EqualTo(TutorialStep.Movement));
                director.ConfirmTouchSelection();float until=Time.realtimeSinceStartup+8;
                while(director.State is ActionExecutionState&&Time.realtimeSinceStartup<until)yield return null;
                Assert.That(unit.Position,Is.EqualTo(BattleDirector.TutorialDestination));Assert.That(director.Tutorial,Is.EqualTo(TutorialStep.Attack));
            }
            finally{InputSystem.RemoveDevice(touch);}
        }
    }
}
