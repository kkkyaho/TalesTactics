using System;
using UnityEngine;
namespace TalesTactics
{
    [Serializable]
    public sealed class CharacterPoses
    {
        public DirectionalSpriteClip Walk=new DirectionalSpriteClip(), Dead=new DirectionalSpriteClip();
        public DirectionalSprites Attack=new DirectionalSprites(), Cast=new DirectionalSprites(),
            Guard=new DirectionalSprites(), Damage=new DirectionalSprites();

        // Skill and Ultimate intentionally reuse Attack and Cast until dedicated art is authored.
        public Sprite Get(AnimationKind action,Facing facing)
        {
            switch(action)
            {
                case AnimationKind.Attack:case AnimationKind.Skill:return Attack?.Get(facing);
                case AnimationKind.Cast:case AnimationKind.Ultimate:return Cast?.Get(facing);
                case AnimationKind.Guard:return Guard?.Get(facing);
                case AnimationKind.Damage:return Damage?.Get(facing);
                default:return null;
            }
        }
    }
}
