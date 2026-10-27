#if UNITY_EDITOR || DEBUG || TALESTACTICS_REVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
namespace TalesTactics
{
    // Development-player presentation fixture. Isolated storage; does not alter shipped assets.
    public sealed class CrossoverPlayerReview:MonoBehaviour
    {
        [Serializable] public sealed class DirectionRow
        {
            public string boss,surface,facing,worldFacing,expectedSprite,actualSprite,capture;public float cameraYaw;public bool matches,visible;
        }
        [Serializable] public sealed class DirectionReport
        {
            public string scope="Presentation fixture with all stages unlocked and level 40; not a campaign playthrough. Status model direction uses the rendered rotation button. Battlefield direction uses the existing board camera rotation API.";
            public DirectionRow[] rows;public string[] errors;
        }
        string directory;BattleDirector battle;bool directionsOnly,finished,oldBackground,started;float oldTimeScale;readonly List<string> errors=new List<string>();readonly List<string> captures=new List<string>();readonly List<DirectionRow> directions=new List<DirectionRow>();
#if UNITY_EDITOR
        public static void BeginEditor(string output,bool onlyDirections=false)
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Enter Play Mode before starting the review.");
            if(FindAnyObjectByType<CrossoverPlayerReview>()!=null)throw new InvalidOperationException("A crossover review already exists.");
            ValidateOutput(output);var review=new GameObject("Crossover presentation review").AddComponent<CrossoverPlayerReview>();review.directory=Path.GetFullPath(output);review.directionsOnly=onlyDirections;
        }
#endif
        static void ValidateOutput(string path)
        {
            string root=Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),user=Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(root.Equals(user,StringComparison.OrdinalIgnoreCase)||root.StartsWith(user+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("User storage cannot be review output.");
            if(Directory.Exists(root)&&Directory.EnumerateFileSystemEntries(root).Any())throw new InvalidOperationException("Use a new empty review output.");
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--crossover-review");bool only=false;if(i<0){i=Array.IndexOf(args,"--crossover-directions-review");only=true;}if(i<0)return;
            if(i+1>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)){Application.Quit(2);return;}
            string output=Path.Combine(Path.GetDirectoryName(Application.dataPath),"CrossoverReviews",args[i+1]);ValidateOutput(output);
            var review=new GameObject("Crossover presentation review").AddComponent<CrossoverPlayerReview>();
            review.directory=output;review.directionsOnly=only;
        }
        IEnumerator Start()
        {
            oldBackground=Application.runInBackground;oldTimeScale=Time.timeScale;started=true;Application.runInBackground=true;Application.logMessageReceived+=Log;
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                object item=null;bool next=false;Exception failure=null;
                try{next=stack.Peek().MoveNext();if(next)item=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){errors.Add(failure.ToString());break;}
                if(!next){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(item is IEnumerator nested)stack.Push(nested);else yield return item;
            }
            Finish();
        }
        IEnumerator Run()
        {
            Directory.CreateDirectory(directory);yield return null;battle=UnityEngine.Object.FindAnyObjectByType<BattleDirector>();battle.ConfigureStorage(directory,true);
            Screen.SetResolution(1600,900,FullScreenMode.Windowed);yield return new WaitForSeconds(.5f);
            var staged=Instantiate(battle.Catalog);staged.CrossoverReady=true;battle.Catalog=staged;battle.TrainingMode=false;
            for(int n=1;n<=20;n++)battle.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(n));
            foreach(var data in staged.Characters)battle.Campaign.Get(data.Id).Level=40;
            battle.Audio.PersonalMusicEnabled=false;
            for(int stage=6;!directionsOnly&&stage<=25;stage++)
            {
                Begin(stage);yield return Capture("episode-"+(stage-5).ToString("00"));
                battle.Hud.ShowMission();yield return Capture("mission-"+(stage-5).ToString("00"));battle.Hud.CloseMission();
            }
            foreach(var entry in CrossoverBossSources.Entries)
            {
                int index=Array.FindIndex(CrossoverCampaign.Episodes,e=>e.Boss==entry[0]||e.SecondBoss==entry[0]);int stage=entry[0]=="hyades"?21:index>=0?index+6:entry[0]=="origin"?28:34;
                if(PersonalScenarios.Contains(stage)){battle.Campaign.StoryProgress.Add(PersonalScenarios.Flag(stage-2));battle.Campaign.StoryProgress.Add(PersonalScenarios.Flag(stage-1));}
                Begin(stage);var b=battle.Session;var boss=b.Units.First(u=>u.Data.Id==entry[0]);
                if(boss.CrossoverReserve){b.Crossover.Boss.Damage(99999,b.Grid);b.Crossover.Update();battle.RefreshViews();}
                yield return CaptureDirections(boss);
                if(directionsOnly)continue;
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
            for(int stage=26;!directionsOnly&&stage<56;stage++)
            {
                battle.SelectedStage=stage;battle.Hud.ShowPersonalScenarios();yield return Capture("personal-menu-"+(stage-26).ToString("00"));
                battle.ReplayStory(false);yield return Capture("personal-story-"+(stage-26).ToString("00"));battle.FinishStory();
            }
        }
        IEnumerator CaptureDirections(UnitRuntime boss)
        {
            RequireActiveBossRenderer(boss);
            battle.Board.SetAnimation(boss,AnimationKind.Idle);battle.Hud.ShowUnitDetails(boss);
            for(int direction=0;direction<4;direction++)
            {
                if(direction>0)
                {
                    var button=battle.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="모델 회전"&&b.gameObject.activeInHierarchy);
                    if(!button.interactable)throw new InvalidOperationException("Model rotation is disabled for "+boss.Data.Id);
                    ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
                }
                yield return null;yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
                var model=battle.Hud.GetComponentsInChildren<CharacterModelImage>().Single(m=>m.Character==boss.Data&&m.name=="Body"&&m.isActiveAndEnabled);
                var facing=(Facing)direction;var expected=EquipmentAppearance.Resolve(boss,boss.Data.Sprites.Get(facing));string name=boss.Data.Id+"-model-direction-"+facing;
                var row=new DirectionRow{boss=boss.Data.Id,surface="StatusModel",facing=facing.ToString(),worldFacing=boss.Facing.ToString(),cameraYaw=battle.Board.BattleCamera.transform.eulerAngles.y,expectedSprite=expected?.name,actualSprite=model.sprite?.name,matches=expected!=null&&expected==model.sprite,visible=model.isActiveAndEnabled,capture=name+".png"};
                directions.Add(row);if(!row.matches)throw new InvalidOperationException("Status model direction mismatch: "+boss.Data.Id+" "+facing);yield return Capture(name);
            }
            battle.Hud.CloseUnitDetails();battle.Board.ResetCamera();
            for(int direction=0;direction<4;direction++)
            {
                if(direction>0)battle.Board.RotateCamera(90);battle.Board.SetAnimation(boss,AnimationKind.Idle);
                yield return null;yield return new WaitForEndOfFrame();
                var renderer=RequireActiveBossRenderer(boss);
                var facing=CharacterMotion.ViewFacing(boss.Facing,battle.Board.BattleCamera.transform.rotation);var expected=EquipmentAppearance.Resolve(boss,boss.Data.Sprites.Get(facing));string name=boss.Data.Id+"-battlefield-direction-"+facing;
                var row=new DirectionRow{boss=boss.Data.Id,surface="Battlefield",facing=facing.ToString(),worldFacing=boss.Facing.ToString(),cameraYaw=battle.Board.BattleCamera.transform.eulerAngles.y,expectedSprite=expected?.name,actualSprite=renderer.sprite?.name,matches=expected!=null&&expected==renderer.sprite,visible=renderer.enabled&&renderer.gameObject.activeInHierarchy&&GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(battle.Board.BattleCamera),renderer.bounds),capture=name+".png"};
                directions.Add(row);if(!row.matches||!row.visible)throw new InvalidOperationException("Battlefield direction mismatch or invisible boss: "+boss.Data.Id+" "+facing);yield return Capture(name);
            }
            foreach(string surface in new[]{"StatusModel","Battlefield"})if(directions.Where(r=>r.boss==boss.Data.Id&&r.surface==surface).Select(r=>r.facing).Distinct().Count()!=4)throw new InvalidOperationException("Four unique directions were not observed for "+boss.Data.Id+" "+surface);
        }
        SpriteRenderer RequireActiveBossRenderer(UnitRuntime boss)
        {
            if(!boss.Alive||boss.CrossoverReserve)throw new InvalidOperationException("Boss is not active for direction review: "+boss.Data.Id+" alive="+boss.Alive+" reserve="+boss.CrossoverReserve);
            // Include inactive objects only to diagnose them; an inactive model must still fail.
            var models=battle.Board.GetComponentsInChildren<CharacterMotion>(true).Where(m=>m.Owner==boss).ToArray();
            if(models.Length!=1)throw new InvalidOperationException("Expected one battlefield model for "+boss.Data.Id+", found "+models.Length);
            var motion=models[0];var renderer=motion.GetComponent<SpriteRenderer>();
            if(!motion.isActiveAndEnabled||renderer==null||!renderer.enabled||!renderer.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Battlefield model is inactive or disabled after view synchronization: "+boss.Data.Id);
            return renderer;
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
            try{File.WriteAllBytes(Path.Combine(directory,name+".png"),texture.EncodeToPNG());}finally{Destroy(texture);}captures.Add(name);
            File.WriteAllText(Path.Combine(directory,"directions.json"),JsonUtility.ToJson(new DirectionReport{rows=directions.ToArray(),errors=errors.ToArray()},true));File.WriteAllLines(Path.Combine(directory,"captures.txt"),captures);
        }
        void Finish()
        {
            if(finished)return;finished=true;int exit=2;Application.logMessageReceived-=Log;
            try
            {
                File.WriteAllLines(Path.Combine(directory,"captures.txt"),captures);File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);
                File.WriteAllText(Path.Combine(directory,"directions.json"),JsonUtility.ToJson(new DirectionReport{rows=directions.ToArray(),errors=errors.ToArray()},true));
                File.WriteAllText(Path.Combine(directory,"result.txt"),"Unity "+Application.unityVersion+"\nPresentation fixtures, not balance certification. Impact recipients are staged; health is unchanged by skill presentation. Reserve bosses are activated by staged damage to the first boss.\nDirectionsOnly "+directionsOnly+"\nCaptures "+captures.Count+"\nErrors "+errors.Count+"\n");
                if(errors.Count==0)exit=0;
            }
            catch(Exception e){errors.Add("Could not write final crossover review results: "+e);Debug.LogError(errors[errors.Count-1]);}
            finally
            {
                try{Restore();}
                catch(Exception e){exit=2;Debug.LogError("Could not restore crossover review state: "+e);}
                finally
                {
#if UNITY_EDITOR
                    UnityEditor.SessionState.SetInt("TalesTactics.CrossoverPlayerReviewExit",exit);UnityEditor.EditorApplication.isPlaying=false;
#else
                    Application.Quit(exit);
#endif
                }
            }
        }
        void Log(string message,string trace,LogType type)
        {
            if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;
            errors.Add(message+"\n"+trace);
            try{File.WriteAllLines(Path.Combine(directory,"errors.txt"),errors);}catch(Exception e){errors.Add("Could not persist crossover review error log: "+e.Message);}
        }
        void Restore(){Application.logMessageReceived-=Log;if(!started)return;Time.timeScale=oldTimeScale;Application.runInBackground=oldBackground;}
        void OnDestroy(){Restore();}
    }
}
#endif
