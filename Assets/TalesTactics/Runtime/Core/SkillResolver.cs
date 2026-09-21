using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed class SkillResolver
    {
        readonly GridMap grid;
        readonly IReadOnlyList<UnitRuntime> units;
        readonly BattleRules rules;
        readonly System.Random random;
        public SkillResolver(GridMap grid,IReadOnlyList<UnitRuntime> units,BattleRules rules,int seed=17){this.grid=grid;this.units=units;this.rules=rules;random=new System.Random(seed);}
        public string CanUse(UnitRuntime u,SkillData s,bool followup=false)
        {
            if(s==null)return "No skill";
            if(!u.Alive||(!followup&&u.Acted))return "Action already used";
            if(!u.Unlocked(s))return "Level requirement";
            if(u.CurrentMP<s.MPCost)return "Not enough MP";
            if(u.CurrentHP<=HPCost(u,s))return "Not enough HP";
            if(u.SpecialGauge<s.GaugeCost)return "Not enough gauge";
            if(u.Cooldowns.ContainsKey(s.Id))return "Cooldown";
            if((s.Gate==SkillGate.Claw||s.Gate==SkillGate.Nightmare||s.Gate==SkillGate.ClawFinisher)&&!u.Has(StatusKind.ConsumeClaw))return "Consume Claw required";
            if((s.Gate==SkillGate.Nightmare||s.Gate==SkillGate.ClawFinisher)&&u.ClawAttacks<1)return "Use another claw attack first";
            if(s.Gate==SkillGate.GuardIgnition&&!u.GuardIgnition)return "Guard Ignition required";
            if(s.Gate==SkillGate.FlamingEdge&&!u.FlamingChain)return "Use a sword arte first";
            if(s.Gate==SkillGate.FarahTiming&&!followup)return "Chain from Lion Howl timing";
            return null;
        }
        public int HPCost(UnitRuntime u,SkillData s)=>s.HPCost+Mathf.CeilToInt(u.Stats.HP*s.HPPercentCost);
        public bool ValidTarget(UnitRuntime u,SkillData s,UnitRuntime t)
        {
            if(t==null)return false;
            return s.Target==TargetType.Self?t==u&&t.Alive:s.Target==TargetType.FallenAlly?t.Team==u.Team&&!t.Alive:s.Target==TargetType.Ally?t.Team==u.Team&&t.Alive:t.Team!=u.Team&&t.Alive;
        }
        public bool InRange(UnitRuntime u,SkillData s,Vector2Int p)
        {int d=GridMap.Distance(u.Position,p);return grid[p]!=null&&d>=s.MinRange&&d<=s.Range;}
        public IEnumerable<UnitRuntime> Targets(UnitRuntime u,SkillData s,Vector2Int p)=>units.Where(t=>ValidTarget(u,s,t)&&GridMap.Distance(t.Position,p)<=s.Area);
        public float DirectionMultiplier(UnitRuntime attacker,UnitRuntime defender)
        {
            var d=attacker.Position-defender.Position;
            var f=FacingVector(defender.Facing);int dot=d.x*f.x+d.y*f.y;
            return dot>0?rules.FrontMultiplier:dot<0?rules.RearMultiplier:rules.SideMultiplier;
        }
        public int DamagePreview(UnitRuntime u,UnitRuntime t,SkillEffect e)
        {
            var a=u.Stats;var b=t.Stats;
            float defense=e.IgnoreDefense?0:(e.Magic?b.MDF:b.DEF)*rules.DefenseFactor;
            float value=(e.Magic?a.MAG:a.STR)*e.Power+e.Flat-defense;
            value*=DirectionMultiplier(u,t);
            if(u.GuardIgnition&&u.Data.Id=="kisara")value*=rules.IgnitionMultiplier;
            if(t.Has(StatusKind.Guard))
            {
                var delta=u.Position-t.Position;var facing=FacingVector(t.Facing);int dot=delta.x*facing.x+delta.y*facing.y;
                value*=dot>0?rules.GuardFront:dot<0?rules.GuardRear:rules.GuardSide;
            }
            return Mathf.Max(1,Mathf.RoundToInt(value));
        }
        public string Preview(UnitRuntime u,SkillData s,UnitRuntime t)
        {
            var messages=new List<string>();
            foreach(var e in s.Effects)
            {
                if(e.Kind==EffectKind.Damage)messages.Add("Damage "+DamagePreview(u,t,e)+" · Hit 100%");
                if(e.Kind==EffectKind.Heal)messages.Add("Heal "+Mathf.RoundToInt(u.Stats.MAG*e.Power+e.Flat));
                if(e.Kind==EffectKind.Status)messages.Add(e.Status+" "+Mathf.RoundToInt(e.Chance*100)+"%");
            }
            return string.Join(" / ",messages);
        }
        public bool Execute(UnitRuntime u,SkillData s,Vector2Int p,out string message,bool followup=false)
        {
            message=CanUse(u,s,followup);if(message!=null)return false;
            var targets=Targets(u,s,p).ToArray();
            if(!InRange(u,s,p)||targets.Length==0){message="Invalid target";return false;}
            if(s.Target==TargetType.FallenAlly&&targets.All(t=>grid[t.Position].Occupant!=null)){message="Revival tile is occupied";return false;}
            u.CurrentMP-=s.MPCost;u.CurrentHP-=HPCost(u,s);u.SpecialGauge-=s.GaugeCost;
            if(s.Cooldown>0)u.Cooldowns[s.Id]=s.Cooldown+1;
            u.Acted=true;u.CanUndoMove=false;
            bool attack=s.Effects.Any(e=>e.Kind==EffectKind.Damage);
            foreach(var e in s.Effects)
            {
                var recipients=e.AffectCaster?new[]{u}:targets;
                foreach(var t in recipients)
                {
                    if(random.NextDouble()>e.Chance)continue;
                    if(e.Kind==EffectKind.Damage&&t.Alive)
                    {
                        int damage=DamagePreview(u,t,e);int actual=Mathf.Min(damage,t.CurrentHP);
                        if(t.Has(StatusKind.Guard)&&DirectionMultiplier(u,t)!=rules.RearMultiplier)t.GuardIgnition=true;
                        t.Damage(damage,grid);u.CurrentHP=Mathf.Min(u.Stats.HP,u.CurrentHP+Mathf.RoundToInt(actual*e.Drain));
                    }
                    else if(e.Kind==EffectKind.Heal&&t.Alive)t.CurrentHP=Mathf.Min(t.Stats.HP,t.CurrentHP+Mathf.RoundToInt(u.Stats.MAG*e.Power+e.Flat));
                    else if(e.Kind==EffectKind.Revive&&!t.Alive&&grid[t.Position].Occupant==null){t.CurrentHP=Mathf.Max(1,Mathf.RoundToInt(t.Stats.HP*e.Power));grid.Place(t,t.Position);}
                    else if(e.Kind==EffectKind.Cleanse)t.Statuses.RemoveAll(x=>x.Kind==StatusKind.Stun||x.Kind==StatusKind.Sleep||x.Kind==StatusKind.Root||x.Kind==StatusKind.Cage);
                    else if(e.Kind==EffectKind.ConsumeClaw){t.AddStatus(StatusKind.ConsumeClaw,rules.ClawDuration);t.Statuses.Find(x=>x.Kind==StatusKind.ConsumeClaw).Fresh=true;t.ClawAttacks=0;}
                    else if(e.Kind==EffectKind.AutoRevive&&t.Alive)t.AddStatus(StatusKind.AutoRevive,e.Duration);
                    else if((e.Kind==EffectKind.Status||e.Kind==EffectKind.Buff||e.Kind==EffectKind.Debuff)&&t.Alive)t.AddStatus(e.Status,e.Duration);
                    else if(e.Kind==EffectKind.Gauge)t.SpecialGauge=Mathf.Clamp(t.SpecialGauge+e.Flat,0,100);
                    else if((e.Kind==EffectKind.Pull||e.Kind==EffectKind.Push)&&t.Alive)Displace(t,e.Kind==EffectKind.Pull?p:u.Position,e.Distance,e.Kind==EffectKind.Push);
                    else if(e.Kind==EffectKind.Move&&t.Alive)Displace(t,p,e.Distance,false);
                }
            }
            if(attack){u.SpecialGauge=Mathf.Clamp(u.SpecialGauge+rules.GaugePerAttack,0,100);if(u.Has(StatusKind.ConsumeClaw))u.ClawAttacks++;}
            if(u.Data.Id=="kisara"&&attack)u.GuardIgnition=false;
            u.FlamingChain=s.StartsFlamingChain;
            message=u.Data.DisplayName+" : "+s.DisplayName;
            return true;
        }
        void Displace(UnitRuntime u,Vector2Int center,int distance,bool away)
        {
            for(int i=0;i<distance;i++)
            {
                var delta=center-u.Position;if(away)delta=-delta;if(delta==Vector2Int.zero)break;
                var step=Mathf.Abs(delta.x)>=Mathf.Abs(delta.y)?new Vector2Int(Math.Sign(delta.x),0):new Vector2Int(0,Math.Sign(delta.y));
                var to=grid[u.Position+step];if(!grid.CanEnter(u,grid[u.Position],to))break;grid.Place(u,to.Coordinate);
            }
        }
        public static Vector2Int FacingVector(Facing f)=>f==Facing.Front?Vector2Int.down:f==Facing.Back?Vector2Int.up:f==Facing.Left?Vector2Int.left:Vector2Int.right;
        public static Facing Toward(Vector2Int from,Vector2Int to)
        {var d=to-from;return Mathf.Abs(d.x)>Mathf.Abs(d.y)?(d.x>0?Facing.Right:Facing.Left):(d.y>0?Facing.Back:Facing.Front);}
    }
}
