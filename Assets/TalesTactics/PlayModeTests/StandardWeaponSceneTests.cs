using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator ExpandedShopPagesAndFilterChangesListEveryProductExactlyOnceWithoutSaving()
        {
            PreparationGuidanceEpisode(5);int writes=0;director.PersistCampaign=_=>{writes++;return true;};
            director.Hud.ShowDeployment();yield return null;Click("장비 상점");yield return null;
            if(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name=="전체")){Click("전체");yield return null;}
            string Label(EquipmentData item)=>item.DisplayName+" · "+item.BuyPrice+"G"+(CampaignInventory.PurchaseUnlockRequirement(director.Campaign,item)!=null?" · 잠김":"");
            var products=director.Catalog.Equipment.Where(e=>e.BuyPrice>0).ToArray();var expected=products.Select(Label).ToArray();
            string[] Visible()=>director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Select(b=>b.name).Where(expected.Contains).ToArray();
            string before=JsonUtility.ToJson(director.Campaign);var seen=new System.Collections.Generic.List<string>();
            int pages=(products.Length+5)/6;
            for(int page=0;page<pages;page++)
            {
                var visible=Visible();Assert.That(visible.Length,Is.InRange(1,6));seen.AddRange(visible);
                Assert.That(FormationControl("이전 상품").interactable,Is.EqualTo(page>0));
                Assert.That(FormationControl("다음 상품").interactable,Is.EqualTo(page+1<pages));
                if(page+1<pages){Click("다음 상품");yield return null;}
            }
            Assert.That(seen,Is.EquivalentTo(expected));Assert.That(seen.Distinct().Count(),Is.EqualTo(seen.Count));
            Click("방어구");yield return null;
            Assert.That(Visible(),Is.EquivalentTo(products.Where(e=>e.Slot==EquipmentSlot.Armor).Select(Label)));
            Assert.That(FormationControl("이전 상품").interactable,Is.False);
            Click("무기");yield return null;
            var weapons=products.Where(e=>e.Slot==EquipmentSlot.Weapon).Select(Label).ToArray();
            Assert.That(Visible(),Is.EquivalentTo(weapons.Take(6)));Assert.That(FormationControl("이전 상품").interactable,Is.False);
            Assert.That(FormationControl("다음 상품").interactable,Is.True);Click("다음 상품");yield return null;
            Assert.That(Visible(),Is.EquivalentTo(weapons.Skip(6)));
            Click("전체");yield return null;Assert.That(Visible(),Is.EquivalentTo(expected.Take(6)));
            Assert.That(FormationControl("이전 상품").interactable,Is.False);
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));Assert.That(writes,Is.Zero);
        }
        [UnityTest] public IEnumerator SevenStandardWeaponCardsPurchaseEquipAndReloadThroughActualShop()
        {
            // Isolated preparation fixture with all companions joined; not a campaign victory simulation.
            PreparationGuidanceEpisode(5);director.Campaign.Gold=2000;int purchases=0;
            string[] ids={"training-staff","steel-claw","steel-fist","training-spear","reinforced-bow","infantry-rifle","steel-shield"};
            foreach(string id in ids)
            {
                director.Hud.ShowDeployment();yield return null;Click("장비 상점");yield return null;
                if(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name=="무기")){Click("무기");yield return null;}
                var item=director.Catalog.Equipment.Single(e=>e.Id==id);string name=item.DisplayName+" · 240G";
                for(int page=0;page<8&&!director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name==name);page++){Click("다음 상품");yield return null;}
                yield return new WaitForEndOfFrame();
                var card=FormationControl(name);
                foreach(var label in card.GetComponentsInChildren<TMPro.TMP_Text>().Where(t=>!string.IsNullOrEmpty(t.text)))
                {label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,"Shop card text overflow: "+id+" / "+label.text);}
                Assert.That(card.GetComponentsInChildren<UnityEngine.UI.Image>().Any(i=>i.sprite==item.Icon),Is.True,"Actual item illustration must be wired on the shop card: "+id);
                Click(name);yield return null;Click("구매 · 바로 장착");yield return null;purchases++;
                Assert.That(director.Campaign.Gold,Is.EqualTo(2000-240*purchases));
                var owner=director.Campaign.Characters.Single(c=>c.Equipment[(int)EquipmentSlot.Weapon]==id);
                var character=director.Catalog.Characters.Single(c=>c.Id==owner.Id);
                Assert.That(character.Weapon,Is.EqualTo(item.Weapon));
                var saved=new CampaignFile(director.Profiles.PathFor(0)).Load();
                Assert.That(CampaignInventory.Owned(saved,id),Is.EqualTo(1));Assert.That(saved.Get(owner.Id).Equipment[(int)EquipmentSlot.Weapon],Is.EqualTo(id));
                Assert.That(saved.Gold,Is.EqualTo(2000-240*purchases));
            }
            Assert.That(director.Campaign.Gold,Is.EqualTo(320));Assert.That(purchases,Is.EqualTo(7));
        }
    }
}
