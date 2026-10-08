using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator LargeTextControlsFitAndPersist()
        {
            var draft=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(director.Preferences));draft.TextScale=1.3f;
            Assert.That(director.SavePreferences(draft,false),Is.True);director.Hud.ShowSystemMenu(3);yield return null;yield return null;
            Assert.That(director.Preferences.TextScale,Is.EqualTo(1.3f));var window=director.Hud.transform.Find("SystemOverlay/SystemWindow");
            foreach(var t in window.GetComponentsInChildren<TMPro.TMP_Text>()){t.ForceMeshUpdate();Assert.That(t.isTextOverflowing,Is.False,t.text);}
            director.ConfigureStorage(testStorage);Assert.That(director.Preferences.TextScale,Is.EqualTo(1.3f));
        }
        [UnityTest] public IEnumerator NewCampaignRecommendsSixRolesAndSavesDevelopment()
        {
            director.ConfigureStorage(testStorage);director.TrainingMode=false;director.Hud.ShowDeployment();yield return null;
            Assert.That(director.Deployment.Count,Is.EqualTo(6));Assert.That(director.Deployment.Select(i=>director.Catalog.Characters[i].Id),Does.Contain("mint"));
            Click("균형 6인 추천 편성");director.Campaign.StoryProgress.AddRange(new[]{"chapter1","chapter2"});director.SelectedStage=2;
            Click("선택 캐릭터 성장");yield return null;Click("합류 훈련 · Lv3");yield return null;
            var c=director.Catalog.Characters[0];Assert.That(director.Campaign.Get(c.Id).Level,Is.EqualTo(3));
            Click("특성 선택: 기동");yield return null;Click("특성 적용 · 저장");yield return null;
            Assert.That(new CampaignFile(director.Profiles.PathFor(0)).Load().Get(c.Id).Trait,Is.EqualTo(TacticalTrait.Swift));
        }
        [UnityTest] public IEnumerator ForecastDetailsListsAllRecipientsAndKeepsStateUntouched()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();var b=director.Session;var u=b.Active;
            var s=u.Data.Skills.First(x=>x.Target==TargetType.Enemy&&x.Gate==SkillGate.None&&!x.IsLionHowl&&x.MinRange<=1&&x.Range>=1);
            var foe=b.Units.First(x=>x.Team==Team.Enemy);b.Grid.Place(foe,u.Position+Vector2Int.right);director.SetState(new CommandState(director));director.RefreshViews();
            int mp=u.CurrentMP,hp=foe.CurrentHP;director.SelectSkill(s);director.SelectTarget(foe.Position);yield return null;
            Assert.That(director.Forecast.Rows.Any(r=>r.Unit==foe),Is.True);Click("전체 예측 · "+director.Forecast.Rows.Count+"명 / 비용");yield return null;
            Assert.That(director.Hud.UnitDetailsOpen,Is.True);Assert.That(u.CurrentMP,Is.EqualTo(mp));Assert.That(foe.CurrentHP,Is.EqualTo(hp));
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("MP "+mp+" →")),Is.True);
            director.Hud.CloseUnitDetails();director.Confirm();while(director.State is ActionExecutionState)yield return null;
            Assert.That(u.CurrentMP,Is.EqualTo(mp-b.Resolver.MPCost(u,s)));
        }
        [UnityTest] public IEnumerator KeyboardTileMovementAndEnemyThreatOverlayWork()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();var b=director.Session;var u=b.Active;
            var foe=b.Units.First(x=>x.Team==Team.Enemy);director.InspectUnit(foe);director.CycleThreat();yield return null;
            Assert.That(Object.FindObjectsByType<LineRenderer>().Any(x=>x.enabled&&x.name.StartsWith("Threat ")),Is.True);
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                var steps=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};var step=steps.First(d=>b.Grid.Path(u,u.Position+d).Count==2);var destination=u.Position+step;
                yield return KeyboardPress(keyboard,Key.M);Assert.That(director.State,Is.TypeOf<MoveSelectionState>());
                yield return KeyboardPress(keyboard,step==Vector2Int.up?Key.UpArrow:step==Vector2Int.right?Key.RightArrow:step==Vector2Int.down?Key.DownArrow:Key.LeftArrow);
                Assert.That(director.KeyboardTile,Is.EqualTo(destination));yield return KeyboardPress(keyboard,Key.Enter);
                while(director.State is ActionExecutionState)yield return null;Assert.That(u.Position,Is.EqualTo(destination));
                yield return KeyboardPress(keyboard,Key.Z);Assert.That(u.Moved,Is.False);
            }
            finally{InputSystem.RemoveDevice(keyboard);}
        }
        [UnityTest] public IEnumerator EveryCampaignObjectiveRewardsAndUnlocksNextChapter()
        {
            director.TrainingMode=false;director.Campaign=new CampaignSave();director.RewardRoll=()=>9999;
            for(int stage=0;stage<CampaignStages.Count;stage++)
            {
                director.SelectedStage=stage;director.BeginBattle();yield return null;director.StopAllCoroutines();var b=director.Session;
                Assert.That(b.Objective,Is.EqualTo(CampaignMissions.Kind(stage)));
                if(b.Objective==ObjectiveKind.Reach||b.Objective==ObjectiveKind.Escort)b.Grid.Place(b.ObjectiveUnit??b.Units[0],b.Destination);
                else if(b.Victory is SurviveTurns survival)for(int i=0;i<survival.Required;i++){if(survival.EnemyPhases)survival.OnEnemyPhaseEnded();else survival.OnTurnEnded(b.Units[0]);}
                else if(b.Objective==ObjectiveKind.Boss)b.ObjectiveUnit.Damage(99999,b.Grid);
                else foreach(var enemy in b.Units.Where(u=>u.Team==Team.Enemy))enemy.Damage(99999,b.Grid);
                director.SetState(new BattleEndState(director));yield return null;Assert.That(director.RewardPending,Is.False);
                Assert.That(director.Campaign.StoryProgress,Does.Contain(CampaignStages.Id(stage)));director.Restart();yield return null;
            }
            Assert.That(new CampaignFile(director.Profiles.PathFor(0)).Load().StoryProgress.Count,Is.EqualTo(6));
        }
    }
}
