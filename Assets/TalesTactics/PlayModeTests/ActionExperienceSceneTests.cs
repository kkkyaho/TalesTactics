using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator ActionExperienceFeedbackRewindAndVictoryCommitUseSameProgress()
        {
            director.TrainingMode=false;director.Preferences.Reactions=false;director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1,3});
            var id=director.Catalog.Characters[0].Id;director.Campaign.Get(id).EXP=99;
            director.BeginBattle();if(director.StoryActive)director.FinishStory();yield return null;
            var b=director.Session;var a=b.Units.First(u=>u.Data.Id==id&&u.Team==Team.Player);director.SelectPlayerUnit(a);
            var target=b.Units.First(u=>u.Team==Team.Enemy);
            foreach(var tile in b.Grid.Tiles.Values){tile.Walkable=true;tile.Height=0;}
            b.Grid.Place(a,new Vector2Int(3,2));b.Grid.Place(target,new Vector2Int(4,2));target.Level=10;target.CurrentHP=target.Stats.HP;
            director.RefreshViews();director.AttackCommand();director.SelectTarget(target.Position);director.Confirm();
            for(float deadline=Time.realtimeSinceStartup+15;director.State is ActionExecutionState&&Time.realtimeSinceStartup<deadline;)yield return null;
            Assert.That(a.Level,Is.EqualTo(2));Assert.That(a.BattleEXP,Is.GreaterThan(0));
            yield return new WaitForSeconds(.25f);Assert.That(Object.FindObjectsByType<TMPro.TMP_Text>().Any(t=>t.gameObject.name=="Experience gain"&&t.text.Contains("EXP +")),Is.True);
            Assert.That(Object.FindObjectsByType<TMPro.TMP_Text>().Any(t=>t.name=="Experience caption"&&t.text.Contains("1 → 2")),Is.True);
            Assert.That(director.Campaign.Get(id).Level,Is.EqualTo(1));Assert.That(director.Campaign.Get(id).EXP,Is.EqualTo(99));
            Assert.That(director.RewindAction(),Is.True);yield return null;
            a=director.Session.Units.First(u=>u.Data.Id==id&&u.Team==Team.Player);Assert.That(a.Level,Is.EqualTo(1));Assert.That(a.EXP,Is.EqualTo(99));Assert.That(a.BattleEXP,Is.Zero);
            ActionExperience.Grant(a,36);int earned=a.BattleEXP;a.Damage(99999,director.Session.Grid);
            foreach(var unit in director.Session.Units.Where(u=>u.Team==Team.Enemy))unit.Damage(99999,director.Session.Grid);
            director.SetState(new BattleEndState(director));yield return null;
            Assert.That(director.Campaign.Get(id).Level,Is.EqualTo(2));Assert.That(director.Campaign.Get(id).EXP,Is.EqualTo(99+earned+30-100));
            Assert.That(director.ResultGrowth.Any(s=>s.Contains("EXP +66")),Is.True);director.SaveBattleReward();Assert.That(director.Campaign.Get(id).EXP,Is.EqualTo(65));
        }
    }
}
