using System;
using System.Linq;
namespace TalesTactics
{
    // A detached draft. Availability excludes this character's committed slots,
    // so swapping/unequipping does not temporarily consume or duplicate inventory.
    public sealed class EquipmentLoadout
    {
        public readonly CharacterData Character;
        readonly EquipmentData[] catalog;
        readonly CampaignSave campaign;
        readonly EquipmentData[] slots=new EquipmentData[3];
        public EquipmentLoadout(CharacterData character,CharacterProgress progress,EquipmentData[] equipment,CampaignSave campaign=null)
        {
            Character=character;catalog=equipment??Array.Empty<EquipmentData>();this.campaign=campaign;
            for(int i=0;i<slots.Length;i++)
            {
                string id=progress?.Equipment!=null&&i<progress.Equipment.Length?progress.Equipment[i]:null;
                slots[i]=Options((EquipmentSlot)i).FirstOrDefault(e=>e.Id==id);
            }
        }
        public EquipmentData Get(EquipmentSlot slot)=>slots[(int)slot];
        public EquipmentData[] Options(EquipmentSlot slot)=>catalog.Where(e=>e!=null&&!string.IsNullOrEmpty(e.Id)&&e.Slot==slot&&(slot!=EquipmentSlot.Weapon||e.Weapon==Character.Weapon)&&
            (campaign==null||CampaignInventory.Available(campaign,e.Id,Character.Id)>0)).ToArray();
        public bool Equip(EquipmentSlot slot,EquipmentData item)
        {
            int index=(int)slot;if(index<0||index>=slots.Length)return false;
            if(item!=null&&!Options(slot).Contains(item))return false;
            slots[index]=item;return true;
        }
        public void Cycle(EquipmentSlot slot)
        {
            var options=Options(slot);int index=Array.IndexOf(options,Get(slot));
            Equip(slot,index+1<options.Length?options[index+1]:null);
        }
        public string[] ExportIDs()=>slots.Select(e=>e!=null?e.Id:null).ToArray();
        public Stats Preview(int level,bool promoted)
        {var stats=Character.StatsAt(level,promoted);foreach(var item in slots)if(item!=null)stats+=item.Bonus;return stats;}
        public void Apply(UnitRuntime unit)
        {for(int i=0;i<slots.Length;i++)unit.Equipment[i]=slots[i];}
        public bool CanCommit(CampaignSave save)=>slots.Where(e=>e!=null).GroupBy(e=>e.Id)
            .All(g=>g.Count()<=CampaignInventory.Available(save,g.Key,Character.Id));
        public bool TrySave(CampaignSave save,Func<CampaignSave,bool> persist)
        {
            if(!CanCommit(save)||persist==null)return false;
            var existing=save.Characters.Find(c=>c.Id==Character.Id);
            var progress=save.Get(Character.Id);var previous=progress.Equipment;
            bool saved=false;progress.Equipment=ExportIDs();
            try{return saved=persist(save);}
            finally{if(!saved){progress.Equipment=previous;if(existing==null)save.Characters.Remove(progress);}}
        }
    }
}
