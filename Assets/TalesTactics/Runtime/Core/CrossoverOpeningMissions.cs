using System;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    // Revision zero is the shipped opening, retained by old checkpoints and retries.
    // Only fresh main episodes 1-3 opt into revision one; personal maps keep revision zero.
    public static class CrossoverOpeningMissions
    {
        public const int CurrentRevision=1;
        public static bool Contains(int stage)=>CrossoverCampaign.IsMain(stage)&&stage<CrossoverCampaign.FirstStage+3;
        public static bool Applies(int stage,int revision)=>Contains(stage)&&revision==CurrentRevision;
        public static Vector2Int[] Signals(int stage,int revision=CurrentRevision)
        {
            if(!Applies(stage,revision))return CrossoverBattle.Nodes;
            switch(stage-CrossoverCampaign.FirstStage)
            {
                case 0:return new[]{new Vector2Int(6,6)};
                case 1:return Array.Empty<Vector2Int>();
                default:return new[]{new Vector2Int(1,8),new Vector2Int(11,8),new Vector2Int(6,11)};
            }
        }
        public static Vector2Int Exit(int stage,int revision=CurrentRevision)
            =>Applies(stage,revision)&&stage==CrossoverCampaign.FirstStage+1?new Vector2Int(6,1):CrossoverBattle.Exit;
        public static string Description(int episode)
        {
            switch(episode)
            {
                case 1:return "중앙 다리 너머 구조 신호 1개 연결 · 수호자 진정 · 깊은 물은 통과 불가";
                case 2:return "서쪽·동쪽 감옥 장치 2개 파괴 후 남쪽 퇴로에 아군 도착 · 비에조 격파 불필요";
                default:return "세 다리 너머 구조 신호 3개 연결 후 북쪽 출구 도착 · 히아데스 격파 불필요 · 깊은 물은 통과 불가";
            }
        }
        public static string Hint(int episode)
        {
            switch(episode)
            {
                case 1:return "깊은 물은 통과 불가 · 중앙 돌다리로 전진 · 다리 너머 (6,6) 구조 신호 · 강타를 피한 뒤 수호자의 빈틈 공략";
                case 2:return "좌우 감옥 통로로 진입 · 장치 (2,8)/(10,8) 파괴 · 남쪽 (6,1)로 복귀 · 우측 고지는 원거리 지원";
                default:return "깊은 물은 통과 불가 · 서쪽·중앙·동쪽 다리를 선택 · 신호 (1,8)/(11,8)/(6,11) 연결 · 북쪽 (6,12) 탈출";
            }
        }
        public static GridMap Map(int stage)
        {
            int episode=stage-CrossoverCampaign.FirstStage+1;var map=new GridMap();
            for(int x=0;x<13;x++)for(int y=0;y<13;y++)
            {
                bool obstacle=false,water=false;int height=0;
                switch(episode)
                {
                    case 1:
                        water=y==5&&(x<5||x>7);obstacle=(x==3||x==9)&&y==8;height=y>=9?1:0;
                        break;
                    case 2:
                        obstacle=y==5&&x>=1&&x<=11&&x!=2&&x!=3&&x!=9&&x!=10;
                        height=x>=8&&y>=6?1:0;
                        break;
                    case 3:
                        water=(y==5||y==6)&&x!=2&&x!=6&&x!=10;
                        obstacle=(x==4||x==8)&&y>=8&&y<=10;height=y>=9?1:0;
                        break;
                    default:throw new ArgumentOutOfRangeException(nameof(stage));
                }
                var position=new Vector2Int(x,y);
                map.Tiles.Add(position,new GridTile{Coordinate=position,Height=height,Walkable=!obstacle&&!water,
                    Terrain=water?TerrainType.Water:obstacle?TerrainType.Obstacle:height>0?TerrainType.HighGround:TerrainType.Normal,MovementCost=water?2:1});
            }
            return map;
        }
        public static Vector2Int PlayerPosition(int episode,int index)=>episode==2
            ?new[]{new Vector2Int(4,1),new Vector2Int(4,2),new Vector2Int(8,1),new Vector2Int(8,2),new Vector2Int(3,1),new Vector2Int(9,1)}[index]:episode==3
            ?new Vector2Int(new[]{2,6,10}[index%3],1+index/3):new Vector2Int(4+index%3,1+index/3);
        public static Vector2Int EnemyPosition(int episode,int index)
        {
            Vector2Int[] positions=episode==1?new[]{new Vector2Int(6,10),new Vector2Int(6,7),new Vector2Int(9,9),new Vector2Int(3,9),new Vector2Int(2,10),new Vector2Int(10,10),new Vector2Int(4,11),new Vector2Int(8,11),new Vector2Int(6,12)}:
                episode==2?new[]{new Vector2Int(6,11),new Vector2Int(3,6),new Vector2Int(10,9),new Vector2Int(2,9),new Vector2Int(2,8),new Vector2Int(10,8),new Vector2Int(4,11),new Vector2Int(8,11),new Vector2Int(6,10)}:
                new[]{new Vector2Int(6,9),new Vector2Int(2,7),new Vector2Int(9,10),new Vector2Int(10,7),new Vector2Int(1,10),new Vector2Int(11,10),new Vector2Int(4,11),new Vector2Int(8,11),new Vector2Int(12,12)};
            return positions[index];
        }
    }
    public sealed partial class CrossoverBattle
    {
        public bool RevisedOpening=>CrossoverOpeningMissions.Applies(battle.CampaignStage,State.OpeningRevision);
        public Vector2Int[] SignalPositions=>RevisedTactics?TacticalLayout.Signals:CrossoverOpeningMissions.Signals(battle.CampaignStage,State.OpeningRevision);
        public Vector2Int ExitPosition=>RevisedTactics?TacticalLayout.Exit:CrossoverOpeningMissions.Exit(battle.CampaignStage,State.OpeningRevision);
        public int RequiredNodeMask=>UsesNodes?(1<<SignalPositions.Length)-1:0;
        public bool AllSignalsConnected=>(State.Nodes&RequiredNodeMask)==RequiredNodeMask;
        public UnitRuntime[] PrisonDevices=>RevisedOpening&&episode==2?new[]{Enemies[4],Enemies[5]}:Array.Empty<UnitRuntime>();
        public bool PrisonRouteOpen=>PrisonDevices.Length==2&&PrisonDevices.All(u=>!u.Alive);
        void ConfigureOpening()
        {
            if(!CrossoverOpeningMissions.Contains(battle.CampaignStage))return;
            State.OpeningRevision=CrossoverOpeningMissions.CurrentRevision;
            foreach(var tile in battle.Grid.Tiles.Values)tile.Occupant=null;
            var party=battle.Units.Where(u=>u.Team==Team.Player).ToArray();
            for(int i=0;i<party.Length;i++)battle.Grid.Place(party[i],CrossoverOpeningMissions.PlayerPosition(episode,i));
            var enemies=Enemies;
            for(int i=0;i<enemies.Length;i++)
            {
                var position=CrossoverOpeningMissions.EnemyPosition(episode,i);
                if(enemies[i].Alive)battle.Grid.Place(enemies[i],position);else enemies[i].Position=position;
            }
        }
        // Called before saved units are placed: the new opening may close an old tile.
        public void PrepareRestore(CrossoverBattleState saved)
        {
            if(saved==null||saved.OpeningRevision<0||saved.OpeningRevision>CrossoverOpeningMissions.CurrentRevision||
                saved.OpeningRevision!=0&&!CrossoverOpeningMissions.Contains(battle.CampaignStage)||saved.TacticsRevision<0||saved.TacticsRevision>CrossoverTacticalMissions.CurrentRevision||saved.TacticsRevision!=0&&!CrossoverTacticalMissions.Contains(battle.CampaignStage))
                throw new System.IO.InvalidDataException("Invalid opening encounter revision");
            if(!CrossoverOpeningMissions.Contains(battle.CampaignStage)&&!CrossoverTacticalMissions.Contains(battle.CampaignStage))return;
            var map=CampaignContent.Map(battle.CampaignStage,saved.OpeningRevision,saved.TacticsRevision);
            foreach(var pair in map.Tiles)
            {
                var tile=battle.Grid[pair.Key];var original=pair.Value;
                tile.Height=original.Height;tile.Terrain=original.Terrain;tile.Walkable=original.Walkable;tile.MovementCost=original.MovementCost;
            }
        }
        bool OpeningVictory=>episode==1?AllSignalsConnected&&!Boss.Alive:episode==2?PrisonRouteOpen&&AtExit:AllSignalsConnected&&AtExit;
    }
}
