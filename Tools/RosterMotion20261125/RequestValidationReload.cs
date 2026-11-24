if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||UnityEditor.EditorApplication.isCompiling||UnityEditor.BuildPipeline.isBuildingPlayer)throw new System.InvalidOperationException("Stopped idle Editor required");
UnityEditor.EditorApplication.delayCall+=()=>UnityEditor.EditorUtility.RequestScriptReload();
return new{utc=System.DateTime.UtcNow.ToString("o"),queued="EditorUtility.RequestScriptReload",reason="Official recompile was up_to_date without domain reload; installed PlayerTestAssemblyProvider retained an empty non-null cache."};
