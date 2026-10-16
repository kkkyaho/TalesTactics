using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    // The presentation clock does not roll combat randomness or change the grid.
    public static class SkillVfxTiming
    {
        public static float ReleaseDuration(CharacterData caster,SkillData skill,float distance)
        {
            if(skill==null||!skill.Effects.Any(e=>e.Kind==EffectKind.Damage&&!e.AffectCaster))return 0;
            float signatureDuration=SkillSignatures.Travel(SkillSignatures.Resolve(skill));
            if(signatureDuration>0)return signatureDuration;
            if(distance<.05f)return 0;
            var pattern=skill.Presentation?.Pattern??SkillVisualPattern.Automatic;
            var style=CombatEffect.Resolve(caster,skill);
            bool projectile=pattern==SkillVisualPattern.Wave||pattern==SkillVisualPattern.Rain||pattern==SkillVisualPattern.AstralRain||pattern==SkillVisualPattern.Meteor||
                skill.Element==Element.Ice||skill.Element==Element.Lightning||
                skill.Element==Element.Fire&&!ImpactParticles.HasWeaponSweep(style)||style==CombatVisualStyle.Bow||style==CombatVisualStyle.Rifle;
            if(!projectile)return 0;
            if(float.IsNaN(distance)||float.IsInfinity(distance))distance=1;
            return Mathf.Clamp(.14f+distance*.025f,.16f,.30f);
        }
    }
    public sealed partial class BoardView
    {
        public System.Collections.IEnumerator PlaySkillRelease(UnitRuntime caster,SkillData skill,Vector2Int aim,float speed=1)
        {
            if(root==null||!units.TryGetValue(caster,out var anchor)){ReleaseSkill(caster);yield break;}
            var signature=SkillSignatures.Resolve(skill);
            var from=(motions.TryGetValue(caster,out var motion)?motion.transform.position:anchor.position)+BattleCamera.transform.up*.65f;
            var to=battle.Session.Grid[aim].WorldPosition(battle.Catalog.Rules.TileHeight)+BattleCamera.transform.up*.65f;
            if(signature!=SkillSignature.None)
            {
                // These signatures travel on the board or descend onto it, independent of camera pitch.
                from=battle.Session.Grid[caster.Position].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*(signature==SkillSignature.DemonFang?.12f:.1f);
                to=battle.Session.Grid[aim].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*(signature==SkillSignature.DemonFang?.12f:.08f);
            }
            float duration=SkillVfxTiming.ReleaseDuration(caster.Data,skill,Vector3.Distance(from,to))/Mathf.Max(.1f,speed);
            ReleaseSkill(caster,signature==SkillSignature.None?0:duration);
            if(duration<=0)yield break;
            var pool=root.GetComponent<ImpactPool>()??root.gameObject.AddComponent<ImpactPool>();
            pool.Travel(BattleCamera,from,to,skill.Element,CombatEffect.Resolve(caster.Data,skill),skill.Presentation?.Size??1,skill.IsUltimate,battle.Preferences.ReducedEffects,duration,skill.Presentation?.Pattern??SkillVisualPattern.Automatic,signature);
            // Contact and damage numbers are emitted only when the travelling effect arrives.
            yield return new WaitForSeconds(duration);
        }
    }
}
