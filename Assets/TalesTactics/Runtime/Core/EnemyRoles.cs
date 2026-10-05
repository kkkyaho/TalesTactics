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
                case EnemyRole.Assault:return "부상당한 아군을 노립니다. 후방을 지키고 먼저 회복하세요.";
                case EnemyRole.Ranged:return "거리를 벌려 사격합니다. 엄폐를 이용하고 근접 압박하세요.";
                case EnemyRole.Caster:return "거리를 유지하며 마법 공격. 마법 방어를 높이고 근접 압박하세요.";
                case EnemyRole.Boss:return "직선·범위 기술을 사용합니다. 일렬 밀집을 피하세요. 부하도 격파해야 합니다.";
                default:return "가까운 아군을 압박합니다. 전방에서 받아내고 측후면을 공략하세요.";
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
