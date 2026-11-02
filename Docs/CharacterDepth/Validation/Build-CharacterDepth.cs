using System;
using System.Collections.Generic;
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

// Ephemeral Pipeline run_script source. Keep outside Assets; do not import it.
// Entry: CharacterDepthBuild.Run, args: [one of the two exact output directories].
// Run returns a SCHEDULED receipt; only the later build-result.json is a verdict.
// It does not call any project/package/settings setters, SaveAssets or Refresh.
public static class CharacterDepthBuild
{
    const string Project = "C:/dev/TalesTactics";
    const string Normal = "D:/TalesTacticsBuildArchive/CharacterDepthWindows20261102Final";
    const string Review = "D:/TalesTacticsBuildArchive/CharacterDepthReview20261102";
    const string Scene = "Assets/TalesTactics/Scenes/TestBattle.unity";
    const string LogRoot = "Logs/CharacterDepth20261102";
    const string Source = "Docs/CharacterDepth/Validation/Build-CharacterDepth.cs";
    const string ActiveKey = "TalesTactics.CharacterDepthBuild20261102.Active";
    static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

    [Serializable] public sealed class Receipt
    { public string status, runId, output, evidence, finalResult; public bool buildCompleted; }
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
    [Serializable] public sealed class Result
    {
        public int schema = 1;
        public string runId, status, startedUtc, finishedUtc, output, evidence, script, scriptSHA256, unityVersion;
        public bool complete, passed, buildStarted, outputCreated, review, protectedFilesUnchanged;
        public string scene = Scene, target = "StandaloneWindows64", options = "None", projectDefines;
        public string[] extraScriptingDefines;
        public string summaryResult, summaryOutputPath, buildGuid, platform, platformGroup;
        public string buildStartedAt, buildEndedAt, summaryOptions;
        public long totalTimeTicks;
        public double totalTimeSeconds;
        public ulong totalSize;
        public int totalErrors, totalWarnings;
        public Property[] summaryProperties = Array.Empty<Property>();
        public Step[] steps = Array.Empty<Step>();
        public Message[] warningErrorMessages = Array.Empty<Message>();
        public EditorLog[] observedEditorWarningsAndErrors = Array.Empty<EditorLog>();
        public ProtectedFile[] protectedBefore = Array.Empty<ProtectedFile>(), protectedAfter = Array.Empty<ProtectedFile>();
        public string exception, finalizationException;
        public string scope = "BuildReport.summary public-property values and every BuildReport step/message are recorded without truncation. Editor log warnings/errors observed during the build are additional evidence, not necessarily emitted by this build. No player, Unity tests, user save operation, asset saving, settings setter or package operation is performed by this helper.";
    }

    public static string Run(string outputDirectory)
    {
        // Checking the Editor context before writing prevents targeting another checkout.
        if (!SamePath(Directory.GetParent(Application.dataPath).FullName, Project))
            throw new InvalidOperationException("This script is only for the C:/dev/TalesTactics Editor.");
        var result = new Result
        {
            runId = Guid.NewGuid().ToString("N"), status = "Validating", startedUtc = DateTime.UtcNow.ToString("o"),
            output = outputDirectory, script = Path.GetFullPath(Path.Combine(Project, Source)), unityVersion = Application.unityVersion
        };
        result.evidence = Path.GetFullPath(Path.Combine(Project, LogRoot, "Build-" + result.runId));
        if (Directory.Exists(result.evidence)) throw new IOException("New build evidence directory already exists.");
        Directory.CreateDirectory(result.evidence);
        try
        {
            result.scriptSHA256 = Hash(result.script);
            if (string.IsNullOrWhiteSpace(outputDirectory) || !Path.IsPathRooted(outputDirectory))
                throw new ArgumentException("Use one of the two exact absolute CharacterDepth output directories.");
            result.output = Path.GetFullPath(outputDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!SamePath(result.output, Normal) && !SamePath(result.output, Review))
                throw new ArgumentException("Only CharacterDepthWindows20261102Final and CharacterDepthReview20261102 are allowed.");
            result.review = SamePath(result.output, Review);
            result.extraScriptingDefines = result.review ? new[] { "TALESTACTICS_REVIEW" } : Array.Empty<string>();
            ValidateReady(result);
            string active = SessionState.GetString(ActiveKey, "");
            if (!string.IsNullOrEmpty(active)) throw new InvalidOperationException("A CharacterDepth build is reserved: " + active);
            SessionState.SetString(ActiveKey, result.runId);
            result.status = "Scheduled";
            WriteNew(Path.Combine(result.evidence, "scheduled.json"), result);

            // Delay outside the Pipeline command dispatcher so its response is not held
            // for a synchronous BuildPlayer call. Detach before executing: exactly once.
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
            return JsonUtility.ToJson(new Receipt { status = "Scheduled", runId = result.runId, output = result.output,
                evidence = result.evidence, finalResult = Path.Combine(result.evidence, "build-result.json"), buildCompleted = false });
        }
        catch (Exception error)
        {
            result.status = "Rejected"; result.exception = error.ToString();
            Complete(result);
            throw;
        }
    }

