using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public enum BossMechanic { None, Summon, DrainMP, CloneNova, LockFacing, CoreRecharge, Replay, Mend, FinalStop,
        FlameField, Sanctuary, Collapse, Chivalry, Counter, GaugeSeal, Command, Cooperate, HighWater, MarkedStrike, IceTrail, Mines, Boundary, Contract }
    public static class CrossoverBossTactics
    {
        public static int MaxPhase(UnitRuntime unit)=>unit.Data.Id=="ordo"?3:BossTimeStop.Major(unit.Data)||unit.Data.Id=="executor"?2:1;
        public static int ClampDamage(UnitRuntime target,int amount)
        {
            if(!target.CrossoverBoss||!target.Alive)return amount;
            if(target.BossPhaseLock)return 0;
            int phases=MaxPhase(target);
            if(target.BossPhase<phases)
                return Math.Max(0,Math.Min(amount,target.CurrentHP-target.Stats.HP*(phases-target.BossPhase)/phases));
            return amount;
        }
        public static void AfterDamage(UnitRuntime target)
        {
            if(!target.CrossoverBoss)return;
            if(target.IntentPhase==1&&EnemyTactics.Intent(target)?.BossMechanic==BossMechanic.CoreRecharge)
            {target.IntentPhase=2;target.IntentTurn=target.TurnsStarted-1;}
            int phases=MaxPhase(target);
            if(target.BossPhase<phases&&target.CurrentHP<=target.Stats.HP*(phases-target.BossPhase)/phases)
            {target.BossPhase++;target.BossPhaseLock=true;target.IntentPhase=0;target.IntentSkill=null;}
        }
        public static string Description(UnitRuntime unit)
        {
            string phase="단계 "+unit.BossPhase+" / "+MaxPhase(unit)+(unit.BossPhaseLock?" · 전환 보호 (다음 실제 행동까지)":"");
            string intent=unit.IntentPhase==1?"[!] "+(EnemyTactics.Intent(unit)?.DisplayName??"공격")+" → "+unit.IntentAim+" · 다음 자기 행동 발동\n범위 이탈 · 기절/수면으로 준비 취소":unit.IntentPhase==2?"[◇] 핵/빈틈 노출 · 받는 피해 +25%":"[○] 다음 행동에 공격 예고";
            return phase+"\n"+intent+(BossTimeStop.Applies(unit)?"\n"+BossTimeStop.Describe(unit):"");
        }
        public static EnemyPlan Plan(BattleSession battle,UnitRuntime unit)
        {
            if(!unit.Alive||unit.Has(StatusKind.Stun)||unit.Has(StatusKind.Sleep)||unit.Data.Id=="boundary-device")return new EnemyPlan{Destination=unit.Position};
            if(unit.IntentPhase==1)
                return unit.TurnsStarted<=unit.IntentTurn?new EnemyPlan{Destination=unit.Position,Guard=true}:new EnemyPlan{Destination=unit.Position,Skill=EnemyTactics.Intent(unit),Aim=unit.IntentAim};
            if(unit.IntentPhase==2)return new EnemyPlan{Destination=unit.Position};
            var skills=unit.Data.Skills.Where(s=>s!=null&&s.Id.StartsWith("boss."+unit.Data.Id+".",StringComparison.Ordinal)).ToArray();
            if(skills.Length!=3)throw new InvalidOperationException("Boss requires three authored patterns: "+unit.Data.Id);
            if(unit.Data.Id=="hyades"&&unit.Data.CrossoverEarlyForm)skills=new[]{skills[0]};
            if(unit.Data.Id=="golem"&&battle.CampaignStage==6)skills=skills.Where(s=>s.BossMechanic!=BossMechanic.Collapse).ToArray();
            var skill=skills[(unit.PatternStep+unit.BossPhase-1)%skills.Length];
            // A canceled nova does not leave the selector stuck on an unusable skill.
            if(battle.Resolver.CanUse(unit,skill)!=null)
                skill=skills.FirstOrDefault(s=>battle.Resolver.CanUse(unit,s)==null)??skill;
            var opponents=battle.Units.Where(u=>u.Alive&&!u.CrossoverReserve&&u.Team!=unit.Team).ToArray();var origin=unit.Position;
            var positions=unit.Moved?new[]{origin}:battle.Grid.Reachable(unit,out _).Keys.OrderBy(p=>GridMap.Distance(origin,p)).ThenBy(p=>p.x).ThenBy(p=>p.y).ToArray();
            var result=new EnemyPlan{Destination=origin,Guard=true};float best=float.NegativeInfinity;
            try
            {
                foreach(var p in positions)
                {
                    if(!battle.Grid.Place(unit,p))continue;
                    var aims=skill.Target==TargetType.Self?new[]{p}:opponents.Select(u=>u.Position).ToArray();
                    foreach(var aim in aims)
                    {
                        if(!battle.Resolver.InRange(unit,skill,aim)||battle.Resolver.CanUse(unit,skill)!=null)continue;
                        if(unit.Data.Id=="rassius"&&skill.Target!=TargetType.Self&&battle.Crossover?.UsesNodes==true&&battle.Resolver.AreaTiles(unit,skill,aim).Any(tile=>CrossoverBattle.Nodes.Where((node,i)=>(battle.Crossover.State.Nodes&(1<<i))==0).Any(node=>GridMap.Distance(node,tile)<=1)))continue;
                        float score=battle.Resolver.Targets(unit,skill,aim).Count()*100-GridMap.Distance(origin,p);
                        if(score>best){best=score;result=new EnemyPlan{Destination=p,PrepareSkill=skill,PrepareAim=aim,Guard=true};}
                    }
                }
            }
            finally{battle.Grid.Place(unit,origin);}
            if(result.PrepareSkill==null&&opponents.Length>0)
                result.Destination=positions.OrderBy(p=>opponents.Min(t=>GridMap.Distance(p,t.Position))).ThenBy(p=>GridMap.Distance(origin,p)).First();
            return result;
        }
        public static void Resolve(UnitRuntime actor,SkillData skill,Vector2Int aim,GridMap grid,IReadOnlyList<UnitRuntime> units,CrossoverBattleState state=null)
        {
            if(!actor.CrossoverBoss)return;
            switch(skill.BossMechanic)
            {
                case BossMechanic.Summon:
                    Summon(actor,grid,units);
                    break;
                case BossMechanic.DrainMP:
                    foreach(var target in units.Where(u=>u.Team!=actor.Team&&u.Alive&&GridMap.Distance(u.Position,aim)<=1&&GridMap.Distance(u.Position,actor.Position)<=4))
                    {int drain=Math.Min(15,target.CurrentMP);target.CurrentMP-=drain;actor.CurrentMP=Math.Min(actor.Stats.MP,actor.CurrentMP+drain);}
                    break;
                case BossMechanic.LockFacing:
                    actor.AddStatus(StatusKind.Root,2);
                    break;
                case BossMechanic.Command:
                    if(units.Any(u=>u.Data.Id=="boundary-device"&&u.Alive))
                        foreach(var target in units.Where(u=>u.Team==actor.Team&&u.Alive&&GridMap.Distance(u.Position,actor.Position)<=4))target.AddStatus(StatusKind.Guard,2);
                    break;
                case BossMechanic.Mend:
                    var broken=state==null||state.BrokenDevices.Count==0?null:units[state.BrokenDevices.Last()];
                    if(broken!=null&&!broken.Alive&&grid[broken.Position]?.Occupant==null){broken.CurrentHP=Math.Max(1,broken.Stats.HP/2);grid.Place(broken,broken.Position);state.RecordDevice(broken,units);}
                    break;
            }
        }
        public static void Prepare(BattleSession battle,UnitRuntime actor,EnemyPlan plan)
        {
            if(battle!=null&&plan.PrepareSkill?.BossMechanic==BossMechanic.CloneNova)Summon(actor,battle.Grid,battle.Units,3);
            if(battle?.Crossover!=null&&plan.PrepareSkill?.BossMechanic==BossMechanic.Replay)
            {var state=battle.Crossover.State;state.ReplayShape=state.LastShape;state.ReplayArea=state.LastArea;}
        }
        static void Summon(UnitRuntime actor,GridMap grid,IReadOnlyList<UnitRuntime> units,int maximum=2)
        {
            if(!units.Any(u=>u.Team==actor.Team&&u.Data.Id=="boundary-device"&&u.Alive))return;
            if(actor.Data.Id=="jahmir"||actor.Data.Id=="demitel"&&actor.CurrentHP<actor.Stats.HP/2)maximum=1;
            var enemies=units.Where(u=>u.Team==actor.Team).ToArray();int alive=enemies.Count(u=>u.Summoned&&u.Alive);
            foreach(var minion in enemies.Where(u=>u.Summoned&&!u.Alive&&!u.CrossoverReserve))
            {
                if(alive>=maximum)break;
                var p=CrossoverCampaign.EnemySpawn(Array.IndexOf(enemies,minion));
                if(grid[p]?.Walkable!=true||grid[p].Occupant!=null)continue;
                minion.CurrentHP=Math.Max(1,minion.Stats.HP/(actor.Data.Id=="ganabelt"?4:2));minion.CurrentMP=minion.Stats.MP;
                minion.Statuses.Clear();minion.IntentPhase=0;minion.IntentSkill=null;minion.CounterUsed=false;minion.SupportUsed=false;
                grid.Place(minion,p);alive++;
            }
        }
    }
}
