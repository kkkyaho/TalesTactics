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
            if(!CanOpenSystemMenu)return;CloseHelp();CloseUnitDetails();
            if(systemOverlay==null)
            {
                systemOverlay=Panel("SystemOverlay",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);systemOverlay.GetComponent<UnityEngine.UI.Image>().color=new Color(0,0,0,.8f);systemOverlay.GetComponent<UnityEngine.UI.Outline>().enabled=false;
                systemWindow=Panel("SystemWindow",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-410,-325),new Vector2(410,325));systemWindow.SetParent(systemOverlay,false);
            }
            systemOverlay.gameObject.SetActive(true);systemOverlay.SetAsLastSibling();SetMainInteraction(false);Clear(systemWindow);
            Label(systemWindow,"시스템 · 슬롯 "+(battle.Profiles.Slot+1),16,36,24);
            Button(systemWindow,"저장 / 이어하기",62,()=>ShowSystemMenu(0));HalfButton(systemWindow,0);
            Button(systemWindow,"화면 / 음량 / 입력",62,()=>{settingsDraft=null;ShowSystemMenu(1);});HalfButton(systemWindow,1);
            if(page==0)DrawSaveMenu();else DrawPreferences();
            Button(systemWindow,"메뉴 닫기",592,()=>CloseSystemMenu(),true,40);
        }
        public bool CloseSystemMenu()
        {
            if(!SystemMenuOpen)return false;systemOverlay.gameObject.SetActive(false);settingsDraft=null;SetMainInteraction(!UnitDetailsOpen&&!HelpOpen);return true;
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
                Label(systemWindow,"다른 슬롯은 현재 파일을 덮어쓰지 않습니다. 보상·거래·장비·승급은 자동 저장됩니다.\n새 전투의 보상을 저장하면 이전 중단 기록은 해제됩니다.",490,55,16);
            }
            else
            {
                Label(systemWindow,"전투 메뉴\n\n아군의 명령 대기 중에 중단 저장할 수 있습니다.\n위치·HP/MP·상태·행동 여부·턴 순서를 함께 저장합니다.\n훈련과 입문 연습은 중단 저장하지 않습니다.\n\n이어하기는 중단 기록을 소비합니다. 다시 종료할 때는\n중단 저장을 사용하세요. 게임을 바로 종료하면 현재 전투는 사라집니다.",126,260,20);
                Button(systemWindow,"전투 중단 · 저장 후 준비로",414,()=>{if(!battle.SuspendBattle())ShowSystemMenu();},battle.CanSuspend&&battle.CanSave,48);
                Button(systemWindow,"저장 없이 출전 준비로",480,()=>{battle.Restart();},!battle.RewardPending,40);
            }
            Label(systemWindow,battle.SaveNotice,548,40,16);
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
            Button(systemWindow,d.CT?"다음 전투 턴 순서: CT":"다음 전투 턴 순서: SPD",380,()=>{d.CT=!d.CT;ShowSystemMenu(1);});
            Button(systemWindow,d.Utility?"다음 전투 AI: Utility":"다음 전투 AI: 기본",424,()=>{d.Utility=!d.Utility;ShowSystemMenu(1);});
            Button(systemWindow,"설정 적용 · 저장",476,()=>{battle.SavePreferences(d);ShowSystemMenu(1);},true,44);
            Label(systemWindow,"닫으면 미적용 변경은 취소됩니다. 해상도는 Windows 플레이어에 적용됩니다.\n"+battle.SaveNotice,532,50,16);
        }
        void PreferenceRow(string title,float value,int y,System.Action lower,System.Action higher)
        {
            Button(systemWindow,title+" −",y,lower);var a=(RectTransform)systemWindow.GetChild(systemWindow.childCount-1);a.anchorMax=new Vector2(.35f,1);
            var label=Label(systemWindow,Mathf.RoundToInt(value*100)+"%",y,38,18);label.alignment=TMPro.TextAlignmentOptions.Center;
            Button(systemWindow,title+" +",y,higher);var b=(RectTransform)systemWindow.GetChild(systemWindow.childCount-1);b.anchorMin=new Vector2(.65f,1);
        }
    }
}
