using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        public void ShowEndPhaseConfirmation()
        {
            if(!battle.IsPlayerCommand||battle.TutorialActive||InputModalOpen||!(battle.Session.Scheduler is TeamTurnScheduler))return;
            ShowUnitDetails(battle.Session.Active);Clear(unitDetails);
            Label(unitDetails,"아군 턴을 종료할까요?",20,52,24);
            var explanation=Label(unitDetails,"미완료 아군 "+battle.Session.RemainingAllies+"명\n\n남은 이동과 행동을 모두 포기합니다.\n현재 방향을 유지하고 적군 턴으로 진행합니다.\n방어 효과는 추가되지 않습니다.",90,220,19);
            explanation.enableAutoSizing=true;explanation.fontSizeMin=16;explanation.fontSizeMax=19;
            Button(unitDetails,"계속 행동",340,()=>CloseUnitDetails());
            Button(unitDetails,"아군 턴 종료 확정",400,()=>
            {
                CloseUnitDetails();
                battle.EndPlayerPhaseWithHistory();
            });
        }
    }
}
