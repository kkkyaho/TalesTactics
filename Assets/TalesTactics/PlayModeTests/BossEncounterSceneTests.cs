using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator CampaignBossWardAppearsInMissionAndSurvivesCheckpoint()
        {
            director.TrainingMode=false;director.SelectedStage=1;director.Campaign.StoryProgress.Add("chapter1");
            Click("전투 시작");yield return null;
            var b=director.Session;Assert.That(b.BossEncounters,Is.True);Assert.That(b.Resolver.BossWardPercent(b.ObjectiveUnit),Is.EqualTo(75));
            director.Hud.ShowMission();yield return null;
            Assert.That(director.Hud.MissionOpen,Is.True);
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text=="75%"),Is.True);
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("호위 1명당")),Is.False);
            Click("임무 상세 보기");yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("호위 1명당")),Is.True);
            b.ObjectiveUnit.IntentPhase=1;b.ObjectiveUnit.IntentSkill=b.ObjectiveUnit.Data.BasicAttack.Id;b.ObjectiveUnit.IntentAim=b.Active.Position;
            director.Hud.ShowMission();yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("예고:")&&t.text.Contains(b.Active.Position.ToString())),Is.True);
            var restored=BattleCheckpoint.Capture(b,director.Deployment.ToArray()).Restore(director.Catalog);
            Assert.That(restored.Resolver.BossWardPercent(restored.ObjectiveUnit),Is.EqualTo(75));
            director.Hud.CloseMission();yield return null;Assert.That(director.Hud.MissionOpen,Is.False);
        }
    }
}
