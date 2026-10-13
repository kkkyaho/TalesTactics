using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace TalesTactics.Editor
{
    public static class CombatPresentationBuild
    {
        public static void Configure()
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/TalesTactics/Resources/TalesTactics/CombatFire.png");
            importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        public static void Release()=>Build("CombatPresentationWindows",false);
        public static void Review()=>Build("CombatPresentationReview",true);
        static void Build(string name,bool review)
        {
            Configure();System.IO.Directory.CreateDirectory("Builds/"+name);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/TalesTactics/Scenes/TestBattle.unity"},locationPathName="Builds/"+name+"/TalesTactics.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,extraScriptingDefines=review?new[]{"TALESTACTICS_REVIEW"}:System.Array.Empty<string>()});
            string result=report.summary.result+"\nErrors "+report.summary.totalErrors+"\nWarnings "+report.summary.totalWarnings+"\nDuration "+report.summary.totalTime;
            System.IO.File.WriteAllText("Builds/"+name+"/build-result.txt",result);if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception(result);
        }
    }
}
