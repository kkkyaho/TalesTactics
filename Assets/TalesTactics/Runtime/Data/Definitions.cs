using System;
using UnityEngine;

namespace TalesTactics
{
    public enum Facing { Front, Back, Left, Right }
    public enum Team { Player, Enemy }
    public enum TerrainType { Normal, Water, Obstacle, HighGround, LowGround }
    public enum WeaponType { Sword, Staff, Claw, Fist, Spear, Bow, Gun, Shield }
    public enum AnimationKind { Idle, Walk, Attack, Skill, Cast, Guard, Damage, Dead, Ultimate }
    public enum Element { None, Fire, Water, Wind, Earth, Light, Dark, Lightning, Ice }
    public enum TargetType { Enemy, Ally, Self, FallenAlly }
    public enum EffectKind { Damage, Heal, Buff, Debuff, Push, Pull, Revive, Status, Move, Cleanse, AutoRevive, ConsumeClaw, Gauge }
    public enum StatusKind { None, Stun, Sleep, Root, Song, Cage, Guard, AutoRevive, ConsumeClaw }
    public enum SkillGate { None, Claw, Nightmare, ClawFinisher, FlamingEdge, GuardIgnition, FarahTiming }
    public enum EquipmentSlot { Weapon, Armor, Accessory }

    [Serializable]
    public struct Stats
    {
        public int HP, MP, STR, MAG, DEF, MDF, SPD, MOV, JMP;
        public static Stats operator +(Stats a, Stats b) => new Stats { HP=a.HP+b.HP, MP=a.MP+b.MP, STR=a.STR+b.STR, MAG=a.MAG+b.MAG, DEF=a.DEF+b.DEF, MDF=a.MDF+b.MDF, SPD=a.SPD+b.SPD, MOV=a.MOV+b.MOV, JMP=a.JMP+b.JMP };
        public static Stats operator *(Stats a, int n) => new Stats { HP=a.HP*n, MP=a.MP*n, STR=a.STR*n, MAG=a.MAG*n, DEF=a.DEF*n, MDF=a.MDF*n, SPD=a.SPD*n, MOV=a.MOV*n, JMP=a.JMP*n };
    }
    [Serializable]
    public class DirectionalSprites
    {
        public Sprite Front, Back, Left, Right;
        public Sprite Get(Facing f) => f == Facing.Back ? Back : f == Facing.Left ? Left : f == Facing.Right ? Right : Front;
    }
    [Serializable]
    public class SkillEffect
    {
        public EffectKind Kind;
        public StatusKind Status;
        public bool Magic, IgnoreDefense, AffectCaster;
        public float Power=1, Chance=1, Drain;
        public int Flat, Duration=2, Distance=1;
    }
}
