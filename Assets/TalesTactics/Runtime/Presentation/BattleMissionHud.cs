using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform missionOverlay,missionWindow;
        public bool MissionOpen=>missionOverlay!=null&&missionOverlay.gameObject.activeSelf;
        public bool CanOpenMission=>!battle.StoryActive&&!battle.TimingActive&&!battle.TutorialActive&&(battle.Session==null||battle.Session.Active?.Team==Team.Player&&!(battle.State is ActionExecutionState)||battle.State is BattleEndState);
        void DrawMissionEntry()
        {
            Button(header,"임무 · 적 정보",16,ShowMission,CanOpenMission,32);
            var r=(RectTransform)header.GetChild(header.childCount-1);r.anchorMin=r.anchorMax=Vector2.one;r.pivot=Vector2.one;r.sizeDelta=new Vector2(112,32);r.anchoredPosition=new Vector2(-596,-16);
            r.GetComponentInChildren<TMPro.TMP_Text>().fontSize=14;
        }
        int missionTab,missionEnemy;
        bool missionDetails;
        public void ShowMission(){missionTab=0;missionDetails=false;RenderMission();}
        bool PreparationEvents(int stage,bool training)=>!training&&battle.Preferences.MissionEvents&&!battle.UseCT&&!battle.UseFixedSpeedOrder&&(stage==2||stage==4);
        string PreparationVictory(int stage)=>PreparationEvents(stage,false)&&stage==2?"거점 "+CampaignMissions.Destination(stage)+"에서 아군 턴 종료 2회 연속 유지":CampaignMissions.Description(stage);
        string BriefVictory(BattleSession s,int stage,bool training)
        {
            if(s?.CaptureMission==true)return "거점 점령 "+s.CaptureProgress+" / 2";
            if(s==null)
            {
                if(training)return TrainingMission();
                switch(CampaignMissions.Kind(stage))
                {
                    case ObjectiveKind.Boss:return CampaignEnemies.Resolve(battle.Catalog,stage,0).DisplayName+" 격파";
                    case ObjectiveKind.Reach:return battle.Preferences.MissionEvents&&!battle.UseCT&&!battle.UseFixedSpeedOrder?"거점 2턴 점령":"봉화에 아군 1명 도착";
                    case ObjectiveKind.Escort:return "선두 출전 아군 호위";
                    case ObjectiveKind.Survive:return battle.UseCT||battle.UseFixedSpeedOrder?"아군 턴 종료 12회 생존":"적군 턴 4회 생존";
                    default:return "모든 적 격파";
                }
            }
            switch(s.Objective)
            {
                case ObjectiveKind.Boss:return s.ObjectiveUnit.Data.DisplayName+" 격파";
                case ObjectiveKind.Reach:return "목표 지점 도달";
                case ObjectiveKind.Escort:return s.ObjectiveUnit.Data.DisplayName+" 호위";
                case ObjectiveKind.Survive:return s.ObjectiveDescription;
                default:return "모든 적 격파";
            }
        }
        void RenderMission()
        {
            if(!CanOpenMission)return;CloseSystemMenu();CloseHelp();CloseUnitDetails();
            if(missionOverlay==null)
            {
                missionOverlay=Panel("MissionOverlay",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
                missionOverlay.GetComponent<UnityEngine.UI.Image>().color=new Color(0,0,0,.12f);missionOverlay.GetComponent<UnityEngine.UI.Outline>().enabled=false;
                missionWindow=Panel("MissionWindow",new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-472,-290),new Vector2(-16,290));missionWindow.SetParent(missionOverlay,false);
            }
            missionWindow.GetComponent<UnityEngine.UI.Image>().color=new Color(.035f,.065f,.11f,1);
            missionOverlay.gameObject.SetActive(true);missionOverlay.SetAsLastSibling();SetMainInteraction(false);Clear(missionWindow);
            var s=battle.Session;bool training=s!=null?s.CampaignStage<0:battle.TrainingMode;int stage=s?.CampaignStage??(training?-1:battle.SelectedStage);
            var title=Label(missionWindow,training?"훈련 임무":CampaignStages.Title(stage),16,36,22);title.rectTransform.offsetMax=new Vector2(-62,title.rectTransform.offsetMax.y);
            Button(missionWindow,"임무 정보 닫기",12,()=>CloseMission(),true,38);var close=(RectTransform)missionWindow.GetChild(missionWindow.childCount-1);close.anchorMin=new Vector2(1,1);close.sizeDelta=new Vector2(44,38);close.anchoredPosition=new Vector2(-30,-12);close.GetComponentInChildren<TMPro.TMP_Text>().text="×";
            Button(missionWindow,"목표",66,()=>{missionTab=0;missionDetails=false;RenderMission();},true,40);HalfButton(missionWindow,0);TintInfoTab(missionWindow,missionTab==0);
            Button(missionWindow,"적 정보",66,()=>{missionTab=1;missionDetails=false;RenderMission();},true,40);HalfButton(missionWindow,1);TintInfoTab(missionWindow,missionTab==1);
            if(missionTab==0)
            {
                var victory=InfoBlock(missionWindow,"VictoryCard",126,100);Label(victory,"승리",10,24,15).color=new Color(.85f,.7f,.4f);Label(victory,BriefVictory(s,stage,training),40,54,24);
                var defeat=InfoBlock(missionWindow,"DefeatCard",238,84);Label(defeat,"패배",8,24,15).color=new Color(1,.45f,.42f);
                bool escort=s!=null?s.Objective==ObjectiveKind.Escort:!training&&CampaignMissions.Kind(stage)==ObjectiveKind.Escort;
                Label(defeat,escort?"아군 전멸 또는 호위 대상 전투불능":"아군 전원 전투불능",36,42,18);
                if(missionDetails)
                {
                    var detail=Label(missionWindow,(s!=null?MissionBriefing.Victory(s):training?TrainingMission():PreparationVictory(stage))+"\n\n"+MissionBriefing.Terrain(stage)+(s?.BossEncounters==true?"\n호위 1명당 피해 -25% (최대 75%)":""),342,174,18);
                }
                else if(s?.BossEncounters==true)
                {
                    var ward=InfoBlock(missionWindow,"WardCard",340,176);Label(ward,"호위 방벽",12,32,20);var value=Label(ward,s.Resolver.BossWardPercent(s.ObjectiveUnit)+"%",4,64,30);value.alignment=TMPro.TextAlignmentOptions.TopRight;value.color=new Color(.35f,.8f,.77f);
                    var escorts=s.Units.Where(u=>u.Team==Team.Enemy&&u!=s.ObjectiveUnit).ToArray();
                    for(int i=0;i<escorts.Length;i++){var portrait=InfoPortrait(ward,escorts[i].Data,20+i*132,58,60,60);if(!escorts[i].Alive)portrait.color=new Color(.4f,.4f,.4f,.5f);}
                    Label(ward,s.ObjectiveUnit.IntentPhase>0?EnemyTactics.Describe(s.ObjectiveUnit):"호위 격파 시 약화",126,48,15);
                }
                else Label(missionWindow,s!=null?MissionBriefing.Progress(s)+(s.ReinforcementMission?"\n"+s.EventSummary:""):PreparationEvents(stage,training)?"3턴 시작 · 적 증원 2명":"출전 후 진행 상황 표시",356,138,20);
                Button(missionWindow,missionDetails?"요약 보기":"임무 상세 보기",532,()=>{missionDetails=!missionDetails;RenderMission();},true,36);
            }
            else
            {
                var enemies=s!=null?s.Units.Where(u=>u.Team==Team.Enemy).Select(u=>u.Data).ToArray():Enumerable.Range(0,4).Select(i=>CampaignEnemies.Resolve(battle.Catalog,stage,i)).ToArray();
                missionEnemy=Mathf.Clamp(missionEnemy,0,enemies.Length-1);
                for(int i=0;i<enemies.Length;i++)
                {
                    int index=i;Button(missionWindow,"적 선택: "+i,128,()=>{missionEnemy=index;missionDetails=false;RenderMission();},true,94);
                    var row=(RectTransform)missionWindow.GetChild(missionWindow.childCount-1);row.anchorMin=new Vector2(i/(float)enemies.Length,1);row.anchorMax=new Vector2((i+1)/(float)enemies.Length,1);row.sizeDelta=new Vector2(-12,94);
                    var label=row.GetComponentInChildren<TMPro.TMP_Text>();label.text=EnemyRoles.Name(enemies[i]);label.rectTransform.anchoredPosition=new Vector2(0,-67);label.rectTransform.sizeDelta=new Vector2(-4,24);label.fontSize=13;
                    InfoPortrait(row,enemies[i],enemies.Length>4?8:16,5,enemies.Length>4?46:58,58);TintInfoTab(missionWindow,i==missionEnemy);
                }
                var data=enemies[missionEnemy];var unit=s?.Units.Where(u=>u.Team==Team.Enemy).ElementAt(missionEnemy);
                Label(missionWindow,data.DisplayName,246,40,24);
                Label(missionWindow,unit==null?EnemyRoles.Name(data):unit.Alive?"HP "+unit.CurrentHP+" / "+unit.Stats.HP:"격파",290,36,20);
                string state=unit!=null&&unit.IntentPhase>0?EnemyTactics.Describe(unit):unit?.BossWard==true?"호위 방벽 "+s.Resolver.BossWardPercent(unit)+"%":EnemyRoles.Name(data);
                Label(missionWindow,state,340,78,18);
                if(missionDetails)Label(missionWindow,data.BasicAttack.DisplayName+" · 사거리 "+data.BasicAttack.MinRange+"–"+data.BasicAttack.Range+"\n"+(unit?.BossWard==true?"호위 1명당 피해 -25% (최대 75%)":EnemyRoles.Advice(data)),428,98,16);
                Button(missionWindow,missionDetails?"적 요약 보기":"적 상세 보기",532,()=>{missionDetails=!missionDetails;RenderMission();},true,36);
            }
        }
        public bool CloseMission()
        {bool open=MissionOpen;if(missionOverlay!=null)missionOverlay.gameObject.SetActive(false);SetMainInteraction(!UnitDetailsOpen&&!HelpOpen&&!SystemMenuOpen);return open;}
        string TrainingMission()
        {
            switch(battle.TrainingObjective)
            {
                case ObjectiveKind.Boss:return "첫 번째 적 보스 격파";
                case ObjectiveKind.Reach:return "아군 1명 목표 (8,8) 도착";
                case ObjectiveKind.Escort:return "첫 출전 아군을 목표 (8,8)까지 호위";
                case ObjectiveKind.Survive:return battle.UseCT||battle.UseFixedSpeedOrder?"아군 턴 종료 12회 생존":"적군 턴 4회 생존";
                default:return "모든 적 격파";
            }
        }
    }
}
