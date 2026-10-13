using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
namespace TalesTactics.Editor
{
    public static class CrossoverBuild
    {
        public static void Development()=>Build("CrossoverDevelopment",BuildOptions.Development);
        public static void Release()=>Build("CrossoverWindows",BuildOptions.None);
        public static void CurrentCampaign()=>Build("CurrentCampaignWindows",BuildOptions.None);
        // Same normal-player rendering/runtime; only isolated review fixtures are included.
        public static void VisualReview()=>Build("CrossoverReview",BuildOptions.None,new[]{"TALESTACTICS_REVIEW"});
        static void Build(string name,BuildOptions options,string[] defines=null)
        {
            string directory="Builds/"+name;Directory.CreateDirectory(directory);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/TalesTactics/Scenes/TestBattle.unity"},locationPathName=directory+"/TalesTactics.exe",target=BuildTarget.StandaloneWindows64,options=options,extraScriptingDefines=defines??Array.Empty<string>()});
            string result=report.summary.result+"\nErrors "+report.summary.totalErrors+"\nWarnings "+report.summary.totalWarnings+"\nDuration "+report.summary.totalTime+"\nSize "+report.summary.totalSize;
            File.WriteAllText(directory+"/build-result.txt",result);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+result);
        }
    }
}
