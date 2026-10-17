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
        [UnityTest] public IEnumerator BattleDockKeepsCommandsAndTimingWithinInnerFrame()
        {
            director.BeginBattle();director.StopAllCoroutines();
            foreach(float scale in new[]{1f,1.15f,1.3f})
            foreach(int state in new[]{0,1,2,3})
            {
                director.Preferences.TextScale=scale;
                director.SetState(state==0?(BattleState)new CommandState(director):state==1?new MoveSelectionState(director):new ActionExecutionState(director));
                if(state==3){director.TimingActive=true;director.TimingProgress=.5f;director.Hud.UpdateTiming();}
                yield return null;yield return new WaitForEndOfFrame();Canvas.ForceUpdateCanvases();
                var dock=director.Hud.transform.Find("BattleCommandDock").GetComponent<FramedContent>();
                var allowed=dock.ContentRect;allowed.xMin-=1;allowed.xMax+=1;allowed.yMin-=1;allowed.yMax+=1;
                foreach(string partName in new[]{"Unit","Commands","Threat controls"})
                {
                    var part=director.Hud.transform.Find(partName);
                    foreach(var button in part.GetComponentsInChildren<UnityEngine.UI.Button>())
                    {
                        var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                        Assert.That(corners.All(c=>allowed.Contains(dock.transform.InverseTransformPoint(c))),Is.True,partName+"/"+button.name+" at "+scale+" state "+state);
                    }
                    foreach(var text in part.GetComponentsInChildren<TMPro.TMP_Text>())
                    {
                        text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.text);
                        foreach(var glyph in text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(c=>c.isVisible))
                        {
                            Assert.That(allowed.Contains(dock.transform.InverseTransformPoint(text.transform.TransformPoint(glyph.bottomLeft))),Is.True,text.text);
                            Assert.That(allowed.Contains(dock.transform.InverseTransformPoint(text.transform.TransformPoint(glyph.topRight))),Is.True,text.text);
                        }
                    }
                }
                director.TimingActive=false;
            }
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
