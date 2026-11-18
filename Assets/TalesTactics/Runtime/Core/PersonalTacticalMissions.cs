using System;
using UnityEngine;
namespace TalesTactics
{
    // Only the two combat chapters of each personal arc receive a new layout.
    // Index 0/1 always means the west/east story choice; trial predicates and rewards remain elsewhere.
    public static class PersonalTacticalMissions
    {
        static Vector2Int[] P(params int[] xy)
        {
            var result=new Vector2Int[xy.Length/2];
            for(int i=0;i<result.Length;i++)result[i]=new Vector2Int(xy[i*2],xy[i*2+1]);
            return result;
        }
        static Vector2Int[] Center()=>P(5,1,6,1,7,1,5,2,6,2,7,2);
        static Vector2Int[] West()=>P(1,1,2,1,3,1,1,2,2,2,3,2);
        static Vector2Int[] Wings()=>P(2,1,3,1,9,1,10,1,3,2,9,2);
        static TacticalMissionLayout Layout(int stage,string location,string description,string hint,
            Vector2Int[] signals,Vector2Int exit,Vector2Int[] players,Vector2Int[] enemies,
            Func<int,int,int> height,Func<int,int,bool> wall,Func<int,int,bool> water=null)
            =>new TacticalMissionLayout(stage,PersonalScenarios.Title(stage),location,description,
                hint+(water!=null?" · 깊은 물은 통과 불가":""),TacticalMissionGoal.Personal,signals,exit,players,enemies,
                ()=>TacticalMissionLayout.Build(height,wall,water),new Vector2Int(6,4));
        static string Rescue(int stage)=>PersonalScenarios.RouteName(stage,0)+" / "+PersonalScenarios.RouteName(stage,1)+
            " 중 선택한 길부터 구조 신호 3개 연결 후 출구 도착 · 보스 격파 불필요";

