using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator GrowthFeedbackUsesSnapshotsAndClearsOnRetry()
        {
            director.BeginBattle();yield return null;var u=director.Session.Active;u.Level=8;u.EXP=68;
            var gain=ActionExperience.Grant(u,47);director.Hud.CombatFeedback.Enqueue(gain);
            ActionExperience.Grant(u,250);Assert.That(gain.AfterLevel,Is.EqualTo(9));Assert.That(gain.AfterEXP,Is.EqualTo(15));
            yield return new WaitForSeconds(1.4f);
            Assert.That(GameObject.Find("Experience level").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo("Lv. 9"));
            Assert.That(GameObject.Find("Experience value").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo("15 / 100"));
            Assert.That(GameObject.Find("Experience feedback").GetComponentsInChildren<UnityEngine.UI.Graphic>().All(g=>!g.raycastTarget),Is.True);
            director.Restart();yield return null;Assert.That(director.Hud.CombatFeedback.PendingCount,Is.Zero);
            Assert.That(GameObject.Find("Experience feedback"),Is.Null);
        }
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator AllElementsRenderPooledGeometryWithoutChangingCombatOrRandom()
        {
            director.BeginBattle();yield return null;var target=director.Session.Units.First(u=>u.Team==Team.Enemy);int hp=target.CurrentHP;
            var holder=new GameObject("Particle pool fixture");var pool=holder.AddComponent<ImpactPool>();
            try
            {
                Assert.That(Resources.Load<Texture2D>("TalesTactics/CombatFire"),Is.Not.Null);Assert.That(Resources.Load<Shader>("TalesTactics/Impact").isSupported,Is.True);
                var before=JsonUtility.ToJson(Random.state);
                foreach(Element element in System.Enum.GetValues(typeof(Element)))
                {
                    pool.Play(director.Board.BattleCamera,Vector3.zero,Vector3.one,element,CombatVisualStyle.Sacred,CombatFeedback.Strike,1,false,false);
                    yield return null;
                    Assert.That(pool.GetComponentsInChildren<MeshFilter>().Any(m=>m.sharedMesh.vertexCount>0),Is.True,element.ToString());
                }
                Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(before));Assert.That(target.CurrentHP,Is.EqualTo(hp));
                for(int i=0;i<40;i++)pool.Play(director.Board.BattleCamera,Vector3.zero,Vector3.one,Element.Fire,CombatVisualStyle.FlameSword,CombatFeedback.Strike,1,true,true);
                Assert.That(pool.ActiveCount,Is.EqualTo(ImpactPool.Capacity));
                yield return new WaitForSeconds(1.2f);Assert.That(pool.ActiveCount,Is.Zero);
            }
            finally{Object.Destroy(holder);}
        }
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator GroundFacingSurvivesHeightAndTextScale()
        {
            director.BeginBattle();yield return null;var unit=director.Session.Active;
            foreach(int height in new[]{0,2,4})foreach(float scale in new[]{1f,1.3f})
            {
                director.Session.Grid[unit.Position].Height=height;director.Preferences.TextScale=scale;director.RefreshViews();director.SetState(new FacingSelectionState(director));yield return new WaitForEndOfFrame();
                var head=director.Board.BattleCamera.WorldToScreenPoint(director.Board.HeadWorld(unit));
                var ground=director.Board.BattleCamera.WorldToScreenPoint(director.Session.Grid[unit.Position].WorldPosition(director.Catalog.Rules.TileHeight));
                var canvasScale=director.Hud.GetComponent<Canvas>().scaleFactor;
                var arrows=director.Hud.GetComponentsInChildren<FacingArrowGraphic>();
                var center=new Vector2(arrows.Average(a=>a.transform.position.x),arrows.Average(a=>a.transform.position.y));
                Assert.That(Vector2.Distance(center,new Vector2(ground.x,ground.y)),Is.LessThan(2),"Direction controls remain anchored to the tile surface after height changes.");
                foreach(var arrow in director.Hud.GetComponentsInChildren<FacingArrowGraphic>())
                {Assert.That(arrow.transform.position.y,Is.LessThan(head.y));Assert.That(arrow.rectTransform.rect.width,Is.EqualTo(30*scale).Within(.01f));
                    Assert.That(Vector2.Distance(arrow.transform.position,ground),Is.LessThan(65*scale*canvasScale));
                    Assert.That(((RectTransform)arrow.transform.parent).rect.width,Is.GreaterThanOrEqualTo(44*scale));}
                director.State.Cancel();
            }
        }
    }
}
