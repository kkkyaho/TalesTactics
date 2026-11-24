using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using TalesTactics;

public static class SetupPilotBattle
{
    public static async Task<object> Main(string storage)
    {
        if(!EditorApplication.isPlaying||EditorApplication.isPaused)throw new InvalidOperationException("Enter empty, unpaused Play Mode first.");
        string project=Path.GetFullPath(Path.Combine(Application.dataPath,"..")).Replace('\\','/').TrimEnd('/');
        if(!project.Equals("C:/dev/TalesTactics/Builds/SourceOnlyPublish20261118/Project",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Wrong project.");
        string allowed="C:/dev/TalesTactics/Logs/VisualPilot20261124/";
        storage=Path.GetFullPath(storage).Replace('\\','/');
        if(!storage.StartsWith(allowed,StringComparison.OrdinalIgnoreCase)||Directory.Exists(storage))throw new InvalidOperationException("Use a fresh isolated storage directory below this task's Logs.");
        if(UnityEngine.Object.FindAnyObjectByType<BattleDirector>()!=null)throw new InvalidOperationException("An existing Director must not be reused.");
        BattleDirector director=null;
        UnityEngine.Events.UnityAction<Scene,LoadSceneMode> callback=null;
        callback=(scene,mode)=>{
            if(scene.name!="TestBattle")return;
            director=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BattleDirector>(true)).Single();
            director.enabled=false;director.Hud.enabled=false;director.Board.enabled=false;
        };
        SceneManager.sceneLoaded+=callback;
        try
        {
            var load=SceneManager.LoadSceneAsync("TestBattle",LoadSceneMode.Single);
            while(!load.isDone)await Awaitable.NextFrameAsync();
            if(director==null||director.enabled)throw new InvalidOperationException("Production Start was not suppressed.");
            director.ConfigureStorage(storage,false,false);
            director.Audio.PersonalMusicEnabled=false;director.Audio.SetVolumes(0,0);
            director.Hud.Initialize(director);director.Board.Initialize(director);
            if(!CatalogValidation.TryValidate(director.Catalog,out var error))throw new InvalidOperationException(error);
            director.Hud.enabled=true;director.Board.enabled=true;
            director.Hud.HideTitle();director.TrainingMode=false;
            director.SelectedStage=CrossoverCampaign.FirstStage+5;
            director.Campaign=new CampaignSave{SelectedStage=director.SelectedStage};
            for(int n=1;n<6;n++)director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(n));
            int level=CampaignStages.Get(director.SelectedStage).EntryLevel;
            foreach(var data in director.Catalog.Characters)director.Campaign.Get(data.Id).Level=level;
            int alphenIndex=Array.FindIndex(director.Catalog.Characters,c=>c.Id=="alphen");if(alphenIndex<0||!director.CanDeploy(alphenIndex))throw new InvalidOperationException("Alphen must be joined.");director.Deployment.Clear();director.Deployment.Add(alphenIndex);director.Deployment.AddRange(Enumerable.Range(0,director.Catalog.Characters.Length).Where(i=>i!=alphenIndex&&director.CanDeploy(i)).Take(5));
            director.Preferences.SeenContextHints=7;director.Preferences.Reactions=false;director.Preferences.MissionEvents=false;director.Campaign.AutoTiming=false;
            director.PersistCampaign=save=>throw new InvalidOperationException("Presentation capture must not write campaign state.");
            director.BeginBattle();director.StopAllCoroutines();var alphen=director.Session.Units.Single(u=>u.Team==Team.Player&&u.Data.Id=="alphen");if(alphen.Equipment.Any(e=>e!=null)||!director.SelectPlayerUnit(alphen))throw new InvalidOperationException("Expected selected Alphen with basic weapon.");
            for(int i=0;i<4;i++)await Awaitable.NextFrameAsync();
            if(director.enabled||director.Session==null||director.Session.CampaignStage!=11||!director.IsPlayerCommand||director.StoryActive)throw new InvalidOperationException("Expected isolated episode-six command state.");
            if(director.GetComponents<MonoBehaviour>().Any(c=>c.GetType().Name=="WindowDisplayController"))throw new InvalidOperationException("Production window autosave was created.");
            return new {storage=director.Profiles.Root,directorStartSuppressed=!director.enabled,campaignStage=director.Session.CampaignStage,allies=director.Session.Units.Count(u=>u.Team==Team.Player),frame=Time.frameCount,seconds=Time.time};
        }
        finally{SceneManager.sceneLoaded-=callback;}
    }
}
