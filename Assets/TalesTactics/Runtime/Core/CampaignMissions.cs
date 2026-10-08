using UnityEngine;
namespace TalesTactics
{
    public static class CampaignMissions
    {
        public static ObjectiveKind Kind(int stage)=>stage==1||stage==5?ObjectiveKind.Boss:stage==2?ObjectiveKind.Reach:stage==3?ObjectiveKind.Survive:stage==4?ObjectiveKind.Escort:ObjectiveKind.Eliminate;
        public static Vector2Int Destination(int stage)=>stage==2?new Vector2Int(11,8):stage==4?new Vector2Int(12,8):new Vector2Int(8,8);
        public static string Description(int stage)
        {
            switch(stage)
            {
                case 1:return "유적의 첫 수호자 격파 · 남은 적은 격파하지 않아도 승리";
                case 2:return "봉화 (11,8)에 아군 1명 도착";
                case 3:return "관측 기록 복원 · 자유 턴: 적군 턴 4회 생존 / SPD·CT: 아군 12회";
                case 4:return "선두 출전 아군을 수문 (12,8)까지 호위 · 해당 아군 전투불능 시 패배";
                case 5:return "다오스 격파 · 예고 → 공격 → 빈틈 순환에 대응";
                default:return "모든 적 격파";
            }
        }
    }
}
