using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TalesTactics
{
    [Serializable] public sealed class OwnedEquipment
    {
        public string Id;
        public int Count;
    }

    public static class CampaignInventory
    {
        public const int StartingGold = 300, MaxGold = 9999999, MaxQuantity = 99;
        public const string StarterSword = "bronze-sword";
        public static List<OwnedEquipment> StartingItems() =>
            new List<OwnedEquipment> { new OwnedEquipment { Id = StarterSword, Count = 1 } };

        public static int Owned(CampaignSave save, string id) =>
            save.Inventory?.FirstOrDefault(x => x.Id == id)?.Count ?? 0;

        public static int Equipped(CampaignSave save, string id, string exceptCharacter = null) =>
            save.Characters.Where(c => c.Id != exceptCharacter)
                .Sum(c => c.Equipment?.Count(e => e == id) ?? 0);

        public static int Available(CampaignSave save, string id, string exceptCharacter = null) =>
            Math.Max(0, Owned(save, id) - Equipped(save, id, exceptCharacter));

        public static void MigrateVersion1(CampaignSave save)
        {
            save.Gold = StartingGold;
            save.Inventory = StartingItems();
            foreach (var group in save.Characters.SelectMany(c => c.Equipment).Where(id => !string.IsNullOrWhiteSpace(id)).GroupBy(id => id))
            {
                var entry = save.Inventory.Find(e => e.Id == group.Key);
                if (entry == null) save.Inventory.Add(new OwnedEquipment { Id = group.Key, Count = group.Count() });
                else entry.Count = Math.Max(entry.Count, group.Count());
            }
            save.Version = 2;
        }

        public static void Validate(CampaignSave save)
        {
            if (save.Gold < 0 || save.Gold > MaxGold || save.Inventory == null)
                throw new InvalidDataException("Invalid inventory or gold");
            var ids = new HashSet<string>();
            foreach (var entry in save.Inventory)
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id) || !ids.Add(entry.Id) ||
                    entry.Count < 1 || entry.Count > MaxQuantity)
                    throw new InvalidDataException("Invalid inventory entry");
            foreach (var id in save.Characters.SelectMany(c => c.Equipment).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct())
                if (Equipped(save, id) > Owned(save, id))
                    throw new InvalidDataException("Equipped quantity exceeds ownership: " + id);
        }

        public static string PurchaseUnlockRequirement(CampaignSave save, EquipmentData item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.RequiredStoryFlag) ||
                save.StoryProgress.Contains(item.RequiredStoryFlag)) return null;
            string requirement = item.RequiredStoryFlag;
            for (int stage = 0; stage < CampaignStages.Count; stage++)
                if (requirement == CampaignStages.Id(stage)) requirement = (stage + 1) + "장 완료";
            return "해금 조건: " + requirement;
        }

        public static string PurchaseUnavailable(CampaignSave save, EquipmentData item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id) || item.BuyPrice <= 0) return "판매하지 않는 장비입니다.";
            string locked = PurchaseUnlockRequirement(save, item);
            if (locked != null) return locked;
            if (Owned(save, item.Id) >= MaxQuantity) return "최대 보유 수량입니다.";
            if (save.Gold < item.BuyPrice) return "소지금이 부족합니다.";
            return null;
        }

        public static int SellPrice(EquipmentData item) => item == null ? 0 : Math.Max(0, item.BuyPrice / 2);

        public static string SaleUnavailable(CampaignSave save, EquipmentData item)
        {
            int price = SellPrice(item);
            if (item == null || string.IsNullOrWhiteSpace(item.Id) || price <= 0) return "매각할 수 없는 장비입니다.";
            if (Available(save, item.Id) <= 0) return "매각할 미장착 장비가 없습니다.";
            if (save.Gold > MaxGold - price) return "소지금 상한을 초과합니다.";
            return null;
        }

        public static bool TrySell(CampaignSave save, EquipmentData item, Func<CampaignSave, bool> persist)
        {
            if (SaleUnavailable(save, item) != null || persist == null) return false;
            var entry = save.Inventory.Find(e => e.Id == item.Id);
            int index = save.Inventory.IndexOf(entry), previousGold = save.Gold, previousCount = entry.Count;
            bool saved = false;
            save.Gold += SellPrice(item);
            if (--entry.Count == 0) save.Inventory.RemoveAt(index);
            try { return saved = persist(save); }
            finally
            {
                if (!saved)
                {
                    save.Gold = previousGold;
                    entry.Count = previousCount;
                    if (!save.Inventory.Contains(entry)) save.Inventory.Insert(index, entry);
                }
            }
        }
        public static bool TryBuy(CampaignSave save, EquipmentData item, Func<CampaignSave, bool> persist)
        {
            if (PurchaseUnavailable(save, item) != null || persist == null) return false;
            var entry = save.Inventory.Find(e => e.Id == item.Id);
            bool added = entry == null, saved = false;
            if (added) { entry = new OwnedEquipment { Id = item.Id }; save.Inventory.Add(entry); }
            int previousGold = save.Gold, previousCount = entry.Count;
            save.Gold -= item.BuyPrice; entry.Count++;
            try { return saved = persist(save); }
            finally
            {
                if (!saved)
                {
                    save.Gold = previousGold;
                    if (added) save.Inventory.Remove(entry); else entry.Count = previousCount;
                }
            }
        }
    }
}
