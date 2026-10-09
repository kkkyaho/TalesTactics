using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator RewindMoveConfirmCancelSuspendAndDefeatPreserveCampaign()
        {
            director.TrainingMode=false;director.BeginBattle();if(director.StoryActive)director.FinishStory();yield return null;
            var b=director.Session;var origin=b.Active.Position;var dest=b.Grid.Reachable(b.Active,out _).Keys.First(p=>p!=origin);
            yield return director.MoveUnit(dest);Assert.That(director.CanRewind,Is.True);
            director.Hud.ShowSystemMenu();Click("행동 되감기 · 3 / 3");Click("현재 전투 계속");director.Hud.CloseSystemMenu();Assert.That(b.Active.Position,Is.EqualTo(dest));
            Assert.That(director.SuspendBattle(),Is.True);yield return null;Assert.That(director.ResumeBattle(),Is.True);yield return null;
            var campaign=JsonUtility.ToJson(director.Campaign);director.Hud.ShowSystemMenu(5);Click("되감기 확정");yield return null;
            Assert.That(director.Session.Active.Position,Is.EqualTo(origin));Assert.That(director.Session.RewindsLeft,Is.EqualTo(2));Assert.That(director.State,Is.InstanceOf<CommandState>());Assert.That(director.Hud.SystemMenuOpen,Is.False);
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(campaign));
            director.Guard();director.ChooseFacing(Facing.Back);yield return null;
            foreach(var u in director.Session.Units.Where(u=>u.Team==Team.Player))u.Damage(99999,director.Session.Grid);
            director.SetState(new BattleEndState(director));yield return null;Click("행동 되감기");Click("되감기 확정");yield return null;
            Assert.That(director.Session.Units.Where(u=>u.Team==Team.Player).All(u=>u.Alive),Is.True);Assert.That(director.Session.RewindsLeft,Is.EqualTo(1));
            foreach(var u in director.Session.Units.Where(u=>u.Team==Team.Enemy))u.Damage(99999,director.Session.Grid);
            director.SetState(new BattleEndState(director));yield return null;Assert.That(director.CanRewind,Is.False);
        }
        [UnityTest] public IEnumerator ReactionsOptionsPredictionExecutionAndRewindStayInSync()
        {
            director.Hud.ShowSystemMenu(2);Click("반격 · 지원: 꺼짐");director.Hud.CloseSystemMenu();Assert.That(director.Preferences.Reactions,Is.False);
            director.Hud.ShowSystemMenu(2);Click("반격 · 지원: 꺼짐");Click("전투 진행 설정 저장");director.Hud.CloseSystemMenu();
            director.TrainingMode=false;director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1,3});director.BeginBattle();if(director.StoryActive)director.FinishStory();yield return null;
            var b=director.Session;var actor=b.Active;var target=b.Units.First(u=>u.Team==Team.Enemy);var helper=b.Units.First(u=>u.Team==Team.Player&&u!=actor);
            foreach(var tile in b.Grid.Tiles.Values){tile.Walkable=true;tile.Height=0;}
            b.Grid.Place(actor,new Vector2Int(3,2));b.Grid.Place(target,new Vector2Int(4,2));b.Grid.Place(helper,new Vector2Int(4,3));target.Level=20;target.CurrentHP=target.Stats.HP;actor.Level=20;actor.CurrentHP=actor.Stats.HP;
            director.RefreshViews();director.AttackCommand();director.SelectTarget(target.Position);yield return null;
            Assert.That(director.Forecast.Reactions,Does.Contain("반격"));var expected=director.Forecast.Rows.ToDictionary(r=>r.Unit,r=>r.AfterHP);
            int hp=actor.CurrentHP;director.Confirm();Assert.That(director.RewindAction(),Is.False);
            for(float deadline=Time.realtimeSinceStartup+15;director.State is ActionExecutionState&&Time.realtimeSinceStartup<deadline;)yield return null;
            Assert.That(director.State,Is.InstanceOf<CommandState>());foreach(var pair in expected)Assert.That(pair.Key.CurrentHP,Is.EqualTo(pair.Value));
            Assert.That(director.RewindAction(),Is.True);yield return null;Assert.That(director.Session.Active.CurrentHP,Is.EqualTo(hp));Assert.That(director.Session.Active.Acted,Is.False);
            Assert.That(director.Session.Units.Any(u=>u.CounterUsed||u.SupportUsed),Is.False);
            var next=director.Session;next.Active.CurrentHP=1;var oldActor=next.Active;
            director.AttackCommand();director.SelectTarget(next.Units.First(u=>u.Team==Team.Enemy).Position);director.Confirm();
            for(float deadline=Time.realtimeSinceStartup+15;director.State is ActionExecutionState&&Time.realtimeSinceStartup<deadline;)yield return null;
            Assert.That(oldActor.Alive,Is.False);Assert.That(director.Session.Active,Is.Not.SameAs(oldActor));Assert.That(director.CanRewind,Is.True);
            Assert.That(director.RewindAction(),Is.True);yield return null;Assert.That(director.Session.Active.Alive,Is.True);
            director.Hud.ShowUnitDetails(director.Session.Active);yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("반격 1 · 지원 1")),Is.True);
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Any(x=>x.name=="이동"),Is.False);
        }
    }
}
