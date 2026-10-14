using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    [Serializable] public sealed class CrossoverBattleState
    {
        public int Nodes,Segment,Choice,Actions;
        public List<CrossoverHazard> Hazards=new List<CrossoverHazard>();
        public int LastShape,LastArea,LastRange=4;
        public int ReplayShape,ReplayArea;
        public int TrialProgress,TrialActions;
        public List<int> BrokenDevices=new List<int>();
        public void RecordDevice(UnitRuntime unit,IReadOnlyList<UnitRuntime> units)
        {
            if(unit.Data.Id!="boundary-device")return;
            int index=-1;for(int i=0;i<units.Count;i++)if(units[i]==unit){index=i;break;}
            if(unit.Alive)BrokenDevices.Remove(index);else if(index>=0&&!BrokenDevices.Contains(index))BrokenDevices.Add(index);
        }
        public CrossoverBattleState Copy()=>new CrossoverBattleState{Nodes=Nodes,Segment=Segment,Choice=Choice,Actions=Actions,LastShape=LastShape,LastArea=LastArea,LastRange=LastRange,ReplayShape=ReplayShape,ReplayArea=ReplayArea,TrialProgress=TrialProgress,TrialActions=TrialActions,BrokenDevices=new List<int>(BrokenDevices??new List<int>()),Hazards=(Hazards??new List<CrossoverHazard>()).Select(h=>h.Copy()).ToList()};
    }
    public sealed partial class CrossoverBattle : IVictoryCondition
    {
        public static readonly Vector2Int[] Nodes={new Vector2Int(2,7),new Vector2Int(10,7),new Vector2Int(6,10)};
        public static readonly Vector2Int Exit=new Vector2Int(6,12);
        readonly BattleSession battle;
        readonly int episode;
        readonly bool personal;
        public CrossoverBattleState State {get;private set;}=new CrossoverBattleState();
        public UnitRuntime Boss=>battle.Units.First(u=>u.Team==Team.Enemy);
        UnitRuntime[] Enemies=>battle.Units.Where(u=>u.Team==Team.Enemy).ToArray();
        public CrossoverBattle(BattleSession battle)
        {
            this.battle=battle;personal=PersonalScenarios.Contains(battle.CampaignStage);
            episode=personal?0:CrossoverCampaign.Episode(battle.CampaignStage).Number;
            foreach(var u in Enemies){u.CrossoverCombat=true;u.BossTimeRules=true;}
            Boss.CrossoverBoss=true;Boss.ExperienceBoss=true;Boss.BossPhase=1;
            Boss.CurrentHP=Boss.Stats.HP;
            var enemies=Enemies;
            for(int i=6;i<Math.Min(8,enemies.Length);i++){enemies[i].Summoned=true;Deactivate(enemies[i]);}
            if(Boss.Data.Id=="ganabelt"){enemies[3].Summoned=true;Deactivate(enemies[3]);}
            if(episode==11&&enemies.Length>4){enemies[4].CrossoverBoss=true;enemies[4].BossPhase=1;Deactivate(enemies[4]);}
            if(episode==11)PrepareFronts();
        }
        void Deactivate(UnitRuntime unit){unit.CurrentHP=0;if(battle.Grid[unit.Position]?.Occupant==unit)battle.Grid[unit.Position].Occupant=null;}
        public void Restore(CrossoverBattleState state)
        {
            if(state==null||state.Nodes<0||state.Nodes>7||state.Segment<0||state.Segment>1||state.Choice<0||state.Choice>1||state.Actions<0||state.Actions>100000||episode!=11&&state.Segment!=0)throw new System.IO.InvalidDataException("Invalid crossover objective state");
            State=state.Copy();
            if(episode!=11&&battle.Units.Any(u=>u.CrossoverReserve))throw new System.IO.InvalidDataException("Reserve outside split-front mission");
            ValidateHazards();battle.Resolver.CrossoverState=State;RefreshBoundaries();
        }
        public void Update()
        {
            foreach(var unit in battle.Units)State.RecordDevice(unit,battle.Units);
            bool ordered=personal&&PersonalScenarios.Tier(battle.CampaignStage)==1||episode==13;
            bool contract=IsTrial&&TrialOwner=="cless";
            for(int i=0;i<Nodes.Length;i++)if((!ordered||State.Nodes!=0||i==State.Choice)&&(!contract||(State.Nodes&((1<<i)-1))==((1<<i)-1))&&battle.Units.Any(u=>u.Team==Team.Player&&u.Alive&&!u.CrossoverReserve&&u.Position==Nodes[i]))State.Nodes|=1<<i;
            if(episode==11&&State.Segment==0&&!Boss.Alive)
            {
                ActivateEasternFront();
            }
        }
        public void OnTurnEnded(UnitRuntime unit){if(unit.Team==Team.Player&&unit.Alive)State.Actions++;ApplyHazards(unit);CompleteWesternTurn(unit);Update();}
        bool DevicesDown=>!Enemies.Any(u=>u.Data.Id=="boundary-device"&&u.Alive);
        bool AtExit=>battle.Units.Any(u=>u.Team==Team.Player&&u.Alive&&u.Position==Exit);
        public string Description
        {
            get
            {
                if(personal)
                {
                    var arc=PersonalScenarios.Arc(battle.CampaignStage);
                    return PersonalScenarios.Tier(battle.CampaignStage)==1?"선택한 길부터 구조 신호 연결 ("+NodeCount+"/3) 후 출구 도착":TrialDescription;
                }
                return MainDescription(episode,Boss.Data.DisplayName,State.Segment);
            }
        }
        public static string MainDescription(int episode,string bossName,int segment=0)
        {
                switch(episode)
                {
                    case 3:return "마을 보호 · 구조 신호 3개 연결 후 출구 도착";
                    case 6:return "착취 장치 파괴 후 데미텔 저지";
                    case 7:return "민간인 구조 신호 3개 연결 · 라시우스와 대립 종료";
                    case 8:return "분신 연결 장치 파괴 후 가나벨트 저지";
                    case 9:return "봉인주 3개 파괴 · 구조 신호 3개 연결 · 출구 탈출 (아르토리우스 격파 불필요)";
                    case 11:return segment==0?"서쪽 전선: 자미르 저지 → 동쪽 전선 합류":"동쪽 전선: 리그렛 저지 · 두 부대 합류";
                    case 12:return "복제 장치 정지 후 반 격파";
                    case 13:return "두 피난로의 신호 연결 후 시구레와 결투";
                    case 16:return "마지막 피난 행렬 호위 · 선두 아군 생존 · 구조 신호와 출구 연결";
                    case 18:return "환영 신호 3개 해방 후 시젤 격파";
                    case 19:return "봉인망 파괴 후 아르토리우스 격파";
                    case 20:return "귀환 신호 3개 연결 · 봉쇄 장치 파괴 · 오르도 3단계 결전";
                    default:return bossName+" 저지 · 예고 공격의 범위를 벗어나거나 준비를 취소";
                }
        }
        public int NodeCount=>Enumerable.Range(0,3).Count(i=>(State.Nodes&(1<<i))!=0);
        public string DefeatDescription=>episode==11?"진행 중인 전선의 아군 전원 전투불능":episode==16?"선두 출전 아군 전투불능 또는 아군 전멸":"아군 전원 전투불능";
        public string HazardDescription=>State.Hazards.Count==0?"":string.Join(" · ",State.Hazards.GroupBy(h=>h.Kind).Select(g=>(g.Key==BossMechanic.Collapse?"붕괴 예고/잔해":g.Key==BossMechanic.Mines?"지뢰":g.Key==BossMechanic.FlameField?"화염":g.Key==BossMechanic.IceTrail?"얼음":g.Key==BossMechanic.Boundary?"통행 봉쇄":"게이지 봉인")+" "+g.Count()+"칸"));
        public bool UsesNodes=>HasSignals(battle.CampaignStage);
        public bool UsesExit=>HasExit(battle.CampaignStage);
        public bool UsesContract=>IsTrial&&TrialOwner=="cless";
        public static bool HasSignals(int stage)
        {
            if(PersonalScenarios.Contains(stage))return PersonalScenarios.Tier(stage)==1||new[]{"cless","mint","farah","tear","natalia","shionne","kisara"}.Contains(PersonalScenarios.Arc(stage).Owner);
            return CrossoverCampaign.IsMain(stage)&&new[]{3,7,9,13,16,18,20}.Contains(CrossoverCampaign.Episode(stage).Number);
        }
        public static bool HasExit(int stage)
        {
            if(PersonalScenarios.Contains(stage))return PersonalScenarios.Tier(stage)==1||new[]{"mint","natalia","shionne"}.Contains(PersonalScenarios.Arc(stage).Owner);
            return CrossoverCampaign.IsMain(stage)&&new[]{3,9,16}.Contains(CrossoverCampaign.Episode(stage).Number);
        }
        public BattleResult Evaluate(IReadOnlyList<UnitRuntime> units)
        {
            if(!units.Any(u=>u.Team==Team.Player&&u.Alive)||episode==16&&!units.First(u=>u.Team==Team.Player).Alive)return BattleResult.Defeat;
            if(episode==11&&FrontDefeated)return BattleResult.Defeat;
            bool win;
            if(personal)win=PersonalScenarios.Tier(battle.CampaignStage)==1?State.Nodes==7&&AtExit:TrialComplete;
            else switch(episode)
            {
                case 3:case 16:win=State.Nodes==7&&AtExit;break;
                case 6:case 8:case 12:case 19:win=DevicesDown&&!Boss.Alive;break;
                case 7:case 13:case 18:win=State.Nodes==7&&!Boss.Alive;break;
                case 9:win=DevicesDown&&State.Nodes==7&&AtExit;break;
                case 11:win=State.Segment==1&&!Boss.Alive&&!Enemies[4].Alive;break;
                case 20:win=State.Nodes==7&&DevicesDown&&!Boss.Alive&&Boss.BossPhase==3;break;
                default:win=!Boss.Alive;break;
            }
            return win?BattleResult.Victory:BattleResult.Ongoing;
        }
    }
}
