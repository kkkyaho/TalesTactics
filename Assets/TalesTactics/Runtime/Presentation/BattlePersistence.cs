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
        // Historical stage IDs remain readable for save compatibility, but are not playable in the current campaign.
        public bool CanPlayCampaignStage(int stage)=>stage>=0&&stage<CampaignStages.Count&&(Catalog.CrossoverReady?stage>=CrossoverCampaign.FirstStage:stage<CampaignStages.LegacyCount);
        public bool CanResumeCampaign=>Campaign.SuspendedBattle!=null&&CanPlayCampaignStage(Campaign.SuspendedBattle.Stage);
        public bool CanSuspend=>IsPlayerCommand&&!battleTraining&&!TutorialActive&&!StoryActive&&!TimingActive&&!RewardPending&&Session.Result==BattleResult.Ongoing;
        public void ConfigureStorage(string root,bool applyDisplay=false,bool? mobilePlatform=null)
        {
            ConfigureMobilePersistence(mobilePlatform??Application.isMobilePlatform);
            string settingsPath=Path.Combine(root,MobilePersistenceEnabled?"settings-mobile.json":"settings.json");
            preferenceFile=new PreferenceFile(settingsPath);Preferences=preferenceFile.Load();
            Profiles=new CampaignProfiles(root);Campaign=Profiles.Load(Preferences.Slot);PersistCampaign=SaveProgress;
            SaveNotice=string.IsNullOrEmpty(preferenceFile.Notice)?Profiles.Store.Notice:preferenceFile.Notice;
            if(!File.Exists(settingsPath))Preferences.AutoTiming=Campaign.AutoTiming;
            Campaign.AutoTiming=Preferences.AutoTiming;RestorePreparation();ApplyPreferences(applyDisplay&&File.Exists(settingsPath));
        }
        void RestorePreparation()
        {
            Deployment.Clear();Deployment.AddRange(TacticalDevelopment.Recommended(Catalog));
            SelectedStage=Mathf.Clamp(Campaign.SelectedStage,0,CampaignStages.Count-1);
            if(!CanPlayCampaignStage(SelectedStage)||!CampaignStages.Unlocked(Campaign,SelectedStage))SelectedStage=Catalog.CrossoverReady?CrossoverCampaign.FirstStage:0;
            if(Campaign.Deployment!=null&&Campaign.Deployment.Length>0&&Campaign.Deployment.Length<=Catalog.Rules.MaxDeployment&&Campaign.Deployment.Distinct().Count()==Campaign.Deployment.Length&&Campaign.Deployment.All(i=>i>=0&&i<Catalog.Characters.Length)){Deployment.Clear();Deployment.AddRange(Campaign.Deployment);}
            if(Catalog.CrossoverReady&&Campaign.SuspendedBattle!=null&&!CanResumeCampaign)SaveNotice="기존 6장 캠페인은 종료되었습니다. 성장·장비를 유지하며 새 메인 1화부터 진행할 수 있습니다.";
            NormalizeDeployment();
        }
        bool SaveProgress(CampaignSave save)=>WriteProgress(save,false);
        bool WriteProgress(CampaignSave save,bool preserveCheckpoint)
        {
            var previous=save.SuspendedBattle;bool hadCheckpoint=save.HasSuspendedBattle;var timestamp=save.SavedAt;var deployment=save.Deployment;int stage=save.SelectedStage;
            save.SavedAt=DateTime.UtcNow.ToString("o");save.Deployment=Deployment.ToArray();save.SelectedStage=SelectedStage;
            bool saved=preserveCheckpoint?Profiles.Store.Save(save):CampaignRecruitment.TryConsumeCheckpoint(save,Profiles.Store.Save);
            if(saved){if(!preserveCheckpoint)ForgetMobileCheckpoint();MobileSaveNotice="";SaveNotice="저장 완료 · 슬롯 "+(Profiles.Slot+1)+" · "+DateTime.Now.ToString("HH:mm:ss");return true;}
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
            Campaign=Profiles.Load(slot);Campaign.AutoTiming=Preferences.AutoTiming;SaveNotice=Profiles.Store.Notice;RestorePreparation();Hud.ShowIntermissionHub();return true;
        }
        public bool SuspendBattle()
        {
            if(!CanSuspend)return false;
            var old=Campaign.SuspendedBattle;
            Campaign.SuspendedBattle=BattleCheckpoint.Capture(Session,Session.Units.Where(u=>u.Team==Team.Player).Select(u=>Array.IndexOf(Catalog.Characters,u.Data)).ToArray());
            if(!WriteProgress(Campaign,true)){Campaign.SuspendedBattle=old;return false;}
            Restart();SaveNotice="전투 중단 저장 완료 · 이어하기로 같은 턴을 재개합니다.";Hud.ShowIntermissionHub();return true;
        }
        public bool ResumeBattle()
        {
            if(Session!=null||StoryActive||Campaign.SuspendedBattle==null)return false;
            if(!CanResumeCampaign){SaveNotice="종료된 기존 캠페인의 중단 전투입니다. 성장·장비와 원본 저장은 유지했습니다.";return false;}
            try
            {
                var checkpoint=Campaign.SuspendedBattle;var restored=checkpoint.Restore(Catalog);
                // Desktop consumes a manual checkpoint before play. Mobile keeps its recovery point
                // until the next command save; the normal reward transaction still consumes it atomically.
                if(!WriteProgress(Campaign,MobilePersistenceEnabled))return false;
                Session=restored;battleStage=checkpoint.Stage;battleTraining=false;completed=false;RewardPending=false;pendingReward=null;
                SelectedStage=checkpoint.Stage;TrainingMode=false;
                Deployment.Clear();Deployment.AddRange(checkpoint.Deployment);UnityEngine.Random.state=checkpoint.RandomState;
                Hud.CloseSystemMenu();Board.Build(Session);Audio.PlayBattle(CampaignStages.Get(battleStage).BossMusic);SetState(new CommandState(this));Message=MobilePersistenceEnabled?"중단한 전투를 이어갑니다. 아군 명령 시 자동 저장합니다.":"중단한 전투를 이어갑니다. 종료 전 다시 중단 저장하세요.";Hud.UpdateBattleMessageText();return true;
            }
            catch(Exception){SaveNotice="중단 전투가 손상되었거나 호환되지 않습니다. 원본을 유지했습니다.";return false;}
        }
        public bool SavePreferences(PlayerPreferences draft,bool display=true)
        {
            MergeContextHints(draft);
            if(!preferenceFile.Save(draft)){SaveNotice=preferenceFile.Notice;return false;}
            Preferences=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(draft));Campaign.AutoTiming=draft.AutoTiming;ApplyPreferences(display);SaveNotice="설정을 저장했습니다.";return true;
        }
        public void SaveBattleOptions()
        {
            var draft=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(Preferences));draft.CT=UseCT;draft.FixedSpeedOrder=UseFixedSpeedOrder;draft.Utility=UseUtilityAI;draft.AutoTiming=Campaign.AutoTiming;
            if(!SavePreferences(draft,false)){UseCT=Preferences.CT;UseFixedSpeedOrder=Preferences.FixedSpeedOrder;UseUtilityAI=Preferences.Utility;Campaign.AutoTiming=Preferences.AutoTiming;}
        }
        void ApplyPreferences(bool display)
        {
            UseCT=Preferences.CT;UseFixedSpeedOrder=Preferences.FixedSpeedOrder;UseUtilityAI=Preferences.Utility;
            Audio.SetVolumes(Preferences.Music,Preferences.Effects);
            if(display&&UsesDesktopDisplaySettings&&!Application.isEditor){Screen.SetResolution(Preferences.Width,Preferences.Height,Preferences.Fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);GetComponent<WindowDisplayController>()?.DisplayApplied();}
        }
    }
}
