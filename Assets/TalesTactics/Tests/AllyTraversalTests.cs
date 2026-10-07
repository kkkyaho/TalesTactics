using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        GridMap Corridor(UnitRuntime mover,UnitRuntime blocker)
        {
            var map=new GridMap();
            for(int x=0;x<5;x++){var p=new Vector2Int(x,0);map.Tiles.Add(p,new GridTile{Coordinate=p});}
            map.Place(mover,Vector2Int.zero);map.Place(blocker,Vector2Int.right);return map;
        }
        [TestCase(Team.Player)] [TestCase(Team.Enemy)]
        public void TeammatesCanBeTraversedButCannotBeDestinations(Team team)
        {
            var mover=new UnitRuntime(data,team,rules);var ally=new UnitRuntime(data,team,rules);var map=Corridor(mover,ally);
            var destination=new Vector2Int(3,0);var reachable=map.Reachable(mover,out _);
            Assert.That(reachable.ContainsKey(ally.Position),Is.False);
            Assert.That(map.Path(mover,destination),Is.EqualTo(new[]{Vector2Int.zero,Vector2Int.right,new Vector2Int(2,0),destination}));
            Assert.That(map.Path(mover,ally.Position),Is.Empty);
            Assert.That(map.CanEnter(mover,map[mover.Position],map[ally.Position]),Is.False,"Forced displacement must still stop at an occupied tile.");
            Assert.That(map.Place(mover,ally.Position),Is.False);Assert.That(map[ally.Position].Occupant,Is.SameAs(ally));
        }
        [TestCase(Team.Player)] [TestCase(Team.Enemy)]
        public void OpponentsStillBlockTheWholeCorridor(Team team)
        {
            var mover=new UnitRuntime(data,team,rules);var blocker=new UnitRuntime(data,team==Team.Player?Team.Enemy:Team.Player,rules);
            var map=Corridor(mover,blocker);Assert.That(map.Reachable(mover,out _).Count,Is.EqualTo(1));Assert.That(map.Path(mover,new Vector2Int(2,0)),Is.Empty);
        }
        [Test] public void AllyTraversalStillPaysTerrainCostAndRespectsJumpAndWalls()
        {
            var ally=new UnitRuntime(data,Team.Player,rules);var map=Corridor(player,ally);map[ally.Position].MovementCost=2;
            var costs=map.Reachable(player,out _);Assert.That(costs[new Vector2Int(3,0)],Is.EqualTo(4));Assert.That(costs.ContainsKey(new Vector2Int(4,0)),Is.False);
            map[ally.Position].Height=player.Stats.JMP+1;Assert.That(map.Path(player,new Vector2Int(2,0)),Is.Empty);
            map[ally.Position].Height=0;map[ally.Position].Walkable=false;Assert.That(map.Path(player,new Vector2Int(2,0)),Is.Empty);
        }
        [TestCase(StatusKind.Root)] [TestCase(StatusKind.Cage)]
        public void ImmobilizedUnitsCannotPassAllies(StatusKind status)
        {
            var map=Corridor(player,new UnitRuntime(data,Team.Player,rules));player.AddStatus(status,1);
            Assert.That(map.Reachable(player,out _).Count,Is.EqualTo(1));Assert.That(map.Path(player,new Vector2Int(2,0)),Is.Empty);
        }
    }
}
