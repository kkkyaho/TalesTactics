using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace TalesTactics.Tests
{
    [Category("AllStageTactics")]
    public sealed class PersonalTacticalMissionTests
    {
        public static int[] CombatStages=>Enumerable.Range(PersonalScenarios.FirstStage,PersonalScenarios.Count)
            .Where(s=>PersonalScenarios.Tier(s)>0).ToArray();
        public static int[] TrialStages=>CombatStages.Where(s=>PersonalScenarios.Tier(s)==2).ToArray();

        // Deliberately ignores combat occupancy: this checks authored routes for a JMP1 unit,
        // not whether an automated policy or human can win the battle.
        static HashSet<Vector2Int> Reach(GridMap map,Vector2Int start,Vector2Int? excluded=null)
        {
            var reached=new HashSet<Vector2Int>();var pending=new Queue<Vector2Int>();
            if(start==excluded||map[start]?.Walkable!=true)return reached;
            reached.Add(start);pending.Enqueue(start);
            while(pending.Count>0)
            {
                var from=pending.Dequeue();
                foreach(var to in map.Neighbors(from))
                    if(to.Coordinate!=excluded&&to.Walkable&&Math.Abs(to.Height-map[from].Height)<=1&&reached.Add(to.Coordinate))pending.Enqueue(to.Coordinate);
            }
            return reached;
        }
        static IEnumerable<Vector2Int> Markers(TacticalMissionLayout layout)
            =>layout.Signals.Concat(new[]{layout.Exit,layout.Contract});

        [TestCaseSource(nameof(CombatStages))]
        public void EachPersonalCombatLayoutHasValidDistinctSlotsAndJumpOneRoutes(int stage)
        {
            var layout=PersonalTacticalMissions.Get(stage);var map=layout.Map();
            Assert.That(layout.Stage,Is.EqualTo(stage));Assert.That(layout.Goal,Is.EqualTo(TacticalMissionGoal.Personal));
            Assert.That(layout.Name,Is.EqualTo(PersonalScenarios.Title(stage)));
            Assert.That(layout.Location,Is.Not.Empty);Assert.That(layout.Description,Is.Not.Empty);Assert.That(layout.Hint,Is.Not.Empty);
            Assert.That(map.Tiles.Count,Is.EqualTo(169));
            Assert.That(layout.PlayerSpawns.Length,Is.EqualTo(6));Assert.That(layout.EnemySpawns.Length,Is.EqualTo(9));
            Assert.That(layout.Signals.Length,Is.EqualTo(3));Assert.That(layout.Signals.Select(p=>p.x).First(),Is.LessThan(layout.Signals[1].x));
            Assert.That(layout.Contract,Is.EqualTo(new Vector2Int(6,4)));
            var actors=layout.PlayerSpawns.Concat(layout.EnemySpawns).ToArray();var markers=Markers(layout).ToArray();
            Assert.That(actors.Distinct().Count(),Is.EqualTo(15),"Inactive summon slots also need their own valid tile");
            Assert.That(markers.Distinct().Count(),Is.EqualTo(5));
            Assert.That(actors.Intersect(markers),Is.Empty,"An actor must not initially occupy a signal, exit or contract");
            foreach(var tile in map.Tiles.Values)
            {
                Assert.That(tile.Coordinate.x,Is.InRange(0,12));Assert.That(tile.Coordinate.y,Is.InRange(0,12));
                Assert.That(tile.Height,Is.InRange(0,2));Assert.That(tile.MovementCost,Is.GreaterThanOrEqualTo(1));
                if(tile.Terrain==TerrainType.Water)Assert.That(tile.Walkable,Is.False,"Deep water is not an alternate shortcut");
            }
            var reached=Reach(map,layout.PlayerSpawns[0]);
            foreach(var p in actors.Concat(markers))
            {
                Assert.That(map[p]?.Walkable,Is.True,stage+" blocked authored position "+p);
                Assert.That(reached,Does.Contain(p),stage+" unreachable with JMP1: "+p);
            }
            Assert.That(reached.Count,Is.EqualTo(map.Tiles.Values.Count(t=>t.Walkable)),"No isolated decorative standing pockets");
            foreach(var marker in markers)
            {
                var around=Reach(map,layout.PlayerSpawns[0],marker);
                int approaches=map.Neighbors(marker).Count(t=>t.Walkable&&Math.Abs(t.Height-map[marker].Height)<=1&&around.Contains(t.Coordinate));
                Assert.That(approaches,Is.GreaterThanOrEqualTo(2),stage+" objective has only one approach: "+marker);
            }
            if(PersonalScenarios.Tier(stage)==1)
            {
                // The chosen route must be reachable before the other route is opened.
                for(int choice=0;choice<2;choice++)
                    Assert.That(Reach(map,layout.PlayerSpawns[0],layout.Signals[1-choice]),Does.Contain(layout.Signals[choice]),"Forced to cross the other story choice first");
                Assert.That(layout.Description,Does.Contain(PersonalScenarios.RouteName(stage,0)));
                Assert.That(layout.Description,Does.Contain(PersonalScenarios.RouteName(stage,1)));
                Assert.That(layout.Description,Does.Contain("격파 불필요"));
            }
            var untouched=layout.Map();var sample=layout.PlayerSpawns[0];map[sample].Height=99;map[sample].Walkable=false;
            Assert.That(untouched[sample].Height,Is.LessThan(3));Assert.That(untouched[sample].Walkable,Is.True,"Separate sessions cannot share mutable tiles");
        }

        static string CanonicalTopology(GridMap map)
        {
            var alternatives=new List<string>();
            for(int reflected=0;reflected<2;reflected++)for(int rotation=0;rotation<4;rotation++)
            {
                var text=new StringBuilder();
                for(int y=0;y<13;y++)for(int x=0;x<13;x++)
                {
                    int a=reflected==0?x:12-x,b=y;
                    for(int r=0;r<rotation;r++){int next=12-b;b=a;a=next;}
                    var tile=map[new Vector2Int(a,b)];
                    text.Append(tile.Walkable?'W':'X').Append(tile.Height).Append((int)tile.Terrain).Append(';');
                }
                alternatives.Add(text.ToString());
            }
            return alternatives.OrderBy(s=>s,StringComparer.Ordinal).First();
        }
        [Test] public void TwentyPersonalMapsHaveDifferentTopologyEvenAfterRotationOrReflection()
        {
            Assert.That(CombatStages.Length,Is.EqualTo(20));
            var fingerprints=new Dictionary<string,int>();
            foreach(var stage in CombatStages)
            {
                string fingerprint=CanonicalTopology(PersonalTacticalMissions.Get(stage).Map());
                Assert.That(fingerprints.ContainsKey(fingerprint),Is.False,"Same geometry as another personal combat stage: "+stage);
                fingerprints.Add(fingerprint,stage);
            }
        }
        [Test] public void TenDialogueChaptersAndNonPersonalStagesAreExcluded()
        {
            foreach(int stage in Enumerable.Range(PersonalScenarios.FirstStage,PersonalScenarios.Count).Where(s=>PersonalScenarios.Tier(s)==0).Concat(new[]{-1,6,25,56}))
                Assert.Throws<ArgumentOutOfRangeException>(()=>PersonalTacticalMissions.Get(stage),"Only personal combat maps belong in this catalog: "+stage);
        }

        [TestCaseSource(nameof(TrialStages))]
        public void TrialSessionUsesAuthoredPositionsAndKeepsItsOwnerBossAndUnfinishedGoals(int stage)
        {
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");Assert.That(catalog,Is.Not.Null);
            string owner=PersonalScenarios.Arc(stage).Owner,support=owner=="mint"?"cless":"mint";
            int ownerIndex=Array.FindIndex(catalog.Characters,c=>c.Id==owner),supportIndex=Array.FindIndex(catalog.Characters,c=>c.Id==support);
            Assert.That(ownerIndex,Is.GreaterThanOrEqualTo(0));Assert.That(supportIndex,Is.GreaterThanOrEqualTo(0));
            var layout=PersonalTacticalMissions.Get(stage);
            var battle=new BattleSession(catalog,new[]{ownerIndex,supportIndex},35,25,stage,teamTurns:true,actionExperience:false);
            Assert.That(battle.Units[0].Data.Id,Is.EqualTo(owner));Assert.That(battle.Units[0].Alive,Is.True);
            Assert.That(battle.Crossover.Boss.Data.Id,Is.EqualTo(CampaignEnemies.Id(stage,0)));
            Assert.That(battle.Crossover.SignalPositions,Is.EqualTo(layout.Signals));Assert.That(battle.Crossover.ExitPosition,Is.EqualTo(layout.Exit));
            Assert.That(battle.Units.Where(u=>u.Team==Team.Player).Select(u=>u.Position),Is.EqualTo(layout.PlayerSpawns.Take(2)));
            Assert.That(battle.Units.Where(u=>u.Team==Team.Enemy).Select(u=>u.Position),Is.EqualTo(layout.EnemySpawns));
            Assert.That(battle.Crossover.State.TrialActions,Is.Zero);Assert.That(battle.Crossover.State.TrialProgress,Is.Zero);
            Assert.That(battle.Crossover.State.Nodes,Is.Zero);Assert.That(battle.Result,Is.EqualTo(BattleResult.Ongoing));
            var reached=Reach(battle.Grid,battle.Units[0].Position);
            foreach(var p in Markers(layout).Concat(layout.EnemySpawns))Assert.That(reached,Does.Contain(p),owner+" cannot reach trial target with JMP1: "+p);
            Assert.That(battle.Crossover.UsesContract,Is.EqualTo(owner=="cless"));
            Assert.That(battle.Crossover.UsesExit,Is.EqualTo(owner=="mint"||owner=="natalia"||owner=="shionne"));
            Assert.That(battle.Crossover.UsesNodes,Is.EqualTo(owner!="jade"&&owner!="velvet"&&owner!="alphen"));
            Assert.That(PersonalScenarios.Chapter(stage).Equipment,Is.EqualTo(PersonalScenarios.Arc(stage).WeaponId));
        }
    }
}
