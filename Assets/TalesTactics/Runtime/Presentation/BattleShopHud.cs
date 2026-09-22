using System;
using System.Collections.Generic;
using System.Linq;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        void ShowShop(int page=0)
        {
            if(battle.Session!=null)return;
            Clear(header);Clear(left);Clear(commands);Clear(footer);
            Label(header,"TALES / TACTICS     ·     장비 상점",12,45,25);
            var stock=(battle.Catalog.Equipment??Array.Empty<EquipmentData>()).Where(e=>e!=null&&e.BuyPrice>0).ToArray();
            const int pageSize=8;int pages=Math.Max(1,(stock.Length+pageSize-1)/pageSize);
            page=Math.Max(0,Math.Min(page,pages-1));int currentPage=page;
            Label(left,$"상품 목록  {page+1} / {pages}",12,35,20);
            for(int i=page*pageSize;i<Math.Min(stock.Length,(page+1)*pageSize);i++)
            {
                var item=stock[i];
                Button(left,item.DisplayName+" · "+item.BuyPrice+"G"+(CampaignInventory.PurchaseUnlockRequirement(battle.Campaign,item)!=null?" · 잠김":""),60+(i%pageSize)*48,()=>ShowShopItem(item,currentPage),true,40);
            }
            if(page>0)Button(left,"이전 상품",460,()=>ShowShop(currentPage-1));
            if(page+1<pages)Button(left,"다음 상품",506,()=>ShowShop(currentPage+1));
            Label(commands,$"소지금 {battle.Campaign.Gold}G\n\n장비를 선택하면 능력치와\n보유 수량을 확인합니다.",20,180,20);
            Button(commands,"장비 매각",240,()=>ShowSellShop());
            Button(commands,"출전 준비로",296,ShowDeployment);
            Label(footer,"장비는 파티가 공유합니다. 장착 중인 수량도 보유 수량에 포함됩니다.\n캠페인 승리 보상: 1장 120G / 2장 180G. 훈련에서는 구매할 수 없습니다.",14,88,17);
        }

        void ShowShopItem(EquipmentData item,int page,string notice=null)
        {
            if(battle.Session!=null)return;
            Clear(header);Clear(left);Clear(commands);Clear(footer);
            Label(header,"TALES / TACTICS     ·     장비 구매",12,45,25);
            string slot=item.Slot==EquipmentSlot.Weapon?"무기 · "+item.Weapon:item.Slot==EquipmentSlot.Armor?"방어구":"장신구";
            Label(left,item.DisplayName,12,65,23);
            Label(left,slot+"\n\n"+EquipmentBonusText(item)+"\n\n보유 "+CampaignInventory.Owned(battle.Campaign,item.Id)+
                " / 장착 중 "+CampaignInventory.Equipped(battle.Campaign,item.Id)+"\n미장착 "+CampaignInventory.Available(battle.Campaign,item.Id),90,360,19);
            Label(commands,$"가격 {item.BuyPrice}G\n소지금 {battle.Campaign.Gold}G\n\n1개 구매 후\n남은 소지금 {Math.Max(0,battle.Campaign.Gold-item.BuyPrice)}G",20,210,20);
            string reason=ShopUnavailable(item);
            Label(commands,reason??"장비 1개를 구매할 수 있습니다.",250,85,18);
            Button(commands,"구매 · 저장",360,()=>
            {
                string blocked=ShopUnavailable(item);
                if(blocked!=null){ShowShopItem(item,page,blocked);return;}
                bool saved=CampaignInventory.TryBuy(battle.Campaign,item,battle.PersistCampaign);
                ShowShopItem(item,page,saved?item.DisplayName+" 1개를 구매했습니다. 장비 관리에서 장착하세요.":"저장 실패. 소지금과 장비 수량은 유지했습니다.");
            },reason==null,48);
            Button(commands,"상품 목록으로",425,()=>ShowShop(page));
            Button(commands,"출전 준비로",477,ShowDeployment);
            Label(footer,notice??"구매 · 저장을 누르면 소지금을 지불하고 장비 1개를 받습니다.\n저장에 실패하면 소지금과 보유 수량을 되돌립니다.",14,88,17);
        }

        string ShopUnavailable(EquipmentData item)
        {
            if(battle.TrainingMode)return "훈련 모드에서는 구매할 수 없습니다.";
            if(!CampaignStorage.CanSave)return "저장 보호 상태에서는 구매할 수 없습니다.";
            return CampaignInventory.PurchaseUnavailable(battle.Campaign,item);
        }

        static string EquipmentBonusText(EquipmentData item)
        {
            var bonus=item.Bonus;var lines=new List<string>();
            string[] names={"HP","MP","STR","MAG","DEF","MDF","SPD","MOV","JMP"};
            int[] values={bonus.HP,bonus.MP,bonus.STR,bonus.MAG,bonus.DEF,bonus.MDF,bonus.SPD,bonus.MOV,bonus.JMP};
            for(int i=0;i<names.Length;i++)if(values[i]!=0)lines.Add(names[i]+" "+(values[i]>0?"+":"")+values[i]);
            return lines.Count==0?"능력치 보너스 없음":string.Join("\n",lines);
        }
    }
}
