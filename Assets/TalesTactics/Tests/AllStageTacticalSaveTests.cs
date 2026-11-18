using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        public static int[] RevisedTacticalStages=>Enumerable.Range(6,50).Where(CrossoverTacticalMissions.Contains).ToArray();

        static void PutLegacyActors(BattleSession b)
        {
            var state=b.Crossover.State.Copy();state.TacticsRevision=0;b.Crossover.Restore(state);
            foreach(var tile in b.Grid.Tiles.Values)tile.Occupant=null;
            var players=b.Units.Where(u=>u.Team==Team.Player).ToArray();
            var enemies=b.Units.Where(u=>u.Team==Team.Enemy).ToArray();
            bool split=b.CampaignStage==16;
            for(int i=0;i<players.Length;i++)
            {
                var p=CrossoverCampaign.PlayerSpawn(i);
                if(split){int half=(players.Length+1)/2,index=i>=half?i-half:i;p=new Vector2Int((i>=half?9:1)+index%2,1+index/2);}
                players[i].Position=p;players[i].MoveOrigin=p;
                if(players[i].Alive&&!players[i].CrossoverReserve)Assert.That(b.Grid.Place(players[i],p),Is.True);
            }
            for(int i=0;i<enemies.Length;i++)
            {
                var p=split&&i==0?new Vector2Int(3,11):split&&i==3?new Vector2Int(3,8):split&&i==4?new Vector2Int(10,11):CrossoverCampaign.EnemySpawn(i);
                enemies[i].Position=p;enemies[i].MoveOrigin=p;
                if(enemies[i].Alive&&!enemies[i].CrossoverReserve)Assert.That(b.Grid.Place(enemies[i],p),Is.True);
            }
        }
        static void AssertTacticalRestore(BattleSession b,int revision,string map)
        {
            Assert.That(b.Crossover.State.TacticsRevision,Is.EqualTo(revision));
            Assert.That(OpeningMapFingerprint(b.Grid),Is.EqualTo(map));
            Assert.That(b.Resolver.CrossoverSignals,Is.EqualTo(b.Crossover.SignalPositions));
            Assert.That(b.Resolver.CrossoverEnemySpawns,Is.EqualTo(b.Crossover.EnemySpawnPositions));
            foreach(var u in b.Units.Where(u=>u.Alive&&!u.CrossoverReserve))
                Assert.That(b.Grid[u.Position].Occupant,Is.SameAs(u),u.Data.Id);
        }
        [Category("AllStageTactics"),TestCaseSource(nameof(RevisedTacticalStages))]
        public void EveryNewLayoutSurvivesCheckpointRetryAndRewind(int stage)
        {
            var c=CrossoverTestCatalog();var party=new[]{0,1,2};
            var b=new BattleSession(c,party,campaignStage:stage,teamTurns:true);
            b.Opening=BattleOpening.Capture(b,c);string map=OpeningMapFingerprint(b.Grid);
            OpeningPlayerCommand(b);b.RecordAction("새 전장 저장");
            var before=b.Active.Position;
            var next=b.Grid.Neighbors(before).First(t=>t.Walkable&&t.Occupant==null).Coordinate;
            Assert.That(b.Grid.Place(b.Active,next),Is.True);
            var cp=BattleCheckpoint.Capture(b,party);
            var restored=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(cp)).Restore(c);
            AssertTacticalRestore(restored,1,map);Assert.That(restored.Active.Position,Is.EqualTo(next));
            var rewind=restored.RewindLast(out _);AssertTacticalRestore(rewind,1,map);
            Assert.That(rewind.Active.Position,Is.EqualTo(before));
            AssertTacticalRestore(restored.Opening.Restore(c),1,map);
        }
        [Category("AllStageTactics"),TestCaseSource(nameof(RevisedTacticalStages))]
        public void MissingTacticsRevisionPreservesEveryLegacyMapCheckpointRetryAndRewind(int stage)
        {
            var c=CrossoverTestCatalog();var party=new[]{0,1,2};
            var b=new BattleSession(c,party,campaignStage:stage,teamTurns:true);PutLegacyActors(b);
            b.Opening=BattleOpening.Capture(b,c);OpeningPlayerCommand(b);
            var candidates=b.Grid.Tiles.Values.Where(t=>t.Walkable&&t.Occupant==null&&!CampaignContent.Map(stage)[t.Coordinate].Walkable).ToArray();
            Assert.That(candidates,Is.Not.Empty,"The fixture must exercise a tile closed by the new map.");
            var legacyOnly=candidates[0].Coordinate;Assert.That(b.Grid.Place(b.Active,legacyOnly),Is.True);
            b.RecordAction("이전 전장 위치");
            var safe=b.Grid.Tiles.Values.First(t=>t.Walkable&&t.Occupant==null&&t.Coordinate!=legacyOnly).Coordinate;
            Assert.That(b.Grid.Place(b.Active,safe),Is.True);
            var cp=BattleCheckpoint.Capture(b,party);
            foreach(var frame in cp.History)frame.Json=Regex.Replace(frame.Json,"\\\"TacticsRevision\\\":0,","");
            string json=Regex.Replace(JsonUtility.ToJson(cp),"\\\"TacticsRevision\\\":0,","");
            Assert.That(json,Does.Not.Contain("TacticsRevision"));
            string map=OpeningMapFingerprint(CampaignContent.Map(stage,0,0));
            var restored=JsonUtility.FromJson<BattleCheckpoint>(json).Restore(c);AssertTacticalRestore(restored,0,map);
            var rewind=restored.RewindLast(out _);AssertTacticalRestore(rewind,0,map);
            Assert.That(rewind.Active.Position,Is.EqualTo(legacyOnly));
            AssertTacticalRestore(restored.Opening.Restore(c),0,map);
        }
        [Category("AllStageTactics"),TestCase(-1),TestCase(2)]
        public void TacticalRevisionRejectsUnknownVersions(int revision)
        {
            var b=new BattleSession(CrossoverTestCatalog(),new[]{0},campaignStage:9);
            var state=b.Crossover.State.Copy();state.TacticsRevision=revision;
            Assert.Throws<InvalidDataException>(()=>b.Crossover.Restore(state));
        }
        [Category("AllStageTactics"),TestCase(6),TestCase(26)]
        public void TacticalRevisionCannotOptAnOpeningOrDialogueIntoNewCombatLayouts(int stage)
        {
            var b=new BattleSession(CrossoverTestCatalog(),new[]{0},campaignStage:stage);
            var state=b.Crossover.State.Copy();state.TacticsRevision=1;
            Assert.Throws<InvalidDataException>(()=>b.Crossover.Restore(state));
        }
        [Category("AllStageTactics"),TestCase(false),TestCase(true)]
        public void CheckpointRejectsOpeningOrHistoryFromAnotherLayoutRevision(bool history)
        {
            var c=CrossoverTestCatalog();var b=new BattleSession(c,new[]{0},campaignStage:9,teamTurns:true);
            b.Opening=BattleOpening.Capture(b,c);b.Advance();b.RecordAction("호환성");
            var cp=BattleCheckpoint.Capture(b,new[]{0});
            if(history)
            {
                var nested=JsonUtility.FromJson<BattleCheckpoint>(cp.History[0].Json);
                nested.Crossover.TacticsRevision=0;
                cp.History[0].Json=JsonUtility.ToJson(nested);
            }
            else cp.Opening.Crossover.TacticsRevision=0;
            Assert.Throws<InvalidDataException>(()=>cp.Restore(c));
        }
        [Category("AllStageTactics"),Test]
        public void SanctuaryAndForecastUseMovedSignalCoordinatesWithoutChangingLiveState()
        {
            var b=new BattleSession(AuthoredCrossoverCatalog(),new[]{0},campaignStage:23);
            var hero=b.Units[0];var boss=b.Crossover.Boss;var node=b.Crossover.SignalPositions[0];
            var signal=b.Grid.Tiles.Values.First(t=>t.Walkable&&t.Occupant==null&&GridMap.Distance(t.Coordinate,node)<=1&&CrossoverBattle.Nodes.All(p=>GridMap.Distance(p,t.Coordinate)>1)).Coordinate;
            Assert.That(CrossoverBattle.Nodes.All(p=>GridMap.Distance(p,signal)>1),Is.True);
            Assert.That(b.Grid.Place(hero,signal),Is.True);
            var skill=Skill(EffectKind.Damage);skill.BossMechanic=BossMechanic.Sanctuary;skill.Range=30;
            b.Crossover.State.Nodes=0;Assert.That(b.Resolver.Targets(boss,skill,signal),Does.Contain(hero));
            b.Crossover.State.Nodes=1;Assert.That(b.Resolver.Targets(boss,skill,signal),Has.No.Member(hero));
            var projected=new BattleProjection(b);
            Assert.That(projected.Resolver.Targets(projected.Copies[boss],skill,signal),Has.No.Member(projected.Copies[hero]));
            Assert.That(projected.Resolver.CrossoverSignals,Is.Not.SameAs(b.Resolver.CrossoverSignals));
            projected.Resolver.CrossoverState.Nodes=0;
            Assert.That(projected.Resolver.Targets(projected.Copies[boss],skill,signal),Does.Contain(projected.Copies[hero]));
            Assert.That(b.Crossover.State.Nodes,Is.EqualTo(1));
        }
        [Category("AllStageTactics"),TestCase(11),TestCase(13)]
        public void SummonsRespectAuthoredOccupiedCirclesAndForecastCopies(int stage)
        {
            var b=new BattleSession(AuthoredCrossoverCatalog(),new[]{0},campaignStage:stage);
            var boss=b.Crossover.Boss;var positions=b.Crossover.EnemySpawnPositions;
            Assert.That(b.Grid.Place(b.Units[0],positions[6]),Is.True);
            var skill=Skill(EffectKind.Damage);skill.BossMechanic=BossMechanic.Summon;
            CrossoverBossTactics.Resolve(boss,skill,boss.Position,b.Grid,b.Units,b.Crossover.State,b.Resolver.CrossoverEnemySpawns);
            var enemies=b.Units.Where(u=>u.Team==Team.Enemy).ToArray();
            Assert.That(enemies[6].Alive,Is.False);
            Assert.That(enemies[7].Alive,Is.True);Assert.That(enemies[7].Position,Is.EqualTo(positions[7]));
            var projection=new BattleProjection(b);
            Assert.That(projection.Resolver.CrossoverEnemySpawns,Is.EqualTo(positions));
            Assert.That(projection.Resolver.CrossoverEnemySpawns,Is.Not.SameAs(b.Resolver.CrossoverEnemySpawns));
        }
    }
}
