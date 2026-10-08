using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void OpeningRestoresPreTurnUnitsAndOriginalScheduler(int mode)
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0,1},level:7,campaignStage:1,useCT:mode==2,teamTurns:mode==0,tacticalCombat:true,bossEncounters:true);
            b.Units[0].Growth=GrowthPath.Assault;b.Units[0].CurrentHP=b.Units[0].Stats.HP;
            var opening=BattleOpening.Capture(b,c);var before=JsonUtility.ToJson(opening);b.Opening=opening;b.Advance();b.Active.CurrentMP=0;b.Units.Last().Damage(99999,b.Grid);
            var restored=opening.Restore(c);
            Assert.That(restored.Scheduler.GetType(),Is.EqualTo(b.Scheduler.GetType()));Assert.That(restored.Active,Is.Null);
            Assert.That(JsonUtility.ToJson(BattleOpening.Capture(restored,c)),Is.EqualTo(before));
            restored.Advance();Assert.That(restored.Active.TurnsStarted,Is.EqualTo(1));Assert.That(restored.Units.All(u=>u.Alive),Is.True);
            Assert.That(JsonUtility.ToJson(opening),Is.EqualTo(before));
        }
        [Test] public void CheckpointRoundTripKeepsOpeningAndLegacyIgnoresIt()
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0,1},campaignStage:1,teamTurns:true,bossEncounters:true);b.Opening=BattleOpening.Capture(b,c);b.Advance();b.Active.CurrentMP=1;
            var cp=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1})));var restored=cp.Restore(c);
            Assert.That(restored.Active.CurrentMP,Is.EqualTo(1));Assert.That(restored.Opening.Restore(c).Units[0].CurrentMP,Is.GreaterThan(1));
            cp.Version=5;Assert.That(cp.Restore(c).Opening,Is.Null);
            cp.Version=6;cp.Opening.Stage=5;Assert.Throws<InvalidDataException>(()=>cp.Restore(c));
        }
        [Test] public void OpeningRejectsPostTurnCaptureAndCorruptUnits()
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0},teamTurns:true);var opening=BattleOpening.Capture(b,c);b.Advance();
            Assert.Throws<System.InvalidOperationException>(()=>BattleOpening.Capture(b,c));
            opening.Units[0].Acted=true;Assert.Throws<InvalidDataException>(()=>opening.Restore(c));
        }
    }
}
