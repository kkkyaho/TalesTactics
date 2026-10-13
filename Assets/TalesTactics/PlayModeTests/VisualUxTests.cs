using System.Collections;
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
        [UnityTest] public IEnumerator VisualUxHealthBarsRemainSeparatedAcrossCameraRotations()
        {
            director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1,2,3,4,5});director.BeginBattle();yield return null;
            for(int rotation=0;rotation<4;rotation++)
            {
                director.Board.RotateCamera(90);yield return null;yield return new WaitForEndOfFrame();
                var bars=Object.FindObjectsByType<UnitHealthDisplay>().Where(b=>b.Bar.enabled).Select(b=>{
                    var r=(RectTransform)b.transform;var p=r.anchoredPosition;
                    return new Rect(p.x-r.rect.width*.5f,p.y,r.rect.width,r.rect.height);
                }).ToArray();
                Assert.That(bars.Length,Is.GreaterThan(6));
                for(int i=0;i<bars.Length;i++)for(int j=i+1;j<bars.Length;j++)Assert.That(bars[i].Overlaps(bars[j]),Is.False,"Overlapping HP bars at rotation "+rotation);
            }
        }
    }
}
