using System;
using System.IO;
using System.Linq;
namespace TalesTactics
{
    public sealed class TitleSlot
    {
        public int Index;
        public bool Exists,Writable,Resumable;
        public CampaignSave Save;
        public string Notice;
    }
    public sealed partial class BattleDirector
    {
        public TitleSlot[] ReadTitleSlots()=>Enumerable.Range(0,3).Select(ReadTitleSlot).ToArray();
        TitleSlot ReadTitleSlot(int slot)
        {
            var path=Profiles.PathFor(slot);var file=new CampaignFile(path);var save=file.Load();
            return new TitleSlot{Index=slot,Exists=File.Exists(path)||File.Exists(path+".bak"),Writable=file.CanSave,Save=save,Notice=file.Notice,
                Resumable=file.CanSave&&save.SuspendedBattle!=null&&CanPlayCampaignStage(save.SuspendedBattle.Stage)};
        }
        public bool StartTitleGame(int slot)
        {
            if(!Hud.TitleOpen||Session!=null||StoryActive||slot<0||slot>2)return false;
            var candidate=new CampaignProfiles(Profiles.Root);candidate.Load(slot);
            if(!candidate.Store.CanSave){SaveNotice=candidate.Store.Notice;return false;}
            int stage=Catalog.CrossoverReady?CrossoverCampaign.FirstStage:0;
            var fresh=new CampaignSave{SelectedStage=stage,AutoTiming=Preferences.AutoTiming,SavedAt=DateTime.UtcNow.ToString("o"),
                Deployment=Catalog.CrossoverReady?Enumerable.Range(0,Catalog.Characters.Length).Where(i=>CrossoverCampaign.Joined(Catalog.Characters[i].Id,1)).ToArray():TacticalDevelopment.Recommended(Catalog).ToArray()};
            // Commit before replacing the active campaign; CampaignFile preserves the previous good save as .bak.
            if(!candidate.Store.Save(fresh)){SaveNotice=candidate.Store.Notice;return false;}
            Profiles=candidate;Campaign=fresh;TrainingMode=false;SaveNotice="새 게임을 저장했습니다 · 슬롯 "+(slot+1);RestorePreparation();RememberTitleSlot(slot);
            Hud.HideTitle();Hud.ShowIntermissionHub();return true;
        }
        public bool LoadTitleGame(int slot,bool resume)
        {
            if(!Hud.TitleOpen||Session!=null||StoryActive||slot<0||slot>2)return false;
            var info=ReadTitleSlot(slot);
            if(!info.Exists||!info.Writable){SaveNotice=info.Exists?info.Notice:"이 슬롯에는 저장된 게임이 없습니다.";return false;}
            var candidate=new CampaignProfiles(Profiles.Root);var save=candidate.Load(slot);
            if(!candidate.Store.CanSave){SaveNotice=candidate.Store.Notice;return false;}
            if(resume&&(save.SuspendedBattle==null||!CanPlayCampaignStage(save.SuspendedBattle.Stage)))
            {SaveNotice="이 저장에서는 중단 전투를 재개할 수 없습니다. 거점으로 불러오세요.";return false;}
            var oldProfiles=Profiles;var oldCampaign=Campaign;int oldStage=SelectedStage;var oldDeployment=Deployment.ToArray();bool oldTraining=TrainingMode;
            Profiles=candidate;Campaign=save;Campaign.AutoTiming=Preferences.AutoTiming;TrainingMode=false;SaveNotice=candidate.Store.Notice;RestorePreparation();
            if(resume&&!ResumeBattle())
            {Profiles=oldProfiles;Campaign=oldCampaign;SelectedStage=oldStage;Deployment.Clear();Deployment.AddRange(oldDeployment);TrainingMode=oldTraining;return false;}
            RememberTitleSlot(slot);Hud.HideTitle();if(!resume)Hud.ShowIntermissionHub();return true;
        }
        void RememberTitleSlot(int slot)
        {
            // Campaign success must not be reported as failure just because remembering the last slot failed.
            var notice=SaveNotice;var draft=UnityEngine.JsonUtility.FromJson<PlayerPreferences>(UnityEngine.JsonUtility.ToJson(Preferences));draft.Slot=slot;
            if(preferenceFile.Save(draft)){Preferences=draft;SaveNotice=notice;}
            else SaveNotice=(string.IsNullOrEmpty(notice)?"":notice+"\n")+"게임은 준비되었습니다. 마지막 슬롯 설정은 저장하지 못했습니다.";
        }
        public void ReturnToTitle()
        {
            if(Session!=null||StoryActive||TutorialActive)return;
            if(!SavePreparation()){Hud.ShowSystemMenu();return;}
            Hud.ShowTitle();
        }
    }
}
