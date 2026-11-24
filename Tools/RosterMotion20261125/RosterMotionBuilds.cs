using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor.Build;
using UnityEngine.Rendering;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Newtonsoft.Json;

public static class RosterMotionBuilds
{
    const string Root="C:/dev/TalesTactics";
    const string Expected=Root+"/Builds/SourceOnlyPublish20261118/Project";
    const string Evidence=Root+"/Logs/RosterMotion20261125/Builds";
    const string FreezePath=Root+"/Logs/RosterMotion20261125/validation-product-freeze.json";
    static readonly string[] Owners={"cless","farah","jade","kisara","mint","natalia","shionne","tear","velvet"};
    sealed class FrozenFile {public string relative,sha256;public long bytes;}
    sealed class FrozenManifest {public int schemaVersion;public string scope;public FrozenFile[] files;}
    sealed class SourceRow {public string relative,expectedSHA256,rootSHA256,publicSHA256;public long expectedBytes,rootBytes,publicBytes;public bool passed;}
    sealed class SourceCheck {public bool passed;public string sha256,scope,manifestJson;public int fileCount;public SourceRow[] files;}
    sealed class PayloadRow {public string path;public int occurrences;}
    public static string Windows()=>Queue(false);
    public static string Android()=>Queue(true);
    static string Output(bool android)=>android?TalesTactics.Editor.RosterMotionAndroidBuild.OutputPath:"Builds/RosterMotion20261125/Windows/TalesTactics.exe";
    static void CheckCheckout()
    {
        if(!string.Equals(Path.GetFullPath(".").Replace('\\','/'),Expected,StringComparison.OrdinalIgnoreCase))throw new Exception("Wrong validation checkout");
    }
    internal static void RequireFreshOutput(string outputPath)
    {
        CheckCheckout();
        string full=Path.GetFullPath(outputPath).Replace('\\','/');
        if(!full.StartsWith(Expected+"/Builds/RosterMotion20261125/",StringComparison.OrdinalIgnoreCase))throw new Exception("Output escaped this task");
        string directory=Path.GetDirectoryName(full);
        if(File.Exists(full)||File.Exists(Path.Combine(directory,"build-result.json"))||
            (Directory.Exists(directory)&&Directory.EnumerateFileSystemEntries(directory).Any()))
            throw new Exception("Existing task output is protected; a retry needs a separately reviewed fresh Attempt path");
    }
    static void WriteNew(string path,string text)
    {
        using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
        using(var writer=new StreamWriter(stream,new System.Text.UTF8Encoding(false)))writer.Write(text);
    }
    static string Hash(string path)
    {
        using(var algorithm=SHA256.Create())using(var stream=File.OpenRead(path))
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
    }
    static string Inside(string checkout,string relative)
    {
        if(string.IsNullOrEmpty(relative)||relative.Contains('\\')||Path.IsPathRooted(relative)||
            relative.Split('/').Any(p=>p==".."||p=="."||p.Length==0)||relative.IndexOf("/LocalMusic/",StringComparison.OrdinalIgnoreCase)>=0)
            throw new Exception("Invalid source-freeze path");
        string path=Path.GetFullPath(Path.Combine(checkout,relative)).Replace('\\','/');
        if(!path.StartsWith(checkout+"/",StringComparison.OrdinalIgnoreCase))throw new Exception("Source-freeze path escaped checkout");
        return path;
    }
    static SourceCheck InspectSource(string expectedSHA=null)
    {
        byte[] raw=File.ReadAllBytes(FreezePath);string json=System.Text.Encoding.UTF8.GetString(raw).TrimStart('\uFEFF'),sha;
        using(var algorithm=SHA256.Create())sha=BitConverter.ToString(algorithm.ComputeHash(raw)).Replace("-","").ToLowerInvariant();
        if(expectedSHA!=null&&sha!=expectedSHA)throw new Exception("Source-freeze document changed since queueing");
        var manifest=JsonConvert.DeserializeObject<FrozenManifest>(json);
        if(manifest==null||manifest.schemaVersion!=1||manifest.files==null||manifest.files.Length!=83||
            manifest.files.Select(f=>f.relative).Distinct(StringComparer.Ordinal).Count()!=83)
            throw new Exception("Expected the exact 83-file product freeze");
        var rows=new List<SourceRow>();
        foreach(var entry in manifest.files)
        {
            if(entry.sha256==null||entry.sha256.Length!=64||entry.sha256.Any(c=>!"0123456789abcdef".Contains(c))||entry.bytes<0)
                throw new Exception("Malformed source-freeze fingerprint");
            string root=Inside(Root,entry.relative),pub=Inside(Expected,entry.relative);
            bool rootExists=File.Exists(root),publicExists=File.Exists(pub);
            var row=new SourceRow{relative=entry.relative,expectedSHA256=entry.sha256,expectedBytes=entry.bytes,
                rootSHA256=rootExists?Hash(root):null,publicSHA256=publicExists?Hash(pub):null,
                rootBytes=rootExists?new FileInfo(root).Length:-1,publicBytes=publicExists?new FileInfo(pub).Length:-1};
            row.passed=row.rootSHA256==entry.sha256&&row.publicSHA256==entry.sha256&&row.rootBytes==entry.bytes&&row.publicBytes==entry.bytes;rows.Add(row);
        }
        if(Hash(FreezePath)!=sha)throw new Exception("Source-freeze document changed while reading");
        return new SourceCheck{passed=rows.All(r=>r.passed),sha256=sha,scope=manifest.scope,manifestJson=json,fileCount=rows.Count,files=rows.ToArray()};
    }
    static string[] ExpectedPayload()
    {
        return Owners.SelectMany(id=>new[]{"Assets/TalesTactics/Art/RosterMotion20261125/"+id+"/idle-v1.png",
            "Assets/TalesTactics/Art/RosterMotion20261125/"+id+"/action-v1.png",
            "Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/"+id+".asset"})
            .Concat(new[]{"Assets/TalesTactics/Art/VisualPilot/Alphen/idle-v1.png",
                "Assets/TalesTactics/Art/VisualPilot/Alphen/action-v1.png",
                "Assets/TalesTactics/Resources/TalesTactics/VisualPilot/AlphenMotion.asset"}).ToArray();
    }
    static string Queue(bool android)
    {
        CheckCheckout();
        if(EditorApplication.isPlayingOrWillChangePlaymode||BuildPipeline.isBuildingPlayer)throw new Exception("Editor busy");
        if(EditorUserBuildSettings.activeBuildTarget!=(android?BuildTarget.Android:BuildTarget.StandaloneWindows64))throw new Exception("Wrong build target");
        if(Directory.Exists("Assets/TalesTactics/Audio/LocalMusic"))throw new Exception("Source-only checkout required");
        string platform=android?"android":"windows";RequireFreshOutput(Output(android));
        foreach(string suffix in new[]{"-build-final.json","-queued.json","-build-result.json",".buildreport"})
            if(File.Exists(Evidence+"/"+platform+suffix))throw new Exception("Existing build evidence is protected; use a new reviewed Attempt path");
        var source=InspectSource();if(!source.passed)throw new Exception("ROOT/PUBLIC no longer match the frozen source");
        string runId=Guid.NewGuid().ToString("N");Directory.CreateDirectory(Evidence);
        WriteNew(Evidence+"/"+platform+"-queued.json",JsonConvert.SerializeObject(new{runId,queuedUtc=DateTime.UtcNow.ToString("o"),platform,sourceFreezeSHA256=source.sha256,sourceFileCount=source.fileCount}));
        var ready=DateTime.UtcNow.AddSeconds(3);EditorApplication.CallbackFunction callback=null;
        callback=()=>{if(DateTime.UtcNow<ready)return;EditorApplication.update-=callback;Build(android,runId,source.sha256);};
        EditorApplication.update+=callback;return platform+" queued once; runId="+runId;
    }
    static void Build(bool android,string runId,string sourceFreezeSHA256)
    {
        string platform=android?"android":"windows",error=null,startedUtc=DateTime.UtcNow.ToString("o");BuildReport report=null;
        var settings=Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");string before=EditorJsonUtility.ToJson(settings);
        bool passed=false,settingsEqual=false,payloadVerified=false,localMusicChecked=false,containsLocalMusic=false,outputReserved=false;
        SourceCheck sourceBefore=null,sourceAfter=null;PayloadRow[] packedPayload=Array.Empty<PayloadRow>();string[] missingPayload=ExpectedPayload();
        string artifactSHA256=null,buildReportSHA256=null;long artifactBytes=0;
        try
        {
            RequireFreshOutput(Output(android));outputReserved=true;sourceBefore=InspectSource(sourceFreezeSHA256);
            if(!sourceBefore.passed)throw new Exception("Frozen source changed before the build");
            if(android)
            {
                TalesTactics.Editor.RosterMotionAndroidBuild.SourceOnlyApk(runId,startedUtc);report=BuildReport.GetLatestReport();
                File.Copy("Builds/RosterMotion20261125/Android/build-result.json",Evidence+"/android-build-result.json",false);
            }
            else
            {
                Directory.CreateDirectory("Builds/RosterMotion20261125/Windows");
                report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/TalesTactics/Scenes/TestBattle.unity"},locationPathName=Output(false),
                    target=BuildTarget.StandaloneWindows64,options=BuildOptions.DetailedBuildReport,extraScriptingDefines=Array.Empty<string>()});
            }
            if(report==null||report.summary.result!=BuildResult.Succeeded||report.summary.totalErrors!=0)throw new Exception("Build failed");
            if(!string.Equals(Path.GetFullPath(report.summary.outputPath),Path.GetFullPath(Output(android)),StringComparison.OrdinalIgnoreCase))throw new Exception("BuildReport output mismatch");
            if(File.Exists("Library/LastBuild.buildreport"))
            {
                string copy=Evidence+"/"+platform+".buildreport";File.Copy("Library/LastBuild.buildreport",copy,false);buildReportSHA256=Hash(copy);
            }
            if(buildReportSHA256==null)throw new Exception("BuildReport file is missing");
            var contents=report.packedAssets.SelectMany(p=>p.contents).Select(a=>(a.sourceAssetPath??"").Replace('\\','/')).ToArray();
            localMusicChecked=true;containsLocalMusic=contents.Any(p=>p.IndexOf("/LocalMusic/",StringComparison.OrdinalIgnoreCase)>=0);
            if(containsLocalMusic)throw new Exception("Unexpected user music in source-only build");
            packedPayload=ExpectedPayload().Select(path=>new PayloadRow{path=path,occurrences=contents.Count(actual=>actual==path)}).ToArray();
            missingPayload=packedPayload.Where(p=>p.occurrences==0).Select(p=>p.path).ToArray();
            payloadVerified=packedPayload.Length==30&&missingPayload.Length==0;
            if(!payloadVerified)throw new Exception("Build is missing roster payload: "+string.Join(", ",missingPayload));
            string artifact=Path.GetFullPath(Output(android));artifactBytes=new FileInfo(artifact).Length;artifactSHA256=Hash(artifact);
            if(artifactBytes<=0)throw new Exception("Empty player output");
            AssetDatabase.SaveAssets();passed=true;
        }
        catch(Exception ex){error=ex.ToString();Debug.LogException(ex);}
        finally
        {
            try{sourceAfter=InspectSource(sourceFreezeSHA256);if(!sourceAfter.passed)error+="\nFrozen source changed after build";}
            catch(Exception ex){error+="\nPost-build source check: "+ex;}
            settingsEqual=before==EditorJsonUtility.ToJson(settings);
            bool sourceFreezeMatched=sourceBefore?.passed==true&&sourceAfter?.passed==true&&sourceBefore.sha256==sourceAfter.sha256;
            passed=passed&&settingsEqual&&sourceFreezeMatched&&payloadVerified&&!File.Exists("Library/TalesTactics/AndroidTextureSettingsRecovery.json");
            string finalJson=JsonConvert.SerializeObject(new{runId,startedUtc,finishedUtc=DateTime.UtcNow.ToString("o"),passed,settingsEqual,error,
                sourceOnly=true,localMusicChecked,containsLocalMusic,devicePlaybackTested=false,sourceFreezeSHA256,sourceFileCount=83,sourceFreezeMatched,
                sourceFreezeScope="The exact 83-file product/import-layout manifest is embedded in sourceBefore/sourceAfter. This does not claim an all-repository source inventory.",
                sourceBefore,sourceAfter,payloadVerified,requiredPayloadCount=30,packedPayload,missingPayload,artifactSHA256,artifactBytes,buildReportSHA256,
                result=report?.summary.result.ToString(),target=report?.summary.platform.ToString(),seconds=report?.summary.totalTime.TotalSeconds,
                bytes=report?.summary.totalSize,errors=report?.summary.totalErrors,warnings=report?.summary.totalWarnings,
                output=report?.summary.outputPath,outputPath=report?.summary.outputPath,
                messages=report?.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Warning||m.type==LogType.Error).Select(m=>m.content).ToArray()},Formatting.Indented);
            WriteNew(Evidence+"/"+platform+"-build-final.json",finalJson);
            if(!android&&outputReserved){Directory.CreateDirectory("Builds/RosterMotion20261125/Windows");WriteNew("Builds/RosterMotion20261125/Windows/build-result.json",finalJson);}
        }
    }
}

