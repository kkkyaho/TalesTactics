// Read-only recovery of a completed CharacterDepth BuildReport. Run before another build.
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CharacterDepthBuildReportCapture
{
    const string Project = "C:/dev/TalesTactics";
    const string EvidenceRoot = Project + "/Logs/CharacterDepth20261102";
    const string Normal = "D:/TalesTacticsBuildArchive/CharacterDepthWindows20261102/TalesTactics.exe";
    const string Review = "D:/TalesTacticsBuildArchive/CharacterDepthReview20261102/TalesTactics.exe";

    public static string Run(string expectedOutputPath, string evidenceDirectory)
    {
        if (!SamePath(Path.GetDirectoryName(Application.dataPath), Project))
            throw new InvalidOperationException("Unexpected project.");
        if (!SamePath(expectedOutputPath, Normal) && !SamePath(expectedOutputPath, Review))
            throw new ArgumentException("Expected output must be an exact CharacterDepth normal or review executable path.");
        string evidence = Path.GetFullPath(evidenceDirectory).TrimEnd('\\', '/');
        string name = Path.GetFileName(evidence);
        Guid run;
        if (!SamePath(Path.GetDirectoryName(evidence), EvidenceRoot) || !name.StartsWith("Build-", StringComparison.Ordinal) ||
            !Guid.TryParseExact(name.Substring(6), "N", out run) || !Directory.Exists(evidence))
            throw new ArgumentException("Evidence must be an existing task-owned Build-<guid> directory.");
        if (BuildPipeline.isBuildingPlayer) throw new InvalidOperationException("Wait until the active build has finished.");
        string originalPath = Path.Combine(evidence, "build-result.json");
        JObject original = JObject.Parse(File.ReadAllText(originalPath));
        if ((bool?)original["complete"] != true || (string)original["runId"] != run.ToString("N") ||
            !SamePath((string)original["output"], Path.GetDirectoryName(expectedOutputPath)) ||
            !SamePath((string)original["evidence"], evidence))
            throw new InvalidOperationException("Completed build evidence does not match the requested run/output.");
        BuildReport report = BuildReport.GetLatestReport();
        if (report == null) throw new InvalidOperationException("No latest BuildReport is available.");
        BuildSummary summary = report.summary;
        if (!SamePath(summary.outputPath, expectedOutputPath) || string.IsNullOrEmpty((string)original["buildGuid"]) ||
            !string.Equals(summary.guid.ToString(), (string)original["buildGuid"], StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Latest BuildReport output or GUID differs from the completed requested build.");
        var properties = typeof(BuildSummary).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => p.Name).Select(p => ReadProperty(p, summary)).ToArray();
        var steps = report.steps.Select((step, index) => new
        {
            index, step.depth, step.name, durationTicks = step.duration.Ticks, durationSeconds = step.duration.TotalSeconds,
            messages = step.messages.Select((message, messageIndex) => new
            { stepIndex = index, messageIndex, step = step.name, type = message.type.ToString(), message.content }).ToArray()
        }).ToArray();
        var warnings = steps.SelectMany(s => s.messages).Where(m =>
            m.type == "Warning" || m.type == "Error" || m.type == "Exception" || m.type == "Assert").ToArray();
        var document = new
        {
            schema = 1, kind = "character-depth-completed-build-report-capture", capturedUtc = DateTime.UtcNow.ToString("o"),
            runId = run.ToString("N"), expectedOutputPath = Path.GetFullPath(expectedOutputPath), evidence,
            originalBuildResultPath = originalPath, originalBuildResultSHA256 = Hash(originalPath),
            buildGuid = summary.guid.ToString(), summaryOutputPath = summary.outputPath, summaryResult = summary.result.ToString(),
            platform = summary.platform.ToString(), platformGroup = summary.platformGroup.ToString(), summaryOptions = summary.options.ToString(),
            buildStartedAt = summary.buildStartedAt.ToUniversalTime().ToString("o"), buildEndedAt = summary.buildEndedAt.ToUniversalTime().ToString("o"),
            totalTimeTicks = summary.totalTime.Ticks, totalTimeSeconds = summary.totalTime.TotalSeconds,
            summary.totalSize, summary.totalErrors, summary.totalWarnings, summaryProperties = properties, steps,
            warningErrorMessages = warnings, stepCount = steps.Length, messageCount = steps.Sum(s => s.messages.Length),
            summaryPropertyCaptureComplete = properties.All(p => string.IsNullOrEmpty((string)p["error"])),
            limitations = new[] { "Captures the latest completed BuildReport only; does not rerun or modify the build.",
                "Cannot reconstruct omitted original protected-file snapshot arrays or independently observed Editor log arrays." }
        };
        string json = JsonConvert.SerializeObject(document, Formatting.Indented);
        JObject verify = JObject.Parse(json);
        if (((JArray)verify["steps"]).Count != steps.Length || ((JArray)verify["summaryProperties"]).Count != properties.Length ||
            ((JArray)verify["warningErrorMessages"]).Count != warnings.Length ||
            verify["steps"].Sum(s => ((JArray)s["messages"]).Count) != steps.Sum(s => s.messages.Length))
            throw new InvalidOperationException("JSON round-trip omitted report arrays.");
        string capturePath = Path.Combine(evidence, "captured-build-report-" + Guid.NewGuid().ToString("N") + ".json");
        using (var stream = new FileStream(capturePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(json);
        return JsonConvert.SerializeObject(new { capturePath, sha256 = Hash(capturePath), buildGuid = summary.guid.ToString(),
            result = summary.result.ToString(), summary.totalErrors, summary.totalWarnings, steps = steps.Length,
            messages = steps.Sum(s => s.messages.Length), capturedWarningErrorMessages = warnings.Length });
    }

    static JObject ReadProperty(PropertyInfo property, BuildSummary summary)
    {
        var row = new JObject { ["name"] = property.Name, ["type"] = property.PropertyType.FullName };
        try
        {
            object value = property.GetValue(summary, null);
            row["value"] = value is DateTime ? ((DateTime)value).ToString("o", CultureInfo.InvariantCulture) :
                value is TimeSpan ? ((TimeSpan)value).Ticks.ToString(CultureInfo.InvariantCulture) :
                value is double ? ((double)value).ToString("R", CultureInfo.InvariantCulture) :
                value is float ? ((float)value).ToString("R", CultureInfo.InvariantCulture) : Convert.ToString(value, CultureInfo.InvariantCulture);
        }
        catch (Exception error) { row["error"] = error.ToString(); }
        return row;
    }
    static bool SamePath(string a, string b)
    { return !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) && string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'), Path.GetFullPath(b).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase); }
    static string Hash(string path)
    { using (var sha = SHA256.Create()) using (var input = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant(); }
}
