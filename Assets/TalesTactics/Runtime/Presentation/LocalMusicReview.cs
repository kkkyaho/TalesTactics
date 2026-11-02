#if UNITY_EDITOR || TALESTACTICS_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace TalesTactics
{
    // A bounded real audio-device check. It does not claim whole-track listening or loop-seam approval.
    public sealed class LocalMusicReview:MonoBehaviour
    {
        public const string Argument="--local-music-review";
        [Serializable] public sealed class Row
        {
            public string scenario,id,clip,loadType,loadState;public int channels,frequency,startSample,endSample;
            public float length,volume,peak,rms;public double dspStart,dspEnd;public bool playing,loop;
        }
        [Serializable] public sealed class Report
        {
            public int schema=1,expectedRows=14,completedRows,originalTracks,outputSampleRate,listeners,dspBufferLength,dspBufferCount;
            public bool complete,passed,userFilesUnchanged,directPlaybackCampaignUnchanged,directPlaybackRandomUnchanged,savedVolumeRestored,stopped;
            public string status="Running",startedUtc,finishedUtc,unityVersion,output,storage;public Row[] rows;public string[] errors,userFilesBefore,userFilesAfter;
            public string scope="Six supplied MP3s played through the production BattleAudio source, real device cursor and nonzero output observations; real title/new-game/hub/story/battle/return flow; original 14 tracks retained; theme resume and mute/stop/settings checks. New isolated saves only.";
            public string limitations="Samples cover playback start and continuation, not full songs, loop seams, all campaign encounters or a listening-quality judgment. User hash coverage here is top-level files except exact Player.log/Player-prev.log; the launch wrapper must additionally protect recursive files/directories and existing player processes.";
        }
        static readonly string[] NewIds={BattleMusicProgram.Title,BattleMusicProgram.Intermission,BattleMusicProgram.Spirit,BattleMusicProgram.Sky,BattleMusicProgram.GoodWine,BattleMusicProgram.Decisive};
        readonly List<Row> rows=new List<Row>();readonly List<string> errors=new List<string>();readonly Report report=new Report();
        BattleDirector battle;string directory;bool initialized,finished,oldBackground;float oldScale;double began;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,Argument);if(index<0)return;
            try
            {
                if(args.Count(a=>a==Argument)!=1||index+1>=args.Length||!Guid.TryParseExact(args[index+1],"N",out var id)||id==Guid.Empty)
                    throw new ArgumentException("Local music review requires one non-empty N-format GUID.");
                string output=Path.Combine(Path.GetDirectoryName(Application.dataPath),"LocalMusicReviews",args[index+1]);
                if(!SamePath(ReviewStorageBootstrap.Resolve(args,Application.dataPath,Application.persistentDataPath),Path.Combine(output,"Storage")))
                    throw new InvalidOperationException("Review storage must be isolated before Director.Start.");
                ValidateOutput(output);new GameObject("Local music review").AddComponent<LocalMusicReview>().directory=output;
            }
            catch(Exception e){Debug.LogError("Local music review initialization rejected: "+e);Application.Quit(2);}
        }
#if UNITY_EDITOR
        public static void BeginEditor(string output)
        {
            if(!Application.isPlaying||FindAnyObjectByType<LocalMusicReview>()!=null)throw new InvalidOperationException("One active Play Mode review is required.");
            ValidateOutput(output);new GameObject("Local music review").AddComponent<LocalMusicReview>().directory=Path.GetFullPath(output);
            UnityEditor.SessionState.SetInt("TalesTactics.LocalMusicReviewExit",-1);
        }
