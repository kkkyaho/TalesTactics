using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator ExpandedEnemyPagesReachLastEnemyAndPreserveBattle()
        {
            director.TrainingMode=false;director.SelectedStage=5;
            director.Campaign.StoryProgress.AddRange(Enumerable.Range(0,5).Select(CampaignStages.Id));
            director.Hud.ShowMission();yield return null;Click("적 정보");yield return null;
            Click("다음 적");yield return null;Click("다음 적");yield return null;Click("적 선택: 9");yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(x=>x.name=="다음 적").interactable,Is.False);
            Click("이전 적");yield return null;Click("적 선택: 4");director.Hud.CloseMission();
            director.BeginBattle();if(director.StoryActive)director.FinishStory();yield return null;
            var b=director.Session;Assert.That(b.ExpandedEncounters,Is.True);Assert.That(b.Units.Count(u=>u.Team==Team.Enemy),Is.EqualTo(10));
            var before=JsonUtility.ToJson(BattleCheckpoint.Capture(b,director.Deployment.ToArray()));
            director.Hud.ShowMission();yield return null;var window=director.Hud.transform.Find("MissionOverlay/MissionWindow");
            foreach(var label in window.GetComponentsInChildren<TMPro.TMP_Text>()){label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.text);}
            Click("적 정보");yield return null;Click("다음 적");yield return null;Click("다음 적");yield return null;Click("적 선택: 9");yield return null;
            foreach(var label in window.GetComponentsInChildren<TMPro.TMP_Text>()){label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.text);}
            director.Hud.CloseMission();Assert.That(JsonUtility.ToJson(BattleCheckpoint.Capture(b,director.Deployment.ToArray())),Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator ExpandedWavesApplyWithoutOptionalCaptureAndRetryStartsFresh()
        {
            director.TrainingMode=false;director.SelectedStage=2;director.Preferences.MissionEvents=false;
            director.Campaign.StoryProgress.AddRange(new[]{"chapter1","chapter2"});director.BeginBattle();if(director.StoryActive)director.FinishStory();yield return null;director.StopAllCoroutines();
            var b=director.Session;Assert.That(b.CaptureMission,Is.False);Assert.That(b.ReinforcementMission,Is.True);Assert.That(b.Units.Count(u=>u.Team==Team.Enemy),Is.EqualTo(6));
            for(int i=0;i<100&&((TeamTurnScheduler)b.Scheduler).Round<3;i++){b.EndTurn();b.Advance();}
            director.RefreshViews();director.Hud.Refresh();yield return null;
            Assert.That(b.Units.Count(u=>u.Team==Team.Enemy),Is.EqualTo(8));Assert.That(director.SuspendBattle(),Is.True);Assert.That(director.ResumeBattle(),Is.True);yield return null;
            Assert.That(director.Session.ReinforcementsArrived,Is.True);Assert.That(director.RetryOpening(),Is.True);yield return null;
            Assert.That(director.Session.ExpandedEncounters,Is.True);Assert.That(director.Session.ReinforcementsArrived,Is.False);Assert.That(director.Session.Units.Count(u=>u.Team==Team.Enemy),Is.EqualTo(6));
        }
    }
}
