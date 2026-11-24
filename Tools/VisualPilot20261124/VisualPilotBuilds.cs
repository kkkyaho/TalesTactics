using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine.Rendering;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Newtonsoft.Json;
public static class VisualPilotBuilds
{
    const string Expected="C:/dev/TalesTactics/Builds/SourceOnlyPublish20261118/Project";
    const string Evidence="C:/dev/TalesTactics/Logs/VisualPilot20261124/Builds";
    public static string Windows()=>Queue(false);
    public static string Android()=>Queue(true);
    static string Queue(bool android)
    {
        if(!string.Equals(Path.GetFullPath(".").Replace('\\','/'),Expected,StringComparison.OrdinalIgnoreCase))throw new Exception("Wrong validation checkout");
        if(EditorApplication.isPlayingOrWillChangePlaymode||BuildPipeline.isBuildingPlayer)throw new Exception("Editor busy");
        var expectedTarget=android?BuildTarget.Android:BuildTarget.StandaloneWindows64;
        if(EditorUserBuildSettings.activeBuildTarget!=expectedTarget)throw new Exception("Wrong build target");
        if(Directory.Exists("Assets/TalesTactics/Audio/LocalMusic"))throw new Exception("Source-only checkout required");
        Directory.CreateDirectory(Evidence);string platform=android?"android":"windows";
        if(File.Exists(Evidence+"/"+platform+"-build-final.json")||File.Exists(Evidence+"/"+platform+"-queued.json"))throw new Exception("Already queued");
        File.WriteAllText(Evidence+"/"+platform+"-queued.json",JsonConvert.SerializeObject(new {startedUtc=DateTime.UtcNow.ToString("o"),platform}));
        var ready=DateTime.UtcNow.AddSeconds(3);EditorApplication.CallbackFunction callback=null;
        callback=()=>{if(DateTime.UtcNow<ready)return;EditorApplication.update-=callback;Build(android);};
        EditorApplication.update+=callback;
        return platform+" queued once";
    }
    static void Build(bool android)
    {
        string platform=android?"android":"windows",error=null;string runId=Guid.NewGuid().ToString("N"),startedUtc=DateTime.UtcNow.ToString("o");BuildReport report=null;
        var settings=Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");string before=EditorJsonUtility.ToJson(settings);
        bool passed=false,settingsEqual=false;
        try
        {
            if(android)
            {
                TalesTactics.Editor.VisualPilotAndroidBuild.SourceOnlyApk();report=BuildReport.GetLatestReport();
                File.Copy("Builds/VisualPilot20261124/Android/build-result.json",Evidence+"/android-build-result.json",true);
            }
            else
            {
                Directory.CreateDirectory("Builds/VisualPilot20261124/Windows");
                report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/TalesTactics/Scenes/TestBattle.unity"},locationPathName="Builds/VisualPilot20261124/Windows/TalesTactics.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,extraScriptingDefines=Array.Empty<string>()});
            }
            if(report==null||report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed");
            if(File.Exists("Library/LastBuild.buildreport"))File.Copy("Library/LastBuild.buildreport",Evidence+"/"+platform+".buildreport",true);
            if(report.packedAssets.SelectMany(p=>p.contents).Any(a=>a.sourceAssetPath.Contains("/LocalMusic/")))throw new Exception("Unexpected user music in source-only build");
            AssetDatabase.SaveAssets();passed=true;
        }
        catch(Exception ex){error=ex.ToString();Debug.LogException(ex);}
        finally
        {
            settingsEqual=before==EditorJsonUtility.ToJson(settings);
            passed=passed&&settingsEqual&&!File.Exists("Library/TalesTactics/AndroidTextureSettingsRecovery.json");
            string finalJson=JsonConvert.SerializeObject(new{runId,startedUtc,finishedUtc=DateTime.UtcNow.ToString("o"),passed,settingsEqual,error,sourceOnly=true,localMusicChecked=report!=null,containsLocalMusic=report!=null&&report.packedAssets.SelectMany(p=>p.contents).Any(a=>a.sourceAssetPath.Contains("/LocalMusic/")),devicePlaybackTested=false,result=report?.summary.result.ToString(),target=report?.summary.platform.ToString(),seconds=report?.summary.totalTime.TotalSeconds,bytes=report?.summary.totalSize,errors=report?.summary.totalErrors,warnings=report?.summary.totalWarnings,output=report?.summary.outputPath,outputPath=report?.summary.outputPath,messages=report?.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Warning||m.type==LogType.Error).Select(m=>m.content).ToArray()},Formatting.Indented);File.WriteAllText(Evidence+"/"+platform+"-build-final.json",finalJson);if(!android){Directory.CreateDirectory("Builds/VisualPilot20261124/Windows");File.WriteAllText("Builds/VisualPilot20261124/Windows/build-result.json",finalJson);}
        }
    }
}
namespace TalesTactics.Editor
{
    // Run in an Android validation checkout. Temporary player settings are restored even on failure.
    public static class VisualPilotAndroidBuild
    {
        public const string ApplicationId = "com.talestactics.game";
        public const string OutputPath = "Builds/VisualPilot20261124/Android/TalesTactics.apk";
        public const string LocalOutputPath = "Builds/VisualPilot20261124/AndroidLocal/TalesTactics.apk";

