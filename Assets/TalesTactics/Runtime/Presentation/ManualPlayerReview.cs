#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

namespace TalesTactics
{
    // Isolated saves and passive measurement only. Never chooses a battle command.
    public sealed class ManualPlayerReview:MonoBehaviour
    {
        string directory;
        BattleDirector battle;
        readonly List<float> frames=new List<float>();
        float started,interval;
        int sample;
        string previous;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if(Application.isEditor)return;
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--manual-review");
            if(i<0)return;
            if(i+1>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)){Debug.LogError("Invalid manual review GUID");Application.Quit(2);return;}
            var component=new GameObject("Manual review measurement").AddComponent<ManualPlayerReview>();
            component.directory=Path.Combine(Application.persistentDataPath,"ManualReviews",args[i+1]);
        }
        IEnumerator Start()
        {
            // Enable ticking before yielding: an unfocused/hidden launch otherwise stalls here.
            Application.runInBackground=true;
            yield return null;
            Directory.CreateDirectory(directory);
            battle=UnityEngine.Object.FindAnyObjectByType<BattleDirector>();
            battle.ConfigureStorage(directory);battle.Hud.ShowDeployment();
            started=interval=Time.realtimeSinceStartup;
            File.WriteAllText(Path.Combine(directory,"hardware.txt"),Application.unityVersion+"\n"+SystemInfo.operatingSystem+"\n"+SystemInfo.processorType+"\n"+SystemInfo.graphicsDeviceName+"\n"+Screen.width+"x"+Screen.height+"\n");
            File.WriteAllText(Path.Combine(directory,"performance.csv"),"sample,seconds,frames,fps,p50_ms,p95_ms,p99_ms,unity_allocated_mb,unity_reserved_mb,managed_mb,working_set_mb,state\n");
            Application.logMessageReceived+=OnLog;
        }
        void OnLog(string message,string trace,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)File.AppendAllText(Path.Combine(directory,"errors.txt"),message+"\n"+trace+"\n");}
        void Update()
        {
            if(battle==null)return;
            frames.Add(Time.unscaledDeltaTime*1000);
            string state=battle.Session==null?"deployment":battle.StoryActive?"story":battle.State?.GetType().Name??"none";
            if(state!=previous){File.AppendAllText(Path.Combine(directory,"events.txt"),(Time.realtimeSinceStartup-started).ToString("F2",CultureInfo.InvariantCulture)+" "+state+" "+battle.Session?.Result+"\n");previous=state;}
            if(Time.realtimeSinceStartup-interval>=60)Flush(state);
            if(Keyboard.current!=null&&Keyboard.current.f12Key.wasPressedThisFrame)
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,"screen-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".png"));
        }
        void Flush(string state)
        {
            if(frames.Count==0)return;
            frames.Sort();float now=Time.realtimeSinceStartup;
            var process=System.Diagnostics.Process.GetCurrentProcess();process.Refresh();
            // Some Mono player configurations return zero for WorkingSet64. Leave it
            // unavailable and use MeasurePlayerProcess.ps1 instead of reporting zero MB.
            object workingSet=process.WorkingSet64>0?(object)(process.WorkingSet64/1048576.0):null;
            var row=new object[]{++sample,now-started,frames.Count,frames.Count/(now-interval),frames[frames.Count/2],frames[Math.Min(frames.Count-1,(int)(frames.Count*.95))],frames[Math.Min(frames.Count-1,(int)(frames.Count*.99))],Profiler.GetTotalAllocatedMemoryLong()/1048576.0,Profiler.GetTotalReservedMemoryLong()/1048576.0,GC.GetTotalMemory(false)/1048576.0,workingSet,state};
            File.AppendAllText(Path.Combine(directory,"performance.csv"),string.Join(",",Array.ConvertAll(row,x=>Convert.ToString(x,CultureInfo.InvariantCulture)))+"\n");
            process.Dispose();frames.Clear();interval=now;
        }
        void OnApplicationQuit(){if(battle!=null)Flush(previous);}
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
#endif
