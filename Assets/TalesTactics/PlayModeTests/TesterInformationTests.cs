using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        string TesterModalText()=>string.Join("\n",director.Hud.transform.Find("UnitDetails").GetComponentsInChildren<TMP_Text>().Select(t=>t.text));
        [UnityTest] public IEnumerator TesterTurnEndSeparatesRemainingResourcesFromDirectionForTwoAndSix()
        {
            foreach(int count in new[]{2,6})
            {
                if(director.Session!=null)director.Restart();
                director.Deployment.Clear();director.Deployment.AddRange(Enumerable.Range(0,count));director.BeginBattle();yield return null;
                var session=director.Session;var team=(TeamTurnScheduler)session.Scheduler;var allies=session.Units.Where(u=>u.Team==Team.Player).ToArray();
                Assert.That(allies.Length,Is.EqualTo(count));
                foreach(var u in allies){team.Begin(u);u.Moved=u.Acted=true;}
                director.Hud.ShowEndPhaseConfirmation();yield return null;
                string text=TesterModalText();Assert.That(text,Does.Contain("남은 이동·행동이 없습니다"));Assert.That(text,Does.Not.Contain("포기"));
                Assert.That(director.Hud.transform.Find("UnitDetails").GetComponentsInChildren<RectTransform>().Count(t=>t.name.StartsWith("TurnPending-")),Is.EqualTo(count));
                Assert.That(session.RemainingAllies,Is.EqualTo(count),"Opening a read-only confirmation must not finish units.");
                director.Hud.CloseUnitDetails();allies[0].Moved=false;
                director.Hud.ShowEndPhaseConfirmation();yield return null;
                Assert.That(TesterModalText(),Does.Contain("이동만 남음"));director.Hud.CloseUnitDetails();
                allies[0].Moved=true;allies[0].Acted=false;
                director.Hud.ShowEndPhaseConfirmation();yield return null;
                Assert.That(TesterModalText(),Does.Contain("행동 남음"));director.Hud.CloseUnitDetails();
                team.Complete(allies[0]);director.Hud.ShowEndPhaseConfirmation();yield return null;
                Assert.That(director.Hud.transform.Find("UnitDetails").GetComponentsInChildren<RectTransform>().Count(t=>t.name.StartsWith("TurnPending-")),Is.EqualTo(count-1));
                director.Hud.CloseUnitDetails();
            }
        }
        [UnityTest] public IEnumerator TesterEnemyClickOpensRealSkillsWithoutChangingBattle()
        {
            director.BeginBattle();yield return null;
            var session=director.Session;var active=session.Active;var enemy=session.Units.First(u=>u.Team==Team.Enemy);var state=director.State;
            int hp=active.CurrentHP,mp=active.CurrentMP,remaining=session.RemainingAllies;var position=active.Position;
            director.Board.ResetCamera();yield return null;
            director.HandleBattleClick(TileScreen(enemy.Position));yield return null;
            Assert.That(director.Hud.UnitDetailsOpen,Is.True);Assert.That(TesterModalText(),Does.Contain(enemy.Data.DisplayName));
            Click("적 기술 보기");yield return null;
            Assert.That(director.Hud.DisplayedStatusUnit,Is.SameAs(enemy));Assert.That(TesterModalText(),Does.Contain(enemy.Data.BasicAttack.DisplayName));
            Assert.That(session.Active,Is.SameAs(active));Assert.That(director.State,Is.SameAs(state));
            Assert.That(active.CurrentHP,Is.EqualTo(hp));Assert.That(active.CurrentMP,Is.EqualTo(mp));Assert.That(active.Position,Is.EqualTo(position));Assert.That(session.RemainingAllies,Is.EqualTo(remaining));
            director.Hud.CloseUnitDetails();director.Hud.ShowMission();Click("적 정보");Click("적 상세 보기");Click("임무 적 기술 보기");yield return null;
            Assert.That(director.Hud.MissionOpen,Is.False);Assert.That(director.Hud.DisplayedStatusUnit,Is.SameAs(enemy));Assert.That(TesterModalText(),Does.Contain(enemy.Data.BasicAttack.DisplayName));
        }
        [UnityTest] public IEnumerator TesterPreparationEnemySkillsDoNotStartBattleOrChangeCampaign()
        {
            string before=JsonUtility.ToJson(director.Campaign);var deployment=director.Deployment.ToArray();
            director.Hud.ShowMission();Click("적 정보");Click("적 상세 보기");Click("임무 적 기술 보기");yield return null;
            Assert.That(director.Session,Is.Null);Assert.That(director.Hud.DisplayedStatusUnit.Team,Is.EqualTo(Team.Enemy));
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(before));CollectionAssert.AreEqual(deployment,director.Deployment);
            Assert.That(TesterModalText(),Does.Contain(director.Catalog.Enemy.BasicAttack.DisplayName));
        }
        [UnityTest] public IEnumerator TesterTurnReadinessSeparatesMovementLocksAndIncapacitation()
        {
            director.BeginBattle();yield return null;
            var session=director.Session;var team=(TeamTurnScheduler)session.Scheduler;var allies=session.Units.Where(u=>u.Team==Team.Player).ToArray();
            foreach(var ally in allies){team.Begin(ally);ally.Moved=ally.Acted=true;}
            var unit=allies.First(u=>u!=session.Active);int pending=session.RemainingAllies;
            foreach(var status in new[]{StatusKind.Root,StatusKind.Cage,StatusKind.Stun,StatusKind.Sleep})
            {
                unit.Statuses.Clear();unit.Moved=false;unit.Acted=true;unit.AddStatus(status,2);
                director.Hud.ShowEndPhaseConfirmation();yield return null;
                var row=director.Hud.transform.Find("UnitDetails/TurnEndContent/TurnPending-"+unit.Data.Id);
                string text=string.Join("\n",row.GetComponentsInChildren<TMP_Text>().Select(t=>t.text));
                Assert.That(text,Does.Not.Contain("이동"));Assert.That(TesterModalText(),Does.Contain("남은 이동·행동이 없습니다"));
                Assert.That(text,Does.Contain(status==StatusKind.Stun||status==StatusKind.Sleep?"행동 불가":"방향 확정 대기"));
                Assert.That(unit.Has(status),Is.True);Assert.That(session.RemainingAllies,Is.EqualTo(pending));director.Hud.CloseUnitDetails();
                unit.Acted=false;director.Hud.ShowEndPhaseConfirmation();yield return null;
                row=director.Hud.transform.Find("UnitDetails/TurnEndContent/TurnPending-"+unit.Data.Id);
                text=string.Join("\n",row.GetComponentsInChildren<TMP_Text>().Select(t=>t.text));
                Assert.That(text,Does.Contain(status==StatusKind.Stun||status==StatusKind.Sleep?"행동 불가":"행동 남음"));
                Assert.That(text,Does.Not.Contain("이동"));director.Hud.CloseUnitDetails();
            }
        }
        SkillData[] TesterEnemySkills(UnitRuntime unit)=>new[]{unit.Data.BasicAttack}.Concat(unit.Data.Skills??new SkillData[0]).Concat(unit.TacticalSkills??new SkillData[0]).Concat(new[]{unit.Data.UltimateSkill}).Where(s=>s!=null).Distinct().ToArray();
        IEnumerator TesterAssertAllEnemySkillPages(UnitRuntime unit)
        {
            var skills=TesterEnemySkills(unit);string displayed="";int pages=Mathf.Max(1,(skills.Length+2)/3);
            for(int page=0;page<pages;page++)
            {
                displayed+="\n"+TesterModalText();
                if(page+1<pages){Click("상태 다음 기술");yield return null;}
            }
            foreach(var skill in skills)Assert.That(displayed,Does.Contain(skill.DisplayName),"Missing enemy skill: "+skill.Id);
        }
        [UnityTest] public IEnumerator TesterMainBossPreparationMatchesLiveLevelAndEverySkill()
        {
            director.Catalog.CrossoverReady=true;director.TrainingMode=false;director.SelectedStage=CrossoverCampaign.FirstStage;
            director.Campaign=new CampaignSave();director.Preferences.Difficulty=BattleDifficulty.Veteran;
            director.Deployment.Clear();director.Deployment.AddRange(new[]{0,1});
            string campaign=JsonUtility.ToJson(director.Campaign);var deployment=director.Deployment.ToArray();
            director.Hud.ShowMission();Click("적 정보");Click("적 상세 보기");Click("임무 적 기술 보기");yield return null;
            var preview=director.Hud.DisplayedStatusUnit;
            Assert.That(director.Session,Is.Null);Assert.That(preview.CrossoverBoss,Is.True);
            Assert.That(preview.Level,Is.EqualTo(DifficultyRules.EnemyLevel(CampaignStages.EnemyLevel(CrossoverCampaign.FirstStage),BattleDifficulty.Veteran)));
            Assert.That(TesterEnemySkills(preview).Length,Is.GreaterThan(1));
            yield return TesterAssertAllEnemySkillPages(preview);
            Assert.That(JsonUtility.ToJson(director.Campaign),Is.EqualTo(campaign));CollectionAssert.AreEqual(deployment,director.Deployment);
            director.Hud.CloseUnitDetails();director.BeginBattle();yield return null;
            var live=director.Session.Units.First(u=>u.Team==Team.Enemy);
            Assert.That(live.CrossoverBoss,Is.True);Assert.That(preview.Data,Is.SameAs(live.Data));Assert.That(preview.Level,Is.EqualTo(live.Level));
            Assert.That(preview.Stats.HP,Is.EqualTo(live.Stats.HP));
            CollectionAssert.AreEquivalent(TesterEnemySkills(live).Select(s=>s.Id),TesterEnemySkills(preview).Select(s=>s.Id));
            CollectionAssert.AreEquivalent(live.TacticalSkills.Select(s=>s.Id),preview.TacticalSkills.Select(s=>s.Id));
            director.Hud.ShowUnitSkills(live);yield return null;yield return TesterAssertAllEnemySkillPages(live);
        }
        [UnityTest] public IEnumerator TesterReinforcementPreviewUsesSelectedDifficulty()
        {
            director.TrainingMode=false;director.SelectedStage=2;director.Preferences.Difficulty=BattleDifficulty.Veteran;
            director.Hud.ShowMission();Click("적 정보");Click("다음 적");Click("적 선택: 6");Click("적 상세 보기");Click("임무 적 기술 보기");yield return null;
            var preview=director.Hud.DisplayedStatusUnit;
            Assert.That(director.Session,Is.Null);Assert.That(preview.Data,Is.SameAs(CampaignEncounters.Enemy(director.Catalog,2,6)));
            Assert.That(preview.Level,Is.EqualTo(DifficultyRules.EnemyLevel(CampaignStages.EnemyLevel(2),BattleDifficulty.Veteran)));
            var expected=new BattleSession(director.Catalog,director.Deployment,campaignStage:2,enemyLevel:CampaignStages.EnemyLevel(2),teamTurns:true,difficulty:BattleDifficulty.Veteran,expandedEncounters:true);
            expected.RestoreMissionEvents(true,0);var spawned=expected.Units.Where(u=>u.Team==Team.Enemy).ElementAt(6);
            Assert.That(preview.Level,Is.EqualTo(spawned.Level));CollectionAssert.AreEquivalent(TesterEnemySkills(spawned).Select(s=>s.Id),TesterEnemySkills(preview).Select(s=>s.Id));
        }
    }
}
