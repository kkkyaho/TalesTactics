#if UNITY_EDITOR || TALESTACTICS_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace TalesTactics
{
    // Explicitly invoked fixture: never touches the user's campaign and never ships normally.
    public sealed class CombatClarityReview:MonoBehaviour
    {
        [Serializable] sealed class Element { public string name,text;public Rect bounds; }
        [Serializable] sealed class Row
        {
            public string scenario,skill,state,capture;public int width,height,rotation,allies,enemyUnits,models,numbers;
            public float textScale;public bool actionMessage,hintOpen,modal;public string hint,message;
            public Element[] health,feedback,labels;public string[] issues;
        }
        [Serializable] sealed class Report
        {
            public string fixture="Actual production six-member session; units relocated for crowding; display APIs injected without damage or rewards.";
            public string[] limits={"Not a whole-campaign playthrough or DPI/performance test.","Hint resets use review-only reflection; success uses gameplay APIs.","Captures are engine frames; synthetic feedback values are presentation fixtures."};
            public Row[] rows;public string[] errors;public bool passed;
        }
        readonly List<Row> rows=new List<Row>();readonly List<string> errors=new List<string>();
        BattleDirector battle;string directory,storage;bool oldBackground,finished;int rotation;
#if UNITY_EDITOR
        readonly Dictionary<Vector2Int,int> editorSizes=new Dictionary<Vector2Int,int>();int originalEditorSize=-1;
        public static void BeginEditor(string output)
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Enter play mode before starting the review.");
            if(FindAnyObjectByType<CombatClarityReview>()!=null)throw new InvalidOperationException("A clarity review is already running.");
            new GameObject("Combat clarity review").AddComponent<CombatClarityReview>().directory=Path.GetFullPath(output);
        }
        void SetEditorResolution(Vector2Int size)
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;var sizes=assembly.GetType("UnityEditor.GameViewSizes");
            var instance=sizes.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
            var group=sizes.GetProperty("currentGroup").GetValue(instance);var type=assembly.GetType("UnityEditor.GameView");
            var window=UnityEditor.EditorWindow.GetWindow(type);var selected=type.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            if(originalEditorSize<0)originalEditorSize=(int)selected.GetValue(window);
            if(!editorSizes.TryGetValue(size,out int index))
            {
                var entry=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),size.x,size.y,"Combat clarity "+size});
                group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{entry});index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;editorSizes.Add(size,index);
            }
            selected.SetValue(window,index);window.Focus();window.Repaint();
        }
        void RestoreEditorResolution()
        {
            if(editorSizes.Count==0)return;
            var assembly=typeof(UnityEditor.Editor).Assembly;var sizes=assembly.GetType("UnityEditor.GameViewSizes");
            var instance=sizes.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
            var group=sizes.GetProperty("currentGroup").GetValue(instance);var type=assembly.GetType("UnityEditor.GameView");
            var window=UnityEditor.EditorWindow.GetWindow(type);
            if(originalEditorSize>=0)type.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(window,originalEditorSize);
            foreach(int index in editorSizes.Values.OrderByDescending(i=>i))group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{index});editorSizes.Clear();
        }
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--combat-clarity-review");if(i<0)return;
            if(i+1>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)){Application.Quit(2);return;}
            new GameObject("Combat clarity review").AddComponent<CombatClarityReview>().directory=Path.Combine(Path.GetDirectoryName(Application.dataPath),"ClarityReviews",args[i+1]);
        }
        IEnumerator Start()
        {
            oldBackground=Application.runInBackground;Application.runInBackground=true;Directory.CreateDirectory(directory);Application.logMessageReceived+=Log;
            // Catch nested coroutine exceptions so an aborted fixture still leaves an honest report.
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                object current=null;bool next=false;Exception failure=null;
                try{next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){errors.Add(failure.ToString());break;}
                if(!next){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
            Finish();
        }
        IEnumerator Run()
        {
            yield return null;battle=FindAnyObjectByType<BattleDirector>();if(battle==null)throw new InvalidOperationException("Battle scene not loaded.");
            storage=Path.Combine(directory,"Storage",Guid.NewGuid().ToString("N"));battle.ConfigureStorage(storage);battle.Audio.PersonalMusicEnabled=false;battle.Audio.SetVolumes(0,0);
            battle.Hud.ShowTitle();battle.StartTitleGame(0);
            for(int episode=1;episode<20;episode++)if(!battle.Campaign.StoryProgress.Contains(CrossoverCampaign.Flag(episode)))battle.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(episode));
            foreach(var data in battle.Catalog.Characters)battle.Campaign.Get(data.Id).Level=25;
            // Episode 5 permits every catalog ally; episode 1 always normalizes deployment to Cless and Mint.
            battle.SelectedStage=CrossoverCampaign.FirstStage+4;battle.TrainingMode=false;battle.Deployment.Clear();battle.Deployment.AddRange(new[]{0,1,2,3,4,5});
            battle.BeginBattle();yield return null;battle.StopAllCoroutines();
            if(battle.Session==null)throw new InvalidOperationException("Six-member production fixture did not start.");
            Require(battle.Session.Units.Count(u=>u.Team==Team.Player)==6,"Production episode 5 did not retain six deployed allies.");
            Cluster();battle.Preferences.SeenContextHints=7;battle.Message="";battle.Hud.UpdateBattleMessageText();
            var longest=battle.Session.Units.SelectMany(u=>u.Data.Skills.Where(s=>s!=null&&!s.IsUltimate&&s.Animation!=AnimationKind.Attack).Select(s=>new {Unit=u,Skill=s})).OrderByDescending(x=>x.Skill.DisplayName.Length).First();
            bool currentWindow=!Application.isEditor&&Environment.GetCommandLineArgs().Contains("--review-current-window");
            var sizes=currentWindow?new[]{new Vector2Int(Screen.width,Screen.height)}:new[]{new Vector2Int(1024,768),new Vector2Int(1920,1080),new Vector2Int(1080,1440)};
            foreach(var size in sizes)foreach(float scale in new[]{1f,1.3f})
            {
#if UNITY_EDITOR
                SetEditorResolution(size);
#else
                if(!currentWindow)Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
#endif
                yield return new WaitForSecondsRealtime(.4f);battle.Preferences.TextScale=scale;battle.Board.ResetCamera();
                for(rotation=0;rotation<4;rotation++)
                {
                    if(rotation>0)battle.Board.RotateCamera(90);
                    battle.Hud.CombatFeedback.Clear();battle.Preferences.SeenContextHints=7;battle.SetState(new ActionExecutionState(battle));
                    battle.Hud.ShowBattleSkillCue(longest.Unit,longest.Skill,2);
                    var targets=battle.Session.Units.Where(u=>u.Team==Team.Player&&u.Alive).Take(2).Concat(battle.Session.Units.Where(u=>u.Team==Team.Enemy&&u.Alive&&!u.CrossoverReserve).Take(4)).ToArray();
                    for(int n=0;n<targets.Length;n++)battle.Hud.CombatFeedback.Number(targets[n],targets[n].Team==Team.Player?48:-(112+n*17),false);
                    yield return Settle();yield return Inspect("crowded-feedback",size,scale,longest.Skill.DisplayName,expectNumbers:6,expectCue:true);
                    battle.Hud.CombatFeedback.Clear();
                }
                rotation=0;battle.Board.ResetCamera();battle.SetState(new CommandState(battle));battle.Message="";battle.Hud.UpdateBattleMessageText();yield return new WaitForSecondsRealtime(2.1f);
                if(currentWindow||(size.x==1920&&scale==1)||(size.x==1024&&scale>1.2f))
                {
                    yield return CheckMessages(size,scale);
                    foreach(var kind in new[]{ContextHintKind.MapPan,ContextHintKind.EnemySkills,ContextHintKind.FacingConfirm})yield return CheckHint(kind,size,scale);
                    yield return CheckDismiss(size,scale);
                    battle.Preferences.SeenContextHints=7;battle.Message="";battle.SetState(new CommandState(battle));
                }
            }
        }
        void Cluster()
        {
            var units=battle.Session.Units.Where(u=>u.Alive&&!u.CrossoverReserve).ToArray();
            foreach(var tile in battle.Session.Grid.Tiles.Values)tile.Occupant=null;
            var center=new Vector2Int(6,6);var tiles=battle.Session.Grid.Tiles.Values.Where(t=>t.Walkable).OrderBy(t=>(t.Coordinate-center).sqrMagnitude).ThenBy(t=>t.Coordinate.y).ThenBy(t=>t.Coordinate.x).Take(units.Length).ToArray();
            if(tiles.Length!=units.Length)throw new InvalidOperationException("Insufficient walkable crowding tiles.");
            for(int i=0;i<units.Length;i++)if(!battle.Session.Grid.Place(units[i],tiles[i].Coordinate))throw new InvalidOperationException("Cannot place crowd unit "+i);
            battle.Session.Active=units.First(u=>u.Team==Team.Player);battle.SetState(new CommandState(battle));battle.RefreshViews();
        }
        IEnumerator CheckMessages(Vector2Int size,float scale)
        {
            battle.ShowActionFeedback("크레스 마신검 · 적에게 128 피해");yield return Settle();yield return Inspect("action-result-visible",size,scale,extra:()=>Require(battle.ActionFeedbackVisible,"Action message did not appear."));
            yield return new WaitForSecondsRealtime(BattleDirector.ActionFeedbackDuration+.2f);yield return Settle();yield return Inspect("action-result-expired",size,scale,extra:()=>Require(!battle.ActionFeedbackVisible&&string.IsNullOrEmpty(battle.Message),"Old action text persisted past its duration."));
            battle.ShowActionFeedback("이전 기술 결과");battle.MoveCommand();yield return Settle();yield return Inspect("action-result-navigation-cleared",size,scale,extra:()=>Require(!battle.ActionFeedbackVisible&&!battle.Message.Contains("이전 기술"),"Navigation retained the previous action result."));battle.State.Cancel();
            battle.Message="사거리 밖의 대상입니다.";battle.Hud.UpdateBattleMessageText();yield return new WaitForSecondsRealtime(BattleDirector.ActionFeedbackDuration+.2f);
            yield return Inspect("persistent-validation-message",size,scale,extra:()=>Require(battle.Message=="사거리 밖의 대상입니다.","A persistent validation message was timed out."));battle.Message="";battle.Hud.UpdateBattleMessageText();
        }
        void ResetHint(ContextHintKind kind)
        {
            battle.StopAllCoroutines();battle.Hud.CloseUnitDetails();battle.Hud.CloseHelp();battle.Hud.CombatFeedback.Clear();
            // Each hint fixture begins a fresh player phase. A real facing confirmation may consume a unit,
            // but must not advance later review rows into an enemy turn.
            var units=battle.Session.Units;var playerIndices=Enumerable.Range(0,units.Count).Where(i=>units[i].Team==Team.Player&&units[i].CanTakeTurn).ToArray();
            Require(playerIndices.Length==6,"Hint fixture requires all six living allies.");
            var active=units[playerIndices[0]];foreach(int i in playerIndices){units[i].Moved=false;units[i].Acted=false;units[i].FlamingChain=false;}
            ((TeamTurnScheduler)battle.Session.Scheduler).Restore(units,playerIndices,new[]{playerIndices[0]},1,active);battle.Session.Active=active;
            battle.SetState(new CommandState(battle));battle.Message="";battle.Hud.UpdateBattleMessageText();
            Private(battle,"learnedContextHints",0);battle.Preferences.SeenContextHints=7^(int)kind;
            battle.Hud.HideContextHints();Private(battle.Hud,"contextHintReadyAt",0f);Private(battle.Hud,"shownContextHint",ContextHintKind.None);
            if(kind==ContextHintKind.FacingConfirm)battle.WaitCommand();battle.Hud.UpdateContextHints();
        }
        IEnumerator CheckHint(ContextHintKind kind,Vector2Int size,float scale)
        {
            ResetHint(kind);yield return new WaitForSecondsRealtime(1.7f);yield return Settle();
            yield return Inspect("hint-"+kind,size,scale,extra:()=>Require(battle.Hud.ActiveContextHint==kind,"Expected hint "+kind+" was not visible."));
            if(kind==ContextHintKind.MapPan)
            {
                battle.Hud.ShowHelp();yield return Settle();yield return Inspect("hint-hidden-by-modal",size,scale,expectHidden:true,extra:()=>Require(!battle.Hud.ContextHintOpen,"Hint is visible over help."));battle.Hud.CloseHelp();
                var viewport=battle.Hud.BattlefieldViewport;var from=new Vector2(viewport.center.x*Screen.width,viewport.center.y*Screen.height);var to=from+new Vector2(48,24);
                var old=battle.Board.CameraPanOffset;battle.HandleCameraDrag(from,true,true);battle.HandleCameraDrag(to,false,true);battle.HandleCameraDrag(to,false,false);
                Require((battle.Board.CameraPanOffset-old).sqrMagnitude>.001f,"Map-pan completion fixture did not move the camera.");
            }
            else if(kind==ContextHintKind.EnemySkills)
            {
                var enemy=battle.Session.Units.First(u=>u.Team==Team.Enemy&&u.Alive&&!u.CrossoverReserve);battle.Hud.ShowEnemySummary(enemy);yield return Settle();
                Require(!battle.HasLearnedContextHint(kind),"Opening a summary must not count as learning skills.");battle.Hud.ShowUnitSkills(enemy);yield return Settle();battle.Hud.CloseUnitDetails();
            }
            else battle.ChooseFacing(battle.PendingFacing);
            yield return Settle();
            yield return Inspect("hint-completed-"+kind,size,scale,extra:()=>{Require(battle.HasLearnedContextHint(kind),"Successful operation did not mark hint learned.");Require(!battle.Hud.ContextHintOpen,"Completed hint stayed open.");Require((new PreferenceFile(Path.Combine(storage,"settings.json")).Load().SeenContextHints&(int)kind)!=0,"Learned operation was not persisted.");});
            battle.Board.ResetCamera();
        }
        IEnumerator CheckDismiss(Vector2Int size,float scale)
        {
            ResetHint(ContextHintKind.MapPan);yield return new WaitForSecondsRealtime(1.7f);yield return Settle();
            Require(battle.Hud.ActiveContextHint==ContextHintKind.MapPan,"Dismiss fixture did not reach the map hint in a player command state.");
            var close=battle.Hud.GetComponentsInChildren<Button>().Single(b=>b.name=="조작 안내 닫기");close.onClick.Invoke();yield return Settle();
            yield return Inspect("hint-dismissed",size,scale,extra:()=>{Require(battle.HasLearnedContextHint(ContextHintKind.MapPan)&&!battle.Hud.ContextHintOpen,"Dismiss did not keep hint closed.");Require((new PreferenceFile(Path.Combine(storage,"settings.json")).Load().SeenContextHints&1)!=0,"Dismiss was not persisted.");});
        }
        static void Private(object target,string name,object value)
        {var field=target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);if(field==null)throw new MissingFieldException(target.GetType().Name,name);field.SetValue(target,value);}
        static void Require(bool condition,string text){if(!condition)throw new InvalidOperationException(text);}
        static IEnumerator Settle(){yield return null;yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();}
        static Rect Bounds(RectTransform rect)
        {var p=new Vector3[4];rect.GetWorldCorners(p);return Rect.MinMaxRect(p.Min(v=>v.x),p.Min(v=>v.y),p.Max(v=>v.x),p.Max(v=>v.y));}
        static bool Visible(Graphic graphic)=>graphic.isActiveAndEnabled&&graphic.canvasRenderer.GetInheritedAlpha()>.01f&&graphic.color.a>.01f;
        static bool Collide(Rect a,Rect b){a.xMin+=.5f;a.xMax-=.5f;a.yMin+=.5f;a.yMax-=.5f;return a.width>0&&a.height>0&&a.Overlaps(b);}
        IEnumerator Inspect(string name,Vector2Int requested,float scale,string skill="",int expectNumbers=-1,bool expectCue=false,bool expectHidden=false,Action extra=null)
        {
            var issues=new List<string>();try{extra?.Invoke();}catch(Exception e){issues.Add(e.Message);}
            if(Screen.width!=requested.x||Screen.height!=requested.y)issues.Add("Requested resolution mismatch.");
            var health=battle.Board.GetComponentsInChildren<UnitHealthDisplay>().Where(d=>d.Bar.enabled).ToArray();
            var bars=health.Select(d=>new Element{name=d.name,text=d.Detailed?d.GetComponentInChildren<TMP_Text>().text:"",bounds=d.ScreenBounds}).ToArray();
            var feedback=battle.Hud.CombatFeedback.NumberScreenBounds.Select((r,i)=>new Element{name="Number "+i,bounds=r}).ToArray();
            var labels=battle.Hud.GetComponentsInChildren<TMP_Text>().Where(t=>Visible(t)&&(t.name.StartsWith("Battle skill ")||t.name.StartsWith("ContextHint"))).Select(t=>new Element{name=t.name,text=t.text,bounds=Bounds(t.rectTransform)}).ToArray();
            if(expectNumbers>=0&&feedback.Length!=expectNumbers)issues.Add("Expected "+expectNumbers+" numbers; got "+feedback.Length);
            if(expectCue&&!battle.Hud.BattleSkillCueVisible)issues.Add("Skill cue is missing.");
            if(expectHidden&&(bars.Length>0||battle.Hud.BattleSkillCueVisible||battle.Hud.ContextHintOpen))issues.Add("World feedback is visible behind a modal.");
            if(battle.Session.Units.Count(u=>u.Team==Team.Player)!=6)issues.Add("Fixture does not have six allies.");
            foreach(var e in bars.Concat(feedback).Concat(labels))if(e.bounds.xMin < -1||e.bounds.yMin < -1||e.bounds.xMax>Screen.width+1||e.bounds.yMax>Screen.height+1)issues.Add("Offscreen: "+e.name);
            var blocks=bars.Concat(feedback).ToArray();for(int i=0;i<blocks.Length;i++)for(int j=i+1;j<blocks.Length;j++)if(Collide(blocks[i].bounds,blocks[j].bounds))issues.Add("Feedback overlap: "+blocks[i].name+" / "+blocks[j].name);
            if(battle.Hud.BattleSkillCueVisible)foreach(var e in blocks)if(Collide(e.bounds,battle.Hud.BattleSkillCueScreenBounds))issues.Add("Skill cue overlap: "+e.name);
            foreach(var text in battle.Hud.GetComponentsInChildren<TMP_Text>().Where(t=>Visible(t)&&(t.name.StartsWith("Battle skill ")||t.name.StartsWith("ContextHint"))))
            {
                text.ForceMeshUpdate();if(text.isTextOverflowing)issues.Add("Text overflow: "+text.name);
                if(text.raycastTarget)issues.Add("Informational text intercepts input: "+text.name);
                var parent=text.GetComponentInParent<FramedContent>();if(parent!=null)
                    foreach(var glyph in text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(g=>g.isVisible))
                    {var allowed=parent.ContentRect;allowed.xMin-=1;allowed.xMax+=1;allowed.yMin-=1;allowed.yMax+=1;if(!allowed.Contains(parent.transform.InverseTransformPoint(text.transform.TransformPoint(glyph.bottomLeft)))||!allowed.Contains(parent.transform.InverseTransformPoint(text.transform.TransformPoint(glyph.topRight)))){issues.Add("Text outside frame: "+text.name);break;}}
            }
            if(battle.Hud.ContextHintOpen)
            {
                var card=battle.Hud.transform.Find("ContextHintCard");if(card==null)issues.Add("Hint card missing.");else
                {
                    foreach(var graphic in card.GetComponentsInChildren<Graphic>())if(graphic.raycastTarget&&graphic.GetComponent<Button>()==null)issues.Add("Hint body blocks map input: "+graphic.name);
                    var body=card.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="ContextHintBody");var hits=new List<RaycastResult>();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=Bounds(body.rectTransform).center},hits);
                    if(hits.Any(h=>h.gameObject.transform.IsChildOf(card)))issues.Add("Hint body raycast intercepted map input.");
                }
            }
            if(battle.Board.GetComponentsInChildren<TextMeshPro>().Any(t=>t.name=="Skill name"))issues.Add("Old world-space skill label remains.");
            var row=new Row{scenario=name,skill=skill,state=battle.State.GetType().Name,width=Screen.width,height=Screen.height,rotation=rotation*90,textScale=scale,allies=battle.Session.Units.Count(u=>u.Team==Team.Player),enemyUnits=battle.Session.Units.Count(u=>u.Team==Team.Enemy),models=battle.Board.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.name=="Directional Sprite"),numbers=feedback.Length,actionMessage=battle.ActionFeedbackVisible,hintOpen=battle.Hud.ContextHintOpen,hint=battle.Hud.ActiveContextHint.ToString(),modal=battle.Hud.InputModalOpen,message=battle.DisplayBattleMessage,health=bars,feedback=feedback,labels=labels,issues=issues.ToArray()};
            row.capture=string.Format("{0:D3}-{1}-{2}x{3}-{4}-r{5}.png",rows.Count,name,Screen.width,Screen.height,Mathf.RoundToInt(scale*100),rotation*90);
            yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(directory,row.capture),texture.EncodeToPNG());Destroy(texture);
            rows.Add(row);WriteReport("progress.json");
        }
        void WriteReport(string name)=>File.WriteAllText(Path.Combine(directory,name),JsonUtility.ToJson(new Report{rows=rows.ToArray(),errors=errors.ToArray(),passed=errors.Count==0&&rows.Count>0&&rows.All(r=>r.issues.Length==0)},true));
        void Log(string message,string trace,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors.Add(message+"\n"+trace);if(!string.IsNullOrEmpty(directory))File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);}}
        void Finish()
        {
            if(finished)return;finished=true;WriteReport("report.json");Application.runInBackground=oldBackground;Application.logMessageReceived-=Log;
            int exitCode=errors.Count==0&&rows.Count>0&&rows.All(r=>r.issues.Length==0)?0:1;
#if UNITY_EDITOR
            RestoreEditorResolution();UnityEditor.SessionState.SetInt("TalesTactics.CombatClarityReviewExit",exitCode);UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit(exitCode);
#endif
        }
        void OnDestroy(){Application.logMessageReceived-=Log;Application.runInBackground=oldBackground;}
    }
}
#endif
