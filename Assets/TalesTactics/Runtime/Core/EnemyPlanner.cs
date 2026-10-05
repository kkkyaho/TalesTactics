using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed class EnemyPlan { public Vector2Int Destination; public UnitRuntime Target; public SkillData Skill; public Vector2Int? Aim; public bool Guard; }
    public sealed class EnemyPlanner
    {
        public EnemyPlan Plan(BattleSession battle,UnitRuntime u)
        {
            if(battle.UseUtilityAI||u.Data.Id=="dhaos")return new UtilityPlanner().Plan(battle,u);
            var enemies=battle.Units.Where(t=>t.Team!=u.Team&&t.Alive).ToArray();
            var plan=new EnemyPlan{Destination=u.Position};if(enemies.Length==0)return plan;
            float best=float.NegativeInfinity;var origin=u.Position;
            var range=u.Moved?new[]{u.Position}:battle.Grid.Reachable(u,out _).Keys.ToArray();
            foreach(var p in range)
            {
                u.Position=p;
                foreach(var t in enemies)
                {
                    int d=GridMap.Distance(p,t.Position);float score=-d*5+EnemyRoles.PositionScore(battle,u,p);
                    bool can=battle.Resolver.InRange(u,u.Data.BasicAttack,t.Position);
                    if(can)score+=100+(1f-(float)t.CurrentHP/t.Stats.HP)*30+(battle.Resolver.DirectionMultiplier(u,t)-1)*50+EnemyRoles.TargetScore(battle,u,t);
                    if(can&&battle.Resolver.DamagePreview(u,t,u.Data.BasicAttack.Effects[0],u.Data.BasicAttack)>=t.CurrentHP)score+=100;
                    if(score>best){best=score;plan.Destination=p;plan.Target=can?t:null;plan.Skill=u.Data.BasicAttack;}
                }
            }
            u.Position=origin;return plan;
        }
    }
}
