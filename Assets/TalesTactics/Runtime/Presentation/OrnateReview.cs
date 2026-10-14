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
    public sealed class OrnateReview:MonoBehaviour
    {
        [Serializable] sealed class Row { public string screen;public int width,height;public float textScale;public string[] issues; }
        [Serializable] sealed class Timing { public int terrain,width,height,frames;public double meanMs,p95Ms;public int renderers,triangles; }
        [Serializable] sealed class Report { public Row[] rows;public string[] errors;public Timing[] timings;public string device;public int storyLines; }
        readonly List<Row> rows=new List<Row>();readonly List<string> errors=new List<string>();
        readonly List<Timing> timings=new List<Timing>();
        BattleDirector battle;string directory;int storyLines;
#if UNITY_EDITOR
        public static void BeginEditor(string output)
        {
            var review=new GameObject("Ornate presentation review").AddComponent<OrnateReview>();review.directory=output;
        }
        readonly Dictionary<Vector2Int,int> editorSizes=new Dictionary<Vector2Int,int>();
        void SetEditorResolution(Vector2Int size)
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;var sizes=assembly.GetType("UnityEditor.GameViewSizes");
            var instance=sizes.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
            var group=sizes.GetProperty("currentGroup").GetValue(instance);
            if(!editorSizes.TryGetValue(size,out int index))
            {
                var entry=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),size.x,size.y,"Ornate presentation "+size});
                group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{entry});index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;editorSizes.Add(size,index);
            }
            var type=assembly.GetType("UnityEditor.GameView");var window=UnityEditor.EditorWindow.GetWindow(type);
            type.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(window,index);window.Focus();window.Repaint();
        }
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--ornate-review");if(i<0)return;
            if(i+1>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)){Application.Quit(2);return;}
            var review=new GameObject("Ornate presentation review").AddComponent<OrnateReview>();
            review.directory=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Reviews",args[i+1]);
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(directory);Application.runInBackground=true;Application.logMessageReceived+=Log;yield return null;
            battle=UnityEngine.Object.FindAnyObjectByType<BattleDirector>();battle.ConfigureStorage(Path.Combine(directory,Guid.NewGuid().ToString("N")));battle.Audio.PersonalMusicEnabled=false;
            battle.Hud.ShowTitle();battle.StartTitleGame(0);
            foreach(var data in battle.Catalog.Characters)battle.Campaign.Get(data.Id).Level=25;
            var sizes=Environment.GetCommandLineArgs().Contains("--ornate-performance")?Array.Empty<Vector2Int>():Environment.GetCommandLineArgs().Contains("--ornate-full")?new[]{new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(1920,1080),new Vector2Int(2560,1080),new Vector2Int(1080,1440)}:new[]{new Vector2Int(1920,1080)};
            foreach(var size in sizes)foreach(float scale in new[]{1f,1.3f})
            {
#if UNITY_EDITOR
                SetEditorResolution(size);
#else
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
#endif
                yield return new WaitForSecondsRealtime(.4f);battle.Preferences.TextScale=scale;
                battle.Hud.ShowIntermissionHub();yield return Settle();yield return InspectVisible("hub",size,scale);
                battle.ReplayStory(false);yield return Settle();
                for(int i=0;i<8&&battle.StoryActive&&battle.Hud.StoryPortrait(battle.ReadStoryHistory().Last().Speaker)==null;i++){battle.AdvanceStory();yield return Settle();}
                yield return InspectVisible("dialogue",size,scale);Click("이야기 건너뛰기");yield return Settle();yield return InspectVisible("popup",size,scale);battle.Hud.CloseStoryDialog();battle.FinishStory();yield return Settle();
                battle.BeginBattle();yield return Settle();yield return InspectVisible("battle",size,scale);
                battle.Hud.ShowUnitDetails(battle.Session.Units.First(u=>u.Team==Team.Player));yield return Settle();yield return InspectVisible("status",size,scale);
                Click("상태 기술");yield return Settle();yield return InspectVisible("skills",size,scale);
                Click("상태 장비");yield return Settle();yield return InspectVisible("equipment",size,scale);battle.Hud.CloseUnitDetails();
                for(int angle=0;angle<4;angle++){battle.Board.RotateCamera(90);yield return Settle();if(scale==1)yield return InspectVisible("map-"+angle,size,scale);}
                battle.Restart();yield return Settle();
            }
            // Exercise the six terrain families through current campaign episodes.
            for(int n=1;n<=20;n++)if(!battle.Campaign.StoryProgress.Contains(CrossoverCampaign.Flag(n)))battle.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(n));
            battle.Preferences.TextScale=1;var reviewSize=new Vector2Int(1920,1080);
#if UNITY_EDITOR
            SetEditorResolution(reviewSize);
