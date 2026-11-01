using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator EndingResumesFromSavedLineAndFailedCompletionCannotAdvanceOrRewardTwice()
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
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator UnrecruitedCharactersStayHiddenAndJoinedRosterScrollsWithoutChangingParty()
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
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator SplitBattlePanelsKeepCommandsStatusAndTimingWithinTheirOwnFrames()
        {
            director.BeginBattle();director.StopAllCoroutines();
            foreach(float scale in new[]{1f,1.15f,1.3f})
            foreach(int state in new[]{0,1,2,3})
            {
                director.Preferences.TextScale=scale;director.TimingActive=false;
                director.SetState(state==0?(BattleState)new CommandState(director):state==1?new MoveSelectionState(director):new ActionExecutionState(director));
                if(state==3){director.TimingActive=true;director.TimingProgress=.5f;director.Hud.UpdateTiming();}
                yield return null;yield return new WaitForEndOfFrame();Canvas.ForceUpdateCanvases();
                var summary=(RectTransform)director.Hud.transform.Find("Unit");var commands=(RectTransform)director.Hud.transform.Find("Commands");
                Assert.That(summary.gameObject.activeInHierarchy,Is.True);
                AssertSplitHudPanelOnScreen(summary);AssertSplitHudPanelContents(summary,8);
                Assert.That(commands.gameObject.activeInHierarchy,Is.EqualTo(state!=2),"Passive execution has no command card; manual timing retains its controls.");
                Assert.That(director.Hud.transform.Find("Threat controls").gameObject.activeInHierarchy,Is.False,"Secondary help does not occupy another permanent battle card.");
                if(commands.gameObject.activeInHierarchy)
                {
                    AssertSplitHudPanelOnScreen(commands);AssertSplitHudPanelContents(commands,8);
                    Assert.That(SplitHudScreenRect(commands).Overlaps(SplitHudScreenRect(summary)),Is.False,"Local controls must not obscure fixed status at text scale "+scale);
                }
                if(state==3)
                {
                    var timing=commands.GetComponentsInChildren<TMPro.TMP_Text>().Single(t=>t.text.Contains("진행"));
                    Assert.That(timing.text,Does.Contain("50").And.Contain("40%"));
                    Assert.That(commands.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name=="타이밍 입력 (Space)"&&b.IsInteractable()),Is.True,"The timing action remains available after relocation.");
                }
                director.TimingActive=false;
            }
        }
        static Rect SplitHudScreenRect(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners.Min(p=>p.x),corners.Min(p=>p.y),corners.Max(p=>p.x),corners.Max(p=>p.y));
        }
        static void AssertSplitHudRectUnchanged(Rect actual,Rect expected,string reason)
        {
            Assert.That(Vector2.Distance(actual.position,expected.position),Is.LessThan(.2f),reason);
            Assert.That(Vector2.Distance(actual.size,expected.size),Is.LessThan(.2f),reason);
        }
        static void AssertSplitHudPanelOnScreen(RectTransform panel)
        {
            var bounds=SplitHudScreenRect(panel);
            Assert.That(bounds.xMin,Is.GreaterThanOrEqualTo(0),panel.name);Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(0),panel.name);
            Assert.That(bounds.xMax,Is.LessThanOrEqualTo(Screen.width),panel.name);Assert.That(bounds.yMax,Is.LessThanOrEqualTo(Screen.height),panel.name);
        }
        static void AssertSplitHudPanelContents(RectTransform panel,float inset)
        {
            // FramedContent is intentionally disabled on manually laid-out battle panels.
            // Test the rendered parent rectangle and a visible safe margin, never its default ContentRect.
            var allowed=panel.rect;allowed.xMin+=inset;allowed.xMax-=inset;allowed.yMin+=inset;allowed.yMax-=inset;
            Assert.That(allowed.width,Is.GreaterThan(0));Assert.That(allowed.height,Is.GreaterThan(0));
            const float roundingTolerance=.2f;
            allowed.xMin-=roundingTolerance;allowed.xMax+=roundingTolerance;allowed.yMin-=roundingTolerance;allowed.yMax+=roundingTolerance;
            foreach(var button in panel.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                Assert.That(corners.All(c=>allowed.Contains(panel.InverseTransformPoint(c))),Is.True,panel.name+"/"+button.name+" crosses its own frame inset.");
            }
            foreach(var text in panel.GetComponentsInChildren<TMPro.TMP_Text>())
            {
                text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.text);
                foreach(var glyph in text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(c=>c.isVisible))
                {
                    Assert.That(allowed.Contains(panel.InverseTransformPoint(text.transform.TransformPoint(glyph.bottomLeft))),Is.True,panel.name+": "+text.text);
                    Assert.That(allowed.Contains(panel.InverseTransformPoint(text.transform.TransformPoint(glyph.topRight))),Is.True,panel.name+": "+text.text);
                }
            }
        }
        [UnityTest, Category("BattleHudRedesign")] public IEnumerator FrameInsetRemainsStableAfterHundredsOfFrames()
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
