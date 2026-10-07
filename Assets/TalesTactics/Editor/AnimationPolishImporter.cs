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
    // Explicitly invoked through the live Editor eval command. Never regenerates content.
    public static class AnimationPolishImporter
    {
        const string Root="Assets/TalesTactics/Art/PixelCampaign/";
        static float F(string s)=>float.Parse(s,CultureInfo.InvariantCulture);
        public static string Import()
        {
            AssetDatabase.Refresh();
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            if(catalog==null)throw new InvalidOperationException("Missing battle catalog");
            var characters=catalog.Characters.Concat(catalog.Enemies).Distinct().ToArray();
            if(characters.Length!=20||characters.Select(c=>c.Id).Distinct().Count()!=20)
                throw new InvalidOperationException("Expected 20 distinct catalog characters");
            var groups=File.ReadAllLines("Tools/animation-polish-layout.csv").Skip(1).Select(l=>l.Split(',')).GroupBy(v=>v[0]).ToArray();
            if(groups.Length!=20||groups.Any(g=>g.Count()!=16))throw new InvalidOperationException("Expected all 20 complete supplemental sheets");
            var polygons=File.ReadAllLines("Tools/animation-polish-outlines.csv").Select(l=>l.Split(',')).GroupBy(v=>v[0]).ToDictionary(g=>g.Key,g=>g.Select(v=>v.Skip(1).Select(p=>p.Split(':')).Select(p=>new Vector2(F(p[0]),F(p[1]))).ToArray()).ToList());
            var factories=new SpriteDataProviderFactories();factories.Init();
            foreach(var group in groups)
            {
                var rows=group.ToArray();string path=Root+group.Key+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null)throw new InvalidOperationException("Missing texture: "+path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
                importer.spritePixelsPerUnit=F(rows[0][8]);importer.SaveAndReimport();
                var provider=factories.GetSpriteEditorDataProviderFromObject(importer);
                if(provider==null)throw new InvalidOperationException("No sprite provider: "+path);
                provider.InitSpriteEditorDataProvider();
                var edit=provider.GetDataProvider<ISpriteFrameEditCapability>();
                if(edit==null)throw new InvalidOperationException("No edit capability: "+path);
                var capabilities=edit.GetEditCapability();
                foreach(var flag in new[]{EEditCapability.EditSpriteName,EEditCapability.EditSpriteRect,EEditCapability.EditPivot,EEditCapability.CreateAndDeleteSprite})
                    if(!capabilities.HasCapability(flag))throw new InvalidOperationException("Unsupported "+flag+": "+path);
                var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
                var outline=provider.GetDataProvider<ISpriteOutlineDataProvider>();
                if(names==null||outline==null)throw new InvalidOperationException("Missing names/outline provider: "+path);
                var previous=provider.GetSpriteRects();var rects=new List<SpriteRect>();var pairs=new List<SpriteNameFileIdPair>();
                foreach(var v in rows)
                {
                    string name=group.Key+"_"+v[1]+"_"+v[2];var old=previous.FirstOrDefault(r=>r.name==name);var guid=old==null?GUID.Generate():old.spriteID;
                    rects.Add(new SpriteRect{name=name,spriteID=guid,rect=new Rect(F(v[3]),F(v[10])-F(v[4])-F(v[6]),F(v[5]),F(v[6])),alignment=SpriteAlignment.Custom,pivot=new Vector2(F(v[7]),0.01f)});
                    pairs.Add(new SpriteNameFileIdPair(name,guid));
                }
                provider.SetSpriteRects(rects.ToArray());names.SetNameFileIdPairs(pairs);
                foreach(var rect in rects)outline.SetOutlines(rect.spriteID,polygons[rect.name]);
                provider.Apply();importer.SaveAndReimport();
            }
            // Resolve every reference before changing any CharacterData.
            var sheets=characters.ToDictionary(c=>c.Id,c=>AssetDatabase.LoadAllAssetsAtPath(Root+c.Id+"-polish.png").OfType<Sprite>().ToDictionary(s=>s.name));
            foreach(var c in characters)for(int r=0;r<4;r++)Frame(sheets[c.Id],c.Id,r);
            foreach(var c in characters)
            {
                var p=c.Poses;var sheet=sheets[c.Id];var anticipation=Frame(sheet,c.Id,0);var follow=Frame(sheet,c.Id,1);
                var preparation=Frame(sheet,c.Id,2);var recovery=Frame(sheet,c.Id,3);
                var old=p.Ultimate.Frames;
                var charge=old.Length==2?old[0]:old[1];var release=old.Length==2?old[1]:old[2];
                p.AttackMotion=new DirectionalSpriteClip{Frames=new[]{anticipation,p.Attack,follow,recovery},ReleaseFrame=1};
                p.Skill=new DirectionalSpriteClip{Frames=new[]{preparation,p.Cast,p.Attack,follow,recovery},ReleaseFrame=2};
                p.Ultimate=new DirectionalSpriteClip{Frames=new[]{preparation,charge,release,recovery},ReleaseFrame=2};
                EditorUtility.SetDirty(c);
            }
            AssetDatabase.SaveAssets();return "Imported 320 sprites and connected attack/skill/ultimate phases for 20 characters";
        }
        static DirectionalSprites Frame(Dictionary<string,Sprite> s,string id,int row)
        {
            string prefix=id+"-polish_"+row+"_";
            // Visual review found reversed profile order in these two Shionne rows.
            bool reverse=id=="shionne"&&(row==0||row==3);
            return new DirectionalSprites{Front=s[prefix+0],Back=s[prefix+1],Right=s[prefix+(reverse?3:2)],Left=s[prefix+(reverse?2:3)]};
        }
    }
}
