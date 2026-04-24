using System;
using System.Collections.Generic;

namespace FrontierDepths.Core
{
    [Serializable]
    public class ProfileState
    {
        public int version = 1;
        public int gold = 350;
        public int townSigils;
        public int curioDust;
        public string equippedWeaponId = "weapon.frontier_revolver";
        public string storedHeirloomId = string.Empty;
        public List<string> unlockedWeaponIds = new List<string> { "weapon.frontier_revolver" };
        public List<string> activeBountyIds = new List<string>();
        public List<ShopPurchaseRecord> purchaseRecords = new List<ShopPurchaseRecord>();

        public void Normalize()
        {
            equippedWeaponId = string.IsNullOrWhiteSpace(equippedWeaponId) ? "weapon.frontier_revolver" : equippedWeaponId;
            unlockedWeaponIds ??= new List<string>();
            activeBountyIds ??= new List<string>();
            purchaseRecords ??= new List<ShopPurchaseRecord>();

            if (!unlockedWeaponIds.Contains("weapon.frontier_revolver"))
            {
                unlockedWeaponIds.Insert(0, "weapon.frontier_revolver");
            }
        }

        public int GetPurchaseCount(string shopId, string offerId)
        {
            for (int i = 0; i < purchaseRecords.Count; i++)
            {
                ShopPurchaseRecord record = purchaseRecords[i];
                if (record.shopId == shopId && record.offerId == offerId)
                {
                    return record.purchaseCount;
                }
            }

            return 0;
        }

        public void RecordPurchase(string shopId, string offerId)
        {
            for (int i = 0; i < purchaseRecords.Count; i++)
            {
                ShopPurchaseRecord record = purchaseRecords[i];
                if (record.shopId == shopId && record.offerId == offerId)
                {
                    record.purchaseCount++;
                    purchaseRecords[i] = record;
                    return;
                }
            }

            purchaseRecords.Add(new ShopPurchaseRecord
            {
                shopId = shopId,
                offerId = offerId,
                purchaseCount = 1
            });
        }
    }

    [Serializable]
    public struct ShopPurchaseRecord
    {
        public string shopId;
        public string offerId;
        public int purchaseCount;
    }
}
