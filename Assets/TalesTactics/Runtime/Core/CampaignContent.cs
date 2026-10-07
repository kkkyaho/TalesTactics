using System;
using UnityEngine;

namespace TalesTactics
{
    public sealed class StoryLine
    {
        public readonly string Speaker, Text;
        public StoryLine(string speaker, string text) { Speaker = speaker; Text = text; }
    }

    // Original scenario for this project; training retains the original test map.
    public static partial class CampaignContent
    {
        public static string Location(int stage) => stage >= 3 ? ExpansionLocation(stage) : stage == 0 ? "유적 외곽 · 갈라진 물길" : stage == 1 ? "심층 제단 · 수호자의 계단" : "바람 협곡 · 끊어진 연락로";
        public static string Briefing(int stage) => stage >= 3 ? ExpansionBriefing(stage) : stage == 0
            ? "물길의 돌다리와 북쪽 우회로로 진입하세요.\n목표: 모든 적 격파. 보상: 120G / 생명의 부적."
            : stage == 1 ? "계단을 따라 중앙 고지로 진입하세요.\n목표: 모든 적 격파. 보상: 180G / 철검."
            : "두 돌다리로 물길을 건너 동쪽 능선을 확보하세요.\n목표: 모든 적 격파. 적 Lv4 / 최초 240G·EXP300 / 강화 갑옷.";

        public static GridMap Map(int stage)
        {
            if (stage < 0 || stage >= CampaignStages.Count) throw new ArgumentOutOfRangeException(nameof(stage));
            if (stage >= 3) return ExpansionMap(stage);
            var map = new GridMap(); int width = stage == 0 ? 11 : stage == 1 ? 12 : 13, depth = stage == 0 ? 9 : 10;
            for (int x = 0; x < width; x++) for (int z = 0; z < depth; z++)
            {
                var p = new Vector2Int(x, z);
                int height = stage == 0 ? (x >= 7 ? 1 : 0) : stage == 1 ? (z >= 6 ? 2 : z >= 4 ? 1 : 0) : (x >= 10 ? 2 : x >= 8 ? 1 : 0);
                var tile = new GridTile { Coordinate = p, Height = height, Terrain = height > 0 ? TerrainType.HighGround : TerrainType.Normal };
                if (stage == 0 && x == 5 && z != 2 && z != 6 || stage == 2 && x == 6 && z != 2 && z != 7)
                { tile.Terrain = TerrainType.Water; tile.MovementCost = 2; }
                bool obstacle = stage == 0 ? (x == 4 && z == 4 || x == 7 && z == 3 || x == 8 && z == 7)
                    : stage == 1 ? ((x == 3 || x == 8) && (z == 4 || z == 7))
                    : ((x == 4 || x == 8) && (z == 4 || z == 5));
                if (obstacle) { tile.Terrain = TerrainType.Obstacle; tile.Walkable = false; }
                map.Tiles.Add(p, tile);
            }
            return map;
        }

        public static Vector2Int PlayerSpawn(int stage, int index) => stage >= 3 ? ExpansionPlayerSpawn(stage,index) : stage == 0
            ? new Vector2Int(1 + index % 2, 1 + index / 2) : stage == 1 ? new Vector2Int(4 + index % 3, 1 + index / 3) : new Vector2Int(1 + index % 2, 3 + index / 2);
        public static Vector2Int EnemySpawn(int stage, int index) => stage >= 3 ? ExpansionEnemySpawn(stage,index) : stage == 0
            ? new Vector2Int(8 + index % 2, 4 + index / 2) : stage == 1 ? new Vector2Int(4 + index % 2 * 3, 7 + index / 2) : new Vector2Int(10 + index % 2, 3 + index / 2 * 3);

