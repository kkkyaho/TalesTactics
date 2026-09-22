using System;
using System.Linq;

namespace TalesTactics
{
    public sealed partial class BattleHud
    {
        void ShowSellShop(int page = 0)
        {
            if (battle.Session != null) return;
            Clear(header); Clear(left); Clear(commands); Clear(footer);
            Label(header, "TALES / TACTICS     ·     장비 매각", 12, 45, 25);
            var items = (battle.Catalog.Equipment ?? Array.Empty<EquipmentData>())
                .Where(e => e != null && CampaignInventory.Owned(battle.Campaign, e.Id) > 0).ToArray();
            const int pageSize = 8;
            int pages = Math.Max(1, (items.Length + pageSize - 1) / pageSize);
            page = Math.Max(0, Math.Min(page, pages - 1)); int currentPage = page;
            Label(left, $"보유 장비  {page + 1} / {pages}", 12, 35, 20);
            for (int i = page * pageSize; i < Math.Min(items.Length, (page + 1) * pageSize); i++)
            {
                var item = items[i];
                Button(left, item.DisplayName + " · 매각 " + CampaignInventory.SellPrice(item) + "G",
                    60 + (i % pageSize) * 48, () => ShowSellItem(item, currentPage), true, 40);
            }
            if (page > 0) Button(left, "이전 장비", 460, () => ShowSellShop(currentPage - 1));
            if (page + 1 < pages) Button(left, "다음 장비", 506, () => ShowSellShop(currentPage + 1));
            Label(commands, $"소지금 {battle.Campaign.Gold}G\n\n" + (items.Length == 0 ? "매각할 장비가 없습니다." : "매각할 장비를 선택하세요."), 20, 180, 20);
            Button(commands, "상품 목록으로", 240, () => ShowShop());
            Button(commands, "출전 준비로", 296, ShowDeployment);
            Label(footer, "매각가는 구매가의 절반(소수점 버림)입니다.\n장착 중인 장비는 장비 관리에서 해제한 뒤 매각하세요.", 14, 88, 17);
        }

        void ShowSellItem(EquipmentData item, int page, string notice = null)
        {
            if (battle.Session != null) return;
            Clear(header); Clear(left); Clear(commands); Clear(footer);
            Label(header, "TALES / TACTICS     ·     장비 매각", 12, 45, 25);
            Label(left, item.DisplayName, 12, 65, 23);
            Label(left, EquipmentBonusText(item) + "\n\n보유 " + CampaignInventory.Owned(battle.Campaign, item.Id) +
                " / 장착 중 " + CampaignInventory.Equipped(battle.Campaign, item.Id) +
                "\n미장착 " + CampaignInventory.Available(battle.Campaign, item.Id), 90, 360, 19);
            int price = CampaignInventory.SellPrice(item);
            Label(commands, $"매각가 {price}G\n소지금 {battle.Campaign.Gold}G\n\n1개 매각 후\n소지금 {(long)battle.Campaign.Gold + price}G", 20, 210, 20);
            string reason = SaleBlocked(item);
            Label(commands, reason ?? "미장착 장비 1개를 매각합니다.", 250, 85, 18);
            Button(commands, "1개 매각 · 저장", 360, () =>
            {
                string blocked = SaleBlocked(item);
                if (blocked != null) { ShowSellItem(item, page, blocked); return; }
                bool saved = CampaignInventory.TrySell(battle.Campaign, item, battle.PersistCampaign);
                ShowSellItem(item, page, saved ? item.DisplayName + " 1개를 매각했습니다. +" + price + "G" : "저장 실패. 소지금과 장비 수량은 유지했습니다.");
            }, reason == null, 48);
            Button(commands, "보유 장비 목록으로", 425, () => ShowSellShop(page));
            Button(commands, "출전 준비로", 477, ShowDeployment);
            Label(footer, notice ?? "1개 매각 · 저장을 누르면 미장착 장비 1개를 팔고 골드를 받습니다.\n저장에 실패하면 소지금과 보유 수량을 되돌립니다.", 14, 88, 17);
        }

        string SaleBlocked(EquipmentData item)
        {
            if (battle.TrainingMode) return "훈련 모드에서는 매각할 수 없습니다.";
            if (!CampaignStorage.CanSave) return "저장 보호 상태에서는 매각할 수 없습니다.";
            return CampaignInventory.SaleUnavailable(battle.Campaign, item);
        }
    }
}
