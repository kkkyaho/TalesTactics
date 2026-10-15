#if UNITY_EDITOR || TALESTACTICS_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
namespace TalesTactics
{
    // Opt-in player verification; never included in the normal Windows build.
    public sealed class UiSafeLayoutReview:MonoBehaviour
    {
        [Serializable] sealed class Row { public string screen;public int width,height;public float textScale;public string[] issues; }
        [Serializable] sealed class Report { public Row[] rows;public string[] errors; }
        readonly List<Row> rows=new List<Row>();readonly List<string> errors=new List<string>();
        BattleDirector battle;string directory;
#if UNITY_EDITOR
        public static void BeginEditor(string output)
        {
            var review=new GameObject("Title screen review").AddComponent<UiSafeLayoutReview>();review.directory=output;
        }
        readonly Dictionary<Vector2Int,int> editorSizes=new Dictionary<Vector2Int,int>();
        void SetEditorResolution(Vector2Int size)
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;var sizes=assembly.GetType("UnityEditor.GameViewSizes");
            var instance=sizes.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
            var group=sizes.GetProperty("currentGroup").GetValue(instance);
            if(!editorSizes.TryGetValue(size,out int index))
            {
                var entry=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),size.x,size.y,"Title screen "+size});
                group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{entry});index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;editorSizes.Add(size,index);
            }
            var type=assembly.GetType("UnityEditor.GameView");var window=UnityEditor.EditorWindow.GetWindow(type);
            type.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(window,index);window.Focus();window.Repaint();
        }
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--ui-safe-layout-review");if(i<0)return;
            if(i+1>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)){Application.Quit(2);return;}
            var review=new GameObject("Title screen review").AddComponent<UiSafeLayoutReview>();
            review.directory=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Reviews",args[i+1]);
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(directory);Application.runInBackground=true;Application.logMessageReceived+=Log;yield return null;
            battle=UnityEngine.Object.FindAnyObjectByType<BattleDirector>();battle.ConfigureStorage(Path.Combine(directory,Guid.NewGuid().ToString("N")));battle.Audio.PersonalMusicEnabled=false;
            battle.Hud.ShowTitle();battle.StartTitleGame(0);
#if UNITY_EDITOR
            if(Environment.GetCommandLineArgs().Contains("--bake-menu-previews"))yield return MenuPreviewBake.Run(battle,Path.Combine(directory,"Previews"));
