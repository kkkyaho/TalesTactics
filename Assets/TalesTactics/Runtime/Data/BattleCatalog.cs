using UnityEngine;
namespace TalesTactics
{
    [CreateAssetMenu(menuName="Tales Tactics/Catalog")]
    public class BattleCatalog : ScriptableObject
    {
        public CharacterData[] Characters;
        public bool CrossoverReady;
        public CharacterData Enemy;
        public CharacterData[] Enemies = new CharacterData[0];
        public CharacterData[] CrossoverVariants = new CharacterData[0];
        public DirectionalSprites[] Civilians = new DirectionalSprites[0];
        public BattleRules Rules;
        public AudioLibrary Audio;
        public EquipmentData[] Equipment;
        public SkillData[] TacticalEnemySkills = new SkillData[0];
    }
}