        [Serializable] sealed class Result
        {
            public string result, outputPath, applicationId, error, runId, startedUtc;
            public double durationSeconds;
            public long sizeBytes;
            public int errors, warnings, androidTextureOverrides, preservedTextureOverrides;
            public bool sourceOnly, containsLocalMusic, localMusicChecked;
            public string[] messages;
        }

        // No project menu is registered by this external verification copy.
        public static void LocalApk() => Build(false);

        public static void SourceOnlyApk() => Build(true);

        static void Build(bool sourceOnly)
        {
            string outputPath = sourceOnly ? OutputPath : LocalOutputPath;
            string reportPath = Path.Combine(Path.GetDirectoryName(outputPath), "build-result.json");
            var result = new Result { applicationId = ApplicationId, outputPath = Path.GetFullPath(outputPath),
                sourceOnly = sourceOnly, result = "Running", runId = Guid.NewGuid().ToString("N"),
                startedUtc = DateTime.UtcNow.ToString("o") };
            SavedSettings settings = null;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                // Replace an older success report before any preflight check. Existing APKs remain untouched.
                File.WriteAllText(reportPath, JsonUtility.ToJson(result, true));
                if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                    throw new InvalidOperationException("Stop Play mode and any previous build first.");
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                    throw new InvalidOperationException("Open a separate checkout with Android as the active build target first.");
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                    throw new InvalidOperationException("Install Android Build Support with SDK, NDK and OpenJDK for this Editor.");

                result.containsLocalMusic = Directory.Exists("Assets/TalesTactics/Audio/LocalMusic") &&
                    Directory.EnumerateFiles("Assets/TalesTactics/Audio/LocalMusic", "*", SearchOption.AllDirectories)
                        .Any(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase));
                result.localMusicChecked = true;
                if (sourceOnly && result.containsLocalMusic)
                    throw new InvalidOperationException("The source-only APK must be built from the checkout without user-provided music.");

