using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        void ShowGrowthFixture(CharacterData character)=>typeof(BattleHud).GetMethod("ShowGrowth",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(director.Hud,new object[]{character,null});
        void TryGrowthModalFixture(CharacterData character)=>typeof(BattleHud).GetMethod("ShowGrowthPath",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(director.Hud,new object[]{character});
        string GrowthChoiceName(CharacterData character,GrowthPath path)=>GrowthPaths.Name(path)+" · "+GrowthPaths.Role(character,path);
        void AssertGrowthFocus(string name)
        {
            var button=FormationControl(name);Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(button.gameObject),name);
            var rect=(RectTransform)button.transform;var expected=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            Assert.That(Vector2.Distance(director.GetComponent<GamepadPointer>().Position,expected),Is.LessThan(2),name+" controller pointer");
        }
        [UnityTest,Category("CommercialSlice")]
        public IEnumerator ClessAndMintGrowthChoicesSaveOnceAndKeepControllerFocusAndScaledTextInsideScreen()
        {
            // Progress prerequisites are fixtures; choices, persistence, modal input and rendering use production code.
            PreparationGuidanceEpisode(3);foreach(string id in new[]{"cless","mint"})director.Campaign.Get(id).Level=10;
            var persist=director.PersistCampaign;int writes=0;director.PersistCampaign=save=>{writes++;return persist(save);};
            var pad=InputSystem.AddDevice<Gamepad>();float oldScale=director.Preferences.TextScale;
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","CommercialSlice20261117","Growth-"+Guid.NewGuid().ToString("N")));Directory.CreateDirectory(output);
            try
            {
                foreach(float scale in new[]{1f,1.3f})foreach(string id in new[]{"cless","mint"})
                {
                    var character=director.Catalog.Characters.Single(c=>c.Id==id);director.Preferences.TextScale=scale;ShowGrowthFixture(character);yield return null;
                    var pointer=director.GetComponent<GamepadPointer>();PointAt(pointer,"성장 방향 선택");string before=JsonUtility.ToJson(director.Campaign);int oldWrites=writes;
                    yield return PadPress(pad,GamepadButton.South);yield return new WaitForEndOfFrame();
                    Assert.That(director.Hud.UnitDetailsOpen,Is.True);Assert.That(pointer.Active,Is.True);AssertGrowthFocus("성장 방향 닫기");
                    Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));Assert.That(writes,Is.EqualTo(oldWrites));
                    Assert.That(FormationControl("성장 방향 선택").IsInteractable(),Is.False,"The underlying growth screen is blocked.");
                    var panel=(RectTransform)director.Hud.transform.Find("UnitDetails");Canvas.ForceUpdateCanvases();AssertRewardComparisonViewport(panel,id+" growth panel");
                    foreach(var label in panel.GetComponentsInChildren<TMPro.TMP_Text>())
                    {
                        label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,id+" / "+scale+" / "+label.text);
                        AssertRewardComparisonViewport(label.rectTransform,id+" / "+label.text);
                    }
                    foreach(var button in panel.GetComponentsInChildren<Button>())AssertRewardComparisonViewport((RectTransform)button.transform,id+" / "+button.name);
                    var image=ScreenCapture.CaptureScreenshotAsTexture();
                    try{File.WriteAllBytes(Path.Combine(output,id+"-scale-"+scale.ToString(System.Globalization.CultureInfo.InvariantCulture)+".png"),image.EncodeToPNG());}
                    finally{UnityEngine.Object.Destroy(image);}
                    foreach(var path in new[]{GrowthPath.Assault,GrowthPath.Bulwark})
                    {
                        if(!director.Hud.UnitDetailsOpen){PointAt(pointer,"성장 방향 선택");yield return PadPress(pad,GamepadButton.South);}
                        // Pointer targeting models stick positioning; the submitted A press is routed by the normal player loop.
                        pointer.FocusButton(FormationControl(GrowthChoiceName(character,path)));int prior=writes;
                        yield return PadPress(pad,GamepadButton.South);yield return null;
                        Assert.That(director.Hud.UnitDetailsOpen,Is.False);Assert.That(writes,Is.EqualTo(prior+1));AssertGrowthFocus("성장 방향 선택");
                        Assert.That(director.Campaign.Get(id).Growth,Is.EqualTo(path));Assert.That(new CampaignFile(director.Profiles.PathFor(0)).Load().Get(id).Growth,Is.EqualTo(path));
                    }
                    var disk=File.ReadAllBytes(director.Profiles.PathFor(0));oldWrites=writes;before=JsonUtility.ToJson(director.Campaign);
                    PointAt(pointer,"성장 방향 선택");yield return PadPress(pad,GamepadButton.South);yield return PadPress(pad,GamepadButton.East);yield return null;
                    Assert.That(director.Hud.UnitDetailsOpen,Is.False);AssertGrowthFocus("성장 방향 선택");
                    Assert.That(writes,Is.EqualTo(oldWrites));Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));Assert.That(File.ReadAllBytes(director.Profiles.PathFor(0)),Is.EqualTo(disk));
                }
            }
            finally{InputSystem.RemoveDevice(pad);director.Preferences.TextScale=oldScale;}
        }
        [UnityTest,Category("CommercialSlice")]
        public IEnumerator GrowthModalRespectsUnlockTrainingOtherModalAndStoryAndRollsBackFailedSave()
        {
            PreparationGuidanceEpisode(3);var cless=director.Catalog.Characters.Single(c=>c.Id=="cless");var progress=director.Campaign.Get(cless.Id);progress.Level=9;
            int writes=0;director.PersistCampaign=_=>{writes++;return false;};ShowGrowthFixture(cless);yield return null;Click("성장 방향 선택");yield return null;
            Assert.That(FormationControl(GrowthChoiceName(cless,GrowthPath.Assault)).interactable,Is.False);Click("성장 방향 닫기");yield return null;
            progress.Level=10;director.TrainingMode=true;ShowGrowthFixture(cless);yield return null;Click("성장 방향 선택");yield return null;
            Assert.That(FormationControl(GrowthChoiceName(cless,GrowthPath.Bulwark)).interactable,Is.False);Click("성장 방향 닫기");yield return null;
            director.TrainingMode=false;ShowGrowthFixture(cless);yield return null;string before=JsonUtility.ToJson(director.Campaign);Click("성장 방향 선택");yield return null;
            Click(GrowthChoiceName(cless,GrowthPath.Assault));yield return null;Assert.That(writes,Is.EqualTo(1));Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));Assert.That(director.Hud.UnitDetailsOpen,Is.False);
            director.Hud.ShowHelp();yield return null;TryGrowthModalFixture(cless);Assert.That(director.Hud.HelpOpen,Is.True);Assert.That(director.Hud.UnitDetailsOpen,Is.False);director.Hud.CloseHelp();
            director.SelectedStage=CrossoverCampaign.FirstStage;director.ReplayStory(false);yield return null;Assert.That(director.StoryActive,Is.True);
            TryGrowthModalFixture(cless);Assert.That(director.Hud.UnitDetailsOpen,Is.False);Assert.That(director.StoryActive,Is.True);Assert.That(writes,Is.EqualTo(1));
            director.FinishStory();yield return null;
        }
    }
}
