using UnityEngine;
namespace TalesTactics
{
    [CreateAssetMenu(menuName="Tales Tactics/Catalog")]
    public class BattleCatalog : ScriptableObject
    {
        public CharacterData[] Characters;
        public CharacterData Enemy;
        public BattleRules Rules;
        public AudioLibrary Audio;
        public EquipmentData[] Equipment;
    }
}