#endif
            battle.BeginBattle();yield return null;battle.SuspendBattle();
            var second=new CampaignSave{SelectedStage=CrossoverCampaign.FirstStage+1,SavedAt=DateTime.UtcNow.ToString("o")};second.StoryProgress.Add(CrossoverCampaign.Flag(1));second.Get("cless").Level=4;
            new CampaignFile(battle.Profiles.PathFor(1)).Save(second);
            var original=Directory.GetFiles(battle.Profiles.Root,"campaign*").ToDictionary(p=>p,File.ReadAllText);
            bool currentWindow=!Application.isEditor&&Environment.GetCommandLineArgs().Contains("--review-current-window");
            var reviewSizes=currentWindow?new[]{new Vector2Int(Screen.width,Screen.height)}:
                new[]{new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(1920,1080),new Vector2Int(2560,1080),new Vector2Int(1080,1440)};
            foreach(var size in reviewSizes)
            foreach(float scale in new[]{1f,1.15f,1.3f})
            {
#if UNITY_EDITOR
                SetEditorResolution(size);
#else
                if(!currentWindow)Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
#endif
                yield return new WaitForSecondsRealtime(.5f);battle.Preferences.TextScale=scale;battle.Hud.ShowTitle();yield return Settle();
                yield return Inspect("title","TitleScreen",size,scale);
                Click("타이틀 불러오기");yield return Settle();yield return Inspect("load","TitleScreen/TitleSlotsOverlay/TitleSlotsWindow",size,scale);
                battle.Hud.CancelTitle();Click("타이틀 새 게임");yield return Settle();yield return Inspect("new","TitleScreen/TitleSlotsOverlay/TitleSlotsWindow",size,scale);
                Click("타이틀 슬롯 3");yield return Settle();yield return Inspect("new-confirm","TitleScreen/TitleConfirm",size,scale);
                battle.Hud.CancelTitle();Click("타이틀 슬롯 1");yield return Settle();yield return Inspect("overwrite","TitleScreen/TitleConfirm",size,scale);
                battle.Hud.CancelTitle();battle.Hud.CancelTitle();
                new CampaignFile(battle.Profiles.PathFor(1)).Save(second);battle.LoadTitleGame(1,false);yield return Settle();yield return Inspect("hub",battle.Hud.transform,size,scale);
                Click("거점 적 정보");yield return Settle();yield return Inspect("hub-enemies",battle.Hud.transform,size,scale);
                Click("거점 목표");yield return Settle();
                battle.Hud.ShowMission();yield return Settle();yield return Inspect("mission",battle.Hud.transform,size,scale);
                Click("적 정보");yield return Settle();yield return Inspect("enemies",battle.Hud.transform,size,scale);
                Click("적 상세 보기");yield return Settle();yield return Inspect("enemy-detail",battle.Hud.transform,size,scale);battle.Hud.CloseMission();
                foreach(var control in new[]{"출전 편성","장비 관리","장비 상점","성장 · 승급","동료의 이야기 · 30"})
                {
                    Click(control);yield return Settle();yield return Inspect(control,battle.Hud.transform,size,scale);
                    if(control=="장비 관리")
                    {
                        var candidate=battle.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.name.StartsWith("장착 후보: ")&&b.IsInteractable());
                        if(candidate==null)throw new InvalidOperationException("Equipment review fixture needs an owned candidate");
                        candidate.onClick.Invoke();yield return Settle();yield return Inspect("equipment-draft",battle.Hud.transform,size,scale);
                        Click("전체 능력치 보기");yield return Settle();yield return Inspect("equipment-stats",battle.Hud.transform,size,scale);battle.Hud.CloseUnitDetails();
                        Click("돌아가기 (미적용 취소)");yield return Settle();
                    }
                    if(control=="장비 상점")
                    {
                        var item=battle.Catalog.Equipment.First(e=>e.BuyPrice>0&&CampaignInventory.PurchaseUnlockRequirement(battle.Campaign,e)==null);
                        Click(item.DisplayName+" · "+item.BuyPrice+"G");yield return Settle();yield return Inspect("purchase-comparison",battle.Hud.transform,size,scale);
                    }
                    if(control=="성장 · 승급")
                    {
                        Click("성장 방향 선택");yield return Settle();yield return Inspect("growth-path",battle.Hud.transform,size,scale);battle.Hud.CloseUnitDetails();
                        Click("기술 숙련 안내");yield return Settle();yield return Inspect("mastery",battle.Hud.transform,size,scale);battle.Hud.CloseUnitDetails();
                    }
                    battle.Hud.ShowIntermissionHub();yield return Settle();
                }
                battle.ReplayStory(false);yield return Settle();yield return Inspect("dialogue",battle.Hud.transform,size,scale);
                battle.FinishStory();yield return Settle();
                battle.BeginBattle();yield return Settle();yield return Inspect("battle",battle.Hud.transform,size,scale);
                battle.Hud.ShowUnitDetails(battle.Session.Units.First(u=>u.Team==Team.Player));yield return Settle();yield return Inspect("status",battle.Hud.transform,size,scale);
                Click("상태 기술");yield return Settle();yield return Inspect("status-skills",battle.Hud.transform,size,scale);
                Click("상태 장비");yield return Settle();yield return Inspect("status-equipment",battle.Hud.transform,size,scale);
                battle.Hud.CloseUnitDetails();battle.Restart();yield return Settle();
                foreach(int page in new[]{0,1,2,3,6,7}){if(page==2)battle.SavePreferences(battle.Preferences,false);battle.Hud.ShowSystemMenu(page);yield return Settle();yield return Inspect("system-"+page,battle.Hud.transform,size,scale);battle.Hud.CloseSystemMenu();}
                for(int episode=1;episode<20;episode++)if(!battle.Campaign.StoryProgress.Contains(CrossoverCampaign.Flag(episode)))battle.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(episode));
                battle.Deployment.Clear();battle.Deployment.AddRange(new[]{0,1,2,3,4,5});battle.Hud.ShowIntermissionHub();yield return Settle();yield return Inspect("hub-six",battle.Hud.transform,size,scale);
                Click("출전 편성");yield return Settle();yield return Inspect("formation-six",battle.Hud.transform,size,scale);
                var roster=battle.Hud.GetComponentsInChildren<UnityEngine.UI.ScrollRect>().Single(s=>s.name=="FormationModels");
                roster.verticalNormalizedPosition=0;yield return Settle();yield return Inspect("formation-scrolled",battle.Hud.transform,size,scale);
                roster.verticalNormalizedPosition=1;yield return Settle();battle.Hud.ShowIntermissionHub();
                Click("장비 관리");yield return Settle();yield return Inspect("equipment-six",battle.Hud.transform,size,scale);Click("다음 캐릭터");yield return Settle();yield return Inspect("equipment-next",battle.Hud.transform,size,scale);battle.Hud.ShowIntermissionHub();
                Click("성장 · 승급");yield return Settle();yield return Inspect("growth-next",battle.Hud.transform,size,scale);Click("이전 캐릭터");yield return Settle();yield return Inspect("growth-six",battle.Hud.transform,size,scale);battle.Hud.ShowIntermissionHub();
                // A real four-stat accessory swap exercises denser purchase comparisons.
                var shopper=battle.Campaign.Get("cless");var previousGear=shopper.Equipment.ToArray();
                shopper.Equipment[2]="swift-boots";
                var medal=battle.Catalog.Equipment.First(e=>e.Id=="guardian-medal");
                if(!string.IsNullOrEmpty(medal.RequiredStoryFlag)&&!battle.Campaign.StoryProgress.Contains(medal.RequiredStoryFlag))battle.Campaign.StoryProgress.Add(medal.RequiredStoryFlag);
                Click("장비 상점");yield return Settle();Click("장신구");yield return Settle();
                Click(medal.DisplayName+" · "+medal.BuyPrice+"G");yield return Settle();
                yield return Inspect("purchase-multiple-stats",battle.Hud.transform,size,scale);
                shopper.Equipment=previousGear;Click("전체");yield return Settle();battle.Hud.ShowIntermissionHub();yield return Settle();
                battle.BeginBattle();yield return Settle();yield return Inspect("battle-six",battle.Hud.transform,size,scale);
                var turn=(TeamTurnScheduler)battle.Session.Scheduler;turn.Complete(battle.Session.Active);battle.Session.Select(turn.Preview(battle.Session.Units).First());battle.SetState(new CommandState(battle));yield return Settle();yield return Inspect("battle-done",battle.Hud.transform,size,scale);
                var foe=battle.Session.Units.First(u=>u.Team==Team.Enemy&&u.Alive);
                var adjacent=battle.Session.Grid.Neighbors(battle.Session.Active.Position).First(t=>t.Walkable&&t.Occupant==null);
                battle.Session.Grid.Place(foe,adjacent.Coordinate);battle.RefreshViews();battle.AttackCommand();battle.PreviewTile(foe.Position);
                yield return Settle();yield return Inspect("battle-forecast",battle.Hud.transform,size,scale);battle.State.Cancel();
                for(int rotation=0;rotation<4;rotation++){battle.WaitCommand();yield return Settle();yield return Inspect("facing-"+rotation,battle.Hud.transform,size,scale);battle.State.Cancel();battle.Board.RotateCamera(90);}
                battle.Session.Active.Level=50;battle.Session.Active.CurrentMP=battle.Session.Active.Stats.MP;
                battle.SkillCommand();yield return Settle();yield return Inspect("skills",battle.Hud.transform,size,scale);
                var feedback=battle.Hud.CombatFeedback;var active=battle.Session.Active;
                feedback.Enqueue(new ExperienceGain{Unit=active,Amount=35,BeforeLevel=2,BeforeEXP=80,AfterLevel=3,AfterEXP=15});yield return Settle();yield return Inspect("experience-center",battle.Hud.transform,size,scale);
                feedback.Clear();
                foreach(var enemy in battle.Session.Units.Where(u=>u.Team==Team.Enemy).ToArray())for(int n=0;n<3&&enemy.Alive;n++){enemy.Damage(99999,battle.Session.Grid);enemy.BeginTurn();enemy.Acted=true;enemy.EndTurn();}
                battle.Session.Crossover.State.Nodes=7;battle.Session.Crossover.Update();
                if(battle.Session.Result!=BattleResult.Victory)throw new InvalidOperationException("Six-member victory fixture did not satisfy its objective");
                battle.SetState(new BattleEndState(battle));yield return Settle();yield return Inspect("results-six",battle.Hud.transform,size,scale);
                Click("결과 계속");yield return Settle();yield return Inspect("after-story",battle.Hud.transform,size,scale);
                Click("대화 기록");yield return Settle();yield return Inspect("story-history",battle.Hud.transform,size,scale);battle.Hud.CloseStoryDialog();
                battle.Campaign.HasPendingAfterStory=false;battle.Campaign.PendingAfterStoryStage=-1;battle.Restart();yield return Settle();
                battle.Hud.ShowHelp();yield return Settle();yield return Inspect("help",battle.Hud.transform,size,scale);battle.Hud.CloseHelp();
                battle.Hud.ShowSystemMenu();yield return Settle();yield return Inspect("settings",battle.Hud.transform,size,scale);battle.Hud.CloseSystemMenu();
            }
            // This review deliberately completes battles in its isolated fixture saves.
