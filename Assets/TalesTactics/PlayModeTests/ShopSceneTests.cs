using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator ShopPurchaseRetryEquipAndBattleUseRealIsolatedSave()
        {
            string path=Path.Combine(Application.temporaryCachePath,"TalesTacticsShopTests",Guid.NewGuid().ToString("N"),"campaign.json");
            var store=new CampaignFile(path);director.Campaign=store.Load();director.TrainingMode=false;
            int attempts=0;director.PersistCampaign=s=>++attempts>1&&store.Save(s);
            var item=director.Catalog.Equipment.Single(e=>e.Id=="leather-armor");
            Click("장비 상점");yield return null;Click(item.DisplayName+" · "+item.BuyPrice+"G");yield return null;
            Click("구매 · 저장");yield return null;
            Assert.That(director.Campaign.Gold,Is.EqualTo(300));Assert.That(CampaignInventory.Owned(director.Campaign,item.Id),Is.Zero);
            Assert.That(UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>().Any(t=>t.text.Contains("저장 실패")),Is.True);
            Click("구매 · 저장");yield return null;
            Assert.That(director.Campaign.Gold,Is.EqualTo(150));Assert.That(CampaignInventory.Owned(director.Campaign,item.Id),Is.EqualTo(1));
            Click("출전 준비로");yield return null;Click("장비 관리");yield return null;
            var character=director.Catalog.Characters[0];Click(character.DisplayName);yield return null;
            Click("방어구: 없음");yield return null;Click("장착 후보: "+item.DisplayName);yield return null;Click("적용 · 저장");yield return null;
            var saved=new CampaignFile(path).Load();Assert.That(saved.Gold,Is.EqualTo(150));
            Assert.That(saved.Get(character.Id).Equipment[(int)EquipmentSlot.Armor],Is.EqualTo(item.Id));
            Click("돌아가기 (미적용 취소)");yield return null;Click(director.Catalog.Characters[1].DisplayName);yield return null;
            Click("방어구: 없음");yield return null;
            Assert.That(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Any(b=>b.name=="장착 후보: "+item.DisplayName),Is.False,"Equipped inventory cannot be offered twice");
            Click("돌아가기 (미적용 취소)");yield return null;Click("출전 준비로");yield return null;
            director.Campaign=saved;Click("전투 시작");yield return null;
            var unit=director.Session.Units.Single(u=>u.Team==Team.Player&&u.Data.Id==character.Id);
            Assert.That(unit.Equipment[(int)EquipmentSlot.Armor],Is.SameAs(item));
            Assert.That(unit.Stats.DEF,Is.EqualTo(character.StatsAt(1).DEF+item.Bonus.DEF));
            Assert.That(unit.CurrentHP,Is.EqualTo(character.StatsAt(1).HP+item.Bonus.HP));
        }

        [UnityTest] public IEnumerator ShopBlocksTrainingAndInsufficientGoldWithoutSaving()
        {
            director.Campaign=new CampaignSave{Gold=0};director.TrainingMode=true;
            director.PersistCampaign=_=>throw new Exception("Blocked purchase must not save");
            var item=director.Catalog.Equipment.Single(e=>e.Id=="leather-armor");
            Click("장비 상점");yield return null;Click(item.DisplayName+" · "+item.BuyPrice+"G");yield return null;
            Assert.That(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name=="구매 · 저장").interactable,Is.False);
            Assert.That(UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>().Any(t=>t.text.Contains("훈련 모드에서는 구매")),Is.True);
            director.TrainingMode=false;Click("상품 목록으로");yield return null;Click(item.DisplayName+" · "+item.BuyPrice+"G");yield return null;
            Assert.That(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name=="구매 · 저장").interactable,Is.False);
            Assert.That(UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>().Any(t=>t.text.Contains("소지금이 부족")),Is.True);
            Assert.That(director.Campaign.Gold,Is.Zero);Assert.That(CampaignInventory.Owned(director.Campaign,item.Id),Is.Zero);
        }
    }
}