#endif
        static bool SamePath(string a,string b)=>!string.IsNullOrEmpty(a)&&!string.IsNullOrEmpty(b)&&string.Equals(Path.GetFullPath(a).TrimEnd('\\','/'),Path.GetFullPath(b).TrimEnd('\\','/'),StringComparison.OrdinalIgnoreCase);
        static void ValidateOutput(string path)
        {
            if(string.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path))throw new ArgumentException("Output must be a new absolute directory.");
            string root=Path.GetFullPath(path).TrimEnd('\\','/'),user=Path.GetFullPath(Application.persistentDataPath).TrimEnd('\\','/');
            if(SamePath(root,user)||root.StartsWith(user+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||user.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Output and user storage must not overlap.");
            if(Directory.Exists(root)&&Directory.EnumerateFileSystemEntries(root).Any())throw new InvalidOperationException("Previous evidence is never overwritten.");
        }
        IEnumerator Start()
        {
            initialized=true;oldBackground=Application.runInBackground;oldScale=Time.timeScale;began=Time.realtimeSinceStartupAsDouble;
            Application.runInBackground=true;Time.timeScale=1;Application.logMessageReceived+=Log;
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0&&!finished)
            {
                object item=null;bool next=false;Exception failure=null;
                try{next=stack.Peek().MoveNext();if(next)item=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){errors.Add(failure.ToString());break;}
                if(!next){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(item is IEnumerator nested)stack.Push(nested);else yield return item;
            }
            while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception e){errors.Add(e.ToString());}}
            Finish();
        }
        void Update()
        {
            if(initialized&&!finished&&Time.realtimeSinceStartupAsDouble-began>120)
            {errors.Add("Local music review exceeded 120 seconds.");StopAllCoroutines();Finish();}
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(directory);report.startedUtc=DateTime.UtcNow.ToString("o");report.unityVersion=Application.unityVersion;
            report.output=directory;report.storage=Path.Combine(directory,"Storage");report.userFilesBefore=Snapshot(Application.persistentDataPath);Write();yield return null;
            battle=FindAnyObjectByType<BattleDirector>();Require(battle!=null&&battle.enabled&&battle.Catalog.CrossoverReady,"Current production scene/catalog required.");
            if(!Application.isEditor)Require(SamePath(battle.Profiles.Root,report.storage),"Director started outside isolated storage.");
            battle.ConfigureStorage(report.storage);battle.Audio.PersonalMusicEnabled=false;
            report.listeners=FindObjectsByType<AudioListener>().Count(l=>l.enabled&&l.gameObject.activeInHierarchy);
            report.outputSampleRate=AudioSettings.outputSampleRate;AudioSettings.GetDSPBufferSize(out report.dspBufferLength,out report.dspBufferCount);
            Require(report.listeners==1,"Exactly one active listener required.");
            report.originalTracks=battle.Audio.Library.Entries.Count(e=>e!=null&&e.Clip!=null&&(e.Id=="battle"||e.Id=="boss"||e.Id=="story"||e.Id=="victory"||e.Id.EndsWith(".theme")));
            Require(report.originalTracks==14,"All original tracks must remain connected.");
            var prefs=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(battle.Preferences));prefs.Music=.19f;prefs.Effects=.23f;
            Require(battle.SavePreferences(prefs,false),"Isolated volume settings did not save.");
            battle.Hud.ShowTitle();yield return Probe(BattleMusicProgram.Title,"title");
            Require(battle.StartTitleGame(0),"New game did not enter the hub.");yield return Probe(BattleMusicProgram.Intermission,"new-game-hub");
            battle.RequestBattle();Require(battle.StoryActive,"Opening story did not start.");yield return Probe("story","opening-story");
            battle.FinishStory();Require(battle.Session!=null,"Opening battle did not start.");yield return Probe("battle","opening-battle");
            battle.Restart();yield return Probe(BattleMusicProgram.Intermission,"return-hub");
            battle.ReturnToTitle();Require(battle.Hud.TitleOpen,"Title did not reopen.");yield return Probe(BattleMusicProgram.Title,"return-title");
            string campaign=JsonUtility.ToJson(battle.Campaign),random=JsonUtility.ToJson(UnityEngine.Random.state);
            foreach(string id in NewIds){battle.Audio.Play(id);yield return Probe(id,"supplied-track");}
            report.directPlaybackCampaignUnchanged=campaign==JsonUtility.ToJson(battle.Campaign);
            report.directPlaybackRandomUnchanged=random==JsonUtility.ToJson(UnityEngine.Random.state);
            Require(report.directPlaybackCampaignUnchanged&&report.directPlaybackRandomUnchanged,"Direct playback changed campaign/RNG.");
            var source=battle.Audio.GetComponent<AudioSource>();int sample=source.timeSamples;
            battle.Audio.BeginTheme("cless.theme");yield return Probe("cless.theme","ultimate-theme");
            battle.Audio.EndTheme();Require(source.timeSamples>=sample&&source.timeSamples<=sample+44100,"Streamed resume did not preserve position.");
            yield return Probe(BattleMusicProgram.Decisive,"ultimate-return");
            battle.Audio.SetVolumes(0,.23f);Require(source.volume==0&&source.isPlaying,"Music mute should retain the running stream.");
            battle.ConfigureStorage(report.storage);report.savedVolumeRestored=Mathf.Abs(source.volume-.19f)<.001f&&Mathf.Abs(battle.Audio.EffectsVolume-.23f)<.001f;
            Require(report.savedVolumeRestored,"Reloading isolated preferences did not restore music/effect volumes.");
            battle.Audio.StopAll();report.stopped=battle.Audio.GetComponents<AudioSource>().All(s=>!s.isPlaying);Require(report.stopped,"StopAll left a source playing.");
            report.complete=rows.Count==report.expectedRows&&NewIds.All(id=>rows.Any(r=>r.id==id&&r.scenario=="supplied-track"));
        }
        IEnumerator Probe(string id,string scenario)
        {
            var source=battle.Audio.GetComponent<AudioSource>();var expected=battle.Audio.Library.Entries.Single(e=>e.Id==id).Clip;
            Require(expected!=null&&source.clip==expected&&battle.Audio.CurrentMusicId==id,"Wrong music cue: "+scenario+" expected "+id);
            var row=new Row{scenario=scenario,id=id,clip=expected.name,loadType=expected.loadType.ToString(),channels=expected.channels,frequency=expected.frequency,
                length=expected.length,volume=source.volume,loop=source.loop,startSample=source.timeSamples,dspStart=AudioSettings.dspTime};
            float[] samples=new float[1024];double until=Time.realtimeSinceStartupAsDouble+10;
            do
            {
                yield return null;source.GetOutputData(samples,0);float square=0;
                foreach(float value in samples){row.peak=Mathf.Max(row.peak,Mathf.Abs(value));square+=value*value;}
                row.rms=Mathf.Max(row.rms,Mathf.Sqrt(square/samples.Length));
                row.endSample=source.timeSamples;row.dspEnd=AudioSettings.dspTime;row.playing=source.isPlaying;row.loadState=expected.loadState.ToString();
                if(row.playing&&row.endSample>0&&row.endSample!=row.startSample&&row.dspEnd>row.dspStart&&row.peak>.00001f)break;
            }while(Time.realtimeSinceStartupAsDouble<until&&expected.loadState!=AudioDataLoadState.Failed);
            rows.Add(row);Write();
            Require(source.clip==expected&&row.playing&&row.endSample>0&&row.endSample!=row.startSample&&row.dspEnd>row.dspStart&&row.peak>.00001f,"No actual advancing nonzero audio: "+id);
        }
        static string[] Snapshot(string root)
        {
            if(!Directory.Exists(root))return Array.Empty<string>();
            using(var sha=SHA256.Create())return Directory.GetFiles(root)
                .Where(p=>!string.Equals(Path.GetFileName(p),"Player.log",StringComparison.OrdinalIgnoreCase)&&!string.Equals(Path.GetFileName(p),"Player-prev.log",StringComparison.OrdinalIgnoreCase))
                .OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).Select(p=>Path.GetFileName(p)+":"+BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","")).ToArray();
        }
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        void Write(){report.rows=rows.ToArray();report.completedRows=rows.Count;report.errors=errors.ToArray();File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(report,true));}
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
        void Finish()
        {
            if(finished)return;finished=true;Application.logMessageReceived-=Log;int exit=1;
            try
            {
                battle?.Audio.StopAll();report.finishedUtc=DateTime.UtcNow.ToString("o");report.userFilesAfter=Snapshot(Application.persistentDataPath);
                report.userFilesUnchanged=report.userFilesBefore!=null&&report.userFilesBefore.SequenceEqual(report.userFilesAfter);
                if(!report.userFilesUnchanged)errors.Add("User save/settings files changed; no user files were restored.");
                report.passed=report.complete&&report.userFilesUnchanged&&errors.Count==0;report.status=report.passed?"Passed":"Stopped or failed";Write();exit=report.passed?0:1;
            }
            catch(Exception e){errors.Add(e.ToString());report.passed=false;report.status="Stopped or failed";try{Write();}catch(Exception write){Debug.LogError(write);}}
            finally
            {
                // A hidden standalone player must keep ticking until deferred Quit completes.
                // Editor restores immediately; standalone restores during OnDestroy.
                if(Application.isEditor){Time.timeScale=oldScale;Application.runInBackground=oldBackground;}
#if UNITY_EDITOR
                UnityEditor.SessionState.SetInt("TalesTactics.LocalMusicReviewExit",exit);UnityEditor.EditorApplication.isPlaying=false;
#else
                Application.Quit(exit);
#endif
            }
        }
        void OnDestroy(){Application.logMessageReceived-=Log;if(initialized){Time.timeScale=oldScale;Application.runInBackground=oldBackground;}}
    }
}
#endif
