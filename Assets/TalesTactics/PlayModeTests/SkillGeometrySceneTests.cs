using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator PiercingSkillPreviewAndExecutionShareSightAndLineArea()
        {
            director.TrainingMode = true;
            director.Deployment.Clear(); director.Deployment.Add(6);
            director.BeginBattle(); yield return null;
            director.StopAllCoroutines();
            var session = director.Session;
            var caster = session.Units.Single(u => u.Team == Team.Player);
            session.Active = caster;
            foreach (var tile in session.Grid.Tiles.Values) { tile.Height = 0; tile.Walkable = true; }
            session.Grid.Place(caster, new Vector2Int(1, 1));
            var enemies = session.Units.Where(u => u.Team == Team.Enemy).ToArray();
            session.Grid.Place(enemies[0], new Vector2Int(2, 1));
            session.Grid.Place(enemies[1], new Vector2Int(4, 1));
            var skill = caster.Data.Skills.Single(s => s.Id == "natalia.0");
            Assert.That(skill.Shape, Is.EqualTo(SkillAreaShape.Line));
            director.SetState(new CommandState(director)); director.SelectSkill(skill);
            director.SelectTarget(enemies[0].Position); yield return null;
            Assert.That(director.Target.HasValue, Is.True);
            Assert.That(session.Resolver.Targets(caster, skill, enemies[0].Position).Count(), Is.EqualTo(2));
            int hp = enemies[1].CurrentHP;
            session.Grid[new Vector2Int(3, 1)].Walkable = false;
            Assert.That(session.Resolver.Targets(caster, skill, enemies[0].Position).Count(), Is.EqualTo(1));
            director.SelectTarget(enemies[1].Position); yield return null;
            Assert.That(director.Message.Contains("유효한 타겟"), Is.True);
            session.Grid[new Vector2Int(3, 1)].Walkable = true;
            Assert.That(session.Resolver.Execute(caster, skill, enemies[0].Position, out _), Is.True);
            Assert.That(enemies[1].CurrentHP < hp, Is.True);
        }
    }
}
