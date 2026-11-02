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
        [UnityTest] public IEnumerator RecruitmentFutureCompanionsStayHiddenUntilTheirIntroductionBattle()
        {
            PreparationGuidanceEpisode(2);director.Campaign.Get("cless").Level=4;director.Campaign.Get("mint").Level=7;
            director.Hud.ShowIntermissionHub();yield return null;
            string before=JsonUtility.ToJson(director.Campaign);
            foreach(string menu in new[]{"출전 편성","장비 관리","성장 · 승급"})
            {
                Click(menu);yield return null;
                foreach(string id in new[]{"alphen","shionne","farah"})
                {
                    var character=director.Catalog.Characters.Single(c=>c.Id==id);
                    Assert.That(director.IsJoined(character),Is.False);
                    Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains(character.DisplayName.Split(' ')[0])),Is.False);
                }
                director.Hud.ShowIntermissionHub();yield return null;
            }
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));
            director.RequestBattle();Assert.That(director.StoryActive,Is.True);Assert.That(director.IsJoined(director.Catalog.Characters.Single(c=>c.Id=="alphen")),Is.False);
            director.FinishStory();yield return null;
            Assert.That(director.Session,Is.Not.Null);
            foreach(string id in new[]{"alphen","shionne"})
            {
                var unit=director.Session.Units.Single(u=>u.Team==Team.Player&&u.Data.Id==id);
                Assert.That(unit.Level,Is.EqualTo(5));Assert.That(director.IsJoined(unit.Data),Is.True);
            }
            var disk=new CampaignFile(director.Profiles.PathFor(0)).Load();
            Assert.That(CampaignRecruitment.Joined(disk,"alphen"),Is.True);Assert.That(disk.Get("shionne").Level,Is.EqualTo(5));
            director.Restart();yield return null;director.Hud.ShowDeployment();yield return null;
            Assert.That(director.Hud.GetComponentInChildren<FormationRosterScroll>().Content.childCount,Is.EqualTo(4));
        }
        [UnityTest] public IEnumerator RecruitmentSaveFailureBlocksBattleAndPreservesRosterAndProgress()
        {
            PreparationGuidanceEpisode(2);var party=director.Deployment.ToArray();string before=JsonUtility.ToJson(director.Campaign);
            int writes=0;director.PersistCampaign=_=>{writes++;return false;};director.RequestBattle();director.FinishStory();yield return null;
            Assert.That(director.Session,Is.Null);Assert.That(writes,Is.EqualTo(1));Assert.That(director.Deployment,Is.EqualTo(party));
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));Assert.That(director.Hud.IntermissionHubOpen,Is.True);
            Assert.That(director.SaveNotice,Does.Contain("합류 동료 저장 실패"));
        }
        [UnityTest] public IEnumerator RecruitmentFullPartyForcesDebutantsWithoutExceedingSixThenAllowsNormalFormation()
        {
            PreparationGuidanceEpisode(5);int[] chosen=new[]{"cless","mint","alphen","shionne","farah","jade"}
                .Select(id=>System.Array.FindIndex(director.Catalog.Characters,c=>c.Id==id)).ToArray();
            foreach(var c in director.Catalog.Characters.Where(director.IsJoined))director.Campaign.Get(c.Id).Level=8;
            director.Deployment.Clear();director.Deployment.AddRange(chosen);director.RequestBattle();director.FinishStory();yield return null;
            var party=director.Session.Units.Where(u=>u.Team==Team.Player).ToArray();Assert.That(party.Length,Is.EqualTo(6));
            foreach(string id in new[]{"velvet","kisara"})Assert.That(party.Single(u=>u.Data.Id==id).Level,Is.EqualTo(8));
            director.Restart();director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(5));director.SelectedStage=11;
            director.Deployment.Clear();director.Deployment.AddRange(chosen);director.BeginBattle();yield return null;
            Assert.That(director.Session.Units.Where(u=>u.Team==Team.Player).Select(u=>u.Data.Id),Is.EqualTo(chosen.Select(i=>director.Catalog.Characters[i].Id)));
        }
        [UnityTest] public IEnumerator RecruitmentLegacySuspensionKeepsDebutGrowthAndJoinAfterConsumingCheckpoint()
        {
            PreparationGuidanceEpisode(2);director.Campaign.Get("cless").Level=4;director.Campaign.Get("mint").Level=7;
            director.BeginBattle();yield return null;Assert.That(director.SuspendBattle(),Is.True);yield return null;
            // Model a pre-change save: the real checkpoint has recruits, but no new recruitment flag.
            director.Campaign.StoryProgress.Remove(CampaignRecruitment.Flag(2));
            Assert.That(director.SavePreparation(),Is.True);
            var checkpoint=director.Campaign.SuspendedBattle;string units=JsonUtility.ToJson(checkpoint);
            Assert.That(director.ResumeBattle(),Is.True);yield return null;
            Assert.That(director.Session.Units.Single(u=>u.Team==Team.Player&&u.Data.Id=="alphen").Level,Is.EqualTo(5));
            Assert.That(JsonUtility.ToJson(checkpoint),Is.EqualTo(units));Assert.That(director.Campaign.SuspendedBattle,Is.Null);
            Assert.That(director.Campaign.StoryProgress.Count(f=>f==CampaignRecruitment.Flag(2)),Is.EqualTo(1));
            director.Restart();yield return null;
            var disk=new CampaignFile(director.Profiles.PathFor(0)).Load();Assert.That(CampaignRecruitment.Joined(disk,"alphen"),Is.True);
            director.Campaign.Get("cless").Level=40;director.BeginBattle();yield return null;
            Assert.That(director.Session.Units.Single(u=>u.Team==Team.Player&&u.Data.Id=="alphen").Level,Is.EqualTo(5));
            Assert.That(director.Campaign.StoryProgress.Count(f=>f==CampaignRecruitment.Flag(2)),Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator RecruitmentLegacyCheckpointPreparationSavePreservesJoinAndRollsBackFailedWrite()
        {
            PreparationGuidanceEpisode(2);director.Campaign.Get("cless").Level=4;director.Campaign.Get("mint").Level=7;
            director.Campaign.Get("alphen").Level=18;director.Campaign.Get("alphen").EXP=51;director.Campaign.Get("shionne").Level=13;
            director.BeginBattle();yield return null;Assert.That(director.SuspendBattle(),Is.True);yield return null;
            director.Campaign.StoryProgress.Remove(CampaignRecruitment.Flag(2));Assert.That(director.SavePreparation(),Is.True);
            director.Hud.ShowTitle();Assert.That(director.LoadTitleGame(0,false),Is.True);yield return null;
            Assert.That(director.Session,Is.Null,"Loading into preparation must not resume the checkpoint.");
            var alphen=director.Catalog.Characters.Single(c=>c.Id=="alphen");
            var shionne=director.Catalog.Characters.Single(c=>c.Id=="shionne");
            Assert.That(director.IsJoined(alphen)&&director.IsJoined(shionne),Is.True);
            Assert.That(director.Campaign.StoryProgress,Does.Not.Contain(CampaignRecruitment.Flag(2)));
            var checkpoint=director.Campaign.SuspendedBattle;string checkpointBefore=JsonUtility.ToJson(checkpoint);
            string before=JsonUtility.ToJson(director.Campaign),path=director.Profiles.PathFor(0),diskBefore=File.ReadAllText(path);
            string shionneBefore=JsonUtility.ToJson(director.Campaign.Get("shionne"));
            // A real temporary-file write failure exercises the shared persistence path.
            Directory.CreateDirectory(path+".tmp");
            try
            {
                LogAssert.Expect(LogType.Warning,new System.Text.RegularExpressions.Regex("Campaign save failed:"));
                Assert.That(TacticalDevelopment.SaveTrait(director.Campaign,alphen,TacticalTrait.Focus,director.PersistCampaign),Is.False);
                Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));Assert.That(File.ReadAllText(path),Is.EqualTo(diskBefore));
                Assert.That(director.Campaign.SuspendedBattle,Is.SameAs(checkpoint));Assert.That(director.IsJoined(alphen)&&director.IsJoined(shionne),Is.True);
            }
            finally{Directory.Delete(path+".tmp");}
            Assert.That(TacticalDevelopment.SaveTrait(director.Campaign,alphen,TacticalTrait.Focus,director.PersistCampaign),Is.True);
            Assert.That(director.Session,Is.Null);Assert.That(director.Campaign.SuspendedBattle,Is.Null);Assert.That(director.Campaign.HasSuspendedBattle,Is.False);
            Assert.That(JsonUtility.ToJson(checkpoint),Is.EqualTo(checkpointBefore));
            Assert.That(director.Campaign.StoryProgress.Count(f=>f==CampaignRecruitment.Flag(2)),Is.EqualTo(1));
            Assert.That(director.IsJoined(alphen)&&director.IsJoined(shionne),Is.True);
            director.Hud.ShowTitle();Assert.That(director.LoadTitleGame(0,false),Is.True);yield return null;
            Assert.That(director.IsJoined(alphen)&&director.IsJoined(shionne),Is.True);
            Assert.That(director.Campaign.Get("alphen").Trait,Is.EqualTo(TacticalTrait.Focus));
            Assert.That(director.Campaign.Get("alphen").Level,Is.EqualTo(18));Assert.That(director.Campaign.Get("alphen").EXP,Is.EqualTo(51));
            Assert.That(JsonUtility.ToJson(director.Campaign.Get("shionne")),Is.EqualTo(shionneBefore));
            director.Hud.ShowDeployment();yield return null;
            Assert.That(director.Hud.GetComponentInChildren<FormationRosterScroll>().Content.childCount,Is.EqualTo(4));
        }
    }
}
