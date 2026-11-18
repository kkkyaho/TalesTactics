using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using TalesTactics.Editor;

namespace TalesTactics.Tests
{
    public sealed class LocalMusicTests
    {
        [Test] public void OptionalMusicKeepsEveryOriginalTrackAndValidatesCompleteLocalStreamingConfiguration()
        {
            var library=AssetDatabase.LoadAssetAtPath<AudioLibrary>("Assets/TalesTactics/Content/AudioLibrary.asset");
            bool supplied=AssertOptionalMusicConfiguration(library);
            var originalIds=new[]{"battle","boss","story","victory","cless.theme","mint.theme","farah.theme","tear.theme","jade.theme","natalia.theme","velvet.theme","alphen.theme","shionne.theme","kisara.theme"};
            foreach(var id in originalIds)
            {
                var clip=library.Entries.Single(e=>e.Id==id).Clip;
                Assert.That(clip,Is.Not.Null,id);
                Assert.That(AssetDatabase.GetAssetPath(clip),Is.EqualTo("Assets/TalesTactics/Audio/Original/"+id+".wav"));
            }
            if(supplied)
            {
                for(int i=0;i<LocalMusicImport.Ids.Length;i++)
                {
                    var entry=library.Entries.Single(e=>e.Id==LocalMusicImport.Ids[i]);
                    string path=LocalMusicImport.Root+LocalMusicImport.Files[i];
                    Assert.That(AssetDatabase.GetAssetPath(entry.Clip),Is.EqualTo(path));
                    Assert.That(entry.Clip.channels,Is.EqualTo(2));Assert.That(entry.Clip.frequency,Is.EqualTo(44100));
                    Assert.That(entry.Clip.length,Is.GreaterThan(80));Assert.That(entry.Clip.loadType,Is.EqualTo(AudioClipLoadType.Streaming));
                    var importer=(AudioImporter)AssetImporter.GetAtPath(path);
                    Assert.That(importer.loadInBackground,Is.True);Assert.That(importer.forceToMono,Is.False);
                    Assert.That(importer.defaultSampleSettings.preloadAudioData,Is.True);
                    Assert.That(importer.defaultSampleSettings.compressionFormat,Is.EqualTo(AudioCompressionFormat.Vorbis));
                    Assert.That(importer.defaultSampleSettings.sampleRateSetting,Is.EqualTo(AudioSampleRateSetting.PreserveSampleRate));
                }
            }
            else
            {
                foreach(var id in new[]{"story","battle","boss"})
                    Assert.That(library.Entries.Single(e=>e.Id==id).Clip,Is.Not.Null,"Source-only fallback "+id);
                foreach(var id in LocalMusicImport.Ids)
                    Assert.That(library.Entries.Any(e=>e.Id==id),Is.False,"A source-only catalog must not retain a dangling local entry: "+id);
            }
        }

        [Test] public void CampaignMusicSeparatesTrainingOrdinaryBossFinaleAndPersonalStoriesWithoutRandomDraws()
        {
            string before=JsonUtility.ToJson(Random.state);
            Assert.That(BattleMusicProgram.Battle(-1,false),Is.EqualTo("battle"));
            Assert.That(BattleMusicProgram.Battle(CrossoverCampaign.FirstStage,false),Is.EqualTo("battle"));
            Assert.That(BattleMusicProgram.Battle(CrossoverCampaign.FirstStage+1,false),Is.EqualTo(BattleMusicProgram.Sky));
            Assert.That(BattleMusicProgram.Battle(CrossoverCampaign.FirstStage+2,false),Is.EqualTo(BattleMusicProgram.GoodWine));
            Assert.That(BattleMusicProgram.Battle(CrossoverCampaign.FirstStage+9,true),Is.EqualTo(BattleMusicProgram.Spirit));
            Assert.That(BattleMusicProgram.Battle(CrossoverCampaign.LastStage,true),Is.EqualTo(BattleMusicProgram.Decisive));
            Assert.That(BattleMusicProgram.Story(PersonalScenarios.FirstStage,false),Is.EqualTo("cless.theme"));
            Assert.That(BattleMusicProgram.Story(CrossoverCampaign.LastStage,true),Is.EqualTo("story"));
            var library=AssetDatabase.LoadAssetAtPath<AudioLibrary>("Assets/TalesTactics/Content/AudioLibrary.asset");
            bool supplied=AssertOptionalMusicConfiguration(library);
            var available=library.Entries.Where(e=>e.Clip!=null).Select(e=>e.Id).ToArray();
            for(int stage=0;stage<CampaignStages.Count;stage++)
            {
                bool boss=CampaignStages.Get(stage).BossMusic;
                string battle=BattleMusicProgram.Battle(stage,boss),intro=BattleMusicProgram.Story(stage,false),outro=BattleMusicProgram.Story(stage,true);
                Assert.That(available,Does.Contain(!supplied&&LocalMusicImport.Ids.Contains(battle)?(boss?"boss":"battle"):battle),"battle "+stage);
                Assert.That(available,Does.Contain(!supplied&&LocalMusicImport.Ids.Contains(intro)?"story":intro),"before "+stage);
                Assert.That(available,Does.Contain(!supplied&&LocalMusicImport.Ids.Contains(outro)?"story":outro),"after "+stage);
            }
            Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(before));
        }

        static bool AssertOptionalMusicConfiguration(AudioLibrary library)
        {
            Assert.That(library,Is.Not.Null);
            Assert.That(library.Entries,Is.Not.Null);
            Assert.That(library.Entries.All(e=>e!=null),Is.True);
            Assert.That(library.Entries.Select(e=>e.Id).Distinct().Count(),Is.EqualTo(library.Entries.Length));
            int files=LocalMusicImport.Files.Count(file=>File.Exists(LocalMusicImport.Root+file));
            var entries=library.Entries.Where(e=>LocalMusicImport.Ids.Contains(e.Id)).ToArray();
            Assert.That(files,Is.EqualTo(0).Or.EqualTo(LocalMusicImport.Files.Length),"Local music is optional only as a complete six-track set.");
            Assert.That(entries.Length,Is.EqualTo(files),"Every supplied file must have exactly one connected entry; source-only catalogs have none.");
            foreach(var entry in entries)Assert.That(entry.Clip,Is.Not.Null,"Missing supplied clip: "+entry.Id);
            return files==LocalMusicImport.Files.Length;
        }
    }
}
