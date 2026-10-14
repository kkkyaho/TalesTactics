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
    public sealed class HelpGuideReview:MonoBehaviour
    {
        [Serializable] sealed class Row { public string screen;public int width,height;public float textScale;public string[] issues; }
        [Serializable] sealed class Report { public Row[] rows;public string[] errors; }
        readonly List<Row> rows=new List<Row>();readonly List<string> errors=new List<string>();
        BattleDirector battle;string directory;
#if UNITY_EDITOR
        public static void BeginEditor(string output)
        {
            var review=new GameObject("Help guide review").AddComponent<HelpGuideReview>();review.directory=output;
        }
        readonly Dictionary<Vector2Int,int> editorSizes=new Dictionary<Vector2Int,int>();
        void SetEditorResolution(Vector2Int size)
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;var sizes=assembly.GetType("UnityEditor.GameViewSizes");
            var instance=sizes.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
            var group=sizes.GetProperty("currentGroup").GetValue(instance);
            if(!editorSizes.TryGetValue(size,out int index))
            {
                var entry=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),size.x,size.y,"Help guide "+size});
                group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{entry});index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;editorSizes.Add(size,index);
            }
            var type=assembly.GetType("UnityEditor.GameView");var window=UnityEditor.EditorWindow.GetWindow(type);
            type.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(window,index);window.Focus();window.Repaint();
        }
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--help-guide-review");if(i<0)return;
            if(i+1>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)){Application.Quit(2);return;}
            var review=new GameObject("Help guide review").AddComponent<HelpGuideReview>();
            review.directory=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Reviews",args[i+1]);
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(directory);Application.runInBackground=true;Application.logMessageReceived+=Log;yield return null;
            battle=UnityEngine.Object.FindAnyObjectByType<BattleDirector>();battle.ConfigureStorage(directory);battle.Audio.PersonalMusicEnabled=false;
            battle.PersistCampaign=_=>throw new InvalidOperationException("Help must not save campaign progress");
            string before=JsonUtility.ToJson(battle.Campaign);
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(1920,1080),new Vector2Int(2560,1080),new Vector2Int(1080,1440)})
            foreach(float scale in new[]{1f,1.3f})
            {
#if UNITY_EDITOR
                SetEditorResolution(size);
#else
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
#endif
                yield return new WaitForSecondsRealtime(.5f);battle.Preferences.TextScale=scale;
                battle.Hud.ShowHelp();yield return Settle();
                var topics=battle.Hud.transform.Find("HelpOverlay/HelpWindow").GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.name.StartsWith("도움말: ")).Select(b=>b.name).ToArray();
                for(int page=0;page<topics.Length;page++)
                {
                    Click(topics[page]);yield return Settle();
                    yield return Inspect("help-"+page.ToString("D2"),"HelpOverlay/HelpWindow",size,scale);
                }
                battle.Hud.CloseHelp();battle.Hud.ShowSystemMenu(6);yield return Settle();yield return Inspect("save-guide","SystemOverlay/SystemWindow",size,scale);
                battle.Hud.ShowSystemMenu(7);yield return Settle();yield return Inspect("rules-guide","SystemOverlay/SystemWindow",size,scale);battle.Hud.CloseSystemMenu();
                Click("성장 · 승급");yield return Settle();Click("기술 숙련 안내");yield return Settle();
                var modal=battle.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="숙련 안내 닫기").transform.parent;
                yield return Inspect("mastery-guide",modal,size,scale);battle.Hud.CloseUnitDetails();battle.Hud.ShowIntermissionHub();
            }
            if(JsonUtility.ToJson(battle.Campaign)!=before)errors.Add("Help browsing changed campaign progress");
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
            UnityEditor.SessionState.SetInt("TalesTactics.HelpReviewExit",exitCode);UnityEditor.EditorApplication.isPlaying=false;
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
            foreach(var text in panel.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();if(text.isTextOverflowing)issues.Add("Overflow: "+text.text);
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
            if(size.x==1280&&scale==1.3f)
            {yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(directory,name+".png"),texture.EncodeToPNG());Destroy(texture);}
        }
        void Log(string message,string trace,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors.Add(message+"\n"+trace);File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);}}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
#endif