        public static TacticalMissionLayout Get(int stage)
        {
            if(!PersonalScenarios.Contains(stage)||PersonalScenarios.Tier(stage)==0)throw new ArgumentOutOfRangeException(nameof(stage));
            switch(stage)
            {
                // Cless: rescuing around a solid ruin contrasts with an ordered contract circuit.
                case 27:return Layout(stage,"무너진 회랑 · 두 갈래 구조로",Rescue(stage),
                    "중앙 잔해 양쪽으로 나뉘어 구조 · 북쪽 합류 신호를 연결한 뒤 남쪽 출발지로 귀환",
                    P(2,8,10,6,7,10),new Vector2Int(6,0),Center(),
                    P(6,11,3,7,9,7,6,10,2,10,10,10,4,11,8,11,6,9),
                    (x,y)=>y>=10?1:0,(x,y)=>x>=5&&x<=7&&y>=5&&y<=8||(x==1||x==11)&&y==4);
                case 28:return Layout(stage,"오리진의 제단 · 계약 회랑",
                    "공격·유효 치유·방어 수행 → 서쪽·동쪽·북쪽 신호 순서대로 연결 → 크레스가 남쪽 계약 문양 도착 · 격파 불필요",
                    "양옆 회랑의 가로 통로로 합류 · 세 가지 행동을 마치고 크레스가 계약 문양으로 돌아오기",
                    P(2,6,10,6,6,11),new Vector2Int(6,12),Center(),
                    P(6,9,2,8,10,8,6,7,2,10,10,10,3,11,9,11,8,3),
                    (x,y)=>y>=9?1:0,(x,y)=>(x==4||x==8)&&y>=5&&y<=10&&y!=7||y==10&&x>=4&&x<=8&&x!=6);

                // Mint: cross-connected treatment rooms, then a sheltered U-shaped ward circuit.
                case 30:return Layout(stage,"갈라진 치료소 · 두 수로",Rescue(stage),
                    "치료소 사이 가로 통로를 확보 · 서쪽과 동쪽 수로의 다리를 건너 북쪽 연락소 연결 후 남쪽 귀환",
                    P(2,5,10,9,6,10),new Vector2Int(6,0),Center(),
                    P(6,11,2,6,10,7,6,8,1,10,11,10,3,11,9,11,6,6),
                    (x,y)=>0,(x,y)=>(x==4||x==8)&&y>=4&&y<=10&&y!=5&&y!=8,
                    (x,y)=>y==7&&x>=1&&x<=3&&x!=2||y==6&&x>=9&&x<=11&&x!=10);
                case 31:return Layout(stage,"푸른 수정 병동 · 돌아오는 기도",
                    "민트가 실제 HP를 회복하는 치유 3회 · 구조 신호 3개 연결 후 남쪽 출구 귀환 · 격파 불필요",
                    "병동 옆길로 부상자와 치유자를 함께 이동 · 만HP 회복은 세지 않음 · 적을 전멸시키기 전에 치유 조건 확인",
                    P(2,5,10,5,6,10),new Vector2Int(6,0),Center(),
                    P(6,8,2,8,10,8,6,6,2,10,10,10,4,11,8,11,6,9),
                    (x,y)=>y>=10?1:0,(x,y)=>y==4&&x>=3&&x<=9&&x!=6||x==3&&y>=5&&y<=9&&y!=7||x==9&&y>=5&&y<=9&&y!=8);

                // Farah: fast low supply road versus raised evacuation road; a terraced elemental loop.
                case 33:return Layout(stage,"피난 능선 · 낮은 보급길",Rescue(stage),
                    "서쪽 피난로와 동쪽 고지를 두 다리로 연결 · 북동쪽 보급을 확인하고 북서쪽 출구로 합류",
                    P(2,9,10,5,8,10),new Vector2Int(1,11),West(),
                    P(9,11,3,6,8,6,7,9,3,10,11,10,7,11,11,11,7,7),
                    (x,y)=>x>=7?1:0,(x,y)=>y==7&&x>=1&&x<=4&&x!=2||y==8&&x>=8&&x<=11&&x!=10,
                    (x,y)=>x==5&&y>=4&&y<=10&&y!=5&&y!=9);
                case 34:return Layout(stage,"맥스웰의 계단 · 원소 순환로",
                    "세 신호를 순회하고 맥스웰 격파 · 파라 생존",
                    "낮은 서쪽에서 동쪽 계단과 북쪽 고지로 순회 · 중앙 다리와 외곽 길로 원소 예고를 나누어 피하기",
                    P(2,5,10,9,4,11),new Vector2Int(6,12),West(),
                    P(9,11,3,7,9,7,6,9,2,9,10,6,7,11,11,11,6,10),
                    (x,y)=>y>=10?2:y>=5?1:0,(x,y)=>y==4&&x>=1&&x<=4&&x!=2||y==10&&x>=8&&x<=11&&x!=10,
                    (x,y)=>x>=4&&x<=8&&y>=6&&y<=8&&x!=6);

                // Tear: bent streets require relay positions; three bridges connect resonance platforms.
                case 36:return Layout(stage,"대피소 골목 · 전열의 노래",Rescue(stage),
                    "서쪽 골목과 동쪽 전열을 두 가로 통로로 연결 · 북쪽 연락소 확인 후 남동쪽 출구로 이동",
                    P(3,9,9,5,6,11),new Vector2Int(12,2),West(),
                    P(9,10,3,6,10,6,7,9,2,10,10,9,4,11,10,11,6,5),
                    (x,y)=>x>=7&&y>=8?1:0,(x,y)=>x==4&&y>=3&&y<=9&&y!=5&&y!=8||y==7&&x>=5&&x<=11&&x!=7&&x!=10);
                case 37:return Layout(stage,"공명 수로 · 세 목소리의 다리",
                    "티아의 홀리 송 사용 · 신호 3개 연결 후 수호자 격파 · 티아 생존",
                    "세 다리로 공명점을 나눠 연결 · 중앙 합류점에서 노래를 사용한 뒤 북쪽 고지로 전진",
                    P(2,8,10,8,6,11),new Vector2Int(6,12),Center(),
                    P(6,7,2,7,10,7,6,8,2,10,10,10,4,11,8,11,6,10),
                    (x,y)=>y>=10?1:0,(x,y)=>(x==4||x==8)&&(y==4||y==8),
                    (x,y)=>y==6&&x>=1&&x<=11&&x!=2&&x!=6&&x!=10||y==9&&x>=3&&x<=9&&x!=6);

                // Jade: linked archive rooms retain evidence; the trial exposes the preparing caster.
                case 39:return Layout(stage,"기록 보관소 · 지우지 않는 이름",Rescue(stage),
                    "장치실과 기록실의 두 출입구 활용 · 북쪽 명단 신호를 연결하고 중앙 통로로 돌아오기",
                    P(2,6,10,8,6,11),new Vector2Int(6,0),Center(),
                    P(6,10,3,7,9,7,6,7,2,10,10,10,4,11,8,11,6,5),
                    (x,y)=>0,(x,y)=>(x==4||x==8)&&y>=4&&y<=10&&y!=6&&y!=9||y==8&&x>=5&&x<=7&&x!=6);
                case 40:return Layout(stage,"봉인 실험실 · 감시 제단",
                    "제이드의 미스틱 케이지로 준비 공격 실제 취소 · 장치 3개 정지 후 집행자 격파 · 제이드 생존",
                    "중앙 문으로 준비 공격을 견제하거나 양옆 장치실부터 진입 · 집행자를 쓰러뜨리기 전에 준비 취소 조건 달성",
                    P(1,9,11,9,6,12),new Vector2Int(12,11),Center(),
                    P(6,10,3,6,9,6,6,7,2,7,10,7,3,11,9,11,6,8),
                    (x,y)=>x>=5&&x<=7&&y>=6?1:0,(x,y)=>y==5&&x>=1&&x<=11&&x!=2&&x!=6&&x!=10||(x==4||x==8)&&y>=7&&y<=10&&y!=9);

                // Natalia: differently sized border crossings; sanctuary islands support a return journey.
                case 42:return Layout(stage,"국경 강변 · 두 행렬의 합류",Rescue(stage),
                    "넓은 서쪽 피난로와 동쪽 고지를 분담 · 북쪽 합류 지점을 거쳐 북동쪽 국경 밖으로 이동",
                    P(2,8,10,6,7,10),new Vector2Int(12,11),West(),
                    P(9,10,3,7,10,8,7,8,2,10,10,10,4,11,8,11,6,11),
                    (x,y)=>x>=9?1:0,(x,y)=>x==6&&y>=6&&y<=10&&y!=8||x==4&&y>=7&&y<=9,
                    (x,y)=>y==5&&x>=1&&x<=11&&x!=2&&x!=3&&x!=9&&x!=10);
                case 43:return Layout(stage,"라시우스의 방벽 · 피난 징검다리",
                    "구조 신호 3개 연결 · 장치 3개 파괴 후 남쪽 출구 도착 · 나탈리아 생존 · 격파 불필요",
                    "두 수로의 세 다리를 나눠 건너기 · 연결된 신호 주변의 보호 구역을 이용해 장치를 처리하고 귀환",
                    P(2,6,10,6,6,10),new Vector2Int(6,0),Center(),
                    P(6,9,2,5,10,5,6,7,3,7,9,7,4,10,8,10,6,11),
                    (x,y)=>x==2||x==6||x==10?1:0,(x,y)=>false,
                    (x,y)=>(y==4||y==8)&&x>=1&&x<=11&&x!=2&&x!=6&&x!=10);

                // Velvet: a hooked pursuit route returns to the injured; the duel has several melee entries.
                case 45:return Layout(stage,"추격의 폐허 · 돌아가는 옆길",Rescue(stage),
                    "꺾인 벽 안쪽의 지름길과 외곽 구조로를 오가기 · 북서쪽 부상자를 확인한 뒤 남동쪽으로 복귀",
                    P(2,5,10,9,3,10),new Vector2Int(12,2),Center(),
                    P(8,10,3,6,10,7,7,8,2,9,11,10,6,11,10,11,6,6),
                    (x,y)=>x<=3&&y>=7?1:0,(x,y)=>y==5&&x>=4&&x<=9&&x!=7||x==4&&y>=6&&y<=10&&y!=8||y==9&&x>=6&&x<=11&&x!=10);
                case 46:return Layout(stage,"시구레의 뜰 · 이름 없는 결투",
                    "벨벳의 클로 흡혈로 실제 HP 회복 후 시구레 격파 · 벨벳 생존",
                    "정면의 넓은 결투장과 양옆 진입로 활용 · 부상한 벨벳이 클로 흡혈로 회복한 뒤 결투 마무리",
                    P(2,10,10,10,6,11),new Vector2Int(6,12),Center(),
                    P(6,8,4,6,8,6,6,5,2,8,10,8,4,11,8,11,6,10),
                    (x,y)=>y>=9?1:0,(x,y)=>(x==3||x==9)&&y>=5&&y<=8&&y!=6||y==9&&x>=4&&x<=8&&x!=5&&x!=7);

                // Alphen: offset prison gates differ from the three-lane command-core assault.
                case 48:return Layout(stage,"포로 구역 · 서로 다른 탈출문",Rescue(stage),
                    "서쪽 수용소와 동쪽 대피로의 엇갈린 문을 통과 · 북쪽 구조 신호 확인 후 남서쪽 퇴로로 귀환",
                    P(3,8,10,6,8,11),new Vector2Int(0,2),Center(),
                    P(6,10,2,6,10,8,6,7,2,10,10,10,4,11,10,11,6,5),
                    (x,y)=>y>=10?1:0,(x,y)=>x==4&&y>=4&&y<=9&&y!=6||x==8&&y>=4&&y<=10&&y!=8||y==8&&x>=1&&x<=3&&x!=3||y==5&&x>=9&&x<=11&&x!=10);
                case 49:return Layout(stage,"명령의 핵 · 세 갈래 화염로",
                    "알펜의 HP 비용을 지불하는 화염검 연계 사용 · 장치 3개 정지 후 집행자 격파 · 알펜 생존",
                    "중앙 핵과 양옆 장치로를 분담 · 화염검 HP 비용을 감당할 치유 동료의 통로를 남기고 북쪽 계단 합류",
                    P(2,10,10,10,6,12),new Vector2Int(12,2),Center(),
                    P(6,10,3,6,9,6,6,9,2,8,10,8,4,11,8,11,6,6),
                    (x,y)=>y>=10?2:y>=7?1:0,(x,y)=>y==8&&x>=5&&x<=7&&x!=6,
                    (x,y)=>(x==4||x==8)&&y>=5&&y<=9&&y!=6&&y!=9);

                // Shionne: staggered crossings create firing lanes; three thin bridges keep support in reach.
                case 51:return Layout(stage,"가시 너머 수로 · 먼 곳의 신호",Rescue(stage),
                    "엇갈린 두 수로의 교량에서 엄호 사선 확보 · 치료소와 피난로를 연결하고 남동쪽 출구로 이동",
                    P(2,9,10,6,8,11),new Vector2Int(12,2),West(),
                    P(10,10,3,5,9,7,6,8,2,10,11,9,4,11,10,11,6,10),
                    (x,y)=>x>=9?1:0,(x,y)=>y==7&&x>=1&&x<=3&&x!=2,
                    (x,y)=>x==4&&y>=3&&y<=10&&y!=5&&y!=9||x==8&&y>=4&&y<=10&&y!=6&&y!=10);
                case 52:return Layout(stage,"환영의 협수로 · 돌아올 약속",
                    "시온의 유효 회복·부활 3회 · 신호 3개 연결 후 남동쪽 출구 귀환 · 시온 생존 · 격파 불필요",
                    "세 교량 사이에서 사격과 치유 거리를 유지 · 실제 HP가 회복된 행동을 세 번 수행하고 동료와 함께 귀환",
                    P(2,8,10,8,6,11),new Vector2Int(12,1),West(),
                    P(6,7,2,6,10,6,6,9,2,10,10,10,4,11,8,11,6,10),
                    (x,y)=>y>=9?1:0,(x,y)=>y==10&&x>=1&&x<=11&&x!=2&&x!=6&&x!=10,
                    (x,y)=>x>=3&&x<=9&&y>=5&&y<=8&&x!=3&&x!=6&&x!=9);

                // Kisara: protect both ends of a convoy, then hold a broad center while allies flank.
                case 54:return Layout(stage,"행렬 집결지 · 앞과 뒤의 약속",Rescue(stage),
                    "서쪽 전방 행렬과 동쪽 후방 보급대를 분담 · 북쪽 집결 신호까지 연결한 뒤 중앙길로 남쪽 귀환",
                    P(2,9,10,5,6,11),new Vector2Int(6,0),Wings(),
                    P(6,10,2,7,10,7,6,8,2,10,10,10,4,11,8,11,6,6),
                    (x,y)=>x>=4&&x<=8&&y>=10?1:0,(x,y)=>(x==4||x==8)&&y>=4&&y<=8&&y!=6||y==9&&x>=3&&x<=9&&x!=6);
                case 55:return Layout(stage,"귀환의 방패 · 중앙 방어선",
                    "방어 중인 키사라가 실제 피해를 받기 · 신호 3개 연결 후 라시우스 격파 · 키사라 생존",
                    "키사라는 중앙의 넓은 길에서 방어하며 공격을 받기 · 동료는 양옆 교량과 가로 통로로 돌아 신호 연결",
                    P(2,7,10,7,6,11),new Vector2Int(6,12),Center(),
                    P(6,9,5,6,7,6,6,7,2,10,10,10,4,11,8,11,6,10),
                    (x,y)=>y>=8?1:0,(x,y)=>(x==4||x==8)&&y>=4&&y<=9&&y!=5&&y!=8,
                    (x,y)=>y==6&&(x>=1&&x<=3&&x!=2||x>=9&&x<=11&&x!=10));
                default:throw new ArgumentOutOfRangeException(nameof(stage));
            }
        }
    }
}
