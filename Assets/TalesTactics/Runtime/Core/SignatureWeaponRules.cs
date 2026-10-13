using System.Linq;
namespace TalesTactics
{
    // Original adaptation effects. Original-game weapon names are documented separately.
    public static class SignatureWeaponRules
    {
        public static bool Has(UnitRuntime u,EquipmentEffect effect)=>u.Equipment.Any(e=>e!=null&&e.Effect==effect&&EquipmentLoadout.CanEquip(u.Data,e,e.Slot));
        public static string Describe(EquipmentEffect effect)
        {
            switch(effect)
            {
                case EquipmentEffect.EternalEdge:return "준비 공격 중인 적에게 피해 +15%";
                case EquipmentEffect.MercifulRod:return "회복·부활 기술 MP 비용 -25%";
                case EquipmentEffect.UnboundBlade:return "가한 실제 피해의 10%만큼 HP 회복";
                case EquipmentEffect.ResoluteFists:return "공격 행동당 게이지 +5";
                case EquipmentEffect.HarmonicHorn:return "노래 강화 지속 +1회 행동";
                case EquipmentEffect.FonicPiercer:return "마법 공격 시 적 마법방어 20% 무시";
                case EquipmentEffect.RoyalMercy:return "유효 회복 시 대상의 속박 해제";
                case EquipmentEffect.FlamingOath:return "기술 HP 비용 -30% (올림)";
                case EquipmentEffect.ThornRelease:return "유효 회복·부활 행동당 게이지 +10";
                case EquipmentEffect.UnbrokenShield:return "방어 중 받는 피해 추가 -15%";
                default:return "";
            }
        }
    }
}
