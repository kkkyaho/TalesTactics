using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
namespace TalesTactics.Editor
{
    // Explicit Editor execution only; does not regenerate content or change original sprites.
    public static class SignatureWeaponImporter
    {
        const string Root="Assets/TalesTactics/Art/SignatureWeapons/";
        public static readonly string[] Heroes={"cless","mint","farah","tear","jade","natalia","velvet","alphen","shionne","kisara"};
        public static readonly string[] Weapons={"eternal-sword","bc-rod","god-weapon","unicorn-horn","blood-pain","holy-quelqatl","unnamed-blade","nebilim","damonisch-core","os-rex"};
        static float F(string s)=>float.Parse(s,CultureInfo.InvariantCulture);
        public static void Import()
        {
            AssetDatabase.Refresh();
            var groups=File.ReadAllLines("Tools/signature-art-layout.csv").Skip(1).Select(l=>l.Split(',')).GroupBy(v=>v[0]).ToArray();
            var polygons=File.ReadAllLines("Tools/signature-art-outlines.csv").Select(l=>l.Split(',')).GroupBy(v=>v[0]).ToDictionary(g=>g.Key,g=>g.Select(v=>v.Skip(1).Select(p=>p.Split(':')).Select(p=>new Vector2(F(p[0]),F(p[1]))).ToArray()).ToList());
            var frames=Heroes.ToDictionary(id=>id,id=>new List<EquipmentAppearance.Frame>());
            foreach(var group in groups)
            {
                var rows=group.ToArray();var importer=AssetImporter.GetAtPath(group.Key) as TextureImporter;
                if(importer==null)throw new InvalidOperationException("Missing texture "+group.Key);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=rows[0][1].Length==0?FilterMode.Bilinear:FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
                importer.spritePixelsPerUnit=F(rows[0][10]);importer.SaveAndReimport();
                var factories=new SpriteDataProviderFactories();factories.Init();
                var provider=factories.GetSpriteEditorDataProviderFromObject(importer);
                if(provider==null)throw new InvalidOperationException("Missing sprite provider");
                provider.InitSpriteEditorDataProvider();
                var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
                if(capability==null)throw new InvalidOperationException("Missing edit capability");
                foreach(var flag in new[]{EEditCapability.EditSpriteName,EEditCapability.EditSpriteRect,EEditCapability.EditPivot,EEditCapability.CreateAndDeleteSprite})
                    if(!capability.GetEditCapability().HasCapability(flag))throw new InvalidOperationException("Unsupported "+flag);
                var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();var outlines=provider.GetDataProvider<ISpriteOutlineDataProvider>();
                if(names==null||outlines==null)throw new InvalidOperationException("Missing name or outline provider");
                var previous=provider.GetSpriteRects();var rects=new List<SpriteRect>();var pairs=new List<SpriteNameFileIdPair>();
                foreach(var v in rows)
                {
                    if(!polygons.ContainsKey(v[3]))throw new InvalidOperationException("Missing outline "+v[3]);
                    var old=previous.FirstOrDefault(r=>r.name==v[3]);var id=old==null?GUID.Generate():old.spriteID;
                    rects.Add(new SpriteRect{name=v[3],spriteID=id,rect=new Rect(F(v[4]),F(v[5]),F(v[6]),F(v[7])),alignment=SpriteAlignment.Custom,pivot=new Vector2(F(v[8]),F(v[9]))});
                    pairs.Add(new SpriteNameFileIdPair(v[3],id));
                }
                provider.SetSpriteRects(rects.ToArray());names.SetNameFileIdPairs(pairs);
                foreach(var rect in rects)outlines.SetOutlines(rect.spriteID,polygons[rect.name]);
                provider.Apply();importer.SaveAndReimport();
                if(rows[0][1].Length==0)continue; // Item icon atlas has no original character frame.
                var source=AssetDatabase.LoadAllAssetsAtPath(rows[0][1]).OfType<Sprite>().ToDictionary(s=>s.name);
                var target=AssetDatabase.LoadAllAssetsAtPath(group.Key).OfType<Sprite>().ToDictionary(s=>s.name);
                string hero=Path.GetFileNameWithoutExtension(group.Key).Split('-')[0];
                foreach(var row in rows)frames[hero].Add(new EquipmentAppearance.Frame{Original=source[row[2]],Equipped=target[row[3]]});
            }
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            var icons=AssetDatabase.LoadAllAssetsAtPath(Root+"icons.png").OfType<Sprite>().ToDictionary(s=>s.name);
            foreach(var id in Weapons)
                if(icons.TryGetValue(id,out var icon)){var item=catalog.Equipment.Single(e=>e.Id==id);item.Icon=icon;EditorUtility.SetDirty(item);}
            for(int i=0;i<Heroes.Length;i++)
            {
                if(frames[Heroes[i]].Count==0)continue; // Partial import for local art review; ValidateAll rejects incomplete sets.
                string path=Root+Heroes[i]+"-appearance.asset";
                var asset=AssetDatabase.LoadAssetAtPath<EquipmentAppearance>(path);
                if(asset==null){asset=ScriptableObject.CreateInstance<EquipmentAppearance>();AssetDatabase.CreateAsset(asset,path);}
                asset.CharacterId=Heroes[i];asset.EquipmentId=Weapons[i];asset.Frames=frames[Heroes[i]].ToArray();EditorUtility.SetDirty(asset);
                var weapon=catalog.Equipment.Single(e=>e.Id==Weapons[i]);weapon.Appearance=asset;EditorUtility.SetDirty(weapon);
            }
            AssetDatabase.SaveAssets();Debug.Log("Signature appearance imported: "+groups.Length+" sheets, "+frames.Sum(f=>f.Value.Count)+" frames.");
        }
        public static IEnumerable<Sprite> CharacterFrames(CharacterData c)
        {
            IEnumerable<Sprite> Directions(DirectionalSprites s)=>s==null?Array.Empty<Sprite>():new[]{s.Front,s.Back,s.Left,s.Right};
            var p=c.Poses;
            var sprites=Directions(c.Sprites).Concat(Directions(p.Attack)).Concat(Directions(p.Cast)).Concat(Directions(p.Guard)).Concat(Directions(p.Damage));
            foreach(var clip in new[]{p.Walk,p.Dead,p.AttackMotion,p.Skill,p.Ultimate})
                if(clip?.Frames!=null)foreach(var f in clip.Frames)sprites=sprites.Concat(Directions(f));
            return sprites.Where(s=>s!=null).Distinct();
        }
        public static void ValidateAll()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            foreach(var id in Heroes)
            {
                var c=catalog.Characters.Single(x=>x.Id==id);var item=catalog.Equipment.Single(e=>e.ExclusiveCharacterId==id&&e.Appearance!=null);
                if(item.Icon==null)throw new InvalidOperationException("Missing signature icon: "+id);
                foreach(var frame in CharacterFrames(c))
                    if(EquipmentAppearance.Resolve(c,item,frame)==frame)throw new InvalidOperationException("Unmapped signature frame: "+id+" / "+frame.name);
            }
            Debug.Log("All ten signature appearances and icons verified.");
        }
    }
}
