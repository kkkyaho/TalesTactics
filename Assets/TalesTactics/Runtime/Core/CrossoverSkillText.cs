namespace TalesTactics
{
    public static class CrossoverSkillText
    {
        public static string Shape(SkillAreaShape shape)
        {
            switch(shape){case SkillAreaShape.Line:return "직선 관통";case SkillAreaShape.Cone:return "부채꼴";case SkillAreaShape.Cross:return "십자";case SkillAreaShape.Diagonal:return "대각선 교차";case SkillAreaShape.Lanes:return "한 칸 간격 평행 구역";case SkillAreaShape.Scatter:return "분산 지점";default:return "마름모";}
        }
        public static string Mechanic(BossMechanic kind)
        {
            switch(kind)
            {
                case BossMechanic.Summon:return "연결 장치가 남아 있으면 빈 소환진에 증원 · 소환진 점령으로 차단";
                case BossMechanic.DrainMP:return "대상 주변 적 MP 최대 15 흡수";
                case BossMechanic.CloneNova:return "예고 시 분신 3기 · 모두 격파하면 발동 취소";
                case BossMechanic.LockFacing:return "방어와 이동 고정 · 뒤로 돌아 약점 공격";
                case BossMechanic.CoreRecharge:return "핵 재충전 · 준비 중 피해를 주면 취소";
                case BossMechanic.Replay:return "직전 아군 기술의 범위 형태를 예고 시점에 기록";
                case BossMechanic.Mend:return "마지막 파괴 장치 HP 50% 복구 · 그 자리를 점령하면 차단";
                case BossMechanic.FinalStop:return "귀환 신호 3개 연결 시 공격 대상에서 제외";
                case BossMechanic.FlameField:return "남은 화염 위에서 턴 종료 시 최대 HP 10% 피해";
                case BossMechanic.Sanctuary:return "연결된 신호 주변 1칸은 공격을 막는 보호 구역";
                case BossMechanic.Collapse:return "다음 보스 턴 종료에 표시 지반 붕괴 · 최대 HP 12.5% 피해와 이동 비용 2";
                case BossMechanic.Chivalry:return "구조 신호 주변을 피해서 조준";
                case BossMechanic.Counter:return "빈틈 기간 첫 공격에 예고 반격 · 측면·원거리로 조건을 피할 수 있음";
                case BossMechanic.GaugeSeal:return "봉인 구역의 공격 게이지 획득 억제 · 장치 전부 파괴 시 해제";
                case BossMechanic.Command:return "장치가 남아 있으면 4칸 이내 호위에게 방어 부여";
                case BossMechanic.Cooperate:return "목표를 공격할 수 있는 호위 1기가 추가 지원 공격";
                case BossMechanic.HighWater:return "높이 0의 대상만 피격 · 고지대로 회피";
                case BossMechanic.MarkedStrike:return "표식 중심 밖의 주변 피해는 절반";
                case BossMechanic.IceTrail:return "얼음 위에서 턴 종료 시 이동 속박 1턴";
                case BossMechanic.Mines:return "이동 경로가 지뢰를 지나면 최대 HP 12.5% 피해 후 소멸";
                case BossMechanic.Boundary:return "격자 일부 통행 봉쇄 · 교차하는 안전길 유지";
                case BossMechanic.Contract:return "계약 시련 · 공격·치유·방어와 순서대로 신호 연결";
                default:return "";
            }
        }
    }
}