#else
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
#endif
            yield return new WaitForSecondsRealtime(.4f);
            foreach(var stage in Enumerable.Range(6,20).GroupBy(CrossoverCampaign.TerrainTheme).Select(g=>g.First()))
            {
                battle.SelectedStage=stage;battle.BeginBattle();if(battle.StoryActive)battle.FinishStory();battle.StopAllCoroutines();yield return Settle();
                for(int angle=0;angle<4;angle++)
                {
                    battle.Board.RotateCamera(90);yield return Settle();
                    foreach(var tile in battle.Session.Grid.Tiles.Values.Where(t=>t.Walkable))
                    {
                        var screen=battle.Board.BattleCamera.WorldToScreenPoint(tile.WorldPosition(battle.Session.Rules.TileHeight));
                        if(!battle.Board.Pick(screen,out var picked)||picked!=tile.Coordinate)errors.Add("Terrain pick: "+stage+" / "+angle+" / "+tile.Coordinate);
                    }
                    yield return InspectVisible("terrain-"+CrossoverCampaign.TerrainTheme(stage)+"-"+angle,reviewSize,1);
                }
                yield return MeasureTerrain();battle.Restart();yield return Settle();
            }
            foreach(var storySize in new[]{new Vector2Int(1280,720),new Vector2Int(1024,768)})
            {
#if UNITY_EDITOR
                SetEditorResolution(storySize);
#else
                Screen.SetResolution(storySize.x,storySize.y,FullScreenMode.Windowed);
#endif
                yield return new WaitForSecondsRealtime(.4f);battle.Preferences.TextScale=1.3f;battle.ReplayStory(false);yield return Settle();
                foreach(int stage in Enumerable.Range(CrossoverCampaign.FirstStage,CampaignStages.Count-CrossoverCampaign.FirstStage))
                foreach(bool after in new[]{false,true})foreach(var line in CampaignContent.Story(stage,after))
                {
                    battle.Hud.ShowStoryLine(line,2,3);yield return Settle();var text=battle.Hud.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="DialogueBody");text.ForceMeshUpdate();
                    if(text.isTextOverflowing)errors.Add("Dialogue overflow: "+storySize+" / "+stage+" / "+line.Text);storyLines++;
                }
                battle.FinishStory();yield return Settle();
            }
#if UNITY_EDITOR
            var assembly=typeof(UnityEditor.Editor).Assembly;var type=assembly.GetType("UnityEditor.GameViewSizes");var instance=type.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);var group=type.GetProperty("currentGroup").GetValue(instance);
            foreach(int index in editorSizes.Values.OrderByDescending(i=>i))group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{index});
#endif
            File.WriteAllText(Path.Combine(directory,"report.json"),JsonUtility.ToJson(new Report{rows=rows.ToArray(),errors=errors.ToArray(),timings=timings.ToArray(),device=SystemInfo.graphicsDeviceName,storyLines=storyLines},true));
#if UNITY_EDITOR
            UnityEditor.SessionState.SetInt("TalesTactics.OrnateReviewExit",errors.Count==0&&rows.All(r=>r.issues.Length==0)?0:1);UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit(errors.Count==0&&rows.All(r=>r.issues.Length==0)?0:1);
#endif
        }
        IEnumerator InspectVisible(string name,Vector2Int size,float scale)
        {
            var issues=new List<string>();if(Screen.width!=size.x||Screen.height!=size.y)issues.Add("Resolution mismatch");
            foreach(var text in battle.Hud.GetComponentsInChildren<TMP_Text>())
            {
                if(!text.gameObject.activeInHierarchy||text.GetComponentsInParent<CanvasGroup>().Any(g=>g.alpha<.01f))continue;
                if(battle.StoryActive&&!text.transform.IsChildOf(battle.Hud.transform.Find("CampaignStory")))continue;
                text.ForceMeshUpdate();if(text.isTextOverflowing)issues.Add("Overflow: "+text.text);
                if(!text.font.HasCharacters(text.text,out uint[] missing,true,true))issues.Add("Missing glyphs: "+text.text);
            }
            foreach(var button in battle.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.IsInteractable()))
            {
                if(button.GetComponentsInParent<CanvasGroup>().Any(g=>g.alpha<.01f))continue;
                var hits=new List<UnityEngine.EventSystems.RaycastResult>();
                var point=((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center);
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=point},hits);
                if(hits.Count==0||hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()!=button)issues.Add("Button obstructed: "+button.name);
                var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                if(corners.Any(p=>p.x < -1||p.y < -1||p.x>Screen.width+1||p.y>Screen.height+1))issues.Add("Offscreen: "+button.name);
            }
            rows.Add(new Row{screen=name,width=Screen.width,height=Screen.height,textScale=scale,issues=issues.ToArray()});
            if(size.x==1920||size.x==1280){yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(directory,name+"-"+size.x+"-"+scale.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+".png"),texture.EncodeToPNG());Destroy(texture);}
        }
        IEnumerator Settle(){yield return null;yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();}
        IEnumerator MeasureTerrain()
        {
            for(int i=0;i<30;i++)yield return null;
            var samples=new List<double>();for(int i=0;i<180;i++){yield return null;samples.Add(Time.unscaledDeltaTime*1000);}
            samples.Sort();var scenery=battle.Board.GetComponentInChildren<PixelBattlefield>();
            timings.Add(new Timing{terrain=scenery.Chapter,width=Screen.width,height=Screen.height,frames=samples.Count,meanMs=samples.Average(),p95Ms=samples[(int)(samples.Count*.95)],renderers=scenery.GetComponentsInChildren<Renderer>().Length,triangles=scenery.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)});
        }
        void Click(string name)
        {
            var button=battle.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name==name);
            if(!button.IsInteractable())throw new InvalidOperationException("Not interactable: "+name);button.onClick.Invoke();
        }
        void Log(string message,string trace,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors.Add(message+"\n"+trace);File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);}}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
#endif
