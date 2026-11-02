using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEditor;
public static class RefreshPreview9 {
 public static string Run() {
  if (Application.dataPath.Replace('\\','/') != "D:/TalesTacticsReleasePreview9/Assets") throw new Exception("Wrong Editor");
  if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Editor busy");
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  AssetDatabase.ImportAsset("Assets/Settings/UniversalRenderPipelineGlobalSettings.asset", ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
  var shaders=GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineRuntimeShaders>();
  var copyType=AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtilsResources")).First(t => t != null); var getter=typeof(GraphicsSettings).GetMethods().Single(m => m.Name == "GetRenderPipelineSettings" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0); var copy=getter.MakeGenericMethod(copyType).Invoke(null,null); var copyShader=(Shader)copyType.GetProperty("coreCopyPS").GetValue(copy);
  if (shaders == null || copy == null || shaders.coreBlitPS == null || shaders.coreBlitColorAndDepthPS == null || copyShader == null) throw new Exception("Required runtime URP resources unresolved");
  return Newtonsoft.Json.JsonConvert.SerializeObject(new {project=Application.dataPath,blit=shaders.coreBlitPS.name,blitGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(shaders.coreBlitPS)),depth=shaders.coreBlitColorAndDepthPS.name,copy=copyShader.name,updating=EditorApplication.isUpdating});
 }
}