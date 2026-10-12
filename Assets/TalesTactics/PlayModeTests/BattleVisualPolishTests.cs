using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator AlphenOnlyOffersAffordableUnlockedFollowupAfterSwordArte()
        {
            director.Deployment.Clear();director.Deployment.Add(System.Array.FindIndex(director.Catalog.Characters,c=>c.Id=="alphen"));
            director.BeginBattle();yield return null;var u=director.Session.Active;u.Level=1;u.CurrentHP=u.Stats.HP;u.CurrentMP=u.Stats.MP;
            var enemy=director.Session.Units.First(x=>x.Team==Team.Enemy);
            var tile=director.Session.Grid.Neighbors(u.Position).First(t=>t.Walkable&&t.Occupant==null);
            Assert.That(director.Session.Grid.Place(enemy,tile.Coordinate),Is.True);
            Assert.That(director.Session.Resolver.Execute(u,u.Data.Skills[0],enemy.Position,out var error),Is.True,error);
            Assert.That(u.Acted&&u.FlamingChain,Is.True);director.SetState(new CommandState(director));yield return null;
            Assert.That(director.CanOpenSkills,Is.False);Assert.That(FormationControl("Skill / 스킬").interactable,Is.False);
            director.SkillCommand();Assert.That(director.State,Is.TypeOf<CommandState>());
            u.Level=13;u.CurrentHP=u.Stats.HP;u.CurrentMP=u.Stats.MP;director.Hud.Refresh();yield return null;
            Assert.That(director.CanOpenSkills,Is.True);Assert.That(FormationControl("Skill / 스킬").GetComponentInChildren<TMPro.TMP_Text>().text,Is.EqualTo("연계 기술"));
            director.SkillCommand();Assert.That(director.State,Is.TypeOf<ActionSelectionState>());
            var follow=u.Data.Skills.First(s=>s.Gate==SkillGate.FlamingEdge);
            Assert.That(director.Session.Resolver.CanUse(u,u.Data.Skills[0]),Is.Not.Null);
            u.CurrentHP=director.Session.Resolver.HPCost(u,follow);Assert.That(director.CanOpenSkills,Is.False);
            u.CurrentHP=u.Stats.HP;u.Cooldowns[follow.Id]=1;Assert.That(director.CanOpenSkills,Is.False);u.Cooldowns.Clear();
            Assert.That(director.Session.Resolver.Execute(u,follow,enemy.Position,out error,true),Is.True,error);
            Assert.That(u.FlamingChain,Is.False);Assert.That(director.CanOpenSkills,Is.False);
            u.BeginTurn();Assert.That(director.CanOpenSkills,Is.True);
        }
        [UnityTest] public IEnumerator HealthDisplayKeepsTrackAndDamageTrailAndHidesDeadUnits()
        {
            director.BeginBattle();yield return null;yield return new WaitForEndOfFrame();var u=director.Session.Active;
            var hp=Object.FindObjectsByType<UnitHealthDisplay>().Single(x=>x.name=="HP "+u.Data.Id);
            var rect=(RectTransform)hp.transform;float width=rect.rect.width;
            Assert.That(hp.Bar.canvasRenderer,Is.Not.Null);Assert.That(hp.Bar.Fraction,Is.EqualTo(1));
            u.CurrentHP=u.Stats.HP/2;yield return new WaitForEndOfFrame();
            Assert.That(rect.rect.width,Is.EqualTo(width));Assert.That(hp.Bar.Fraction,Is.InRange(.49f,.5f));Assert.That(hp.Bar.Trail,Is.GreaterThan(.9f));
            yield return new WaitForSeconds(.8f);Assert.That(hp.Bar.Trail,Is.EqualTo(hp.Bar.Fraction).Within(.01f));
            director.Board.RotateCamera(90);yield return new WaitForEndOfFrame();Assert.That(rect.rect.width,Is.EqualTo(width));
            u.CurrentHP=0;yield return new WaitForEndOfFrame();Assert.That(hp.Bar.enabled,Is.False);
        }
        [UnityTest] public IEnumerator MovementGhostAndFacingPreviewClearWithoutChangingUnit()
        {
            director.BeginBattle();yield return null;var u=director.Session.Active;var facing=u.Facing;
            director.MoveCommand();var destination=director.Session.Grid.Reachable(u,out _).Keys.First(t=>director.Session.Grid.Path(u,t).Count>1);
            director.PreviewTile(destination);yield return null;
            Assert.That(director.Board.GetComponentsInChildren<SpriteRenderer>().Single(s=>s.name=="Destination ghost").enabled,Is.True);
            director.State.Cancel();yield return null;
            Assert.That(director.Board.GetComponentsInChildren<SpriteRenderer>().Single(s=>s.name=="Destination ghost").enabled,Is.False);
            director.WaitCommand();director.PreviewFacing(Facing.Left);yield return null;
            Assert.That(u.Facing,Is.EqualTo(facing));
            var arrows=director.Board.GetComponentsInChildren<GroundOverlay>().Where(a=>a.name.StartsWith("Ground facing ")).ToArray();
            Assert.That(arrows.Length,Is.EqualTo(4));
            director.Hud.ShowHelp();yield return new WaitForEndOfFrame();Assert.That(director.Hud.InputModalOpen,Is.True);Assert.That(arrows.All(a=>!a.enabled),Is.True);
            director.Hud.CloseHelp();yield return new WaitForEndOfFrame();Assert.That(arrows.All(a=>a.enabled),Is.True);
            director.State.Cancel();yield return new WaitForEndOfFrame();
            Assert.That(u.Facing,Is.EqualTo(facing));Assert.That(director.Session.Active,Is.SameAs(u));
            Assert.That(arrows.All(a=>!a.enabled),Is.True);
        }
    }
}
