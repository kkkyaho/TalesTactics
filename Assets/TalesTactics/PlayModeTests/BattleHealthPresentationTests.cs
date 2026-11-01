using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator HeadHealthIsQuietUntilFocusedAndKeepsTeamHueAtCriticalHealth()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();director.enabled=false;
            var enemy=director.Session.Units.First(u=>u.Team==Team.Enemy&&u.Alive&&!u.CrossoverReserve);
            var display=director.Board.GetComponentsInChildren<UnitHealthDisplay>().Single(d=>d.Unit==enemy);
            var visuals=(Dictionary<UnitRuntime,Transform>)typeof(BoardView).GetField("units",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(director.Board);
            MoveGaugeFixtureIntoClearArea(display,visuals[enemy]);
            enemy.CurrentHP=enemy.Stats.HP;director.HoveredUnit=director.InspectedUnit=null;
            yield return new WaitForEndOfFrame();
            Assert.That(display.Bar.enabled,Is.False,"A healthy idle unit does not add overhead UI clutter.");
            Assert.That(display.GetComponentsInChildren<TMP_Text>(true),Is.Empty,"Names and exact values belong to the fixed status card.");
            director.HoveredUnit=enemy;yield return new WaitForEndOfFrame();
            MoveGaugeFixtureIntoClearArea(display,visuals[enemy]);yield return new WaitForEndOfFrame();
            Assert.That(display.Bar.enabled,Is.True,"Pointing at a unit reveals its nearby gauge.");
            Assert.That(display.GetComponentInChildren<OrnateButtonGraphic>(),Is.Not.Null,"Use the same authored metal frame as the HUD.");
            Assert.That(display.GetComponentsInChildren<UnityEngine.UI.Graphic>().All(g=>!g.raycastTarget),Is.True);
            director.HoveredUnit=null;enemy.CurrentHP=Mathf.Max(1,enemy.Stats.HP/10);yield return new WaitForEndOfFrame();
            Assert.That(display.Bar.enabled,Is.True,"A damaged unit retains a visible health cue.");
            Assert.That(display.Bar.Fill,Is.EqualTo(UnitHealthDisplay.EnemyTint),"Critical health retains enemy hue rather than switching to an ambiguous yellow.");
            Assert.That(display.Bar.Trail,Is.GreaterThan(display.Bar.Fraction));
            var offset=new Vector2(display.ScreenBounds.center.x,display.ScreenBounds.yMin)-display.ScreenOrigin;
            float scale=Mathf.Clamp(Screen.height/1080f,.8f,1.5f);
            Assert.That(Mathf.Abs(offset.x),Is.LessThanOrEqualTo(UnitHealthLayout.MaximumHorizontalShift*scale+.1f));
            Assert.That(Mathf.Abs(offset.y),Is.LessThanOrEqualTo(UnitHealthLayout.MaximumVerticalShift*scale+.1f));
            enemy.CurrentHP=enemy.Stats.HP;yield return new WaitForEndOfFrame();
            Assert.That(display.Bar.enabled,Is.True,"A full heal remains visible briefly so recovery is readable.");
            yield return new WaitForSeconds(UnitHealthDisplay.RecentChangeSeconds+.1f);yield return new WaitForEndOfFrame();
            Assert.That(display.Bar.enabled,Is.False,"The healed idle gauge clears after its feedback interval.");
            director.HoveredUnit=enemy;director.Hud.ShowHelp();yield return new WaitForEndOfFrame();
            Assert.That(display.Bar.enabled,Is.False,"Modal panels hide passive battlefield gauges.");
            Assert.That(display.GetComponentInChildren<OrnateButtonGraphic>(),Is.Null,"Suppression also hides the frame, not just its fill.");
        }
        void MoveGaugeFixtureIntoClearArea(UnitHealthDisplay display,Transform visual)
        {
            var safe=CombatLabelPlacement.Viewport(director.Hud);var occupied=new List<Rect>();
            director.Hud.AppendCombatLabelReservations(occupied);
            occupied.AddRange(director.Board.GetComponentsInChildren<UnitHealthDisplay>().Where(d=>d!=display&&d.Bar.enabled).Select(d=>d.ScreenBounds));
            float scale=Mathf.Clamp(Screen.height/1080f,.8f,1.5f);var size=new Vector2(74,16)*scale;
            for(float y=safe.center.y;y<safe.yMax-size.y;y+=12)
            for(float x=safe.xMin+size.x*.5f;x<safe.xMax-size.x*.5f;x+=12)
            {
                if(!UnitHealthLayout.TryPlaceNearHead(new Vector2(x,y),size,safe,occupied,scale,out var target))continue;
                var camera=director.Board.BattleCamera;var head=director.Board.UnitHeadScreenOrigin(display.Unit);
                var origin=camera.WorldToScreenPoint(visual.position);
                visual.position=camera.ScreenToWorldPoint(new Vector3(origin.x+target.center.x-head.x,origin.y+target.yMin-head.y,origin.z));
                return;
            }
            Assert.Fail("The health-visibility fixture needs an unobstructed head region rather than moving an enemy underneath the live command menu.");
        }
    }
}
