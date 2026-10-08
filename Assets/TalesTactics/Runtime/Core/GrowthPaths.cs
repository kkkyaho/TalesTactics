using System;
using UnityEngine;
namespace TalesTactics
{
    public enum GrowthPath { Unselected, Assault, Bulwark }
    public static class GrowthPaths
    {
        public static string Name(GrowthPath path)=>path==GrowthPath.Assault?"돌파":path==GrowthPath.Bulwark?"수호":"미선택";
        public static string Description(GrowthPath path)=>path==GrowthPath.Assault?"공격·마력 +12% / 방어·마방 -8%":path==GrowthPath.Bulwark?"방어·마방 +12% / 공격·마력 -8%":"기본 능력치 유지";
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
