using System;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    // Deterministic one-action lookahead. Uses the resolver for gates, geometry and damage.
    public sealed class UtilityPlanner
    {
        public EnemyPlan Plan(BattleSession battle,UnitRuntime unit)
        {
            var result=new EnemyPlan{Destination=unit.Position,Guard=!unit.Acted};
            if(!unit.Alive||unit.Has(StatusKind.Stun)||unit.Has(StatusKind.Sleep))return result;
            var origin=unit.Position;var facing=unit.Facing;
            var destinations=unit.Moved?new[]{origin}:battle.Grid.Reachable(unit,out _).Keys.OrderBy(p=>p.x).ThenBy(p=>p.y).ToArray();
            var opponents=battle.Units.Where(u=>u.Team!=unit.Team&&u.Alive).ToArray();
            var skills=new[]{unit.Data.BasicAttack}.Concat(unit.Data.Skills).Concat(unit.TacticalSkills).Concat(new[]{unit.Data.UltimateSkill})
                .Where(s=>s!=null&&battle.Resolver.CanUse(unit,s)==null).Distinct().ToArray();
            float best=float.NegativeInfinity;
            try
            {
                foreach(var position in destinations)
                {
                    if(!battle.Grid.Place(unit,position))continue;
                    float distance=opponents.Length==0?0:opponents.Min(e=>GridMap.Distance(position,e.Position));
                    if(unit.Team==Team.Player&&(battle.Objective==ObjectiveKind.Reach||battle.Objective==ObjectiveKind.Escort&&unit==battle.ObjectiveUnit))
                        distance=GridMap.Distance(position,battle.Destination)*3;
                    float exposure=opponents.Where(e=>e.Data.BasicAttack!=null&&battle.Resolver.InRange(e,e.Data.BasicAttack,position))
                        .Sum(e=>battle.Resolver.DamagePreview(e,unit,e.Data.BasicAttack.Effects[0],e.Data.BasicAttack));
                    float movement=-distance*2-exposure*(unit.CurrentHP<unit.Stats.HP/3?0.3f:0.06f)-GridMap.Distance(origin,position)*0.05f+EnemyRoles.PositionScore(battle,unit,position);
                    if(movement>best){best=movement;result=new EnemyPlan{Destination=position,Guard=!unit.Acted};}
                    foreach(var skill in skills)
                    foreach(var aim in battle.Grid.Tiles.Keys)
                    {
                        if(!battle.Resolver.InRange(unit,skill,aim))continue;
                        var targets=battle.Resolver.Targets(unit,skill,aim).ToArray();if(targets.Length==0)continue;
                        unit.Facing=aim==position?facing:SkillResolver.Toward(position,aim);
                        float value=0;
                        foreach(var effect in skill.Effects)
                        foreach(var target in effect.AffectCaster?new[]{unit}:targets)
                            value+=Value(battle,unit,target,skill,effect);
                        value-=skill.MPCost*0.4f+battle.Resolver.HPCost(unit,skill)*0.7f+skill.GaugeCost*0.1f;
                        if(value<=0)continue;
                        float score=movement+value+targets.Max(t=>EnemyRoles.TargetScore(battle,unit,t));
                        if(score>best){best=score;result=new EnemyPlan{Destination=position,Target=targets[0],Skill=skill,Aim=aim};}
                    }
                    unit.Facing=facing;
                }
            }
            finally{battle.Grid.Place(unit,origin);unit.Facing=facing;}
            return result;
        }
        static float Value(BattleSession battle,UnitRuntime actor,UnitRuntime target,SkillData skill,SkillEffect effect)
        {
            bool ally=target.Team==actor.Team;float value=0;
            if(effect.Kind==EffectKind.Damage&&target.Alive)
            {
                int damage=battle.Resolver.DamagePreview(actor,target,effect,skill);
                value=Math.Min(damage,target.CurrentHP)+(damage>=target.CurrentHP?60:0);
                if(ally)value=-value*2;
            }
            else if(effect.Kind==EffectKind.Heal&&target.Alive&&ally)
                value=Math.Min(target.Stats.HP-target.CurrentHP,Math.Max(0,Mathf.RoundToInt(actor.Stats.MAG*effect.Power+effect.Flat)))*1.5f;
            else if(effect.Kind==EffectKind.Revive&&!target.Alive&&ally&&battle.Grid[target.Position].Occupant==null)value=target.Stats.HP*effect.Power+65;
            else if(effect.Kind==EffectKind.Cleanse&&ally)value=target.Statuses.Count(s=>s.Kind==StatusKind.Stun||s.Kind==StatusKind.Sleep||s.Kind==StatusKind.Root||s.Kind==StatusKind.Cage)*25;
            else if(effect.Kind==EffectKind.AutoRevive&&ally&&target.Alive&&!target.Has(StatusKind.AutoRevive))value=25;
            else if((effect.Kind==EffectKind.Status||effect.Kind==EffectKind.Debuff||effect.Kind==EffectKind.Buff)&&target.Alive&&!target.Has(effect.Status))
            {
                bool harmful=effect.Status==StatusKind.Stun||effect.Status==StatusKind.Sleep||effect.Status==StatusKind.Root||effect.Status==StatusKind.Cage;
                if(harmful!=ally)value=harmful?30:18;
            }
            else if(effect.Kind==EffectKind.ConsumeClaw&&ally&&!target.Has(StatusKind.ConsumeClaw))value=22;
            return value*Math.Max(0,Math.Min(1,effect.Chance));
        }
    }
}
