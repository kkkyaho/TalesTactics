using UnityEngine;
namespace TalesTactics
{
    [CreateAssetMenu(menuName="Tales Tactics/Equipment")]
    public class EquipmentData : ScriptableObject
    {
        public string Id, DisplayName;
        public EquipmentSlot Slot;
        public WeaponType Weapon;
        public Stats Bonus;
    }
}
