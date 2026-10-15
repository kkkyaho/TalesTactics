using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class CrossoverBattle
    {
        bool IsTrial=>personal&&PersonalScenarios.Tier(battle.CampaignStage)==2;
        string TrialOwner=>PersonalScenarios.Arc(battle.CampaignStage).Owner;
        public static readonly Vector2Int ContractSeal=new Vector2Int(6,4);
        void RecordTrialAction(UnitRuntime actor,SkillData skill)
        {
            if(!IsTrial)return;
            var resolver=battle.Resolver;var owner=TrialOwner;
            if(owner=="cless")
            {
                if(actor.Team==Team.Player&&skill.Effects.Any(e=>e.Kind==EffectKind.Damage))State.TrialProgress|=1;
                if(actor.Team==Team.Player&&resolver.LastHealing>0)State.TrialProgress|=2;
            }
            if(owner=="kisara"&&resolver.LastKisaraGuardedDamage>0)State.TrialProgress|=1;
            if(actor.Data.Id!=owner)return;
            if((owner=="mint"||owner=="shionne")&&resolver.LastHealing>0)State.TrialActions++;
            if(owner=="tear"&&skill.Effects.Any(e=>e.Status==StatusKind.Song))State.TrialProgress|=1;
            if(owner=="jade"&&resolver.LastInterruptions>0)State.TrialProgress|=1;
            if(owner=="velvet"&&resolver.LastDrain>0)State.TrialProgress|=1;
            if(owner=="alphen"&&skill.Gate==SkillGate.FlamingEdge&&resolver.HPCost(actor,skill)>0)State.TrialProgress|=1;
        }
        void RecordTrialGuard(UnitRuntime unit)
        {
            if(IsTrial&&TrialOwner=="cless"&&unit.Team==Team.Player&&unit.Has(StatusKind.Guard))State.TrialProgress|=4;
        }
        string TrialDescription
        {
            get
            {
                switch(TrialOwner)
                {
                    case "cless":return "오리진 계약 · "+((State.TrialProgress&1)!=0?"공격 완료":"공격 미완료")+" / "+((State.TrialProgress&2)!=0?"치유 완료":"치유 미완료")+" / "+((State.TrialProgress&4)!=0?"방어 완료":"방어 미완료")+" · 서쪽→동쪽→북쪽 신호 · 남쪽 계약 문양 (6,4)";
                    case "mint":return "민트의 치료소 · 유효 치유 "+State.TrialActions+"/3 · 구조 신호 3개 연결 후 귀환 (격파 불필요)";
                    case "farah":return "파라의 원소 시련 · 세 신호를 순회하고 맥스웰 격파";
                    case "tear":return "티아의 선율 · 홀리 송 사용 · 신호 3개 연결 후 수호자 저지";
                    case "jade":return "제이드의 제어술 · 미스틱 케이지로 준비 공격 취소 · 장치 정지 후 집행자 저지";
                    case "natalia":return "나탈리아의 약속 · 구조 신호 3개와 출구 연결 · 장치 파괴 (격파 불필요)";
                    case "velvet":return "벨벳의 의지 · 클로 흡혈로 실제 HP 회복 후 시련 상대에게 승리";
                    case "alphen":return "알펜의 불꽃 · HP를 지불하는 화염검 연계 사용 · 장치 정지 후 시련 상대에게 승리";
                    case "shionne":return "시온의 손길 · 유효 회복/부활 "+State.TrialActions+"/3 · 구조 신호와 귀환 연결 (격파 불필요)";
                    case "kisara":return "키사라의 방패 · 방어 중 공격을 버텨 동료 보호 · 신호 3개 연결 후 시련 상대에게 승리";
                    default:return "시련 상대에게 승리";
                }
            }
        }
        bool TrialComplete
        {
            get
            {
                var owner=battle.Units.FirstOrDefault(u=>u.Team==Team.Player&&u.Data.Id==TrialOwner);if(owner==null||!owner.Alive)return false;
                switch(TrialOwner)
                {
                    case "cless":return State.TrialProgress==7&&State.Nodes==7&&owner.Position==ContractSeal;
                    case "mint":case "shionne":return State.TrialActions>=3&&State.Nodes==7&&AtExit;
                    case "farah":return State.Nodes==7&&!Boss.Alive;
                    case "tear":return State.TrialProgress==1&&State.Nodes==7&&!Boss.Alive;
                    case "jade":return State.TrialProgress==1&&DevicesDown&&!Boss.Alive;
                    case "natalia":return State.Nodes==7&&DevicesDown&&AtExit;
                    case "velvet":return State.TrialProgress==1&&!Boss.Alive;
                    case "alphen":return State.TrialProgress==1&&DevicesDown&&!Boss.Alive;
                    case "kisara":return State.TrialProgress==1&&State.Nodes==7&&!Boss.Alive;
                    default:return false;
                }
            }
        }
    }
}
