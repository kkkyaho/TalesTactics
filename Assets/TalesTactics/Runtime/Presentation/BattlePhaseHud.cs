using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        public void ShowEndPhaseConfirmation()
        {
            if(!battle.IsPlayerCommand||battle.TutorialActive||InputModalOpen||!(battle.Session.Scheduler is TeamTurnScheduler))return;
            ShowUnitDetails(battle.Session.Active);Clear(unitDetails);
            unitDetails.sizeDelta=new Vector2(560,270);
            Label(unitDetails,"아군 턴을 종료할까요?",20,52,24);
            var explanation=Label(unitDetails,"미완료 아군 "+battle.Session.RemainingAllies+"명 · 남은 행동을 포기합니다.",92,60,19);
            explanation.enableAutoSizing=true;explanation.fontSizeMin=16;explanation.fontSizeMax=19;
            Button(unitDetails,"계속 행동",192,()=>CloseUnitDetails(),true,48);HalfButton(unitDetails,0);CaptionLastButton(unitDetails,"취소");
            Button(unitDetails,"아군 턴 종료 확정",192,()=>
            {
                CloseUnitDetails();
                battle.EndPlayerPhaseWithHistory();
            },true,48);HalfButton(unitDetails,1);CaptionLastButton(unitDetails,"턴 종료");
        }
    }
}
