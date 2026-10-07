using System;
using UnityEngine;

namespace TalesTactics
{
    public static partial class CampaignContent
    {
        static string ExpansionLocation(int stage) => stage == 3 ? "산정 관측소 · 별빛 회랑" : stage == 4 ? "저수 시설 · 두 갈래 수로" : "중계탑 · 새벽의 핵실";
        static string ExpansionBriefing(int stage)
        {
            var chapter=CampaignStages.Get(stage);
            string route=stage==3?"기둥 사이 회랑을 지나 북쪽 관측단으로 올라가세요.":stage==4?"남북의 교량으로 두 수로를 건너세요. 물길에서는 이동 비용이 늘어납니다.":"양쪽 통로로 중앙 단상에 접근하세요. 기둥 뒤에서는 사격 시야를 확인하세요.";
            return route+"\n목표: "+CampaignMissions.Description(stage)+". 적 Lv"+chapter.EnemyLevel+" / 최초 "+chapter.FirstGold+"G·EXP"+chapter.FirstEXP+" / "+chapter.EquipmentName+".";
        }
        static GridMap ExpansionMap(int stage)
        {
            CampaignStages.Get(stage);
            int width=stage==3?12:stage==4?14:13,depth=stage==3?12:stage==4?10:13;
            var map=new GridMap();
            for(int x=0;x<width;x++)for(int z=0;z<depth;z++)
            {
                int ring=Math.Max(Math.Abs(x-6),Math.Abs(z-6));
                int height=stage==3?(z>=8?2:z>=5?1:0):stage==4?(x>=11?1:0):(ring<=2?2:ring<=4?1:0);
                var p=new Vector2Int(x,z);
                var tile=new GridTile{Coordinate=p,Height=height,Terrain=height>0?TerrainType.HighGround:TerrainType.Normal};
                if(stage==4&&(x==5||x==8)&&z!=2&&z!=7){tile.Terrain=TerrainType.Water;tile.MovementCost=2;}
                bool obstacle=stage==3?((x==3||x==8)&&(z==5||z==8)):
                    stage==4?((x==3||x==10)&&(z==4||z==5)):((x==4||x==8)&&(z==4||z==8));
                if(obstacle){tile.Walkable=false;tile.Terrain=TerrainType.Obstacle;}
                map.Tiles.Add(p,tile);
            }
            return map;
        }
        static Vector2Int ExpansionPlayerSpawn(int stage,int index) => stage==4?new Vector2Int(1+index%2,3+index/2):new Vector2Int(4+index%3,1+index/3);
        static Vector2Int ExpansionEnemySpawn(int stage,int index) => stage==3?new Vector2Int(4+index%2*3,9+index/2):stage==4?new Vector2Int(11+index%2,3+index/2*3):new Vector2Int(5+index%2*2,6+index/2);
        static StoryLine[] ExpansionStory(int stage,bool after)
        {
            CampaignStages.Get(stage);
            if(stage==3&&!after)return new[]{
                new StoryLine("기록 · 관측소", "협곡의 신호에 응답한 것은 멈춘 관측반의 잔광이었다. 인기척이 없는 탑에서 별의 위치를 새기는 고리만 거꾸로 돌고 있었다."),
                new StoryLine("제이드", "누군가 구조 신호를 보낸 것은 아니었군요. 관측반이 협곡의 봉화를 받아 자동으로 답한 겁니다."),
                new StoryLine("티아", "제단에서 들었던 리듬과 같아. 하지만 이곳의 음은 한 박자씩 뒤로 밀리고 있어."),
                new StoryLine("나탈리아", "도움이 필요한 사람이 있는지 살펴봐요. 술사와 궁수가 골렘 뒤에서 회랑을 지키고 있어요."),
                new StoryLine("크레스", "높은 단상으로 갈수록 적이 유리해. 기둥 사이에서 대열을 나누지 말자.")};
            if(stage==3)return new[]{
                new StoryLine("제이드", "관측반은 고장 난 것이 아니라 늦게 도착한 명령을 반복하고 있었습니다. 명령은 저수 시설 아래에서 올라오는군요."),
                new StoryLine("시온", "탑에 남은 작업 일지를 찾았어. 관리인들은 수문을 점검하러 내려간 뒤 돌아오지 않았대."),
                new StoryLine("민트", "방어 장치가 갑자기 움직였다면 안전한 곳에 숨어 있을지도 몰라요. 서둘러 찾아요."),
                new StoryLine("키사라", "이 메달은 충격을 줄여 주는 장비로 보인다. 내려가기 전에 정비소에서 같은 장비를 준비해 두자."),
                new StoryLine("기록", "관측 기록에서 수문의 위치를 알아냈다. 5장과 수호의 메달 상점이 열렸다. 일행은 관리인들의 흔적을 따라 저수 시설로 향한다.")};
            if(stage==4&&!after)return new[]{
                new StoryLine("기록", "수문은 반쯤 닫힌 채 멈춰 있었다. 오타오타와 펭기니스트가 물길에 몰려들고, 에그베어가 교량을 막고 있었다."),
                new StoryLine("알펜", "저 건너에서 사람이 작업하고 있어. 마물들 때문에 나오는 길이 막힌 것 같군."),
                new StoryLine("벨벳", "물이 더 불어나기 전에 길부터 열지. 다리가 둘이라고 양쪽으로 흩어질 필요는 없어."),
                new StoryLine("나탈리아", "후방에서 엄호하겠어요. 물길로 들어가면 움직임이 느려지니 교량을 이용하세요."),
                new StoryLine("파라", "알았어. 앞쪽 적을 끌어낸 다음 다 같이 건너자!")};
            if(stage==4)return new[]{
                new StoryLine("관리인", "중계탑에 금빛 머리의 남자가 들어간 뒤 폐쇄 명령이 반복됐소. 다오스라고 했지. 덕분에 이제 나갈 수 있겠군."),
                new StoryLine("제이드", "관측소와 수문이 서로 옛 명령을 되돌려 보내고 있었습니다. 중계핵에서 반복을 끊어야 합니다."),
                new StoryLine("키사라", "마을로 돌아가는 길은 확보했다. 관리인들은 먼저 대피시키고, 우리는 중계탑으로 가자."),
                new StoryLine("관리인", "정비용 갑옷을 가져가시오. 남은 재료도 마을 상점으로 보내겠소. 핵실의 기둥은 오래됐으니 사격할 때 조심하시오."),
                new StoryLine("기록", "물이 다시 흘렀고 관리인들이 구조되었다. 6장과 단련 갑옷 상점이 열렸다. 오래된 폐쇄 명령의 근원은 중계탑 안에 남아 있었다.")};
            if(!after)return new[]{
                new StoryLine("기록", "중계핵에서 흘러나오는 빛 속에 금빛 머리의 남자가 서 있었다. 다오스가 손을 들자 골렘과 수호병들이 길을 막았다."),
                new StoryLine("다오스", "이 핵에 모인 마나는 넘겨줄 수 없다. 돌아가라. 너희가 감당할 일이 아니다."),
                new StoryLine("크레스", "다오스! 네가 이 장치들을 움직이고 있었나. 마을 사람들을 더 위험에 빠뜨리게 두진 않겠어."),
                new StoryLine("시온", "빛이 손에 모이면 흩어져. 기둥으로 사선을 끊고 호위부터 정리하자."),
                new StoryLine("다오스", "그 의지가 얼마나 강한지, 여기서 보여 보아라.")};
            return new[]{
                new StoryLine("다오스", "다른 세계를 지키려는 의지인가... 너희의 답은 보았다."),
                new StoryLine("티아", "마나의 흐름이 풀렸어. 관측소와 수문도 정상으로 돌아오고 있어."),
                new StoryLine("민트", "이 빛을 누군가를 해치는 데 쓰지 않아도 돼요. 모두가 돌아갈 길을 찾을 수 있을 거예요."),
                new StoryLine("크레스", "마을로 돌아가자. 우리가 지켜 낸 사람들에게 이 새벽을 보여 주고 싶어."),
                new StoryLine("기록", "다오스가 물러나고 중계핵은 고요해졌다. 여섯 지역을 이은 빛 아래 일행은 구조한 사람들과 귀환했다. 6장까지의 여정이 끝났다. 완료한 장은 다시 도전할 수 있다.")};
        }
    }
}
