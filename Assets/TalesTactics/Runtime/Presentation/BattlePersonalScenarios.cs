using System;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleDirector
    {
        int personalChoice;
        public bool CanDeploy(int index)=>index>=0&&index<Catalog.Characters.Length&&(TrainingMode||!CrossoverCampaign.IsMain(SelectedStage)||CrossoverCampaign.Joined(Catalog.Characters[index].Id,CrossoverCampaign.Episode(SelectedStage).Number));
        public void NormalizeDeployment()
        {
            Deployment.RemoveAll(i=>!CanDeploy(i));
            if(Deployment.Count==0)Deployment.AddRange(TacticalDevelopment.Recommended(Catalog).Where(CanDeploy));
            if(PersonalScenarios.Contains(SelectedStage))
            {
                int owner=Array.FindIndex(Catalog.Characters,c=>c.Id==PersonalScenarios.Arc(SelectedStage).Owner);
                if(owner>=0&&!Deployment.Contains(owner)){if(Deployment.Count>=Catalog.Rules.MaxDeployment)Deployment.RemoveAt(Deployment.Count-1);Deployment.Insert(0,owner);}
            }
        }
        void BeginScenarioAfterStory()
        {
            if(PersonalScenarios.Contains(SelectedStage)&&PersonalScenarios.Tier(SelectedStage)==0)
            {
                bool saved=false;
                try{saved=CampaignStages.TryReward(Campaign,CampaignEconomy.Prepare(Campaign,SelectedStage,9999),new[]{Catalog.Characters.First(c=>c.Id==PersonalScenarios.Arc(SelectedStage).Owner)},PersistCampaign);}catch(Exception){ }
                SaveNotice=saved?"동료의 이야기를 기록했습니다.":"이야기 저장 실패 · 기록과 보상은 적용되지 않았습니다. 다시 시도하세요.";
                if(saved)OpenStory(SelectedStage,true,Hud.ShowPersonalScenarios);else Hud.ShowPersonalScenarios();
                return;
            }
            bool choice=PersonalScenarios.Contains(SelectedStage)&&PersonalScenarios.Tier(SelectedStage)==1||CrossoverCampaign.IsMain(SelectedStage)&&CrossoverCampaign.Episode(SelectedStage).Number==13;
            if(choice){Hud.ShowScenarioChoice();return;}
            personalChoice=0;BeginBattle();
        }
        public void ChooseScenarioRoute(int choice)
        {if(Session!=null||choice<0||choice>1)return;personalChoice=choice;BeginBattle();}
    }
    public sealed partial class BattleHud
    {
        int personalPage;
        public void ShowPersonalScenarios()
        {
            if(battle.Session!=null)return;BeginPreparation("동료의 이야기");
            Label(left,"동료 · "+(personalPage+1)+" / 4",12,36,22);
            for(int n=personalPage*3;n<Math.Min(10,personalPage*3+3);n++)
            {
                int arcIndex=n;var arc=PersonalScenarios.Arcs[n];
                Button(left,arc.Name,66+n%3*82,()=>{selectedCharacter=Array.FindIndex(battle.Catalog.Characters,c=>c.Id==arc.Owner);battle.SelectedStage=PersonalScenarios.FirstStage+arcIndex*3;ShowPersonalScenarios();},true,66);
            }
            Button(left,"이전 동료",332,()=>{personalPage--;battle.SelectedStage=PersonalScenarios.FirstStage+personalPage*9;ShowPersonalScenarios();},personalPage>0,40);HalfButton(left,0);
            Button(left,"다음 동료",332,()=>{personalPage++;battle.SelectedStage=PersonalScenarios.FirstStage+personalPage*9;ShowPersonalScenarios();},personalPage<3,40);HalfButton(left,1);
            int chosen=PersonalScenarios.Contains(battle.SelectedStage)?(battle.SelectedStage-PersonalScenarios.FirstStage)/3:personalPage*3;
            var selected=PersonalScenarios.Arcs[chosen];Label(center,selected.Name+" · 세 개의 이야기",20,48,24);
            for(int tier=0;tier<3;tier++)
            {
                int stage=PersonalScenarios.FirstStage+chosen*3+tier;bool done=battle.Campaign.StoryProgress.Contains(PersonalScenarios.Flag(stage));bool unlocked=PersonalScenarios.Unlocked(battle.Campaign,stage);
                Button(center,(done?"완료 · ":"")+PersonalScenarios.Title(stage),85+tier*80,()=>{battle.SelectedStage=stage;battle.NormalizeDeployment();if(done)battle.ReplayStory(true);else battle.RequestBattle();},unlocked,66);
            }
            Label(center,"개방: "+string.Join(" / ",Enumerable.Range(0,3).Select(t=>PersonalScenarios.RequiredEpisode(PersonalScenarios.FirstStage+chosen*3+t)+"화 완료"))+"\n무기 미획득으로 메인이나 귀환이 막히지 않습니다.",340,100,19);
            Label(commands,selected.WeaponName,20,90,22);Label(commands,SignatureWeaponRules.Describe(selected.Effect),115,150,20);
            Button(commands,"메인 임무로",305,()=>{battle.SelectedStage=CrossoverCampaign.FirstStage;ShowChapterPage(2);},true,48);
            Button(commands,"출전 준비로",365,ShowDeployment,true,48);
            Label(footer,string.IsNullOrEmpty(battle.SaveNotice)?"1 개인 이야기 → 2 구조 순서 선택 → 3 전용 무기 시련 · 완료 보상은 한 번만 지급됩니다.":battle.SaveNotice,20,75,19);
        }
        public void ShowScenarioChoice()
        {
            BeginPreparation("먼저 열 길을 선택하세요");
            Label(center,"두 길을 모두 돕습니다.",35,50,25);
            Label(center,"왼쪽 (2,7) 또는 오른쪽 (10,7)의 신호부터 확보하세요.\n선택한 첫 신호 뒤에는 나머지 신호와 중앙 (6,10)을 연결합니다.\n구출 불가 분기는 없습니다.",110,175,21);
            Button(center,PersonalScenarios.RouteName(battle.SelectedStage,0)+"부터",320,()=>battle.ChooseScenarioRoute(0),true,60);
            Button(center,PersonalScenarios.RouteName(battle.SelectedStage,1)+"부터",395,()=>battle.ChooseScenarioRoute(1),true,60);
            Button(commands,"돌아가기",30,()=>{if(PersonalScenarios.Contains(battle.SelectedStage))ShowPersonalScenarios();else ShowDeployment();},true,50);
        }
    }
}
