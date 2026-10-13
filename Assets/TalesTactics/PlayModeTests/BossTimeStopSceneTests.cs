using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator TimeStopCancelsVisibleBossIntentAndRestoresThroughRewind()
        {
            director.TrainingMode=false;director.SelectedStage=5;
            for(int i=0;i<5;i++)director.Campaign.StoryProgress.Add(CampaignStages.Id(i));
            int mintIndex=System.Array.FindIndex(director.Catalog.Characters,c=>c.Id=="mint");
            director.Deployment.Clear();director.Deployment.Add(mintIndex);director.Campaign.Get("mint").Level=25;
            director.BeginBattle();yield return null;
            var b=director.Session;var mint=b.Active;var boss=b.ObjectiveUnit;mint.SpecialGauge=100;
            b.Grid.Place(mint,new Vector2Int(6,5));
            boss.IntentPhase=1;boss.IntentTurn=boss.TurnsStarted;boss.IntentSkill=boss.Data.BasicAttack.Id;boss.IntentAim=mint.Position;
            b.RecordAction("타임 스톱 이전");
            Assert.That(b.Resolver.Execute(mint,mint.Data.UltimateSkill,boss.Position,out _),Is.True);
            director.RefreshViews();director.Hud.ShowUnitDetails(boss);yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("재정지 저항")),Is.True);
            Assert.That(boss.IntentPhase,Is.Zero);Assert.That(boss.Statuses.Single(s=>s.Kind==StatusKind.Stun).Turns,Is.EqualTo(1));
            var cp=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(BattleCheckpoint.Capture(b,director.Deployment.ToArray())));
            var copy=cp.Restore(director.Catalog);Assert.That(copy.ObjectiveUnit.TimeStopResistance,Is.EqualTo(2));
            var rewound=copy.RewindLast(out _);Assert.That(rewound.ObjectiveUnit.TimeStopResistance,Is.Zero);
            Assert.That(rewound.ObjectiveUnit.IntentPhase,Is.EqualTo(1));Assert.That(rewound.Active.SpecialGauge,Is.EqualTo(100));
            director.Hud.CloseUnitDetails();
        }
    }
}
