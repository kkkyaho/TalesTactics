using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        UnityEngine.UI.Button EquipmentCandidate(EquipmentData item)=>director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="장착 후보: "+item.DisplayName);
        string CandidateState(EquipmentData item)=>EquipmentCandidate(item).GetComponentsInChildren<TMPro.TMP_Text>().Single(t=>t.name=="EquipmentWearerState").text;

        [UnityTest] public IEnumerator TesterEquipmentShowsOtherWearerWithoutTakingOrDuplicatingTheirItem()
        {
            director.Campaign=new CampaignSave();director.TrainingMode=false;director.Catalog.CrossoverReady=true;
            var first=director.Catalog.Characters.Single(c=>c.Id=="cless");var other=director.Catalog.Characters.Single(c=>c.Id=="mint");
            var armor=director.Catalog.Equipment.Single(e=>e.Id=="leather-armor");
            director.Campaign.Inventory.Add(new OwnedEquipment{Id=armor.Id,Count=1});
            director.Campaign.Get(other.Id).Equipment[(int)EquipmentSlot.Armor]=armor.Id;
            string original=JsonUtility.ToJson(director.Campaign);
            director.Hud.ShowDeployment();yield return null;Click("장비 관리");yield return null;Click("방어구: 없음");yield return null;
            Assert.That(EquipmentCandidate(armor).interactable,Is.False);
            Assert.That(CandidateState(armor),Does.Contain("민트 착용 중"));
            Assert.That(new EquipmentLoadout(first,new CharacterProgress{Id=first.Id},director.Catalog.Equipment,director.Campaign).Equip(EquipmentSlot.Armor,armor),Is.False);
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(original));
            Click(other.DisplayName);yield return null;
            Assert.That(CandidateState(armor),Is.EqualTo("현재 장착"));Assert.That(EquipmentCandidate(armor).interactable,Is.True);
            Assert.That(CampaignInventory.Equipped(director.Campaign,armor.Id),Is.EqualTo(1));
            Assert.That(CampaignInventory.Available(director.Campaign,armor.Id),Is.Zero);
        }

        [UnityTest] public IEnumerator TesterEquipmentSeparatesSavedWearerFromDraftAndFailedSave()
        {
            director.Campaign=new CampaignSave();director.TrainingMode=false;director.Catalog.CrossoverReady=true;
            var first=director.Catalog.Characters.Single(c=>c.Id=="cless");var other=director.Catalog.Characters.Single(c=>c.Id=="mint");
            var armor=director.Catalog.Equipment.Single(e=>e.Id=="leather-armor");
            director.Campaign.Inventory.Add(new OwnedEquipment{Id=armor.Id,Count=2});director.Campaign.Get(other.Id).Equipment[(int)EquipmentSlot.Armor]=armor.Id;
            string before=JsonUtility.ToJson(director.Campaign);director.PersistCampaign=_=>false;
            director.Hud.ShowDeployment();yield return null;Click("장비 관리");yield return null;Click("방어구: 없음");yield return null;
            Assert.That(EquipmentCandidate(armor).interactable,Is.True);Click("장착 후보: "+armor.DisplayName);yield return null;
            Assert.That(CandidateState(armor),Does.Contain("선택 초안").And.Contain("민트 착용 중"));
            Click("적용 · 저장");yield return null;
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));Assert.That(CandidateState(armor),Does.Contain("선택 초안"));
            Click("돌아가기 (미적용 취소)");yield return null;
            Assert.That(CandidateState(armor),Does.Not.Contain("선택 초안"));
            Click("장착 후보: "+armor.DisplayName);yield return null;director.PersistCampaign=_=>true;Click("적용 · 저장");yield return null;
            Assert.That(CandidateState(armor),Does.Contain("현재 장착").And.Contain("민트 착용 중"));
            Assert.That(CampaignInventory.Equipped(director.Campaign,armor.Id),Is.EqualTo(2));
            Assert.That(CampaignInventory.Owned(director.Campaign,armor.Id),Is.EqualTo(2));
            Assert.That(director.Campaign.Get(first.Id).Equipment[(int)EquipmentSlot.Armor],Is.EqualTo(armor.Id));
        }

        [UnityTest] public IEnumerator TesterRewardDetailsAreReadOnlyAndPreserveMandatoryEnding()
        {
            director.Campaign=new CampaignSave();director.TrainingMode=false;int writes=0;
            director.PersistCampaign=_=>{writes++;return true;};director.RewardRoll=()=>9999;
            director.BeginBattle();yield return null;
            foreach(var enemy in director.Session.Units.Where(u=>u.Team==Team.Enemy))enemy.Damage(99999,director.Session.Grid);
            director.SetState(new BattleEndState(director));yield return null;
            Assert.That(director.ItemResults,Is.Not.Empty);var reward=director.ItemResults[0];string name="보상 상세: "+reward.Item.Id;
            string saved=JsonUtility.ToJson(director.Campaign);int savedWrites=writes;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                foreach(float scale in new[]{1f,1.3f})
                {
                    director.Preferences.TextScale=scale;Click(name);yield return null;yield return new WaitForEndOfFrame();
                    Assert.That(director.Hud.InputModalOpen,Is.True);
                    Assert.That(FormationControl("결과 계속").IsInteractable(),Is.False);
                    var panel=director.Hud.transform.Find("UnitDetails");
                    var expected=(string)typeof(BattleHud).GetMethod("ReadableBonus",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(director.Hud,new object[]{reward.Item});
                    Assert.That(panel.GetComponentsInChildren<TMPro.TMP_Text>().Single(t=>t.name=="RewardItemEffects").text,Is.EqualTo(expected));
                    Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.Button>().Select(b=>b.name),Is.EquivalentTo(new[]{"보상 상세 닫기"}));
                    foreach(var text in panel.GetComponentsInChildren<TMPro.TMP_Text>()){text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.text);}
                    if(scale==1)Click("보상 상세 닫기");else yield return KeyboardPress(keyboard,Key.Escape);
                    yield return null;Assert.That(director.Hud.InputModalOpen,Is.False);
                    Assert.That(FormationControl("결과 계속").IsInteractable(),Is.True);
                    Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(saved));Assert.That(writes,Is.EqualTo(savedWrites));
                }
            }
            finally{InputSystem.RemoveDevice(keyboard);}
            Click("결과 계속");yield return null;Assert.That(director.StoryActive,Is.True);
            while(director.StoryActive){director.AdvanceStory();yield return null;}
            Assert.That(director.Campaign.HasPendingAfterStory,Is.False);Assert.That(director.SelectedStage,Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator TesterRewardDetailsFilterUnjoinedAndIncompatibleRecipients()
        {
            director.Campaign=new CampaignSave();director.TrainingMode=false;director.PersistCampaign=_=>true;
            director.BeginBattle();yield return null;
            foreach(var enemy in director.Session.Units.Where(u=>u.Team==Team.Enemy))enemy.Damage(99999,director.Session.Grid);
            director.SetState(new BattleEndState(director));yield return null;
            director.Catalog.CrossoverReady=true;
            var item=Object.Instantiate(director.Catalog.Equipment.Single(e=>e.Id=="leather-armor"));
            var open=typeof(BattleHud).GetMethod("ShowRewardItemDetails",BindingFlags.Instance|BindingFlags.NonPublic);
            string before=JsonUtility.ToJson(director.Campaign);
            try
            {
                var hidden=director.Catalog.Characters.First(c=>!director.IsJoined(c));
                foreach(string exclusive in new[]{"", "cless",hidden.Id})
                {
                    item.ExclusiveCharacterId=exclusive;
                    open.Invoke(director.Hud,new object[]{new BattleDirector.ItemResult{Item=item,Count=1},null});yield return null;
                    string names=director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Single(t=>t.name=="RewardEligibleCharacters").text;
                    var allowed=director.Catalog.Characters.Where(c=>director.IsJoined(c)&&EquipmentLoadout.CanEquip(c,item,item.Slot)).ToArray();
                    foreach(var c in allowed)Assert.That(names,Does.Contain(c.DisplayName.Split(' ')[0]));
                    foreach(var c in director.Catalog.Characters.Where(c=>!allowed.Contains(c)))Assert.That(names,Does.Not.Contain(c.DisplayName.Split(' ')[0]));
                    Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));director.Hud.CloseUnitDetails();yield return null;
                }
            }
            finally{Object.Destroy(item);}
        }
    }
}
