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
    public sealed class UiRefinementReview:MonoBehaviour
    {
        [Serializable] sealed class Row { public string screen;public int width,height;public float textScale;public string[] issues; }
        [Serializable] sealed class Report { public Row[] rows;public string[] errors; }
        readonly List<Row> rows=new List<Row>();readonly List<string> errors=new List<string>();
        BattleDirector battle;string directory;
#if UNITY_EDITOR
        public static void BeginEditor(string output)
        {
            var review=new GameObject("Title screen review").AddComponent<UiRefinementReview>();review.directory=output;
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
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--ui-refinement-review");if(i<0)return;
            if(i+1>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)){Application.Quit(2);return;}
            var review=new GameObject("Title screen review").AddComponent<UiRefinementReview>();
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
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(1920,1080),new Vector2Int(2560,1080),new Vector2Int(1080,1440)})
            foreach(float scale in new[]{1f,1.15f,1.3f})
            {
#if UNITY_EDITOR
                SetEditorResolution(size);
#else
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
#endif
                yield return new WaitForSecondsRealtime(.5f);battle.Preferences.TextScale=scale;battle.Hud.ShowTitle();yield return Settle();
                yield return Inspect("title","TitleScreen",size,scale);
                Click("타이틀 불러오기");yield return Settle();yield return Inspect("load","TitleScreen/TitleSlotsOverlay/TitleSlotsWindow",size,scale);
                battle.Hud.CancelTitle();Click("타이틀 새 게임");yield return Settle();yield return Inspect("new","TitleScreen/TitleSlotsOverlay/TitleSlotsWindow",size,scale);
                Click("타이틀 슬롯 3");yield return Settle();yield return Inspect("new-confirm","TitleScreen/TitleConfirm",size,scale);
                battle.Hud.CancelTitle();Click("타이틀 슬롯 1");yield return Settle();yield return Inspect("overwrite","TitleScreen/TitleConfirm",size,scale);
                battle.Hud.CancelTitle();battle.Hud.CancelTitle();
                battle.LoadTitleGame(1,false);yield return Settle();yield return Inspect("hub",battle.Hud.transform,size,scale);
                Click("거점 적 정보");yield return Settle();yield return Inspect("hub-enemies",battle.Hud.transform,size,scale);
                Click("거점 목표");yield return Settle();
                Click("이야기 다시 보기");yield return Settle();yield return Inspect("story-menu",battle.Hud.transform,size,scale);battle.Hud.CloseUnitDetails();
                foreach(var control in new[]{"출전 편성","장비 관리","장비 상점","성장 · 승급","동료의 이야기 · 30"})
                {Click(control);yield return Settle();yield return Inspect(control,battle.Hud.transform,size,scale);battle.Hud.ShowIntermissionHub();yield return Settle();}
                battle.ReplayStory(false);yield return Settle();yield return Inspect("dialogue",battle.Hud.transform,size,scale);
                Click("이야기 건너뛰기");yield return Settle();yield return Inspect("story-confirm",battle.Hud.transform,size,scale);battle.Hud.CloseStoryDialog();battle.FinishStory();
                battle.BeginBattle();yield return Settle();yield return Inspect("battle",battle.Hud.transform,size,scale);
                battle.Hud.ShowUnitDetails(battle.Session.Units.First(u=>u.Team==Team.Player));yield return Settle();yield return Inspect("status",battle.Hud.transform,size,scale);
                battle.Hud.CloseUnitDetails();battle.Restart();yield return Settle();
                battle.Hud.ShowHelp();yield return Settle();yield return Inspect("help",battle.Hud.transform,size,scale);battle.Hud.CloseHelp();
                battle.Hud.ShowSystemMenu();yield return Settle();yield return Inspect("settings",battle.Hud.transform,size,scale);battle.Hud.CloseSystemMenu();
            }
            foreach(var file in original)if(File.ReadAllText(file.Key)!=file.Value)errors.Add("Title browsing changed "+Path.GetFileName(file.Key));
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
            UnityEditor.SessionState.SetInt("TalesTactics.UiRefinementReviewExit",exitCode);UnityEditor.EditorApplication.isPlaying=false;
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
        IEnumerator Inspect(string name,Transform panel,Vector2Int size,float scale)
        {
            var issues=new List<string>();if(Screen.width!=size.x||Screen.height!=size.y)issues.Add("Resolution mismatch");
            foreach(var preview in panel.GetComponentsInChildren<UnityEngine.UI.RawImage>().Where(i=>i.name=="MapImage"))
            {
                var corners=new Vector3[4];preview.rectTransform.GetWorldCorners(corners);var bounds=(RectTransform)preview.transform.parent;
                var allowed=bounds.rect;allowed.xMin-=.1f;allowed.yMin-=.1f;allowed.xMax+=.1f;allowed.yMax+=.1f;
                if(corners.Any(p=>!allowed.Contains(bounds.InverseTransformPoint(p))))issues.Add("Map preview outside its bounds");
            }
            foreach(var text in panel.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();if(text.isTextOverflowing)issues.Add("Overflow: "+text.text);
                var glyphCorners=new Vector3[4];text.rectTransform.GetWorldCorners(glyphCorners);
                if(glyphCorners.Any(p=>p.x<0||p.y<0||p.x>Screen.width||p.y>Screen.height))issues.Add("Text offscreen: "+text.text);
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
            }
            foreach(var button in battle.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.IsInteractable()))
            {
                if(!button.transform.IsChildOf(panel))issues.Add("Background button active: "+button.name);
                var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                if(corners.Any(p=>p.x < -1||p.y < -1||p.x>Screen.width+1||p.y>Screen.height+1))issues.Add("Offscreen: "+button.name);
                var hits=new List<UnityEngine.EventSystems.RaycastResult>();
                var point=((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center);
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
