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
        [UnityTest] public IEnumerator UltimatePortraitStaysInCommandSpaceDoesNotBlockInputAndClearsOnRestart()
        {
            director.Deployment.Clear();director.Deployment.AddRange(Enumerable.Range(0,6));director.BeginBattle();yield return null;
            var actor=director.Session.Units.First(u=>u.Team==Team.Player&&u.Data.UltimateSkill!=null);
            director.SetState(new ActionExecutionState(director));
            var health=director.Session.Units.Select(u=>u.CurrentHP).ToArray();var random=JsonUtility.ToJson(Random.state);
            foreach(bool reduced in new[]{false,true})
            {
                var commandSpace=director.Hud.transform.Find("Commands");
                var behind=commandSpace.GetComponentsInChildren<TMP_Text>().Where(t=>t.transform.parent==commandSpace&&t.text.Contains("행동 중")).ToArray();
                Assert.That(behind.Length,Is.EqualTo(2));Assert.That(behind.All(t=>t.enabled),Is.True);
                director.Preferences.ReducedEffects=reduced;director.Board.BeginSkill(actor,actor.Data.UltimateSkill,actor.Position);
                yield return null;Canvas.ForceUpdateCanvases();
                var cue=director.Hud.GetComponentInChildren<UltimateSkillCue>();Assert.That(cue,Is.Not.Null);
                var card=(RectTransform)cue.transform;var parent=(RectTransform)card.parent;
                Assert.That(parent.name,Is.EqualTo("Commands"));
                var corners=new Vector3[4];card.GetWorldCorners(corners);
                foreach(var corner in corners)Assert.That(parent.rect.Contains(parent.InverseTransformPoint(corner)),Is.True,"Cut-in escaped the dock");
                Assert.That(cue.GetComponentsInChildren<UnityEngine.UI.Graphic>().All(g=>!g.raycastTarget),Is.True);
                Assert.That(cue.GetComponent<CanvasGroup>().blocksRaycasts,Is.False);
                Assert.That(behind.All(t=>!t.enabled),Is.True,"Context text must not bleed through the portrait card");
                Assert.That(cue.GetComponentsInChildren<UnityEngine.UI.RawImage>().Any(p=>p.texture!=null),Is.True);
                foreach(var label in cue.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.text);}
                var position=card.anchoredPosition;yield return new WaitForSeconds(.12f);
                if(reduced)Assert.That(card.anchoredPosition,Is.EqualTo(position),"Reduced effects must keep the card still");
                director.Hud.ClearUltimateCue();yield return null;
                Assert.That(behind.All(t=>t!=null&&t.enabled),Is.True,"Surviving contextual labels must return after the cue");
            }
            Assert.That(director.Session.Units.Select(u=>u.CurrentHP),Is.EqualTo(health));Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random));
            director.Board.BeginSkill(actor,actor.Data.UltimateSkill,actor.Position);director.Restart();yield return null;yield return null;
            Assert.That(director.Hud.GetComponentInChildren<UltimateSkillCue>(true),Is.Null);
        }
    }
}
