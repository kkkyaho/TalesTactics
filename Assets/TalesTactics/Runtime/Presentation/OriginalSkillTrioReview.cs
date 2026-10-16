#if UNITY_EDITOR || TALESTACTICS_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace TalesTactics
{
    // Opt-in actual skill execution and capture. All fixture writes use a fresh isolated save root.
    public sealed class OriginalSkillTrioReview:MonoBehaviour
    {
        public static readonly string[] SkillIds={"cless.0","jade.6","jade.3"};
        static readonly string[] Names={"sword-wave","meteor","lightning"};
        static readonly string[] Party={"cless","mint","jade","farah","tear","alphen"};
        [Serializable] sealed class Row
        {
            public string name,skill,character,element,pattern;
            public bool reduced,executed,gridUnchanged,clean,titleClosed,unityRandomUnchanged,resolverMatches,singleResolution;
            public int beforeHP,afterHP,frames,peakActiveParticles,peakCombatEffects,poolInstances,actualContactFrame=-1;
            public int beforeMP,afterMP,expectedMP,expectedHP,expectedMPCost,hpChangeFrames,mpChangeFrames,resolutionCount,randomDrawsBefore,randomDrawsAfter,expectedRandomDraws;
            public int alliesBefore,alliesAfter,modelsBefore,modelsAfter;
            public float cameraSize;
            public string[] captures;
        }
        [Serializable] sealed class Report
        {
            public string label,saveRoot;
            public int width,height,fps=30,allies=6;
            public bool actualSkillExecution=true,fixturePlacement=true,defaultZoom=true,reactionsDisabled=true,userFilesUnchanged,isolatedFilesUnchanged,titleClosed;
            public Row[] runs;public string[] errors;
        }
        BattleDirector battle;
        string directory,label="after",saveRoot;
        bool captureFrames,finished,previousBackground,titleClosed;
        int previousCaptureRate;
        readonly List<Row> rows=new List<Row>();
        readonly List<string> errors=new List<string>();
        Dictionary<string,string> userFiles,isolatedFiles;
        readonly Dictionary<string,int> normalDeltas=new Dictionary<string,int>();
#if UNITY_EDITOR
        UnityEditor.EditorWindow gameView;
        object sizeGroup;
        int oldSize=-1,addedSize=-1;
        public static OriginalSkillTrioReview BeginEditor(string output,string phase="after",bool frames=false)
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Enter Play Mode before starting VFX review.");
            if(FindAnyObjectByType<OriginalSkillTrioReview>()!=null)throw new InvalidOperationException("A VFX review is already running.");
            var review=new GameObject("Original skill trio review").AddComponent<OriginalSkillTrioReview>();
            review.directory=Path.GetFullPath(output);review.label=phase;review.captureFrames=frames;
            UnityEditor.SessionState.SetString("TalesTactics.OriginalSkillTrioReviewOutput",review.directory);
            UnityEditor.SessionState.SetInt("TalesTactics.OriginalSkillTrioReviewExit",-1);return review;
        }
        void SetEditorResolution()
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;var type=assembly.GetType("UnityEditor.GameView");
            gameView=UnityEditor.EditorWindow.GetWindow(type);
            var selected=type.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
            oldSize=(int)selected.GetValue(gameView);
            var sizes=assembly.GetType("UnityEditor.GameViewSizes");
            var instance=sizes.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
            sizeGroup=sizes.GetProperty("currentGroup").GetValue(instance);
            var entry=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),1920,1080,"Original skill trio review"});
            sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup,new[]{entry});
            addedSize=(int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup,null)-1;
            selected.SetValue(gameView,addedSize);gameView.Focus();gameView.Repaint();
        }
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--original-skill-trio-review");if(i<0)return;
            if(i+1>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)){Application.Quit(2);return;}
            var review=new GameObject("Original skill trio review").AddComponent<OriginalSkillTrioReview>();
            review.directory=Path.Combine(Path.GetDirectoryName(Application.dataPath),"OriginalSkillTrio",args[i+1]);
            review.captureFrames=args.Contains("--trio-frames");int phase=Array.IndexOf(args,"--trio-label");
            if(phase>=0&&phase+1<args.Length)review.label=args[phase+1];
        }
        IEnumerator Start()
        {
            previousCaptureRate=Time.captureFramerate;previousBackground=Application.runInBackground;
            Directory.CreateDirectory(directory);
            if(File.Exists(Path.Combine(directory,"result.json")))throw new InvalidOperationException("Use a fresh VFX output directory; existing evidence is never overwritten.");
            Application.logMessageReceived+=Log;Application.runInBackground=true;
            yield return Guard(Run());Finish();
        }
        IEnumerator Guard(IEnumerator work)
        {
            var stack=new Stack<IEnumerator>();stack.Push(work);
            while(stack.Count>0)
            {
                object current=null;bool next=false;
                try{next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}
                catch(Exception e){errors.Add(e.ToString());yield break;}
                if(!next){stack.Pop();continue;}
                if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
        }
        IEnumerator Run()
        {
            yield return null;battle=FindAnyObjectByType<BattleDirector>();
            userFiles=Snapshot(Application.persistentDataPath);
            saveRoot=Path.Combine(directory,"isolated-save",Guid.NewGuid().ToString("N"));
            battle.ConfigureStorage(saveRoot);battle.Hud.HideTitle();battle.Audio.PersonalMusicEnabled=false;
            battle.Preferences.Reactions=false;battle.Preferences.MissionEvents=false;
            battle.Preferences.TextScale=1;battle.Preferences.ReducedEffects=false;
            for(int i=1;i<=20;i++)battle.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(i));
            foreach(var character in battle.Catalog.Characters)battle.Campaign.Get(character.Id).Level=25;
            battle.TrainingMode=false;battle.SelectedStage=10;
            battle.Deployment.Clear();battle.Deployment.AddRange(Party.Select(id=>Array.FindIndex(battle.Catalog.Characters,c=>c.Id==id)));
            if(battle.Deployment.Any(i=>i<0))throw new InvalidOperationException("Review party assets are missing.");
            if(!battle.SavePreparation())throw new InvalidOperationException("Could not seed isolated review save.");
#if UNITY_EDITOR
            SetEditorResolution();
#else
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
#endif
            // Windows remembers the requested window size through its normal settings writer.
            // Let setup settle before measuring whether skill presentation changes saved data.
            yield return new WaitForSecondsRealtime(Application.isEditor?.5f:2f);
            isolatedFiles=Snapshot(saveRoot);
            battle.BeginBattle();battle.StopAllCoroutines();
            if(battle.Session==null||battle.Session.Units.Count(u=>u.Team==Team.Player)!=6)throw new InvalidOperationException("Six-ally review fixture failed.");
            RequireClosedTitle();titleClosed=true;
            Time.captureFramerate=30;
            foreach(bool reduced in new[]{false,true})
            for(int example=0;example<SkillIds.Length;example++)yield return Sequence(example,reduced);
            var pools=battle.Board.GetComponentsInChildren<ImpactPool>(true);
            // The pool is attached to Runtime Battlefield, whose other children also
            // contain terrain and character resources. Only particle children own VFX meshes.
            var particles=pools.SelectMany(p=>p.GetComponentsInChildren<ImpactParticles>(true)).Distinct().ToArray();
            var meshes=particles.SelectMany(p=>p.GetComponentsInChildren<MeshFilter>(true)).Select(m=>m.sharedMesh).Where(m=>m!=null).ToArray();
            var materials=particles.SelectMany(p=>p.GetComponentsInChildren<Renderer>(true)).Select(r=>r.sharedMaterial).Where(m=>m!=null).Distinct().ToArray();
            battle.Restart();yield return null;yield return null;
            if(FindObjectsByType<CombatEffect>().Length>0||battle.Board.GetComponentsInChildren<ImpactPool>(true).Length>0||meshes.Any(m=>m!=null)||materials.Any(m=>m!=null))
                errors.Add("Restart left an owned VFX object, mesh, or material alive.");
        }
        IEnumerator Sequence(int example,bool reduced)
        {
            string id=SkillIds[example];var actor=battle.Session.Units.Single(u=>u.Team==Team.Player&&u.Data.Id==id.Split('.')[0]);
            var skill=actor.Data.Skills.Concat(new[]{actor.Data.BasicAttack,actor.Data.UltimateSkill}).Single(s=>s!=null&&s.Id==id);
            var enemy=battle.Session.Units.First(u=>u.Team==Team.Enemy&&!u.CrossoverBoss&&!u.CrossoverReserve&&u.Data.Id!="boundary-device");
            var recipient=skill.Target==TargetType.Ally?battle.Session.Units.First(u=>u.Team==Team.Player&&u!=actor):enemy;
            Arrange(actor,recipient);
            foreach(var unit in battle.Session.Units)
            {
                unit.Level=unit.Team==Team.Player?25:50;unit.EXP=0;unit.Statuses.Clear();unit.Cooldowns.Clear();
                unit.CurrentHP=unit.Stats.HP;unit.CurrentMP=unit.Stats.MP;unit.Acted=false;unit.SpecialGauge=100;
            }
            if(skill.Target==TargetType.Ally)recipient.CurrentHP=Mathf.Max(1,recipient.Stats.HP/3);
            battle.Session.Active=actor;actor.BeginTurn();battle.Preferences.ReducedEffects=reduced;
            battle.SetState(new CommandState(battle));battle.RefreshViews();battle.Board.ResetCamera();
            yield return null;yield return new WaitForEndOfFrame();
            RequireClosedTitle();
            string blocked=battle.Session.Resolver.CanUse(actor,skill);
            if(blocked!=null||!battle.Session.Resolver.InRange(actor,skill,recipient.Position)||!battle.Session.Resolver.Targets(actor,skill,recipient.Position).Contains(recipient))
                throw new InvalidOperationException(id+": invalid review action "+blocked);
            var positions=battle.Session.Units.ToDictionary(u=>u,u=>u.Position);
            int before=recipient.CurrentHP;float size=battle.Board.BattleCamera.orthographicSize;
            int actorIndex=battle.Session.Units.IndexOf(actor),recipientIndex=battle.Session.Units.IndexOf(recipient);
            var reference=ReferenceResolver(battle,out var referenceUnits);
            referenceUnits[actorIndex].Facing=SkillResolver.Toward(actor.Position,recipient.Position);
            if(!reference.Execute(referenceUnits[actorIndex],skill,recipient.Position,out var referenceError))
                throw new InvalidOperationException(id+": independent resolver fixture failed "+referenceError);
            string random=JsonUtility.ToJson(UnityEngine.Random.state);
            string name=Names[example]+(reduced?"-reduced":"-normal");string output=Path.Combine(directory,name);Directory.CreateDirectory(output);
            var row=new Row{name=name,skill=id,character=actor.Data.Id,element=skill.Element.ToString(),pattern=skill.Presentation.Pattern.ToString(),reduced=reduced,beforeHP=before,cameraSize=size,titleClosed=!battle.Hud.TitleOpen,
                beforeMP=actor.CurrentMP,expectedMP=referenceUnits[actorIndex].CurrentMP,expectedHP=referenceUnits[recipientIndex].CurrentHP,expectedMPCost=battle.Session.Resolver.MPCost(actor,skill),
                randomDrawsBefore=battle.Session.Resolver.RandomDraws,expectedRandomDraws=reference.RandomDraws,
                alliesBefore=battle.Session.Units.Count(u=>u.Team==Team.Player),modelsBefore=PlayerModels()};
            var captures=new List<string>();
            int windupSample=Mathf.Max(1,Mathf.CeilToInt(BoardView.Windup(skill)*15));
            float distance=Vector3.Distance(battle.Session.Grid[actor.Position].WorldPosition(battle.Catalog.Rules.TileHeight),battle.Session.Grid[recipient.Position].WorldPosition(battle.Catalog.Rules.TileHeight));
            int frames=Mathf.CeilToInt((BoardView.Windup(skill)+SkillVfxTiming.ReleaseDuration(actor.Data,skill,distance)+BoardView.Recovery(skill)+ImpactParticles.Duration+.7f)*30);
            const int maximumFrames=600;
            int lastHP=before,lastMP=actor.CurrentMP;
            Action<UnitRuntime,SkillData,Vector2Int> resolved=(unit,used,aim)=>{if(unit==actor&&used==skill)row.resolutionCount++;};
            battle.Session.Resolver.SkillResolved+=resolved;
            battle.StartCoroutine(battle.Execute(skill,recipient.Position));
            for(int frame=0;frame<frames;frame++)
            {
                yield return new WaitForEndOfFrame();
                var pools=battle.Board.GetComponentsInChildren<ImpactPool>(true);
                row.peakActiveParticles=Math.Max(row.peakActiveParticles,pools.Sum(p=>p.ActiveCount));
                int combatEffects=battle.Board.GetComponentsInChildren<CombatEffect>().Length;
                row.peakCombatEffects=Math.Max(row.peakCombatEffects,combatEffects);
                row.poolInstances=Math.Max(row.poolInstances,pools.Sum(p=>p.GetComponentsInChildren<ImpactParticles>(true).Length));
                if(row.poolInstances>ImpactPool.Capacity)errors.Add(name+": pool capacity exceeded");
                if(recipient.CurrentHP!=lastHP){row.hpChangeFrames++;lastHP=recipient.CurrentHP;}
                if(actor.CurrentMP!=lastMP){row.mpChangeFrames++;lastMP=actor.CurrentMP;}
                // Travel duration is part of production choreography. Observe the real
                // resolution/impact frame instead of guessing contact from windup alone.
                if(row.actualContactFrame<0&&(recipient.CurrentHP!=before||combatEffects>0))
                {
                    row.actualContactFrame=frame;
                    frames=Math.Max(frames,frame+Mathf.CeilToInt((BoardView.Recovery(skill)+ImpactParticles.Duration+.7f)*30));
                }
                bool busy=battle.State is ActionExecutionState||combatEffects>0||pools.Any(p=>p.ActiveCount>0);
                if(frame==frames-1&&busy)
                {
                    if(frame+1<maximumFrames)frames=Math.Min(maximumFrames,frames+15);
                    else errors.Add(name+": choreography exceeded the twenty-second capture limit");
                }
                string sample=frame==windupSample?"windup":row.actualContactFrame>=0&&frame==row.actualContactFrame+2?"contact":
                    row.actualContactFrame>=0&&frame==row.actualContactFrame+9?"follow":frame==frames-1?"clear":null;
                if(captureFrames||sample!=null)
                {
                    RequireClosedTitle();
                    string file=captureFrames?frame.ToString("D4")+".png":sample+".png";
                    var texture=ScreenCapture.CaptureScreenshotAsTexture();
                    try{File.WriteAllBytes(Path.Combine(output,file),texture.EncodeToPNG());}finally{Destroy(texture);}
                    if(sample!=null)captures.Add(name+"/"+file);
                }
            }
            battle.Session.Resolver.SkillResolved-=resolved;
            row.frames=frames;row.afterHP=recipient.CurrentHP;row.afterMP=actor.CurrentMP;
            row.randomDrawsAfter=battle.Session.Resolver.RandomDraws;
            row.unityRandomUnchanged=JsonUtility.ToJson(UnityEngine.Random.state)==random;
            row.resolverMatches=row.randomDrawsAfter==row.expectedRandomDraws&&battle.Session.Units.Select((unit,index)=>unit.CurrentHP==referenceUnits[index].CurrentHP&&unit.CurrentMP==referenceUnits[index].CurrentMP&&unit.Position==referenceUnits[index].Position).All(matches=>matches);
            row.singleResolution=row.resolutionCount==1&&row.hpChangeFrames==1&&row.mpChangeFrames==1;
            row.alliesAfter=battle.Session.Units.Count(u=>u.Team==Team.Player);row.modelsAfter=PlayerModels();
            row.executed=skill.Target==TargetType.Ally?row.afterHP>before:row.afterHP<before;
            row.gridUnchanged=positions.All(p=>p.Key.Position==p.Value);
            row.clean=battle.Board.GetComponentsInChildren<CombatEffect>().Length==0&&battle.Board.GetComponentsInChildren<ImpactPool>(true).All(p=>p.ActiveCount==0);
            row.captures=captures.ToArray();rows.Add(row);
            if(!row.executed||!row.gridUnchanged||!row.clean||row.peakActiveParticles==0||row.actualContactFrame<0)errors.Add(name+": execution/grid/cleanup/visible effect check failed");
            if(!row.unityRandomUnchanged||!row.resolverMatches||!row.singleResolution)errors.Add(name+": presentation RNG/reference resolver/single cost and result check failed");
            if(row.alliesBefore!=6||row.alliesAfter!=6||row.modelsBefore!=6||row.modelsAfter!=6)errors.Add(name+": six allied models were not preserved");
            if(Mathf.Abs(battle.Board.BattleCamera.orthographicSize-size)>.001f)errors.Add(name+": default camera zoom changed");
            int delta=row.afterHP-row.beforeHP;
            if(!reduced)normalDeltas[id]=delta;else if(normalDeltas[id]!=delta)errors.Add(name+": reduced effects changed resolved HP delta");
            battle.Hud.ClearCombatFeedback();
        }
        // Clone only the simulation fixture; no live state, campaign, or saved data is changed.
        // BattleSession constructs its resolver with the default seed. Restore its draw position
        // so normal hit/effect rolls remain allowed while extra presentation rolls are detected.
        static SkillResolver ReferenceResolver(BattleDirector director,out UnitRuntime[] units)
        {
            var source=director.Session;var grid=new GridMap();
            foreach(var tile in source.Grid.Tiles.Values)grid.Tiles.Add(tile.Coordinate,new GridTile{Coordinate=tile.Coordinate,Height=tile.Height,MovementCost=tile.MovementCost,Walkable=tile.Walkable,Terrain=tile.Terrain});
            units=source.Units.Select(unit=>new UnitRuntime(unit.Data,unit.Team,unit.Rules,unit.Level)).ToArray();
            for(int i=0;i<units.Length;i++)new CheckpointUnit(source.Units[i]).Apply(units[i],grid,director.Catalog);
            foreach(var tile in source.Grid.Tiles.Values)grid[tile.Coordinate].Occupant=tile.Occupant==null?null:units[source.Units.IndexOf(tile.Occupant)];
            var resolver=new SkillResolver(grid,units,source.Rules,tacticalCombat:source.Resolver.TacticalCombat,reactions:source.Resolver.ReactionsEnabled,actionExperience:source.Resolver.ActionExperienceEnabled);
            resolver.CrossoverState=source.Resolver.CrossoverState?.Copy();resolver.RestoreRandom(source.Resolver.RandomDraws);return resolver;
        }
        void RequireClosedTitle()
        {
            if(battle.Hud!=null&&!battle.Hud.TitleOpen)return;
            titleClosed=false;
            throw new InvalidOperationException("Title overlay is open; the battlefield capture would be invalid.");
        }
        int PlayerModels()=>battle.Board.GetComponentsInChildren<CharacterMotion>().Count(m=>m.Owner!=null&&m.Owner.Team==Team.Player);
        void Arrange(UnitRuntime actor,UnitRuntime recipient)
        {
            var grid=battle.Session.Grid;
            var a=grid.Tiles.Values.Where(t=>t.Walkable).OrderBy(t=>GridMap.Distance(t.Coordinate,new Vector2Int(5,5))).First(t=>
                grid[t.Coordinate+Vector2Int.right]?.Walkable==true&&grid[t.Coordinate+Vector2Int.right*2]?.Walkable==true&&
                grid[t.Coordinate+Vector2Int.right].Height==t.Height&&grid[t.Coordinate+Vector2Int.right*2].Height==t.Height).Coordinate;
            var b=a+Vector2Int.right*2;
            foreach(var tile in grid.Tiles.Values)tile.Occupant=null;
            actor.CurrentHP=actor.Stats.HP;recipient.CurrentHP=recipient.Stats.HP;
            grid.Place(actor,a);grid.Place(recipient,b);
            var rest=battle.Session.Units.Where(u=>u!=actor&&u!=recipient).OrderBy(u=>u.Team==Team.Player?0:1).ToArray();
            var cells=grid.Tiles.Values.Where(t=>t.Walkable&&t.Coordinate!=a&&t.Coordinate!=b&&t.Coordinate!=a+Vector2Int.right).OrderBy(t=>GridMap.Distance(t.Coordinate,a)).ToList();
            foreach(var unit in rest)
            {
                unit.CurrentHP=unit.Stats.HP;
                var tile=unit.Team==Team.Player?cells.First():cells.Last();cells.Remove(tile);
                if(!grid.Place(unit,tile.Coordinate))throw new InvalidOperationException("Review fixture placement failed.");
            }
        }
        static Dictionary<string,string> Snapshot(string root)
        {
            var result=new Dictionary<string,string>();if(!Directory.Exists(root))return result;
            foreach(var path in Directory.GetFiles(root).Where(p=>Path.GetFileName(p).StartsWith("campaign",StringComparison.OrdinalIgnoreCase)||Path.GetFileName(p).StartsWith("settings",StringComparison.OrdinalIgnoreCase)))
                using(var sha=SHA256.Create())result[Path.GetFileName(path)]=Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
            return result;
        }
        static bool Equal(Dictionary<string,string> a,Dictionary<string,string> b)=>a!=null&&a.Count==b.Count&&a.All(p=>b.TryGetValue(p.Key,out var value)&&value==p.Value);
        void Finish()
        {
            if(finished)return;finished=true;
            bool userSafe=Equal(userFiles,Snapshot(Application.persistentDataPath)),isolatedSafe=saveRoot!=null&&Equal(isolatedFiles,Snapshot(saveRoot));
            if(!userSafe)errors.Add("User save/settings file hashes changed during the review.");
            if(!isolatedSafe)errors.Add("Seeded isolated save changed during presentation sequences.");
            var report=new Report{label=label,saveRoot=saveRoot,width=Screen.width,height=Screen.height,userFilesUnchanged=userSafe,isolatedFilesUnchanged=isolatedSafe,titleClosed=titleClosed,runs=rows.ToArray(),errors=errors.ToArray()};
            File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(report,true));File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);
            int result=errors.Count==0&&rows.Count==SkillIds.Length*2?0:1;
#if UNITY_EDITOR
            Restore();UnityEditor.SessionState.SetInt("TalesTactics.OriginalSkillTrioReviewExit",result);UnityEditor.EditorApplication.isPlaying=false;
#else
            // Keep the hidden player ticking until Unity processes its quit request.
            Time.captureFramerate=previousCaptureRate;Application.Quit(result);
#endif
        }
        void Log(string message,string trace,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
        void Restore()
        {
            Time.captureFramerate=previousCaptureRate;Application.runInBackground=previousBackground;
#if UNITY_EDITOR
            if(gameView!=null&&oldSize>=0)
            {
                gameView.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(gameView,oldSize);
                if(addedSize>=0)sizeGroup.GetType().GetMethod("RemoveCustomSize").Invoke(sizeGroup,new object[]{addedSize});
                oldSize=addedSize=-1;
            }
#endif
        }
        void OnDestroy(){Restore();Application.logMessageReceived-=Log;}
    }
}
#endif
