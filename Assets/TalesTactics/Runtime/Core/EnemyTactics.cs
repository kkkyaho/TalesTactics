using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    // Intent is recorded at preparation and never tracks a target that moves away.
    public static class EnemyTactics
    {
        public static void Configure(BattleSession battle,BattleCatalog catalog)
        {
            foreach(var u in battle.Units.Where(u=>u.Team==Team.Enemy))
            {
                u.TacticalEnemy=true;
                if(EnemyRoles.Role(u.Data)==EnemyRole.Frontline)u.Trait=TacticalTrait.Protector;
                var role=u.BossWard?EnemyRole.Boss:EnemyRoles.Role(u.Data);
                u.TacticalSkills=(catalog.TacticalEnemySkills??new SkillData[0]).Where(s=>s!=null&&
                    (role==EnemyRole.Caster&&(s.Id=="tactic.heal"||s.Id=="tactic.burst")||role==EnemyRole.Boss&&s.Id=="tactic.nova"||role==EnemyRole.Assault&&s.Id=="tactic.charge")).ToArray();
            }
        }
        public static SkillData Intent(UnitRuntime u)=>new[]{u.Data.BasicAttack}.Concat(u.Data.Skills).Concat(u.TacticalSkills).Concat(new[]{u.Data.UltimateSkill}).FirstOrDefault(s=>s!=null&&s.Id==u.IntentSkill);
        public static string Describe(UnitRuntime u)
        {
            if(u.IntentPhase==1)return "예고: "+(Intent(u)?.DisplayName??"공격")+" → "+u.IntentAim+" · 다음 자기 턴 발동";
            if(u.IntentPhase==2)return "빈틈 · 받는 피해 +25% · 다음 자기 턴 휴식";
            if(u.BossWard)return "호위 방벽 · 호위 1명당 피해 -25% (최대 75%)";
            if(u.Trait==TacticalTrait.Protector)return "방어 시 인접 동료 보호 1회";
            return EnemyRoles.Name(u.Data);
        }
        public static EnemyPlan Plan(BattleSession battle,UnitRuntime u)
        {
            if(!u.Alive||u.Has(StatusKind.Stun)||u.Has(StatusKind.Sleep))return new EnemyPlan{Destination=u.Position};
            var role=u.BossWard?EnemyRole.Boss:EnemyRoles.Role(u.Data);
            if(u.IntentPhase==1)
            {
                if(u.TurnsStarted<=u.IntentTurn)return new EnemyPlan{Destination=u.Position,Guard=true};
                var skill=Intent(u);
                var destination=u.Position;
                if(role==EnemyRole.Assault&&skill?.Id=="tactic.charge"&&!u.Moved)
                    destination=battle.Grid.Reachable(u,out _).Keys.Where(p=>p!=u.IntentAim).OrderBy(p=>GridMap.Distance(p,u.IntentAim)).ThenBy(p=>GridMap.Distance(p,u.Position)).FirstOrDefault();
                return new EnemyPlan{Destination=destination,Skill=skill,Aim=u.IntentAim};
            }
            if(u.IntentPhase==2)return new EnemyPlan{Destination=u.Position};
            var plan=new UtilityPlanner().Plan(battle,u,!battle.UseUtilityAI);
            if(role==EnemyRole.Frontline&&u.TurnsStarted%2==1)
            {
                var friends=battle.Units.Where(a=>a!=u&&a.Team==u.Team&&a.Alive).ToArray();
                var reach=battle.Grid.Reachable(u,out _).Keys;
                var cover=reach.Where(p=>friends.Any(a=>GridMap.Distance(p,a.Position)==1)).OrderBy(p=>GridMap.Distance(u.Position,p)).ThenBy(p=>p.x).ThenBy(p=>p.y).ToArray();
                if(cover.Length>0)return new EnemyPlan{Destination=cover[0],Guard=true};
            }
            bool preparing=role==EnemyRole.Boss||role==EnemyRole.Caster||role==EnemyRole.Assault&&u.TurnsStarted%3==1;
            if(preparing&&plan.Skill!=null&&plan.Skill.Target==TargetType.Enemy&&(plan.Aim.HasValue||plan.Target!=null))
            {
                plan.PrepareSkill=plan.Skill;plan.PrepareAim=plan.Aim??plan.Target.Position;
                plan.Skill=null;plan.Target=null;plan.Aim=null;plan.Guard=true;
            }
            return plan;
        }
        public static void Commit(UnitRuntime u,EnemyPlan plan)
        {
            if(!u.TacticalEnemy||!u.Alive||u.Has(StatusKind.Stun)||u.Has(StatusKind.Sleep))return;
            if(plan.PrepareSkill!=null){u.IntentSkill=plan.PrepareSkill.Id;u.IntentAim=plan.PrepareAim;u.IntentTurn=u.TurnsStarted;u.IntentPhase=1;}
            else if(u.IntentPhase==1&&u.TurnsStarted>u.IntentTurn&&plan.Skill!=null)u.IntentPhase=2;
        }
    }
}
