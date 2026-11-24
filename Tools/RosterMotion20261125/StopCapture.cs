if(UnityEditor.BuildPipeline.isBuildingPlayer||UnityEditor.EditorApplication.isCompiling)throw new System.InvalidOperationException("Editor must be idle");
var b=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();
if(b!=null&&(b.enabled||!b.Profiles.Root.Replace('\\','/').StartsWith("C:/dev/TalesTactics/Logs/RosterMotion20261125/",System.StringComparison.OrdinalIgnoreCase)))throw new System.InvalidOperationException("Only this task's isolated scene may be stopped");
UnityEditor.EditorApplication.isPlaying=false;
return "Owned capture Play stopping";
