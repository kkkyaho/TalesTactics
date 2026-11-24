using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public static class ImportRosterArt
{
 public static string Begin(string output,bool requireStableIds)
 {
  string project=Path.GetFullPath(Path.Combine(Application.dataPath,"..")).Replace('\\','/');
  if(!project.Equals("C:/dev/TalesTactics/Builds/SourceOnlyPublish20261118/Project",StringComparison.OrdinalIgnoreCase)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Stopped idle validation Editor required.");
  output=Path.GetFullPath(output);if(!output.Replace('\\','/').StartsWith("C:/dev/TalesTactics/Logs/RosterMotion20261125/Import/",StringComparison.OrdinalIgnoreCase)||File.Exists(output)||File.Exists(output+".queued.json"))throw new InvalidOperationException("Fresh import report path required.");
  Directory.CreateDirectory(Path.GetDirectoryName(output));string run=Guid.NewGuid().ToString("N"),started=DateTime.UtcNow.ToString("o");
  File.WriteAllText(output+".queued.json",JsonConvert.SerializeObject(new{runId=run,startedUtc=started,requireStableIds}));
  double ready=EditorApplication.timeSinceStartup+3;EditorApplication.CallbackFunction callback=null;
  callback=()=>{if(EditorApplication.timeSinceStartup<ready)return;EditorApplication.update-=callback;JObject imported=null;string error=null;bool passed=false;
   try{imported=JObject.Parse(TalesTactics.Editor.RosterMotionArtImporter.Import());passed=(bool)imported["passed"]&&(int)imported["characters"]==9&&(int)imported["sprites"]==288;
    if(requireStableIds)passed&=imported["sheets"].All(s=>(int)s["preservedSpriteIds"]==16&&(int)s["preservedLocalFileIds"]==16);
    if(!passed)throw new InvalidOperationException("Import proof totals/idempotent IDs did not match expected 9/288/18x16.");
   }catch(Exception e){error=e.ToString();passed=false;Debug.LogException(e);}
   var value=new{runId=run,startedUtc=started,finishedUtc=DateTime.UtcNow.ToString("o"),passed,requireStableIds,report=imported,error};
   using(var stream=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read))using(var writer=new StreamWriter(stream))writer.Write(JsonConvert.SerializeObject(value,Formatting.Indented));
  };EditorApplication.update+=callback;return output;
 }
}