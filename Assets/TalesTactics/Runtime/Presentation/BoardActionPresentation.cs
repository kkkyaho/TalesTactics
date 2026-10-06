using System.Collections;
using TMPro;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BoardView
    {
        public static float Windup(SkillData skill)=>Duration(skill.Presentation?.Windup??0.18f,0.18f);
        public static float Recovery(SkillData skill)=>Duration(skill.Presentation?.Recovery??0.3f,0.3f);
        static float Duration(float value,float fallback)=>float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp(value,0.08f,1.5f);
        public void BeginSkill(UnitRuntime caster,SkillData skill,Vector2Int aim,float speed=1)
        {
            SetAnimation(caster,skill.Animation);
            if(motions.TryGetValue(caster,out var motion))motion.BeginSkill(skill,Windup(skill)/speed,Recovery(skill)/speed);
            battle.Audio.PlayEffect(skill.Animation==AnimationKind.Cast||skill.IsUltimate?"cast":"swing");
            if(skill.IsUltimate)battle.Audio.BeginTheme(caster.Data.AudioTheme);
            if(root!=null&&skill.Animation!=AnimationKind.Attack)StartCoroutine(PrepareSkill(caster,skill,aim,speed));
        }
        public void ReleaseSkill(UnitRuntime caster)
        {if(motions.TryGetValue(caster,out var motion))motion.ReleaseSkill();}
        IEnumerator PrepareSkill(UnitRuntime caster,SkillData skill,Vector2Int aim,float speed)
        {
            var ring=EffectLine("Skill preparation",ElementColor(skill.Element),skill.IsUltimate?0.045f:0.025f);
            ring.positionCount=33;
            var labelObject=new GameObject("Skill name");labelObject.transform.SetParent(root,false);
            var label=labelObject.AddComponent<TextMeshPro>();label.font=battle.Hud.Font;
            label.text=skill.DisplayName;label.fontSize=skill.IsUltimate?3.8f:3.4f;
            label.alignment=TextAlignmentOptions.Center;label.sortingOrder=35;
            label.color=new Color(.96f,.97f,1);label.textWrappingMode=TextWrappingModes.NoWrap;
            // Measure once: long names fit without a per-frame auto-size pass.
            float preferred=label.GetPreferredValues(label.text).x;
            if(preferred>5.6f)label.fontSize*=5.6f/preferred;
            float width=Mathf.Clamp(label.GetPreferredValues(label.text).x+.4f,1.2f,6);
            label.rectTransform.sizeDelta=new Vector2(width,.7f);
            var backing=GameObject.CreatePrimitive(PrimitiveType.Quad);backing.name="Skill name backing";
            Destroy(backing.GetComponent<Collider>());backing.transform.SetParent(labelObject.transform,false);
            backing.transform.localPosition=new Vector3(0,0,.02f);backing.transform.localScale=new Vector3(width,.65f,1);
            var background=backing.GetComponent<Renderer>();background.sharedMaterial=SpriteMaterial;background.sortingOrder=34;
            SetColor(background,new Color(.025f,.035f,.06f,.9f));
            float duration=Windup(skill)/speed;
            for(float elapsed=0;elapsed<duration;elapsed+=Time.deltaTime)
            {
                float progress=elapsed/duration,radius=Mathf.Lerp(skill.IsUltimate?0.8f:0.5f,0.18f,progress);
                var center=units[caster].position+BattleCamera.transform.up*0.65f;
                for(int i=0;i<33;i++)
                {float a=i/32f*Mathf.PI*2;ring.SetPosition(i,center+BattleCamera.transform.right*(Mathf.Cos(a)*radius)+BattleCamera.transform.up*(Mathf.Sin(a)*radius));}
                label.transform.position=units[caster].position+BattleCamera.transform.up*1.85f;
                label.transform.rotation=BattleCamera.transform.rotation;yield return null;
            }
            if(ring!=null)Destroy(ring.gameObject);if(labelObject!=null)Destroy(labelObject);
        }
    }
}
