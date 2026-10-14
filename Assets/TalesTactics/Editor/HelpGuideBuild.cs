using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
namespace TalesTactics.Editor
{
    public static class HelpGuideBuild
    {
        public static void All(){Build(true);Build(false);}
        [InitializeOnLoadMethod] static void RegisterReview()=>EditorApplication.playModeStateChanged+=ReviewState;
        public static void EditorReview()
        {
            if(UnityEngine.Application.isBatchMode)throw new InvalidOperationException("Use a graphical Editor for Game View verification");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/TalesTactics/Scenes/TestBattle.unity");
            SessionState.SetBool("TalesTactics.HelpReviewActive",true);EditorApplication.isPlaying=true;
        }
        static void ReviewState(PlayModeStateChange state)
        {
            if(!SessionState.GetBool("TalesTactics.HelpReviewActive",false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
                Type.GetType("TalesTactics.HelpGuideReview, TalesTactics.Runtime",true).GetMethod("BeginEditor").Invoke(null,new object[]{"Logs/HelpGuideEditorReview"});
            if(state==PlayModeStateChange.EnteredEditMode)
            {SessionState.SetBool("TalesTactics.HelpReviewActive",false);EditorApplication.Exit(SessionState.GetInt("TalesTactics.HelpReviewExit",1));}
        }
        static void Build(bool review)
        {
            string folder="Builds/HelpGuide"+(review?"Review":"Windows");Directory.CreateDirectory(folder);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/TalesTactics/Scenes/TestBattle.unity"},locationPathName=folder+"/TalesTactics.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,extraScriptingDefines=review?new[]{"TALESTACTICS_REVIEW"}:Array.Empty<string>()});
            var s=report.summary;File.WriteAllText(folder+"/build-result.txt",s.result+"\nErrors "+s.totalErrors+"\nWarnings "+s.totalWarnings+"\nDuration "+s.totalTime);
            if(s.result!=BuildResult.Succeeded)throw new Exception("Help guide build: "+s.result);
        }
    }
}
