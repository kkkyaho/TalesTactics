using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

// External Pipeline run_script source; never import into Assets.
// Entry: Preview9FinalBuild.Run, args: ["D:/TalesTacticsBuildArchive/WindowsPreview9Final2"].
// Only the exact release-worktree Editor may run it. A Scheduled receipt is not success.
// All helper evidence stays outside the player output. No SaveAssets, Refresh,
// DemoContent, settings mutation, package operation, test run or player launch occurs.
public static class Preview9FinalBuild
{
    const string Project = "D:/TalesTacticsReleasePreview9";
    const string Output = "D:/TalesTacticsBuildArchive/WindowsPreview9Final2";
    const string ExpectedHead = "e081a592d398aa0354b04a827fb3833a277552d2";
    const string RequiredUnity = "6000.6.0f1";
    const string Scene = "Assets/TalesTactics/Scenes/TestBattle.unity";
    const string Source = "C:/dev/TalesTactics/Docs/Distribution/Preview9/Build-Preview9Final.cs";
    const string EvidenceRoot = "C:/dev/TalesTactics/Logs/DistributionPreview9";
    const string ActiveKey = "TalesTactics.Preview9FinalBuild.Active";
    const string CleanPaths = "status --porcelain=v1 -z --untracked-files=all -- Assets Packages ProjectSettings";
    static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

    [Serializable] public sealed class Receipt
    { public string status, runId, output, evidence, finalResult, packerReport; public bool buildCompleted; }
    [Serializable] public sealed class Property
    { public string name, type, value, error; }
    [Serializable] public sealed class Message
    { public int stepIndex, messageIndex; public string step, type, content; }
    [Serializable] public sealed class Step
    { public int index, depth; public string name; public long durationTicks; public double durationSeconds; public Message[] messages; }
    [Serializable] public sealed class EditorLog
    { public string utc, type, condition, stackTrace; }
    [Serializable] public sealed class ProtectedFile
    { public string path, sha256; public long bytes; }
    [Serializable] public sealed class ReportFile
    { public string path, role; public ulong sizeBytes; }
    [Serializable] public sealed class Artifact
    { public string path, sha256; public long bytes; }
    [Serializable] public sealed class SemanticFile
    { public string path, classification, headSHA256, currentSHA256, headEvidence, currentEvidence; public bool matches; }
    [Serializable] public sealed class ResourceCheck
    { public string type, property, shaderName, assetPath, guid; public bool matches; }
    [Serializable] public sealed class Result
    {
        public int schema = 1;
        public string runId, status, startedUtc, finishedUtc, output, evidence, script = Source, scriptSHA256, unityVersion;
        public string project = Project, expectedSourceHead = ExpectedHead, sourceHeadBefore, sourceHeadAfter;
        public string sourceStatusBefore, sourceStatusAfter;
        public bool sourceCleanBefore, sourceCleanAfter, sourceHeadUnchanged, complete, passed, buildStarted, outputCreated, protectedFilesUnchanged;
        public bool review = false;
        public string scene = Scene, target = "StandaloneWindows64", options = "None", projectDefines;
        public string[] extraScriptingDefines = Array.Empty<string>();
        public string summaryResult, summaryOutputPath, buildGuid, platform, platformGroup, buildStartedAt, buildEndedAt, summaryOptions;
        public long totalTimeTicks;
        public double totalTimeSeconds;
        public ulong totalSize;
        public int totalErrors, totalWarnings;
        public Property[] summaryProperties = Array.Empty<Property>();
        public Step[] steps = Array.Empty<Step>();
        public ReportFile[] files = Array.Empty<ReportFile>();
        public Message[] warningErrorMessages = Array.Empty<Message>();
        public EditorLog[] observedEditorWarningsAndErrors = Array.Empty<EditorLog>();
        public ProtectedFile[] protectedBefore = Array.Empty<ProtectedFile>(), protectedAfter = Array.Empty<ProtectedFile>();
        public Artifact executable, runtime;
        public string exception, finalizationException;
        public bool semanticSourceMatchesBefore, semanticSourceMatchesAfter;
        public SemanticFile[] semanticAuditBefore = Array.Empty<SemanticFile>(), semanticAuditAfter = Array.Empty<SemanticFile>();
        public ResourceCheck[] runtimeShaderPreflight = Array.Empty<ResourceCheck>();
        public string scope = "Full BuildReport and actual GetFiles are preserved. Every tracked path is compared to pinned HEAD by Git, and changed candidates undergo exact bytes or narrow text/schema equivalence checks. All tracked files are SHA-256 snapshotted before/after; raw clean and byte-equality flags are never rewritten. Known serializer input versions are archived per phase. Product untracked files are rejected. Runtime shader references must resolve to their expected name/GUID before build. Semantic pass permits only EOL/trailing whitespace, the derived 16-entry runtime list clearing, and the exact disabled UnityConnect migration. No source restoration, saving, refresh or auto-refresh suppression occurs.";
    }

