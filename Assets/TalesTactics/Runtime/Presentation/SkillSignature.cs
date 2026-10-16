using UnityEngine;
namespace TalesTactics
{
    // Per-skill choreography is presentation data only; combat and content assets stay authoritative.
    public enum SkillSignature { None, DemonFang, MeteorStorm, ThunderBlade }
    public static class SkillSignatures
    {
        public static SkillSignature Resolve(SkillData skill)
        {
            switch(skill?.Id)
            {
                case "cless.0":return SkillSignature.DemonFang;
                case "jade.6":return SkillSignature.MeteorStorm;
                case "jade.3":return SkillSignature.ThunderBlade;
                default:return SkillSignature.None;
            }
        }
        public static bool IsSpell(SkillSignature signature)=>signature==SkillSignature.MeteorStorm||signature==SkillSignature.ThunderBlade;
        public static float Windup(SkillSignature signature)=>signature==SkillSignature.DemonFang?.4f:IsSpell(signature)?.65f:0;
        public static float Travel(SkillSignature signature)
        {
            switch(signature)
            {
                case SkillSignature.DemonFang:return .35f;
                case SkillSignature.MeteorStorm:return .6f;
                case SkillSignature.ThunderBlade:return .4f;
                default:return 0;
            }
        }
        public static float Recovery(SkillSignature signature)=>signature==SkillSignature.DemonFang?.65f:IsSpell(signature)?1.1f:0;
    }
}
