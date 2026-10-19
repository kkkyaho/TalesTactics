using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        IEnumerator WaitForContextHint(ContextHintKind expected)
        {
            float deadline=Time.realtimeSinceStartup+4;
            while(director.Hud.ActiveContextHint!=expected&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(director.Hud.ActiveContextHint,Is.EqualTo(expected));yield return new WaitForEndOfFrame();
        }
        [UnityTest] public IEnumerator ContextHintsCardDoesNotStealMapInputOrFocusAndDismissalSurvivesProfiles()
        {
            director.BeginBattle();yield return null;director.Message="";
            var focus=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Move / 이동").gameObject;
            EventSystem.current.SetSelectedGameObject(focus);string campaign=JsonUtility.ToJson(director.Campaign);int writes=0;director.PersistCampaign=_=>{writes++;return true;};
            yield return WaitForContextHint(ContextHintKind.MapPan);
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(focus));Assert.That(director.Hud.InputModalOpen,Is.False);
            var card=director.Hud.transform.Find("ContextHintCard");var close=card.GetComponentsInChildren<UnityEngine.UI.Button>().Single();
            foreach(var g in card.GetComponentsInChildren<UnityEngine.UI.Graphic>())Assert.That(g.raycastTarget,Is.EqualTo(g.gameObject==close.gameObject),g.name);
            var body=card.Find("ContextHintBody").GetComponent<RectTransform>();var point=(Vector2)body.TransformPoint(body.rect.center);
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            Assert.That(hits.Any(h=>h.gameObject.transform.IsChildOf(card)),Is.False,"Help copy must pass pointer input to the battlefield.");
            var stale=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(director.Preferences));string preferences=JsonUtility.ToJson(stale);
            Click("조작 안내 닫기");yield return null;
            Assert.That(director.HasLearnedContextHint(ContextHintKind.MapPan),Is.True);Assert.That(director.Hud.ContextHintOpen,Is.False);
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(campaign));Assert.That(writes,Is.Zero);
            var persisted=new PreferenceFile(Path.Combine(testStorage,"settings.json")).Load();Assert.That(persisted.SeenContextHints,Is.EqualTo(1));
            persisted.SeenContextHints=0;Assert.That(JsonUtility.ToJson(persisted),Is.EqualTo(preferences),"Automatic help persistence changes no applied setting.");
            Assert.That(director.SavePreferences(stale,false),Is.True);Assert.That(director.Preferences.SeenContextHints,Is.EqualTo(1),"A preexisting settings draft must merge learned help.");
            director.Hud.UpdateContextHints();Assert.That(director.Hud.ContextHintOpen,Is.False,"The next card is deferred instead of appearing immediately.");
            director.Restart();yield return null;Assert.That(director.SelectProfile(1),Is.True);Assert.That(director.HasLearnedContextHint(ContextHintKind.MapPan),Is.True);
            Assert.That(new PreferenceFile(Path.Combine(testStorage,"settings.json")).Load().SeenContextHints,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator ContextHintsCameraRequiresActualValidMiddleDrag()
        {
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                director.BeginBattle();yield return null;yield return new WaitForEndOfFrame();
                var point=CameraDragPoint();var step=CameraDragStep();var button=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Move / 이동");
                var rect=(RectTransform)button.transform;Vector2 ui=rect.TransformPoint(rect.rect.center);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=ui}.WithButton(MouseButton.Middle));yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Middle));yield return null;
                Assert.That(director.HasLearnedContextHint(ContextHintKind.MapPan),Is.False);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Middle));yield return null;
                Assert.That(director.Board.CameraDragging,Is.True);Assert.That(director.HasLearnedContextHint(ContextHintKind.MapPan),Is.False,"A press without movement is not a completed pan.");
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point+step,delta=step}.WithButton(MouseButton.Middle));yield return null;
                Assert.That(director.Board.CameraPanOffset.sqrMagnitude,Is.GreaterThan(.01f));Assert.That(director.HasLearnedContextHint(ContextHintKind.MapPan),Is.True);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point+step});yield return null;
                Assert.That(new PreferenceFile(Path.Combine(testStorage,"settings.json")).Load().SeenContextHints,Is.EqualTo(1));
            }
            finally{InputSystem.RemoveDevice(mouse);}
        }
        [UnityTest] public IEnumerator ContextHintsEnemyRequiresSuccessfullyOpenedLiveEnemySkills()
        {
            director.BeginBattle();yield return null;
            var active=director.Session.Active;var enemy=director.Session.Units.First(u=>u.Team==Team.Enemy);var state=director.State;int draws=director.Session.Resolver.RandomDraws;
            director.Hud.ShowEnemySummary(enemy);yield return null;Assert.That(director.HasLearnedContextHint(ContextHintKind.EnemySkills),Is.False);
            director.Hud.CloseUnitDetails();director.Hud.ShowUnitSkills(active);yield return null;Assert.That(director.HasLearnedContextHint(ContextHintKind.EnemySkills),Is.False);
            director.Hud.CloseUnitDetails();var preview=new UnitRuntime(enemy.Data,Team.Enemy,director.Catalog.Rules,1);
            director.Hud.ShowUnitSkills(preview);yield return null;Assert.That(director.HasLearnedContextHint(ContextHintKind.EnemySkills),Is.False,"Preparation previews must not acknowledge live-battle discovery.");
            director.Hud.CloseUnitDetails();director.Hud.ShowHelp();director.Hud.ShowUnitSkills(enemy);yield return null;
            Assert.That(director.HasLearnedContextHint(ContextHintKind.EnemySkills),Is.False,"A blocked modal request is not success.");director.Hud.CloseHelp();
            director.Hud.ShowEnemySummary(enemy);yield return null;Click("적 기술 보기");yield return null;
            Assert.That(director.Hud.CharacterStatusOpen,Is.True);Assert.That(director.Hud.DisplayedStatusUnit,Is.SameAs(enemy));Assert.That(director.HasLearnedContextHint(ContextHintKind.EnemySkills),Is.True);
            Assert.That(director.Session.Active,Is.SameAs(active));Assert.That(director.State,Is.SameAs(state));Assert.That(director.Session.Resolver.RandomDraws,Is.EqualTo(draws));Assert.That(active.Acted||active.Moved,Is.False);
        }
        [UnityTest] public IEnumerator ContextHintsEnemyStatusTabLearnsOnlyFromLiveEnemy()
        {
            director.BeginBattle();yield return null;
            var active=director.Session.Active;var enemy=director.Session.Units.First(u=>u.Team==Team.Enemy);string campaign=JsonUtility.ToJson(director.Campaign);
            director.Hud.ShowUnitDetails(active);yield return null;Click("상태 기술");yield return null;
            Assert.That(director.HasLearnedContextHint(ContextHintKind.EnemySkills),Is.False,"Opening an ally's ordinary skill tab does not teach enemy inspection.");director.Hud.CloseUnitDetails();
            var preview=new UnitRuntime(enemy.Data,Team.Enemy,director.Catalog.Rules,1);
            director.Hud.ShowUnitDetails(preview);yield return null;Click("상태 기술");yield return null;
            Assert.That(director.HasLearnedContextHint(ContextHintKind.EnemySkills),Is.False,"A detached preparation preview remains excluded on the ordinary tab path.");director.Hud.CloseUnitDetails();
            director.Hud.ShowUnitDetails(enemy);yield return null;
            Assert.That(director.HasLearnedContextHint(ContextHintKind.EnemySkills),Is.False,"The ability tab alone must not mark skills learned.");
            Click("상태 기술");yield return null;
            Assert.That(director.Hud.DisplayedStatusUnit,Is.SameAs(enemy));Assert.That(director.HasLearnedContextHint(ContextHintKind.EnemySkills),Is.True);
            Assert.That(new PreferenceFile(Path.Combine(testStorage,"settings.json")).Load().SeenContextHints,Is.EqualTo((int)ContextHintKind.EnemySkills));
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(campaign));Assert.That(director.Session.Active,Is.SameAs(active));Assert.That(active.Acted||active.Moved,Is.False);
        }
        [UnityTest] public IEnumerator ContextHintsFacingRequiresAValidConfirmationAndDoesNotReplaceTutorial()
        {
            director.BeginBattle();yield return null;director.ChooseFacing(Facing.Left);
            Assert.That(director.HasLearnedContextHint(ContextHintKind.FacingConfirm),Is.False);
            director.WaitCommand();director.PreviewFacing(Facing.Back);yield return null;
            Assert.That(director.HasLearnedContextHint(ContextHintKind.FacingConfirm),Is.False);director.State.Cancel();yield return null;
            Assert.That(director.HasLearnedContextHint(ContextHintKind.FacingConfirm),Is.False);
            director.WaitCommand();yield return WaitForContextHint(ContextHintKind.FacingConfirm);
            var actor=director.Session.Active;director.ChooseFacing(Facing.Left);yield return null;
            Assert.That(actor.Facing,Is.EqualTo(Facing.Left));Assert.That(director.HasLearnedContextHint(ContextHintKind.FacingConfirm),Is.True);Assert.That(director.Hud.ContextHintOpen,Is.False);
            director.Restart();yield return null;director.BeginTutorial();yield return null;
            Assert.That(director.Tutorial,Is.EqualTo(TutorialStep.Movement));Assert.That(director.Hud.ContextHintOpen,Is.False);
            director.LearnContextHint(ContextHintKind.MapPan);Assert.That(director.HasLearnedContextHint(ContextHintKind.MapPan),Is.False,"The optional four-step drill retains ownership of its teaching flow.");
        }
        [UnityTest] public IEnumerator ContextHintsHideForModalsTimingActionsAndEnemyPhaseWithoutAcknowledging()
        {
            director.BeginBattle();yield return null;director.Message="";yield return WaitForContextHint(ContextHintKind.MapPan);
            // WaitForContextHint resumes at end-of-frame; visibility is suppressed in the next LateUpdate before rendering.
            director.Hud.ShowSystemMenu();Assert.That(director.Hud.SystemMenuOpen,Is.True);yield return new WaitForEndOfFrame();Assert.That(director.Hud.ContextHintOpen,Is.False);director.Hud.CloseSystemMenu();
            director.Hud.ShowHelp();Assert.That(director.Hud.HelpOpen,Is.True);yield return new WaitForEndOfFrame();Assert.That(director.Hud.ContextHintOpen,Is.False);director.Hud.CloseHelp();
            director.TimingActive=true;director.Hud.UpdateContextHints();Assert.That(director.Hud.ContextHintOpen,Is.False);director.TimingActive=false;
            director.SetState(new ActionExecutionState(director));director.Hud.UpdateContextHints();Assert.That(director.Hud.ContextHintOpen,Is.False);
            director.SetState(new CommandState(director));director.Message="";
            while(director.Session.Active.Team==Team.Player){director.Session.EndTurn();director.Session.Advance();}
            director.Hud.UpdateContextHints();Assert.That(director.Hud.ContextHintOpen,Is.False);Assert.That(director.Preferences.SeenContextHints,Is.Zero);
        }
        [UnityTest] public IEnumerator ContextHintsProtectedAndFailedSettingsStayLearnedInMemoryWithoutCampaignWrites()
        {
            var protectedPath=Path.Combine(testStorage,"settings.json");const string future="{\"Version\":99}";Directory.CreateDirectory(testStorage);File.WriteAllText(protectedPath,future);
            director.ConfigureStorage(testStorage);director.TrainingMode=true;director.BeginBattle();yield return null;director.Message="";
            string campaign=JsonUtility.ToJson(director.Campaign);int writes=0;director.PersistCampaign=_=>{writes++;return true;};
            yield return WaitForContextHint(ContextHintKind.MapPan);Click("조작 안내 닫기");yield return null;
            Assert.That(director.HasLearnedContextHint(ContextHintKind.MapPan),Is.True);Assert.That(File.ReadAllText(protectedPath),Is.EqualTo(future));Assert.That(File.Exists(protectedPath+".tmp"),Is.False);
            Assert.That(director.SaveNotice,Does.Contain("원본은 보존"));Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(campaign));Assert.That(writes,Is.Zero);
            director.Restart();yield return null;File.Delete(protectedPath);director.ConfigureStorage(testStorage);Directory.CreateDirectory(protectedPath+".tmp");
            director.TrainingMode=true;director.PersistCampaign=_=>{writes++;return true;};director.BeginBattle();yield return null;var enemy=director.Session.Units.First(u=>u.Team==Team.Enemy);
            director.Hud.ShowUnitSkills(enemy);yield return null;Assert.That(director.HasLearnedContextHint(ContextHintKind.EnemySkills),Is.True);Assert.That(File.Exists(protectedPath),Is.False);
            director.Hud.CloseUnitDetails();director.Hud.UpdateContextHints();yield return null;
            Assert.That(Directory.Exists(protectedPath+".tmp"),Is.True);Assert.That(File.Exists(protectedPath),Is.False);Assert.That(File.Exists(Path.Combine(testStorage,"campaign.json")),Is.False);Assert.That(writes,Is.Zero);
        }
    }
}