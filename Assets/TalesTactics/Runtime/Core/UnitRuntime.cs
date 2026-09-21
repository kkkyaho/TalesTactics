using System;
using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    [Serializable] public class RuntimeStatus { public StatusKind Kind; public int Turns; public bool Fresh; }
    public sealed class UnitRuntime
    {
        public readonly CharacterData Data;
        public readonly Team Team;
        public readonly BattleRules Rules;
        public int Level, CurrentHP, CurrentMP, SpecialGauge, ClawAttacks;
        public bool Promoted, Moved, Acted, CanUndoMove, FlamingChain, GuardIgnition;
        public Vector2Int Position, MoveOrigin;
        public Facing Facing=Facing.Front;
        public Facing OriginalFacing;
        public readonly List<RuntimeStatus> Statuses=new List<RuntimeStatus>();
        public readonly Dictionary<string,int> Cooldowns=new Dictionary<string,int>();
        public readonly EquipmentData[] Equipment=new EquipmentData[3];
        public bool Alive=>CurrentHP>0;
        public Stats Stats
        {
            get
            {
                var s=Data.StatsAt(Level,Promoted); foreach(var e in Equipment)if(e!=null)s+=e.Bonus;
                if(Has(StatusKind.ConsumeClaw)){s.STR=Mathf.RoundToInt(s.STR*Rules.ClawStrength);s.SPD=Mathf.RoundToInt(s.SPD*Rules.ClawSpeed);}
                if(Has(StatusKind.Song)){s.STR=Mathf.RoundToInt(s.STR*Rules.SongMultiplier);s.MAG=Mathf.RoundToInt(s.MAG*Rules.SongMultiplier);}
                if(Has(StatusKind.Cage)){s.SPD=Mathf.Max(1,Mathf.RoundToInt(s.SPD*Rules.CageSpeedMultiplier));s.MDF=Mathf.RoundToInt(s.MDF*Rules.CageMagicDefenseMultiplier);}
                return s;
            }
        }
        public UnitRuntime(CharacterData data,Team team,BattleRules rules,int level=1)
        { Data=data;Team=team;Rules=rules;Level=Mathf.Clamp(level,1,50);CurrentHP=Stats.HP;CurrentMP=Stats.MP; }
        public bool Has(StatusKind kind)=>Statuses.Exists(s=>s.Kind==kind&&s.Turns>0);
        public void AddStatus(StatusKind kind,int turns)
        {var s=Statuses.Find(x=>x.Kind==kind);if(s==null)Statuses.Add(new RuntimeStatus{Kind=kind,Turns=turns});else{s.Turns=Mathf.Max(s.Turns,turns);s.Fresh=false;}}
        public bool Unlocked(SkillData s)=>s!=null&&Level>=s.UnlockLevel;
        public void BeginTurn()
        {
            Moved=Acted=CanUndoMove=FlamingChain=false;
            Statuses.RemoveAll(s=>s.Kind==StatusKind.Guard);
            foreach(var key in new List<string>(Cooldowns.Keys))if(--Cooldowns[key]<=0)Cooldowns.Remove(key);
        }
        public void EndTurn()
        {
            foreach(var s in Statuses){if(s.Fresh)s.Fresh=false;else s.Turns--;}
            Statuses.RemoveAll(s=>s.Turns<=0);
            if(!Has(StatusKind.ConsumeClaw))ClawAttacks=0;
        }
        public void Damage(int amount,GridMap grid)
        {
            CurrentHP=Mathf.Max(0,CurrentHP-Mathf.Max(0,amount));Statuses.RemoveAll(s=>s.Kind==StatusKind.Sleep);
            if(!Alive&&Has(StatusKind.AutoRevive)){Statuses.RemoveAll(s=>s.Kind==StatusKind.AutoRevive);CurrentHP=Mathf.Max(1,Mathf.RoundToInt(Stats.HP*Rules.AutoReviveHealth));}
            if(!Alive){Statuses.Clear();if(grid[Position]?.Occupant==this)grid[Position].Occupant=null;}
        }
    }
}
