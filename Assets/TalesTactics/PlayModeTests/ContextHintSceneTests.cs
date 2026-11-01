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
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator AutomaticBattleHintsStayAbsentWithoutStealingFocusAndLearnedFlagsRemainCompatible()
        {
            director.BeginBattle();yield return null;director.Message="";
            var focus=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Move / 이동").gameObject;
            EventSystem.current.SetSelectedGameObject(focus);string campaign=JsonUtility.ToJson(director.Campaign);int writes=0;director.PersistCampaign=_=>{writes++;return true;};
            var stale=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(director.Preferences));string preferences=JsonUtility.ToJson(stale);
            yield return new WaitForSecondsRealtime(1.8f);yield return new WaitForEndOfFrame();
            Assert.That(director.Hud.ContextHintOpen,Is.False);Assert.That(director.Hud.ActiveContextHint,Is.EqualTo(ContextHintKind.None));
            Assert.That(director.Hud.transform.Find("ContextHintCard"),Is.Null,"Ordinary combat must never create delayed explanatory cards.");
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(focus));Assert.That(director.Hud.InputModalOpen,Is.False);
            Assert.That(JsonUtility.ToJson(director.Preferences),Is.EqualTo(preferences),"Removing automatic hints must not mark unseen operations learned.");
            director.Hud.DismissContextHint();Assert.That(director.Preferences.SeenContextHints,Is.Zero);
            director.LearnContextHint(ContextHintKind.MapPan);yield return null;
            Assert.That(director.HasLearnedContextHint(ContextHintKind.MapPan),Is.True);Assert.That(director.Hud.ContextHintOpen,Is.False);
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(campaign));Assert.That(writes,Is.Zero);
            var persisted=new PreferenceFile(Path.Combine(testStorage,"settings.json")).Load();Assert.That(persisted.SeenContextHints,Is.EqualTo(1));
            persisted.SeenContextHints=0;Assert.That(JsonUtility.ToJson(persisted),Is.EqualTo(preferences),"Compatibility persistence changes no applied setting.");
            Assert.That(director.SavePreferences(stale,false),Is.True);Assert.That(director.Preferences.SeenContextHints,Is.EqualTo(1),"A preexisting settings draft must merge learned operations.");
            director.Restart();yield return null;Assert.That(director.SelectProfile(1),Is.True);Assert.That(director.HasLearnedContextHint(ContextHintKind.MapPan),Is.True);
            Assert.That(new PreferenceFile(Path.Combine(testStorage,"settings.json")).Load().SeenContextHints,Is.EqualTo(1));
        }
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator ContextHintsCameraRequiresActualValidMiddleDrag()
        {
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                director.BeginBattle();yield return null;yield return new WaitForEndOfFrame();
                // The shared helper finds a real raycast-free path around the local command menu.
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
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator ContextHintsEnemyRequiresSuccessfullyOpenedLiveEnemySkills()
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
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator ContextHintsEnemyStatusTabLearnsOnlyFromLiveEnemy()
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
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator ContextHintsFacingRequiresAValidConfirmationAndDoesNotReplaceTutorial()
        {
            director.BeginBattle();yield return null;director.ChooseFacing(Facing.Left);
            Assert.That(director.HasLearnedContextHint(ContextHintKind.FacingConfirm),Is.False);
            director.WaitCommand();director.PreviewFacing(Facing.Back);yield return null;
            Assert.That(director.HasLearnedContextHint(ContextHintKind.FacingConfirm),Is.False);director.State.Cancel();yield return null;
            Assert.That(director.HasLearnedContextHint(ContextHintKind.FacingConfirm),Is.False);
            director.WaitCommand();yield return new WaitForSecondsRealtime(1.8f);yield return new WaitForEndOfFrame();
            Assert.That(director.Hud.ContextHintOpen,Is.False);Assert.That(director.DisplayBattleMessage,Is.Empty);
            Assert.That(director.Hud.GetComponentsInChildren<FacingArrowGraphic>().Length,Is.EqualTo(4));
            var actor=director.Session.Active;director.ChooseFacing(Facing.Left);yield return null;
            Assert.That(actor.Facing,Is.EqualTo(Facing.Left));Assert.That(director.HasLearnedContextHint(ContextHintKind.FacingConfirm),Is.True);Assert.That(director.Hud.ContextHintOpen,Is.False);
            director.Restart();yield return null;director.BeginTutorial();yield return null;
            Assert.That(director.Tutorial,Is.EqualTo(TutorialStep.Movement));Assert.That(director.Hud.ContextHintOpen,Is.False);
            director.LearnContextHint(ContextHintKind.MapPan);Assert.That(director.HasLearnedContextHint(ContextHintKind.MapPan),Is.False,"The optional four-step drill retains ownership of its teaching flow.");
        }
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator ContextHintsHideForModalsTimingActionsAndEnemyPhaseWithoutAcknowledging()
        {
            director.BeginBattle();yield return null;director.Message="";yield return new WaitForSecondsRealtime(1.8f);Assert.That(director.Hud.ContextHintOpen,Is.False);
            // Explicit help stays available while ordinary combat never creates an automatic overlay.
            director.Hud.ShowSystemMenu();Assert.That(director.Hud.SystemMenuOpen,Is.True);yield return new WaitForEndOfFrame();Assert.That(director.Hud.ContextHintOpen,Is.False);director.Hud.CloseSystemMenu();
            director.Hud.ShowHelp();Assert.That(director.Hud.HelpOpen,Is.True);yield return new WaitForEndOfFrame();Assert.That(director.Hud.ContextHintOpen,Is.False);director.Hud.CloseHelp();
            director.TimingActive=true;director.Hud.UpdateContextHints();Assert.That(director.Hud.ContextHintOpen,Is.False);director.TimingActive=false;
            director.SetState(new ActionExecutionState(director));director.Hud.UpdateContextHints();Assert.That(director.Hud.ContextHintOpen,Is.False);
            director.SetState(new CommandState(director));director.Message="";
            while(director.Session.Active.Team==Team.Player){director.Session.EndTurn();director.Session.Advance();}
            director.Hud.UpdateContextHints();Assert.That(director.Hud.ContextHintOpen,Is.False);Assert.That(director.Preferences.SeenContextHints,Is.Zero);
        }
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator ContextHintsProtectedAndFailedSettingsStayLearnedInMemoryWithoutCampaignWrites()
        {
            var protectedPath=Path.Combine(testStorage,"settings.json");const string future="{\"Version\":99}";Directory.CreateDirectory(testStorage);File.WriteAllText(protectedPath,future);
            director.ConfigureStorage(testStorage);director.TrainingMode=true;director.BeginBattle();yield return null;director.Message="";
            string campaign=JsonUtility.ToJson(director.Campaign);int writes=0;director.PersistCampaign=_=>{writes++;return true;};
            director.LearnContextHint(ContextHintKind.MapPan);yield return null;
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
