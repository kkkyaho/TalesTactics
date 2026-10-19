using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BoardView
    {
        public static float Windup(SkillData skill)=>ArteProfiles.Resolve(skill).Enabled?ArteProfiles.Resolve(skill).Windup:SkillSignatures.Resolve(skill)!=SkillSignature.None?SkillSignatures.Windup(SkillSignatures.Resolve(skill)):Mathf.Max(skill.Element==Element.Fire?.6f:skill.Animation==AnimationKind.Attack||skill.Animation==AnimationKind.Skill?.36f:.18f,Duration(skill.Presentation?.Windup??0.18f,0.18f));
        public static float Recovery(SkillData skill)=>ArteProfiles.Resolve(skill).Enabled?ArteProfiles.Resolve(skill).Recovery:SkillSignatures.Resolve(skill)!=SkillSignature.None?SkillSignatures.Recovery(SkillSignatures.Resolve(skill)):Mathf.Max(skill.Element==Element.Fire?.8f:skill.Animation==AnimationKind.Attack||skill.Animation==AnimationKind.Skill?.65f:.3f,Duration(skill.Presentation?.Recovery??0.3f,0.3f));
        static float Duration(float value,float fallback)=>float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp(value,0.08f,1.5f);
        public void BeginSkill(UnitRuntime caster,SkillData skill,Vector2Int aim,float speed=1)
        {
            FocusPresentation(caster,aim,(Windup(skill)+Recovery(skill))/Mathf.Max(.01f,speed)+.85f);
            SetAnimation(caster,skill.Animation);
            if(motions.TryGetValue(caster,out var motion))motion.BeginSkill(skill,Windup(skill)/speed,Recovery(skill)/speed);
            var style=CombatEffect.Resolve(caster.Data,skill);var signature=SkillSignatures.Resolve(skill);var arte=ArteProfiles.Resolve(skill);
            if(root!=null&&(arte.Enabled||skill.Animation!=AnimationKind.Attack||skill.IsUltimate||ImpactParticles.HasWeaponSweep(style))&&units.TryGetValue(caster,out var anchor))
            {
                var pool=root.GetComponent<ImpactPool>()??root.gameObject.AddComponent<ImpactPool>();
                var target=battle.Session.Grid[aim].WorldPosition(battle.Catalog.Rules.TileHeight)+(signature==SkillSignature.None&&!arte.Enabled?BattleCamera.transform.up*.65f:Vector3.up*(signature==SkillSignature.DemonFang?.12f:.08f));
                pool.Prepare(BattleCamera,motion!=null?motion.transform:anchor,target,skill.Element,style,skill.Presentation?.Size??1,skill.IsUltimate,battle.Preferences.ReducedEffects,Windup(skill)/speed,skill.Presentation?.Pattern??SkillVisualPattern.Automatic,signature,arte);
            }
            battle.Audio.PlaySkillCue(skill,style,false,battle.Preferences.ReducedEffects);
            if(skill.IsUltimate){battle.Audio.BeginTheme(caster.Data.AudioTheme);battle.Hud.ShowUltimateCue(caster.Data,skill,Windup(skill)/speed,battle.Preferences.ReducedEffects);}
            if(root!=null)battle.Hud.ShowBattleSkillCue(caster,skill,(Windup(skill)+Recovery(skill))/Mathf.Max(.01f,speed));
        }
        public void ReleaseSkill(UnitRuntime caster,float travelDuration=0)
        {if(motions.TryGetValue(caster,out var motion))motion.ReleaseSkill(travelDuration);}
    }
}
