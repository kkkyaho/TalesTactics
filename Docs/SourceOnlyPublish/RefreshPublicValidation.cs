using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class RefreshPublicValidation
{
 public static string Run()
 {
  if(!string.Equals(Directory.GetParent(Application.dataPath).FullName.Replace(Path.DirectorySeparatorChar,'/'),"C:/dev/TalesTactics/Builds/SourceOnlyPublish20261118/Project",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Wrong project.");
  if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)throw new InvalidOperationException("Editor must be idle.");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  EditorUtility.RequestScriptReload();
  return "Restored source imported; script domain reload requested. No gameplay source changes.";
 }
}