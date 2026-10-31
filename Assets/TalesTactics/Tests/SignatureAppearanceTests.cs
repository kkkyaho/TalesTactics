using System;
using System.Collections.Generic;
using System.IO;
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
        // Match the native-art measurement policy. All strong-alpha pixels count, including
        // disconnected weapons; preserved weak-alpha support padding is not body height.
        const byte VisibleAlphaThreshold=128;
        sealed class NativePixels
        {
            public readonly int Width,Height;
            public readonly Color32[] Pixels;
            public NativePixels(int width,int height,Color32[] pixels){Width=width;Height=height;Pixels=pixels;}
        }
        static float VisibleHeight(NativePixels image,RectInt rect,float pixelsPerUnit,string context)
        {
            Assert.That(pixelsPerUnit>0&&!float.IsNaN(pixelsPerUnit)&&!float.IsInfinity(pixelsPerUnit),Is.True,context+" PPU");
            Assert.That(rect.width>0&&rect.height>0&&rect.xMin>=0&&rect.yMin>=0&&rect.xMax<=image.Width&&rect.yMax<=image.Height,Is.True,context+" native rect");
            Assert.That(image.Pixels.Length,Is.EqualTo(image.Width*image.Height),context+" native pixels");
            int first=rect.yMax,last=rect.yMin-1;
            for(int y=rect.yMin;y<rect.yMax;y++)for(int x=rect.xMin;x<rect.xMax;x++)
                if(image.Pixels[y*image.Width+x].a>VisibleAlphaThreshold){first=Math.Min(first,y);last=Math.Max(last,y);}
            Assert.That(last,Is.GreaterThanOrEqualTo(first),context+" has no alpha>128 pixels inside its frame");
            return (last-first+1)/pixelsPerUnit;
        }
        static float VisibleHeight(Sprite sprite,Dictionary<string,NativePixels> cache)
        {
            string path=AssetDatabase.GetAssetPath(sprite);
            Assert.That(Path.GetExtension(path),Is.EqualTo(".png").IgnoreCase,sprite.name+" native PNG");
            if(!cache.TryGetValue(path,out var image))
            {
                var decoded=new Texture2D(2,2,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave};
                try
                {
                    Assert.That(ImageConversion.LoadImage(decoded,File.ReadAllBytes(path),false),Is.True,path+" PNG decode");
                    image=new NativePixels(decoded.width,decoded.height,decoded.GetPixels32());cache.Add(path,image);
                }
                finally{UnityEngine.Object.DestroyImmediate(decoded);}
            }
            // Native PNG pixels can exceed an imported texture's resolution. Normalize
            // rect and PPU together so downsampling cannot alter measured world height.
            float sx=(float)image.Width/sprite.texture.width,sy=(float)image.Height/sprite.texture.height;
            var r=sprite.rect;
            var nativeRect=new RectInt(Mathf.RoundToInt(r.x*sx),Mathf.RoundToInt(r.y*sy),Mathf.RoundToInt(r.width*sx),Mathf.RoundToInt(r.height*sy));
            return VisibleHeight(image,nativeRect,sprite.pixelsPerUnit*sy,path+" / "+sprite.name);
        }
        static void AssertVisibleScale(float originalHeight,float equippedHeight,string context)
        {
            Assert.That(equippedHeight/originalHeight,Is.InRange(.65f,1.65f),context+" visible body/weapon scale (alpha>128)");
        }
        [Test] public void VisibleScaleIgnoresWeakPaddingButIncludesDetachedArtAndRejectsEmptyFrames()
        {
            var small=new Color32[8*8];var padded=new Color32[24*40];
            for(int y=0;y<8;y++)for(int x=0;x<8;x++)small[y*8+x]=new Color32(255,255,255,255);
            for(int i=0;i<padded.Length;i++)padded[i]=new Color32(255,255,255,128);
            for(int y=16;y<24;y++)for(int x=8;x<16;x++)padded[y*24+x]=new Color32(255,255,255,255);
            padded[39*24]=new Color32(255,255,255,255); // Another frame must not affect this frame's bounds.
            float original=VisibleHeight(new NativePixels(8,8,small),new RectInt(0,0,8,8),8,"original fixture");
            var image=new NativePixels(24,40,padded);var rect=new RectInt(0,0,24,32);
            float equal=VisibleHeight(image,rect,8,"padded fixture");
            Assert.That(equal,Is.EqualTo(original));AssertVisibleScale(original,equal,"padding invariant");
            padded[31*24+20]=new Color32(255,255,255,129);
            Assert.That(VisibleHeight(image,rect,8,"detached weapon fixture"),Is.EqualTo(2f),"Detached opaque art must remain part of total visible bounds");
            Assert.Throws<AssertionException>(()=>VisibleHeight(new NativePixels(8,8,new Color32[64]),new RectInt(0,0,8,8),8,"empty fixture"));
        }
        [Test] public void VisibleScaleRejectsARealTwofoldBodyEnlargement()
        {
            var original=new Color32[8*8];var enlarged=new Color32[16*16];
            for(int i=0;i<original.Length;i++)original[i]=new Color32(255,255,255,255);
            for(int i=0;i<enlarged.Length;i++)enlarged[i]=new Color32(255,255,255,255);
            float oldHeight=VisibleHeight(new NativePixels(8,8,original),new RectInt(0,0,8,8),8,"original fixture");
            float newHeight=VisibleHeight(new NativePixels(16,16,enlarged),new RectInt(0,0,16,16),8,"enlarged fixture");
            Assert.That(newHeight/oldHeight,Is.EqualTo(2f));
            Assert.Throws<AssertionException>(()=>AssertVisibleScale(oldHeight,newHeight,"true 2x enlargement"));
        }
        [TestCaseSource(nameof(Heroes))] public void EveryAuthoredFrameHasUniqueUsableEquippedArt(string id)
        {
            var c=Catalog.Characters.Single(x=>x.Id==id);var item=Catalog.Equipment.Single(e=>e.ExclusiveCharacterId==id&&e.Slot==EquipmentSlot.Weapon);
            Assert.That(item.Icon,Is.Not.Null,id);Assert.That(item.Icon.name,Is.EqualTo(item.Id));Assert.That(item.Appearance,Is.Not.Null,id);
            Assert.That(item.Appearance.Frames.Select(f=>f.Original).Distinct().Count(),Is.EqualTo(item.Appearance.Frames.Length),"Ambiguous source mapping");
            Assert.That(item.Appearance.Frames.Select(f=>f.Equipped).Distinct().Count(),Is.EqualTo(item.Appearance.Frames.Length),"Each pose retains a distinct frame");
            var nativePixels=new Dictionary<string,NativePixels>();
            try
            {
              foreach(var original in Frames(c))
              {
                var equipped=EquipmentAppearance.Resolve(c,item,original);Assert.That(equipped,Is.Not.SameAs(original),original.name);
                Assert.That(equipped,Is.Not.Null);Assert.That(equipped.vertices.Length,Is.GreaterThan(3));
                AssertVisibleScale(VisibleHeight(original,nativePixels),VisibleHeight(equipped,nativePixels),original.name);
                Assert.That(float.IsNaN(equipped.pivot.x)||float.IsNaN(equipped.pivot.y),Is.False);
                Assert.That(EquipmentAppearance.Resolve(c,null,original),Is.SameAs(original));
                Assert.That(EquipmentAppearance.Resolve(Catalog.Characters.First(x=>x.Id!=id),item,original),Is.SameAs(original));
              }
            }
            finally{nativePixels.Clear();}
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
