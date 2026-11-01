using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator FixedBattleStatusStaysPutAcrossAllySelectionCameraAndSkillBrowsing()
        {
            director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1,2,3,4,5});director.Preferences.SeenContextHints=7;
            director.BeginBattle();yield return null;director.StopAllCoroutines();yield return new WaitForEndOfFrame();
            var original=director.Session.Active;
            // A first selection legitimately begins that ally's turn. Initialize this before the
            // strict browsing invariant; later re-selection must leave every captured field unchanged.
            foreach(var ally in director.Session.Units.Where(u=>u.Team==Team.Player))Assert.That(director.SelectPlayerUnit(ally),Is.True);
            Assert.That(director.SelectPlayerUnit(original),Is.True);yield return new WaitForEndOfFrame();

            var status=(RectTransform)director.Hud.transform.Find("Unit");var commands=(RectTransform)director.Hud.transform.Find("Commands");
            var fixedBounds=HudReviewBounds(status);string before=SkillRosterVfxFixture.State(director.Session.Units);
            int random=director.Session.Resolver.RandomDraws;
            Assert.That(director.Session.Units.Count(u=>u.Team==Team.Player),Is.EqualTo(6));
            Assert.That(director.Hud.BattlefieldViewport.xMin,Is.LessThan(.05f),"The former dock must not reserve the left third of the map.");
            var other=director.Session.Units.Last(u=>u.Team==Team.Player);
            Assert.That(director.SelectPlayerUnit(other),Is.True);yield return new WaitForEndOfFrame();
            Assert.That(Vector2.Distance(fixedBounds.center,HudReviewBounds(status).center),Is.LessThan(1));
            Assert.That(director.SelectPlayerUnit(original),Is.True);yield return new WaitForEndOfFrame();
            var skill=commands.GetComponentsInChildren<Button>().Single(b=>b.name=="Skill / 스킬");HudReviewSubmit(skill);
            yield return new WaitForEndOfFrame();Assert.That(director.State,Is.InstanceOf<ActionSelectionState>());
            Assert.That(Vector2.Distance(fixedBounds.center,HudReviewBounds(status).center),Is.LessThan(1));
            director.State.Cancel();yield return new WaitForEndOfFrame();
            var menuBefore=HudReviewBounds(commands);director.Board.ZoomCamera(.65f);
            var middle=new Vector2(Screen.width*.5f,Screen.height*.5f);
            director.Board.BeginCameraDrag(middle);director.Board.DragCamera(middle+new Vector2(160,40));director.Board.EndCameraDrag();
            yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
            Assert.That(Vector2.Distance(fixedBounds.center,HudReviewBounds(status).center),Is.LessThan(1));
            Assert.That(Vector2.Distance(menuBefore.center,HudReviewBounds(commands).center),Is.GreaterThan(2),"The local command menu must track its actor.");
            Assert.That(HudReviewBounds(commands).Overlaps(HudReviewBounds(status)),Is.False);
            Assert.That(SkillRosterVfxFixture.State(director.Session.Units),Is.EqualTo(before));
            Assert.That(director.Session.Resolver.RandomDraws,Is.EqualTo(random));
        }
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator SimplifiedBattleHudRetainsMissionAccessAndAllFiveLocalCommands()
        {
            director.Preferences.SeenContextHints=7;director.BeginBattle();yield return null;director.StopAllCoroutines();yield return new WaitForEndOfFrame();
            Assert.That(director.Hud.GetComponentsInChildren<TMP_Text>().Any(t=>t.isActiveAndEnabled&&t.name=="Battle objective"),Is.False);
            var commands=director.Hud.transform.Find("Commands");
            foreach(string name in new[]{"Move / 이동","Attack / 공격","Skill / 스킬","Guard / 가드","Wait / 방향 선택"})
                Assert.That(commands.GetComponentsInChildren<Button>().Count(b=>b.name==name&&b.isActiveAndEnabled),Is.EqualTo(1),name);
            Assert.That(commands.GetComponentsInChildren<Button>().Any(b=>b.name=="아군 턴 종료"),Is.False,"Whole-team completion belongs to the global turn controls.");
            var mission=director.Hud.GetComponentsInChildren<Button>().Single(b=>b.name=="임무 · 적 정보");
            HudReviewSubmit(mission);yield return new WaitForEndOfFrame();
            Assert.That(director.Hud.MissionOpen,Is.True,"Removing the persistent paragraph must preserve access to mission information.");
        }
        static Rect HudReviewBounds(RectTransform rect)
        {var c=new Vector3[4];rect.GetWorldCorners(c);return Rect.MinMaxRect(c.Min(p=>p.x),c.Min(p=>p.y),c.Max(p=>p.x),c.Max(p=>p.y));}
        static void HudReviewSubmit(Button button)
        {
            Assert.That(button.IsInteractable(),Is.True);var hits=new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=HudReviewBounds((RectTransform)button.transform).center},hits);
            Assert.That(hits.Count,Is.GreaterThan(0));
            Assert.That(hits[0].gameObject==button.gameObject||hits[0].gameObject.transform.IsChildOf(button.transform),Is.True,"Button center must be usable: "+button.name);
            ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
        }
    }
}
