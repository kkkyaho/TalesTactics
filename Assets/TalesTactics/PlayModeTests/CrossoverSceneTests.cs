using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator RetiredCampaignCannotStartReplayOrResumeAndOldProgressIsPreserved()
        {
            director.Catalog.CrossoverReady=true;
            director.ConfigureStorage(testStorage);Assert.That(director.SelectedStage,Is.EqualTo(CrossoverCampaign.FirstStage));
            director.TrainingMode=false;director.SelectedStage=0;
            director.Campaign.Gold=1234;director.Campaign.Get("cless").Level=12;director.Campaign.StoryProgress.Add("chapter2");
            director.Campaign.SuspendedBattle=new BattleCheckpoint{Stage=0};
            string before=JsonUtility.ToJson(director.Campaign);
            for(int stage=0;stage<6;stage++)
            {
                director.SelectedStage=stage;director.RequestBattle();director.ReplayStory(false);director.BeginBattle();
                Assert.That(director.StoryActive,Is.False);Assert.That(director.Session,Is.Null);
            }
            Assert.That(director.CanResumeCampaign,Is.False);Assert.That(director.ResumeBattle(),Is.False);
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));
            director.Campaign.SuspendedBattle=null;director.SelectedStage=0;Assert.That(director.SavePreparation(),Is.True);
            director.ConfigureStorage(testStorage);Assert.That(director.SelectedStage,Is.EqualTo(CrossoverCampaign.FirstStage));
            Assert.That(director.Campaign.Gold,Is.EqualTo(1234));Assert.That(director.Campaign.Get("cless").Level,Is.EqualTo(12));
            Assert.That(director.Campaign.StoryProgress,Does.Contain("chapter2"));
            director.Hud.ShowChapterPage(0);yield return null;
            Assert.That(director.Hud.IntermissionHubOpen,Is.True);Assert.That(director.SelectedStage,Is.EqualTo(CrossoverCampaign.FirstStage));
            Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("이전 6장 다시 보기")||t.text.Contains("유적의 경계")),Is.False);
            director.RequestBattle();Assert.That(director.StoryActive,Is.True);director.FinishStory();yield return null;
            Assert.That(director.Session.CampaignStage,Is.EqualTo(CrossoverCampaign.FirstStage));
        }
        [UnityTest] public IEnumerator RescueGroupsDisappearAtSafetyAndEvacueesFollowTheirEscort()
        {
            var original=director.Catalog;var review=Object.Instantiate(original);review.CrossoverReady=true;director.Catalog=review;
            try
            {
                director.TrainingMode=false;for(int n=1;n<=20;n++)director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(n));
                foreach(int stage in new[]{12,21})
                {
                    director.SelectedStage=stage;director.BeginBattle();director.StopAllCoroutines();yield return null;
                    var view=Object.FindAnyObjectByType<CrossoverCivilianView>();Assert.That(view,Is.Not.Null);
                    Assert.That(view.GetComponentsInChildren<SpriteRenderer>().Length,Is.EqualTo(3));
                    director.Session.Crossover.State.Nodes=1;yield return new WaitForSeconds(2);
                    var rescued=view.transform.Find("Civilian group 0");
                    Assert.That(rescued.gameObject.activeSelf,Is.EqualTo(stage==21));
                    if(stage==21)
                    {
                        var lead=director.Session.Units.First(u=>u.Team==Team.Player);
                        Assert.That(Vector3.Distance(rescued.position,director.Session.Grid[lead.Position].WorldPosition(director.Session.Rules.TileHeight)),Is.LessThan(1.5f));
                    }
                    director.Restart();yield return null;
                }
            }
            finally{director.Restart();director.Catalog=original;Object.Destroy(review);}
        }
        void SatisfyObjectiveFixture()
        {
            var b=director.Session;
            // Fixture transitions cover scene/reward wiring; actual skills and balance have separate checks.
            foreach(var unit in b.Units.Where(u=>u.Team==Team.Enemy).ToArray())
                for(int i=0;i<3&&unit.Alive;i++){unit.Damage(99999,b.Grid);unit.BeginTurn();unit.Acted=true;unit.EndTurn();}
            b.Crossover.Update();
            if(CrossoverCampaign.IsMain(b.CampaignStage)&&CrossoverCampaign.Episode(b.CampaignStage).Number==11)
                foreach(var unit in b.Units.Where(u=>u.Team==Team.Enemy&&u.Alive).ToArray())unit.Damage(99999,b.Grid);
            b.Crossover.State.Nodes=7;
            if(PersonalScenarios.Contains(b.CampaignStage)&&PersonalScenarios.Tier(b.CampaignStage)==2)
            {b.Crossover.State.TrialProgress=PersonalScenarios.Arc(b.CampaignStage).Owner=="cless"?7:1;b.Crossover.State.TrialActions=3;}
            var hero=PersonalScenarios.Contains(b.CampaignStage)?b.Units.First(u=>u.Team==Team.Player&&u.Data.Id==PersonalScenarios.Arc(b.CampaignStage).Owner):b.Units.First(u=>u.Team==Team.Player&&u.Alive&&!u.CrossoverReserve);
            if(b.Crossover.UsesContract)b.Grid.Place(hero,CrossoverBattle.ContractSeal);else if(b.Crossover.UsesExit)b.Grid.Place(hero,CrossoverBattle.Exit);
            b.Crossover.Update();Assert.That(b.Result,Is.EqualTo(BattleResult.Victory),"Stage "+b.CampaignStage);
            director.SetState(new BattleEndState(director));
        }
        [UnityTest] public IEnumerator TwentyEpisodeStoriesScenesAndRewardsProgressThroughIndependentSave()
        {
            var original=director.Catalog;var review=Object.Instantiate(original);review.CrossoverReady=true;director.Catalog=review;
            try
            {
                director.TrainingMode=false;
                for(int stage=6;stage<=25;stage++)
                {
                    director.SelectedStage=stage;director.Deployment.Clear();director.Deployment.AddRange(Enumerable.Range(0,review.Characters.Length).Where(director.CanDeploy).Take(6));
                    director.RequestBattle();Assert.That(director.StoryActive,Is.True);director.FinishStory();
                    if(stage==18)director.ChooseScenarioRoute(1);
                    yield return null;Assert.That(director.Session,Is.Not.Null,"Episode "+(stage-5));
                    Assert.That(director.Session.Units.Where(u=>u.Team==Team.Player).All(u=>CrossoverCampaign.Joined(u.Data.Id,stage-5)),Is.True);
                    SatisfyObjectiveFixture();yield return null;
                    Assert.That(director.RewardPending,Is.False);Assert.That(director.Campaign.StoryProgress,Has.Member(CampaignStages.Id(stage)));
                    director.ReadEnding();Assert.That(director.StoryActive,Is.True);director.FinishStory();
                    if(stage<25){Assert.That(director.CanPrepareNext,Is.True);director.PrepareNextBattle();}else Assert.That(director.CanPrepareNext,Is.False);
                    yield return null;
                }
                Assert.That(director.Campaign.StoryProgress.Count(s=>s.StartsWith("crossover.chapter")&&!s.Contains(".route.")),Is.EqualTo(20));
                Assert.That(director.Campaign.StoryProgress,Has.Member("crossover.chapter13.route.right"));
            }
            finally{director.Restart();director.Catalog=original;Object.Destroy(review);}
        }
        [UnityTest] public IEnumerator ThirtyPersonalStoriesChoicesTrialsAndWeaponsRemainReplaySafe()
        {
            var original=director.Catalog;var review=Object.Instantiate(original);review.CrossoverReady=true;director.Catalog=review;
            try
            {
                director.TrainingMode=false;for(int n=1;n<=20;n++)director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(n));
                for(int stage=26;stage<56;stage++)
                {
                    int tier=PersonalScenarios.Tier(stage);string owner=PersonalScenarios.Arc(stage).Owner;
                    director.SelectedStage=stage;director.RequestBattle();Assert.That(director.StoryActive,Is.True,stage.ToString());director.FinishStory();
                    if(tier==0)
                    {Assert.That(director.Session,Is.Null);Assert.That(director.StoryActive,Is.True);director.FinishStory();}
                    else
                    {
                        if(tier==1)director.ChooseScenarioRoute((stage/3)%2);
                        yield return null;Assert.That(director.Session.Units.Any(u=>u.Data.Id==owner&&u.Team==Team.Player),Is.True);
                        SatisfyObjectiveFixture();yield return null;director.Restart();
                    }
                    Assert.That(director.Campaign.StoryProgress,Has.Member(PersonalScenarios.Flag(stage)));
                    string saved=JsonUtility.ToJson(director.Campaign);director.SelectedStage=stage;director.RequestBattle();Assert.That(director.StoryActive,Is.True);Assert.That(director.Session,Is.Null);
                    director.FinishStory();Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(saved));yield return null;
                }
                foreach(var arc in PersonalScenarios.Arcs)Assert.That(CampaignInventory.Owned(director.Campaign,arc.WeaponId),Is.EqualTo(1));
            }
            finally{director.Restart();director.Catalog=original;Object.Destroy(review);}
        }
        [UnityTest] public IEnumerator CrossoverBoardSignalsAndSplitFrontsSurviveRetry()
        {
            var original=director.Catalog;var review=Object.Instantiate(original);review.CrossoverReady=true;director.Catalog=review;
            try
            {
                director.TrainingMode=false;for(int n=1;n<20;n++)director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(n));
                director.SelectedStage=16;director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1,2,3});director.BeginBattle();yield return null;
                Assert.That(director.Session.Units.Count(u=>u.Team==Team.Player&&u.CrossoverReserve),Is.EqualTo(2));
                Assert.That(director.RetryOpening(),Is.True);yield return null;
                Assert.That(director.Session.Units.Count(u=>u.Team==Team.Player&&u.CrossoverReserve),Is.EqualTo(2));
                Assert.That(director.Session.Scheduler.Preview(director.Session.Units).All(u=>!u.CrossoverReserve),Is.True);
                director.Restart();director.SelectedStage=28;director.Campaign.StoryProgress.Add(PersonalScenarios.Flag(26));director.Campaign.StoryProgress.Add(PersonalScenarios.Flag(27));director.BeginBattle();yield return null;
                Assert.That(director.Session.Crossover.UsesContract,Is.True);Assert.That(director.Session.Crossover.UsesExit,Is.False);
                Assert.That(director.Board.GetComponentsInChildren<Transform>().Any(t=>t.name.Contains("Origin contract seal")),Is.True);
            }
            finally{director.Restart();director.Catalog=original;Object.Destroy(review);}
        }
    }
}
