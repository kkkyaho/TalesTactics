using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public class PersistenceTests
    {
        string root;
        [SetUp] public void Setup(){root=Path.Combine(Application.temporaryCachePath,"SaveTests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);}
        [TearDown] public void Cleanup(){Directory.Delete(root,true);}
        [Test] public void ThreeSlotsRemainIndependentAndLegacyPathIsPreserved()
        {
            var profiles=new CampaignProfiles(root);Assert.That(profiles.PathFor(0),Is.EqualTo(Path.Combine(root,"campaign.json")));
            for(int i=0;i<3;i++){var save=profiles.Load(i);save.Gold=400+i;Assert.That(profiles.Store.Save(save),Is.True);}
            for(int i=0;i<3;i++)Assert.That(profiles.Load(i).Gold,Is.EqualTo(400+i));
            Assert.Throws<ArgumentOutOfRangeException>(()=>profiles.Load(3));
        }
        [Test] public void LegacyCampaignLoadsAndFutureCheckpointCannotBeOverwritten()
        {
            var path=Path.Combine(root,"campaign.json");File.WriteAllText(path,"{\"Version\":1,\"Characters\":[],\"StoryProgress\":[]}");
            var store=new CampaignFile(path);Assert.That(store.Load().Version,Is.EqualTo(2));Assert.That(store.Save(new CampaignSave()),Is.True);Assert.That(File.Exists(path+".v1.bak"),Is.True);
            var future=JsonUtility.ToJson(new CampaignSave{SuspendedBattle=new BattleCheckpoint{Version=99}});File.WriteAllText(path,future);
            store=new CampaignFile(path);store.Load();Assert.That(store.CanSave,Is.False);Assert.That(store.Save(new CampaignSave()),Is.False);Assert.That(File.ReadAllText(path),Is.EqualTo(future));
        }
        [Test] public void SettingsRoundTripAndFutureSettingsRemainUntouched()
        {
            var path=Path.Combine(root,"settings.json");var file=new PreferenceFile(path);file.Load();
            Assert.That(file.Save(new PlayerPreferences{Music=.6f,Effects=.2f,CursorSpeed=1.5f,CT=true,Utility=true,AutoTiming=true,Slot=2,Width=1366,Height=768,EnemySpeedMode=2,SkipEnemyAnimations=true}),Is.True);
            var read=new PreferenceFile(path).Load();Assert.That(read.Slot,Is.EqualTo(2));Assert.That(read.Music,Is.EqualTo(.6f));Assert.That(read.CursorSpeed,Is.EqualTo(1.5f));Assert.That(read.CT&&read.Utility&&read.AutoTiming,Is.True);
            Assert.That(read.EnemySpeedMode,Is.EqualTo(2));Assert.That(read.SkipEnemyAnimations,Is.True);
            File.WriteAllText(path,"{\"Version\":99}");file=new PreferenceFile(path);file.Load();Assert.That(file.Save(read),Is.False);Assert.That(File.ReadAllText(path),Is.EqualTo("{\"Version\":99}"));
        }
    }
}
