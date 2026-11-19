using System;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleDirector
    {
        public bool MobilePersistenceEnabled {get;private set;}
        public bool UsesDesktopDisplaySettings=>!MobilePersistenceEnabled&&!Application.isMobilePlatform;
        public string MobileSaveNotice {get;private set;}="";
        BattleSession mobileSavedSession;
        string mobileSavedCheckpoint;

        void ConfigureMobilePersistence(bool mobile)
        {
            MobilePersistenceEnabled=mobile;MobileSaveNotice="";
            mobileSavedSession=null;mobileSavedCheckpoint=null;
        }
        void ForgetMobileCheckpoint()
        {mobileSavedSession=null;mobileSavedCheckpoint=null;}

        // Invoked after a completed transition to a player command, including movement,
        // an attack, rewind and undo. Selection previews and running coroutines are never captured.
        public bool SaveMobileCommandBoundary()
        {
            if(!MobilePersistenceEnabled||Profiles==null||Campaign==null||!CanSuspend)return false;
            BattleCheckpoint snapshot;string fingerprint;
            try
            {
                snapshot=BattleCheckpoint.Capture(Session,Session.Units.Where(u=>u.Team==Team.Player)
                    .Select(u=>Array.IndexOf(Catalog.Characters,u.Data)).ToArray());
                fingerprint=JsonUtility.ToJson(snapshot);
            }
            catch(Exception){MobileSaveFailed();return false;}
            if(mobileSavedSession==Session&&mobileSavedCheckpoint==fingerprint&&Campaign.SuspendedBattle!=null)return true;
            var previous=Campaign.SuspendedBattle;bool hadCheckpoint=Campaign.HasSuspendedBattle;
            Campaign.SuspendedBattle=snapshot;Campaign.HasSuspendedBattle=true;
            if(!WriteProgress(Campaign,true))
            {
                Campaign.SuspendedBattle=previous;Campaign.HasSuspendedBattle=hadCheckpoint;
                MobileSaveFailed();return false;
            }
            mobileSavedSession=Session;mobileSavedCheckpoint=fingerprint;MobileSaveNotice="";
            return true;
        }
        void MobileSaveFailed()
        {
            MobileSaveNotice="자동 저장 실패 · 마지막으로 저장된 상태까지만 복구할 수 있습니다.";
            SaveNotice=MobileSaveNotice+"\n"+(Profiles?.Store.Notice??"현재 전투는 계속할 수 있습니다.");
        }
        void SaveMobileBackgroundProgress()
        {
            if(!MobilePersistenceEnabled||Profiles==null||Campaign==null)return;
            if(CanSuspend){SaveMobileCommandBoundary();return;}
            // Rewards and after-story cursors already use their own atomic transactions.
            // Before-story dialogue restarts from its beginning after process termination.
            // Unsafe battle states retain the last completed command checkpoint on disk.
            if(Session!=null||StoryActive||TutorialActive||Hud==null||Hud.TitleOpen)return;
            if(!SavePreparation())MobileSaveFailed();
        }
        void OnApplicationPause(bool paused)
        {
            if(!MobilePersistenceEnabled)return;
            if(paused)SaveMobileBackgroundProgress();else ResetTouchInput();
        }
        void OnApplicationFocus(bool focused)
        {
            if(!MobilePersistenceEnabled)return;
            if(!focused)SaveMobileBackgroundProgress();else ResetTouchInput();
        }
    }
}
