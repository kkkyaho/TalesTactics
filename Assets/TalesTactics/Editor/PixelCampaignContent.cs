using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace TalesTactics.Editor
{
    public static class PixelCampaignContent
    {
        const string Root="Assets/TalesTactics/Content/PixelCampaign";
        public static readonly string[] EnemyIds={"wolf","slime","golem","sentinel","archer","mage","polwigle","eggbear","penguinist","dhaos"};
        [MenuItem("Tales Tactics/Pixel Campaign/Create Enemy Catalog")]
        public static void CreateEnemies()
        {
            if(!AssetDatabase.IsValidFolder(Root))AssetDatabase.CreateFolder("Assets/TalesTactics/Content","PixelCampaign");
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            if(catalog==null)throw new InvalidOperationException("Missing Catalog");
            string[] names={"울프","슬라임","록 골렘","유적 파수병","유적 궁수","유적 마법사","오타오타","에그베어","펭기니스트","다오스"};
            int[] hp={115,125,190,145,110,100,105,175,125,280};
            int[] strength={27,23,33,30,25,18,24,32,25,27};
            int[] magic={12,22,10,20,12,29,24,10,26,31};
            int[] defense={11,12,23,15,10,9,10,18,12,18};
            int[] speed={17,11,9,13,14,12,16,10,15,15};
            var result=new CharacterData[EnemyIds.Length];
            for(int i=0;i<EnemyIds.Length;i++)
            {
                string id=EnemyIds[i],path=Root+"/"+id+".asset";
                var data=AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if(data==null)
                {
                    data=ScriptableObject.CreateInstance<CharacterData>();data.Id=id;data.DisplayName=names[i];
                    data.Job=i==9?"최종 보스":i==4?"궁수":i==5?"술사":"마물";
                    data.SourceTitle=i==9?"Tales of Phantasia":i>=3&&i<=5?"TalesTactics":"Tales series / project reinterpretation";
                    data.Weapon=i==4?WeaponType.Bow:i==3?WeaponType.Spear:i==5||i==9?WeaponType.Staff:WeaponType.Claw;
                    data.BaseStats=new Stats{HP=hp[i],MP=i==9?90:40,STR=strength[i],MAG=magic[i],DEF=defense[i],MDF=i==9?20:12,SPD=speed[i],MOV=i==0?4:3,JMP=1};
                    data.GrowthStats=new Stats{HP=5,STR=1,MAG=1,DEF=1,MDF=1,SPD=1};
                    bool magical=i==1||i==5||i==6||i==8||i==9;
                    int range=i==4||i==5||i==9?3:i==3||i==6||i==8?2:1;
                    string[] attacks={"송곳니","점액 타격","바위 주먹","창 찌르기","유적의 화살","그림자 탄","꼬리치기","곰의 일격","서리 부리","다오스 레이저"};
                    data.BasicAttack=Attack(id+"-attack",attacks[i],range,magical,i==8?Element.Ice:i==6||i==1?Element.Water:i==9?Element.Light:i==5?Element.Dark:Element.None);
                    if(i==9)
                    {
                        var blast=Attack("dhaos-blast","다오스 블래스트",3,true,Element.Light);
                        blast.MPCost=16;blast.Area=1;blast.Animation=AnimationKind.Skill;blast.Effects[0].Power=0.9f;
                        data.Skills=new[]{blast};EditorUtility.SetDirty(blast);
                    }
                    AssetDatabase.CreateAsset(data,path);
                }
                result[i]=data;
            }
            catalog.Enemies=result;EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        }
        static SkillData Attack(string id,string name,int range,bool magic,Element element)
        {
            var path=Root+"/"+id+".asset";var skill=AssetDatabase.LoadAssetAtPath<SkillData>(path);if(skill!=null)return skill;
            skill=ScriptableObject.CreateInstance<SkillData>();skill.Id=id;skill.DisplayName=name;
            skill.Range=range;skill.Target=TargetType.Enemy;skill.Animation=AnimationKind.Attack;
            skill.Element=element;skill.RequiresLineOfSight=range>1;skill.UsesHeightDamage=!magic;
            skill.Effects=new[]{new SkillEffect{Kind=EffectKind.Damage,Magic=magic,Power=1}};
            AssetDatabase.CreateAsset(skill,path);return skill;
        }
    }
}
