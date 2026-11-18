using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        static string OpeningMapFingerprint(GridMap map)=>string.Join(";",map.Tiles.Values.OrderBy(t=>t.Coordinate.x).ThenBy(t=>t.Coordinate.y).Select(t=>$"{t.Coordinate}:{t.Height}:{t.Walkable}:{t.Terrain}:{t.MovementCost}"));
        static void OpeningPlayerCommand(BattleSession battle)
        {
            for(int n=0;n<100;n++){battle.Advance();if(battle.Active?.Team==Team.Player)return;battle.EndTurn();}
            Assert.Fail("No player command in the opening scheduler");
        }
        static void AssertOpeningPlacement(BattleSession battle)
        {
            var living=battle.Units.Where(u=>u.Alive).ToArray();
            Assert.That(living.Select(u=>u.Position).Distinct().Count(),Is.EqualTo(living.Length));
            foreach(var u in battle.Units)
            {
                Assert.That(battle.Grid[u.Position]?.Walkable,Is.True,u.Data.Id+" "+u.Position);
                if(u.Alive)Assert.That(battle.Grid[u.Position].Occupant,Is.SameAs(u),u.Data.Id);
            }
        }
        [Category("OpeningMissions")]
        [TestCase(6)] [TestCase(7)] [TestCase(8)]
        public void OpeningMissionsHaveDistinctReachableObjectivesAndHonestBriefings(int stage)
        {
            var b=new BattleSession(CrossoverTestCatalog(),new[]{0,1,2},campaignStage:stage);
            Assert.That(b.Crossover.State.OpeningRevision,Is.EqualTo(1));AssertOpeningPlacement(b);
            var reached=new HashSet<Vector2Int>{b.Units[0].Position};var pending=new Queue<Vector2Int>(reached);
            while(pending.Count>0)
            {
                var p=pending.Dequeue();foreach(var t in b.Grid.Neighbors(p))
                    if(t.Walkable&&Mathf.Abs(t.Height-b.Grid[p].Height)<=1&&reached.Add(t.Coordinate))pending.Enqueue(t.Coordinate);
            }
            foreach(var p in b.Crossover.SignalPositions.Concat(b.Crossover.PrisonDevices.Select(u=>u.Position)).Concat(b.Crossover.UsesExit?new[]{b.Crossover.ExitPosition}:Array.Empty<Vector2Int>()))
                Assert.That(reached,Does.Contain(p),"Objective isolated: "+p);
            Assert.That(CampaignContent.Briefing(stage),Is.EqualTo(b.Crossover.Description));
            Assert.That(CampaignMissions.Description(stage),Is.EqualTo(b.Crossover.Description));
            if(stage==7)
            {
                Assert.That(b.Crossover.UsesNodes,Is.False);Assert.That(b.Crossover.UsesExit,Is.True);
                Assert.That(b.Units.Where(u=>u.Team==Team.Player).All(u=>u.Position!=b.Crossover.ExitPosition),Is.True);
                Assert.That(MissionBriefing.Progress(b),Does.Contain("감옥 장치 0 / 2"));
                Assert.That(b.Crossover.Description,Does.Contain("격파 불필요"));
            }
            else
            {
                Assert.That(b.Crossover.SignalPositions.Length,Is.EqualTo(stage==6?1:3));
                Assert.That(MissionBriefing.Progress(b),Does.Contain(stage==6?"구조 신호 0 / 1":"구조 신호 0 / 3"));
                Assert.That(CampaignContent.Briefing(stage),Does.Contain("깊은 물은 통과 불가"));
                Assert.That(MissionBriefing.Terrain(stage),Does.Contain("깊은 물은 통과 불가"));
                foreach(var t in b.Grid.Tiles.Values.Where(t=>t.Terrain==TerrainType.Water))Assert.That(t.Walkable,Is.False);
                var crossings=b.Grid.Tiles.Values.Where(t=>t.Coordinate.y==5&&t.Walkable).Select(t=>t.Coordinate.x).OrderBy(x=>x);
                Assert.That(crossings,Is.EqualTo(stage==6?new[]{5,6,7}:new[]{2,6,10}));
            }
        }
        [Category("OpeningMissions")]
        [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void ThirdOpeningSpreadsActualJoinedPartyAndForcedFarahAcrossThreeBridges(int count)
        {
            var c=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");Assert.That(c,Is.Not.Null);
            var save=new CampaignSave();save.StoryProgress.Add(CrossoverCampaign.Flag(1));save.StoryProgress.Add(CrossoverCampaign.Flag(2));
            var prior=Enumerable.Range(0,c.Characters.Length).Where(i=>CampaignRecruitment.Joined(save,c.Characters[i].Id)).Take(count-1).ToArray();
            Assert.That(prior.Length,Is.EqualTo(count-1));
            var deployment=CampaignRecruitment.BattleDeployment(save,c,8,prior);
            Assert.That(deployment.Length,Is.EqualTo(count));Assert.That(c.Characters[deployment.Last()].Id,Is.EqualTo("farah"));
            var b=new BattleSession(c,deployment,campaignStage:8);AssertOpeningPlacement(b);
            var players=b.Units.Where(u=>u.Team==Team.Player).ToArray();
            Assert.That(players.Select(u=>u.Position.x).Distinct().OrderBy(x=>x),Is.EqualTo(new[]{2,6,10}));
            Assert.That(players.Select((u,i)=>u.Position==CampaignContent.PlayerSpawn(8,i)).All(v=>v),Is.True);
            Assert.That(players.GroupBy(u=>u.Position.x).Max(g=>g.Count())-players.GroupBy(u=>u.Position.x).Min(g=>g.Count()),Is.LessThanOrEqualTo(1));
            Assert.That(CampaignRecruitment.Joined(save,"farah"),Is.False,"Inspecting deployment does not commit recruitment");
            Assert.That(CampaignRecruitment.TryBegin(save,c,8,_=>true),Is.True);Assert.That(CampaignRecruitment.Joined(save,"farah"),Is.True);
        }
        [Category("OpeningMissions")]
        [TestCase(true)] [TestCase(false)]
        public void FirstOpeningRequiresActualRescueAndGuardianInEitherOrder(bool rescueFirst)
        {
            var b=new BattleSession(CrossoverTestCatalog(),new[]{0,1},campaignStage:6);var hero=b.Units[0];
            Action rescue=()=>{Assert.That(b.Grid.Place(hero,new Vector2Int(6,6)),Is.True);b.Crossover.Update();};
            Action defeat=()=>{b.Crossover.Boss.Damage(99999,b.Grid);Assert.That(b.Crossover.Boss.Alive,Is.False);};
            if(rescueFirst)rescue();else defeat();Assert.That(b.Result,Is.EqualTo(BattleResult.Ongoing));
            if(rescueFirst)defeat();else rescue();Assert.That(b.Result,Is.EqualTo(BattleResult.Victory));
            Assert.That(b.Crossover.NodeCount,Is.EqualTo(1));Assert.That(b.Units.Any(u=>u.Team==Team.Enemy&&u.Alive),Is.True);
        }
        [Category("OpeningMissions")]
        [Test] public void PrisonOpeningRequiresBothLocksAndSouthRetreatWhileBossAndOptionalDeviceSurvive()
        {
            var b=new BattleSession(CrossoverTestCatalog(),new[]{0,1,2},campaignStage:7);var hero=b.Units[0];
            Assert.That(b.Grid.Place(hero,b.Crossover.ExitPosition),Is.True);
            Assert.That(b.Result,Is.EqualTo(BattleResult.Ongoing));
            b.Crossover.PrisonDevices[0].Damage(99999,b.Grid);b.Crossover.Update();Assert.That(b.Result,Is.EqualTo(BattleResult.Ongoing));
            Assert.That(MissionBriefing.Progress(b),Does.Contain("감옥 장치 1 / 2"));
            Assert.That(b.Grid.Place(hero,new Vector2Int(5,1)),Is.True);
            b.Crossover.PrisonDevices[1].Damage(99999,b.Grid);b.Crossover.Update();Assert.That(b.Result,Is.EqualTo(BattleResult.Ongoing));
            Assert.That(b.Crossover.PrisonRouteOpen,Is.True);Assert.That(b.Crossover.Boss.Alive,Is.True);
            Assert.That(b.Units.Any(u=>u.Data.Id=="boundary-device"&&u.Alive),Is.True,"Third seal is optional");
            Assert.That(b.Grid.Place(hero,b.Crossover.ExitPosition),Is.True);Assert.That(b.Result,Is.EqualTo(BattleResult.Victory));
        }
        [Category("OpeningMissions")]
        [Test] public void ThirdOpeningRequiresEveryDistributedSignalAndNorthExitWhileBossSurvives()
        {
            var b=new BattleSession(CrossoverTestCatalog(),new[]{0,1,2},campaignStage:8);var hero=b.Units[0];
            Assert.That(b.Grid.Place(hero,b.Crossover.ExitPosition),Is.True);Assert.That(b.Result,Is.EqualTo(BattleResult.Ongoing));
            foreach(var p in b.Crossover.SignalPositions){Assert.That(b.Grid.Place(hero,p),Is.True);b.Crossover.Update();Assert.That(b.Result,Is.EqualTo(BattleResult.Ongoing));}
            Assert.That(b.Crossover.AllSignalsConnected,Is.True);Assert.That(b.Crossover.Boss.Alive,Is.True);
            Assert.That(b.Grid.Place(hero,b.Crossover.ExitPosition),Is.True);Assert.That(b.Result,Is.EqualTo(BattleResult.Victory));
        }
        [Category("OpeningMissions")]
        [Test] public void OpeningRescueMoveUndoAndRewindRestoreSignalAndPosition()
        {
            var c=CrossoverTestCatalog();var b=new BattleSession(c,new[]{0,1,2},campaignStage:6,teamTurns:true);b.Advance();
            Assert.That(b.Grid.Place(b.Active,new Vector2Int(6,4)),Is.True);b.RecordAction("다리 진입 전");
            Assert.That(b.Move(new Vector2Int(6,6)),Is.True);Assert.That(b.Crossover.NodeCount,Is.EqualTo(1));
            Assert.That(b.UndoMove(),Is.True);Assert.That(b.Crossover.NodeCount,Is.Zero);Assert.That(b.Active.Position,Is.EqualTo(new Vector2Int(6,4)));
            Assert.That(b.Move(new Vector2Int(6,6)),Is.True);var restored=b.RewindLast(out _);
            Assert.That(restored.Crossover.NodeCount,Is.Zero);Assert.That(restored.Active.Position,Is.EqualTo(new Vector2Int(6,4)));
            Assert.That(restored.Crossover.State.OpeningRevision,Is.EqualTo(1));
        }
        [Category("OpeningMissions")]
        [TestCase(6,0)] [TestCase(6,1)] [TestCase(6,2)] [TestCase(7,0)] [TestCase(7,1)] [TestCase(7,2)] [TestCase(8,0)] [TestCase(8,1)] [TestCase(8,2)]
        public void RevisedOpeningCheckpointRetryAndRewindPreserveMapAndProgress(int stage,int mode)
        {
            var c=CrossoverTestCatalog();var party=new[]{0,1,2};var b=new BattleSession(c,party,campaignStage:stage,useCT:mode==1,teamTurns:mode==2);
            b.Opening=BattleOpening.Capture(b,c);string map=OpeningMapFingerprint(b.Grid);OpeningPlayerCommand(b);b.RecordAction("목표 진행 전");
            if(stage==7)b.Crossover.PrisonDevices[0].Damage(99999,b.Grid);
            else Assert.That(b.Grid.Place(b.Active,b.Crossover.SignalPositions[0]),Is.True);
            b.Crossover.Update();var cp=BattleCheckpoint.Capture(b,party);
            var restored=JsonUtility.FromJson<BattleCheckpoint>(JsonUtility.ToJson(cp)).Restore(c);
            Assert.That(restored.Crossover.State.OpeningRevision,Is.EqualTo(1));Assert.That(OpeningMapFingerprint(restored.Grid),Is.EqualTo(map));AssertOpeningPlacement(restored);
            if(stage==7)Assert.That(restored.Crossover.PrisonDevices[0].Alive,Is.False);else Assert.That(restored.Crossover.NodeCount,Is.EqualTo(1));
            var rewind=restored.RewindLast(out _);Assert.That(rewind.Crossover.State.OpeningRevision,Is.EqualTo(1));Assert.That(OpeningMapFingerprint(rewind.Grid),Is.EqualTo(map));
            Assert.That(rewind.Crossover.NodeCount,Is.Zero);if(stage==7)Assert.That(rewind.Crossover.PrisonDevices.All(u=>u.Alive),Is.True);
            var retry=restored.Opening.Restore(c);Assert.That(retry.Crossover.State.OpeningRevision,Is.EqualTo(1));Assert.That(OpeningMapFingerprint(retry.Grid),Is.EqualTo(map));AssertOpeningPlacement(retry);
            Assert.That(retry.Crossover.NodeCount,Is.Zero);if(stage==7)Assert.That(retry.Crossover.PrisonDevices.All(u=>u.Alive),Is.True);
        }
        [Category("OpeningMissions")]
        [TestCase(6)] [TestCase(7)] [TestCase(8)]
        public void PreRevisionJsonRestoresOriginalMapGoalsRetryAndRewindBeforePlacingUnits(int stage)
        {
            var c=CrossoverTestCatalog();var party=new[]{0,1,2};var b=new BattleSession(c,party,campaignStage:stage,teamTurns:true);
            var old=b.Crossover.State.Copy();old.OpeningRevision=0;b.Crossover.Restore(old);
            foreach(var tile in b.Grid.Tiles.Values)tile.Occupant=null;
            var players=b.Units.Where(u=>u.Team==Team.Player).ToArray();var enemies=b.Units.Where(u=>u.Team==Team.Enemy).ToArray();
            for(int i=0;i<players.Length;i++)Assert.That(b.Grid.Place(players[i],CrossoverCampaign.PlayerSpawn(i)),Is.True);
            for(int i=0;i<enemies.Length;i++)Assert.That(b.Grid.Place(enemies[i],CrossoverCampaign.EnemySpawn(i)),Is.True);
            b.Opening=BattleOpening.Capture(b,c);b.Advance();var oldOnlyTile=stage==7?new Vector2Int(6,5):new Vector2Int(0,5);
            Assert.That(CampaignContent.Map(stage)[oldOnlyTile].Walkable,Is.False);Assert.That(b.Grid.Place(b.Active,oldOnlyTile),Is.True);
            b.RecordAction("구버전 위치");Assert.That(b.Grid.Place(b.Active,new Vector2Int(0,4)),Is.True);
            var cp=BattleCheckpoint.Capture(b,party);
            // Remove the new field from both JSON levels, including escaped history payloads.
            foreach(var frame in cp.History)frame.Json=Regex.Replace(frame.Json,"\"OpeningRevision\":0,","");
            string json=Regex.Replace(JsonUtility.ToJson(cp),"\"OpeningRevision\":0,","");
            Assert.That(json,Does.Not.Contain("OpeningRevision"));var restored=JsonUtility.FromJson<BattleCheckpoint>(json).Restore(c);
            string map=OpeningMapFingerprint(CrossoverCampaign.Map(stage,0,0));
            Assert.That(restored.Crossover.State.OpeningRevision,Is.Zero);Assert.That(restored.Crossover.RevisedOpening,Is.False);
            Assert.That(OpeningMapFingerprint(restored.Grid),Is.EqualTo(map));AssertOpeningPlacement(restored);
            var rewind=restored.RewindLast(out _);Assert.That(rewind.Active.Position,Is.EqualTo(oldOnlyTile));Assert.That(OpeningMapFingerprint(rewind.Grid),Is.EqualTo(map));
            var retry=restored.Opening.Restore(c);Assert.That(retry.Crossover.State.OpeningRevision,Is.Zero);Assert.That(OpeningMapFingerprint(retry.Grid),Is.EqualTo(map));AssertOpeningPlacement(retry);
            if(stage==8){Assert.That(restored.Crossover.SignalPositions,Is.EqualTo(CrossoverBattle.Nodes));Assert.That(restored.Crossover.Description,Does.Not.Contain("깊은 물"));}
            else{Assert.That(restored.Crossover.UsesNodes,Is.False);Assert.That(restored.Crossover.UsesExit,Is.False);restored.Crossover.Boss.BossPhaseLock=false;restored.Crossover.Boss.Damage(99999,restored.Grid);Assert.That(restored.Result,Is.EqualTo(BattleResult.Victory));}
        }
        [Category("OpeningMissions")]
        [TestCase(6)] [TestCase(7)] [TestCase(8)]
        public void RevisedOpeningHazardsProtectTheActualSignalAndExitTiles(int stage)
        {
            var b=new BattleSession(CrossoverTestCatalog(),new[]{0},campaignStage:stage);var boss=b.Crossover.Boss;
            var flame=Skill(EffectKind.Damage);flame.BossMechanic=BossMechanic.FlameField;flame.Range=30;
            foreach(var p in b.Crossover.SignalPositions.Concat(b.Crossover.UsesExit?new[]{b.Crossover.ExitPosition}:Array.Empty<Vector2Int>()))
                b.Crossover.OnResolved(boss,flame,p);
            Assert.That(b.Crossover.State.Hazards,Is.Empty,"Mission markers remain safe");
            b.Crossover.OnResolved(boss,flame,new Vector2Int(1,1));Assert.That(b.Crossover.State.Hazards.Count,Is.EqualTo(1),"Control tile proves hazard creation was exercised");
        }
        [Category("OpeningMissions")]
        [Test] public void OpeningRevisionRejectsUnknownVersionsAndLeavesExplicitLegacyMapsUnchanged()
        {
            var c=CrossoverTestCatalog();var b=new BattleSession(c,new[]{0},campaignStage:6);
            foreach(int invalid in new[]{-1,2}){var state=b.Crossover.State.Copy();state.OpeningRevision=invalid;Assert.Throws<InvalidDataException>(()=>b.Crossover.Restore(state));}
            var later=new BattleSession(c,new[]{0},campaignStage:9);var wrong=later.Crossover.State.Copy();wrong.OpeningRevision=1;Assert.Throws<InvalidDataException>(()=>later.Crossover.Restore(wrong));
            for(int stage=9;stage<=25;stage++)Assert.That(OpeningMapFingerprint(CampaignContent.Map(stage,0,0)),Is.EqualTo(OpeningMapFingerprint(CrossoverCampaign.Map(stage,0,0))),"Legacy main "+stage);
            for(int stage=26;stage<56;stage++)Assert.That(OpeningMapFingerprint(CampaignContent.Map(stage,0,0)),Is.EqualTo(OpeningMapFingerprint(CrossoverCampaign.Map(6+(stage-26)%20,0,0))),"Legacy personal "+stage);
        }
    }
}
