using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace TalesTactics
{
    [Serializable] public class CharacterProgress
    {
        public string Id; public int Level=1, EXP; public bool Promoted;
        public TacticalTrait Trait;
        public GrowthPath Growth;
        public string[] Equipment=new string[3];public List<string> UnlockedSkills=new List<string>();
        public void AddExperience(int amount)=>ExperienceRules.Advance(ref Level,ref EXP,amount);
        public bool TryPromote(CharacterData data,List<string> flags){if(Promoted||Level<data.PromotionLevel||!CampaignPromotion.StorySatisfied(data,flags))return false;Promoted=true;return true;}
    }
    [Serializable] public class CampaignSave
    {
        public int Version=3;public bool AutoTiming;
        public BattleCheckpoint SuspendedBattle;
        public bool HasSuspendedBattle;
        public string SavedAt;
        public int SelectedStage;
        public bool HasPendingAfterStory;
        public int PendingAfterStoryStage=-1,PendingAfterStoryLine;
        public int[] Deployment;
        public FormationPreset[] Presets=new FormationPreset[3];
        public int Gold=CampaignInventory.StartingGold;
        public List<OwnedEquipment> Inventory=CampaignInventory.StartingItems();
        public List<string> StoryProgress=new List<string>();public List<CharacterProgress> Characters=new List<CharacterProgress>();
        public CharacterProgress Get(string id){var c=Characters.Find(x=>x.Id==id);if(c==null){c=new CharacterProgress{Id=id};Characters.Add(c);}return c;}
    }
    public static class CampaignStorage
    {
        public static string PathName=>Path.Combine(Application.persistentDataPath,"campaign.json");
        static CampaignFile file;
        static CampaignFile FileStore=>file??(file=new CampaignFile(PathName));
        public static string Notice=>FileStore.Notice;
        public static bool CanSave=>FileStore.CanSave;
        public static CampaignSave Load()=>FileStore.Load();
        public static bool Save(CampaignSave save)=>FileStore.Save(save);
    }
}
