using System;
using System.Linq;

namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        void ShowSellShop(int page=0){RenderMarket(true,page);}
        void ShowSellItem(EquipmentData item,int page,string notice=null){RenderMarket(true,page,item,notice);}

        string SaleBlocked(EquipmentData item)
        {
            if (battle.TrainingMode) return "훈련 모드에서는 매각할 수 없습니다.";
            if (!battle.CanSave) return "저장 보호 상태에서는 매각할 수 없습니다.";
            return CampaignInventory.SaleUnavailable(battle.Campaign, item);
        }
    }
}
