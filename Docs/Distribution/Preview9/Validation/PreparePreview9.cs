using System;
using UnityEngine;
using UnityEditor;
public static class PreparePreview9 {
 public static string Run() {
  if (Application.dataPath.Replace('\\','/') != "D:/TalesTacticsReleasePreview9/Assets") throw new Exception("Wrong Editor");
  if (EditorApplication.isUpdating || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Editor is busy");
  if (!SessionState.GetBool("Preview9RefreshHeld", false)) { AssetDatabase.DisallowAutoRefresh(); SessionState.SetBool("Preview9RefreshHeld", true); }
  return Newtonsoft.Json.JsonConvert.SerializeObject(new { project=Application.dataPath, refreshHeld=SessionState.GetBool("Preview9RefreshHeld",false), compiling=EditorApplication.isCompiling, updating=EditorApplication.isUpdating });
 }
}