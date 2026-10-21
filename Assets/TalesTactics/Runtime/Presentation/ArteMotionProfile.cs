using UnityEngine;
namespace TalesTactics
{
    public static class ArteMotionProfile
    {
        public static CombatMotionProfile For(ArteProfile arte,CombatVisualStyle style)
        {
            switch(arte.Motion)
            {
                case ArteMotion.Cast:return new CombatMotionProfile(.025f,.018f,.055f,2,.025f);
                case ArteMotion.Jump:return new CombatMotionProfile(.46f,.16f,.56f,19,.075f);
                case ArteMotion.Dash:return new CombatMotionProfile(.80f,.24f,.065f,25,.080f);
                case ArteMotion.Blink:return new CombatMotionProfile(.85f,.11f,.11f,10,.055f);
                case ArteMotion.Guard:return new CombatMotionProfile(.11f,.09f,.025f,5,.055f);
                case ArteMotion.Shoot:return new CombatMotionProfile(.015f,.16f,.018f,4,.050f);
                case ArteMotion.Spin:return new CombatMotionProfile(.42f,.16f,.24f,16,.065f);
                default:return CombatMotionProfile.For(style,arte.Ultimate);
            }
        }
    }
}
