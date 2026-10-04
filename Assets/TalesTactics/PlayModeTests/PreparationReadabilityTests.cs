using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator FormationInspectionDoesNotToggleAndPurchaseCanEquipDirectly()
        {
            director.Campaign=new CampaignSave();director.TrainingMode=false;
            int saves=0;director.PersistCampaign=_=>{saves++;return true;};
            director.Hud.ShowDeployment();yield return null;
            var c=director.Catalog.Characters[0];var party=director.Deployment.ToArray();
            Click("● "+c.DisplayName);yield return null;
            Assert.That(director.Deployment,Is.EquivalentTo(party));
            Click("편성에서 제외");yield return null;Assert.That(director.Deployment.Contains(0),Is.False);
            Click("출전 편성에 추가");yield return null;Assert.That(director.Deployment,Is.EquivalentTo(party));
            Click("장비 상점");yield return null;Click("방어구");yield return null;
            var armor=director.Catalog.Equipment.Single(e=>e.Id=="leather-armor");
            Click(armor.DisplayName+" · "+armor.BuyPrice+"G");yield return null;
            Click("구매 · 저장");yield return null;
            Assert.That(Object.FindObjectsByType<UnityEngine.UI.Button>().Any(b=>b.name=="● 방어구"),Is.True);
            Assert.That(CampaignInventory.Owned(director.Campaign,armor.Id),Is.EqualTo(1));
            Click("구매 장비 장착");yield return null;Click(c.DisplayName);yield return null;
            Assert.That(director.Campaign.Get(c.Id).Equipment[(int)EquipmentSlot.Armor],Is.Null.Or.Empty);
            Click("적용 · 저장");yield return null;
            Assert.That(director.Campaign.Get(c.Id).Equipment[(int)EquipmentSlot.Armor],Is.EqualTo(armor.Id));
            Assert.That(saves,Is.EqualTo(2));
        }
        [UnityTest] public IEnumerator FocusOverviewAndDestinationSurviveAllChapterRestarts()
        {
            director.Campaign=new CampaignSave();director.PersistCampaign=_=>true;director.TrainingMode=false;
            for(int i=0;i<6;i++)director.Campaign.StoryProgress.Add(CampaignStages.Id(i));
            for(int stage=0;stage<6;stage++)
            {
                director.SelectedStage=stage;director.BeginBattle();yield return null;
                var camera=director.Board.BattleCamera;float overview=camera.orthographicSize;
                director.Board.FocusCurrent();Assert.That(camera.orthographicSize,Is.LessThanOrEqualTo(overview));
                var active=director.Session.Active;
                var p=camera.WorldToScreenPoint(director.Session.Grid[active.Position].WorldPosition(director.Catalog.Rules.TileHeight));
                Assert.That(camera.pixelRect.Contains(new Vector2(p.x,p.y)),Is.True);
                director.Board.ResetCamera();
                for(int turn=0;turn<4;turn++)
                {
                    director.Board.RotateCamera(90);
                    foreach(var tile in director.Session.Grid.Tiles.Values)
                    {p=camera.WorldToScreenPoint(tile.WorldPosition(director.Catalog.Rules.TileHeight));Assert.That(camera.pixelRect.Contains(new Vector2(p.x,p.y)),Is.True,$"Chapter {stage+1}: {tile.Coordinate}");}
                }
                var destination=director.Session.Grid.Reachable(active,out _).Keys.First(t=>t!=active.Position);
                director.Board.ShowPath(director.Session.Grid.Path(active,destination));yield return null;
                var marker=director.Board.GetComponentsInChildren<LineRenderer>().Single(x=>x.name=="Movement destination");
                Assert.That(marker.enabled,Is.True);director.Board.ClearHighlights();Assert.That(marker.enabled,Is.False);
                director.Restart();yield return null;
            }
        }
        [UnityTest] public IEnumerator TargetCyclingWrapsWithoutSpendingResourcesAndRespectsDetails()
        {
            director.BeginBattle();yield return null;
            var u=director.Session.Active;
            var skill=Object.Instantiate(u.Data.BasicAttack);
            try
            {
                skill.Range=99;director.SelectSkill(skill);yield return null;
                var targets=director.AvailableTargets();Assert.That(targets.Length,Is.GreaterThan(1));
                int hp=u.CurrentHP,mp=u.CurrentMP;
                director.CycleTarget(1);Assert.That(director.Target,Is.EqualTo(targets[0]));
                director.CycleTarget(-1);Assert.That(director.Target,Is.EqualTo(targets[targets.Length-1]));
                director.Hud.ShowUnitDetails(u);director.CycleTarget(1);
                Assert.That(director.Target,Is.EqualTo(targets[targets.Length-1]));director.Hud.CloseUnitDetails();
                director.CycleTarget(1);Assert.That(director.Target,Is.EqualTo(targets[0]));
                Assert.That(u.CurrentHP,Is.EqualTo(hp));Assert.That(u.CurrentMP,Is.EqualTo(mp));Assert.That(u.Acted,Is.False);
                director.State.Cancel();director.CycleTarget(1);Assert.That(director.Target,Is.Null);
            }
            finally{Object.Destroy(skill);}
        }
    }
}