        public static StoryLine[] Story(int stage, bool after)
        {
            if (stage >= 3) return ExpansionStory(stage,after);
            if (stage == 2 && !after) return new[] {
                new StoryLine("기록", "마을로 돌아온 일행은 제단의 기록에서 산 너머 관측소의 표식을 발견했다. 그러나 관측소로 향하는 연락로에서는 며칠째 봉화가 오르지 않았다."),
                new StoryLine("제이드", "제단은 끝이 아니라 연결점이었군요. 기록의 다음 부분은 저 관측소에서 찾을 수 있겠습니다."),
                new StoryLine("나탈리아", "연락이 끊겼다면 사람들도 고립되어 있을지 몰라요. 먼저 협곡을 통과할 길을 확보해야 해요."),
                new StoryLine("파라", "돌다리는 남쪽과 북쪽에 하나씩 있어. 물을 건너면 느려지니까, 앞사람만 너무 멀리 가지 말자."),
                new StoryLine("키사라", "동쪽 능선에 에그베어와 궁수가 있다. 다리를 건넌 뒤 대열을 정비하고 올라가겠다.") };
            if (stage == 2) return new[] {
                new StoryLine("알펜", "길이 열렸어. 봉화대도 무사해. 이제 마을에 신호를 보낼 수 있겠군."),
                new StoryLine("티아", "저 멀리 관측소에서도 빛이 돌아왔어. 누군가 우리 신호를 보고 있어."),
                new StoryLine("제이드", "수호병에 새겨진 문양이 제단과 같습니다. 장치들이 같은 명령에 반응했다면, 관측소의 기록이 이유를 알려 주겠지요."),
                new StoryLine("민트", "오늘은 여기서 쉬어요. 다음 길을 가려면 모두 힘을 되찾아야 해요."),
                new StoryLine("기록", "협곡의 연락로가 다시 이어졌다. 일행은 봉화대에서 찾은 강화 갑옷을 정비하고 산 너머를 바라본다. 관측소로 향하는 이야기는 다음 장에서 계속된다.") };
            if (stage == 0 && !after) return new[] {
                new StoryLine("서막 · 경계의 빛", "서로 다른 길을 걷던 일행은 같은 빛을 따라 무너진 유적에 모였다. 밤마다 울리는 진동은 산 아래 마을까지 번지고 있었다."),
                new StoryLine("크레스", "안쪽에서 또 진동이 왔어. 마을로 돌아가기 전에 원인을 확인하자."),
                new StoryLine("민트", "물길 너머에 누군가 남긴 부적이 있어요. 이곳을 지키려던 사람들의 흔적 같아요."),
                new StoryLine("파라", "돌다리는 좁지만 북쪽으로도 돌아갈 수 있어. 서로 떨어지지 않게 움직이자!"),
                new StoryLine("크레스", "울프와 슬라임까지 길을 막고 있어. 물가의 오타오타도 조심해. 다들 준비됐지?") };
            if (stage == 0) return new[] {
                new StoryLine("민트", "부적의 빛이 안쪽 문양과 이어져 있어요. 진동의 근원은 더 깊은 곳에 있나 봐요."),
                new StoryLine("제이드", "침입자를 쫓는 장치가 아니라, 무언가 새어 나오는 것을 막는 장치일 수도 있겠군요."),
                new StoryLine("키사라", "그렇다면 준비가 필요해. 돌아가 장비를 점검하고, 다음에는 제단으로 향하자."),
                new StoryLine("기록", "외곽의 길이 열렸다. 일행은 부적을 단서로 심층 제단을 향한다. 2장과 상위 장비 상점이 해금되었다.") };
            if (!after) return new[] {
                new StoryLine("기록", "물길 아래에서 발견한 계단은 거대한 제단으로 이어졌다. 중앙의 빛이 흔들릴 때마다 돌로 된 수호병들이 깨어났다."),
                new StoryLine("티아", "빛의 리듬이 불안정해. 골렘과 술사들이 제단을 지키고 있어."),
                new StoryLine("알펜", "먼저 길을 열자. 제단에 도착하면 이 진동을 멈출 방법이 있을 거야."),
                new StoryLine("나탈리아", "위쪽 적을 주의하세요. 계단에서 대열을 유지하고, 다친 동료는 뒤로 물러나세요."),
                new StoryLine("키사라", "내가 앞을 맡겠다. 모두 함께 올라간다!") };
            return new[] {
                new StoryLine("크레스", "수호병들이 멈췄어. 민트, 부적을 제단에 올려 줘."),
                new StoryLine("민트", "이 빛은... 금빛 머리의 남자가 마나를 모으던 기억이에요. 관측소의 표식도 보여요."),
                new StoryLine("벨벳", "기억 하나 때문에 이 난리였다는 거야? 적어도 마을은 무사하겠네."),
                new StoryLine("시온", "아직 돌아갈 길이 남았어. 다친 사람부터 확인하자."),
                new StoryLine("기록", "제단의 진동은 잦아들고 새벽빛이 물길에 닿았다. 일행은 발견한 기록을 품고 마을로 돌아간다. 유적의 두 장 이야기가 막을 내렸다.") };
        }
    }
}
