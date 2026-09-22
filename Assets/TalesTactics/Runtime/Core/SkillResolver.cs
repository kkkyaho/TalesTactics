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
                var recipient=e.AffectCaster?u:t;
                string effect=e.Kind==EffectKind.Damage?"피해 "+DamagePreview(u,recipient,e):
                    e.Kind==EffectKind.Heal?"회복량 "+Mathf.RoundToInt(u.Stats.MAG*e.Power+e.Flat):EffectDescription(e);
                if(e.Kind==EffectKind.Damage||e.Kind==EffectKind.Heal)effect+=$" ({Mathf.RoundToInt(e.Chance*100)}%)";
                messages.Add((e.AffectCaster?"시전자: ":"")+effect);
            }
            return string.Join(" / ",messages);
        }
        public string Describe(UnitRuntime u,SkillData s)
        {
            string target=s.Target==TargetType.Self?"자신":s.Target==TargetType.Ally?"아군":s.Target==TargetType.FallenAlly?"전투불능 아군":"적";
            string details=$"{s.DisplayName}\nMP {s.MPCost} · HP {HPCost(u,s)} · 게이지 {s.GaugeCost}\n사거리 {s.MinRange}–{s.Range} · 범위 반경 {s.Area}\n대상 {target} · 해금 Lv{s.UnlockLevel}";
            if(s.Cooldown>0)details+=$"\n재사용 대기 {s.Cooldown}턴";
            foreach(var effect in s.Effects)details+="\n"+(effect.AffectCaster?"시전자: ":"")+EffectDescription(effect);
            if(s.StartsFlamingChain)details+="\n검술 후 Flaming Edge 연계 가능";
            if(s.IsLionHowl)details+="\n조건 충족 시 타이밍 궁극기 연계";
            return details;
        }
        static string EffectDescription(SkillEffect e)
        {
            string text;
            switch(e.Kind)
            {
                case EffectKind.Damage:text=$"{(e.Magic?"마법":"물리")} 피해 ×{e.Power:0.##}"+(e.Flat!=0?$" +{e.Flat}":"")+(e.IgnoreDefense?" · 방어 무시":"")+(e.Drain>0?$" · 흡혈 {e.Drain:P0}":"");break;
                case EffectKind.Heal:text=$"회복 MAG ×{e.Power:0.##} +{e.Flat}";break;
                case EffectKind.Revive:text=$"부활 HP {e.Power:P0} (타일이 비어 있어야 함)";break;
                case EffectKind.Cleanse:text="기절/수면/속박/케이지 해제";break;
                case EffectKind.ConsumeClaw:text=$"컨슘 클로 활성화";break;
                case EffectKind.AutoRevive:text=$"자동 부활 {e.Duration}턴";break;
                case EffectKind.Gauge:text=$"게이지 {e.Flat:+0;-0;0}";break;
                case EffectKind.Pull:text=$"끌어당김 최대 {e.Distance}칸";break;
                case EffectKind.Push:text=$"밀치기 최대 {e.Distance}칸";break;
                case EffectKind.Move:text=$"이동 최대 {e.Distance}칸";break;
                default:text=$"{e.Status} {e.Duration}턴";break;
            }
            return text+(e.Chance<1?$" · 확률 {e.Chance:P0}":"");
        }
        public static string ExplainUnavailable(string reason)
        {
            switch(reason)
            {
                case null:return "사용 가능";
                case "Action already used":return "행동을 이미 사용했거나 전투불능입니다.";
                case "Level requirement":return "해금 레벨이 부족합니다.";
                case "Not enough MP":return "MP가 부족합니다.";
                case "Not enough HP":return "HP 비용 지불 후 1 이상 남아야 합니다.";
                case "Not enough gauge":return "게이지가 부족합니다.";
                case "Cooldown":return "재사용 대기 중입니다.";
                case "Consume Claw required":return "컨슘 클로가 필요합니다.";
                case "Use another claw attack first":return "클로 상태에서 선행 공격이 필요합니다.";
                case "Guard Ignition required":return "가드 이그니션이 필요합니다.";
                case "Use a sword arte first":return "같은 턴에 검술을 먼저 사용하세요.";
                case "Chain from Lion Howl timing":return "사자전후의 타이밍 입력으로 발동합니다.";
                default:return reason;
            }
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
