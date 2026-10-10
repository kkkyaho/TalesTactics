using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform systemOverlay,systemWindow;
        PlayerPreferences settingsDraft;
        public bool SystemMenuOpen=>systemOverlay!=null&&systemOverlay.gameObject.activeSelf;
        public bool CanOpenSystemMenu=>!battle.StoryActive&&!battle.TimingActive&&(battle.Session==null||battle.IsPlayerCommand||battle.State is BattleEndState||battle.State is TutorialCompleteState);
        void DrawSystemEntry(bool preparation)
        {
            Button(header,"메뉴 · 저장/설정",16,()=>ShowSystemMenu(),CanOpenSystemMenu,32);
            var r=(RectTransform)header.GetChild(header.childCount-1);r.anchorMin=r.anchorMax=Vector2.one;r.pivot=Vector2.one;r.sizeDelta=new Vector2(preparation?146:88,32);r.anchoredPosition=new Vector2(preparation?-232:-498,-16);
            var label=r.GetComponentInChildren<TMPro.TMP_Text>();label.text=preparation?"저장 · 설정":"메뉴 F5";label.fontSize=preparation?16:14;
        }
        public void ShowSystemMenu(int page=0)
        {
            if(!CanOpenSystemMenu)return;CloseMission();CloseHelp();CloseUnitDetails();
            if(systemOverlay==null)
            {
                systemOverlay=Panel("SystemOverlay",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);systemOverlay.GetComponent<UnityEngine.UI.Image>().color=new Color(0,0,0,.8f);systemOverlay.GetComponent<UnityEngine.UI.Outline>().enabled=false;
                systemWindow=Panel("SystemWindow",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-410,-325),new Vector2(410,325));systemWindow.SetParent(systemOverlay,false);
            }
            systemOverlay.gameObject.SetActive(true);systemOverlay.SetAsLastSibling();SetMainInteraction(false);Clear(systemWindow);
            bool confirmation=page==4||page==5;
            bool battleMenu=page==0&&battle.Session!=null;
            systemWindow.sizeDelta=confirmation?new Vector2(560,270):battleMenu?new Vector2(640,460):new Vector2(820,650);
            if(confirmation){if(page==4)DrawRetryConfirmation();else DrawRewindConfirmation();return;}
            Label(systemWindow,"시스템 · 슬롯 "+(battle.Profiles.Slot+1),16,36,24);
            Button(systemWindow,"저장 / 이어하기",62,()=>ShowSystemMenu(0));SystemTab(0);
            Button(systemWindow,"화면 / 음량 / 입력",62,()=>{settingsDraft=null;ShowSystemMenu(1);});SystemTab(1);
            Button(systemWindow,"전투 진행",62,()=>{settingsDraft=null;ShowSystemMenu(2);});SystemTab(2);
            Button(systemWindow,"글자 / 조작",62,()=>{settingsDraft=null;ShowSystemMenu(3);});SystemTab(3);
            if(page==0)DrawSaveMenu();else if(page==1)DrawPreferences();else if(page==2)DrawFlowPreferences();else if(page==6||page==7)DrawSystemGuide(page);else DrawAccessibility();
            Button(systemWindow,"메뉴 닫기",battleMenu?402:592,()=>CloseSystemMenu(),true,40);CaptionLastButton(systemWindow,"닫기");
        }
        void SystemTab(int index)
        {var r=(RectTransform)systemWindow.GetChild(systemWindow.childCount-1);r.anchorMin=new Vector2(index/4f,1);r.anchorMax=new Vector2((index+1)/4f,1);CaptionLastButton(systemWindow,new[]{"저장","설정","전투","조작"}[index]);}
        void CaptionLastButton(Transform parent,string caption)=>parent.GetChild(parent.childCount-1).GetComponentInChildren<TMPro.TMP_Text>().text=caption;
        void DrawAccessibility()
        {
            if(settingsDraft==null)settingsDraft=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(battle.Preferences));var d=settingsDraft;
            Button(systemWindow,"글자 최대 크기: "+Mathf.RoundToInt(d.TextScale*100)+"%",126,()=>{d.TextScale=d.TextScale<1.1f?1.15f:d.TextScale<1.2f?1.3f:1;ShowSystemMenu(3);},true,48);
            Label(systemWindow,"M 이동   A 공격   S 기술   G 방어   W 대기\n\nEnter 선택   Esc 취소   Z 이동 취소\n\nTab 대상 전환   V 위험 표시   Q / E 회전",210,220,20);
            Button(systemWindow,"전체 조작 보기",444,()=>{CloseSystemMenu();ShowHelp(7);},true,36);
            Button(systemWindow,"글자 설정 저장",526,()=>{battle.SavePreferences(d,false);ShowSystemMenu(3);},true,44);
        }
        void DrawFlowPreferences()
        {
            if(settingsDraft==null)settingsDraft=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(battle.Preferences));var d=settingsDraft;
            Button(systemWindow,"다음 전투 난이도: "+DifficultyRules.Name(d.Difficulty),124,()=>{d.Difficulty=(BattleDifficulty)(((int)d.Difficulty+1)%3);ShowSystemMenu(2);},true,42);
            Button(systemWindow,d.MissionEvents?"임무 변형: 켜짐":"임무 변형: 꺼짐",178,()=>{d.MissionEvents=!d.MissionEvents;ShowSystemMenu(2);},true,42);
            Button(systemWindow,d.Reactions?"반격 · 지원: 켜짐":"반격 · 지원: 꺼짐",232,()=>{d.Reactions=!d.Reactions;ShowSystemMenu(2);},true,42);
            Button(systemWindow,"전투 규칙 자세히",286,()=>ShowSystemMenu(7),true,32);CaptionLastButton(systemWindow,"규칙 보기  ›");
            Button(systemWindow,"적 행동 속도: "+(1<<d.EnemySpeedMode)+"배",340,()=>{d.EnemySpeedMode=(d.EnemySpeedMode+1)%3;ShowSystemMenu(2);},true,42);
            Button(systemWindow,d.SkipEnemyAnimations?"적 연출: 간략 (이동·기술 생략)":"적 연출: 전체",394,()=>{d.SkipEnemyAnimations=!d.SkipEnemyAnimations;ShowSystemMenu(2);},true,42);
            CaptionLastButton(systemWindow,d.SkipEnemyAnimations?"적 연출: 간략":"적 연출: 전체");
            Label(systemWindow,"전투 규칙은 다음 출전부터 적용",460,32,18);
            Button(systemWindow,"전투 진행 설정 저장",526,()=>{battle.SavePreferences(d,false);ShowSystemMenu(2);},true,44);
            CaptionLastButton(systemWindow,"적용");
            Label(systemWindow,battle.SaveNotice,574,20,14);
        }
        public bool CloseSystemMenu()
        {
            if(!SystemMenuOpen)return false;systemOverlay.gameObject.SetActive(false);settingsDraft=null;RefreshPreparationMissionText();SetMainInteraction(!UnitDetailsOpen&&!HelpOpen);return true;
        }
        void DrawSaveMenu()
        {
            if(battle.Session==null)
            {
                for(int i=0;i<3;i++)
                {
                    int slot=i;Button(systemWindow,(battle.Profiles.Slot==slot?"● ":"")+"슬롯 "+(slot+1)+" 불러오기",118+i*105,()=>{if(battle.SelectProfile(slot))CloseSystemMenu();else ShowSystemMenu();},true,36);
                    Label(systemWindow,battle.Profiles.Describe(slot),156+i*105,48,16);
                }
                Button(systemWindow,"현재 슬롯 저장",442,()=>{battle.SavePreparation();ShowSystemMenu();},battle.CanSave);HalfButton(systemWindow,0);
                Button(systemWindow,"중단 전투 이어하기",442,()=>{if(!battle.ResumeBattle())ShowSystemMenu();},battle.Campaign.SuspendedBattle!=null&&battle.CanSave);HalfButton(systemWindow,1);
                Button(systemWindow,"저장 도움말",494,()=>ShowSystemMenu(6),true,32);
            }
            else
            {
                Button(systemWindow,"전투 중단 · 저장 후 준비로",124,()=>{if(!battle.SuspendBattle())ShowSystemMenu();},battle.CanSuspend&&battle.CanSave,46);CaptionLastButton(systemWindow,"저장 후 나가기");
                Button(systemWindow,"행동 되감기 · "+battle.Session.RewindsLeft+" / 3",184,()=>ShowSystemMenu(5),battle.CanRewind,46);CaptionLastButton(systemWindow,"되돌리기  ·  "+battle.Session.RewindsLeft+"회");
                Button(systemWindow,"시작 상태로 재도전",244,()=>ShowSystemMenu(4),battle.CanRetryOpening,46);HalfButton(systemWindow,0);CaptionLastButton(systemWindow,"재도전");
                Button(systemWindow,"저장 없이 출전 준비로",244,()=>{battle.Restart();},!battle.RewardPending,46);HalfButton(systemWindow,1);CaptionLastButton(systemWindow,"저장 없이 나가기");
                Button(systemWindow,"저장 도움말",312,()=>ShowSystemMenu(6),true,32);CaptionLastButton(systemWindow,"도움말  ›");
            }
            Label(systemWindow,battle.SaveNotice,battle.Session==null?548:350,40,16);
        }
        void DrawRetryConfirmation()
        {
            Label(systemWindow,"처음부터 다시 시작할까요?",24,48,24);
            Label(systemWindow,"현재 전투 진행은 사라집니다.",92,48,19);
            Button(systemWindow,"현재 전투 계속",192,()=>ShowSystemMenu(),true,48);HalfButton(systemWindow,0);CaptionLastButton(systemWindow,"취소");
            Button(systemWindow,"재도전 확정",192,()=>{if(!battle.RetryOpening())ShowSystemMenu();},battle.CanRetryOpening,48);HalfButton(systemWindow,1);CaptionLastButton(systemWindow,"재도전");
        }
        void DrawSystemGuide(int page)
        {
            string text=page==6?
                "저장 · 이어하기\n아군 명령 중 저장 후 나가기를 선택하세요. 이어한 뒤 종료할 때도 다시 저장해야 합니다. 보상·거래·장비·승급은 자동 저장됩니다. 새 전투 보상 저장 시 이전 중단 기록은 지워집니다.\n\n되돌리기\n전투당 3회, 최근 12개 행동 중 직전 행동 전으로 돌아갑니다. 이후 적 행동과 확률 판정도 함께 되돌립니다. 사용 횟수와 기록은 중단 저장에 남습니다. 훈련·연출 중·승리 후에는 사용할 수 없습니다.\n\n재도전\n출전 당시 편성·장비·레벨·규칙으로 다시 시작합니다. 저장된 성장·골드·장비는 유지됩니다.":
                "난이도\n여유: 적 Lv−2 / 표준: 기존 / 도전: 적 Lv+2. 보상은 같습니다.\n\n임무 변형\n자유 선택 턴에서 3장 거점 점령, 3·5장 적 증원이 추가됩니다.\n\n반격 · 지원\n단일 공격 뒤 인접 지원 50% → 생존 대상 반격 75%. 각자 자기 턴까지 1회이며 사거리·시야와 행동 불가 상태를 따릅니다.\n\n규칙은 다음 출전부터 적용됩니다. 이어하기·재도전은 출전 당시 규칙을 유지합니다. 적 연출 속도는 판정에 영향을 주지 않습니다.";
            Label(systemWindow,text,124,392,20);
            Button(systemWindow,"설명 돌아가기",532,()=>ShowSystemMenu(page==6?0:2),true,40);CaptionLastButton(systemWindow,"돌아가기");
        }
        void DrawPreferences()
        {
            if(settingsDraft==null)settingsDraft=JsonUtility.FromJson<PlayerPreferences>(JsonUtility.ToJson(battle.Preferences));
            var d=settingsDraft;
            Button(systemWindow,"해상도: "+d.Width+" × "+d.Height,116,()=>{if(d.Width==1280){d.Width=1366;d.Height=768;}else if(d.Width==1366){d.Width=1920;d.Height=1080;}else{d.Width=1280;d.Height=800;}ShowSystemMenu(1);});
            Button(systemWindow,d.Fullscreen?"화면: 전체 화면":"화면: 창 모드",160,()=>{d.Fullscreen=!d.Fullscreen;ShowSystemMenu(1);});
            PreferenceRow("음악",d.Music,204,()=>{d.Music=Mathf.Max(0,d.Music-.1f);ShowSystemMenu(1);},()=>{d.Music=Mathf.Min(1,d.Music+.1f);ShowSystemMenu(1);});
            PreferenceRow("효과음",d.Effects,248,()=>{d.Effects=Mathf.Max(0,d.Effects-.1f);ShowSystemMenu(1);},()=>{d.Effects=Mathf.Min(1,d.Effects+.1f);ShowSystemMenu(1);});
            Button(systemWindow,"패드 커서 속도: "+d.CursorSpeed.ToString("0.0")+"배",292,()=>{d.CursorSpeed=d.CursorSpeed>=2?.5f:d.CursorSpeed+.25f;ShowSystemMenu(1);});
            Button(systemWindow,d.AutoTiming?"파라 타이밍: 자동 입력":"파라 타이밍: 수동 입력",336,()=>{d.AutoTiming=!d.AutoTiming;ShowSystemMenu(1);});
            Button(systemWindow,"다음 전투 턴 순서: "+(d.CT?"CT":d.FixedSpeedOrder?"SPD":"아군 자유 선택"),380,()=>{if(d.CT){d.CT=false;d.FixedSpeedOrder=false;}else if(d.FixedSpeedOrder){d.FixedSpeedOrder=false;d.CT=true;}else d.FixedSpeedOrder=true;ShowSystemMenu(1);});
            Button(systemWindow,d.Utility?"다음 전투 AI: Utility":"다음 전투 AI: 기본",424,()=>{d.Utility=!d.Utility;ShowSystemMenu(1);});
            Button(systemWindow,"설정 적용 · 저장",476,()=>{battle.SavePreferences(d);ShowSystemMenu(1);},true,44);
            Label(systemWindow,"적용하지 않고 닫으면 취소됩니다.\n"+battle.SaveNotice,532,50,16);
        }
        void PreferenceRow(string title,float value,int y,System.Action lower,System.Action higher)
        {
            Button(systemWindow,title+" −",y,lower);var a=(RectTransform)systemWindow.GetChild(systemWindow.childCount-1);a.anchorMax=new Vector2(.35f,1);
            var label=Label(systemWindow,Mathf.RoundToInt(value*100)+"%",y,38,18);label.alignment=TMPro.TextAlignmentOptions.Center;
            Button(systemWindow,title+" +",y,higher);var b=(RectTransform)systemWindow.GetChild(systemWindow.childCount-1);b.anchorMin=new Vector2(.65f,1);
        }
    }
}
