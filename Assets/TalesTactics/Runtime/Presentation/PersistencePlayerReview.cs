#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    // Separate processes and isolated files verify actual player persistence, not Editor memory.
    public sealed class PersistencePlayerReview:MonoBehaviour
    {
        string root,phase;bool ct;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot()
        {
            if(Application.isEditor)return;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--persistence-review");if(i<0)return;
            if(i+3>=args.Length||!Guid.TryParseExact(args[i+1],"N",out _)||(args[i+2]!="spd"&&args[i+2]!="ct")||(args[i+3]!="save"&&args[i+3]!="resume")){Application.Quit(2);return;}
            var review=new GameObject("Persistence review").AddComponent<PersistencePlayerReview>();review.root=Path.Combine(Application.persistentDataPath,"PersistenceReviews",args[i+1],args[i+2]);review.ct=args[i+2]=="ct";review.phase=args[i+3];Application.runInBackground=true;
        }
        IEnumerator Start()
        {
            yield return null;
            bool passed=false;
            try{Run();passed=true;}
            catch(Exception e){Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,phase+"-failed.txt"),e.ToString());Application.Quit(2);}
            if(!passed)yield break;
            yield return new WaitForSecondsRealtime(.5f);
            try{Check(Screen.width==1366&&Screen.height==768,"Display setting applied in player");File.WriteAllText(Path.Combine(root,phase+"-passed.txt"),"PASS "+DateTime.UtcNow.ToString("o")+" · 1366x768");Application.Quit(0);}
            catch(Exception e){File.WriteAllText(Path.Combine(root,phase+"-failed.txt"),e.ToString());Application.Quit(2);}
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        void Run()
        {
            var battle=UnityEngine.Object.FindAnyObjectByType<BattleDirector>();battle.ConfigureStorage(root,true);Directory.CreateDirectory(root);
            if(phase=="save")
            {
                var settings=new PlayerPreferences{Music=.4f,Effects=.2f,CursorSpeed=1.5f,AutoTiming=true,CT=ct,Width=1366,Height=768};Check(battle.SavePreferences(settings),"Settings write");
                Check(battle.SelectProfile(1),"Switch to slot2");battle.Campaign.Gold=123;Check(battle.SavePreparation(),"Save slot2");Check(battle.SelectProfile(0)&&battle.Campaign.Gold==300,"Slots isolated");
                battle.TrainingMode=false;battle.BeginBattle();var session=battle.Session;
                var p=session.Grid.Reachable(session.Active,out _).Keys.First(x=>x!=session.Active.Position);Check(session.Move(p),"Move before checkpoint");
                session.Active.AddStatus(StatusKind.Song,3);session.Active.SpecialGauge=40;
                var state=BattleCheckpoint.Capture(session,battle.Deployment.ToArray());File.WriteAllText(Path.Combine(root,"expected.json"),JsonUtility.ToJson(state));
                Check(battle.SuspendBattle()&&battle.Session==null,"Suspend succeeds");
            }
            else
            {
                Check(battle.Preferences.Music==.4f&&battle.Preferences.Effects==.2f&&battle.Preferences.CursorSpeed==1.5f&&battle.Preferences.AutoTiming&&battle.Preferences.CT==ct,"Settings survive process restart");
                Check(battle.Campaign.SuspendedBattle!=null,"Checkpoint read");var expected=File.ReadAllText(Path.Combine(root,"expected.json"));
                Check(battle.ResumeBattle(),"Resume succeeds");Check(JsonUtility.ToJson(BattleCheckpoint.Capture(battle.Session,battle.Deployment.ToArray()))==expected,"All combat state preserved");
                Check(new CampaignFile(battle.Profiles.PathFor(0)).Load().SuspendedBattle==null,"Checkpoint consumed on disk");
                var reference=JsonUtility.FromJson<BattleCheckpoint>(expected).Restore(battle.Catalog);
                for(int i=0;i<20;i++){reference.EndTurn();reference.Advance();battle.Session.EndTurn();battle.Session.Advance();Check(reference.Units.IndexOf(reference.Active)==battle.Session.Units.IndexOf(battle.Session.Active),"Next20 turns preserve scheduler");}
                battle.Restart();Check(battle.SelectProfile(1)&&battle.Campaign.Gold==123,"Other slot survives process restart");
            }
        }
    }
}
#endif
