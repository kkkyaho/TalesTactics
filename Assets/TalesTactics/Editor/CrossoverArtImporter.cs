using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
namespace TalesTactics.Editor
{
    public static class CrossoverArtImporter
    {
        static float F(string text)=>float.Parse(text,CultureInfo.InvariantCulture);
        public static void ImportBatch(){Debug.Log(Import());}
        public static string Import()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset");
            if(catalog.CrossoverReady)throw new InvalidOperationException("Published crossover art requires an explicit reviewed update");
            AssetDatabase.Refresh();
            var groups=File.ReadAllLines("Tools/crossover-layout.csv").Skip(1).Select(l=>l.Split(',')).GroupBy(v=>v[0]).ToArray();
            if(groups.Length!=26||groups.Any(g=>g.Count()!=(g.Key.StartsWith("civilian-")?4:g.Key=="boundary-device"?12:80)))throw new InvalidOperationException("Expected 20 boss atlases, early Hyades, device and four civilians");
            catalog.Civilians=new DirectionalSprites[4];
            var polygons=File.ReadAllLines("Tools/crossover-outlines.csv").Select(l=>l.Split(',')).GroupBy(v=>v[0]).ToDictionary(g=>g.Key,g=>g.Select(v=>v.Skip(1).Select(p=>p.Split(':')).Select(p=>new Vector2(F(p[0]),F(p[1]))).ToArray()).ToList());
            var factory=new SpriteDataProviderFactories();factory.Init();
            foreach(var group in groups.SelectMany(g=>g).GroupBy(v=>v[1]))
            {
                var rows=group.ToArray();string path="Assets/TalesTactics/Art/Crossover/"+rows[0][1]+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new InvalidOperationException(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.spritePixelsPerUnit=F(rows[0][9]);importer.SaveAndReimport();
                var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
                var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();var outline=provider.GetDataProvider<ISpriteOutlineDataProvider>();
                var previous=provider.GetSpriteRects();var rects=new List<SpriteRect>();var pairs=new List<SpriteNameFileIdPair>();
                foreach(var row in rows)
                {
                    string name=row[0]+"_"+row[2]+"_"+row[3];var old=previous.FirstOrDefault(r=>r.name==name);var guid=old==null?GUID.Generate():old.spriteID;
                    if(!polygons.ContainsKey(name))throw new InvalidOperationException("Missing outline "+name);
                    rects.Add(new SpriteRect{name=name,spriteID=guid,rect=new Rect(F(row[4]),F(row[11])-F(row[5])-F(row[7]),F(row[6]),F(row[7])),alignment=SpriteAlignment.Custom,pivot=new Vector2(F(row[8]),.01f)});pairs.Add(new SpriteNameFileIdPair(name,guid));
                }
                provider.SetSpriteRects(rects.ToArray());names.SetNameFileIdPairs(pairs);foreach(var rect in rects)outline.SetOutlines(rect.spriteID,polygons[rect.name]);provider.Apply();importer.SaveAndReimport();
            }
            foreach(var group in groups)
            {
                var sprites=group.Select(r=>r[1]).Distinct().SelectMany(source=>AssetDatabase.LoadAllAssetsAtPath("Assets/TalesTactics/Art/Crossover/"+source+".png").OfType<Sprite>()).ToDictionary(s=>s.name);
                Func<int,int,int> approved=(pose,direction)=>ApprovedPose(group.Key,pose,direction);
                Func<int,DirectionalSprites> frame=i=>new DirectionalSprites{Front=sprites[group.Key+"_"+approved(i,0)+"_0"],Back=sprites[group.Key+"_"+approved(i,1)+"_1"],Right=sprites[group.Key+"_"+approved(i,2)+"_2"],Left=sprites[group.Key+"_"+approved(i,3)+"_3"]};
                if(group.Key.StartsWith("civilian-")){catalog.Civilians[int.Parse(group.Key.Substring(9))]=frame(0);continue;}
                var c=CrossoverCampaign.Boss(catalog,group.Key=="hyades-human"?"hyades":group.Key,group.Key=="hyades-human");c.Sprites=frame(0);c.Portrait=c.Sprites.Front;
                if(c.Id=="boundary-device")
                {c.Poses=new CharacterPoses{Cast=frame(2),Guard=frame(0),Damage=frame(2),Dead=new DirectionalSpriteClip{Frames=new[]{frame(1)}}};EditorUtility.SetDirty(c);continue;}
                c.Poses=new CharacterPoses{Attack=frame(8),Cast=frame(11),Guard=frame(17),Damage=frame(16),
                    Dead=new DirectionalSpriteClip{Frames=new[]{frame(18),frame(19)},FramesPerSecond=5},
                    Walk=new DirectionalSpriteClip{Frames=new[]{frame(2),frame(3),frame(4),frame(5)},FramesPerSecond=8},
                    AttackMotion=new DirectionalSpriteClip{Frames=new[]{frame(6),frame(7),frame(8),frame(9),frame(1)},ReleaseFrame=2},
                    Skill=new DirectionalSpriteClip{Frames=new[]{frame(10),frame(11),frame(12),frame(13),frame(1)},ReleaseFrame=2},
                    Ultimate=new DirectionalSpriteClip{Frames=new[]{frame(10),frame(14),frame(11),frame(15),frame(12),frame(13),frame(1)},ReleaseFrame=3}};
                EditorUtility.SetDirty(c);
            }
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();return "Connected reviewed boss directions, collapse motions, early Hyades and civilians; CrossoverReady remains false pending visual review.";
        }
        static int ApprovedPose(string id,int pose,int direction)
        {
            if(id=="artorius")return direction==0&&pose==12?9:direction==1&&pose==3?1:direction==1&&pose==14?6:direction==1&&pose==16?11:direction==1&&pose==17?12:direction==3&&pose==1?0:pose;
            if(id=="dhaos"&&direction==3)return pose==14?11:pose==15?12:pose;
            if(id=="dhaos"&&direction==2)return pose==2||pose==4?0:pose==5?3:pose;
            if(id=="vholran"&&direction==1)return pose==4?2:pose==5?3:pose;
            if(id=="shigure"&&direction==1)return pose==4?2:pose==17?11:pose;
            if(id=="jahmir")return direction==1&&pose==17?16:direction==2&&(pose==3||pose==5||pose==11||pose==13||pose==15)?pose-1:pose;
            if(id=="ganabelt"&&(direction==3||direction==2)&&pose>=4&&pose<=5)return pose-2;
            if(id=="shizel"&&direction==1&&pose==16)return 17;
            if(id=="maxwell"&&direction==1&&pose==13)return 12;
            if(id=="ordo"&&direction==1&&pose==16)return 17;
            return pose;
        }
        public static void RefineTractorStatusBatch()
        {
            foreach(var id in new[]{"boss.jahmir.0","boss.maxwell.1"})
            {
                var s=AssetDatabase.LoadAssetAtPath<SkillData>("Assets/TalesTactics/Content/Crossover/"+id+".asset");
                if(s==null)throw new InvalidOperationException(id);
                foreach(var effect in s.Effects.Where(e=>e.Status==StatusKind.Cage))effect.Status=StatusKind.Slow;EditorUtility.SetDirty(s);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
