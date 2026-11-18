using System;
using UnityEngine;
namespace TalesTactics
{
    public enum TacticalMissionGoal { Boss, SignalsBoss, DevicesBoss, SignalsExit, DevicesSignalsExit, SplitFronts, Finale, Personal }

    // Authored gameplay data; existing save and content assets are not rewritten.
    public sealed class TacticalMissionLayout
    {
        public readonly int Stage;
        public readonly string Name, Location, Description, Hint;
        public readonly TacticalMissionGoal Goal;
        public readonly Vector2Int[] Signals, PlayerSpawns, EnemySpawns;
        public readonly Vector2Int Exit, Contract;
        readonly Func<GridMap> createMap;

        public TacticalMissionLayout(int stage,string name,string location,string description,string hint,
            TacticalMissionGoal goal,Vector2Int[] signals,Vector2Int exit,Vector2Int[] players,Vector2Int[] enemies,
            Func<GridMap> map,Vector2Int? contract=null)
        {
            if(signals==null||signals.Length>3||players==null||players.Length!=6||enemies==null||enemies.Length!=9||map==null)
                throw new ArgumentException("Invalid tactical layout data");
            Stage=stage;Name=name;Location=location;Description=description;Hint=hint;Goal=goal;
            Signals=(Vector2Int[])signals.Clone();Exit=exit;PlayerSpawns=(Vector2Int[])players.Clone();EnemySpawns=(Vector2Int[])enemies.Clone();
            createMap=map;Contract=contract??new Vector2Int(6,4);
        }
        public GridMap Map()=>createMap();

        // Deep water is impassable. Every route, actor and objective must be authored on a valid tile.
        public static GridMap Build(Func<int,int,int> height,Func<int,int,bool> wall,Func<int,int,bool> water=null)
        {
            var map=new GridMap();
            for(int x=0;x<13;x++)for(int y=0;y<13;y++)
            {
                var p=new Vector2Int(x,y);bool blocked=wall(x,y),deep=water!=null&&water(x,y);int h=height(x,y);
                map.Tiles.Add(p,new GridTile{Coordinate=p,Height=h,Walkable=!blocked&&!deep,
                    Terrain=deep?TerrainType.Water:blocked?TerrainType.Obstacle:h>0?TerrainType.HighGround:TerrainType.Normal,
                    MovementCost=deep?2:1});
            }
            return map;
        }
    }

    public static partial class CrossoverTacticalMissions
    {
        public const int CurrentRevision=1;
        public static bool Contains(int stage)=>CrossoverCampaign.IsMain(stage)&&stage>=CrossoverCampaign.FirstStage+3
            ||PersonalScenarios.Contains(stage)&&PersonalScenarios.Tier(stage)>0;
        public static bool Applies(int stage,int revision)=>Contains(stage)&&revision==CurrentRevision;
        public static TacticalMissionLayout Get(int stage)
        {
            if(!Contains(stage))throw new ArgumentOutOfRangeException(nameof(stage));
            return CrossoverCampaign.IsMain(stage)?MainTacticalMissions.Get(stage):PersonalTacticalMissions.Get(stage);
        }
    }
}