    public static string Run(string outputDirectory)
    {
        ValidateContext();
        if (string.IsNullOrWhiteSpace(outputDirectory) || !Path.IsPathRooted(outputDirectory) || !SamePath(outputDirectory, Output))
            throw new ArgumentException("Only D:/TalesTacticsBuildArchive/WindowsPreview9Final2 is allowed.");
        var result = new Result
        {
            runId = Guid.NewGuid().ToString("N"), status = "Validating", startedUtc = DateTime.UtcNow.ToString("o"),
            output = Path.GetFullPath(Output), unityVersion = Application.unityVersion
        };
        RejectReparseAncestors(EvidenceRoot);
        result.evidence = Path.Combine(EvidenceRoot, "Build-" + result.runId);
        if (Directory.Exists(result.evidence) || File.Exists(result.evidence)) throw new IOException("Evidence path already exists.");
        Directory.CreateDirectory(result.evidence);
        try
        {
            result.scriptSHA256 = Hash(Source);
            ValidateReady(result);
            result.sourceHeadBefore = Git("rev-parse HEAD").Trim();
            result.sourceStatusBefore = Git(CleanPaths);
            result.sourceCleanBefore = result.sourceStatusBefore.Length == 0;
            RequireSource(result.sourceHeadBefore);
            string active = SessionState.GetString(ActiveKey, "");
            if (!string.IsNullOrEmpty(active)) throw new InvalidOperationException("A Preview9 build is already reserved: " + active);
            SessionState.SetString(ActiveKey, result.runId);
            result.status = "Scheduled";
            WriteNew(Path.Combine(result.evidence, "scheduled.json"), result);

            EditorApplication.CallbackFunction callback = null;
            AssemblyReloadEvents.AssemblyReloadCallback cancel = null;
            cancel = () =>
            {
                EditorApplication.update -= callback;
                AssemblyReloadEvents.beforeAssemblyReload -= cancel;
                result.status = "CancelledBeforeBuild";
                result.exception = "Editor domain reload occurred before the scheduled build began.";
                Complete(result);
            };
            callback = () =>
            {
                EditorApplication.update -= callback;
                AssemblyReloadEvents.beforeAssemblyReload -= cancel;
                Execute(result);
            };
            AssemblyReloadEvents.beforeAssemblyReload += cancel;
            EditorApplication.update += callback;
            return JsonUtility.ToJson(new Receipt
            {
                status = "Scheduled", runId = result.runId, output = result.output, evidence = result.evidence,
                finalResult = Path.Combine(result.evidence, "build-result.json"),
                packerReport = Path.Combine(result.evidence, "windows-build.json"), buildCompleted = false
            });
        }
        catch (Exception error)
        {
            result.status = "Rejected"; result.exception = error.ToString();
            Complete(result);
            throw;
        }
    }