                settings = new SavedSettings();
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
                PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
                PlayerSettings.Android.useCustomKeystore = false; // Local test signing; no store credentials are read.
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
                PlayerSettings.allowedAutorotateToPortrait = false;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
                PlayerSettings.allowedAutorotateToLandscapeLeft = true;
                PlayerSettings.allowedAutorotateToLandscapeRight = true;
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.Vulkan });
                EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                using var textures = new AndroidTextureSettingsScope();
                result.androidTextureOverrides = textures.AppliedCount;
                result.preservedTextureOverrides = textures.PreservedOverrideCount;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { "Assets/TalesTactics/Scenes/TestBattle.unity" },
                    locationPathName = outputPath, target = BuildTarget.Android,
                    options = BuildOptions.None, extraScriptingDefines = Array.Empty<string>()
                });
                var summary = report.summary;
                result.result = summary.result.ToString();
                result.durationSeconds = summary.totalTime.TotalSeconds;
                result.sizeBytes = (long)summary.totalSize;
                result.errors = summary.totalErrors;
                result.warnings = summary.totalWarnings;
                result.messages = report.steps.SelectMany(step => step.messages)
                    .Where(message => message.type == LogType.Error || message.type == LogType.Warning)
                    .Select(message => message.content).ToArray();
                if (summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Android APK build failed: " + summary.result);
            }
            catch (Exception exception)
            {
                result.result = "Failed";
                result.error = exception.ToString();
                throw;
            }
            finally
            {
                try { settings?.Restore(); }
                catch (Exception restoreError) { result.result = "Failed"; result.error += "\nSettings restore failed: " + restoreError; throw; }
                finally { File.WriteAllText(reportPath, JsonUtility.ToJson(result, true)); }
            }
        }

        sealed class SavedSettings
        {
            readonly UnityEngine.Object playerSettingsObject = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
            readonly string serializedPlayerSettings;

            public SavedSettings()
            {
                if (playerSettingsObject == null)
                    throw new InvalidOperationException("PlayerSettings could not be captured before the Android build.");
                // Getters resolve implicit defaults. Preserve their serialized absence and API side effects too.
                // Keep this snapshot in memory: PlayerSettings can contain signing credentials.
                serializedPlayerSettings = EditorJsonUtility.ToJson(playerSettingsObject);
            }
            readonly string id = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            readonly ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
            readonly Il2CppCompilerConfiguration compiler = PlayerSettings.GetIl2CppCompilerConfiguration(NamedBuildTarget.Android);
            readonly AndroidArchitecture architectures = PlayerSettings.Android.targetArchitectures;
            readonly AndroidSdkVersions minSdk = PlayerSettings.Android.minSdkVersion, targetSdk = PlayerSettings.Android.targetSdkVersion;
            readonly bool customKeystore = PlayerSettings.Android.useCustomKeystore;
            readonly UIOrientation orientation = PlayerSettings.defaultInterfaceOrientation;
            readonly bool portrait = PlayerSettings.allowedAutorotateToPortrait, upsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            readonly bool left = PlayerSettings.allowedAutorotateToLandscapeLeft, right = PlayerSettings.allowedAutorotateToLandscapeRight;
            readonly bool defaultApis = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
            readonly GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            readonly MobileTextureSubtarget textures = EditorUserBuildSettings.androidBuildSubtarget;
            readonly bool appBundle = EditorUserBuildSettings.buildAppBundle, export = EditorUserBuildSettings.exportAsGoogleAndroidProject;

            public void Restore()
            {
                var failures = new List<Exception>();
                void Attempt(string setting, Action restore)
                {
                    try { restore(); }
                    catch (Exception exception) { failures.Add(new InvalidOperationException("Could not restore " + setting, exception)); }
                }
                Attempt("application identifier", () => PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, id));
                Attempt("scripting backend", () => PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, backend));
                Attempt("IL2CPP compiler configuration", () => PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, compiler));
                Attempt("Android architectures", () => PlayerSettings.Android.targetArchitectures = architectures);
                Attempt("minimum SDK", () => PlayerSettings.Android.minSdkVersion = minSdk);
                Attempt("target SDK", () => PlayerSettings.Android.targetSdkVersion = targetSdk);
                Attempt("custom keystore toggle", () => PlayerSettings.Android.useCustomKeystore = customKeystore);
                Attempt("interface orientation", () => PlayerSettings.defaultInterfaceOrientation = orientation);
                Attempt("portrait rotation", () => PlayerSettings.allowedAutorotateToPortrait = portrait);
                Attempt("upside-down rotation", () => PlayerSettings.allowedAutorotateToPortraitUpsideDown = upsideDown);
                Attempt("landscape-left rotation", () => PlayerSettings.allowedAutorotateToLandscapeLeft = left);
                Attempt("landscape-right rotation", () => PlayerSettings.allowedAutorotateToLandscapeRight = right);
                Attempt("graphics APIs", () => PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, apis));
                Attempt("default graphics API toggle", () => PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, defaultApis));
                Attempt("texture subtarget", () => EditorUserBuildSettings.androidBuildSubtarget = textures);
                Attempt("app bundle toggle", () => EditorUserBuildSettings.buildAppBundle = appBundle);
                Attempt("Android project export toggle", () => EditorUserBuildSettings.exportAsGoogleAndroidProject = export);
                Attempt("serialized player settings", () => {
                    EditorJsonUtility.FromJsonOverwrite(serializedPlayerSettings, playerSettingsObject);
                    EditorUtility.SetDirty(playerSettingsObject);
                    if (EditorJsonUtility.ToJson(playerSettingsObject) != serializedPlayerSettings)
                        throw new InvalidOperationException("Serialized PlayerSettings differ from the pre-build snapshot.");
                });
                if (failures.Count > 0) throw new AggregateException("Some Android build settings could not be restored.", failures);
            }
        }
    }
}
