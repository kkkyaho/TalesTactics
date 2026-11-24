using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TalesTactics.Editor
{
    // Explicitly imports only the new pilot sheets. Never regenerates Content or replaces old art.
    public static class VisualPilotArtImporter
    {
        const string ArtRoot="Assets/TalesTactics/Art/VisualPilot/Alphen/";
        const string AssetPath="Assets/TalesTactics/Resources/TalesTactics/VisualPilot/AlphenMotion.asset";
        const float PixelsPerUnit=188.5f; // Native 232px body / original 242px body at 196.61841 PPU.
        static readonly string[] Directions={"front","back","left","right"};
        [Serializable] public sealed class Point { public float x,y; }
        [Serializable] public sealed class Frame {public string name;public int x,y,width,height;public Point pivot;}
        [Serializable] public sealed class Sheet {public string path,sha256;public int width,height;public Frame[] frames;}
        [Serializable] public sealed class Manifest {public int schemaVersion=1;public float pixelsPerUnit;public Sheet[] sheets;}
        [Serializable] sealed class Report {public bool passed;public string asset;public int idleFrames,attackFrames,damageFrames,sprites;public float pixelsPerUnit;}

        public static string Import()
        {
            string manifestPath=Path.GetFullPath("Docs/VisualPilot20261124/art-layout.json");
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if(manifest==null||manifest.schemaVersion!=1||manifest.sheets==null||manifest.sheets.Length!=2||
                Mathf.Abs(manifest.pixelsPerUnit-PixelsPerUnit)>.001f)throw new InvalidDataException("Unexpected pilot art manifest.");
            var sprites=new Dictionary<string,Sprite>(StringComparer.Ordinal);
            foreach(var sheet in manifest.sheets)
            {
                if(sheet.path!=ArtRoot+"idle-v1.png"&&sheet.path!=ArtRoot+"action-v1.png")throw new InvalidDataException("Unexpected art path.");
                string hash=Hash(File.ReadAllBytes(sheet.path));
                if(!string.Equals(hash,sheet.sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Pilot source changed: "+sheet.path);
                if(sheet.frames==null||sheet.frames.Length!=16||sheet.frames.Select(f=>f.name).Distinct().Count()!=16)
                    throw new InvalidDataException("Exactly sixteen unique poses required.");
                AssetDatabase.ImportAsset(sheet.path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(sheet.path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit=PixelsPerUnit;importer.npotScale=TextureImporterNPOTScale.None;
                importer.maxTextureSize=2048;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
                importer.isReadable=false;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                var factories=new SpriteDataProviderFactories();factories.Init();
                var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
                var old=provider.GetSpriteRects().ToDictionary(s=>s.name);
                var rects=new List<SpriteRect>();var ids=new List<SpriteNameFileIdPair>();
                foreach(var f in sheet.frames)
                {
                    if(f.width<=0||f.height<=0||f.x<0||f.y<0||f.x+f.width>sheet.width||f.y+f.height>sheet.height||
                        f.pivot==null||f.pivot.x<0||f.pivot.x>1||f.pivot.y<0||f.pivot.y>1)
                        throw new InvalidDataException("Out of bounds pilot frame "+f.name);
                    var id=old.TryGetValue(f.name,out var previous)?previous.spriteID:GUID.Generate();
                    rects.Add(new SpriteRect{name=f.name,rect=new Rect(f.x,f.y,f.width,f.height),
                        alignment=SpriteAlignment.Custom,pivot=new Vector2(f.pivot.x,f.pivot.y),spriteID=id});
                    ids.Add(new SpriteNameFileIdPair(f.name,id));
                }
                provider.SetSpriteRects(rects.ToArray());
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(ids);
                var outline=provider.GetDataProvider<ISpriteOutlineDataProvider>();
                foreach(var rect in rects)
                {
                    float x=rect.rect.width*.5f,y=rect.rect.height*.5f;
                    outline.SetOutlines(rect.spriteID,new List<Vector2[]>{new[]{new Vector2(-x,-y),new Vector2(x,-y),new Vector2(x,y),new Vector2(-x,y)}});
                }
                provider.Apply();importer.SaveAndReimport();
                AssetDatabase.ImportAsset(sheet.path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                var imported=AssetDatabase.LoadAllAssetsAtPath(sheet.path).OfType<Sprite>().ToArray();
                if(imported.Length!=16||imported.Any(s=>Mathf.Abs(s.pixelsPerUnit-PixelsPerUnit)>.001f))
                    throw new InvalidDataException("Imported pilot frame count or scale mismatch.");
                foreach(var sprite in imported)sprites.Add(sprite.name,sprite);
                if(Hash(File.ReadAllBytes(sheet.path))!=hash)throw new InvalidDataException("Importer changed source PNG.");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));AssetDatabase.Refresh();
            var data=AssetDatabase.LoadAssetAtPath<AlphenVisualPilotMotion>(AssetPath);
            if(data==null){data=ScriptableObject.CreateInstance<AlphenVisualPilotMotion>();AssetDatabase.CreateAsset(data,AssetPath);}
            data.Idle=Clip(sprites,"idle",4);data.Attack=Clip(sprites,"attack",3);data.Damage=Clip(sprites,"damage",1);
            // Preserve any explicitly authored equipment variants on a later rerun.
            EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
            var result=new Report{passed=true,asset=AssetPath,idleFrames=4,attackFrames=3,damageFrames=1,sprites=sprites.Count,pixelsPerUnit=PixelsPerUnit};
            return JsonUtility.ToJson(result,true);
        }
        static DirectionalSpriteClip Clip(Dictionary<string,Sprite> sprites,string kind,int count)
        {
            var clip=new DirectionalSpriteClip{Frames=new DirectionalSprites[count],FramesPerSecond=8,ReleaseFrame=1};
            for(int i=0;i<count;i++)
            {
                var frame=new DirectionalSprites();
                frame.Front=sprites["alphen-pilot-"+kind+"-front-"+i];frame.Back=sprites["alphen-pilot-"+kind+"-back-"+i];
                frame.Left=sprites["alphen-pilot-"+kind+"-left-"+i];frame.Right=sprites["alphen-pilot-"+kind+"-right-"+i];clip.Frames[i]=frame;
            }
            return clip;
        }
        static string Hash(byte[] bytes){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    }
}
