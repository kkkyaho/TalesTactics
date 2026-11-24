var assemblies=System.AppDomain.CurrentDomain.GetAssemblies();
var runner=assemblies.Select(a=>a.GetType("Unity.Pipeline.Editor.Testing.PipelineTestRunner")).First(t=>t!=null);
var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
var collector=runner.GetField("m_ActiveCollector",flags).GetValue(null);
var rows=collector==null?new object[0]:((System.Collections.IEnumerable)collector.GetType().GetProperty("Results").GetValue(collector)).Cast<object>().ToArray();
var tick=assemblies.Select(a=>a.GetType("Unity.Pipeline.Editor.Commands.AutoTickCommand")).First(t=>t!=null);
return new{utc=System.DateTime.UtcNow.ToString("o"),playing=UnityEditor.EditorApplication.isPlaying,paused=UnityEditor.EditorApplication.isPaused,background=UnityEngine.Application.runInBackground,frame=UnityEngine.Time.frameCount,seconds=UnityEngine.Time.time,realSeconds=UnityEngine.Time.realtimeSinceStartupAsDouble,focused=UnityEditor.EditorWindow.focusedWindow?.GetType().Name,autoTick=tick.GetProperty("IsEnabled",flags).GetValue(null),tickInterval=tick.GetProperty("CurrentIntervalMs",flags).GetValue(null),collectorPresent=collector!=null,completedResults=rows.Length,lastResults=rows.Skip(System.Math.Max(0,rows.Length-3)).ToArray()};
