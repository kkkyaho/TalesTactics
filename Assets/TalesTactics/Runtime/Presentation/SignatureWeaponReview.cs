#if UNITY_EDITOR || TALESTACTICS_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed class SignatureWeaponReview:MonoBehaviour
    {
        [Serializable] sealed class Report { public int equipmentScreens,battleScreens;public string[] errors; }
        readonly List<string> errors=new List<string>();BattleDirector battle;string directory;int equipmentScreens,battleScreens;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--signature-weapon-review");if(i<0)return;
            if(i+1>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)){Application.Quit(2);return;}
            var review=new GameObject("Signature weapon review").AddComponent<SignatureWeaponReview>();
            review.directory=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Reviews",args[i+1]);
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(directory);Application.runInBackground=true;Application.logMessageReceived+=Log;yield return null;
            battle=UnityEngine.Object.FindAnyObjectByType<BattleDirector>();battle.ConfigureStorage(directory);battle.Audio.PersonalMusicEnabled=false;battle.PersistCampaign=_=>true;
            battle.TrainingMode=false;for(int i=1;i<=20;i++)battle.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(i));
            var items=battle.Catalog.Equipment.Where(e=>e.Appearance!=null).ToArray();
            if(items.Length!=10)errors.Add("Expected ten signature weapons");
            foreach(var item in items){battle.Campaign.Inventory.Add(new OwnedEquipment{Id=item.Id,Count=1});battle.Campaign.Get(item.ExclusiveCharacterId).Level=35;}
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080)})
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);battle.Preferences.TextScale=size.x==1280?1.3f:1;yield return new WaitForSecondsRealtime(.5f);
                foreach(var item in items)
                {
                    var c=battle.Catalog.Characters.Single(x=>x.Id==item.ExclusiveCharacterId);battle.Campaign.Get(c.Id).Equipment[0]=null;
                    battle.Deployment.Clear();battle.Deployment.Add(Array.IndexOf(battle.Catalog.Characters,c));battle.Hud.ShowIntermissionHub();yield return Settle();Click("거점 캐릭터: "+c.Id);yield return Settle();
                    if(size.x==1280)yield return Capture(c.Id+"-default");
                    string name="장착 후보: "+item.DisplayName;int pages=0;
                    while(!battle.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name==name)&&pages++<10){Click("다음 장비 후보");yield return Settle();}
                    Click(name);yield return Settle();
                    var preview=battle.Hud.GetComponentsInChildren<CharacterModelImage>().Single(i=>i.name=="EquippedModelPreview");
                    if(preview.sprite==c.Sprites.Front)errors.Add("Unchanged preview: "+c.Id);
                    Inspect();yield return Capture(c.Id+"-equipped-"+size.x);equipmentScreens++;
                    Click("적용 · 저장");yield return Settle();if(battle.Campaign.Get(c.Id).Equipment[0]!=item.Id)errors.Add("Not saved: "+c.Id);
                }
            }
            battle.TrainingMode=true;battle.Hud.ShowDeployment();yield return Settle();yield return Capture("formation-equipped");
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);battle.Preferences.TextScale=1;
            for(int group=0;group<2;group++)
            {
                battle.Deployment.Clear();battle.Deployment.AddRange(items.Skip(group*5).Take(5).Select(e=>Array.FindIndex(battle.Catalog.Characters,c=>c.Id==e.ExclusiveCharacterId)));
                battle.TrainingMode=true;battle.SelectedStage=6;battle.BeginBattle();if(battle.StoryActive)battle.FinishStory();yield return new WaitForSecondsRealtime(1);
                if(battle.Session==null){errors.Add("No battle session");break;}
                var expected=items.Skip(group*5).Take(5).Select(e=>e.ExclusiveCharacterId).OrderBy(id=>id);
                var actual=battle.Session.Units.Where(u=>u.Team==Team.Player).Select(u=>u.Data.Id).OrderBy(id=>id);
                if(!expected.SequenceEqual(actual))errors.Add("Incomplete battle party: "+group);
                for(int rotation=0;rotation<4;rotation++)
                {
                    if(rotation>0)battle.Board.RotateCamera(90);yield return Settle();
                    foreach(var motion in UnityEngine.Object.FindObjectsByType<CharacterMotion>().Where(m=>m.Owner.Team==Team.Player))
                        if(!motion.Owner.Equipment[0].Appearance.Frames.Any(f=>f.Equipped==motion.GetComponent<SpriteRenderer>().sprite))errors.Add("Original battle art: "+motion.Owner.Data.Id);
                    yield return Capture("battle-"+group+"-"+rotation);battleScreens++;
                }
                battle.Restart();yield return Settle();
            }
            File.WriteAllText(Path.Combine(directory,"report.json"),JsonUtility.ToJson(new Report{equipmentScreens=equipmentScreens,battleScreens=battleScreens,errors=errors.ToArray()},true));
            Application.Quit(errors.Count==0?0:1);
        }
        void Inspect()
        {
            foreach(var text in battle.Hud.GetComponentsInChildren<TMPro.TMP_Text>())
            {text.ForceMeshUpdate();if(text.isTextOverflowing)errors.Add("Text overflow: "+text.text);}
            foreach(var button in battle.Hud.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                if(!button.IsInteractable())continue;var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                if(corners.Any(c=>c.x < -1||c.y < -1||c.x>Screen.width+1||c.y>Screen.height+1))errors.Add("Offscreen: "+button.name);
            }
        }
        IEnumerator Settle(){yield return null;yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();}
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());Destroy(image);}
        void Click(string name){var b=battle.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(x=>x.name==name);if(!b.IsInteractable())throw new InvalidOperationException(name);b.onClick.Invoke();}
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors.Add(message+"\n"+trace);File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);}}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
#endif