    static void ValidateContext()
    {
        if (!SamePath(Directory.GetParent(Application.dataPath).FullName, Project))
            throw new InvalidOperationException("Only the D:/TalesTacticsReleasePreview9 Editor is allowed.");
        if (Application.unityVersion != RequiredUnity)
            throw new InvalidOperationException("Unity " + RequiredUnity + " is required; no project upgrade is allowed.");
        RejectReparseAncestors(Project);
        if (!SamePath(Git("rev-parse --show-toplevel").Trim(), Project))
            throw new InvalidOperationException("The release worktree must be the Git repository root.");
    }

    static void ValidateReady(Result result)
    {
        ValidateContext();
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Build requires an idle, stopped, fully compiled Editor.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            throw new InvalidOperationException("Active target must already be StandaloneWindows64; targets are never switched here.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("A loaded scene is dirty; scenes are neither saved nor discarded.");
        if (!File.Exists(Path.Combine(Project, Scene))) throw new FileNotFoundException("TestBattle scene is missing.");
        if (Directory.Exists(result.output) || File.Exists(result.output))
            throw new IOException("The fixed Preview9 output already exists. It will not be overwritten or deleted.");
        if (!Directory.Exists(Path.GetDirectoryName(result.output))) throw new DirectoryNotFoundException("The build archive parent must already exist.");
        RejectReparseAncestors(result.output);
        result.projectDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
        var symbols = result.projectDefines.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
        if (symbols.Any(s => s == "TALESTACTICS_REVIEW" || s == "DEVELOPMENT_BUILD" || s == "DEBUG" || s == "UNITY_INCLUDE_TESTS"))
            throw new InvalidOperationException("Project-level REVIEW/development/debug/test symbols violate normal-build isolation; none were changed.");
    }

    static void Execute(Result result)
    {
        var logs = new List<EditorLog>();
        Application.LogCallback log = (condition, trace, type) =>
        {
            if (type != LogType.Warning && type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            lock (logs) logs.Add(new EditorLog { utc = DateTime.UtcNow.ToString("o"), type = type.ToString(), condition = condition, stackTrace = trace });
        };
        try
        {
            ValidateReady(result);
            string currentHead = Git("rev-parse HEAD").Trim(), currentStatus = Git(CleanPaths);
            RequireSource(currentHead);
            if (currentHead != result.sourceHeadBefore) throw new InvalidOperationException("Source HEAD changed after reservation.");
            if (Hash(Source) != result.scriptSHA256) throw new InvalidOperationException("Build helper source changed after reservation.");
            result.protectedBefore = SnapshotTracked();
            result.sourceStatusBefore = currentStatus;
            result.sourceCleanBefore = currentStatus.Length == 0;
            result.semanticAuditBefore = AuditSemanticSource(result, "before");
            result.semanticSourceMatchesBefore = result.semanticAuditBefore.All(a => a.matches);
            if (!result.semanticSourceMatchesBefore) throw new InvalidOperationException("Source semantic audit failed before build.");
            result.runtimeShaderPreflight = CheckRuntimeShaders();
            WriteNew(Path.Combine(result.evidence, "runtime-shader-preflight.json"), result.runtimeShaderPreflight);
            if (result.runtimeShaderPreflight.Any(r => !r.matches)) throw new InvalidOperationException("Required imported URP runtime shader reference is missing or incorrect.");
            // No evidence files are placed in this directory, only Unity's build output.
            Directory.CreateDirectory(result.output); result.outputCreated = true;
            result.status = "Building"; result.buildStarted = true;
            WriteNew(Path.Combine(result.evidence, "running.json"), result);
            Application.logMessageReceivedThreaded += log;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Scene }, locationPathName = Path.Combine(result.output, "TalesTactics.exe"),
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None,
                extraScriptingDefines = Array.Empty<string>()
            });
            if (report == null) throw new InvalidOperationException("BuildPipeline returned no BuildReport.");
            CaptureReport(result, report);
            // Preserve the direct report before validating packaging and source invariants.
            WriteNew(Path.Combine(result.evidence, "build-report.json"), new
            {
                result.runId, result.unityVersion, result.sourceHeadBefore, result.summaryResult, result.summaryOutputPath,
                result.buildGuid, result.platform, result.platformGroup, result.buildStartedAt, result.buildEndedAt,
                result.summaryOptions, result.totalTimeTicks, result.totalTimeSeconds, result.totalSize,
                result.totalErrors, result.totalWarnings, result.summaryProperties, result.steps, result.files
            });
            if (report.summary.result != BuildResult.Succeeded || result.totalErrors != 0)
                throw new InvalidOperationException("BuildReport did not report Succeeded with zero errors: " + result.summaryResult);
            if (report.summary.platform != BuildTarget.StandaloneWindows64 || report.summary.options != BuildOptions.None)
                throw new InvalidOperationException("BuildReport platform/options do not match the normal Windows release request.");
            if (result.summaryProperties.Any(p => !string.IsNullOrEmpty(p.error)))
                throw new InvalidOperationException("A BuildSummary property could not be captured; see the raw report.");
            if (result.files.Length == 0) throw new InvalidOperationException("The actual BuildReport.GetFiles inventory is empty.");
            if (!SamePath(result.summaryOutputPath, Path.Combine(Output, "TalesTactics.exe")))
                throw new InvalidOperationException("BuildReport output does not match the approved executable.");
            result.executable = ReadArtifact(Path.Combine(Output, "TalesTactics.exe"));
            result.runtime = ReadArtifact(Path.Combine(Output, "TalesTactics_Data/Managed/TalesTactics.Runtime.dll"));
            result.status = "Built";
        }
        catch (Exception error) { result.status = "Failed"; result.exception = error.ToString(); }
        finally
        {
            Application.logMessageReceivedThreaded -= log;
            lock (logs) result.observedEditorWarningsAndErrors = logs.ToArray();
            try
            {
                result.sourceHeadAfter = Git("rev-parse HEAD").Trim();
                result.sourceStatusAfter = Git(CleanPaths);
                result.sourceCleanAfter = result.sourceStatusAfter.Length == 0;
                result.sourceHeadUnchanged = result.sourceHeadAfter == result.sourceHeadBefore && result.sourceHeadAfter == ExpectedHead;
                result.protectedAfter = SnapshotTracked();
                result.protectedFilesUnchanged = SameSnapshot(result.protectedBefore, result.protectedAfter);
                result.semanticAuditAfter = AuditSemanticSource(result, "after");
                result.semanticSourceMatchesAfter = result.semanticAuditAfter.All(a => a.matches);
                WriteNew(Path.Combine(result.evidence, "source-byte-changes.json"), result.protectedAfter.Where(a => !result.protectedBefore.Any(b => a.path == b.path && a.sha256 == b.sha256 && a.bytes == b.bytes)).ToArray());
                if (!result.sourceHeadUnchanged || !result.semanticSourceMatchesAfter)
                    throw new InvalidOperationException("Git HEAD or semantic source audit changed. Raw byte/clean flags are preserved; nothing was restored.");
                if (Hash(Source) != result.scriptSHA256) throw new InvalidOperationException("Build helper source changed during the run.");
            }
            catch (Exception error) { result.status = "Failed"; result.finalizationException = error.ToString(); }
            Complete(result);
        }
    }

    static void CaptureReport(Result result, BuildReport report)
    {
        var summary = report.summary;
        result.summaryResult = summary.result.ToString(); result.summaryOutputPath = summary.outputPath;
        result.buildGuid = summary.guid.ToString(); result.platform = summary.platform.ToString(); result.platformGroup = summary.platformGroup.ToString();
        result.buildStartedAt = summary.buildStartedAt.ToUniversalTime().ToString("o"); result.buildEndedAt = summary.buildEndedAt.ToUniversalTime().ToString("o");
        result.summaryOptions = summary.options.ToString(); result.totalTimeTicks = summary.totalTime.Ticks; result.totalTimeSeconds = summary.totalTime.TotalSeconds;
        result.totalSize = summary.totalSize; result.totalErrors = summary.totalErrors; result.totalWarnings = summary.totalWarnings;
        result.summaryProperties = typeof(BuildSummary).GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(p => p.Name).Select(p => ReadProperty(p, summary)).ToArray();
        result.steps = report.steps.Select((step, index) => new Step
        {
            index = index, depth = step.depth, name = step.name, durationTicks = step.duration.Ticks, durationSeconds = step.duration.TotalSeconds,
            messages = step.messages.Select((message, n) => new Message
            { stepIndex = index, messageIndex = n, step = step.name, type = message.type.ToString(), content = message.content }).ToArray()
        }).ToArray();
        result.files = report.GetFiles().Select(file => new ReportFile { path = file.path, role = file.role, sizeBytes = file.size }).ToArray();
        result.warningErrorMessages = result.steps.SelectMany(s => s.messages).Where(m => m.type == "Warning" || m.type == "Error" || m.type == "Exception" || m.type == "Assert").ToArray();
    }

    static void Complete(Result result)
    {
        try
        {
            result.complete = true; result.finishedUtc = DateTime.UtcNow.ToString("o");
            result.passed = result.status == "Built" && result.semanticSourceMatchesBefore && result.semanticSourceMatchesAfter && result.sourceHeadUnchanged
                && string.IsNullOrEmpty(result.exception) && string.IsNullOrEmpty(result.finalizationException);
            // Emit the legacy packer envelope only after all build/source checks pass.
            // Its data.result is a JSON string derived solely from the same BuildReport.
            if (result.passed)
            {
                try
                {
                    WriteNew(Path.Combine(result.evidence, "build-hashes.json"), new[]
                    {
                        new { Algorithm = "SHA256", Hash = result.executable.sha256, Path = result.executable.path },
                        new { Algorithm = "SHA256", Hash = result.runtime.sha256, Path = result.runtime.path }
                    });
                    var payload = new
                    {
                        result = result.summaryResult, platform = result.platform, outputPath = result.summaryOutputPath,
                        result.totalErrors, result.totalWarnings, durationSeconds = result.totalTimeSeconds,
                        messages = result.steps.SelectMany(s => s.messages).Select(m => new { m.type, message = m.content }).ToArray(),
                        result.files
                    };
                    WriteNew(Path.Combine(result.evidence, "windows-build.json"), new
                    { data = new { result = Newtonsoft.Json.JsonConvert.SerializeObject(payload, Newtonsoft.Json.Formatting.None) } });
                    result.status = "Succeeded";
                }
                catch (Exception error)
                { result.passed = false; result.status = "Failed"; result.finalizationException = error.ToString(); }
            }
            // This is the authoritative completion marker, written after the evidence set.
            WriteNew(Path.Combine(result.evidence, "build-result.json"), result);
        }
        finally
        {
            if (SessionState.GetString(ActiveKey, "") == result.runId) SessionState.EraseString(ActiveKey);
        }
    }

    static void RequireSource(string head)
    {
        if (head != ExpectedHead) throw new InvalidOperationException("Expected source HEAD " + ExpectedHead + ", found " + head);
        if (Git("ls-files --others --exclude-standard -z -- Assets Packages ProjectSettings").Length != 0)
            throw new InvalidOperationException("Untracked product files are not allowed.");
    }
    static SemanticFile[] AuditSemanticSource(Result result, string phase)
    {
        RequireSource(Git("rev-parse HEAD").Trim());
        // Git compares every tracked path to HEAD; its normal EOL conversion is allowed.
        // Capture all known serializer inputs even when Git considers their normalized bytes equal.
        var paths = Git("diff --no-ext-diff --no-textconv --name-only -z HEAD --").Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries).Concat(new[]
        {
            "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset", "Assets/Settings/PC_RPAsset.asset",
            "Assets/TalesTactics/Resources/TalesTactics/KoreanFallback.asset", "ProjectSettings/GraphicsSettings.asset",
            "ProjectSettings/ProjectAuditorSettings.asset", "ProjectSettings/ShaderGraphSettings.asset", "ProjectSettings/UnityConnectSettings.asset",
            "Assets/TalesTactics/Resources/TalesTactics/Ornate/CommonIcons.png.meta", "Assets/TalesTactics/Resources/TalesTactics/Ornate/CompactIcons.png.meta",
            "Assets/TalesTactics/Resources/TalesTactics/Ornate/FieldIcons.png.meta", "Assets/TalesTactics/Resources/TalesTactics/Ornate/Ground.png.meta",
            "Assets/TalesTactics/Resources/TalesTactics/Ornate/Panel.png.meta", "Assets/TalesTactics/Resources/TalesTactics/Ornate/Trees.png.meta"
        }).Distinct(StringComparer.Ordinal).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        var rows = new List<SemanticFile>();
        foreach (string path in paths)
        {
            if (path.Contains("\"") || path.Contains("\n") || path.Contains("\r")) throw new IOException("Unsupported tracked path.");
            string full = Path.GetFullPath(Path.Combine(Project, path));
            if (!full.StartsWith(Path.GetFullPath(Project) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("Source path escapes worktree.");
            byte[] head = HeadBytes(path), current = File.ReadAllBytes(full);
            string folder = Path.Combine(result.evidence, "source-versions", phase, path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(folder));
            File.WriteAllBytes(folder + ".head", head); File.WriteAllBytes(folder + ".worktree", current);
            var row = new SemanticFile { path = path, headSHA256 = HashBytes(head), currentSHA256 = HashBytes(current), headEvidence = folder + ".head", currentEvidence = folder + ".worktree" };
            row.matches = head.SequenceEqual(current); row.classification = row.matches ? "Identical" : "Rejected";
            if (!row.matches)
            {
                try
                {
                    string a = NormalizeText(head), b = NormalizeText(current);
                    if (a == b) { row.matches = true; row.classification = "EOLOrTrailingWhitespaceOnly"; }
                    else if (path == "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset" && RuntimeListOnly(a, b))
                    { row.matches = true; row.classification = "DerivedRuntimeSettingsClearedByEditorSerialization"; }
                    else if (path == "ProjectSettings/UnityConnectSettings.asset" && KnownDisabledMigration(a) == b)
                    { row.matches = true; row.classification = "KnownDisabledUnityConnectSchemaMigration"; }
                }
                catch (DecoderFallbackException) { }
            }
            rows.Add(row);
        }
        WriteNew(Path.Combine(result.evidence, "semantic-source-" + phase + ".json"), rows);
        return rows.ToArray();
    }
    static string NormalizeText(byte[] bytes)
    {
        string value = new UTF8Encoding(false, true).GetString(bytes).Replace("\r\n", "\n");
        if (value.IndexOf('\0') >= 0) throw new DecoderFallbackException("Binary input cannot be whitespace-normalized.");
        return System.Text.RegularExpressions.Regex.Replace(value, @"(?m)[ \t]+$", "");
    }
    static bool RuntimeListOnly(string head, string current)
    {
        const string pattern = @"(?m)^    m_RuntimeSettings:\n      m_List:(?: \[\]\n|\n(?:      - rid: [0-9]+\n)+)";
        var matches = System.Text.RegularExpressions.Regex.Matches(head, pattern);
        var now = System.Text.RegularExpressions.Regex.Matches(current, pattern);
        if (matches.Count != 1 || now.Count != 1 || System.Text.RegularExpressions.Regex.Matches(matches[0].Value, "rid:").Count != 16) return false;
        const string cleared = "    m_RuntimeSettings:\n      m_List: []\n";
        return now[0].Value == cleared && head.Replace(matches[0].Value, cleared) == current;
    }
    static string KnownDisabledMigration(string head)
    {
        return head.Replace("  m_TestInitMode: 0\n", "  m_DashboardUrl: https://dashboard.unity3d.com\n  m_TestInitMode: 0\n  InsightsSettings:\n    m_EventUrl: https://engine-data.unity3d.com\n    m_StackTraceUrl: https://engine-data.services.api.unity.com/v1/stack-traces\n    m_EngineDiagnosticsEnabled: 0\n    m_Enabled: 0\n")
            .Replace("  CrashReportingSettings:\n    m_EventUrl: https://perf-events.cloud.unity3d.com\n    m_Enabled: 0\n", "  CrashReportingSettings:\n    serializedVersion: 2\n    m_EventUrl: https://perf-events.cloud.unity3d.com\n    m_EnableCloudDiagnosticsReporting: 0\n")
            .Replace("  UnityAnalyticsSettings:\n    m_Enabled: 0\n    m_TestMode: 0\n    m_InitializeOnStartup: 1\n", "  UnityAnalyticsSettings:\n    m_Enabled: 0\n    m_TestMode: 0\n    m_InitializeOnStartup: 1\n    m_PackageRequiringCoreStatsPresent: 0\n");
    }
    static byte[] HeadBytes(string path)
    {
        var start = new ProcessStartInfo { FileName = "git", Arguments = "-C \"" + Project + "\" cat-file blob \"HEAD:" + path + "\"", WorkingDirectory = Project,
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string key in new[] { "GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE", "GIT_COMMON_DIR" }) start.EnvironmentVariables.Remove(key);
        using (var process = Process.Start(start)) using (var memory = new MemoryStream())
        {
            var copy = process.StandardOutput.BaseStream.CopyToAsync(memory); var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(); throw new TimeoutException("Read-only HEAD blob read timed out."); }
            copy.GetAwaiter().GetResult();
            if (process.ExitCode != 0) throw new IOException(error.GetAwaiter().GetResult());
            return memory.ToArray();
        }
    }
    static string HashBytes(byte[] bytes)
    { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    static ResourceCheck[] CheckRuntimeShaders()
    {
        var get = typeof(UnityEngine.Rendering.GraphicsSettings).GetMethods(BindingFlags.Public | BindingFlags.Static).Single(m => m.Name == "GetRenderPipelineSettings" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
        string[] types = { "UnityEngine.Rendering.Universal.UniversalRenderPipelineRuntimeShaders", "UnityEngine.Rendering.Universal.UniversalRenderPipelineRuntimeShaders", "UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtilsResources" };
        string[] properties = { "coreBlitPS", "coreBlitColorAndDepthPS", "coreCopyPS" };
        string[] names = { "Hidden/Universal/CoreBlit", "Hidden/Universal/CoreBlitColorAndDepth", "Hidden/CoreSRP/CoreCopy" };
        string[] guids = { "93446b5c5339d4f00b85c159e1159b7c", "d104b2fc1ca6445babb8e90b0758136b", "12dc59547ea167a4ab435097dd0f9add" };
        return Enumerable.Range(0, 3).Select(i =>
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(types[i])).FirstOrDefault(t => t != null);
            object settings = type == null ? null : get.MakeGenericMethod(type).Invoke(null, null);
            var shader = settings == null ? null : type.GetProperty(properties[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(settings) as Shader;
            string path = shader == null ? null : AssetDatabase.GetAssetPath(shader), guid = string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
            return new ResourceCheck { type = types[i], property = properties[i], shaderName = shader == null ? null : shader.name, assetPath = path, guid = guid, matches = shader != null && shader.name == names[i] && guid == guids[i] };
        }).ToArray();
    }
    static ProtectedFile[] SnapshotTracked()
    {
        var checkedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var paths = Git("ls-files -z").Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        if (paths.Length == 0) throw new InvalidOperationException("Tracked source inventory is empty.");
        return paths.Select(relative =>
        {
            string full = Path.GetFullPath(Path.Combine(Project, relative));
            if (!full.StartsWith(Path.GetFullPath(Project).TrimEnd('\\', '/') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Tracked path escapes the release worktree: " + relative);
            RejectReparseAncestors(full, checkedPaths);
            return new ProtectedFile { path = relative.Replace('\\', '/'), bytes = new FileInfo(full).Length, sha256 = Hash(full) };
        }).ToArray();
    }
    static string Git(string arguments)
    {
        var start = new ProcessStartInfo
        {
            FileName = "git", Arguments = "-C \"" + Project + "\" " + arguments, WorkingDirectory = Project,
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Utf8, StandardErrorEncoding = Utf8
        };
        // A caller's repository overrides must not redirect the fixed-worktree checks.
        foreach (string key in new[] { "GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE", "GIT_COMMON_DIR" }) start.EnvironmentVariables.Remove(key);
        using (var process = Process.Start(start))
        {
            if (process == null) throw new InvalidOperationException("Git could not be started.");
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000))
            {
                process.Kill();
                throw new TimeoutException("The helper's own read-only Git command timed out: " + arguments);
            }
            string output = stdout.GetAwaiter().GetResult(), error = stderr.GetAwaiter().GetResult();
            if (process.ExitCode != 0) throw new InvalidOperationException("Git check failed (" + arguments + "): " + error);
            return output;
        }
    }
    static Property ReadProperty(PropertyInfo property, BuildSummary summary)
    {
        var row = new Property { name = property.Name, type = property.PropertyType.FullName };
        try
        {
            object value = property.GetValue(summary, null);
            row.value = value is DateTime ? ((DateTime)value).ToString("o", CultureInfo.InvariantCulture) :
                value is TimeSpan ? ((TimeSpan)value).Ticks.ToString(CultureInfo.InvariantCulture) :
                value is double ? ((double)value).ToString("R", CultureInfo.InvariantCulture) :
                value is float ? ((float)value).ToString("R", CultureInfo.InvariantCulture) : Convert.ToString(value, CultureInfo.InvariantCulture);
        }
        catch (Exception error) { row.error = error.ToString(); }
        return row;
    }
    static Artifact ReadArtifact(string path)
    { return new Artifact { path = Path.GetFullPath(path), bytes = new FileInfo(path).Length, sha256 = Hash(path) }; }
    static bool SameSnapshot(ProtectedFile[] a, ProtectedFile[] b)
    { return a.Select(p => p.path + ":" + p.bytes + ":" + p.sha256).SequenceEqual(b.Select(p => p.path + ":" + p.bytes + ":" + p.sha256)); }
    static string Hash(string path)
    { using (var sha = SHA256.Create()) using (var input = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant(); }
    static bool SamePath(string a, string b)
    { return string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'), Path.GetFullPath(b).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase); }
    static void RejectReparseAncestors(string path, HashSet<string> checkedPaths = null)
    {
        string current = Path.GetFullPath(path);
        while (!string.IsNullOrEmpty(current))
        {
            if (checkedPaths != null && checkedPaths.Contains(current)) break;
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Reparse points are not allowed in build/source/evidence paths: " + current);
            if (checkedPaths != null) checkedPaths.Add(current);
            current = Path.GetDirectoryName(current);
        }
    }
    static void WriteNew(string path, object value)
    {
        if (File.Exists(path) || Directory.Exists(path)) throw new IOException("Evidence already exists: " + path);
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temp, Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented), Utf8);
        File.Move(temp, path);
    }
}
