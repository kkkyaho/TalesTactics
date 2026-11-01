#if UNITY_EDITOR || TALESTACTICS_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TalesTactics
{
    // Targeted environment/UI evidence, not a campaign-completion or combat-balance test.
    public sealed class EnvironmentIntegrationReview:MonoBehaviour
    {
        public const string Argument="--environment-integration-review";
        [Serializable] public sealed class Model
        {
            public string id,sprite,action,worldFacing,viewFacing,shader;public int level,hp,mp;
            public Vector2Int tile;public Vector3 root,ground,shadowPosition;public Rect viewportBounds;
            public float groundError,pathError;public bool shadow,insideCamera,materialSupported;
        }
        [Serializable] public sealed class MaterialEvidence
        {public string name,shader,mainTexture;public bool supported;public string[] environmentProperties;}
        [Serializable] public sealed class Row
        {
            public string scenario,phase,file,state,fixture;public int episode,stage,width,height,rotation,allies,models,tiles,pickChecks,pickMatches,buttons,texts,terrainMeshes,terrainVertices;
            public float cameraYaw,orthographicSize,textScale,battlefieldScreenFraction;public Vector3 cameraPosition;public Rect battlefieldViewport;
            public Model[] units;public MaterialEvidence[] materials;public string[] issues;
        }
        [Serializable] public sealed class Report
        {
            public int schema=2,expectedCaptures=90,completedCaptures;public bool complete,passed,userFilesUnchanged,campaignsUnchanged=true;
            public string status="Running",startedUtc,finishedUtc,output,storage,unityVersion;public Row[] rows;public string[] errors,userFilesBefore,userFilesAfter;
            public string scope="Eight isolated presentation fixtures spanning all six terrain themes: actual Director opening boards for main episodes 1/11/12/15/16/17/20 with joined allies at authored EntryLevel, plus six-ally Lv25 training. Prior completion flags/levels exist only in new in-memory fixtures. No natural progression, story completion, rewards, AI/balance or full-campaign claim.";
            public string captures="90 unedited ScreenCapture frames: each board idle at 0/90/180/270 degrees, legal Director movement in flight/at arrival, and a damage-pose-only fixture at 1280x720; two additional character focus frames; command and skill-list HUD at 1280x720 and 1024x768. Damage does not execute the resolver or change HP. No effect/skill-execution claim.";
            public string checks="Active model count, authored non-null sprites, finite ground/path placement, contact-shadow presence, supported materials, tile-pick round trips, rotation state/RNG invariance, actual legal movement completion, damage-fixture state invariance, visible UI glyph/button boundaries and exact requested resolution. Sprite AABB camera containment is telemetry only because transparent artwork padding is not visible geometry.";
            public string limitations="Automated pass is not visual approval. Inspect PNGs for palette harmony, terrain occlusion, visible foot anchoring, contact-shadow quality, character/background separation and UI readability. Seven campaign opening boards and one training board are not all maps/poses/resolutions or performance coverage. User-storage hashing is top-level save/settings files; external wrapper may additionally hash the entire tree. No user files are restored.";
        }
        readonly List<Row> rows=new List<Row>();readonly List<string> errors=new List<string>();
        Report report=new Report();BattleDirector battle;string directory,storage,scenario,campaignBefore;int episode,rotation;
        float oldScale;bool oldBackground,initialized,finished;double began;Vector2Int requested;
        UnitRuntime moving;List<Vector2Int> movementPath;const double TimeoutSeconds=180;
#if UNITY_EDITOR
        readonly Dictionary<Vector2Int,int> editorSizes=new Dictionary<Vector2Int,int>();int originalEditorSize=-1;
        public static void BeginEditor(string output)
        {
            if(!Application.isPlaying||FindAnyObjectByType<EnvironmentIntegrationReview>()!=null)throw new InvalidOperationException("One active Play Mode environment review is required.");
            ValidateOutput(output);var r=new GameObject("Environment integration review").AddComponent<EnvironmentIntegrationReview>();r.directory=Path.GetFullPath(output);
            UnityEditor.SessionState.SetInt("TalesTactics.EnvironmentIntegrationReviewExit",-1);
        }
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,Argument);if(index<0)return;
            try
            {
                if(args.Count(a=>a==Argument)!=1||index+1>=args.Length||!Guid.TryParseExact(args[index+1],"N",out var id)||id==Guid.Empty)throw new ArgumentException("Environment review requires one non-empty N-format GUID.");
                string output=Path.Combine(Path.GetDirectoryName(Application.dataPath),"EnvironmentIntegrationReviews",args[index+1]);
                string resolved=ReviewStorageBootstrap.Resolve(args,Application.dataPath,Application.persistentDataPath);
                if(resolved==null||!SamePath(resolved,Path.Combine(output,"Storage")))throw new InvalidOperationException("ReviewStorageBootstrap must isolate environment review storage before Director.Start.");
                ValidateOutput(output);new GameObject("Environment integration review").AddComponent<EnvironmentIntegrationReview>().directory=output;
            }
            catch(Exception e){Debug.LogError("Environment review initialization rejected: "+e);Application.Quit(2);}
        }
        static bool SamePath(string a,string b)=>string.Equals(Path.GetFullPath(a).TrimEnd('\\','/'),Path.GetFullPath(b).TrimEnd('\\','/'),StringComparison.OrdinalIgnoreCase);
        static void ValidateOutput(string path)
        {
            if(string.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path))throw new ArgumentException("Use an absolute, new output directory.");
            string root=Path.GetFullPath(path).TrimEnd('\\','/'),user=Path.GetFullPath(Application.persistentDataPath).TrimEnd('\\','/');
            if(SamePath(root,user)||root.StartsWith(user+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||user.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Output and user storage must not overlap.");
            if(Directory.Exists(root)&&Directory.EnumerateFileSystemEntries(root).Any())throw new InvalidOperationException("Output must be empty; existing review evidence is never overwritten.");
        }
        IEnumerator Start()
        {
            oldScale=Time.timeScale;oldBackground=Application.runInBackground;initialized=true;began=Time.realtimeSinceStartupAsDouble;
            Time.timeScale=1;Application.runInBackground=true;Application.logMessageReceived+=Log;
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0&&!finished)
            {
                object item=null;bool next=false;Exception failure=null;
                try{next=stack.Peek().MoveNext();if(next)item=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){errors.Add(failure.ToString());break;}
                if(!next){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(item is IEnumerator nested)stack.Push(nested);else yield return item;
            }
            while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception e){errors.Add(e.ToString());}}
            Finish();
        }
        void Update(){if(initialized&&!finished&&Time.realtimeSinceStartupAsDouble-began>TimeoutSeconds){errors.Add("Review exceeded 180-second wall-clock limit.");StopAllCoroutines();Finish();}}
        IEnumerator Run()
        {
            Directory.CreateDirectory(directory);storage=Path.Combine(directory,"Storage");
            report.startedUtc=DateTime.UtcNow.ToString("o");report.unityVersion=Application.unityVersion;report.output=directory;report.storage=storage;
            report.userFilesBefore=Snapshot(Application.persistentDataPath);Write();yield return null;
            battle=FindAnyObjectByType<BattleDirector>();Require(battle!=null&&battle.enabled&&battle.Catalog.CrossoverReady,"Current battle scene and catalog are required.");
            if(!Application.isEditor)Require(SamePath(battle.Profiles.Root,storage),"Director used storage outside this isolated run.");
            battle.ConfigureStorage(storage);battle.Audio.PersonalMusicEnabled=false;battle.Audio.SetVolumes(0,0);
            // WindowDisplayController may write only this isolated settings file after resolution changes.
            foreach(int number in new[]{1,11,12,15,16,17,20,0})
            {
                yield return Resolution(new Vector2Int(1280,720));yield return BeginBoard(number);
                string state=SkillRosterVfxFixture.State(battle.Session.Units),grid=SkillRosterVfxFixture.GridState(battle.Session.Grid,battle.Session.Units);int draws=battle.Session.Resolver.RandomDraws;
                for(int r=0;r<4;r++)
                {
                    rotation=r*90;if(r>0)battle.Board.RotateCamera(90);yield return Settle();yield return Capture("idle");
                    Require(state==SkillRosterVfxFixture.State(battle.Session.Units)&&grid==SkillRosterVfxFixture.GridState(battle.Session.Grid,battle.Session.Units)&&draws==battle.Session.Resolver.RandomDraws,"Camera rotation changed gameplay state/RNG.");
                }
                rotation=0;battle.Board.ResetCamera();
                if(number==1||number==0){battle.Board.FocusCurrent();yield return Settle();yield return Capture("character-close");battle.Board.ResetCamera();}
                yield return Move();yield return DamagePose();
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1024,768)})
                {
                    yield return Resolution(size);battle.SetState(new CommandState(battle));battle.Board.ResetCamera();yield return Settle();yield return Capture("command-ui");
                    battle.SkillCommand();Require(battle.State is ActionSelectionState,"Skill menu did not open.");yield return Settle();yield return Capture("skill-ui");battle.SetState(new CommandState(battle));
                }
                bool unchanged=campaignBefore==JsonUtility.ToJson(battle.Campaign);report.campaignsUnchanged&=unchanged;Require(unchanged,"Presentation fixture changed its campaign profile.");
            }
            report.complete=rows.Count==report.expectedCaptures;
        }
        IEnumerator BeginBoard(int number)
        {
            battle.Restart();yield return null;episode=number;scenario=number==0?"training-six":"episode-"+number.ToString("00");rotation=0;
            battle.Hud.HideTitle();battle.TrainingMode=number==0;battle.TrainingObjective=ObjectiveKind.Eliminate;
            battle.SelectedStage=CrossoverCampaign.FirstStage+Mathf.Max(0,number-1);battle.Campaign=new CampaignSave{SelectedStage=battle.SelectedStage};
            for(int n=1;n<number;n++)battle.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(n));
            int level=number==0?25:CampaignStages.Get(battle.SelectedStage).EntryLevel;
            foreach(var data in battle.Catalog.Characters)battle.Campaign.Get(data.Id).Level=level;
            battle.Deployment.Clear();battle.Deployment.AddRange(Enumerable.Range(0,battle.Catalog.Characters.Length).Where(battle.CanDeploy).Take(6));
            battle.Preferences.SeenContextHints=7;battle.Preferences.Reactions=false;battle.Preferences.MissionEvents=false;battle.Campaign.AutoTiming=false;
            battle.PersistCampaign=save=>throw new InvalidOperationException("Environment review must not save campaign fixtures.");
            int count=battle.Deployment.Count;battle.BeginBattle();Require(battle.Session!=null&&battle.IsPlayerCommand,"Director opening did not reach an allied command state.");
            Require(count==(number==1?2:6)&&battle.Session.Units.Count(u=>u.Team==Team.Player)==count,"Unexpected joined/deployed ally count.");
            battle.StopAllCoroutines();campaignBefore=JsonUtility.ToJson(battle.Campaign);yield return Settle();
        }
        IEnumerator Move()
        {
            moving=battle.Session.Active;movementPath=battle.Session.Grid.Reachable(moving,out _).Keys.Select(p=>battle.Session.Grid.Path(moving,p)).Where(p=>p.Count>=2)
                .OrderBy(p=>Mathf.Abs(p.Count-4)).ThenBy(p=>p.Last().x).ThenBy(p=>p.Last().y).FirstOrDefault();
            Require(movementPath!=null,"No legal movement path is available.");var destination=movementPath.Last();
            battle.MoveCommand();battle.StartCoroutine(battle.MoveUnit(destination));
            double until=Time.realtimeSinceStartupAsDouble+10;
            yield return null;Require(battle.State is ActionExecutionState,"Legal move did not enter its animated state.");yield return Capture("move-mid");
            while(battle.State is ActionExecutionState&&Time.realtimeSinceStartupAsDouble<until)yield return null;
            Require(battle.IsPlayerCommand&&moving.Moved&&moving.Position==destination,"Legal Director move did not complete within ten seconds.");
            moving=null;movementPath=null;yield return Settle();yield return Capture("move-arrived");
        }
        IEnumerator DamagePose()
        {
            var unit=battle.Session.Active;var motion=Motion(unit);string state=SkillRosterVfxFixture.State(battle.Session.Units),grid=SkillRosterVfxFixture.GridState(battle.Session.Grid,battle.Session.Units);int draws=battle.Session.Resolver.RandomDraws;
            battle.Board.SetAnimation(unit,AnimationKind.Damage);motion.Impact(Vector3.back,false);yield return null;yield return Capture("damage-pose-only");
            Require(state==SkillRosterVfxFixture.State(battle.Session.Units)&&grid==SkillRosterVfxFixture.GridState(battle.Session.Grid,battle.Session.Units)&&draws==battle.Session.Resolver.RandomDraws,"Damage presentation mutated gameplay state/RNG.");
            battle.RefreshViews();yield return Settle();
        }
        CharacterMotion Motion(UnitRuntime unit)=>battle.Board.GetComponentsInChildren<CharacterMotion>(true).Single(m=>m.Owner==unit);
        static IEnumerator Settle(){yield return null;yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();}
        IEnumerator Resolution(Vector2Int size)
        {
            requested=size;
#if UNITY_EDITOR
            SetEditorResolution(size);
#else
            Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
#endif
            double until=Time.realtimeSinceStartupAsDouble+10;
            while((Screen.width!=size.x||Screen.height!=size.y)&&Time.realtimeSinceStartupAsDouble<until)yield return null;
            Require(Screen.width==size.x&&Screen.height==size.y,"Requested resolution was not applied within ten seconds: "+size);yield return Settle();
        }
        IEnumerator Capture(string phase)
        {
            Require(rows.Count<report.expectedCaptures,"Capture budget exceeded.");float prior=Time.timeScale;Time.timeScale=0;
            try
            {
                Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();var camera=battle.Board.BattleCamera;var issues=new List<string>();
                var units=battle.Session.Units.Where(u=>u.Alive&&!u.CrossoverReserve).ToArray();var models=new List<Model>();
                var active=battle.Board.GetComponentsInChildren<CharacterMotion>().Where(m=>m.Owner!=null&&m.Owner.Alive&&!m.Owner.CrossoverReserve).ToArray();
                if(active.Length!=units.Length)issues.Add("Active model count differs from alive deployed units.");
                foreach(var u in units)
                {
                    var motion=active.SingleOrDefault(m=>m.Owner==u);if(motion==null){issues.Add("Missing model: "+u.Data.Id);continue;}
                    var sprite=motion.GetComponent<SpriteRenderer>();var root=motion.transform.parent.parent;var ground=battle.Session.Grid[u.Position].WorldPosition(battle.Catalog.Rules.TileHeight);var shadow=root.Find("Contact shadow");
                    float groundError=Vector3.Distance(root.position,ground),pathError=0;
                    if(u==moving&&movementPath!=null)pathError=Enumerable.Range(1,movementPath.Count-1).Min(i=>SegmentDistance(root.position,battle.Session.Grid[movementPath[i-1]].WorldPosition(battle.Catalog.Rules.TileHeight),battle.Session.Grid[movementPath[i]].WorldPosition(battle.Catalog.Rules.TileHeight)));
                    if(!Finite(root.position)||!Finite(sprite.bounds.center)||sprite.sprite==null)issues.Add("Invalid model geometry/sprite: "+u.Data.Id);
                    if(u==moving?pathError>.025f:groundError>.025f)issues.Add("Model detached from ground/path: "+u.Data.Id);
                    if(shadow==null||!shadow.gameObject.activeInHierarchy)issues.Add("Missing live contact shadow: "+u.Data.Id);
                    var material=sprite.sharedMaterial;if(material==null||material.shader==null||!material.shader.isSupported)issues.Add("Missing/unsupported unit material: "+u.Data.Id);
                    Rect projected=Project(camera,sprite.bounds);models.Add(new Model{id=u.Data.Id,sprite=sprite.sprite?.name,action=motion.Action.ToString(),worldFacing=u.Facing.ToString(),viewFacing=CharacterMotion.ViewFacing(u.Facing,camera.transform.rotation).ToString(),level=u.Level,hp=u.CurrentHP,mp=u.CurrentMP,tile=u.Position,root=root.position,ground=ground,groundError=groundError,pathError=pathError,shadow=shadow!=null&&shadow.gameObject.activeInHierarchy,shadowPosition=shadow==null?Vector3.zero:shadow.position,viewportBounds=projected,insideCamera=projected.xMin>=0&&projected.yMin>=0&&projected.xMax<=1&&projected.yMax<=1,shader=material?.shader?.name,materialSupported=material!=null&&material.shader!=null&&material.shader.isSupported});
                }
                int picks=0,matches=0;
                foreach(var tile in battle.Session.Grid.Tiles.Values.Where(t=>t.Walkable).OrderBy(t=>t.Coordinate.x).ThenBy(t=>t.Coordinate.y).Where((t,i)=>i%11==0).Take(16))
                {
                    var screen=camera.WorldToScreenPoint(tile.WorldPosition(battle.Catalog.Rules.TileHeight));if(screen.z<=0||!camera.pixelRect.Contains(screen))continue;picks++;
                    if(battle.Board.Pick(screen,out var hit)&&hit==tile.Coordinate)matches++;else issues.Add("Tile pick mismatch: "+tile.Coordinate);
                }
                CheckUi(issues,out int buttons,out int texts);if(Screen.width!=requested.x||Screen.height!=requested.y)issues.Add("Capture resolution mismatch.");
                var materials=battle.Board.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().Select(MaterialInfo).ToArray();
                foreach(var mat in materials)if(!mat.supported)issues.Add("Unsupported material: "+mat.name);
                var terrain=battle.Board.GetComponentInChildren<PixelBattlefield>();
                var terrainGeometry=terrain==null?Array.Empty<MeshFilter>():terrain.GetComponentsInChildren<MeshFilter>();
                var row=new Row{terrainMeshes=terrainGeometry.Length,terrainVertices=terrainGeometry.Sum(f=>f.sharedMesh==null?0:f.sharedMesh.vertexCount),scenario=scenario,episode=episode,stage=battle.Session.CampaignStage,phase=phase,fixture=phase=="damage-pose-only"?"Presentation API only; no damage applied.":"Actual Director opening and legal movement; seeded isolated profile.",width=Screen.width,height=Screen.height,rotation=rotation,cameraYaw=camera.transform.eulerAngles.y,cameraPosition=camera.transform.position,orthographicSize=camera.orthographicSize,textScale=battle.Preferences.TextScale,battlefieldViewport=battle.Hud.BattlefieldViewport,battlefieldScreenFraction=battle.Hud.BattlefieldViewport.width*battle.Hud.BattlefieldViewport.height,state=battle.State.GetType().Name,allies=units.Count(u=>u.Team==Team.Player),models=active.Length,tiles=battle.Session.Grid.Tiles.Count,pickChecks=picks,pickMatches=matches,buttons=buttons,texts=texts,units=models.ToArray(),materials=materials,issues=issues.ToArray()};
                row.file=$"{rows.Count:D2}-{scenario}-{phase}-{Screen.width}x{Screen.height}-r{rotation}.png";
                var image=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(Path.Combine(directory,row.file),image.EncodeToPNG());}finally{Destroy(image);}
                rows.Add(row);Write();
            }
            finally{Time.timeScale=prior;}
        }
        static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b){var d=b-a;float t=d.sqrMagnitude>.00001f?Mathf.Clamp01(Vector3.Dot(p-a,d)/d.sqrMagnitude):0;return Vector3.Distance(p,a+d*t);}
        static bool Finite(Vector3 v)=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z);
        static Rect Project(Camera camera,Bounds b)
        {var p=Enumerable.Range(0,8).Select(i=>camera.WorldToViewportPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)))).ToArray();return Rect.MinMaxRect(p.Min(v=>v.x),p.Min(v=>v.y),p.Max(v=>v.x),p.Max(v=>v.y));}
        static MaterialEvidence MaterialInfo(Material material)
        {
            var values=new List<string>();var shader=material.shader;if(shader==null)return new MaterialEvidence{name=material.name,supported=false,environmentProperties=Array.Empty<string>()};
            foreach(string name in new[]{"_EnvironmentTint","_EnvironmentColor","_AmbientTint","_GroundTint","_ShadowColor","_EnvironmentLight","_ArtSaturation","_ArtContrast","_CanopyShade","_KeyColor","_FillColor","_DetailContrast","_ShadowCount"})
            {
                if(!material.HasProperty(name))continue;int index=shader.FindPropertyIndex(name);if(index<0)continue;
                var kind=shader.GetPropertyType(index);values.Add(name+"="+(kind==UnityEngine.Rendering.ShaderPropertyType.Color?material.GetColor(name).ToString():kind==UnityEngine.Rendering.ShaderPropertyType.Vector?material.GetVector(name).ToString():material.GetFloat(name).ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
            }
            return new MaterialEvidence{name=material.name,mainTexture=material.mainTexture==null?null:material.mainTexture.name,shader=shader==null?null:shader.name,supported=shader!=null&&shader.isSupported,environmentProperties=values.ToArray()};
        }
        static bool Visible(Graphic g)=>g.isActiveAndEnabled&&g.canvasRenderer.GetInheritedAlpha()>.01f&&g.color.a>.01f;
        static bool InScroll(Transform t)=>t.GetComponentInParent<ScrollRect>()!=null;
        void CheckUi(List<string> issues,out int buttons,out int texts)
        {
            var screen=new Rect(-1,-1,Screen.width+2,Screen.height+2);buttons=0;texts=0;
            foreach(var b in battle.Hud.GetComponentsInChildren<Button>().Where(b=>b.IsInteractable()))
            {
                buttons++;if(InScroll(b.transform))continue;var corners=new Vector3[4];((RectTransform)b.transform).GetWorldCorners(corners);
                if(corners.Any(p=>!screen.Contains(RectTransformUtility.WorldToScreenPoint(null,p))))issues.Add("Button outside screen: "+b.name);
            }
            foreach(var text in battle.Hud.GetComponentsInChildren<TMP_Text>().Where(Visible))
            {
                texts++;if(InScroll(text.transform))continue;text.ForceMeshUpdate();if(text.isTextOverflowing)issues.Add("Text overflowing: "+text.name);
                var button=text.GetComponentInParent<Button>();var frame=text.GetComponentInParent<FramedContent>();
                foreach(var glyph in text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(g=>g.isVisible))
                {
                    var lo=text.transform.TransformPoint(glyph.bottomLeft);var hi=text.transform.TransformPoint(glyph.topRight);
                    if(!screen.Contains(RectTransformUtility.WorldToScreenPoint(null,lo))||!screen.Contains(RectTransformUtility.WorldToScreenPoint(null,hi))){issues.Add("Glyph outside screen: "+text.name);break;}
                    if(button!=null){var rect=(RectTransform)button.transform;var allowed=rect.rect;allowed.xMin-=1;allowed.xMax+=1;allowed.yMin-=1;allowed.yMax+=1;if(!allowed.Contains(rect.InverseTransformPoint(lo))||!allowed.Contains(rect.InverseTransformPoint(hi))){issues.Add("Glyph outside button: "+button.name);break;}}
                    if(frame!=null&&frame.GetComponent<Image>()?.enabled==true){var allowed=frame.ContentRect;allowed.xMin-=2;allowed.xMax+=2;allowed.yMin-=2;allowed.yMax+=2;if(!allowed.Contains(frame.transform.InverseTransformPoint(lo))||!allowed.Contains(frame.transform.InverseTransformPoint(hi))){issues.Add("Glyph outside frame: "+frame.name);break;}}
                }
            }
        }
        static string[] Snapshot(string root)
        {
            if(!Directory.Exists(root))return Array.Empty<string>();using(var sha=SHA256.Create())return Directory.GetFiles(root).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).Select(p=>Path.GetFileName(p)+":"+BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant()).ToArray();
        }
        void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        void Write(){report.rows=rows.ToArray();report.errors=errors.ToArray();report.completedCaptures=rows.Count;File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(report,true));}
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
        void Finish()
        {
            if(finished)return;finished=true;Application.logMessageReceived-=Log;int exit=1;
            try
            {
                battle?.StopAllCoroutines();report.finishedUtc=DateTime.UtcNow.ToString("o");report.userFilesAfter=Snapshot(Application.persistentDataPath);
                report.userFilesUnchanged=report.userFilesBefore!=null&&report.userFilesBefore.SequenceEqual(report.userFilesAfter);if(!report.userFilesUnchanged)errors.Add("User save/settings files changed; this review did not restore them.");
                report.passed=report.complete&&report.userFilesUnchanged&&report.campaignsUnchanged&&errors.Count==0&&rows.All(r=>r.issues.Length==0);report.status=report.passed?"Passed":"Stopped or failed";Write();exit=report.passed?0:1;
            }
            catch(Exception e){Debug.LogError("Environment review finalization failed: "+e);}
            finally
            {
                Time.timeScale=oldScale;Application.runInBackground=oldBackground;
#if UNITY_EDITOR
                try{RestoreEditorResolution();}catch(Exception e)
                {
                    exit=1;report.passed=false;report.status="Stopped or failed";errors.Add("Environment review resolution restore failed: "+e);
                    try{Write();}catch(Exception writeError){Debug.LogError("Environment review failure report could not be written: "+writeError);}
                    Debug.LogError("Environment review resolution restore failed: "+e);
                }
                finally{UnityEditor.SessionState.SetInt("TalesTactics.EnvironmentIntegrationReviewExit",exit);UnityEditor.EditorApplication.isPlaying=false;}
#else
                Application.Quit(exit);
#endif
            }
        }
        void OnDestroy(){Application.logMessageReceived-=Log;if(initialized){Time.timeScale=oldScale;Application.runInBackground=oldBackground;}}
