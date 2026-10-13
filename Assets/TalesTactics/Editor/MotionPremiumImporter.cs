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
    // Explicit live-Editor invocation. No content regeneration or automatic reimport hook.
    public static class MotionPremiumImporter
    {
        const string Root="Assets/TalesTactics/Art/MotionPremium/";
        static float F(string s)=>float.Parse(s,CultureInfo.InvariantCulture);
        public static string Import()
        {
            AssetDatabase.Refresh();
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            if(catalog==null||catalog.Characters.Length!=10)throw new InvalidOperationException("Expected ten heroes");
            if(catalog.Characters.Any(c=>c.Poses.AttackMotion.Frames.Length!=4||c.Poses.Ultimate.Frames.Length!=4||c.Poses.Walk.Frames.Length!=4))
                throw new InvalidOperationException("Already upgraded or edited clips: review baseline before reapplying");
            var groups=File.ReadAllLines("Tools/motion-premium-layout.csv").Skip(1).Select(l=>l.Split(',')).GroupBy(v=>v[0]).ToArray();
            if(groups.Length!=10||groups.Any(g=>g.Count()!=72&&g.Count()!=80))throw new InvalidOperationException("Expected complete 18 or 20 pose atlases");
            var polygons=File.ReadAllLines("Tools/motion-premium-outlines.csv").Select(l=>l.Split(',')).GroupBy(v=>v[0]).ToDictionary(g=>g.Key,g=>g.Select(v=>v.Skip(1).Select(p=>p.Split(':')).Select(p=>new Vector2(F(p[0]),F(p[1]))).ToArray()).ToList());
            var factories=new SpriteDataProviderFactories();factories.Init();
            foreach(var group in groups)
            {
                var rows=group.ToArray();string path=Root+group.Key+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null)throw new InvalidOperationException("Missing "+path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
                importer.spritePixelsPerUnit=F(rows[0][8]);importer.SaveAndReimport();
                var provider=factories.GetSpriteEditorDataProviderFromObject(importer);
                if(provider==null)throw new InvalidOperationException("No sprite provider");
                provider.InitSpriteEditorDataProvider();
                var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
                if(capability==null)throw new InvalidOperationException("No edit capability");
                foreach(var flag in new[]{EEditCapability.EditSpriteName,EEditCapability.EditSpriteRect,EEditCapability.EditPivot,EEditCapability.CreateAndDeleteSprite})
                    if(!capability.GetEditCapability().HasCapability(flag))throw new InvalidOperationException("Unsupported "+flag);
                var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();var outline=provider.GetDataProvider<ISpriteOutlineDataProvider>();
                if(names==null||outline==null)throw new InvalidOperationException("Missing names/outlines");
                var previous=provider.GetSpriteRects();var rects=new List<SpriteRect>();var pairs=new List<SpriteNameFileIdPair>();
                foreach(var v in rows)
                {
                    string name=group.Key+"_"+v[1]+"_"+v[2];var old=previous.FirstOrDefault(r=>r.name==name);var guid=old==null?GUID.Generate():old.spriteID;
                    if(!polygons.ContainsKey(name))throw new InvalidOperationException("Missing outline "+name);
                    rects.Add(new SpriteRect{name=name,spriteID=guid,rect=new Rect(F(v[3]),F(v[10])-F(v[4])-F(v[6]),F(v[5]),F(v[6])),alignment=SpriteAlignment.Custom,pivot=new Vector2(F(v[7]),.01f)});
                    pairs.Add(new SpriteNameFileIdPair(name,guid));
                }
                provider.SetSpriteRects(rects.ToArray());names.SetNameFileIdPairs(pairs);
                foreach(var rect in rects)outline.SetOutlines(rect.spriteID,polygons[rect.name]);
                provider.Apply();importer.SaveAndReimport();
            }
            var sheets=catalog.Characters.ToDictionary(c=>c.Id,c=>AssetDatabase.LoadAllAssetsAtPath(Root+c.Id+".png").OfType<Sprite>().ToDictionary(s=>s.name));
            foreach(var c in catalog.Characters)for(int i=0;i<sheets[c.Id].Count/4;i++)Frame(sheets[c.Id],c.Id,i);
            foreach(var c in catalog.Characters)
            {
                var p=c.Poses;var s=sheets[c.Id];Func<int,DirectionalSprites> f=i=>Frame(s,c.Id,i);
                var old=p.Ultimate.Frames;var walk=p.Walk.Frames;
                p.Walk=new DirectionalSpriteClip{Frames=new[]{walk[0],f(0),walk[1],f(1),walk[2],f(2),walk[3],f(3)},FramesPerSecond=12};
                p.AttackMotion=new DirectionalSpriteClip{Frames=new[]{f(4),f(5),f(6),f(7),f(8),p.Attack,f(12),f(13),f(14),f(15)},ReleaseFrame=5};
                p.Skill=new DirectionalSpriteClip{Frames=new[]{f(4),f(5),f(6),f(7),f(8),f(9),p.Cast,p.Attack,f(12),f(13),f(14),f(15),f(16),f(17)},ReleaseFrame=7};
                var ultimate=new List<DirectionalSprites>{old[0],f(4),f(5),f(6),f(7),f(8),p.Cast,f(9),f(10),old[1],old[2],f(11),f(12),p.Attack,f(13),f(14),f(15),f(16),f(17)};
                if(s.Count==80){ultimate.Add(f(18));ultimate.Add(f(19));}ultimate.Add(old[3]);
                p.Ultimate=new DirectionalSpriteClip{Frames=ultimate.ToArray(),ReleaseFrame=10};EditorUtility.SetDirty(c);
                foreach(var skill in new[]{c.BasicAttack}.Concat(c.Skills).Concat(new[]{c.UltimateSkill}).Where(x=>x!=null).Distinct())
                {
                    if(skill.Presentation==null)skill.Presentation=new SkillPresentation();
                    skill.Presentation.Windup=Mathf.Max(skill.Presentation.Windup,skill.IsUltimate?.65f:skill==c.BasicAttack?.32f:.42f);
                    skill.Presentation.Recovery=Mathf.Max(skill.Presentation.Recovery,skill.IsUltimate?.8f:skill==c.BasicAttack?.4f:.5f);
                    EditorUtility.SetDirty(skill);
                }
            }
            AssetDatabase.SaveAssets();
            return "Connected "+sheets.Sum(s=>s.Value.Count)+" new directional sprites; walk 8, attack 10, skill 14, ultimate 20-22.";
        }
        static DirectionalSprites Frame(Dictionary<string,Sprite> sheet,string id,int pose)
        {
            string prefix=id+"_"+pose+"_";
            return new DirectionalSprites{Front=sheet[prefix+0],Back=sheet[prefix+1],Right=sheet[prefix+2],Left=sheet[prefix+3]};
        }
    }
}
