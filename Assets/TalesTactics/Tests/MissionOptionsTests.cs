using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        static void NextMissionRound(BattleSession b)
        {
            int round=((TeamTurnScheduler)b.Scheduler).Round;
            for(int i=0;i<100&&((TeamTurnScheduler)b.Scheduler).Round==round;i++){b.EndTurn();b.Advance();}
            Assert.That(((TeamTurnScheduler)b.Scheduler).Round,Is.EqualTo(round+1));
        }
        [TestCase(BattleDifficulty.Standard,5)] [TestCase(BattleDifficulty.Relaxed,3)] [TestCase(BattleDifficulty.Veteran,7)]
        public void DifficultyChangesOnlyEnemyStartingLevelAndRetryPreservesIt(BattleDifficulty difficulty,int expected)
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0},level:4,enemyLevel:5,campaignStage:2,teamTurns:true,difficulty:difficulty);
            b.Opening=BattleOpening.Capture(b,c);Assert.That(b.Units[0].Level,Is.EqualTo(4));Assert.That(b.Units.Skip(1).All(u=>u.Level==expected),Is.True);
            var retry=b.Opening.Restore(c);Assert.That(retry.Difficulty,Is.EqualTo(difficulty));Assert.That(retry.Units.Skip(1).All(u=>u.Level==expected),Is.True);
        }
        [Test] public void OptionalCaptureRequiresTwoPhaseEndsAndResetsWhenVacant()
        {
            var b=new BattleSession(FreeTurnCatalog(),new[]{0,1},campaignStage:2,teamTurns:true,missionEvents:true);b.Advance();var u=b.Active;var origin=u.Position;
            b.Grid.Place(u,b.Destination);Assert.That(b.Result,Is.EqualTo(BattleResult.Ongoing));b.EndTurn();Assert.That(b.CaptureProgress,Is.Zero);b.Advance();
            NextMissionRound(b);Assert.That(b.CaptureProgress,Is.EqualTo(1));b.Grid.Place(u,origin);NextMissionRound(b);Assert.That(b.CaptureProgress,Is.Zero);
            b.Grid.Place(u,b.Destination);NextMissionRound(b);NextMissionRound(b);Assert.That(b.Result,Is.EqualTo(BattleResult.Victory));
        }
        [TestCase(2)] [TestCase(4)]
        public void ReinforcementsSpawnOnceWithoutOccupancyLossAndCheckpointRoundTrips(int stage)
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0,1},enemyLevel:5,campaignStage:stage,teamTurns:true,missionEvents:true,difficulty:BattleDifficulty.Veteran);b.Opening=BattleOpening.Capture(b,c);b.Advance();
            var blocked=CampaignContent.EnemySpawn(stage,3);var occupant=b.Grid[blocked]?.Occupant;
            NextMissionRound(b);Assert.That(b.Units.Count,Is.EqualTo(6));NextMissionRound(b);Assert.That(b.Units.Count,Is.EqualTo(8));Assert.That(b.ReinforcementsArrived,Is.True);
            Assert.That(b.Grid[blocked]?.Occupant,Is.SameAs(occupant));Assert.That(b.Units.Where(u=>u.Alive).Select(u=>u.Position).Distinct().Count(),Is.EqualTo(8));
            Assert.That(b.Units.Skip(6).All(u=>u.Team==Team.Enemy&&u.Level==7&&u.TacticalEnemy&&u.TurnsStarted==0),Is.True);
            var checkpoint=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1})));var restored=checkpoint.Restore(c);
            Assert.That(JsonUtility.ToJson(BattleCheckpoint.Capture(restored,new[]{0,1})),Is.EqualTo(JsonUtility.ToJson(checkpoint)));
            NextMissionRound(restored);Assert.That(restored.Units.Count,Is.EqualTo(8));Assert.That(restored.Units.Skip(6).All(u=>u.TurnsStarted==1),Is.True);
            var retry=restored.Opening.Restore(c);Assert.That(retry.Units.Count,Is.EqualTo(6));Assert.That(retry.ReinforcementsArrived,Is.False);Assert.That(retry.MissionEvents,Is.True);
        }
        [Test] public void CheckpointPreservesCaptureProgressAndRejectsImpossibleEventState()
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0},campaignStage:2,teamTurns:true,missionEvents:true);b.Advance();b.Grid.Place(b.Active,b.Destination);NextMissionRound(b);
            var cp=BattleCheckpoint.Capture(b,new[]{0});Assert.That(cp.Restore(c).CaptureProgress,Is.EqualTo(1));cp.CaptureProgress=2;Assert.Throws<InvalidDataException>(()=>cp.Restore(c));
        }
        [Test] public void StandardLegacyAndNonTeamBattlesRetainOriginalArrivalObjective()
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0},campaignStage:2,teamTurns:true);b.Advance();var cp=BattleCheckpoint.Capture(b,new[]{0});cp.Version=6;cp.MissionEvents=true;cp.Difficulty=BattleDifficulty.Veteran;
            var restored=cp.Restore(c);Assert.That(restored.MissionEvents,Is.False);Assert.That(restored.Difficulty,Is.EqualTo(BattleDifficulty.Standard));restored.Grid.Place(restored.Active,restored.Destination);Assert.That(restored.Result,Is.EqualTo(BattleResult.Victory));
            Assert.That(new BattleSession(c,new[]{0},campaignStage:2,useCT:true,missionEvents:true).MissionEvents,Is.False);
            Assert.That(new BattleSession(c,new[]{0},teamTurns:true,missionEvents:true,difficulty:BattleDifficulty.Veteran).Difficulty,Is.EqualTo(BattleDifficulty.Standard));
        }
        [Test] public void MissionPreferencesPersistAndRejectUnknownDifficulty()
        {
            var prefs=JsonUtility.FromJson<PlayerPreferences>("{\"Version\":1}");Assert.That(prefs.Difficulty,Is.EqualTo(BattleDifficulty.Standard));Assert.That(prefs.MissionEvents,Is.False);
            prefs=new PlayerPreferences{Difficulty=BattleDifficulty.Veteran,MissionEvents=true};var path=SaveTestPath();var file=new PreferenceFile(path);Assert.That(file.Save(prefs),Is.True);Assert.That(file.Load().Difficulty,Is.EqualTo(prefs.Difficulty));Assert.That(file.Load().MissionEvents,Is.True);
            prefs.Difficulty=(BattleDifficulty)99;Assert.That(file.Save(prefs),Is.False);Assert.That(file.Load().Difficulty,Is.EqualTo(BattleDifficulty.Veteran));
        }
    }
}
