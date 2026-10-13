#if UNITY_EDITOR || DEBUG || TALESTACTICS_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed class VfxQualityReview:MonoBehaviour
    {
        string directory;BattleDirector battle;readonly List<string> errors=new List<string>(),runs=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--vfx-quality-review");if(i<0)return;
            if(i+1>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)){Application.Quit(2);return;}
            var r=new GameObject("VFX quality review").AddComponent<VfxQualityReview>();r.directory=Path.Combine(Path.GetDirectoryName(Application.dataPath),"VfxReviews",args[i+1]);
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;Directory.CreateDirectory(directory);Application.logMessageReceived+=Log;
            yield return null;battle=UnityEngine.Object.FindAnyObjectByType<BattleDirector>();battle.ConfigureStorage(directory);battle.Audio.PersonalMusicEnabled=false;
            Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSeconds(.5f);
            for(int i=1;i<=20;i++)battle.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(i));
            foreach(var c in battle.Catalog.Characters)battle.Campaign.Get(c.Id).Level=25;
            Time.captureFramerate=30;
            foreach(bool fire in new[]{true,false})foreach(bool focus in new[]{false,true})
            {
                Begin(fire);var u=battle.Session.Active;var target=battle.Session.Units.First(x=>x.Team==Team.Enemy);
                var skill=fire?u.Data.Skills.First(s=>s.Element==Element.Fire):u.Data.BasicAttack;
                battle.Target=target.Position;if(focus)battle.Board.FocusCurrent();yield return null;
                string name=(fire?"fire":"sword")+(focus?"-focus":"-wide");string frames=Path.Combine(directory,name);Directory.CreateDirectory(frames);
                string reason=battle.Session.Resolver.CanUse(u,skill);if(reason!=null){errors.Add(name+": "+reason);continue;}
                int before=target.CurrentHP;var position=u.Position;
                battle.StartCoroutine(battle.Execute(skill,target.Position));
                battle.Target=target.Position;battle.Board.RefreshFocus();
                for(int frame=0;frame<75;frame++)
                {
                    yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(Path.Combine(frames,frame.ToString("D4")+".png"),texture.EncodeToPNG());Destroy(texture);
                    if(frame==(int)(BoardView.Windup(skill)*30)+3)File.Copy(Path.Combine(frames,frame.ToString("D4")+".png"),Path.Combine(directory,name+"-contact.png"),true);
                    yield return null;
                }
                if(target.CurrentHP>=before)errors.Add(name+": actual skill caused no damage");if(u.Position!=position)errors.Add(name+": presentation changed grid position");
                runs.Add("{\"name\":\""+name+"\",\"skill\":\""+skill.Id+"\",\"beforeHP\":"+before+",\"afterHP\":"+target.CurrentHP+",\"frames\":75}");
            }
            Time.captureFramerate=0;File.WriteAllText(Path.Combine(directory,"result.json"),"{\"errors\":"+errors.Count+",\"fps\":30,\"width\":"+Screen.width+",\"height\":"+Screen.height+",\"actualSkillExecution\":true,\"fixturePlacement\":true,\"runs\":["+string.Join(",",runs)+"]}");
            File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);Application.Quit(errors.Count==0?0:1);
        }
        void Begin(bool fire)
        {
            if(battle.Session!=null)battle.Restart();battle.TrainingMode=false;battle.SelectedStage=10;battle.Deployment.Clear();battle.Deployment.Add(Array.FindIndex(battle.Catalog.Characters,c=>c.Id==(fire?"jade":"cless")));battle.BeginBattle();battle.StopAllCoroutines();
            var units=battle.Session.Units.ToArray();int n=0;foreach(var unit in units){var p=new Vector2Int(1+n++%10,1);battle.Session.Grid[p].Walkable=true;battle.Session.Grid.Place(unit,p);}
            var actor=units.First(u=>u.Team==Team.Player);var enemy=units.First(u=>u.Team==Team.Enemy);
            var a=new Vector2Int(4,5);var b=new Vector2Int(fire?7:5,5);
            foreach(var p in new[]{a,b})battle.Session.Grid[p].Walkable=true;
            battle.Session.Grid.Place(actor,a);battle.Session.Grid.Place(enemy,b);battle.Session.Active=actor;actor.BeginTurn();battle.SetState(new CommandState(battle));battle.RefreshViews();
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors.Add(message+"\n"+trace);File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);Application.Quit(1);}}
        void OnDestroy(){Time.captureFramerate=0;Application.logMessageReceived-=Log;}
    }
}
#endif
