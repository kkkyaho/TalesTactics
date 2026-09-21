using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace TalesTactics
{
    [Serializable] public class CharacterProgress
    {
        public string Id; public int Level=1, EXP; public bool Promoted;
        public string[] Equipment=new string[3];public List<string> UnlockedSkills=new List<string>();
        public void AddExperience(int amount){EXP+=Mathf.Max(0,amount);while(Level<50&&EXP>=Level*100){EXP-=Level*100;Level++;}if(Level==50)EXP=0;}
        public bool TryPromote(CharacterData data,List<string> flags){if(Promoted||Level<data.PromotionLevel||!flags.Contains(data.PromotionStoryFlag))return false;Promoted=true;return true;}
    }
    [Serializable] public class CampaignSave
    {
        public int Version=1;public bool AutoTiming;
        public List<string> StoryProgress=new List<string>();public List<CharacterProgress> Characters=new List<CharacterProgress>();
        public CharacterProgress Get(string id){var c=Characters.Find(x=>x.Id==id);if(c==null){c=new CharacterProgress{Id=id};Characters.Add(c);}return c;}
    }
    public static class CampaignStorage
    {
        public static string PathName=>Path.Combine(Application.persistentDataPath,"campaign.json");
        public static CampaignSave Load()
        {
            try{if(!File.Exists(PathName))return new CampaignSave();var save=JsonUtility.FromJson<CampaignSave>(File.ReadAllText(PathName));if(save==null||save.Version!=1||save.Characters==null||save.StoryProgress==null)throw new InvalidDataException("Unsupported save");return save;}
            catch(Exception e){Debug.LogWarning("Campaign load failed; original file preserved: "+e.Message);return new CampaignSave();}
        }
        public static bool Save(CampaignSave save)
        {
            try{var path=PathName;Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path+".tmp",JsonUtility.ToJson(save,true));if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");else File.Move(path+".tmp",path);return true;}
            catch(Exception e){Debug.LogWarning("Campaign save failed: "+e.Message);return false;}
        }
    }
}
