using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator TrainingVictoryRetainsNewBattleRetry()
        {
            Click("전투 시작");yield return null;var old=director.Session;
            foreach(var u in old.Units.Where(u=>u.Team==Team.Enemy))u.Damage(99999,old.Grid);
            director.SetState(new BattleEndState(director));yield return null;Click("다시 도전");yield return null;
            Assert.That(director.Session,Is.Not.SameAs(old));Assert.That(director.Session.Result,Is.EqualTo(BattleResult.Ongoing));
        }
        [UnityTest] public IEnumerator RetryConfirmationCancelsThenRestoresOpeningWithoutChangingCampaign()
        {
            Click("전투 시작");yield return null;var old=director.Session;var actor=old.Active;int original=actor.CurrentMP;actor.CurrentMP=1;
            string campaign=JsonUtility.ToJson(director.Campaign);director.Hud.ShowSystemMenu();Click("시작 상태로 재도전");yield return null;
            Assert.That(director.Hud.SystemMenuOpen,Is.True);Click("현재 전투 계속");yield return null;Assert.That(director.Session,Is.SameAs(old));Assert.That(actor.CurrentMP,Is.EqualTo(1));
            Click("시작 상태로 재도전");Click("재도전 확정");yield return null;
            Assert.That(director.Session,Is.Not.SameAs(old));Assert.That(director.Session.Active.CurrentMP,Is.EqualTo(original));
            Assert.That(director.Session.Active.TurnsStarted,Is.EqualTo(1));Assert.That(director.Hud.SystemMenuOpen,Is.False);Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(campaign));
        }
        [UnityTest] public IEnumerator SuspendedOpeningRetriesAfterReloadAndKeepsOriginalLevel()
        {
            director.TrainingMode=false;director.BeginBattle();yield return null;var b=director.Session;int level=b.Units[0].Level;
            b.Active.CurrentMP=1;Assert.That(director.SuspendBattle(),Is.True);yield return null;
            director.ConfigureStorage(director.Profiles.Root);Assert.That(director.ResumeBattle(),Is.True);yield return null;
            director.Campaign.Get(b.Units[0].Data.Id).Level=20;string campaign=JsonUtility.ToJson(director.Campaign);
            Assert.That(director.RetryOpening(),Is.True);yield return null;
            Assert.That(director.Session.Units[0].Level,Is.EqualTo(level));Assert.That(director.Session.Active.CurrentMP,Is.GreaterThan(1));
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(campaign));
        }
        [UnityTest] public IEnumerator DefeatRetryAndCorruptOpeningDoNotLoseCurrentBattle()
        {
            Click("전투 시작");yield return null;var old=director.Session;old.Opening.Units[0].Acted=true;
            Assert.That(director.RetryOpening(),Is.False);Assert.That(director.Session,Is.SameAs(old));old.Opening.Units[0].Acted=false;
            foreach(var u in old.Units.Where(u=>u.Team==Team.Player))u.Damage(99999,old.Grid);
            director.SetState(new BattleEndState(director));yield return null;Click("다시 도전");yield return null;
            Assert.That(director.Session,Is.SameAs(old));Click("재도전 확정");yield return null;
            Assert.That(director.Session.Result,Is.EqualTo(BattleResult.Ongoing));Assert.That(director.Session.Units.Where(u=>u.Team==Team.Player).All(u=>u.Alive),Is.True);
        }
    }
}
