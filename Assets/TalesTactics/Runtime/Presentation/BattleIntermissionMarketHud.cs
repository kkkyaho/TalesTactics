using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        void RenderIntermissionMarket(bool selling,int page,EquipmentData selected,string notice)
        {
            if(battle.Session!=null)return;BeginIntermission("장비 상점",2);
            var items=(battle.Catalog.Equipment??Array.Empty<EquipmentData>()).Where(e=>e!=null&&(selling?CampaignInventory.Owned(battle.Campaign,e.Id)>0:e.BuyPrice>0)&&(shopSlot<0||(int)e.Slot==shopSlot)).ToArray();
            const int pageSize=6;int pages=Math.Max(1,(items.Length+pageSize-1)/pageSize);page=Mathf.Clamp(page,0,pages-1);int current=page;
            var buy=FormationButton(center,"구매","구매",new Vector2(.01f,.9f),new Vector2(.5f,.99f),()=>RenderMarket(false,0));IntermissionSelection(buy,!selling);
            var sell=FormationButton(center,"장비 매각","장비 매각",new Vector2(.5f,.9f),new Vector2(.99f,.99f),()=>RenderMarket(true,0));IntermissionSelection(sell,selling);
            string[] groups={"전체","무기","방어구","장신구"};
            for(int i=0;i<groups.Length;i++)
            {
                int filter=i-1;string label=(shopSlot==filter?"● ":"")+groups[i];
                var b=FormationButton(center,label,groups[i],new Vector2(.01f+i*.245f,.81f),new Vector2(.01f+(i+1)*.245f,.9f),()=>{shopSlot=filter;RenderMarket(selling,0);});IntermissionSelection(b,shopSlot==filter);
            }
            for(int i=page*pageSize;i<Math.Min(items.Length,(page+1)*pageSize);i++)
            {
                var item=items[i];int slot=i%pageSize;float x=.01f+slot%3*.327f,top=.8f-slot/3*.351f;
                string requirement=CampaignInventory.PurchaseUnlockRequirement(battle.Campaign,item);
                string name=item.DisplayName+(selling?" · 매각 "+CampaignInventory.SellPrice(item)+"G":" · "+item.BuyPrice+"G"+(requirement!=null?" · 잠김":""));
                var card=FormationButton(center,name,"",new Vector2(x,top-.343f),new Vector2(x+.327f,top),()=>RenderMarket(selling,current,item));IntermissionSelection(card,selected==item);
                EquipmentArt(card,item,new Vector2(.16f,.30f),new Vector2(.84f,.91f));
                FormationText(card,"보유 "+CampaignInventory.Owned(battle.Campaign,item.Id),new Vector2(.50f,.86f),new Vector2(.99f,.99f),16,TextAlignmentOptions.MidlineRight).color=formationTeal;
                FormationText(card,item.DisplayName,new Vector2(.02f,.15f),new Vector2(.98f,.31f),20,TextAlignmentOptions.Center);
                FormationText(card,(!selling&&requirement!=null?"잠김 · ":"")+(selling?CampaignInventory.SellPrice(item):item.BuyPrice).ToString("N0")+" G",new Vector2(.02f,0),new Vector2(.98f,.16f),20,TextAlignmentOptions.Center).color=formationGold;
            }
            if(items.Length==0)FormationText(center,selling?"이 분류에 매각할 장비가 없습니다.":"이 분류에 표시할 장비가 없습니다.",new Vector2(.05f,.3f),new Vector2(.95f,.65f),23,TextAlignmentOptions.Center);
            FormationButton(center,selling?"이전 장비":"이전 상품","이전",new Vector2(.02f,.015f),new Vector2(.25f,.095f),()=>RenderMarket(selling,current-1),page>0);
            FormationText(center,(page+1)+" / "+pages,new Vector2(.25f,.015f),new Vector2(.75f,.095f),18,TextAlignmentOptions.Center);
            FormationButton(center,selling?"다음 장비":"다음 상품","다음",new Vector2(.75f,.015f),new Vector2(.98f,.095f),()=>RenderMarket(selling,current+1),page+1<pages);
            FormationText(commands,"상품 정보",new Vector2(.025f,.91f),new Vector2(.975f,1),25).color=formationGold;
            if(selected==null)
            {
                FormationIcon(commands,"attack",new Vector2(.36f,.60f),new Vector2(.64f,.8f),formationGold);
                FormationText(commands,"목록에서 장비를 선택하세요.\n능력치·가격·수량을 비교한 뒤\n거래를 확정합니다.",new Vector2(.06f,.30f),new Vector2(.94f,.60f),22,TextAlignmentOptions.Center);
            }
            else
            {
                var item=selected;int price=selling?CampaignInventory.SellPrice(item):item.BuyPrice;
                EquipmentArt(commands,item,new Vector2(.025f,.65f),new Vector2(.46f,.91f));
                FormationText(commands,item.DisplayName,new Vector2(.48f,.82f),new Vector2(.98f,.91f),24);
                FormationText(commands,SlotName(item.Slot)+"\n"+ReadableBonus(item),new Vector2(.48f,.64f),new Vector2(.98f,.82f),19).color=formationGold;
                var stock=IntermissionPanel(commands,"Stock",new Vector2(.025f,.56f),new Vector2(.975f,.65f));
                FormationText(stock,"보유 "+CampaignInventory.Owned(battle.Campaign,item.Id)+"  /  장착 중 "+CampaignInventory.Equipped(battle.Campaign,item.Id)+"  /  미장착 "+CampaignInventory.Available(battle.Campaign,item.Id),Vector2.zero,Vector2.one,17,TextAlignmentOptions.Center);
                FormationText(commands,selling?"매각 정보":"구매 정보",new Vector2(.04f,.48f),new Vector2(.96f,.56f),22).color=formationGold;
                FormationText(commands,"거래 수량                     1개\n"+(selling?"매각가":"가격")+"                     "+price.ToString("N0")+" G\n현재 골드             "+battle.Campaign.Gold.ToString("N0")+" G",new Vector2(.04f,.31f),new Vector2(.96f,.48f),19);
                long balance=selling?(long)battle.Campaign.Gold+price:(long)battle.Campaign.Gold-price;
                FormationText(commands,"거래 후    "+(balance<0?"골드 부족":balance.ToString("N0")+" G"),new Vector2(.04f,.23f),new Vector2(.96f,.31f),24).color=balance<0?new Color(1,.63f,.56f):formationTeal;
                string reason=selling?SaleBlocked(item):ShopUnavailable(item);
                FormationText(commands,reason??"1개 거래 · 즉시 저장됩니다.",new Vector2(.04f,.12f),new Vector2(.96f,.23f),17);
                FormationButton(commands,selling?"보유 장비 목록으로":"상품 목록으로","선택 해제",new Vector2(.025f,.015f),new Vector2(.40f,.11f),()=>RenderMarket(selling,current));
                if(!selling)FormationButton(commands,"구매 장비 장착","구매 장비 장착",new Vector2(.40f,.015f),new Vector2(.975f,.11f),()=>ShowPurchaseRecipients(item,current),CampaignInventory.Available(battle.Campaign,item.Id)>0);
                IntermissionPrimary(selling?"1개 매각 · 저장":"구매 · 저장",selling?"1개 매각 · 저장":"1개 구매 · 저장",()=>
                {
                    string blocked=selling?SaleBlocked(item):ShopUnavailable(item);
                    if(blocked!=null){RenderMarket(selling,current,item,blocked);return;}
                    bool saved=selling?CampaignInventory.TrySell(battle.Campaign,item,battle.PersistCampaign):CampaignInventory.TryBuy(battle.Campaign,item,battle.PersistCampaign);
                    RenderMarket(selling,current,item,saved?item.DisplayName+(selling?" 1개를 매각했습니다.":" 1개를 구매했습니다."):"저장 실패. 소지금과 장비 수량을 유지했습니다.");
                },reason==null);
            }
            if(selected==null)IntermissionPrimary("거래 대기","장비를 선택하세요",()=>{},false);
            IntermissionNotice(notice??(selling?"장착 중인 장비는 매각할 수 없습니다. 미장착 수량만 판매합니다.":"구매 후 ‘구매 장비 장착’으로 이동하여 장착을 확정하세요."));
            FormationButton(footer,"출전 준비로","출전 준비로",new Vector2(.55f,.14f),new Vector2(.74f,.86f),ShowDeployment);
        }
    }
}
