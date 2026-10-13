using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        bool hubLayout, focusedEquipmentLayout;
        public bool IntermissionHubOpen => hubLayout;
        void HubLayout()
        {
            Place(header,new Vector2(0,1),Vector2.one,new Vector2(12,-82),new Vector2(-12,-12));
            Place(center,Vector2.zero,Vector2.one,new Vector2(12,122),new Vector2(-12,-94));
            Place(footer,Vector2.zero,new Vector2(1,0),new Vector2(12,12),new Vector2(-12,110));
        }
        public void ShowIntermissionHub()
        {
            if(battle.Session!=null||battle.StoryActive)return;
            BeginPreparation("인터미션");hubLayout=true;left.gameObject.SetActive(false);commands.gameObject.SetActive(false);HubLayout();
            StoryBackdrop(center);
            var title=header.GetComponentInChildren<TMP_Text>();title.text="INTERMISSION";title.fontSizeMax=25;
            FormationText(header,battle.Campaign.Gold.ToString("N0")+" G",new Vector2(.30f,0),new Vector2(.56f,1),22,TextAlignmentOptions.Center).color=formationGold;
            var mission=FormationRect(center,"HubMission",new Vector2(.03f,.03f),new Vector2(.44f,.96f));
            FormationText(mission,battle.TrainingMode?"TRAINING":"NEXT STORY",new Vector2(0,.85f),new Vector2(1,.98f),17).color=formationGold;
            FormationText(mission,CampaignStages.Title(battle.SelectedStage),new Vector2(0,.61f),new Vector2(1,.84f),34);
            FormationText(mission,battle.TrainingMode?"훈련 · Lv25 · 저장 보상 없음":BriefVictory(null,battle.SelectedStage,false),new Vector2(0,.40f),new Vector2(1,.60f),22);
            FormationButton(mission,"임무 선택","임무 변경",new Vector2(0,.26f),new Vector2(.46f,.39f),()=>ShowChapterPage(battle.SelectedStage/3));
            FormationButton(mission,"임무 · 적 정보","임무 정보",new Vector2(.48f,.26f),new Vector2(.96f,.39f),ShowMission);
            var start=FormationButton(mission,"전투 시작",battle.TrainingMode?"훈련 시작  ›":"이야기 시작  ›",new Vector2(0,.03f),new Vector2(.96f,.20f),battle.RequestBattle,battle.Deployment.Count>0,25);IntermissionGoldButton(start);
            var party=FormationRect(center,"HubParty",new Vector2(.48f,.06f),new Vector2(.97f,.96f));
            FormationText(party,"출전 멤버  ·  "+battle.Deployment.Count+" / "+battle.Catalog.Rules.MaxDeployment,new Vector2(0,.88f),Vector2.one,23);
            var members=battle.Deployment.Where(i=>i>=0&&i<battle.Catalog.Characters.Length).ToArray();
            int columns=members.Length>4?3:Math.Max(2,members.Length),rows=members.Length>4?2:1;
            for(int n=0;n<members.Length;n++)
            {
                int index=members[n];var c=battle.Catalog.Characters[index];float x=n%columns/(float)columns,top=rows==1?.78f:.86f-n/columns*(.74f/rows),bottom=rows==1?.30f:top-.72f/rows;
                var card=FormationButton(party,"거점 캐릭터: "+c.Id,"",new Vector2(x,bottom),new Vector2(x+1f/columns,top),()=>{selectedCharacter=index;equipmentPage=0;ShowEquipment(c);});
                IntermissionPortrait(card,c,new Vector2(.02f,.24f),new Vector2(.98f,.98f));
                FormationText(card,c.DisplayName.Split(' ')[0]+"  Lv."+FormationLevel(index),new Vector2(.03f,.02f),new Vector2(.97f,.23f),19,TextAlignmentOptions.Center);
            }
            if(members.Length==0)FormationText(party,"출전할 동료를 편성하세요.",new Vector2(0,.3f),new Vector2(1,.7f),24,TextAlignmentOptions.Center);
            FormationText(party,"동료 선택 · 장비 정비",Vector2.zero,new Vector2(1,.10f),16).color=new Color(.7f,.77f,.85f);
            string[] names={"출전 편성","장비 관리","장비 상점","성장 · 승급"};string[] labels={"편성","장비","상점","성장"};string[] icons={"move","attack","accessory","skill"};
            Action[] actions={ShowDeployment,ShowEquipmentRoster,()=>ShowShop(),ShowGrowthRoster};
            for(int i=0;i<4;i++)
            {
                var button=FormationButton(footer,names[i],labels[i],new Vector2(i*.25f,.36f),new Vector2((i+1)*.25f,.99f),actions[i],true,24);
                FormationIcon(button,icons[i],new Vector2(.08f,.25f),new Vector2(.25f,.75f),formationGold);
            }
            if(!string.IsNullOrEmpty(battle.SaveNotice))FormationText(footer,battle.SaveNotice,new Vector2(.015f,0),new Vector2(.985f,.33f),16).color=formationGold;
        }
        void IntermissionGoldButton(RectTransform button)
        {
            StyleButton(button,HudRole.Primary);
            button.GetComponent<UnityEngine.UI.Image>().color=new Color(.87f,.76f,.55f);
            button.GetComponentInChildren<TMP_Text>().color=new Color(.06f,.10f,.16f);
            button.GetComponent<UnityEngine.UI.Outline>().effectColor=formationGold;
        }
        void EquipmentChangedStats(Transform parent,Stats before,Stats after)
        {
            string[] names={"HP","MP","공격","마력","방어","마방","민첩","이동","도약"};
            int[] a={before.HP,before.MP,before.STR,before.MAG,before.DEF,before.MDF,before.SPD,before.MOV,before.JMP};
            int[] b={after.HP,after.MP,after.STR,after.MAG,after.DEF,after.MDF,after.SPD,after.MOV,after.JMP};
            var changed=Enumerable.Range(0,names.Length).Where(i=>a[i]!=b[i]).ToArray();
            FormationText(parent,"장착 시 변화",new Vector2(.02f,.70f),new Vector2(.98f,.97f),18).color=formationGold;
            for(int j=0;j<Math.Min(3,changed.Length);j++)
            {int i=changed[j];float x=j/3f;FormationText(parent,StatChange(names[i],a[i],b[i]),new Vector2(x,.28f),new Vector2(x+1/3f,.70f),24);}
            if(changed.Length==0)FormationText(parent,"능력치 변화 없음",new Vector2(.02f,.28f),new Vector2(.98f,.70f),23);
            FormationButton(parent,"전체 능력치 보기",changed.Length>3?"전체 비교 · 추가 "+(changed.Length-3)+"개":"전체 능력치 보기",new Vector2(.02f,0),new Vector2(.48f,.28f),()=>
            {
                PrepareUnitModal(new UnitRuntime(IntermissionCharacter(),Team.Player,battle.Catalog.Rules));Clear(unitDetails);
                FormationText(unitDetails,"능력치 비교",new Vector2(.04f,.86f),new Vector2(.96f,.98f),25).color=formationGold;
                IntermissionStats(unitDetails,before,after,new Vector2(.04f,.16f),new Vector2(.96f,.86f));
                FormationButton(unitDetails,"능력치 비교 닫기","닫기",new Vector2(.5f,.025f),new Vector2(.96f,.145f),()=>CloseUnitDetails());
            });
        }
    }
}
