using UnityEngine;
namespace TalesTactics
{
    // Presentation only: never changes targeting, damage, turn state or the global clock.
    public readonly struct CombatMotionProfile
    {
        public readonly float Advance, Recoil, Lift, Tilt, Hold;
        public CombatMotionProfile(float advance,float recoil,float lift,float tilt,float hold)
        {Advance=advance;Recoil=recoil;Lift=lift;Tilt=tilt;Hold=hold;}
        public static CombatMotionProfile For(CombatVisualStyle style,bool ultimate=false)
        {
            CombatMotionProfile p;
            switch(style)
            {
                case CombatVisualStyle.Claw:p=new CombatMotionProfile(.26f,.06f,.025f,4,.055f);break;
                case CombatVisualStyle.Martial:p=new CombatMotionProfile(.22f,.035f,.09f,3,.05f);break;
                case CombatVisualStyle.FlameSword:p=new CombatMotionProfile(.21f,.075f,.025f,4,.075f);break;
                case CombatVisualStyle.Shield:p=new CombatMotionProfile(.14f,.09f,0,2,.08f);break;
                case CombatVisualStyle.Spear:p=new CombatMotionProfile(.24f,.025f,0,1,.045f);break;
                case CombatVisualStyle.Bow:p=new CombatMotionProfile(.015f,.055f,0,1,.04f);break;
                case CombatVisualStyle.Rifle:p=new CombatMotionProfile(.005f,.11f,0,2,.045f);break;
                case CombatVisualStyle.Sacred:case CombatVisualStyle.Song:p=new CombatMotionProfile(.015f,.005f,.035f,0,0);break;
                default:p=new CombatMotionProfile(.18f,.045f,.015f,3,.06f);break;
            }
            return ultimate?new CombatMotionProfile(p.Advance*1.15f,p.Recoil,p.Lift,p.Tilt,Mathf.Min(.08f,p.Hold+.015f)):p;
        }
    }
}
