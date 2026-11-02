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
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TalesTactics
{
    // Real rendered HUD evidence. Fixture initialization is separate from gameplay-invariance checks.
    public sealed class BattleHudRedesignReview:MonoBehaviour
    {
        public const string Argument="--battle-hud-redesign-review";
        [Serializable] public sealed class Health
        {public string id;public bool selected,visible,detailed;public Rect bounds;public Vector2 anchor;public int hp;}
        [Serializable] public sealed class Row
        {
            public string scenario,state,file,active,statusName,skill;public int width,height,allies,buttons,raycasts,texts,randomDraws;
            public float textScale,orthographicSize;public Rect status,commands,viewport;public Vector2 actor;
            public bool facingHintOpen,objectiveVisible,statusVisible;public int forecastTargets;public Health[] health;public string[] issues;
        }
        [Serializable] public sealed class StateEvidence
        {
            public string phase,before,expected,after,beforeGrid,afterGrid;public int allies,width,height,beforeRandom,afterRandom;
            public bool stateMatches,gridMatches,randomMatches;
        }

        [Serializable] public sealed class Report
        {
            public int schema=1,expectedCaptures=32,completedCaptures;public bool complete,passed,userFilesUnchanged,campaignsUnchanged=true;
            public string status="Running",startedUtc,finishedUtc,output,storage,unityVersion;public Row[] rows;public StateEvidence[] states;public string[] errors,userFilesBefore,userFilesAfter;
            public string[] userFileHashExclusions=new[]{"Player.log","Player-prev.log"};
            public string scope="All allies receive their normal first selection before the strict browsing invariant, with exact expected TurnsStarted initialization checked and recorded. Two actual Director boards: main episode 1 with its two joined allies and an isolated Lv25 six-ally training fixture. Both at 1920x1080 and 1280x720, covering command, skills, ground facing, legal target forecasts, enemy hover, left/right camera pans and zoom with larger text. Thirty-two unedited ScreenCapture PNGs.";
            public string checks="Fixed status bounds; command/status separation; removal of persistent objective and facing instruction text; active button center raycasts; actual UI submit to skill/wait and cancel; visible glyph/frame/screen boundaries; short overhead labels; camera/selection/menu browsing state, grid, RNG and campaign invariance; hashes of all top-level user-storage files except the two explicitly listed Unity live logs.";
            public string limitations="Programmatic UI submit and Unity EventSystem raycasts are not operating-system mouse/keyboard automation. Only the exact case-insensitive top-level names Player.log and Player-prev.log are excluded from user-storage hashing; unknown files, backups and temporary files remain included. Nested files and directory paths require an external wrapper. Opening boards do not prove a campaign playthrough, balance, all resolutions, all six-person arrangements or manual visual approval. Edge rows record achieved actor positions because map panning remains bounded. Inspect PNGs for artwork and occlusion.";
        }
        readonly List<Row> rows=new List<Row>();readonly List<StateEvidence> states=new List<StateEvidence>();readonly List<string> errors=new List<string>();
        Report report=new Report();BattleDirector battle;string directory,storage,campaignBefore;Vector2Int requested;
        float oldScale;bool oldBackground,initialized,finished;double began;Rect fixedStatus;int allyCount;
#if UNITY_EDITOR
        readonly Dictionary<Vector2Int,int> editorSizes=new Dictionary<Vector2Int,int>();int originalEditorSize=-1;
        public static void BeginEditor(string output)
        {
            if(!Application.isPlaying||FindAnyObjectByType<BattleHudRedesignReview>()!=null)throw new InvalidOperationException("One active Play Mode HUD review is required.");
            ValidateOutput(output);new GameObject("Battle HUD redesign review").AddComponent<BattleHudRedesignReview>().directory=Path.GetFullPath(output);
            UnityEditor.SessionState.SetInt("TalesTactics.BattleHudRedesignReviewExit",-1);
        }
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,Argument);if(index<0)return;
            try
            {
                if(args.Count(a=>a==Argument)!=1||index+1>=args.Length||!Guid.TryParseExact(args[index+1],"N",out var id)||id==Guid.Empty)throw new ArgumentException("HUD review requires one non-empty N-format GUID.");
                string output=Path.Combine(Path.GetDirectoryName(Application.dataPath),"BattleHudRedesignReviews",args[index+1]);
                string resolved=ReviewStorageBootstrap.Resolve(args,Application.dataPath,Application.persistentDataPath);
                if(resolved==null||!SamePath(resolved,Path.Combine(output,"Storage")))throw new InvalidOperationException("ReviewStorageBootstrap must isolate HUD review storage before Director.Start.");
                ValidateOutput(output);new GameObject("Battle HUD redesign review").AddComponent<BattleHudRedesignReview>().directory=output;
            }
            catch(Exception e){Debug.LogError("HUD review initialization rejected: "+e);Application.Quit(2);}
        }
        static bool SamePath(string a,string b)=>string.Equals(Path.GetFullPath(a).TrimEnd('\\','/'),Path.GetFullPath(b).TrimEnd('\\','/'),StringComparison.OrdinalIgnoreCase);
        static void ValidateOutput(string path)
        {
            if(string.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path))throw new ArgumentException("Use an absolute, new output directory.");
            string root=Path.GetFullPath(path).TrimEnd('\\','/'),user=Path.GetFullPath(Application.persistentDataPath).TrimEnd('\\','/');
            if(SamePath(root,user)||root.StartsWith(user+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||user.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Output and user storage must not overlap.");
            if(Directory.Exists(root)&&Directory.EnumerateFileSystemEntries(root).Any())throw new InvalidOperationException("Output must be empty; previous evidence is never overwritten.");
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
        void Update(){if(initialized&&!finished&&Time.realtimeSinceStartupAsDouble-began>180){errors.Add("HUD review exceeded 180-second wall-clock limit.");StopAllCoroutines();Finish();}}
        IEnumerator Run()
        {
            Directory.CreateDirectory(directory);storage=Path.Combine(directory,"Storage");
            report.startedUtc=DateTime.UtcNow.ToString("o");report.unityVersion=Application.unityVersion;report.output=directory;report.storage=storage;
            Write();report.userFilesBefore=Snapshot(Application.persistentDataPath);Write();yield return null;
            battle=FindAnyObjectByType<BattleDirector>();Require(battle!=null&&battle.enabled&&battle.Catalog.CrossoverReady,"Production battle scene/catalog is required.");
            if(!Application.isEditor)Require(SamePath(battle.Profiles.Root,storage),"Director started outside this isolated run.");
            battle.ConfigureStorage(storage);battle.Audio.PersonalMusicEnabled=false;battle.Audio.SetVolumes(0,0);
            foreach(int count in new[]{2,6})foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})
            {
                allyCount=count;yield return Resolution(size);yield return BeginBoard();
                PrepareFirstSelections();yield return Settle();
                fixedStatus=Bounds(Panel("Unit"));
                string before=State(),grid=Grid();int draws=battle.Session.Resolver.RandomDraws;
                var original=battle.Session.Active;
                // A portrait submit really selects a different ally; the fixed card must not move.
                var other=battle.Session.Units.Last(u=>u.Team==Team.Player);
                Submit("아군 선택: "+other.Data.Id);yield return Settle();
                Require(battle.Session.Active==other,"Portrait submit did not select another ally.");Require(SameRect(fixedStatus,Bounds(Panel("Unit"))),"Status moved when selecting another ally.");
                Submit("아군 선택: "+original.Data.Id);yield return Settle();yield return Capture("command");
                Submit("Skill / 스킬");yield return Settle();Require(battle.State is ActionSelectionState,"Skill submit did not open the skill list.");yield return Capture("skills");
                battle.State.Cancel();yield return Settle();Require(battle.State is CommandState,"Skill cancel did not restore commands.");
                Submit("Wait / 방향 선택");yield return Settle();Require(battle.State is FacingSelectionState,"Wait submit did not open facing.");yield return Capture("facing");
                battle.State.Cancel();yield return Settle();Require(battle.State is CommandState,"Facing cancel did not restore commands.");
                yield return TargetPreview(original);
                var enemy=battle.Session.Units.First(u=>u.Team==Team.Enemy&&u.Alive&&!u.CrossoverReserve);
                battle.HoveredUnit=enemy;yield return Settle();
                Require(battle.Session.Active==original,"Enemy hover changed the active allied unit.");
                yield return Capture("enemy-hover");
                battle.HoveredUnit=null;yield return Settle();

                foreach(float target in new[]{.18f,.82f})
                {
                    battle.Board.ResetCamera();battle.Board.ZoomCamera(.7f);yield return Settle();
                    var origin=Actor();var middle=new Vector2(Screen.width*.5f,Screen.height*.5f);
                    battle.Board.BeginCameraDrag(middle);battle.Board.DragCamera(middle+new Vector2(Screen.width*target-origin.x,0));battle.Board.EndCameraDrag();yield return Settle();
                    Require(Mathf.Abs(Actor().x-origin.x)>20,"Camera pan did not meaningfully move the selected actor.");
                    yield return Capture(target<.5f?"left-edge":"right-edge");
                }
                battle.Board.ResetCamera();battle.Board.ZoomCamera(.55f);battle.Preferences.TextScale=1.3f;battle.Hud.Refresh();yield return Settle();
                // Text scaling may alter the card's dimensions; freeze a new baseline for this scale.
                fixedStatus=Bounds(Panel("Unit"));yield return Capture("zoom-text130");
                CheckState("browse-cancel-camera",before,grid,draws);
                bool unchanged=campaignBefore==JsonUtility.ToJson(battle.Campaign);report.campaignsUnchanged&=unchanged;Require(unchanged,"Presentation review mutated its campaign.");
            }
            report.complete=rows.Count==report.expectedCaptures;
        }
        void PrepareFirstSelections()
        {
            var team=battle.Session.Scheduler as TeamTurnScheduler;Require(team!=null,"The presentation fixture needs free allied turns.");
            var original=battle.Session.Active;string before=State(),grid=Grid();int draws=battle.Session.Resolver.RandomDraws;
            var expected=battle.Session.Units.Select(u=>new CheckpointUnit(u)).ToArray();
            for(int i=0;i<battle.Session.Units.Count;i++)
            {
                var unit=battle.Session.Units[i];if(unit.Team!=Team.Player)continue;
                Require(unit.Statuses.Count==0&&unit.Cooldowns.Count==0&&!unit.Moved&&!unit.Acted&&!unit.CanUndoMove&&!unit.FlamingChain&&!unit.ProtectionUsed&&!unit.CounterUsed&&!unit.SupportUsed,"First-selection fixture must begin with clean allied turn state.");
                if(!team.HasBegun(unit))expected[i].TurnsStarted++;
                Require(battle.SelectPlayerUnit(unit),"Could not initialize the ally's first selection: "+unit.Data.Id);
            }
            Require(battle.SelectPlayerUnit(original),"Could not restore original ally after first selections.");
            CheckState("fixture-first-selections",before,grid,draws,string.Join("\n",expected.Select(u=>JsonUtility.ToJson(u))));
        }
        void CheckState(string phase,string before,string grid,int draws,string expected=null)
        {
            var actual=State();var actualGrid=Grid();int actualDraws=battle.Session.Resolver.RandomDraws;
            var evidence=new StateEvidence{phase=phase,allies=allyCount,width=Screen.width,height=Screen.height,before=before,expected=expected??before,after=actual,
                beforeGrid=grid,afterGrid=actualGrid,beforeRandom=draws,afterRandom=actualDraws,stateMatches=actual==(expected??before),gridMatches=grid==actualGrid,randomMatches=draws==actualDraws};
            states.Add(evidence);Write();
            Require(evidence.stateMatches&&evidence.gridMatches&&evidence.randomMatches,
                phase+" invariant failed: state="+evidence.stateMatches+", grid="+evidence.gridMatches+", RNG="+evidence.randomMatches+". Full before/expected/after snapshots are in result.json.");
        }

        IEnumerator TargetPreview(UnitRuntime original)
        {
            // Find an authored, currently usable skill/target pair without moving or modifying any unit.
            // Enemy attacks are preferred. Episode 1 may require Mint's legal ally-healing forecast.
            var candidate=(from unit in battle.Session.Units.Where(u=>u.Team==Team.Player&&u.Alive)
                from skill in new[]{unit.Data.BasicAttack}.Concat(unit.Data.Skills).Where(s=>s!=null)
                where battle.Session.Resolver.CanUse(unit,skill)==null
                from target in battle.Session.Units.Where(t=>t.Alive&&!t.CrossoverReserve)
                where battle.Session.Resolver.InRange(unit,skill,target.Position)&&battle.Session.Resolver.Targets(unit,skill,target.Position).Any()
                orderby target.Team==Team.Enemy?0:1,skill==unit.Data.BasicAttack?0:1,unit.Data.Id,skill.Id
                select new{unit,skill,target}).FirstOrDefault();
            Require(candidate!=null,"No legal opening-board forecast exists.");
            Require(battle.SelectPlayerUnit(candidate.unit),"Could not select forecast actor.");
            battle.SelectSkill(candidate.skill);Require(battle.State is TargetSelectionState,"Authored skill did not open targeting.");
            battle.SelectTarget(candidate.target.Position);yield return Settle();
            Require(battle.Forecast!=null&&battle.Forecast.Rows.Count>0&&battle.Target==candidate.target.Position,"Legal target did not produce a real forecast.");
            yield return Capture("target-forecast");
            battle.State.Cancel();if(battle.State is ActionSelectionState)battle.State.Cancel();
            Require(battle.IsPlayerCommand,"Cancelling a forecast did not restore commands.");
            Require(battle.SelectPlayerUnit(original),"Could not restore original selected ally.");yield return Settle();
        }

        IEnumerator BeginBoard()
        {
            battle.Restart();yield return null;battle.Hud.HideTitle();battle.TrainingMode=allyCount==6;battle.TrainingObjective=ObjectiveKind.Eliminate;
            battle.SelectedStage=CrossoverCampaign.FirstStage;battle.Campaign=new CampaignSave{SelectedStage=battle.SelectedStage};
            foreach(var data in battle.Catalog.Characters)battle.Campaign.Get(data.Id).Level=allyCount==6?25:1;
            battle.Deployment.Clear();battle.Deployment.AddRange(Enumerable.Range(0,battle.Catalog.Characters.Length).Where(battle.CanDeploy).Take(allyCount));
            battle.Preferences.TextScale=1;battle.Preferences.SeenContextHints=3;battle.Preferences.Reactions=false;battle.Preferences.MissionEvents=false;battle.Campaign.AutoTiming=false;
            battle.PersistCampaign=save=>throw new InvalidOperationException("HUD review must not persist campaign fixtures.");
            battle.BeginBattle();Require(battle.Session!=null&&battle.IsPlayerCommand,"Director did not reach allied command state.");
            Require(battle.Session.Units.Count(u=>u.Team==Team.Player)==allyCount,"Fixture has wrong allied count.");
            battle.StopAllCoroutines();campaignBefore=JsonUtility.ToJson(battle.Campaign);yield return Settle();
        }
        string State()=>SkillRosterVfxFixture.State(battle.Session.Units);
        string Grid()=>SkillRosterVfxFixture.GridState(battle.Session.Grid,battle.Session.Units);
        RectTransform Panel(string name)=>(RectTransform)battle.Hud.transform.Find(name);
        Vector2 Actor()
        {var p=battle.Board.BattleCamera.WorldToScreenPoint(battle.Session.Grid[battle.Session.Active.Position].WorldPosition(battle.Catalog.Rules.TileHeight));return new Vector2(p.x,p.y);}
        void Submit(string name)
        {
            var button=battle.Hud.GetComponentsInChildren<Button>().Single(b=>b.name==name&&b.isActiveAndEnabled);
            Require(button.IsInteractable(),"Button disabled: "+name);Require(Hits(button),"Button center is blocked: "+name);
            ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
        }
        static bool Hits(Button button)
        {
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=Bounds((RectTransform)button.transform).center},hits);
            return hits.Count>0&&(hits[0].gameObject==button.gameObject||hits[0].gameObject.transform.IsChildOf(button.transform));
        }
        static bool Visible(Graphic graphic)=>graphic.isActiveAndEnabled&&graphic.canvasRenderer.GetInheritedAlpha()>.01f&&graphic.color.a>.01f;
        static Rect Bounds(RectTransform rect)
        {var c=new Vector3[4];rect.GetWorldCorners(c);return Rect.MinMaxRect(c.Min(p=>p.x),c.Min(p=>p.y),c.Max(p=>p.x),c.Max(p=>p.y));}
        static bool SameRect(Rect a,Rect b)=>Vector2.Distance(a.position,b.position)<1&&Vector2.Distance(a.size,b.size)<1;
        static bool Within(Rect inner,Rect outer,float tolerance=1)=>inner.xMin>=outer.xMin-tolerance&&inner.yMin>=outer.yMin-tolerance&&inner.xMax<=outer.xMax+tolerance&&inner.yMax<=outer.yMax+tolerance;
        IEnumerator Capture(string phase)
        {
            Require(rows.Count<report.expectedCaptures,"Capture budget exceeded.");yield return Settle();
            var issues=new List<string>();var status=Panel("Unit");var commands=Panel("Commands");var screen=new Rect(0,0,Screen.width,Screen.height);
            var statusRect=Bounds(status);var commandRect=Bounds(commands);
            bool targeting=battle.State is TargetSelectionState;
            if(!status.gameObject.activeInHierarchy&&!targeting)issues.Add("Fixed status card is hidden.");
            if(!SameRect(fixedStatus,statusRect))issues.Add("Fixed status card moved during "+phase);
            if(statusRect.center.x>Screen.width*.5f||statusRect.center.y>Screen.height*.5f)issues.Add("Status card is not bottom-left.");
            if(!Within(statusRect,screen)||!Within(commandRect,screen))issues.Add("HUD card outside screen.");
            if(status.gameObject.activeInHierarchy&&commandRect.Overlaps(statusRect))issues.Add("Command and status cards overlap.");
            if(targeting)
            {
                if(status.gameObject.activeInHierarchy)issues.Add("The forecast must replace the fixed status card during targeting.");
                if(commandRect.center.x>Screen.width*.5f||commandRect.yMin>Screen.height*.2f)issues.Add("Forecast is not anchored at bottom-left.");
                if(battle.Forecast==null||battle.Forecast.Rows.Count==0)issues.Add("Targeting has no actual forecast.");
            }
            if(phase=="enemy-hover")
            {
                var name=status.Find("UnitName")?.GetComponent<TMP_Text>();
                if(battle.HoveredUnit?.Team!=Team.Enemy||name==null||!name.text.Contains(battle.HoveredUnit.Data.DisplayName))issues.Add("Fixed status did not show the hovered enemy.");
            }
            if(battle.Hud.BattlefieldViewport.xMin>.05f)issues.Add("Old left-side viewport reservation remains.");
            if(battle.Hud.transform.Find("BattleCommandDock") is Transform old&&old.gameObject.activeInHierarchy)issues.Add("Old combined command dock remains.");
            bool objective=battle.Hud.GetComponentsInChildren<TMP_Text>().Any(t=>Visible(t)&&t.name=="Battle objective");
            if(objective)issues.Add("Persistent objective paragraph remains.");
            bool facing=battle.State is FacingSelectionState;
            if(battle.Hud.ActiveContextHint==ContextHintKind.FacingConfirm&&battle.Hud.ContextHintOpen)issues.Add("Facing instruction card remains.");
            var forbidden=new[]{"방향을 고르면 턴 완료","방향을 고르고 턴 완료","방향 선택 · Enter"};
            if(battle.Hud.GetComponentsInChildren<TMP_Text>().Any(t=>Visible(t)&&forbidden.Any(s=>t.text.Contains(s))))issues.Add("Redundant facing text remains.");
            if(facing&&commands.GetComponentsInChildren<FacingArrowGraphic>().Length!=4)issues.Add("Facing must have four ground directions.");
            int buttons=0,raycasts=0,texts=0;CheckUi(issues,ref buttons,ref raycasts,ref texts);
            var displays=battle.Board.GetComponentsInChildren<UnitHealthDisplay>().ToArray();
            foreach(var d in displays.Where(d=>d.Bar.enabled))
            {
                if(d.Detailed)issues.Add("Floating name/numeric HP remains: "+d.Unit.Data.Id);
                if(!Within(d.ScreenBounds,screen))issues.Add("HP outside screen: "+d.Unit.Data.Id);
                if(d.Bar.raycastTarget)issues.Add("Overhead health intercepts input: "+d.Unit.Data.Id);
            }
            if(Screen.width!=requested.x||Screen.height!=requested.y)issues.Add("Requested capture resolution mismatch.");
            var row=new Row{scenario=phase,state=battle.State.GetType().Name,active=battle.Session.Active.Data.Id,width=Screen.width,height=Screen.height,allies=allyCount,
                buttons=buttons,raycasts=raycasts,texts=texts,randomDraws=battle.Session.Resolver.RandomDraws,textScale=battle.Preferences.TextScale,orthographicSize=battle.Board.BattleCamera.orthographicSize,
                status=statusRect,commands=commandRect,viewport=battle.Hud.BattlefieldViewport,actor=Actor(),
                statusVisible=status.gameObject.activeInHierarchy,statusName=status.Find("UnitName")?.GetComponent<TMP_Text>()?.text,skill=targeting?battle.SelectedSkill?.Id:null,forecastTargets=battle.Forecast?.TargetCount??0,facingHintOpen=battle.Hud.ContextHintOpen,objectiveVisible=objective,
                health=displays.Select(d=>new Health{id=d.Unit.Data.Id,selected=d.Unit==battle.Session.Active,visible=d.Bar.enabled,detailed=d.Detailed,bounds=d.ScreenBounds,anchor=d.ScreenOrigin,hp=d.Unit.CurrentHP}).ToArray(),issues=issues.ToArray()};
            row.file=string.Format("{0:D2}-{1}-allies{2}-{3}x{4}.png",rows.Count,phase,allyCount,Screen.width,Screen.height);
            yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(directory,row.file),texture.EncodeToPNG());Destroy(texture);
            rows.Add(row);Write();
        }
        void CheckUi(List<string> issues,ref int buttons,ref int raycasts,ref int texts)
        {
            var screen=new Rect(0,0,Screen.width,Screen.height);
            foreach(var button in battle.Hud.GetComponentsInChildren<Button>().Where(b=>b.isActiveAndEnabled&&b.IsInteractable()))
            {
                buttons++;if(!Within(Bounds((RectTransform)button.transform),screen))issues.Add("Button outside screen: "+button.name);
                if(Hits(button))raycasts++;else issues.Add("Button center blocked: "+button.name);
            }
            foreach(var text in battle.Hud.GetComponentsInChildren<TMP_Text>().Where(Visible))
            {
                texts++;text.ForceMeshUpdate();if(text.isTextOverflowing)issues.Add("Text overflowing: "+text.name+" / "+text.text);
                var button=text.GetComponentInParent<Button>();var frame=text.GetComponentInParent<FramedContent>();
                foreach(var glyph in text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(g=>g.isVisible))
                {
                    var lo=text.transform.TransformPoint(glyph.bottomLeft);var hi=text.transform.TransformPoint(glyph.topRight);
                    if(!screen.Contains(lo)||!screen.Contains(hi)){issues.Add("Glyph outside screen: "+text.text);break;}
                    if(button!=null)
                    {
                        var r=(RectTransform)button.transform;var allowed=r.rect;allowed.xMin-=1;allowed.xMax+=1;allowed.yMin-=1;allowed.yMax+=1;
                        if(!allowed.Contains(r.InverseTransformPoint(lo))||!allowed.Contains(r.InverseTransformPoint(hi))){issues.Add("Glyph outside button: "+button.name);break;}
                    }
                    if(frame!=null&&frame.GetComponent<Image>()?.enabled==true)
                    {
                        var allowed=frame.enabled?frame.ContentRect:((RectTransform)frame.transform).rect;
                        if(!frame.enabled&&(frame.name=="Unit"||frame.name=="Commands")){allowed.xMin+=14;allowed.xMax-=14;allowed.yMin+=14;allowed.yMax-=14;}
                        allowed.xMin-=2;allowed.xMax+=2;allowed.yMin-=2;allowed.yMax+=2;
                        if(!allowed.Contains(frame.transform.InverseTransformPoint(lo))||!allowed.Contains(frame.transform.InverseTransformPoint(hi))){issues.Add("Glyph outside frame: "+frame.name+" / "+text.text);break;}
                    }
                }
            }
        }
        static IEnumerator Settle(){yield return null;yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();}
        IEnumerator Resolution(Vector2Int size)
        {
            requested=size;
#if UNITY_EDITOR
            SetEditorResolution(size);
#else
            Screen.SetResolution(size.x,size.y,size.x==1920&&size.y==1080?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
#endif
            double until=Time.realtimeSinceStartupAsDouble+10;
            while((Screen.width!=size.x||Screen.height!=size.y)&&Time.realtimeSinceStartupAsDouble<until)yield return null;
            Require(Screen.width==size.x&&Screen.height==size.y,"Requested resolution not applied: "+size);yield return Settle();
        }
        static string[] Snapshot(string root)
        {
            if(!Directory.Exists(root))return Array.Empty<string>();
            // Unity's two live logs are volatile process output, not saves; a running game may lock them.
            // All other top-level files remain protected, including unknown names, backups and .tmp files.
            using(var sha=SHA256.Create())return Directory.GetFiles(root)
                .Where(p=>!string.Equals(Path.GetFileName(p),"Player.log",StringComparison.OrdinalIgnoreCase)&&!string.Equals(Path.GetFileName(p),"Player-prev.log",StringComparison.OrdinalIgnoreCase))
                .OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).Select(p=>Path.GetFileName(p)+":"+BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant()).ToArray();
        }
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        void Write(){report.rows=rows.ToArray();report.states=states.ToArray();report.errors=errors.ToArray();report.completedCaptures=rows.Count;File.WriteAllText(Path.Combine(directory,"result.json"),JsonUtility.ToJson(report,true));}
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
        void Finish()
        {
            if(finished)return;finished=true;Application.logMessageReceived-=Log;int exit=1;
            try
            {
                battle?.StopAllCoroutines();report.finishedUtc=DateTime.UtcNow.ToString("o");report.userFilesAfter=Snapshot(Application.persistentDataPath);
                report.userFilesUnchanged=report.userFilesBefore!=null&&report.userFilesBefore.SequenceEqual(report.userFilesAfter);
                if(!report.userFilesUnchanged)errors.Add("User save/settings files changed; review did not restore them.");
                report.passed=report.complete&&report.userFilesUnchanged&&report.campaignsUnchanged&&errors.Count==0&&rows.All(r=>r.issues.Length==0);report.status=report.passed?"Passed":"Stopped or failed";Write();exit=report.passed?0:1;
            }
            catch(Exception e)
            {
                errors.Add("HUD review finalization failed: "+e);report.passed=false;report.status="Stopped or failed";report.finishedUtc=DateTime.UtcNow.ToString("o");
                try{Write();}catch(Exception writeError){Debug.LogError("HUD review failure evidence could not be written: "+writeError); }
                Debug.LogError("HUD review finalization failed: "+e);
            }
            finally
            {
                // A hidden standalone player must keep ticking until deferred Quit completes.
                // Editor restores immediately; standalone restores during OnDestroy.
                if(Application.isEditor){Time.timeScale=oldScale;Application.runInBackground=oldBackground;}
#if UNITY_EDITOR
                try{RestoreEditorResolution();}catch(Exception e){exit=1;report.passed=false;report.status="Stopped or failed";errors.Add("Resolution restore failed: "+e);try{Write();}catch(Exception writeError){Debug.LogError(writeError);}}
                finally{UnityEditor.SessionState.SetInt("TalesTactics.BattleHudRedesignReviewExit",exit);UnityEditor.EditorApplication.isPlaying=false;}
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
            if(!editorSizes.TryGetValue(size,out int index)){var entry=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),size.x,size.y,"HUD redesign review "+size});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{entry});index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;editorSizes.Add(size,index);}
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
