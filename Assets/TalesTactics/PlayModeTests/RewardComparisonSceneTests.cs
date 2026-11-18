using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
        const string PreviousRewardCompanion="이전 보상 비교 동료",NextRewardCompanion="다음 보상 비교 동료";
        [Serializable] sealed class RewardComparisonCapture
        {
            public string scenario,item,character,file,sha256;
            public float textScale;
            public int screenWidth,screenHeight,imageWidth,imageHeight;
            public bool layoutPassed;
        }
        [Serializable] sealed class RewardComparisonManifest
        {
            public string scope="Editor PlayMode at the actual Game View size; synthetic prerequisite/victory/input fixtures exercise production result and comparison UI. These captures do not establish required desktop resolutions or natural campaign balance.";
            public string test,unityVersion,startedUtc;public bool complete;
            public RewardComparisonCapture[] captures;
        }
        sealed class RewardComparisonReview
        {
            public readonly string Output;
            public readonly RewardComparisonManifest Manifest;
            public readonly List<RewardComparisonCapture> Captures=new List<RewardComparisonCapture>();
            public RewardComparisonReview(string test)
            {
                Output=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Logs","RewardComparison20261117","PlayMode-"+Guid.NewGuid().ToString("N")));
                Manifest=new RewardComparisonManifest{test=test,unityVersion=Application.unityVersion,startedUtc=DateTime.UtcNow.ToString("o")};
                Directory.CreateDirectory(Output);Save();
            }
            public void Save(){Manifest.captures=Captures.ToArray();File.WriteAllText(Path.Combine(Output,"manifest.json"),JsonUtility.ToJson(Manifest,true));}
            public void Complete(){Manifest.complete=true;Save();}
        }
        void CaptureAndAssertRewardComparison(RewardComparisonReview review,string scenario,EquipmentData item)
        {
            Canvas.ForceUpdateCanvases();
            var panel=(RectTransform)director.Hud.transform.Find("UnitDetails");
            var capture=new RewardComparisonCapture{scenario=scenario,item=item.Id,character=RewardComparisonText("RewardComparisonCharacter"),
                textScale=director.Preferences.TextScale,screenWidth=Screen.width,screenHeight=Screen.height,file=(review.Captures.Count+1).ToString("00")+"-"+scenario+".png"};
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                capture.imageWidth=image.width;capture.imageHeight=image.height;
                byte[] bytes=image.EncodeToPNG();File.WriteAllBytes(Path.Combine(review.Output,capture.file),bytes);
                using(var sha=System.Security.Cryptography.SHA256.Create())capture.sha256=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");
            }
            finally{UnityEngine.Object.Destroy(image);}
            review.Captures.Add(capture);review.Save();
            string context=scenario+" / scale "+capture.textScale+" / actual Screen "+Screen.width+"x"+Screen.height+" / "+review.Output;
            Assert.That(capture.imageWidth,Is.EqualTo(capture.screenWidth),context);Assert.That(capture.imageHeight,Is.EqualTo(capture.screenHeight),context);
            AssertRewardComparisonViewport(panel,context);
            foreach(var text in panel.GetComponentsInChildren<TMPro.TMP_Text>())
            {
                text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,context+" / "+text.name+" / "+text.text);
                AssertRewardComparisonViewport(text.rectTransform,context+" / "+text.name);
            }
            foreach(var button in panel.GetComponentsInChildren<UnityEngine.UI.Button>())AssertRewardComparisonViewport((RectTransform)button.transform,context+" / "+button.name);
            capture.layoutPassed=true;review.Save();
        }
        static void AssertRewardComparisonViewport(RectTransform rect,string context)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            var canvas=rect.GetComponentInParent<Canvas>();var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            foreach(var corner in corners)
            {
                var point=RectTransformUtility.WorldToScreenPoint(camera,corner);
                Assert.That(point.x,Is.InRange(-1f,Screen.width+1f),context+" / x "+point.x);
                Assert.That(point.y,Is.InRange(-1f,Screen.height+1f),context+" / y "+point.y);
            }
        }
        void AssertRewardComparisonPointer(string name)
        {
            var button=FormationControl(name);var rect=(RectTransform)button.transform;
            var expected=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(button.gameObject),name);
            Assert.That(Vector2.Distance(director.GetComponent<GamepadPointer>().Position,expected),Is.LessThan(2),name+" / active controller pointer follows focus");
        }
        string RewardComparisonText(string name)=>director.Hud.transform.Find("UnitDetails").GetComponentsInChildren<TMPro.TMP_Text>().Single(t=>t.name==name).text;
        string RewardComparisonEffects(EquipmentData item)=>(string)typeof(BattleHud).GetMethod("ReadableBonus",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(director.Hud,new object[]{item});
        EquipmentData RewardComparisonItem(string id)=>director.Catalog.Equipment.Single(e=>e!=null&&e.Id==id);
        CharacterData[] RewardComparisonRoster(EquipmentData item)=>director.Catalog.Characters.Where(c=>director.IsJoined(c)&&EquipmentLoadout.CanEquip(c,item,item.Slot)).ToArray();
        static int[] RewardComparisonValues(Stats s)=>new[]{s.HP,s.MP,s.STR,s.MAG,s.DEF,s.MDF,s.SPD,s.MOV,s.JMP};
        void EquipRewardComparisonFixture(CharacterData character,string id)
        {
            var item=RewardComparisonItem(id);var owned=director.Campaign.Inventory.Find(e=>e.Id==id);
            if(owned==null)director.Campaign.Inventory.Add(new OwnedEquipment{Id=id,Count=1});else owned.Count++;
            director.Campaign.Get(character.Id).Equipment[(int)item.Slot]=id;
        }
        IEnumerator CompleteRewardComparisonFixture()
        {
            // Completion prerequisites and the victory are fixtures. Result rendering,
            // reward persistence, modal input and the mandatory ending use production paths.
            director.RewardRoll=()=>9999;director.BeginBattle();yield return null;
            Assert.That(director.Session,Is.Not.Null);SatisfyObjectiveFixture();
            yield return null;yield return new WaitForEndOfFrame();
            Assert.That(director.RewardPending,Is.False);Assert.That(director.Campaign.HasPendingAfterStory,Is.True);
        }
        void AssertRewardComparisonValues(CharacterData character,EquipmentData item)
        {
            Assert.That(RewardComparisonText("RewardComparisonCharacter"),Does.Contain(character.DisplayName.Split(' ')[0]));
            var progress=director.Campaign.Characters.Find(p=>p.Id==character.Id)??new CharacterProgress{Id=character.Id};
            var preview=new EquipmentLoadout(character,progress,director.Catalog.Equipment);
            var previous=preview.Get(item.Slot);int level=director.TrainingMode?25:Mathf.Clamp(progress.Level,1,50);
            bool promoted=!director.TrainingMode&&progress.Promoted;
            var before=RewardComparisonValues(preview.Preview(level,promoted));
            Assert.That(preview.Equip(item.Slot,item),Is.True);var after=RewardComparisonValues(preview.Preview(level,promoted));
            string text=Regex.Replace(RewardComparisonText("RewardComparisonStats"),"<[^>]+>","");
            string[] labels={"HP","MP","공격","마력","방어","마방","(?:민첩|속도)","이동","도약"};
            for(int i=0;i<labels.Length;i++)
            {
                var rows=Regex.Matches(text,@"(?:^|\s)"+labels[i]+@"\s+(-?\d+)\s*→\s*(-?\d+)\s*\(([+-]?\d+)\)");
                Assert.That(rows.Count,Is.EqualTo(1),character.Id+" / "+item.Id+" / "+labels[i]+" / "+text);
                var row=rows[0];Assert.That(int.Parse(row.Groups[1].Value),Is.EqualTo(before[i]),text);
                Assert.That(int.Parse(row.Groups[2].Value),Is.EqualTo(after[i]),text);
                Assert.That(int.Parse(row.Groups[3].Value),Is.EqualTo(after[i]-before[i]),text);
                if(after[i]>before[i])Assert.That(row.Groups[3].Value,Does.StartWith("+"),text);
            }
            Assert.That(RewardComparisonText("RewardItemEffects"),Is.EqualTo(RewardComparisonEffects(item)));
            Assert.That(RewardComparisonText("RewardEligibleCharacters"),Is.EqualTo(string.Join(" · ",RewardComparisonRoster(item).Select(c=>c.DisplayName.Split(' ')[0]))));
            if(previous==null)
            {
                Assert.That(RewardComparisonText("RewardComparisonEquipment"),Does.Contain("미장착"));
                Assert.That(RewardComparisonText("RewardComparisonCurrentEffects"),Does.Contain("미장착"));
            }
            else
            {
                Assert.That(RewardComparisonText("RewardComparisonEquipment"),Does.Contain(previous.DisplayName).And.Contain(item.DisplayName));
                Assert.That(RewardComparisonText("RewardComparisonCurrentEffects"),Does.Contain(RewardComparisonEffects(previous)));
                if(previous.Id==item.Id)
                {
                    Assert.That(RewardComparisonText("RewardComparisonEquipment"),Does.Contain("이미 같은 장비를 착용 중입니다."));
                    Assert.That(after,Is.EqualTo(before));
                }
            }
        }

        [UnityTest,Category("RewardComparison")]
        public IEnumerator RewardCompanionComparisonCyclesSavedLoadoutsWithKeyboardWithoutWritingOrSkippingEnding()
        {
            PreparationGuidanceEpisode(14);var item=RewardComparisonItem("tempered-armor");var roster=RewardComparisonRoster(item);
            Assert.That(roster.Length,Is.EqualTo(10));
            for(int i=0;i<roster.Length;i++)
            {
                var progress=director.Campaign.Get(roster[i].Id);progress.Level=20+i;progress.Promoted=true;
                progress.Trait=i%2==0?TacticalTrait.Swift:TacticalTrait.Healer;progress.Growth=i%2==0?GrowthPath.Assault:GrowthPath.Bulwark;
                EquipRewardComparisonFixture(roster[i],"reinforced-armor");
            }
            int writes=0;var persist=director.PersistCampaign;director.PersistCampaign=save=>{writes++;return persist(save);};
            yield return CompleteRewardComparisonFixture();
            string original=JsonUtility.ToJson(director.Campaign);int savedWrites=writes;byte[] disk=File.ReadAllBytes(director.Profiles.PathFor(0));
            var review=new RewardComparisonReview(nameof(RewardCompanionComparisonCyclesSavedLoadoutsWithKeyboardWithoutWritingOrSkippingEnding));
            string cardName="보상 상세: "+item.Id;var card=FormationControl(cardName);
            foreach(float scale in new[]{1f,1.3f})
            {
                director.Preferences.TextScale=scale;Click(cardName);
                var openedSize=((RectTransform)director.Hud.transform.Find("UnitDetails")).rect.size;
                yield return null;yield return new WaitForEndOfFrame();
                Assert.That(((RectTransform)director.Hud.transform.Find("UnitDetails")).rect.size,Is.EqualTo(openedSize),"The modal must use its final size before focus, including when the prior content awaits destruction.");
                Assert.That(FormationControl(PreviousRewardCompanion).IsInteractable(),Is.True);Assert.That(FormationControl(NextRewardCompanion).IsInteractable(),Is.True);
                for(int i=0;i<roster.Length;i++)
                {
                    AssertRewardComparisonValues(roster[i],item);
                    CaptureAndAssertRewardComparison(review,"armor-"+roster[i].Id,item);
                    Assert.That(FormationControl("결과 계속").IsInteractable(),Is.False);
                    Click(NextRewardCompanion);yield return new WaitForEndOfFrame();
                    Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(original));Assert.That(writes,Is.EqualTo(savedWrites));
                }
                AssertRewardComparisonValues(roster[0],item);Click(PreviousRewardCompanion);yield return new WaitForEndOfFrame();
                AssertRewardComparisonValues(roster.Last(),item);
                Click("보상 상세 닫기");yield return null;Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(card.gameObject));
            }
            Click(cardName);yield return new WaitForEndOfFrame();Click(PreviousRewardCompanion);yield return new WaitForEndOfFrame();
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                EventSystem.current.SetSelectedGameObject(FormationControl("보상 상세 닫기").gameObject);
                for(int i=0;i<3&&EventSystem.current.currentSelectedGameObject!=FormationControl(NextRewardCompanion).gameObject;i++)yield return KeyboardPress(keyboard,Key.Tab);
                Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(FormationControl(NextRewardCompanion).gameObject),"Tab reaches the comparison controls inside the modal.");
                yield return KeyboardPress(keyboard,Key.Enter);yield return new WaitForEndOfFrame();AssertRewardComparisonValues(roster[0],item);
                yield return KeyboardPress(keyboard,Key.Escape);yield return new WaitForEndOfFrame();
                Assert.That(director.Hud.UnitDetailsOpen,Is.False);Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(card.gameObject));
            }
            finally{InputSystem.RemoveDevice(keyboard);}
            var pad=InputSystem.AddDevice<Gamepad>();
            try
            {
                var pointer=director.GetComponent<GamepadPointer>();PointAt(pointer,cardName);
                yield return PadPress(pad,GamepadButton.South);yield return new WaitForEndOfFrame();
                Assert.That(pointer.Active,Is.True);Assert.That(director.Hud.UnitDetailsOpen,Is.True);
                AssertRewardComparisonPointer("보상 상세 닫기");AssertRewardComparisonValues(roster[0],item);
                yield return PadPress(pad,GamepadButton.DpadDown);AssertRewardComparisonPointer(NextRewardCompanion);
                yield return PadPress(pad,GamepadButton.South);yield return new WaitForEndOfFrame();AssertRewardComparisonValues(roster[1],item);
                yield return PadPress(pad,GamepadButton.DpadLeft);AssertRewardComparisonPointer(PreviousRewardCompanion);
                yield return PadPress(pad,GamepadButton.South);yield return new WaitForEndOfFrame();AssertRewardComparisonValues(roster[0],item);
                yield return PadPress(pad,GamepadButton.East);yield return new WaitForEndOfFrame();
                Assert.That(director.Hud.UnitDetailsOpen,Is.False);AssertRewardComparisonPointer(cardName);
            }
            finally{InputSystem.RemoveDevice(pad);}
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(original));Assert.That(writes,Is.EqualTo(savedWrites));
            Assert.That(File.ReadAllBytes(director.Profiles.PathFor(0)),Is.EqualTo(disk));
            Assert.That(FormationControl("결과 계속").IsInteractable(),Is.True);
            Click("결과 계속");yield return null;Assert.That(director.StoryActive,Is.True);
            int lines=0;while(director.StoryActive&&lines++<100){director.AdvanceStory();yield return null;}
            Assert.That(director.StoryActive,Is.False);Assert.That(director.Campaign.HasPendingAfterStory,Is.False);
            Assert.That(director.SelectedStage,Is.EqualTo(CrossoverCampaign.FirstStage+14));review.Complete();
        }

        [UnityTest,Category("RewardComparison")]
        public IEnumerator RewardCompanionComparisonShowsLowerStatsReplacedEffectsAndSameEquipment()
        {
            PreparationGuidanceEpisode(19);var item=RewardComparisonItem("healer-badge");var roster=RewardComparisonRoster(item);
            Assert.That(roster.Length,Is.GreaterThan(1));
            foreach(var character in roster.Take(2))
            {var progress=director.Campaign.Get(character.Id);progress.Level=25;progress.Promoted=true;progress.Trait=TacticalTrait.Focus;progress.Growth=GrowthPath.Bulwark;}
            EquipRewardComparisonFixture(roster[0],"focus-charm");EquipRewardComparisonFixture(roster[1],item.Id);
            int writes=0;var persist=director.PersistCampaign;director.PersistCampaign=save=>{writes++;return persist(save);};
            yield return CompleteRewardComparisonFixture();string original=JsonUtility.ToJson(director.Campaign);int savedWrites=writes;
            var review=new RewardComparisonReview(nameof(RewardCompanionComparisonShowsLowerStatsReplacedEffectsAndSameEquipment));
            foreach(float scale in new[]{1f,1.3f})
            {
                director.Preferences.TextScale=scale;Click("보상 상세: "+item.Id);yield return null;yield return new WaitForEndOfFrame();AssertRewardComparisonValues(roster[0],item);
                var focus=RewardComparisonItem("focus-charm");Assert.That(focus.Effect,Is.EqualTo(EquipmentEffect.EfficientCasting));Assert.That(item.Effect,Is.EqualTo(EquipmentEffect.Restorative));
                Assert.That(RewardComparisonText("RewardComparisonCurrentEffects"),Does.Contain("MP 비용 -15%"));
                Assert.That(RewardComparisonText("RewardItemEffects"),Does.Contain("회복량 +20%").And.Not.Contain("MP 비용 -15%"));
                string stats=Regex.Replace(RewardComparisonText("RewardComparisonStats"),"<[^>]+>","");
                Assert.That(stats,Does.Match(@"(?:민첩|속도)\s+\d+\s*→\s*\d+\s*\(-2\)"));
                Assert.That(stats,Does.Match(@"이동\s+\d+\s*→\s*\d+\s*\(\+1\)"));
                CaptureAndAssertRewardComparison(review,"accessory-tradeoff",item);
                Click(NextRewardCompanion);yield return new WaitForEndOfFrame();AssertRewardComparisonValues(roster[1],item);
                Assert.That(RewardComparisonText("RewardComparisonCurrentEffects"),Does.Contain("회복량 +20%"));
                CaptureAndAssertRewardComparison(review,"accessory-already-equipped",item);
                Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(original));Assert.That(writes,Is.EqualTo(savedWrites));
                Click("보상 상세 닫기");yield return null;
                Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(original));Assert.That(writes,Is.EqualTo(savedWrites));
            }
            review.Complete();
        }

        [UnityTest,Category("RewardComparison")]
        public IEnumerator RewardCompanionComparisonFiltersUnjoinedAndDisablesSingleOrEmptyRecipients()
        {
            PreparationGuidanceEpisode(1);yield return CompleteRewardComparisonFixture();
            var joined=director.Catalog.Characters.First(director.IsJoined);var hidden=director.Catalog.Characters.First(c=>!director.IsJoined(c));
            var item=UnityEngine.Object.Instantiate(RewardComparisonItem("leather-armor"));var originalEquipment=director.Catalog.Equipment;
            // Keep the controlled exclusive item in the isolated catalog so the detached
            // loadout exercises a real candidate. Do not alter any authored asset.
            director.Catalog.Equipment=originalEquipment.Select(e=>e==null||e.Id!=item.Id?e:item).ToArray();
            director.Campaign.Characters.RemoveAll(p=>p.Id==joined.Id);
            var open=typeof(BattleHud).GetMethod("ShowRewardItemDetails",BindingFlags.Instance|BindingFlags.NonPublic);
            string original=JsonUtility.ToJson(director.Campaign);int writes=0;director.PersistCampaign=_=>{writes++;return false;};
            var review=new RewardComparisonReview(nameof(RewardCompanionComparisonFiltersUnjoinedAndDisablesSingleOrEmptyRecipients));
            try
            {
                foreach(float scale in new[]{1f,1.3f})
                foreach(string owner in new[]{joined.Id,hidden.Id})
                {
                    director.Preferences.TextScale=scale;item.ExclusiveCharacterId=owner;
                    open.Invoke(director.Hud,new object[]{new BattleDirector.ItemResult{Item=item,Count=1},null});
                    yield return null;yield return new WaitForEndOfFrame();
                    Assert.That(FormationControl(PreviousRewardCompanion).IsInteractable(),Is.False);Assert.That(FormationControl(NextRewardCompanion).IsInteractable(),Is.False);
                    Assert.That(RewardComparisonText("RewardItemEffects"),Is.EqualTo(RewardComparisonEffects(item)));
                    if(owner==joined.Id)AssertRewardComparisonValues(joined,item);
                    else
                    {
                        Assert.That(RewardComparisonText("RewardEligibleCharacters"),Is.EqualTo("현재 합류한 동료 중 장착 가능한 캐릭터가 없습니다."));
                        Assert.That(RewardComparisonText("RewardComparisonCharacter"),Does.Not.Contain(hidden.DisplayName.Split(' ')[0]));
                        var rows=director.Hud.transform.Find("UnitDetails").GetComponentsInChildren<TMPro.TMP_Text>().Where(t=>t.name=="RewardComparisonStats");
                        Assert.That(rows.All(t=>!t.text.Contains("→")),Is.True,"No recipient must not leave the previous companion's numbers visible.");
                    }
                    CaptureAndAssertRewardComparison(review,owner==joined.Id?"single-recipient":"no-joined-recipient",item);
                    Assert.That(FormationControl("결과 계속").IsInteractable(),Is.False);
                    Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(original));Assert.That(writes,Is.Zero);
                    Assert.That(director.Campaign.Characters.Any(p=>p.Id==joined.Id),Is.False,"Inspecting a default progress entry must not add it to the save.");
                    Click("보상 상세 닫기");yield return null;
                }
                Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(original));Assert.That(writes,Is.Zero);review.Complete();
            }
            finally{director.Hud.CloseUnitDetails();director.Catalog.Equipment=originalEquipment;UnityEngine.Object.Destroy(item);}
        }
    }
}
