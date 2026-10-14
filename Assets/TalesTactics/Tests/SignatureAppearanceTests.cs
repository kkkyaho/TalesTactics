using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace TalesTactics.Tests
{
    public sealed class SignatureAppearanceTests
    {
        BattleCatalog Catalog=>AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
        public static string[] Heroes=>new[]{"cless","mint","farah","tear","jade","natalia","velvet","alphen","shionne","kisara"};
        static IEnumerable<Sprite> Directions(DirectionalSprites s)=>s==null?Array.Empty<Sprite>():new[]{s.Front,s.Back,s.Left,s.Right};
        static IEnumerable<Sprite> Frames(CharacterData c)
        {
            var p=c.Poses;var all=Directions(c.Sprites).Concat(Directions(p.Attack)).Concat(Directions(p.Cast)).Concat(Directions(p.Guard)).Concat(Directions(p.Damage));
            foreach(var clip in new[]{p.Walk,p.Dead,p.AttackMotion,p.Skill,p.Ultimate})
                foreach(var frame in clip.Frames)all=all.Concat(Directions(frame));
            return all.Where(s=>s!=null).Distinct();
        }
        [TestCaseSource(nameof(Heroes))] public void EveryAuthoredFrameHasUniqueUsableEquippedArt(string id)
        {
            var c=Catalog.Characters.Single(x=>x.Id==id);var item=Catalog.Equipment.Single(e=>e.ExclusiveCharacterId==id&&e.Slot==EquipmentSlot.Weapon);
            Assert.That(item.Icon,Is.Not.Null,id);Assert.That(item.Icon.name,Is.EqualTo(item.Id));Assert.That(item.Appearance,Is.Not.Null,id);
            Assert.That(item.Appearance.Frames.Select(f=>f.Original).Distinct().Count(),Is.EqualTo(item.Appearance.Frames.Length),"Ambiguous source mapping");
            Assert.That(item.Appearance.Frames.Select(f=>f.Equipped).Distinct().Count(),Is.EqualTo(item.Appearance.Frames.Length),"Each pose retains a distinct frame");
            foreach(var original in Frames(c))
            {
                var equipped=EquipmentAppearance.Resolve(c,item,original);Assert.That(equipped,Is.Not.SameAs(original),original.name);
                Assert.That(equipped,Is.Not.Null);Assert.That(equipped.vertices.Length,Is.GreaterThan(3));
                Assert.That(equipped.bounds.size.y/original.bounds.size.y,Is.InRange(.65f,1.65f),original.name+" body/weapon scale");
                Assert.That(float.IsNaN(equipped.pivot.x)||float.IsNaN(equipped.pivot.y),Is.False);
                Assert.That(EquipmentAppearance.Resolve(c,null,original),Is.SameAs(original));
                Assert.That(EquipmentAppearance.Resolve(Catalog.Characters.First(x=>x.Id!=id),item,original),Is.SameAs(original));
            }
        }
        [TestCaseSource(nameof(Heroes))] public void SavedEquipmentIDsRestoreAppearanceAndFailedChangesRetainIt(string id)
        {
            var c=Catalog.Characters.Single(x=>x.Id==id);var item=Catalog.Equipment.Single(e=>e.ExclusiveCharacterId==id&&e.Slot==EquipmentSlot.Weapon);
            var save=new CampaignSave();save.Inventory.Add(new OwnedEquipment{Id=item.Id,Count=1});
            var draft=new EquipmentLoadout(c,save.Get(id),Catalog.Equipment,save);Assert.That(draft.Equip(EquipmentSlot.Weapon,item),Is.True);
            Assert.That(draft.TrySave(save,_=>true),Is.True);save=JsonUtility.FromJson<CampaignSave>(JsonUtility.ToJson(save));
            var unit=new UnitRuntime(c,Team.Player,Catalog.Rules);draft=new EquipmentLoadout(c,save.Get(id),Catalog.Equipment,save);draft.Apply(unit);
            Assert.That(EquipmentAppearance.Resolve(unit,c.Sprites.Front),Is.Not.SameAs(c.Sprites.Front));
            draft.Equip(EquipmentSlot.Weapon,null);Assert.That(draft.TrySave(save,_=>false),Is.False);
            new EquipmentLoadout(c,save.Get(id),Catalog.Equipment,save).Apply(unit);Assert.That(EquipmentAppearance.Resolve(unit,c.Sprites.Front),Is.Not.SameAs(c.Sprites.Front));
            Assert.That(draft.TrySave(save,_=>true),Is.True);new EquipmentLoadout(c,save.Get(id),Catalog.Equipment,save).Apply(unit);
            Assert.That(EquipmentAppearance.Resolve(unit,c.Sprites.Front),Is.SameAs(c.Sprites.Front));
        }
    }
}
