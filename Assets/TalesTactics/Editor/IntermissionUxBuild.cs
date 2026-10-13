using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace TalesTactics.Editor
{
    public static class IntermissionUxBuild
    {
        public static void All(){Build(true);Build(false);}
        [MenuItem("Tales Tactics/Build Intermission UX")]
        public static void Release()=>Build(false);
        [MenuItem("Tales Tactics/Build Intermission UX Review")]
        public static void Review()=>Build(true);
        static void Build(bool review)
        {
            string folder="Builds/IntermissionUx"+(review?"Review":"Windows");Directory.CreateDirectory(folder);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/TalesTactics/Scenes/TestBattle.unity"},locationPathName=folder+"/TalesTactics.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,extraScriptingDefines=review?new[]{"TALESTACTICS_REVIEW"}:Array.Empty<string>()});
            var s=report.summary;File.WriteAllText(folder+"/build-result.txt",s.result+"\nErrors "+s.totalErrors+"\nWarnings "+s.totalWarnings+"\nDuration "+s.totalTime);
            if(s.result!=BuildResult.Succeeded)throw new Exception("Intermission build: "+s.result);
        }
    }
}
