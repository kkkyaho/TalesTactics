using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
namespace TalesTactics.Editor
{
    public static class UiSafeLayoutBuild
    {
        public static void Illustrated(){Build(true,"IllustratedIcons");Build(false,"IllustratedIcons");}
        public static void CommercialUx(){Build(true,"CommercialUx");Build(false,"CommercialUx");}
        public static void All(){Build(true);Build(false);}
        public static void ClassIcons(){Build(true,"ClassIcons");Build(false,"ClassIcons");}
        [InitializeOnLoadMethod] static void RegisterReview()=>EditorApplication.playModeStateChanged+=ReviewState;
        public static void CommercialEditorReview()=>StartEditorReview("Logs/CommercialUxEditorReview");
        public static void EditorReview()=>StartEditorReview("Logs/UiSafeLayoutEditorReview");
        static void StartEditorReview(string output)
        {
            if(UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Use a graphical Editor for Game View verification");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TalesTactics/Scenes/TestBattle.unity");
            SessionState.SetString("TalesTactics.UiSafeLayoutReviewOutput",output);
            SessionState.SetBool("TalesTactics.UiSafeLayoutReviewActive",true);EditorApplication.isPlaying=true;
        }
        static void ReviewState(PlayModeStateChange state)
        {
            if(!SessionState.GetBool("TalesTactics.UiSafeLayoutReviewActive",false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
                Type.GetType("TalesTactics.UiSafeLayoutReview, TalesTactics.Runtime",true).GetMethod("BeginEditor").Invoke(null,new object[]{SessionState.GetString("TalesTactics.UiSafeLayoutReviewOutput","Logs/UiSafeLayoutEditorReview")});
            if(state==PlayModeStateChange.EnteredEditMode)
            {SessionState.SetBool("TalesTactics.UiSafeLayoutReviewActive",false);EditorApplication.Exit(SessionState.GetInt("TalesTactics.UiSafeLayoutReviewExit",1));}
        }
        static void Build(bool review,string name="UiSafeLayout")
        {
            string folder="Builds/"+name+(review?"Review":"Windows");Directory.CreateDirectory(folder);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/TalesTactics/Scenes/TestBattle.unity"},locationPathName=folder+"/TalesTactics.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,extraScriptingDefines=review?new[]{"TALESTACTICS_REVIEW"}:Array.Empty<string>()});
            var s=report.summary;File.WriteAllText(folder+"/build-result.txt",s.result+"\nErrors "+s.totalErrors+"\nWarnings "+s.totalWarnings+"\nDuration "+s.totalTime);
            if(s.result!=BuildResult.Succeeded)throw new Exception("Title screen build: "+s.result);
        }
    }
}
