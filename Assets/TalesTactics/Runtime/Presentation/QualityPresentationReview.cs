#if UNITY_EDITOR || TALESTACTICS_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Profiling;

namespace TalesTactics
{
    // Complements (not replaces) SkillRosterVfxReview's independent resolver checks.
    // These actors never join the session, occupy grid cells, or execute combat commands.
    public sealed class QualityPresentationReview:MonoBehaviour
    {
        [Serializable] public sealed class Frame
        {
            public int frame,activeEffects,alliedModels,visibleAlliedModels;public float seconds,cameraYaw;
            public string phase,sprite,worldFacing,viewFacing,file;public bool presenting,authoredSprite,insideFrame;
            public Rect nativeRect;public Vector2 nativePivot;public string texture;public float pixelsPerUnit;public Vector3 localPosition;public Rect viewportBounds;
        }
        [Serializable] public sealed class Row
        {
            public string owner,skill,equipment,facing,mode,capture;public bool reduced,continuous,finished,clean,preparationObserved,releaseObserved,returnedIdle,authoredDirectionMatches,sessionUnchanged;
            public int frames,peakEffects,alliedModels,visibleAlliedModels;public string[] expectedSprites,observedSprites,unobservedAuthoredSprites,errors;public Frame[] timeline;
            public float preparationSeconds,travelSeconds;public string travelCaptureNote;public CaptureEvidence[] captures;
        }
        [Serializable] public sealed class EffectGeometry
        {
            public string name,material,shader;public int vertices,triangles;public float age,alphaMin,alphaMax,softParticles;public Bounds worldBounds;public Rect viewportBounds;
            public bool enabled,inCameraFrustum,positiveVertexAlpha;
        }
        [Serializable] public sealed class CaptureEvidence
        {
            public string file,phase,cameraName,depthMode,urpDepthRequirement,visibility="Geometry/frustum/vertex-alpha evidence only. Terrain occlusion, texture alpha, depth fading and perceptual readability require visual review of this PNG.";
            public float seconds,effectContactAge,orthographicSize;public int activeEffects,alliedModels,visibleAlliedModels;
            public Vector3 cameraPosition,cameraEuler,actorOrigin,recipientGround,travelOrigin,travelTarget,contactTarget;public bool originalBattleCamera,hasInFramePositiveAlphaGeometry;public EffectGeometry[] meshes;
        }
        [Serializable] public sealed class Performance
        {
            public string environment,graphicsDevice,graphicsApi,limitation="Uncapped real wall-frame samples include scene, Editor/OS scheduling and read-only sampling. No PNG/readback or per-frame file IO. Not isolated GPU time or release-player certification.";
            public int width,height,samples,vSyncCount,targetFrameRate,gc0,gc1,gc2,peakEffects,transitionSamplesOmitted;public float meanMs,p50Ms,p95Ms,p99Ms,maxMs;
            public long managedBefore,managedAfter,allocatedBefore,allocatedAfter,peakAllocated,reservedBefore,reservedAfter;
        }
        [Serializable] public sealed class Report
        {
            public int schema=2,expectedRuns,completedRuns,catalogSkills,owners,heroes,bosses,signatureWeapons,pngCount,captureWidth=960,captureHeight=540;
            public string status,mode,filter,saveRoot,unityVersion;
            public string scope="Presentation-only fixture: six original battlefield allies plus one isolated native-sprite actor. CharacterMotion, ImpactPool prepare/travel and CombatEffect contact APIs use authored timing. One staged recipient per effect; no multi-recipient/HP-number/audio/resolver/AI/turn/campaign certification. Use SkillRosterVfxReview separately for actual Director execution.";
            public string images="Unedited original BattleCamera Camera.Render output at 960x540, retaining its actual URP camera component/depth settings. VFX: near preparation, mid-travel when present, contact +0.12s, plus the same contact with all six battlefield allies. Motion images isolate the actor. PNG capture is excluded from performance mode.";
            public string visualApproval="Not approved by this automated report. passed means bounded execution/state/geometry checks only, not visible VFX or visual quality. Inspect captures and their geometry/depth evidence. Prior schema1 single-contact captures are incomplete visual evidence.";
            public string coverage="All-direction phase telemetry is sampled at 30 simulated fps. Unobserved individual authored poses are listed rather than claimed covered. With continuous capture enabled: one selected base skill for Cless, Mint, Alphen, Farah, Jade, Tear, Shionne, Velvet, Dhaos, Barbatos, Front/Back only. Candidate preview also captures the same selected skill with signature weapons Front/Back, plus Velvet base ultimate and Alphen signature ultimate in all four directions. Split owner filters to stay within the bounded image budget. All other motion runs retain one representative release image plus phase telemetry, not continuous-image coverage. Missing categories fail coverage counts; absent optional ultimates are not invented.";
            public bool passed,userFilesUnchanged,isolatedFilesUnchanged,sessionUnchanged;public string[] excludedMotionOwners=new[]{"boundary-device: stationary mission object, not an acting character; no authored attack clip"};public Performance performance;public Row[] runs;public string[] errors;
            public bool candidatePreview,candidateOriginalsUnchanged,candidateCleanupComplete;public int candidateClonesCreated,candidateClonesDestroyed;
            public string candidateSelection,candidateSelectionSha256;
            public string candidateScope="When enabled: six battlefield allies remain original; only the isolated actor uses transient candidate CharacterData/EquipmentData/EquipmentAppearance clones. This is read-only candidate evidence, not production application or approval.";
            public CandidateInput[] candidateInputs=Array.Empty<CandidateInput>();
        }
        [Serializable] public sealed class CandidateInput
        {
            public string source,sourceGuid,sourceSha256,sourceMetaSha256,candidate,candidateSha256,candidateMetaSha256,measurement,measurementSha256;
            public int frames;public string identity="Exact authored frame name and original source GUID/local file ID; candidate measured rect/pivot/PPU checked. Candidate asset IDs are separate and are never written into production.";
        }
        readonly List<Row> rows=new List<Row>();readonly List<string> errors=new List<string>();readonly List<float> wallFrames=new List<float>(32768);
        readonly Dictionary<string,string> contentBefore=new Dictionary<string,string>();
        string directory,mode="vfx",filter="",saveRoot,sessionBefore,gridBefore,campaignBefore,randomBefore;int drawsBefore,expected,pngCount,oldRate,oldTarget,oldVsync,reviewLayer;float oldScale;bool oldBackground,finished,continuous=true;
        BattleDirector battle;Camera camera;GameObject actorRoot,poolRoot;CharacterMotion motion;SpriteRenderer sprite;UnitRuntime actor;ImpactPool pool;RenderTexture renderTarget;Texture2D readback;
        Quaternion oldCameraRotation;Vector3 oldCameraPosition,origin,target;float oldCameraSize;Dictionary<string,string> userFiles,isolatedFiles;
        SkillRosterVfxFixture.Entry[] entries;CharacterData[] owners;EquipmentData[] signatures;Performance performance;
        int gc0,gc1,gc2,lastSampleFrame,omittedSamples;double lastWall;bool measuring;long peakMemory;int performancePeak;
        Vector3[] actorEnvelope=Array.Empty<Vector3>();
        static readonly Dictionary<string,string> ContinuousSkills=new Dictionary<string,string>{{"cless","cless.0"},{"mint","mint.0"},{"alphen","alphen.ultimate"},{"farah","farah.ultimate"},{"jade","jade.0"},{"tear","tear.0"},{"shionne","shionne.0"},{"velvet","velvet.ultimate"}};
        bool candidatePreview,candidateOriginalsUnchanged,candidateCleanupComplete;int candidateClonesCreated,candidateClonesDestroyed;
        string candidateSelection,candidateSelectionSha256;CandidateInput[] candidateInputs=Array.Empty<CandidateInput>();
#if UNITY_EDITOR
        CandidatePreviewAssets candidateAssets;
        public static QualityPresentationReview BeginEditor(string output,string mode="vfx",string owners="",bool continuous=true)
        {
            if(!Application.isPlaying||FindAnyObjectByType<QualityPresentationReview>()!=null)throw new InvalidOperationException("One active Play Mode review is required.");
            Validate(output,mode);var r=new GameObject("Quality presentation review").AddComponent<QualityPresentationReview>();r.directory=Path.GetFullPath(output);r.mode=mode;r.filter=owners;r.continuous=continuous;
            UnityEditor.SessionState.SetInt("TalesTactics.QualityPresentationReviewExit",-1);return r;
        }
        public static QualityPresentationReview BeginEditorCandidates(string output,string selectionJson,string mode="motion",string owners="",bool continuous=true)
        {
            if(!Application.isPlaying||FindAnyObjectByType<QualityPresentationReview>()!=null)throw new InvalidOperationException("One active Play Mode review is required.");
            Validate(output,mode);var assets=CandidatePreviewAssets.Load(selectionJson);
            try
            {
                var r=BeginEditor(output,mode,owners,continuous);r.candidateAssets=assets;r.candidatePreview=true;r.candidateSelection=assets.Selection;r.candidateSelectionSha256=assets.SelectionSha256;r.candidateInputs=assets.Inputs;return r;
            }
            catch{assets.Dispose();throw;}
        }
        // Editor-only memory ownership. No importer, AssetDatabase mutation, dirtying,
        // save, reimport, production assignment or file-write API is used here.
        public sealed class CandidatePreviewAssets:IDisposable
        {
            [Serializable] sealed class SelectionManifest {public int schemaVersion,sheets,frames;public SelectionJob[] jobs;}
            [Serializable] sealed class SelectionJob {public string source,candidate,measurement,currentSourceSha256,candidateSha256;public int frameCount;}
            [Serializable] sealed class Measurement {public int schemaVersion,width,height,frameCount;public string source,candidate,sourceGuid,candidateSha256;public float pixelsPerUnit;public MeasuredFrame[] frames;}
            [Serializable] sealed class MeasuredFrame {public string name,originalInternalId;public float x,y,width,height,pivotX,pivotY;}
            sealed class Original {public UnityEngine.Object value;public string json;public bool dirty;}
            readonly Dictionary<Sprite,Sprite> sprites=new Dictionary<Sprite,Sprite>();
            readonly Dictionary<UnityEngine.Object,UnityEngine.Object> clones=new Dictionary<UnityEngine.Object,UnityEngine.Object>();
            readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
            readonly List<Original> originals=new List<Original>();
            readonly Dictionary<string,string> files=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            bool disposed;int destroyed;
            public string SelectionPath {get;private set;}
            public string Selection=>SelectionPath;
            public string SelectionSha256 {get;private set;}
            public CandidateInput[] Inputs {get;private set;}=Array.Empty<CandidateInput>();
            public int MappingCount=>sprites.Count;
            public int CreatedCount=>owned.Count;
            public int DestroyedCount=>destroyed;
            public int LiveCount=>owned.Count(o=>o!=null);

