using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;
using Newtonsoft.Json;
[Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
public static class RosterFullPlayTests
{
 static TestRunnerApi api;
 static Collector collector;
 public static string Begin(string directory)
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Stopped idle Editor required");
  if(!Path.GetFullPath(Path.Combine(Application.dataPath,"..")).Replace('\\','/').Equals("C:/dev/TalesTactics/Builds/SourceOnlyPublish20261118/Project",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Validation checkout required");
  directory=Path.GetFullPath(directory);
  if(!directory.Replace('\\','/').StartsWith("C:/dev/TalesTactics/Logs/RosterMotion20261125/",StringComparison.OrdinalIgnoreCase)||Directory.Exists(directory)||File.Exists(directory))throw new InvalidOperationException("Fresh task output directory required");
  Directory.CreateDirectory(directory);api=ScriptableObject.CreateInstance<TestRunnerApi>();
  api.RetrieveTestList(TestMode.PlayMode,root=>{
   var names=new List<string>();Collect(root,names);
   Write(Path.Combine(directory,"discovery.json"),new{utc=DateTime.UtcNow.ToString("o"),count=names.Count,names});
   if(names.Count!=321)throw new InvalidOperationException("Expected 321 real discovered PlayMode tests.");
   collector=new Collector(directory,names.ToArray());api.RegisterCallbacks(collector);
   // Official mode-wide filter runs the unchanged suite. A prior zero-case run was traced
   // to an empty non-null PlayerTestAssemblyProvider cache, not to this filter choice.
   // No test name, implementation, outcome, timing, scene, or project setting is changed.
   string runId=api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode}));
   Write(Path.Combine(directory,"queued.json"),new{utc=DateTime.UtcNow.ToString("o"),runId,filter="PlayMode; no testNames override",discovered=names.Count});
  });return directory;
 }
 public static string Attach(string directory)
 {
  directory=Path.GetFullPath(directory);
  if(!Application.isPlaying||!directory.Replace('\\','/').StartsWith("C:/dev/TalesTactics/Logs/RosterMotion20261125/",StringComparison.OrdinalIgnoreCase)||!File.Exists(Path.Combine(directory,"queued.json"))||File.Exists(Path.Combine(directory,"attached.json"))||File.Exists(Path.Combine(directory,"full-play-result.json")))throw new InvalidOperationException("Active owned pending full suite required");
  var names=JsonConvert.DeserializeObject<Discovery>(File.ReadAllText(Path.Combine(directory,"discovery.json"))).names;
  if(names.Length!=321)throw new InvalidOperationException("Expected complete suite");
  api=ScriptableObject.CreateInstance<TestRunnerApi>();collector=new Collector(directory,names);api.RegisterCallbacks(collector);
  Write(Path.Combine(directory,"attached.json"),new{utc=DateTime.UtcNow.ToString("o"),scope="Collector reattached after PlayMode lifecycle; authoritative RunFinished tree supplies every result, including earlier tests. No new test execution started.",frame=Time.frameCount});return directory;
 }
 sealed class Discovery{public string[] names;}
 static void Collect(ITestAdaptor t,List<string> names){if(!t.HasChildren&&!t.IsSuite)names.Add(t.FullName);foreach(var c in t.Children)Collect(c,names);}
 static void Leaves(ITestResultAdaptor r,List<object> rows){if(!r.HasChildren&&!r.Test.IsSuite)rows.Add(new{FullName=r.Test.FullName,Status=r.TestStatus.ToString(),Duration=r.Duration,Message=r.Message,StackTrace=r.StackTrace});foreach(var c in r.Children)Leaves(c,rows);}
 static void Write(string path,object value){using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read))using(var writer=new StreamWriter(stream))writer.Write(JsonConvert.SerializeObject(value,Formatting.Indented));}
 sealed class Collector:ICallbacks
 {
  readonly string directory;readonly string[] expected;string started;int completed;bool finished;
  public Collector(string path,string[] names){directory=path;expected=names;}
  public void RunStarted(ITestAdaptor tests){started=DateTime.UtcNow.ToString("o");Write(Path.Combine(directory,"started.json"),new{startedUtc=started,declaredTests=tests.TestCaseCount,expectedTests=expected.Length});}
  public void TestStarted(ITestAdaptor test){}
  public void TestFinished(ITestResultAdaptor test){if(!test.Test.IsSuite){completed++;File.WriteAllText(Path.Combine(directory,"progress.json"),JsonConvert.SerializeObject(new{utc=DateTime.UtcNow.ToString("o"),completed,lastTest=test.Test.FullName,status=test.TestStatus.ToString(),frame=Time.frameCount,playing=EditorApplication.isPlaying}));}}
  public void RunFinished(ITestResultAdaptor result)
  {
   if(finished)return;finished=true;var rows=new List<object>();Leaves(result,rows);var total=result.PassCount+result.FailCount+result.SkipCount+result.InconclusiveCount;
   TestRunnerApi.SaveResultToFile(result,Path.Combine(directory,"full-play-result.xml"));
   Write(Path.Combine(directory,"full-play-result.json"),new{status="completed",duration=result.Duration,startedUtc=result.StartTime.ToUniversalTime().ToString("o"),finishedUtc=result.EndTime.ToUniversalTime().ToString("o"),collectedUtc=DateTime.UtcNow.ToString("o"),runner="Unity TestRunnerApi official mode-wide PlayMode filter",summary=new{total,passed=result.PassCount,failed=result.FailCount,skipped=result.SkipCount,inconclusive=result.InconclusiveCount},expectedCount=expected.Length,actualLeafCount=rows.Count,results=rows});
   api.UnregisterCallbacks(this);
  }
 }
}
