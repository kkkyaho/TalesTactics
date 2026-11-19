using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public sealed class MobileSafeAreaTests
    {
        [TestCase(1920,1080,80,24), TestCase(2340,1080,108,24), TestCase(1280,720,0,0), Category("AndroidFoundation")] public void SafeAreaInsetsKeepExistingMenuAnchorsAndAllowDisplayChanges(float width,float height,float insetX,float insetY)
        {
            var root=new GameObject("Safe area fixture",typeof(RectTransform),typeof(Canvas));
            try
            {
                var menu=new GameObject("Existing menu",typeof(RectTransform)).GetComponent<RectTransform>();
                menu.SetParent(root.transform,false);menu.anchorMin=new Vector2(.1f,.2f);menu.anchorMax=new Vector2(.9f,.8f);
                menu.offsetMin=new Vector2(12,18);menu.offsetMax=new Vector2(-12,-18);
                var overlay=new GameObject("Screen pixel overlay",typeof(RectTransform),typeof(Canvas));overlay.transform.SetParent(root.transform,false);
                var layout=root.AddComponent<MobileUiLayout>();var content=layout.Initialize((RectTransform)root.transform);
                Assert.That(overlay.transform.parent,Is.SameAs(root.transform),"World damage numbers keep their screen-pixel origin.");
                // Touch -> mouse -> touch can recreate the wrapper without a screen
                // change. Cached display values must not leave the new root collapsed.
                var originalContent=content;var expectedMin=content.anchorMin;var expectedMax=content.anchorMax;
                layout.Restore((RectTransform)root.transform);
                Assert.That(layout.ContentRoot,Is.Null);Assert.That(menu.parent,Is.SameAs(root.transform));
                content=layout.Initialize((RectTransform)root.transform);
                Assert.That(content,Is.Not.SameAs(originalContent));
                Assert.That(content.anchorMin,Is.EqualTo(expectedMin),"Re-entering touch mode must reapply the unchanged safe area.");
                Assert.That(content.anchorMax,Is.EqualTo(expectedMax));
                Assert.That(content.offsetMin,Is.EqualTo(Vector2.zero));Assert.That(content.offsetMax,Is.EqualTo(Vector2.zero));
                Assert.That(menu.parent,Is.SameAs(content));Assert.That(menu.anchorMin,Is.EqualTo(new Vector2(.1f,.2f)));
                Assert.That(menu.offsetMin,Is.EqualTo(new Vector2(12,18)));
                Assert.That(overlay.transform.parent,Is.SameAs(root.transform));
                layout.ApplySafeArea(new Rect(insetX,insetY,width-2*insetX,height-2*insetY),new Vector2(width,height));
                Assert.That(content.anchorMin.x,Is.EqualTo(insetX/width).Within(.00001f));
                Assert.That(content.anchorMin.y,Is.EqualTo(insetY/height).Within(.00001f));
                Assert.That(content.anchorMax.x,Is.EqualTo((width-insetX)/width).Within(.00001f));
                Assert.That(content.anchorMax.y,Is.EqualTo((height-insetY)/height).Within(.00001f));
                Assert.That(menu.parent,Is.SameAs(content));Assert.That(menu.anchorMin,Is.EqualTo(new Vector2(.1f,.2f)));
                Assert.That(menu.offsetMin,Is.EqualTo(new Vector2(12,18)));
                Assert.That(content.GetComponent<UnityEngine.UI.Graphic>(),Is.Null,"The safe-area wrapper must not intercept touches.");
                layout.ApplySafeArea(new Rect(0,0,0,0),new Vector2(1080,1920));
                Assert.That(content.anchorMin,Is.EqualTo(Vector2.zero));Assert.That(content.anchorMax,Is.EqualTo(Vector2.one),"A transient empty safe area must not hide the entire UI.");
                layout.ApplySafeArea(new Rect(0,72,1080,1776),new Vector2(1080,1920));
                Assert.That(content.anchorMin.y,Is.EqualTo(.0375f).Within(.00001f));
            }
            finally{Object.DestroyImmediate(root);}
        }
    }

    public partial class BattleSceneTests
    {
        void UseMobileHud()
        {
            typeof(BattleDirector).GetField("touchControls",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(director,true);
        }
        RectTransform MobilePanel(string name)=>director.Hud.GetComponent<MobileUiLayout>().ContentRoot.Find(name) as RectTransform;
        bool VisibleMobileTile(Vector2Int tile)
        {
            var screen=TileScreen(tile);
            return director.Hud.BattlefieldViewport.Contains(new Vector2(screen.x/Screen.width,screen.y/Screen.height))&&OpeningMapPoint(tile,out _);
        }
        void PreviewMobileTile(Vector2Int tile)
        {
            typeof(BattleDirector).GetMethod("PreviewTouchTile",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(director,new object[]{(Vector2)TileScreen(tile)});
        }
        UnityEngine.UI.Button MobileButton(string name)=>director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name==name&&b.gameObject.activeInHierarchy);

        [UnityTest, Category("AndroidFoundation")] public IEnumerator MobileMenusStayInSafeAreaAndCombatHasLargeExplicitActions()
        {
            Assert.That(director.Hud.GetComponent<MobileUiLayout>(),Is.Null,"Desktop keeps its original hierarchy.");
            UseMobileHud();yield return null;
            director.Hud.ShowTitle();yield return null;yield return new WaitForEndOfFrame();
            var layout=director.Hud.GetComponent<MobileUiLayout>();Assert.That(layout,Is.Not.Null);
            Assert.That(layout.ContentRoot.Find("TitleScreen"),Is.Not.Null);
            Assert.That(MobileButton("타이틀 새 게임").IsInteractable(),Is.True);
            director.Hud.ShowIntermissionHub();yield return null;
            Assert.That(director.Hud.IntermissionHubOpen,Is.True);
            Assert.That(layout.ContentRoot.Find("Header"),Is.Not.Null);
            BeginOpeningControlsFixture();yield return null;yield return new WaitForEndOfFrame();
            var command=MobilePanel("Commands");
            foreach(var button in command.GetComponentsInChildren<UnityEngine.UI.Button>())
                Assert.That(((RectTransform)button.transform).rect.height,Is.GreaterThanOrEqualTo(56),button.name);
            foreach(var name in new[]{"왼쪽 회전","전체 보기","오른쪽 회전","현재 유닛"})
                Assert.That(((RectTransform)MobileButton(name).transform).rect.height,Is.GreaterThanOrEqualTo(56),name);
            AssertSplitHudPanelOnScreen(command);
            director.MoveCommand();yield return null;yield return new WaitForEndOfFrame();
            Assert.That(MobileButton("이동 확정").interactable,Is.False);
            var actor=director.Session.Active;var origin=actor.Position;
            var destination=director.Session.Grid.Reachable(actor,out _).Keys.First(p=>p!=origin&&VisibleMobileTile(p));
            PreviewMobileTile(destination);yield return null;
            Assert.That(actor.Position,Is.EqualTo(origin),"Preview must not execute movement.");
            Assert.That(MobileButton("이동 확정").interactable,Is.True);
            MobileButton("이동 취소").onClick.Invoke();yield return null;
            Assert.That(director.State,Is.TypeOf<CommandState>());Assert.That(actor.Position,Is.EqualTo(origin));Assert.That(actor.Moved,Is.False);
            director.SkillCommand();yield return null;yield return new WaitForEndOfFrame();
            Assert.That(MobileButton("기술 선택 취소").IsInteractable(),Is.True);
            AssertSplitHudPanelOnScreen(MobilePanel("Commands"));
            MobileButton("기술 선택 취소").onClick.Invoke();yield return null;
            Assert.That(director.State,Is.TypeOf<CommandState>());
        }

        [UnityTest, Category("AndroidFoundation")] public IEnumerator MobileForecastRequiresVisibleExecutionButtonAndCancelsWithoutSpendingAction()
        {
            SkillData skill=null;
            try
            {
                UseMobileHud();yield return null;BeginOpeningControlsFixture();yield return null;
                var actor=director.Session.Active;skill=Object.Instantiate(actor.Data.BasicAttack);skill.Range=99;
                director.SelectSkill(skill);yield return null;yield return new WaitForEndOfFrame();
                Assert.That(MobileButton("공격 실행").interactable,Is.False);
                var target=director.AvailableTargets().First(VisibleMobileTile);
                string units=string.Join("|",director.Session.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u))));
                int history=director.Session.History.Count;
                PreviewMobileTile(target);yield return null;yield return new WaitForEndOfFrame();
                Assert.That(director.State,Is.TypeOf<TargetSelectionState>());
                Assert.That(director.Forecast,Is.Not.Null);Assert.That(MobileButton("공격 실행").interactable,Is.True);
                Assert.That(string.Join("|",director.Session.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u)))),Is.EqualTo(units));
                AssertSplitHudPanelOnScreen(MobilePanel("Commands"));
                MobileButton("예측 취소").onClick.Invoke();yield return null;
                // The expanded-range clone is an independent skill, not this actor's
                // actual basic-attack reference. Skill cancellation returns to its list.
                Assert.That(director.State,Is.TypeOf<ActionSelectionState>());
                Assert.That(MobileButton("기술 선택 취소").IsInteractable(),Is.True);
                Assert.That(actor.Acted,Is.False);Assert.That(director.CanConfirmTouchSelection,Is.False);
                Assert.That(director.Session.History.Count,Is.EqualTo(history));Assert.That(director.TouchTileSelection,Is.Null);
                MobileButton("기술 선택 취소").onClick.Invoke();yield return null;
                Assert.That(director.State,Is.TypeOf<CommandState>());

                // The real Attack command has a different return destination; keep
                // that existing desktop/mobile rule covered without changing assets.
                director.AttackCommand();yield return null;
                Assert.That(director.SelectedSkill,Is.SameAs(actor.Data.BasicAttack));
                Assert.That(director.State,Is.TypeOf<TargetSelectionState>());
                Assert.That(MobileButton("공격 실행").interactable,Is.False);
                MobileButton("예측 취소").onClick.Invoke();yield return null;
                Assert.That(director.State,Is.TypeOf<CommandState>());
                Assert.That(director.CanConfirmTouchSelection,Is.False);Assert.That(director.TouchTileSelection,Is.Null);
                Assert.That(director.Session.History.Count,Is.EqualTo(history));
                Assert.That(string.Join("|",director.Session.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u)))),Is.EqualTo(units));
            }
            finally{if(skill!=null)Object.Destroy(skill);}
        }
    }
}
