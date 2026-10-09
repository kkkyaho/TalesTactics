using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator MissionOptionsSaveCancelAndNewBattleOnly()
        {
            director.Hud.ShowSystemMenu(2);yield return null;Click("다음 전투 난이도: 표준");Click("임무 변형: 꺼짐");director.Hud.CloseSystemMenu();
            Assert.That(director.Preferences.Difficulty,Is.EqualTo(BattleDifficulty.Standard));Assert.That(director.Preferences.MissionEvents,Is.False);
            director.Hud.ShowSystemMenu(2);Click("다음 전투 난이도: 표준");Click("다음 전투 난이도: 여유");Click("임무 변형: 꺼짐");Click("전투 진행 설정 저장");director.Hud.CloseSystemMenu();
            director.TrainingMode=false;director.SelectedStage=2;director.Campaign.StoryProgress.AddRange(new[]{"chapter1","chapter2"});Click("임무 선택");yield return null;
            var description=director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Single(t=>t.name=="PreparationMissionDescription").text;
            Assert.That(description,Does.Contain("적 Lv"+DifficultyRules.EnemyLevel(CampaignStages.EnemyLevel(2),BattleDifficulty.Veteran)));Assert.That(description,Does.Contain("2회 연속 유지"));
            director.Hud.ShowMission();yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("적 증원 2명")),Is.True);Click("임무 상세 보기");yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("2회 연속 유지")),Is.True);director.Hud.CloseMission();director.BeginBattle();if(director.StoryActive)director.FinishStory();yield return null;
            Assert.That(director.Session.Difficulty,Is.EqualTo(BattleDifficulty.Veteran));Assert.That(director.Session.CaptureMission,Is.True);
            director.Hud.ShowSystemMenu(2);Click("다음 전투 난이도: 도전");Click("임무 변형: 켜짐");Click("전투 진행 설정 저장");director.Hud.CloseSystemMenu();
            Assert.That(director.Session.Difficulty,Is.EqualTo(BattleDifficulty.Veteran));Assert.That(director.RetryOpening(),Is.True);yield return null;Assert.That(director.Session.CaptureMission,Is.True);
        }
        [UnityTest] public IEnumerator ReinforcementViewsAndSavedMissionProgressRestoreWithoutDuplicates()
        {
            director.TrainingMode=false;director.SelectedStage=2;director.Campaign.StoryProgress.AddRange(new[]{"chapter1","chapter2"});director.Preferences.MissionEvents=true;director.BeginBattle();if(director.StoryActive)director.FinishStory();yield return null;director.StopAllCoroutines();
            var b=director.Session;for(int i=0;i<100&&((TeamTurnScheduler)b.Scheduler).Round<3;i++){b.EndTurn();b.Advance();}
            director.RefreshViews();director.Hud.Refresh();yield return null;Assert.That(b.ReinforcementsArrived,Is.True);
            foreach(var u in b.Units.Skip(b.Units.Count-2))Assert.That(Object.FindObjectsByType<CharacterMotion>().Any(m=>m.transform.parent.name==u.Data.DisplayName),Is.True);
            director.Hud.ShowMission();Click("적 정보");yield return null;Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Count(x=>x.name.StartsWith("적 선택: ")),Is.EqualTo(6));director.Hud.CloseMission();
            Assert.That(director.SuspendBattle(),Is.True);Assert.That(director.ResumeBattle(),Is.True);yield return null;Assert.That(director.Session.Units.Count,Is.EqualTo(b.Units.Count));Assert.That(director.Session.ReinforcementsArrived,Is.True);
        }
    }
}
