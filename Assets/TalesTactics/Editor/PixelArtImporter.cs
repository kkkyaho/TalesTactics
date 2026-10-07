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
    public static class PixelArtImporter
    {
        static float F(string s)=>float.Parse(s,CultureInfo.InvariantCulture);
        [MenuItem("Tales Tactics/Pixel Campaign/Import Authored Sprites")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            var groups=File.ReadAllLines("Tools/pixel-campaign-layout.csv").Skip(1).Select(l=>l.Split(',')).GroupBy(v=>v[0]);
            var polygons=File.ReadAllLines("Tools/pixel-campaign-outlines.csv").Select(l=>l.Split(',')).GroupBy(v=>v[0]).ToDictionary(g=>g.Key,g=>g.Select(v=>v.Skip(1).Select(p=>p.Split(':')).Select(p=>new Vector2(F(p[0]),F(p[1]))).ToArray()).ToList());
            foreach(var group in groups)
            {
                string id=group.Key,path=id=="PixelProps"?"Assets/TalesTactics/Resources/TalesTactics/PixelProps.png":"Assets/TalesTactics/Art/PixelCampaign/"+id+".png";
                var rows=group.ToArray();var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
                importer.spritePixelsPerUnit=F(rows[0][8]);importer.SaveAndReimport();
                var factories=new SpriteDataProviderFactories();factories.Init();
                var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
                var previous=provider.GetSpriteRects();var rects=new List<SpriteRect>();var pairs=new List<SpriteNameFileIdPair>();
                foreach(var v in rows)
                {
                    string name=id+"_"+v[1]+"_"+v[2];var old=previous.FirstOrDefault(r=>r.name==name);var guid=old==null?GUID.Generate():old.spriteID;
                    rects.Add(new SpriteRect{name=name,spriteID=guid,rect=new Rect(F(v[3]),F(v[10])-F(v[4])-F(v[6]),F(v[5]),F(v[6])),alignment=SpriteAlignment.Custom,pivot=new Vector2(F(v[7]),0.01f)});
                    pairs.Add(new SpriteNameFileIdPair(name,guid));
                }
                provider.SetSpriteRects(rects.ToArray());provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
                var outline=provider.GetDataProvider<ISpriteOutlineDataProvider>();
                foreach(var rect in rects)outline.SetOutlines(rect.spriteID,polygons[rect.name]);
                provider.Apply();importer.SaveAndReimport();
            }
            foreach(var guid in AssetDatabase.FindAssets("t:CharacterData"))
            {
                    var data=AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guid));string id=data.Id;
                    string root="Assets/TalesTactics/Art/PixelCampaign/";
                    bool paired=File.Exists(root+id+"-hq.png");
                    string main=paired?id+"-hq":id,motion=paired?id+"-motion":id;
                    if(!File.Exists(root+main+".png")||!File.Exists(root+motion+".png"))continue;
                    // Only completed pairs replace an existing character's art.
                    var a=AssetDatabase.LoadAllAssetsAtPath(root+main+".png").OfType<Sprite>().ToDictionary(s=>s.name);
                    var b=AssetDatabase.LoadAllAssetsAtPath(root+motion+".png").OfType<Sprite>().ToDictionary(s=>s.name);
                    if(a.Count!=(paired?16:20)||b.Count!=(paired?16:20))continue;
                    Func<int,DirectionalSprites> frame=r=>Frame(a,main,r);
                    Func<int,DirectionalSprites> move=r=>Frame(b,motion,r);
                    var idle=frame(0);var attack=frame(paired?1:3);
                    var walkA=move(paired?0:1);var walkB=move(paired?1:2);
                    var cast=paired?frame(2):attack;var guard=paired?frame(3):idle;
                    var hurt=paired?move(2):idle;var dead=move(paired?3:4);
                    if(id=="tear") {var swap=dead.Front;dead.Front=dead.Left;dead.Left=swap;}
                    if(id=="shionne")
                    {
                        var swap=idle.Right;idle.Right=idle.Left;idle.Left=swap;
                        swap=guard.Right;guard.Right=guard.Left;guard.Left=swap;
                        var right=AssetDatabase.LoadAllAssetsAtPath(root+"shionne-right.png").OfType<Sprite>().ToDictionary(s=>s.name);
                        if(right.Count!=4)throw new InvalidOperationException("Missing corrected Shionne right-facing strip");
                        walkA.Right=right["shionne-right_0_0"];walkB.Right=right["shionne-right_0_1"];
                        hurt.Right=right["shionne-right_0_2"];dead.Right=right["shionne-right_0_3"];
                    }
                    var walkFrames=new[]{idle,walkA,idle,walkB};var ultimateFrames=new[]{cast,attack};
                    string extra=id+"-extra";
                    if(File.Exists(root+extra+".png"))
                    {
                        var e=AssetDatabase.LoadAllAssetsAtPath(root+extra+".png").OfType<Sprite>().ToDictionary(s=>s.name);
                        if(e.Count!=16)throw new InvalidOperationException("Incomplete supplementary sheet: "+id);
                        var passingA=Frame(e,extra,0);var passingB=Frame(e,extra,1);
                        var charge=Frame(e,extra,2);var release=Frame(e,extra,3);
                        if(id=="shionne")
                        {
                            var swap=passingA.Right;passingA.Right=passingA.Left;passingA.Left=swap;
                            swap=release.Right;release.Right=release.Left;release.Left=swap;
                        }
                        walkFrames=new[]{walkA,passingA,walkB,passingB};
                        ultimateFrames=new[]{charge,release};
                    }
                    data.Sprites=idle;data.Portrait=idle.Front;
                    data.Poses=new CharacterPoses{Attack=attack,Cast=cast,Guard=guard,Damage=hurt,
                        Walk=new DirectionalSpriteClip{Frames=walkFrames,FramesPerSecond=8},
                        Dead=new DirectionalSpriteClip{Frames=new[]{hurt,dead,dead},FramesPerSecond=6},
                        Skill=new DirectionalSpriteClip{Frames=new[]{cast,attack},FramesPerSecond=8},
                        Ultimate=new DirectionalSpriteClip{Frames=ultimateFrames,FramesPerSecond=8}};
                    EditorUtility.SetDirty(data);
            }
            foreach(string texture in new[]{"PixelTerrain","PixelBackdrops"})
            {
                var terrain=(TextureImporter)AssetImporter.GetAtPath("Assets/TalesTactics/Resources/TalesTactics/"+texture+".png");
                if(terrain!=null){terrain.textureType=TextureImporterType.Default;terrain.filterMode=FilterMode.Point;terrain.mipmapEnabled=false;terrain.textureCompression=TextureImporterCompression.Uncompressed;terrain.maxTextureSize=4096;terrain.SaveAndReimport();}
            }
            AssetDatabase.SaveAssets();
        }
        static DirectionalSprites Frame(Dictionary<string,Sprite> sprites,string id,int r)=>new DirectionalSprites{Front=sprites[id+"_"+r+"_0"],Back=sprites[id+"_"+r+"_1"],Right=sprites[id+"_"+r+"_2"],Left=sprites[id+"_"+r+"_3"]};
    }
}
