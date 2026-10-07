using System.Linq;
using UnityEditor;
using UnityEngine;
namespace TalesTactics.Editor
{
    public static class TacticalContent
    {
        const string Root="Assets/TalesTactics/Content/";
        // Add only missing assets. Existing designer edits are never reset.
        [MenuItem("Tales Tactics/Add Tactical Options")]
        public static void Add()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>(Root+"BattleCatalog.asset");
            if(catalog==null)throw new System.InvalidOperationException("Existing catalog required");
            var skills=new[]{Skill("TacticalHeal","tactic.heal","응급 치료",TargetType.Ally,4,0,6,false,1.2f,20),
                Skill("TacticalBurst","tactic.burst","예고 화염진",TargetType.Enemy,5,1,8,true,1.5f,0),
                Skill("TacticalCharge","tactic.charge","예고 돌진",TargetType.Enemy,4,0,0,false,1.6f,0),
                Skill("TacticalNova","tactic.nova","다오스 붕괴진",TargetType.Enemy,6,2,10,true,1.8f,0)};
            catalog.TacticalEnemySkills=(catalog.TacticalEnemySkills??new SkillData[0]).Concat(skills).Distinct().ToArray();
            var gear=new[]{Item("SwiftBoots","swift-boots","기동 장화",new Stats{MOV=1,DEF=-3},EquipmentEffect.None,180),
                Item("FocusCharm","focus-charm","집중 부적",new Stats{MOV=-1},EquipmentEffect.EfficientCasting,180),
                Item("HealerBadge","healer-badge","치유 휘장",new Stats{SPD=-2},EquipmentEffect.Restorative,180)};
            catalog.Equipment=catalog.Equipment.Concat(gear).Distinct().ToArray();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
        static SkillData Skill(string file,string id,string title,TargetType target,int range,int area,int mp,bool magic,float power,int flat)
        {
            string path=Root+file+".asset";var s=AssetDatabase.LoadAssetAtPath<SkillData>(path);if(s!=null)return s;
            s=ScriptableObject.CreateInstance<SkillData>();s.Id=id;s.DisplayName=title;s.Target=target;s.Range=range;s.MinRange=target==TargetType.Ally?0:1;s.Area=area;s.MPCost=mp;
            s.Animation=magic||target==TargetType.Ally?AnimationKind.Cast:AnimationKind.Skill;s.Element=magic?Element.Fire:Element.None;
            s.Effects=new[]{new SkillEffect{Kind=target==TargetType.Ally?EffectKind.Heal:EffectKind.Damage,Magic=magic,Power=power,Flat=flat}};
            AssetDatabase.CreateAsset(s,path);return s;
        }
        static EquipmentData Item(string file,string id,string title,Stats stats,EquipmentEffect effect,int price)
        {
            string path=Root+file+".asset";var e=AssetDatabase.LoadAssetAtPath<EquipmentData>(path);if(e!=null)return e;
            e=ScriptableObject.CreateInstance<EquipmentData>();e.Id=id;e.DisplayName=title;e.Slot=EquipmentSlot.Accessory;e.Bonus=stats;e.Effect=effect;e.BuyPrice=price;e.RequiredStoryFlag="chapter1";
            AssetDatabase.CreateAsset(e,path);return e;
        }
    }
}
