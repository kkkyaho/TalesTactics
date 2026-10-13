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
    public sealed class IntermissionUxReview:MonoBehaviour
    {
        [Serializable] sealed class Row { public string screen;public int width,height,buttons;public float textScale;public string[] issues; }
        [Serializable] sealed class Report { public Row[] rows;public string[] errors;public int storyLines; }
        readonly List<Row> rows=new List<Row>();readonly List<string> errors=new List<string>();
        BattleDirector battle;string directory;int storyCount;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--intermission-ux-review");if(i<0)return;
            if(i+1>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)){Application.Quit(2);return;}
            var review=new GameObject("Intermission UX review").AddComponent<IntermissionUxReview>();
            review.directory=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Reviews",args[i+1]);
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(directory);Application.runInBackground=true;Application.logMessageReceived+=Log;yield return null;
            battle=UnityEngine.Object.FindAnyObjectByType<BattleDirector>();battle.ConfigureStorage(directory);battle.Audio.PersonalMusicEnabled=false;battle.PersistCampaign=_=>true;
            battle.TrainingMode=false;battle.Campaign.Gold=2480;for(int i=1;i<=20;i++)battle.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(i));
            foreach(var c in battle.Catalog.Characters)battle.Campaign.Get(c.Id).Level=12;
            foreach(var item in battle.Catalog.Equipment)battle.Campaign.Inventory.Add(new OwnedEquipment{Id=item.Id,Count=4});
            battle.SelectedStage=6;battle.Deployment.Clear();battle.Deployment.AddRange(new[]{"cless","mint","alphen","shionne"}.Select(id=>Array.FindIndex(battle.Catalog.Characters,c=>c.Id==id)));
            string before=JsonUtility.ToJson(battle.Campaign);
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(1920,1080),new Vector2Int(2560,1080),new Vector2Int(1080,1440)})
            foreach(float scale in new[]{1f,1.3f})
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);battle.Preferences.TextScale=scale;
                battle.Hud.ShowIntermissionHub();yield return Settle();yield return Inspect("hub",size,scale,true);
                Click("거점 캐릭터: cless");yield return Settle();Click("장착 후보: "+battle.Catalog.Equipment.Single(e=>e.Id=="iron-sword").DisplayName);yield return Settle();yield return Inspect("equipment",size,scale,true);
                Click("전체 능력치 보기");yield return Settle();yield return Inspect("comparison",size,scale,false);battle.Hud.CloseUnitDetails();
                battle.Hud.ShowIntermissionHub();battle.ReplayStory(false);yield return Settle();
                yield return Inspect("narration",size,scale,false);
                for(int step=0;step<8&&battle.StoryActive&&battle.Hud.StoryPortrait(battle.ReadStoryHistory().Last().Speaker)==null;step++){battle.AdvanceStory();yield return Settle();}
                yield return Inspect("dialogue",size,scale,true);Click("대화 기록");yield return Settle();yield return Inspect("history",size,scale,false);battle.Hud.CloseStoryDialog();
                Click("이야기 건너뛰기");yield return Settle();yield return Inspect("skip",size,scale,false);Click("이야기 계속 읽기");battle.FinishStory();yield return Settle();
            }
            Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);battle.Preferences.TextScale=1.3f;battle.ReplayStory(false);yield return Settle();
            foreach(int stage in Enumerable.Range(CrossoverCampaign.FirstStage,CampaignStages.Count-CrossoverCampaign.FirstStage))
            foreach(bool after in new[]{false,true})foreach(var line in CampaignContent.Story(stage,after))
            {
                battle.Hud.ShowStoryLine(line,2,3);yield return Settle();var text=battle.Hud.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="DialogueBody");text.ForceMeshUpdate();
                if(text.isTextOverflowing)errors.Add("Dialogue overflow: "+stage+" / "+line.Speaker+" / "+line.Text);storyCount++;
            }
            battle.FinishStory();if(JsonUtility.ToJson(battle.Campaign)!=before)errors.Add("Browsing changed campaign save");
            File.WriteAllText(Path.Combine(directory,"report.json"),JsonUtility.ToJson(new Report{rows=rows.ToArray(),errors=errors.ToArray(),storyLines=storyCount},true));
            Application.Quit(errors.Count==0&&rows.All(r=>r.issues.Length==0)?0:1);
        }
        IEnumerator Settle(){yield return null;yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();}
        void Click(string name)
        {
            var button=battle.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name==name);
            if(!button.IsInteractable())throw new InvalidOperationException("Not interactable: "+name);button.onClick.Invoke();
        }
        IEnumerator Inspect(string name,Vector2Int requested,float scale,bool capture)
        {
            var problems=new List<string>();int buttons=0;
            if(Screen.width!=requested.x||Screen.height!=requested.y)problems.Add("Resolution differs from request "+requested);
            foreach(var button in battle.Hud.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                if(!button.IsInteractable())continue;buttons++;var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                if(corners.Any(c=>c.x < -1||c.y < -1||c.x>Screen.width+1||c.y>Screen.height+1))problems.Add("Offscreen: "+button.name);
            }
            foreach(var text in battle.Hud.GetComponentsInChildren<TMP_Text>())
            {
                if(string.IsNullOrWhiteSpace(text.text))continue;
                if(battle.StoryActive&&!text.transform.IsChildOf(battle.Hud.transform.Find("CampaignStory")))continue;
                text.ForceMeshUpdate();if(text.isTextOverflowing)problems.Add("Overflow: "+text.text);
            }
            rows.Add(new Row{screen=name,width=Screen.width,height=Screen.height,textScale=scale,buttons=buttons,issues=problems.ToArray()});
            if(capture&&scale==1&&requested.x==1280)
            {yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(directory,name+".png"),texture.EncodeToPNG());Destroy(texture);}
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors.Add(message+"\n"+trace);File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);}}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
#endif
