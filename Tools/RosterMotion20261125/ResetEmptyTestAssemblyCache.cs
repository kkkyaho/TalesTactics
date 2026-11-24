if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||UnityEditor.EditorApplication.isCompiling||UnityEditor.BuildPipeline.isBuildingPlayer)throw new System.InvalidOperationException("Stopped idle Editor required");
var t=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEngine.TestTools.Utils.PlayerTestAssemblyProvider")).First(x=>x!=null);
var f=t.GetField("m_LoadedAssemblies",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
var before=f.GetValue(null) as System.Collections.IList;
if(before==null||before.Count!=0)throw new System.InvalidOperationException("Only the proven empty non-null installed test-provider cache may be invalidated");
var source=System.IO.Path.Combine(UnityEngine.Application.dataPath,"../Library/PackageCache/com.unity.test-framework@7a3849e09bd0/UnityEngine.TestRunner/Utils/AssemblyProvider/PlayerTestAssemblyProvider.cs");
string sha;using(var hash=System.Security.Cryptography.SHA256.Create())using(var stream=System.IO.File.OpenRead(source))sha=System.BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
f.SetValue(null,null);
return new{utc=System.DateTime.UtcNow.ToString("o"),provider=t.FullName,beforeNull=false,beforeCount=before.Count,afterNull=f.GetValue(null)==null,sourceSHA256=sha,scope="Only empty installed test-provider assembly cache invalidated to its original static default null; no test definitions, results, assertions, binaries, package source, game data, or project settings changed."};
