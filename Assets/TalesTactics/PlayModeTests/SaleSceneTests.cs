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
        [UnityTest] public IEnumerator SaleUiProtectsEquippedCopyRollsBackAndPersistsRetry()
        {
            var path = Path.Combine(Application.temporaryCachePath, "TalesTacticsSaleTests", Guid.NewGuid().ToString("N"), "campaign.json");
            var file = new CampaignFile(path);
            director.Campaign = new CampaignSave(); director.TrainingMode = false;
            var item = director.Catalog.Equipment.Single(e => e.Id == CampaignInventory.StarterSword);
            director.Campaign.Get("cless").Equipment[0] = item.Id;
            int writes = 0; director.PersistCampaign = s => ++writes > 1 && file.Save(s);
            Click("장비 상점"); yield return null; Click("장비 매각"); yield return null;
            Click(item.DisplayName + " · 매각 " + CampaignInventory.SellPrice(item) + "G"); yield return null;
            Assert.That(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b => b.name == "1개 매각 · 저장").interactable, Is.False);
            director.Campaign.Get("cless").Equipment[0] = null;
            director.TrainingMode = true;
            Click("보유 장비 목록으로"); yield return null;
            Click(item.DisplayName + " · 매각 " + CampaignInventory.SellPrice(item) + "G"); yield return null;
            Assert.That(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b => b.name == "1개 매각 · 저장").interactable, Is.False);
            director.TrainingMode = false;
            Click("보유 장비 목록으로"); yield return null;
            Click(item.DisplayName + " · 매각 " + CampaignInventory.SellPrice(item) + "G"); yield return null;
            Click("1개 매각 · 저장"); yield return null;
            Assert.That(director.Campaign.Gold, Is.EqualTo(300));
            Assert.That(CampaignInventory.Owned(director.Campaign, item.Id), Is.EqualTo(1));
            Click("1개 매각 · 저장"); yield return null;
            var saved = file.Load(); Assert.That(saved.Gold, Is.EqualTo(350));
            Assert.That(CampaignInventory.Owned(saved, item.Id), Is.Zero); Assert.That(writes, Is.EqualTo(2));
            Assert.That(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b => b.name == "1개 매각 · 저장").interactable, Is.False);
            Click("보유 장비 목록으로"); yield return null;
            Assert.That(UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>().Any(t => t.text.Contains("매각할 장비가 없습니다")), Is.True);
        }
    }
}
