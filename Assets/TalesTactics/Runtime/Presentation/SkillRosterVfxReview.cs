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
    // Review-only fixture shared with engine tests. It never changes authored skill data.
    public static class SkillRosterVfxFixture
    {
        public sealed class Entry { public CharacterData Owner;public SkillData Skill;public bool Ally; }
        public static Entry[] Entries(BattleCatalog catalog,bool alliesOnly=false)
        {
            var entries=new List<Entry>();
            foreach(var owner in catalog.Characters.Concat(alliesOnly?new CharacterData[0]:new[]{catalog.Enemy}.Concat(catalog.Enemies).Concat(catalog.CrossoverVariants)).Where(c=>c!=null).Distinct())
                foreach(var skill in new[]{owner.BasicAttack}.Concat(owner.Skills).Concat(new[]{owner.UltimateSkill}).Where(s=>s!=null))
                    entries.Add(new Entry{Owner=owner,Skill=skill,Ally=catalog.Characters.Contains(owner)});
            if(!alliesOnly)foreach(var skill in catalog.TacticalEnemySkills.Where(s=>s!=null))entries.Add(new Entry{Owner=catalog.Enemy,Skill=skill});
            return entries.GroupBy(e=>e.Skill.Id,StringComparer.Ordinal).Select(g=>g.First()).OrderBy(e=>e.Skill.Id,StringComparer.Ordinal).ToArray();
        }
        public static void BeginActor(BattleDirector battle,string id)
        {
            battle.Restart();battle.Hud.HideTitle();battle.TrainingMode=true;battle.TrainingObjective=ObjectiveKind.Eliminate;
            battle.Preferences.Reactions=false;battle.Preferences.MissionEvents=false;battle.Campaign.AutoTiming=false;
            var roster=battle.Catalog.Characters;int owner=Array.FindIndex(roster,c=>c.Id==id);
            if(owner<0)throw new InvalidOperationException("Unknown fixture actor "+id);
            battle.Deployment.Clear();battle.Deployment.Add(owner);battle.Deployment.AddRange(Enumerable.Range(0,roster.Length).Where(i=>i!=owner).Take(5));
            battle.BeginBattle();battle.StopAllCoroutines();
            if(battle.Session==null||battle.Session.Units.Count(u=>u.Team==Team.Player)!=6)throw new InvalidOperationException("Six-ally training fixture failed "+id);
        }
        public static string State(IEnumerable<UnitRuntime> units)
        {return string.Join("\n",units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u))));}
        public static string Hash(string value)
        {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
        public static string GridState(GridMap grid,IList<UnitRuntime> units)
        {return string.Join(";",grid.Tiles.Values.OrderBy(t=>t.Coordinate.x).ThenBy(t=>t.Coordinate.y).Select(t=>t.Coordinate.x+","+t.Coordinate.y+":"+(t.Occupant==null?-1:units.IndexOf(t.Occupant))));}
        public static int Models(BattleDirector battle)=>battle.Board.GetComponentsInChildren<CharacterMotion>(true).Count(m=>m.Owner?.Team==Team.Player);
        public static bool Clean(BattleDirector battle)=>battle.Board.GetComponentsInChildren<CombatEffect>().Length==0&&battle.Board.GetComponentsInChildren<ImpactPool>(true).All(p=>p.ActiveCount==0);
        public static string ArteId(ImpactParticles particles)
        {
            // Reflection keeps this new capture harness compilable against the unchanged baseline runtime.
            var property=typeof(ImpactParticles).GetProperty("Arte");var value=property?.GetValue(particles);
            return value==null?null:(value.GetType().GetProperty("Id")?.GetValue(value)??value.GetType().GetField("Id")?.GetValue(value)) as string;
        }
        public sealed class Scenario:IDisposable
        {
            public readonly BattleDirector Battle;public readonly SkillData Skill,LaunchSkill;public readonly UnitRuntime Actor,Recipient;
            public readonly Vector2Int Aim;public readonly bool Followup,TimingGate,ExpectProfile;
            public readonly string BeforeState,ExpectedState,ExpectedGrid,InitialRandom;public readonly int ExpectedDraws,BeforeModels,BeforeLiving;
            public readonly int ExpectedMPCost,ExpectedHPCost;public int Resolutions,ContactFrame=-1,Frames,PeakParticles,PeakEffects,PoolInstances;
            public bool BeforeContactUnchanged=true,ProfileSeen,NumberSeen;public string UnexpectedSkill;
            readonly Action<UnitRuntime,SkillData,Vector2Int> resolved;
            public Scenario(BattleDirector battle,SkillData skill,bool reduced,bool expectProfile=true,bool woundedActor=false)
            {
                Battle=battle;Skill=skill;ExpectProfile=expectProfile&&SkillSignatures.Resolve(skill)==SkillSignature.None;
                Actor=battle.Session.Units.Single(u=>u.Team==Team.Player&&u.Data.Id==skill.Id.Split('.')[0]);
                var units=battle.Session.Units;var grid=battle.Session.Grid;
                foreach(var u in units)
                {
                    u.Level=u.Team==Team.Player?25:50;u.EXP=u.BattleEXP=0;u.Statuses.Clear();u.Cooldowns.Clear();
                    u.Acted=u.Moved=u.CanUndoMove=u.FlamingChain=u.GuardIgnition=false;u.ClawAttacks=0;u.SpecialGauge=100;
                    u.ProtectionUsed=u.CounterUsed=u.SupportUsed=false;u.TurnsStarted=u.IntentTurn=u.IntentPhase=0;u.IntentSkill=null;
                    u.Facing=u.OriginalFacing=Facing.Front;u.TurnBeganIncapacitated=false;u.TimeStopResistance=0;
                    u.CurrentHP=u.Stats.HP;u.CurrentMP=u.Stats.MP;
                }
                Actor.BeginTurn();battle.Session.Active=Actor;battle.Campaign.AutoTiming=false;
                Recipient=skill.Target==TargetType.Self?Actor:skill.Target==TargetType.Ally||skill.Target==TargetType.FallenAlly?
                    units.First(u=>u.Team==Team.Player&&u!=Actor):units.First(u=>u.Team==Team.Enemy&&!u.CrossoverReserve);
                Aim=Arrange(battle,Actor,Recipient,skill);
                if(woundedActor)Actor.CurrentHP=Mathf.Max(1,Actor.Stats.HP/4);
                if(skill.Target==TargetType.Ally)
                    foreach(var u in units.Where(u=>u.Team==Team.Player&&u!=Actor))
                    {u.CurrentHP=Mathf.Max(1,u.Stats.HP/3);if(skill.Effects.Any(e=>e.Kind==EffectKind.Cleanse)){u.AddStatus(StatusKind.Root,3);u.AddStatus(StatusKind.Slow,3);}}
                if(skill.Target==TargetType.FallenAlly){Recipient.CurrentHP=0;grid[Recipient.Position].Occupant=null;}
                if(skill.Gate==SkillGate.Claw||skill.Gate==SkillGate.Nightmare||skill.Gate==SkillGate.ClawFinisher)
                {Actor.AddStatus(StatusKind.ConsumeClaw,3);Actor.ClawAttacks=skill.Gate==SkillGate.Claw?0:1;}
                if(skill.Gate==SkillGate.GuardIgnition)Actor.GuardIgnition=true;
                Followup=skill.Gate==SkillGate.FlamingEdge||skill.Gate==SkillGate.FarahTiming;
                if(skill.Gate==SkillGate.FlamingEdge){Actor.FlamingChain=true;Actor.Acted=true;}
                TimingGate=skill.Gate==SkillGate.FarahTiming;
                LaunchSkill=TimingGate?Actor.Data.Skills.Single(s=>s.IsLionHowl):skill;
                if(Aim!=Actor.Position)Actor.Facing=SkillResolver.Toward(Actor.Position,Aim);
                battle.Preferences.ReducedEffects=reduced;battle.Session.Resolver.RestoreRandom(0);
                string reason=battle.Session.Resolver.CanUse(Actor,skill,Followup);
                if(reason!=null||!battle.Session.Resolver.InRange(Actor,skill,Aim)||!battle.Session.Resolver.Targets(Actor,skill,Aim).Contains(Recipient))
                    throw new InvalidOperationException(skill.Id+": invalid actual-action fixture: "+reason);
                battle.SetState(new CommandState(battle));battle.Hud.ClearCombatFeedback();battle.RefreshViews();battle.Board.ResetCamera();
                BeforeState=State(units);InitialRandom=JsonUtility.ToJson(UnityEngine.Random.state);
                BeforeModels=Models(battle);BeforeLiving=units.Count(u=>u.Team==Team.Player&&u.Alive);
                ExpectedMPCost=battle.Session.Resolver.MPCost(Actor,skill);ExpectedHPCost=battle.Session.Resolver.HPCost(Actor,skill);
                var projection=new BattleProjection(battle.Session);projection.Resolver.RestoreRandom(battle.Session.Resolver.RandomDraws);
                if(!projection.Resolver.Execute(projection.Copies[Actor],skill,Aim,out reason,Followup))throw new InvalidOperationException(skill.Id+": reference resolver: "+reason);
                var copies=units.Select(u=>projection.Copies[u]).ToArray();ExpectedState=State(copies);ExpectedGrid=GridState(projection.Grid,copies);ExpectedDraws=projection.Resolver.RandomDraws;
                resolved=(u,s,p)=>{if(u!=Actor)return;Resolutions++;if(s!=Skill)UnexpectedSkill=s?.Id;};
                battle.Session.Resolver.SkillResolved+=resolved;
            }
            public void Start()=>Battle.StartCoroutine(Battle.Execute(LaunchSkill,Aim,TimingGate?false:Followup));
            public void Observe()
            {
                if(TimingGate&&Battle.TimingActive&&Battle.TimingProgress>=Battle.Catalog.Rules.TimingWindowStart&&Battle.TimingProgress<=Battle.Catalog.Rules.TimingWindowEnd)Battle.TimingInput();
                var particles=Battle.Board.GetComponentsInChildren<ImpactParticles>();var pools=Battle.Board.GetComponentsInChildren<ImpactPool>(true);
                PeakParticles=Math.Max(PeakParticles,pools.Sum(p=>p.ActiveCount));PeakEffects=Math.Max(PeakEffects,Battle.Board.GetComponentsInChildren<CombatEffect>().Length);
                PoolInstances=Math.Max(PoolInstances,pools.Sum(p=>p.GetComponentsInChildren<ImpactParticles>(true).Length));
                if(particles.Any(p=>ArteId(p)==Skill.Id))ProfileSeen=true;
                if(Resolutions==0&&State(Battle.Session.Units)!=BeforeState)BeforeContactUnchanged=false;
                if(Resolutions>0&&ContactFrame<0)ContactFrame=Frames;
                if(Battle.Hud.HasHealthFeedback(Recipient))NumberSeen=true;Frames++;
            }
            public bool Finished=>Resolutions>0&&!(Battle.State is ActionExecutionState)&&Clean(Battle);
            public string[] Problems()
            {
                var problems=new List<string>();var units=Battle.Session.Units;
                if(Resolutions!=1||UnexpectedSkill!=null)problems.Add("Expected one resolution of "+Skill.Id+", actual count="+Resolutions+" replacement="+UnexpectedSkill);
                if(!BeforeContactUnchanged)problems.Add("Simulation changed before contact");
                if(State(units)!=ExpectedState)problems.Add("Runtime unit state differs from independent resolver");
                if(GridState(Battle.Session.Grid,units)!=ExpectedGrid)problems.Add("Occupancy differs from independent resolver");
                if(Battle.Session.Resolver.RandomDraws!=ExpectedDraws)problems.Add("Combat RNG draw count differs from independent resolver");
                if(JsonUtility.ToJson(UnityEngine.Random.state)!=InitialRandom)problems.Add("Presentation consumed global RNG");
                if(!(Battle.State is CommandState))problems.Add("Action did not return to command state: "+Battle.State?.GetType().Name);
                if(!Clean(Battle)||PeakParticles<1||PoolInstances>ImpactPool.Capacity)problems.Add("Effects did not render/clean up within pool capacity");
                if(ExpectProfile&&!ProfileSeen)problems.Add("Exact-ID presentation profile was never rendered");
                if(units.Count(u=>u.Team==Team.Player)!=6||BeforeModels!=6||Models(Battle)!=6)problems.Add("Six allied units/models were not preserved (inactive KO models included)");
                return problems.ToArray();
            }
            public void Dispose()=>Battle.Session.Resolver.SkillResolved-=resolved;
        }
        static Vector2Int Arrange(BattleDirector battle,UnitRuntime actor,UnitRuntime target,SkillData skill)
        {
            var grid=battle.Session.Grid;var cells=grid.Tiles.Values.Where(t=>t.Walkable).OrderBy(t=>GridMap.Distance(t.Coordinate,new Vector2Int(4,4))).ToArray();
            Vector2Int origin=default,aim=default,targetCell=default;bool found=false;
            foreach(var tile in cells)
            {
                var aims=skill.Range==0||skill.Target==TargetType.Self?new[]{tile.Coordinate}:cells.OrderBy(t=>Math.Abs(GridMap.Distance(tile.Coordinate,t.Coordinate)-Math.Max(1,Math.Min(2,skill.Range)))).Select(t=>t.Coordinate);
                foreach(var candidate in aims)
                {
                    if(!SkillGeometry.CanAim(grid,tile.Coordinate,skill,candidate))continue;
                    var targets=SkillGeometry.Area(grid,tile.Coordinate,skill,candidate).Where(p=>grid[p].Walkable&&(target==actor?p==tile.Coordinate:p!=tile.Coordinate)).OrderBy(p=>GridMap.Distance(p,candidate)).ToArray();
                    if(targets.Length==0)continue;origin=tile.Coordinate;aim=candidate;targetCell=targets[0];found=true;break;
                }
                if(found)break;
            }
            if(!found)throw new InvalidOperationException(skill.Id+": no legal aim in training map");
            foreach(var tile in grid.Tiles.Values)tile.Occupant=null;
            grid.Place(actor,origin);if(target!=actor)grid.Place(target,targetCell);
            var free=cells.Where(t=>t.Occupant==null).ToList();
            foreach(var unit in battle.Session.Units.Where(u=>u!=actor&&u!=target).OrderBy(u=>u.Team==Team.Player?0:1))
            {
                var tile=unit.Team==Team.Player?free.OrderBy(t=>GridMap.Distance(t.Coordinate,origin)).First():free.OrderByDescending(t=>GridMap.Distance(t.Coordinate,aim)).First();
                free.Remove(tile);if(!grid.Place(unit,tile.Coordinate))throw new InvalidOperationException("Fixture placement failed");
            }
            // Gravity has a real displacement effect: put one additional enemy two cells from its centre.
            if(skill.Effects.Any(e=>e.Kind==EffectKind.Pull))
            {
                var extra=battle.Session.Units.FirstOrDefault(u=>u.Team!=actor.Team&&u!=target);
                var destinations=SkillGeometry.Area(grid,origin,skill,aim).Where(p=>grid[p].Walkable&&grid[p].Occupant==null&&GridMap.Distance(p,aim)==2)
                    .Where(p=>p.x==aim.x||p.y==aim.y).ToArray();
                if(extra!=null&&destinations.Length>0)grid.Place(extra,destinations[0]);
            }
            return aim;
        }
    }

    public sealed class SkillRosterVfxReview:MonoBehaviour
    {
        public static readonly string[] Representatives={"cless.5","cless.ultimate","mint.6","mint.ultimate","velvet.0","velvet.ultimate","farah.3","farah.ultimate","tear.3","tear.5","jade.0","jade.ultimate","natalia.5","natalia.6","alphen.4","alphen.ultimate","shionne.5","shionne.6","kisara.3","kisara.ultimate"};
        [Serializable] public sealed class Row
        {
            public string skill,character,name,launchSkill,expectedStateHash,actualStateHash;public bool reduced,executed,clean,beforeContactUnchanged,resolverMatches,gridMatches,unityRandomUnchanged,profileSeen,timingGate,titleClosed;
            public int frames,actualContactFrame,resolutionCount,expectedMPCost,expectedHPCost,expectedRandomDraws,randomDraws,alliedModels,livingAlliesBefore,livingAlliesAfter;public float cameraSize;public string[] captures,errors;
        }
        [Serializable] sealed class Report
        {
            public string phase,saveRoot,status,skillFilter,characterFilter,currentSkill,scope="Ally actual execution only; enemy/tactical rendering is covered separately by engine tests.";
            public int width,height,fps=30,expectedRuns,completedRuns,alliedCatalogSkills;public bool trainingFixture=true,reactionsDisabled=true,autoTimingDisabled=true,includeReduced,profileAssertions,userFilesUnchanged,isolatedFilesUnchanged;
            public Row[] runs;public string[] errors;
        }
        BattleDirector battle;string directory,phase="after",skillFilter="",characterFilter="",saveRoot,currentSkill;bool fullFrames,includeReduced=true,captureContacts=true,finished,previousBackground;
        int previousRate,expectedRuns;readonly List<Row> rows=new List<Row>();readonly List<string> errors=new List<string>();Dictionary<string,string> userFiles,isolatedFiles;
#if UNITY_EDITOR
        UnityEditor.EditorWindow gameView;object sizeGroup;int oldSize=-1,addedSize=-1;
        public static SkillRosterVfxReview BeginEditor(string output,string phase="after",bool frames=false,string skills="",string characters="",bool reduced=true,bool contacts=true)
        {
            if(!Application.isPlaying||FindAnyObjectByType<SkillRosterVfxReview>()!=null)throw new InvalidOperationException("A single active Play Mode review is required.");
            var review=new GameObject("Skill roster VFX review").AddComponent<SkillRosterVfxReview>();
            review.directory=Path.GetFullPath(output);review.phase=phase;review.fullFrames=frames;review.skillFilter=skills??"";review.characterFilter=characters??"";review.includeReduced=reduced;review.captureContacts=contacts;
            UnityEditor.SessionState.SetString("TalesTactics.SkillRosterVfxReviewOutput",review.directory);UnityEditor.SessionState.SetInt("TalesTactics.SkillRosterVfxReviewExit",-1);return review;
        }
        void SetEditorResolution()
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;var type=assembly.GetType("UnityEditor.GameView");gameView=UnityEditor.EditorWindow.GetWindow(type);
            var selected=type.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);oldSize=(int)selected.GetValue(gameView);
            var sizes=assembly.GetType("UnityEditor.GameViewSizes");var instance=sizes.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);sizeGroup=sizes.GetProperty("currentGroup").GetValue(instance);
            var entry=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),1920,1080,"Skill roster VFX review"});
            sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup,new[]{entry});addedSize=(int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup,null)-1;
            selected.SetValue(gameView,addedSize);gameView.Focus();gameView.Repaint();
        }
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--skill-roster-vfx-review");if(index<0)return;
            if(index+1>=args.Length||!Guid.TryParseExact(args[index+1],"N",out _)){Application.Quit(2);return;}
            string Value(string key,string fallback=""){int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
            var review=new GameObject("Skill roster VFX review").AddComponent<SkillRosterVfxReview>();review.directory=Path.Combine(Path.GetDirectoryName(Application.dataPath),"SkillRosterVfx",args[index+1]);
            review.phase=Value("--roster-phase","after");review.skillFilter=Value("--roster-skills");review.characterFilter=Value("--roster-characters");review.fullFrames=args.Contains("--roster-frames");review.includeReduced=!args.Contains("--roster-normal-only");review.captureContacts=!args.Contains("--roster-no-contact-captures");
        }
        IEnumerator Start()
        {
            previousRate=Time.captureFramerate;previousBackground=Application.runInBackground;
            if(Directory.Exists(directory)&&Directory.EnumerateFileSystemEntries(directory).Any())throw new InvalidOperationException("Use a fresh review directory; focus filters allow isolated retries without overwriting evidence.");
            Directory.CreateDirectory(directory);Application.runInBackground=true;Application.logMessageReceived+=Log;
            yield return Guard(Run());Finish();
        }
        IEnumerator Guard(IEnumerator work)
        {
            var stack=new Stack<IEnumerator>();stack.Push(work);
            while(stack.Count>0){object current=null;bool next=false;try{next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}catch(Exception e){errors.Add(e.ToString());yield break;}if(!next){stack.Pop();continue;}if(current is IEnumerator nested)stack.Push(nested);else yield return current;}
        }
        static string[] Tokens(string value)=>value.Split(new[]{',',';'},StringSplitOptions.RemoveEmptyEntries).Select(v=>v.Trim()).ToArray();
        IEnumerator Run()
        {
            if(phase!="before"&&phase!="after")throw new InvalidOperationException("Review phase must be before or after");
            yield return null;battle=FindAnyObjectByType<BattleDirector>();userFiles=Snapshot(Application.persistentDataPath);
            saveRoot=Path.Combine(directory,"isolated-save",Guid.NewGuid().ToString("N"));battle.ConfigureStorage(saveRoot);battle.Hud.HideTitle();battle.Audio.PersonalMusicEnabled=false;
            battle.Preferences.TextScale=1;battle.Preferences.Reactions=false;battle.Preferences.MissionEvents=false;battle.Campaign.AutoTiming=false;battle.TrainingMode=true;
            for(int chapter=1;chapter<=20;chapter++)battle.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(chapter));
            if(!battle.SavePreparation())throw new InvalidOperationException("Could not seed isolated save");
