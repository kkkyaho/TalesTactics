using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace TalesTactics.Editor
{
    // Explicit, additive installation. Never calls DemoContent and never resets hero assets.
    public static class CrossoverContentInstaller
    {
        const string Root="Assets/TalesTactics/Content/Crossover/";
        public static void InstallBatch(){Debug.Log(Install());}
        public static void CreateStagedVariantsBatch()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            if(catalog.CrossoverReady||catalog.CrossoverVariants.Length>0)throw new InvalidOperationException("Variants already installed or published");
            var variants=new List<CharacterData>();
            foreach(string id in new[]{"dhaos","golem","hyades"})
            {
                var source=catalog.Enemies.Single(c=>c.Id==id);string path=Root+(id=="hyades"?"hyades-human":id)+".asset";
                if(AssetDatabase.LoadAssetAtPath<CharacterData>(path)!=null||!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source),path))throw new InvalidOperationException("Refusing to overwrite variant "+path);
                var variant=AssetDatabase.LoadAssetAtPath<CharacterData>(path);variant.CrossoverEarlyForm=id=="hyades";
                if(variant.CrossoverEarlyForm){variant.Job="초반형 추격자";variant.Weapon=WeaponType.Fist;}
                variants.Add(variant);EditorUtility.SetDirty(variant);
            }
            catalog.CrossoverVariants=variants.ToArray();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            Debug.Log("Preserved crossover Dhaos/Golem in dedicated variants; early Hyades variant awaits reviewed art.");
        }
        public static void RefreshStagedSkillsBatch()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            if(catalog==null||catalog.CrossoverReady)throw new InvalidOperationException("Only unpublished crossover skill assets may be refreshed");
            foreach(var entry in CrossoverBossSources.Entries)for(int i=0;i<3;i++)
            {
                string id="boss."+entry[0]+"."+i;var existing=AssetDatabase.LoadAssetAtPath<SkillData>(Root+id+".asset");
                if(existing==null||existing.Id!=id)throw new InvalidOperationException("Missing staged skill "+id);
                var authored=ScriptableObject.CreateInstance<SkillData>();authored.Id=existing.Id;authored.DisplayName=existing.DisplayName;authored.SourceNote=existing.SourceNote;
                CrossoverSkillRules.Configure(entry[0],i,authored);EditorUtility.CopySerialized(authored,existing);UnityEngine.Object.DestroyImmediate(authored);EditorUtility.SetDirty(existing);
            }
            AssetDatabase.SaveAssets();Debug.Log("Updated 60 unpublished authored skill assets; hero content and readiness unchanged.");
        }
        static T New<T>(string name) where T:ScriptableObject
        {
            string path=Root+name+".asset";
            if(AssetDatabase.LoadAssetAtPath<T>(path)!=null)throw new InvalidOperationException("Refusing to overwrite reviewed content: "+path);
            var asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);return asset;
        }
        public static string Install()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            if(catalog==null)throw new InvalidOperationException("Missing existing catalog");
            if(Directory.Exists(Root)&&Directory.GetFiles(Root,"*.asset").Length>0)throw new InvalidOperationException("Crossover content already installed; edit it explicitly");
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var enemies=new List<CharacterData>(catalog.Enemies);
            foreach(var entry in CrossoverBossSources.Entries)
            {
                string id=entry[0];bool major=BossTimeStop.MajorId(id);
                var character=enemies.FirstOrDefault(c=>c.Id==id);
                bool existing=character!=null;
                if(!existing){character=New<CharacterData>(id);character.Id=id;enemies.Add(character);}
                if(!existing)
                {
                    character.DisplayName=entry[1];character.SourceTitle=entry[2];character.Job=major?"경계의 강자":"독립 세력";
                    character.BaseStats=new Stats{HP=major?680:440,MP=500,STR=40,MAG=40,DEF=21,MDF=21,SPD=14,MOV=3,JMP=2};
                    character.GrowthStats=new Stats{HP=24,MP=4,STR=2,MAG=2,DEF=1,MDF=1,SPD=1};
                    character.Weapon=id=="legretta"?WeaponType.Gun:id=="executor"?WeaponType.Spear:WeaponType.Sword;
                    character.VisualStyle=id=="legretta"?CombatVisualStyle.Rifle:CombatVisualStyle.Sword;
                }
                var authored=new List<SkillData>();
                for(int i=0;i<3;i++)
                {
                    var skill=New<SkillData>("boss."+id+"."+i);skill.Id="boss."+id+"."+i;
                    skill.DisplayName=entry[4+i*2];skill.SourceNote=entry[5+i*2];
                    CrossoverSkillRules.Configure(id,i,skill);authored.Add(skill);EditorUtility.SetDirty(skill);
                }
                if(!existing){character.BasicAttack=New<SkillData>("boss."+id+".attack");var attack=character.BasicAttack;attack.Id="boss."+id+".attack";attack.DisplayName="공격";attack.Animation=AnimationKind.Attack;attack.Target=TargetType.Enemy;attack.Range=id=="legretta"?4:1;attack.Effects=new[]{new SkillEffect{Kind=EffectKind.Damage,Power=1}};EditorUtility.SetDirty(attack);}
                character.Skills=(character.Skills??new SkillData[0]).Concat(authored).ToArray();EditorUtility.SetDirty(character);
            }
            var device=New<CharacterData>("boundary-device");device.Id="boundary-device";device.DisplayName="경계 장치";device.Job="파괴 가능한 연결 핵";device.BaseStats=new Stats{HP=180,MP=1,DEF=12,MDF=12,SPD=1,MOV=0,JMP=0};device.GrowthStats=new Stats{HP=6,DEF=1,MDF=1};device.BasicAttack=catalog.Enemy.BasicAttack;enemies.Add(device);
            var equipment=new List<EquipmentData>(catalog.Equipment);
            for(int i=0;i<PersonalScenarios.Arcs.Length;i++)
            {
                var arc=PersonalScenarios.Arcs[i];var hero=catalog.Characters.First(c=>c.Id==arc.Owner);
                var weapon=New<EquipmentData>(arc.WeaponId);weapon.Id=arc.WeaponId;weapon.DisplayName=arc.WeaponName;weapon.ExclusiveCharacterId=arc.Owner;weapon.Slot=EquipmentSlot.Weapon;weapon.Weapon=hero.Weapon;weapon.Effect=arc.Effect;weapon.BuyPrice=0;
                weapon.Bonus=new Stats{STR=hero.Weapon==WeaponType.Staff?8:22,MAG=hero.Weapon==WeaponType.Staff?24:8,HP=30};
                weapon.SourceNote="원작 무기명 채택 · 획득 시련과 전용 효과는 본작 창작";equipment.Add(weapon);EditorUtility.SetDirty(weapon);
            }
            catalog.Enemies=enemies.ToArray();catalog.Equipment=equipment.ToArray();
            // Presentation and end-to-end checks must pass before explicitly enabling CrossoverReady.
            EditorUtility.SetDirty(device);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            return "Installed 60 authored boss skills and 10 exclusive weapons; campaign remains gated pending art and validation.";
        }
    }
}
