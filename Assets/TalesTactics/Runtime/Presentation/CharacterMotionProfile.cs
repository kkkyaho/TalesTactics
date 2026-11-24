using System;
using UnityEngine;
namespace TalesTactics
{
    // Authored roster motion replaces presentation frames only. Existing combat timing,
    // character data and equipment mappings remain authoritative.
    [CreateAssetMenu(menuName="Tales Tactics/Character Motion Profile")]
    public class CharacterMotionProfile:ScriptableObject
    {
        public string CharacterId;
        public virtual string OwnerId=>CharacterId;
        public const float IdleCycleSeconds=5.4f;
        public DirectionalSpriteClip Idle=new DirectionalSpriteClip();
        public DirectionalSpriteClip Attack=new DirectionalSpriteClip();
        public DirectionalSpriteClip Damage=new DirectionalSpriteClip();
        [Serializable] public sealed class EquipmentVariant
        {
            public EquipmentAppearance Appearance;
            public DirectionalSpriteClip Idle,Attack,Damage;
        }
        public EquipmentVariant[] EquipmentVariants=Array.Empty<EquipmentVariant>();

        public static int IdleFrameIndex(float elapsed)
        {
            if(float.IsNaN(elapsed)||float.IsInfinity(elapsed))return 0;
            float phase=Mathf.Repeat(Mathf.Max(0,elapsed),IdleCycleSeconds);
            // 0: resting, 1: inhale, 2: briefly closed eyes, 3: settle/open again.
            // Closed eyes occupy 0.10s, rather than one quarter of a uniform loop.
            if(phase<1.4f)return 0;
            if(phase<2.4f)return 1;
            if(phase<3.1f)return 3;
            if(phase<4.8f)return 0;
            if(phase<4.9f)return 2;
            return phase<5.1f?3:0;
        }

        public Sprite Sample(UnitRuntime owner,AnimationKind action,Facing view,float elapsed,
            bool presenting=false,bool released=false,float phaseElapsed=0,float phaseDuration=0)
        {
            if(owner?.Data==null||string.IsNullOrEmpty(OwnerId)||owner.Data.Id!=OwnerId||!owner.Alive)return null;
            var clip=ClipFor(owner,action);
            int expected=action==AnimationKind.Idle?4:action==AnimationKind.Attack?3:action==AnimationKind.Damage?1:0;
            // A missing direction falls back as a whole clip; never show a front pose
            // during a back view or intermittently switch between different artwork.
            if(expected==0||clip?.Frames==null||clip.Frames.Length!=expected)return null;
            foreach(var frame in clip.Frames)if(frame?.Get(view)==null)return null;
            if(action==AnimationKind.Idle)return clip.Frames[IdleFrameIndex(elapsed)].Get(view);
            if(action==AnimationKind.Damage)
            {
                if(float.IsNaN(elapsed)||float.IsInfinity(elapsed)||elapsed<0)return null;
                // End the hit pose at its original boundary, then keep the new resting
                // artwork while the existing recoil finishes. Do not flash the old idle.
                return elapsed<.28f?clip.Frames[0].Get(view):Sample(owner,AnimationKind.Idle,view,0);
            }
            if(clip.ReleaseFrame!=1)return null;
            if(presenting)return clip.SamplePhase(view,phaseElapsed,phaseDuration,released,SpritePhasePacing.Action);
            if(float.IsNaN(elapsed)||float.IsInfinity(elapsed)||elapsed<0)return null;
            if(elapsed>=.42f)return Sample(owner,AnimationKind.Idle,view,0);
            return clip.SamplePhase(view,elapsed<.18f?elapsed:elapsed-.18f,elapsed<.18f?.18f:.24f,
                elapsed>=.18f,SpritePhasePacing.Action);
        }

        DirectionalSpriteClip ClipFor(UnitRuntime owner,AnimationKind action)
        {
            var weapon=owner.Equipment[(int)EquipmentSlot.Weapon];var appearance=weapon!=null?weapon.Appearance:null;
            if(appearance!=null&&appearance.CharacterId==owner.Data.Id&&appearance.EquipmentId==weapon.Id&&
                EquipmentLoadout.CanEquip(owner.Data,weapon,EquipmentSlot.Weapon))
            {
                // Authored sprites contain the weapon. Never replace signature-weapon
                // art with the base weapon just because the new names do not map.
                if(EquipmentVariants!=null)foreach(var variant in EquipmentVariants)
                    if(variant!=null&&variant.Appearance==appearance)
                        return Select(action,variant.Idle,variant.Attack,variant.Damage);
                return null;
            }
            return Select(action,Idle,Attack,Damage);
        }
        static DirectionalSpriteClip Select(AnimationKind action,DirectionalSpriteClip idle,
            DirectionalSpriteClip attack,DirectionalSpriteClip damage)=>action==AnimationKind.Idle?idle:
            action==AnimationKind.Attack?attack:action==AnimationKind.Damage?damage:null;
    }
}
