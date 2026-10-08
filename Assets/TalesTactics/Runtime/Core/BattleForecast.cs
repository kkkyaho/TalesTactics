using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TalesTactics
{
    // A private simulation graph: queries never mutate units, occupancy, resources, or combat RNG.
    public sealed class BattleProjection
    {
        public readonly GridMap Grid=new GridMap();
        public readonly Dictionary<UnitRuntime,UnitRuntime> Copies=new Dictionary<UnitRuntime,UnitRuntime>();
        public readonly SkillResolver Resolver;
        public BattleProjection(BattleSession battle)
        {
            foreach(var t in battle.Grid.Tiles.Values)Grid.Tiles.Add(t.Coordinate,new GridTile{Coordinate=t.Coordinate,Height=t.Height,MovementCost=t.MovementCost,Walkable=t.Walkable,Terrain=t.Terrain});
            foreach(var source in battle.Units)
            {
                var u=new UnitRuntime(source.Data,source.Team,source.Rules,source.Level){CurrentHP=source.CurrentHP,CurrentMP=source.CurrentMP,SpecialGauge=source.SpecialGauge,
                    Promoted=source.Promoted,Moved=source.Moved,Acted=source.Acted,CanUndoMove=source.CanUndoMove,FlamingChain=source.FlamingChain,GuardIgnition=source.GuardIgnition,ClawAttacks=source.ClawAttacks,
                    Position=source.Position,Facing=source.Facing,MoveOrigin=source.MoveOrigin,OriginalFacing=source.OriginalFacing,Growth=source.Growth,Trait=source.Trait,CampaignMastery=source.CampaignMastery,
                    UltimateTrial=source.UltimateTrial,UltimateTrialUsed=source.UltimateTrialUsed,ProtectionUsed=source.ProtectionUsed,TacticalSkills=source.TacticalSkills,
                    BossWard=source.BossWard,TacticalEnemy=source.TacticalEnemy,TurnsStarted=source.TurnsStarted,IntentTurn=source.IntentTurn,IntentPhase=source.IntentPhase,IntentSkill=source.IntentSkill,IntentAim=source.IntentAim};
                for(int i=0;i<3;i++)u.Equipment[i]=source.Equipment[i];
                foreach(var s in source.Statuses)u.Statuses.Add(new RuntimeStatus{Kind=s.Kind,Turns=s.Turns,Fresh=s.Fresh});
                foreach(var c in source.Cooldowns)u.Cooldowns.Add(c.Key,c.Value);
                Copies.Add(source,u);if(u.Alive)Grid[u.Position].Occupant=u;
            }
            Resolver=new SkillResolver(Grid,Copies.Values.ToArray(),battle.Rules,tacticalCombat:battle.TacticalCombat);
        }
    }
    public sealed class ForecastRow
    {
        public UnitRuntime Unit;
        public int BeforeHP,AfterHP;
        public string Effects,ImportantEffects;
        public int BeforeWard,AfterWard;
        public bool DirectDamage,Immune;
        public string Text=>Unit.Data.DisplayName+": HP "+BeforeHP+" → "+AfterHP+(AfterHP==0?" · 격파/전투불능":"")+(string.IsNullOrEmpty(Effects)?"":"\n"+Effects);
    }
    public sealed class BattleForecast
    {
        public readonly List<ForecastRow> Rows=new List<ForecastRow>();
        public string Cost,Note;
        public static BattleForecast Create(BattleSession battle,UnitRuntime actor,SkillData skill,Vector2Int aim,bool followup=false)
        {
            var result=new BattleForecast();var projection=new BattleProjection(battle);var u=projection.Copies[actor];
            result.Cost=$"MP {u.CurrentMP} → {u.CurrentMP-projection.Resolver.MPCost(u,skill)} · HP 비용 {projection.Resolver.HPCost(u,skill)} · SP 비용 {skill.GaugeCost}";
            var targets=battle.Resolver.Targets(actor,skill,aim).ToArray();
            if(!projection.Resolver.Execute(u,skill,aim,out var reason,followup,true)){result.Note=SkillResolver.ExplainUnavailable(reason);return result;}
            bool chance=skill.Effects.Any(e=>e.Chance<1);
            result.Note=chance?"확률 효과 제외 가정 · 실제 결과는 달라질 수 있음":"확정 피해·회복 · 현재 배치 기준";
            foreach(var original in battle.Units)
            {
                var after=projection.Copies[original];
                if(!targets.Contains(original)&&original!=actor&&after.ProtectionUsed==original.ProtectionUsed&&battle.Resolver.BossWardPercent(original)==projection.Resolver.BossWardPercent(after))continue;
                if(original==actor&&!targets.Contains(actor)&&after.CurrentHP==original.CurrentHP&&after.Statuses.Count==original.Statuses.Count)continue;
                var effects=new List<string>();
                if(original.BossWard)effects.Add("호위 방벽 "+battle.Resolver.BossWardPercent(original)+"% → "+projection.Resolver.BossWardPercent(after)+"%");
                if(targets.Contains(original)&&skill.Effects.Any(e=>e.Kind==EffectKind.Damage&&!e.Magic)&&battle.TacticalCombat)effects.Add(battle.Resolver.TacticalModifiers(actor,original));
                int mechanical=effects.Count;
                foreach(var e in skill.Effects.Where(e=>(e.AffectCaster?original==actor:targets.Contains(original))))
                {
                    if(e.Chance<1)effects.Add(SkillSummary.Effect(e)+$" {e.Chance:P0}");
                }
                foreach(var status in after.Statuses.Where(s=>!original.Has(s.Kind)||original.Statuses.First(x=>x.Kind==s.Kind).Turns!=s.Turns))effects.Add(StatusText.Name(status.Kind)+" "+status.Turns+"턴");
                foreach(var status in original.Statuses.Where(s=>!after.Has(s.Kind)))effects.Add(StatusText.Name(status.Kind)+" 해제");
                if(after.Position!=original.Position)effects.Add("위치 "+after.Position);
                if(after.ProtectionUsed&&!original.ProtectionUsed)effects.Add("인접 보호 1회 사용");
                result.Rows.Add(new ForecastRow{Unit=original,BeforeHP=original.CurrentHP,AfterHP=after.CurrentHP,Effects=string.Join(" · ",effects),ImportantEffects=string.Join(" · ",effects.Skip(mechanical)),BeforeWard=battle.Resolver.BossWardPercent(original),AfterWard=projection.Resolver.BossWardPercent(after),
                    DirectDamage=skill.Effects.Any(e=>e.Kind==EffectKind.Damage&&(e.AffectCaster?original==actor:targets.Contains(original))),
                    Immune=targets.Contains(original)&&skill.Effects.Any(e=>e.Kind==EffectKind.Damage)&&ElementalRules.Multiplier(original.Data,skill.Element)<=0});
            }
            return result;
        }
    }
    public static class ThreatMap
    {
        public static Dictionary<UnitRuntime,HashSet<Vector2Int>> Calculate(BattleSession battle)
        {
            var result=new Dictionary<UnitRuntime,HashSet<Vector2Int>>();var copy=new BattleProjection(battle);
            foreach(var original in battle.Units.Where(u=>u.Team==Team.Enemy&&u.Alive))
            {
                var cells=new HashSet<Vector2Int>();result.Add(original,cells);var u=copy.Copies[original];
                if(u.Has(StatusKind.Stun)||u.Has(StatusKind.Sleep))continue;
                if(u.IntentPhase==1){var intent=EnemyTactics.Intent(u);if(intent!=null)cells.UnionWith(copy.Resolver.AreaTiles(u,intent,u.IntentAim));continue;}
                if(u.IntentPhase==2)continue;
                u.BeginTurn();var start=u.Position;
                var skills=new[]{u.Data.BasicAttack}.Concat(u.Data.Skills).Concat(u.TacticalSkills).Concat(new[]{u.Data.UltimateSkill})
                    .Where(s=>s!=null&&s.Target==TargetType.Enemy&&copy.Resolver.CanUse(u,s)==null).Distinct().ToArray();
                var positions=copy.Grid.Reachable(u,out _).Keys.ToArray();
                foreach(var position in positions)
                {
                    copy.Grid.Place(u,position);
                    foreach(var skill in skills)
                    foreach(var aim in copy.Grid.Tiles.Keys)
                        if(copy.Resolver.InRange(u,skill,aim))cells.UnionWith(copy.Resolver.AreaTiles(u,skill,aim));
                }
                copy.Grid.Place(u,start);
            }
            return result;
        }
    }
    public static class StatusText
    {
        public static string Name(StatusKind kind)
        {
            switch(kind){case StatusKind.Guard:return "방어";case StatusKind.Stun:return "기절";case StatusKind.Sleep:return "수면";case StatusKind.Root:return "속박";case StatusKind.Cage:return "케이지";case StatusKind.ConsumeClaw:return "클로";case StatusKind.AutoRevive:return "자동 부활";case StatusKind.Song:return "성가";default:return kind.ToString();}
        }
        public static string Describe(UnitRuntime u)=>u.Statuses.Count==0?"정상":string.Join(" · ",u.Statuses.Select(s=>Name(s.Kind)+(s.Kind==StatusKind.Guard?" (다음 자기 턴까지)":" "+s.Turns+"턴")));
    }
    public static class SkillSummary
    {
        public static string Effect(SkillEffect e)=>e.Kind==EffectKind.Damage?(e.Magic?"마법 피해":"물리 피해"):e.Kind==EffectKind.Heal?"회복":e.Kind==EffectKind.Cleanse?"상태 해제":e.Kind==EffectKind.Revive?"부활":e.Kind==EffectKind.Pull?"끌어당김":e.Kind==EffectKind.Push?"밀치기":e.Kind==EffectKind.ConsumeClaw?"클로 활성":e.Kind==EffectKind.AutoRevive?"자동 부활":e.Kind==EffectKind.Gauge?"게이지 변화":e.Kind==EffectKind.Move?"이동":StatusText.Name(e.Status);
        public static string Describe(SkillData s)=>(s.Shape==SkillAreaShape.Line?"직선 관통":s.Shape==SkillAreaShape.Cone?"부채꼴":s.Area>0?"반경 "+s.Area:"단일")+" · "+string.Join("/",s.Effects.Select(Effect).Distinct());
    }
}
