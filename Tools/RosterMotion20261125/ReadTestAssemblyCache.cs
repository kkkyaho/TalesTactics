if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||UnityEditor.EditorApplication.isCompiling)throw new System.InvalidOperationException("Stopped Editor required");
var t=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEngine.TestTools.Utils.PlayerTestAssemblyProvider")).First(x=>x!=null);
var f=t.GetField("m_LoadedAssemblies",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
var value=f.GetValue(null) as System.Collections.IList;
return new{utc=System.DateTime.UtcNow.ToString("o"),provider=t.FullName,cacheNull=value==null,cachedCount=value?.Count,optionsEnabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled,options=UnityEditor.EditorSettings.enterPlayModeOptions.ToString(),scope="Read only. Installed ResetStaticsOnLoad clears the list but LoadAssemblies returns whenever the list is non-null."};
