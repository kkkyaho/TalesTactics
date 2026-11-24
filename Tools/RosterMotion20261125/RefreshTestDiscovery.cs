using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;
public static class RefreshTestDiscovery
{
 public static string Main(string output)
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Stopped Editor required");
  output=Path.GetFullPath(output);if(!output.Replace('\\','/').StartsWith("C:/dev/TalesTactics/Logs/RosterMotion20261125/",StringComparison.OrdinalIgnoreCase)||File.Exists(output))throw new InvalidOperationException("Fresh task output required");
  var assembly=typeof(TestRunnerApi).Assembly;var type=assembly.GetType("UnityEditor.TestTools.TestRunner.TestListCacheData");
  var instance=type.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
  var platforms=(IList)type.GetProperty("platforms").GetValue(instance);var data=(IList)type.GetProperty("cachedData").GetValue(instance);
  var before=new List<object>();for(int i=0;i<data.Count;i++){var names=new List<string>();Collect((NUnit.Framework.Interfaces.ITest)data[i],names);before.Add(new{platform=platforms[i].ToString(),leaves=names.Count,names});}
  // This is the exact in-memory cache invalidation used by Unity's DidReloadScripts callback.
  // No test implementation, result, filter, scene, project setting, or player file changes.
  assembly.GetType("UnityEditor.TestTools.TestRunner.TestListCache").GetMethod("ScriptReloaded",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
  var api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RetrieveTestList(TestMode.PlayMode,root=>{
   var names=new List<string>();Collect(root,names);var result=new{utc=DateTime.UtcNow.ToString("o"),scope="Unity test discovery in-memory cache invalidated using its installed DidReloadScripts callback; results and source unchanged",before,afterLeaves=names.Count,afterNames=names};
   using(var stream=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))using(var writer=new StreamWriter(stream))writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));UnityEngine.Object.DestroyImmediate(api);
  });return output;
 }
 static void Collect(NUnit.Framework.Interfaces.ITest t,List<string> names){if(!t.HasChildren&&!t.IsSuite)names.Add(t.FullName);foreach(var c in t.Tests)Collect(c,names);}
 static void Collect(ITestAdaptor t,List<string> names){if(!t.HasChildren&&!t.IsSuite)names.Add(t.FullName);foreach(var c in t.Children)Collect(c,names);}
}
