using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator FreeTurnMapAndPortraitSelectionKeepActionsAndBlockFinishedUnits()
        {
            Click("전투 시작");yield return null;var b=director.Session;Assert.That(b.Scheduler,Is.TypeOf<TeamTurnScheduler>());
            var first=b.Active;var other=b.Units.First(u=>u.Team==Team.Player&&u!=first);
            director.Board.ResetCamera();yield return null;director.HandleBattleClick(TileScreen(other.Position));yield return null;
            Assert.That(b.Active,Is.SameAs(other));Assert.That(director.Hud.UnitDetailsOpen,Is.False);
            var destination=b.Grid.Reachable(other,out _).Keys.First(p=>p!=other.Position);yield return director.MoveUnit(destination);
            Click("아군 선택: "+first.Data.Id);yield return null;Click("아군 선택: "+other.Data.Id);yield return null;
            Assert.That(other.Moved&&other.CanUndoMove,Is.True);Assert.That(other.TurnsStarted,Is.EqualTo(1));
            director.WaitCommand();director.ChooseFacing(Facing.Right);yield return null;
            Assert.That(director.SelectPlayerUnit(other),Is.False);Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(x=>x.name=="아군 선택: "+other.Data.Id).interactable,Is.False);
            Assert.That(b.Active.Team,Is.EqualTo(Team.Player));
        }
        [UnityTest] public IEnumerator FreeTurnSelectionCannotInterruptTargetsModalsOrEnemyPhase()
        {
            Click("전투 시작");yield return null;var b=director.Session;var first=b.Active;var other=b.Units.First(u=>u.Team==Team.Player&&u!=first);
            director.AttackCommand();Assert.That(director.SelectPlayerUnit(other),Is.False);Assert.That(b.Active,Is.SameAs(first));director.State.Cancel();
            director.Hud.ShowSystemMenu();Assert.That(director.SelectPlayerUnit(other),Is.False);director.Hud.CloseSystemMenu();
            director.SetState(new ActionExecutionState(director));Assert.That(director.SelectPlayerUnit(other),Is.False);director.SetState(new CommandState(director));
            director.Preferences.SkipEnemyAnimations=true;
            while(b.Active.Team==Team.Player){director.WaitCommand();director.ChooseFacing(Facing.Front);}
            Assert.That(director.SelectPlayerUnit(other),Is.False);
            float deadline=Time.realtimeSinceStartup+15;
            while(b.Active.Team==Team.Enemy&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(b.Active.Team,Is.EqualTo(Team.Player));Assert.That(((TeamTurnScheduler)b.Scheduler).Round,Is.EqualTo(2));
            Assert.That(b.Units.Where(u=>u.Team==Team.Enemy).All(u=>u.TurnsStarted==1),Is.True);
            Assert.That(director.SelectPlayerUnit(other),Is.True);
        }
        [UnityTest] public IEnumerator FreeTurnStunnedSelectionSkipsOnlyThatUnitOnce()
        {
            Click("전투 시작");yield return null;var b=director.Session;var other=b.Units.First(u=>u.Team==Team.Player&&u!=b.Active);
            other.AddStatus(StatusKind.Stun,1);Assert.That(director.SelectPlayerUnit(other),Is.True);Assert.That(director.State,Is.TypeOf<ActionExecutionState>());
            Assert.That(director.SelectPlayerUnit(b.Units.First(u=>u.Team==Team.Player&&u!=other)),Is.False);
            yield return new WaitForSeconds(.5f);Assert.That(b.Active,Is.Not.SameAs(other));Assert.That(b.Active.Team,Is.EqualTo(Team.Player));
            Assert.That(other.Has(StatusKind.Stun),Is.False);Assert.That(other.TurnsStarted,Is.EqualTo(1));Assert.That(director.SelectPlayerUnit(other),Is.False);
        }
        [UnityTest] public IEnumerator FreeTurnSuspendResumeRetainsMultipleAlliedTurns()
        {
            director.TrainingMode=false;Click("전투 시작");yield return null;var b=director.Session;var first=b.Active;
            var other=b.Units.First(u=>u.Team==Team.Player&&u!=first);first.Cooldowns["test"]=3;first.Acted=true;
            Assert.That(director.SelectPlayerUnit(other),Is.True);Assert.That(director.SuspendBattle(),Is.True);yield return null;
            director.ConfigureStorage(testStorage);Assert.That(director.ResumeBattle(),Is.True);yield return null;
            Assert.That(director.Session.Active.Data.Id,Is.EqualTo(other.Data.Id));
            var restored=director.Session.Units.First(u=>u.Team==Team.Player&&u.Data.Id==first.Data.Id);Click("아군 선택: "+restored.Data.Id);yield return null;
            Assert.That(restored.Acted,Is.True);Assert.That(restored.Cooldowns["test"],Is.EqualTo(3));Assert.That(restored.TurnsStarted,Is.EqualTo(1));
        }
    }
}
