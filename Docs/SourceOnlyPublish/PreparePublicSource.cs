using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TalesTactics;

public static class PreparePublicSource
{
    const string Project = "C:/dev/TalesTactics/Builds/SourceOnlyPublish20261118/Project";
    static readonly string[] Optional = { "title.local", "intermission.local", "battle.spirit", "battle.sky", "battle.good-wine", "battle.decisive" };
    [Serializable] public class Entry { public string id, path; public bool loaded; }
    [Serializable] public class Report { public bool passed; public string project, utc; public int before, after, removed; public Entry[] entries; }
    public static string Run()
    {
        string project=Directory.GetParent(Application.dataPath).FullName.Replace('\\','/');
        if(!string.Equals(project,Project,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Use the isolated publication checkout.");
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)throw new InvalidOperationException("Editor is not idle.");
        string local=Path.Combine(Project,"Assets/TalesTactics/Audio/LocalMusic");
        if(Directory.Exists(local)&&Directory.EnumerateFiles(local,"*",SearchOption.AllDirectories).Any())throw new InvalidOperationException("Local music is still present.");
        const string path="Assets/TalesTactics/Content/AudioLibrary.asset";
        var library=AssetDatabase.LoadAssetAtPath<AudioLibrary>(path);
        if(library==null||library.Entries==null)throw new InvalidOperationException("AudioLibrary missing.");
        int before=library.Entries.Length;
        var optional=library.Entries.Where(e=>e!=null&&Optional.Contains(e.Id)).ToArray();
        if(optional.Length!=6||optional.Select(e=>e.Id).Distinct().Count()!=6)throw new InvalidOperationException("Expected exactly the original six optional entries before preparation.");
        var keep=library.Entries.Where(e=>e!=null&&!Optional.Contains(e.Id)).ToArray();
        if(keep.Length!=18 || keep.Any(e=>e.Clip==null)||keep.Select(e=>e.Id).Distinct().Count()!=18)throw new InvalidOperationException("The original fourteen tracks and four effects must remain complete.");
        library.Entries=keep;
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssetIfDirty(library);
        var report=new Report{passed=true,project=project,utc=DateTime.UtcNow.ToString("o"),before=before,after=keep.Length,removed=6,
            entries=keep.Select(e=>new Entry{id=e.Id,path=AssetDatabase.GetAssetPath(e.Clip),loaded=e.Clip!=null}).ToArray()};
        string output=Path.Combine(Project,"Docs/SourceOnlyPublish/Validation/audio-library.json");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        if(File.Exists(output))throw new IOException("Preparation receipt already exists.");
        File.WriteAllText(output,JsonUtility.ToJson(report,true),new System.Text.UTF8Encoding(false));
        return output;
    }
}
