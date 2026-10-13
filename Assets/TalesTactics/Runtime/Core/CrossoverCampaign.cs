using System;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed class CrossoverEpisode
    {
        public readonly int Number;
        public readonly string Title,Boss,SecondBoss,Briefing;
        public readonly StoryLine[] Before,After;
        public CrossoverEpisode(int number,string title,string boss,string secondBoss,string briefing,StoryLine[] before,StoryLine[] after)
        {Number=number;Title=title;Boss=boss=="guardian"?"golem":boss;SecondBoss=secondBoss;Briefing=briefing;Before=before;After=after;}
    }
    public static partial class CrossoverCampaign
    {
        // Original six stage indexes remain intact for old checkpoints and replays.
        public const int FirstStage=6,EpisodeCount=20,LastStage=25;
        public static bool IsMain(int stage)=>stage>=FirstStage&&stage<=LastStage;
        public static CrossoverEpisode Episode(int stage)=>IsMain(stage)?Episodes[stage-FirstStage]:throw new ArgumentOutOfRangeException(nameof(stage));
        public static string Flag(int number)=>"crossover.chapter"+number;
        public static bool Joined(string id,int number)
        {
            switch(id)
            {
                case "cless":case "mint":return true;
                case "alphen":case "shionne":return number>=2;
                case "farah":return number>=3;
                case "tear":case "jade":case "natalia":return number>=4;
                case "velvet":case "kisara":return number>=5;
                default:return false;
            }
        }
        public static GridMap Map(int stage)
        {
            int n=Episode(stage).Number;var map=new GridMap();
            for(int x=0;x<13;x++)for(int z=0;z<13;z++)
            {
                int height=z>=10?2:z>=7?1:0;var p=new Vector2Int(x,z);bool pillar=false,water=false;
                switch(n)
                {
                    case 1: pillar=(x==3||x==9)&&(z==5||z==9);break; // Ruined gate: two flanks.
                    case 2: height=z>=8?1:0;pillar=z==5&&x>=3&&x<=9&&x!=5&&x!=6;break; // Prison gate.
                    case 3: height=z>=9?1:0;water=z==6&&x!=2&&x!=6&&x!=10;pillar=(x==0||x==12)&&z%3==0;break;
                    case 4: pillar=(x==4||x==8)&&z>=4&&z<=9&&z!=6;break; // Archive aisles.
                    case 5: height=x>=8?1:0;pillar=(x==3||x==7)&&z>=5&&z<=9&&z!=7;break;
                    case 6: height=z>=9?1:0;pillar=z==5&&(x==3||x==4||x==8||x==9);break; // Extraction rooms.
                    case 7: height=x<=3||x>=9?1:0;water=x==6&&z>=4&&z<=10&&z!=7;break;
                    case 8: pillar=(x==4||x==8)&&(z==5||z==8);height=z>=8?1:0;break; // Clone chamber.
                    case 9: pillar=(z==4||z==8)&&x>=3&&x<=9&&x!=6;break; // Escape switches.
                    case 10: height=z>=6?1:0;pillar=(x==1||x==11)&&(z==5||z==9);break;
                    case 11: height=z>=8?1:0;pillar=x==6&&z>=3&&z<=11;break; // Separated fronts.
                    case 12: height=x>=5&&x<=7&&z>=5?1:0;pillar=(x==3||x==9)&&(z==4||z==8);break;
                    case 13: height=x>=5&&x<=7?1:0;pillar=z==5&&(x<=3||x>=9);break; // Two evacuation approaches.
                    case 14: height=z>=9?2:z>=5?1:0;pillar=(x==2||x==10)&&z==5;break;
                    case 15: height=x<4?1:x>8?1:0;pillar=(x==4||x==8)&&z>=5&&z<=9&&z!=7;break;
                    case 16: height=x==2||x==6||x==10?1:0;water=z>=5&&z<=8&&height==0;pillar=(x==0||x==12)&&z==10;break;
                    case 17: height=z>=9?1:0;water=(x==4||x==8)&&z>=4&&z<=10&&z!=7;break;
                    case 18: height=z>=8?1:0;pillar=(x==4||x==8)&&(z==4||z==6||z==9);break;
                    case 19: height=z>=10?2:z>=5?1:0;pillar=z==8&&x!=2&&x!=6&&x!=10;break;
                    case 20: height=Math.Abs(x-6)+Math.Abs(z-8)<=3?1:0;pillar=(x==2||x==10)&&(z==4||z==9);break;
                }
                // Authored actors and mission markers always have a valid standing tile.
                if(Enumerable.Range(0,9).Any(i=>EnemySpawn(i)==p)||Enumerable.Range(0,6).Any(i=>PlayerSpawn(i)==p)||CrossoverBattle.Nodes.Contains(p)||p==CrossoverBattle.Exit||p==CrossoverBattle.ContractSeal)pillar=false;
                if(n==11&&(x<=2||x>=9)&&z<=2){pillar=false;height=0;}
                var tile=new GridTile{Coordinate=p,Height=height,Walkable=!pillar,Terrain=pillar?TerrainType.Obstacle:height>0?TerrainType.HighGround:TerrainType.Normal};
                if(water&&!pillar){tile.Terrain=TerrainType.Water;tile.MovementCost=2;}
                map.Tiles.Add(p,tile);
            }
            return map;
        }
        public static Vector2Int PlayerSpawn(int index)=>new Vector2Int(4+index%3,1+index/3);
        public static int TerrainTheme(int stage)=>new[]{0,2,4,3,1,0,4,3,1,5,2,3,4,5,1,2,4,1,5,5}[(stage-FirstStage)%20];
        public static string TerrainHint(int stage)
        {
            if(PersonalScenarios.Contains(stage))stage=FirstStage+(stage-PersonalScenarios.FirstStage)%EpisodeCount;
            return new[]{"양쪽 기둥 사이로 진입 · 수호자의 강타 뒤 핵을 공략", "감옥 벽의 두 입구를 나눠 사용 · 도끼 예고에서 이탈", "세 다리로 마을을 연결 · 물 위 이동 비용 2", "문서고 통로와 가로 샛길 · 정면 반격을 피하고 측면 진입", "동쪽 고지와 두 긴 벽 · 호위와 지휘관을 분리", "착취실 양쪽을 열고 연결 장치부터 정지", "강 양쪽 고지의 주민 신호를 연결", "분신 소환진을 점령하거나 분신부터 제거", "중앙 문과 좌우 우회로 · 봉인주를 파괴하고 북쪽 출구로", "넓은 제단에서 레이저와 충격파를 분산 회피", "서쪽 부대가 자미르를 저지한 뒤 동쪽 부대가 리그렛과 교전", "중앙 제단의 고지와 양쪽 복제 장치", "선택한 피난로부터 연결 · 중앙 고지의 결투에 대비", "세 높이의 계단을 이용해 직선 공격을 분산", "양쪽 고지를 연결하는 세 길 · 집행자의 방어 뒤를 공략", "세 개의 높은 제방으로 피난 행렬을 보호 · 물 위 이동 비용 2", "두 수로의 중앙 교량 · 파도는 높이 0만 공격", "환영 통로 사이 구조 신호를 연결 · 신호 주변은 보호 구역", "북쪽 방벽의 세 문으로 진입 · 봉인망을 먼저 파괴", "중앙 경계핵과 세 귀환 신호 · 봉쇄 사이 안전길을 유지"}[stage-FirstStage];
        }
        public static Vector2Int EnemySpawn(int index)=>new[]{new Vector2Int(6,11),new Vector2Int(3,7),new Vector2Int(9,7),new Vector2Int(6,8),new Vector2Int(2,10),new Vector2Int(10,10),new Vector2Int(4,11),new Vector2Int(8,11),new Vector2Int(6,10)}[index];
        public static CharacterData Enemy(BattleCatalog catalog,int stage,int index)
        {
            string id=index==0?CampaignEnemies.Id(stage,0):index==4&&IsMain(stage)&&Episode(stage).Number==11?"legretta":index==4||index==5||index==8?"boundary-device":index==2?"mage":index==3?"archer":"sentinel";
            if(CampaignEnemies.Id(stage,0)=="ganabelt"&&(index==3||index==6||index==7))id="ganabelt";
            if(catalog.Enemies==null||catalog.Enemies.Length==0)return catalog.Enemy;
            return Boss(catalog,id,id=="hyades"&&IsMain(stage)&&Episode(stage).Number==3);
        }
        public static CharacterData Boss(BattleCatalog catalog,string id,bool early=false)
            =>(catalog.CrossoverVariants??Array.Empty<CharacterData>()).FirstOrDefault(c=>c!=null&&c.Id==id&&c.CrossoverEarlyForm==early)
                ??catalog.Enemies.FirstOrDefault(c=>c!=null&&c.Id==id)??throw new InvalidOperationException("Missing crossover enemy: "+id);
        public static CampaignStages.Chapter Chapter(int stage)
        {
            var e=Episode(stage);int level=1+(e.Number-1)*2;
            return new CampaignStages.Chapter(e.Number+"화 · "+e.Title,level,Math.Max(1,level-2),35,120+e.Number*50,"guardian-medal","수호의 메달",BossTimeStop.MajorId(e.Boss));
        }
    }
}
