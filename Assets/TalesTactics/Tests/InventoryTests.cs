using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [Test] public void LegacyInventoryMigrationPreservesEveryEquippedCopyAndOriginalFile()
        {
            var legacy=new CampaignSave{Version=1};
            legacy.Get("a").Equipment[0]="sword";legacy.Get("b").Equipment[0]="sword";
            legacy.Get("a").Level=12;legacy.Get("a").EXP=30;legacy.StoryProgress.Add("chapter1");
            string path=SaveTestPath(),json=JsonUtility.ToJson(legacy);
            File.WriteAllText(path,json);
            var store=new CampaignFile(path);var loaded=store.Load();
            Assert.That(loaded.Version,Is.EqualTo(2));Assert.That(loaded.Gold,Is.EqualTo(300));
            Assert.That(CampaignInventory.Owned(loaded,"sword"),Is.EqualTo(2));
            Assert.That(loaded.Get("a").Level,Is.EqualTo(12));Assert.That(loaded.Get("a").EXP,Is.EqualTo(30));
            Assert.That(File.ReadAllText(path),Is.EqualTo(json),"Loading must not rewrite the original.");
            Assert.That(store.Save(loaded),Is.True);Assert.That(File.ReadAllText(path+".v1.bak"),Is.EqualTo(json));
            loaded.Gold=45;Assert.That(store.Save(loaded),Is.True);
            var again=new CampaignFile(path).Load();Assert.That(again.Gold,Is.EqualTo(45));
            Assert.That(CampaignInventory.Owned(again,"sword"),Is.EqualTo(2));
            Assert.That(File.ReadAllText(path+".v1.bak"),Is.EqualTo(json));
        }

        [Test] public void PurchaseFailureOrExceptionRestoresGoldAndQuantity()
        {
            var save=new CampaignSave();var item=Sword();item.BuyPrice=100;
            Assert.That(CampaignInventory.TryBuy(save,item,_=>false),Is.False);
            Assert.That(save.Gold,Is.EqualTo(300));Assert.That(CampaignInventory.Owned(save,item.Id),Is.Zero);
            Assert.Throws<IOException>(()=>CampaignInventory.TryBuy(save,item,_=>throw new IOException("test")));
            Assert.That(save.Gold,Is.EqualTo(300));Assert.That(CampaignInventory.Owned(save,item.Id),Is.Zero);
            Assert.That(CampaignInventory.TryBuy(save,item,_=>true),Is.True);
            Assert.That(save.Gold,Is.EqualTo(200));Assert.That(CampaignInventory.Owned(save,item.Id),Is.EqualTo(1));
            Assert.That(CampaignInventory.TryBuy(save,item,_=>false),Is.False);
            Assert.That(save.Gold,Is.EqualTo(200));Assert.That(CampaignInventory.Owned(save,item.Id),Is.EqualTo(1));
        }

        [Test] public void PurchaseRejectsUnaffordableUnpricedAndFullInventory()
        {
            var save=new CampaignSave();var item=Sword();int writes=0;
            Func<CampaignSave,bool> persist=_=>{writes++;return true;};
            Assert.That(CampaignInventory.TryBuy(save,item,persist),Is.False);
            item.BuyPrice=301;Assert.That(CampaignInventory.TryBuy(save,item,persist),Is.False);
            item.BuyPrice=1;save.Inventory.Add(new OwnedEquipment{Id=item.Id,Count=CampaignInventory.MaxQuantity});
            Assert.That(CampaignInventory.TryBuy(save,item,persist),Is.False);
            Assert.That(writes,Is.Zero);Assert.That(save.Gold,Is.EqualTo(300));
        }

        [Test] public void TwoDraftsCannotReserveOneCopyAndUnequipReleasesIt()
        {
            var save=new CampaignSave();var item=Sword();save.Inventory.Add(new OwnedEquipment{Id=item.Id,Count=1});
            data.Weapon=WeaponType.Sword;var other=New<CharacterData>();other.Id="other";other.Weapon=WeaponType.Sword;
            var a=new EquipmentLoadout(data,save.Get(data.Id),new[]{item},save);
            var b=new EquipmentLoadout(other,save.Get(other.Id),new[]{item},save);
            Assert.That(a.Equip(EquipmentSlot.Weapon,item),Is.True);Assert.That(b.Equip(EquipmentSlot.Weapon,item),Is.True);
            Assert.That(a.TrySave(save,_=>true),Is.True);int writes=0;
            Assert.That(b.TrySave(save,_=>{writes++;return true;}),Is.False);Assert.That(writes,Is.Zero);
            Assert.That(b.Options(EquipmentSlot.Weapon),Is.Empty);
            a.Equip(EquipmentSlot.Weapon,null);Assert.That(a.TrySave(save,_=>false),Is.False);
            Assert.That(b.CanCommit(save),Is.False,"Failed unequip must not release the copy.");
            Assert.That(a.TrySave(save,_=>true),Is.True);Assert.That(b.TrySave(save,_=>true),Is.True);
            Assert.That(CampaignInventory.Owned(save,item.Id),Is.EqualTo(1));
            Assert.That(CampaignInventory.Available(save,item.Id),Is.Zero);
        }

        [Test] public void PurchaseAndLoadoutPersistTogetherAcrossReload()
        {
            var save=new CampaignSave();var file=new CampaignFile(SaveTestPath());var item=Sword();item.BuyPrice=100;
            data.Weapon=WeaponType.Sword;
            Assert.That(CampaignInventory.TryBuy(save,item,file.Save),Is.True);
            var draft=new EquipmentLoadout(data,save.Get(data.Id),new[]{item},save);
            draft.Equip(EquipmentSlot.Weapon,item);Assert.That(draft.TrySave(save,file.Save),Is.True);
            var loaded=file.Load();Assert.That(loaded.Gold,Is.EqualTo(200));
            Assert.That(CampaignInventory.Owned(loaded,item.Id),Is.EqualTo(1));
            Assert.That(loaded.Get(data.Id).Equipment[0],Is.EqualTo(item.Id));
        }

        [Test] public void Version2RejectsDuplicateInventoryAndOverallocatedEquipment()
        {
            var save=new CampaignSave();save.Inventory.Add(new OwnedEquipment{Id=CampaignInventory.StarterSword,Count=1});
            Assert.Throws<InvalidDataException>(()=>CampaignFile.Normalize(save));
            save.Inventory.RemoveAt(1);save.Get("a").Equipment[0]=CampaignInventory.StarterSword;save.Get("b").Equipment[0]=CampaignInventory.StarterSword;
            Assert.Throws<InvalidDataException>(()=>CampaignFile.Normalize(save));
            string path=SaveTestPath();File.WriteAllText(path,JsonUtility.ToJson(save));
            var file=new CampaignFile(path);file.Load();Assert.That(file.CanSave,Is.False);
        }

        [Test] public void GoldRewardsRollbackRetryAndClampWithoutOverflow()
        {
            var save=new CampaignSave();data.Skills=new SkillData[0];
            Assert.That(CampaignStages.TryReward(save,0,new[]{data},_=>false),Is.False);Assert.That(save.Gold,Is.EqualTo(300));
            Assert.That(CampaignStages.TryReward(save,0,new[]{data},_=>true),Is.True);Assert.That(save.Gold,Is.EqualTo(420));
            Assert.That(CampaignStages.TryReward(save,1,new[]{data},_=>true),Is.True);Assert.That(save.Gold,Is.EqualTo(600));
            save.Gold=CampaignInventory.MaxGold-1;
            Assert.That(CampaignStages.TryReward(save,1,new[]{data},_=>true),Is.True);Assert.That(save.Gold,Is.EqualTo(CampaignInventory.MaxGold));
        }
    }
}
