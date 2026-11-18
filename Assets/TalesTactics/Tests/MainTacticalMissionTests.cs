using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        static IEnumerable<int> MainTacticalStages=>Enumerable.Range(CrossoverCampaign.FirstStage+3,17);
        static string MainTacticalFingerprint(GridMap map)=>string.Join(";",map.Tiles.Values.OrderBy(t=>t.Coordinate.x).ThenBy(t=>t.Coordinate.y)
            .Select(t=>$"{t.Coordinate}:{t.Height}:{t.Walkable}:{t.Terrain}:{t.MovementCost}"));

        // Explore repeated legal movement turns, using the actual MOV/JMP and terrain rules.
        // The map is empty here: enemies can be defeated; this proves topology, not AI difficulty.
        HashSet<Vector2Int> MainTacticalReachable(GridMap map,Vector2Int start)
        {
            var mover=New<CharacterData>();mover.Id="route-check";
            mover.BaseStats=new Stats{HP=10,MP=10,SPD=1,MOV=4,JMP=1};
            var unit=new UnitRuntime(mover,Team.Player,rules);
            Assert.That(unit.Stats.JMP,Is.EqualTo(1));Assert.That(unit.Stats.MOV,Is.EqualTo(4));
            Assert.That(map.Place(unit,start),Is.True,"Authored start cannot be placed");
            var reached=new HashSet<Vector2Int>{start};var pending=new Queue<Vector2Int>();pending.Enqueue(start);
            while(pending.Count>0)
            {
                var position=pending.Dequeue();Assert.That(map.Place(unit,position),Is.True);
                foreach(var destination in map.Reachable(unit,out _).Keys)
                    if(reached.Add(destination))pending.Enqueue(destination);
            }
            return reached;
        }
        BattleCatalog MainTacticalCatalog()
        {
            var c=CrossoverTestCatalog();c.Rules.MaxDeployment=6;
            c.Characters=Enumerable.Range(0,6).Select(i=>
            {
                var hero=New<CharacterData>();hero.Id="main-tactical-"+i;hero.DisplayName=hero.Id;hero.BaseStats=data.BaseStats;
                hero.BasicAttack=Skill(EffectKind.Damage);hero.Skills=Array.Empty<SkillData>();return hero;
            }).ToArray();return c;
        }
        static void MainTacticalDefeat(BattleSession battle,UnitRuntime target)
        {
            for(int attempt=0;attempt<4&&target.Alive;attempt++)
            {
                target.Damage(99999,battle.Grid);
                if(target.BossPhaseLock){target.BeginTurn();target.Acted=true;target.EndTurn();}
            }
            Assert.That(target.Alive,Is.False,target.Data.Id+" did not complete its phase sequence");
        }
        static void MainTacticalConnect(BattleSession battle,UnitRuntime hero,Vector2Int signal)
        {Assert.That(battle.Grid.Place(hero,signal),Is.True,"Signal occupied or blocked: "+signal);battle.Crossover.Update();}

        [Category("TacticalMissions")]
        [TestCaseSource(nameof(MainTacticalStages))]
        public void MainTacticalLayoutsHaveAuthoredStandingTilesAndLowJumpRoutesForEverySlot(int stage)
        {
            var layout=MainTacticalMissions.Get(stage);var map=layout.Map();
            Assert.That(layout.Stage,Is.EqualTo(stage));Assert.That(map.Tiles.Count,Is.EqualTo(169));
            Assert.That(layout.PlayerSpawns.Length,Is.EqualTo(6));Assert.That(layout.EnemySpawns.Length,Is.EqualTo(9));
            var actors=layout.PlayerSpawns.Concat(layout.EnemySpawns).ToArray();
            Assert.That(actors.Distinct().Count(),Is.EqualTo(15),"Every active, dead and reserved slot needs its own tile");
            Assert.That(layout.Signals.Distinct().Count(),Is.EqualTo(layout.Signals.Length));
            Assert.That(layout.Signals.Intersect(actors),Is.Empty,"No rescue is completed at deployment or hidden under an enemy");
            Assert.That(layout.PlayerSpawns,Has.No.Member(layout.Exit));
            foreach(var position in actors.Concat(layout.Signals).Concat(new[]{layout.Exit}))
                Assert.That(map[position]?.Walkable,Is.True,layout.Location+" authored tile "+position);
            var reached=MainTacticalReachable(map,layout.PlayerSpawns[0]);
            foreach(var position in actors.Concat(layout.Signals).Concat(new[]{layout.Exit}))
                Assert.That(reached,Does.Contain(position),layout.Location+" unreachable tile "+position);
            Assert.That(reached.Count,Is.EqualTo(map.Tiles.Values.Count(t=>t.Walkable)),"No isolated walkable pocket behind authored walls");
            foreach(var tile in map.Tiles.Values.Where(t=>t.Terrain==TerrainType.Water))
            {
                Assert.That(tile.Walkable,Is.False);Assert.That(layout.Hint,Does.Contain("깊은 물은 통과 불가"));
            }
            Assert.That(layout.Name,Is.EqualTo(CrossoverCampaign.Episode(stage).Title));
            Assert.That(layout.Location,Is.Not.Empty);Assert.That(layout.Description,Is.Not.Empty);Assert.That(layout.Hint,Is.Not.Empty);
        }

        [Category("TacticalMissions")]
        [Test] public void AllSeventeenMainLayoutsHaveDistinctGeometryAndReturnIndependentMaps()
        {
            var layouts=MainTacticalStages.Select(MainTacticalMissions.Get).ToArray();
            Assert.That(layouts.Select(l=>l.Location).Distinct().Count(),Is.EqualTo(17));
            Assert.That(layouts.Select(l=>MainTacticalFingerprint(l.Map())).Distinct().Count(),Is.EqualTo(17),"Coordinates alone do not count as different maps");
            foreach(var layout in layouts)
            {
                var first=layout.Map();var original=MainTacticalFingerprint(first);first[layout.PlayerSpawns[0]].Walkable=false;
                Assert.That(MainTacticalFingerprint(layout.Map()),Is.EqualTo(original),layout.Location+" leaked another battle's terrain mutation");
            }
            Assert.Throws<ArgumentOutOfRangeException>(()=>MainTacticalMissions.Get(8));
            Assert.Throws<ArgumentOutOfRangeException>(()=>MainTacticalMissions.Get(26));
        }

        [Category("TacticalMissions")]
        [TestCaseSource(nameof(MainTacticalStages))]
        public void MainTacticalSessionsUseTheAuthoredLayoutAndSharedLiveObjectives(int stage)
        {
            var layout=MainTacticalMissions.Get(stage);var battle=new BattleSession(MainTacticalCatalog(),Enumerable.Range(0,6),campaignStage:stage);
            Assert.That(battle.Crossover.State.TacticsRevision,Is.EqualTo(1));Assert.That(battle.Crossover.State.OpeningRevision,Is.Zero);
            Assert.That(MainTacticalFingerprint(battle.Grid),Is.EqualTo(MainTacticalFingerprint(layout.Map())));
            Assert.That(battle.Crossover.SignalPositions,Is.EqualTo(layout.Signals));Assert.That(battle.Crossover.ExitPosition,Is.EqualTo(layout.Exit));
            Assert.That(battle.Crossover.UsesNodes,Is.EqualTo(layout.Signals.Length>0));
            Assert.That(battle.Crossover.RequiredNodeMask,Is.EqualTo((1<<layout.Signals.Length)-1));
            Assert.That(battle.Crossover.UsesExit,Is.EqualTo(stage==14||stage==21));
            Assert.That(battle.Result,Is.EqualTo(BattleResult.Ongoing));
            var actors=battle.Units.Where(u=>u.Alive&&!u.CrossoverReserve).ToArray();
            Assert.That(actors.Select(u=>u.Position).Distinct().Count(),Is.EqualTo(actors.Length));
            foreach(var unit in battle.Units)
            {
                Assert.That(battle.Grid[unit.Position]?.Walkable,Is.True,unit.Data.Id+" "+unit.Position);
                if(unit.Alive&&!unit.CrossoverReserve)Assert.That(battle.Grid[unit.Position].Occupant,Is.SameAs(unit));
                else Assert.That(battle.Grid[unit.Position].Occupant,Is.Not.SameAs(unit),"Dead/reserved unit occupies the map");
            }
            if(stage!=16)
            {
                Assert.That(battle.Units.Where(u=>u.Team==Team.Player).Select(u=>u.Position),Is.EqualTo(layout.PlayerSpawns));
                Assert.That(battle.Units.Where(u=>u.Team==Team.Enemy).Select(u=>u.Position),Is.EqualTo(layout.EnemySpawns));
                Assert.That(battle.Crossover.Description,Is.EqualTo(layout.Description));
            }
            Assert.That(CampaignContent.Briefing(stage),Is.EqualTo(layout.Description));
            Assert.That(CampaignMissions.Description(stage),Is.EqualTo(layout.Description));
            Assert.That(MissionBriefing.Terrain(stage),Is.EqualTo(layout.Hint));
        }

        [Category("TacticalMissions")]
        [TestCase(4,true)] [TestCase(4,false)] [TestCase(5,true)] [TestCase(5,false)] [TestCase(15,true)] [TestCase(15,false)]
        public void NewMainRouteObjectivesRequireBothTheirAuthoredSignalsAndBossInEitherOrder(int episode,bool signalsFirst)
        {
            var battle=new BattleSession(MainTacticalCatalog(),new[]{0,1,2},campaignStage:episode+5);var hero=battle.Units[0];
            Assert.That(battle.Crossover.SignalPositions.Length,Is.EqualTo(episode==5?2:1));
            Action signals=()=>
            {
                for(int i=0;i<battle.Crossover.SignalPositions.Length;i++)
                {
                    MainTacticalConnect(battle,hero,battle.Crossover.SignalPositions[i]);
                    Assert.That(battle.Crossover.NodeCount,Is.EqualTo(i+1));
                    if(i+1<battle.Crossover.SignalPositions.Length)Assert.That(battle.Result,Is.EqualTo(BattleResult.Ongoing));
                }
            };
            Action boss=()=>MainTacticalDefeat(battle,battle.Crossover.Boss);
            if(signalsFirst)signals();else boss();Assert.That(battle.Result,Is.EqualTo(BattleResult.Ongoing));
            if(signalsFirst)boss();else signals();Assert.That(battle.Result,Is.EqualTo(BattleResult.Victory));
            Assert.That(battle.Units.Any(u=>u.Team==Team.Enemy&&u.Alive),Is.True,"Ordinary enemies are not a new elimination requirement");
        }

        [Category("TacticalMissions")]
        [Test] public void NinthTacticalMissionRequiresThreeDevicesAndRescueBeforeReturningSouthWithBossAlive()
        {
            var battle=new BattleSession(MainTacticalCatalog(),new[]{0,1,2},campaignStage:14);var hero=battle.Units[0];
            Assert.That(battle.Crossover.ExitPosition,Is.EqualTo(new Vector2Int(6,0)));
            MainTacticalConnect(battle,hero,battle.Crossover.ExitPosition);Assert.That(battle.Result,Is.EqualTo(BattleResult.Ongoing));
            foreach(var signal in battle.Crossover.SignalPositions)MainTacticalConnect(battle,hero,signal);
            MainTacticalConnect(battle,hero,battle.Crossover.ExitPosition);Assert.That(battle.Result,Is.EqualTo(BattleResult.Ongoing),"Devices still hold the route");
            var devices=battle.Units.Where(u=>u.Data.Id=="boundary-device").ToArray();Assert.That(devices.Length,Is.EqualTo(3));
            devices[0].Damage(99999,battle.Grid);devices[1].Damage(99999,battle.Grid);Assert.That(battle.Result,Is.EqualTo(BattleResult.Ongoing));
            Assert.That(battle.Grid.Place(hero,new Vector2Int(6,3)),Is.True);
            devices[2].Damage(99999,battle.Grid);battle.Crossover.Update();Assert.That(battle.Result,Is.EqualTo(BattleResult.Ongoing),"Must return to the exit");
            Assert.That(battle.Grid.Place(hero,battle.Crossover.ExitPosition),Is.True);
            Assert.That(battle.Result,Is.EqualTo(BattleResult.Victory));Assert.That(battle.Crossover.Boss.Alive,Is.True);
        }

        [Category("TacticalMissions")]
        [TestCase(0)] [TestCase(1)]
        public void ThirteenthTacticalMissionKeepsChosenRescueOrderAndBothRoutes(int choice)
        {
            var battle=new BattleSession(MainTacticalCatalog(),new[]{0,1,2},campaignStage:18);var hero=battle.Units[0];
            battle.Crossover.State.Choice=choice;var signals=battle.Crossover.SignalPositions;
            Assert.That(signals[0].x,Is.LessThan(signals[1].x),"Choice zero stays west and choice one stays east");
            MainTacticalConnect(battle,hero,signals[1-choice]);Assert.That(battle.Crossover.NodeCount,Is.Zero);
            MainTacticalConnect(battle,hero,signals[choice]);Assert.That(battle.Crossover.State.Nodes,Is.EqualTo(1<<choice));
            MainTacticalConnect(battle,hero,signals[1-choice]);MainTacticalConnect(battle,hero,signals[2]);
            Assert.That(battle.Crossover.AllSignalsConnected,Is.True);Assert.That(battle.Result,Is.EqualTo(BattleResult.Ongoing));
            MainTacticalDefeat(battle,battle.Crossover.Boss);Assert.That(battle.Result,Is.EqualTo(BattleResult.Victory));
        }

        [Category("TacticalMissions")]
        [TestCase(false)] [TestCase(true)]
        public void SixteenthTacticalMissionKeepsLeaderSurvivalAndBossOptionalEscape(bool leaderFalls)
        {
            var battle=new BattleSession(MainTacticalCatalog(),new[]{0,1,2},campaignStage:21);var leader=battle.Units[0];var runner=battle.Units[1];
            Assert.That(battle.Crossover.ExitPosition,Is.EqualTo(new Vector2Int(1,12)));
            foreach(var signal in battle.Crossover.SignalPositions)MainTacticalConnect(battle,runner,signal);
            Assert.That(battle.Result,Is.EqualTo(BattleResult.Ongoing));
            if(leaderFalls)leader.Damage(99999,battle.Grid);
            Assert.That(battle.Grid.Place(runner,battle.Crossover.ExitPosition),Is.True);
            Assert.That(battle.Result,Is.EqualTo(leaderFalls?BattleResult.Defeat:BattleResult.Victory));
            Assert.That(battle.Crossover.Boss.Alive,Is.True);
        }

        [Category("TacticalMissions")]
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(6)]
        public void EleventhTacticalMissionRetainsFixedFrontPositionsAndSequentialReserveActivation(int count)
        {
            var battle=new BattleSession(MainTacticalCatalog(),Enumerable.Range(0,count),campaignStage:16);var party=battle.Units.Where(u=>u.Team==Team.Player).ToArray();
            var enemies=battle.Units.Where(u=>u.Team==Team.Enemy).ToArray();int west=(count+1)/2;
            Assert.That(enemies[0].Position,Is.EqualTo(new Vector2Int(3,11)));Assert.That(enemies[3].Position,Is.EqualTo(new Vector2Int(3,8)));
            Assert.That(enemies[4].Position,Is.EqualTo(new Vector2Int(10,11)));Assert.That(enemies[4].Alive,Is.False);
            for(int i=0;i<count;i++)
            {
                bool east=i>=west;int index=east?i-west:i;
                Assert.That(party[i].Position,Is.EqualTo(new Vector2Int((east?9:1)+index%2,1+index/2)));
                Assert.That(party[i].CrossoverReserve,Is.EqualTo(east));
            }
            MainTacticalDefeat(battle,enemies[0]);battle.Crossover.Update();
            Assert.That(battle.Crossover.State.Segment,Is.EqualTo(1));Assert.That(enemies[4].Alive,Is.True);
            Assert.That(battle.Result,Is.EqualTo(BattleResult.Ongoing));
            for(int i=0;i<count;i++)Assert.That(party[i].CrossoverReserve,Is.EqualTo(count>1&&i<west));
            foreach(var unit in battle.Units.Where(u=>u.Alive&&!u.CrossoverReserve))Assert.That(battle.Grid[unit.Position].Occupant,Is.SameAs(unit));
            MainTacticalDefeat(battle,enemies[4]);Assert.That(battle.Result,Is.EqualTo(BattleResult.Victory));
        }

        [Category("TacticalMissions")]
        [Test] public void FinalTacticalMissionRetainsAllSignalsDevicesAndThreeActualBossPhases()
        {
            var battle=new BattleSession(MainTacticalCatalog(),new[]{0,1,2},campaignStage:25);var boss=battle.Crossover.Boss;var hero=battle.Units[0];
            foreach(var signal in battle.Crossover.SignalPositions)MainTacticalConnect(battle,hero,signal);
            foreach(var device in battle.Units.Where(u=>u.Data.Id=="boundary-device"))device.Damage(99999,battle.Grid);
            battle.Crossover.Update();Assert.That(battle.Result,Is.EqualTo(BattleResult.Ongoing));
            for(int phase=2;phase<=3;phase++)
            {
                boss.Damage(99999,battle.Grid);Assert.That(boss.BossPhase,Is.EqualTo(phase));Assert.That(boss.Alive,Is.True);
                int hp=boss.CurrentHP;boss.Damage(99999,battle.Grid);Assert.That(boss.CurrentHP,Is.EqualTo(hp));
                Assert.That(battle.Result,Is.EqualTo(BattleResult.Ongoing));boss.BeginTurn();boss.Acted=true;boss.EndTurn();
            }
            boss.Damage(99999,battle.Grid);Assert.That(battle.Result,Is.EqualTo(BattleResult.Victory));
        }
    }
}
