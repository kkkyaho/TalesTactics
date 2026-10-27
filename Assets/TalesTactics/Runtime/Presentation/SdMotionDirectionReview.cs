#if UNITY_EDITOR || TALESTACTICS_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;

namespace TalesTactics
{
    // Render-only camera fixture around the existing actual-execution VFX fixture.
    // The production camera continues to drive CharacterMotion direction and combat is unchanged.
    public sealed class SdMotionDirectionReview:MonoBehaviour
    {
        static readonly string[] Skills={"cless.attack","cless.2","cless.ultimate"};
        static readonly string[] Watched={"cless_16_1","cless_18_1","cless_10_2","cless_9_2","cless_11_2","cless_14_2"};
        [Serializable] public sealed class FrameRow
        {
            public int frame,unityFrame,resolutions,alliedModels;public float simulatedSeconds,cameraYaw,orthographicSize;
            public string file,sprite,worldFacing,viewFacing,action;public bool presenting,insideFrame,authoredDirectionSprite;public Rect viewportBounds;
        }
        [Serializable] public sealed class RunRow
        {
            public string name,skill,viewFacing,worldFacing,battlefieldCapture,representativeFrame,expectedStateHash,actualStateHash;
            public int frames,contactFrame,resolutions,alliedModels;public bool finished,clean,resolverMatches,gridMatches,beforeContactUnchanged,unityRandomUnchanged,authoredDirectionMatches,requiredPosesObserved;
            public string[] observedSprites,watchedSprites,authoredDirectionSprites,requiredPoseSprites,missingRequiredPoseSprites,requiredWatchedSprites,errors;public FrameRow[] timeline;
        }
        [Serializable] public sealed class Report
        {
            public string status="running",saveRoot;
            public string scope="12 actual Director actions using the unchanged SkillRosterVfxFixture: Cless attack, cless.2, ultimate, four camera-relative directions. Training levels/placement/resources are staged by that existing fixture; this is not campaign or balance evidence.";
            public string framing="Full battlefield idle reference is 1920x1080. Continuous close-ups are actual Game View renders from a separate camera showing only Cless on a plain background; HUD, other models and VFX are excluded from close-up rendering, but remain in the simulation. No raster editing, compositing, sprite replacement or altered animation timing.";
            public int width,height,fps=30,expectedRuns=12,completedRuns;public bool passed,userFilesUnchanged,isolatedFilesUnchanged;
            public string[] watchedFrames=Watched,observedWatchedFrames,missingWatchedFrames,errors;public RunRow[] runs;
        }
        readonly List<RunRow> runs=new List<RunRow>();readonly List<string> errors=new List<string>();
        string directory,saveRoot;int width=960,height=540,oldRate,oldLayer,reviewLayer;float oldTimeScale;bool oldBackground,finished;
        BattleDirector battle;Camera closeCamera;SpriteRenderer actorRenderer;Behaviour displayController;bool oldDisplayController;readonly Dictionary<Canvas,bool> canvasState=new Dictionary<Canvas,bool>();
        Dictionary<string,string> userFiles,isolatedFiles;
#if UNITY_EDITOR
        UnityEditor.EditorWindow gameView;object sizeGroup;int oldSize=-1;readonly Dictionary<Vector2Int,int> editorSizes=new Dictionary<Vector2Int,int>();
        public static SdMotionDirectionReview BeginEditor(string output,int captureWidth=960)
        {
            if(!Application.isPlaying||FindAnyObjectByType<SdMotionDirectionReview>()!=null)throw new InvalidOperationException("A single active Play Mode review is required.");
            ValidateOutput(output);if(captureWidth!=960&&captureWidth!=1280)throw new ArgumentOutOfRangeException(nameof(captureWidth));
            var review=new GameObject("SD motion direction review").AddComponent<SdMotionDirectionReview>();review.directory=Path.GetFullPath(output);review.width=captureWidth;review.height=captureWidth*9/16;
            UnityEditor.SessionState.SetInt("TalesTactics.SdMotionDirectionReviewExit",-1);return review;
        }
        void SetEditorResolution(int w,int h)
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;var type=assembly.GetType("UnityEditor.GameView");gameView=UnityEditor.EditorWindow.GetWindow(type);
            var selected=type.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);if(oldSize<0)oldSize=(int)selected.GetValue(gameView);
            if(sizeGroup==null){var sizes=assembly.GetType("UnityEditor.GameViewSizes");var instance=sizes.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);sizeGroup=sizes.GetProperty("currentGroup").GetValue(instance);}
            var size=new Vector2Int(w,h);
            if(!editorSizes.TryGetValue(size,out int index))
            {
                var entry=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),w,h,"SD motion review "+size});
                sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup,new[]{entry});index=(int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup,null)-1;editorSizes.Add(size,index);
            }
            selected.SetValue(gameView,index);gameView.Focus();gameView.Repaint();
        }
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--sd-motion-review");if(i<0)return;
            if(i+1>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)){Application.Quit(2);return;}
            string output=Path.Combine(Path.GetDirectoryName(Application.dataPath),"SdMotionReviews",args[i+1]);ValidateOutput(output);
            new GameObject("SD motion direction review").AddComponent<SdMotionDirectionReview>().directory=output;
        }
        static void ValidateOutput(string path)
        {
            string root=Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),user=Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(root.Equals(user,StringComparison.OrdinalIgnoreCase)||root.StartsWith(user+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("User storage cannot be review output.");
            if(Directory.Exists(root)&&Directory.EnumerateFileSystemEntries(root).Any())throw new InvalidOperationException("Use a new empty output directory.");
        }
        IEnumerator Start()
        {
            oldRate=Time.captureFramerate;oldTimeScale=Time.timeScale;oldBackground=Application.runInBackground;Application.runInBackground=true;Time.timeScale=1;
            Directory.CreateDirectory(directory);Application.logMessageReceived+=Log;
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
            yield return null;battle=FindAnyObjectByType<BattleDirector>();if(battle==null||!battle.enabled)throw new InvalidOperationException("Battle scene is not ready.");
            userFiles=Snapshot(Application.persistentDataPath);saveRoot=Path.Combine(directory,"isolated-save");battle.ConfigureStorage(saveRoot);battle.Hud.HideTitle();battle.Audio.PersonalMusicEnabled=false;battle.Audio.SetVolumes(0,0);
            // Resolution switching is review framing, not a saved user display preference.
            displayController=battle.GetComponent<WindowDisplayController>();if(displayController!=null){oldDisplayController=displayController.enabled;displayController.enabled=false;}
            battle.Preferences.TextScale=1;battle.Preferences.Reactions=false;battle.Preferences.MissionEvents=false;battle.Campaign.AutoTiming=false;battle.TrainingMode=true;
            for(int n=1;n<=20;n++)battle.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(n));if(!battle.SavePreparation())throw new InvalidOperationException("Could not seed isolated fixture save.");
            isolatedFiles=Snapshot(saveRoot);Time.captureFramerate=30;SkillRosterVfxFixture.BeginActor(battle,"cless");yield return Settle();
            reviewLayer=Enumerable.Range(8,24).Reverse().First(layer=>!FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(r=>r.gameObject.layer==layer));
            foreach(string id in Skills)foreach(Facing facing in new[]{Facing.Front,Facing.Back,Facing.Left,Facing.Right})
            {
                var entry=SkillRosterVfxFixture.Entries(battle.Catalog,true).Single(e=>e.Skill.Id==id);yield return Sequence(entry.Skill,facing);Write(false);
            }
            Closeup(false);battle.Restart();yield return Settle();if(!SkillRosterVfxFixture.Clean(battle))errors.Add("Effects survived the final restart.");
        }
        IEnumerator Sequence(SkillData skill,Facing facing)
        {
            using(var scenario=new SkillRosterVfxFixture.Scenario(battle,skill,false))
            {
                string name=skill.Id+"-"+facing;string output=Path.Combine(directory,name);Directory.CreateDirectory(output);
                for(int i=0;i<4&&CharacterMotion.ViewFacing(scenario.Actor.Facing,battle.Board.BattleCamera.transform.rotation)!=facing;i++)battle.Board.RotateCamera(90);
                if(CharacterMotion.ViewFacing(scenario.Actor.Facing,battle.Board.BattleCamera.transform.rotation)!=facing)throw new InvalidOperationException("Could not establish view direction "+facing);
                var motion=battle.Board.GetComponentsInChildren<CharacterMotion>().Single(m=>m.Owner==scenario.Actor);actorRenderer=motion.GetComponent<SpriteRenderer>();oldLayer=actorRenderer.gameObject.layer;
                var authored=ActionClip(scenario.Actor,skill).Frames.Select(f=>f?.Get(facing)).Where(s=>s!=null).Distinct().ToArray();
                var required=new HashSet<Sprite>(authored.Select(s=>EquipmentAppearance.Resolve(scenario.Actor,s)));
                var allowed=new HashSet<Sprite>(required);var idle=EquipmentAppearance.Resolve(scenario.Actor,scenario.Actor.Data.Sprites.Get(facing));if(idle!=null){allowed.Add(idle);required.Add(idle);}
                var watched=authored.Where(s=>Watched.Contains(s.name)).ToArray();var observed=new HashSet<Sprite>();
                yield return Resolution(1920,1080);yield return Settle();string battlefield=name+"/battlefield.png";SaveImage(battlefield);
                yield return Resolution(width,height);Closeup(true);FrameCamera(scenario.Actor,motion,facing);yield return Settle();
                var frames=new List<FrameRow>();float began=Time.time;frames.Add(CaptureFrame(name,0,began,scenario,motion,allowed,observed));scenario.Start();double deadline=Time.realtimeSinceStartupAsDouble+120;
                for(int frame=1;frame<=150;frame++)
                {
                    yield return new WaitForEndOfFrame();scenario.Observe();frames.Add(CaptureFrame(name,frame,began,scenario,motion,allowed,observed));
                    if(scenario.Finished&&!motion.Presenting&&frame>=scenario.ContactFrame+18)break;
                    if(Time.realtimeSinceStartupAsDouble>deadline)break;
                }
                var issues=scenario.Problems().ToList();if(!scenario.Finished||motion.Presenting)issues.Add("Actual action/motion did not finish within 150 simulated frames or 120 wall seconds.");
                if(frames.Any(f=>f.viewFacing!=facing.ToString()))issues.Add("Camera-relative facing changed during the action.");
                if(frames.Any(f=>!f.authoredDirectionSprite))issues.Add("Rendered Sprite is not an authored idle/action-clip Sprite for this camera-relative direction after equipment appearance resolution.");
                var missingPoses=required.Except(observed).Select(s=>s.name).ToArray();if(missingPoses.Length>0)issues.Add("Required direction-specific clip/idle poses were not rendered: "+string.Join(", ",missingPoses));
                if(frames.Any(f=>!f.insideFrame))issues.Add("Cless sprite vertices left the close-up camera frame.");
                if(frames.Any(f=>f.alliedModels!=6))issues.Add("The six allied models were not preserved throughout the sequence.");
                var actual=SkillRosterVfxFixture.State(battle.Session.Units);var sprites=frames.Select(f=>f.sprite).Distinct().ToArray();
                var row=new RunRow{name=name,skill=skill.Id,viewFacing=facing.ToString(),worldFacing=scenario.Actor.Facing.ToString(),battlefieldCapture=battlefield,
                    representativeFrame=frames.FirstOrDefault(f=>f.resolutions>0)?.file??frames[0].file,frames=frames.Count,contactFrame=scenario.ContactFrame,resolutions=scenario.Resolutions,
                    alliedModels=SkillRosterVfxFixture.Models(battle),finished=scenario.Finished&&!motion.Presenting,clean=SkillRosterVfxFixture.Clean(battle),resolverMatches=actual==scenario.ExpectedState,
                    gridMatches=SkillRosterVfxFixture.GridState(battle.Session.Grid,battle.Session.Units)==scenario.ExpectedGrid,beforeContactUnchanged=scenario.BeforeContactUnchanged,
                    unityRandomUnchanged=JsonUtility.ToJson(UnityEngine.Random.state)==scenario.InitialRandom,expectedStateHash=SkillRosterVfxFixture.Hash(scenario.ExpectedState),actualStateHash=SkillRosterVfxFixture.Hash(actual),
                    authoredDirectionMatches=frames.All(f=>f.authoredDirectionSprite),requiredPosesObserved=missingPoses.Length==0,
                    authoredDirectionSprites=allowed.Select(s=>s.name).OrderBy(s=>s,StringComparer.Ordinal).ToArray(),requiredPoseSprites=required.Select(s=>s.name).OrderBy(s=>s,StringComparer.Ordinal).ToArray(),missingRequiredPoseSprites=missingPoses,
                    observedSprites=sprites,requiredWatchedSprites=watched.Select(s=>s.name).ToArray(),watchedSprites=watched.Where(s=>observed.Contains(EquipmentAppearance.Resolve(scenario.Actor,s))).Select(s=>s.name).ToArray(),timeline=frames.ToArray(),errors=issues.ToArray()};
                runs.Add(row);File.WriteAllText(Path.Combine(output,"timeline.json"),JsonUtility.ToJson(row,true));errors.AddRange(issues.Select(e=>name+": "+e));Closeup(false);battle.Hud.ClearCombatFeedback();
            }
        }
        static DirectionalSpriteClip ActionClip(UnitRuntime actor,SkillData skill)=>skill.IsUltimate?actor.Data.Poses.Ultimate:
            skill.Animation==AnimationKind.Attack||SkillSignatures.Resolve(skill)==SkillSignature.DemonFang?actor.Data.Poses.AttackMotion:actor.Data.Poses.Skill;
        void FrameCamera(UnitRuntime actor,CharacterMotion motion,Facing facing)
        {
            var source=battle.Board.BattleCamera;closeCamera.CopyFrom(source);closeCamera.enabled=true;closeCamera.depth=source.depth+10;closeCamera.clearFlags=CameraClearFlags.SolidColor;closeCamera.backgroundColor=new Color(.035f,.055f,.085f);closeCamera.cullingMask=1<<reviewLayer;closeCamera.rect=new Rect(0,0,1,1);closeCamera.aspect=(float)width/height;closeCamera.targetTexture=null;
            closeCamera.transform.rotation=source.transform.rotation;var scale=CharacterVisualSizing.Scale(actor.Data);float radius=.8f;
            var poses=actor.Data.Poses;var frames=poses.AttackMotion.Frames.Concat(poses.Skill.Frames).Concat(poses.Ultimate.Frames).Select(f=>f.Get(facing)).Concat(new[]{actor.Data.Sprites.Get(facing)}).Where(s=>s!=null);
            foreach(var sprite in frames)foreach(var vertex in sprite.vertices)radius=Mathf.Max(radius,(vertex*scale-new Vector2(0,.65f)).magnitude);
            closeCamera.orthographicSize=radius+.4f;var center=motion.transform.position+source.transform.up*.65f;closeCamera.transform.position=center-source.transform.forward*15;
        }
        void Closeup(bool active)
        {
            if(active)
            {
                if(closeCamera==null)closeCamera=new GameObject("SD review close-up camera",typeof(Camera)).GetComponent<Camera>();
                canvasState.Clear();foreach(var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None)){canvasState[c]=c.enabled;c.enabled=false;}
                actorRenderer.gameObject.layer=reviewLayer;closeCamera.enabled=true;
            }
            else
            {
                if(closeCamera!=null)closeCamera.enabled=false;if(actorRenderer!=null)actorRenderer.gameObject.layer=oldLayer;
                foreach(var entry in canvasState)if(entry.Key!=null)entry.Key.enabled=entry.Value;canvasState.Clear();
            }
        }
        FrameRow CaptureFrame(string run,int frame,float began,SkillRosterVfxFixture.Scenario scenario,CharacterMotion motion,HashSet<Sprite> authored,HashSet<Sprite> observed)
        {
            observed.Add(actorRenderer.sprite);
            var min=Vector2.one*float.PositiveInfinity;var max=Vector2.one*float.NegativeInfinity;bool inFront=true;
            foreach(var vertex in actorRenderer.sprite.vertices){var p=closeCamera.WorldToViewportPoint(actorRenderer.transform.TransformPoint(vertex));min=Vector2.Min(min,p);max=Vector2.Max(max,p);inFront&=p.z>0;}
            string file=run+"/"+frame.ToString("D4")+".png";SaveImage(file);
            return new FrameRow{frame=frame,unityFrame=Time.frameCount,simulatedSeconds=Time.time-began,file=file,sprite=actorRenderer.sprite.name,worldFacing=scenario.Actor.Facing.ToString(),viewFacing=CharacterMotion.ViewFacing(scenario.Actor.Facing,battle.Board.BattleCamera.transform.rotation).ToString(),
                cameraYaw=closeCamera.transform.eulerAngles.y,orthographicSize=closeCamera.orthographicSize,action=motion.Action.ToString(),presenting=motion.Presenting,resolutions=scenario.Resolutions,alliedModels=SkillRosterVfxFixture.Models(battle),authoredDirectionSprite=authored.Contains(actorRenderer.sprite),viewportBounds=Rect.MinMaxRect(min.x,min.y,max.x,max.y),insideFrame=inFront&&min.x>=0&&min.y>=0&&max.x<=1&&max.y<=1};
        }
        IEnumerator Resolution(int w,int h)
        {
#if UNITY_EDITOR
            SetEditorResolution(w,h);
#else
            Screen.SetResolution(w,h,FullScreenMode.Windowed);
#endif
            yield return new WaitForSecondsRealtime(.25f);yield return Settle();if(Screen.width!=w||Screen.height!=h)throw new InvalidOperationException("Requested Game View "+w+"x"+h+", actual "+Screen.width+"x"+Screen.height);
        }
        static IEnumerator Settle(){yield return null;yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();}
        void SaveImage(string name){var texture=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(Path.Combine(directory,name),texture.EncodeToPNG());}finally{Destroy(texture);}}
        static Dictionary<string,string> Snapshot(string root)
        {
            var files=new Dictionary<string,string>();if(!Directory.Exists(root))return files;
            foreach(var file in Directory.GetFiles(root).Where(p=>Path.GetFileName(p).StartsWith("campaign",StringComparison.OrdinalIgnoreCase)||Path.GetFileName(p).StartsWith("settings",StringComparison.OrdinalIgnoreCase)))using(var sha=SHA256.Create())files[Path.GetFileName(file)]=Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(file)));return files;
        }
        static bool Equal(Dictionary<string,string> a,Dictionary<string,string> b)=>a!=null&&b!=null&&a.Count==b.Count&&a.All(p=>b.TryGetValue(p.Key,out var v)&&v==p.Value);
        void Write(bool final)
        {
            var seen=runs.SelectMany(r=>r.watchedSprites).Distinct().ToArray();var missing=Watched.Except(seen).ToArray();
            var report=new Report{status=final?(errors.Count==0&&runs.Count==12&&missing.Length==0?"passed":"failed"):"running",saveRoot=saveRoot,width=width,height=height,completedRuns=runs.Count,
                userFilesUnchanged=Equal(userFiles,Snapshot(Application.persistentDataPath)),isolatedFilesUnchanged=saveRoot!=null&&Equal(isolatedFiles,Snapshot(saveRoot)),observedWatchedFrames=seen,missingWatchedFrames=missing,runs=runs.ToArray(),errors=errors.ToArray()};
            report.passed=final&&report.status=="passed"&&report.userFilesUnchanged&&report.isolatedFilesUnchanged;File.WriteAllText(Path.Combine(directory,final?"result.json":"progress.json"),JsonUtility.ToJson(report,true));
        }
        void Finish()
        {
            if(finished)return;finished=true;int exit=1;Application.logMessageReceived-=Log;
            try
            {
                if(!Equal(userFiles,Snapshot(Application.persistentDataPath)))errors.Add("User campaign/settings files changed.");if(saveRoot==null||!Equal(isolatedFiles,Snapshot(saveRoot)))errors.Add("Isolated campaign/settings files changed.");
                var missing=Watched.Except(runs.SelectMany(r=>r.watchedSprites)).ToArray();if(missing.Length>0)errors.Add("Required pose frames were not observed: "+string.Join(", ",missing));
                Write(true);File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);exit=errors.Count==0&&runs.Count==12?0:1;
            }
            catch(Exception e){errors.Add("Could not finalize motion review: "+e);Debug.LogError("SD motion review finalization failed: "+e);}
            finally
            {
                try{Restore();}
                catch(Exception e){exit=1;Debug.LogError("SD motion review restoration failed: "+e);}
                finally
                {
#if UNITY_EDITOR
                    try{UnityEditor.SessionState.SetInt("TalesTactics.SdMotionDirectionReviewExit",exit);}finally{UnityEditor.EditorApplication.isPlaying=false;}
#else
                    Application.Quit(exit);
#endif
                }
            }
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
        void Restore()
        {
            try{Closeup(false);}
            finally
            {
                Time.captureFramerate=oldRate;Time.timeScale=oldTimeScale;Application.runInBackground=oldBackground;
                try
                {
                    if(displayController!=null)displayController.enabled=oldDisplayController;
#if UNITY_EDITOR
                    if(gameView!=null&&oldSize>=0){gameView.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(gameView,oldSize);foreach(int index in editorSizes.Values.OrderByDescending(i=>i))sizeGroup.GetType().GetMethod("RemoveCustomSize").Invoke(sizeGroup,new object[]{index});editorSizes.Clear();oldSize=-1;}
#endif
                }
                finally{if(closeCamera!=null)Destroy(closeCamera.gameObject);}
            }
        }
        void OnDestroy(){Application.logMessageReceived-=Log;Restore();}
    }
}
#endif
