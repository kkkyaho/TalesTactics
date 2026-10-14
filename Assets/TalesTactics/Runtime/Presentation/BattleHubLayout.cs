using System;
using System.Linq;
using TMPro;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        int hubMissionTab;
        void HubLayout()
        {
            Place(header,new Vector2(0,1),Vector2.one,new Vector2(12,-76),new Vector2(-12,-12));
            Place(center,Vector2.zero,Vector2.one,new Vector2(12,156),new Vector2(-12,-88));
            Place(footer,Vector2.zero,new Vector2(1,0),new Vector2(12,12),new Vector2(-12,144));
        }
        public void ShowIntermissionHub()
        {
            if(battle.Session!=null||battle.StoryActive)return;
            if(battle.ResumePendingEnding())return;
            if(!battle.TrainingMode&&battle.Catalog.CrossoverReady)battle.SelectCurrentMain();
            BeginPreparation("출전 준비");hubLayout=true;left.gameObject.SetActive(false);commands.gameObject.SetActive(false);HubLayout();
            StoryBackdrop(center);
            var title=header.GetComponentInChildren<TMP_Text>();title.text="출전 준비";title.fontSizeMax=25;
            FormationText(header,battle.Campaign.Gold.ToString("N0")+" G",new Vector2(.32f,.12f),new Vector2(.54f,.88f),22,TextAlignmentOptions.Center).color=formationGold;
            var mission=IntermissionPanel(center,"HubMission",new Vector2(.02f,.03f),new Vector2(.55f,.97f));
            FormationText(mission,battle.TrainingMode?"전투 훈련":CampaignStages.Title(battle.SelectedStage),new Vector2(.04f,.85f),new Vector2(.96f,.96f),28);
            var objective=FormationButton(mission,"거점 목표","목표",new Vector2(.04f,.73f),new Vector2(.27f,.84f),()=>{hubMissionTab=0;ShowIntermissionHub();});
            var enemies=FormationButton(mission,"거점 적 정보","적 정보",new Vector2(.28f,.73f),new Vector2(.51f,.84f),()=>{hubMissionTab=1;ShowIntermissionHub();});
            StyleButton(objective,HudRole.Tab,hubMissionTab==0);StyleButton(enemies,HudRole.Tab,hubMissionTab==1);
            if(hubMissionTab==0)
            {
                FormationText(mission,"승리 조건",new Vector2(.04f,.65f),new Vector2(.96f,.72f),16).color=formationTeal;
                FormationText(mission,BriefVictory(null,battle.SelectedStage,battle.TrainingMode),new Vector2(.04f,.47f),new Vector2(.96f,.65f),21);
                FormationText(mission,"패배 조건",new Vector2(.04f,.39f),new Vector2(.96f,.46f),16).color=new Color(.86f,.61f,.58f);
                FormationText(mission,PreparationDefeat(),new Vector2(.04f,.29f),new Vector2(.96f,.39f),20);
            }
            else
            {
                var data=battle.TrainingMode?new[]{battle.Catalog.Enemy}:Enumerable.Range(0,CampaignEncounters.Total(battle.SelectedStage)).Select(i=>CampaignEncounters.Enemy(battle.Catalog,battle.SelectedStage,i)).Distinct().Take(3).ToArray();
                for(int i=0;i<data.Length;i++)
                {
                    float top=.71f-i*.14f;
                    FormationText(mission,data[i].DisplayName+" · "+EnemyRoles.Name(data[i]),new Vector2(.04f,top-.06f),new Vector2(.96f,top),19).color=formationTeal;
                    FormationText(mission,EnemyRoles.Advice(data[i]),new Vector2(.04f,top-.14f),new Vector2(.96f,top-.06f),16);
                }
                FormationButton(mission,"거점 적 상세","적 상세 보기",new Vector2(.66f,.73f),new Vector2(.96f,.84f),()=>{missionTab=1;missionEnemy=0;missionDetails=false;RenderMission();},true,16);
            }
            var preview=FormationRect(mission,"HubMapPreview",new Vector2(.04f,.045f),new Vector2(.44f,.27f));
            var mapArt=Resources.Load<Texture2D>("TalesTactics/MenuPreviews/Stage"+battle.SelectedStage);
            if(mapArt!=null){var imageBounds=FormationRect(preview,"MapImage",Vector2.zero,Vector2.one);var mapImage=imageBounds.gameObject.AddComponent<UnityEngine.UI.RawImage>();mapImage.texture=mapArt;mapImage.raycastTarget=false;var fit=imageBounds.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=16f/9;}
            else preview.gameObject.AddComponent<MissionMapPreview>().Initialize(battle.SelectedStage);
            FormationText(mission,"전장 미리보기",new Vector2(.47f,.17f),new Vector2(.96f,.26f),19);
            FormationText(mission,"권장 Lv."+CampaignStages.Get(battle.SelectedStage).EntryLevel+" · 최대 "+battle.Catalog.Rules.MaxDeployment+"명",new Vector2(.47f,.06f),new Vector2(.96f,.17f),17);
            var party=IntermissionPanel(center,"HubParty",new Vector2(.57f,.03f),new Vector2(.98f,.97f));
            FormationText(party,"출전 멤버  "+battle.Deployment.Count+" / "+battle.Catalog.Rules.MaxDeployment,new Vector2(.04f,.85f),new Vector2(.96f,.96f),23);
            var members=battle.Deployment.Where(i=>i>=0&&i<battle.Catalog.Characters.Length).ToArray();
            for(int n=0;n<members.Length;n++)
            {
                int index=members[n];var c=battle.Catalog.Characters[index];float x=.035f+n%3*.31f,top=.81f-n/3*.28f;
                var card=FormationButton(party,"거점 캐릭터: "+c.Id,"",new Vector2(x,top-.27f),new Vector2(x+.31f,top),()=>{selectedCharacter=index;equipmentPage=0;ShowEquipment(c);});
                IntermissionPortrait(card,c,new Vector2(.08f,.25f),new Vector2(.92f,.97f));
                FormationText(card,c.DisplayName.Split(' ')[0]+" Lv."+FormationLevel(index),new Vector2(.02f,.025f),new Vector2(.98f,.24f),16,TextAlignmentOptions.Center);
            }
            if(members.Length==0)FormationText(party,"출전할 동료를 편성하세요.",new Vector2(.04f,.4f),new Vector2(.96f,.7f),22,TextAlignmentOptions.Center);
            FormationButton(party,"동료의 이야기 · 30","동료의 이야기",new Vector2(.04f,.12f),new Vector2(.96f,.23f),ShowPersonalScenarios,!battle.TrainingMode,17);
            FormationText(party,"동료를 선택하면 장비를 정비합니다.",new Vector2(.04f,.035f),new Vector2(.96f,.105f),15).color=new Color(.7f,.77f,.85f);
            string[] names={"출전 편성","장비 관리","장비 상점","성장 · 승급"};string[] labels={"편성","장비","상점","성장"};
            Action[] actions={ShowDeployment,ShowEquipmentRoster,()=>ShowShop(),ShowGrowthRoster};
            for(int i=0;i<4;i++)
            {
                var button=FormationButton(footer,names[i],labels[i],new Vector2(.02f+i*.17f,.32f),new Vector2(.18f+i*.17f,.91f),actions[i],true,21);
                MenuArt(button,i+1,new Vector2(.02f,.08f),new Vector2(.40f,.94f));
                Place(button.GetComponentInChildren<TMP_Text>().rectTransform,new Vector2(.40f,.05f),new Vector2(.97f,.95f),Vector2.zero,Vector2.zero);
            }
            bool finished=!battle.TrainingMode&&battle.Catalog.CrossoverReady&&battle.Campaign.StoryProgress.Contains(CrossoverCampaign.Flag(20));
            var start=FormationButton(footer,"전투 시작",battle.TrainingMode?"훈련 시작":finished?"메인 이야기 완료":"이야기 시작",new Vector2(.74f,.37f),new Vector2(.97f,.86f),battle.RequestBattle,battle.Deployment.Count>0&&!finished,22);IntermissionGoldButton(start);
            FormationText(footer,battle.SaveNotice??"",new Vector2(.025f,.015f),new Vector2(.975f,.30f),15).color=new Color(.72f,.79f,.82f);
        }
        string PreparationDefeat()=>!battle.TrainingMode&&battle.SelectedStage==16?"진행 중인 전선의 아군 전원 전투불능":!battle.TrainingMode&&battle.SelectedStage==21?"선두 출전 아군 전투불능 또는 아군 전멸":!battle.TrainingMode&&CampaignMissions.Kind(battle.SelectedStage)==ObjectiveKind.Escort?"아군 전멸 또는 호위 대상 전투불능":"아군 전원 전투불능";
    }
}
