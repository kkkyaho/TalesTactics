using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace TalesTactics.Editor
{
    // Explicit, additive import; never regenerate DemoContent or replace an existing song.
    public static class LocalMusicImport
    {
        public const string Root="Assets/TalesTactics/Audio/LocalMusic/";
        public static readonly string[] Files={"99AA233359F955C515.mp3","Tales of Destiny 2 - Wilderness.mp3","9953EC3359F9562305.mp3","9993893359F955C218.mp3","99898D3359F955C018.mp3","99852C3359F955BE1A.mp3"};
        public static readonly string[] Ids={BattleMusicProgram.Title,BattleMusicProgram.Intermission,BattleMusicProgram.Spirit,BattleMusicProgram.Sky,BattleMusicProgram.GoodWine,BattleMusicProgram.Decisive};
        public static readonly string[] Hashes={"391CFFF8272AE7D6D05BAD77001A85AFBB14F14B2F704B9B8A93498628D9539E","C9E16EED15BA9C8A0BCB7D7E718932BAEAB08F5B2E540622B6705E9BDC109D12","15B974B7CBF295E40DE0D5FD03848A2ED259839A9839502A683C68E05EC4E3C2","7E86314D9C6CA6896B36E4E8E49ED4D9ED0FB49876871D74B75A34E4E2B0F3EC","9F19FFD36FE67296AC6017543D9D05974558E6D6978B216CDD6280CFD9AF6302","60795178D607B4F39B33AE060761C5BAC2CA9E4785128B6E0FC56C349C7B7704"};
        static readonly string[] Names={"Yume wa Owaranai ~ Psx Version","Wilderness (filename; no embedded title)","Fighting of The Spirit","Threatening Sky","Good Wine Makes A Good Blood","Decisive"};
        static readonly string[] Usages={"타이틀","인터미션 거점","주요 보스·동료 무기 시련","협곡·문서고 전투 및 주요 전투 전 대화","성소·수문 일반 전투","최종 결전 및 결전 전 대화"};

        [Serializable] public sealed class ImportedTrack
        {
            public string id,path,sha256,loadType,compression;
            public float seconds,quality;
            public int channels,frequency;
            public bool background,preload;
        }
        [Serializable] public sealed class ImportReport
        {
            public int existingEntries,finalEntries;
            public ImportedTrack[] tracks;
        }

        [MenuItem("Tales Tactics/Audio/Import Supplied Local Music")]
        public static void ImportMenu()=>Debug.Log(Apply());
        public static string Apply()
        {
            // Validate all source copies before changing any importer or catalog reference.
            for(int i=0;i<Files.Length;i++)
                if(!File.Exists(Root+Files[i])||Hash(Root+Files[i])!=Hashes[i])throw new InvalidOperationException("Local music source mismatch: "+Files[i]);
            AssetDatabase.Refresh();
            var library=AssetDatabase.LoadAssetAtPath<AudioLibrary>("Assets/TalesTactics/Content/AudioLibrary.asset");
            if(library==null)throw new InvalidOperationException("Missing AudioLibrary");
            var entries=new List<AudioEntry>(library.Entries);int previous=entries.Count;
            var report=new List<ImportedTrack>();
            for(int i=0;i<Files.Length;i++)
            {
                string path=Root+Files[i];var importer=AssetImporter.GetAtPath(path) as AudioImporter;
                if(importer==null)throw new InvalidOperationException("Audio import missing: "+path);
                importer.forceToMono=false;importer.loadInBackground=true;
                var settings=importer.defaultSampleSettings;
                settings.loadType=AudioClipLoadType.Streaming;settings.compressionFormat=AudioCompressionFormat.Vorbis;
                settings.quality=.85f;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.preloadAudioData=true;
                importer.defaultSampleSettings=settings;importer.SaveAndReimport();
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if(clip==null||clip.channels!=2||clip.frequency!=44100||clip.length<80||clip.loadType!=AudioClipLoadType.Streaming)
                    throw new InvalidOperationException("Invalid imported music: "+path);
                var entry=entries.SingleOrDefault(e=>e!=null&&e.Id==Ids[i]);
                if(entry!=null&&entry.Clip!=null&&entry.Clip!=clip)throw new InvalidOperationException("Refusing to replace existing music: "+Ids[i]);
                if(entry==null){entry=new AudioEntry{Id=Ids[i]};entries.Add(entry);}
                entry.Clip=clip;entry.Usage=Usages[i];
                entry.SourceMetadata="User supplied local MP3; "+Files[i]+"; "+Names[i]+"; source bytes preserved, SHA256 "+Hashes[i]+". No remote download.";
                importer=(AudioImporter)AssetImporter.GetAtPath(path);settings=importer.defaultSampleSettings;
                report.Add(new ImportedTrack{id=Ids[i],path=path,sha256=Hash(path),seconds=clip.length,channels=clip.channels,frequency=clip.frequency,
                    loadType=clip.loadType.ToString(),compression=settings.compressionFormat.ToString(),quality=settings.quality,background=importer.loadInBackground,preload=settings.preloadAudioData});
            }
            library.Entries=entries.ToArray();EditorUtility.SetDirty(library);AssetDatabase.SaveAssetIfDirty(library);
            return JsonUtility.ToJson(new ImportReport{existingEntries=previous,finalEntries=entries.Count,tracks=report.ToArray()},true);
        }
        static string Hash(string path)
        {using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");}
    }
}
