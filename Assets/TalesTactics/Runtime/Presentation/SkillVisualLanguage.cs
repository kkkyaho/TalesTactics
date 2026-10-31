using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public static class SkillVisualLanguage
    {
        // A common ink and timing vocabulary for every authored arte. These values
        // affect presentation only; resolver timing and Content assets remain intact.
        public static Color Ink(Color color)=>new Color(.055f+color.r*.09f,.045f+color.g*.07f,.11f+color.b*.12f,color.a);
        public static readonly Color HealingIvory=new Color(1f,.98f,.79f);
        public static readonly Color HealingLeaf=new Color(.68f,.92f,.61f);
        public static Color Highlight(Color color)=>Color.Lerp(color,new Color(1f,.99f,.92f,color.a),.78f);
        // A narrow tail and a rounded leading shoulder, with zero width at both ends.
        public static float TrailWidth(float phase)
        {phase=Mathf.Clamp01(phase);return Mathf.Pow(Mathf.Max(0,Mathf.Sin(phase*Mathf.PI)),.72f)*Mathf.Lerp(.35f,1,phase);}
        public static float Snap(float phase)
        {
            phase=Mathf.Clamp01(phase);
            // A short anticipation, fast contact and readable follow-through.
            return phase<.16f?phase*.28f:phase<.66f?Mathf.Lerp(.0448f,.96f,Mathf.SmoothStep(0,1,(phase-.16f)/.5f)):Mathf.Lerp(.96f,1,(phase-.66f)/.34f);
        }
        public static float ContactEnvelope(float seconds,float length)
        {
            if(seconds<0||seconds>=length)return 0;
            float hold=Mathf.Min(.085f,length*.24f);
            return seconds<=hold?1:1-Mathf.SmoothStep(0,1,(seconds-hold)/(length-hold));
        }
        public static string Icon(SkillData s)=>s.Effects.Any(e=>e.Kind==EffectKind.Heal||e.Kind==EffectKind.Revive)?"heal":s.Shape==SkillAreaShape.Line?"line":s.Shape==SkillAreaShape.Cone?"cone":s.Area>0?"area":s.Target==TargetType.Self?"guard":"single";
        public static string ElementName(Element e)
        {switch(e){case Element.Fire:return "불";case Element.Water:return "물";case Element.Wind:return "바람";case Element.Earth:return "땅";case Element.Light:return "빛";case Element.Dark:return "어둠";case Element.Lightning:return "번개";case Element.Ice:return "얼음";default:return "무속성";}}
        public static Color Tint(SkillData s)=>s.Target==TargetType.Ally||s.Target==TargetType.FallenAlly?new Color(.4f,.86f,.65f):s.IsUltimate?new Color(.94f,.76f,.42f):new Color(.39f,.78f,.86f);
        public static string Range(SkillData s)=>"거리 "+s.MinRange+"–"+s.Range+" · "+ElementName(s.Element);
    }
}
