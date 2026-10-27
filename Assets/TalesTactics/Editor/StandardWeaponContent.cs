using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
namespace TalesTactics.Editor
{
    // Explicit additive installation only. Never calls DemoContent.Create or rewrites existing item values.
    public static class StandardWeaponContent
    {
        public const string ContentRoot="Assets/TalesTactics/Content/StandardWeapons";
        public const string IconPath="Assets/TalesTactics/Art/StandardWeapons/icons.png";
        sealed class Definition
        {
            public readonly string Id,Name,File;public readonly WeaponType Weapon;public readonly Stats Bonus;
            public Definition(string id,string name,string file,WeaponType weapon,Stats bonus){Id=id;Name=name;File=file;Weapon=weapon;Bonus=bonus;}
        }
        static readonly Definition[] definitions={
            new Definition("training-staff","수련 지팡이","TrainingStaff",WeaponType.Staff,new Stats{MAG=8}),
            new Definition("steel-claw","강철 발톱","SteelClaw",WeaponType.Claw,new Stats{STR=8}),
            new Definition("steel-fist","강철 권갑","SteelFist",WeaponType.Fist,new Stats{STR=8}),
            new Definition("training-spear","수련 창","TrainingSpear",WeaponType.Spear,new Stats{MAG=8}),
            new Definition("reinforced-bow","강화 활","ReinforcedBow",WeaponType.Bow,new Stats{STR=8}),
            new Definition("infantry-rifle","보병 소총","InfantryRifle",WeaponType.Gun,new Stats{STR=8}),
            new Definition("steel-shield","강철 방패","SteelShield",WeaponType.Shield,new Stats{STR=4,DEF=4})
        };
        public static void Install()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            ValidateDestinations(catalog,ContentRoot);
            var icons=ImportIcons();
            if(!AssetDatabase.IsValidFolder(ContentRoot))AssetDatabase.CreateFolder("Assets/TalesTactics/Content","StandardWeapons");
            AddMissing(catalog,ContentRoot,icons);
            Debug.Log("Standard weapons installed additively: 7 weapon types. Existing item values preserved.");
        }
        static void ValidateDestinations(BattleCatalog catalog,string root)
        {
            if(catalog==null)throw new InvalidOperationException("Existing catalog required.");
            if(!root.StartsWith("Assets/",StringComparison.Ordinal)||root.Contains("..")||root.EndsWith("/"))throw new ArgumentException("An Assets folder path is required.");
            var items=catalog.Equipment??Array.Empty<EquipmentData>();
            foreach(var definition in definitions)
            {
                var matches=items.Where(e=>e!=null&&e.Id==definition.Id).ToArray();
                if(matches.Length>1)throw new InvalidOperationException("Duplicate equipment ID: "+definition.Id);
                string path=root+"/"+definition.File+".asset";
                var existing=AssetDatabase.LoadMainAssetAtPath(path);
                if(existing!=null&&(!(existing is EquipmentData item)||item.Id!=definition.Id))throw new InvalidOperationException("Conflicting asset preserved: "+path);
                if(existing!=null&&matches.Length==1&&existing!=matches[0])throw new InvalidOperationException("Two assets have the same equipment ID: "+definition.Id);
            }
        }
        // Public for isolated Editor regression fixtures as well as explicit production installation.
        public static void AddMissing(BattleCatalog catalog,string root,IReadOnlyDictionary<string,Sprite> icons)
        {
            ValidateDestinations(catalog,root);
            if(!AssetDatabase.IsValidFolder(root))throw new InvalidOperationException("Create the destination folder first.");
            foreach(var definition in definitions)
                if(icons==null||!icons.TryGetValue(definition.Id,out var icon)||icon==null)throw new InvalidOperationException("Missing illustrated icon: "+definition.Id);
            var items=(catalog.Equipment??Array.Empty<EquipmentData>()).ToList();bool changed=false;
            foreach(var definition in definitions)
            {
                string path=root+"/"+definition.File+".asset";
                var item=items.FirstOrDefault(e=>e!=null&&e.Id==definition.Id)??AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
                if(item==null)
                {
                    item=ScriptableObject.CreateInstance<EquipmentData>();item.Id=definition.Id;item.DisplayName=definition.Name;
                    item.Slot=EquipmentSlot.Weapon;item.Weapon=definition.Weapon;item.Bonus=definition.Bonus;item.BuyPrice=240;
                    item.RequiredStoryFlag=CampaignStages.Id(0);item.Icon=icons[definition.Id];
                    item.SourceNote="일반 상점 무기 · 메인 1화 완료 후 구매";
                    AssetDatabase.CreateAsset(item,path);
                }
                if(!items.Contains(item)){items.Add(item);changed=true;}
            }
            if(changed){catalog.Equipment=items.ToArray();EditorUtility.SetDirty(catalog);}
            AssetDatabase.SaveAssets();
        }
        static Dictionary<string,Sprite> ImportIcons()
        {
            AssetDatabase.ImportAsset(IconPath,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("Missing standard weapon icon atlas: "+IconPath);
            var existing=AssetDatabase.LoadAllAssetsAtPath(IconPath).OfType<Sprite>().ToArray();
            if(definitions.All(d=>existing.Count(s=>s.name==d.Id)==1))
            {
                // Keep source pixels at 1774x887, including installations made before this setting
                // became explicit. Reimport preserves the existing sprite IDs and reviewed rects.
                if(importer.npotScale!=TextureImporterNPOTScale.None)
                {
                    importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
                    existing=AssetDatabase.LoadAllAssetsAtPath(IconPath).OfType<Sprite>().ToArray();
                }
                return definitions.ToDictionary(d=>d.Id,d=>existing.Single(s=>s.name==d.Id));
            }
            if(existing.Length>0)throw new InvalidOperationException("Existing incomplete/custom icon slicing preserved; provide seven named sprites or a fresh atlas.");
            byte[] png=File.ReadAllBytes(IconPath);
            if(png.Length<24||png[0]!=137||png[1]!=80||png[2]!=78||png[3]!=71)throw new InvalidOperationException("Expected PNG icon atlas.");
            int SourceInt(int offset)=>(png[offset]<<24)|(png[offset+1]<<16)|(png[offset+2]<<8)|png[offset+3];
            int width=SourceInt(16),height=SourceInt(20);
            // Source coordinates were measured from each alpha-connected main illustration,
            // with a 5px safety margin; faint grid-edge pixels are deliberately outside the rect.
            var layout=File.ReadAllLines("Tools/standard-weapon-icons.csv").Skip(1).Where(line=>!string.IsNullOrWhiteSpace(line)).Select(line=>line.Split(',')).ToArray();
            if(layout.Length!=definitions.Length)throw new InvalidOperationException("Expected seven reviewed icon rectangles.");
            foreach(var row in layout)
            {
                if(row.Length!=7||int.Parse(row[5])!=width||int.Parse(row[6])!=height)throw new InvalidOperationException("Icon source size differs from the reviewed slicing layout.");
                int x=int.Parse(row[1]),y=int.Parse(row[2]),w=int.Parse(row[3]),h=int.Parse(row[4]);
                if(x<0||y<0||w<=0||h<=0||x+w>width||y+h>height)throw new InvalidOperationException("Icon rectangle lies outside its source: "+row[0]);
            }
            foreach(var definition in definitions)if(layout.Count(row=>row[0]==definition.Id)!=1)throw new InvalidOperationException("Missing or duplicate icon layout: "+definition.Id);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.spritePixelsPerUnit=100;
            importer.SaveAndReimport();
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);
            if(provider==null)throw new InvalidOperationException("Sprite provider unavailable; slicing aborted.");
            provider.InitSpriteEditorDataProvider();
            var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
            if(capability==null)throw new InvalidOperationException("Sprite edit capability unavailable; slicing aborted.");
            foreach(var flag in new[]{EEditCapability.EditSpriteName,EEditCapability.EditSpriteRect,EEditCapability.EditPivot,EEditCapability.CreateAndDeleteSprite})
                if(!capability.GetEditCapability().HasCapability(flag))throw new InvalidOperationException("Unsupported sprite operation: "+flag);
            var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if(names==null)throw new InvalidOperationException("Sprite name/file ID provider unavailable; slicing aborted.");
            var rects=new List<SpriteRect>();var pairs=new List<SpriteNameFileIdPair>();
            for(int i=0;i<definitions.Length;i++)
            {
                var id=GUID.Generate();string name=definitions[i].Id;var row=layout.Single(value=>value[0]==name);
                rects.Add(new SpriteRect{name=name,spriteID=id,rect=new Rect(int.Parse(row[1]),int.Parse(row[2]),int.Parse(row[3]),int.Parse(row[4])),alignment=SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)});
                pairs.Add(new SpriteNameFileIdPair(name,id));
            }
            provider.SetSpriteRects(rects.ToArray());names.SetNameFileIdPairs(pairs);provider.Apply();importer.SaveAndReimport();
            var sprites=AssetDatabase.LoadAllAssetsAtPath(IconPath).OfType<Sprite>().ToDictionary(s=>s.name);
            foreach(var definition in definitions)if(!sprites.ContainsKey(definition.Id))throw new InvalidOperationException("Imported icon missing: "+definition.Id);
            return sprites;
        }
    }
}
