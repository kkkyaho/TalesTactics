using System.IO;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [TestCase(0,6,99)] [TestCase(1,7,120)] [TestCase(2,8,130)]
        [TestCase(3,8,144)] [TestCase(4,10,140)] [TestCase(5,10,169)]
        public void ExpandedEncountersKeepMapAndValidSeparatedRosters(int stage,int total,int tiles)
        {
            var c=FreeTurnCatalog();
            foreach(int mode in new[]{0,1,2})
            {
                var b=new BattleSession(c,new[]{0,1,2},campaignStage:stage,teamTurns:mode==0,useCT:mode==2,bossEncounters:true,expandedEncounters:true);
                Assert.That(b.Grid.Tiles.Count,Is.EqualTo(tiles));Assert.That(CampaignEncounters.Total(stage),Is.EqualTo(total));
                Assert.That(b.Units.Count(u=>u.Team==Team.Enemy),Is.EqualTo(total-(mode==0&&(stage==2||stage==4)?2:0)));
                Assert.That(b.Units.Select(u=>u.Position).Distinct().Count(),Is.EqualTo(b.Units.Count));
                Assert.That(b.Units.All(u=>b.Grid[u.Position].Walkable&&b.Grid[u.Position].Occupant==u),Is.True);
                var reached=new HashSet<Vector2Int>{b.Units[0].Position};var queue=new Queue<Vector2Int>(reached);
                while(queue.Count>0){var origin=queue.Dequeue();foreach(var tile in b.Grid.Neighbors(origin))if(tile.Walkable&&Mathf.Abs(tile.Height-b.Grid[origin].Height)<=1&&reached.Add(tile.Coordinate))queue.Enqueue(tile.Coordinate);}
                Assert.That(b.Units.All(u=>reached.Contains(u.Position)),Is.True);
                b.Opening=BattleOpening.Capture(b,c);Assert.That(b.Opening.Restore(c).Units.Count,Is.EqualTo(b.Units.Count));
            }
        }
        [TestCase(2,false)] [TestCase(2,true)] [TestCase(4,false)] [TestCase(4,true)]
        public void ExpandedWaveIsFiniteAndSuspendRewindRetryPreserveRoster(int stage,bool missionEvents)
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0,1},campaignStage:stage,enemyLevel:5,teamTurns:true,expandedEncounters:true,missionEvents:missionEvents);
            b.Opening=BattleOpening.Capture(b,c);int initial=b.Units.Count;b.Advance();b.RecordAction("증원 전");
            NextMissionRound(b);NextMissionRound(b);Assert.That(b.Units.Count,Is.EqualTo(initial+2));Assert.That(b.ReinforcementsArrived,Is.True);
            Assert.That(b.Units.Select(u=>u.Position).Distinct().Count(),Is.EqualTo(b.Units.Count));
            Assert.That(b.Units.Skip(initial).All(u=>u.Level==5&&u.TurnsStarted==0),Is.True);
            var cp=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(BattleCheckpoint.Capture(b,new[]{0,1})));
            var restored=cp.Restore(c);Assert.That(JsonUtility.ToJson(BattleCheckpoint.Capture(restored,new[]{0,1})),Is.EqualTo(JsonUtility.ToJson(cp)));
            NextMissionRound(restored);Assert.That(restored.Units.Count,Is.EqualTo(initial+2));Assert.That(restored.Units.Skip(initial).All(u=>u.TurnsStarted==1),Is.True);
            var retry=restored.Opening.Restore(c);Assert.That(retry.Units.Count,Is.EqualTo(initial));Assert.That(retry.ReinforcementsArrived,Is.False);
            var rewind=restored.RewindLast(out _);Assert.That(rewind.Units.Count,Is.EqualTo(initial));Assert.That(rewind.ReinforcementsArrived,Is.False);
            NextMissionRound(rewind);NextMissionRound(rewind);Assert.That(rewind.Units.Count,Is.EqualTo(initial+2));
        }
        [Test] public void LegacyCheckpointsAndOpeningsKeepOriginalFourEnemies()
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0},campaignStage:2,teamTurns:true);
            b.Opening=BattleOpening.Capture(b,c);b.Opening.Version=4;b.Opening.ExpandedEncounters=true;b.Advance();
            var cp=BattleCheckpoint.Capture(b,new[]{0});cp.Version=9;cp.ExpandedEncounters=true;
            var restored=cp.Restore(c);Assert.That(restored.ExpandedEncounters,Is.False);Assert.That(restored.Units.Count,Is.EqualTo(5));
            Assert.That(restored.Opening.Restore(c).Units.Count,Is.EqualTo(5));
            NextMissionRound(restored);NextMissionRound(restored);Assert.That(restored.Units.Count,Is.EqualTo(5));
            Assert.That(new BattleSession(c,new[]{0},expandedEncounters:true).ExpandedEncounters,Is.False);
        }
        [Test] public void ExpandedCheckpointRejectsMismatchedOpeningAndHistory()
        {
            var c=FreeTurnCatalog();var b=new BattleSession(c,new[]{0},campaignStage:0,teamTurns:true,expandedEncounters:true);
            var old=new BattleSession(c,new[]{0},campaignStage:0,teamTurns:true);old.Opening=BattleOpening.Capture(old,c);
            b.Advance();var cp=BattleCheckpoint.Capture(b,new[]{0});cp.HasOpening=true;cp.Opening=old.Opening;
            Assert.Throws<InvalidDataException>(()=>cp.Restore(c));cp.HasOpening=false;cp.Opening=null;
            old.Advance();old.RecordAction("이전 구성");cp.History=old.History.ToArray();Assert.Throws<InvalidDataException>(()=>cp.Restore(c));
        }
    }
}
