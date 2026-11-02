using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using TalesTactics.Editor;

namespace TalesTactics.Tests
{
    public sealed class LocalMusicTests
    {
        [Test] public void SuppliedMusicKeepsEveryOriginalTrackAndUsesStereoBackgroundStreaming()
        {
            var library=AssetDatabase.LoadAssetAtPath<AudioLibrary>("Assets/TalesTactics/Content/AudioLibrary.asset");
            var originalIds=new[]{"battle","boss","story","victory","cless.theme","mint.theme","farah.theme","tear.theme","jade.theme","natalia.theme","velvet.theme","alphen.theme","shionne.theme","kisara.theme"};
            Assert.That(library.Entries.Select(e=>e.Id).Distinct().Count(),Is.EqualTo(library.Entries.Length));
            foreach(var id in originalIds)
            {
                var clip=library.Entries.Single(e=>e.Id==id).Clip;
                Assert.That(clip,Is.Not.Null,id);
                Assert.That(AssetDatabase.GetAssetPath(clip),Is.EqualTo("Assets/TalesTactics/Audio/Original/"+id+".wav"));
            }
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
            var available=library.Entries.Where(e=>e.Clip!=null).Select(e=>e.Id).ToArray();
            for(int stage=0;stage<CampaignStages.Count;stage++)
            {
                Assert.That(available,Does.Contain(BattleMusicProgram.Battle(stage,CampaignStages.Get(stage).BossMusic)),"battle "+stage);
                Assert.That(available,Does.Contain(BattleMusicProgram.Story(stage,false)),"before "+stage);
                Assert.That(available,Does.Contain(BattleMusicProgram.Story(stage,true)),"after "+stage);
            }
            Assert.That(JsonUtility.ToJson(Random.state),Is.EqualTo(before));
        }
    }
}
