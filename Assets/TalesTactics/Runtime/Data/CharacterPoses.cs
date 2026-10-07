using System;
using UnityEngine;
namespace TalesTactics
{
    [Serializable]
    public sealed class CharacterPoses
    {
        public DirectionalSpriteClip Walk=new DirectionalSpriteClip(), Dead=new DirectionalSpriteClip();
        public DirectionalSpriteClip Skill=new DirectionalSpriteClip(), Ultimate=new DirectionalSpriteClip();
        public DirectionalSpriteClip AttackMotion=new DirectionalSpriteClip();
        public DirectionalSprites Attack=new DirectionalSprites(), Cast=new DirectionalSprites(),
            Guard=new DirectionalSprites(), Damage=new DirectionalSprites();

        // Legacy/enemy assets may omit the dedicated clips.
        public Sprite Get(AnimationKind action,Facing facing)
        {
            switch(action)
            {
                case AnimationKind.Attack:return Attack?.Get(facing);
                case AnimationKind.Skill:return Skill?.Sample(facing,0,false)??Attack?.Get(facing);
                case AnimationKind.Cast:return Cast?.Get(facing);
                case AnimationKind.Ultimate:return Ultimate?.Sample(facing,0,false)??Cast?.Get(facing);
                case AnimationKind.Guard:return Guard?.Get(facing);
                case AnimationKind.Damage:return Damage?.Get(facing);
                default:return null;
            }
        }
    }
}
