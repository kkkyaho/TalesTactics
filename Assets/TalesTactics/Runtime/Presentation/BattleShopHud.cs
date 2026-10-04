using System;
using System.Collections.Generic;
using System.Linq;
namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        void ShowShop(int page=0){RenderMarket(false,page);}
        void ShowShopItem(EquipmentData item,int page,string notice=null){RenderMarket(false,page,item,notice);}

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
