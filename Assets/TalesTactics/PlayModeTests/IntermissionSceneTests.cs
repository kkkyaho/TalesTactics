using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator IntermissionBrowsingAndTraitDraftDoNotChangeSaveAndFailureRollsBack()
        {
            director.Campaign=new CampaignSave();director.TrainingMode=false;int writes=0;director.PersistCampaign=_=>{writes++;return false;};
            string original=JsonUtility.ToJson(director.Campaign);director.Hud.ShowDeployment();yield return null;
            Click("장비 관리");yield return null;Click(director.Catalog.Characters[1].DisplayName);yield return null;
            Click("장비 상점");yield return null;Click("성장 · 승급");yield return null;Click(director.Catalog.Characters[0].DisplayName);yield return null;
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(original));
            PointAndSubmit("특성 선택: 기동");yield return null;
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(original));Assert.That(writes,Is.Zero);
            PointAndSubmit("특성 적용 · 저장");yield return null;
            Assert.That(writes,Is.EqualTo(1));Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(original));
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("저장 실패")),Is.True);
            director.PersistCampaign=_=>{writes++;return true;};Click("특성 적용 · 저장");yield return null;
            Assert.That(director.Campaign.Get(director.Catalog.Characters[0].Id).Trait,Is.EqualTo(TacticalTrait.Swift));Assert.That(writes,Is.EqualTo(2));
            Assert.That(FormationControl("특성 적용 · 저장").interactable,Is.False);
            Click("특성 선택: 치유");yield return null;Click("장비 상점");yield return null;Click("성장 · 승급");yield return null;
            Assert.That(FormationControl("특성 적용 · 저장").interactable,Is.False,"Navigation cancels an unapplied trait draft");
            director.TrainingMode=true;Click("특성 선택: 호위");yield return null;
            Assert.That(FormationControl("특성 적용 · 저장").interactable,Is.False);
        }
        [UnityTest] public IEnumerator IntermissionEquipmentDraftPreviewCancellationFailureAndRetry()
        {
            director.Campaign=new CampaignSave();director.TrainingMode=false;
            var c=director.Catalog.Characters[0];var item=director.Catalog.Equipment.Single(e=>e.Id=="iron-sword");
            director.Campaign.Inventory.Add(new OwnedEquipment{Id=item.Id,Count=1});int writes=0;director.PersistCampaign=_=>++writes>1;
            director.Hud.ShowDeployment();yield return null;PointAndSubmit("장비 관리");yield return null;
            string original=JsonUtility.ToJson(director.Campaign);PointAndSubmit("장착 후보: "+item.DisplayName);yield return null;
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(original));
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("변경 사항 1건")),Is.True);
            PointAndSubmit("돌아가기 (미적용 취소)");yield return null;Assert.That(FormationControl("무기: 없음"),Is.Not.Null);
            Click("장착 후보: "+item.DisplayName);yield return null;Click("적용 · 저장");yield return null;
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(original));
            Click("적용 · 저장");yield return null;Assert.That(director.Campaign.Get(c.Id).Equipment[0],Is.EqualTo(item.Id));
            Assert.That(writes,Is.EqualTo(2));
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.RawImage>().Any(x=>x.texture!=null&&x.texture.name=="EquipmentIcons"),Is.True);
        }
        [UnityTest] public IEnumerator IntermissionMarketPaginationAndModalPreserveSelection()
        {
            director.Campaign=new CampaignSave{Gold=2000};director.TrainingMode=false;director.PersistCampaign=_=>true;
            director.Campaign.StoryProgress.AddRange(new[]{"chapter1","chapter2","chapter3","chapter4","chapter5"});
            director.Hud.ShowDeployment();yield return null;Click("장비 상점");yield return null;Click("다음 상품");yield return null;
            var item=director.Catalog.Equipment.Where(e=>e.BuyPrice>0).Skip(6).First();
            string button=item.DisplayName+" · "+item.BuyPrice+"G";Click(button);yield return null;
            int gold=director.Campaign.Gold;Click("구매 · 저장");yield return null;
            Assert.That(director.Campaign.Gold,Is.EqualTo(gold-item.BuyPrice));Assert.That(FormationControl(button),Is.Not.Null);
            Click("성장 · 승급");yield return null;Click("기술 숙련 안내");yield return null;
            Assert.That(director.Hud.UnitDetailsOpen,Is.True);Assert.That(FormationControl("장비 상점").IsInteractable(),Is.False);
            Click("숙련 안내 닫기");yield return null;Assert.That(FormationControl("장비 상점").IsInteractable(),Is.True);
            Click("출전 준비로");yield return null;Assert.That(director.Hud.transform.Find("PreparationContent/FormationModels"),Is.Not.Null);
        }
    }
}
