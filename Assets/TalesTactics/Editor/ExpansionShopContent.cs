using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TalesTactics.Editor
{
    public static class ExpansionShopContent
    {
        [MenuItem("Tales Tactics/Add Expansion Shop Content")]
        public static void Add()
        {
            const string root="Assets/TalesTactics/Content/";
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>(root+"BattleCatalog.asset");
            if(catalog==null)throw new InvalidOperationException("Existing catalog required");
            var medal=Create(root+"GuardianMedal.asset","guardian-medal","수호의 메달",EquipmentSlot.Accessory,480,new Stats{HP=50,MDF=8},"chapter4");
            var armor=Create(root+"TemperedArmor.asset","tempered-armor","단련 갑옷",EquipmentSlot.Armor,650,new Stats{HP=70,DEF=10},"chapter5");
            catalog.Equipment=catalog.Equipment.Concat(new[]{medal,armor}).Distinct().ToArray();
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
        static EquipmentData Create(string path,string id,string name,EquipmentSlot slot,int price,Stats bonus,string flag)
        {
            var existing=AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
            if(existing!=null)return existing; // Preserve user tuning on subsequent invocations.
            var item=ScriptableObject.CreateInstance<EquipmentData>();
            item.Id=id;item.DisplayName=name;item.Slot=slot;item.BuyPrice=price;item.Bonus=bonus;item.RequiredStoryFlag=flag;
            AssetDatabase.CreateAsset(item,path);return item;
        }
    }
}
