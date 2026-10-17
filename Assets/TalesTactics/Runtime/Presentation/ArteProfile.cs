using UnityEngine;
namespace TalesTactics
{
    public enum ArteMotif
    {
        None, Slash, CrossSlash, RisingSlash, Thrust, Wave, Dragon, Phoenix, Lion,
        Martial, Claw, Arrow, ArrowRain, Rifle, Grenade, Shield, Earth, Water, Wind,
        Ice, Fire, Lightning, Prism, Heal, Cleanse, Revive, Barrier, Sleep, Song,
        Gravity, Judgment, Clock, Cage, FlameFinale, Radiance, Hammer, Meteor
    }
    public enum ArteMotion { Attack, Cast, Jump, Dash, Blink, Guard, Shoot, Spin }

    // Immutable presentation data. Counts describe visual accents, never combat hits.
    public readonly struct ArteProfile
    {
        public readonly string Id;
        public readonly ArteMotif Motif, Accent;
        public readonly ArteMotion Motion;
        public readonly int Count, Variant;
        public readonly float Windup, Travel, Recovery, Spread, Angle;
        public readonly Color Tint;
        public readonly bool Ultimate;
        public bool Enabled=>Motif!=ArteMotif.None;
        public bool IsSpell=>Motion==ArteMotion.Cast;

        public ArteProfile(string id, ArteMotif motif, ArteMotion motion=ArteMotion.Attack,
            int count=1, int variant=0, float windup=.45f, float travel=0, float recovery=.85f,
            float spread=.6f, float angle=0, Color? tint=null, ArteMotif accent=ArteMotif.None,
            bool ultimate=false)
        {
            Id=id;Motif=motif;Accent=accent;Motion=motion;Count=count;Variant=variant;
            Windup=windup;Travel=travel;Recovery=recovery;Spread=spread;Angle=angle;
            Tint=tint??Color.white;Ultimate=ultimate;
        }
    }
}
