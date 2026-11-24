using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TalesTactics.Editor
{
    // Captures a presentation-only actor on the already opened episode-six board.
    // The caller owns storage isolation and the scene's lifecycle; this never starts a battle.
    public static class VisualPilotCapture
    {
        const int Width=960,Height=540;
        const float Interval=1f/12f;
        static Run active;
        public static bool Running=>active!=null;

        [Serializable] public sealed class Frame
        {
            public string file,phase,viewFacing,worldFacing,sprite,texture;
            public int frame,width,height;public double realtime;public float time,elapsed,pixelsPerUnit;
            public Rect rect;public Vector2 pivot;public Vector3 rootPosition,localPosition,worldPosition;
            public Vector3 cameraPosition,cameraEuler;public float cameraSize;
            public bool presenting,fullBoard,blinkSupplement;
        }
        [Serializable] public sealed class Result
        {
            public int schema=1,campaignStage,pngCount,discardedColdFrames,targetFramesPerSecond=12,originalAllies,originalModels;
            public float timeScale;public int captureFramerate;
            public bool pilot,complete,passed,sessionUnchanged,gridUnchanged,campaignUnchanged,randomUnchanged,
                contentUnchanged,cameraRestored,temporaryObjectsRemoved;
            public string status="running",unityVersion,sessionBefore,sessionAfter,gridBefore,gridAfter,
                campaignBefore,campaignAfter,randomBefore,randomAfter;
            public string scope="An isolated base-weapon Alphen actor on the existing episode-six battlefield. Four directions of six-second idle, one basic-attack presentation and one damage presentation. No combat command, damage calculation, save write or gameplay RNG.";
            public string limitation="Actual advancing Unity frames. PNG readback/encoding and Editor overhead alter wall cadence; target 12 fps is not a performance measurement or Android-device result. Closeups use the original BattleCamera and omit overlay UI; full-board ScreenCapture includes the current HUD. Visual quality requires human review.";
            public string[] errors,closedIdleDirections;public Frame[] frames;
        }

        public static string Begin(string output,bool pilot)
        {
            if(active!=null)throw new InvalidOperationException("A visual pilot capture is already running.");
            if(!Application.isPlaying||EditorApplication.isPaused||Time.timeScale<=0)
                throw new InvalidOperationException("An unpaused Play Mode battle is required.");
            var battle=Object.FindAnyObjectByType<BattleDirector>();
            if(battle?.Session==null||battle.Board?.BattleCamera==null||battle.Session.CampaignStage!=11||
                !(battle.State is CommandState)||battle.StoryActive)
                throw new InvalidOperationException("Open episode six (CampaignStage 11) at a stable player command before capture.");
            if(pilot&&Resources.Load<AlphenVisualPilotMotion>(AlphenVisualPilotMotion.ResourcePath)==null)
                throw new InvalidOperationException("Import the new pilot motion asset before the after capture.");
            ValidateOutput(output);
            var run=new Run(battle,Path.GetFullPath(output),pilot);
            active=run;
            try
            {
                run.Open();SessionState.SetInt("TalesTactics.VisualPilotCaptureExit",-1);EditorApplication.update+=Watchdog;
                EditorApplication.playModeStateChanged+=PlayModeChanged;
                AssemblyReloadEvents.beforeAssemblyReload+=AbortForReload;
                run.coroutine=battle.Board.StartCoroutine(Guarded(run));
            }
            catch(Exception e){Finish(run,e);throw;}
            return Path.Combine(run.directory,"result.json");
        }

        public static void Cancel()
        {
            if(active==null)return;
            var run=active;if(run.battle?.Board!=null&&run.coroutine!=null)run.battle.Board.StopCoroutine(run.coroutine);
            Finish(run,new OperationCanceledException("Capture cancelled; temporary presentation objects were removed."));
        }
        static void Watchdog()
        {
            if(active!=null&&EditorApplication.timeSinceStartup-active.began>180)
            {var run=active;if(run.battle?.Board!=null&&run.coroutine!=null)run.battle.Board.StopCoroutine(run.coroutine);Finish(run,new TimeoutException("Capture exceeded its 180-second wall-clock bound."));}
        }
        static void PlayModeChanged(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)Cancel();}
        static void AbortForReload()=>Cancel();
        static IEnumerator Guarded(Run run)
        {
            var stack=new Stack<IEnumerator>();stack.Push(run.Capture());
            Exception failure=null;
            while(stack.Count>0)
            {
                object next=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}
                catch(Exception e){failure=e;break;}
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(next is IEnumerator nested){stack.Push(nested);continue;}
                yield return next;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
            Finish(run,failure);
        }
        static void Finish(Run run,Exception failure)
        {
            if(run.finished)return;run.finished=true;
            EditorApplication.update-=Watchdog;EditorApplication.playModeStateChanged-=PlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload-=AbortForReload;
            try{run.Finish(failure);}
            finally{if(active==run)active=null;}
        }
        static void ValidateOutput(string output)
        {
            if(string.IsNullOrWhiteSpace(output)||!Path.IsPathRooted(output))throw new ArgumentException("Use a new absolute output directory.");
            string path=Path.GetFullPath(output).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            string user=Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(path.Equals(user,StringComparison.OrdinalIgnoreCase)||path.StartsWith(user+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||
                user.StartsWith(path+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Capture output must not overlap player storage.");
            if(Directory.Exists(path)&&Directory.EnumerateFileSystemEntries(path).Any())throw new InvalidOperationException("Existing capture evidence is never overwritten.");
        }

        sealed class Run
        {
            public readonly BattleDirector battle;public readonly string directory;public readonly double began=EditorApplication.timeSinceStartup;
            public Coroutine coroutine;public bool finished;
            readonly bool pilot;readonly Camera camera;readonly Result report;
            readonly List<Frame> frames=new List<Frame>();readonly List<string> errors=new List<string>();
            readonly HashSet<string> closed=new HashSet<string>();
            readonly Vector3 cameraPosition;readonly Quaternion cameraRotation;readonly float cameraSize,cameraAspect;
            readonly Rect cameraRect;readonly RenderTexture cameraTarget;
            readonly BattleSession session;readonly string state,grid,campaign,random,sourceData;
            readonly int randomDraws;readonly CharacterData original;
            GameObject actorRoot;CharacterData data;UnitRuntime actor;CharacterMotion motion;SpriteRenderer sprite;
            RenderTexture target;Texture2D readback;FileStream outputLock;string lockPath;
            int lastFrame=-1;bool complete,ownsOutput;
            public Run(BattleDirector b,string output,bool usePilot)
            {
                battle=b;directory=output;pilot=usePilot;camera=b.Board.BattleCamera;session=b.Session;
                original=b.Catalog.Characters.Single(c=>c.Id=="alphen");sourceData=EditorJsonUtility.ToJson(original);
                state=SkillRosterVfxFixture.State(session.Units);grid=SkillRosterVfxFixture.GridState(session.Grid,session.Units);
                campaign=JsonUtility.ToJson(b.Campaign);random=JsonUtility.ToJson(UnityEngine.Random.state);randomDraws=session.Resolver.RandomDraws;
                cameraPosition=camera.transform.position;cameraRotation=camera.transform.rotation;cameraSize=camera.orthographicSize;
                cameraAspect=camera.aspect;cameraRect=camera.rect;cameraTarget=camera.targetTexture;
                report=new Result{pilot=pilot,campaignStage=session.CampaignStage,unityVersion=Application.unityVersion,
                    originalAllies=session.Units.Count(u=>u.Team==Team.Player),originalModels=SkillRosterVfxFixture.Models(b),
                    timeScale=Time.timeScale,captureFramerate=Time.captureFramerate,
                    sessionBefore=SkillRosterVfxFixture.Hash(state),gridBefore=SkillRosterVfxFixture.Hash(grid),
                    campaignBefore=SkillRosterVfxFixture.Hash(campaign),randomBefore=SkillRosterVfxFixture.Hash(random)};
            }
            public void Open()
            {
                Directory.CreateDirectory(directory);lockPath=Path.Combine(directory,"capture.lock");
                outputLock=new FileStream(lockPath,FileMode.CreateNew,FileAccess.Write,FileShare.None);ownsOutput=true;
                if(Directory.EnumerateFileSystemEntries(directory).Count()!=1)throw new IOException("Capture directory changed while acquiring its lock.");
                Write(false);
            }
            public IEnumerator Capture()
            {
                var live=battle.Board.GetComponentsInChildren<CharacterMotion>().FirstOrDefault(m=>m.Owner?.Data?.Id=="alphen")??
                    battle.Board.GetComponentsInChildren<CharacterMotion>().FirstOrDefault();
                if(live==null||live.GetComponent<SpriteRenderer>()?.sharedMaterial==null)throw new InvalidOperationException("The existing board has no live sprite material.");
                var candidates=session.Grid.Tiles.Values.Where(t=>t.Walkable&&t.Occupant==null&&t.Terrain!=TerrainType.Water)
                    .Select(t=>new{tile=t,p=camera.WorldToViewportPoint(t.WorldPosition(battle.Catalog.Rules.TileHeight)+Vector3.up*.6f),
                        clearance=session.Units.Min(u=>Vector2.Distance((Vector2)t.Coordinate,(Vector2)u.Position))})
                    .Where(x=>x.p.z>0&&x.p.x>.18f&&x.p.x<.82f&&x.p.y>.2f&&x.p.y<.8f)
                    // Keep the comparison actor apart from the actual party. Two Alphen
                    // drawings in one closeup obscure which animation is being reviewed.
                    .OrderByDescending(x=>Mathf.Min(x.clearance,3.5f))
                    .ThenBy(x=>Vector2.SqrMagnitude(new Vector2(x.p.x-.5f,x.p.y-.48f)))
                    .ThenBy(x=>x.tile.Coordinate.x).ThenBy(x=>x.tile.Coordinate.y).ToArray();
                if(candidates.Length==0)throw new InvalidOperationException("No visible vacant tile is available for the isolated actor.");
                var floor=candidates[0].tile;
                data=Object.Instantiate(original);data.name="Temporary Alphen visual pilot capture";
                if(data.Animator!=null)throw new InvalidOperationException("Animator-authored data needs a separate capture.");
                actor=new UnitRuntime(data,Team.Player,battle.Catalog.Rules){Position=floor.Coordinate};
                actorRoot=new GameObject("Temporary isolated Alphen capture");actorRoot.transform.position=floor.WorldPosition(battle.Catalog.Rules.TileHeight);
                var scale=new GameObject("Body Scale");scale.transform.SetParent(actorRoot.transform,false);
                scale.transform.localScale=Vector3.one*CharacterVisualSizing.Scale(data);
                var body=new GameObject("Directional Sprite");body.transform.SetParent(scale.transform,false);
                sprite=body.AddComponent<SpriteRenderer>();sprite.sharedMaterial=live.GetComponent<SpriteRenderer>().sharedMaterial;StandingSpriteDepth.Configure(sprite);
                motion=body.AddComponent<CharacterMotion>();motion.Initialize(sprite,actor,camera,pilot);
                var contactObject=new GameObject("Contact shadow");contactObject.transform.SetParent(actorRoot.transform,false);
                var contact=contactObject.AddComponent<GroundOverlay>();contact.Initialize(sprite.sharedMaterial,-3);
                var scenery=battle.Board.GetComponentInChildren<PixelBattlefield>();
                actorRoot.AddComponent<GroundedUnitVisual>().Initialize(sprite,contact,actor,scenery,session.CampaignStage);
                target=new RenderTexture(Width,Height,24,RenderTextureFormat.ARGB32);target.Create();
                readback=new Texture2D(Width,Height,TextureFormat.RGBA32,false);

                foreach(Facing view in new[]{Facing.Front,Facing.Back,Facing.Left,Facing.Right})
                {
                    actor.Facing=Enum.GetValues(typeof(Facing)).Cast<Facing>().Single(f=>CharacterMotion.ViewFacing(f,camera.transform.rotation)==view);
                    motion.Set(AnimationKind.Guard);motion.Set(AnimationKind.Idle);
                    yield return ColdFrames();
                    float start=Time.time,next=0;
                    Save(view+"/fullboard.png","idle",start,true,false);
                    int sequence=0;
                    while(Time.time-start<6f)
                    {
                        yield return new WaitForEndOfFrame();EnsureAdvanced();
                        if(Time.time-start+0.0001f<next)continue;
                        Save(view+"/idle-"+sequence.ToString("D3")+".png","idle",start,false,false);
                        sequence++;next=Mathf.Max(next+Interval,Time.time-start+Interval);
                    }
                    // If PNG IO skipped a 100ms blink, observe the next actual cycle without
                    // readback until that pose is visible. Never move the animation clock.
                    if(pilot&&!closed.Contains(view.ToString()))
                    {
                        float until=Time.time+AlphenVisualPilotMotion.IdleCycleSeconds+.5f;
                        while(Time.time<until&&!closed.Contains(view.ToString()))
                        {
                            yield return new WaitForEndOfFrame();EnsureAdvanced();
                            if(IsClosed(view))Save(view+"/idle-blink-supplement.png","idle",start,false,true);
                        }
                        if(!closed.Contains(view.ToString()))throw new InvalidOperationException("No actual closed-eye frame was captured for "+view);
                    }
                    CheckSession();Write(false);
                }
                actor.Facing=Enum.GetValues(typeof(Facing)).Cast<Facing>().Single(f=>CharacterMotion.ViewFacing(f,camera.transform.rotation)==Facing.Front);
                motion.Set(AnimationKind.Idle);yield return ColdFrames();
                var skill=data.BasicAttack;float prepare=BoardView.Windup(skill),recover=BoardView.Recovery(skill);
                motion.BeginSkill(skill,prepare,recover);
                float attackStart=Time.time,nextAttack=0;bool released=false;int attackIndex=0;
                while(Time.time-attackStart<prepare+recover+.2f)
                {
                    yield return new WaitForEndOfFrame();EnsureAdvanced();
                    float elapsed=Time.time-attackStart;
                    if(!released&&elapsed>=prepare){motion.ReleaseSkill();released=true;yield return new WaitForEndOfFrame();EnsureAdvanced();}
                    if(Time.time-attackStart+0.0001f<nextAttack)continue;
                    Save("Front/attack-"+attackIndex.ToString("D3")+".png",released?"attack-release":"attack-prepare",attackStart,false,false);
                    attackIndex++;nextAttack=Time.time-attackStart+Interval;
                }
                if(motion.Presenting)throw new InvalidOperationException("Basic-attack presentation did not return to idle.");
                motion.Set(AnimationKind.Damage);motion.Impact(Vector3.back,false);
                float damageStart=Time.time,nextDamage=0;int damageIndex=0;
                while(Time.time-damageStart<.45f)
                {
                    yield return new WaitForEndOfFrame();EnsureAdvanced();
                    if(Time.time-damageStart+0.0001f<nextDamage)continue;
                    Save("Front/damage-"+damageIndex.ToString("D3")+".png","damage",damageStart,false,false);
                    damageIndex++;nextDamage=Time.time-damageStart+Interval;
                }
                motion.Set(AnimationKind.Idle);yield return ColdFrames();
                if(pilot)
                {
                    foreach(int index in new[]{0,1,2})if(!frames.Any(f=>f.sprite=="alphen-pilot-attack-front-"+index))
                        throw new InvalidOperationException("A real basic-attack pose was not captured: "+index);
                    if(!frames.Any(f=>f.sprite=="alphen-pilot-damage-front-0"))throw new InvalidOperationException("No real damage pose was captured.");
                }
                Save("Front/fullboard-final.png","idle",Time.time,true,false);
                CheckSession();complete=true;
            }
            IEnumerator ColdFrames()
            {
                for(int i=0;i<2;i++){yield return new WaitForEndOfFrame();EnsureAdvanced();report.discardedColdFrames++;}
            }
            void EnsureAdvanced()
            {
                if(Time.frameCount<=lastFrame)throw new InvalidOperationException("A capture sample did not advance an actual Unity frame.");
                lastFrame=Time.frameCount;
            }
            bool IsClosed(Facing view)=>sprite.sprite!=null&&sprite.sprite.name=="alphen-pilot-idle-"+view.ToString().ToLowerInvariant()+"-2";
            void Save(string relative,string phase,float start,bool fullBoard,bool supplement)
            {
                if(frames.Count>=450)throw new InvalidOperationException("The 450-image capture budget was exceeded.");
                if(sprite.sprite==null)throw new InvalidOperationException("The isolated actor has no sprite.");
                string path=Path.Combine(directory,relative);Directory.CreateDirectory(Path.GetDirectoryName(path));
                if(File.Exists(path))throw new IOException("Capture file already exists: "+relative);
                var view=CharacterMotion.ViewFacing(actor.Facing,camera.transform.rotation);
                var frame=new Frame{file=relative.Replace('\\','/'),phase=phase,viewFacing=view.ToString(),worldFacing=actor.Facing.ToString(),
                    frame=Time.frameCount,realtime=Time.realtimeSinceStartupAsDouble,time=Time.time,elapsed=Time.time-start,
                    sprite=sprite.sprite.name,texture=AssetDatabase.GetAssetPath(sprite.sprite),rect=sprite.sprite.rect,pivot=sprite.sprite.pivot,
                    pixelsPerUnit=sprite.sprite.pixelsPerUnit,rootPosition=actorRoot.transform.position,localPosition=motion.transform.localPosition,
                    worldPosition=motion.transform.position,presenting=motion.Presenting,fullBoard=fullBoard,blinkSupplement=supplement};
                if(fullBoard)
                {
                    frame.cameraPosition=camera.transform.position;frame.cameraEuler=camera.transform.eulerAngles;frame.cameraSize=camera.orthographicSize;
                    var screen=ScreenCapture.CaptureScreenshotAsTexture();
                    try{frame.width=screen.width;frame.height=screen.height;File.WriteAllBytes(path,screen.EncodeToPNG());}
                    finally{Object.DestroyImmediate(screen);}
                }
                else
                {
                    var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
                    var pos=camera.transform.position;var rotation=camera.transform.rotation;float size=camera.orthographicSize,aspect=camera.aspect;var rect=camera.rect;
                    try
                    {
                        camera.transform.position=actorRoot.transform.position+Vector3.up*.64f-camera.transform.forward*12;
                        camera.orthographicSize=1.22f;camera.aspect=(float)Width/Height;camera.rect=new Rect(0,0,1,1);camera.targetTexture=target;
                        frame.cameraPosition=camera.transform.position;frame.cameraEuler=camera.transform.eulerAngles;frame.cameraSize=camera.orthographicSize;
                        camera.Render();RenderTexture.active=target;readback.ReadPixels(new Rect(0,0,Width,Height),0,0);readback.Apply(false,false);
                        frame.width=Width;frame.height=Height;File.WriteAllBytes(path,readback.EncodeToPNG());
                    }
                    finally{camera.targetTexture=oldTarget;camera.transform.SetPositionAndRotation(pos,rotation);camera.orthographicSize=size;camera.aspect=aspect;camera.rect=rect;RenderTexture.active=oldActive;}
                }
                frames.Add(frame);if(IsClosed(view))closed.Add(view.ToString());
            }
            void CheckSession()
            {
                if(battle.Session!=session||session.Units.Count(u=>u.Team==Team.Player)!=report.originalAllies||
                    SkillRosterVfxFixture.Models(battle)!=report.originalModels||SkillRosterVfxFixture.State(session.Units)!=state||
                    SkillRosterVfxFixture.GridState(session.Grid,session.Units)!=grid||JsonUtility.ToJson(battle.Campaign)!=campaign||
                    JsonUtility.ToJson(UnityEngine.Random.state)!=random||session.Resolver.RandomDraws!=randomDraws)
                    throw new InvalidOperationException("The original battle/campaign/RNG changed during the capture.");
            }
            public void Finish(Exception failure)
            {
                if(failure!=null)errors.Add(failure.ToString());
                try
                {
                    report.sessionAfter=SkillRosterVfxFixture.Hash(SkillRosterVfxFixture.State(session.Units));
                    report.gridAfter=SkillRosterVfxFixture.Hash(SkillRosterVfxFixture.GridState(session.Grid,session.Units));
                    report.campaignAfter=SkillRosterVfxFixture.Hash(JsonUtility.ToJson(battle.Campaign));
                    report.randomAfter=SkillRosterVfxFixture.Hash(JsonUtility.ToJson(UnityEngine.Random.state));
                    report.sessionUnchanged=battle.Session==session&&report.sessionAfter==report.sessionBefore&&
                        session.Units.Count(u=>u.Team==Team.Player)==report.originalAllies&&SkillRosterVfxFixture.Models(battle)==report.originalModels;
                    report.gridUnchanged=report.gridAfter==report.gridBefore;report.campaignUnchanged=report.campaignAfter==report.campaignBefore;
                    report.randomUnchanged=report.randomAfter==report.randomBefore&&session.Resolver.RandomDraws==randomDraws;
                    report.contentUnchanged=EditorJsonUtility.ToJson(original)==sourceData;
                    report.cameraRestored=camera!=null&&camera.transform.position==cameraPosition&&camera.transform.rotation==cameraRotation&&
                        camera.orthographicSize==cameraSize&&camera.aspect==cameraAspect&&camera.rect==cameraRect&&camera.targetTexture==cameraTarget;
                }
                catch(Exception e){errors.Add("Final state inspection failed: "+e);}
                finally
                {
                    if(actorRoot!=null){actorRoot.SetActive(false);Object.DestroyImmediate(actorRoot);}
                    if(data!=null)Object.DestroyImmediate(data);
                    if(target!=null){target.Release();Object.DestroyImmediate(target);}
                    if(readback!=null)Object.DestroyImmediate(readback);
                    report.temporaryObjectsRemoved=actorRoot==null&&data==null&&target==null&&readback==null;
                    outputLock?.Dispose();outputLock=null;
                    if(ownsOutput&&lockPath!=null&&File.Exists(lockPath))File.Delete(lockPath);
                }
                report.complete=complete;
                report.passed=complete&&errors.Count==0&&report.sessionUnchanged&&report.gridUnchanged&&report.campaignUnchanged&&
                    report.randomUnchanged&&report.contentUnchanged&&report.cameraRestored&&report.temporaryObjectsRemoved&&(!pilot||closed.Count==4);
                report.status=report.passed?"passed":"failed";if(ownsOutput)Write(true);
                SessionState.SetInt("TalesTactics.VisualPilotCaptureExit",report.passed?0:1);
            }
            void Write(bool final)
            {
                report.frames=frames.ToArray();report.errors=errors.ToArray();report.closedIdleDirections=closed.OrderBy(x=>x).ToArray();report.pngCount=frames.Count;
                string path=Path.Combine(directory,final?"result.json":"progress.json");
                if(final){using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read))
                    using(var writer=new StreamWriter(stream))writer.Write(JsonUtility.ToJson(report,true));}
                else File.WriteAllText(path,JsonUtility.ToJson(report,true));
            }
        }
    }
}
