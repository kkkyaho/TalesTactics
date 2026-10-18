using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator LevelUpFeedbackCuesOncePerQueuedGainAndKeepsSnapshots()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();
            var feedback=director.Hud.CombatFeedback;feedback.Clear();director.Audio.StopAll();director.Audio.SetVolumes(0,0);
            var units=director.Session.Units.Where(u=>u.Team==Team.Player).ToArray();
            units[0].Level=8;units[0].EXP=99;units[1].Level=4;units[1].EXP=95;
            var first=ActionExperience.Grant(units[0],205);feedback.Enqueue(first);
            feedback.Enqueue(ActionExperience.Grant(units[1],10));
            // Neither a mutable award object nor later growth may rewrite a queued notification.
            first.Amount=9999;first.AfterLevel=50;ActionExperience.Grant(units[0],200);
            var levels=units.Select(u=>u.Level).ToArray();var exp=units.Select(u=>u.EXP).ToArray();
            var random=JsonUtility.ToJson(Random.state);var state=director.State;
            Assert.That(feedback.PendingCount,Is.EqualTo(2));
            // The level reaches 11 before the final four EXP finish filling the new bar.
            yield return WaitForLevelUpFeedback(()=>GameObject.Find("Experience level")?.GetComponent<TMPro.TMP_Text>().text=="Lv. 11"&&
                GameObject.Find("Experience value")?.GetComponent<TMPro.TMP_Text>().text=="4 / 100",4);
            Assert.That(GameObject.Find("Experience value").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo("4 / 100"));
            Assert.That(GameObject.Find("Experience gain").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo("EXP +205"));
            Assert.That(feedback.PresentedLevelUpCount,Is.EqualTo(1),"A three-level gain has one visual/audio cue, not one every frame or level.");
            yield return WaitForLevelUpFeedback(()=>feedback.PendingCount==0,5);
            Assert.That(feedback.PresentedLevelUpCount,Is.EqualTo(2));
            Assert.That(units.Select(u=>u.Level),Is.EqualTo(levels));Assert.That(units.Select(u=>u.EXP),Is.EqualTo(exp));
            Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random));Assert.That(director.State,Is.SameAs(state));
            feedback.Enqueue(ActionExperience.Grant(units[0],100));director.Restart();yield return null;
            Assert.That(feedback.PendingCount,Is.Zero);Assert.That(feedback.PresentedLevelUpCount,Is.Zero);
            Assert.That(GameObject.Find("Experience feedback"),Is.Null);
        }
        [UnityTest] public IEnumerator LevelUpFeedbackWaitsForModalAndRespectsReducedEffectsAndSound()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();
            var feedback=director.Hud.CombatFeedback;feedback.Clear();director.Audio.StopAll();director.Audio.SetVolumes(0,0);
            director.Preferences.ReducedEffects=true;var unit=director.Session.Active;unit.Level=4;unit.EXP=99;
            director.Hud.ShowHelp();feedback.Enqueue(ActionExperience.Grant(unit,1));
            yield return new WaitForSeconds(.25f);
            Assert.That(feedback.PresentedLevelUpCount,Is.Zero);Assert.That(director.Audio.ActiveEffectVoices,Is.Zero);
            director.Hud.CloseHelp();float timeScale=Time.timeScale;
            yield return WaitForLevelUpFeedback(()=>feedback.PresentedLevelUpCount==1,3);
            var light=GameObject.Find("Experience ascent light").GetComponent<UnityEngine.UI.RawImage>();
            Assert.That(light.texture,Is.Not.Null);Assert.That(light.enabled,Is.False);
            Assert.That(director.Audio.ActiveEffectVoices,Is.Zero);Assert.That(director.Hud.InputModalOpen,Is.False);Assert.That(Time.timeScale,Is.EqualTo(timeScale));
            var group=light.GetComponentInParent<CanvasGroup>();Assert.That(group.blocksRaycasts,Is.False);Assert.That(group.interactable,Is.False);
            Assert.That(GameObject.Find("Experience feedback").GetComponentsInChildren<UnityEngine.UI.Graphic>().All(g=>!g.raycastTarget),Is.True);
            director.Hud.ShowHelp();yield return new WaitForSeconds(.2f);director.Hud.CloseHelp();yield return null;
            Assert.That(feedback.PresentedLevelUpCount,Is.EqualTo(1),"Closing a modal must not replay the level-up cue.");
            feedback.Clear();yield return null;director.Preferences.ReducedEffects=false;director.Audio.SetVolumes(0,.45f);unit.EXP=99;
            feedback.Enqueue(ActionExperience.Grant(unit,1));yield return WaitForLevelUpFeedback(()=>feedback.PresentedLevelUpCount==1,3);
            light=GameObject.Find("Experience ascent light").GetComponent<UnityEngine.UI.RawImage>();
            Assert.That(light.enabled,Is.True);Assert.That(director.Audio.ActiveEffectVoices,Is.EqualTo(1));
            float normalGain=director.Audio.LastEffectGain;
            // Existing volume/voice accounting is used, and the softer setting attenuates the chime.
            director.Audio.StopAll();director.Audio.PlayLevelUpCue(true);
            Assert.That(director.Audio.ActiveEffectVoices,Is.EqualTo(1));Assert.That(director.Audio.LastEffectGain,Is.LessThan(normalGain));
            director.Audio.StopAll();director.Audio.SetVolumes(0,0);director.Audio.PlayLevelUpCue();
            Assert.That(director.Audio.ActiveEffectVoices,Is.Zero);
            var card=(RectTransform)light.transform.parent;var corners=new Vector3[4];light.rectTransform.GetWorldCorners(corners);
            foreach(var corner in corners)Assert.That(card.rect.Contains(card.InverseTransformPoint(corner)),Is.True,"The accent remains inside the experience card.");
        }
        [UnityTest] public IEnumerator LevelUpFeedbackSkipsOrdinaryExpAndStopsAtLevelCap()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();
            var feedback=director.Hud.CombatFeedback;feedback.Clear();director.Audio.StopAll();director.Audio.SetVolumes(0,0);
            var unit=director.Session.Active;unit.Level=49;unit.EXP=98;
            feedback.Enqueue(ActionExperience.Grant(unit,1));
            yield return WaitForLevelUpFeedback(()=>feedback.PendingCount==0,3);Assert.That(feedback.PresentedLevelUpCount,Is.Zero);
            feedback.Enqueue(ActionExperience.Grant(unit,999));
            yield return WaitForLevelUpFeedback(()=>feedback.PresentedLevelUpCount==1,3);
            Assert.That(GameObject.Find("Experience level").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo("Lv. 50"));
            Assert.That(GameObject.Find("Experience value").GetComponent<TMPro.TMP_Text>().text,Is.EqualTo("MAX"));
            feedback.Enqueue(ActionExperience.Grant(unit,100)); // At cap Grant returns null.
            yield return WaitForLevelUpFeedback(()=>feedback.PendingCount==0,2);
            Assert.That(feedback.PresentedLevelUpCount,Is.EqualTo(1));Assert.That(unit.Level,Is.EqualTo(50));Assert.That(unit.EXP,Is.Zero);
        }
        static IEnumerator WaitForLevelUpFeedback(System.Func<bool> ready,float seconds)
        {
            float deadline=Time.realtimeSinceStartup+seconds;
            while(!ready()&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(ready(),Is.True,"Timed out waiting for experience presentation.");
        }
    }
}
