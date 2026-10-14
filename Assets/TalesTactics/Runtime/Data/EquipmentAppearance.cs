using System;
using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    // One-to-one replacement of authored frames. The original clips retain their timing,
    // phase split, direction corrections and fallback behavior.
    [CreateAssetMenu(menuName="Tales Tactics/Equipment Appearance")]
    public sealed class EquipmentAppearance:ScriptableObject
    {
        [Serializable] public struct Frame
        {
            public Sprite Original, Equipped;
        }
        public string CharacterId, EquipmentId;
        public Frame[] Frames=Array.Empty<Frame>();
        Dictionary<Sprite,Sprite> frames;
        public Sprite Resolve(Sprite original)
        {
            if(original==null)return null;
            if(frames==null)
            {
                frames=new Dictionary<Sprite,Sprite>();
                foreach(var frame in Frames)
                    if(frame.Original!=null&&frame.Equipped!=null)frames[frame.Original]=frame.Equipped;
            }
            return frames.TryGetValue(original,out var art)?art:original;
        }
        void OnEnable()=>frames=null;
        void OnValidate()=>frames=null;
        public static Sprite Resolve(CharacterData character,EquipmentData weapon,Sprite original)
        {
            var appearance=weapon!=null?weapon.Appearance:null;
            if(character==null||appearance==null||appearance.CharacterId!=character.Id||
                appearance.EquipmentId!=weapon.Id||!EquipmentLoadout.CanEquip(character,weapon,EquipmentSlot.Weapon))return original;
            return appearance.Resolve(original);
        }
        public static Sprite Resolve(UnitRuntime unit,Sprite original)=>unit==null?original:
            Resolve(unit.Data,unit.Equipment[(int)EquipmentSlot.Weapon],original);
    }
}
