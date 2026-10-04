using System;
using System.Linq;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        int shopSlot=-1;
        void RenderMarket(bool selling,int page,EquipmentData selected=null,string notice=null)
        {
            if(battle.Session!=null)return;BeginPreparation(selling?"상점 · 판매":"상점 · 구매");
            var items=(battle.Catalog.Equipment??Array.Empty<EquipmentData>()).Where(e=>e!=null&&(selling?CampaignInventory.Owned(battle.Campaign,e.Id)>0:e.BuyPrice>0)&&(shopSlot<0||(int)e.Slot==shopSlot)).ToArray();
            int pages=Math.Max(1,(items.Length+7)/8);page=Math.Max(0,Math.Min(page,pages-1));int current=page;
            Label(left,"장비 상점",14,36,23);
            Button(left,"구매",66,()=>RenderMarket(false,0));Button(left,"장비 매각",114,()=>RenderMarket(true,0));
            Label(left,"분류",182,30,18);
            string[] groups={"전체","무기","방어구","장신구"};
            for(int i=0;i<groups.Length;i++){int filter=i-1;Button(left,(shopSlot==filter?"● ":"")+groups[i],226+i*48,()=>{shopSlot=filter;RenderMarket(selling,0);});}
            Button(left,"장비 관리",448,ShowEquipmentRoster);Button(left,"출전 준비로",500,ShowDeployment);
            Label(center,(selling?"보유 장비":"상품 목록")+"  "+(page+1)+" / "+pages,12,36,21);
            for(int i=page*8;i<Math.Min(items.Length,(page+1)*8);i++)
            {
                var item=items[i];string name=item.DisplayName+(selling?" · 매각 "+CampaignInventory.SellPrice(item)+"G":" · "+item.BuyPrice+"G"+(CampaignInventory.PurchaseUnlockRequirement(battle.Campaign,item)!=null?" · 잠김":""));
                Button(center,name,60+(i%8)*52,()=>RenderMarket(selling,current,item),true,44);
                if(selected==item)center.GetChild(center.childCount-1).GetComponent<UnityEngine.UI.Image>().color=new UnityEngine.Color(.2f,.28f,.38f);
            }
            if(items.Length==0)Label(center,selling?"이 분류에 매각할 장비가 없습니다.":"이 분류에 표시할 장비가 없습니다.",80,90,20);
            Button(center,selling?"이전 장비":"이전 상품",490,()=>RenderMarket(selling,current-1),page>0);HalfButton(center,0);
            Button(center,selling?"다음 장비":"다음 상품",490,()=>RenderMarket(selling,current+1),page+1<pages);HalfButton(center,1);
            if(selected==null)
            {Label(commands,"소지금 "+battle.Campaign.Gold+" G\n\n목록에서 장비를 선택하세요.\n능력치·가격·수량을 비교한 뒤\n거래를 확정합니다.",20,210,20);}
            else
            {
                var item=selected;int price=selling?CampaignInventory.SellPrice(item):item.BuyPrice;
                Label(commands,item.DisplayName,12,60,23);
                Label(commands,SlotName(item.Slot)+"\n"+EquipmentBonusText(item)+"\n\n보유 "+CampaignInventory.Owned(battle.Campaign,item.Id)+" / 장착 중 "+CampaignInventory.Equipped(battle.Campaign,item.Id)+"\n미장착 "+CampaignInventory.Available(battle.Campaign,item.Id),80,196,18);
                Label(commands,(selling?"매각가 ":"가격 ")+price+" G\n거래 후 "+(selling?(long)battle.Campaign.Gold+price:Math.Max(0,battle.Campaign.Gold-price))+" G",280,62,20);
                string reason=selling?SaleBlocked(item):ShopUnavailable(item);
                Label(commands,reason??"1개 거래 · 저장",348,50,16);
                Button(commands,selling?"1개 매각 · 저장":"구매 · 저장",408,()=>
                {
                    string blocked=selling?SaleBlocked(item):ShopUnavailable(item);
                    if(blocked!=null){RenderMarket(selling,current,item,blocked);return;}
                    bool saved=selling?CampaignInventory.TrySell(battle.Campaign,item,battle.PersistCampaign):CampaignInventory.TryBuy(battle.Campaign,item,battle.PersistCampaign);
                    RenderMarket(selling,current,item,saved?item.DisplayName+(selling?" 1개를 매각했습니다.":" 1개를 구매했습니다."):"저장 실패. 소지금과 장비 수량을 유지했습니다.");
                },reason==null,44);
                Button(commands,selling?"보유 장비 목록으로":"상품 목록으로",464,()=>RenderMarket(selling,current));
                if(!selling)Button(commands,"구매 장비 장착",514,()=>ShowPurchaseRecipients(item,current),CampaignInventory.Available(battle.Campaign,item.Id)>0);
            }
            Label(footer,notice??(selling?"미장착 수량만 판매할 수 있습니다. 거래 확정 후에도 현재 분류와 페이지를 유지합니다.":"구매 후 ‘구매 장비 장착’에서 캐릭터를 선택할 수 있습니다. 장착은 별도로 적용 · 저장하세요."),16,72,17);
        }
        void ShowPurchaseRecipients(EquipmentData item,int page)
        {
            BeginPreparation("장착할 캐릭터 선택");Label(left,item.DisplayName+"\n\n"+EquipmentBonusText(item),20,240,21);
            Label(center,"장착 가능한 캐릭터",12,36,22);int row=0;
            foreach(var c in battle.Catalog.Characters)
            {
                var draft=new EquipmentLoadout(c,battle.Campaign.Get(c.Id),battle.Catalog.Equipment,battle.Campaign);
                if(!draft.Options(item.Slot).Contains(item))continue;
                Button(center,c.DisplayName,64+row++*48,()=>{draft.Equip(item.Slot,item);selectedEquipmentSlot=item.Slot;equipmentPage=0;ShowEquipment(c,draft);});
            }
            if(row==0)Label(center,"현재 장착 가능한 캐릭터가 없습니다.",64,100,19);
            Label(commands,"캐릭터를 선택하면\n장비 변경 내용을 비교합니다.\n‘적용 · 저장’으로 확정하세요.",20,140,19);
            Button(commands,"상품 목록으로",204,()=>RenderMarket(false,page,item));Button(commands,"출전 준비로",260,ShowDeployment);
        }
    }
}
