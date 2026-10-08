using System;
using System.Linq;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        int shopSlot=-1;
        void RenderMarket(bool selling,int page,EquipmentData selected=null,string notice=null) => RenderIntermissionMarket(selling,page,selected,notice);
        void ShowPurchaseRecipients(EquipmentData item,int page)
        {
            BeginIntermission("장착할 캐릭터 선택",1);
            FormationText(center,"장착 가능한 캐릭터",new UnityEngine.Vector2(.02f,.89f),new UnityEngine.Vector2(.98f,.99f),25).color=formationGold;
            int row=0;
            foreach(var c in battle.Catalog.Characters)
            {
                var draft=new EquipmentLoadout(c,FormationProgress(c),battle.Catalog.Equipment,battle.Campaign);
                if(!draft.Options(item.Slot).Contains(item))continue;
                int cell=row++;float x=.02f+(cell%5)*.192f,top=.88f-(cell/5)*.38f;
                var card=FormationButton(center,c.DisplayName,"",new UnityEngine.Vector2(x,top-.36f),new UnityEngine.Vector2(x+.192f,top),()=>{selectedCharacter=Array.IndexOf(battle.Catalog.Characters,c);draft.Equip(item.Slot,item);selectedEquipmentSlot=item.Slot;equipmentPage=0;ShowEquipment(c,draft);});
                IntermissionPortrait(card,c,new UnityEngine.Vector2(.03f,.22f),new UnityEngine.Vector2(.97f,.97f));
                FormationText(card,c.DisplayName,UnityEngine.Vector2.zero,new UnityEngine.Vector2(1,.22f),18,TMPro.TextAlignmentOptions.Center);
            }
            if(row==0)FormationText(center,"현재 장착 가능한 캐릭터가 없습니다.",new UnityEngine.Vector2(.05f,.35f),new UnityEngine.Vector2(.95f,.7f),23);
            FormationText(commands,item.DisplayName,new UnityEngine.Vector2(.04f,.86f),new UnityEngine.Vector2(.96f,.98f),26).color=formationGold;
            EquipmentArt(commands,item,new UnityEngine.Vector2(.2f,.5f),new UnityEngine.Vector2(.8f,.85f));
            FormationText(commands,ReadableBonus(item),new UnityEngine.Vector2(.06f,.32f),new UnityEngine.Vector2(.94f,.5f),21);
            FormationText(commands,"캐릭터를 선택하면 장비 변경 내용을 비교합니다.\n‘적용 · 저장’으로 확정하세요.",new UnityEngine.Vector2(.06f,.05f),new UnityEngine.Vector2(.94f,.30f),20);
            IntermissionNotice("장착 가능한 캐릭터만 표시합니다. 선택만으로 저장하지 않습니다.");
            FormationButton(footer,"상품 목록으로","상품 목록으로",new UnityEngine.Vector2(.55f,.14f),new UnityEngine.Vector2(.74f,.86f),()=>RenderMarket(false,page,item));
            IntermissionPrimary("출전 준비로","출전 준비로",ShowDeployment,true);
        }
    }
}