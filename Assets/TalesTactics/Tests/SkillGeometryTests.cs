using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [Test] public void LineHitsMultipleEnemiesAndRejectsDiagonalAimWithoutSpending()
        {
            var skill = Skill(EffectKind.Damage); skill.Shape = SkillAreaShape.Line; skill.Range = 4;
            var second = new UnitRuntime(data, Team.Enemy, rules, 25); grid.Place(second, new Vector2Int(4, 1));
            resolver = new SkillResolver(grid, new[] { player, enemy, second }, rules);
            Assert.That(resolver.Targets(player, skill, enemy.Position).Count(), Is.EqualTo(2));
            int hp = second.CurrentHP, mp = player.CurrentMP;
            Assert.That(resolver.Execute(player, skill, new Vector2Int(3, 2), out _), Is.False);
            Assert.That(player.CurrentMP, Is.EqualTo(mp)); Assert.That(player.Acted, Is.False);
            Assert.That(resolver.Execute(player, skill, enemy.Position, out _), Is.True);
            Assert.That(second.CurrentHP < hp, Is.True);
        }

        [Test] public void ConeRotatesAndExcludesBehindAndOutsideManhattanRange()
        {
            var skill = Skill(EffectKind.Damage); skill.Shape = SkillAreaShape.Cone; skill.Range = 3;
            grid.Place(player, new Vector2Int(4, 3));
            var east = resolver.AreaTiles(player, skill, new Vector2Int(5, 3)).ToArray();
            Assert.That(east.Contains(new Vector2Int(6, 4)), Is.True);
            Assert.That(east.Contains(new Vector2Int(5, 4)), Is.False);
            Assert.That(east.Contains(new Vector2Int(3, 3)), Is.False);
            Assert.That(east.Contains(new Vector2Int(7, 4)), Is.False);
            var north = resolver.AreaTiles(player, skill, new Vector2Int(4, 4)).ToArray();
            Assert.That(north.Contains(new Vector2Int(5, 5)), Is.True);
        }

        [Test] public void SightBlocksCornersObstaclesAndHighTerrainSymmetrically()
        {
            var a = new Vector2Int(0, 0); var b = new Vector2Int(2, 2);
            Assert.That(SkillGeometry.HasLineOfSight(grid, a, b), Is.True);
            grid[new Vector2Int(1, 0)].Walkable = false;
            Assert.That(SkillGeometry.HasLineOfSight(grid, a, b), Is.False);
            Assert.That(SkillGeometry.HasLineOfSight(grid, b, a), Is.False);
            grid[new Vector2Int(1, 0)].Walkable = true;
            grid[new Vector2Int(1, 1)].Height = 3;
            Assert.That(SkillGeometry.HasLineOfSight(grid, a, b), Is.False);
            grid[new Vector2Int(1, 1)].Height = 0;
            Assert.That(SkillGeometry.HasLineOfSight(grid, a, b), Is.True);
        }

        [Test] public void HeightAndSightApplyToActualAreaAndDefaultsPreserveLegacy()
        {
            var skill = Skill(EffectKind.Damage); skill.Area = 2;
            grid[enemy.Position].Height = 4;
            Assert.That(resolver.InRange(player, skill, enemy.Position), Is.True);
            skill.MaxHeightDifference = 1;
            Assert.That(resolver.InRange(player, skill, enemy.Position), Is.False);
            Assert.That(resolver.Targets(player, skill, player.Position), Is.Empty);
            skill.MaxHeightDifference = -1; grid[enemy.Position].Height = 0;
            grid.Place(enemy, new Vector2Int(3, 1)); grid[new Vector2Int(2, 1)].Walkable = false;
            skill.RequiresLineOfSight = true;
            Assert.That(resolver.InRange(player, skill, enemy.Position), Is.False);
            Assert.That(resolver.Execute(player, skill, enemy.Position, out _), Is.False);
            Assert.That(player.Acted, Is.False);
        }
    }
}
