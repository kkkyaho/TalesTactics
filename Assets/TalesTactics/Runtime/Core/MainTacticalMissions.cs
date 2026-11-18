using System;
using UnityEngine;
namespace TalesTactics
{
    // Each encounter is authored independently. No actor or objective silently clears terrain.
    public static class MainTacticalMissions
    {
        static Vector2Int[] Points(params int[] coordinates)
        {
            var result=new Vector2Int[coordinates.Length/2];
            for(int i=0;i<result.Length;i++)result[i]=new Vector2Int(coordinates[i*2],coordinates[i*2+1]);
            return result;
        }
        static TacticalMissionLayout Layout(int stage,string location,string description,string hint,TacticalMissionGoal goal,
            Vector2Int[] signals,Vector2Int exit,Vector2Int[] players,Vector2Int[] enemies,
            Func<int,int,int> height,Func<int,int,bool> wall,Func<int,int,bool> water=null)
            =>new TacticalMissionLayout(stage,CrossoverCampaign.Episode(stage).Title,location,description,hint,goal,
                signals,exit,players,enemies,()=>TacticalMissionLayout.Build(height,wall,water));

        public static TacticalMissionLayout Get(int stage)
        {
            if(!CrossoverCampaign.IsMain(stage)||stage<CrossoverCampaign.FirstStage+3)throw new ArgumentOutOfRangeException(nameof(stage));
            switch(CrossoverCampaign.Episode(stage).Number)
            {
                case 4:
                    return Layout(stage,"관측소 · 교차 서가",
                        "북동 관측 신호 1개 연결 후 애쉬 저지",
                        "서쪽 입구에서 교차 서가로 진입 · 가로 통로 (4,6)/(8,8) 이용 · 북동 신호 (10,9) 확보 · 애쉬의 정면 반격을 피해 측면 공략",
                        TacticalMissionGoal.SignalsBoss,Points(10,9),new Vector2Int(12,6),
                        Points(1,1,2,1,3,1,1,2,2,2,3,2),Points(6,9,5,5,10,7,6,7,2,9,10,11,6,10,9,10,6,11),
                        (x,y)=>x>=9&&y>=6?1:0,(x,y)=>(x==4&&y>=3&&y<=10&&y!=6)||(x==8&&y>=3&&y<=10&&y!=8));
                case 5:
                    return Layout(stage,"추격로 · 두 구출 통로",
                        "서쪽·동쪽 구출 신호 2개 연결 후 오스카 저지",
                        "신호 (2,7)/(11,9)를 양쪽에서 확보 · 중앙 지휘 범위와 호위를 분리 · 동쪽 고지는 원거리 엄호에 활용",
                        TacticalMissionGoal.SignalsBoss,Points(2,7,11,9),new Vector2Int(6,0),
                        Points(5,1,6,1,7,1,5,2,6,2,7,2),Points(7,10,3,6,10,6,6,7,3,9,10,10,6,10,9,11,7,8),
                        (x,y)=>x>=9?1:0,(x,y)=>(x==5&&y>=3&&y<=9&&y!=5&&y!=8)||(x==8&&y>=4&&y<=10&&y!=7));
                case 6:
                    return Layout(stage,"착취 시설 · 세 격리실",
                        "착취 장치 3개 파괴 후 데미텔 저지",
                        "남동 입구에서 두 문 (3,5)/(9,5)으로 진입 · 장치 (2,8)/(10,8)/(7,10) 정지 · 소환진을 점유하거나 장치를 먼저 파괴",
                        TacticalMissionGoal.DevicesBoss,Points(),new Vector2Int(6,0),
                        Points(9,1,10,1,11,1,9,2,10,2,11,2),Points(3,10,9,6,10,9,3,7,2,8,10,8,2,11,4,11,7,10),
                        (x,y)=>y>=9?1:0,(x,y)=>(y==5&&x>=1&&x<=11&&x!=3&&x!=9)||(x==6&&y>=6&&y<=11&&y!=8));
                case 7:
                    return Layout(stage,"국경 강변 · 두 교량",
                        "민간인 구조 신호 3개 연결 후 라시우스와 대립 종료",
                        "깊은 물은 통과 불가 · 강을 건너는 교량 (6,5)/(6,9) 선택 · 신호 (1,8)/(11,8)/(7,11) 연결 · 양쪽 고지의 주민을 보호",
                        TacticalMissionGoal.SignalsBoss,Points(1,8,11,8,7,11),new Vector2Int(6,1),
                        Points(1,1,2,1,3,1,9,1,10,1,11,1),Points(8,10,3,5,9,9,7,6,2,10,10,10,4,11,8,11,7,3),
                        (x,y)=>x<=3||x>=9?1:0,(x,y)=>(x==2||x==10)&&y==7,(x,y)=>x==6&&y>=2&&y!=5&&y!=9);
                case 8:
                    return Layout(stage,"구원 시설 · 분신 고리",
                        "분신 연결 장치 3개 파괴 후 가나벨트 저지",
                        "고리형 벽의 남문과 옆문으로 진입 · 장치 (2,9)/(10,9)/(6,11) 분산 공략 · 중앙 분신을 제거하거나 소환 자리를 점유해 긴 시전 저지",
                        TacticalMissionGoal.DevicesBoss,Points(),new Vector2Int(6,0),
                        Points(4,1,5,1,6,1,7,1,8,1,6,2),Points(6,8,3,6,9,6,7,7,2,9,10,9,5,7,7,9,6,11),
                        (x,y)=>x>=5&&x<=7&&y>=5&&y<=9?1:0,
                        (x,y)=>((x==4||x==8)&&y>=4&&y<=10&&y!=6&&y!=9)||((y==4||y==10)&&x>=4&&x<=8&&x!=6));
                case 9:
                    return Layout(stage,"침묵의 성역 · 남쪽 퇴로",
                        "봉인주 3개 파괴 · 구조 신호 3개 연결 후 남쪽 퇴로 도착 · 아르토리우스 격파 불필요",
                        "좌우 문을 통해 북진 · 봉인주 (2,8)/(10,8)/(6,8)와 신호 (1,7)/(11,7)/(6,11) 확보 · 남쪽 (6,0)로 복귀",
                        TacticalMissionGoal.DevicesSignalsExit,Points(1,7,11,7,6,11),new Vector2Int(6,0),
                        Points(5,1,6,1,7,1,5,2,6,2,7,2),Points(6,10,2,6,10,6,8,8,2,8,10,8,4,11,8,11,6,8),
                        (x,y)=>y>=10?1:0,(x,y)=>(y==5&&x!=2&&x!=3&&x!=9&&x!=10)||(y==9&&x>=2&&x<=10&&x!=4&&x!=8));
                case 10:
                    return Layout(stage,"충돌하는 경계핵 · 원형 제단",
                        "중앙 제단의 다오스 저지 · 직선 레이저와 근접 충격파 회피",
                        "중앙 제단의 양쪽으로 전열 분산 · 외곽 고지에서 엄호하거나 낮은 고리길로 측면 진입 · 직선 예고에서 옆 칸으로 이탈",
                        TacticalMissionGoal.Boss,Points(),new Vector2Int(6,12),
                        Points(4,1,5,1,6,1,7,1,8,1,6,2),Points(6,7,4,5,9,8,3,8,2,7,10,7,5,10,7,10,6,11),
                        (x,y)=>Math.Abs(x-6)+Math.Abs(y-7)<=2||Math.Abs(x-6)+Math.Abs(y-7)>=7?1:0,
                        (x,y)=>(x==2||x==10)&&(y==4||y==10));
                case 11:
                    // CrossoverFronts owns reserve membership and the sequential activation.
                    // Its fixed western/eastern actor coordinates are deliberately retained.
                    return Layout(stage,"갈라진 전선 · 서쪽 중계로와 동쪽 사선",
                        "서쪽 자미르 저지 후 동쪽 리그렛 저지 · 진행 중인 전선의 아군 생존",
                        "서쪽 부대는 꺾인 중계로를 확보 · 자미르 격파 뒤 동쪽 부대 활성화 · 동쪽 엄폐물 사이에서 리그렛의 사선을 끊기",
                        TacticalMissionGoal.SplitFronts,Points(),new Vector2Int(6,12),
                        Points(1,1,2,1,1,2,9,1,10,1,9,2),Points(3,11,3,7,9,7,3,8,10,11,10,10,4,11,8,11,2,10),
                        (x,y)=>y>=9?1:0,(x,y)=>(x==6&&y>=3&&y<=11)||(y==5&&x>=1&&x<=4&&x!=2)||(x==8&&y==5)||(x==10&&y==8));
                case 12:
                    return Layout(stage,"복제 성역 · 중앙 상승로",
                        "복제 장치 3개 정지 후 중앙 제단의 반 격파",
                        "양쪽 회랑 장치 (2,9)/(10,9)와 북쪽 장치 (6,12) 정지 · 중앙 높은 길과 낮은 측면을 오가며 부채꼴 공격 분산",
                        TacticalMissionGoal.DevicesBoss,Points(),new Vector2Int(6,0),
                        Points(4,1,5,1,6,1,7,1,8,1,6,2),Points(6,10,3,6,9,6,6,7,2,9,10,9,5,11,7,11,6,12),
                        (x,y)=>x>=5&&x<=7&&y>=4?(y>=10?2:1):y>=8?1:0,
                        (x,y)=>(x==4||x==8)&&y>=3&&y<=9&&y!=6);
                case 13:
                    return Layout(stage,"두 피난로 · 합류의 언덕",
                        "선택한 피난로부터 구조 신호 3개 연결 후 시구레와 결투",
                        "깊은 물은 통과 불가 · 선택한 서쪽 (2,9) 또는 동쪽 (10,9) 신호부터 연결 · 반대편도 돕고 중앙 (6,11)에서 합류 · 시구레 저지",
                        TacticalMissionGoal.SignalsBoss,Points(2,9,10,9,6,11),new Vector2Int(6,0),
                        Points(2,1,3,1,4,1,8,1,9,1,10,1),Points(6,12,3,8,9,8,5,10,1,10,11,10,4,11,8,11,7,10),
                        (x,y)=>(x<=3||x>=9)&&y>=7?1:0,(x,y)=>x==6&&y>=7&&y<=10,
                        (x,y)=>y==6&&x!=2&&x!=3&&x!=9&&x!=10);
                case 14:
                    return Layout(stage,"마나의 계단 · 증거를 지키는 길",
                        "엇갈린 계단을 넘어 다오스 저지 · 준비 공격의 직선에서 이탈",
                        "서쪽 문 (2,4)/(3,4) → 동쪽 문 (9,7)/(10,7) → 북쪽 문 (5,10)/(6,10) · 높이 0/1/2의 계단을 이용해 레이저 사선 분리",
                        TacticalMissionGoal.Boss,Points(),new Vector2Int(12,12),
                        Points(1,1,2,1,3,1,1,2,2,2,3,2),Points(9,11,2,5,10,8,6,9,1,8,11,5,9,12,11,11,5,11),
                        (x,y)=>y>=9?2:y>=5?1:0,
                        (x,y)=>(y==4&&x<=8&&x!=2&&x!=3)||(y==7&&x>=4&&x!=9&&x!=10)||(y==10&&x<=8&&x!=5&&x!=6));
                case 15:
                    return Layout(stage,"경계 기록고 · 봉쇄 회랑",
                        "북쪽 귀환 좌표 신호 1개 확보 후 경계의 집행관 저지",
                        "북쪽 기록 신호 (6,11) 확보 · 중앙 봉쇄가 예고되면 가로 연결로로 측면 고지 이동 · 집행관이 방향을 고정하면 뒤로 진입",
                        TacticalMissionGoal.SignalsBoss,Points(6,11),new Vector2Int(6,0),
                        Points(5,1,6,1,7,1,5,2,6,2,7,2),Points(6,8,2,5,10,5,6,6,2,10,10,10,5,10,7,10,6,12),
                        (x,y)=>x<4||x>8?1:0,
                        (x,y)=>((x==4||x==8)&&y>=2&&y<=11&&y!=4&&y!=8&&y!=11)||((x==5||x==7)&&y==6));
                case 16:
                    return Layout(stage,"마지막 피난 행렬 · 굽은 제방",
                        "선두 출전 아군 생존 · 구조 신호 3개 연결 후 북서쪽 출구 도착 · 히아데스 격파 불필요",
                        "깊은 물은 통과 불가 · 높은 제방과 가로길로 신호 (10,5)/(6,8)/(2,10) 연결 · 선두 출전 아군을 보호하며 북서쪽 (1,12)로 호위",
                        TacticalMissionGoal.SignalsExit,Points(10,5,6,8,2,10),new Vector2Int(1,12),
                        Points(6,1,5,1,7,1,5,2,6,2,7,2),Points(10,9,6,4,2,7,10,7,3,8,11,8,4,11,8,11,6,11),
                        (x,y)=>(x==2||x==3||x==6||x==7||x==10||x==11)&&y>=3&&y<=10?1:0,(x,y)=>false,
                        (x,y)=>y>=4&&y<=9&&y!=6&&y!=9&&x!=2&&x!=3&&x!=6&&x!=7&&x!=10&&x!=11);
                case 17:
                    return Layout(stage,"귀환 거점 · 두 수로의 반격",
                        "동쪽 귀환 거점의 볼란 격파 · 물결을 높은 땅에서 피하고 얼음 예고에서 이탈",
                        "깊은 물은 통과 불가 · 교량은 남북 두 줄 (y=5/9) · 높이 0의 중앙 저지는 물결에 노출 · 양쪽 높은 둑과 교량으로 공격 방향 전환",
                        TacticalMissionGoal.Boss,Points(),new Vector2Int(6,12),
                        Points(1,1,2,1,3,1,1,2,2,2,3,2),Points(10,10,3,5,9,6,6,8,2,10,10,7,5,11,7,11,6,10),
                        (x,y)=>x<=3||x>=9||y==5||y==9?1:0,(x,y)=>(x==2&&y==8)||(x==10&&y==4),
                        (x,y)=>(x==4||x==8)&&y>=2&&y<=11&&y!=5&&y!=9);
                case 18:
                    return Layout(stage,"환영 성역 · 세 보호 구역",
                        "환영의 보호 신호 3개 해방 후 시젤 격파",
                        "신호 (2,6)/(10,6)/(6,11)를 연결하면 주변이 오의 보호 구역 · 여러 문을 오가며 화염을 피하고 양쪽 보호 구역에서 북쪽으로 전진",
                        TacticalMissionGoal.SignalsBoss,Points(2,6,10,6,6,11),new Vector2Int(6,0),
                        Points(4,1,5,1,6,1,7,1,8,1,6,2),Points(6,9,2,5,10,5,6,7,1,10,11,10,4,11,8,11,6,12),
                        (x,y)=>y>=10?1:0,
                        (x,y)=>(y==4&&x>=1&&x<=11&&x!=2&&x!=6&&x!=10)||(y==8&&x>=1&&x<=11&&x!=1&&x!=4&&x!=8&&x!=11));
                case 19:
                    return Layout(stage,"영원의 요새 · 봉인망 삼문",
                        "봉인망 장치 3개 파괴 후 아르토리우스 격파",
                        "좌우 입구로 전열을 나누어 장치 (2,8)/(10,8) 정지 · 북쪽 세 문 (2,9)/(6,9)/(10,9)으로 합류해 중앙 장치 (6,10)와 아르토리우스 공략",
                        TacticalMissionGoal.DevicesBoss,Points(),new Vector2Int(6,12),
                        Points(5,1,6,1,7,1,5,2,6,2,7,2),Points(6,11,2,6,10,6,6,8,2,8,10,8,4,11,8,11,6,10),
                        (x,y)=>y>=10?2:y>=6?1:0,
                        (x,y)=>(y==5&&x!=2&&x!=3&&x!=9&&x!=10)||(y==9&&x!=2&&x!=6&&x!=10));
                case 20:
                    return Layout(stage,"경계핵 · 세 귀환의 약속",
                        "귀환 신호 3개 연결 · 봉쇄 장치 3개 파괴 · 오르도 3단계 결전",
                        "귀환 신호 (1,9)/(11,9)/(6,3)와 장치 (2,10)/(10,10)/(6,11) 연결 차단 · 중앙 핵은 양쪽 길로 접근 · 복구된 장치를 다시 끊고 세 단계를 모두 돌파",
                        TacticalMissionGoal.Finale,Points(1,9,11,9,6,3),new Vector2Int(6,12),
                        Points(4,1,5,1,6,1,7,1,8,1,6,2),Points(6,8,2,6,10,6,6,5,2,10,10,10,5,10,7,10,6,11),
                        (x,y)=>Math.Abs(x-6)+Math.Abs(y-8)<=2?2:Math.Abs(x-6)+Math.Abs(y-8)<=4?1:0,
                        (x,y)=>(x==3||x==9)&&y>=3&&y<=10&&y!=5&&y!=8);
                default:throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }
    }
}
