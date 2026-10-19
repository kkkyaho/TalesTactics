using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
namespace TalesTactics.Tests
{
    public class ContextHintPreferencesTests
    {
        string root;
        [SetUp] public void Setup(){root=Path.Combine(Application.temporaryCachePath,"ContextHintSettingsTests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);}
        [TearDown] public void Cleanup(){Directory.Delete(root,true);}
        [Test] public void ContextHintsOldSettingsDefaultUnseenAndNewFlagsRoundTrip()
        {
            var path=Path.Combine(root,"settings.json");
            File.WriteAllText(path,"{\"Version\":1,\"Music\":0.3,\"Effects\":0.2,\"Slot\":2}");
            var store=new PreferenceFile(path);var old=store.Load();Assert.That(store.Writable,Is.True);Assert.That(old.SeenContextHints,Is.Zero);
            old.SeenContextHints=(int)(ContextHintKind.MapPan|ContextHintKind.FacingConfirm);
            Assert.That(store.Save(old),Is.True);var read=new PreferenceFile(path).Load();
            Assert.That(read.SeenContextHints,Is.EqualTo(5));Assert.That(read.Slot,Is.EqualTo(2));Assert.That(read.Music,Is.EqualTo(.3f));Assert.That(read.Effects,Is.EqualTo(.2f));Assert.That(read.Version,Is.EqualTo(1));
        }
        [TestCase(-1)][TestCase(8)][TestCase(15)] public void ContextHintsUnknownFlagsProtectExistingSettings(int flags)
        {
            var path=Path.Combine(root,"settings.json");var original=JsonUtility.ToJson(new PlayerPreferences{SeenContextHints=flags});File.WriteAllText(path,original);
            var store=new PreferenceFile(path);store.Load();Assert.That(store.Writable,Is.False);
            Assert.That(store.Save(new PlayerPreferences()),Is.False);Assert.That(File.ReadAllText(path),Is.EqualTo(original));
            Assert.Throws<InvalidDataException>(()=>new PlayerPreferences{SeenContextHints=flags}.Validate());
        }
        [Test] public void ContextHintsAllKnownCombinationsAreValidWithoutCampaignSchemaChanges()
        {
            for(int flags=0;flags<=PlayerPreferences.KnownContextHints;flags++)Assert.DoesNotThrow(()=>new PlayerPreferences{SeenContextHints=flags}.Validate());
            var campaign=JsonUtility.ToJson(new CampaignSave());Assert.That(campaign,Does.Not.Contain("SeenContextHints"));
        }
    }
}