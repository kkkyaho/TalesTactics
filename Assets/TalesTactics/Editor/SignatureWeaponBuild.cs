using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
namespace TalesTactics.Editor
{
    public static class SignatureWeaponBuild
    {
        public static void All(){SignatureWeaponImporter.ValidateAll();Build(true);Build(false);}
        public static void Review(){SignatureWeaponImporter.ValidateAll();Build(true);}
        static void Build(bool review)
        {
            string folder="Builds/SignatureWeapons"+(review?"Review":"Windows");Directory.CreateDirectory(folder);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/TalesTactics/Scenes/TestBattle.unity"},locationPathName=folder+"/TalesTactics.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,extraScriptingDefines=review?new[]{"TALESTACTICS_REVIEW"}:Array.Empty<string>()});
            var s=report.summary;File.WriteAllText(folder+"/build-result.txt",s.result+"\nErrors "+s.totalErrors+"\nWarnings "+s.totalWarnings+"\nDuration "+s.totalTime);
            if(s.result!=BuildResult.Succeeded)throw new Exception("Signature weapon build: "+s.result);
        }
    }
}
