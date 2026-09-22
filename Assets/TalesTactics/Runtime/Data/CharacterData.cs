using UnityEngine;
namespace TalesTactics
{
    [CreateAssetMenu(menuName="Tales Tactics/Character")]
    public class CharacterData : ScriptableObject
    {
        public string Id, DisplayName, Job, SourceTitle, PassiveSkill, AudioTheme;
        public Sprite Portrait;
        public DirectionalSprites Sprites = new DirectionalSprites();
        public RuntimeAnimatorController Animator;
        public ElementAffinity[] Affinities=new ElementAffinity[0];
        public WeaponType Weapon;
        public Stats BaseStats, GrowthStats, PromotionBonus;
        public int PromotionLevel=20;
        public string PromotionStoryFlag="chapter2", PromotionJob;
        public SkillData BasicAttack, UltimateSkill;
        public SkillData[] Skills=new SkillData[0];
        public Color PlaceholderColor=Color.cyan;
        public Stats StatsAt(int level, bool promoted=false) => BaseStats + GrowthStats * (Mathf.Clamp(level,1,50)-1) + (promoted ? PromotionBonus : default);
    }
}
