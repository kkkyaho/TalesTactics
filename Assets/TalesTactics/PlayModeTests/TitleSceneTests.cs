using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        void TitleSetup(){director.Catalog.CrossoverReady=true;director.ConfigureStorage(testStorage);director.TrainingMode=false;director.Hud.ShowTitle();}
        [UnityTest] public IEnumerator TitleKeyboardAndVirtualPadSubmitOnceAndCancelOneLevel()
        {
            TitleSetup();var keyboard=InputSystem.AddDevice<Keyboard>();var pad=InputSystem.AddDevice<Gamepad>();
            try
            {
                yield return KeyboardPress(keyboard,Key.Enter);Assert.That(director.Hud.transform.Find("TitleScreen/TitleSlotsOverlay").gameObject.activeSelf,Is.True);
                yield return KeyboardPress(keyboard,Key.Escape);Assert.That(director.Hud.transform.Find("TitleScreen/TitleSlotsOverlay").gameObject.activeSelf,Is.False);
                var pointer=director.GetComponent<GamepadPointer>();PointAt(pointer,"타이틀 새 게임");yield return PadPress(pad,GamepadButton.South);
                PointAt(pointer,"타이틀 슬롯 1");yield return PadPress(pad,GamepadButton.South);
                Assert.That(director.Hud.transform.Find("TitleScreen/TitleConfirm").gameObject.activeSelf,Is.True);
                Assert.That(Directory.Exists(testStorage),Is.False,"Opening confirmation must not start a game");
                yield return PadPress(pad,GamepadButton.East);Assert.That(director.Hud.transform.Find("TitleScreen/TitleConfirm").gameObject.activeSelf,Is.False);
                Assert.That(director.Hud.transform.Find("TitleScreen/TitleSlotsOverlay").gameObject.activeSelf,Is.True);
                yield return PadPress(pad,GamepadButton.East);Assert.That(director.Hud.transform.Find("TitleScreen/TitleSlotsOverlay").gameObject.activeSelf,Is.False);
            }
            finally{InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(pad);}
        }
        [UnityTest] public IEnumerator TitleAllSlotsFullAndSettingsProtectionDoNotUndoSuccessfulNewGame()
        {
            TitleSetup();for(int i=0;i<3;i++)Assert.That(new CampaignFile(director.Profiles.PathFor(i)).Save(new CampaignSave{Gold=500+i}),Is.True);
            File.WriteAllText(Path.Combine(testStorage,"settings.json"),"protected");director.ConfigureStorage(testStorage);director.Hud.ShowTitle();director.Hud.ShowTitleSlots(true);yield return null;
            Click("타이틀 슬롯 1");yield return null;Click("타이틀 새 게임 확정");yield return null;
            Assert.That(director.Hud.TitleOpen,Is.False);Assert.That(director.Profiles.Slot,Is.EqualTo(0));Assert.That(director.Campaign.Gold,Is.EqualTo(300));
            Assert.That(new CampaignFile(director.Profiles.PathFor(0)+".bak").Load().Gold,Is.EqualTo(500));
            Assert.That(new CampaignFile(director.Profiles.PathFor(1)).Load().Gold,Is.EqualTo(501));Assert.That(new CampaignFile(director.Profiles.PathFor(2)).Load().Gold,Is.EqualTo(502));
            Assert.That(File.ReadAllText(Path.Combine(testStorage,"settings.json")),Is.EqualTo("protected"));Assert.That(director.SaveNotice,Does.Contain("설정은 저장하지 못했습니다"));
        }
        [UnityTest] public IEnumerator TitleResumeWriteFailureKeepsSelectedProfileAndCheckpoint()
        {
            TitleSetup();director.StartTitleGame(0);director.BeginBattle();yield return null;director.SuspendBattle();yield return null;
            var path=director.Profiles.PathFor(0);string before=File.ReadAllText(path);director.Hud.ShowTitle();director.StartTitleGame(1);director.Hud.ShowTitle();var active=director.Campaign;
            Directory.CreateDirectory(path+".tmp");LogAssert.Expect(LogType.Warning,new System.Text.RegularExpressions.Regex("Campaign save failed:"));
            Assert.That(director.LoadTitleGame(0,true),Is.False);Assert.That(director.Profiles.Slot,Is.EqualTo(1));Assert.That(director.Campaign,Is.SameAs(active));
            Assert.That(director.Session,Is.Null);Assert.That(director.Hud.TitleOpen,Is.True);Assert.That(File.ReadAllText(path),Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator TitleBrowsingIsReadOnlyAndCancelPreservesAllSlots()
        {
            TitleSetup();var path=director.Profiles.PathFor(0);var save=new CampaignSave{Gold=777,SelectedStage=CrossoverCampaign.FirstStage};Assert.That(new CampaignFile(path).Save(save),Is.True);
            string before=File.ReadAllText(path);var campaign=director.Campaign;
            director.Hud.ShowTitleSlots(false);yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.IsInteractable()).All(b=>b.transform.IsChildOf(director.Hud.transform.Find("TitleScreen/TitleSlotsOverlay/TitleSlotsWindow"))),Is.True);
            Assert.That(director.ReadTitleSlots().Count(s=>s.Exists),Is.EqualTo(1));
            director.GetComponent<GamepadPointer>().Cancel();yield return null;Click("타이틀 새 게임");yield return null;
            Click("타이틀 슬롯 1");yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject.name,Is.EqualTo("타이틀 새 게임 취소"));
            director.GetComponent<GamepadPointer>().Cancel();yield return null;director.GetComponent<GamepadPointer>().Cancel();yield return null;
            Assert.That(director.Campaign,Is.SameAs(campaign));Assert.That(File.ReadAllText(path),Is.EqualTo(before));Assert.That(Directory.GetFiles(testStorage).Length,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator TitleNewGameUsesEmptySlotAndKeepsOtherSaveAndSettings()
        {
            TitleSetup();var path=director.Profiles.PathFor(0);Assert.That(new CampaignFile(path).Save(new CampaignSave{Gold=901}),Is.True);string before=File.ReadAllText(path);
            director.Preferences.Music=.12f;director.Hud.ShowTitleSlots(true);yield return null;Click("타이틀 슬롯 2");yield return null;Click("타이틀 새 게임 확정");yield return null;
            Assert.That(director.Profiles.Slot,Is.EqualTo(1));Assert.That(director.Hud.TitleOpen,Is.False);Assert.That(director.Hud.IntermissionHubOpen,Is.True);
            Assert.That(director.Campaign.Gold,Is.EqualTo(300));Assert.That(director.SelectedStage,Is.EqualTo(CrossoverCampaign.FirstStage));
            Assert.That(director.Deployment.Select(i=>director.Catalog.Characters[i].Id),Is.EquivalentTo(new[]{"cless","mint"}));Assert.That(director.Preferences.Music,Is.EqualTo(.12f));Assert.That(File.ReadAllText(path),Is.EqualTo(before));
            Assert.That(new CampaignFile(director.Profiles.PathFor(1)).Load().SelectedStage,Is.EqualTo(CrossoverCampaign.FirstStage));
        }
        [UnityTest] public IEnumerator TitleOverwriteBacksUpSelectedSlotAndFailedSaveKeepsTitle()
        {
            TitleSetup();var path=director.Profiles.PathFor(0);Assert.That(new CampaignFile(path).Save(new CampaignSave{Gold=999}),Is.True);string before=File.ReadAllText(path);var old=director.Campaign;
            Directory.CreateDirectory(path+".tmp");LogAssert.Expect(LogType.Warning,new System.Text.RegularExpressions.Regex("Campaign save failed:"));
            Assert.That(director.StartTitleGame(0),Is.False);Assert.That(director.Hud.TitleOpen,Is.True);Assert.That(director.Campaign,Is.SameAs(old));Assert.That(File.ReadAllText(path),Is.EqualTo(before));
            Directory.Delete(path+".tmp");Assert.That(director.StartTitleGame(0),Is.True);yield return null;
            Assert.That(director.SaveNotice,Does.Contain("새 게임을 저장했습니다"));
            Assert.That(File.ReadAllText(path+".bak"),Is.EqualTo(before));Assert.That(new CampaignFile(path).Load().Gold,Is.EqualTo(300));
        }
        [UnityTest] public IEnumerator TitleProtectedAndRecoveredSlotsPreserveOriginalFiles()
        {
            TitleSetup();Directory.CreateDirectory(testStorage);var path=director.Profiles.PathFor(0);File.WriteAllText(path,"{\"Version\":99}");
            File.WriteAllText(director.Profiles.PathFor(1),"broken");var good=new CampaignSave{Gold=654,SelectedStage=CrossoverCampaign.FirstStage};Assert.That(new CampaignFile(director.Profiles.PathFor(2)+".bak").Save(good),Is.True);File.WriteAllText(director.Profiles.PathFor(2),"broken");
            var files=Directory.GetFiles(testStorage).ToDictionary(p=>p,File.ReadAllText);
            var slots=director.ReadTitleSlots();Assert.That(slots[0].Writable,Is.False);Assert.That(slots[1].Writable,Is.False);Assert.That(slots[2].Writable,Is.True);
            Assert.That(director.LoadTitleGame(0,false),Is.False);Assert.That(director.StartTitleGame(1),Is.False);Assert.That(director.LoadTitleGame(2,false),Is.True);yield return null;
            Assert.That(director.Campaign.Gold,Is.EqualTo(654));foreach(var f in files)Assert.That(File.ReadAllText(f.Key),Is.EqualTo(f.Value));
        }
        [UnityTest] public IEnumerator TitleLoadResumesCheckpointAndRejectsLegacyOrDamagedCheckpoint()
        {
            TitleSetup();Assert.That(director.StartTitleGame(0),Is.True);director.BeginBattle();yield return null;Assert.That(director.SuspendBattle(),Is.True);yield return null;
            var expected=JsonUtility.ToJson(director.Campaign.SuspendedBattle);director.Hud.ShowTitle();Assert.That(director.LoadTitleGame(0,true),Is.True);yield return null;
            Assert.That(director.Hud.TitleOpen,Is.False);Assert.That(JsonUtility.ToJson(BattleCheckpoint.Capture(director.Session,director.Deployment.ToArray())),Is.EqualTo(expected));
            Assert.That(new CampaignFile(director.Profiles.PathFor(0)).Load().SuspendedBattle,Is.Null);
            Assert.That(director.SuspendBattle(),Is.True);yield return null;var path=director.Profiles.PathFor(0);var save=new CampaignFile(path).Load();save.SuspendedBattle.Units[0].Id="missing";Assert.That(new CampaignFile(path).Save(save),Is.True);
            director.Hud.ShowTitle();string before=File.ReadAllText(path);Assert.That(director.LoadTitleGame(0,true),Is.False);Assert.That(director.Session,Is.Null);Assert.That(director.Hud.TitleOpen,Is.True);Assert.That(File.ReadAllText(path),Is.EqualTo(before));
            save.SuspendedBattle.Stage=0;Assert.That(new CampaignFile(path).Save(save),Is.True);Assert.That(director.ReadTitleSlots()[0].Resumable,Is.False);Assert.That(director.LoadTitleGame(0,true),Is.False);Assert.That(director.LoadTitleGame(0,false),Is.True);yield return null;Assert.That(director.SelectedStage,Is.EqualTo(CrossoverCampaign.FirstStage));
        }
        [UnityTest] public IEnumerator TitlePointerStartsAndReturnToTitleSavesPreparation()
        {
            TitleSetup();yield return null;var pointer=director.GetComponent<GamepadPointer>();var b=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(x=>x.name=="타이틀 새 게임");
            pointer.MoveTo(RectTransformUtility.WorldToScreenPoint(null,b.transform.TransformPoint(((RectTransform)b.transform).rect.center)));pointer.Submit();yield return null;
            Assert.That(director.Hud.transform.Find("TitleScreen/TitleSlotsOverlay").gameObject.activeSelf,Is.True);pointer.Cancel();yield return null;Assert.That(director.Hud.TitleOpen,Is.True);
            Assert.That(director.StartTitleGame(0),Is.True);director.Campaign.Gold=678;director.ReturnToTitle();yield return null;
            Assert.That(director.Hud.TitleOpen,Is.True);Assert.That(new CampaignFile(director.Profiles.PathFor(0)).Load().Gold,Is.EqualTo(678));
            director.Hud.ShowSystemMenu();director.Hud.ShowHelp();Assert.That(director.Hud.SystemMenuOpen||director.Hud.HelpOpen,Is.False);
        }
    }
}
