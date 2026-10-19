using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator ActionFeedbackExpiresWithoutRebuildingControlsOrChangingBattle()
        {
            Click("전투 시작");yield return null;
            var unit=director.Session.Active;var hp=unit.CurrentHP;var mp=unit.CurrentMP;var position=unit.Position;
            var button=Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name=="Move / 이동");
            EventSystem.current.SetSelectedGameObject(button.gameObject);
            director.ShowActionFeedback("검증용 공격 결과");Assert.That(director.ActionFeedbackVisible,Is.True);
            yield return new WaitForSecondsRealtime(BattleDirector.ActionFeedbackDuration+.2f);
            Assert.That(director.Message,Is.Empty);Assert.That(director.ActionFeedbackVisible,Is.False);
            Assert.That(button!=null,Is.True);Assert.That(EventSystem.current.currentSelectedGameObject,Is.SameAs(button.gameObject));
            Assert.That(director.Session.Active,Is.SameAs(unit));Assert.That(unit.CurrentHP,Is.EqualTo(hp));
            Assert.That(unit.CurrentMP,Is.EqualTo(mp));Assert.That(unit.Position,Is.EqualTo(position));
            Assert.That(unit.Acted,Is.False);
        }
        [UnityTest] public IEnumerator ActionFeedbackPausesWhileReadingAndCannotExpireNewError()
        {
            Click("전투 시작");yield return null;director.ShowActionFeedback("회복 결과");
            director.Hud.ShowSystemMenu();yield return new WaitForSecondsRealtime(BattleDirector.ActionFeedbackDuration+.2f);
            Assert.That(director.ActionFeedbackVisible,Is.True);Assert.That(director.Message,Is.EqualTo("회복 결과"));
            director.Hud.CloseSystemMenu();director.Message="MP가 부족합니다.";
            yield return new WaitForSecondsRealtime(BattleDirector.ActionFeedbackDuration+.2f);
            Assert.That(director.Message,Is.EqualTo("MP가 부족합니다."));Assert.That(director.ActionFeedbackVisible,Is.False);
        }
        [UnityTest] public IEnumerator ActionFeedbackClearsOnNextCommandAndRestart()
        {
            Click("전투 시작");yield return null;director.ShowActionFeedback("이전 행동 결과");
            director.MoveCommand();Assert.That(director.State,Is.InstanceOf<MoveSelectionState>());
            Assert.That(director.Message,Is.Empty);director.State.Cancel();
            director.ShowActionFeedback("마지막 행동 결과");director.Restart();yield return null;
            Assert.That(director.Message,Is.Empty);Assert.That(director.ActionFeedbackVisible,Is.False);
        }
        [UnityTest] public IEnumerator NewActionFeedbackReceivesItsOwnLifetime()
        {
            Click("전투 시작");yield return null;director.ShowActionFeedback("첫 번째 결과");
            yield return new WaitForSecondsRealtime(BattleDirector.ActionFeedbackDuration-.8f);
            director.ShowActionFeedback("두 번째 결과");yield return new WaitForSecondsRealtime(1);
            Assert.That(director.Message,Is.EqualTo("두 번째 결과"));Assert.That(director.ActionFeedbackVisible,Is.True);
            yield return new WaitForSecondsRealtime(BattleDirector.ActionFeedbackDuration);
            Assert.That(director.Message,Is.Empty);
        }
    }
}
