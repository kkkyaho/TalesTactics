using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator UltimatePortraitUsesFixedOverlayDuringActionAndNeverBlocksInput()
        {
            director.Deployment.Clear();director.Deployment.AddRange(Enumerable.Range(0,6));director.BeginBattle();yield return null;
            var actor=director.Session.Units.First(u=>u.Team==Team.Player&&u.Data.UltimateSkill!=null);
            director.SetState(new ActionExecutionState(director));yield return new WaitForEndOfFrame();
            string state=SkillRosterVfxFixture.State(director.Session.Units);int draws=director.Session.Resolver.RandomDraws;var random=JsonUtility.ToJson(Random.state);
            foreach(bool reduced in new[]{false,true})
            {
                director.SetState(new ActionExecutionState(director));
                yield return null;yield return new WaitForEndOfFrame();
                var commandSpace=director.Hud.transform.Find("Commands");
                Assert.That(commandSpace.gameObject.activeInHierarchy,Is.False,"Ordinary actions keep the local command menu hidden.");
                director.Preferences.ReducedEffects=reduced;director.Board.BeginSkill(actor,actor.Data.UltimateSkill,actor.Position);
                yield return null;yield return new WaitForEndOfFrame();Canvas.ForceUpdateCanvases();
                var cue=director.Hud.GetComponentInChildren<UltimateSkillCue>();Assert.That(cue,Is.Not.Null,"A hidden Commands parent must not suppress the ultimate portrait.");
                Assert.That(cue.Visible,Is.True);Assert.That(director.Hud.UltimateCueVisible,Is.True);
                var card=(RectTransform)cue.transform;Assert.That(card.parent,Is.EqualTo(director.Hud.transform));
                var safe=CombatLabelPlacement.Viewport(director.Hud);var bounds=cue.ScreenBounds;
                Assert.That(bounds.xMin,Is.GreaterThanOrEqualTo(safe.xMin));Assert.That(bounds.xMax,Is.LessThanOrEqualTo(safe.xMax));
                Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(safe.yMin));Assert.That(bounds.yMax,Is.LessThanOrEqualTo(safe.yMax));
                Assert.That(bounds.Overlaps(SplitHudScreenRect((RectTransform)director.Hud.transform.Find("Unit"))),Is.False,"The ultimate card must preserve the fixed status area.");
                Assert.That(cue.GetComponentsInChildren<UnityEngine.UI.Graphic>().All(g=>!g.raycastTarget),Is.True);
                Assert.That(cue.GetComponent<CanvasGroup>().blocksRaycasts,Is.False);
                Assert.That(cue.GetComponentsInChildren<UnityEngine.UI.RawImage>().Any(p=>p.texture!=null),Is.True);
                foreach(var label in cue.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.text);}
                var position=card.anchoredPosition;yield return new WaitForSeconds(.12f);
                if(reduced)Assert.That(card.anchoredPosition,Is.EqualTo(position),"Reduced effects must keep the card still");
                // Help is intentionally unavailable while an action executes. Once that
                // state ends, establish a real modal before checking rendered suppression.
                director.SetState(new CommandState(director));
                Assert.That(director.Hud.CanOpenHelp,Is.True);
                director.Hud.ShowHelp();Assert.That(director.Hud.HelpOpen,Is.True);Assert.That(director.Hud.InputModalOpen,Is.True);
                yield return new WaitForEndOfFrame();Assert.That(director.Hud.UltimateCueVisible,Is.False,"A modal must suppress the passive portrait.");
                director.Hud.CloseHelp();director.Hud.ClearUltimateCue();yield return null;
                Assert.That(director.Hud.GetComponentInChildren<UltimateSkillCue>(true),Is.Null);
            }
            Assert.That(SkillRosterVfxFixture.State(director.Session.Units),Is.EqualTo(state));Assert.That(director.Session.Resolver.RandomDraws,Is.EqualTo(draws));
            Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random));
            director.Board.BeginSkill(actor,actor.Data.UltimateSkill,actor.Position);director.Restart();yield return null;yield return null;
            Assert.That(director.Hud.GetComponentInChildren<UltimateSkillCue>(true),Is.Null);
        }
    }
}
