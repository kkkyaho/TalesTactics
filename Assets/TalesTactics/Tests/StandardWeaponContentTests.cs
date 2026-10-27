using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [TestCase("training-staff",WeaponType.Staff,0,8,0)]
        [TestCase("steel-claw",WeaponType.Claw,8,0,0)]
        [TestCase("steel-fist",WeaponType.Fist,8,0,0)]
        [TestCase("training-spear",WeaponType.Spear,0,8,0)]
        [TestCase("reinforced-bow",WeaponType.Bow,8,0,0)]
        [TestCase("infantry-rifle",WeaponType.Gun,8,0,0)]
        [TestCase("steel-shield",WeaponType.Shield,4,0,4)]
        public void StandardWeaponActualCatalogPurchaseCompatibilityAndReload(string id,WeaponType type,int attack,int magic,int defense)
        {
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            var item=catalog.Equipment.Single(e=>e.Id==id);
            Assert.That(item.Weapon,Is.EqualTo(type));Assert.That(item.Slot,Is.EqualTo(EquipmentSlot.Weapon));
            Assert.That(item.BuyPrice,Is.EqualTo(240));Assert.That(item.RequiredStoryFlag,Is.EqualTo("chapter1"));
            Assert.That(item.Bonus.STR,Is.EqualTo(attack));Assert.That(item.Bonus.MAG,Is.EqualTo(magic));Assert.That(item.Bonus.DEF,Is.EqualTo(defense));
            Assert.That(item.Bonus.HP+item.Bonus.MP+item.Bonus.MDF+item.Bonus.SPD+item.Bonus.MOV+item.Bonus.JMP,Is.Zero);
            Assert.That(item.Icon,Is.Not.Null);Assert.That(item.Icon.name,Is.EqualTo(id));Assert.That(item.Appearance,Is.Null);Assert.That(item.ExclusiveCharacterId,Is.Null.Or.Empty);
            var iconImporter=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(item.Icon));
            Assert.That(iconImporter.npotScale,Is.EqualTo(TextureImporterNPOTScale.None));
            Assert.That(item.Icon.texture.width,Is.EqualTo(1774));Assert.That(item.Icon.texture.height,Is.EqualTo(887),"Reviewed NPOT source pixels must not be resampled.");
            var compatible=catalog.Characters.Where(c=>c.Weapon==type).ToArray();Assert.That(compatible,Is.Not.Empty);
            foreach(var character in catalog.Characters)Assert.That(EquipmentLoadout.CanEquip(character,item,EquipmentSlot.Weapon),Is.EqualTo(character.Weapon==type),character.Id);
            var save=new CampaignSave();int writes=0;
            Assert.That(CampaignInventory.TryBuy(save,item,_=>{writes++;return true;}),Is.False);Assert.That(writes,Is.Zero);
            var file=new CampaignFile(SaveTestPath());
            // Invoke the first-main reward API directly: this is inventory integration, not a played battle.
            Assert.That(CampaignStages.TryReward(save,CrossoverCampaign.FirstStage,catalog.Characters.Where(c=>CrossoverCampaign.Joined(c.Id,1)).ToArray(),file.Save),Is.True);
            Assert.That(save.Gold,Is.EqualTo(470));string before=JsonUtility.ToJson(save);
            Assert.That(CampaignInventory.TryBuy(save,item,_=>false),Is.False);Assert.That(JsonUtility.ToJson(save),Is.EqualTo(before));
            Assert.That(CampaignInventory.TryBuy(save,item,file.Save),Is.True);Assert.That(save.Gold,Is.EqualTo(230));
            var owner=compatible[0];var draft=new EquipmentLoadout(owner,save.Get(owner.Id),catalog.Equipment,save);
            Assert.That(draft.Equip(EquipmentSlot.Weapon,item),Is.True);Assert.That(draft.TrySave(save,file.Save),Is.True);
            var loaded=file.Load();var progress=loaded.Get(owner.Id);var equipped=new EquipmentLoadout(owner,progress,catalog.Equipment,loaded);
            Assert.That(loaded.Gold,Is.EqualTo(230));Assert.That(CampaignInventory.Owned(loaded,id),Is.EqualTo(1));Assert.That(CampaignInventory.Available(loaded,id),Is.Zero);
            Assert.That(equipped.Get(EquipmentSlot.Weapon),Is.SameAs(item));Assert.That(loaded.StoryProgress,Does.Contain(CrossoverCampaign.Flag(1)).And.Not.Contain("chapter1"));
            var baseline=owner.StatsAt(progress.Level);var stats=equipped.Preview(progress.Level,false);
            Assert.That(stats.STR-baseline.STR,Is.EqualTo(attack));Assert.That(stats.MAG-baseline.MAG,Is.EqualTo(magic));Assert.That(stats.DEF-baseline.DEF,Is.EqualTo(defense));
            if(compatible.Length>1)
            {
                var other=new EquipmentLoadout(compatible[1],new CharacterProgress{Id=compatible[1].Id},catalog.Equipment,loaded);
                Assert.That(other.Equip(EquipmentSlot.Weapon,item),Is.False,"One purchased staff cannot be equipped by two companions at once.");
            }
        }
        [Test] public void StandardWeaponReinstallationPreservesExistingUserValuesAndDoesNotDuplicateCatalog()
        {
            var source=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            string[] ids={"training-staff","steel-claw","steel-fist","training-spear","reinforced-bow","infantry-rifle","steel-shield"};
            var icons=ids.ToDictionary(id=>id,id=>source.Equipment.Single(e=>e.Id==id).Icon);
            string folder="Assets/StandardWeaponRegression-"+Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets",folder.Substring("Assets/".Length));
            var catalog=New<BattleCatalog>();var untouched=New<EquipmentData>();untouched.Id="user-equipment";untouched.BuyPrice=713;
            catalog.Equipment=new[]{untouched};
            try
            {
                TalesTactics.Editor.StandardWeaponContent.AddMissing(catalog,folder,icons);
                var staff=catalog.Equipment.Single(e=>e.Id=="training-staff");staff.BuyPrice=937;staff.Bonus=new Stats{MAG=13};staff.SourceNote="User authored note";EditorUtility.SetDirty(staff);AssetDatabase.SaveAssets();
                var references=catalog.Equipment.ToArray();string authored=EditorJsonUtility.ToJson(staff);
                TalesTactics.Editor.StandardWeaponContent.AddMissing(catalog,folder,icons);
                Assert.That(catalog.Equipment,Is.EqualTo(references));Assert.That(EditorJsonUtility.ToJson(staff),Is.EqualTo(authored));
                Assert.That(untouched.BuyPrice,Is.EqualTo(713));Assert.That(catalog.Equipment.Select(e=>e.Id).Distinct().Count(),Is.EqualTo(8));
            }
            finally{AssetDatabase.DeleteAsset(folder);AssetDatabase.Refresh();}
        }
    }
}
