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
        [UnityTest] public IEnumerator SuspendResumePreservesTeamSPDAndCTTurnsActionsAndRandomState()
        {
            foreach(int mode in new[]{0,1,2})
            {
                director.TrainingMode=false;director.UseCT=mode==2;director.UseFixedSpeedOrder=mode==1;director.BeginBattle();yield return null;
                var session=director.Session;var active=session.Active;
                var destination=session.Grid.Reachable(active,out _).Keys.First(p=>p!=active.Position);
                Assert.That(session.Move(destination),Is.True);active.AddStatus(StatusKind.Song,3);active.Cooldowns["test"]=2;active.SpecialGauge=45;active.GuardIgnition=true;
                session.Units.Last().Damage(99999,session.Grid);
                var expected=BattleCheckpoint.Capture(session,director.Deployment.ToArray());var expectedJson=JsonUtility.ToJson(expected);
                var random=Random.state;int roll=Random.Range(0,10000);Random.state=random;
                Assert.That(director.SuspendBattle(),Is.True);yield return null;Assert.That(director.Session,Is.Null);
                var root=director.Profiles.Root;director.ConfigureStorage(root);Assert.That(director.Campaign.SuspendedBattle,Is.Not.Null);
                Assert.That(director.ResumeBattle(),Is.True);yield return null;
                Assert.That(JsonUtility.ToJson(BattleCheckpoint.Capture(director.Session,director.Deployment.ToArray())),Is.EqualTo(expectedJson));
                Assert.That(Random.Range(0,10000),Is.EqualTo(roll));
                Assert.That(new CampaignFile(director.Profiles.PathFor(0)).Load().SuspendedBattle,Is.Null,"Checkpoint must be consumed before play");
                var reference=expected.Restore(director.Catalog);
                for(int i=0;i<20;i++){reference.EndTurn();director.Session.EndTurn();reference.Advance();director.Session.Advance();Assert.That(director.Session.Units.IndexOf(director.Session.Active),Is.EqualTo(reference.Units.IndexOf(reference.Active)));}
                director.Restart();yield return null;
            }
        }
        [UnityTest] public IEnumerator SlotAndSettingsMenusPersistAndBlockBackgroundInput()
        {
            director.Hud.ShowDeployment();director.Hud.ShowSystemMenu();yield return null;
            var panel=director.Hud.transform.Find("SystemOverlay/SystemWindow");
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.IsInteractable()).All(b=>b.transform.IsChildOf(panel)),Is.True);
            Click("현재 슬롯 저장");yield return null;Click("슬롯 2 불러오기");yield return null;Assert.That(director.Profiles.Slot,Is.EqualTo(1));
            director.Campaign.Gold=123;Assert.That(director.SavePreparation(),Is.True);
            Assert.That(director.SelectProfile(0),Is.True);Assert.That(director.Campaign.Gold,Is.EqualTo(300));
            director.Hud.ShowSystemMenu(1);yield return null;Click("음악 +");yield return null;Click("설정 적용 · 저장");yield return null;
            Assert.That(director.Audio.MusicVolume,Is.EqualTo(.38f).Within(.001));
            Click("음악 +");yield return null;Assert.That(director.Preferences.Music,Is.EqualTo(.38f).Within(.001),"Editing a saved draft must not mutate applied preferences");
            foreach(var text in panel.GetComponentsInChildren<TMPro.TMP_Text>()){text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.text);}
            director.GetComponent<GamepadPointer>().Cancel();yield return null;Assert.That(director.Hud.SystemMenuOpen,Is.False);
            director.ConfigureStorage(director.Profiles.Root);Assert.That(director.Preferences.Music,Is.EqualTo(.38f).Within(.001));
            Assert.That(director.SelectProfile(1),Is.True);Assert.That(director.Campaign.Gold,Is.EqualTo(123));
        }
        [UnityTest] public IEnumerator CorruptCheckpointAndFailedSuspendDoNotLoseCurrentBattle()
        {
            director.TrainingMode=false;director.BeginBattle();yield return null;var session=director.Session;
            Directory.CreateDirectory(director.Profiles.PathFor(0)+".tmp");
            LogAssert.Expect(LogType.Warning,new System.Text.RegularExpressions.Regex("Campaign save failed:"));
            Assert.That(director.SuspendBattle(),Is.False);Assert.That(director.Session,Is.SameAs(session));Assert.That(director.Campaign.SuspendedBattle,Is.Null);
            Directory.Delete(director.Profiles.PathFor(0)+".tmp");Assert.That(director.SuspendBattle(),Is.True);yield return null;
            director.Campaign.SuspendedBattle.Units[0].Id="missing";string before=File.ReadAllText(director.Profiles.PathFor(0));
            Assert.That(director.ResumeBattle(),Is.False);Assert.That(director.Session,Is.Null);Assert.That(File.ReadAllText(director.Profiles.PathFor(0)),Is.EqualTo(before));
            director.Campaign.SuspendedBattle=null;director.TrainingMode=true;director.BeginBattle();yield return null;Assert.That(director.CanSuspend,Is.False);
        }
    }
}
