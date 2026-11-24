using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TalesTactics.Editor
{
    // Explicit authoring operation for the nine new roster sheets. The original
    // Alphen pilot and CharacterData/EquipmentAppearance assets are not rewritten.
    public static class RosterMotionArtImporter
    {
        const string ManifestPath="Docs/RosterMotion20261125/art-layout.json";
        const string ArtRoot="Assets/TalesTactics/Art/RosterMotion20261125/";
        const string ProfileRoot="Assets/TalesTactics/Resources/TalesTactics/CharacterMotion";
        static readonly string[] Owners={"cless","farah","jade","kisara","mint","natalia","shionne","tear","velvet"};
        static readonly string[] Directions={"front","back","left","right"};
        [Serializable] public sealed class Point {public float x,y;}
        [Serializable] public sealed class Frame {public string name;public int x,y,width,height;public Point pivot;}
        [Serializable] public sealed class Sheet {public string path,sha256;public int width,height;public float pixelsPerUnit;public Frame[] frames;}
        [Serializable] public sealed class Character {public string characterId;public float pixelsPerUnit;public Sheet[] sheets;}
        [Serializable] public sealed class Manifest {public int schemaVersion;public Character[] characters;}
        [Serializable] sealed class SheetReport
        {
            public string path,sha256,textureGuid;
            public int sprites,preservedSpriteIds,preservedLocalFileIds;
            public float pixelsPerUnit;
            public bool capabilitiesPassed,nativeSourceUnchanged;
        }
        [Serializable] sealed class CharacterReport {public string characterId,asset;public int sprites;public float pixelsPerUnit;}
        [Serializable] sealed class Report
        {
            public bool passed;
            public string manifest;
            public int characters,sprites;
            public SheetReport[] sheets;
            public CharacterReport[] profiles;
        }

        public static string Import()
        {
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.GetFullPath(ManifestPath)));
            ValidateManifest(manifest); // Validate every path, frame and native source before editing any importer.
            foreach(var owner in manifest.characters)
            {
                string asset=ProfileRoot+"/"+owner.characterId+".asset";
                if(File.Exists(asset))
                {
                    var profile=AssetDatabase.LoadAssetAtPath<CharacterMotionProfile>(asset);
                    if(profile==null||profile.GetType()!=typeof(CharacterMotionProfile)||profile.OwnerId!=owner.characterId)
                        throw new InvalidDataException("Unexpected existing roster profile: "+asset);
                }
                foreach(var sheet in owner.sheets)
                {
                    AssetDatabase.ImportAsset(sheet.path,ImportAssetOptions.ForceSynchronousImport);
                    var importer=AssetImporter.GetAtPath(sheet.path) as TextureImporter;
                    if(importer==null)throw new InvalidDataException("Texture importer unavailable: "+sheet.path);
                    var provider=RequireCapabilities(importer,sheet.path);
                    if(importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Multiple)
                    {
                        var allowed=new HashSet<string>(sheet.frames.Select(f=>f.name),StringComparer.Ordinal);
                        if(provider.GetSpriteRects().Any(r=>!allowed.Contains(r.name)))
                            throw new InvalidDataException("Unexpected existing sprite would be removed: "+sheet.path);
                    }
                }
            }

            var sheetReports=new List<SheetReport>();var profiles=new List<CharacterReport>();int count=0;
            EnsureFolder(ProfileRoot);
            foreach(var owner in manifest.characters)
            {
                var sprites=new Dictionary<string,Sprite>(StringComparer.Ordinal);
                foreach(var sheet in owner.sheets)sheetReports.Add(ImportSheet(owner,sheet,sprites));
                string path=ProfileRoot+"/"+owner.characterId+".asset";
                var profile=AssetDatabase.LoadAssetAtPath<CharacterMotionProfile>(path);
                if(profile==null)
                {
                    profile=ScriptableObject.CreateInstance<CharacterMotionProfile>();
                    profile.CharacterId=owner.characterId;AssetDatabase.CreateAsset(profile,path);
                }
                profile.Idle=Clip(sprites,owner.characterId,"idle",4);
                profile.Attack=Clip(sprites,owner.characterId,"attack",3);
                profile.Damage=Clip(sprites,owner.characterId,"damage",1);
                // A rerun preserves explicitly authored equipment variants and the existing asset GUID.
                EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
                profiles.Add(new CharacterReport{characterId=owner.characterId,asset=path,sprites=sprites.Count,pixelsPerUnit=owner.pixelsPerUnit});
                count+=sprites.Count;
            }
            return JsonUtility.ToJson(new Report{passed=true,manifest=ManifestPath,characters=profiles.Count,sprites=count,
                sheets=sheetReports.ToArray(),profiles=profiles.ToArray()},true);
        }

        static void ValidateManifest(Manifest manifest)
        {
            if(manifest==null||manifest.schemaVersion!=1||manifest.characters==null||manifest.characters.Length!=Owners.Length||
                manifest.characters.Any(c=>c==null)||!new HashSet<string>(manifest.characters.Select(c=>c.characterId),StringComparer.Ordinal).SetEquals(Owners))
                throw new InvalidDataException("Expected exactly the nine newly authored roster characters.");
            var paths=new HashSet<string>(StringComparer.Ordinal);
            foreach(var owner in manifest.characters)
            {
                if(float.IsNaN(owner.pixelsPerUnit)||float.IsInfinity(owner.pixelsPerUnit)||owner.pixelsPerUnit<=0||
                    owner.sheets==null||owner.sheets.Length!=2||owner.sheets.Any(s=>s==null))
                    throw new InvalidDataException("Invalid scale or sheet list for "+owner.characterId);
                foreach(var sheet in owner.sheets)
                {
                    float pixelsPerUnit=PixelsPerUnit(owner,sheet);
                    if(float.IsNaN(pixelsPerUnit)||float.IsInfinity(pixelsPerUnit)||pixelsPerUnit<=0)
                        throw new InvalidDataException("Invalid sheet scale for "+owner.characterId);
                    bool idle=sheet.path==ArtRoot+owner.characterId+"/idle-v1.png";
                    bool action=sheet.path==ArtRoot+owner.characterId+"/action-v1.png";
                    if((!idle&&!action)||!paths.Add(sheet.path)||sheet.width<1||sheet.height<1||sheet.width>8192||sheet.height>8192)
                        throw new InvalidDataException("Unexpected roster source path or native size.");
                    byte[] bytes=File.ReadAllBytes(sheet.path);
                    if(!string.Equals(Hash(bytes),sheet.sha256,StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Roster source SHA256 changed: "+sheet.path);
                    ValidateNativePngSize(bytes,sheet);
                    var names=new HashSet<string>(StringComparer.Ordinal);
                    foreach(string direction in Directions)
                        if(idle)for(int i=0;i<4;i++)names.Add(owner.characterId+"-roster-idle-"+direction+"-"+i);
                        else
                        {
                            for(int i=0;i<3;i++)names.Add(owner.characterId+"-roster-attack-"+direction+"-"+i);
                            names.Add(owner.characterId+"-roster-damage-"+direction+"-0");
                        }
                    if(sheet.frames==null||sheet.frames.Length!=16||sheet.frames.Any(f=>f==null)||
                        !new HashSet<string>(sheet.frames.Select(f=>f.name),StringComparer.Ordinal).SetEquals(names))
                        throw new InvalidDataException("Exactly sixteen expected directional names are required: "+sheet.path);
                    foreach(var frame in sheet.frames)
                        if(frame.width<=0||frame.height<=0||frame.x<0||frame.y<0||
                            (long)frame.x+frame.width>sheet.width||(long)frame.y+frame.height>sheet.height||frame.pivot==null||
                            float.IsNaN(frame.pivot.x)||float.IsInfinity(frame.pivot.x)||float.IsNaN(frame.pivot.y)||float.IsInfinity(frame.pivot.y)||
                            frame.pivot.x<0||frame.pivot.x>1||frame.pivot.y<0||frame.pivot.y>1)
                            throw new InvalidDataException("Invalid native frame or foot pivot: "+frame.name);
                }
            }
        }

        static SheetReport ImportSheet(Character owner,Sheet sheet,Dictionary<string,Sprite> sprites)
        {
            float pixelsPerUnit=PixelsPerUnit(owner,sheet);
            var importer=(TextureImporter)AssetImporter.GetAtPath(sheet.path);
            var beforeProvider=RequireCapabilities(importer,sheet.path);
            var oldRects=beforeProvider.GetSpriteRects().ToDictionary(r=>r.name,StringComparer.Ordinal);
            var oldLocalIds=new Dictionary<string,long>(StringComparer.Ordinal);
            foreach(var sprite in AssetDatabase.LoadAllAssetsAtPath(sheet.path).OfType<Sprite>())
                if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long localId))oldLocalIds.Add(sprite.name,localId);

            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=pixelsPerUnit;importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=Mathf.Max(32,Mathf.NextPowerOfTwo(Mathf.Max(sheet.width,sheet.height)));
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.isReadable=false;
            importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var provider=RequireCapabilities(importer,sheet.path);
            var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            var outlines=provider.GetDataProvider<ISpriteOutlineDataProvider>();
            if(names==null||outlines==null)throw new InvalidOperationException("Required sprite data providers unavailable: "+sheet.path);
            var rects=new List<SpriteRect>();var pairs=new List<SpriteNameFileIdPair>();int preserved=0;
            foreach(var frame in sheet.frames)
            {
                bool reuse=oldRects.TryGetValue(frame.name,out var old);
                var id=reuse?old.spriteID:GUID.Generate();if(reuse)preserved++;
                rects.Add(new SpriteRect{name=frame.name,rect=new Rect(frame.x,frame.y,frame.width,frame.height),
                    alignment=SpriteAlignment.Custom,pivot=new Vector2(frame.pivot.x,frame.pivot.y),spriteID=id});
                pairs.Add(new SpriteNameFileIdPair(frame.name,id));
            }
            provider.SetSpriteRects(rects.ToArray());names.SetNameFileIdPairs(pairs);
            foreach(var rect in rects)
            {
                float x=rect.rect.width*.5f,y=rect.rect.height*.5f;
                outlines.SetOutlines(rect.spriteID,new List<Vector2[]>{new[]{new Vector2(-x,-y),new Vector2(x,-y),new Vector2(x,y),new Vector2(-x,y)}});
            }
            provider.Apply();importer.SaveAndReimport();
            AssetDatabase.ImportAsset(sheet.path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            var finalProvider=RequireCapabilities(importer,sheet.path);
            var finalRects=finalProvider.GetSpriteRects().ToDictionary(r=>r.name,StringComparer.Ordinal);
            var imported=AssetDatabase.LoadAllAssetsAtPath(sheet.path).OfType<Sprite>().ToArray();
            if(imported.Length!=16||finalRects.Count!=16)throw new InvalidDataException("Imported sprite count mismatch: "+sheet.path);
            string textureGuid=AssetDatabase.AssetPathToGUID(sheet.path);int preservedLocal=0;
            foreach(var sprite in imported)
            {
                var frame=sheet.frames.SingleOrDefault(f=>f.name==sprite.name);
                if(frame==null||Mathf.Abs(sprite.pixelsPerUnit-pixelsPerUnit)>.001f||sprite.texture.width!=sheet.width||sprite.texture.height!=sheet.height||
                    sprite.rect!=new Rect(frame.x,frame.y,frame.width,frame.height)||
                    Vector2.Distance(sprite.pivot,new Vector2(frame.pivot.x*frame.width,frame.pivot.y*frame.height))>.005f)
                    throw new InvalidDataException("Imported native frame differs from manifest: "+sprite.name);
                var expected=rects.Single(r=>r.name==sprite.name);
                if(!finalRects.TryGetValue(sprite.name,out var actual)||actual.spriteID!=expected.spriteID||
                    !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long localId)||guid!=textureGuid)
                    throw new InvalidDataException("Imported sprite identity mismatch: "+sprite.name);
                if(oldLocalIds.TryGetValue(sprite.name,out long prior))
                {
                    if(prior!=localId)throw new InvalidDataException("A rerun changed an existing sprite reference: "+sprite.name);
                    preservedLocal++;
                }
                sprites.Add(sprite.name,sprite);
            }
            if(!string.Equals(Hash(File.ReadAllBytes(sheet.path)),sheet.sha256,StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Import changed the native PNG: "+sheet.path);
            return new SheetReport{path=sheet.path,sha256=sheet.sha256,textureGuid=textureGuid,sprites=imported.Length,
                preservedSpriteIds=preserved,preservedLocalFileIds=preservedLocal,pixelsPerUnit=pixelsPerUnit,capabilitiesPassed=true,nativeSourceUnchanged=true};
        }

        static ISpriteEditorDataProvider RequireCapabilities(TextureImporter importer,string path)
        {
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);
            if(provider==null)throw new InvalidOperationException("Sprite provider unavailable; operation stopped: "+path);
            provider.InitSpriteEditorDataProvider();
            var edit=provider.GetDataProvider<ISpriteFrameEditCapability>();
            if(edit==null)throw new InvalidOperationException("Sprite frame capabilities unavailable; operation stopped: "+path);
            var capability=edit.GetEditCapability();
            foreach(var required in new[]{EEditCapability.CreateAndDeleteSprite,EEditCapability.EditSpriteName,EEditCapability.EditSpriteRect,EEditCapability.EditPivot})
                if(!capability.HasCapability(required))
                    throw new InvalidOperationException("Sprite importer denies "+required+"; operation stopped: "+path);
            if(provider.GetDataProvider<ISpriteNameFileIdDataProvider>()==null||provider.GetDataProvider<ISpriteOutlineDataProvider>()==null)
                throw new InvalidOperationException("Sprite names or outline provider unavailable; operation stopped: "+path);
            return provider;
        }

        static float PixelsPerUnit(Character owner,Sheet sheet)=>sheet.pixelsPerUnit==0?owner.pixelsPerUnit:sheet.pixelsPerUnit;
        static DirectionalSpriteClip Clip(Dictionary<string,Sprite> sprites,string id,string kind,int count)
        {
            var clip=new DirectionalSpriteClip{Frames=new DirectionalSprites[count],FramesPerSecond=8,ReleaseFrame=1};
            for(int i=0;i<count;i++)clip.Frames[i]=new DirectionalSprites{
                Front=sprites[id+"-roster-"+kind+"-front-"+i],Back=sprites[id+"-roster-"+kind+"-back-"+i],
                Left=sprites[id+"-roster-"+kind+"-left-"+i],Right=sprites[id+"-roster-"+kind+"-right-"+i]};
            return clip;
        }
        static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);
            if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent,Path.GetFileName(path))))
                throw new IOException("Cannot create authored profile folder: "+path);
        }
        static void ValidateNativePngSize(byte[] bytes,Sheet sheet)
        {
            byte[] signature={137,80,78,71,13,10,26,10};
            if(bytes.Length<24||!signature.SequenceEqual(bytes.Take(8))||
                bytes[12]!=(byte)'I'||bytes[13]!=(byte)'H'||bytes[14]!=(byte)'D'||bytes[15]!=(byte)'R'||
                BigEndianInt(bytes,16)!=sheet.width||BigEndianInt(bytes,20)!=sheet.height)
                throw new InvalidDataException("Native PNG dimensions differ from the manifest: "+sheet.path);
        }
        static uint BigEndianInt(byte[] bytes,int start)=>(uint)bytes[start]<<24|(uint)bytes[start+1]<<16|(uint)bytes[start+2]<<8|bytes[start+3];
        static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    }
}
