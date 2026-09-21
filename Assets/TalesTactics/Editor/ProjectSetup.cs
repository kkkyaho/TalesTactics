using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
namespace TalesTactics.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string ScenePath="Assets/TalesTactics/Scenes/TestBattle.unity";
        static bool importingResources;
        static ProjectSetup(){EditorApplication.delayCall+=FirstImport;}
        static void FirstImport()
        {
            if(Application.isBatchMode||AssetDatabase.IsAssetImportWorkerProcess()||EditorApplication.isPlayingOrWillChangePlaymode||File.Exists(ScenePath))return;
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=FirstImport;return;}
            CreateScene();
        }
        [MenuItem("Tales Tactics/Create Test Battle")]
        public static void CreateScene()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                if(importingResources)return;
                importingResources=true;AssetDatabase.importPackageCompleted+=ResourcesImported;
                AssetDatabase.importPackageFailed+=ResourcesFailed;
                TMPro.TMP_PackageResourceImporter.ImportResources(true,false,false);
                return;
            }
            var catalog=AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/TalesTactics/Content/BattleCatalog.asset")??DemoContent.Create();
            if(!CatalogValidation.TryValidate(catalog,out var error))throw new System.InvalidOperationException("Cannot create battle scene: "+error);
            Directory.CreateDirectory("Assets/TalesTactics/Scenes");Directory.CreateDirectory("Assets/TalesTactics/Materials");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("Battle Systems");var director=root.AddComponent<BattleDirector>();director.Catalog=catalog;
            director.Audio=root.AddComponent<BattleAudio>();director.Audio.Library=catalog.Audio;
            var cameraObject=new GameObject("Battle Camera");cameraObject.tag="MainCamera";var camera=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<AudioListener>();
            camera.orthographic=true;camera.orthographicSize=7.6f;camera.nearClipPlane=0.1f;camera.farClipPlane=100;camera.backgroundColor=new Color(0.065f,0.09f,0.12f);camera.clearFlags=CameraClearFlags.SolidColor;
            camera.transform.position=new Vector3(15,17,-9);camera.transform.LookAt(new Vector3(4.5f,0,4));
            var light=new GameObject("Sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(50,-30,0);
            var board=new GameObject("Board").AddComponent<BoardView>();board.BattleCamera=camera;board.TileMaterial=Material("Tiles","Universal Render Pipeline/Unlit");board.HighlightMaterial=Material("Highlights","Universal Render Pipeline/Unlit");board.SpriteMaterial=Material("Sprites","Sprites/Default");director.Board=board;
            var canvasObject=new GameObject("Battle HUD",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scaler=canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,900);scaler.matchWidthOrHeight=0.5f;
            director.Hud=canvasObject.AddComponent<BattleHud>();
            new GameObject("Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));
            PlayerSettings.companyName="LocalTactics";PlayerSettings.productName="TalesTactics";PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();
            Debug.Log("TalesTactics: TestBattle created. Press Play for deployment.");
        }
        static void ResourcesImported(string packageName)
        {
            if(packageName!="TMP Essential Resources")return;
            FinishResourceImport();EditorApplication.delayCall+=CreateScene;
        }
        static void ResourcesFailed(string packageName,string error)
        {
            if(packageName!="TMP Essential Resources")return;
            FinishResourceImport();Debug.LogError("TalesTactics: TMP resource import failed: "+error);
        }
        static void FinishResourceImport()
        {importingResources=false;AssetDatabase.importPackageCompleted-=ResourcesImported;AssetDatabase.importPackageFailed-=ResourcesFailed;}
        static Material Material(string name,string shaderName)
        {
            var path="Assets/TalesTactics/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat!=null)return mat;
            var shader=Shader.Find(shaderName);if(shader==null)throw new System.InvalidOperationException("Missing shader: "+shaderName);
            mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);return mat;
        }
        [MenuItem("Tales Tactics/Build Windows Player")]
        public static void Build()
        {
            if(!File.Exists(ScenePath))CreateScene();
            if(!File.Exists(ScenePath))throw new System.InvalidOperationException("TestBattle is not ready. Wait for TMP import and scene creation before building.");
            Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Windows/TalesTactics.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Player build failed");
        }
    }
}
