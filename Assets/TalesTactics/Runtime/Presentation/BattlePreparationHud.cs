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
        void RenderEquipmentRoster()
        {
            BeginPreparation("장비 관리");CharacterCards(i=>{selectedCharacter=i;ShowEquipment(battle.Catalog.Characters[i]);},false);
            Label(left,"장비 관리\n\n캐릭터를 선택한 뒤\n무기·방어구·장신구를\n변경하세요.",20,210,21);
            Label(commands,"장비는 파티가 공유합니다.\n다른 캐릭터가 장착한 수량은\n중복해서 사용할 수 없습니다.",20,150,18);
            Button(commands,"장비 상점",210,()=>ShowShop());Button(commands,"출전 준비로",266,ShowDeployment);
            Label(footer,"목록에서 직접 선택하고 변경 전후 능력치를 비교할 수 있습니다.",20,60,18);
        }
        string StatChange(string name,int before,int after)=>name+"  "+before+" → "+(before==after?after.ToString():"<color="+(after>before?"#82E8AC":"#FFA490")+">"+after+" ("+(after>before?"+":"")+(after-before)+")</color>");
        void RenderEquipment(CharacterData c,EquipmentLoadout draft,string notice)
        {
            if(battle.Session!=null)return;BeginPreparation("장비 교체");
            var progress=battle.Campaign.Get(c.Id);var current=draft??new EquipmentLoadout(c,progress,battle.Catalog.Equipment,battle.Campaign);
            int level=battle.TrainingMode?25:Mathf.Clamp(progress.Level,1,50);bool promoted=!battle.TrainingMode&&progress.Promoted;
            var before=new EquipmentLoadout(c,progress,battle.Catalog.Equipment,battle.Campaign).Preview(level,promoted);var after=current.Preview(level,promoted);
            Label(left,c.DisplayName+" · Lv"+level,12,64,22);Portrait(left,c,new Vector2(20,-210),new Vector2(118,-82));
            Label(left,"현재 → 변경 후",230,30,17);
            Label(left,string.Join("\n",new[]{StatChange("HP",before.HP,after.HP),StatChange("MP",before.MP,after.MP),StatChange("STR",before.STR,after.STR),StatChange("MAG",before.MAG,after.MAG),StatChange("DEF",before.DEF,after.DEF),StatChange("MDF",before.MDF,after.MDF),StatChange("SPD",before.SPD,after.SPD),StatChange("MOV",before.MOV,after.MOV),StatChange("JMP",before.JMP,after.JMP)}),268,290,17);
            foreach(EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {var chosen=slot;var item=current.Get(slot);Button(commands,SlotName(slot)+": "+(item==null?"없음":item.DisplayName),20+(int)slot*66,()=>{selectedEquipmentSlot=chosen;equipmentPage=0;RenderEquipment(c,current,null);},true,54);}
            Label(center,SlotName(selectedEquipmentSlot)+" · 보유한 호환 장비",12,36,21);
            var options=current.Options(selectedEquipmentSlot);int pages=Math.Max(1,(options.Length+7)/8);equipmentPage=Mathf.Clamp(equipmentPage,0,pages-1);
            Button(center,"장비 해제",58,()=>{current.Equip(selectedEquipmentSlot,null);RenderEquipment(c,current,null);},true,38);
            for(int i=equipmentPage*8;i<Math.Min(options.Length,(equipmentPage+1)*8);i++)
            {var item=options[i];Button(center,"장착 후보: "+item.DisplayName,110+(i%8)*48,()=>{current.Equip(selectedEquipmentSlot,item);RenderEquipment(c,current,null);},true,40);}
            Button(center,"이전 장비 후보",510,()=>{equipmentPage--;RenderEquipment(c,current,null);},equipmentPage>0);HalfButton(center,0);
            Button(center,"다음 장비 후보",510,()=>{equipmentPage++;RenderEquipment(c,current,null);},equipmentPage+1<pages);HalfButton(center,1);
            var selected=current.Get(selectedEquipmentSlot);Label(commands,selected==null?"목록에서 장비를 선택하세요.":selected.DisplayName+"\n"+EquipmentBonusText(selected),230,150,18);
            Button(commands,"적용 · 저장",398,()=>{bool saved=current.TrySave(battle.Campaign,battle.PersistCampaign);RenderEquipment(c,current,saved?"장비를 저장했습니다.":"수량 부족 또는 저장 실패. 기존 장비를 유지했습니다.");},true,48);
            Button(commands,"돌아가기 (미적용 취소)",464,ShowEquipmentRoster);Button(commands,"출전 준비로",516,ShowDeployment);
            Label(footer,notice??"장비를 고르면 증감이 표시됩니다. 적용 · 저장으로 확정하고, 돌아가면 미적용 변경을 취소합니다.",16,74,17);
        }
        static string SlotName(EquipmentSlot slot)=>slot==EquipmentSlot.Weapon?"무기":slot==EquipmentSlot.Armor?"방어구":"장신구";
    }
}
