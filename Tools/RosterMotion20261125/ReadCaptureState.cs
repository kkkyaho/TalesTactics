var t=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries");
var m=t.GetMethod("GetCountsByType",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
object[] counts={0,0,0};m.Invoke(null,counts);
var b=UnityEngine.Object.FindAnyObjectByType<TalesTactics.BattleDirector>();
return new {utc=System.DateTime.UtcNow.ToString("o"),frame=UnityEngine.Time.frameCount,seconds=UnityEngine.Time.time,realSeconds=UnityEngine.Time.realtimeSinceStartupAsDouble,playing=UnityEditor.EditorApplication.isPlaying,paused=UnityEditor.EditorApplication.isPaused,compiling=UnityEditor.EditorApplication.isCompiling,background=UnityEngine.Application.runInBackground,stage=b?.Session?.CampaignStage,selected=b?.Session?.Active?.Data?.Id,directorEnabled=b?.enabled,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,consoleErrors=counts[0],consoleWarnings=counts[1],consoleLogs=counts[2]};
