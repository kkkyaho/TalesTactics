#if UNITY_EDITOR || DEBUG || TALESTACTICS_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed class CombatPresentationReview:MonoBehaviour
    {
        string directory;BattleDirector battle;readonly List<string> captures=new List<string>(),errors=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--combat-presentation-review");if(i<0)return;
            if(i+1>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)){Application.Quit(2);return;}
            var r=new GameObject("Combat presentation review").AddComponent<CombatPresentationReview>();r.directory=Path.Combine(Path.GetDirectoryName(Application.dataPath),"PresentationReviews",args[i+1]);
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;Directory.CreateDirectory(directory);Application.logMessageReceived+=Log;
            yield return null;battle=UnityEngine.Object.FindAnyObjectByType<BattleDirector>();battle.ConfigureStorage(directory);battle.Audio.PersonalMusicEnabled=false;
            battle.Preferences.Width=1280;battle.Preferences.Height=720;Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSeconds(.5f);
            for(int i=1;i<=20;i++)battle.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(i));
            foreach(var c in battle.Catalog.Characters)battle.Campaign.Get(c.Id).Level=25;
            Begin(0);var u=battle.Session.Active;
            foreach(float scale in new[]{1f,1.3f})for(int rotation=0;rotation<4;rotation++)
            {
                battle.Preferences.TextScale=scale;battle.Board.RotateCamera(rotation==0?0:90);battle.SetState(new FacingSelectionState(battle));
                yield return Capture("facing-"+scale+"-"+rotation);battle.State.Cancel();
            }
            battle.Preferences.TextScale=1;battle.Board.ResetCamera();u.Level=8;u.EXP=68;u.CurrentHP=u.Stats.HP;u.CurrentMP=u.Stats.MP;
            battle.Hud.CombatFeedback.Enqueue(ActionExperience.Grant(u,47));battle.Hud.CombatFeedback.Number(u,-128,false);
            yield return Capture("damage-experience");yield return new WaitForSeconds(1.3f);yield return Capture("level-up");
            yield return new WaitForSeconds(1f);
            var target=battle.Session.Units.First(x=>x.Team==Team.Enemy);
            var pool=battle.Board.GetComponent<ImpactPool>()??battle.Board.gameObject.AddComponent<ImpactPool>();
            foreach(Element element in Enum.GetValues(typeof(Element)))foreach(bool reduced in new[]{false,true})
            {
                pool.Play(battle.Board.BattleCamera,battle.Board.HeadWorld(u),battle.Board.HeadWorld(target)-battle.Board.BattleCamera.transform.up*.55f,element,CombatVisualStyle.Sacred,CombatFeedback.Strike,1.2f,false,reduced);
                yield return new WaitForSeconds(.22f);yield return Capture("element-"+element+(reduced?"-reduced":""));yield return new WaitForSeconds(1.1f);
            }
            for(int i=0;i<battle.Catalog.Characters.Length;i++)
            {
                Begin(i);u=battle.Session.Active;target=battle.Session.Units.First(x=>x.Team==Team.Enemy);var skill=u.Data.BasicAttack;
                u.Facing=SkillResolver.Toward(u.Position,target.Position);battle.Board.BeginSkill(u,skill,target.Position);yield return new WaitForSeconds(BoardView.Windup(skill)*.7f);yield return Capture(u.Data.Id+"-windup");
                var before=battle.Board.CaptureHealth();target.CurrentHP-=12;battle.Board.ReleaseSkill(u);battle.Board.PresentImpact(u,skill,target.Position,before,new[]{target});
                yield return Capture(u.Data.Id+"-contact");yield return new WaitForSeconds(.25f);yield return Capture(u.Data.Id+"-follow");yield return new WaitForSeconds(1.1f);
            }
            var mapping=new List<string>{"character,skill,element,style,pattern"};
            foreach(var c in battle.Catalog.Characters.Concat(battle.Catalog.Enemies).Concat(battle.Catalog.CrossoverVariants).Where(c=>c!=null).Distinct())
                foreach(var s in c.Skills.Concat(new[]{c.BasicAttack,c.UltimateSkill}).Where(s=>s!=null).Distinct())mapping.Add(c.Id+","+s.Id+","+s.Element+","+CombatEffect.Resolve(c,s)+","+s.Presentation.Pattern);
            File.WriteAllLines(Path.Combine(directory,"skill-mapping.csv"),mapping);
            File.WriteAllText(Path.Combine(directory,"result.json"),"{\"captures\":"+captures.Count+",\"errors\":"+errors.Count+",\"width\":"+Screen.width+",\"height\":"+Screen.height+",\"fixture\":true}");
            Application.Quit(errors.Count==0?0:1);
        }
        void Begin(int index)
        {
            if(battle.Session!=null)battle.Restart();battle.TrainingMode=false;battle.SelectedStage=10;battle.Deployment.Clear();battle.Deployment.Add(index);battle.BeginBattle();battle.StopAllCoroutines();
            var units=battle.Session.Units.ToArray();int n=0;foreach(var unit in units){var p=new Vector2Int(1+n++%10,1);battle.Session.Grid[p].Walkable=true;battle.Session.Grid.Place(unit,p);}
            var actor=units.First(u=>u.Team==Team.Player);var enemy=units.First(u=>u.Team==Team.Enemy);
            foreach(var p in new[]{new Vector2Int(5,5),new Vector2Int(6,5)})battle.Session.Grid[p].Walkable=true;
            battle.Session.Grid.Place(actor,new Vector2Int(5,5));battle.Session.Grid.Place(enemy,new Vector2Int(6,5));battle.Session.Active=actor;battle.SetState(new CommandState(battle));battle.RefreshViews();
        }
        IEnumerator Capture(string name)
        {yield return new WaitForSeconds(.04f);yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(directory,name+".png"),texture.EncodeToPNG());Destroy(texture);captures.Add(name);}
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors.Add(message+"\n"+trace);File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);}}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
#endif
