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
    public static class CampaignContent
    {
        public static string Location(int stage) => stage == 0 ? "유적 외곽 · 갈라진 물길" : "심층 제단 · 수호자의 계단";
        public static string Briefing(int stage) => stage == 0
            ? "물길의 돌다리와 북쪽 우회로로 진입하세요.\n목표: 모든 적 격파. 보상: 120G / 생명의 부적."
            : "계단을 따라 중앙 고지로 진입하세요.\n목표: 모든 적 격파. 보상: 180G / 철검.";

        public static GridMap Map(int stage)
        {
            if (stage < 0 || stage >= CampaignStages.Count) throw new ArgumentOutOfRangeException(nameof(stage));
            var map = new GridMap(); int width = stage == 0 ? 11 : 12, depth = stage == 0 ? 9 : 10;
            for (int x = 0; x < width; x++) for (int z = 0; z < depth; z++)
            {
                var p = new Vector2Int(x, z);
                int height = stage == 0 ? (x >= 7 ? 1 : 0) : (z >= 6 ? 2 : z >= 4 ? 1 : 0);
                var tile = new GridTile { Coordinate = p, Height = height, Terrain = height > 0 ? TerrainType.HighGround : TerrainType.Normal };
                if (stage == 0 && x == 5 && z != 2 && z != 6)
                { tile.Terrain = TerrainType.Water; tile.MovementCost = 2; }
                bool obstacle = stage == 0 ? (x == 4 && z == 4 || x == 7 && z == 3 || x == 8 && z == 7)
                    : ((x == 3 || x == 8) && (z == 4 || z == 7));
                if (obstacle) { tile.Terrain = TerrainType.Obstacle; tile.Walkable = false; }
                map.Tiles.Add(p, tile);
            }
            return map;
        }

        public static Vector2Int PlayerSpawn(int stage, int index) => stage == 0
            ? new Vector2Int(1 + index % 2, 1 + index / 2) : new Vector2Int(4 + index % 3, 1 + index / 3);
        public static Vector2Int EnemySpawn(int stage, int index) => stage == 0
            ? new Vector2Int(8 + index % 2, 4 + index / 2) : new Vector2Int(4 + index % 2 * 3, 7 + index / 2);

        public static StoryLine[] Story(int stage, bool after)
        {
            if (stage == 0 && !after) return new[] {
                new StoryLine("서막 · 경계의 빛", "서로 다른 길을 걷던 일행은 같은 빛을 따라 무너진 유적에 모였다. 밤마다 울리는 진동은 산 아래 마을까지 번지고 있었다."),
                new StoryLine("크레스", "안쪽에서 또 진동이 왔어. 마을로 돌아가기 전에 원인을 확인하자."),
                new StoryLine("민트", "물길 너머에 누군가 남긴 부적이 있어요. 이곳을 지키려던 사람들의 흔적 같아요."),
                new StoryLine("파라", "돌다리는 좁지만 북쪽으로도 돌아갈 수 있어. 서로 떨어지지 않게 움직이자!"),
                new StoryLine("크레스", "길을 막은 수호병부터 정리하자. 다들 준비됐지?") };
            if (stage == 0) return new[] {
                new StoryLine("민트", "부적의 빛이 안쪽 문양과 이어져 있어요. 진동의 근원은 더 깊은 곳에 있나 봐요."),
                new StoryLine("제이드", "침입자를 쫓는 장치가 아니라, 무언가 새어 나오는 것을 막는 장치일 수도 있겠군요."),
                new StoryLine("키사라", "그렇다면 준비가 필요해. 돌아가 장비를 점검하고, 다음에는 제단으로 향하자."),
                new StoryLine("기록", "외곽의 길이 열렸다. 일행은 부적을 단서로 심층 제단을 향한다. 2장과 상위 장비 상점이 해금되었다.") };
            if (!after) return new[] {
                new StoryLine("기록", "물길 아래에서 발견한 계단은 거대한 제단으로 이어졌다. 중앙의 빛이 흔들릴 때마다 돌로 된 수호병들이 깨어났다."),
                new StoryLine("티아", "빛의 리듬이 불안정해. 수호병들이 우리를 적으로 인식하고 있어."),
                new StoryLine("알펜", "먼저 길을 열자. 제단에 도착하면 이 진동을 멈출 방법이 있을 거야."),
                new StoryLine("나탈리아", "위쪽 적을 주의하세요. 계단에서 대열을 유지하고, 다친 동료는 뒤로 물러나세요."),
                new StoryLine("키사라", "내가 앞을 맡겠다. 모두 함께 올라간다!") };
            return new[] {
                new StoryLine("크레스", "수호병들이 멈췄어. 민트, 부적을 제단에 올려 줘."),
                new StoryLine("민트", "이 빛은... 누군가에게 전하려던 기억이었어요. 이제 조용해졌어요."),
                new StoryLine("벨벳", "기억 하나 때문에 이 난리였다는 거야? 적어도 마을은 무사하겠네."),
                new StoryLine("시온", "아직 돌아갈 길이 남았어. 다친 사람부터 확인하자."),
                new StoryLine("기록", "제단의 진동은 잦아들고 새벽빛이 물길에 닿았다. 일행은 발견한 기록을 품고 마을로 돌아간다. 유적의 두 장 이야기가 막을 내렸다.") };
        }
    }
}
