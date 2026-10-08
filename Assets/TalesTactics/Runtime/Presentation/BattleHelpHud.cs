using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform helpOverlay,helpWindow;
        int helpPage;
        public bool HelpOpen=>helpOverlay!=null&&helpOverlay.gameObject.activeSelf;
        public bool InputModalOpen=>UnitDetailsOpen||HelpOpen||SystemMenuOpen||MissionOpen;
        public bool CanOpenHelp=>!SystemMenuOpen&&!battle.StoryActive&&!battle.TimingActive&&(battle.Session==null||
            battle.Session.Active?.Team==Team.Player&&!(battle.State is ActionExecutionState));
        static readonly string[] helpTitles={"처음 시작","이동 · 공격","회복 · 대기","높이 · 방향","상태이상","궁극기 · 조건","편성 · 저장","조작 안내"};
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
                int pageIndex=i;Button(helpWindow,"도움말: "+helpTitles[i],80+i*55,()=>ShowHelp(pageIndex),true,44);
                var r=(RectTransform)helpWindow.GetChild(helpWindow.childCount-1);r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(16,-80-i*55);r.sizeDelta=new Vector2(194,44);
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
                case 0:return "처음이라면 아래 ‘입문 연습 시작’을 선택하세요.\n\n1. 크레스를 목표 타일로 이동\n2. 적을 선택하고 공격 확정\n3. 민트의 기술로 아군 회복\n4. 방향을 정하고 대기\n\n연습은 Lv1·기본 장비의 별도 전장입니다. 적은 기다리며, 공격 후 크레스에게 연습용 부상을 줍니다.\n\n성장·골드·장비·장 완료·출전 편성은 변경하지 않습니다. 언제든 출전 화면으로 나와 다시 시작할 수 있습니다.";
                case 1:return "기본 규칙은 아군 턴 → 적군 턴입니다. 명령 대기 중 맵의 아군·상단 초상화·PgUp/PgDn으로 미완료 아군을 선택합니다. 선택을 바꿔도 이동·행동 기록은 유지됩니다.\n\n각 아군은 이동과 행동을 한 번씩 사용하며 순서를 바꿀 수 있습니다.\n\n이동 → 푸른 테두리 타일 → 경로를 따라 이동. 이동만 했다면 이동 취소로 되돌릴 수 있습니다. 행동 후에는 취소할 수 없습니다.\n\n공격 → 적에 마우스를 올려 미리보기 → 클릭하면 바로 사용. Tab으로 대상을 고른 뒤 Enter로 사용할 수도 있습니다.\n\n흰 네모: 행동자 / 금색 네모: 선택 대상\n청록색 네모와 선: 이동 목적지와 경로\n\n대상이 없다면 취소 후 사거리 안으로 이동하세요. 장애물·높이·시야도 사거리에 영향을 줍니다.";
                case 2:return "회복은 ‘기술’에서 퍼스트 에이드 같은 회복 기술을 선택합니다. 기술 선택 → 아군 클릭으로 바로 사용합니다.\n\n회복도 행동 1회를 사용하고 MP가 필요합니다. HP는 최대치를 넘지 않습니다. 쓰러진 아군은 일반 회복 대신 부활 기술이 필요합니다.\n\n대기 · 방향 → 마지막으로 바라볼 방향을 선택하면 턴을 종료합니다. 이동이나 행동을 남긴 채 대기해도 됩니다.\n\n방어는 행동을 사용하고 받는 피해를 줄입니다. 이후 바라볼 방향을 선택하세요. 앞·옆·뒤에 따라 효과가 다릅니다.\n\n명령의 턴 종료는 미완료 인원을 확인한 뒤 모두 대기합니다. 방향은 유지되고 방어 효과는 추가하지 않습니다.\n\n아군 모두 대기·방어를 마치면 적군 턴이 시작됩니다. 상단 회색 초상화는 완료한 아군입니다. SPD·CT 규칙에서는 상단에 다음 행동 순서가 표시됩니다.";
                case 3:return $"MOV는 이동 거리, JMP는 넘을 수 있는 높이 차입니다. 물 타일은 이동 비용이 더 큽니다.\n\n높이 피해를 사용하는 물리 기술은 높이 1단계마다 ±{r.HeightDamagePerStep:P0}, 최대 {r.HeightDamageMaxSteps}단계까지 보정합니다. 활·총의 일부 기술은 높이에 따라 사거리도 변합니다.\n\n방향 피해 배율: 정면 ×{r.FrontMultiplier:0.##}, 측면 ×{r.SideMultiplier:0.##}, 후면 ×{r.RearMultiplier:0.##}. 흰 발밑 표시는 바라보는 방향입니다.\n\n방어 중 받는 배율: 정면 ×{r.GuardFront:0.##}, 측면 ×{r.GuardSide:0.##}, 후면 ×{r.GuardRear:0.##}.\n\n새 전투의 물리 피해: 민첩 차이 1당 0.5%, 최대 ±15%. 적을 양쪽 인접 칸에서 아군과 포위하면 협공 +10%. 적에게도 적용됩니다. 기절·수면 동료는 협공할 수 없습니다.\n기술 상세와 피해 미리보기에서 적용 수치를 확인하세요.";
                case 4:return "능력 버튼에서 현재 상태와 남은 턴을 확인합니다.\n\nStun (기절) · Sleep (수면): 행동 불가. 수면은 피해를 받으면 해제됩니다.\nRoot (속박): 이동 불가.\nCage (케이지): 이동 불가, 속도·마법 방어 감소.\nSong (송): 힘·마력 증가.\nGuard (방어): 방향에 따라 받는 피해 감소.\nAutoRevive (자동 부활): 쓰러질 때 한 번 회복.\nConsumeClaw (컨슘 클로): 벨벳의 강화·연계 상태.\n\n회복 기술과 상태 해제 기술은 다릅니다. 기술 상세의 효과와 사용 불가 사유를 확인하세요.";
                case 5:return "궁극기도 해금 레벨·MP·SP·고유 조건을 모두 만족해야 합니다. 기술 목록 위의 ‘사용 가능’을 눌러 ‘전체 조회’로 바꾸면 잠긴 기술의 조건도 읽을 수 있습니다.\n\n"+
                    string.Join("\n",battle.Catalog.Characters.Where(c=>c.UltimateSkill!=null).Select(c=>{var s=c.UltimateSkill;return c.DisplayName+" · Lv"+s.UnlockLevel+" / MP"+s.MPCost+" / SP"+s.GaugeCost;}))+
                    $"\n\n파라는 모든 일반 기술 해금 후 사자전후에서 연계합니다. 회전 진행률 {r.TimingWindowStart:P0}–{r.TimingWindowEnd:P0}에 Space/패드 A를 누르세요. 훈련 · 전투 설정에 자동 타이밍 옵션도 있습니다.\n다른 고유 조건은 기술 상세의 안내를 따르세요.";
                case 6:return "출전 준비의 카드는 캐릭터를 선택합니다. 우측 출전 추가/제외로 편성을 바꾸고 1–6명을 출전시킵니다. 캠페인은 6명을 권장합니다.\n\n장비: 슬롯 → 보유 후보 → 능력치 비교 → 적용 · 저장. 저장 전에 돌아가면 변경을 취소합니다.\n\n상점: 종류 선택 → 상품 → 구매/매각 · 저장. 장착 중인 수량은 팔 수 없습니다. 구매 장비 장착으로 비교 화면에 바로 이동합니다.\n\n프리셋: 편성과 전체 장비를 3슬롯에 저장·적용합니다. 수량이 부족하면 적용하지 않습니다.\n성장 방향: Lv10부터 돌파·수호 선택. 준비 중 무료 변경.\n\n승리 보상·구매·매각·장비 적용·승급 시 저장합니다. 저장 실패 안내가 나오면 재시도하세요.\n아군 명령 대기 중 메뉴에서 중단 저장할 수 있습니다. 이어하기는 기록을 한 번 소비합니다. 훈련·입문 연습에는 성장·골드·장 완료 보상이 없습니다.";
                default:return "마우스: 버튼·타일 클릭 / 우클릭: 취소\n전장 위 휠: 확대·축소\n\n키보드: Esc 취소, Q/E 회전, Home 전체 보기\nTab/Shift+Tab: 다음/이전 대상\nSpace: 파라 타이밍 / F1: 도움말 / F5: 메뉴\n\n패드: 왼쪽 스틱 커서 / 방향키 메뉴 순환\nA/× 선택 / B/○ 취소\nLB/RB 회전 / LT/RT 대상 순환\n오른쪽 스틱 확대 / R3 전체 보기\n\n상단 현재 유닛 버튼은 행동자와 선택 대상을 확대합니다. 전체 보기로 지도 전체를 다시 볼 수 있습니다. 능력 버튼은 상세 정보를 엽니다.";
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
