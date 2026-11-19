using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace TalesTactics.Editor
{
    // Run in an Android validation checkout. Temporary player settings are restored even on failure.
    public static class AndroidBuild
    {
        public const string ApplicationId = "com.talestactics.game";
        public const string OutputPath = "Builds/Android/TalesTactics.apk";
        public const string LocalOutputPath = "Builds/Android/Local/TalesTactics.apk";

        [Serializable] sealed class Result
        {
            public string result, outputPath, applicationId, error, runId, startedUtc;
            public double durationSeconds;
            public long sizeBytes;
            public int errors, warnings, androidTextureOverrides, preservedTextureOverrides;
            public bool sourceOnly, containsLocalMusic, localMusicChecked;
            public string[] messages;
        }

        [MenuItem("TalesTactics/Build/Android APK (Local Test)")]
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
