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
    // Explicitly invoked. Candidate import cannot modify CharacterData, clips or equipment.
    public static class SdCharacterArtImporter
    {
        [Serializable] public sealed class Point { public float x,y; }
        [Serializable] public sealed class Polygon { public Point[] points; }
        [Serializable] public sealed class Frame
        {
            public string name,originalSpriteId,originalInternalId;
            public float x,y,width,height,pivotX,pivotY;
            public Polygon[] outlines;
        }
        [Serializable] public sealed class Measurement
        {
            public int schemaVersion,width,height,frameCount;
            public string source,candidate,sourceGuid,sourceSha256,candidateSha256;
            public float pixelsPerUnit,alphaZeroFraction,solidFraction;
            public bool visualApproved;
            public Frame[] frames;
        }
        [Serializable] public sealed class Job
        {
            public string source,sourceGuid,sourceSha256,candidate,measurement,status;
            public bool visualApproved;
            public string[] spriteNames;
        }
        [Serializable] public sealed class Manifest { public int schemaVersion; public Job[] jobs; }
        const string Prototype="Docs/SdCharacters/cless-hq-measurement.json";
        const string Production="Docs/SdCharacters/production-manifest.json";
        static string Hash(string path)
        {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        // Never truncate a destination that Unity or another process may still have mapped.
        // A sibling guarantees the same volume. Unsupported filesystems/sharing modes fail;
        // there is no delete/copy fallback and no retry that could overwrite a partial file.
        static void ReplaceFileAtomically(string destination,byte[] bytes)
        {
            string path=Path.GetFullPath(destination),directory=Path.GetDirectoryName(path);
            if(!File.Exists(path))throw new FileNotFoundException("Atomic replacement requires an existing destination",path);
            string token=Guid.NewGuid().ToString("N"),prefix=Path.Combine(directory,"."+Path.GetFileName(path)+".sd-"+token);
            string staged=prefix+".new",backup=prefix+".old";
            bool ownsStaged=false,replaced=false;
            try
            {
                if(File.Exists(backup))throw new IOException("Atomic backup path already exists: "+backup);
                using(var stream=new FileStream(staged,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {ownsStaged=true;stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                // Supplying a backup also retains the old file for documented partial native
                // rename failures. Do not ignore ACL/metadata merge errors.
                File.Replace(staged,path,backup,false);replaced=true;
            }
            catch(Exception error)when(error is IOException||error is UnauthorizedAccessException||error is NotSupportedException)
            {
                // ReplaceFile can leave the old file under its backup name. Restore that
                // name only when the destination is absent; Move never overwrites a file.
                if(File.Exists(backup)&&!File.Exists(path))
                {
                    try{File.Move(backup,path);}
                    catch(Exception restore){throw new AggregateException("Atomic replacement failed and its original backup could not be renamed back: "+backup,error,restore);}
                }
                if(error is NotSupportedException)throw new NotSupportedException("Atomic File.Replace is unsupported by this platform/filesystem; no in-place fallback was attempted: "+path,error);
                throw new IOException("Atomic File.Replace failed (filesystem support, permissions or file sharing); no in-place fallback or retry was attempted: "+path+". Any retained original backup: "+backup,error);
            }
            finally
            {
                if(ownsStaged&&File.Exists(staged))File.Delete(staged);
                if(replaced&&File.Exists(backup))File.Delete(backup);
            }
        }
        static void ValidatePath(string path,string root)
        {
            string resolved=Path.GetFullPath(path),allowed=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(!resolved.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Art path outside "+root+": "+path);
        }
        static Measurement Read(string path,string expectedCurrentSha256=null)
        {
            ValidatePath(path,"Docs/SdCharacters");
            var m=JsonUtility.FromJson<Measurement>(File.ReadAllText(path));
            if(m==null||m.schemaVersion!=1||m.frames==null||m.frameCount!=m.frames.Length||m.frameCount==0)throw new InvalidOperationException("Invalid SD measurement: "+path);
            ValidatePath(m.source,"Assets/TalesTactics/Art");ValidatePath(m.candidate,"Assets/TalesTactics/Art/SdCharacters");
            if(m.source==m.candidate||Hash(m.source)!=(expectedCurrentSha256??m.sourceSha256)||Hash(m.candidate)!=m.candidateSha256)throw new InvalidOperationException("Source/candidate changed after measurement: "+path);
            if(AssetDatabase.AssetPathToGUID(m.source)!=m.sourceGuid)throw new InvalidOperationException("Source GUID changed: "+m.source);
            if(m.pixelsPerUnit<=0||float.IsNaN(m.pixelsPerUnit)||float.IsInfinity(m.pixelsPerUnit))throw new InvalidOperationException("Invalid PPU");
            if(m.alphaZeroFraction<.15f||m.solidFraction<.02f||m.solidFraction>.75f)throw new InvalidOperationException("Invalid transparent atlas");
            if(m.frames.Select(f=>f.name).Distinct().Count()!=m.frames.Length)throw new InvalidOperationException("Duplicate frame names");
            var existing=AssetDatabase.LoadAllAssetsAtPath(m.source).OfType<Sprite>().Select(s=>s.name).OrderBy(s=>s).ToArray();
            if(!existing.SequenceEqual(m.frames.Select(f=>f.name).OrderBy(s=>s)))throw new InvalidOperationException("Replacement would remove or add source frames: "+m.source);
            foreach(var f in m.frames)
            {
                if(f.width<=0||f.height<=0||f.x<0||f.y<0||f.x+f.width>m.width||f.y+f.height>m.height||f.pivotX<0||f.pivotX>1||f.pivotY<0||f.pivotY>1)throw new InvalidOperationException("Frame out of bounds: "+f.name);
                if(f.outlines==null||f.outlines.Length==0||f.outlines.Any(p=>p.points==null||p.points.Length<3))throw new InvalidOperationException("Missing outline: "+f.name);
            }
            return m;
        }
        static void Import(Measurement m,string destination,bool preserveOriginalIds)
        {
            AssetDatabase.ImportAsset(destination,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            var importer=AssetImporter.GetAtPath(destination) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("Missing candidate importer: "+destination);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
            importer.npotScale=TextureImporterNPOTScale.None;importer.spritePixelsPerUnit=m.pixelsPerUnit;
            importer.SaveAndReimport();
            importer=AssetImporter.GetAtPath(destination) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("Missing refreshed sprite importer: "+destination);
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);
            if(provider==null)throw new InvalidOperationException("Missing sprite provider: "+destination);
            provider.InitSpriteEditorDataProvider();
            var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
            if(capability==null)throw new InvalidOperationException("Missing sprite edit capability: "+destination);
            foreach(var flag in new[]{EEditCapability.EditSpriteName,EEditCapability.EditSpriteRect,EEditCapability.EditPivot,EEditCapability.CreateAndDeleteSprite})
                if(!capability.GetEditCapability().HasCapability(flag))throw new InvalidOperationException("Unsupported sprite operation "+flag+": "+destination);
            var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();var outlines=provider.GetDataProvider<ISpriteOutlineDataProvider>();
            if(names==null||outlines==null)throw new InvalidOperationException("Missing sprite names/outlines provider");
            var previous=provider.GetSpriteRects().ToDictionary(r=>r.name);
            var rects=new List<SpriteRect>();var pairs=new List<SpriteNameFileIdPair>();
            foreach(var frame in m.frames)
            {
                GUID id;
                if(preserveOriginalIds)
                {
                    if(!previous.TryGetValue(frame.name,out var old)||old.spriteID.ToString()!=frame.originalSpriteId)throw new InvalidOperationException("Source Sprite ID changed: "+frame.name);
                    id=old.spriteID;
                }
                else id=previous.TryGetValue(frame.name,out var old)?old.spriteID:GUID.Generate();
                rects.Add(new SpriteRect{name=frame.name,spriteID=id,rect=new Rect(frame.x,frame.y,frame.width,frame.height),alignment=SpriteAlignment.Custom,pivot=new Vector2(frame.pivotX,frame.pivotY)});
                pairs.Add(new SpriteNameFileIdPair(frame.name,id));
            }
            provider.SetSpriteRects(rects.ToArray());names.SetNameFileIdPairs(pairs);
            for(int i=0;i<rects.Count;i++)outlines.SetOutlines(rects[i].spriteID,m.frames[i].outlines.Select(p=>p.points.Select(v=>new Vector2(v.x,v.y)).ToArray()).ToList());
            provider.Apply();importer.SaveAndReimport();
            // Complete the refreshed sprite import before reading actual sub-assets, including
            // when an earlier atlas revision is already loaded during a batch application.
            AssetDatabase.ImportAsset(destination,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            var appliedImporter=AssetImporter.GetAtPath(destination) as TextureImporter;
            if(appliedImporter==null)throw new InvalidOperationException("Missing applied sprite importer: "+destination);
            var appliedProvider=factories.GetSpriteEditorDataProviderFromObject(appliedImporter);
            if(appliedProvider==null)throw new InvalidOperationException("Missing applied sprite provider: "+destination);
            appliedProvider.InitSpriteEditorDataProvider();var appliedRects=appliedProvider.GetSpriteRects();
            if(appliedRects.Length!=rects.Count||!appliedRects.Select(r=>r.name).OrderBy(n=>n,StringComparer.Ordinal).SequenceEqual(rects.Select(r=>r.name).OrderBy(n=>n,StringComparer.Ordinal)))
                throw new InvalidOperationException("Applied sprite provider name/count mismatch: "+destination+"; actual="+appliedRects.Length+", expected="+rects.Count);
            foreach(var expected in rects)
            {
                var actual=appliedRects.Single(r=>r.name==expected.name);
                if(actual.spriteID.ToString()!=expected.spriteID.ToString()||!MatchesImportValue(actual.rect.x,expected.rect.x)||!MatchesImportValue(actual.rect.y,expected.rect.y)
                    ||!MatchesImportValue(actual.rect.width,expected.rect.width)||!MatchesImportValue(actual.rect.height,expected.rect.height)
                    ||!MatchesImportValue(actual.pivot.x*actual.rect.width,expected.pivot.x*expected.rect.width)||!MatchesImportValue(actual.pivot.y*actual.rect.height,expected.pivot.y*expected.rect.height))
                    throw new InvalidOperationException("Applied sprite provider geometry/ID mismatch: "+destination+"/"+expected.name+"; actual rect="+actual.rect+", pivot="+actual.pivot+", ID="+actual.spriteID+"; expected rect="+expected.rect+", pivot="+expected.pivot+", ID="+expected.spriteID);
            }
            // Both production callers execute Import inside their backup/restore try block.
            ValidateImportedSprites(m,destination,preserveOriginalIds);
        }
        static bool MatchesImportValue(float actual,float expected)=>Mathf.Abs(actual-expected)<=.01f;
        internal static Sprite[] ValidateImportedSprites(Measurement m,string destination,bool preserveOriginalIds)
        {
            var imported=AssetDatabase.LoadAllAssetsAtPath(destination).OfType<Sprite>().OrderBy(s=>s.name,StringComparer.Ordinal).ToArray();
            if(imported.Length!=m.frameCount||!imported.Select(s=>s.name).SequenceEqual(m.frames.Select(f=>f.name).OrderBy(n=>n,StringComparer.Ordinal)))
                throw new InvalidOperationException("Imported frame coverage mismatch: "+destination+"; actual count="+imported.Length+", expected count="+m.frameCount+"; missing=["+string.Join(", ",m.frames.Select(f=>f.name).Except(imported.Select(s=>s.name),StringComparer.Ordinal))+"], unexpected=["+string.Join(", ",imported.Select(s=>s.name).Except(m.frames.Select(f=>f.name),StringComparer.Ordinal))+"]");
            var frames=m.frames.ToDictionary(f=>f.name);
            foreach(var sprite in imported)
            {
                var f=frames[sprite.name];
                if(preserveOriginalIds&&(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id)||guid!=m.sourceGuid||id.ToString()!=f.originalInternalId))
                    throw new InvalidOperationException("Source reference changed: "+sprite.name);
                if(sprite.triangles.Length<3||sprite.vertices.Length<3)throw new InvalidOperationException("Empty sprite mesh: "+sprite.name);
                if(!MatchesImportValue(sprite.rect.x,f.x)||!MatchesImportValue(sprite.rect.y,f.y)
                    ||!MatchesImportValue(sprite.rect.width,f.width)||!MatchesImportValue(sprite.rect.height,f.height)
                    ||!MatchesImportValue(sprite.pixelsPerUnit,m.pixelsPerUnit))
                    throw new InvalidOperationException(FormattableString.Invariant($"Measured bounds or scale differ: {destination}/{sprite.name}; actual rect=({sprite.rect.x:R}, {sprite.rect.y:R}, {sprite.rect.width:R}, {sprite.rect.height:R}), PPU={sprite.pixelsPerUnit:R}; expected rect=({f.x:R}, {f.y:R}, {f.width:R}, {f.height:R}), PPU={m.pixelsPerUnit:R}"));
                // Sprite.pivot is in pixels; measurements store normalized rect coordinates.
                if(!MatchesImportValue(sprite.pivot.x,f.pivotX*f.width)||!MatchesImportValue(sprite.pivot.y,f.pivotY*f.height))
                    throw new InvalidOperationException(FormattableString.Invariant($"Measured pivot differs: {destination}/{sprite.name}; actual pixels=({sprite.pivot.x:R}, {sprite.pivot.y:R}); expected pixels=({f.pivotX*f.width:R}, {f.pivotY*f.height:R})"));
            }
            return imported;
        }
        [MenuItem("Tales Tactics/SD Art/Import Cless Prototype")]
        public static void ImportClessPrototype()
        {
            ImportCandidate(Prototype);
        }
        public static void ImportCandidate(string measurementPath,string expectedCurrentSha256=null)
        {
            // Revisions retain the initial provenance hash; bind this preview to the current source explicitly.
            AssetDatabase.Refresh();var m=Read(measurementPath,expectedCurrentSha256);string currentHash=Hash(m.source);
            Import(m,m.candidate,false);
            if(Hash(m.source)!=currentHash||AssetDatabase.AssetPathToGUID(m.source)!=m.sourceGuid)throw new InvalidOperationException("Prototype changed production source");
            Debug.Log("SD candidate imported: "+m.frameCount+" frames. Production sprites, clips and equipment remain unchanged.");
        }
        static string[] ReferencedProductionSheets()
        {
            var roots=new[]{"Assets/TalesTactics/Content","Assets/TalesTactics/Art/SignatureWeapons"};
            var folders=new[]{"PixelCampaign","MotionPremium","Crossover","SignatureWeapons"};
            var assets=roots.SelectMany(root=>Directory.GetFiles(root,"*.asset",SearchOption.AllDirectories)).Select(p=>p.Replace('\\','/')).ToArray();
            return AssetDatabase.GetDependencies(assets,true).Where(p=>p.EndsWith(".png",StringComparison.OrdinalIgnoreCase)
                &&folders.Any(folder=>p.StartsWith("Assets/TalesTactics/Art/"+folder+"/",StringComparison.Ordinal)))
                .Distinct().OrderBy(p=>p,StringComparer.Ordinal).ToArray();
        }
        public static string ValidateProductionCoverage()
        {
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Production));
            if(manifest==null||manifest.schemaVersion!=1||manifest.jobs==null||manifest.jobs.Length==0)throw new InvalidOperationException("Invalid production manifest");
            var declared=manifest.jobs.Select(j=>j.source).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
            var referenced=ReferencedProductionSheets();
            if(declared.Distinct().Count()!=declared.Length||!declared.SequenceEqual(referenced))
                throw new InvalidOperationException("SD manifest source coverage differs from current referenced art. Missing: "+string.Join(", ",referenced.Except(declared))+"; unexpected/duplicate: "+string.Join(", ",declared.Except(referenced)));
            var pending=manifest.jobs.Where(j=>j.status!="approved"||!j.visualApproved||string.IsNullOrEmpty(j.measurement)||string.IsNullOrEmpty(j.candidate)).ToArray();
            if(pending.Length>0)throw new InvalidOperationException("SD production is incomplete: "+pending.Length+" of "+manifest.jobs.Length+" sheets pending. No production texture was changed.");
            foreach(var job in manifest.jobs)
            {
                var m=Read(job.measurement);
                if(!m.visualApproved||m.source!=job.source||m.candidate!=job.candidate||m.sourceGuid!=job.sourceGuid||m.sourceSha256!=job.sourceSha256||!m.frames.Select(f=>f.name).OrderBy(s=>s).SequenceEqual(job.spriteNames.OrderBy(s=>s)))throw new InvalidOperationException("Unapproved/stale production job: "+job.source);
            }
            return "All "+manifest.jobs.Length+" SD sheets have measured, visually approved replacements.";
        }
        // A later approved refinement compares against the exact already-applied pixels,
        // while its measurement retains the original source IDs and provenance.
        public static void ApplyApprovedRevision(string measurementPath,string expectedCurrentSha256)
        {
            if(string.IsNullOrEmpty(expectedCurrentSha256)||expectedCurrentSha256.Length!=64)
                throw new InvalidOperationException("An explicit prior production hash is required");
            AssetDatabase.Refresh();var m=Read(measurementPath,expectedCurrentSha256);
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Production));
            var job=manifest.jobs.SingleOrDefault(j=>j.source==m.source);
            if(job==null||job.measurement!=measurementPath||job.candidate!=m.candidate||job.sourceGuid!=m.sourceGuid
                ||job.sourceSha256!=m.sourceSha256||job.status!="approved"||!job.visualApproved||!m.visualApproved
                ||!job.spriteNames.OrderBy(n=>n).SequenceEqual(m.frames.Select(f=>f.name).OrderBy(n=>n)))
                throw new InvalidOperationException("Revision does not match its approved production job");
            var original=File.ReadAllBytes(m.source);var meta=File.ReadAllBytes(m.source+".meta");
            // Explicit imports below own this transaction. Avoid an automatic import racing
            // the file replacement and release Unity's cached mapped source-file handles.
            AssetDatabase.DisallowAutoRefresh();
            try
            {
                AssetDatabase.ReleaseCachedFileHandles();
                ReplaceFileAtomically(m.source,File.ReadAllBytes(m.candidate));Import(m,m.source,true);
                SignatureWeaponImporter.ValidateAll(); // Import saves only this texture; preserve unrelated dirty assets.
            }
            catch(Exception failure)
            {
                AssetDatabase.ReleaseCachedFileHandles();
                var errors=new List<Exception>{failure};
                // A denied atomic rename may have changed nothing. Do not replace an
                // unchanged mapped file merely to restore the bytes it already contains.
                try{if(!File.ReadAllBytes(m.source).SequenceEqual(original))ReplaceFileAtomically(m.source,original);}catch(Exception restore){errors.Add(restore);}
                try{if(!File.ReadAllBytes(m.source+".meta").SequenceEqual(meta))ReplaceFileAtomically(m.source+".meta",meta);}catch(Exception restore){errors.Add(restore);}
                try{AssetDatabase.Refresh();}catch(Exception refresh){errors.Add(refresh);}
                if(errors.Count>1)throw new AggregateException("SD revision failed; one or more atomic restoration/refresh steps also failed: "+m.source,errors);
                throw;
            }
            finally{AssetDatabase.AllowAutoRefresh();}
            Debug.Log("Approved SD refinement applied with original sprite IDs: "+m.source);
        }
        // This explicit call only runs after all coverage checks pass. It preserves source .meta GUIDs,
        // sprite IDs and every animation/equipment reference; it never calls older content importers.
        public static void ApplyCompleteProduction()
        {
            AssetDatabase.Refresh();ValidateProductionCoverage();
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Production));
            var measured=manifest.jobs.Select(j=>Read(j.measurement)).ToArray();
            var originals=measured.ToDictionary(m=>m.source,m=>new[]{File.ReadAllBytes(m.source),File.ReadAllBytes(m.source+".meta")});
            try
            {
                foreach(var m in measured){File.Copy(m.candidate,m.source,true);Import(m,m.source,true);}
                SignatureWeaponImporter.ValidateAll();AssetDatabase.SaveAssets();
            }
            catch
            {
                foreach(var item in originals){File.WriteAllBytes(item.Key,item.Value[0]);File.WriteAllBytes(item.Key+".meta",item.Value[1]);}
                AssetDatabase.Refresh();throw;
            }
            Debug.Log("Complete measured SD production applied; original Sprite references and clips preserved.");
        }
    }
}
