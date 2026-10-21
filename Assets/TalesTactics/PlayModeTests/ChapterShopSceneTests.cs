using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator CampaignGrowthMainShopUnlocksPurchasesAndEquipsWithIndependentSave()
        {
            director.Catalog.CrossoverReady=true;director.ConfigureStorage(testStorage);director.TrainingMode=false;
            Assert.That(director.SelectedStage,Is.EqualTo(CrossoverCampaign.FirstStage));
            Assert.That(director.Campaign.StoryProgress,Is.Empty);Assert.That(director.Campaign.Gold,Is.EqualTo(300));
            director.Hud.ShowDeployment();yield return null;
            var item=director.Catalog.Equipment.Single(e=>e.Id=="iron-sword");
            var character=director.Catalog.Characters.Single(c=>c.Id=="cless");
            string label=item.DisplayName+" · "+item.BuyPrice+"G";
            Click("장비 상점");yield return null;Click(label+" · 잠김");yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="구매 · 저장").interactable,Is.False);
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="구매 · 바로 장착").interactable,Is.False);
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("해금 조건: 메인 1화 완료")),Is.True);

            // Exercise the real first-episode reward API and durable test save directly.
            // This is shop/story-flag integration coverage, not a played or forced battle victory.
            var party=director.Catalog.Characters.Where(c=>CrossoverCampaign.Joined(c.Id,1)).ToArray();
            Assert.That(party.Length,Is.EqualTo(2));
            Assert.That(CampaignStages.TryReward(director.Campaign,CrossoverCampaign.FirstStage,party,director.PersistCampaign),Is.True);
            Assert.That(director.Session,Is.Null);Assert.That(director.Campaign.Gold,Is.EqualTo(470));
            Assert.That(director.Campaign.StoryProgress,Does.Contain(CrossoverCampaign.Flag(1)).And.Not.Contain("chapter1"));
            Click("상품 목록으로");yield return null;Click(label);yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="구매 · 바로 장착").interactable,Is.True);
            Click("구매 · 바로 장착");yield return null;
            Assert.That(director.Campaign.Gold,Is.EqualTo(230));
            Assert.That(CampaignInventory.Owned(director.Campaign,item.Id),Is.EqualTo(1));
            Assert.That(director.Campaign.Get(character.Id).Equipment[(int)EquipmentSlot.Weapon],Is.EqualTo(item.Id));
            var candidate=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="장착 후보: "+item.DisplayName);
            Assert.That(candidate.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text=="현재 장착"),Is.True);

            var saved=new CampaignFile(director.Profiles.PathFor(director.Profiles.Slot)).Load();
            Assert.That(saved.Gold,Is.EqualTo(230));Assert.That(CampaignInventory.Owned(saved,item.Id),Is.EqualTo(1));
            Assert.That(saved.Get(character.Id).Equipment[(int)EquipmentSlot.Weapon],Is.EqualTo(item.Id));
            Assert.That(saved.StoryProgress,Does.Contain(CrossoverCampaign.Flag(1)).And.Not.Contain("chapter1"));
            var progress=saved.Get(character.Id);var gear=new EquipmentLoadout(character,progress,director.Catalog.Equipment,saved);
            Assert.That(gear.Preview(progress.Level,progress.Promoted).STR,Is.EqualTo(character.StatsAt(progress.Level).STR+item.Bonus.STR));
        }

        [UnityTest] public IEnumerator ChapterShopShowsLockedDetailsThenUnlocksAfterReward()
        {
            director.Campaign = new CampaignSave(); director.TrainingMode = false;
            director.PersistCampaign = _ => true;
            var item = director.Catalog.Equipment.Single(e => e.Id == "iron-sword");
            string label = item.DisplayName + " · " + item.BuyPrice + "G";
            Click("장비 상점"); yield return null; Click(label + " · 잠김"); yield return null;
            Assert.That(Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b => b.name == "구매 · 저장").interactable, Is.False);
            Assert.That(Object.FindObjectsByType<TMPro.TMP_Text>().Any(t => t.text.Contains("해금 조건: 메인 1화 완료")), Is.True);
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
