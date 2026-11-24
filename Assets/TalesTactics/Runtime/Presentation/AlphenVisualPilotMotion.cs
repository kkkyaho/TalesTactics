using UnityEngine;
namespace TalesTactics
{
    // Retain the original script GUID and resource path so the pilot asset, saved
    // sprite references and existing before/after comparisons remain compatible.
    [CreateAssetMenu(menuName="Tales Tactics/Alphen Visual Pilot Motion")]
    public sealed class AlphenVisualPilotMotion:CharacterMotionProfile
    {
        public const string ResourcePath="TalesTactics/VisualPilot/AlphenMotion";
        public override string OwnerId=>"alphen";
    }
}
