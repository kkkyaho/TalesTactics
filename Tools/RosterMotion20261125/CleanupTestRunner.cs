if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||UnityEditor.EditorApplication.isCompiling||UnityEditor.BuildPipeline.isBuildingPlayer)throw new System.InvalidOperationException("Stopped idle Editor required");
var proof=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("C:/dev/TalesTactics/Logs/RosterMotion20261125/Tests/FullPlayAfterCacheReset/full-play-result.json"));
var retry=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("C:/dev/TalesTactics/Logs/RosterMotion20261125/Tests/pose-diagnostic-result.json"));
var failed=proof["results"].Where(r=>(string)r["Status"]!="Passed").ToArray();
if((string)proof["status"]!="completed"||(int)proof["summary"]["total"]!=321||(int)proof["summary"]["passed"]!=320||(int)proof["summary"]["failed"]!=1||(int)proof["actualLeafCount"]!=321||failed.Length!=1||(int)retry["summary"]["total"]!=1||(int)retry["summary"]["passed"]!=1||(string)retry["results"][0]["FullName"]!=(string)failed[0]["FullName"])throw new System.InvalidOperationException("Actual full320/321 plus same-test isolated retry1/1 proof required");
var assemblies=System.AppDomain.CurrentDomain.GetAssemblies();var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
var t=assemblies.Select(a=>a.GetType("UnityEngine.TestTools.Utils.PlayerTestAssemblyProvider")).First(x=>x!=null);var f=t.GetField("m_LoadedAssemblies",flags);var before=f.GetValue(null) as System.Collections.IList;
var source=System.IO.Path.Combine(UnityEngine.Application.dataPath,"../Library/PackageCache/com.unity.test-framework@7a3849e09bd0/UnityEngine.TestRunner/Utils/AssemblyProvider/PlayerTestAssemblyProvider.cs");
string sha;using(var hash=System.Security.Cryptography.SHA256.Create())using(var stream=System.IO.File.OpenRead(source))sha=System.BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
if(sha!="5dabf5ea82a570ee13d3fe0b81bbca4f438f3a425088b7daf6df8d337b1eb13f")throw new System.InvalidOperationException("Installed test package source changed");
int? beforeCount=before?.Count;bool beforeNull=before==null;f.SetValue(null,null);
var cleaned=new System.Collections.Generic.List<string>();
foreach(var a in assemblies.Where(a=>a.GetName().Name.StartsWith("PipelineRunScript_RosterFullPlayTests_",System.StringComparison.Ordinal)))
{
 var owner=a.GetType("RosterFullPlayTests");if(owner==null)continue;var apiField=owner.GetField("api",flags);var callbackField=owner.GetField("collector",flags);
 var api=apiField?.GetValue(null) as UnityEditor.TestTools.TestRunner.Api.TestRunnerApi;var callback=callbackField?.GetValue(null) as UnityEditor.TestTools.TestRunner.Api.ICallbacks;
 if(api!=null){if(callback!=null)api.UnregisterCallbacks(callback);UnityEngine.Object.DestroyImmediate(api);apiField.SetValue(null,null);callbackField.SetValue(null,null);cleaned.Add(a.GetName().Name);}
}
return new{utc=System.DateTime.UtcNow.ToString("o"),passed=f.GetValue(null)==null,beforeNull,beforeCount,afterNull=f.GetValue(null)==null,sourceSHA256=sha,sourceUnchanged=true,ownedApiInstancesCleaned=cleaned.Count,scope="Owned external result collectors cleaned after actual full320/321 plus matching isolated retry1/1, recorded separately; not one all-pass full run. Temporary test-provider cache left in its normal null default so the next loader repopulates it; no package/source/test results/project settings changed."};