#if UNITY_EDITOR
        void SetEditorResolution(Vector2Int size)
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;var type=assembly.GetType("UnityEditor.GameViewSizes");var instance=type.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);var group=type.GetProperty("currentGroup").GetValue(instance);
            var view=assembly.GetType("UnityEditor.GameView");var window=UnityEditor.EditorWindow.GetWindow(view);var selected=view.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);if(originalEditorSize<0)originalEditorSize=(int)selected.GetValue(window);
            if(!editorSizes.TryGetValue(size,out int index)){var entry=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),size.x,size.y,"Environment review "+size});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{entry});index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;editorSizes.Add(size,index);}
            selected.SetValue(window,index);window.Focus();window.Repaint();
        }
        void RestoreEditorResolution()
        {
            if(editorSizes.Count==0)return;var assembly=typeof(UnityEditor.Editor).Assembly;var type=assembly.GetType("UnityEditor.GameViewSizes");var instance=type.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);var group=type.GetProperty("currentGroup").GetValue(instance);var view=assembly.GetType("UnityEditor.GameView");var window=UnityEditor.EditorWindow.GetWindow(view);
            if(originalEditorSize>=0)view.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(window,originalEditorSize);foreach(int index in editorSizes.Values.OrderByDescending(i=>i))group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{index});editorSizes.Clear();
        }
#endif
    }
}
#endif
