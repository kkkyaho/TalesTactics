using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator MissionBriefingCoversSixChaptersWithoutMutatingProgress()
        {
            director.TrainingMode=false;foreach(var i in Enumerable.Range(0,6))director.Campaign.StoryProgress.Add(CampaignStages.Id(i));
            director.Hud.ShowDeployment();string saved=JsonUtility.ToJson(director.Campaign);
            for(int stage=0;stage<6;stage++)
            {
                director.SelectedStage=stage;director.Hud.ShowMission();yield return null;var window=director.Hud.transform.Find("MissionOverlay/MissionWindow");
                foreach(var label in window.GetComponentsInChildren<TMPro.TMP_Text>()){label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.text);}
                string text=string.Join("\n",window.GetComponentsInChildren<TMPro.TMP_Text>().Select(t=>t.text));
                Assert.That(text,Does.Contain("승리"));Assert.That(text,Does.Contain("패배"));
                Assert.That(text,Does.Contain(CampaignMissions.Kind(stage)==ObjectiveKind.Escort?"호위 대상 전투불능":"아군 전원 전투불능"));
                Click("임무 상세 보기");yield return null;
                Assert.That(window.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains(CampaignMissions.Description(stage))),Is.True);
                Click("적 정보");yield return null;
                foreach(var i in Enumerable.Range(0,4))
                {
                    Click("적 선택: "+i);yield return null;
                    Assert.That(window.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text==CampaignEnemies.Resolve(director.Catalog,stage,i).DisplayName),Is.True);
                }
                Assert.That(director.Hud.transform.Find("Commands").GetComponentsInChildren<UnityEngine.UI.Button>().All(b=>!b.IsInteractable()),Is.True);
                director.Hud.CloseMission();Assert.That(director.Session,Is.Null);
            }
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(saved));
        }
        [UnityTest] public IEnumerator MissionModalPreservesBattleAndUpdatesRemainingEnemyCount()
        {
            director.TrainingMode=false;director.BeginBattle();yield return null;director.AttackCommand();var state=director.State;var unit=director.Session.Active;int mp=unit.CurrentMP;var position=unit.Position;
            director.Hud.ShowMission();yield return null;Assert.That(director.Hud.InputModalOpen,Is.True);
            director.CycleTarget(1);Assert.That(director.State,Is.SameAs(state));Assert.That(unit.Position,Is.EqualTo(position));Assert.That(unit.CurrentMP,Is.EqualTo(mp));
            var pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();
            try{yield return PadPress(pad,UnityEngine.InputSystem.LowLevel.GamepadButton.East);Assert.That(director.Hud.MissionOpen,Is.False);Assert.That(director.State,Is.SameAs(state));}
            finally{UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);}
            director.State.Cancel();director.Hud.ShowMission();
            director.Hud.ShowSystemMenu();Assert.That(director.Hud.MissionOpen,Is.False);Assert.That(director.Hud.SystemMenuOpen,Is.True);director.Hud.CloseSystemMenu();
            director.Session.Units.Last().Damage(99999,director.Session.Grid);director.Hud.Refresh();director.Hud.ShowMission();yield return null;
            var text=string.Join("\n",director.Hud.transform.Find("MissionOverlay/MissionWindow").GetComponentsInChildren<TMPro.TMP_Text>().Select(t=>t.text));
            Assert.That(text,Does.Contain("적 잔존 3 / 4"));Assert.That(text,Does.Contain("격파"));director.Restart();Assert.That(director.Hud.MissionOpen,Is.False);Assert.That(director.Hud.InputModalOpen,Is.False);
        }
    }
}
