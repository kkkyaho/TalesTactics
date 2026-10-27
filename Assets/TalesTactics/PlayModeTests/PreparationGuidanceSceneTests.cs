using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        // Explicit preparation fixture: no simulated victory, reward or automatic balance claim.
        // The shared fixture redirects every write to its unique temporary profile directory.
        void PreparationGuidanceEpisode(int episode)
        {
            TitleSetup();Assert.That(director.StartTitleGame(0),Is.True);
            for(int number=1;number<episode;number++)director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(number));
            director.SelectedStage=CrossoverCampaign.FirstStage+episode-1;
            director.Campaign.SelectedStage=director.SelectedStage;
        }
        string PreparationButtonText(string name)=>FormationControl(name).GetComponentInChildren<TMPro.TMP_Text>().text;
        string[] PreparationArrivalIds()=>director.Hud.GetComponentsInChildren<TMPro.TMP_Text>()
            .Where(t=>t.name.StartsWith("CurrentEpisodeArrival-")).Select(t=>t.name.Substring("CurrentEpisodeArrival-".Length)).ToArray();

        [UnityTest] public IEnumerator ArrivalGuidanceShowsOnlyCurrentJoinedCompanionsAndBrowsingDoesNotSave()
        {
            PreparationGuidanceEpisode(2);int writes=0;director.PersistCampaign=_=>{writes++;return true;};
            director.Hud.ShowDeployment();yield return null;
            Click("병과 필터");yield return null;Click("병과 필터");yield return null;
            Assert.That(director.Hud.GetComponentInChildren<FormationRosterScroll>().Content.childCount,Is.EqualTo(1));
            director.Hud.ShowIntermissionHub();yield return null;
            string before=JsonUtility.ToJson(director.Campaign);var party=director.Deployment.ToArray();
            string arrivals=PreparationButtonText("합류 동료 편성 보기");
            Assert.That(arrivals,Does.Contain("알펜").And.Contain("시온"));Assert.That(arrivals,Does.Not.Contain("파라"));
            Click("합류 동료 편성 보기");yield return null;
            Assert.That(director.Hud.GetComponentInChildren<FormationRosterScroll>().Content.childCount,Is.EqualTo(4),"Arrival link clears an old class filter.");
            Assert.That(PreparationArrivalIds(),Is.EquivalentTo(new[]{"alphen","shionne"}));
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));Assert.That(director.Deployment,Is.EqualTo(party));Assert.That(writes,Is.Zero);

            director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(2));director.Hud.ShowIntermissionHub();yield return null;
            Assert.That(PreparationButtonText("합류 동료 편성 보기"),Does.Contain("파라").And.Not.Contain("알펜").And.Not.Contain("시온"));
            Click("합류 동료 편성 보기");yield return null;
            Assert.That(PreparationArrivalIds(),Is.EquivalentTo(new[]{"farah"}),"Earlier companions must not remain marked as current arrivals.");
            Assert.That(director.Hud.GetComponentInChildren<FormationRosterScroll>().Content.childCount,Is.EqualTo(5));
            Assert.That(director.Deployment,Is.EqualTo(party));Assert.That(writes,Is.Zero);
        }

        [UnityTest] public IEnumerator FreeTrainingShortcutPrioritizesDeployedCompanionAndRequiresSuccessfulExplicitSave()
        {
            PreparationGuidanceEpisode(5);
            var cless=director.Campaign.Get("cless");cless.Level=7;cless.EXP=19;
            var mint=director.Campaign.Get("mint");mint.EXP=31;
            var persist=director.PersistCampaign;int gold=director.Campaign.Gold;
            var party=director.Deployment.ToArray();string diskBefore=System.IO.File.ReadAllText(director.Profiles.PathFor(0));
            director.Hud.ShowIntermissionHub();yield return null;
            Assert.That(PreparationButtonText("성장 · 승급"),Does.Contain("훈련"));
            string before=JsonUtility.ToJson(director.Campaign);
            Click("성장 · 승급");yield return null;
            Assert.That(director.Hud.transform.Find("PreparationContent/GrowthCharacter").GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("민트")),Is.True);
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("Lv1 → Lv7")&&t.text.Contains("골드 소모 없음")),Is.True);
            Assert.That(PreparationButtonText("합류 훈련 · Lv7"),Does.Contain("무료 훈련"));
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));
            Assert.That(System.IO.File.ReadAllText(director.Profiles.PathFor(0)),Is.EqualTo(diskBefore));
            int failedWrites=0;director.PersistCampaign=_=>{failedWrites++;return false;};
            Click("합류 훈련 · Lv7");yield return null;
            Assert.That(failedWrites,Is.EqualTo(1));Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));
            Assert.That(System.IO.File.ReadAllText(director.Profiles.PathFor(0)),Is.EqualTo(diskBefore));
            director.PersistCampaign=persist;Click("합류 훈련 · Lv7");yield return null;
            Assert.That(mint.Level,Is.EqualTo(7));Assert.That(mint.EXP,Is.Zero);
            Assert.That(director.Campaign.Gold,Is.EqualTo(gold));Assert.That(cless.Level,Is.EqualTo(7));Assert.That(cless.EXP,Is.EqualTo(19));
            Assert.That(director.Deployment,Is.EqualTo(party));Assert.That(FormationControl("합류 훈련 · Lv7").interactable,Is.False);
            var saved=new CampaignFile(director.Profiles.PathFor(0)).Load();
            Assert.That(saved.Get("mint").Level,Is.EqualTo(7));Assert.That(saved.Gold,Is.EqualTo(gold));
            Assert.That(saved.Characters.Any(c=>c.Id=="velvet"),Is.False,"Browsing must not create records for other joined companions.");
        }

        [UnityTest] public IEnumerator ArrivalAndTrainingGuidancePreserveFullSixMemberPartyAndTrainingModeIsReadOnly()
        {
            PreparationGuidanceEpisode(5);
            director.Deployment.Clear();director.Deployment.AddRange(new[]{"cless","mint","alphen","shionne","farah","jade"}.Select(id=>System.Array.FindIndex(director.Catalog.Characters,c=>c.Id==id)));
            var party=director.Deployment.ToArray();int writes=0;director.PersistCampaign=_=>{writes++;return true;};
            director.Hud.ShowIntermissionHub();yield return null;
            string before=JsonUtility.ToJson(director.Campaign);Click("합류 동료 편성 보기");yield return null;
            Assert.That(director.Deployment,Is.EqualTo(party));Assert.That(director.Deployment.Count,Is.EqualTo(6));
            Assert.That(FormationControl("편성 변경: velvet").interactable,Is.False);
            Assert.That(PreparationArrivalIds(),Is.EquivalentTo(new[]{"velvet","kisara"}));
            var velvet=director.Catalog.Characters.Single(c=>c.Id=="velvet");Click("○ "+velvet.DisplayName);yield return null;
            Assert.That(PreparationButtonText("선택 캐릭터 성장"),Is.EqualTo("무료 훈련"));
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));Assert.That(writes,Is.Zero);
            director.TrainingMode=true;director.Hud.ShowDeployment();yield return null;
            Assert.That(PreparationArrivalIds(),Is.Empty);Assert.That(PreparationButtonText("선택 캐릭터 성장"),Is.EqualTo("성장"));
            Click("선택 캐릭터 성장");yield return null;
            Assert.That(FormationControl("합류 훈련 · Lv7").interactable,Is.False);
            Assert.That(director.Deployment,Is.EqualTo(party));Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));Assert.That(writes,Is.Zero);
        }
    }
}
