using UnityEngine;
namespace TalesTactics
{
    [CreateAssetMenu(menuName="Tales Tactics/Rules")]
    public class BattleRules : ScriptableObject
    {
        public float HeightDamagePerStep=0.1f;
        public int HeightDamageMaxSteps=2;
        public float FrontMultiplier=1, SideMultiplier=1.2f, RearMultiplier=1.5f;
        public float GuardFront=0.35f, GuardSide=0.7f, GuardRear=1;
        public float DefenseFactor=0.6f, ClawStrength=1.2f, ClawSpeed=1.15f, IgnitionMultiplier=1.25f;
        public float SongMultiplier=1.15f, CageSpeedMultiplier=0.75f, CageMagicDefenseMultiplier=0.75f, AutoReviveHealth=0.5f;
        public int GaugePerAttack=20, ClawDuration=3, MaxDeployment=6;
        public float TileHeight=0.5f, StepSeconds=0.13f, ActionSeconds=0.3f;
        public float TimingWindowStart=0.4f, TimingWindowEnd=0.75f, TimingDuration=1.1f;
    }
}
