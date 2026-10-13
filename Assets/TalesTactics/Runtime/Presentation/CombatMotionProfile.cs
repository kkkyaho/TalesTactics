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
                case CombatVisualStyle.Claw:p=new CombatMotionProfile(.38f,.10f,.055f,13,.08f);break;
                case CombatVisualStyle.Martial:p=new CombatMotionProfile(.36f,.085f,.10f,11,.075f);break;
                case CombatVisualStyle.FlameSword:p=new CombatMotionProfile(.40f,.12f,.045f,15,.085f);break;
                case CombatVisualStyle.Shield:p=new CombatMotionProfile(.27f,.12f,0,8,.09f);break;
                case CombatVisualStyle.Spear:p=new CombatMotionProfile(.42f,.08f,.015f,6,.075f);break;
                case CombatVisualStyle.Bow:p=new CombatMotionProfile(.015f,.055f,0,1,.04f);break;
                case CombatVisualStyle.Rifle:p=new CombatMotionProfile(.005f,.11f,0,2,.045f);break;
                case CombatVisualStyle.Sacred:case CombatVisualStyle.Song:p=new CombatMotionProfile(.015f,.005f,.035f,0,0);break;
                default:p=new CombatMotionProfile(.36f,.10f,.035f,14,.08f);break;
            }
            return ultimate?new CombatMotionProfile(p.Advance*1.15f,p.Recoil,p.Lift,p.Tilt,Mathf.Min(.08f,p.Hold+.015f)):p;
        }
    }
}