            public CandidatePreviewAssets(IEnumerable<KeyValuePair<Sprite,Sprite>> pairs)
            {
                if(pairs==null)throw new ArgumentNullException(nameof(pairs));var targets=new HashSet<Sprite>();
                foreach(var pair in pairs)
                {
                    if(pair.Key==null||pair.Value==null||pair.Key==pair.Value||pair.Key.name!=pair.Value.name)throw new InvalidOperationException("Candidate pair must preserve an exact non-null authored name with separate sprites.");
                    if(sprites.ContainsKey(pair.Key)||!targets.Add(pair.Value))throw new InvalidOperationException("Duplicate source/candidate sprite mapping.");
                    sprites.Add(pair.Key,pair.Value);
                }
                if(sprites.Count==0)throw new InvalidOperationException("Candidate sprite mapping is empty.");
            }
            static string HashFile(string path)
            {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
            static string Full(string path)
            {
                if(string.IsNullOrWhiteSpace(path))throw new InvalidOperationException("Missing candidate path.");
                string root=Path.GetFullPath(Path.Combine(Application.dataPath,"..")).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar;
                string full=Path.GetFullPath(Path.IsPathRooted(path)?path:Path.Combine(root,path));
                if(!full.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Candidate input must stay inside the current project: "+path);
                return full;
            }
            static string AssetPath(string path)
            {
                string full=Full(path),root=Path.GetFullPath(Path.Combine(Application.dataPath,"..")).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar;
                string relative=full.Substring(root.Length).Replace('\\','/');
                if(!relative.StartsWith("Assets/TalesTactics/Art/",StringComparison.Ordinal))throw new InvalidOperationException("Candidate source is outside game art: "+path);return relative;
            }
            static void MatchHash(string path,string expected,string label)
            {
                if(expected==null||expected.Length!=64||!expected.All(Uri.IsHexDigit)||!HashFile(Full(path)).Equals(expected,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException(label+" hash mismatch: "+path);
            }
            void Pin(string path)
            {
                string full=Full(path),hash=HashFile(full);
                if(files.TryGetValue(full,out var prior)&&prior!=hash)throw new InvalidOperationException("Input changed while loading: "+path);files[full]=hash;
            }
            static bool Near(float a,float b)=>!float.IsNaN(a)&&!float.IsInfinity(a)&&Mathf.Abs(a-b)<=.01f;
            public static CandidatePreviewAssets Load(string selectionJson)
            {
                string selectionPath=Full(selectionJson),selectionHash=HashFile(selectionPath);var selection=JsonUtility.FromJson<SelectionManifest>(File.ReadAllText(selectionPath));
                if(selection==null||selection.schemaVersion!=1||selection.jobs==null||selection.jobs.Length==0||selection.sheets!=selection.jobs.Length||selection.frames!=selection.jobs.Sum(j=>j?.frameCount??0))throw new InvalidOperationException("Invalid candidate selection counts/schema.");
                var jobs=selection.jobs;var sourcePaths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var candidatePaths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var pairs=new List<KeyValuePair<Sprite,Sprite>>();var evidence=new List<CandidateInput>();var pins=new List<string>{selectionPath};
                foreach(var job in jobs)
                {
                    if(job==null)throw new InvalidOperationException("Null candidate job.");string source=AssetPath(job.source),candidate=AssetPath(job.candidate),measurementPath=Full(job.measurement);
                    if(source.Equals(candidate,StringComparison.OrdinalIgnoreCase)||!sourcePaths.Add(source)||!candidatePaths.Add(candidate))throw new InvalidOperationException("Duplicate or identical source/candidate path.");
                    MatchHash(source,job.currentSourceSha256,"Current source");MatchHash(candidate,job.candidateSha256,"Candidate");
                    string measurementHash=HashFile(measurementPath),sourceMetaHash=HashFile(Full(source+".meta")),candidateMetaHash=HashFile(Full(candidate+".meta"));
                    var m=JsonUtility.FromJson<Measurement>(File.ReadAllText(measurementPath));
                    if(m==null||m.schemaVersion!=1||m.frames==null||m.frameCount<=0||m.frameCount!=m.frames.Length||m.frameCount!=job.frameCount||AssetPath(m.source)!=source||AssetPath(m.candidate)!=candidate||!string.Equals(m.candidateSha256,job.candidateSha256,StringComparison.OrdinalIgnoreCase)||m.width<=0||m.height<=0||float.IsNaN(m.pixelsPerUnit)||float.IsInfinity(m.pixelsPerUnit)||m.pixelsPerUnit<=0)throw new InvalidOperationException("Candidate measurement mismatch: "+measurementPath);
                    if(UnityEditor.AssetDatabase.AssetPathToGUID(source)!=m.sourceGuid)throw new InvalidOperationException("Source GUID mismatch: "+source);
                    var old=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(source).OfType<Sprite>().ToArray();var next=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(candidate).OfType<Sprite>().ToArray();
                    if(old.Length!=m.frameCount||next.Length!=m.frameCount||old.Select(s=>s.name).Distinct().Count()!=old.Length||next.Select(s=>s.name).Distinct().Count()!=next.Length||m.frames.Select(f=>f?.name).Distinct().Count()!=m.frameCount)throw new InvalidOperationException("Duplicate/missing candidate frames: "+candidate);
                    var oldByName=old.ToDictionary(s=>s.name);var nextByName=next.ToDictionary(s=>s.name);
                    foreach(var f in m.frames)
                    {
                        if(f==null||string.IsNullOrEmpty(f.name)||!oldByName.TryGetValue(f.name,out var original)||!nextByName.TryGetValue(f.name,out var replacement))throw new InvalidOperationException("Missing authored candidate frame: "+candidate);
                        if(!UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original,out string guid,out long localId)||guid!=m.sourceGuid||localId.ToString(System.Globalization.CultureInfo.InvariantCulture)!=f.originalInternalId)throw new InvalidOperationException("Original sprite ID mismatch: "+f.name);
                        if(f.width<=0||f.height<=0||f.x<0||f.y<0||f.x+f.width>m.width||f.y+f.height>m.height||f.pivotX<0||f.pivotX>1||f.pivotY<0||f.pivotY>1||!Near(replacement.rect.x,f.x)||!Near(replacement.rect.y,f.y)||!Near(replacement.rect.width,f.width)||!Near(replacement.rect.height,f.height)||!Near(replacement.pivot.x,f.pivotX*f.width)||!Near(replacement.pivot.y,f.pivotY*f.height)||!Near(replacement.pixelsPerUnit,m.pixelsPerUnit)||replacement.texture.width!=m.width||replacement.texture.height!=m.height||replacement.vertices.Length<3||replacement.triangles.Length<3)throw new InvalidOperationException("Candidate imported geometry differs from measurement: "+f.name);
                        pairs.Add(new KeyValuePair<Sprite,Sprite>(original,replacement));
                    }
                    evidence.Add(new CandidateInput{source=source,sourceGuid=m.sourceGuid,sourceSha256=job.currentSourceSha256.ToLowerInvariant(),sourceMetaSha256=sourceMetaHash,candidate=candidate,candidateSha256=job.candidateSha256.ToLowerInvariant(),candidateMetaSha256=candidateMetaHash,measurement=job.measurement,measurementSha256=measurementHash,frames=m.frameCount});
                    pins.AddRange(new[]{source,source+".meta",candidate,candidate+".meta",measurementPath});
                }
                var result=new CandidatePreviewAssets(pairs){SelectionPath=selectionPath,SelectionSha256=selectionHash,Inputs=evidence.ToArray()};
                try
                {
                    foreach(var path in pins)result.Pin(path);
                    // Catch concurrent writes between initial validation and final pinning.
                    if(result.files[selectionPath]!=selectionHash)throw new InvalidOperationException("Selection changed during load.");
                    foreach(var input in result.Inputs)
                        if(result.files[Full(input.source)]!=input.sourceSha256||result.files[Full(input.source+".meta")]!=input.sourceMetaSha256||result.files[Full(input.candidate)]!=input.candidateSha256||result.files[Full(input.candidate+".meta")]!=input.candidateMetaSha256||result.files[Full(input.measurement)]!=input.measurementSha256)throw new InvalidOperationException("Candidate input changed during load.");
                    return result;
                }
                catch{result.Dispose();throw;}
            }
            void Remember(UnityEngine.Object original)
            {
                originals.Add(new Original{value=original,json=UnityEditor.EditorJsonUtility.ToJson(original),dirty=UnityEditor.EditorUtility.IsDirty(original)});
                string path=UnityEditor.AssetDatabase.GetAssetPath(original);if(!string.IsNullOrEmpty(path)){Pin(path);Pin(path+".meta");}
            }
            T Copy<T>(T original)where T:ScriptableObject
            {
                if(disposed)throw new ObjectDisposedException(nameof(CandidatePreviewAssets));if(original==null)return null;
                if(clones.TryGetValue(original,out var existing))return (T)existing;Remember(original);
                var clone=UnityEngine.Object.Instantiate(original);clone.hideFlags=HideFlags.HideAndDontSave;owned.Add(clone);clones.Add(original,clone);
                using(var serialized=new UnityEditor.SerializedObject(clone))
                {
                    var property=serialized.GetIterator();while(property.Next(true))
                        if(property.propertyType==UnityEditor.SerializedPropertyType.ObjectReference&&property.objectReferenceValue is Sprite old&&sprites.TryGetValue(old,out var replacement))property.objectReferenceValue=replacement;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                return clone;
            }
            public CharacterData Clone(CharacterData original)=>Copy(original);
            public EquipmentData Clone(EquipmentData original)
            {
                var clone=Copy(original);if(clone!=null&&original.Appearance!=null)clone.Appearance=Copy(original.Appearance);return clone;
            }
            public Sprite Replacement(Sprite original)=>original!=null&&sprites.TryGetValue(original,out var replacement)?replacement:original;
            public void AssertOriginalsUnchanged()
            {
                foreach(var item in originals)if(item.value==null||UnityEditor.EditorJsonUtility.ToJson(item.value)!=item.json||UnityEditor.EditorUtility.IsDirty(item.value)!=item.dirty)throw new InvalidOperationException("Original Content object changed: "+item.value?.name);
                foreach(var file in files)if(!File.Exists(file.Key)||HashFile(file.Key)!=file.Value)throw new InvalidOperationException("Pinned source/candidate/measurement/Content file changed: "+file.Key);
            }
            public void Dispose()
            {
                if(disposed)return;Exception failure=null;
                for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null)
                {
                    try{UnityEngine.Object.DestroyImmediate(owned[i]);destroyed++;}catch(Exception e){failure=e;}
                }
                disposed=LiveCount==0;if(failure!=null)throw failure;if(!disposed)throw new InvalidOperationException("Candidate clone disposal was incomplete.");
            }
        }
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var a=Environment.GetCommandLineArgs();int n=Array.IndexOf(a,"--quality-presentation-review");if(n<0)return;
            if(n+2>=a.Length||!Guid.TryParseExact(a[n+1],"N",out _)){Application.Quit(2);return;}
            string path=Path.Combine(Path.GetDirectoryName(Application.dataPath),"QualityPresentationReviews",a[n+1]);Validate(path,a[n+2]);
            var r=new GameObject("Quality presentation review").AddComponent<QualityPresentationReview>();r.directory=path;r.mode=a[n+2];
            int f=Array.IndexOf(a,"--quality-owners");if(f>=0&&f+1<a.Length)r.filter=a[f+1];r.continuous=!a.Contains("--quality-no-continuous");
        }
        static void Validate(string output,string mode)
        {
            if(mode!="vfx"&&mode!="motion"&&mode!="performance")throw new ArgumentException("Mode must be vfx, motion or performance.");
            string root=Path.GetFullPath(output).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),user=Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(root.Equals(user,StringComparison.OrdinalIgnoreCase)||root.StartsWith(user+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("User storage cannot be review output.");
            if(Directory.Exists(root)&&Directory.EnumerateFileSystemEntries(root).Any())throw new InvalidOperationException("Use a new empty review output.");
        }
        IEnumerator Start()
        {
            oldRate=Time.captureFramerate;oldScale=Time.timeScale;oldBackground=Application.runInBackground;oldTarget=Application.targetFrameRate;oldVsync=QualitySettings.vSyncCount;
            Application.runInBackground=true;Time.timeScale=1;Directory.CreateDirectory(directory);Application.logMessageReceived+=Log;
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                object item=null;bool next=false;Exception failure=null;
                try{next=stack.Peek().MoveNext();if(next)item=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){errors.Add(failure.ToString());break;}
                if(!next){(stack.Pop() as IDisposable)?.Dispose();continue;}if(item is IEnumerator nested)stack.Push(nested);else yield return item;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Finish();
        }
        IEnumerator Run()
        {
            yield return null;battle=FindAnyObjectByType<BattleDirector>();if(battle==null)throw new InvalidOperationException("Battle scene is not ready.");
            userFiles=Snapshot(Application.persistentDataPath);saveRoot=Path.Combine(directory,"isolated-save");battle.ConfigureStorage(saveRoot);battle.Hud.HideTitle();battle.Audio.PersonalMusicEnabled=false;battle.Audio.SetVolumes(0,0);
            for(int i=1;i<=20;i++)battle.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(i));if(!battle.SavePreparation())throw new InvalidOperationException("Cannot seed isolated save.");
            SkillRosterVfxFixture.BeginActor(battle,"cless");yield return null;yield return new WaitForEndOfFrame();
#if !UNITY_EDITOR
            // Let standalone window preference persistence settle before the immutable save baseline.
            yield return new WaitForSecondsRealtime(2f);
#endif
            isolatedFiles=Snapshot(saveRoot);sessionBefore=SkillRosterVfxFixture.State(battle.Session.Units);gridBefore=SkillRosterVfxFixture.GridState(battle.Session.Grid,battle.Session.Units);campaignBefore=JsonUtility.ToJson(battle.Campaign);randomBefore=JsonUtility.ToJson(UnityEngine.Random.state);drawsBefore=battle.Session.Resolver.RandomDraws;
            entries=SkillRosterVfxFixture.Entries(battle.Catalog);owners=battle.Catalog.Characters.Concat(new[]{battle.Catalog.Enemy}).Concat(battle.Catalog.Enemies).Concat(battle.Catalog.CrossoverVariants).Where(c=>c!=null).GroupBy(c=>c.Id).Select(g=>g.First()).OrderBy(c=>c.Id).ToArray();
            signatures=battle.Catalog.Equipment.Where(e=>e!=null&&e.Appearance!=null).ToArray();
            foreach(var c in owners)contentBefore["character:"+c.Id]=JsonUtility.ToJson(c);foreach(var e in entries)contentBefore["skill:"+e.Skill.Id]=JsonUtility.ToJson(e.Skill);foreach(var e in signatures)contentBefore["equipment:"+e.Id]=JsonUtility.ToJson(e);
            if(entries.Length!=182||battle.Catalog.Characters.Length!=10||signatures.Length!=10)throw new InvalidOperationException("Catalog coverage changed: require 182 skills, 10 heroes, 10 signatures.");
            foreach(var b in CrossoverBossSources.Entries)if(!owners.Any(c=>c.Id==b[0]))throw new InvalidOperationException("Missing boss "+b[0]);
            var wanted=filter.Split(new[]{','},StringSplitOptions.RemoveEmptyEntries).Select(s=>s.Trim()).ToArray();if(wanted.Any(id=>!owners.Any(c=>c.Id==id)))throw new ArgumentException("Unknown owner filter.");
            var selected=owners.Where(c=>c.Id!="boundary-device"&&(wanted.Length==0||wanted.Contains(c.Id))).ToArray();var selectedEntries=entries.Where(e=>wanted.Length==0||wanted.Contains(e.Owner.Id)).ToArray();
            // Performance is a bounded sample, not a duplicate 182-skill capture run.
            if(mode=="performance"&&wanted.Length==0)selectedEntries=selectedEntries.GroupBy(e=>CombatEffect.Resolve(e.Owner,e.Skill)).Select(g=>g.OrderByDescending(e=>e.Skill.IsUltimate).First()).ToArray();
            var boardCamera=battle.Board.BattleCamera;oldCameraRotation=boardCamera.transform.rotation;oldCameraPosition=boardCamera.transform.position;oldCameraSize=boardCamera.orthographicSize;
            reviewLayer=Enumerable.Range(8,24).Reverse().First(l=>(boardCamera.cullingMask&(1<<l))!=0&&!FindObjectsByType<Renderer>(FindObjectsInactive.Include).Any(r=>r.gameObject.layer==l));
            camera=new GameObject("Quality render camera",typeof(Camera)).GetComponent<Camera>();camera.CopyFrom(boardCamera);camera.enabled=false;camera.aspect=16f/9;camera.rect=new Rect(0,0,1,1);
            var allies=battle.Board.GetComponentsInChildren<CharacterMotion>().Where(m=>m.Owner?.Team==Team.Player).ToArray();if(allies.Length!=6)throw new InvalidOperationException("Six ally models required.");
            var middle=allies.Select(m=>m.transform.position).Aggregate(Vector3.zero,(a,b)=>a+b)/allies.Length;
            var floor=battle.Session.Grid.Tiles.Values.Where(t=>t.Walkable&&t.Occupant==null&&battle.Session.Grid[t.Coordinate+Vector2Int.down*2]?.Walkable==true).OrderBy(t=>(t.WorldPosition(battle.Catalog.Rules.TileHeight)-middle).sqrMagnitude).First();
            origin=floor.WorldPosition(battle.Catalog.Rules.TileHeight);target=battle.Session.Grid[floor.Coordinate+Vector2Int.down*2].WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.08f;
            poolRoot=new GameObject("Quality presentation pool");pool=poolRoot.AddComponent<ImpactPool>();Time.captureFramerate=mode=="performance"?0:30;
            if(mode=="motion")
            {
                expected=selected.Sum(c=>MotionSkills(c).Length*(1+signatures.Count(e=>e.ExclusiveCharacterId==c.Id))*4);
                Write(false);
                foreach(var c in selected)foreach(var item in new EquipmentData[]{null}.Concat(signatures.Where(e=>e.ExclusiveCharacterId==c.Id)))
                {
                    CreateActor(c,item);yield return null;
                    foreach(var skill in MotionSkills(c))foreach(Facing direction in Enum.GetValues(typeof(Facing)))yield return Sequence(c,skill,item,direction,false,false,false);
                }
            }
            else
            {
                expected=selectedEntries.Length*2;
                Write(false);
                if(mode=="performance")
                {
                    QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                    // Warm every selected effect once, including the pool/materials, before samples.
                    foreach(var e in selectedEntries){CreateActor(e.Owner,null);yield return null;yield return Sequence(e.Owner,e.Skill,null,Facing.Front,false,true,true);}
                    BeginPerformance();
                }
                foreach(var e in selectedEntries)
                {
                    CreateActor(e.Owner,null);yield return null;
                    foreach(bool reduced in new[]{false,true})yield return Sequence(e.Owner,e.Skill,null,Facing.Front,reduced,true,false);
                }
                if(mode=="performance")EndPerformance();
            }
            measuring=false;RequireSession();
            foreach(var c in owners)if(contentBefore["character:"+c.Id]!=JsonUtility.ToJson(c))errors.Add("Character asset mutated: "+c.Id);
            foreach(var e in entries)if(contentBefore["skill:"+e.Skill.Id]!=JsonUtility.ToJson(e.Skill))errors.Add("Skill asset mutated: "+e.Skill.Id);
            foreach(var e in signatures)if(contentBefore["equipment:"+e.Id]!=JsonUtility.ToJson(e))errors.Add("Equipment asset mutated: "+e.Id);
            var meshes=poolRoot.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();var materials=poolRoot.GetComponentsInChildren<Renderer>(true).Select(r=>r.sharedMaterial).Distinct().ToArray();
            Destroy(poolRoot);poolRoot=null;yield return null;yield return null;if(meshes.Any(m=>m!=null)||materials.Any(m=>m!=null))errors.Add("Owned VFX resources survived destruction.");
        }
        static SkillData[] MotionSkills(CharacterData c)
        {
            // Include both physical and casting paths when the roster has them.
            return new[]{c.BasicAttack,c.Skills.FirstOrDefault(s=>s!=null&&!Casting(s)),c.Skills.FirstOrDefault(s=>s!=null&&Casting(s)),c.UltimateSkill}.Where(s=>s!=null).Distinct().ToArray();
        }
        static bool Casting(SkillData s)=>!s.IsUltimate&&(SkillSignatures.IsSpell(SkillSignatures.Resolve(s))||ArteProfiles.Resolve(s).IsSpell||s.Animation==AnimationKind.Cast);
        static DirectionalSpriteClip Clip(CharacterData c,SkillData s)=>s.IsUltimate?c.Poses.Ultimate:Casting(s)?CharacterMotionClips.Casting(c):s.Animation==AnimationKind.Attack||SkillSignatures.Resolve(s)==SkillSignature.DemonFang?c.Poses.AttackMotion:c.Poses.Skill;
        void CreateActor(CharacterData data,EquipmentData item)
        {
            if(actorRoot!=null){actorRoot.SetActive(false);Destroy(actorRoot);}if(data.Animator!=null)throw new InvalidOperationException("Animator-authored actor requires its own harness: "+data.Id);
            var team=battle.Catalog.Characters.Contains(data)?Team.Player:Team.Enemy;
#if UNITY_EDITOR
            if(candidateAssets!=null){data=candidateAssets.Clone(data);item=candidateAssets.Clone(item);}
#endif
            actor=new UnitRuntime(data,team,battle.Catalog.Rules);actor.Equipment[0]=item;
            actorRoot=new GameObject("Isolated native model "+data.Id);actorRoot.transform.position=origin;
            var scale=new GameObject("Body Scale");scale.transform.SetParent(actorRoot.transform,false);scale.transform.localScale=Vector3.one*CharacterVisualSizing.Scale(data);
            var visual=new GameObject("Directional Sprite");visual.transform.SetParent(scale.transform,false);visual.layer=reviewLayer;sprite=visual.AddComponent<SpriteRenderer>();
            sprite.sharedMaterial=battle.Board.GetComponentsInChildren<CharacterMotion>().First().GetComponent<SpriteRenderer>().sharedMaterial;
            motion=visual.AddComponent<CharacterMotion>();motion.Initialize(sprite,actor,battle.Board.BattleCamera);
        }
        IEnumerator Sequence(CharacterData data,SkillData skill,EquipmentData item,Facing direction,bool reduced,bool effects,bool warmup)
        {
            // Expected clip frames and equipment keys must come from the same transient
            // actor as CharacterMotion, never the original catalog during candidate review.
            data=actor.Data;item=actor.Equipment[0];
            var boardCamera=battle.Board.BattleCamera;
            if(effects){boardCamera.transform.SetPositionAndRotation(oldCameraPosition,oldCameraRotation);direction=CharacterMotion.ViewFacing(actor.Facing,oldCameraRotation);}
            else for(int i=0;i<4&&CharacterMotion.ViewFacing(actor.Facing,boardCamera.transform.rotation)!=direction;i++)boardCamera.transform.rotation=Quaternion.Euler(boardCamera.transform.eulerAngles.x,boardCamera.transform.eulerAngles.y+90,0);
            if(CharacterMotion.ViewFacing(actor.Facing,boardCamera.transform.rotation)!=direction)throw new InvalidOperationException("Could not establish camera-relative facing "+direction);
            string name=data.Id+"/"+(item==null?"base":"signature")+"/"+skill.Id+"-"+direction+(effects?(reduced?"-reduced":"-normal"):"");
            motion.Set(AnimationKind.Idle);FrameCamera(effects,skill);yield return new WaitForEndOfFrame();
            bool capture=mode!="performance",frames=capture&&!effects&&continuous&&((item==null||candidatePreview)&&(direction==Facing.Front||direction==Facing.Back)&&ContinuousSkill(data)==skill||candidatePreview&&skill==data.UltimateSkill&&(data.Id=="velvet"&&item==null||data.Id=="alphen"&&item!=null));
            var timeline=new List<Frame>();var captures=new List<CaptureEvidence>();var issues=new List<string>();var observed=new HashSet<Sprite>();var clip=Clip(data,skill);
            var authored=new HashSet<Sprite>((clip?.Frames??new DirectionalSprites[0]).Where(f=>f!=null).Select(f=>EquipmentAppearance.Resolve(actor,f.Get(direction))).Where(s=>s!=null));
            var arte=ArteProfiles.Resolve(skill);bool guard=arte.Enabled&&arte.Motion==ArteMotion.Guard&&!skill.IsUltimate;
            if(guard){authored.Clear();authored.Add(EquipmentAppearance.Resolve(actor,data.Poses.Guard.Get(direction)));}
            var idle=EquipmentAppearance.Resolve(actor,data.Sprites.Get(direction));authored.Add(idle);
            var guardSprite=EquipmentAppearance.Resolve(actor,data.Poses.Guard?.Get(direction));
            var prepared=new HashSet<Sprite>(guard?new[]{guardSprite}:(clip?.Frames??new DirectionalSprites[0]).Take(Mathf.Clamp(clip?.ReleaseFrame??0,1,clip?.Frames?.Length??1)).Where(f=>f!=null).Select(f=>EquipmentAppearance.Resolve(actor,f.Get(direction))));
            var releasedSprites=new HashSet<Sprite>(guard?new[]{guardSprite}:(clip?.Frames??new DirectionalSprites[0]).Skip(Mathf.Clamp(clip?.ReleaseFrame??0,1,clip?.Frames?.Length??1)).Where(f=>f!=null).Select(f=>EquipmentAppearance.Resolve(actor,f.Get(direction))));
            var signature=SkillSignatures.Resolve(skill);var style=CombatEffect.Resolve(data,skill);float size=skill.Presentation?.Size??1;var pattern=skill.Presentation?.Pattern??SkillVisualPattern.Automatic;
            var feedback=Feedback(skill);bool ground=signature!=SkillSignature.None||arte.Enabled;
            var recipientGround=feedback==CombatFeedback.Strike?target-Vector3.up*.08f:origin;
            var travelFrom=ground?origin+Vector3.up*(signature==SkillSignature.DemonFang?.12f:.1f):motion.transform.position+boardCamera.transform.up*.65f;
            var travelTo=recipientGround+(ground?Vector3.up*(signature==SkillSignature.DemonFang?.12f:.08f):boardCamera.transform.up*.65f);
            var contactTo=recipientGround+(ground?Vector3.up*.08f:boardCamera.transform.up*.65f);
            float windup=BoardView.Windup(skill),recovery=BoardView.Recovery(skill),travel=SkillVfxTiming.ReleaseDuration(data,skill,Vector3.Distance(travelFrom,travelTo));
            bool release=false,contact=false,prepareSeen=false,releaseSeen=false,prepareCaptured=false,travelCaptured=false;int peak=0,frame=0;string captureFile=null;float began=Time.time,releaseAt=-1,contactAt=-1;double deadline=Time.realtimeSinceStartupAsDouble+120;
            motion.BeginSkill(skill,windup,recovery);
            if(effects&&(arte.Enabled||skill.Animation!=AnimationKind.Attack||skill.IsUltimate||ImpactParticles.HasWeaponSweep(style)))pool.Prepare(boardCamera,motion.transform,travelTo,skill.Element,style,size,skill.IsUltimate,reduced,windup,pattern,signature,arte);
            do
            {
                yield return null;float elapsed=Time.time-began;
                if(!release&&elapsed>=windup){release=true;releaseAt=Time.time;motion.ReleaseSkill(ground?travel:0);if(effects&&travel>0)pool.Travel(boardCamera,travelFrom,travelTo,skill.Element,style,size,skill.IsUltimate,reduced,travel,pattern,signature,arte);}
                if(!contact&&release&&Time.time>=releaseAt+travel)
                {
                    contact=true;contactAt=Time.time;if(effects){var hit=new GameObject("Staged contact");hit.transform.SetParent(poolRoot.transform,false);hit.AddComponent<CombatEffect>().Initialize(battle.Board.SpriteMaterial,boardCamera,style,feedback,actor,motion.transform.position+boardCamera.transform.up*.65f,contactTo,FeedbackColor(skill.Element,feedback),skill.IsUltimate,skill.Presentation,skill.Element,reduced,false,signature,arte);}
                }
                yield return new WaitForEndOfFrame();elapsed=Time.time-began;
                peak=Mathf.Max(peak,pool.ActiveCount);observed.Add(sprite.sprite);
                // Phase changes happen before LateUpdate; capture records the sprite rendered that frame.
                prepareSeen|=!release&&prepared.Contains(sprite.sprite);releaseSeen|=release&&motion.Presenting&&releasedSprites.Contains(sprite.sprite);
                if(capture)
                {
                    var row=ObserveFrame(frame,elapsed,contact?"contact/recovery":release?"travel":"prepare",authored,effects);timeline.Add(row);
                    bool representative=captureFile==null&&contact&&Time.time>=contactAt+(effects?.12f:2f/30);
                    if(effects)
                    {
                        if(!prepareCaptured&&!release&&elapsed>=windup*.5f){prepareCaptured=true;captures.Add(CaptureVfx(name+"/prepare.png","prepare",elapsed,-1,false,recipientGround,travelFrom,travelTo,contactTo));}
                        if(!travelCaptured&&release&&!contact&&Time.time>=releaseAt+travel*.5f){travelCaptured=true;captures.Add(CaptureVfx(name+"/travel.png","travel",elapsed,-1,false,recipientGround,travelFrom,travelTo,contactTo));}
                        if(representative)
                        {
                            captureFile=row.file=name+"/contact.png";captures.Add(CaptureVfx(row.file,"contact",elapsed,Time.time-contactAt,false,recipientGround,travelFrom,travelTo,contactTo));
                            captures.Add(CaptureVfx(name+"/field-contact.png","field-contact",elapsed,Time.time-contactAt,true,recipientGround,travelFrom,travelTo,contactTo));CheckGeometry(issues);
                        }
                    }
                    else if(frames||representative){row.file=name+"/"+(frames?frame.ToString("D4"):"contact")+".png";SaveImage(row.file);if(representative)captureFile=row.file;}
                }
                if(measuring)SamplePerformance();frame++;
                if(!motion.Presenting&&contact&&pool.ActiveCount==0&&poolRoot.GetComponentsInChildren<CombatEffect>().Length==0&&Time.time>=contactAt+recovery+.15f)break;
            }while(frame<6000&&Time.realtimeSinceStartupAsDouble<deadline);
            if(motion.Presenting||pool.ActiveCount!=0)issues.Add("Animation/effect exceeded bounded deadline.");
            if(effects&&peak==0)issues.Add("No active effect was observed.");
            if(!prepareSeen||!releaseSeen)issues.Add("Authored preparation/release was not observed.");
            if(sprite.sprite!=idle||motion.Action!=AnimationKind.Idle)issues.Add("Did not return to authored idle.");
            if(observed.Any(s=>!authored.Contains(s)))issues.Add("A sprite outside the equipped authored direction was rendered.");
            if(timeline.Any(f=>!f.insideFrame))issues.Add("Actor sprite left the render frame.");if(timeline.Any(f=>f.viewFacing!=direction.ToString()))issues.Add("Camera-relative direction changed during animation.");
            if(SkillRosterVfxFixture.Models(battle)!=6)issues.Add("Six battlefield ally models were not preserved.");
            int visibleAllies=VisibleAllies(effects?boardCamera:camera);if(effects&&visibleAllies!=6)issues.Add("The field camera does not include all six allies.");
            if(capture&&effects){if(!prepareCaptured)issues.Add("Preparation capture was not sampled.");if(travel>1f/30&&!travelCaptured)issues.Add("Mid-travel capture was not sampled.");if(captureFile==null)issues.Add("Contact capture was not sampled.");}
            bool unchanged=SessionUnchanged();if(!unchanged)issues.Add("Presentation changed session/grid/campaign/RNG.");
            if(!warmup)
            {
                var row=new Row{owner=data.Id,skill=skill.Id,equipment=item?.Id,facing=direction.ToString(),mode=mode,reduced=reduced,continuous=frames,finished=!motion.Presenting,clean=pool.ActiveCount==0,preparationObserved=prepareSeen,releaseObserved=releaseSeen,returnedIdle=sprite.sprite==idle,authoredDirectionMatches=observed.All(authored.Contains),sessionUnchanged=unchanged,frames=frame,peakEffects=peak,alliedModels=SkillRosterVfxFixture.Models(battle),visibleAlliedModels=visibleAllies,capture=captureFile,
                    expectedSprites=authored.Where(s=>s!=null).Select(s=>s.name).OrderBy(s=>s).ToArray(),observedSprites=observed.Where(s=>s!=null).Select(s=>s.name).OrderBy(s=>s).ToArray(),unobservedAuthoredSprites=authored.Except(observed).Where(s=>s!=null).Select(s=>s.name).ToArray(),timeline=timeline.ToArray(),errors=issues.ToArray(),preparationSeconds=windup,travelSeconds=travel,travelCaptureNote=!capture?"No captures in performance mode.":!effects?"Motion-only run.":travelCaptured?"Actual mid-travel sample.":travel<=0?"No separate travel phase in authored timing; no travel image fabricated.":"Travel shorter than one sample; no travel image fabricated.",captures=captures.ToArray()};
                rows.Add(row);errors.AddRange(issues.Select(e=>name+": "+e));
                if(mode!="performance"){Directory.CreateDirectory(Path.Combine(directory,name));File.WriteAllText(Path.Combine(directory,name,"timeline.json"),JsonUtility.ToJson(row,true));Write(false);}
            }
            else if(issues.Count>0)throw new InvalidOperationException("Warmup "+name+": "+string.Join("; ",issues));
        }
        SkillData ContinuousSkill(CharacterData c)
        {
            if(ContinuousSkills.TryGetValue(c.Id,out var id))return new[]{c.BasicAttack}.Concat(c.Skills).Concat(new[]{c.UltimateSkill}).FirstOrDefault(s=>s!=null&&s.Id==id);
            return c.Id=="dhaos"||c.Id=="barbatos"?MotionSkills(c).LastOrDefault():null;
        }
        static CombatFeedback Feedback(SkillData s)=>s.Effects.Any(e=>e.Kind==EffectKind.Damage&&!e.AffectCaster)?CombatFeedback.Strike:s.Effects.Any(e=>e.Kind==EffectKind.Revive)?CombatFeedback.Revive:s.Effects.Any(e=>e.Kind==EffectKind.Heal)?CombatFeedback.Heal:CombatFeedback.Support;
        static Color FeedbackColor(Element element,CombatFeedback feedback)
        {
            if(feedback==CombatFeedback.Revive)return new Color(1,.95f,.5f);if(feedback==CombatFeedback.Heal)return new Color(.35f,1,.6f);
            switch(element){case Element.Fire:return new Color(1,.35f,.1f);case Element.Water:case Element.Ice:return new Color(.25f,.8f,1);case Element.Wind:return new Color(.45f,1,.7f);case Element.Earth:return new Color(1,.7f,.3f);case Element.Dark:return new Color(.85f,.35f,1);case Element.Lightning:return new Color(1,.95f,.3f);default:return new Color(1,.95f,.8f);}
        }
        void FrameCamera(bool effects,SkillData skill)
        {
            camera.CopyFrom(battle.Board.BattleCamera);camera.enabled=false;camera.targetTexture=null;camera.aspect=16f/9;camera.rect=new Rect(0,0,1,1);camera.cullingMask=effects?-1:1<<reviewLayer;
            if(effects){camera.transform.rotation=oldCameraRotation;BuildActorEnvelope(skill);FrameVfxNear();return;}
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.055f,.085f);
            // Guard artes replace the phase clip at runtime. Fit those equipped vertices
            // and the full movement/tilt envelope, rather than omitting the override pose.
            BuildActorEnvelope(skill,true);Vector2 min=Vector2.one*float.PositiveInfinity,max=Vector2.one*float.NegativeInfinity;
            var right=camera.transform.right;var up=camera.transform.up;
            foreach(var p in actorEnvelope){if(!Finite(p.x)||!Finite(p.y)||!Finite(p.z))throw new InvalidOperationException("Nonfinite motion capture bounds.");var v=new Vector2(Vector3.Dot(p,right),Vector3.Dot(p,up));min=Vector2.Min(min,v);max=Vector2.Max(max,v);}
            if(actorEnvelope.Length==0)throw new InvalidOperationException("No authored motion vertices to frame.");
            var mid=(min+max)*.5f;var center=right*mid.x+up*mid.y+camera.transform.forward*Vector3.Dot(origin,camera.transform.forward);
            camera.orthographicSize=Mathf.Max(.9f,Mathf.Max((max.y-min.y)*.5f,(max.x-min.x)*.5f/camera.aspect)+.32f);camera.transform.position=center-camera.transform.forward*15;
        }
        Frame ObserveFrame(int frame,float seconds,string phase,HashSet<Sprite> authored,bool effects)
        {
            Vector2 min=Vector2.one*float.PositiveInfinity,max=Vector2.one*float.NegativeInfinity;bool front=true;
            foreach(var v in sprite.sprite.vertices){var p=camera.WorldToViewportPoint(sprite.transform.TransformPoint(v));min=Vector2.Min(min,p);max=Vector2.Max(max,p);front&=p.z>0;}
            return new Frame{frame=frame,seconds=seconds,phase=phase,sprite=sprite.sprite.name,worldFacing=actor.Facing.ToString(),viewFacing=CharacterMotion.ViewFacing(actor.Facing,battle.Board.BattleCamera.transform.rotation).ToString(),cameraYaw=battle.Board.BattleCamera.transform.eulerAngles.y,presenting=motion.Presenting,authoredSprite=authored.Contains(sprite.sprite),nativeRect=sprite.sprite.rect,nativePivot=sprite.sprite.pivot,texture=sprite.sprite.texture.name,pixelsPerUnit=sprite.sprite.pixelsPerUnit,localPosition=motion.transform.localPosition,viewportBounds=Rect.MinMaxRect(min.x,min.y,max.x,max.y),insideFrame=front&&min.x>=0&&min.y>=0&&max.x<=1&&max.y<=1,activeEffects=pool.ActiveCount,alliedModels=SkillRosterVfxFixture.Models(battle),visibleAlliedModels=effects?VisibleAllies(battle.Board.BattleCamera):0};
        }
        int VisibleAllies(Camera view)
        {
            var planes=GeometryUtility.CalculateFrustumPlanes(view);return battle.Board.GetComponentsInChildren<CharacterMotion>().Count(m=>m.Owner?.Team==Team.Player&&m.isActiveAndEnabled&&m.GetComponent<SpriteRenderer>().enabled&&GeometryUtility.TestPlanesAABB(planes,m.GetComponent<SpriteRenderer>().bounds));
        }
        static IEnumerable<Vector3> Corners(Bounds b)
        {for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)yield return b.center+Vector3.Scale(b.extents,new Vector3(x,y,z));}
        void BuildActorEnvelope(SkillData skill,bool motionOnly=false)
        {
            // Use the actual equipped clip vertices/PPU, not just the initial idle bounds.
            // Thus a larger authored cast pose remains visible without changing its actual scale.
            var data=actor.Data;var direction=CharacterMotion.ViewFacing(actor.Facing,camera.transform.rotation);var arte=ArteProfiles.Resolve(skill);var signature=SkillSignatures.Resolve(skill);var style=CombatEffect.Resolve(data,skill);
            var profile=arte.Enabled?ArteMotionProfile.For(arte,style):Casting(skill)?new CombatMotionProfile(.015f,.005f,.025f,0,0):signature==SkillSignature.DemonFang?new CombatMotionProfile(.3f,.12f,0,17,.065f):CombatMotionProfile.For(style,skill.IsUltimate);
            var phaseSprites=(Clip(data,skill)?.Frames??Array.Empty<DirectionalSprites>()).Where(f=>f!=null).Select(f=>f.Get(direction));var guard=data.Poses.Guard?.Get(direction);
            if(motionOnly&&arte.Enabled&&arte.Motion==ArteMotion.Guard&&!skill.IsUltimate&&guard!=null)phaseSprites=new[]{guard};
            // Preserve the established VFX framing. Motion-only captures include a guard
            // only when CharacterMotion actually uses it, keeping unrelated poses readable.
            var sprites=phaseSprites.Concat(motionOnly?new[]{data.Sprites.Get(direction)}:new[]{data.Sprites.Get(direction),guard}).Select(s=>EquipmentAppearance.Resolve(actor,s)).Where(s=>s!=null).Distinct();
            float scale=CharacterVisualSizing.Scale(data),tilt=Mathf.Abs(profile.Tilt)+(arte.Enabled&&arte.Motion==ArteMotion.Spin?28:0);var facing=SkillResolver.FacingVector(actor.Facing);var forward=new Vector3(facing.x,0,facing.y);var points=new List<Vector3>();
            foreach(var s in sprites)foreach(var vertex in s.vertices)for(int turn=0;turn<=8;turn++)foreach(float travel in new[]{-Mathf.Abs(profile.Recoil),Mathf.Abs(profile.Advance)+Mathf.Abs(profile.Recoil)*.12f})foreach(float lift in new[]{0,Mathf.Abs(profile.Lift)})
            {var rotation=camera.transform.rotation*Quaternion.Euler(0,0,Mathf.Lerp(-tilt,tilt,turn/8f));points.Add(origin+(forward*travel+Vector3.up*(.04f+lift))*scale+rotation*(Vector3)(vertex*scale));}
            actorEnvelope=points.ToArray();
        }
        void FrameVfxNear()
        {
            // Do not move actors/effects or hide the terrain. Fit the real staged geometry.
            var points=new List<Vector3>{origin,origin+Vector3.up*1.6f,target,target+Vector3.up*1.6f};points.AddRange(actorEnvelope);points.AddRange(Corners(sprite.bounds));
            if(poolRoot!=null)foreach(var f in poolRoot.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh!=null&&f.sharedMesh.vertexCount>0&&f.TryGetComponent<Renderer>(out var r)&&r.enabled)points.AddRange(Corners(r.bounds));
            Vector2 min=Vector2.one*float.PositiveInfinity,max=Vector2.one*float.NegativeInfinity;var right=camera.transform.right;var up=camera.transform.up;
            foreach(var p in points){if(!Finite(p.x)||!Finite(p.y)||!Finite(p.z))throw new InvalidOperationException("Nonfinite VFX capture bounds.");var v=new Vector2(Vector3.Dot(p,right),Vector3.Dot(p,up));min=Vector2.Min(min,v);max=Vector2.Max(max,v);}
            var mid=(min+max)*.5f;var center=right*mid.x+up*mid.y+camera.transform.forward*Vector3.Dot((origin+target)*.5f,camera.transform.forward);
            camera.orthographicSize=Mathf.Max(1.65f,Mathf.Max((max.y-min.y)*.5f,(max.x-min.x)*.5f/camera.aspect)+.32f);camera.transform.position=center-camera.transform.forward*25;
        }
        CaptureEvidence CaptureVfx(string file,string phase,float seconds,float contactAge,bool field,Vector3 recipient,Vector3 travelFrom,Vector3 travelTo,Vector3 contactTo)
        {
            camera.transform.rotation=oldCameraRotation;camera.cullingMask=battle.Board.BattleCamera.cullingMask;
            if(field){camera.transform.position=oldCameraPosition;camera.orthographicSize=oldCameraSize;}else FrameVfxNear();
            CaptureEvidence result=null;
            SaveImage(file,view=>
            {
                var geometry=new List<EffectGeometry>();var planes=GeometryUtility.CalculateFrustumPlanes(view);
                foreach(var f in poolRoot.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh=f.sharedMesh;if(mesh==null||mesh.vertexCount==0||!f.TryGetComponent<Renderer>(out var renderer))continue;var colors=mesh.colors;float min=colors.Length==0?1:colors.Min(c=>c.a),max=colors.Length==0?1:colors.Max(c=>c.a);
                    var projected=Corners(renderer.bounds).Select(view.WorldToViewportPoint).ToArray();var material=renderer.sharedMaterial;
                    geometry.Add(new EffectGeometry{name=f.name,material=material?.name,shader=material?.shader?.name,vertices=mesh.vertexCount,triangles=mesh.triangles.Length/3,age=f.GetComponentInParent<ImpactParticles>()?.Age??-1,alphaMin=min,alphaMax=max,softParticles=material!=null&&material.HasProperty("_SoftParticles")?material.GetFloat("_SoftParticles"):0,worldBounds=renderer.bounds,viewportBounds=Rect.MinMaxRect(projected.Min(p=>p.x),projected.Min(p=>p.y),projected.Max(p=>p.x),projected.Max(p=>p.y)),enabled=renderer.enabled,inCameraFrustum=renderer.enabled&&GeometryUtility.TestPlanesAABB(planes,renderer.bounds),positiveVertexAlpha=max>.001f});
                }
                string depth="No UniversalAdditionalCameraData";foreach(var component in view.GetComponents<Component>())if(component!=null&&component.GetType().Name=="UniversalAdditionalCameraData"){depth=component.GetType().GetProperty("requiresDepthTexture")?.GetValue(component)?.ToString()??"unavailable";break;}
                result=new CaptureEvidence{file=file,phase=phase,seconds=seconds,effectContactAge=contactAge,cameraName=view.name,originalBattleCamera=view==battle.Board.BattleCamera,depthMode=view.depthTextureMode.ToString(),urpDepthRequirement=depth,cameraPosition=view.transform.position,cameraEuler=view.transform.eulerAngles,orthographicSize=view.orthographicSize,actorOrigin=origin,recipientGround=recipient,travelOrigin=travelFrom,travelTarget=travelTo,contactTarget=contactTo,activeEffects=pool.ActiveCount,alliedModels=SkillRosterVfxFixture.Models(battle),visibleAlliedModels=VisibleAllies(view),meshes=geometry.ToArray(),hasInFramePositiveAlphaGeometry=geometry.Any(g=>g.enabled&&g.inCameraFrustum&&g.positiveVertexAlpha)};
            });
            // Keep per-frame actor telemetry in the near view, even after the field capture.
            FrameVfxNear();return result;
        }
        void SaveImage(string relative,Action<Camera> observe=null)
        {
            if(pngCount>=2500)throw new InvalidOperationException("Review image budget (2500 PNG) exceeded; use owner filters.");
            if(renderTarget==null){renderTarget=new RenderTexture(960,540,24,RenderTextureFormat.ARGB32);renderTarget.Create();readback=new Texture2D(960,540,TextureFormat.RGBA32,false);}
            // Render through the actual battle camera: CopyFrom cannot copy URP camera components.
            // Temporarily change framing only, without allowing Update/LateUpdate between capture and restoration.
            var view=battle.Board.BattleCamera;var old=RenderTexture.active;var prior=view.targetTexture;var position=view.transform.position;var rotation=view.transform.rotation;float size=view.orthographicSize,aspect=view.aspect;var rect=view.rect;int mask=view.cullingMask;var clear=view.clearFlags;var background=view.backgroundColor;
            try{view.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);view.orthographicSize=camera.orthographicSize;view.aspect=camera.aspect;view.rect=camera.rect;view.cullingMask=camera.cullingMask;view.clearFlags=camera.clearFlags;view.backgroundColor=camera.backgroundColor;view.targetTexture=renderTarget;view.Render();RenderTexture.active=renderTarget;readback.ReadPixels(new Rect(0,0,960,540),0,0);readback.Apply(false,false);observe?.Invoke(view);string path=Path.Combine(directory,relative);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,readback.EncodeToPNG());pngCount++;}
            finally{view.targetTexture=prior;view.transform.SetPositionAndRotation(position,rotation);view.orthographicSize=size;view.aspect=aspect;view.rect=rect;view.cullingMask=mask;view.clearFlags=clear;view.backgroundColor=background;RenderTexture.active=old;}
        }
        void CheckGeometry(List<string> issues)
        {
            foreach(var filter in poolRoot.GetComponentsInChildren<MeshFilter>(true))foreach(var v in filter.sharedMesh.vertices)if(!Finite(v.x)||!Finite(v.y)||!Finite(v.z)){issues.Add("Nonfinite VFX mesh vertex.");return;}
        }
        static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
        void BeginPerformance()
        {
            gc0=GC.CollectionCount(0);gc1=GC.CollectionCount(1);gc2=GC.CollectionCount(2);peakMemory=Profiler.GetTotalAllocatedMemoryLong();
            performance=new Performance{environment=Application.isEditor?"Unity Editor":"Standalone review player",graphicsDevice=SystemInfo.graphicsDeviceName,graphicsApi=SystemInfo.graphicsDeviceType.ToString(),width=Screen.width,height=Screen.height,vSyncCount=QualitySettings.vSyncCount,targetFrameRate=Application.targetFrameRate,managedBefore=GC.GetTotalMemory(false),allocatedBefore=peakMemory,reservedBefore=Profiler.GetTotalReservedMemoryLong()};
            lastWall=Time.realtimeSinceStartupAsDouble;lastSampleFrame=Time.frameCount;measuring=true;
        }
        void SamplePerformance()
        {
            double now=Time.realtimeSinceStartupAsDouble;if(Time.frameCount==lastSampleFrame+1)wallFrames.Add((float)((now-lastWall)*1000));else omittedSamples++;
            lastWall=now;lastSampleFrame=Time.frameCount;peakMemory=Math.Max(peakMemory,Profiler.GetTotalAllocatedMemoryLong());performancePeak=Mathf.Max(performancePeak,pool.ActiveCount);
        }
        void EndPerformance()
        {
            measuring=false;var sorted=wallFrames.OrderBy(v=>v).ToArray();if(sorted.Length==0)throw new InvalidOperationException("No uncaptured frame samples.");
            performance.samples=sorted.Length;performance.transitionSamplesOmitted=omittedSamples;performance.meanMs=sorted.Average();performance.p50Ms=Percentile(sorted,.5f);performance.p95Ms=Percentile(sorted,.95f);performance.p99Ms=Percentile(sorted,.99f);performance.maxMs=sorted.Last();performance.peakEffects=performancePeak;
            performance.managedAfter=GC.GetTotalMemory(false);performance.allocatedAfter=Profiler.GetTotalAllocatedMemoryLong();performance.peakAllocated=peakMemory;performance.reservedAfter=Profiler.GetTotalReservedMemoryLong();performance.gc0=GC.CollectionCount(0)-gc0;performance.gc1=GC.CollectionCount(1)-gc1;performance.gc2=GC.CollectionCount(2)-gc2;
        }
        static float Percentile(float[] values,float p)=>values[Mathf.Clamp(Mathf.CeilToInt(values.Length*p)-1,0,values.Length-1)];
        bool SessionUnchanged()=>battle?.Session!=null&&sessionBefore!=null&&sessionBefore==SkillRosterVfxFixture.State(battle.Session.Units)&&gridBefore==SkillRosterVfxFixture.GridState(battle.Session.Grid,battle.Session.Units)&&campaignBefore==JsonUtility.ToJson(battle.Campaign)&&randomBefore==JsonUtility.ToJson(UnityEngine.Random.state)&&drawsBefore==battle.Session.Resolver.RandomDraws;
        void RequireSession(){if(!SessionUnchanged())throw new InvalidOperationException("Presentation fixture changed session/grid/campaign/RNG.");}
        static Dictionary<string,string> Snapshot(string root)
        {
            var files=new Dictionary<string,string>();if(!Directory.Exists(root))return files;foreach(var path in Directory.GetFiles(root).Where(p=>Path.GetFileName(p).StartsWith("campaign",StringComparison.OrdinalIgnoreCase)||Path.GetFileName(p).StartsWith("settings",StringComparison.OrdinalIgnoreCase)))using(var sha=SHA256.Create())files[Path.GetFileName(path)]=Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));return files;
        }
        static bool Equal(Dictionary<string,string> a,Dictionary<string,string> b)=>a!=null&&b!=null&&a.Count==b.Count&&a.All(p=>b.TryGetValue(p.Key,out var v)&&v==p.Value);
        void Write(bool final)
        {
            bool users=Equal(userFiles,Snapshot(Application.persistentDataPath)),isolated=saveRoot!=null&&Equal(isolatedFiles,Snapshot(saveRoot)),state=SessionUnchanged();
            bool passed=final&&errors.Count==0&&expected>0&&rows.Count==expected&&users&&isolated&&state&&(!candidatePreview||candidateOriginalsUnchanged&&candidateCleanupComplete);
            var report=new Report{mode=mode,filter=filter,status=final?(passed?"passed":"failed"):"running",passed=passed,saveRoot=saveRoot,unityVersion=Application.unityVersion,expectedRuns=expected,completedRuns=rows.Count,catalogSkills=entries?.Length??0,owners=owners?.Length??0,heroes=battle?.Catalog.Characters.Length??0,bosses=CrossoverBossSources.Entries.Length,signatureWeapons=signatures?.Length??0,pngCount=pngCount,userFilesUnchanged=users,isolatedFilesUnchanged=isolated,sessionUnchanged=state,performance=performance,runs=rows.ToArray(),errors=errors.ToArray()};
            report.candidatePreview=candidatePreview;report.candidateSelection=candidateSelection;report.candidateSelectionSha256=candidateSelectionSha256;report.candidateInputs=candidateInputs;report.candidateOriginalsUnchanged=candidateOriginalsUnchanged;report.candidateCleanupComplete=candidateCleanupComplete;report.candidateClonesCreated=candidateClonesCreated;report.candidateClonesDestroyed=candidateClonesDestroyed;
            File.WriteAllText(Path.Combine(directory,final?"result.json":"progress.json"),JsonUtility.ToJson(report,true));
        }
        void Finish()
        {
            if(finished)return;finished=true;measuring=false;Application.logMessageReceived-=Log;int exit=1;
            try{if(!Equal(userFiles,Snapshot(Application.persistentDataPath)))errors.Add("User files changed.");if(saveRoot==null||!Equal(isolatedFiles,Snapshot(saveRoot)))errors.Add("Isolated files changed.");if(!SessionUnchanged())errors.Add("Session changed.");CloseCandidatePreview();Write(true);File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);exit=errors.Count==0&&rows.Count==expected&&expected>0?0:1;}
            catch(Exception e){Debug.LogError("Quality review finalization failed: "+e);}
            finally{try{Restore();}catch(Exception e){exit=1;Debug.LogError("Quality review restoration failed: "+e);}finally{
#if UNITY_EDITOR
                try{UnityEditor.SessionState.SetInt("TalesTactics.QualityPresentationReviewExit",exit);}finally{UnityEditor.EditorApplication.isPlaying=false;}
#else
                Application.Quit(exit);
#endif
            }}
        }
        void Restore()
        {
            Time.captureFramerate=oldRate;Time.timeScale=oldScale;Application.runInBackground=oldBackground;Application.targetFrameRate=oldTarget;QualitySettings.vSyncCount=oldVsync;
            if(battle!=null&&camera!=null){battle.Board.BattleCamera.transform.SetPositionAndRotation(oldCameraPosition,oldCameraRotation);battle.Board.BattleCamera.orthographicSize=oldCameraSize;}
            if(actorRoot!=null)Destroy(actorRoot);if(poolRoot!=null)Destroy(poolRoot);if(camera!=null)Destroy(camera.gameObject);if(renderTarget!=null){renderTarget.Release();Destroy(renderTarget);}if(readback!=null)Destroy(readback);
            CloseCandidatePreview();
        }
        void CloseCandidatePreview()
        {
#if UNITY_EDITOR
            if(candidateAssets==null||candidateCleanupComplete)return;
            if(actorRoot!=null)actorRoot.SetActive(false);
            try{candidateAssets.AssertOriginalsUnchanged();candidateOriginalsUnchanged=true;}
            catch(Exception e){errors.Add("Candidate original preservation: "+e.Message);}
            finally
            {
                try{candidateAssets.Dispose();}catch(Exception e){errors.Add("Candidate clone cleanup: "+e.Message);}
                candidateClonesCreated=candidateAssets.CreatedCount;candidateClonesDestroyed=candidateAssets.DestroyedCount;candidateCleanupComplete=candidateAssets.LiveCount==0;
                if(!candidateCleanupComplete)errors.Add("Transient candidate clones survived cleanup.");
            }
#endif
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
        void OnDestroy(){Application.logMessageReceived-=Log;Restore();}
    }
}
#endif
