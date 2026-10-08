using System;
using System.Linq;
using UnityEngine;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        RectTransform center;
        bool preparationLayout;
        int selectedCharacter;
        EquipmentSlot selectedEquipmentSlot;
        int equipmentPage;
        void PreparationLayout()
        {
            if(intermissionLayout){IntermissionLayout();return;}
            if(modelDeploymentLayout){ModelDeploymentLayout();return;}
            Place(header,new Vector2(0,1),Vector2.one,new Vector2(12,-82),new Vector2(-12,-12));
            Place(footer,Vector2.zero,new Vector2(1,0),new Vector2(12,12),new Vector2(-12,114));
            if(preparationLayout)
            {
                Place(left,Vector2.zero,new Vector2(.23f,1),new Vector2(12,126),new Vector2(-6,-94));
                Place(center,new Vector2(.23f,0),new Vector2(.74f,1),new Vector2(6,126),new Vector2(-6,-94));
                Place(commands,new Vector2(.74f,0),Vector2.one,new Vector2(6,126),new Vector2(-12,-94));
            }
            else
            {
                Place(left,Vector2.zero,new Vector2(.5f,1),new Vector2(12,126),new Vector2(-6,-94));
                Place(commands,new Vector2(.5f,0),Vector2.one,new Vector2(6,126),new Vector2(-12,-94));
            }
        }
        void BeginPreparation(string title)
        {
            CloseUnitDetails();Clear(header);Clear(left);Clear(commands);Clear(footer);
            if(center==null)center=Panel("PreparationContent",new Vector2(.23f,0),new Vector2(.74f,1),new Vector2(6,126),new Vector2(-6,-94));
            Clear(center);preparationLayout=true;center.gameObject.SetActive(true);PreparationLayout();
            var heading=Label(header,title+"    ·    "+battle.Campaign.Gold+" G",16,38,25);heading.rectTransform.offsetMax=new Vector2(-400,heading.rectTransform.offsetMax.y);DrawHelpEntry(true);DrawSystemEntry(true);
        }
        void Portrait(Transform parent,CharacterData c,Vector2 low,Vector2 high)
        {
            var g=new GameObject("Portrait",typeof(RectTransform),typeof(UnityEngine.UI.Image));g.transform.SetParent(parent,false);
            Place(g.GetComponent<RectTransform>(),new Vector2(0,1),new Vector2(0,1),low,high);
            var image=g.GetComponent<UnityEngine.UI.Image>();image.sprite=c.Sprites?.Front;image.preserveAspect=true;image.raycastTarget=false;
        }
        void CharacterCards(Action<int> click,bool deployment)
        {
            Label(center,deployment?"편성 · "+battle.Deployment.Count+" / 6":"캐릭터 선택",12,32,21);
            for(int i=0;i<battle.Catalog.Characters.Length;i++)
            {
                int index=i;var c=battle.Catalog.Characters[i];var p=battle.Campaign.Get(c.Id);
                string name=deployment?(battle.Deployment.Contains(i)?"● ":"○ ")+c.DisplayName:c.DisplayName;
                Button(center,name,54+(i/2)*110,()=>click(index),true,100);var card=(RectTransform)center.GetChild(center.childCount-1);
                card.anchorMin=new Vector2((i%2)*.5f,1);card.anchorMax=new Vector2((i%2+1)*.5f,1);card.sizeDelta=new Vector2(-18,100);
                if(i==selectedCharacter)card.GetComponent<UnityEngine.UI.Image>().color=new Color(.2f,.28f,.38f);
                var label=card.GetComponentInChildren<TMPro.TMP_Text>();label.text=c.DisplayName+"\nLv"+(battle.TrainingMode?25:p.Level)+" · "+c.Job+(deployment?"\n"+(battle.Deployment.Contains(i)?"출전 중":"대기 중"):"");
                label.text=c.DisplayName+"\nLv"+(battle.TrainingMode?25:p.Level)+" · "+TacticalDevelopment.Role(c)+(deployment?"\n"+(battle.Deployment.Contains(i)?"출전 중":"대기 중")+(p.Level<CampaignStages.Get(battle.SelectedStage).EntryLevel?" · 합류 훈련 가능":""):"");
                label.fontSize=16;label.alignment=TMPro.TextAlignmentOptions.MidlineLeft;label.rectTransform.offsetMin=new Vector2(80,label.rectTransform.offsetMin.y);label.rectTransform.offsetMax=new Vector2(-8,label.rectTransform.offsetMax.y);
                Portrait(card,c,new Vector2(10,-94),new Vector2(72,-6));
            }
        }
        void RenderDeployment() => RenderModelDeployment();
        void RenderEquipmentRoster() => ShowEquipment(IntermissionCharacter());
        string StatChange(string name,int before,int after)=>name+"  "+before+" → "+(before==after?after.ToString():"<color="+(after>before?"#82E8AC":"#FFA490")+">"+after+" ("+(after>before?"+":"")+(after-before)+")</color>");
        void RenderEquipment(CharacterData c,EquipmentLoadout draft,string notice) => RenderIntermissionEquipment(c,draft,notice);
        static string SlotName(EquipmentSlot slot)=>slot==EquipmentSlot.Weapon?"무기":slot==EquipmentSlot.Armor?"방어구":"장신구";
    }
}
