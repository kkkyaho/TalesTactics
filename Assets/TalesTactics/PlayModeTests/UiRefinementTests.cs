using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator HubProgressSelectsFirstUnfinishedAndPersonalReturnPreservesProgress()
        {
            TitleSetup();director.StartTitleGame(0);
            director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(1));
            director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(2));
            string before=JsonUtility.ToJson(director.Campaign);
            director.SelectedStage=PersonalScenarios.FirstStage;director.Hud.ShowIntermissionHub();yield return null;
            Assert.That(director.SelectedStage,Is.EqualTo(CrossoverCampaign.FirstStage+2));
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name=="임무 선택"),Is.False);
            Click("동료의 이야기 · 30");yield return null;Click("메인 임무로");yield return null;
            Assert.That(director.Hud.IntermissionHubOpen,Is.True);Assert.That(director.SelectedStage,Is.EqualTo(CrossoverCampaign.FirstStage+2));
            director.ReplayMainEnding(CrossoverCampaign.FirstStage+1);yield return null;
            Assert.That(director.StoryStage,Is.EqualTo(CrossoverCampaign.FirstStage+1));director.FinishStory();yield return null;
            Assert.That(director.SelectedStage,Is.EqualTo(CrossoverCampaign.FirstStage+2));Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator EmptySlotClickRequiresConfirmationAndCancelDoesNotCreateSave()
        {
            TitleSetup();director.Hud.ShowTitleSlots(true);yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name=="타이틀 새 게임 확인"),Is.False);
            Click("타이틀 슬롯 3");yield return null;
            Assert.That(director.Hud.transform.Find("TitleScreen/TitleConfirm/TitleConfirmationWindow").GetComponent<OrnatePanel>(),Is.Not.Null);
            Assert.That(System.IO.Directory.Exists(testStorage),Is.False);
            Click("타이틀 확인 닫기");yield return null;
            Assert.That(System.IO.Directory.Exists(testStorage),Is.False);
            Click("타이틀 슬롯 3");yield return null;Click("타이틀 새 게임 확정");yield return null;
            Assert.That(director.Profiles.Slot,Is.EqualTo(2));Assert.That(director.Hud.IntermissionHubOpen,Is.True);
            director.Deployment.Clear();director.Hud.ShowIntermissionHub();yield return null;
            Assert.That(FormationControl("전투 시작").interactable,Is.False);
        }
    }
}
