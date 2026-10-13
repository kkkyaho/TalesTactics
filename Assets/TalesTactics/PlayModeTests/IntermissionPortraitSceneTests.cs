using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator StoryKeyboardAdvancesOnceAndEscapeCancelsOnlyOverlay()
        {
            var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            try
            {
                director.TrainingMode=false;director.ReplayStory(false);yield return null;
                yield return KeyboardPress(keyboard,UnityEngine.InputSystem.Key.Space);
                Assert.That(director.StoryIndex,Is.EqualTo(1),"One Space press advances exactly one line");
                yield return KeyboardPress(keyboard,UnityEngine.InputSystem.Key.Enter);
                Assert.That(director.StoryIndex,Is.EqualTo(2));
                Click("이야기 건너뛰기");yield return null;
                yield return KeyboardPress(keyboard,UnityEngine.InputSystem.Key.Escape);
                Assert.That(director.Hud.StoryDialogOpen,Is.False);Assert.That(director.StoryActive,Is.True);Assert.That(director.StoryIndex,Is.EqualTo(2));
                director.FinishStory();
            }
            finally {UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);}
        }
        [UnityTest] public IEnumerator PortraitStoryHistoryAndSkipAreModalAndDoNotWriteProgress()
        {
            director.TrainingMode=false;director.SelectedStage=0;int writes=0;director.PersistCampaign=_=>{writes++;return true;};
            string before=JsonUtility.ToJson(director.Campaign);director.ReplayStory(false);yield return null;
            Assert.That(director.StoryActive,Is.True);Assert.That(director.ReadStoryHistory().Length,Is.EqualTo(1));
            Assert.That(FormationControl("장비 관리").IsInteractable(),Is.False);
            Assert.That(director.Hud.StoryPortrait("크레스").Id,Is.EqualTo("cless"));
            Assert.That(director.Hud.StoryPortrait("클레스").Id,Is.EqualTo("cless"));
            Assert.That(director.Hud.StoryPortrait("기록"),Is.Null);
            PointAndSubmit("다음 대사");yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.RawImage>().Any(i=>i.texture!=null&&i.texture.name=="FormationPortraits"),Is.True);
            Click("대화 자동 진행");yield return null;Assert.That(director.Hud.StoryAutomatic,Is.True);
            Click("대화 기록");yield return null;Assert.That(director.Hud.StoryDialogOpen,Is.True);Assert.That(director.Hud.StoryAutomatic,Is.False);
            Assert.That(FormationControl("다음 대사").IsInteractable(),Is.False);
            int index=director.StoryIndex;Assert.That(director.ReadStoryHistory().Length,Is.EqualTo(index+1));
            Click("대화 기록 닫기");yield return null;Assert.That(director.StoryIndex,Is.EqualTo(index));
            Click("이야기 건너뛰기");yield return null;Click("이야기 계속 읽기");yield return null;
            Assert.That(director.StoryActive,Is.True);Assert.That(director.StoryIndex,Is.EqualTo(index));
            Click("이야기 건너뛰기");yield return null;Click("이야기 건너뛰기 확인");yield return null;
            Assert.That(director.StoryActive,Is.False);Assert.That(director.Hud.IntermissionHubOpen,Is.True);Assert.That(director.Session,Is.Null);
            Assert.That(writes,Is.Zero);Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator IntermissionHubRoutesAndEmptyPartyRemainSafe()
        {
            director.Hud.ShowIntermissionHub();yield return null;Assert.That(director.Hud.IntermissionHubOpen,Is.True);
            string before=JsonUtility.ToJson(director.Campaign);
            Click("출전 편성");yield return null;Assert.That(director.Hud.IntermissionHubOpen,Is.False);
            Click("거점으로");yield return null;
            Click("장비 관리");yield return null;Assert.That(director.Hud.transform.Find("PreparationContent/EquipmentInventory"),Is.Not.Null);
            Assert.That(director.Hud.transform.Find("Commands").gameObject.activeSelf,Is.False);
            Click("전체 능력치 보기");yield return null;Assert.That(director.Hud.UnitDetailsOpen,Is.True);
            Assert.That(FormationControl("거점으로").IsInteractable(),Is.False);
            Click("능력치 비교 닫기");yield return null;Click("거점으로");yield return null;
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));
            director.Deployment.Clear();director.Hud.ShowIntermissionHub();yield return null;
            Assert.That(FormationControl("전투 시작").interactable,Is.False);
            director.RequestBattle();Assert.That(director.Session,Is.Null);
        }
        [UnityTest] public IEnumerator StoryAutoStopsOnRestartAndNarrationDoesNotRetainPortrait()
        {
            director.TrainingMode=false;director.ReplayStory(false);yield return null;
            director.Hud.ShowStoryLine(new StoryLine("민트","다친 분들은 제가 돌볼게요."),2,3);yield return null;
            Assert.That(director.Hud.transform.Find("CampaignStory/StoryControls/DialogueBox/Portrait-mint"),Is.Not.Null);
            director.Hud.ShowStoryLine(new StoryLine("기록","다음 여정이 시작된다."),3,3);yield return null;
            Assert.That(director.Hud.transform.Find("CampaignStory/StoryControls/DialogueBox/Portrait-mint"),Is.Null);
            Click("대화 자동 진행");yield return null;director.Restart();yield return null;
            Assert.That(director.Hud.StoryAutomatic,Is.False);Assert.That(director.Hud.StoryDialogOpen,Is.False);
            Assert.That(director.Hud.IntermissionHubOpen,Is.True);
        }
    }
}
