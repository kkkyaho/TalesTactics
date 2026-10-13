using UnityEngine;
namespace TalesTactics
{
    // Versioned separately from the original four-enemy encounters for suspended battles.
    public static class CampaignEncounters
    {
        static readonly int[][] slots={
            new[]{0,1,2,3,1,2},
            new[]{0,1,2,3,1,3,2},
            new[]{0,1,2,3,1,3,2,1},
            new[]{0,1,2,3,3,2,1,3},
            new[]{0,1,2,3,1,0,2,3,1,3},
            new[]{0,1,2,3,3,2,1,3,2,3}
        };
        static readonly Vector2Int[][] positions={
            new[]{new Vector2Int(4,2),new Vector2Int(6,3),new Vector2Int(5,6),new Vector2Int(8,4),new Vector2Int(9,6),new Vector2Int(9,2)},
            new[]{new Vector2Int(6,9),new Vector2Int(4,5),new Vector2Int(7,5),new Vector2Int(6,7),new Vector2Int(2,5),new Vector2Int(9,6),new Vector2Int(5,8)},
            new[]{new Vector2Int(5,2),new Vector2Int(5,7),new Vector2Int(8,2),new Vector2Int(8,7),new Vector2Int(10,3),new Vector2Int(10,6),new Vector2Int(12,2),new Vector2Int(12,6)},
            new[]{new Vector2Int(3,4),new Vector2Int(8,4),new Vector2Int(5,6),new Vector2Int(7,7),new Vector2Int(2,8),new Vector2Int(9,8),new Vector2Int(4,10),new Vector2Int(8,10)},
            new[]{new Vector2Int(4,2),new Vector2Int(4,7),new Vector2Int(7,2),new Vector2Int(7,7),new Vector2Int(10,2),new Vector2Int(10,7),new Vector2Int(12,4),new Vector2Int(12,6),new Vector2Int(13,2),new Vector2Int(13,7)},
            new[]{new Vector2Int(6,12),new Vector2Int(3,5),new Vector2Int(9,5),new Vector2Int(5,7),new Vector2Int(7,7),new Vector2Int(2,8),new Vector2Int(10,8),new Vector2Int(4,10),new Vector2Int(8,10),new Vector2Int(6,10)}
        };
        public static int Total(int stage){CampaignStages.Get(stage);return stage>=CampaignStages.LegacyCount?9:slots[stage].Length;}
        public static bool HasWave(int stage,bool teamTurns)=>teamTurns&&(stage==2||stage==4);
        public static int Initial(int stage,bool teamTurns)=>Total(stage)-(HasWave(stage,teamTurns)?2:0);
        public static CharacterData Enemy(BattleCatalog catalog,int stage,int index)=>stage>=CampaignStages.LegacyCount?CrossoverCampaign.Enemy(catalog,stage,index):CampaignEnemies.Resolve(catalog,stage,slots[stage][index]);
        public static Vector2Int Position(int stage,int index)=>stage>=CampaignStages.LegacyCount?CrossoverCampaign.EnemySpawn(index):positions[stage][index];
    }
}
