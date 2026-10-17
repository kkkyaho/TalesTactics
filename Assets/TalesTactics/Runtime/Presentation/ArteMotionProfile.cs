using UnityEngine;
namespace TalesTactics
{
    public static class ArteMotionProfile
    {
        public static CombatMotionProfile For(ArteProfile arte,CombatVisualStyle style)
        {
            switch(arte.Motion)
            {
                case ArteMotion.Cast:return new CombatMotionProfile(.015f,.005f,.035f,0,0);
                case ArteMotion.Jump:return new CombatMotionProfile(.40f,.12f,.45f,16,.055f);
                case ArteMotion.Dash:return new CombatMotionProfile(.72f,.20f,.045f,22,.065f);
                case ArteMotion.Blink:return new CombatMotionProfile(.85f,.08f,.08f,8,.04f);
                case ArteMotion.Guard:return new CombatMotionProfile(.08f,.06f,0,3,.04f);
                case ArteMotion.Shoot:return new CombatMotionProfile(.01f,.12f,0,2,.035f);
                case ArteMotion.Spin:return new CombatMotionProfile(.35f,.12f,.18f,12,.045f);
                default:return CombatMotionProfile.For(style,arte.Ultimate);
            }
        }
    }
}
