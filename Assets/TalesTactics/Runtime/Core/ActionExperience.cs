using System;
using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    public static class ExperienceRules
    {
        public const int PerLevel=100;
        public static float LevelMultiplier(int actor,int target,bool support=false)=>Mathf.Clamp(1+.2f*(target-actor),.2f,support?1.5f:2);
        public static int Award(float value,int actor,int target,bool support=false)=>value<=0?0:Mathf.Max(1,Mathf.RoundToInt(value*LevelMultiplier(actor,target,support)));
        public static void Advance(ref int level,ref int exp,int amount)
        {exp=(int)Math.Min(int.MaxValue,(long)exp+Math.Max(0,amount));while(level<50&&exp>=PerLevel){exp-=PerLevel;level++;}if(level>=50)exp=0;}
    }
    public sealed class ExperienceGain
    {
        public UnitRuntime Unit;public int Amount,BeforeLevel;
    }
    // One ledger per complete action. Effects and hits aggregate per recipient, then reactions finish before growth.
    public sealed class ActionExperience
    {
        sealed class Credit { public int Damage,Healing;public float Other;public bool Killed;public int MaxHP,Level; }
        readonly Dictionary<UnitRuntime,Credit> targets=new Dictionary<UnitRuntime,Credit>();
        readonly UnitRuntime actor;readonly SkillData skill;readonly int actorLevel;
        public ActionExperience(UnitRuntime actor,SkillData skill){this.actor=actor;this.skill=skill;actorLevel=actor.Level;}
        Credit For(UnitRuntime target)
        {if(!targets.TryGetValue(target,out var c)){c=new Credit{MaxHP=target.Stats.HP,Level=target.Level};targets.Add(target,c);}return c;}
        public void Damage(UnitRuntime target,int actual,bool killed)
        {if(actor.Team!=Team.Player||target.Team==actor.Team||actual<=0)return;var c=For(target);c.Damage+=actual;c.Killed|=killed;}
        public void Effect(UnitRuntime target,SkillEffect effect,int hp,int statuses,bool hadStatus,int gauge,Vector2Int position)
        {
            if(actor.Team!=Team.Player)return;float credit=0;
            var status=effect.Kind==EffectKind.AutoRevive?StatusKind.AutoRevive:effect.Kind==EffectKind.ConsumeClaw?StatusKind.ConsumeClaw:effect.Status;
            bool addedStatus=!hadStatus&&target.Has(status);
            if(target.Team==actor.Team)
            {
                if(effect.Kind==EffectKind.Heal&&target.CurrentHP>hp){For(target).Healing+=target.CurrentHP-hp;return;}
                if(effect.Kind==EffectKind.Revive&&hp==0&&target.Alive)credit=24;
                else if(effect.Kind==EffectKind.Cleanse&&target.Statuses.Count<statuses)credit=14;
                else if(addedStatus&&(effect.Kind==EffectKind.Status||effect.Kind==EffectKind.Buff||effect.Kind==EffectKind.AutoRevive||effect.Kind==EffectKind.ConsumeClaw)&&target.Alive)credit=10;
                else if(effect.Kind==EffectKind.Gauge&&target.SpecialGauge>gauge)credit=10;
            }
            else if(target.Alive&&(addedStatus&&(effect.Kind==EffectKind.Status||effect.Kind==EffectKind.Debuff)||target.Position!=position))credit=16;
            if(credit>0){var c=For(target);c.Other=Mathf.Max(c.Other,credit);}
        }
        public int Total(float scale=1)
        {
            if(actor.Team!=Team.Player||actor.Level>=50)return 0;
            var values=new List<int>();
            foreach(var pair in targets)
            {
                var t=pair.Key;var c=pair.Value;float value=c.Other;
                if(c.Damage>0)value=Mathf.Max(value,(skill==actor.Data.BasicAttack?12:16)*Mathf.Clamp(c.Damage/(c.MaxHP*.1f),.1f,1));
                if(c.Healing>0)value=Mathf.Max(value,Mathf.Lerp(6,18,Mathf.Clamp01(c.Healing/(c.MaxHP*.5f)))*Mathf.Min(1,c.Healing/(c.MaxHP*.05f)));
                if(c.Killed)value+=t.ExperienceBoss?40:24;
                values.Add(ExperienceRules.Award(value,actorLevel,c.Level,t.Team==actor.Team));
            }
            values.Sort((a,b)=>b.CompareTo(a));float sum=0;
            for(int i=0;i<values.Count;i++)sum+=values[i]*(i==0?1:i==1?.5f:.25f);
            return sum<=0?0:Mathf.Max(1,Mathf.RoundToInt(sum*scale));
        }
        public static ExperienceGain Grant(UnitRuntime unit,int amount)
        {
            int level=unit.Level;var stats=unit.Stats;
            if(unit.Level>=50||amount<=0)return null;
            // Record only usable EXP so reaching the cap cannot accumulate phantom rewards.
            amount=Math.Min(amount,(50-unit.Level)*100-unit.EXP);
            unit.BattleEXP+=amount;ExperienceRules.Advance(ref unit.Level,ref unit.EXP,amount);
            if(unit.Level!=level){if(unit.Alive)unit.CurrentHP+=unit.Stats.HP-stats.HP;unit.CurrentMP+=unit.Stats.MP-stats.MP;}
            return new ExperienceGain{Unit=unit,Amount=amount,BeforeLevel=level};
        }
    }
}
