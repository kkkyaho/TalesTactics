using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator CompactForecastCyclesRecipientsAndDetailsWithoutSpendingResources()
        {
            director.BeginBattle();yield return null;director.StopAllCoroutines();var b=director.Session;var actor=b.Active;
            var skill=ScriptableObject.CreateInstance<SkillData>();skill.Id="compact-review";skill.DisplayName="범위 공격";skill.Range=30;skill.Area=30;skill.Target=TargetType.Enemy;
            skill.Effects=new[]{new SkillEffect{Kind=EffectKind.Damage},new SkillEffect{Kind=EffectKind.Status,Status=StatusKind.Stun,Chance=.5f}};
            try
            {
                var foe=b.Units.First(u=>u.Team==Team.Enemy);int hp=foe.CurrentHP,mp=actor.CurrentMP;
                director.SelectSkill(skill);director.SelectTarget(foe.Position);yield return null;yield return null;
                var forecast=director.Forecast;Assert.That(forecast.Rows.Count,Is.GreaterThan(1));var aim=director.Target;
                var card=director.Hud.transform.Find("Commands");
                foreach(var row in forecast.Rows.OrderBy(r=>r.Unit.Team==actor.Team))
                {
                    var text=string.Join("\n",card.GetComponentsInChildren<TMPro.TMP_Text>().Select(t=>t.text));
                    Assert.That(text,Does.Contain(row.Unit.Data.DisplayName));Assert.That(text.Replace(" ",""),Does.Contain("50%"));Assert.That(text,Does.Not.Contain("MP 0"));
                    var bar=(RectTransform)card.Find("ForecastHP/Change");
                    Assert.That(bar.anchorMax.x-bar.anchorMin.x,Is.EqualTo(Mathf.Abs(row.BeforeHP-row.AfterHP)/(float)row.Unit.Stats.HP).Within(.001f));
                    Click("예측 다음 대상");yield return null;
                }
                Click("전체 예측 · "+forecast.Rows.Count+"명 / 비용");yield return null;yield return null;
                Assert.That(card.gameObject.activeSelf,Is.False);Assert.That(director.Hud.UnitDetailsOpen,Is.True);
                director.Hud.CloseUnitDetails();yield return null;Assert.That(card.gameObject.activeSelf,Is.True);
                Assert.That(director.Forecast,Is.SameAs(forecast));Assert.That(director.Target,Is.EqualTo(aim));
                Assert.That(foe.CurrentHP,Is.EqualTo(hp));Assert.That(actor.CurrentMP,Is.EqualTo(mp));Assert.That(actor.Acted,Is.False);
            }
            finally{Object.Destroy(skill);}
        }
    }
}
