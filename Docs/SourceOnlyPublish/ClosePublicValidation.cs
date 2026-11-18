using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class ClosePublicValidation
{
 public static string Run()
 {
  var project=Directory.GetParent(Application.dataPath).FullName.Replace(Path.DirectorySeparatorChar,'/');
  if(!string.Equals(project,"C:/dev/TalesTactics/Builds/SourceOnlyPublish20261118/Project",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Wrong project.");
  if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||BuildPipeline.isBuildingPlayer)throw new InvalidOperationException("Editor must be idle.");
  EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
  return "Scheduled isolated publication Editor exit; source and user Editor are not saved or changed.";
 }
}