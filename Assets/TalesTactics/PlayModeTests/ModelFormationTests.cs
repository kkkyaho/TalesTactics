using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        UnityEngine.UI.Button FormationControl(string name)=>director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name==name);
        void PointAndSubmit(string name)
        {
            var b=FormationControl(name);var r=(RectTransform)b.transform;
            b.GetComponentInParent<FormationRosterScroll>()?.EnsureVisible(r);Canvas.ForceUpdateCanvases();
            var p=director.GetComponent<GamepadPointer>();p.MoveTo(RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center)));p.Submit();
        }
        [UnityTest] public IEnumerator ModelFormationPointerInspectionAndToggleAreSeparateAndCapped()
        {
            director.Deployment.Clear();director.Deployment.AddRange(TacticalDevelopment.Recommended(director.Catalog));director.Hud.ShowDeployment();yield return null;
            var party=director.Deployment.ToArray();int index=System.Array.FindIndex(director.Catalog.Characters,c=>c.Id=="velvet");var c=director.Catalog.Characters[index];
            PointAndSubmit("○ "+c.DisplayName);yield return null;
            Assert.That(director.Deployment,Is.EqualTo(party));
            Assert.That(FormationControl("출전 편성에 추가").interactable,Is.False);
            Assert.That(FormationControl("편성 변경: velvet").interactable,Is.False);
            PointAndSubmit("편성 변경: cless");yield return null;
            Assert.That(director.Deployment.Count,Is.EqualTo(party.Length-1));
            PointAndSubmit("편성 변경: velvet");yield return null;
            Assert.That(director.Deployment.Contains(index),Is.True);Assert.That(director.Deployment.Count,Is.EqualTo(director.Catalog.Rules.MaxDeployment));
            Assert.That(director.Deployment.Distinct().Count(),Is.EqualTo(director.Deployment.Count));
            Click("전체 해제");yield return null;
            Assert.That(director.Deployment,Is.Empty);Assert.That(FormationControl("전투 시작").interactable,Is.False);
            Click("균형 6인 추천 편성");yield return null;
            Assert.That(director.Deployment,Is.EqualTo(TacticalDevelopment.Recommended(director.Catalog)));
        }
        [UnityTest] public IEnumerator ModelFormationFiltersSortAndPortraitFollowCharacterIdentity()
        {
            director.TrainingMode=false;director.Campaign=new CampaignSave();
            string original=JsonUtility.ToJson(director.Campaign);director.Hud.ShowDeployment();yield return null;
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(original),"Inspecting a roster must not add progress records");
            int jade=System.Array.FindIndex(director.Catalog.Characters,c=>c.Id=="jade");director.Campaign.Get("jade").Level=12;
            director.Hud.ShowDeployment();yield return null;var party=director.Deployment.ToArray();
            Click("편성 정렬");yield return null;
            var models=director.Hud.transform.Find("PreparationContent/FormationModels/Viewport/Content");
            Assert.That(models.GetChild(0).name,Is.EqualTo("ModelCell-jade"));
            Click((director.Deployment.Contains(jade)?"● ":"○ ")+director.Catalog.Characters[jade].DisplayName);yield return null;
            var portrait=director.Hud.transform.Find("Commands/CharacterPortrait/PortraitArt").GetComponent<UnityEngine.UI.RawImage>();
            Assert.That(portrait.texture.name,Is.EqualTo("FormationPortraits"));Assert.That(portrait.uvRect.x,Is.EqualTo(.6f).Within(.001f));
            Click("병과 필터");yield return null;Click("병과 필터");yield return null;
            models=director.Hud.transform.Find("PreparationContent/FormationModels/Viewport/Content");
            Assert.That(models.Cast<Transform>().Select(t=>t.name),Is.EquivalentTo(new[]{"ModelCell-natalia","ModelCell-shionne"}));
            Assert.That(director.Deployment,Is.EqualTo(party));
            Click("병과 필터");yield return null;Click("병과 필터");yield return null;
            Click("전체 해제");yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("조건에 맞는 캐릭터")),Is.True);
            Click("균형 6인 추천 편성");yield return null;
            Assert.That(director.Hud.transform.Find("PreparationContent/FormationModels/Viewport/Content").childCount,Is.EqualTo(6));
        }
        [UnityTest] public IEnumerator FormationWheelReachesLaterCardsAndKeepsOffsetAfterInspectionAndDeployment()
        {
            director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1});director.Hud.ShowDeployment();
            yield return null;yield return new WaitForEndOfFrame();
            var roster=director.Hud.GetComponentInChildren<FormationRosterScroll>();
            var scroll=roster.GetComponent<UnityEngine.UI.ScrollRect>();
            Assert.That(roster.Content.childCount,Is.EqualTo(director.Catalog.Characters.Length));
            Assert.That(scroll.horizontal,Is.False);Assert.That(scroll.vertical,Is.True);
            Assert.That(roster.Viewport.GetComponent<UnityEngine.UI.Mask>(),Is.Not.Null);
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name=="이전 캐릭터"||b.name=="다음 캐릭터"),Is.False);
            var origin=roster.Offset;
            var input=EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Assert.That(input.scrollDeltaPerTick,Is.EqualTo(6));
            var wheel=new PointerEventData(EventSystem.current){position=roster.Viewport.TransformPoint(roster.Viewport.rect.center),scrollDelta=new Vector2(0,-input.scrollDeltaPerTick)};
            ExecuteEvents.ExecuteHierarchy(roster.Content.GetChild(0).gameObject,wheel,ExecuteEvents.scrollHandler);
            yield return null;yield return new WaitForEndOfFrame();
            Assert.That(roster.Offset-origin,Is.EqualTo(72).Within(1),"An ordinary wheel notch should move part of a row, not jump a page");
            Assert.That(roster.Offset-origin,Is.LessThan(((RectTransform)roster.Content.GetChild(0)).rect.height*.5f));
            for(int notch=0;notch<11;notch++)
            {
                ExecuteEvents.ExecuteHierarchy(roster.Content.gameObject,wheel,ExecuteEvents.scrollHandler);
                yield return null;
            }
            yield return new WaitForEndOfFrame();
            Assert.That(roster.Offset,Is.GreaterThan(origin+100),"Wheel input must scroll the roster rather than just change a page number");
            var card=(RectTransform)roster.Content.GetChild(6);string id=card.name.Substring("ModelCell-".Length);
            Assert.That(RectTransformUtility.RectangleContainsScreenPoint(roster.Viewport,card.TransformPoint(card.rect.center)),Is.True,"Seventh card is visible after scrolling");
            var inspect=card.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>!b.name.StartsWith("편성 변경:"));
            var party=director.Deployment.ToArray();float offset=roster.Offset;
            var pointer=director.GetComponent<GamepadPointer>();var buttonRect=(RectTransform)inspect.transform;
            pointer.MoveTo(buttonRect.TransformPoint(buttonRect.rect.center));pointer.Submit();
            yield return null;yield return new WaitForEndOfFrame();
            roster=director.Hud.GetComponentInChildren<FormationRosterScroll>();
            Assert.That(roster.Offset,Is.EqualTo(offset).Within(1));Assert.That(director.Deployment,Is.EqualTo(party));
            var toggle=FormationControl("편성 변경: "+id);var toggleRect=(RectTransform)toggle.transform;
            pointer.MoveTo(toggleRect.TransformPoint(toggleRect.rect.center));pointer.Submit();
            yield return null;yield return new WaitForEndOfFrame();
            roster=director.Hud.GetComponentInChildren<FormationRosterScroll>();
            Assert.That(roster.Offset,Is.EqualTo(offset).Within(1));
            int index=System.Array.FindIndex(director.Catalog.Characters,c=>c.Id==id);
            Assert.That(director.Deployment.Contains(index),Is.True);
            Click("선택 캐릭터 장비");yield return null;director.Hud.ShowDeployment();yield return null;yield return new WaitForEndOfFrame();
            Assert.That(director.Hud.GetComponentInChildren<FormationRosterScroll>().Offset,Is.EqualTo(offset).Within(1),"Returning from equipment keeps the roster position");
            Click("병과 필터");yield return null;yield return new WaitForEndOfFrame();
            Assert.That(director.Hud.GetComponentInChildren<FormationRosterScroll>().Offset,Is.EqualTo(0).Within(1),"Changing the filter starts at the first matching row");
        }
        [UnityTest] public IEnumerator FormationFocusRevealsWholeLaterCardWithoutChangingDeployment()
        {
            director.Hud.ShowDeployment();yield return null;yield return new WaitForEndOfFrame();
            var roster=director.Hud.GetComponentInChildren<FormationRosterScroll>();
            var party=director.Deployment.ToArray();var card=(RectTransform)roster.Content.GetChild(roster.Content.childCount-1);
            var control=card.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>!b.name.StartsWith("편성 변경:"));
            director.GetComponent<GamepadPointer>().FocusButton(control);Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
            Assert.That(roster.Offset,Is.GreaterThan(0));
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(roster.Viewport,card);
            Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(roster.Viewport.rect.yMin-1));
            Assert.That(bounds.max.y,Is.LessThanOrEqualTo(roster.Viewport.rect.yMax+1));
            Assert.That(director.Deployment,Is.EqualTo(party),"Revealing a focused card is not a deployment command");
            var first=roster.Content.GetChild(0).GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>!b.name.StartsWith("편성 변경:"));
            director.GetComponent<GamepadPointer>().FocusButton(first);yield return new WaitForEndOfFrame();
            Assert.That(roster.Offset,Is.EqualTo(0).Within(1));
        }
        [UnityTest] public IEnumerator FormationWheelClearsOnlyClippedRosterKeyboardFocus()
        {
            director.Hud.ShowDeployment();yield return null;yield return new WaitForEndOfFrame();
            var roster=director.Hud.GetComponentInChildren<FormationRosterScroll>();
            var first=roster.Content.GetChild(0).GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name.StartsWith("편성 변경:"));
            var events=EventSystem.current;events.SetSelectedGameObject(first.gameObject);
            var party=director.Deployment.ToArray();
            var wheel=new PointerEventData(events){position=roster.Viewport.TransformPoint(roster.Viewport.rect.center),scrollDelta=new Vector2(0,-6)};
            for(int notch=0;notch<12;notch++)
            {
                ExecuteEvents.ExecuteHierarchy(roster.Content.gameObject,wheel,ExecuteEvents.scrollHandler);
                yield return null;
            }
            yield return null;yield return new WaitForEndOfFrame();
            Assert.That(roster.Offset,Is.GreaterThan(100));
            Assert.That(GamepadPointer.ButtonVisible(first),Is.False);
            Assert.That(events.currentSelectedGameObject,Is.Null,"A clipped card must not retain the target for keyboard Enter");
            Assert.That(director.Deployment,Is.EqualTo(party),"Scrolling must neither deploy nor inspect another character");

            var filter=FormationControl("병과 필터");events.SetSelectedGameObject(filter.gameObject);
            wheel.scrollDelta=new Vector2(0,6);
            for(int notch=0;notch<12;notch++)
            {
                ExecuteEvents.ExecuteHierarchy(roster.Content.gameObject,wheel,ExecuteEvents.scrollHandler);
                yield return null;
            }
            yield return null;yield return new WaitForEndOfFrame();
            Assert.That(roster.Offset,Is.EqualTo(0).Within(1));
            Assert.That(events.currentSelectedGameObject,Is.EqualTo(filter.gameObject),"Scrolling the roster must not clear focus in unrelated menus");
            Assert.That(director.Deployment,Is.EqualTo(party));
        }
        [UnityTest] public IEnumerator FormationArrowNavigationRevealsNextRowWithoutDeploying()
        {
            director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1});director.Hud.ShowDeployment();
            yield return null;yield return new WaitForEndOfFrame();
            var roster=director.Hud.GetComponentInChildren<FormationRosterScroll>();
            var party=director.Deployment.ToArray();var events=EventSystem.current;
            var from=roster.Content.GetChild(3).GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name.StartsWith("편성 변경:"));
            var card=(RectTransform)roster.Content.GetChild(6);
            var target=card.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>!b.name.StartsWith("편성 변경:"));
            Assert.That(GamepadPointer.ButtonVisible(target),Is.False);
            events.SetSelectedGameObject(from.gameObject);
            ExecuteEvents.Execute(from.gameObject,new AxisEventData(events){moveDir=MoveDirection.Down,moveVector=Vector2.down},ExecuteEvents.moveHandler);
            yield return null;yield return new WaitForEndOfFrame();
            Assert.That(events.currentSelectedGameObject,Is.EqualTo(target.gameObject),"Automatic down navigation should reach the next roster row");
            Assert.That(roster.Offset,Is.GreaterThan(0));Assert.That(GamepadPointer.ButtonVisible(target),Is.True);
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(roster.Viewport,card);
            Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(roster.Viewport.rect.yMin-1));
            Assert.That(bounds.max.y,Is.LessThanOrEqualTo(roster.Viewport.rect.yMax+1));
            Assert.That(director.Deployment,Is.EqualTo(party));
        }
        [UnityTest] public IEnumerator ModelFormationGearTraitsAndModalUseRealPreviewAndPreserveParty()
        {
            director.TrainingMode=false;director.Campaign=new CampaignSave();var c=director.Catalog.Characters[0];var p=director.Campaign.Get(c.Id);
            p.Level=7;p.Trait=TacticalTrait.Swift;
            var item=director.Catalog.Equipment.First(e=>e.Slot==EquipmentSlot.Weapon&&e.Weapon==c.Weapon);
            director.Campaign.Inventory.RemoveAll(e=>e.Id==item.Id);director.Campaign.Inventory.Add(new OwnedEquipment{Id=item.Id,Count=1});p.Equipment[0]=item.Id;
            var armor=director.Catalog.Equipment.First(e=>e.Slot==EquipmentSlot.Armor);
            director.Campaign.Inventory.RemoveAll(e=>e.Id==armor.Id);director.Campaign.Inventory.Add(new OwnedEquipment{Id=armor.Id,Count=1});
            director.Hud.ShowDeployment();yield return null;var party=director.Deployment.ToArray();
            Assert.That(FormationControl("장비 슬롯: Weapon").GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text==item.DisplayName),Is.True);
            Click("편성 캐릭터 능력");yield return null;
            Assert.That(director.Hud.UnitDetailsOpen,Is.True);
            var before=director.Deployment.ToArray();FormationControl("편성 변경: "+c.Id).onClick.Invoke();
            Assert.That(director.Deployment,Is.EqualTo(before),"Modal blocks roster changes even through direct callbacks");
            var s=new EquipmentLoadout(c,p,director.Catalog.Equipment,director.Campaign).Preview(7,false);
            var rendered=director.Hud.transform.Find("UnitDetails").GetComponentsInChildren<TMPro.TMP_Text>();
            Assert.That(rendered.Single(t=>t.name=="StatusValueMOV").text,Is.EqualTo(s.MOV.ToString()));
            Assert.That(rendered.Single(t=>t.name=="StatusValueSTR").text,Is.EqualTo(s.STR.ToString()));
            director.GetComponent<GamepadPointer>().Cancel();yield return null;Assert.That(director.Hud.UnitDetailsOpen,Is.False);
            Click("장비 슬롯: Armor");yield return null;
            var candidates=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.name.StartsWith("장착 후보: ")).Select(b=>b.name).ToArray();
            Assert.That(candidates,Does.Contain("장착 후보: "+armor.DisplayName),"The formation armor shortcut must open the armor inventory");
            Assert.That(candidates,Does.Not.Contain("장착 후보: "+item.DisplayName));
            Click("출전 준비로");yield return null;Assert.That(director.Deployment,Is.EqualTo(party));
            director.Hud.ShowIntermissionHub();yield return null;Click("출전 편성");yield return null;
            Click("전투 시작");yield return null;
            Assert.That(director.Session.Units.Where(u=>u.Team==Team.Player).Select(u=>u.Data.Id),Is.EquivalentTo(party.Select(i=>director.Catalog.Characters[i].Id)));
        }
    }
}
