using System;
using System.IO;
using UnityEngine;
namespace TalesTactics
{
    [Serializable] public sealed class PlayerPreferences
    {
        public int Version=1,Slot,Width=1280,Height=800;
        public bool Fullscreen,CT,Utility,AutoTiming,FixedSpeedOrder;
        public float Music=.28f,Effects=.45f,CursorSpeed=1;
        public int EnemySpeedMode; // 0=1x, 1=2x, 2=4x; absent in older settings means normal.
        public bool SkipEnemyAnimations;
        public BattleDifficulty Difficulty;
        public bool MissionEvents;
        public float TextScale=1;
        public void Validate()
        {
            if(!Enum.IsDefined(typeof(BattleDifficulty),Difficulty))throw new InvalidDataException("Unsupported difficulty");
            if(EnemySpeedMode<0||EnemySpeedMode>2)throw new InvalidDataException("Unsupported enemy speed");
            if(float.IsNaN(TextScale)||TextScale<1||TextScale>1.3f)throw new InvalidDataException("Unsupported text size");
            if(Version!=1||Slot<0||Slot>2||Width<640||Width>7680||Height<480||Height>4320||float.IsNaN(Music)||float.IsNaN(Effects)||float.IsNaN(CursorSpeed)||Music<0||Music>1||Effects<0||Effects>1||CursorSpeed<.5f||CursorSpeed>2)throw new InvalidDataException("Unsupported settings");
        }
    }
    public sealed class PreferenceFile
    {
        readonly string path;public string Notice {get;private set;}="";public bool Writable {get;private set;}=true;
        public PreferenceFile(string path){this.path=path;}
        public PlayerPreferences Load()
        {
            if(!File.Exists(path))return new PlayerPreferences();
            try{var value=JsonUtility.FromJson<PlayerPreferences>(File.ReadAllText(path));if(value==null)throw new InvalidDataException();value.Validate();return value;}
            catch{Writable=false;Notice="설정 파일을 읽지 못해 기본값을 사용합니다. 원본은 보존합니다.";return new PlayerPreferences();}
        }
        public bool Save(PlayerPreferences value)
        {
            if(!Writable)return false;
            try{value.Validate();Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));File.WriteAllText(path+".tmp",JsonUtility.ToJson(value,true));if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");else File.Move(path+".tmp",path);Notice="설정을 저장했습니다.";return true;}
            catch{Notice="설정 저장 실패. 이전 설정을 유지합니다.";return false;}
        }
    }
    public sealed class CampaignProfiles
    {
        public readonly string Root;public int Slot {get;private set;}public CampaignFile Store {get;private set;}
        public CampaignProfiles(string root){Root=root;}
        public string PathFor(int slot){if(slot<0||slot>2)throw new ArgumentOutOfRangeException(nameof(slot));return Path.Combine(Root,slot==0?"campaign.json":"campaign-slot"+(slot+1)+".json");}
        public CampaignSave Load(int slot){var store=new CampaignFile(PathFor(slot));var save=store.Load();Slot=slot;Store=store;return save;}
        public string Describe(int slot)
        {
            var path=PathFor(slot);if(!File.Exists(path)&&!File.Exists(path+".bak"))return "빈 슬롯 · 새 캠페인";
            var store=new CampaignFile(path);var save=store.Load();if(!store.CanSave)return store.Notice;
            string stamp=DateTime.TryParse(save.SavedAt,out var time)?time.ToLocalTime().ToString("yyyy-MM-dd HH:mm"):"이전 버전 저장";
            return stamp+" · "+save.Gold+" G · 완료 "+save.StoryProgress.Count+"장"+(save.SuspendedBattle!=null?" · 중단 전투 있음":"")+(string.IsNullOrEmpty(store.Notice)?"":" · 백업 복구");
        }
    }
}
