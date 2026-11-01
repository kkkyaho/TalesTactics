#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace TalesTactics.Editor
{
    public static class BattleHudRedesignBuild
    {
        [Serializable] sealed class Evidence {public string result,output;public int errors,warnings;public double seconds;public ulong bytes;public bool review;}
        // Call with a new absolute directory under this project's Builds; never overwrites old evidence.
        public static void BuildTo(string directory,bool review)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave Play Mode before building.");
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Builds")).TrimEnd(Path.DirectorySeparatorChar);
            if(!Path.IsPathRooted(directory))throw new ArgumentException("Use an absolute output directory.");
            directory=Path.GetFullPath(directory);
            string archive=Path.GetFullPath(@"D:\TalesTacticsBuildArchive").TrimEnd(Path.DirectorySeparatorChar);
            bool inProject=directory.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);
            bool inArchive=string.Equals(Path.GetDirectoryName(directory),archive,StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(directory).StartsWith("HudRedesign",StringComparison.Ordinal);
            if(!inProject&&!inArchive)throw new ArgumentException("Output must be under project Builds or a new direct D:/TalesTacticsBuildArchive/HudRedesign* directory.");
            if(Directory.Exists(directory))throw new InvalidOperationException("Choose a new output directory; existing builds are not overwritten.");
            Directory.CreateDirectory(directory);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/TalesTactics/Scenes/TestBattle.unity"},
                locationPathName=Path.Combine(directory,"TalesTactics.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,
                extraScriptingDefines=review?new[]{"TALESTACTICS_REVIEW"}:Array.Empty<string>()});
            File.WriteAllText(Path.Combine(directory,"build-result.json"),JsonUtility.ToJson(new Evidence{result=report.summary.result.ToString(),
                output=report.summary.outputPath,errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,seconds=report.summary.totalTime.TotalSeconds,bytes=report.summary.totalSize,review=review},true));
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("HUD Windows build failed.");
        }
    }
}
#endif
