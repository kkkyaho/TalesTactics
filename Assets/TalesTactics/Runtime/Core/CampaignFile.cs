using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace TalesTactics
{
    // One storage instance owns one path, so tests never touch the player's campaign.
    public sealed class CampaignFile
    {
        [Serializable] sealed class Header { public int Version=0,Gold=-1; public List<OwnedEquipment> Inventory; }
        sealed class FutureVersion : Exception { }
        readonly string path;
        bool loaded, recovered;
        public bool CanSave {get;private set;}=true;
        public string Notice {get;private set;}="";
        public CampaignFile(string path){this.path=path;}
        static CampaignSave Read(string file)
        {
            string json=File.ReadAllText(file);
            var header=JsonUtility.FromJson<Header>(json);
            if(header!=null&&header.Version>2)throw new FutureVersion();
            if(header==null||(header.Version!=1&&header.Version!=2))throw new InvalidDataException("Missing or unsupported save version");
            if(header.Version==2&&(header.Gold<0||header.Inventory==null))throw new InvalidDataException("Missing economy fields");
            var save=JsonUtility.FromJson<CampaignSave>(json);
            if(save==null)throw new InvalidDataException("Empty campaign");
            if(save.SuspendedBattle!=null&&save.SuspendedBattle.Version>5)throw new FutureVersion();
            if(!save.HasSuspendedBattle)save.SuspendedBattle=null; // JsonUtility recreates inline null classes.
            Normalize(save);return save;
        }
        public static void Normalize(CampaignSave save)
        {
            if(save.Version!=1&&save.Version!=2)throw new InvalidDataException("Unsupported save version");
            if(save.Characters==null)save.Characters=new List<CharacterProgress>();
            if(save.StoryProgress==null)save.StoryProgress=new List<string>();
            var ids=new HashSet<string>();
            foreach(var c in save.Characters)
            {
                if(c==null||string.IsNullOrWhiteSpace(c.Id)||!ids.Add(c.Id))throw new InvalidDataException("Invalid or duplicate character ID");
                c.Level=Mathf.Clamp(c.Level,1,50);c.EXP=Mathf.Max(0,c.EXP);c.AddExperience(0);
                if(c.Equipment==null)c.Equipment=new string[3];
                else if(c.Equipment.Length!=3)Array.Resize(ref c.Equipment,3);
                if(c.UnlockedSkills==null)c.UnlockedSkills=new List<string>();
                if(!Enum.IsDefined(typeof(GrowthPath),c.Growth)||!Enum.IsDefined(typeof(TacticalTrait),c.Trait))throw new InvalidDataException("Unknown tactical trait");
            }
            save.StoryProgress.RemoveAll(string.IsNullOrWhiteSpace);
            if(save.Version==1)CampaignInventory.MigrateVersion1(save);
            CampaignInventory.Validate(save);
        }
        public CampaignSave Load()
        {
            loaded=true;recovered=false;CanSave=true;Notice="";
            try
            {
                if(File.Exists(path))return Read(path);
                if(!File.Exists(path+".bak"))return new CampaignSave();
            }
            catch(FutureVersion){return Block("더 새로운 버전의 저장 파일입니다. 원본 보호를 위해 저장을 차단했습니다.");}
            catch(Exception){ }
            try
            {
                var save=Read(path+".bak");recovered=true;
                Notice="백업에서 저장 데이터를 복구했습니다. 다음 저장 시 손상 원본을 별도 보존합니다.";
                return save;
            }
            catch(Exception){return Block("저장 파일을 읽지 못했습니다. 원본 보호를 위해 저장을 차단했습니다.");}
        }
        CampaignSave Block(string message){CanSave=false;Notice=message;return new CampaignSave();}
        public bool Save(CampaignSave save)
        {
            if(!loaded)Load();
            if(!CanSave)return false;
            try
            {
                Normalize(save);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                // Preserve the unreadable primary without replacing the known-good backup.
                if(recovered&&File.Exists(path))File.Copy(path,path+".corrupt-"+Guid.NewGuid().ToString("N"));
                string previousPath=recovered?path+".bak":path;
                if(File.Exists(previousPath)&&!File.Exists(path+".v1.bak"))
                {
                    var oldHeader=JsonUtility.FromJson<Header>(File.ReadAllText(previousPath));
                    if(oldHeader!=null&&oldHeader.Version==1)File.Copy(previousPath,path+".v1.bak");
                }
                save.HasSuspendedBattle=save.SuspendedBattle!=null;
                File.WriteAllText(path+".tmp",JsonUtility.ToJson(save,true));
                if(File.Exists(path))File.Replace(path+".tmp",path,recovered?null:path+".bak");
                else File.Move(path+".tmp",path);
                recovered=false;Notice="";return true;
            }
            catch(Exception e){Notice="저장 실패. 기존 파일을 유지했습니다.";Debug.LogWarning("Campaign save failed: "+e.Message);return false;}
        }
    }
}
