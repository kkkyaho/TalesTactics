using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace TalesTactics.Editor
{
    public static class ReleaseBuild
    {
        [Serializable] sealed class FileRecord { public string path,role;public long sizeBytes; }
        [Serializable] sealed class ReportRecord
        {public string result,platform,outputPath;public int totalErrors,totalWarnings;public double durationSeconds;public FileRecord[] files;}
        [Serializable] sealed class Payload {public string result;}
        [Serializable] sealed class Envelope {public Payload data;}
        public static void Preview6()
        {
            const string output="Builds/WindowsPreview6/TalesTactics.exe",evidence="Docs/Distribution/Preview6";
            CombatPresentationBuild.Configure();Directory.CreateDirectory(Path.GetDirectoryName(output));Directory.CreateDirectory(evidence);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/TalesTactics/Scenes/TestBattle.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,extraScriptingDefines=Array.Empty<string>()});
            var s=report.summary;
            var record=new ReportRecord{result=s.result.ToString(),platform=s.platform.ToString(),outputPath=Path.GetFullPath(s.outputPath),totalErrors=(int)s.totalErrors,totalWarnings=(int)s.totalWarnings,durationSeconds=s.totalTime.TotalSeconds,
                files=report.GetFiles().Select(f=>new FileRecord{path=Path.GetFullPath(f.path),role=f.role,sizeBytes=(long)f.size}).ToArray()};
            File.WriteAllText(evidence+"/windows-build.json",JsonUtility.ToJson(new Envelope{data=new Payload{result=JsonUtility.ToJson(record)}},true));
            if(s.result!=BuildResult.Succeeded)throw new Exception("Release build failed: "+s.result);
        }
    }
}
