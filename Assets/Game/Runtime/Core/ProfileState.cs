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
        public int classXp;
        public int skillPoints;
        public int townReputation;
        public string equippedWeaponId = "weapon.frontier_revolver";
        public string storedHeirloomId = string.Empty;
        public List<string> unlockedWeaponIds = new List<string> { "weapon.frontier_revolver" };
        public List<string> activeBountyIds = new List<string>();
        public List<BountyRuntimeState> bounties = new List<BountyRuntimeState>();
        public List<string> unlockedSkillNodeIds = new List<string>();
        public List<ShopPurchaseRecord> purchaseRecords = new List<ShopPurchaseRecord>();

        public void Normalize()
        {
            equippedWeaponId = string.IsNullOrWhiteSpace(equippedWeaponId) ? "weapon.frontier_revolver" : equippedWeaponId;
            unlockedWeaponIds ??= new List<string>();
            activeBountyIds ??= new List<string>();
            bounties ??= new List<BountyRuntimeState>();
            unlockedSkillNodeIds ??= new List<string>();
            purchaseRecords ??= new List<ShopPurchaseRecord>();
            classXp = Math.Max(0, classXp);
            skillPoints = Math.Max(0, skillPoints);
            townReputation = Math.Max(0, townReputation);

            if (!unlockedWeaponIds.Contains("weapon.frontier_revolver"))
            {
                unlockedWeaponIds.Insert(0, "weapon.frontier_revolver");
            }

            for (int i = bounties.Count - 1; i >= 0; i--)
            {
                if (bounties[i] == null || string.IsNullOrWhiteSpace(bounties[i].bountyId))
                {
                    bounties.RemoveAt(i);
                    continue;
                }

                bounties[i].Normalize();
                if ((bounties[i].state == BountyState.Accepted || bounties[i].state == BountyState.Spawned) &&
                    !activeBountyIds.Contains(bounties[i].bountyId))
                {
                    activeBountyIds.Add(bounties[i].bountyId);
                }
            }

            for (int i = 0; i < activeBountyIds.Count; i++)
            {
                string bountyId = activeBountyIds[i];
                if (string.IsNullOrWhiteSpace(bountyId))
                {
                    continue;
                }

                BountyRuntimeState state = BountyObjectiveTracker.GetOrCreate(this, bountyId);
                if (state.state == BountyState.Available)
                {
                    state.state = BountyState.Accepted;
                }
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
