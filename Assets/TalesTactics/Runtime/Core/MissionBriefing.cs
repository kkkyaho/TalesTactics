using System.Linq;
namespace TalesTactics
{
    public static class MissionBriefing
    {
        public static string Victory(BattleSession session)
        {return session.Objective==ObjectiveKind.Eliminate?"모든 적 격파":session.ObjectiveDescription;}
        public static string Defeat(BattleSession session)
        {return session.Objective==ObjectiveKind.Escort?"아군 전원 전투불능 또는 "+session.ObjectiveUnit.Data.DisplayName+" 전투불능":"아군 전원 전투불능";}
        public static string Progress(BattleSession session)
        {
            int alive=session.Units.Count(u=>u.Team==Team.Enemy&&u.Alive),total=session.Units.Count(u=>u.Team==Team.Enemy);
            if(session.Objective==ObjectiveKind.Survive)return session.ObjectiveDescription;
            if(session.Objective==ObjectiveKind.Reach||session.Objective==ObjectiveKind.Escort)return session.ObjectiveDescription;
            if(session.Objective==ObjectiveKind.Boss)return "보스 HP "+session.ObjectiveUnit.CurrentHP+" / "+session.ObjectiveUnit.Stats.HP;
            return "적 잔존 "+alive+" / "+total;
        }
        public static string Terrain(int stage)
        {
            switch(stage)
            {
                case 0:return "첫 교전 · 부상자를 회복하고 아군 전열을 함께 전진시키세요.";
                case 1:return "유적 · 전열 뒤의 마법형을 확인하고 마법 방어를 준비하세요.";
                case 2:return "협곡 · 돌격형과 사격형을 분리해 상대하세요.";
                case 3:return "관측소 · 원거리 적에게 접근할 때 높이와 시야 차단을 확인하세요.";
                case 4:return "수문 · 물 타일의 이동 비용을 고려해 아군이 고립되지 않게 하세요.";
                case 5:return "중계핵 · 다오스의 직선·범위 기술에 대비해 간격을 벌리세요.";
                default:return "훈련 · 선택한 목표 조건을 확인하세요. 성장 보상은 저장하지 않습니다.";
            }
        }
    }
}
