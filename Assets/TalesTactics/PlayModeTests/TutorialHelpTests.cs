using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        IEnumerator FinishTutorialAction()
        {
            for(int i=0;i<100&&director.State is ActionExecutionState;i++)yield return new WaitForSeconds(.05f);
            Assert.That(director.State,Is.Not.InstanceOf<ActionExecutionState>());
        }
        [UnityTest] public IEnumerator TutorialRequiresRealMoveAttackHealAndFacingWithoutSaving()
        {
            director.Campaign=new CampaignSave();director.Hud.ShowDeployment();yield return null;
            string original=JsonUtility.ToJson(director.Campaign);
            var party=director.Deployment.ToArray();director.TrainingMode=false;director.UseCT=true;director.UseUtilityAI=true;
            director.PersistCampaign=_=>throw new System.Exception("Tutorial must never save");
            Click("처음 플레이 · 도움말");yield return null;Click("입문 연습 시작");yield return null;
            var fighter=director.Session.Active;Assert.That(fighter.Data.Id,Is.EqualTo("cless"));Assert.That(fighter.Level,Is.EqualTo(1));
            director.WaitCommand();director.AttackCommand();Assert.That(director.Tutorial,Is.EqualTo(TutorialStep.Movement));
            Click("Move / 이동");yield return null;director.State.Tile(new Vector2Int(0,1));yield return null;
            Assert.That(fighter.Moved,Is.False);Click("취소");yield return null;Click("Move / 이동");
            director.State.Tile(BattleDirector.TutorialDestination);yield return FinishTutorialAction();
            Assert.That(fighter.Position,Is.EqualTo(BattleDirector.TutorialDestination));Assert.That(director.Tutorial,Is.EqualTo(TutorialStep.Attack));
            var enemy=director.Session.Units.Single(u=>u.Team==Team.Enemy);int enemyHP=enemy.CurrentHP;
            Click("Attack / 공격");yield return null;director.CycleTarget(1);yield return null;
            Assert.That(enemy.CurrentHP,Is.EqualTo(enemyHP));
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.StartsWith(enemy.Data.DisplayName+": ")),Is.True,"Tutorial must keep the real damage preview visible");
            director.Confirm();yield return FinishTutorialAction();
            Assert.That(enemy.CurrentHP,Is.LessThan(enemyHP));Assert.That(director.Tutorial,Is.EqualTo(TutorialStep.Healing));
            var mint=director.Session.Active;Assert.That(mint.Data.Id,Is.EqualTo("mint"));int hp=fighter.CurrentHP,mp=mint.CurrentMP;
            var heal=mint.Data.Skills.Single(s=>s.Id=="mint.0");Click("Skill / 스킬");yield return null;
            Click(heal.DisplayName+" · MP"+heal.MPCost);yield return null;
            director.SelectTarget(mint.Position);Assert.That(director.Target,Is.Null);
            director.CycleTarget(1);yield return null;Assert.That(director.Target,Is.EqualTo(fighter.Position));
            director.Confirm();yield return FinishTutorialAction();
            Assert.That(fighter.CurrentHP,Is.GreaterThan(hp));Assert.That(mint.CurrentMP,Is.EqualTo(mp-heal.MPCost));
            Assert.That(director.Tutorial,Is.EqualTo(TutorialStep.Waiting));mint.AddStatus(StatusKind.Song,2);Click("Wait / 방향 선택");yield return null;
            Click("Left");yield return null;Assert.That(mint.Facing,Is.EqualTo(Facing.Left));Assert.That(director.Tutorial,Is.EqualTo(TutorialStep.Complete));
            Assert.That(mint.Statuses.Single(s=>s.Kind==StatusKind.Song).Turns,Is.EqualTo(1));
            Assert.That(director.RewardPending,Is.False);Click("처음부터 연습");yield return null;
            Assert.That(director.Tutorial,Is.EqualTo(TutorialStep.Movement));Click("메뉴 · 저장/설정");yield return null;Click("저장 없이 출전 준비로");yield return null;
            Assert.That(director.TutorialActive,Is.False);Assert.That(director.Session,Is.Null);
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(original));Assert.That(director.Deployment,Is.EqualTo(party));
            Assert.That(director.TrainingMode,Is.False);Assert.That(director.UseCT&&director.UseUtilityAI,Is.True);
        }
        [UnityTest] public IEnumerator HelpPagesFitBlockUnderlyingMenusAndRestoreSelection()
        {
            for(int page=0;page<8;page++)
            {
                director.Hud.ShowHelp(page);yield return null;
                var panel=(RectTransform)director.Hud.transform.Find("HelpOverlay/HelpWindow");
                foreach(var text in panel.GetComponentsInChildren<TMPro.TMP_Text>())
                {text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.text);Assert.That(text.font.HasCharacters(text.text,out uint[] missing,true,true),Is.True,text.text);}
                foreach(var button in director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.IsInteractable()))
                    Assert.That(button.transform.IsChildOf(panel),Is.True,button.name);
                var corners=new Vector3[4];panel.GetWorldCorners(corners);
                Assert.That(corners.All(p=>p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height),Is.True);
            }
            director.GetComponent<GamepadPointer>().Cancel();yield return null;Assert.That(director.Hud.HelpOpen,Is.False);
            director.BeginBattle();yield return null;director.AttackCommand();yield return null;
            var state=director.State;var active=director.Session.Active;var position=active.Position;int hp=active.CurrentHP,mp=active.CurrentMP;
            director.Hud.ShowHelp(3);yield return null;director.CycleTarget(1);
            Assert.That(director.State,Is.SameAs(state));Assert.That(director.Target,Is.Null);
            director.GetComponent<GamepadPointer>().Cancel();yield return null;
            Assert.That(director.State,Is.SameAs(state));Assert.That(active.Position,Is.EqualTo(position));Assert.That(active.CurrentHP,Is.EqualTo(hp));Assert.That(active.CurrentMP,Is.EqualTo(mp));
            director.SetState(new ActionExecutionState(director));director.Hud.ShowHelp();Assert.That(director.Hud.HelpOpen,Is.False);
        }
        [UnityTest] public IEnumerator TutorialExitRestoresCampaignAndNormalBattleRules()
        {
            director.Campaign=new CampaignSave();director.TrainingMode=false;director.SelectedStage=0;
            director.PersistCampaign=_=>throw new System.Exception("No reward should be written");
            director.BeginTutorial();yield return null;director.MoveCommand();yield return null;
            director.GetComponent<GamepadPointer>().Cancel();yield return null;Click("메뉴 · 저장/설정");yield return null;Click("저장 없이 출전 준비로");yield return null;
            director.BeginBattle();yield return null;
            Assert.That(director.TutorialActive,Is.False);Assert.That(director.Session.CampaignStage,Is.Zero);
            Assert.That(director.Session.Victory,Is.TypeOf<EliminateEnemies>());Assert.That(director.Session.Units.Count,Is.EqualTo(7));
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="Wait / 방향 선택").IsInteractable(),Is.True);
        }
    }
}
