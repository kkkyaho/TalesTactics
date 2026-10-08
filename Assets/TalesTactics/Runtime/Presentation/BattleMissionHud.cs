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
        public void ShowMission()
        {
            if(!CanOpenMission)return;CloseSystemMenu();CloseHelp();CloseUnitDetails();
            if(missionOverlay==null)
            {
                missionOverlay=Panel("MissionOverlay",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);missionOverlay.GetComponent<UnityEngine.UI.Image>().color=new Color(0,0,0,.8f);missionOverlay.GetComponent<UnityEngine.UI.Outline>().enabled=false;
                missionWindow=Panel("MissionWindow",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-480,-325),new Vector2(480,325));missionWindow.SetParent(missionOverlay,false);
            }
            missionOverlay.gameObject.SetActive(true);missionOverlay.SetAsLastSibling();SetMainInteraction(false);Clear(missionWindow);
            var s=battle.Session;bool training=s!=null?s.CampaignStage<0:battle.TrainingMode;int stage=s?.CampaignStage??(training?-1:battle.SelectedStage);
            Label(missionWindow,"임무 · "+(training?"훈련 전투":CampaignStages.Title(stage)),16,44,25);
            string victory=s!=null?MissionBriefing.Victory(s):training?TrainingMission():CampaignMissions.Description(stage);
            string defeat=s!=null?MissionBriefing.Defeat(s):(training&&battle.TrainingObjective==ObjectiveKind.Escort||!training&&CampaignMissions.Kind(stage)==ObjectiveKind.Escort)?"아군 전원 전투불능 또는 첫 출전 아군 전투불능":"아군 전원 전투불능";
            Label(missionWindow,"승리  ·  "+victory,72,36,20);Label(missionWindow,"패배  ·  "+defeat,112,36,20);
            Label(missionWindow,s!=null?"진행  ·  "+MissionBriefing.Progress(s):"출전 전 정보 · 전투 중에도 같은 메뉴에서 확인할 수 있습니다.",152,36,18);
            Label(missionWindow,MissionBriefing.Terrain(stage),194,38,18);
            var enemies=s!=null?s.Units.Where(u=>u.Team==Team.Enemy).Select(u=>u.Data).ToArray():Enumerable.Range(0,4).Select(i=>CampaignEnemies.Resolve(battle.Catalog,stage,i)).ToArray();
            for(int i=0;i<enemies.Length;i++)
            {
                var data=enemies[i];var unit=s?.Units.Where(u=>u.Team==Team.Enemy).ElementAt(i);var skill=data.BasicAttack;
                string status=unit==null?"":unit.Alive?" · HP "+unit.CurrentHP+" / "+unit.Stats.HP:" · 격파";
                Label(missionWindow,data.DisplayName+"  ["+(training?"훈련":EnemyRoles.Name(data))+"]"+status+"  · "+skill.DisplayName+" (기본 사거리 "+skill.MinRange+"–"+skill.Range+")",242+i*76,32,18);
                Label(missionWindow,training?"훈련 규칙에 따라 행동합니다. 높이·시야·상태에 따라 실제 공격 가능 범위가 달라집니다.":unit!=null&&unit.IntentPhase>0?EnemyTactics.Describe(unit):unit!=null&&unit.BossWard?"호위 1명당 피해 -25% (최대 75%) · 호위 격파로 해제 · 공격 예고 뒤 위치를 피하세요.":EnemyRoles.Advice(data),274+i*76,32,16);
            }
            Button(missionWindow,"임무 정보 닫기",574,()=>CloseMission(),true,44);
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
