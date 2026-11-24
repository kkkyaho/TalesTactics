using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Newtonsoft.Json.Linq;
public static class ReadRosterCapabilities
{
 public static object Main()
 {
  if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stopped idle Editor required.");
  var manifest=JObject.Parse(File.ReadAllText("Docs/RosterMotion20261125/art-layout.json"));
  var paths=manifest["characters"].SelectMany(c=>c["sheets"]).Select(s=>(string)s["path"]).ToArray();
  if(paths.Length!=18||paths.Distinct().Count()!=18||paths.Any(p=>!p.StartsWith("Assets/TalesTactics/Art/RosterMotion20261125/",StringComparison.Ordinal)))throw new InvalidOperationException("Exact 18 roster sheet paths required.");
  var factories=new SpriteDataProviderFactories();factories.Init();
  var required=new[]{EEditCapability.CreateAndDeleteSprite,EEditCapability.EditSpriteName,EEditCapability.EditSpriteRect,EEditCapability.EditPivot};
  var rows=paths.Select(path=>{
   var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new InvalidOperationException("Texture importer absent: "+path);
   var provider=factories.GetSpriteEditorDataProviderFromObject(importer);if(provider==null)throw new InvalidOperationException("Sprite provider absent: "+path);provider.InitSpriteEditorDataProvider();
   var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
   var flags=required.Select(value=>new{name=value.ToString(),supported=capability!=null&&capability.GetEditCapability().HasCapability(value)}).ToArray();
   return new{path,textureType=importer.textureType.ToString(),spriteMode=importer.spriteImportMode.ToString(),rectCount=provider.GetSpriteRects().Length,flags,passed=flags.All(f=>f.supported)};
  }).ToArray();
  return new{readOnly=true,passed=rows.All(r=>r.passed),count=rows.Length,rows};
 }
}