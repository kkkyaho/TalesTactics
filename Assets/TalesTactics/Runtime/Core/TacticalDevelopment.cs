using System;
using System.Linq;
using UnityEngine;

namespace TalesTactics
{
    public enum TacticalTrait { Balanced, Swift, Protector, Focus, Healer }
    public enum EquipmentEffect { None, EfficientCasting, Restorative }

    public static class TacticalDevelopment
    {
        public static string TraitName(TacticalTrait trait) => new[]{"균형", "기동", "호위", "집중", "치유"}[(int)trait];
        public static string TraitDescription(TacticalTrait trait)
        {
            switch(trait)
            {
                case TacticalTrait.Swift:return "MOV +1 / DEF -15%";
                case TacticalTrait.Protector:return "방어 중 인접 동료의 피해 30% 감소 · 자기 턴마다 1회 / MOV -1";
                case TacticalTrait.Focus:return "기술 MP 비용 -20% / MOV -1";
                case TacticalTrait.Healer:return "회복량 +25% / STR -15%";
                default:return "능력치 변화 없음";
            }
        }
        public static string Role(CharacterData c)
        {
            switch(c.Id)
            {
                case "mint":return "전담 회복";
                case "tear":return "회복 · 지원";
                case "kisara":return "전열 · 보호";
                case "jade":return "범위 마법";
                case "natalia":case "shionne":return "원거리 · 회복";
                case "velvet":case "alphen":return "돌파 · 연계";
                default:return "전열 · 공격";
            }
        }
        public static Stats ApplyStats(Stats s,TacticalTrait trait)
        {
            if(trait==TacticalTrait.Swift){s.MOV++;s.DEF=Mathf.RoundToInt(s.DEF*.85f);}
            if(trait==TacticalTrait.Protector||trait==TacticalTrait.Focus)s.MOV=Mathf.Max(1,s.MOV-1);
            if(trait==TacticalTrait.Healer)s.STR=Mathf.RoundToInt(s.STR*.85f);
            return s;
        }
        public static int[] Recommended(BattleCatalog catalog)
        {
            string[] ids={"cless","mint","kisara","jade","shionne","farah"};
            return ids.Select(id=>Array.FindIndex(catalog.Characters,c=>c.Id==id)).Where(i=>i>=0).Take(catalog.Rules.MaxDeployment).ToArray();
        }
        public static int Mastery(CampaignSave save)=>save.StoryProgress.Contains("chapter4")?3:save.StoryProgress.Contains("chapter2")?2:save.StoryProgress.Contains("chapter1")?1:0;
        public static string MasteryDescription=>"1장 완료: Lv7 이하 일반 기술 / 2장: Lv13 이하 / 4장: Lv19 이하\n6장 출전: 궁극기 체험 1회 (자원·연계 조건 적용). 승급은 Lv20 유지";
        public static void Apply(UnitRuntime unit,CampaignSave save,int stage)
        {
            unit.Trait=save.Get(unit.Data.Id).Trait;
            unit.CampaignMastery=Mastery(save);
            unit.UltimateTrial=stage==5;
            if(unit.UltimateTrial)unit.SpecialGauge=100;
        }
        public static bool SaveTrait(CampaignSave save,CharacterData character,TacticalTrait trait,Func<CampaignSave,bool> persist)
        {
            if(!Enum.IsDefined(typeof(TacticalTrait),trait)||persist==null)return false;
            var progress=save.Get(character.Id);var previous=progress.Trait;progress.Trait=trait;
            try{if(persist(save))return true;}catch(Exception){}
            progress.Trait=previous;return false;
        }
        // Existing levels/EXP are never reduced. Training is offered only for unlocked chapters.
        public static bool CatchUp(CampaignSave save,CharacterData character,int stage,Func<CampaignSave,bool> persist)
        {
            if(!CampaignStages.Unlocked(save,stage)||persist==null)return false;
            var progress=save.Get(character.Id);int target=CampaignStages.Get(stage).EntryLevel;
            if(progress.Level>=target)return false;
            int oldLevel=progress.Level,oldExp=progress.EXP;var oldSkills=progress.UnlockedSkills;
            progress.Level=target;progress.EXP=0;
            progress.UnlockedSkills=character.Skills.Where(s=>s.UnlockLevel<=target).Select(s=>s.Id).ToList();
            try{if(persist(save))return true;}catch(Exception){}
            progress.Level=oldLevel;progress.EXP=oldExp;progress.UnlockedSkills=oldSkills;return false;
        }
    }
}
