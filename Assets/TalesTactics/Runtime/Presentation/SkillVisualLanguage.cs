using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public static class SkillVisualLanguage
    {
        public static string Icon(SkillData s)=>s.Effects.Any(e=>e.Kind==EffectKind.Heal||e.Kind==EffectKind.Revive)?"heal":s.Shape==SkillAreaShape.Line?"line":s.Shape==SkillAreaShape.Cone?"cone":s.Area>0?"area":s.Target==TargetType.Self?"guard":"single";
        public static string ElementName(Element e)
        {switch(e){case Element.Fire:return "불";case Element.Water:return "물";case Element.Wind:return "바람";case Element.Earth:return "땅";case Element.Light:return "빛";case Element.Dark:return "어둠";case Element.Lightning:return "번개";case Element.Ice:return "얼음";default:return "무속성";}}
        public static Color Tint(SkillData s)=>s.Target==TargetType.Ally||s.Target==TargetType.FallenAlly?new Color(.4f,.86f,.65f):s.IsUltimate?new Color(.94f,.76f,.42f):new Color(.39f,.78f,.86f);
        public static string Range(SkillData s)=>"거리 "+s.MinRange+"–"+s.Range+" · "+ElementName(s.Element);
    }
}
