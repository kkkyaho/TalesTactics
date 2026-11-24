using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
public static class ReadSpriteCapabilities { public static object Main(string path="Assets/TalesTactics/Art/VisualPilot/Alphen/idle-v1.png") {
if (EditorApplication.isCompiling || EditorApplication.isUpdating) throw new InvalidOperationException("Idle Editor required.");

var importer=AssetImporter.GetAtPath(path) as TextureImporter;
if(importer==null)throw new InvalidOperationException("Existing pilot importer missing.");
var factories=new SpriteDataProviderFactories();factories.Init();
var provider=factories.GetSpriteEditorDataProviderFromObject(importer);
if(provider==null)throw new InvalidOperationException("Sprite provider missing.");
provider.InitSpriteEditorDataProvider();
var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
var required=new[]{EEditCapability.CreateAndDeleteSprite,EEditCapability.EditSpriteName,EEditCapability.EditSpriteRect,EEditCapability.EditPivot};
var rows=required.Select(value=>new{name=value.ToString(),supported=capability!=null&&capability.GetEditCapability().HasCapability(value)}).ToArray();
return new{readOnly=true,asset=path,providerType=provider.GetType().FullName,capabilityType=capability?.GetType().FullName,interfaceAssembly=typeof(ISpriteFrameEditCapability).Assembly.GetName().Name,rectCount=provider.GetSpriteRects().Length,flags=rows,passed=rows.All(r=>r.supported)};
} }