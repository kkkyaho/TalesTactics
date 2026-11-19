using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        void MobilePersistenceFixture()
        {
            director.ConfigureStorage(testStorage,false,true);
            director.SelectedStage=0;director.TrainingMode=false;
            director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1,3});
            director.Hud.HideTitle();director.Hud.ShowDeployment();
            Assert.That(director.MobilePersistenceEnabled,Is.True);
        }
        CampaignSave MobileDisk()=>new CampaignFile(director.Profiles.PathFor(director.Profiles.Slot)).Load();
        string MobileDiskText()=>File.ReadAllText(director.Profiles.PathFor(director.Profiles.Slot));
        void MobilePause()
        {
            director.SendMessage("OnApplicationFocus",false);
            director.SendMessage("OnApplicationPause",true);
        }
        void MobileResume()
        {
            director.SendMessage("OnApplicationPause",false);
            director.SendMessage("OnApplicationFocus",true);
        }
        void MobileAssertCheckpointMatchesLive()
        {
            var disk=MobileDisk();Assert.That(disk.HasSuspendedBattle,Is.True);
            Assert.That(disk.SuspendedBattle,Is.Not.Null);
            var live=BattleCheckpoint.Capture(director.Session,director.Deployment.ToArray());
            // Presentation may consume Unity's global random between frames; compare the
            // captured units, scheduler, objective, opening and rewind history at this boundary.
            live.RandomState=disk.SuspendedBattle.RandomState;
            Assert.That(JsonUtility.ToJson(disk.SuspendedBattle),Is.EqualTo(JsonUtility.ToJson(live)));
            Assert.That(disk.SuspendedBattle.Restore(director.Catalog).Result,Is.EqualTo(BattleResult.Ongoing));
        }

        [UnityTest,Category("AndroidFoundation")]
        public IEnumerator MobileMovementSavesCompletedCommandsAndKeepsPriorSnapshotDuringAnimation()
        {
            MobilePersistenceFixture();director.BeginBattle();yield return null;
            Assert.That(director.IsPlayerCommand,Is.True);MobileAssertCheckpointMatchesLive();
            var actor=director.Session.Active;int actorIndex=director.Session.Units.IndexOf(actor);
            var original=actor.Position;
            var destination=director.Session.Grid.Reachable(director.Session.Active,out _).Keys.First(p=>p!=original);
            string before=MobileDiskText();
            var movement=director.StartCoroutine(director.MoveUnit(destination));
            Assert.That(director.State,Is.InstanceOf<ActionExecutionState>());
            Assert.That(director.Session.Active.Position,Is.EqualTo(destination),"The model moves before its animation finishes.");
            MobilePause();Assert.That(MobileDiskText(),Is.EqualTo(before),"No partial movement capture.");
            MobileResume();yield return movement;
            Assert.That(director.IsPlayerCommand,Is.True);MobileAssertCheckpointMatchesLive();
            var moved=MobileDisk().SuspendedBattle;
            Assert.That(moved.Active,Is.EqualTo(actorIndex));Assert.That(moved.Units[actorIndex].Id,Is.EqualTo(actor.Data.Id));
            Assert.That(moved.Units[actorIndex].Position,Is.EqualTo(destination));
            director.Undo();MobileAssertCheckpointMatchesLive();
            Assert.That(director.Session.Active.Position,Is.EqualTo(original),"Undo also updates the recoverable command.");
            yield return director.MoveUnit(destination);
            var checkpoint=MobileDisk().SuspendedBattle;
            director.Restart();director.ConfigureStorage(testStorage,false,true);
            Assert.That(director.ResumeBattle(),Is.True);yield return null;
            Assert.That(director.Session.Active.Position,Is.EqualTo(destination));
            Assert.That(director.Session.History.Count,Is.EqualTo(checkpoint.History.Length));
            Assert.That(MobileDisk().SuspendedBattle,Is.Not.Null,"Mobile resume retains recovery until the next durable command/reward.");
            Assert.That(director.RewindAction(),Is.True);yield return null;
            MobileAssertCheckpointMatchesLive();Assert.That(MobileDisk().SuspendedBattle.RewindsUsed,Is.EqualTo(1));
        }

        [UnityTest,Category("AndroidFoundation")]
        public IEnumerator MobileAttackAndSelectionPreviewsNeverReplaceTheLastCompletedCommand()
        {
            MobilePersistenceFixture();director.BeginBattle();yield return null;
            var session=director.Session;var actor=session.Active;var enemy=session.Units.First(u=>u.Team==Team.Enemy);
            var adjacent=session.Grid.Neighbors(actor.Position).First(t=>t.Walkable&&t.Occupant==null&&Math.Abs(t.Height-session.Grid[actor.Position].Height)<=1);
            Assert.That(session.Grid.Place(enemy,adjacent.Coordinate),Is.True);
            director.RefreshViews();Assert.That(director.SaveMobileCommandBoundary(),Is.True);
            string before=MobileDiskText();int hp=enemy.CurrentHP;
            director.WaitCommand();Assert.That(director.State,Is.InstanceOf<FacingSelectionState>());
            actor.Facing=Facing.Back;MobilePause();Assert.That(MobileDiskText(),Is.EqualTo(before),"Unconfirmed facing must not become a checkpoint.");
            director.State.Cancel();Assert.That(director.IsPlayerCommand,Is.True);
            director.AttackCommand();director.SelectTarget(enemy.Position);
            string selected=MobileDiskText();director.Confirm();
            Assert.That(director.State,Is.InstanceOf<ActionExecutionState>());
            MobilePause();Assert.That(MobileDiskText(),Is.EqualTo(selected),"No attack windup capture.");
            MobileResume();
            for(float deadline=Time.realtimeSinceStartup+15;director.State is ActionExecutionState&&Time.realtimeSinceStartup<deadline;)yield return null;
            Assert.That(director.IsPlayerCommand,Is.True);Assert.That(enemy.CurrentHP,Is.LessThan(hp));
            MobileAssertCheckpointMatchesLive();
            Assert.That(MobileDisk().SuspendedBattle.Units[session.Units.IndexOf(actor)].Acted,Is.True);
            Assert.That(MobileDisk().SuspendedBattle.Units[session.Units.IndexOf(enemy)].HP,Is.EqualTo(enemy.CurrentHP));
        }

        [UnityTest,Category("AndroidFoundation")]
        public IEnumerator MobileFailedAutosavePreservesLastFileAndRetriesWithoutLosingBattle()
        {
            MobilePersistenceFixture();director.BeginBattle();yield return null;
            string before=MobileDiskText();var previous=director.Campaign.SuspendedBattle;var session=director.Session;
            var actor=session.Active;int actorIndex=session.Units.IndexOf(actor);
            var destination=session.Grid.Reachable(actor,out _).Keys.First(p=>p!=actor.Position);
            Assert.That(session.Move(destination),Is.True);
            string blocked=director.Profiles.PathFor(0)+".tmp";Directory.CreateDirectory(blocked);
            try
            {
                LogAssert.Expect(LogType.Warning,new System.Text.RegularExpressions.Regex("Campaign save failed:"));
                Assert.That(director.SaveMobileCommandBoundary(),Is.False);
                Assert.That(MobileDiskText(),Is.EqualTo(before));Assert.That(director.Campaign.SuspendedBattle,Is.SameAs(previous));
                Assert.That(director.Campaign.HasSuspendedBattle,Is.True);Assert.That(director.Session,Is.SameAs(session));
                Assert.That(director.MobileSaveNotice,Does.Contain("실패"));Assert.That(director.SaveNotice,Does.Contain("마지막으로 저장된 상태"));
                LogAssert.Expect(LogType.Warning,new System.Text.RegularExpressions.Regex("Campaign save failed:"));
                Assert.That(director.SaveMobileCommandBoundary(),Is.False);Assert.That(MobileDiskText(),Is.EqualTo(before));
            }
            finally{Directory.Delete(blocked);}
            Assert.That(director.SaveMobileCommandBoundary(),Is.True);Assert.That(director.MobileSaveNotice,Is.Empty);
            var saved=MobileDisk().SuspendedBattle;
            Assert.That(saved.Active,Is.EqualTo(actorIndex));Assert.That(saved.Units[actorIndex].Id,Is.EqualTo(actor.Data.Id));
            Assert.That(saved.Units[actorIndex].Position,Is.EqualTo(destination));MobileAssertCheckpointMatchesLive();
        }

        [UnityTest,Category("AndroidFoundation")]
        public IEnumerator MobileRewardAndEndingTransactionsConsumeAutosaveExactlyOnce()
        {
            MobilePersistenceFixture();director.RewardRoll=()=>9999;
            director.RequestBattle();yield return null;
            Assert.That(director.StoryActive,Is.True);Assert.That(director.Session,Is.Null);
            Assert.That(director.SaveMobileCommandBoundary(),Is.False);
            MobilePause();Assert.That(File.Exists(director.Profiles.PathFor(0)),Is.False,"Before-story display is not a saved partial battle.");
            director.FinishStory();yield return null;Assert.That(MobileDisk().SuspendedBattle,Is.Not.Null);
            foreach(var enemy in director.Session.Units.Where(u=>u.Team==Team.Enemy))enemy.Damage(99999,director.Session.Grid);
            director.SetState(new BattleEndState(director));yield return null;
            Assert.That(director.RewardPending,Is.False);var paid=MobileDisk();Assert.That(paid.SuspendedBattle,Is.Null);Assert.That(paid.HasSuspendedBattle,Is.False);
            Assert.That(paid.HasPendingAfterStory,Is.True);string rewarded=MobileDiskText();
            director.CompleteBattle();director.SaveBattleReward();MobilePause();
            Assert.That(MobileDiskText(),Is.EqualTo(rewarded),"Duplicate completion and background events cannot replay rewards.");
            director.ReadEnding();director.AdvanceStory();Assert.That(director.StoryIndex,Is.EqualTo(1));
            string line=MobileDiskText();MobilePause();Assert.That(MobileDiskText(),Is.EqualTo(line),"Only the existing story transaction advances a saved cursor.");
            Assert.That(MobileDisk().PendingAfterStoryLine,Is.EqualTo(1));
            var previousCampaign=director.Campaign;director.ConfigureStorage(testStorage,false,true);
            Assert.That(director.Campaign,Is.Not.SameAs(previousCampaign),"Reload campaign from disk before entering the hub.");
            director.Restart();
            Assert.That(director.StoryActive,Is.True,"The intermission hub automatically resumes the pending ending.");
            Assert.That(director.Campaign.HasPendingAfterStory,Is.True);Assert.That(director.StoryStage,Is.EqualTo(0));
            Assert.That(director.StoryIndex,Is.EqualTo(1));Assert.That(MobileDiskText(),Is.EqualTo(line));
            Assert.That(director.Campaign.Gold,Is.EqualTo(paid.Gold));
            Assert.That(director.Campaign.Inventory.Select(i=>i.Id+":"+i.Count),Is.EqualTo(paid.Inventory.Select(i=>i.Id+":"+i.Count)));
            director.FinishStory();yield return null;
            Assert.That(MobileDisk().HasPendingAfterStory,Is.False);Assert.That(MobileDisk().SuspendedBattle,Is.Null);
        }

        [UnityTest,Category("AndroidFoundation")]
        public IEnumerator MobilePreferencesAndBackgroundPreparationAreSeparateFromDesktopAndTitle()
        {
            var desktop=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(director.Preferences));
            desktop.Width=1366;desktop.Height=768;desktop.Fullscreen=true;
            Assert.That(director.SavePreferences(desktop,false),Is.True);
            string desktopPath=Path.Combine(testStorage,"settings.json"),desktopBefore=File.ReadAllText(desktopPath);
            MobilePersistenceFixture();Assert.That(director.UsesDesktopDisplaySettings,Is.False);
            Assert.That(director.Preferences.Width,Is.EqualTo(1280),"Mobile settings do not import desktop window dimensions.");
            var mobile=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(director.Preferences));mobile.Music=.2f;mobile.TextScale=1.3f;
            Assert.That(director.SavePreferences(mobile),Is.True);
            Assert.That(File.Exists(Path.Combine(testStorage,"settings-mobile.json")),Is.True);
            Assert.That(File.ReadAllText(desktopPath),Is.EqualTo(desktopBefore));
            director.Campaign.Gold=217;MobilePause();Assert.That(MobileDisk().Gold,Is.EqualTo(217));
            string prepared=MobileDiskText();director.Hud.ShowTitle();director.Campaign.Gold=999;
            MobilePause();Assert.That(MobileDiskText(),Is.EqualTo(prepared),"Backgrounding the title must not write a loaded slot.");
            director.ConfigureStorage(testStorage,false,false);director.Hud.HideTitle();director.TrainingMode=false;
            Assert.That(director.MobilePersistenceEnabled,Is.False);Assert.That(director.UsesDesktopDisplaySettings,Is.True);
            Assert.That(director.Preferences.Width,Is.EqualTo(1366));director.BeginBattle();yield return null;
            string desktopBattle=MobileDiskText();MobilePause();Assert.That(MobileDiskText(),Is.EqualTo(desktopBattle));
            Assert.That(MobileDisk().SuspendedBattle,Is.Null,"Desktop does not silently enable command autosave.");
            Assert.That(director.SuspendBattle(),Is.True);Assert.That(director.ResumeBattle(),Is.True);
            Assert.That(MobileDisk().SuspendedBattle,Is.Null,"Desktop manual resume still consumes its checkpoint.");
        }

        [UnityTest,Category("AndroidFoundation")]
        public IEnumerator MobileAutosaveProtectsUnreadableSlotsAndRetainsFreshRecruitment()
        {
            Directory.CreateDirectory(testStorage);string path=Path.Combine(testStorage,"campaign.json");
            File.WriteAllText(path,"protected unreadable campaign");MobilePersistenceFixture();
            Assert.That(director.CanSave,Is.False);director.BeginBattle();yield return null;
            Assert.That(director.MobileSaveNotice,Does.Contain("실패"));Assert.That(File.ReadAllText(path),Is.EqualTo("protected unreadable campaign"));
            director.Restart();
            // A separate fixture slot is created through the existing new-game transaction.
            director.ConfigureStorage(testStorage,false,true);director.Hud.ShowTitle();Assert.That(director.StartTitleGame(1),Is.True);
            director.Catalog.CrossoverReady=true;director.SelectedStage=7;
            director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(1));
            director.Campaign.Get("cless").Level=5;director.Campaign.Get("mint").Level=3;
            director.Deployment.Clear();director.Deployment.AddRange(new[]{"cless","mint"}.Select(id=>Array.FindIndex(director.Catalog.Characters,c=>c.Id==id)));
            Assert.That(director.SavePreparation(),Is.True);director.BeginBattle();yield return null;
            Assert.That(director.Session.Units.Where(u=>u.Team==Team.Player).Select(u=>u.Data.Id),Does.Contain("alphen").And.Contain("shionne"));
            var saved=MobileDisk();Assert.That(saved.StoryProgress,Does.Contain(CampaignRecruitment.Flag(2)));
            Assert.That(saved.Get("alphen").Level,Is.EqualTo(4));Assert.That(saved.Get("shionne").Level,Is.EqualTo(4));
            director.Restart();director.ConfigureStorage(testStorage,false,true);
            Assert.That(director.Profiles.Slot,Is.EqualTo(1));Assert.That(director.ResumeBattle(),Is.True);yield return null;
            Assert.That(director.Session.Crossover.State.OpeningRevision,Is.EqualTo(1));
            Assert.That(MobileDisk().SuspendedBattle,Is.Not.Null);
            Assert.That(File.ReadAllText(path),Is.EqualTo("protected unreadable campaign"),"Other slots must remain untouched.");
        }
    }
}
