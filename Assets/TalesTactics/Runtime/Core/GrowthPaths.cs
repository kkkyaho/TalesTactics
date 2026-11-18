using System;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public enum GrowthPath { Unselected, Assault, Bulwark }
    public static class GrowthPaths
    {
        public static string Name(GrowthPath path)=>path==GrowthPath.Assault?"돌파":path==GrowthPath.Bulwark?"수호":"미선택";
        public static string Description(GrowthPath path)=>path==GrowthPath.Assault?"공격·마력 +12% / 방어·마방 -8%":path==GrowthPath.Bulwark?"방어·마방 +12% / 공격·마력 -8%":"기본 능력치 유지";
        public static string Role(CharacterData character,GrowthPath path)
        {
            if(character?.Id=="cless")return path==GrowthPath.Assault?"진형 돌파":path==GrowthPath.Bulwark?"전열 호위":"기본 성장";
            if(character?.Id=="mint")return path==GrowthPath.Assault?"공격 지원":path==GrowthPath.Bulwark?"방어 지원":"기본 성장";
            return path==GrowthPath.Unselected?"기본 성장":"능력치 강화";
        }
        public static string CombatDescription(CharacterData character,GrowthPath path)
        {
            if(character?.Id=="cless"&&path==GrowthPath.Assault)return "이동 후 기본공격으로 피해를 주면 적을 1칸 밀칩니다.\n빈칸·높이 조건 적용 · 보스/장치·지원/반격·연계 제외";
            if(character?.Id=="cless"&&path==GrowthPath.Bulwark)return "방어 중 인접 아군 피해 -30% · 자기 턴마다 1회\n호위 특성과 중첩되지 않으며 보호 횟수를 공유합니다.";
            if(character?.Id=="mint"&&path==GrowthPath.Assault)return "다른 아군 단일 회복에 실제 회복량이 있으면 성가 1턴\n공격·마력 증가 · 전체 회복/부활/자기 회복·연계 제외";
            if(character?.Id=="mint"&&path==GrowthPath.Bulwark)return "단일 회복으로 HP가 늘면 방어 · 행동 마친 아군에 권장\n대상 다음 턴 시작에 해제 · 전체/부활/자기 회복·연계 제외";
            return path==GrowthPath.Unselected?"추가 전투 효과 없이 기존 직업·기술을 유지합니다.":"기존 직업·기술·전술 특성을 유지합니다.";
        }
        public static bool Active(UnitRuntime unit,string character,GrowthPath path)=>unit!=null&&unit.Team==Team.Player&&unit.Level>=10&&unit.Data.Id==character&&unit.Growth==path;
        public static bool Protects(UnitRuntime unit)=>Active(unit,"cless",GrowthPath.Bulwark);
        public static bool HasActionEffect(UnitRuntime unit,SkillData skill)
        {
            if(unit==null||skill==null||skill.Area!=0||skill.Shape!=SkillAreaShape.Diamond)return false;
            if(Active(unit,"cless",GrowthPath.Assault))return skill==unit.Data.BasicAttack&&skill.Target==TargetType.Enemy&&skill.Effects.Any(e=>e.Kind==EffectKind.Damage&&!e.Magic&&!e.AffectCaster);
            return (Active(unit,"mint",GrowthPath.Assault)||Active(unit,"mint",GrowthPath.Bulwark))&&skill.Target==TargetType.Ally&&skill.Effects.Any(e=>e.Kind==EffectKind.Heal&&!e.AffectCaster);
        }
        public static string ActionDescription(UnitRuntime unit,SkillData skill)=>HasActionEffect(unit,skill)?"성장 · "+Role(unit.Data,unit.Growth)+"\n"+CombatDescription(unit.Data,unit.Growth):"";
        public static Stats Apply(Stats s,GrowthPath path)
        {
            float attack=path==GrowthPath.Assault?1.12f:path==GrowthPath.Bulwark?.92f:1;
            float defense=path==GrowthPath.Bulwark?1.12f:path==GrowthPath.Assault?.92f:1;
            s.STR=Mathf.RoundToInt(s.STR*attack);s.MAG=Mathf.RoundToInt(s.MAG*attack);
            s.DEF=Mathf.RoundToInt(s.DEF*defense);s.MDF=Mathf.RoundToInt(s.MDF*defense);return s;
        }
        public static bool Save(CampaignSave save,CharacterData character,GrowthPath choice,Func<CampaignSave,bool> persist)
        {
            var progress=save.Characters.Find(p=>p.Id==character.Id);
            if(progress==null||progress.Level<10||!Enum.IsDefined(typeof(GrowthPath),choice)||persist==null)return false;
            var previous=progress.Growth;progress.Growth=choice;bool saved=false;
            try{return saved=persist(save);}catch(Exception){return false;}
            finally{if(!saved)progress.Growth=previous;}
        }
    }
}