namespace TalesTactics.Editor
{
    // Run in an Android validation checkout. Temporary player settings are restored even on failure.
    public static class RosterMotionAndroidBuild
    {
        public const string ApplicationId = "com.talestactics.game";
        public const string OutputPath = "Builds/RosterMotion20261125/Android/TalesTactics.apk";
        public const string LocalOutputPath = "Builds/RosterMotion20261125/AndroidLocal/TalesTactics.apk";

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
        public static void LocalApk() => Build(false,null,null);

        public static void SourceOnlyApk() => Build(true,null,null);

        internal static void SourceOnlyApk(string runId,string startedUtc) => Build(true,runId,startedUtc);

        static void Build(bool sourceOnly,string runId,string startedUtc)
        {
            string outputPath = sourceOnly ? OutputPath : LocalOutputPath;
            string reportPath = Path.Combine(Path.GetDirectoryName(outputPath), "build-result.json");
            RosterMotionBuilds.RequireFreshOutput(outputPath);
            var result = new Result { applicationId = ApplicationId, outputPath = Path.GetFullPath(outputPath),
                sourceOnly = sourceOnly, result = "Running", runId = runId ?? Guid.NewGuid().ToString("N"),
                startedUtc = startedUtc ?? DateTime.UtcNow.ToString("o") };
            SavedSettings settings = null;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                // The fresh-output guard above rejects any earlier APK/report before this run writes evidence.
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
                    options = BuildOptions.DetailedBuildReport, extraScriptingDefines = Array.Empty<string>()
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
