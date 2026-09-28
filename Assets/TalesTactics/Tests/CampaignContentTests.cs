using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TalesTactics.Tests
{
    public partial class BattleRuleTests
    {
        [Test] public void CampaignMapsHaveDistinctTerrainAndConnectedSpawnPoints()
        {
            Assert.That(CampaignContent.Map(0).Tiles.Count, Is.EqualTo(99));
            Assert.That(CampaignContent.Map(1).Tiles.Count, Is.EqualTo(120));
            Assert.That(CampaignContent.Map(2).Tiles.Count, Is.EqualTo(130));
            for (int stage = 0; stage < CampaignStages.Count; stage++)
            {
                var map = CampaignContent.Map(stage);
                var spawns = Enumerable.Range(0, 6).Select(i => CampaignContent.PlayerSpawn(stage, i))
                    .Concat(Enumerable.Range(0, 4).Select(i => CampaignContent.EnemySpawn(stage, i))).ToArray();
                Assert.That(spawns.Distinct().Count(), Is.EqualTo(10));
                var seen = new HashSet<Vector2Int>(); var queue = new Queue<Vector2Int>();
                queue.Enqueue(spawns[0]); seen.Add(spawns[0]);
                while (queue.Count > 0)
                {
                    var p = queue.Dequeue();
                    foreach (var tile in map.Neighbors(p))
                        if (tile.Walkable && Mathf.Abs(tile.Height - map[p].Height) <= 1 && seen.Add(tile.Coordinate)) queue.Enqueue(tile.Coordinate);
                }
                foreach (var p in spawns) { Assert.That(map[p].Walkable, Is.True); Assert.That(seen.Contains(p), Is.True); }
                Assert.That(seen.Count, Is.EqualTo(map.Tiles.Values.Count(t => t.Walkable)));
            }
        }

        [Test] public void EveryChapterHasCompleteBeforeAndAfterDialogue()
        {
            for (int stage = 0; stage < CampaignStages.Count; stage++)
                foreach (bool after in new[] { false, true })
                {
                    var lines = CampaignContent.Story(stage, after);
                    Assert.That(lines.Length >= 4, Is.True);
                    Assert.That(lines.All(l => !string.IsNullOrWhiteSpace(l.Speaker) && !string.IsNullOrWhiteSpace(l.Text)), Is.True);
                }
        }
    }
}
