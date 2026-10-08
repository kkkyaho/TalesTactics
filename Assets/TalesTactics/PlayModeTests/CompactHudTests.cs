using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator CompactUnitSummaryAndDetailsPreserveBattleAndInput()
        {
            director.BeginBattle();yield return null;
            var unit=director.Session.Active;int hp=unit.CurrentHP,mp=unit.CurrentMP;var state=director.State;
            var summary=(RectTransform)director.Hud.transform.Find("Unit");
            Assert.That(summary.rect.height,Is.LessThanOrEqualTo(180));
            Assert.That(summary.rect.width,Is.LessThanOrEqualTo(350));
            Assert.That(director.Hud.BattlefieldViewport.width,Is.GreaterThan(.9f));
            Click("능력");yield return null;
            Assert.That(director.Hud.UnitDetailsOpen,Is.True);
            Assert.That(director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Where(b=>b.IsInteractable()).Select(b=>b.name),Is.EquivalentTo(new[]{"닫기"}));
            director.GetComponent<GamepadPointer>().Cancel();yield return null;
            Assert.That(director.Hud.UnitDetailsOpen,Is.False);Assert.That(director.State,Is.SameAs(state));
            var commands=(RectTransform)director.Hud.transform.Find("Commands");
            foreach(var button in commands.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                var r=(RectTransform)button.transform;
                Assert.That(commands.rect.Contains(commands.InverseTransformPoint(r.TransformPoint(r.rect.min))),Is.True,button.name);
                Assert.That(commands.rect.Contains(commands.InverseTransformPoint(r.TransformPoint(r.rect.max))),Is.True,button.name);
            }
            director.SkillCommand();yield return null;
            Assert.That(commands.rect.width,Is.EqualTo(300).Within(.01f));
            director.State.Cancel();yield return null;
            director.AttackCommand();yield return null;
            Assert.That(commands.rect.height,Is.LessThanOrEqualTo(180));
            director.State.Cancel();yield return null;
            Assert.That(unit.CurrentHP,Is.EqualTo(hp));Assert.That(unit.CurrentMP,Is.EqualTo(mp));
            director.Restart();yield return null;
            Assert.That(((RectTransform)director.Hud.transform.Find("PreparationContent")).rect.height,Is.GreaterThan(400));
            Assert.That(director.Hud.transform.Find("PreparationContent/FormationModels").gameObject.activeInHierarchy,Is.True);
        }
    }
}