#if UNITY_EDITOR
            // Remove only the temporary Game View sizes created by this review.
            var assembly=typeof(UnityEditor.Editor).Assembly;var sizes=assembly.GetType("UnityEditor.GameViewSizes");
            var instance=sizes.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
            var group=sizes.GetProperty("currentGroup").GetValue(instance);
            foreach(int index in editorSizes.Values.OrderByDescending(i=>i))group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{index});
#endif
            File.WriteAllText(Path.Combine(directory,"report.json"),JsonUtility.ToJson(new Report{rows=rows.ToArray(),errors=errors.ToArray()},true));
            int exitCode=errors.Count==0&&rows.All(r=>r.issues.Length==0)?0:1;
#if UNITY_EDITOR
            UnityEditor.SessionState.SetInt("TalesTactics.UiSafeLayoutReviewExit",exitCode);UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit(exitCode);
#endif
        }
        IEnumerator Settle(){yield return null;yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();}
        void Click(string name)
        {
            var button=battle.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name==name);
            if(!button.IsInteractable())throw new InvalidOperationException("Not interactable: "+name);button.onClick.Invoke();
        }
        IEnumerator Inspect(string name,string path,Vector2Int size,float scale)=>Inspect(name,battle.Hud.transform.Find(path),size,scale);
        static UnityEngine.UI.ScrollRect ContentScroll(Transform child)
        {
            var scroll=child.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            return scroll!=null&&scroll.content!=null&&child.IsChildOf(scroll.content)?scroll:null;
        }
        IEnumerator Inspect(string name,Transform panel,Vector2Int size,float scale)
        {
            var issues=new List<string>();if(Screen.width!=size.x||Screen.height!=size.y)issues.Add("Resolution mismatch");
            var dock=panel.GetComponentsInChildren<RectTransform>().FirstOrDefault(r=>r.name=="BattleCommandDock");
            if(dock!=null)
            {
                var allowed=dock.rect;allowed.xMin+=8;allowed.xMax-=8;allowed.yMin+=8;allowed.yMax-=8;
                foreach(var part in panel.GetComponentsInChildren<RectTransform>().Where(r=>r.parent==dock.parent&&(r.name=="Unit"||r.name=="Commands"||r.name=="Threat controls")))
                {
                    if(part.name=="Commands"&&battle.State is FacingSelectionState)continue;
                    foreach(var button in part.GetComponentsInChildren<UnityEngine.UI.Button>())
                    {
                        if(button.name.StartsWith("Facing"))continue;
                        var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                        if(corners.Any(p=>!allowed.Contains(dock.InverseTransformPoint(p))))issues.Add("Button outside battle dock: "+button.name);
                    }
                    foreach(var text in part.GetComponentsInChildren<TMP_Text>())
                    {
                        text.ForceMeshUpdate();
                        foreach(var glyph in text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(g=>g.isVisible))
                            if(!allowed.Contains(dock.InverseTransformPoint(text.transform.TransformPoint(glyph.bottomLeft)))||!allowed.Contains(dock.InverseTransformPoint(text.transform.TransformPoint(glyph.topRight))))
                            {issues.Add("Text outside battle dock: "+text.text);break;}
                    }
                }
            }
            foreach(var scroll in panel.GetComponentsInChildren<UnityEngine.UI.ScrollRect>())
            {
                if(scroll.viewport==null||scroll.content==null){issues.Add("Unwired scroll viewport: "+scroll.name);continue;}
                if(!scroll.viewport.GetComponents<UnityEngine.UI.Mask>().Any(m=>m.isActiveAndEnabled)&&!scroll.viewport.GetComponents<UnityEngine.UI.RectMask2D>().Any(m=>m.isActiveAndEnabled))
                    issues.Add("Unclipped scroll viewport: "+scroll.name);
            }
            foreach(var icon in panel.GetComponentsInChildren<TacticalIcon>())
            {
                if(icon.Region==null||icon.mainTexture==null)issues.Add("Missing illustrated icon: "+icon.Kind);
                if(icon.raycastTarget)issues.Add("Decorative icon intercepts input: "+icon.Kind);
                var bounds=(RectTransform)icon.transform.parent;var corners=new Vector3[4];icon.rectTransform.GetWorldCorners(corners);var allowed=bounds.rect;allowed.xMin-=1;allowed.yMin-=1;allowed.xMax+=1;allowed.yMax+=1;
                if(corners.Any(p=>!allowed.Contains(bounds.InverseTransformPoint(p))))issues.Add("Illustrated icon outside parent: "+icon.Kind);
            }
            foreach(var art in panel.GetComponentsInChildren<UnityEngine.UI.RawImage>().Where(i=>i.name=="ClassIllustration"))
            {
                if(art.texture==null||art.texture.name!="ClassIcons")issues.Add("Missing class artwork: "+art.transform.parent.name);
                if(art.raycastTarget)issues.Add("Class artwork intercepts input");
                var rect=art.rectTransform.rect;
                if(rect.width<16||rect.height<16)issues.Add("Class artwork too small: "+art.transform.parent.name);
                var corners=new Vector3[4];art.rectTransform.GetWorldCorners(corners);var bounds=(RectTransform)art.transform.parent;
                var allowed=bounds.rect;allowed.xMin-=.1f;allowed.yMin-=.1f;allowed.xMax+=.1f;allowed.yMax+=.1f;
                if(corners.Any(p=>!allowed.Contains(bounds.InverseTransformPoint(p))))issues.Add("Class artwork outside its bounds");
            }
            foreach(var preview in panel.GetComponentsInChildren<UnityEngine.UI.RawImage>().Where(i=>i.name=="MapImage"))
            {
                var corners=new Vector3[4];preview.rectTransform.GetWorldCorners(corners);var bounds=(RectTransform)preview.transform.parent;
                var allowed=bounds.rect;allowed.xMin-=.1f;allowed.yMin-=.1f;allowed.xMax+=.1f;allowed.yMax+=.1f;
                if(corners.Any(p=>!allowed.Contains(bounds.InverseTransformPoint(p))))issues.Add("Map preview outside its bounds");
            }
            foreach(var text in panel.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();if(text.isTextOverflowing)issues.Add("Overflow: "+text.text+" ["+text.rectTransform.rect.size+" font "+text.fontSize+"]");
                var frame=text.GetComponentInParent<FramedContent>();
                var scroll=ContentScroll(text.transform);
                if(frame!=null&&frame.GetComponent<UnityEngine.UI.Image>().enabled&&(scroll==null||frame.transform.IsChildOf(scroll.content)))
                {
                    var allowed=frame.ContentRect;allowed.xMin-=2;allowed.yMin-=2;allowed.xMax+=2;allowed.yMax+=2;
                    for(int c=0;c<text.textInfo.characterCount;c++)
                    {
                        var glyph=text.textInfo.characterInfo[c];if(!glyph.isVisible)continue;
                        if(!allowed.Contains(frame.transform.InverseTransformPoint(text.transform.TransformPoint(glyph.bottomLeft)))||!allowed.Contains(frame.transform.InverseTransformPoint(text.transform.TransformPoint(glyph.topRight))))
                        {issues.Add("Glyph outside frame "+frame.name+": "+text.text);break;}
                    }
                }
                var glyphCorners=new Vector3[4];text.rectTransform.GetWorldCorners(glyphCorners);
                if(scroll==null&&glyphCorners.Any(p=>p.x<0||p.y<0||p.x>Screen.width||p.y>Screen.height))issues.Add("Text offscreen: "+text.text);
                if(!string.IsNullOrWhiteSpace(text.text))foreach(var sibling in text.transform.parent.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.transform.parent==text.transform.parent&&b.IsInteractable()))
                {
                    var bc=new Vector3[4];((RectTransform)sibling.transform).GetWorldCorners(bc);
                    var br=new Rect(bc[0].x,bc[0].y,bc[2].x-bc[0].x,bc[2].y-bc[0].y);
                    for(int c=0;c<text.textInfo.characterCount;c++)
                    {
                        var glyph=text.textInfo.characterInfo[c];if(!glyph.isVisible)continue;
                        var lo=text.transform.TransformPoint(glyph.bottomLeft);var hi=text.transform.TransformPoint(glyph.topRight);
                        if(new Rect(lo.x,lo.y,hi.x-lo.x,hi.y-lo.y).Overlaps(br)){issues.Add("Text/button overlap: "+text.text+" / "+sibling.name);break;}
                    }
                }
                if(!text.font.HasCharacters(text.text,out uint[] missing,true,true))issues.Add("Missing glyphs: "+text.text);
                var owner=text.GetComponentInParent<UnityEngine.UI.Button>();
                if(owner!=null)
                {
                    var r=(RectTransform)owner.transform;var allowed=r.rect;allowed.xMin-=1;allowed.yMin-=1;allowed.xMax+=1;allowed.yMax+=1;
                    for(int c=0;c<text.textInfo.characterCount;c++)
                    {var glyph=text.textInfo.characterInfo[c];if(glyph.isVisible&&(!allowed.Contains(r.InverseTransformPoint(text.transform.TransformPoint(glyph.bottomLeft)))||!allowed.Contains(r.InverseTransformPoint(text.transform.TransformPoint(glyph.topRight))))){issues.Add("Glyph outside button "+owner.name+": "+text.text);break;}}
                }
            }
            foreach(var frame in panel.GetComponentsInChildren<FramedContent>().Where(f=>f.GetComponent<UnityEngine.UI.Image>().enabled))
            foreach(Transform child in frame.transform)
            {
                if(!child.gameObject.activeSelf||FramedContent.IsDecoration(child)||!(child is RectTransform r))continue;
                var corners=new Vector3[4];r.GetWorldCorners(corners);var allowed=frame.ContentRect;allowed.xMin-=1;allowed.yMin-=1;allowed.xMax+=1;allowed.yMax+=1;
                if(corners.Any(p=>!allowed.Contains(frame.transform.InverseTransformPoint(p))))issues.Add("Component outside frame "+frame.name+": "+child.name);
            }
            // Model buttons and captions must stay in their own grid cell, not just in the outer frame.
            foreach(var cell in panel.GetComponentsInChildren<RectTransform>().Where(r=>r.name.StartsWith("ModelCell-")))
            foreach(var child in cell.GetComponentsInChildren<RectTransform>())
            {
                if(child==cell||child.GetComponent<OrnateButtonGraphic>()!=null)continue;
                var corners=new Vector3[4];child.GetWorldCorners(corners);var allowed=cell.rect;allowed.xMin-=1;allowed.yMin-=1;allowed.xMax+=1;allowed.yMax+=1;
                if(corners.Any(p=>!allowed.Contains(cell.InverseTransformPoint(p))))issues.Add("Component outside roster cell "+cell.name+": "+child.name);
            }
            foreach(var button in battle.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.IsInteractable()))
            {
                if(!button.transform.IsChildOf(panel))issues.Add("Background button active: "+button.name);
                var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                var scroll=ContentScroll(button.transform);
                if(scroll==null&&corners.Any(p=>p.x < -1||p.y < -1||p.x>Screen.width+1||p.y>Screen.height+1))issues.Add("Offscreen: "+button.name);
                var hits=new List<UnityEngine.EventSystems.RaycastResult>();
                var point=((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center);
                if(scroll!=null&&!RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport,point))continue;
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=point},hits);
                if(hits.Count==0||hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()!=button)issues.Add("Button obstructed: "+button.name);
            }
            rows.Add(new Row{screen=name,width=Screen.width,height=Screen.height,textScale=scale,issues=issues.ToArray()});
            if((size.x==1920&&scale==1)||(size.x==1024&&scale==1.3f))
            {yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(directory,name+"-"+size.x+".png"),texture.EncodeToPNG());Destroy(texture);}
        }
        void Log(string message,string trace,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors.Add(message+"\n"+trace);File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);}}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
#endif
