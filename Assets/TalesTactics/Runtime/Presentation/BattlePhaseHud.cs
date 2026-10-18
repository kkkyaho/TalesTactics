using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        bool PhaseActionRemaining(UnitRuntime unit)
        {
            var team=battle.Session.Scheduler as TeamTurnScheduler;
            if(team==null||!team.CanSelect(unit)||unit.Has(StatusKind.Stun)||unit.Has(StatusKind.Sleep))return false;
            return !team.HasBegun(unit)||!unit.Acted||unit.FlamingChain&&unit.Data.Skills.Any(s=>s!=null&&s.Gate==SkillGate.FlamingEdge&&battle.Session.Resolver.CanUse(unit,s,true)==null);
        }
        bool PhaseMoveRemaining(UnitRuntime unit)
        {
            var team=battle.Session.Scheduler as TeamTurnScheduler;
            return team!=null&&team.CanSelect(unit)&&!unit.Has(StatusKind.Stun)&&!unit.Has(StatusKind.Sleep)&&!unit.Has(StatusKind.Root)&&!unit.Has(StatusKind.Cage)&&(!team.HasBegun(unit)||!unit.Moved);
        }
        string PhaseReadiness(UnitRuntime unit)
        {
            if(unit.Has(StatusKind.Stun)||unit.Has(StatusKind.Sleep))return "행동 불가";
            bool move=PhaseMoveRemaining(unit),action=PhaseActionRemaining(unit);
            return move&&action?"이동·행동 남음":move?"이동만 남음":action?"행동 남음":"방향 확정 대기";
        }
        public void ShowEndPhaseConfirmation()
        {
            if(!battle.IsPlayerCommand||battle.TutorialActive||InputModalOpen||!(battle.Session.Scheduler is TeamTurnScheduler team))return;
            var pending=battle.Session.Units.Where(team.CanSelect).ToArray();
            bool remaining=pending.Any(u=>PhaseMoveRemaining(u)||PhaseActionRemaining(u));
            PrepareUnitModal(battle.Session.Active);Clear(unitDetails);
            var content=FormationRect(unitDetails,"TurnEndContent",new Vector2(.03f,.03f),new Vector2(.97f,.97f));
            UpdateTesterFeedbackLayout();
            FormationText(content,"아군 턴을 종료할까요?",new Vector2(0,.87f),new Vector2(.92f,1),25);
            CloseIcon(unitDetails,"턴 종료 안내 닫기",()=>CloseUnitDetails());
            FormationText(content,remaining?"아래 동료에게 이동·행동이 남아 있습니다.":"남은 이동·행동이 없습니다. 현재 방향으로 턴을 종료합니다.",new Vector2(0,.71f),new Vector2(1,.85f),18).color=formationTeal;
            for(int i=0;i<pending.Length;i++)
            {
                float top=.69f-i*.081f;var row=FormationRect(content,"TurnPending-"+pending[i].Data.Id,new Vector2(0,top-.075f),new Vector2(1,top));
                FormationText(row,pending[i].Data.DisplayName.Split(' ')[0],Vector2.zero,new Vector2(.34f,1),18);
                FormationText(row,PhaseReadiness(pending[i]),new Vector2(.35f,0),Vector2.one,18).color=formationTeal;
            }
            FormationButton(content,"계속 행동","돌아가기",Vector2.zero,new Vector2(.48f,.12f),()=>CloseUnitDetails());
            FormationButton(content,"아군 턴 종료 확정","턴 종료",new Vector2(.52f,0),new Vector2(1,.12f),()=>{CloseUnitDetails();battle.EndPlayerPhaseWithHistory();});
        }
        void UpdateTesterFeedbackLayout()
        {
            if(!UnitDetailsOpen)return;
            bool phase=unitDetails.Find("TurnEndContent")?.gameObject.activeSelf==true,enemy=unitDetails.Find("EnemyInspectionContent")?.gameObject.activeSelf==true;
            if(!phase&&!enemy)return;
            float scale=battle.Preferences.TextScale;
            unitDetails.sizeDelta=new Vector2(Mathf.Min((phase?650:640)*scale,canvas.rect.width-32),Mathf.Min((phase?540:530)*scale,canvas.rect.height-32));
        }
    }
}
