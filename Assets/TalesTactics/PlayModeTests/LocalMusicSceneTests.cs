using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator LocalMusicAllSixSuppliedTracksActuallyPlayAndKeepMusicAndEffectsIndependent()
        {
            var audio=director.Audio;var source=audio.GetComponent<AudioSource>();
            audio.SetVolumes(.17f,.31f);string random=JsonUtility.ToJson(Random.state);
            foreach(var id in new[]{BattleMusicProgram.Title,BattleMusicProgram.Intermission,BattleMusicProgram.Spirit,BattleMusicProgram.Sky,BattleMusicProgram.GoodWine,BattleMusicProgram.Decisive})
            {
                audio.Play(id);yield return LocalMusicAdvances(id);
                Assert.That(source.loop,Is.True,id);Assert.That(source.volume,Is.EqualTo(.17f).Within(.001));
                Assert.That(source.spatialBlend,Is.Zero);
                var playing=source.clip;audio.PlayEffect("hit");yield return null;
                Assert.That(source.clip,Is.SameAs(playing));Assert.That(audio.GetComponents<AudioSource>().Single(s=>s!=source).volume,Is.EqualTo(.31f).Within(.001));
                audio.SetVolumes(0,.31f);Assert.That(source.volume,Is.Zero);Assert.That(source.isPlaying,Is.True);
                audio.SetVolumes(.17f,.31f);
            }
            Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random));
            audio.StopAll();yield return null;
            Assert.That(audio.GetComponents<AudioSource>().All(s=>!s.isPlaying),Is.True);
        }

        [UnityTest] public IEnumerator LocalMusicTitleHubStoryBattleAndReturnFollowRealScreenFlowAndSavedVolume()
        {
            TitleSetup();var prefs=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(director.Preferences));prefs.Music=.19f;prefs.Effects=.23f;
            Assert.That(director.SavePreferences(prefs,false),Is.True);
            yield return LocalMusicAdvances(BattleMusicProgram.Title);
            Assert.That(director.StartTitleGame(0),Is.True);yield return LocalMusicAdvances(BattleMusicProgram.Intermission);
            var source=director.Audio.GetComponent<AudioSource>();int sample=source.timeSamples;
            director.Hud.ShowIntermissionHub();yield return null;
            Assert.That(source.timeSamples,Is.GreaterThanOrEqualTo(sample),"Refreshing a hub tab must not restart its song.");
            director.RequestBattle();Assert.That(director.StoryActive,Is.True);yield return LocalMusicAdvances("story");
            director.FinishStory();Assert.That(director.Session,Is.Not.Null);yield return LocalMusicAdvances("battle");
            director.Restart();yield return LocalMusicAdvances(BattleMusicProgram.Intermission);
            director.ReturnToTitle();Assert.That(director.Hud.TitleOpen,Is.True);yield return LocalMusicAdvances(BattleMusicProgram.Title);
            director.ConfigureStorage(testStorage);Assert.That(director.Preferences.Music,Is.EqualTo(.19f).Within(.001));
            Assert.That(source.volume,Is.EqualTo(.19f).Within(.001));Assert.That(director.Audio.EffectsVolume,Is.EqualTo(.23f).Within(.001));
        }

        [UnityTest] public IEnumerator LocalMusicBattleSelectionUsesRunningStageAndThemeRestoresStreamedSong()
        {
            director.Hud.HideTitle();director.Catalog.CrossoverReady=true;director.TrainingMode=false;
            for(int episode=1;episode<=20;episode++)director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(episode));
            int[] stages={7,8,15,25};string[] ids={BattleMusicProgram.Sky,BattleMusicProgram.GoodWine,BattleMusicProgram.Spirit,BattleMusicProgram.Decisive};
            for(int i=0;i<stages.Length;i++)
            {
                director.SelectedStage=stages[i];director.BeginBattle();Assert.That(director.Session,Is.Not.Null);
                yield return LocalMusicAdvances(ids[i]);
                // UI selection may change independently; resume/rewind routing uses the live session.
                director.SelectedStage=CrossoverCampaign.FirstStage;
                director.Audio.PlayBattle(CampaignStages.Get(stages[i]).BossMusic);Assert.That(director.Audio.CurrentMusicId,Is.EqualTo(ids[i]));
                var source=director.Audio.GetComponent<AudioSource>();var prior=source.clip;int sample=source.timeSamples;
                director.Audio.BeginTheme("cless.theme");yield return LocalMusicAdvances("cless.theme");
                director.Audio.EndTheme();Assert.That(source.clip,Is.SameAs(prior));
                Assert.That(source.timeSamples,Is.InRange(sample,sample+44100),"Ultimate theme returns to the interrupted streamed position.");
                yield return LocalMusicAdvances(ids[i]);
                director.Restart();yield return null;
            }
            director.Audio.Play(BattleMusicProgram.Spirit);director.Audio.BeginTheme("cless.theme");director.Audio.PlayIntermission();director.Audio.EndTheme();
            yield return LocalMusicAdvances(BattleMusicProgram.Intermission);
        }

        [UnityTest] public IEnumerator LocalMusicRouteCancellationReturnsMainAndPersonalPreparationToIntermissionWithoutSaving()
        {
            director.Hud.HideTitle();director.Catalog.CrossoverReady=true;director.TrainingMode=false;
            for(int episode=1;episode<=12;episode++)director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(episode));
            director.Campaign.StoryProgress.Add(PersonalScenarios.Flag(PersonalScenarios.FirstStage));
            // Prepare existing roster records before inspecting read-only navigation.
            foreach(var character in director.Catalog.Characters)director.Campaign.Get(character.Id);
            foreach(int stage in new[]{CrossoverCampaign.FirstStage+12,PersonalScenarios.FirstStage+1})
            {
                director.SelectedStage=stage;Assert.That(director.SavePreparation(),Is.True);
                director.RequestBattle();Assert.That(director.StoryActive,Is.True,"Story did not open for "+stage);
                yield return LocalMusicAdvances(BattleMusicProgram.Story(stage,false));
                director.FinishStory();Assert.That(director.StoryActive,Is.False);Assert.That(director.Session,Is.Null);
                yield return null;
                string campaign=JsonUtility.ToJson(director.Campaign),random=JsonUtility.ToJson(Random.state);
                var files=Directory.GetFiles(testStorage,"*",SearchOption.AllDirectories).ToDictionary(path=>path,File.ReadAllBytes);
                Click("돌아가기");yield return null;yield return LocalMusicAdvances(BattleMusicProgram.Intermission);
                Assert.That(director.SelectedStage,Is.EqualTo(stage));Assert.That(director.Session,Is.Null);Assert.That(director.StoryActive,Is.False);
                if(PersonalScenarios.Contains(stage))
                    Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(text=>text.text.Contains("세 개의 이야기")),Is.True);
                else Assert.That(director.Hud.GetComponentInChildren<FormationRosterScroll>(),Is.Not.Null);
                Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(campaign),"Cancel must not change campaign progress.");
                Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(random),"Cancel must not consume gameplay randomness.");
                Assert.That(Directory.GetFiles(testStorage,"*",SearchOption.AllDirectories),Is.EquivalentTo(files.Keys));
                foreach(var file in files)Assert.That(File.ReadAllBytes(file.Key),Is.EqualTo(file.Value),file.Key);
            }
        }

        IEnumerator LocalMusicAdvances(string id)
        {
            var audio=director.Audio;var source=audio.GetComponent<AudioSource>();var expected=audio.Library.Entries.Single(e=>e.Id==id).Clip;
            float until=Time.realtimeSinceStartup+8;
            while(Time.realtimeSinceStartup<until&&expected.loadState!=AudioDataLoadState.Failed&&(!source.isPlaying||source.timeSamples<=0))yield return null;
            Assert.That(audio.CurrentMusicId,Is.EqualTo(id));Assert.That(source.clip,Is.SameAs(expected),id);
            Assert.That(source.isPlaying,Is.True,id+" load="+expected.loadState+" dsp="+AudioSettings.dspTime);
            Assert.That(source.timeSamples,Is.GreaterThan(0),id);
            int sample=source.timeSamples;until=Time.realtimeSinceStartup+5;
            while(Time.realtimeSinceStartup<until&&source.timeSamples==sample)yield return null;
            Assert.That(source.timeSamples,Is.Not.EqualTo(sample),id+" must keep advancing after its single production Play call.");
        }
    }
}
