using System;
using System.Linq;
namespace TalesTactics
{
    // A detached draft: browsing and cycling equipment never changes the campaign.
    public sealed class EquipmentLoadout
    {
        public readonly CharacterData Character;
        readonly EquipmentData[] catalog;
        readonly EquipmentData[] slots=new EquipmentData[3];
        public EquipmentLoadout(CharacterData character,CharacterProgress progress,EquipmentData[] equipment)
        {
            Character=character;catalog=equipment??Array.Empty<EquipmentData>();
            for(int i=0;i<slots.Length;i++)
            {
                string id=progress?.Equipment!=null&&i<progress.Equipment.Length?progress.Equipment[i]:null;
                slots[i]=Options((EquipmentSlot)i).FirstOrDefault(e=>e.Id==id);
            }
        }
        public EquipmentData Get(EquipmentSlot slot)=>slots[(int)slot];
        public EquipmentData[] Options(EquipmentSlot slot)=>catalog.Where(e=>e!=null&&!string.IsNullOrEmpty(e.Id)&&e.Slot==slot&&(slot!=EquipmentSlot.Weapon||e.Weapon==Character.Weapon)).ToArray();
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
        public bool TrySave(CampaignSave campaign,Func<CampaignSave,bool> persist)
        {
            var progress=campaign.Get(Character.Id);var previous=progress.Equipment;
            bool saved=false;progress.Equipment=ExportIDs();
            try{return saved=persist(campaign);}
            finally{if(!saved)progress.Equipment=previous;}
        }
    }
}
