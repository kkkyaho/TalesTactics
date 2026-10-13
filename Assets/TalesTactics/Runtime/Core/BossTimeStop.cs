using System.Linq;

namespace TalesTactics
{
    // These are project battle rules, not claims about the original games.
    public static class BossTimeStop
    {
        public static bool MajorId(string id)=>new[] { "dhaos", "shizel", "van", "artorius", "vholran", "ordo" }.Contains(id);
        public static bool Major(CharacterData data) => data != null && MajorId(data.Id);

        public static bool Applies(UnitRuntime unit) => unit.Team == Team.Enemy && unit.BossTimeRules && Major(unit.Data);
        public static bool Apply(UnitRuntime target, SkillData skill, SkillEffect effect)
        {
            if (skill.Id != "mint.ultimate" || effect.Status != StatusKind.Stun || !Applies(target)) return false;
            // Handled even when resisted: do not fall back to the generic stun path.
            if (target.TimeStopResistance > 0) return true;
            target.AddStatus(StatusKind.Stun, 1);
            target.IntentPhase = 0;
            target.IntentSkill = null;
            target.TimeStopResistance = 2;
            return true;
        }

        public static string Describe(UnitRuntime unit) => !Applies(unit) ? "" : unit.TimeStopResistance > 0
            ? "[시계] 재정지 저항 · 실제 행동 " + unit.TimeStopResistance + "회 후 해제"
            : "[시계] 타임 스톱: 1회 행동 정지 · 준비 공격 취소";
    }
}
