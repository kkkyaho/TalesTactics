using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TalesTactics.Editor
{
    public static class ChapterShopContent
    {
        [MenuItem("Tales Tactics/Add Chapter Shop Content")]
        public static void AddSamples()
        {
            const string root = "Assets/TalesTactics/Content/";
            var catalog = AssetDatabase.LoadAssetAtPath<BattleCatalog>(root + "BattleCatalog.asset");
            if (catalog == null) throw new System.InvalidOperationException("Existing catalog required.");
            var sword = Create(root + "IronSword.asset", "iron-sword", "철검", EquipmentSlot.Weapon,
                240, new Stats { STR = 8 });
            var armor = Create(root + "ReinforcedArmor.asset", "reinforced-armor", "강화 갑옷", EquipmentSlot.Armor,
                300, new Stats { HP = 35, DEF = 6 });
            catalog.Equipment = (catalog.Equipment ?? new EquipmentData[0]).Concat(new[] { sword, armor }).Distinct().ToArray();
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        }

        static EquipmentData Create(string path, string id, string title, EquipmentSlot slot, int price, Stats bonus)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
            if (existing != null) return existing;
            var item = ScriptableObject.CreateInstance<EquipmentData>();
            item.Id = id; item.DisplayName = title; item.Slot = slot; item.Weapon = WeaponType.Sword;
            item.BuyPrice = price; item.Bonus = bonus; item.RequiredStoryFlag = CampaignStages.Id(0);
            AssetDatabase.CreateAsset(item, path); return item;
        }
    }
}
