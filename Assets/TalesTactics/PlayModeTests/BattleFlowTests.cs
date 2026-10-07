using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator SkillClickSkipsDetailsAndCancelRemembersEachCharacter()
        {
            director.BeginBattle();yield return null;var unit=director.Session.Active;int mp=unit.CurrentMP;
            var skill=unit.Data.Skills.First(s=>director.Session.Resolver.CanUse(unit,s)==null);
            director.SkillCommand();yield return null;Click(skill.DisplayName+" · MP"+skill.MPCost);yield return null;
            Assert.That(director.State,Is.TypeOf<TargetSelectionState>());director.State.Cancel();yield return null;
            Assert.That(director.State,Is.TypeOf<ActionSelectionState>());Assert.That(Object.FindObjectsByType<LineRenderer>().Any(r=>r.name.StartsWith("Range border ")&&r.enabled),Is.False);
            Assert.That(director.LastSkill,Is.SameAs(skill));Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name,Is.EqualTo(skill.DisplayName+" · MP"+skill.MPCost));
            Assert.That(unit.CurrentMP,Is.EqualTo(mp));Assert.That(unit.Acted,Is.False);
            director.State.Cancel();director.AttackCommand();director.State.Cancel();Assert.That(director.State,Is.TypeOf<CommandState>());
            director.Session.Active=director.Session.Units.First(u=>u.Team==Team.Player&&u!=unit);Assert.That(director.LastSkill,Is.Null);
            director.Session.Active=unit;director.SkillCommand();yield return null;Assert.That(director.LastSkill,Is.SameAs(skill));
        }
        [UnityTest] public IEnumerator EnemySpeedAndBriefModePreserveCostsDamageRngAndTurnOrder()
        {
            string expected=null;float[] elapsed=new float[4];float timeScale=Time.timeScale;
            for(int mode=0;mode<4;mode++)
            {
                var settings=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(director.Preferences));settings.EnemySpeedMode=mode==3?0:mode;settings.SkipEnemyAnimations=mode==3;
                Assert.That(director.SavePreferences(settings,false),Is.True);director.TrainingMode=true;director.BeginBattle();yield return null;
                var session=director.Session;var enemy=session.Units.First(u=>u.Team==Team.Enemy);Assert.That(session.Grid.Place(enemy,new Vector2Int(6,1)),Is.True);session.Active=enemy;director.Board.Sync();
                Random.InitState(9876);float start=Time.realtimeSinceStartup;director.StartCoroutine(director.EnemyTurn());
                while(director.State is ActionExecutionState&&Time.realtimeSinceStartup-start<10f)yield return null;
                elapsed[mode]=Time.realtimeSinceStartup-start;Assert.That(director.State,Is.TypeOf<CommandState>());
                Assert.That(enemy.Moved&&enemy.Acted,Is.True);Assert.That(session.Units.Any(u=>u.Team==Team.Player&&u.CurrentHP<u.Stats.HP),Is.True);
                string outcome=string.Join("|",session.Units.Select(u=>JsonUtility.ToJson(new CheckpointUnit(u))))+"|"+session.Units.IndexOf(session.Active)+"|"+JsonUtility.ToJson(Random.state);
                if(expected==null)expected=outcome;else Assert.That(outcome,Is.EqualTo(expected),"Presentation must not change gameplay: mode "+mode);
                Assert.That(Time.timeScale,Is.EqualTo(timeScale));director.Restart();yield return null;
            }
            Assert.That(elapsed[1],Is.LessThan(elapsed[0]*.85f));Assert.That(elapsed[2],Is.LessThan(elapsed[1]*.85f));Assert.That(elapsed[3],Is.LessThan(elapsed[0]*.85f));
            System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath,"../Docs/BattleFlow/enemy-speed.csv"),"mode,seconds\n"+string.Join("\n",elapsed.Select((v,i)=>i+","+v.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture))));
        }
        [UnityTest] public IEnumerator ResultsShowCommittedGrowthRetryOnceAndPrepareNextChapter()
        {
            director.TrainingMode=false;director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1,2,3,4,5});int writes=0;director.PersistCampaign=_=>++writes>1;director.RewardRoll=()=>9999;
            director.BeginBattle();yield return null;foreach(var enemy in director.Session.Units.Where(u=>u.Team==Team.Enemy))enemy.Damage(99999,director.Session.Grid);
            director.SetState(new BattleEndState(director));yield return null;
            Assert.That(director.RewardPending,Is.True);Assert.That(director.ResultRewards,Does.Contain("미적용").Or.Contain("아직 적용"));Assert.That(director.ResultGrowth,Is.Empty);
            Click("보상 저장 재시도");yield return null;Assert.That(director.RewardPending,Is.False);Assert.That(director.ResultRewards,Does.Contain("골드 +120"));Assert.That(director.ResultGrowth.Length,Is.EqualTo(6));Assert.That(director.ResultGrowth.All(s=>s.Contains("Lv1 → 2")),Is.True);
            var panel=(RectTransform)director.Hud.transform.Find("BattleResults");
            foreach(var text in panel.GetComponentsInChildren<TMPro.TMP_Text>()){text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.text);}
            director.Hud.ShowSystemMenu();yield return null;Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.Button>().All(b=>!b.IsInteractable()),Is.True);director.Hud.CloseSystemMenu();
            director.CompleteBattle();director.SaveBattleReward();Assert.That(writes,Is.EqualTo(2));Click("다음 장 출전 준비");yield return null;
            Assert.That(director.Session,Is.Null);Assert.That(director.SelectedStage,Is.EqualTo(1));Assert.That(director.Campaign.Gold,Is.EqualTo(420));
            Assert.That(director.Hud.transform.Find("Commands").gameObject.activeSelf,Is.True);
        }
    }
}
