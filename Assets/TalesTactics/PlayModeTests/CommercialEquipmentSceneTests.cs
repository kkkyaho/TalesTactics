using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator EquipmentDraftsSurviveCharacterSwitchAndCancelOnlyCurrentCharacter()
        {
            director.Campaign=new CampaignSave();director.TrainingMode=false;
            var first=director.Catalog.Characters[0];var second=director.Catalog.Characters[1];
            var sword=director.Catalog.Equipment.Single(e=>e.Id==CampaignInventory.StarterSword);
            var armor=director.Catalog.Equipment.Single(e=>e.Id=="leather-armor");
            director.Campaign.Inventory.Add(new OwnedEquipment{Id=armor.Id,Count=1});
            int writes=0;director.PersistCampaign=_=>{writes++;return false;};
            string before=JsonUtility.ToJson(director.Campaign);
            director.Hud.ShowDeployment();yield return null;Click("장비 관리");yield return null;
            Click("장착 후보: "+sword.DisplayName);yield return null;
            Click(second.DisplayName);yield return null;Click("방어구: 없음");yield return null;
            Click("장착 후보: "+armor.DisplayName);yield return null;
            Click(first.DisplayName);yield return null;
            Assert.That(FormationControl("적용 · 저장").interactable,Is.True);
            Click("적용 · 저장");yield return null;
            Assert.That(writes,Is.EqualTo(1));Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));
            Assert.That(FormationControl("적용 · 저장").interactable,Is.True,"A failed save keeps the detached draft for retry.");
            Click("돌아가기 (미적용 취소)");yield return null;
            Assert.That(FormationControl("적용 · 저장").interactable,Is.False);
            Click(second.DisplayName);yield return null;
            Assert.That(FormationControl("적용 · 저장").interactable,Is.True,"Cancel is local to the inspected character.");
            Click("장비 상점");yield return null;Click("장비 관리");yield return null;
            Assert.That(FormationControl("적용 · 저장").interactable,Is.True,"Changing menus does not silently discard pending edits.");
            director.PersistCampaign=_=>{writes++;return true;};Click("적용 · 저장");yield return null;
            Assert.That(director.Campaign.Get(second.Id).Equipment[(int)EquipmentSlot.Armor],Is.EqualTo(armor.Id));
            Assert.That(FormationControl("적용 · 저장").interactable,Is.False);
            Assert.That(writes,Is.EqualTo(2));
            Assert.That(director.Campaign.Characters.Any(p=>p.Id==first.Id),Is.False,"Read-only draft browsing must not create progress records.");
        }

        [UnityTest] public IEnumerator EquipmentDraftStockConflictRemainsVisibleWithoutDuplicatingInventory()
        {
            director.Campaign=new CampaignSave();director.TrainingMode=false;director.PersistCampaign=_=>true;
            var armor=director.Catalog.Equipment.Single(e=>e.Id=="leather-armor");
            director.Campaign.Inventory.Add(new OwnedEquipment{Id=armor.Id,Count=1});
            var first=director.Catalog.Characters[0];var second=director.Catalog.Characters[1];
            director.Hud.ShowDeployment();yield return null;Click("장비 관리");yield return null;
            Click("방어구: 없음");yield return null;Click("장착 후보: "+armor.DisplayName);yield return null;
            Click(second.DisplayName);yield return null;Click("장착 후보: "+armor.DisplayName);yield return null;
            Click(first.DisplayName);yield return null;Click("적용 · 저장");yield return null;
            Click(second.DisplayName);yield return null;
            Assert.That(FormationControl("적용 · 저장").interactable,Is.False);
            var candidate=director.Hud.GetComponentsInChildren<Transform>().Single(t=>t.name=="DraftEquipment");
            Assert.That(candidate.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text==armor.DisplayName),Is.True,"A conflicting draft remains visible until explicitly changed or cancelled.");
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("수량이 부족")),Is.True);
            Assert.That(CampaignInventory.Equipped(director.Campaign,armor.Id),Is.EqualTo(1));
            Assert.That(CampaignInventory.Owned(director.Campaign,armor.Id),Is.EqualTo(1));
            Click("돌아가기 (미적용 취소)");yield return null;
            CampaignInventory.Validate(director.Campaign);
        }

        [UnityTest] public IEnumerator PurchaseComparisonUsesOnlyJoinedCompatibleAndExclusiveRecipients()
        {
            director.Catalog.CrossoverReady=true;director.Campaign=new CampaignSave();director.TrainingMode=false;
            var item=UnityEngine.Object.Instantiate(director.Catalog.Equipment.Single(e=>e.Id=="leather-armor"));
            var rosterMethod=typeof(BattleHud).GetMethod("PurchasePreviewRoster",BindingFlags.Instance|BindingFlags.NonPublic);
            var showItem=typeof(BattleHud).GetMethod("ShowShopItem",BindingFlags.Instance|BindingFlags.NonPublic);
            try
            {
                var joined=director.Catalog.Characters.Where(director.IsJoined).ToArray();
                var hidden=director.Catalog.Characters.First(c=>!director.IsJoined(c));
                var before=JsonUtility.ToJson(director.Campaign);
                var roster=(CharacterData[])rosterMethod.Invoke(director.Hud,new object[]{item});
                Assert.That(roster,Is.EquivalentTo(joined));
                item.ExclusiveCharacterId=hidden.Id;
                showItem.Invoke(director.Hud,new object[]{item,0,null});yield return null;
                Assert.That(((CharacterData[])rosterMethod.Invoke(director.Hud,new object[]{item})),Is.Empty);
                Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text=="장착 가능한 캐릭터가 없습니다"),Is.True);
                Assert.That(FormationControl("구매 · 바로 장착").interactable,Is.False);
                item.ExclusiveCharacterId=joined[0].Id;
                Assert.That((CharacterData[])rosterMethod.Invoke(director.Hud,new object[]{item}),Is.EqualTo(new[]{joined[0]}));
                item.Slot=EquipmentSlot.Weapon;item.Weapon=joined.First(c=>c.Weapon!=joined[0].Weapon).Weapon;
                Assert.That((CharacterData[])rosterMethod.Invoke(director.Hud,new object[]{item}),Is.Empty,"Ownership restrictions cannot bypass weapon compatibility.");
                Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));
            }
            finally {UnityEngine.Object.Destroy(item);}
        }

        [UnityTest] public IEnumerator BuyAndEquipUsesOneAtomicWriteAndPreservesOtherDraftSlots()
        {
            director.Campaign=new CampaignSave{Gold=1000};director.TrainingMode=false;
            var c=director.Catalog.Characters[0];var sword=director.Catalog.Equipment.Single(e=>e.Id==CampaignInventory.StarterSword);
            var armor=director.Catalog.Equipment.Single(e=>e.Id=="leather-armor");
            director.Hud.ShowDeployment();yield return null;Click("장비 관리");yield return null;
            Click("장착 후보: "+sword.DisplayName);yield return null;Click("장비 상점");yield return null;
            Click(armor.DisplayName+" · "+armor.BuyPrice+"G");yield return null;
            string before=JsonUtility.ToJson(director.Campaign);int writes=0;
            director.PersistCampaign=_=>{writes++;return false;};Click("구매 · 바로 장착");yield return null;
            Assert.That(writes,Is.EqualTo(1));Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));
            director.PersistCampaign=_=>{writes++;return true;};Click("구매 · 바로 장착");yield return null;
            Assert.That(writes,Is.EqualTo(2),"One combined purchase and equip writes only once.");
            Assert.That(director.Campaign.Gold,Is.EqualTo(1000-armor.BuyPrice));
            Assert.That(CampaignInventory.Owned(director.Campaign,armor.Id),Is.EqualTo(1));
            Assert.That(director.Campaign.Get(c.Id).Equipment[(int)EquipmentSlot.Armor],Is.EqualTo(armor.Id));
            Assert.That(director.Campaign.Get(c.Id).Equipment[(int)EquipmentSlot.Weapon],Is.Null.Or.Empty,"An unrelated pending weapon is not implicitly committed.");
            Assert.That(FormationControl("적용 · 저장").interactable,Is.True,"Other slots retain their detached draft.");
            Click("적용 · 저장");yield return null;
            Assert.That(director.Campaign.Get(c.Id).Equipment[(int)EquipmentSlot.Weapon],Is.EqualTo(sword.Id));
            Assert.That(writes,Is.EqualTo(3));CampaignInventory.Validate(director.Campaign);
        }

        [UnityTest] public IEnumerator BuyAndEquipExceptionRollsBackAllStateAndPreservesDraft()
        {
            director.Campaign=new CampaignSave{Gold=1000};director.TrainingMode=false;
            var c=director.Catalog.Characters[0];var armor=director.Catalog.Equipment.Single(e=>e.Id=="leather-armor");
            var method=typeof(BattleHud).GetMethod("TryPurchaseAndEquip",BindingFlags.Instance|BindingFlags.NonPublic);
            var draft=(EquipmentLoadout)typeof(BattleHud).GetMethod("EquipmentDraft",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(director.Hud,new object[]{c,null});
            draft.Equip(EquipmentSlot.Weapon,director.Catalog.Equipment.Single(e=>e.Id==CampaignInventory.StarterSword));
            var draftBefore=draft.ExportIDs();
            string before=JsonUtility.ToJson(director.Campaign);
            director.PersistCampaign=_=>throw new InvalidOperationException("simulated storage failure");
            var failure=Assert.Throws<TargetInvocationException>(()=>method.Invoke(director.Hud,new object[]{c,armor}));
            Assert.That(failure.InnerException,Is.TypeOf<InvalidOperationException>());
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));CampaignInventory.Validate(director.Campaign);
            Assert.That(draft.ExportIDs(),Is.EqualTo(draftBefore));
            yield return null;
        }

        [UnityTest] public IEnumerator PurchaseComparisonKeepsThreeAndFourStatSwapsReadableWithoutSaving()
        {
            director.Campaign=new CampaignSave{Gold=1000};director.TrainingMode=false;
            var c=director.Catalog.Characters.Single(x=>x.Id=="cless");
            var showItem=typeof(BattleHud).GetMethod("ShowShopItem",BindingFlags.Instance|BindingFlags.NonPublic);
            string[] wornIds={"swift-boots","eternal-sword"},candidateIds={"guardian-medal","bronze-sword"};
            string[][] expectedNames={new[]{"HP ","방어 ","마방 ","이동 "},new[]{"HP ","공격 ","마력 "}};
            int writes=0;director.PersistCampaign=_=>{writes++;return true;};float originalScale=director.Preferences.TextScale;
            try
            {
                for(int example=0;example<wornIds.Length;example++)
                {
                    var worn=director.Catalog.Equipment.Single(e=>e.Id==wornIds[example]);
                    var candidate=director.Catalog.Equipment.Single(e=>e.Id==candidateIds[example]);
                    director.Campaign.Inventory.Add(new OwnedEquipment{Id=worn.Id,Count=1});
                    director.Campaign.Get(c.Id).Equipment[(int)worn.Slot]=worn.Id;
                    string before=JsonUtility.ToJson(director.Campaign);
                    foreach(float scale in new[]{1f,1.3f})
                    {
                        director.Preferences.TextScale=scale;showItem.Invoke(director.Hud,new object[]{candidate,0,null});
                        yield return null;yield return new WaitForEndOfFrame();
                        var panel=director.Hud.GetComponentsInChildren<Transform>().Single(t=>t.name=="PurchaseComparison");
                        var labels=panel.GetComponentsInChildren<TMPro.TMP_Text>().Where(t=>t.text.Contains(" → ")).ToArray();
                        Assert.That(labels.Length,Is.EqualTo(expectedNames[example].Length+1),"Every changed stat plus the current/candidate item row remains visible.");
                        foreach(string name in expectedNames[example])Assert.That(labels.Any(t=>t.text.StartsWith(name)),Is.True);
                        foreach(var label in labels){label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.text+" at scale "+scale);}
                        Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));Assert.That(writes,Is.Zero);
                    }
                }
            }
            finally {director.Preferences.TextScale=originalScale;}
        }
    }
}
