using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public enum EnemyRole { Frontline, Assault, Ranged, Caster, Boss }
    public static class EnemyRoles
    {
        public static EnemyRole Role(CharacterData data)
        {
            switch(data.Id)
            {
                case "wolf":case "polwigle":case "penguinist":return EnemyRole.Assault;
                case "archer":return EnemyRole.Ranged;
                case "mage":return EnemyRole.Caster;
                case "dhaos":return EnemyRole.Boss;
                default:return EnemyRole.Frontline;
            }
        }
        public static string Name(CharacterData data)
        {
            switch(Role(data))
            {case EnemyRole.Assault:return "돌격";case EnemyRole.Ranged:return "사격";case EnemyRole.Caster:return "마법";case EnemyRole.Boss:return "보스";default:return "전열";}
        }
        public static string Advice(CharacterData data)
        {
            switch(Role(data))
            {
                case EnemyRole.Assault:return "돌진 준비 위치에서 예고한 칸을 공격합니다. 표식 밖으로 피한 뒤 빈틈을 공략하세요.";
                case EnemyRole.Ranged:return "거리를 벌려 사격합니다. 엄폐를 이용하고 근접 압박하세요.";
                case EnemyRole.Caster:return "부상당한 동료를 회복하고 범위 마법을 예고합니다. 시전 전에 흩어지거나 행동 불가로 저지하세요.";
                case EnemyRole.Boss:return "예고 → 확정 위치 공격 → 휴식. 예고 범위 밖으로 이동하고 받는 피해가 25% 늘어난 빈틈에 집중하세요.";
                default:return "격턴으로 동료 옆에서 방어합니다. 인접 동료 피해를 1회 30% 줄이므로 먼저 보호를 소모시키세요.";
            }
        }
        // Role preferences apply only to campaign enemies; training and player lookahead retain their rules.
        public static float PositionScore(BattleSession battle,UnitRuntime unit,Vector2Int position)
        {
            if(unit.Team!=Team.Enemy||battle.CampaignStage<0)return 0;
            var foes=battle.Units.Where(u=>u.Team!=unit.Team&&u.Alive).ToArray();if(foes.Length==0)return 0;
            var role=Role(unit.Data);int distance=foes.Min(u=>GridMap.Distance(position,u.Position));
            if(role==EnemyRole.Ranged||role==EnemyRole.Caster)
                return -18*Mathf.Max(0,Mathf.Min(3,unit.Data.BasicAttack.Range)-distance);
            return 0;
        }
        public static float TargetScore(BattleSession battle,UnitRuntime unit,UnitRuntime target)
        {
            if(unit.Team!=Team.Enemy||battle.CampaignStage<0||target.Team==unit.Team)return 0;
            return Role(unit.Data)==EnemyRole.Assault?45*(1f-(float)target.CurrentHP/target.Stats.HP):0;
        }
    }
}
