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
        static readonly string[] OptionalLocalMusicIds={BattleMusicProgram.Title,BattleMusicProgram.Intermission,BattleMusicProgram.Spirit,BattleMusicProgram.Sky,BattleMusicProgram.GoodWine,BattleMusicProgram.Decisive};

        [UnityTest] public IEnumerator LocalMusicOptionalTracksOrFallbacksActuallyPlayAndKeepMusicAndEffectsIndependent()
        {
            bool supplied=LocalMusicConfiguration();
            var audio=director.Audio;var source=audio.GetComponent<AudioSource>();
            audio.SetVolumes(.17f,.31f);string random=JsonUtility.ToJson(Random.state);
            string[] fallbackIds={"story","story","story","battle","boss","story"};
            for(int i=0;i<OptionalLocalMusicIds.Length;i++)
            {
                string id=supplied?OptionalLocalMusicIds[i]:fallbackIds[i];
                if(supplied)audio.Play(id);
                else
                {
                    // Exercise production routing, never direct Play on an absent local ID.
                    switch(i)
                    {
                        case 0:audio.PlayTitle();break;
                        case 1:audio.PlayIntermission();break;
                        case 2:audio.PlayStory(CrossoverCampaign.FirstStage+9,false);break;
                        case 3:audio.PlayBattle(false);break;
                        case 4:audio.PlayBattle(true);break;
                        default:audio.PlayStory(CrossoverCampaign.LastStage,false);break;
                    }
                }
                yield return LocalMusicAdvances(id);
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
            LocalMusicConfiguration();
            TitleSetup();var prefs=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(director.Preferences));prefs.Music=.19f;prefs.Effects=.23f;
            Assert.That(director.SavePreferences(prefs,false),Is.True);
            string title=LocalMusicExpected(BattleMusicProgram.Title,"story"),intermission=LocalMusicExpected(BattleMusicProgram.Intermission,"story");
            yield return LocalMusicAdvances(title);
            Assert.That(director.StartTitleGame(0),Is.True);yield return LocalMusicAdvances(intermission);
            var source=director.Audio.GetComponent<AudioSource>();int sample=source.timeSamples;
            director.Hud.ShowIntermissionHub();yield return null;
            Assert.That(source.timeSamples,Is.GreaterThanOrEqualTo(sample),"Refreshing a hub tab must not restart its song.");
            director.RequestBattle();Assert.That(director.StoryActive,Is.True);yield return LocalMusicAdvances("story");
            director.FinishStory();Assert.That(director.Session,Is.Not.Null);yield return LocalMusicAdvances("battle");
            director.Restart();yield return LocalMusicAdvances(intermission);
            director.ReturnToTitle();Assert.That(director.Hud.TitleOpen,Is.True);yield return LocalMusicAdvances(title);
            director.ConfigureStorage(testStorage);Assert.That(director.Preferences.Music,Is.EqualTo(.19f).Within(.001));
            Assert.That(source.volume,Is.EqualTo(.19f).Within(.001));Assert.That(director.Audio.EffectsVolume,Is.EqualTo(.23f).Within(.001));
        }

        [UnityTest] public IEnumerator LocalMusicBattleSelectionUsesRunningStageAndThemeRestoresStreamedSong()
        {
            bool supplied=LocalMusicConfiguration();
            director.Hud.HideTitle();director.Catalog.CrossoverReady=true;director.TrainingMode=false;
            for(int episode=1;episode<=20;episode++)director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(episode));
            int[] stages={7,8,15,25};string[] preferred={BattleMusicProgram.Sky,BattleMusicProgram.GoodWine,BattleMusicProgram.Spirit,BattleMusicProgram.Decisive};
            for(int i=0;i<stages.Length;i++)
            {
                string id=LocalMusicExpected(preferred[i],CampaignStages.Get(stages[i]).BossMusic?"boss":"battle");
                director.SelectedStage=stages[i];director.BeginBattle();Assert.That(director.Session,Is.Not.Null);
                yield return LocalMusicAdvances(id);
                // UI selection may change independently; resume/rewind routing uses the live session.
                director.SelectedStage=CrossoverCampaign.FirstStage;
                director.Audio.PlayBattle(CampaignStages.Get(stages[i]).BossMusic);Assert.That(director.Audio.CurrentMusicId,Is.EqualTo(id));
                var source=director.Audio.GetComponent<AudioSource>();var prior=source.clip;int sample=source.timeSamples;
                director.Audio.BeginTheme("cless.theme");yield return LocalMusicAdvances("cless.theme");
                director.Audio.EndTheme();Assert.That(source.clip,Is.SameAs(prior));
                Assert.That(source.timeSamples,Is.InRange(sample,sample+44100),"Ultimate theme returns to the interrupted streamed position.");
                yield return LocalMusicAdvances(id);
                director.Restart();yield return null;
            }
            if(supplied)director.Audio.Play(BattleMusicProgram.Spirit);else director.Audio.PlayBattle(true);
            director.Audio.BeginTheme("cless.theme");director.Audio.PlayIntermission();director.Audio.EndTheme();
            yield return LocalMusicAdvances(LocalMusicExpected(BattleMusicProgram.Intermission,"story"));
        }

        [UnityTest] public IEnumerator LocalMusicRouteCancellationReturnsMainAndPersonalPreparationToIntermissionWithoutSaving()
        {
            LocalMusicConfiguration();
            director.Hud.HideTitle();director.Catalog.CrossoverReady=true;director.TrainingMode=false;
            for(int episode=1;episode<=12;episode++)director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(episode));
            director.Campaign.StoryProgress.Add(PersonalScenarios.Flag(PersonalScenarios.FirstStage));
            // Prepare existing roster records before inspecting read-only navigation.
            foreach(var character in director.Catalog.Characters)director.Campaign.Get(character.Id);
            foreach(int stage in new[]{CrossoverCampaign.FirstStage+12,PersonalScenarios.FirstStage+1})
            {
                director.SelectedStage=stage;Assert.That(director.SavePreparation(),Is.True);
                director.RequestBattle();Assert.That(director.StoryActive,Is.True,"Story did not open for "+stage);
                yield return LocalMusicAdvances(LocalMusicExpected(BattleMusicProgram.Story(stage,false),"story"));
                director.FinishStory();Assert.That(director.StoryActive,Is.False);Assert.That(director.Session,Is.Null);
                yield return null;
                string campaign=JsonUtility.ToJson(director.Campaign),random=JsonUtility.ToJson(Random.state);
                var files=Directory.GetFiles(testStorage,"*",SearchOption.AllDirectories).ToDictionary(path=>path,File.ReadAllBytes);
                Click("돌아가기");yield return null;yield return LocalMusicAdvances(LocalMusicExpected(BattleMusicProgram.Intermission,"story"));
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

        bool LocalMusicConfiguration()
        {
            var audio=director.Audio;audio.PersonalMusicEnabled=false;
            Assert.That(audio.Library,Is.Not.Null);Assert.That(audio.Library.Entries,Is.Not.Null);
            Assert.That(audio.Library.Entries.All(e=>e!=null),Is.True);
            Assert.That(audio.Library.Entries.Select(e=>e.Id).Distinct().Count(),Is.EqualTo(audio.Library.Entries.Length));
            var entries=audio.Library.Entries.Where(e=>OptionalLocalMusicIds.Contains(e.Id)).ToArray();
            Assert.That(entries.Length,Is.EqualTo(0).Or.EqualTo(OptionalLocalMusicIds.Length),"Local music must be absent or a complete six-track set.");
            foreach(var entry in entries)Assert.That(entry.Clip,Is.Not.Null,"Partial local music connection: "+entry.Id);
            foreach(var id in new[]{"story","battle","boss"})
                Assert.That(audio.Library.Entries.Single(e=>e.Id==id).Clip,Is.Not.Null,"Required original fallback: "+id);
            return entries.Length==OptionalLocalMusicIds.Length;
        }

        string LocalMusicExpected(string preferred,string fallback)
            =>OptionalLocalMusicIds.Contains(preferred)&&!director.Audio.Library.Entries.Any(e=>e.Id==preferred)?fallback:preferred;

        IEnumerator LocalMusicAdvances(string id)
        {
            var audio=director.Audio;var source=audio.GetComponent<AudioSource>();var expected=audio.Library.Entries.Single(e=>e.Id==id).Clip;
            Assert.That(expected,Is.Not.Null,id);
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
