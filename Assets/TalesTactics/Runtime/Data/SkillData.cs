using UnityEngine;
namespace TalesTactics
{
    [CreateAssetMenu(menuName="Tales Tactics/Skill")]
    public class SkillData : ScriptableObject
    {
        public string Id, DisplayName;
        public int MPCost, HPCost, MinRange=1, Range=1, Area, UnlockLevel=1, Cooldown, GaugeCost;
        [Range(0,1)] public float HPPercentCost;
        public Element Element;
        public TargetType Target;
        public AnimationKind Animation=AnimationKind.Skill;
        public SkillGate Gate;
        public bool IsUltimate, StartsFlamingChain, IsLionHowl;
        public SkillEffect[] Effects = new SkillEffect[0];
    }
}
