using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace TalesTactics.PlayModeTests
{
    public partial class BattleSceneTests
    {
        [UnityTest] public IEnumerator UtilityEnemyCoroutineUsesHealingThroughNormalResolver()
        {
            director.UseUtilityAI=true;director.BeginBattle();yield return null;director.StopAllCoroutines();
            var enemy=director.Session.Units.First(u=>u.Team==Team.Enemy);director.Session.Active=enemy;enemy.BeginTurn();
            enemy.CurrentHP=10;int mp=enemy.CurrentMP;
            yield return director.EnemyTurn();director.StopAllCoroutines();
            Assert.That(enemy.CurrentHP>10,Is.True);Assert.That(enemy.CurrentMP<mp,Is.True);
        }
        [UnityTest] public IEnumerator TacticalOptionsButtonsConfigureSessionAndTrainingDoesNotSave()
        {
            int writes=0;director.Campaign=new CampaignSave();director.PersistCampaign=_=>{writes++;return true;};
            director.Hud.ShowDeployment();Click("전투 규칙 설정");Click("턴 순서: 아군 자유 선택");Click("턴 순서: SPD 라운드");Click("적 AI: 기본");
            Click("훈련 목표: 모든 적 격파");Click("출전 준비로");Click("전투 시작");yield return null;
            Assert.That(director.Session.Scheduler,Is.TypeOf<CTTurnScheduler>());Assert.That(director.Session.UseUtilityAI,Is.True);
            Assert.That(director.Session.Objective,Is.EqualTo(ObjectiveKind.Boss));
            director.StopAllCoroutines();director.Session.ObjectiveUnit.Damage(99999,director.Session.Grid);
            director.SetState(new BattleEndState(director));yield return null;
            Assert.That(director.Session.Result,Is.EqualTo(BattleResult.Victory));Assert.That(writes,Is.Zero);
            Assert.That(director.Message,Does.Contain("훈련"));Assert.That(director.CanReadEnding,Is.False);
        }
        [UnityTest] public IEnumerator ArrivalMovementEndsBattleAndEscortDeathFails()
        {
            director.TrainingObjective=ObjectiveKind.Reach;director.BeginBattle();yield return null;
            director.StopAllCoroutines();var session=director.Session;session.Active=session.Units.First(u=>u.Team==Team.Player);
            session.Grid.Place(session.Active,session.Destination+Vector2Int.left);session.Active.BeginTurn();
            director.SetState(new CommandState(director));yield return director.MoveUnit(session.Destination);
            Assert.That(director.State,Is.TypeOf<BattleEndState>());Assert.That(session.Result,Is.EqualTo(BattleResult.Victory));
            director.Restart();director.TrainingObjective=ObjectiveKind.Escort;director.BeginBattle();yield return null;
            director.StopAllCoroutines();director.Session.ObjectiveUnit.Damage(99999,director.Session.Grid);
            director.SetState(new TurnStartState(director));
            Assert.That(director.State,Is.TypeOf<BattleEndState>());Assert.That(director.Session.Result,Is.EqualTo(BattleResult.Defeat));
        }
        [UnityTest] public IEnumerator CTUtilityScenariosReachEveryObjectiveWithoutForcedDamage()
        {
            var rows=new List<string>{"objective,result,turns,skills,elapsedMs"};
            foreach(ObjectiveKind objective in System.Enum.GetValues(typeof(ObjectiveKind)))
            {
                var session=new BattleSession(director.Catalog,new[]{0,1,3,5,6,8},25,10,-1,true,true,objective);
                var watch=System.Diagnostics.Stopwatch.StartNew();int turns=0,skills=0;
                while(session.Result==BattleResult.Ongoing&&turns<350)
                {
                    session.Advance();var u=session.Active;turns++;
                    if(!u.Has(StatusKind.Stun)&&!u.Has(StatusKind.Sleep))
                    {
                        var plan=new EnemyPlanner().Plan(session,u);
                        if(plan.Destination!=u.Position)Assert.That(session.Move(plan.Destination),Is.True);
                        if(session.Result==BattleResult.Ongoing&&plan.Skill!=null)
                        {
                            var aim=plan.Aim??plan.Target.Position;if(aim!=u.Position)u.Facing=SkillResolver.Toward(u.Position,aim);
                            Assert.That(session.Resolver.Execute(u,plan.Skill,aim,out var error),Is.True,error);
                            if(plan.Skill!=u.Data.BasicAttack)skills++;
                        }
                        else if(plan.Guard)u.AddStatus(StatusKind.Guard,2);
                    }
                    session.EndTurn();if(turns%10==0)yield return null;
                }
                watch.Stop();rows.Add($"{objective},{session.Result},{turns},{skills},{watch.ElapsedMilliseconds}");
                File.WriteAllLines(Path.Combine(Application.dataPath,"../Docs/tactical-scenarios.csv"),rows);
                Assert.That(session.Result,Is.EqualTo(BattleResult.Victory),objective.ToString());
                Assert.That(skills>0,Is.True,"Utility must use more than basic attacks");
            }
        }
    }
}
