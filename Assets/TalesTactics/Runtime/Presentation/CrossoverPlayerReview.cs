#if UNITY_EDITOR || DEBUG || TALESTACTICS_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    // Development-player presentation fixture. Isolated storage; does not alter shipped assets.
    public sealed class CrossoverPlayerReview:MonoBehaviour
    {
        string directory;BattleDirector battle;readonly List<string> errors=new List<string>();readonly List<string> captures=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--crossover-review");if(i<0)return;
            if(i+1>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)){Application.Quit(2);return;}
            var review=new GameObject("Crossover presentation review").AddComponent<CrossoverPlayerReview>();
            review.directory=Path.Combine(Path.GetDirectoryName(Application.dataPath),"CrossoverReviews",args[i+1]);
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;Directory.CreateDirectory(directory);Application.logMessageReceived+=Log;
            yield return null;battle=UnityEngine.Object.FindAnyObjectByType<BattleDirector>();battle.ConfigureStorage(directory,true);
            Screen.SetResolution(1600,900,FullScreenMode.Windowed);yield return new WaitForSeconds(.5f);
            var staged=Instantiate(battle.Catalog);staged.CrossoverReady=true;battle.Catalog=staged;battle.TrainingMode=false;
            for(int n=1;n<=20;n++)battle.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(n));
            foreach(var data in staged.Characters)battle.Campaign.Get(data.Id).Level=40;
            battle.Audio.PersonalMusicEnabled=false;
            for(int stage=6;stage<=25;stage++)
            {
                Begin(stage);yield return Capture("episode-"+(stage-5).ToString("00"));
                battle.Hud.ShowMission();yield return Capture("mission-"+(stage-5).ToString("00"));battle.Hud.CloseMission();
            }
            foreach(var entry in CrossoverBossSources.Entries)
            {
                int index=Array.FindIndex(CrossoverCampaign.Episodes,e=>e.Boss==entry[0]||e.SecondBoss==entry[0]);int stage=entry[0]=="hyades"?21:index>=0?index+6:entry[0]=="origin"?28:34;
                if(PersonalScenarios.Contains(stage)){battle.Campaign.StoryProgress.Add(PersonalScenarios.Flag(stage-2));battle.Campaign.StoryProgress.Add(PersonalScenarios.Flag(stage-1));}
                Begin(stage);var b=battle.Session;var boss=b.Units.First(u=>u.Data.Id==entry[0]);
                if(boss.CrossoverReserve){b.Crossover.Boss.Damage(99999,b.Grid);b.Crossover.Update();}
                for(int rotation=0;rotation<4;rotation++)
                {
                    battle.Board.RotateCamera(rotation==0?0:90);battle.Hud.ShowUnitDetails(boss);yield return Capture(entry[0]+"-direction-"+rotation);battle.Hud.CloseUnitDetails();
                }
                foreach(var skill in boss.Data.Skills.Where(s=>s.Id.StartsWith("boss."+entry[0]+".")))
                {
                    boss.BeginTurn();boss.IntentPhase=1;boss.IntentSkill=skill.Id;boss.IntentTurn=boss.TurnsStarted;boss.IntentAim=b.Units.First(u=>u.Team==Team.Player&&!u.CrossoverReserve).Position;
                    battle.RefreshViews();battle.Hud.ShowUnitDetails(boss);yield return Capture(skill.Id+"-telegraph");battle.Hud.CloseUnitDetails();
                    battle.Board.BeginSkill(boss,skill,boss.IntentAim);yield return new WaitForSeconds(BoardView.Windup(skill)*.75f);yield return Capture(skill.Id+"-windup");
                    var before=battle.Board.CaptureHealth();var recipient=skill.Target==TargetType.Self?boss:b.Units.First(u=>u.Team==Team.Player&&!u.CrossoverReserve);
                    battle.Board.ReleaseSkill(boss);battle.Board.PresentImpact(boss,skill,boss.IntentAim,before,new[]{recipient},before,false);
                    yield return new WaitForSeconds(BoardView.Recovery(skill)*.35f);yield return Capture(skill.Id+"-release");
                    yield return new WaitForSeconds(BoardView.Recovery(skill));
                }
                boss.IntentPhase=0;battle.RefreshViews();battle.Board.SetAnimation(boss,AnimationKind.Dead);yield return new WaitForSeconds(.3f);
                for(int rotation=0;rotation<4;rotation++){battle.Board.RotateCamera(90);yield return Capture(entry[0]+"-collapse-"+rotation);}
            }
            battle.Restart();
            for(int stage=26;stage<56;stage++)if(!battle.Campaign.StoryProgress.Contains(PersonalScenarios.Flag(stage)))battle.Campaign.StoryProgress.Add(PersonalScenarios.Flag(stage));
            for(int stage=26;stage<56;stage++)
            {
                battle.SelectedStage=stage;battle.Hud.ShowPersonalScenarios();yield return Capture("personal-menu-"+(stage-26).ToString("00"));
                battle.ReplayStory(false);yield return Capture("personal-story-"+(stage-26).ToString("00"));battle.FinishStory();
            }
            File.WriteAllLines(Path.Combine(directory,"captures.txt"),captures);File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);
            File.WriteAllText(Path.Combine(directory,"result.txt"),"Unity "+Application.unityVersion+"\nPresentation fixtures, not balance certification. Impact recipients are staged; health is unchanged.\nCaptures "+captures.Count+"\nErrors "+errors.Count+"\n");
            Application.Quit(errors.Count==0?0:2);
        }
        void Begin(int stage)
        {
            battle.Restart();battle.SelectedStage=stage;battle.Deployment.Clear();battle.Deployment.AddRange(Enumerable.Range(0,battle.Catalog.Characters.Length).Where(battle.CanDeploy).Take(6));
            battle.BeginBattle();battle.StopAllCoroutines();if(battle.Session==null)throw new InvalidOperationException("Review could not start "+stage);
            battle.RefreshViews();
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForSeconds(.12f);yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(directory,name+".png"),texture.EncodeToPNG());Destroy(texture);captures.Add(name);
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors.Add(message+"\n"+trace);File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);}}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
#endif
