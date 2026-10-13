using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed class ReactionStrike
    {
        public UnitRuntime Actor,Target;
        public SkillData Skill;
        public string Kind;
        public int Damage,BeforeHP,AfterHP,Experience;
        public string Text=>Kind+" · "+Actor.Data.DisplayName+" → "+Target.Data.DisplayName+" "+Damage;
    }
    public sealed partial class SkillResolver
    {
        public readonly bool ReactionsEnabled;
        public Dictionary<UnitRuntime,int> PrimaryHealth {get;private set;}
        public readonly List<ReactionStrike> LastReactions=new List<ReactionStrike>();
        bool ReactionReady(UnitRuntime actor,UnitRuntime target)
        {
            var skill=actor.Data.BasicAttack;
            return actor.CanTakeTurn&&target.Alive&&!target.CrossoverReserve&&actor.Team!=target.Team&&!actor.Has(StatusKind.Stun)&&!actor.Has(StatusKind.Sleep)&&
                skill!=null&&skill.Target==TargetType.Enemy&&skill.Area==0&&skill.Shape==SkillAreaShape.Diamond&&
                skill.Effects.Any(e=>e.Kind==EffectKind.Damage&&!e.AffectCaster)&&InRange(actor,skill,target.Position);
        }
        void ResolveReactions(UnitRuntime actor,SkillData skill,UnitRuntime[] targets,bool forecast)
        {
            if(actor.Alive&&skill.Effects.Any(e=>e.Kind==EffectKind.Damage))
            {
                foreach(var defender in targets.Where(t=>t.Alive&&t.CrossoverBoss&&!t.CounterUsed&&t.IntentPhase==2&&EnemyTactics.Intent(t)?.BossMechanic==BossMechanic.Counter))
                {
                    bool front=DirectionMultiplier(actor,defender)==rules.FrontMultiplier;
                    if(defender.Data.Id=="asch"&&(!front||GridMap.Distance(actor.Position,defender.Position)!=1))continue;
                    defender.CounterUsed=true;
                    if(ReactionReady(defender,actor))ReactionHit(defender,actor,1.2f,"예고 반격",forecast);
                }
                if(skill.BossMechanic==BossMechanic.Cooperate&&actor.CrossoverBoss)
                {
                    var victim=targets.FirstOrDefault(t=>t.Alive);
                    var escort=victim==null?null:units.FirstOrDefault(u=>u!=actor&&u.Team==actor.Team&&!u.SupportUsed&&!u.CrossoverBoss&&ReactionReady(u,victim));
                    if(escort!=null){escort.SupportUsed=true;ReactionHit(escort,victim,.7f,"돌격 협동",forecast);}
                }
            }
            if(!ReactionsEnabled||!actor.Alive||skill.Target!=TargetType.Enemy||skill.Area!=0||skill.Shape!=SkillAreaShape.Diamond||targets.Length!=1||
                !skill.Effects.Any(e=>e.Kind==EffectKind.Damage&&!e.AffectCaster&&e.Chance>=1))return;
            var target=targets[0];if(!target.Alive)return;
            var support=units.FirstOrDefault(u=>u!=actor&&u.Team==actor.Team&&!u.SupportUsed&&
                (GridMap.Distance(u.Position,actor.Position)==1||GridMap.Distance(u.Position,target.Position)==1)&&ReactionReady(u,target));
            if(support!=null){support.SupportUsed=true;ReactionHit(support,target,.5f,"지원",forecast);}
            if(actor.Alive&&target.Alive&&!target.CrossoverBoss&&!target.CounterUsed&&ReactionReady(target,actor))
            {target.CounterUsed=true;ReactionHit(target,actor,.75f,"반격",forecast);}
        }
        void ReactionHit(UnitRuntime actor,UnitRuntime target,float power,string kind,bool forecast)
        {
            var skill=actor.Data.BasicAttack;var experience=ActionExperienceEnabled?new ActionExperience(actor,skill):null;int damage=0,before=target.CurrentHP;
            foreach(var effect in skill.Effects.Where(e=>e.Kind==EffectKind.Damage&&!e.AffectCaster))
            {
                if(!target.Alive|| (forecast?effect.Chance<1:NextRoll()>effect.Chance))continue;
                int hit=DamagePreview(actor,target,effect,skill);hit=hit==0?0:Mathf.Max(1,Mathf.RoundToInt(hit*power));
                var protector=Protector(target);if(hit>0&&protector!=null)protector.ProtectionUsed=true;
                if(target.Has(StatusKind.Guard)&&DirectionMultiplier(actor,target)!=rules.RearMultiplier)target.GuardIgnition=true;
                int actual=Mathf.Min(hit,target.CurrentHP);damage+=actual;target.Damage(hit,grid);experience?.Damage(target,actual,!target.Alive);
                CrossoverState?.RecordDevice(target,units);
                if(target.Alive&&target.Data.Id=="kisara"&&target.Has(StatusKind.Guard))LastKisaraGuardedDamage+=actual;
                if(SignatureWeaponRules.Has(actor,EquipmentEffect.UnboundBlade))actor.CurrentHP=Mathf.Min(actor.Stats.HP,actor.CurrentHP+Mathf.RoundToInt(actual*.1f));
            }
            // Reactions never spend the normal action, MP, cooldowns, chains or gauge; they cannot chain reactions.
            LastReactions.Add(new ReactionStrike{Actor=actor,Target=target,Skill=skill,Kind=kind,Experience=experience?.Total(.5f)??0,Damage=damage,BeforeHP=before,AfterHP=target.CurrentHP});
        }
    }
}
