using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform helpOverlay,helpWindow;
        int helpPage;
        public bool HelpOpen=>helpOverlay!=null&&helpOverlay.gameObject.activeSelf;
        public bool InputModalOpen=>TitleOpen||UnitDetailsOpen||HelpOpen||SystemMenuOpen||MissionOpen;
        public bool CanOpenHelp=>!TitleOpen&&!SystemMenuOpen&&!battle.StoryActive&&!battle.TimingActive&&(battle.Session==null||
            battle.Session.Active?.Team==Team.Player&&!(battle.State is ActionExecutionState));
        static readonly string[] helpTitles={"처음 시작","이동 · 공격","회복 · 대기","높이 · 방향","상태이상","궁극기 · 조건","편성 · 성장","조작 안내","캠페인 · 임무","대화 · 기록","장비 · 전용무기","저장 · 재도전"};
        void DrawHelpEntry(bool preparation)
        {
            Button(header,preparation?"처음 플레이 · 도움말":"도움말",16,()=>ShowHelp(),CanOpenHelp,32);
            var r=(RectTransform)header.GetChild(header.childCount-1);r.anchorMin=r.anchorMax=Vector2.one;r.pivot=Vector2.one;
            r.sizeDelta=new Vector2(preparation?210:78,32);r.anchoredPosition=new Vector2(preparation?-12:-410,-16);
            r.GetComponentInChildren<TMPro.TMP_Text>().fontSize=preparation?18:15;
        }
        public void ShowHelp(int page=0)
        {
            if(!CanOpenHelp)return;CloseMission();CloseUnitDetails();helpPage=Mathf.Clamp(page,0,helpTitles.Length-1);
            if(helpOverlay==null)
            {
                helpOverlay=Panel("HelpOverlay",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
                helpOverlay.GetComponent<UnityEngine.UI.Image>().color=new Color(0,0,0,.82f);
                helpOverlay.GetComponent<UnityEngine.UI.Outline>().enabled=false;
                helpWindow=Panel("HelpWindow",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-470,-325),new Vector2(470,325));
                helpWindow.SetParent(helpOverlay,false);
            }
            helpOverlay.gameObject.SetActive(true);helpOverlay.SetAsLastSibling();SetMainInteraction(false);Clear(helpWindow);
            Label(helpWindow,"플레이 가이드 · "+helpTitles[helpPage],18,40,25);
            for(int i=0;i<helpTitles.Length;i++)
            {
                int pageIndex=i;Button(helpWindow,"도움말: "+helpTitles[i],80+i*38,()=>ShowHelp(pageIndex),true,34);
                var r=(RectTransform)helpWindow.GetChild(helpWindow.childCount-1);r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(16,-80-i*38);r.sizeDelta=new Vector2(194,34);
                var label=r.GetComponentInChildren<TMPro.TMP_Text>();label.text=(i==helpPage?"● ":"")+helpTitles[i];
            }
            var body=Label(helpWindow,HelpBody(helpPage),84,448,21);body.rectTransform.offsetMin=new Vector2(238,body.rectTransform.offsetMin.y);body.rectTransform.offsetMax=new Vector2(-22,body.rectTransform.offsetMax.y);
            body.enableAutoSizing=true;body.fontSizeMin=18;body.fontSizeMax=21;
            Button(helpWindow,"입문 연습 시작",555,()=>{CloseHelp();battle.BeginTutorial();},battle.Session==null,48);HalfButton(helpWindow,0);
            Button(helpWindow,"도움말 닫기",555,()=>CloseHelp(),true,48);HalfButton(helpWindow,1);
            Label(helpWindow,"F1: 도움말 · Esc / 우클릭 / 패드 B: 닫기 · 전투 진행과 선택 상태를 유지합니다.",610,26,15);
        }
        public bool CloseHelp()
        {
            bool open=HelpOpen;if(helpOverlay!=null)helpOverlay.gameObject.SetActive(false);
            SetMainInteraction(!UnitDetailsOpen);return open;
        }
        string HelpBody(int page)
        {
            var r=battle.Catalog.Rules;
            switch(page)
            {
                case 0:return "타이틀에서 새 게임·불러오기로 시작합니다. 거점에서 편성·장비·성장을 정비한 뒤 ‘이야기 시작’을 누르세요. 새 메인 1화는 크레스와 민트로 시작하고 동료들이 순서대로 합류합니다.\n\n처음이라면 아래 ‘입문 연습 시작’을 선택하세요.\n1. 크레스를 목표 타일로 이동\n2. 공격 선택 → 적 클릭으로 사용\n3. 민트의 기술로 크레스 회복\n4. 머리 위 화살표로 대기 방향 확정\n\n입문 연습은 Lv1·기본 장비의 별도 전장입니다. 적은 기다리고, 공격 후 연습용 부상을 줍니다. 성장·골드·장비·편성은 저장하지 않습니다.\n\n연습을 마치면 거점으로 돌아가 캠페인을 시작하세요. Lv25 훈련은 편성의 ‘훈련 · 전투 설정’에서 선택합니다.";
                case 1:return "기본은 아군 턴 → 적군 턴입니다. 명령 대기 중 맵의 아군·상단 초상화·PgUp/PgDn으로 미완료 아군을 선택합니다. 선택을 바꿔도 이동·행동 기록은 유지됩니다.\n\n이동과 행동은 한 번씩, 순서는 자유입니다.\n이동 → 푸른 타일 클릭. 아군 칸을 통과할 수 있지만 빈 칸에만 도착합니다. 선은 경로, 도착 모델은 이동 후 위치를 보여 줍니다.\n\n이동 뒤 행동하기 전에는 ‘이동 취소’나 Z로 돌아갈 수 있습니다.\n공격·기술 → 대상에 마우스를 올려 예측 확인 → 유효한 대상 클릭으로 즉시 사용합니다.\nTab/Shift+Tab으로 대상을 골랐다면 Enter로 사용합니다. 별도 실행 버튼은 없습니다.\n\nEsc·우클릭·메뉴 밖 빈 곳 클릭은 한 단계 취소합니다. 기술 대상 선택을 취소하면 목록으로 돌아갑니다.";
                case 2:return "회복: 기술 → 퍼스트 에이드 등 선택 → 아군 클릭. 행동과 MP를 사용하며 HP는 최대치를 넘지 않습니다. 쓰러진 아군은 부활 기술이 필요합니다.\n\n대기: 머리 위 4방향 화살표 중 하나를 고르면 해당 아군의 턴을 마칩니다. 이동·행동이 남아 있어도 대기할 수 있습니다. 선택한 방향은 금색입니다.\n\n방어: 행동을 사용해 받는 피해를 줄이고 방향을 선택합니다. 정면·측면·후면에 따라 효과가 다릅니다.\n\n‘아군 턴 종료’는 확인 후 미완료 아군을 모두 대기시킵니다. 기존 방향을 유지하며 방어 효과는 주지 않습니다.\n\n아군 전원이 마치면 적군 턴이 시작됩니다. 완료한 아군은 채도가 낮아지고 다음 아군 턴에 돌아옵니다. SPD·CT를 선택한 경우에는 표시된 행동 순서를 따르세요.";
                case 3:return $"MOV는 이동 거리, JMP는 넘을 수 있는 높이 차입니다. 물·일부 잔해는 이동 비용이 더 큽니다.\n\n물리 기술의 높이 보정: 1단계마다 ±{r.HeightDamagePerStep:P0}, 최대 {r.HeightDamageMaxSteps}단계. 활·총 일부 기술은 높이에 따라 사거리도 변합니다. 장애물·시야 제한은 기술 상세를 확인하세요.\n\n공격 방향 배율: 정면 ×{r.FrontMultiplier:0.##} / 측면 ×{r.SideMultiplier:0.##} / 후면 ×{r.RearMultiplier:0.##}.\n방어 중 받는 배율: 정면 ×{r.GuardFront:0.##} / 측면 ×{r.GuardSide:0.##} / 후면 ×{r.GuardRear:0.##}.\n대기·방어 시 머리 위 화살표로 방향을 정합니다.\n\n물리 피해는 민첩 차이당 0.5%, 최대 ±15% 보정됩니다. 적 양옆의 인접 칸에서 포위하면 협공 +10%. 적도 같은 규칙을 쓰며 기절·수면 동료는 협공할 수 없습니다.\n\nV는 적 위험 범위를 켜고 끕니다. 황색 공격 예고와 임무의 지형 위험도 함께 살피세요.";
                case 4:return "‘능력’의 상태창에서 현재 상태와 남은 턴, 기술·장비·특성을 확인합니다. 캐릭터를 조회하거나 모델을 회전해도 전투 선택과 방향은 바뀌지 않습니다.\n\n기절 · 수면: 행동 불가. 수면은 피해로 해제.\n속박: 이동 불가.\n케이지: 이동 불가, 속도·마법 방어 감소.\n송: 힘·마력 증가.\n방어: 바라보는 방향에 따라 피해 감소.\n자동 부활: 쓰러질 때 한 번 회복.\n컨슘 클로: 벨벳의 강화·연계 상태.\n\n회복과 상태 해제는 서로 다른 효과입니다. 기술 상세의 효과·비용·사용 불가 사유를 확인하세요.\n\n보스의 준비·저항·시련 조건은 ‘임무 · 적 정보’와 전장 예고를 확인하세요. 단순 처치만으로 끝나지 않는 임무도 있습니다.";
                case 5:return "궁극기는 해금 레벨·MP·SP·고유 조건을 모두 만족해야 합니다. 기술 목록의 ‘사용 가능’을 ‘전체 조회’로 바꾸면 잠긴 기술의 조건도 읽을 수 있습니다.\n\n"+
                    string.Join("\n",JoinedCharacters().Where(c=>c.UltimateSkill!=null).Select((c,i)=>new{Index=i,Text=c.DisplayName.Split(' ')[0]+" Lv"+c.UltimateSkill.UnlockLevel+" · MP"+c.UltimateSkill.MPCost}).GroupBy(x=>x.Index/2).Select(g=>string.Join("    /    ",g.Select(x=>x.Text))))+
                    $"\n\n파라: 모든 일반 기술 해금 후 사자전후에서 연계. 회전 진행률 {r.TimingWindowStart:P0}–{r.TimingWindowEnd:P0}에 Space/패드 A를 누르세요. 자동 타이밍은 저장 · 설정의 ‘설정’에서 선택하고 저장합니다.\n그 외 연계 조건은 기술 상세를 따르세요.";
                case 6:return $"거점 → 편성: 목록에서 마우스 휠로 아래로 이동합니다. 모델로 조회할 동료를 고른 뒤 출전 추가/제외를 누르세요. 최대 {r.MaxDeployment}명이며 메인에서는 해당 화에 합류한 동료만 출전합니다. 병과 필터·정렬·추천 편성·전체 해제도 사용할 수 있습니다.\n\n프리셋은 편성과 장비를 3슬롯에 저장합니다.\n거점 → 성장: 레벨·EXP·승급·전술 특성을 확인하고, Lv10부터 성장 방향을 선택합니다.\n\n합류 훈련은 선택한 해금 임무의 권장 진입 레벨까지 올립니다. Lv25 전투 훈련과 달리 성장에 저장됩니다. 승급은 캐릭터의 레벨 조건과 메인 6화 완료가 필요합니다. 이전 저장의 승급 조건 충족도 인정합니다.\n\n공격·유효 회복·지원·처치로 EXP를 얻고 100마다 레벨업합니다. 승리 시 성장과 임무 EXP를 확정합니다. 재도전·저장 없이 나가면 이번 전투 성장은 취소됩니다.";
                case 7:return "마우스: 버튼·타일 클릭 / 우클릭: 취소\n전장 위 휠: 확대·축소\n\nM 이동 / A 공격 / S 기술 / G 방어 / W 대기\nZ 이동 취소 / V 위험 범위\n방향키: 타일 선택 / Enter: 선택·사용\nPgUp/PgDn: 자유 턴의 미완료 아군 선택\nTab/Shift+Tab: 대상·메뉴 선택 순환\nQ/E: 카메라 회전 / Home: 전체 보기\nF1: 도움말 / F5: 메뉴 / Esc: 취소\nSpace: 파라 타이밍·대화의 다음 진행\n\n패드: 왼쪽 스틱 커서 / 방향키 메뉴 순환\nA/× 선택·타이밍 / B/○ 취소\nLB/RB 회전 / LT/RT 대상 순환\n오른쪽 스틱 확대·축소 / R3 전체 보기";
                case 8:return "현재 캠페인은 ‘경계를 잇는 의지’ 메인 20화와 열 동료의 개인 이야기 30단계입니다. 기존 6장 캠페인은 출전·이어하기에서 제외됩니다. 이전 성장·장비·완료 이력은 보존됩니다.\n\n출전 준비에서 다음 메인 임무를 자동으로 표시합니다.\n같은 화면의 ‘동료의 이야기’에서 개인 이야기를 고릅니다.\n개인 이야기 → 구조 순서 선택 → 전용 무기 시련의 세 단계이며, 메인 진행과 앞 단계 완료에 따라 열립니다. 화면에서 필요한 완료 화를 확인하세요.\n\n첫 단계는 대화, 나머지는 전투입니다. 구조 순서가 달라도 두 길을 모두 돕습니다. 완료 보상은 한 번만 받으며 전용무기 없이도 메인 결말에 도달할 수 있습니다.\n\n출전 준비의 ‘목표’·‘적 정보’, 전투 중 ‘임무 · 적 정보’에서 승패 조건·구조 신호·장치·보스 대응을 확인하세요.";
                case 9:return "거점의 ‘이야기 시작’으로 임무 대화와 전투를 진행합니다. 초상화 옆에 화자와 대사, 현재 진행 수가 표시됩니다.\n\n‘다음’ 또는 ‘이야기 마치기’를 누릅니다. 기본 다음 버튼 선택 상태에서는 Space도 사용할 수 있습니다. Tab으로 메뉴를 고르고 Enter로 실행할 수 있습니다.\n\n‘대화 기록’은 현재 이야기에서 읽은 대사를 보여 줍니다. ‘자동 OFF/ON’으로 자동 진행을 바꾸며, 창이 비활성화되면 자동 진행이 멈춥니다.\n\n전투 전 이야기 → 전투 → 결과의 계속 → 전투 후 이야기 → 다음 장 준비 순서로 진행합니다. 후일담은 읽은 위치를 저장하며, 다시 실행하면 이어집니다. 보상은 중복 지급하지 않습니다.";
                case 10:return "거점 → 장비: 캐릭터 → 무기·방어구·장신구 슬롯 → 후보 선택 → ‘장착하고 저장’. 현재 장비와 후보, 변경 능력치를 나란히 비교합니다. 캐릭터를 바꿔도 미리보기는 유지됩니다. 취소를 누르면 해당 캐릭터의 초안을 버립니다.\n\n개인 이야기의 마지막 시련에서 전용무기를 얻습니다. 열 명에게 각자 한 종류가 있으며 해당 소유자만 장착할 수 있습니다. 고유 효과는 장비 설명을 확인하세요.\n\n전용무기를 선택하면 장비 화면에서 외형을 미리 봅니다. 저장하면 편성·상태창·전투 동작·이동 도착 모델에 반영됩니다. 해제하거나 일반 무기로 바꾸면 기본 외형으로 돌아갑니다.\n\n상점의 ‘구매 · 바로 장착’은 선택한 합류 동료에게 구매·장착하고 함께 저장합니다. 구매만 하거나 보유 장비를 장착할 수도 있습니다. 재고는 장착 중인 수량까지 파티가 공유합니다. 장착된 수량은 팔 수 없고 전용무기는 상점 거래 대상이 아닙니다. 저장 실패 시 기존 장비·재고를 유지합니다.";
                case 11:return "거점의 ‘저장 · 설정’ 또는 전투 중 F5 → ‘저장’에서 3개 슬롯을 관리합니다. 준비 중 ‘현재 슬롯 저장’으로 편성·선택 임무를 저장합니다. 보상·거래·장비·승급 등은 확정 시 저장됩니다.\n\n전투 중 아군 명령 대기 → ‘저장 후 나가기’. 거점에서 ‘중단 전투 이어하기’를 누릅니다. 이어한 뒤 나갈 때도 다시 저장하세요. 기존 6장 중단 기록은 재개할 수 없습니다.\n\n되돌리기: 전투당 3회, 최근 행동 전으로 돌아가며 이후 적 행동·확률도 되돌립니다. 횟수와 기록은 중단 저장에 남습니다. 훈련·연출 중·승리 후에는 사용할 수 없습니다.\n\n재도전: 출전 당시 편성·장비·성장·규칙으로 다시 시작합니다. 저장 없이 나가면 이번 전투 진행은 버립니다.\n\n저장 실패 안내가 있으면 재시도하세요. 입문 연습·Lv25 훈련은 성장·보상을 저장하지 않습니다.";
                default:return "왼쪽에서 도움말 항목을 선택하세요.";
            }
        }
        void ApplyTutorialCommands()
        {
            if(!battle.TutorialActive)return;
            foreach(var b in commands.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                if(b.name=="Move / 이동")b.interactable&=battle.TutorialAllows(TutorialStep.Movement);
                else if(b.name=="Attack / 공격")b.interactable&=battle.TutorialAllows(TutorialStep.Attack);
                else if(b.name=="Skill / 스킬")b.interactable&=battle.TutorialAllows(TutorialStep.Healing);
                else if(b.name=="Wait / 방향 선택")b.interactable&=battle.TutorialAllows(TutorialStep.Waiting);
                else if(b.name=="Guard / 가드"||b.name=="Undo Move / 이동 취소")b.interactable=false;
            }
        }
    }
}
