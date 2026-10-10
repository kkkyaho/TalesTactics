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
            Label(systemWindow,"행동을 되돌릴까요?",24,48,24);
            Label(systemWindow,battle.RewindLabel+" 전으로",82,44,20);
            Label(systemWindow,"남은 횟수  "+(battle.Session?.RewindsLeft??0)+" / 3",136,30,18);
            Button(systemWindow,"현재 전투 계속",192,()=>ShowSystemMenu(),true,48);HalfButton(systemWindow,0);CaptionLastButton(systemWindow,"취소");
            Button(systemWindow,"되감기 확정",192,()=>{if(!battle.RewindAction())ShowSystemMenu();},battle.CanRewind,48);HalfButton(systemWindow,1);CaptionLastButton(systemWindow,"되돌리기");
        }
    }
}
