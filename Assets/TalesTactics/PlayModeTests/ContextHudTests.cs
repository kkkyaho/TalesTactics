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
        Vector2 OutsideContext=>new Vector2(Screen.width-2,Screen.height*.5f);
        Vector2 TileScreen(Vector2Int p)=>director.Board.BattleCamera.WorldToScreenPoint(director.Session.Grid[p].WorldPosition(director.Catalog.Rules.TileHeight)+Vector3.up*.05f);
        [UnityTest] public IEnumerator ContextOutsideClickCancelsOneStepAndUndoesOnlyUncommittedMove()
        {
            director.BeginBattle();yield return null;var u=director.Session.Active;var origin=u.Position;int mp=u.CurrentMP;
            director.SkillCommand();yield return null;director.HandleBattleClick(OutsideContext);yield return null;
            Assert.That(director.State,Is.TypeOf<CommandState>());
            director.MoveCommand();yield return null;
            var destination=director.Session.Grid.Reachable(u,out _).Keys.First(p=>p!=origin&&p==new Vector2Int(2,2));
            director.HandleBattleClick(TileScreen(destination));
            float until=Time.realtimeSinceStartup+5;while(director.State is ActionExecutionState&&Time.realtimeSinceStartup<until)yield return null;
            Assert.That(u.Position,Is.EqualTo(destination));Assert.That(u.CanUndoMove,Is.True);
            director.SkillCommand();yield return null;director.HandleBattleClick(OutsideContext);yield return null;
            Assert.That(u.Position,Is.EqualTo(destination),"Closing a submenu must not also undo movement.");
            director.Hud.ShowUnitDetails(u);director.HandleBattleClick(OutsideContext);Assert.That(u.Position,Is.EqualTo(destination));director.Hud.CloseUnitDetails();
            var header=(RectTransform)director.Hud.transform.Find("Header");director.HandleBattleClick(header.TransformPoint(header.rect.center));Assert.That(u.Position,Is.EqualTo(destination));
            var pointer=director.GetComponent<GamepadPointer>();pointer.MoveTo(OutsideContext);pointer.Submit();yield return null;
            Assert.That(u.Position,Is.EqualTo(origin));Assert.That(u.Moved,Is.False);Assert.That(u.CurrentMP,Is.EqualTo(mp));
            Assert.That(director.Session.Move(destination),Is.True);u.Acted=true;u.CanUndoMove=false;director.SetState(new CommandState(director));yield return null;
            director.HandleBattleClick(OutsideContext);Assert.That(u.Position,Is.EqualTo(destination));
            director.SetState(new ActionExecutionState(director));director.HandleBattleClick(OutsideContext);Assert.That(director.State,Is.TypeOf<ActionExecutionState>());
        }
        [UnityTest] public IEnumerator ContextTargetClickAndTabWorkWithoutNavigationWindow()
        {
            var keyboard=InputSystem.AddDevice<Keyboard>();SkillData skill=null;
            try
            {
                director.BeginBattle();yield return null;var u=director.Session.Active;skill=Object.Instantiate(u.Data.BasicAttack);skill.Range=99;
                director.SelectSkill(skill);yield return null;
                Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name.Contains("대상")),Is.False);
                Assert.That(director.Hud.transform.Find("Message").GetComponent<CanvasGroup>().alpha,Is.Zero);
                var targets=director.AvailableTargets();yield return KeyboardPress(keyboard,Key.Tab);Assert.That(director.Target,Is.EqualTo(targets[0]));
                yield return KeyboardPress(keyboard,Key.LeftShift,Key.Tab);Assert.That(director.Target,Is.EqualTo(targets.Last()));
                Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name=="실행"),Is.False);
                var panel=(RectTransform)director.Hud.transform.Find("Commands");var actor=TileScreen(u.Position);
                Assert.That(Vector2.Distance(panel.TransformPoint(panel.rect.center),actor),Is.LessThan(Screen.width*.5f));
                for(int i=0;i<4;i++)
                {
                    var old=panel.position;director.Board.RotateCamera(90);yield return null;
                    Assert.That(Vector3.Distance(old,panel.position),Is.GreaterThan(1));
                    var corners=new Vector3[4];panel.GetWorldCorners(corners);Assert.That(corners.All(c=>c.x>=0&&c.x<=Screen.width&&c.y>=0&&c.y<=Screen.height),Is.True);
                }
                director.HandleBattleClick(OutsideContext);Assert.That(director.State,Is.TypeOf<ActionSelectionState>());Assert.That(director.Target,Is.Null);
                director.SelectSkill(skill);director.CycleTarget(1);yield return KeyboardPress(keyboard,Key.Enter);Assert.That(director.State,Is.TypeOf<ActionExecutionState>());
            }
            finally{InputSystem.RemoveDevice(keyboard);if(skill!=null)Object.Destroy(skill);}
        }
        [UnityTest] public IEnumerator HoverPreviewsAndMouseClickCastsWithoutConfirmation()
        {
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                director.BeginBattle();yield return null;
                var u=director.Session.Active;var enemy=director.Session.Units.First(x=>x.Team==Team.Enemy);
                Assert.That(director.Session.Grid.Place(enemy,new Vector2Int(2,2)),Is.True);director.Board.Sync();
                var skill=u.Data.Skills.First(x=>x.Target==TargetType.Enemy&&x.Gate==SkillGate.None&&x.MPCost>0&&!x.IsLionHowl&&x.MinRange<=1&&x.Range>=1);
                int mp=u.CurrentMP,hp=enemy.CurrentHP;director.SelectSkill(skill);yield return null;
                Vector2 screen=TileScreen(enemy.Position);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=screen,delta=new Vector2(10,10)});yield return null;yield return null;
                Assert.That(director.Target,Is.EqualTo(enemy.Position));Assert.That(director.State,Is.TypeOf<TargetSelectionState>());
                Assert.That(u.CurrentMP,Is.EqualTo(mp));Assert.That(enemy.CurrentHP,Is.EqualTo(hp));
                InputSystem.QueueStateEvent(mouse,new MouseState{position=screen}.WithButton(MouseButton.Left));yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});yield return null;
                Assert.That(director.State,Is.TypeOf<ActionExecutionState>());
                float deadline=Time.realtimeSinceStartup+10;while(director.State is ActionExecutionState&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(u.CurrentMP,Is.EqualTo(mp-skill.MPCost));Assert.That(enemy.CurrentHP,Is.LessThan(hp));
            }
            finally{InputSystem.RemoveDevice(mouse);}
        }
        [UnityTest] public IEnumerator ContextMouseOutsideCancelsButSkillRowClickKeepsTargeting()
        {
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                director.BeginBattle();yield return null;director.SkillCommand();yield return null;
                var b=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name.Contains(" · MP"));var r=(RectTransform)b.transform;
                Vector2 pos=r.TransformPoint(r.rect.center);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=pos});yield return null;yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=pos}.WithButton(MouseButton.Left));yield return null;yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=pos});yield return null;yield return null;
                Assert.That(director.State,Is.TypeOf<TargetSelectionState>());
                InputSystem.QueueStateEvent(mouse,new MouseState{position=OutsideContext});yield return null;yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=OutsideContext}.WithButton(MouseButton.Left));yield return null;yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=OutsideContext});yield return null;
                Assert.That(director.State,Is.TypeOf<ActionSelectionState>());
            }
            finally{InputSystem.RemoveDevice(mouse);}
        }
    }
}
