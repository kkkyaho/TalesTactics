using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator EndingResumesFromSavedLineAndFailedCompletionCannotAdvanceOrRewardTwice()
        {
            director.TrainingMode=false;director.SelectedStage=0;director.BeginBattle();yield return null;
            foreach(var enemy in director.Session.Units.Where(u=>u.Team==Team.Enemy))enemy.Damage(99999,director.Session.Grid);
            director.SetState(new BattleEndState(director));yield return null;
            int gold=director.Campaign.Gold;Assert.That(director.Campaign.HasPendingAfterStory,Is.True);
            Click("결과 계속");yield return null;director.AdvanceStory();yield return null;
            Assert.That(director.Campaign.PendingAfterStoryLine,Is.EqualTo(1));
            director.ConfigureStorage(testStorage);director.Restart();yield return null;
            Assert.That(director.StoryActive,Is.True);Assert.That(director.StoryIndex,Is.EqualTo(1));
            Assert.That(director.Campaign.Gold,Is.EqualTo(gold));
            director.BeginBattle();Assert.That(director.Session,Is.Null,"Pending ending blocks another battle");
            var persist=director.PersistCampaign;director.PersistCampaign=_=>false;director.FinishStory();yield return null;
            Assert.That(director.StoryActive,Is.True);Assert.That(director.Campaign.HasPendingAfterStory,Is.True);
            director.PersistCampaign=persist;while(director.StoryActive){director.AdvanceStory();yield return null;}
            Assert.That(director.Campaign.HasPendingAfterStory,Is.False);Assert.That(director.SelectedStage,Is.EqualTo(1));
            Assert.That(director.Campaign.Gold,Is.EqualTo(gold));Assert.That(director.Hud.IntermissionHubOpen,Is.True);
        }
        [UnityTest] public IEnumerator UnrecruitedCharactersStayHiddenAndJoinedRosterScrollsWithoutChangingParty()
        {
            TitleSetup();director.StartTitleGame(0);yield return null;
            foreach(string menu in new[]{"출전 편성","장비 관리","성장 · 승급"})
            {
                Click(menu);yield return null;
                foreach(var character in director.Catalog.Characters.Where(c=>!director.IsJoined(c)))
                    Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains(character.DisplayName.Split(' ')[0])),Is.False,menu+" leaked "+character.Id);
                director.Hud.ShowIntermissionHub();yield return null;
            }
            for(int n=1;n<20;n++)director.Campaign.StoryProgress.Add(CrossoverCampaign.Flag(n));
            director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1,2,3,4,5});director.Hud.ShowDeployment();yield return null;
            Assert.That(director.Hud.GetComponentsInChildren<RectTransform>().Count(r=>r.name.StartsWith("ModelCell-")),Is.EqualTo(10));
            var roster=director.Hud.GetComponentInChildren<FormationRosterScroll>();yield return new WaitForEndOfFrame();
            roster.GetComponent<UnityEngine.UI.ScrollRect>().verticalNormalizedPosition=0;yield return null;
            Assert.That(roster.Offset,Is.GreaterThan(0));
            Assert.That(director.Hud.GetComponentsInChildren<RectTransform>().Count(r=>r.name.StartsWith("ModelCell-")),Is.EqualTo(10));
            Assert.That(director.Deployment.Count,Is.EqualTo(6));
        }
        [UnityTest] public IEnumerator FrameInsetRemainsStableAfterHundredsOfFrames()
        {
            director.Hud.ShowIntermissionHub();yield return null;yield return null;
            var frame=director.Hud.GetComponentsInChildren<FramedContent>().First(f=>f.transform.childCount>0);
            var child=frame.transform.Cast<Transform>().OfType<RectTransform>().First(r=>!FramedContent.IsDecoration(r));
            Vector2 min=child.offsetMin,max=child.offsetMax;
            for(int i=0;i<120;i++)yield return null;
            Assert.That(Vector2.Distance(min,child.offsetMin),Is.LessThan(.1f));Assert.That(Vector2.Distance(max,child.offsetMax),Is.LessThan(.1f));
        }
    }
}
