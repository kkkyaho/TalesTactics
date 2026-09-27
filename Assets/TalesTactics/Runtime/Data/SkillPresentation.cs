using System;
namespace TalesTactics
{
    public enum SkillVisualPattern
    {
        Automatic, Slash, Thrust, Rising, Wave, Burst, Rain, Pillar, Lightning,
        Ice, Meteor, Heal, Cleanse, Revive, Barrier, Sleep, Gravity, Claw,
        SwordFinale, TimeStop, ClawFinale, LionFinale, Radiance, Cage,
        AstralRain, FlameFinale, Wildfire, ShieldFinale
    }

    // Presentation only: pulse counts never repeat gameplay damage or consume resources.
    [Serializable]
    public sealed class SkillPresentation
    {
        public SkillVisualPattern Pattern;
        public int Pulses=1;
        public float Windup=0.18f, Recovery=0.3f, Size=1;
    }
}
