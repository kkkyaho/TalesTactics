using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator SlimBattleHeaderLeavesMapGapsAndKeepsSixAllySelectionAtLargeText()
        {
            director.Preferences.TextScale=1.3f;
            director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1,2,3,4,5});
            director.BeginBattle();yield return null;yield return new WaitForEndOfFrame();
            var allies=director.Session.Units.Where(u=>u.Team==Team.Player).ToArray();Assert.That(allies.Length,Is.EqualTo(6));
            var resources=allies.Select(u=>$"{u.CurrentHP}/{u.CurrentMP}/{u.Moved}/{u.Acted}").ToArray();
            var header=(RectTransform)director.Hud.transform.Find("Header");
            Assert.That(header.GetComponent<UnityEngine.UI.Image>().enabled,Is.False,"Empty header space must reveal the battlefield.");
            Assert.That(header.Find("BattleObjectiveBackdrop"),Is.Null,"The long objective remains in the mission screen, not a permanent HUD card.");
            var turn=(RectTransform)header.Find("BattleTurnBackdrop");var turnBounds=SplitHudScreenRect(turn);
            Assert.That(turnBounds.center.x,Is.EqualTo(Screen.width*.5f).Within(2),"Turn portraits keep a stable centered home.");
            var headerBounds=SplitHudScreenRect(header);
            var gap=new Vector2(headerBounds.xMin+24,turnBounds.center.y);
            var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=gap},hits);
            Assert.That(hits,Is.Empty,"The reclaimed upper-left area must not consume battlefield input.");
            var portraits=header.GetComponentsInChildren<UnityEngine.UI.Button>().Where(x=>x.name.StartsWith("아군 선택: ")).ToArray();
            Assert.That(portraits.Length,Is.EqualTo(6));
            var inner=turn.rect;inner.xMin+=8;inner.xMax-=8;inner.yMin+=8;inner.yMax-=8;
            foreach(var portrait in portraits)
            {
                var corners=new Vector3[4];((RectTransform)portrait.transform).GetWorldCorners(corners);
                Assert.That(corners.All(c=>inner.Contains(turn.InverseTransformPoint(c))),Is.True,portrait.name);
            }
            var summary=(RectTransform)director.Hud.transform.Find("Unit");var fixedSummary=SplitHudScreenRect(summary);
            foreach(var unit in allies)
            {
                Click("아군 선택: "+unit.Data.Id);yield return null;yield return new WaitForEndOfFrame();
                Assert.That(director.Session.Active,Is.SameAs(unit));
                AssertSplitHudRectUnchanged(SplitHudScreenRect(summary),fixedSummary,"Selecting another ally must not move the status window.");
                var commands=(RectTransform)director.Hud.transform.Find("Commands");
                AssertSplitHudPanelOnScreen(commands);AssertSplitHudPanelContents(commands,8);
                var commandBounds=SplitHudScreenRect(commands);
                Assert.That(commandBounds.Overlaps(fixedSummary),Is.False,"Local commands must not cover the fixed status card.");
                foreach(var other in allies)
                    Assert.That(commandBounds.Overlaps(director.Board.UnitScreenBounds(other)),Is.False,"Local commands must avoid the full sprite including clothing: "+other.Data.Id);
                var foot=director.Board.BattleCamera.WorldToScreenPoint(director.Session.Grid[unit.Position].WorldPosition(director.Catalog.Rules.TileHeight));
                var nearest=new Vector2(Mathf.Clamp(foot.x,commandBounds.xMin,commandBounds.xMax),Mathf.Clamp(foot.y,commandBounds.yMin,commandBounds.yMax));
                Assert.That(Vector2.Distance(nearest,foot),Is.LessThan(220*director.Hud.GetComponent<Canvas>().scaleFactor),"Each selected ally's controls must stay nearby.");
                foreach(var label in header.GetComponentsInChildren<TMPro.TMP_Text>())
                {label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.text);}
            }
            Assert.That(allies.Select(u=>$"{u.CurrentHP}/{u.CurrentMP}/{u.Moved}/{u.Acted}"),Is.EqualTo(resources));
            Assert.That(director.Hud.transform.Find("BattleCommandDock"),Is.Null,"Status and commands no longer share a container.");
            Click("Move / 이동");yield return null;Assert.That(director.State,Is.TypeOf<MoveSelectionState>());
            director.State.Cancel();yield return null;
            Click("Skill / 스킬");yield return null;Assert.That(director.State,Is.TypeOf<ActionSelectionState>());
            Assert.That(((RectTransform)director.Hud.transform.Find("Commands")).rect.width,Is.GreaterThanOrEqualTo(300),"Skill descriptions retain enough reading width.");
        }
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator CompactUnitSummaryAndDetailsPreserveBattleAndInput()
        {
            director.BeginBattle();yield return null;yield return new WaitForEndOfFrame();
            var unit=director.Session.Active;int hp=unit.CurrentHP,mp=unit.CurrentMP;var state=director.State;
            var summary=(RectTransform)director.Hud.transform.Find("Unit");
            Assert.That(summary.rect.height,Is.LessThanOrEqualTo(180));
            Assert.That(summary.rect.width,Is.LessThanOrEqualTo(460));
            Assert.That(director.Hud.BattlefieldViewport.width,Is.GreaterThan(.9f),"A bottom card must not reserve the old full-height left column.");
            var fixedSummary=SplitHudScreenRect(summary);
            Assert.That(fixedSummary.xMin,Is.LessThan(Screen.width*.05f));Assert.That(fixedSummary.yMin,Is.LessThan(Screen.height*.05f));
            AssertSplitHudPanelContents(summary,8);
            Click("능력");yield return null;
            Assert.That(director.Hud.UnitDetailsOpen,Is.True);
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.IsInteractable()).All(b=>b.transform.IsChildOf(director.Hud.transform.Find("UnitDetails"))),Is.True,"Only the status modal may accept input");
            director.GetComponent<GamepadPointer>().Cancel();yield return null;yield return new WaitForEndOfFrame();
            Assert.That(director.Hud.UnitDetailsOpen,Is.False);Assert.That(director.State,Is.SameAs(state));
            var commands=(RectTransform)director.Hud.transform.Find("Commands");
            Assert.That(SplitHudScreenRect(commands).Overlaps(fixedSummary),Is.False,"Status and local commands occupy separate areas.");
            Assert.That(director.Hud.transform.Find("BattleCommandDock"),Is.Null);
            AssertSplitHudPanelContents(commands,8);
            foreach(var button in commands.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                var r=(RectTransform)button.transform;
                var icon=button.GetComponentInChildren<TacticalIcon>();var caption=button.GetComponentInChildren<TMPro.TMP_Text>();
                if(icon!=null)
                {
                    var iconRight=r.InverseTransformPoint(icon.rectTransform.TransformPoint(icon.rectTransform.rect.max)).x;
                    var textLeft=r.InverseTransformPoint(caption.rectTransform.TransformPoint(caption.rectTransform.rect.min)).x;
                    const float requiredGap=8f,transformTolerance=.001f;
                    Assert.That(textLeft-iconRight+transformTolerance,Is.GreaterThanOrEqualTo(requiredGap),"Command icon and caption require an 8 px gap: "+button.name);
                }
            }
            director.Board.RotateCamera(90);yield return null;yield return new WaitForEndOfFrame();
            AssertSplitHudRectUnchanged(SplitHudScreenRect(summary),fixedSummary,"Camera rotation must not move status.");
            AssertSplitHudPanelOnScreen(commands);Assert.That(SplitHudScreenRect(commands).Overlaps(fixedSummary),Is.False);
            director.SkillCommand();yield return null;yield return new WaitForEndOfFrame();
            Assert.That(commands.rect.width,Is.GreaterThanOrEqualTo(300));Assert.That(commands.rect.width,Is.LessThanOrEqualTo(400));
            AssertSplitHudRectUnchanged(SplitHudScreenRect(summary),fixedSummary,"Opening a skill submenu must not move status.");
            AssertSplitHudPanelOnScreen(commands);AssertSplitHudPanelContents(commands,8);
            director.State.Cancel();yield return null;
            director.AttackCommand();yield return null;yield return new WaitForEndOfFrame();
            Assert.That(summary.gameObject.activeInHierarchy,Is.False,"Target prediction replaces the fixed information card while aiming.");
            Assert.That(SplitHudScreenRect(commands).yMin,Is.LessThan(Screen.height*.05f));
            AssertSplitHudPanelOnScreen(commands);AssertSplitHudPanelContents(commands,8);
            director.State.Cancel();yield return null;yield return new WaitForEndOfFrame();
            Assert.That(summary.gameObject.activeInHierarchy,Is.True);AssertSplitHudRectUnchanged(SplitHudScreenRect(summary),fixedSummary,"Closing prediction restores the same status position.");
            Assert.That(unit.CurrentHP,Is.EqualTo(hp));Assert.That(unit.CurrentMP,Is.EqualTo(mp));
            director.Restart();yield return null;
            Assert.That(director.Hud.transform.Find("BattleCommandDock"),Is.Null);
            Assert.That(((RectTransform)director.Hud.transform.Find("PreparationContent")).rect.height,Is.GreaterThan(400));
            Assert.That(director.Hud.IntermissionHubOpen,Is.True);Click("출전 편성");yield return null;
            Assert.That(director.Hud.transform.Find("Unit").GetComponent<UnityEngine.UI.Image>().enabled,Is.True,"Preparation restores its ornate navigation frame.");
            Assert.That(director.Hud.transform.Find("Unit").GetComponent<UnityEngine.UI.Image>().raycastTarget,Is.True);
            Assert.That(director.Hud.transform.Find("Header").GetComponent<UnityEngine.UI.Image>().enabled,Is.True,"The preparation header regains its surface.");
            Assert.That(director.Hud.transform.Find("Header").GetComponent<UnityEngine.UI.Image>().raycastTarget,Is.True);
            Assert.That(director.Hud.transform.Find("PreparationContent/FormationModels").gameObject.activeInHierarchy,Is.True);
        }
    }
}
