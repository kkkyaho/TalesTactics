using System;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public enum TutorialStep { None, Movement, Attack, Healing, Waiting, Complete }
    public sealed partial class BattleDirector
    {
        public TutorialStep Tutorial {get;private set;}
        public bool TutorialActive=>Tutorial!=TutorialStep.None;
        public static readonly Vector2Int TutorialDestination=new Vector2Int(2,1);
        UnitRuntime tutorialFighter,tutorialHealer,tutorialEnemy;
        int tutorialInjuryHP;
        // A drill ends through its four real player actions, not enemy elimination.
        sealed class PracticeObjective:IVictoryCondition
        {public BattleResult Evaluate(System.Collections.Generic.IReadOnlyList<UnitRuntime> units)=>BattleResult.Ongoing;}
        sealed class PracticeTurns:ITurnScheduler
        {
            readonly UnitRuntime[] actors;int next;
            public PracticeTurns(params UnitRuntime[] actors){this.actors=actors;}
            public UnitRuntime Next(System.Collections.Generic.IReadOnlyList<UnitRuntime> units)=>next<actors.Length?actors[next++]:null;
            public System.Collections.Generic.IReadOnlyList<UnitRuntime> Preview(System.Collections.Generic.IReadOnlyList<UnitRuntime> units)=>actors.Skip(next).ToArray();
        }
        public void BeginTutorial()
        {
            if(Session!=null||StoryActive)return;
            Hud.CloseHelp();completed=false;RewardPending=false;pendingReward=null;battleTraining=true;
            int fighter=Array.FindIndex(Catalog.Characters,c=>c.Id=="cless");
            int healer=Array.FindIndex(Catalog.Characters,c=>c.Id=="mint");
            Session=new BattleSession(Catalog,new[]{fighter,healer},1,1);
            tutorialFighter=Session.Units.Single(u=>u.Data.Id=="cless"&&u.Team==Team.Player);
            tutorialHealer=Session.Units.Single(u=>u.Data.Id=="mint"&&u.Team==Team.Player);
            var enemies=Session.Units.Where(u=>u.Team==Team.Enemy).ToArray();tutorialEnemy=enemies[0];
            foreach(var enemy in enemies){Session.Grid[enemy.Position].Occupant=null;if(enemy!=tutorialEnemy)Session.Units.Remove(enemy);}
            Session.Grid.Place(tutorialHealer,new Vector2Int(1,2));Session.Grid.Place(tutorialEnemy,new Vector2Int(3,1));
            Session.Victory=new PracticeObjective();Session.Scheduler=new PracticeTurns(tutorialFighter,tutorialHealer);Session.Advance();
            Tutorial=TutorialStep.Movement;Message="입문 연습은 성장·장비·골드·편성을 변경하지 않습니다.";
            Board.Build(Session);Audio.PlayBattle(false);SetState(new CommandState(this));Board.FocusCurrent();
        }
        public bool TutorialAllows(TutorialStep step)=>!TutorialActive||Tutorial==step;
        public bool TutorialSkillAllowed(SkillData skill)=>!TutorialActive||skill!=null&&(
            Tutorial==TutorialStep.Attack&&skill==tutorialFighter.Data.BasicAttack||
            Tutorial==TutorialStep.Healing&&skill.Id=="mint.0");
        public bool TutorialTargetAllowed(Vector2Int p)=>!TutorialActive||
            Tutorial==TutorialStep.Attack&&p==tutorialEnemy.Position||
            Tutorial==TutorialStep.Healing&&p==tutorialFighter.Position;
        public string TutorialInstruction
        {
            get
            {
                switch(Tutorial)
                {
                    case TutorialStep.Movement:return "1/4 이동 · ‘이동’을 누르고 하나뿐인 푸른 목표 타일을 선택하세요.\n취소해도 다시 시도할 수 있습니다. 흰 테두리는 현재 행동자입니다.";
                    case TutorialStep.Attack:return "2/4 공격 · ‘공격’ → 적 선택(또는 Tab) → ‘실행’을 누르세요.\n금색은 선택 대상입니다. 미리보기만으로는 행동이 소모되지 않습니다.";
                    case TutorialStep.Healing:return "3/4 회복 · 민트의 ‘기술’ → ‘퍼스트 에이드’.\n연습용으로 다친 크레스를 선택하고 ‘실행’을 누르세요.";
                    case TutorialStep.Waiting:return "4/4 대기 · 회복으로 MP를 사용했습니다. ‘대기 · 방향’을 누르세요.\n마지막으로 바라볼 방향을 하나 선택하면 턴과 연습을 마칩니다.";
                    default:return "연습 완료 · 이동, 공격 확정, 회복, 방향 선택을 모두 수행했습니다.\n출전 준비로 돌아가거나 처음부터 다시 연습할 수 있습니다.";
                }
            }
        }
        void TutorialMoved(){if(Tutorial==TutorialStep.Movement)Tutorial=TutorialStep.Attack;}
        void TutorialExecuted(SkillData skill,bool executed)
        {
            if(!executed||!TutorialActive)return;
            if(Tutorial==TutorialStep.Attack&&skill==tutorialFighter.Data.BasicAttack)
            {
                Session.EndTurn();tutorialFighter.CurrentHP=Math.Max(1,tutorialFighter.Stats.HP-45);
                tutorialInjuryHP=tutorialFighter.CurrentHP;Session.Advance();
                Tutorial=TutorialStep.Healing;
            }
            else if(Tutorial==TutorialStep.Healing&&skill.Id=="mint.0"&&tutorialFighter.CurrentHP>tutorialInjuryHP)
                Tutorial=TutorialStep.Waiting;
        }
        bool TutorialEndTurn()
        {
            if(!TutorialActive)return false;
            Session.EndTurn();Tutorial=TutorialStep.Complete;SetState(new TutorialCompleteState(this));return true;
        }
        void ResetTutorial(){Tutorial=TutorialStep.None;tutorialFighter=tutorialHealer=tutorialEnemy=null;}
        public void RepeatTutorial(){if(Tutorial!=TutorialStep.Complete)return;Restart();BeginTutorial();}
    }
    public sealed class TutorialCompleteState:BattleState
    {public TutorialCompleteState(BattleDirector b):base(b){}public override void Cancel(){}}
}
