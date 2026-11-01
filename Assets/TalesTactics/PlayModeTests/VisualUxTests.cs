using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator VisualUxPurchaseComparisonAndMissionPreviewNeverMutateCampaign()
        {
            director.Campaign=new CampaignSave{Gold=2000};director.TrainingMode=false;
            director.Campaign.StoryProgress.AddRange(new[]{"chapter1","chapter2"});director.SelectedStage=2;
            int writes=0;director.PersistCampaign=_=>{writes++;return true;};
            var before=JsonUtility.ToJson(director.Campaign);director.Hud.ShowDeployment();yield return null;
            director.Hud.ShowMission();yield return null;
            var map=director.Hud.GetComponentInChildren<MissionMapPreview>();Assert.That(map,Is.Not.Null);
            Assert.That(map.GetComponent<CanvasRenderer>(),Is.Not.Null);Canvas.ForceUpdateCanvases();
            Assert.That(map.canvasRenderer.GetMesh().vertexCount,Is.GreaterThan(0));
            director.Hud.CloseMission();Click("장비 상점");yield return null;
            Click("철검 · 240G");yield return null;
            var comparison=director.Hud.GetComponentsInChildren<Transform>().Single(t=>t.name=="PurchaseComparison");
            Assert.That(comparison.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("공격 ")&&t.text.Contains(" → ")),Is.True);
            if(FormationControl("구매 비교 캐릭터").interactable){Click("구매 비교 캐릭터");yield return null;}
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));Assert.That(writes,Is.Zero);
            Click("구매 · 저장");yield return null;
            Assert.That(director.Campaign.Gold,Is.EqualTo(1760));Assert.That(writes,Is.EqualTo(1));
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("구매 완료")),Is.True);
        }
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator VisualUxHealthBarsRemainSeparatedAcrossCameraRotations()
        {
            director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1,2,3,4,5});director.BeginBattle();yield return null;
            director.StopAllCoroutines();director.enabled=false;
            Assert.That(director.Session.Units.Count(u=>u.Team==Team.Player),Is.EqualTo(6));
            // Healthy idle gauges are intentionally absent. Wound all deployed actors to exercise the dense layout.
            foreach(var unit in director.Session.Units.Where(u=>u.Alive&&!u.CrossoverReserve))unit.CurrentHP=Mathf.Max(1,unit.Stats.HP-1);
            for(int rotation=0;rotation<4;rotation++)
            {
                director.Board.RotateCamera(90);yield return null;yield return new WaitForEndOfFrame();
                var displays=Object.FindObjectsByType<UnitHealthDisplay>();
                var visible=displays.Where(b=>b.Bar.enabled).ToArray();
                Assert.That(visible.Length,Is.GreaterThan(0),"A wounded army must retain visible nearby health cues.");
                var bars=visible.Select(b=>b.ScreenBounds).ToArray();
                for(int i=0;i<bars.Length;i++)for(int j=i+1;j<bars.Length;j++)Assert.That(bars[i].Overlaps(bars[j]),Is.False,"Overlapping HP bars at rotation "+rotation);
                var safe=CombatLabelPlacement.Viewport(director.Hud);float scale=Mathf.Clamp(Screen.height/1080f,.8f,1.5f);
                foreach(var display in displays.Where(d=>d.Unit.Alive&&!d.Unit.CrossoverReserve))
                {
                    if(display.Bar.enabled)
                    {
                        var shift=new Vector2(display.ScreenBounds.center.x,display.ScreenBounds.yMin)-display.ScreenOrigin;
                        Assert.That(Mathf.Abs(shift.x),Is.LessThanOrEqualTo(UnitHealthLayout.MaximumHorizontalShift*scale+.1f));
                        Assert.That(Mathf.Abs(shift.y),Is.LessThanOrEqualTo(UnitHealthLayout.MaximumVerticalShift*scale+.1f));
                    }
                    else if(safe.Contains(display.ScreenOrigin))
                    {
                        var obstacles=new List<Rect>();director.Hud.AppendCombatLabelReservations(obstacles);obstacles.AddRange(bars);
                        Assert.That(UnitHealthLayout.TryPlaceNearHead(display.ScreenOrigin,display.OccupiedSize,safe,obstacles,scale,out _),Is.False,
                            "An on-screen wounded gauge may disappear only when its local head region is obstructed.");
                    }
                }
            }
        }
    }
}
