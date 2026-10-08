namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        void ShowBattleOptions()
        {
            Clear(header);Clear(left);Clear(commands);Clear(footer);
            Label(header,"TALES / TACTICS · 전투 규칙",12,40,25);
            Label(left,"턴 규칙\n자유 선택: 아군 전체 → 적군 전체\n아군 턴에는 미완료 아군을 자유 선택\nSPD 라운드: 라운드마다 속도순\nCT: 속도가 높을수록 자주 행동\n\n적 AI\n역할 AI: 보호·회복·예고·휴식\nUtility: 피해·회복·상태·비용 비교",20,320,18);
            Button(commands,battle.UseCT?"턴 순서: CT":battle.UseFixedSpeedOrder?"턴 순서: SPD 라운드":"턴 순서: 아군 자유 선택",30,()=>{if(battle.UseCT){battle.UseCT=false;battle.UseFixedSpeedOrder=false;}else if(battle.UseFixedSpeedOrder){battle.UseFixedSpeedOrder=false;battle.UseCT=true;}else battle.UseFixedSpeedOrder=true;battle.SaveBattleOptions();ShowBattleOptions();});
            Button(commands,battle.UseUtilityAI?"적 AI: Utility":"적 AI: 기본",86,()=>{battle.UseUtilityAI=!battle.UseUtilityAI;battle.SaveBattleOptions();ShowBattleOptions();});
            Button(commands,"훈련 목표: "+ObjectiveNames.Name(battle.TrainingObjective),142,()=>{battle.TrainingObjective=(ObjectiveKind)(((int)battle.TrainingObjective+1)%5);battle.TrainingMode=true;ShowBattleOptions();});
            Label(commands,"목표 도착: 아군 1명 도착\n호위: 첫 출전 인원 도착, KO 시 패배\n금색 목표 타일: (8,8)\n보스: 금색 HP 막대의 첫 적\n생존: 자유 턴은 적군 턴 4회\nSPD·CT는 아군 턴 종료 12회\n\n캠페인 목표는 장별로 다릅니다.\n훈련은 선택한 목표를 연습합니다.",208,240,17);
            Button(commands,"출전 준비로",465,ShowDeployment);
            Button(commands,battle.TrainingMode?"훈련: Lv25 / 모든 일반 스킬":"캠페인: 저장된 성장 사용",520,()=>{battle.TrainingMode=!battle.TrainingMode;ShowBattleOptions();});
            Button(commands,battle.Campaign.AutoTiming?"파라 타이밍: 자동":"파라 타이밍: 수동",572,()=>{battle.Campaign.AutoTiming=!battle.Campaign.AutoTiming;battle.SaveBattleOptions();ShowBattleOptions();});
            Label(footer,"턴 순서·AI·타이밍 설정은 다음 실행에도 유지됩니다. 훈련은 경험치·골드·장 완료 보상을 저장하지 않습니다.\nUtility 적은 응급 회복과 땅 마법도 사용합니다. 생존 목표는 적 전멸만으로 완료되지 않습니다.",14,88,17);
        }
    }
}
