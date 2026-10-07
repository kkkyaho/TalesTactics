using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleDirector
    {
        public CampaignProfiles Profiles {get;private set;}
        public PlayerPreferences Preferences {get;private set;}
        PreferenceFile preferenceFile;
        public string SaveNotice {get;private set;}="";
        public bool CanSave=>Profiles==null||Profiles.Store.CanSave;
        public bool CanSuspend=>IsPlayerCommand&&!battleTraining&&!TutorialActive&&!StoryActive&&!TimingActive&&!RewardPending&&Session.Result==BattleResult.Ongoing;
        public void ConfigureStorage(string root,bool applyDisplay=false)
        {
            preferenceFile=new PreferenceFile(Path.Combine(root,"settings.json"));Preferences=preferenceFile.Load();
            Profiles=new CampaignProfiles(root);Campaign=Profiles.Load(Preferences.Slot);PersistCampaign=SaveProgress;
            SaveNotice=string.IsNullOrEmpty(preferenceFile.Notice)?Profiles.Store.Notice:preferenceFile.Notice;
            if(!File.Exists(Path.Combine(root,"settings.json")))Preferences.AutoTiming=Campaign.AutoTiming;
            Campaign.AutoTiming=Preferences.AutoTiming;RestorePreparation();ApplyPreferences(applyDisplay&&File.Exists(Path.Combine(root,"settings.json")));
        }
        void RestorePreparation()
        {
            Deployment.Clear();Deployment.AddRange(TacticalDevelopment.Recommended(Catalog));
            SelectedStage=Mathf.Clamp(Campaign.SelectedStage,0,CampaignStages.Count-1);
            if(!CampaignStages.Unlocked(Campaign,SelectedStage))SelectedStage=0;
            if(Campaign.Deployment!=null&&Campaign.Deployment.Length>0&&Campaign.Deployment.Length<=Catalog.Rules.MaxDeployment&&Campaign.Deployment.Distinct().Count()==Campaign.Deployment.Length&&Campaign.Deployment.All(i=>i>=0&&i<Catalog.Characters.Length)){Deployment.Clear();Deployment.AddRange(Campaign.Deployment);}
        }
        bool SaveProgress(CampaignSave save)=>WriteProgress(save,false);
        bool WriteProgress(CampaignSave save,bool preserveCheckpoint)
        {
            var previous=save.SuspendedBattle;bool hadCheckpoint=save.HasSuspendedBattle;var timestamp=save.SavedAt;var deployment=save.Deployment;int stage=save.SelectedStage;
            if(!preserveCheckpoint)save.SuspendedBattle=null;
            save.SavedAt=DateTime.UtcNow.ToString("o");save.Deployment=Deployment.ToArray();save.SelectedStage=SelectedStage;
            if(Profiles.Store.Save(save)){SaveNotice="저장 완료 · 슬롯 "+(Profiles.Slot+1)+" · "+DateTime.Now.ToString("HH:mm:ss");return true;}
            save.SuspendedBattle=previous;save.HasSuspendedBattle=hadCheckpoint;save.SavedAt=timestamp;save.Deployment=deployment;save.SelectedStage=stage;
            SaveNotice=Profiles.Store.Notice;return false;
        }
        public bool SavePreparation()
        {
            if(Session!=null||StoryActive)return false;
            return WriteProgress(Campaign,true);
        }
        public bool SelectProfile(int slot)
        {
            if(Session!=null||StoryActive)return false;
            var draft=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(Preferences));draft.Slot=slot;
            if(!SavePreferences(draft,false))return false;
            Campaign=Profiles.Load(slot);Campaign.AutoTiming=Preferences.AutoTiming;SaveNotice=Profiles.Store.Notice;RestorePreparation();Hud.ShowDeployment();return true;
        }
        public bool SuspendBattle()
        {
            if(!CanSuspend)return false;
            var old=Campaign.SuspendedBattle;
            Campaign.SuspendedBattle=BattleCheckpoint.Capture(Session,Session.Units.Where(u=>u.Team==Team.Player).Select(u=>Array.IndexOf(Catalog.Characters,u.Data)).ToArray());
            if(!WriteProgress(Campaign,true)){Campaign.SuspendedBattle=old;return false;}
            Restart();SaveNotice="전투 중단 저장 완료 · 이어하기로 같은 턴을 재개합니다.";Hud.ShowDeployment();return true;
        }
        public bool ResumeBattle()
        {
            if(Session!=null||StoryActive||Campaign.SuspendedBattle==null)return false;
            try
            {
                var checkpoint=Campaign.SuspendedBattle;var restored=checkpoint.Restore(Catalog);
                // Consume the checkpoint atomically before play, preventing repeat rewards from an old checkpoint.
                if(!WriteProgress(Campaign,false))return false;
                Session=restored;battleStage=checkpoint.Stage;battleTraining=false;completed=false;RewardPending=false;pendingReward=null;
                SelectedStage=checkpoint.Stage;TrainingMode=false;
                Deployment.Clear();Deployment.AddRange(checkpoint.Deployment);UnityEngine.Random.state=checkpoint.RandomState;
                Hud.CloseSystemMenu();Board.Build(Session);Audio.PlayBattle(CampaignStages.Get(battleStage).BossMusic);Message="중단한 전투를 이어갑니다. 종료 전 다시 중단 저장하세요.";SetState(new CommandState(this));return true;
            }
            catch(Exception){SaveNotice="중단 전투가 손상되었거나 호환되지 않습니다. 원본을 유지했습니다.";return false;}
        }
        public bool SavePreferences(PlayerPreferences draft,bool display=true)
        {
            if(!preferenceFile.Save(draft)){SaveNotice=preferenceFile.Notice;return false;}
            Preferences=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(draft));Campaign.AutoTiming=draft.AutoTiming;ApplyPreferences(display);SaveNotice="설정을 저장했습니다.";return true;
        }
        public void SaveBattleOptions()
        {
            var draft=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(Preferences));draft.CT=UseCT;draft.Utility=UseUtilityAI;draft.AutoTiming=Campaign.AutoTiming;
            if(!SavePreferences(draft,false)){UseCT=Preferences.CT;UseUtilityAI=Preferences.Utility;Campaign.AutoTiming=Preferences.AutoTiming;}
        }
        void ApplyPreferences(bool display)
        {
            UseCT=Preferences.CT;UseUtilityAI=Preferences.Utility;
            Audio.SetVolumes(Preferences.Music,Preferences.Effects);
            if(display&&!Application.isEditor)Screen.SetResolution(Preferences.Width,Preferences.Height,Preferences.Fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
        }
    }
}
