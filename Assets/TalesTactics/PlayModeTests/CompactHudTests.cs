using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator SlimBattleHeaderLeavesMapGapsAndKeepsSixAllySelectionAtLargeText()
        {
            director.Preferences.TextScale=1.3f;
            director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1,2,3,4,5});
            director.BeginBattle();yield return null;yield return new WaitForEndOfFrame();
            var allies=director.Session.Units.Where(u=>u.Team==Team.Player).ToArray();Assert.That(allies.Length,Is.EqualTo(6));
            var resources=allies.Select(u=>$"{u.CurrentHP}/{u.CurrentMP}/{u.Moved}/{u.Acted}").ToArray();
            var header=(RectTransform)director.Hud.transform.Find("Header");
            Assert.That(header.GetComponent<UnityEngine.UI.Image>().enabled,Is.False,"Empty space between the small header cards must reveal the battlefield.");
            var objective=(RectTransform)header.Find("BattleObjectiveBackdrop");var turn=(RectTransform)header.Find("BattleTurnBackdrop");
            var a=new Vector3[4];var b=new Vector3[4];objective.GetWorldCorners(a);turn.GetWorldCorners(b);
            Assert.That(a[2].x,Is.LessThan(b[0].x),"The objective must not crowd the six-portrait turn card.");
            var gap=new Vector2((a[2].x+b[0].x)*.5f,(a[0].y+a[1].y)*.5f);
            var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=gap},hits);
            Assert.That(hits,Is.Empty,"Transparent gaps must not consume battlefield input.");
            var portraits=header.GetComponentsInChildren<UnityEngine.UI.Button>().Where(x=>x.name.StartsWith("아군 선택: ")).ToArray();
            Assert.That(portraits.Length,Is.EqualTo(6));
            var inner=turn.rect;inner.xMin+=8;inner.xMax-=8;inner.yMin+=8;inner.yMax-=8;
            foreach(var portrait in portraits)
            {
                var corners=new Vector3[4];((RectTransform)portrait.transform).GetWorldCorners(corners);
                Assert.That(corners.All(c=>inner.Contains(turn.InverseTransformPoint(c))),Is.True,portrait.name);
            }
            foreach(var unit in allies)
            {
                Click("아군 선택: "+unit.Data.Id);yield return null;yield return new WaitForEndOfFrame();
                Assert.That(director.Session.Active,Is.SameAs(unit));
                foreach(var label in header.GetComponentsInChildren<TMPro.TMP_Text>().Concat(director.Hud.transform.Find("Commands").GetComponentsInChildren<TMPro.TMP_Text>()))
                {label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.text);}
            }
            Assert.That(allies.Select(u=>$"{u.CurrentHP}/{u.CurrentMP}/{u.Moved}/{u.Acted}"),Is.EqualTo(resources));
            var dock=(RectTransform)director.Hud.transform.Find("BattleCommandDock");
            Assert.That(dock.rect.height,Is.LessThanOrEqualTo(450),"Routine commands must leave more battlefield visible than the old tall list.");
            Click("Move / 이동");yield return null;Assert.That(director.State,Is.TypeOf<MoveSelectionState>());
            director.State.Cancel();yield return null;
            Click("Skill / 스킬");yield return null;Assert.That(director.State,Is.TypeOf<ActionSelectionState>());
            Assert.That(((RectTransform)director.Hud.transform.Find("Commands")).rect.width,Is.GreaterThanOrEqualTo(316),"Skill descriptions retain their reading width.");
        }
        [UnityTest] public IEnumerator CompactUnitSummaryAndDetailsPreserveBattleAndInput()
        {
            director.BeginBattle();yield return null;
            var unit=director.Session.Active;int hp=unit.CurrentHP,mp=unit.CurrentMP;var state=director.State;
            var summary=(RectTransform)director.Hud.transform.Find("Unit");
            Assert.That(summary.rect.height,Is.LessThanOrEqualTo(180));
            Assert.That(summary.rect.width,Is.LessThanOrEqualTo(380));
            Assert.That(director.Hud.BattlefieldViewport.width,Is.GreaterThan(.65f));
            Click("능력");yield return null;
            Assert.That(director.Hud.UnitDetailsOpen,Is.True);
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.IsInteractable()).All(b=>b.transform.IsChildOf(director.Hud.transform.Find("UnitDetails"))),Is.True,"Only the status modal may accept input");
            director.GetComponent<GamepadPointer>().Cancel();yield return null;
            Assert.That(director.Hud.UnitDetailsOpen,Is.False);Assert.That(director.State,Is.SameAs(state));
            var commands=(RectTransform)director.Hud.transform.Find("Commands");
            Assert.That(commands.position.x,Is.EqualTo(summary.position.x).Within(.01f),"Status and commands share one left dock.");
            Assert.That(director.Hud.transform.Find("BattleCommandDock").gameObject.activeInHierarchy,Is.True);
            foreach(var button in commands.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                var r=(RectTransform)button.transform;
                Assert.That(commands.rect.Contains(commands.InverseTransformPoint(r.TransformPoint(r.rect.min))),Is.True,button.name);
                Assert.That(commands.rect.Contains(commands.InverseTransformPoint(r.TransformPoint(r.rect.max))),Is.True,button.name);
                var icon=button.GetComponentInChildren<TacticalIcon>();var caption=button.GetComponentInChildren<TMPro.TMP_Text>();
                if(icon!=null)
                {
                    var iconRight=r.InverseTransformPoint(icon.rectTransform.TransformPoint(icon.rectTransform.rect.max)).x;
                    var textLeft=r.InverseTransformPoint(caption.rectTransform.TransformPoint(caption.rectTransform.rect.min)).x;
                    // World-to-canvas transforms can round an authored 8 px gap to 7.99994 px.
                    const float requiredGap=8f,transformTolerance=.001f;
                    Assert.That(textLeft-iconRight+transformTolerance,Is.GreaterThanOrEqualTo(requiredGap),"Command icon and caption require an 8 px gap (0.001 px transform tolerance): "+button.name);
                }
            }
            director.SkillCommand();yield return null;
            Assert.That(commands.rect.width,Is.EqualTo(summary.rect.width).Within(.01f));
            director.State.Cancel();yield return null;
            director.AttackCommand();yield return null;
            Assert.That(commands.rect.height,Is.LessThanOrEqualTo(240));
            director.State.Cancel();yield return null;
            Assert.That(unit.CurrentHP,Is.EqualTo(hp));Assert.That(unit.CurrentMP,Is.EqualTo(mp));
            director.Restart();yield return null;
            Assert.That(director.Hud.transform.Find("BattleCommandDock").gameObject.activeInHierarchy,Is.False);
            Assert.That(((RectTransform)director.Hud.transform.Find("PreparationContent")).rect.height,Is.GreaterThan(400));
            Assert.That(director.Hud.IntermissionHubOpen,Is.True);Click("출전 편성");yield return null;
            Assert.That(director.Hud.transform.Find("Unit").GetComponent<UnityEngine.UI.Image>().enabled,Is.True,"Preparation restores its ornate navigation frame after the shared battle dock.");
            Assert.That(director.Hud.transform.Find("Unit").GetComponent<UnityEngine.UI.Image>().raycastTarget,Is.True);
            Assert.That(director.Hud.transform.Find("Header").GetComponent<UnityEngine.UI.Image>().enabled,Is.True,"The preparation header regains its surface after the battle header's separated cards.");
            Assert.That(director.Hud.transform.Find("Header").GetComponent<UnityEngine.UI.Image>().raycastTarget,Is.True);
            Assert.That(director.Hud.transform.Find("PreparationContent/FormationModels").gameObject.activeInHierarchy,Is.True);
        }
    }
}
