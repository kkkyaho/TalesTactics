using System.Linq;
using UnityEditor;
using UnityEngine;
namespace TalesTactics.Editor
{
    public static class ShopContent
    {
        // Explicit additive setup; never call DemoContent.Create or recreate the scene.
        [MenuItem("Tales Tactics/Add Shop Sample Content")]
        public static void AddSamples()
        {
            const string root="Assets/TalesTactics/Content/";
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>(root+"BattleCatalog.asset");
            if(catalog==null)throw new System.InvalidOperationException("Existing battle catalog required.");
            var sword=AssetDatabase.LoadAssetAtPath<EquipmentData>(root+"BronzeSword.asset");
            if(sword!=null&&sword.BuyPrice==0){sword.BuyPrice=100;EditorUtility.SetDirty(sword);}
            var armor=CreateIfMissing(root+"LeatherArmor.asset","leather-armor","가죽 갑옷",EquipmentSlot.Armor,150,new Stats{HP=20,DEF=3});
            var charm=CreateIfMissing(root+"VitalCharm.asset","vital-charm","생명의 부적",EquipmentSlot.Accessory,100,new Stats{HP=25});
            catalog.Equipment=(catalog.Equipment??new EquipmentData[0]).Concat(new[]{sword,armor,charm}).Where(e=>e!=null).Distinct().ToArray();
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
        static EquipmentData CreateIfMissing(string path,string id,string title,EquipmentSlot slot,int price,Stats bonus)
        {
            var existing=AssetDatabase.LoadAssetAtPath<EquipmentData>(path);if(existing!=null)return existing;
            var item=ScriptableObject.CreateInstance<EquipmentData>();
            item.Id=id;item.DisplayName=title;item.Slot=slot;item.BuyPrice=price;item.Bonus=bonus;
            AssetDatabase.CreateAsset(item,path);return item;
        }
    }
}
