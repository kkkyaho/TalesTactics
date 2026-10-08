using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    [Serializable] public sealed class FormationPreset
    {
        public string[] Members;
        public List<PresetEquipment> Equipment=new List<PresetEquipment>();
    }
    [Serializable] public sealed class PresetEquipment {public string Character;public string[] Items;}
    public static class FormationPresets
    {
        public static bool Store(CampaignSave save,BattleCatalog catalog,int[] deployment,int slot,Func<CampaignSave,bool> persist)
        {
            if(slot<0||slot>2||deployment==null||deployment.Length<1||deployment.Length>catalog.Rules.MaxDeployment||deployment.Distinct().Count()!=deployment.Length||deployment.Any(i=>i<0||i>=catalog.Characters.Length))return false;
            var previous=save.Presets;var next=new FormationPreset[3];if(previous!=null)Array.Copy(previous,next,Math.Min(3,previous.Length));
            next[slot]=new FormationPreset{Members=deployment.Select(i=>catalog.Characters[i].Id).ToArray(),Equipment=catalog.Characters.Select(c=>new PresetEquipment{Character=c.Id,Items=(save.Characters.Find(p=>p.Id==c.Id)?.Equipment??new string[3]).ToArray()}).ToList()};
            bool saved=false;save.Presets=next;
            try{return saved=persist!=null&&persist(save);}catch(Exception){return false;}
            finally{if(!saved)save.Presets=previous;}
        }
        public static string Apply(CampaignSave save,BattleCatalog catalog,int slot,Func<CampaignSave,bool> persist,out int[] deployment)
        {
            deployment=null;var preset=save.Presets!=null&&slot>=0&&slot<save.Presets.Length?save.Presets[slot]:null;
            if(preset?.Members==null||preset.Equipment==null)return "저장된 프리셋이 없습니다.";
            var members=preset.Members.Select(id=>Array.FindIndex(catalog.Characters,c=>c.Id==id)).ToArray();
            if(members.Length<1||members.Length>catalog.Rules.MaxDeployment||members.Any(i=>i<0)||members.Distinct().Count()!=members.Length)return "편성 캐릭터를 확인할 수 없습니다.";
            var draft=JsonUtility.FromJson<CampaignSave>(JsonUtility.ToJson(save));var seen=new HashSet<string>();
            foreach(var entry in preset.Equipment)
            {
                var character=catalog.Characters.FirstOrDefault(c=>c.Id==entry?.Character);
                if(character==null||!seen.Add(character.Id)||entry.Items==null||entry.Items.Length!=3)return "장비 프리셋이 올바르지 않습니다.";
                for(int i=0;i<3;i++)
                {
                    if(string.IsNullOrEmpty(entry.Items[i]))continue;
                    var item=catalog.Equipment.FirstOrDefault(e=>e!=null&&e.Id==entry.Items[i]);
                    if(item==null||(int)item.Slot!=i||i==0&&item.Weapon!=character.Weapon)return "호환되지 않는 장비가 있습니다.";
                }
                draft.Get(character.Id).Equipment=entry.Items.ToArray();
            }
            try{CampaignInventory.Validate(draft);}catch(Exception){return "장비 수량이 부족합니다. 구매 또는 장착 상태를 확인하세요.";}
            var oldCharacters=save.Characters;var oldDeployment=save.Deployment;bool saved=false;
            save.Characters=draft.Characters;save.Deployment=members;
            try{saved=persist!=null&&persist(save);}catch(Exception){}
            if(!saved){save.Characters=oldCharacters;save.Deployment=oldDeployment;return "저장 실패. 기존 편성과 장비를 유지합니다.";}
            deployment=members;return null;
        }
    }
}
