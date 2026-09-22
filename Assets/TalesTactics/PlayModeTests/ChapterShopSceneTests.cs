using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator ChapterShopShowsLockedDetailsThenUnlocksAfterReward()
        {
            director.Campaign = new CampaignSave(); director.TrainingMode = false;
            director.PersistCampaign = _ => true;
            var item = director.Catalog.Equipment.Single(e => e.Id == "iron-sword");
            string label = item.DisplayName + " · " + item.BuyPrice + "G";
            Click("장비 상점"); yield return null; Click(label + " · 잠김"); yield return null;
            Assert.That(Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b => b.name == "구매 · 저장").interactable, Is.False);
            Assert.That(Object.FindObjectsByType<TMPro.TMP_Text>().Any(t => t.text.Contains("해금 조건: 1장 완료")), Is.True);
            Assert.That(CampaignStages.TryReward(director.Campaign, 0, new[] { director.Catalog.Characters[0] }, director.PersistCampaign), Is.True);
            Click("상품 목록으로"); yield return null; Click(label); yield return null;
            Click("구매 · 저장"); yield return null;
            Assert.That(director.Campaign.Gold, Is.EqualTo(180));
            Assert.That(CampaignInventory.Owned(director.Campaign, item.Id), Is.EqualTo(1));
            var character = director.Catalog.Characters.Single(c => c.Id == "cless");
            var draft = new EquipmentLoadout(character, director.Campaign.Get(character.Id), director.Catalog.Equipment, director.Campaign);
            Assert.That(draft.Equip(EquipmentSlot.Weapon, item), Is.True);
            Assert.That(draft.TrySave(director.Campaign, director.PersistCampaign), Is.True);
            Click("출전 준비로"); yield return null; Click("전투 시작"); yield return null;
            var unit = director.Session.Units.Single(u => u.Team == Team.Player && u.Data.Id == character.Id);
            Assert.That(unit.Stats.STR, Is.EqualTo(character.StatsAt(unit.Level).STR + item.Bonus.STR));
        }
    }
}
