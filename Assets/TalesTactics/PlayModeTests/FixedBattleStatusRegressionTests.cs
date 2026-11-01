using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator MapHoverShowsEnemyPortraitAndLiveLowResourcesInFixedStatus()
        {
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                director.BeginBattle();yield return null;director.StopAllCoroutines();yield return new WaitForEndOfFrame();
                var active=director.Session.Active;var state=director.State;int draws=director.Session.Resolver.RandomDraws;
                var panel=(RectTransform)director.Hud.transform.Find("Unit");var originalPosition=panel.position;var originalSize=panel.rect.size;
                var candidates=director.Session.Units.Where(u=>u.Team==Team.Enemy&&u.Alive&&!u.CrossoverReserve&&u.Stats.HP>=20&&u.Stats.MP>=20&&!CharacterVisualSizing.IsHero(u.Data)).ToArray();
                UnitRuntime enemy=null;Vector2 point=default;
                var hits=new System.Collections.Generic.List<RaycastResult>();
                foreach(var candidate in candidates)
                {
                    var world=director.Session.Grid[candidate.Position].WorldPosition(director.Catalog.Rules.TileHeight);
                    var screen=director.Board.BattleCamera.WorldToScreenPoint(world);var p=new Vector2(screen.x,screen.y);
                    if(screen.z<=0||!CombatLabelPlacement.Viewport(director.Hud).Contains(p))continue;
                    if(!director.Board.Pick(p,out var cell)||cell!=candidate.Position)continue;
                    hits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);
                    if(hits.Count>0)continue;
                    enemy=candidate;point=p;break;
                }
                Assert.That(enemy,Is.Not.Null,"Find a real enemy tile that can be hovered without passing through a HUD control.");
                director.HoveredUnit=director.InspectedUnit=null;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point,delta=Vector2.one});
                yield return null;yield return new WaitForEndOfFrame();
                Assert.That(director.HealthHoveredUnit,Is.SameAs(enemy),"Exercise the production map-pick path, not a header portrait hover.");
                Assert.That(director.HoveredUnit,Is.Null);
                var title=panel.Find("UnitName").GetComponent<TMP_Text>();
                Assert.That(title.text,Does.Contain(enemy.Data.DisplayName));
                var portrait=panel.Find("Portrait");var art=portrait.GetComponents<UnityEngine.UI.Image>();
                Assert.That(art.Length,Is.EqualTo(1),"Enemy portrait must not receive a second Image when its inspection hit target is created.");
                Assert.That(art[0].sprite,Is.Not.Null);Assert.That(art[0].color.a,Is.GreaterThan(.5f));
                var inspect=portrait.GetComponentInChildren<UnityEngine.UI.Button>();Assert.That(inspect,Is.Not.Null);
                Assert.That(inspect.targetGraphic,Is.Not.SameAs(art[0]),"The transparent input target must not erase the enemy artwork.");
                enemy.CurrentHP=1;enemy.CurrentMP=1;enemy.SpecialGauge=1;
                yield return null;yield return new WaitForEndOfFrame();
                Assert.That(panel.Find("UnitName").GetComponent<TMP_Text>(),Is.SameAs(title),"Live resource changes update existing UI without destroying focusable controls.");
                Assert.That(portrait.GetComponentInChildren<UnityEngine.UI.Button>(),Is.SameAs(inspect));
                foreach(string resource in new[]{"HP","MP","SP"})
                {
                    var bar=(RectTransform)panel.Find(resource+"Bar");
                    int maximum=resource=="HP"?enemy.Stats.HP:resource=="MP"?enemy.Stats.MP:100;
                    Assert.That(bar.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Value").text,Is.EqualTo("1 / "+maximum));
                    var fill=bar.GetComponentsInChildren<RectTransform>(true).Single(t=>t.name=="Fill");
                    Assert.That(fill.gameObject.activeInHierarchy,Is.True);
                    Assert.That(fill.rect.width,Is.GreaterThan(0),resource+"=1 must not produce a negative or vanished fill.");
                    var corners=new Vector3[4];fill.GetWorldCorners(corners);
                    Assert.That(corners.All(c=>bar.rect.Contains(bar.InverseTransformPoint(c))),Is.True,resource+" fill stays inside its framed track.");
                    Assert.That(fill.rect.width,Is.LessThan(bar.rect.width*.1f),"A low resource must not look full or reversed.");
                }
                Assert.That(Vector3.Distance(panel.position,originalPosition),Is.LessThan(.01f));Assert.That(panel.rect.size,Is.EqualTo(originalSize));
                Assert.That(director.Session.Active,Is.SameAs(active));Assert.That(director.State,Is.SameAs(state));Assert.That(director.Session.Resolver.RandomDraws,Is.EqualTo(draws));
                LogAssert.NoUnexpectedReceived();
            }
            finally{InputSystem.RemoveDevice(mouse);}
        }
    }
    public class ExplicitFramedLayoutTests
    {
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator NestedFrameCannotReflowAnExplicitlyPositionedDisabledAncestor()
        {
            var root=new GameObject("Explicit forecast",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(FramedContent));
            try
            {
                var rootRect=root.GetComponent<RectTransform>();rootRect.sizeDelta=new Vector2(340,400);
                var rootFit=root.GetComponent<FramedContent>();rootFit.enabled=false;
                var header=new GameObject("Authored title",typeof(RectTransform));header.transform.SetParent(root.transform,false);
                var title=header.GetComponent<RectTransform>();title.anchorMin=new Vector2(0,1);title.anchorMax=Vector2.one;
                title.offsetMin=new Vector2(24,-48);title.offsetMax=new Vector2(-66,-14);
                var nested=new GameObject("Forecast HP",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(FramedContent));nested.transform.SetParent(root.transform,false);
                var nestedRect=nested.GetComponent<RectTransform>();nestedRect.anchorMin=new Vector2(0,1);nestedRect.anchorMax=Vector2.one;
                nestedRect.offsetMin=new Vector2(24,-294);nestedRect.offsetMax=new Vector2(-24,-278);
                var content=new GameObject("Nested fill",typeof(RectTransform));content.transform.SetParent(nested.transform,false);
                var fill=content.GetComponent<RectTransform>();fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;
                var originalTitleMin=title.offsetMin;var originalTitleMax=title.offsetMax;
                var originalNestedMin=nestedRect.offsetMin;var originalNestedMax=nestedRect.offsetMax;
                yield return null;yield return new WaitForEndOfFrame();
                Assert.That(title.offsetMin,Is.EqualTo(originalTitleMin));Assert.That(title.offsetMax,Is.EqualTo(originalTitleMax));
                Assert.That(nestedRect.offsetMin,Is.EqualTo(originalNestedMin));Assert.That(nestedRect.offsetMax,Is.EqualTo(originalNestedMax));
                Assert.That(fill.offsetMin.x,Is.GreaterThan(0),"The enabled child frame still applies its own internal padding.");
                Assert.That(fill.offsetMax.x,Is.LessThan(0));
                rootFit.enabled=true;yield return null;yield return new WaitForEndOfFrame();
                Assert.That(title.offsetMin,Is.Not.EqualTo(originalTitleMin),"Re-enabling automatic layout explicitly restores its padding behavior.");
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
