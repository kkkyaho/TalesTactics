using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    public sealed class GridTile
    {
        public Vector2Int Coordinate;
        public int Height, MovementCost=1;
        public bool Walkable=true;
        public TerrainType Terrain;
        public UnitRuntime Occupant;
        public Vector3 WorldPosition(float step) => new Vector3(Coordinate.x, Height*step, Coordinate.y);
    }
    public sealed class GridMap
    {
        public readonly Dictionary<Vector2Int,GridTile> Tiles=new Dictionary<Vector2Int,GridTile>();
        static readonly Vector2Int[] Directions={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
        public GridTile this[Vector2Int p] => Tiles.TryGetValue(p,out var t)?t:null;
        public static int Distance(Vector2Int a,Vector2Int b) => Mathf.Abs(a.x-b.x)+Mathf.Abs(a.y-b.y);
        public IEnumerable<GridTile> Neighbors(Vector2Int p) { foreach(var d in Directions) if(Tiles.TryGetValue(p+d,out var t)) yield return t; }
        public bool CanEnter(UnitRuntime u,GridTile from,GridTile to) => to!=null && to.Walkable && (to.Occupant==null || to.Occupant==u) && Mathf.Abs(from.Height-to.Height)<=u.Stats.JMP;
        // Walking may pass allies; landing and forced displacement still require a vacant tile.
        bool CanTraverse(UnitRuntime u,GridTile from,GridTile to) => to!=null && to.Walkable &&
            (to.Occupant==null || to.Occupant==u || to.Occupant.Team==u.Team) && Mathf.Abs(from.Height-to.Height)<=u.Stats.JMP;
        public Dictionary<Vector2Int,int> Reachable(UnitRuntime unit,out Dictionary<Vector2Int,Vector2Int> parents)
        {
            parents=new Dictionary<Vector2Int,Vector2Int>();
            var costs=new Dictionary<Vector2Int,int>{{unit.Position,0}};
            var open=new List<Vector2Int>{unit.Position};
            if(unit.Has(StatusKind.Root)||unit.Has(StatusKind.Cage)) return costs;
            while(open.Count>0)
            {
                open.Sort((a,b)=>costs[a].CompareTo(costs[b])); var p=open[0]; open.RemoveAt(0);
                foreach(var t in Neighbors(p))
                {
                    if(!CanTraverse(unit,this[p],t))continue;
                    int cost=costs[p]+Mathf.Max(1,t.MovementCost);
                    if(cost>unit.Stats.MOV || (costs.TryGetValue(t.Coordinate,out int old)&&cost>=old))continue;
                    costs[t.Coordinate]=cost; parents[t.Coordinate]=p;
                    if(!open.Contains(t.Coordinate))open.Add(t.Coordinate);
                }
            }
            foreach(var p in new List<Vector2Int>(costs.Keys))
                if(this[p].Occupant!=null&&this[p].Occupant!=unit)costs.Remove(p);
            return costs;
        }
        public List<Vector2Int> Path(UnitRuntime u,Vector2Int destination)
        {
            var range=Reachable(u,out var parents); var result=new List<Vector2Int>();
            if(!range.ContainsKey(destination))return result;
            var p=destination; result.Add(p);
            while(p!=u.Position){p=parents[p];result.Add(p);} result.Reverse();return result;
        }
        public bool Place(UnitRuntime u,Vector2Int p)
        {
            var t=this[p]; if(t==null||!t.Walkable||(t.Occupant!=null&&t.Occupant!=u))return false;
            if(this[u.Position]?.Occupant==u)this[u.Position].Occupant=null;
            u.Position=p; if(u.Alive)t.Occupant=u; return true;
        }
        public static GridMap TestStage()
        {
            var map=new GridMap();
            for(int x=0;x<10;x++)for(int z=0;z<9;z++)
            {
                int h=x>=6&&z>=4?2:x>=4&&z>=3?1:0;
                var p=new Vector2Int(x,z); var t=new GridTile{Coordinate=p,Height=h,Terrain=h>0?TerrainType.HighGround:TerrainType.Normal};
                if(x==4&&z<=2){t.Terrain=TerrainType.Water;t.MovementCost=2;}
                if((x==3&&z==4)||(x==5&&z==5)){t.Terrain=TerrainType.Obstacle;t.Walkable=false;}
                map.Tiles.Add(p,t);
            }
            return map;
        }
    }
}