    static void ValidateReady(Result result)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Build requires an idle, stopped, fully compiled Editor.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            throw new InvalidOperationException("Active target must already be StandaloneWindows64; this script does not switch targets.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Loaded scene is dirty; this script does not save or discard scenes.");
        if (!File.Exists(Path.Combine(Project, Scene))) throw new FileNotFoundException("TestBattle scene is missing.");
        if (Directory.Exists(result.output) || File.Exists(result.output))
            throw new IOException("Choose the unused approved directory; existing builds or files are never overwritten.");
        var archive = new DirectoryInfo(Path.GetDirectoryName(result.output));
        if (!archive.Exists || (archive.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new IOException("The direct build archive parent must exist and must not be a reparse point.");
        result.projectDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
        var symbols = result.projectDefines.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
        if (symbols.Any(s => s == "TALESTACTICS_REVIEW" || s == "DEVELOPMENT_BUILD" || s == "DEBUG"))
            throw new InvalidOperationException("Project-level REVIEW/development/debug symbols would violate build isolation. No symbols were changed.");
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
            result.protectedBefore = SnapshotProtected();
            Directory.CreateDirectory(result.output); result.outputCreated = true;
            result.status = "Building"; result.buildStarted = true;
            WriteNew(Path.Combine(result.evidence, "running.json"), result);
            Application.logMessageReceivedThreaded += log;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Scene }, locationPathName = Path.Combine(result.output, "TalesTactics.exe"),
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None,
                extraScriptingDefines = result.extraScriptingDefines
            });
            if (report == null) throw new InvalidOperationException("BuildPipeline returned no BuildReport.");
            var summary = report.summary;
            result.summaryResult = summary.result.ToString(); result.summaryOutputPath = summary.outputPath;
            result.buildGuid = summary.guid.ToString(); result.platform = summary.platform.ToString(); result.platformGroup = summary.platformGroup.ToString();
            result.buildStartedAt = summary.buildStartedAt.ToUniversalTime().ToString("o"); result.buildEndedAt = summary.buildEndedAt.ToUniversalTime().ToString("o");
            result.summaryOptions = summary.options.ToString(); result.totalTimeTicks = summary.totalTime.Ticks; result.totalTimeSeconds = summary.totalTime.TotalSeconds;
            result.totalSize = summary.totalSize; result.totalErrors = summary.totalErrors; result.totalWarnings = summary.totalWarnings;
            result.summaryProperties = typeof(BuildSummary).GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(p => p.Name).Select(p => ReadProperty(p, summary)).ToArray();
            result.steps = report.steps.Select((step, index) => new Step { index = index, depth = step.depth, name = step.name,
                durationTicks = step.duration.Ticks, durationSeconds = step.duration.TotalSeconds,
                messages = step.messages.Select((message, n) => new Message { stepIndex = index, messageIndex = n, step = step.name,
                    type = message.type.ToString(), content = message.content }).ToArray() }).ToArray();
            result.warningErrorMessages = result.steps.SelectMany(s => s.messages).Where(m => m.type == "Warning" || m.type == "Error" || m.type == "Exception" || m.type == "Assert").ToArray();
            if (summary.result != BuildResult.Succeeded || summary.totalErrors != 0)
                throw new InvalidOperationException("BuildReport did not report Succeeded with zero errors: " + summary.result);
            if (result.summaryProperties.Any(p => !string.IsNullOrEmpty(p.error)))
                throw new InvalidOperationException("One or more BuildSummary properties could not be captured; see summaryProperties.");
            result.status = "Built";
        }
        catch (Exception error) { result.status = "Failed"; result.exception = error.ToString(); }
        finally
        {
            Application.logMessageReceivedThreaded -= log;
            lock (logs) result.observedEditorWarningsAndErrors = logs.ToArray();
            try
            {
                result.protectedAfter = SnapshotProtected();
                result.protectedFilesUnchanged = SameSnapshot(result.protectedBefore, result.protectedAfter);
                if (!result.protectedFilesUnchanged && result.buildStarted)
                {
                    result.status = "Failed";
                    result.finalizationException = "Packages, ProjectSettings or Content bytes changed during the build. This helper did not restore them.";
                }
                if (Hash(result.script) != result.scriptSHA256)
                { result.status = "Failed"; result.finalizationException = "Build helper source changed during the run."; }
            }
            catch (Exception error) { result.status = "Failed"; result.finalizationException = error.ToString(); }
            Complete(result);
        }
    }

    static void Complete(Result result)
    {
        try
        {
            result.complete = true; result.finishedUtc = DateTime.UtcNow.ToString("o");
            result.passed = result.status == "Built" && result.protectedFilesUnchanged && string.IsNullOrEmpty(result.exception) && string.IsNullOrEmpty(result.finalizationException);
            if (result.passed) result.status = "Succeeded";
            WriteNew(Path.Combine(result.evidence, "build-result.json"), result);
            if (result.outputCreated) WriteNew(Path.Combine(result.output, "build-result.json"), result);
        }
        finally
        {
            if (SessionState.GetString(ActiveKey, "") == result.runId) SessionState.EraseString(ActiveKey);
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
    static ProtectedFile[] SnapshotProtected()
    {
        return new[] { "Packages", "ProjectSettings", "Assets/TalesTactics/Content" }
            .SelectMany(folder => Directory.GetFiles(Path.Combine(Project, folder), "*", SearchOption.AllDirectories))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new ProtectedFile { path = path.Substring(Path.GetFullPath(Project).Length + 1).Replace('\\', '/'),
                bytes = new FileInfo(path).Length, sha256 = Hash(path) }).ToArray();
    }
    static bool SameSnapshot(ProtectedFile[] a, ProtectedFile[] b)
    { return a.Select(p => p.path + ":" + p.bytes + ":" + p.sha256).SequenceEqual(b.Select(p => p.path + ":" + p.bytes + ":" + p.sha256)); }
    static string Hash(string path)
    { using (var sha = SHA256.Create()) using (var input = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant(); }
    static bool SamePath(string a, string b)
    { return string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'), Path.GetFullPath(b).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase); }
    static void WriteNew(string path, object value)
    {
        if (File.Exists(path)) throw new IOException("Evidence already exists: " + path);
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temp, Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented), Utf8);
        File.Move(temp, path);
    }
}
