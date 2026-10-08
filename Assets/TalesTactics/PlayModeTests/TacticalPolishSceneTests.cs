using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator TacticalPolishBulkEndConfirmsAndCancelPreservesActions()
        {
            Click("전투 시작");yield return null;var b=director.Session;var active=b.Active;
            Click("아군 턴 종료");yield return null;Assert.That(director.Hud.InputModalOpen,Is.True);Assert.That(b.RemainingAllies,Is.EqualTo(3));
            Click("계속 행동");yield return null;Assert.That(b.Active,Is.SameAs(active));Assert.That(b.RemainingAllies,Is.EqualTo(3));
            director.Preferences.SkipEnemyAnimations=true;Click("아군 턴 종료");yield return null;Click("아군 턴 종료 확정");yield return null;
            float deadline=Time.realtimeSinceStartup+15;while(b.Active.Team==Team.Enemy&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(((TeamTurnScheduler)b.Scheduler).Round,Is.EqualTo(2));Assert.That(b.Units.Where(u=>u.Team==Team.Enemy).All(u=>u.TurnsStarted==1),Is.True);
        }
        [UnityTest] public IEnumerator TacticalPolishAllSkillsShowsUnavailableReasonWithoutSpendingAction()
        {
            Click("전투 시작");yield return null;var u=director.Session.Active;u.CurrentMP=0;director.SkillCommand();yield return null;
            Click("기술 · 사용 가능");yield return null;
            var buttons=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>();var row=buttons.FirstOrDefault(b=>b.name.Contains(" · MP")&&b.GetComponentInChildren<TMPro.TMP_Text>().text.StartsWith("◇"));
            Assert.That(row,Is.Not.Null);Click(row.name);yield return null;
            Assert.That(director.Hud.UnitDetailsOpen,Is.True);Assert.That(director.Hud.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("부족")||t.text.Contains("조건")||t.text.Contains("레벨")),Is.True);Assert.That(u.Acted,Is.False);
        }
        [UnityTest] public IEnumerator TacticalPolishPresetsAndGrowthPersistThroughUi()
        {
            director.TrainingMode=false;director.Campaign.Get(director.Catalog.Characters[0].Id).Level=10;director.Hud.ShowDeployment();
            Click("편성 프리셋");yield return null;
            var save=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="현재 편성 저장");save.onClick.Invoke();yield return null;
            Assert.That(director.Campaign.Presets[0].Members.Length,Is.EqualTo(3));Click("닫기");yield return null;
            Click("성장 · 승급");yield return null;Click("성장 방향 선택");yield return null;
            var assault=director.Hud.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name.StartsWith("돌파 ·"));Assert.That(assault.interactable,Is.True);assault.onClick.Invoke();yield return null;
            Assert.That(director.Campaign.Get(director.Catalog.Characters[0].Id).Growth,Is.EqualTo(GrowthPath.Assault));
        }
    }
}
