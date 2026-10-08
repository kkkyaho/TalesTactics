using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        BattleCatalog FreeTurnCatalog()
        {
            var c=TacticsCatalog();c.Characters=new CharacterData[3];
            for(int i=0;i<3;i++){var d=New<CharacterData>();d.Id="ally"+i;d.DisplayName=d.Id;d.BaseStats=data.BaseStats;d.BasicAttack=data.BasicAttack;d.Skills=new SkillData[0];c.Characters[i]=d;}
            return c;
        }
        [Test] public void FreeTurnSelectionPreservesMoveActionCooldownStatusAndUndo()
        {
            var b=new BattleSession(FreeTurnCatalog(),new[]{0,1,2},teamTurns:true);b.Advance();var a=b.Active;var other=b.Units[2];
            Assert.That(b.Move(new Vector2Int(0,1)),Is.True);a.Cooldowns["other-skill"]=3;a.AddStatus(StatusKind.Song,3);a.FlamingChain=true;
            var before=JsonUtility.ToJson(new CheckpointUnit(a));
            for(int i=0;i<3;i++){Assert.That(b.Select(other),Is.True);Assert.That(b.Select(a),Is.True);}
            Assert.That(JsonUtility.ToJson(new CheckpointUnit(a)),Is.EqualTo(before));Assert.That(a.TurnsStarted,Is.EqualTo(1));
            Assert.That(b.UndoMove(),Is.True);Assert.That(a.Position,Is.EqualTo(new Vector2Int(1,1)));
            var enemy=b.Units.First(u=>u.Team==Team.Enemy);b.Grid.Place(enemy,new Vector2Int(0,1));
            Assert.That(b.Resolver.Execute(a,a.Data.BasicAttack,enemy.Position,out _),Is.True);b.Select(other);b.Select(a);
            Assert.That(a.Acted,Is.True);Assert.That(b.Resolver.Execute(a,a.Data.BasicAttack,enemy.Position,out _),Is.False);
        }
        [Test] public void FreeTurnAlliesFinishInAnyOrderBeforeOneEnemyPhase()
        {
            var b=new BattleSession(FreeTurnCatalog(),new[]{0,1,2},teamTurns:true);b.Advance();var scheduler=(TeamTurnScheduler)b.Scheduler;
            foreach(int i in new[]{2,0,1})
            {
                Assert.That(b.Select(b.Units[i]),Is.True);b.EndTurn();b.EndTurn();Assert.That(b.Select(b.Units[i]),Is.False);b.Advance();
                Assert.That(b.Select(b.Units[i]),Is.False);
            }
            var enemies=b.Units.Where(u=>u.Team==Team.Enemy).ToArray();
            foreach(var enemy in enemies){Assert.That(b.Active,Is.SameAs(enemy));Assert.That(b.Select(b.Units[0]),Is.False);b.EndTurn();b.Advance();}
            Assert.That(scheduler.Round,Is.EqualTo(2));Assert.That(scheduler.Phase,Is.EqualTo(Team.Player));
            foreach(var ally in b.Units.Take(3)){Assert.That(b.Select(ally),Is.True);Assert.That(ally.TurnsStarted,Is.EqualTo(2));Assert.That(ally.Moved||ally.Acted,Is.False);}
        }
        [Test] public void FreeTurnDeadAndRevivedFinishedUnitsCannotGainExtraActions()
        {
            var b=new BattleSession(FreeTurnCatalog(),new[]{0,1,2},teamTurns:true);b.Advance();var a=b.Active;b.EndTurn();b.Advance();
            a.Damage(99999,b.Grid);a.CurrentHP=1;b.Grid.Place(a,a.Position);Assert.That(b.Select(a),Is.False);
            var dead=b.Units[2];dead.Damage(99999,b.Grid);Assert.That(b.Select(dead),Is.False);b.EndTurn();b.Advance();
            Assert.That(b.Active.Team,Is.EqualTo(Team.Enemy));
        }
        [Test] public void FreeTurnCheckpointPreservesSwitchedAndCompletedUnits()
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0,1,2},campaignStage:0,teamTurns:true);b.Advance();
            b.Move(b.Grid.Reachable(b.Active,out _).Keys.First(p=>p!=b.Active.Position));var moving=b.Active;moving.Cooldowns["test"]=3;
            b.Select(b.Units[2]);b.EndTurn();b.Advance();b.Select(b.Units[1]);
            var cp=BattleCheckpoint.Capture(b,new[]{0,1,2});var restored=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(cp)).Restore(c);
            Assert.That(restored.Active.Data.Id,Is.EqualTo(b.Active.Data.Id));Assert.That(restored.Select(restored.Units[2]),Is.False);
            Assert.That(restored.Select(restored.Units[0]),Is.True);Assert.That(restored.Active.Moved,Is.True);Assert.That(restored.Active.Cooldowns["test"],Is.EqualTo(3));Assert.That(restored.Active.TurnsStarted,Is.EqualTo(1));
            Assert.That(restored.UndoMove(),Is.True);
            cp.Begun=new int[0];Assert.Throws<InvalidDataException>(()=>cp.Restore(c));
        }
        [Test] public void FreeTurnRevivedUnitAbsentAtPhaseStartCanSuspendAndActsNextRound()
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0,1,2},campaignStage:0,teamTurns:true);var revived=b.Units[2];revived.Damage(99999,b.Grid);b.Advance();
            revived.CurrentHP=10;b.Grid.Place(revived,revived.Position);Assert.That(b.Select(revived),Is.False);
            b=BattleCheckpoint.Capture(b,new[]{0,1,2}).Restore(c);Assert.That(b.Select(b.Units[2]),Is.False);
            for(int i=0;i<6;i++){b.EndTurn();b.Advance();}
            Assert.That(((TeamTurnScheduler)b.Scheduler).Round,Is.EqualTo(2));Assert.That(b.Select(b.Units[2]),Is.True);
        }
        [TestCase(false)] [TestCase(true)] public void FreeTurnChangeStillRestoresLegacySpeedAndCTCheckpoints(bool ct)
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0},campaignStage:0,useCT:ct);b.Advance();var cp=BattleCheckpoint.Capture(b,new[]{0});cp.Version=2;
            var restored=cp.Restore(c);Assert.That(restored.Scheduler.GetType(),Is.EqualTo(b.Scheduler.GetType()));
            for(int i=0;i<12;i++){b.EndTurn();restored.EndTurn();b.Advance();restored.Advance();Assert.That(restored.Units.IndexOf(restored.Active),Is.EqualTo(b.Units.IndexOf(b.Active)));}
        }
        [Test] public void FreeTurnSelectionDoesNotAdvanceSurvivalObjective()
        {
            var b=new BattleSession(FreeTurnCatalog(),new[]{0,1,2},objective:ObjectiveKind.Survive,teamTurns:true);b.Advance();
            for(int i=0;i<10;i++)b.Select(b.Units[i%3]);Assert.That(((SurviveTurns)b.Victory).Completed,Is.Zero);
            b.EndTurn();b.EndTurn();Assert.That(((SurviveTurns)b.Victory).Completed,Is.EqualTo(1));
        }
    }
}
