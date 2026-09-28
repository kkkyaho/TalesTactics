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
            return route+"\n목표: 모든 적 격파. 적 Lv"+chapter.EnemyLevel+" / 최초 "+chapter.FirstGold+"G·EXP"+chapter.FirstEXP+" / "+chapter.EquipmentName+".";
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
                new StoryLine("나탈리아", "그렇다면 아직 도움이 필요한 사람이 있는지부터 살펴봐요. 회랑을 막은 수호병을 돌파해야겠어요."),
                new StoryLine("크레스", "높은 단상으로 갈수록 적이 유리해. 기둥 사이에서 대열을 나누지 말자.")};
            if(stage==3)return new[]{
                new StoryLine("제이드", "관측반은 고장 난 것이 아니라 늦게 도착한 명령을 반복하고 있었습니다. 명령은 저수 시설 아래에서 올라오는군요."),
                new StoryLine("시온", "탑에 남은 작업 일지를 찾았어. 관리인들은 수문을 점검하러 내려간 뒤 돌아오지 않았대."),
                new StoryLine("민트", "방어 장치가 갑자기 움직였다면 안전한 곳에 숨어 있을지도 몰라요. 서둘러 찾아요."),
                new StoryLine("키사라", "이 메달은 충격을 줄여 주는 장비로 보인다. 내려가기 전에 정비소에서 같은 장비를 준비해 두자."),
                new StoryLine("기록", "관측 기록에서 수문의 위치를 알아냈다. 5장과 수호의 메달 상점이 열렸다. 일행은 관리인들의 흔적을 따라 저수 시설로 향한다.")};
            if(stage==4&&!after)return new[]{
                new StoryLine("기록", "수문은 반쯤 닫힌 채 멈춰 있었다. 두 갈래 물길 사이에서 들려오는 망치 소리에 맞춰 수호병들이 순찰 경로를 바꾸었다."),
                new StoryLine("알펜", "저 건너에서 사람이 작업하고 있어. 수호병 때문에 나오는 길이 막힌 것 같군."),
                new StoryLine("벨벳", "물이 더 불어나기 전에 길부터 열지. 다리가 둘이라고 양쪽으로 흩어질 필요는 없어."),
                new StoryLine("나탈리아", "후방에서 엄호하겠어요. 물길로 들어가면 움직임이 느려지니 교량을 이용하세요."),
                new StoryLine("파라", "알았어. 앞쪽 적을 끌어낸 다음 다 같이 건너자!")};
            if(stage==4)return new[]{
                new StoryLine("관리인", "봉화가 돌아온 걸 보고 수문을 열려 했소. 그런데 중계탑에서 폐쇄 명령이 계속 내려왔지. 덕분에 이제 나갈 수 있겠군."),
                new StoryLine("제이드", "관측소와 수문이 서로 옛 명령을 되돌려 보내고 있었습니다. 중계핵에서 반복을 끊어야 합니다."),
                new StoryLine("키사라", "마을로 돌아가는 길은 확보했다. 관리인들은 먼저 대피시키고, 우리는 중계탑으로 가자."),
                new StoryLine("관리인", "정비용 갑옷을 가져가시오. 남은 재료도 마을 상점으로 보내겠소. 핵실의 기둥은 오래됐으니 사격할 때 조심하시오."),
                new StoryLine("기록", "물이 다시 흘렀고 관리인들이 구조되었다. 6장과 단련 갑옷 상점이 열렸다. 오래된 폐쇄 명령의 근원은 중계탑 안에 남아 있었다.")};
            if(!after)return new[]{
                new StoryLine("기록", "중계핵은 비어 있는 시설들을 지키라는 마지막 명령을 수없이 되풀이했다. 일행이 원형 단상에 들어서자 남은 수호병들이 핵을 둘러쌌다."),
                new StoryLine("티아", "반복되는 리듬 사이에 빈틈이 있어. 수호병이 멈추면 관측소의 새 신호를 핵에 전할 수 있어."),
                new StoryLine("벨벳", "그럼 단순하네. 네가 신호를 보낼 동안 저 녀석들을 치우면 되잖아."),
                new StoryLine("시온", "기둥 뒤로 들어가면 사선이 끊겨. 중앙의 높은 자리로 한꺼번에 몰리지 마."),
                new StoryLine("크레스", "여기서 끝내자. 모두 무사히 마을로 돌아가는 거야!")};
            return new[]{
                new StoryLine("티아", "새 신호가 들어갔어. 더는 옛 명령이 되돌아오지 않아."),
                new StoryLine("민트", "마을의 진동도, 수문의 소리도 잦아들었어요. 이제 관리인들이 안전하게 고칠 수 있겠어요."),
                new StoryLine("제이드", "이 장치는 한 시설만을 위한 것이 아니었군요. 북쪽에도 같은 표식이 있습니다. 다만 조사는 쉬고 난 뒤에 해도 늦지 않겠습니다."),
                new StoryLine("파라", "오늘은 마을로 돌아가자! 모두 기다리고 있을 거야."),
                new StoryLine("기록", "새벽의 봉화가 관측소와 마을을 이었다. 일행은 구조한 사람들과 돌아와 첫 여정을 마무리했다. 6장까지의 이야기가 끝났다. 완료한 장은 다시 도전할 수 있다.")};
        }
    }
}
