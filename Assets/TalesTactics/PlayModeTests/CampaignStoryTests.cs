using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator StoryFlowBuildsChapterMapsAndReplaysWithoutRewards()
        {
            director.Campaign = new CampaignSave(); director.TrainingMode = false;
            int writes = 0; director.PersistCampaign = _ => { writes++; return true; };
            director.RequestBattle(); yield return null;
            Assert.That(director.StoryActive, Is.True); Assert.That(director.Session, Is.Null);
            Click("다음 대사"); yield return null; Assert.That(director.StoryIndex, Is.EqualTo(1));
            Click("이야기 건너뛰기"); yield return null;
            Assert.That(director.StoryActive, Is.False); Assert.That(director.Session.Grid.Tiles.Count, Is.EqualTo(99));
            foreach (var enemy in director.Session.Units.Where(u => u.Team == Team.Enemy)) enemy.Damage(99999, director.Session.Grid);
            director.SetState(new TurnStartState(director)); yield return null;
            Assert.That(writes, Is.EqualTo(1));
            Click("전투 후 이야기"); yield return null;
            while (director.StoryActive) { director.AdvanceStory(); yield return null; }
            Assert.That(writes, Is.EqualTo(1)); Assert.That(director.State, Is.InstanceOf<BattleEndState>());
            director.Restart(); yield return null;
            director.ReplayStory(true); yield return null; Assert.That(director.StoryActive, Is.True);
            director.FinishStory(); yield return null; Assert.That(writes, Is.EqualTo(1));
            director.SelectedStage = 1; director.RequestBattle(); yield return null;
            while (director.StoryActive) { director.AdvanceStory(); yield return null; }
            Assert.That(director.Session.Grid.Tiles.Count, Is.EqualTo(120));
            foreach (var tile in director.Session.Grid.Tiles.Values)
            {
                var screen = director.Board.BattleCamera.WorldToScreenPoint(tile.WorldPosition(director.Catalog.Rules.TileHeight));
                Assert.That(director.Board.BattleCamera.pixelRect.Contains(new Vector2(screen.x, screen.y)), Is.True);
            }
        }

        [UnityTest] public IEnumerator LockedEndingAndFailedRewardsDoNotOpenAfterStory()
        {
            director.Campaign = new CampaignSave(); director.TrainingMode = false;
            director.ReplayStory(true); Assert.That(director.StoryActive, Is.False);
            director.SelectedStage = 1; director.RequestBattle(); Assert.That(director.StoryActive, Is.False);
            director.SelectedStage = 0; director.PersistCampaign = _ => false;
            director.BeginBattle(); yield return null;
            foreach (var enemy in director.Session.Units.Where(u => u.Team == Team.Enemy)) enemy.Damage(99999, director.Session.Grid);
            director.SetState(new TurnStartState(director)); yield return null;
            Assert.That(director.CanReadEnding, Is.False); director.ReadEnding(); Assert.That(director.StoryActive, Is.False);
            director.Restart(); yield return null; director.TrainingMode = true; director.RequestBattle(); yield return null;
            Assert.That(director.StoryActive, Is.False); Assert.That(director.Session.Grid.Tiles.Count, Is.EqualTo(90));
        }
    }
}
