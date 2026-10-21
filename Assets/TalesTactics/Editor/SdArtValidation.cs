#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TalesTactics.Editor
{
    // Explicit validation entry points. They never run during a normal import or player startup.
    public static class SdArtValidation
    {
        [Serializable] public sealed class SpriteRecord { public string name,guid;public long localId;public float x,y,width,height,pivotX,pivotY,pixelsPerUnit; }
        [Serializable] public sealed class SheetRecord { public string path,sha256,metaSha256;public SpriteRecord[] sprites; }
        [Serializable] public sealed class ArtReport { public string status,error;public int sheetCount,spriteCount;public SheetRecord[] sheets; }
        [Serializable] public sealed class BuildReportData { public string result,output;public int errors,warnings;public double seconds;public ulong bytes; }
        static string Hash(string path)
        {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static void Write(string path,object value)
        {Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(value,true));}
        public static void AuditProduction(string output)
        {
            var manifest=JsonUtility.FromJson<SdCharacterArtImporter.Manifest>(File.ReadAllText("Docs/SdCharacters/production-manifest.json"));
            var rows=new List<SheetRecord>();
            foreach(var job in manifest.jobs)
            {
                var measure=JsonUtility.FromJson<SdCharacterArtImporter.Measurement>(File.ReadAllText(job.measurement));
                if(Hash(job.source)!=measure.candidateSha256)throw new InvalidOperationException("Production pixels differ from approved art: "+job.source);
                var sprites=SdCharacterArtImporter.ValidateImportedSprites(measure,job.source,true);
                if(!sprites.Select(s=>s.name).SequenceEqual(job.spriteNames.OrderBy(s=>s,StringComparer.Ordinal)))throw new InvalidOperationException("Production frame coverage differs: "+job.source);
                var records=new List<SpriteRecord>();
                foreach(var sprite in sprites)
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long localId);
                    records.Add(new SpriteRecord{name=sprite.name,guid=guid,localId=localId,x=sprite.rect.x,y=sprite.rect.y,width=sprite.rect.width,height=sprite.rect.height,pivotX=sprite.pivot.x,pivotY=sprite.pivot.y,pixelsPerUnit=sprite.pixelsPerUnit});
                }
                rows.Add(new SheetRecord{path=job.source,sha256=Hash(job.source),metaSha256=Hash(job.source+".meta"),sprites=records.ToArray()});
            }
            SignatureWeaponImporter.ValidateAll();
            Write(output,new ArtReport{status="passed",sheetCount=rows.Count,spriteCount=rows.Sum(r=>r.sprites.Length),sheets=rows.ToArray()});
        }
        public static void Apply()
        {
            const string output="Temp/SdArtDirection/production-apply.json";
            Write(output,new ArtReport{status="running"});
            try{SdCharacterArtImporter.ApplyCompleteProduction();AuditProduction(output);}
            catch(Exception e){Write(output,new ArtReport{status="failed",error=e.ToString()});Debug.LogException(e);}
        }
        public static void BakePreviews()
        {
            var battle=UnityEngine.Object.FindAnyObjectByType<BattleDirector>();
            if(!EditorApplication.isPlaying||battle==null)throw new InvalidOperationException("Play Mode battle required");
            const string root="Temp/SdArtDirection/MenuPreviews";
            Directory.CreateDirectory(root);battle.ConfigureStorage(Path.Combine(root,"isolated-save",Guid.NewGuid().ToString("N")));
            battle.Audio.PersonalMusicEnabled=false;battle.Hud.ShowTitle();battle.StartTitleGame(0);
            MenuPreviewBakeRunner.Begin(battle,root);
        }
        public static void Windows()=>Build(false);
        public static void Review()=>Build(true);
        static void Build(bool review)
        {
            string directory="Builds/"+(review?"SdArtReview":"SdArtWindows");
            Directory.CreateDirectory(directory);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/TalesTactics/Scenes/TestBattle.unity"},locationPathName=directory+"/TalesTactics.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,extraScriptingDefines=review?new[]{"TALESTACTICS_REVIEW"}:Array.Empty<string>()});
            Write(directory+"/build-result.json",new BuildReportData{result=report.summary.result.ToString(),output=report.summary.outputPath,errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,seconds=report.summary.totalTime.TotalSeconds,bytes=report.summary.totalSize});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("SD Windows build failed");
        }
    }
}
#endif
