using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TalesTactics.Editor
{
    // Build-only overrides. Capture the whole importer and exact meta bytes so a user's
    // sprite slicing, userData, platform settings and uncommitted edits survive restoration.
    public sealed class AndroidTextureSettingsScope : IDisposable
    {
        const string AssetRoot = "Assets/TalesTactics";
        const string Platform = "Android";
        const string RecoveryFile = "Library/TalesTactics/AndroidTextureSettingsRecovery.json";
        [Serializable] sealed class Snapshot
        {
            public string path, importerJson, metaBase64;
        }
        [Serializable] sealed class Journal
        {
            public string projectAssets;
            public List<Snapshot> textures = new List<Snapshot>();
        }
        readonly Journal journal;
        bool disposed;
        public int AppliedCount { get; private set; }
        public int PreservedOverrideCount { get; private set; }

        public AndroidTextureSettingsScope()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Android texture overrides require an Android validation checkout.");
            if (File.Exists(RecoveryFile))
                throw new InvalidOperationException("An interrupted Android texture scope needs restoration first. Run TalesTactics/Build/Restore Interrupted Android Texture Settings.");
            journal = new Journal { projectAssets = Path.GetFullPath(Application.dataPath) };
            // Complete the snapshots and recovery journal before changing the first importer.
            foreach (string path in AssetDatabase.FindAssets("t:Texture2D", new[] { AssetRoot })
                .Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(path => path, StringComparer.Ordinal))
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue; // Embedded font atlases and other non-texture importers.
                if (importer.GetPlatformTextureSettings(Platform).overridden)
                { PreservedOverrideCount++; continue; }
                journal.textures.Add(new Snapshot { path = path, importerJson = EditorJsonUtility.ToJson(importer),
                    metaBase64 = Convert.ToBase64String(File.ReadAllBytes(path + ".meta")) });
            }
            Directory.CreateDirectory(Path.GetDirectoryName(RecoveryFile));
            string temporaryJournal = RecoveryFile + ".tmp";
            try
            {
                File.WriteAllText(temporaryJournal, JsonUtility.ToJson(journal), new UTF8Encoding(false));
                File.Move(temporaryJournal, RecoveryFile);
            }
            finally { if (File.Exists(temporaryJournal)) File.Delete(temporaryJournal); }
            try
            {
                foreach (var snapshot in journal.textures)
                {
                    var importer = RequireImporter(snapshot.path);
                    // Copy every default parameter, including size and resize algorithm. Only
                    // Android compression changes; desktop/default import settings stay intact.
                    var settings = importer.GetDefaultPlatformTextureSettings();
                    settings.name = Platform;
                    settings.overridden = true;
                    settings.format = TextureImporterFormat.ASTC_6x6;
                    settings.textureCompression = TextureImporterCompression.Compressed;
                    settings.crunchedCompression = false;
                    importer.SetPlatformTextureSettings(settings);
                    importer.SaveAndReimport();
                    var actual = RequireImporter(snapshot.path).GetPlatformTextureSettings(Platform);
                    if (!actual.overridden || actual.format != TextureImporterFormat.ASTC_6x6 ||
                        actual.maxTextureSize != settings.maxTextureSize)
                        throw new InvalidOperationException("Android texture override did not survive import: " + snapshot.path);
                    AppliedCount++;
                }
            }
            catch (Exception applyError)
            {
                var failures = Restore(journal);
                disposed = true;
                if (failures.Count == 0) File.Delete(RecoveryFile);
                else
                {
                    failures.Insert(0, applyError);
                    throw new AggregateException("Android texture preparation failed; some original settings still need recovery. Keep " + RecoveryFile, failures);
                }
                throw;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            var failures = Restore(journal);
            if (failures.Count > 0)
                throw new AggregateException("Could not restore all Android texture settings. Keep " + RecoveryFile, failures);
            File.Delete(RecoveryFile);
        }

        // The journal also survives an Editor crash/process termination, where Dispose cannot run.
        // Recovery is explicit so restarting the Editor cannot silently overwrite later user edits.
        [MenuItem("TalesTactics/Build/Restore Interrupted Android Texture Settings")]
        public static void RestorePending()
        {
            if (!File.Exists(RecoveryFile)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Stop Play mode and the current build before restoring texture settings.");
            var pending = JsonUtility.FromJson<Journal>(File.ReadAllText(RecoveryFile));
            if (pending == null || pending.textures == null ||
                !string.Equals(pending.projectAssets, Path.GetFullPath(Application.dataPath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Texture recovery journal does not belong to this project.");
            var failures = Restore(pending);
            if (failures.Count > 0)
                throw new AggregateException("Texture restoration remains incomplete. Keep " + RecoveryFile, failures);
            File.Delete(RecoveryFile);
        }

        static List<Exception> Restore(Journal saved)
        {
            var failures = new List<Exception>();
            // Do not stop at one failed import: every independently recoverable texture is restored.
            for (int index = saved.textures.Count - 1; index >= 0; index--)
            {
                var snapshot = saved.textures[index];
                try { RestoreOne(snapshot); }
                catch (Exception exception)
                { failures.Add(new InvalidOperationException("Texture restoration failed: " + snapshot?.path, exception)); }
            }
            return failures;
        }
        static void RestoreOne(Snapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.path) ||
                !snapshot.path.StartsWith(AssetRoot + "/", StringComparison.Ordinal) ||
                snapshot.path.Contains("..") || snapshot.path.Contains("\\"))
                throw new InvalidOperationException("Unexpected texture recovery path.");
            byte[] originalMeta = Convert.FromBase64String(snapshot.metaBase64);
            string metaPath = snapshot.path + ".meta";
            try
            {
                var importer = RequireImporter(snapshot.path);
                // Entries after a partially failed preparation were never changed and need no reimport.
                if (EditorJsonUtility.ToJson(importer) == snapshot.importerJson &&
                    File.Exists(metaPath) && File.ReadAllBytes(metaPath).SequenceEqual(originalMeta)) return;
                Exception importFailure = null;
                try
                {
                    EditorJsonUtility.FromJsonOverwrite(snapshot.importerJson, importer);
                    importer.SaveAndReimport();
                }
                catch (Exception exception) { importFailure = exception; }
                // API restoration preserves semantics; also restore the original serialized bytes.
                RestoreMetaBytes(metaPath, originalMeta);
                importer = RequireImporter(snapshot.path);
                if (importFailure != null || EditorJsonUtility.ToJson(importer) != snapshot.importerJson)
                {
                    AssetDatabase.ImportAsset(snapshot.path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    importer = RequireImporter(snapshot.path);
                }
                if (EditorJsonUtility.ToJson(importer) != snapshot.importerJson)
                    throw new InvalidOperationException("Importer settings differ from the original recovery snapshot.", importFailure);
            }
            finally
            {
                // Even an importer failure must leave the user's exact original meta on disk.
                RestoreMetaBytes(metaPath, originalMeta);
            }
        }
        static void RestoreMetaBytes(string path, byte[] original)
        {
            if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(original)) File.WriteAllBytes(path, original);
        }
        static TextureImporter RequireImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Texture importer is unavailable: " + path);
            return importer;
        }
    }
}
