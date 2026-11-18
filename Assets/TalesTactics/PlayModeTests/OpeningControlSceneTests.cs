using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        void BeginOpeningControlsFixture()
        {
            director.Catalog.CrossoverReady=true;director.TrainingMode=false;
            director.Campaign=new CampaignSave();director.SelectedStage=CrossoverCampaign.FirstStage;
            director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1});
            director.BeginBattle();
            Assert.That(director.Session,Is.Not.Null);Assert.That(director.Session.CampaignStage,Is.EqualTo(CrossoverCampaign.FirstStage));
        }
        [UnityTest, Category("OpeningControls")] public IEnumerator OpeningGuardCancelPreservesMoveHistoryAndSuspendState()
        {
            var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();var pad=InputSystem.AddDevice<Gamepad>();
            try
            {
                BeginOpeningControlsFixture();yield return null;
                var unit=director.Session.Active;var origin=unit.Position;
                var destination=director.Session.Grid.Reachable(unit,out _).Keys.First(p=>p!=origin);
                yield return director.MoveUnit(destination);
                string before=JsonUtility.ToJson(new CheckpointUnit(unit));int history=director.Session.History.Count;
                string campaign=JsonUtility.ToJson(director.Campaign);string random=JsonUtility.ToJson(Random.state);
                for(int input=0;input<4;input++)
                {
                    director.Guard();yield return null;Assert.That(director.State,Is.TypeOf<FacingSelectionState>());
                    if(input==0)yield return KeyboardPress(keyboard,Key.Escape);
                    else if(input==1)
                    {
                        InputSystem.QueueStateEvent(mouse,new MouseState{position=OutsideContext}.WithButton(MouseButton.Right));yield return null;yield return null;
                        InputSystem.QueueStateEvent(mouse,new MouseState{position=OutsideContext});yield return null;
                    }
                    else if(input==2)yield return PadPress(pad,GamepadButton.East);
                    else{director.HandleBattleClick(OutsideContext);yield return null;}
                    Assert.That(director.State,Is.TypeOf<CommandState>(),"Cancel input "+input);
                    Assert.That(JsonUtility.ToJson(new CheckpointUnit(unit)),Is.EqualTo(before),"Cancelling direction selection must not spend the guard action or the reversible move: "+input);
                    Assert.That(director.Session.History.Count,Is.EqualTo(history),"A cancelled command must not consume a rewind record.");
                    Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(campaign));
                    Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random));
                }
                Assert.That(director.SuspendBattle(),Is.True);yield return null;
                Assert.That(director.ResumeBattle(),Is.True);yield return null;
                Assert.That(JsonUtility.ToJson(new CheckpointUnit(director.Session.Active)),Is.EqualTo(before));
                Assert.That(director.Session.History.Count,Is.EqualTo(history));
                director.Undo();Assert.That(director.Session.Active.Position,Is.EqualTo(origin));
                Assert.That(director.Session.Active.Acted,Is.False);Assert.That(director.Session.Active.Has(StatusKind.Guard),Is.False);
            }
            finally{InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(pad);}
        }
        [UnityTest, Category("OpeningControls")] public IEnumerator OpeningGuardConfirmCommitsOnceAndRewindsToTheUncommittedMove()
        {
            BeginOpeningControlsFixture();yield return null;
            var unit=director.Session.Active;var destination=director.Session.Grid.Reachable(unit,out _).Keys.First(p=>p!=unit.Position);
            yield return director.MoveUnit(destination);
            string before=JsonUtility.ToJson(new CheckpointUnit(unit));int history=director.Session.History.Count;
            director.Guard();director.ChooseFacing(Facing.Back);yield return null;
            Assert.That(unit.Acted,Is.True);Assert.That(unit.CanUndoMove,Is.False);Assert.That(unit.Has(StatusKind.Guard),Is.True);
            Assert.That(unit.Facing,Is.EqualTo(Facing.Back));Assert.That(director.Session.Active,Is.Not.SameAs(unit));
            Assert.That(director.Session.History.Count,Is.EqualTo(history+1));Assert.That(director.RewindLabel,Is.EqualTo("방어"));
            director.ChooseFacing(Facing.Front);Assert.That(director.Session.History.Count,Is.EqualTo(history+1),"A repeated confirmation outside facing selection must do nothing.");
            Assert.That(director.RewindAction(),Is.True);yield return null;
            Assert.That(JsonUtility.ToJson(new CheckpointUnit(director.Session.Active)),Is.EqualTo(before));
            Assert.That(director.Session.History.Count,Is.EqualTo(history));
            var restored=director.Session.Active;director.Guard();director.State.Cancel();director.WaitCommand();director.ChooseFacing(Facing.Front);
            Assert.That(restored.Has(StatusKind.Guard),Is.False,"A later wait must not inherit a cancelled guard intent.");
            Assert.That(director.RewindLabel,Is.EqualTo("대기"));
        }
        bool OpeningMapPoint(Vector2Int tile,out Vector2 screen)
        {
            screen=TileScreen(tile);
            if(!director.Board.BattleCamera.pixelRect.Contains(screen)||!director.Board.Pick(screen,out var picked)||picked!=tile)return false;
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},hits);
            return hits.Count==0;
        }
        [UnityTest, Category("OpeningControls")] public IEnumerator OpeningMouseLeavesTargetClearsForecastAndCannotCastTheOldTarget()
        {
            var mouse=InputSystem.AddDevice<Mouse>();var keyboard=InputSystem.AddDevice<Keyboard>();SkillData skill=null;
            try
            {
                BeginOpeningControlsFixture();yield return null;
                var unit=director.Session.Active;skill=Object.Instantiate(unit.Data.BasicAttack);skill.Range=99;
                director.SelectSkill(skill);yield return null;yield return new WaitForEndOfFrame();
                var range=director.Board.GetComponentsInChildren<LineRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Range border ")).ToDictionary(r=>r,r=>new KeyValuePair<Color,float>(r.startColor,r.startWidth));
                Assert.That(range.Count,Is.GreaterThan(0));
                var target=director.AvailableTargets().First(p=>OpeningMapPoint(p,out _));
                var empty=director.Session.Grid.Tiles.Keys.First(p=>director.Session.Grid[p].Occupant==null&&OpeningMapPoint(p,out _));
                Assert.That(OpeningMapPoint(target,out var targetPoint),Is.True);Assert.That(OpeningMapPoint(empty,out var emptyPoint),Is.True);
                string units=string.Join("|",director.Session.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u))));
                int history=director.Session.History.Count;string random=JsonUtility.ToJson(Random.state);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=targetPoint,delta=Vector2.one});yield return null;yield return null;
                Assert.That(director.Target,Is.EqualTo(target));Assert.That(director.Forecast,Is.Not.Null);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=emptyPoint,delta=emptyPoint-targetPoint});yield return null;yield return null;
                Assert.That(director.State,Is.TypeOf<TargetSelectionState>());Assert.That(director.Target,Is.Null,"The old target cannot remain armed after the cursor moves onto an empty tile.");
                Assert.That(director.Forecast,Is.Null);
                var visible=director.Board.GetComponentsInChildren<LineRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Range border ")).ToArray();
                CollectionAssert.AreEquivalent(range.Keys,visible,"Removing a preview must restore the skill range without keeping the old impact area.");
                foreach(var edge in visible){Assert.That(edge.startColor,Is.EqualTo(range[edge].Key));Assert.That(edge.startWidth,Is.EqualTo(range[edge].Value));}
                yield return KeyboardPress(keyboard,Key.Enter);
                Assert.That(director.State,Is.TypeOf<TargetSelectionState>());
                Assert.That(string.Join("|",director.Session.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u)))),Is.EqualTo(units));
                Assert.That(director.Session.History.Count,Is.EqualTo(history));Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random));
                // With no new pointer movement, Tab selection remains usable from the keyboard.
                yield return KeyboardPress(keyboard,Key.Tab);Assert.That(director.Target.HasValue,Is.True);
                yield return null;Assert.That(director.Target.HasValue,Is.True);
                yield return KeyboardPress(keyboard,Key.Enter);Assert.That(director.State,Is.TypeOf<ActionExecutionState>());
            }
            finally{InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);if(skill!=null)Object.Destroy(skill);}
        }
    }
}
