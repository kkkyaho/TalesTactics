using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleDirector
    {
        bool guardRecorded;
        IEnumerator PresentReactions(float speed=1)
        {
            foreach(var strike in Session.Resolver.LastReactions)
            {
                Message=strike.Text;Hud.Refresh();Board.BeginSkill(strike.Actor,strike.Skill,strike.Target.Position,speed);
                yield return new WaitForSeconds(.18f/speed);Board.ReleaseSkill(strike.Actor);
                Board.PresentImpact(strike.Actor,strike.Skill,strike.Target.Position,new Dictionary<UnitRuntime,int>{{strike.Target,strike.BeforeHP}},new[]{strike.Target},new Dictionary<UnitRuntime,int>{{strike.Target,strike.AfterHP}},false);
                yield return new WaitForSeconds(.22f/speed);
            }
        }
        public bool CanRewind=>Session!=null&&Session.CampaignStage>=0&&Session.History.Count>0&&Session.RewindsLeft>0&&!RewardPending&&!TutorialActive&&!StoryActive&&!TimingActive&&
            (IsPlayerCommand&&Session.Result==BattleResult.Ongoing||State is BattleEndState&&Session.Result==BattleResult.Defeat);
        public string RewindLabel=>Session!=null&&Session.History.Count>0?Session.History[Session.History.Count-1].Label:"기록 없음";
        void RecordAction(string label){if(!TutorialActive)Session?.RecordAction(label);}
        public bool RewindAction()
        {
            if(!CanRewind)return false;
            BattleSession restored;UnityEngine.Random.State random;
            try{restored=Session.RewindLast(out random);}catch(Exception){SaveNotice="행동 기록을 복원할 수 없습니다. 현재 전투를 유지했습니다.";return false;}
            Hud.CloseMission();Hud.CloseSystemMenu();Hud.CloseHelp();Hud.CloseUnitDetails();StopAllCoroutines();ClearTacticalSelection();
            Session=restored;UnityEngine.Random.state=random;completed=false;pendingReward=null;RewardPending=false;guardRecorded=false;
            SelectedSkill=null;Target=null;Board.Build(Session);Audio.PlayBattle(CampaignStages.Get(battleStage).BossMusic);
            Message="행동을 되돌렸습니다. 남은 횟수 "+Session.RewindsLeft;SetState(new CommandState(this));return true;
        }
        public void EndPlayerPhaseWithHistory()
        {if(!IsPlayerCommand)return;RecordAction("아군 턴 종료");if(Session.EndPlayerPhase())SetState(new TurnStartState(this));}
    }
    public sealed partial class BattleHud
    {
        void DrawRewindConfirmation()
        {
            Label(systemWindow,"직전 행동을 되돌릴까요?",132,56,24);
            Label(systemWindow,battle.RewindLabel+" 이전으로 복원\nHP·MP·상태·위치·턴 순서·확률 판정 복원\n그 뒤에 진행된 적 행동도 함께 되돌립니다.\n\n남은 횟수 "+(battle.Session?.RewindsLeft??0)+" / 3 · 최근 12개 행동\n캠페인 전투에서만 사용 · 승리 확정 뒤 사용 불가",210,200,20);
            Button(systemWindow,"되감기 확정",430,()=>{if(!battle.RewindAction())ShowSystemMenu();},battle.CanRewind,48);
            Button(systemWindow,"현재 전투 계속",492,()=>ShowSystemMenu(),true,40);
        }
    }
}