#if UNITY_EDITOR
            SetEditorResolution();
#else
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
#endif
            yield return new WaitForSecondsRealtime(Application.isEditor?.5f:2f);isolatedFiles=Snapshot(saveRoot);Time.captureFramerate=30;
            var skills=skillFilter=="representative"?Representatives:Tokens(skillFilter);var characters=Tokens(characterFilter);
            var all=SkillRosterVfxFixture.Entries(battle.Catalog,true);
            if(skills.Any(id=>!all.Any(e=>e.Skill.Id==id))||characters.Any(id=>!battle.Catalog.Characters.Any(c=>c.Id==id)))throw new InvalidOperationException("Unknown skill/character filter");
            var selected=all.Where(e=>(skills.Length==0||skills.Contains(e.Skill.Id))&&(characters.Length==0||characters.Contains(e.Owner.Id))).ToArray();
            if(selected.Length==0)throw new InvalidOperationException("No actions matched the review filters");expectedRuns=selected.Length*(includeReduced?2:1);WriteReport("running");
            foreach(var group in selected.GroupBy(e=>e.Owner.Id))
            {
                SkillRosterVfxFixture.BeginActor(battle,group.Key);yield return null;yield return new WaitForEndOfFrame();
                foreach(var entry in group)foreach(bool reduced in includeReduced?new[]{false,true}:new[]{false})yield return Sequence(entry,reduced);
                var particles=battle.Board.GetComponentsInChildren<ImpactParticles>(true);var meshes=particles.SelectMany(p=>p.GetComponentsInChildren<MeshFilter>(true)).Select(m=>m.sharedMesh).ToArray();
                var materials=particles.SelectMany(p=>p.GetComponentsInChildren<Renderer>(true)).Select(r=>r.sharedMaterial).Distinct().ToArray();
                battle.Restart();yield return null;yield return null;
                if(meshes.Any(m=>m!=null)||materials.Any(m=>m!=null)||battle.Board.GetComponentsInChildren<ImpactPool>(true).Length>0)errors.Add(group.Key+": owned VFX resources survived restart");
            }
        }
        IEnumerator Sequence(SkillRosterVfxFixture.Entry entry,bool reduced)
        {
            currentSkill=entry.Skill.Id+(reduced?"-reduced":"-normal");WriteReport("running");
            using(var scenario=new SkillRosterVfxFixture.Scenario(battle,entry.Skill,reduced,phase!="before"))
            {
                yield return null;yield return new WaitForEndOfFrame();RequireTitleClosed();float cameraSize=battle.Board.BattleCamera.orthographicSize;
                var captures=new List<string>();bool frames=fullFrames&&!reduced&&(Representatives.Contains(entry.Skill.Id)||Tokens(skillFilter).Contains(entry.Skill.Id));string output=Path.Combine(directory,currentSkill);Directory.CreateDirectory(output);
                // PNG encoding is synchronous; slow storage must not shorten the simulated sequence.
                scenario.Start();float deadline=Time.realtimeSinceStartup+600;int frame=0;bool capturedContact=false;
                do
                {
                    yield return new WaitForEndOfFrame();scenario.Observe();
                    bool contact=!capturedContact&&scenario.ContactFrame>=0&&frame>=scenario.ContactFrame+2;
                    if(frames||captureContacts&&contact)
                    {
                        RequireTitleClosed();string file=frames?frame.ToString("D4")+".png":"contact.png";var texture=ScreenCapture.CaptureScreenshotAsTexture();
                        try{File.WriteAllBytes(Path.Combine(output,file),texture.EncodeToPNG());}finally{Destroy(texture);}
                        if(contact)captures.Add(currentSkill+"/"+file);
                    }
                    if(contact)capturedContact=true;frame++;
                    if(scenario.Finished&&frame>(scenario.ContactFrame+20))break;
                }while(frame<1200&&Time.realtimeSinceStartup<deadline);
                var issues=scenario.Problems().ToList();if(!scenario.Finished)issues.Add("Actual action exceeded capture deadline");
                if(Mathf.Abs(cameraSize-battle.Board.BattleCamera.orthographicSize)>.001f)issues.Add("Default camera zoom changed");
                var actual=SkillRosterVfxFixture.State(battle.Session.Units);
                var row=new Row{skill=entry.Skill.Id,character=entry.Owner.Id,name=currentSkill,launchSkill=scenario.LaunchSkill.Id,reduced=reduced,executed=scenario.Resolutions==1&&scenario.UnexpectedSkill==null,
                    clean=SkillRosterVfxFixture.Clean(battle),beforeContactUnchanged=scenario.BeforeContactUnchanged,resolverMatches=actual==scenario.ExpectedState,gridMatches=SkillRosterVfxFixture.GridState(battle.Session.Grid,battle.Session.Units)==scenario.ExpectedGrid,
                    unityRandomUnchanged=JsonUtility.ToJson(UnityEngine.Random.state)==scenario.InitialRandom,profileSeen=scenario.ProfileSeen,timingGate=scenario.TimingGate,titleClosed=!battle.Hud.TitleOpen,frames=frame,actualContactFrame=scenario.ContactFrame,resolutionCount=scenario.Resolutions,
                    expectedMPCost=scenario.ExpectedMPCost,expectedHPCost=scenario.ExpectedHPCost,expectedRandomDraws=scenario.ExpectedDraws,randomDraws=battle.Session.Resolver.RandomDraws,alliedModels=SkillRosterVfxFixture.Models(battle),livingAlliesBefore=scenario.BeforeLiving,livingAlliesAfter=battle.Session.Units.Count(u=>u.Team==Team.Player&&u.Alive),
                    expectedStateHash=SkillRosterVfxFixture.Hash(scenario.ExpectedState),actualStateHash=SkillRosterVfxFixture.Hash(actual),cameraSize=cameraSize,captures=captures.ToArray(),errors=issues.ToArray()};
                rows.Add(row);errors.AddRange(issues.Select(issue=>currentSkill+": "+issue));WriteReport("running");battle.Hud.ClearCombatFeedback();
            }
        }
        void RequireTitleClosed(){if(battle.Hud.TitleOpen)throw new InvalidOperationException("Title overlay hides the battlefield");}
        static Dictionary<string,string> Snapshot(string root)
        {
            var result=new Dictionary<string,string>();if(!Directory.Exists(root))return result;
            foreach(var file in Directory.GetFiles(root).Where(p=>Path.GetFileName(p).StartsWith("campaign",StringComparison.OrdinalIgnoreCase)||Path.GetFileName(p).StartsWith("settings",StringComparison.OrdinalIgnoreCase)))
                using(var sha=SHA256.Create())result[Path.GetFileName(file)]=Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(file)));return result;
        }
        static bool Equal(Dictionary<string,string> a,Dictionary<string,string> b)=>a!=null&&a.Count==b.Count&&a.All(p=>b.TryGetValue(p.Key,out var value)&&value==p.Value);
        void WriteReport(string status)
        {
            var report=new Report{phase=phase,saveRoot=saveRoot,status=status,skillFilter=skillFilter,characterFilter=characterFilter,currentSkill=currentSkill,width=Screen.width,height=Screen.height,expectedRuns=expectedRuns,completedRuns=rows.Count,alliedCatalogSkills=battle==null?0:SkillRosterVfxFixture.Entries(battle.Catalog,true).Length,
                includeReduced=includeReduced,profileAssertions=phase!="before",userFilesUnchanged=Equal(userFiles,Snapshot(Application.persistentDataPath)),isolatedFilesUnchanged=saveRoot!=null&&Equal(isolatedFiles,Snapshot(saveRoot)),runs=rows.ToArray(),errors=errors.ToArray()};
            File.WriteAllText(Path.Combine(directory,status=="running"?"progress.json":"result.json"),JsonUtility.ToJson(report,true));
        }
        void Finish()
        {
            if(finished)return;finished=true;if(!Equal(userFiles,Snapshot(Application.persistentDataPath)))errors.Add("User saves/settings changed");if(saveRoot==null||!Equal(isolatedFiles,Snapshot(saveRoot)))errors.Add("Isolated save/settings changed");
            int exit=errors.Count==0&&expectedRuns>0&&rows.Count==expectedRuns?0:1;WriteReport(exit==0?"passed":"failed");File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);
#if UNITY_EDITOR
            Restore();UnityEditor.SessionState.SetInt("TalesTactics.SkillRosterVfxReviewExit",exit);UnityEditor.EditorApplication.isPlaying=false;
#else
            Time.captureFramerate=previousRate;Application.Quit(exit);
#endif
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
        void Restore()
        {
            Time.captureFramerate=previousRate;Application.runInBackground=previousBackground;
#if UNITY_EDITOR
            if(gameView!=null&&oldSize>=0){gameView.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(gameView,oldSize);if(addedSize>=0)sizeGroup.GetType().GetMethod("RemoveCustomSize").Invoke(sizeGroup,new object[]{addedSize});oldSize=addedSize=-1;}
#endif
        }
        void OnDestroy(){Restore();Application.logMessageReceived-=Log;}
    }
}
#endif
