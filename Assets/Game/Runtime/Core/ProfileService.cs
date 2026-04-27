using System.Collections.Generic;

namespace FrontierDepths.Core
{
    public sealed class ProfileService
    {
        private readonly SaveService saveService;

        public ProfileService(SaveService saveService)
        {
            this.saveService = saveService;
            using (LoadTimingLogger.Measure("Profile load"))
            {
                Current = saveService.LoadProfile() ?? new ProfileState();
                Current.Normalize();
                Save();
            }
        }

        public ProfileState Current { get; private set; }

        public void Save()
        {
            Current.Normalize();
            saveService.SaveProfile(Current);
        }

        public bool TrySpendGold(int amount)
        {
            if (amount < 0 || Current.gold < amount)
            {
                return false;
            }

            Current.gold -= amount;
            Save();
            return true;
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Current.gold += amount;
            Save();
        }

        public bool UnlockWeapon(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId) || Current.unlockedWeaponIds.Contains(weaponId))
            {
                return false;
            }

            Current.unlockedWeaponIds.Add(weaponId);
            if (string.IsNullOrWhiteSpace(Current.secondaryWeaponId) && weaponId != Current.primaryWeaponId)
            {
                Current.secondaryWeaponId = weaponId;
            }

            Save();
            return true;
        }

        public void EquipWeapon(string weaponId)
        {
            EquipWeapon(weaponId, 0);
        }

        public void EquipWeapon(string weaponId, int preferredSlot)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return;
            }

            if (!Current.unlockedWeaponIds.Contains(weaponId))
            {
                Current.unlockedWeaponIds.Add(weaponId);
            }

            int slot = preferredSlot == 2 ? 2 : preferredSlot == 1 ? 1 : Current.activeWeaponSlot;
            if (slot == 2 && string.IsNullOrWhiteSpace(Current.secondaryWeaponId))
            {
                Current.secondaryWeaponId = weaponId;
            }
            else if (slot == 1)
            {
                Current.primaryWeaponId = weaponId;
            }

            if (!Current.TryAssignWeaponToSlot(weaponId, slot))
            {
                Current.equippedWeaponId = weaponId;
            }

            Save();
        }

        public bool AcceptBounty(string bountyId)
        {
            if (!BountyObjectiveTracker.MarkAccepted(Current, bountyId, out _))
            {
                return false;
            }

            if (!Current.activeBountyIds.Contains(bountyId))
            {
                Current.activeBountyIds.Add(bountyId);
            }

            Save();
            return true;
        }

        public bool TryTurnInBounty(string bountyId, out string message)
        {
            if (!BountyObjectiveTracker.TryTurnIn(Current, bountyId, out BountyDefinition definition, out string reason))
            {
                message = reason;
                return false;
            }

            if (definition.goldReward > 0)
            {
                Current.gold += definition.goldReward;
            }

            if (definition.xpReward > 0)
            {
                Current.classXp += definition.xpReward;
                while (Current.classXp >= (Current.skillPoints + 1) * 100)
                {
                    Current.skillPoints++;
                }
            }

            int reputation = ReputationService.AddReputation(Current, ReputationService.GetBountyReputationReward(definition));
            Current.activeBountyIds.Remove(bountyId);
            Save();
            message = $"{definition.targetName} bounty complete: +{definition.goldReward}g, +{definition.xpReward} XP, +{reputation} reputation.";
            return true;
        }

        public void SetHeirloom(string heirloomId)
        {
            Current.storedHeirloomId = heirloomId ?? string.Empty;
            Save();
        }

        public void AddTownSigil(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Current.townSigils += amount;
            Save();
        }

        public bool ConsumeTownSigil()
        {
            if (Current.townSigils <= 0)
            {
                return false;
            }

            Current.townSigils--;
            Save();
            return true;
        }

        public int GetPurchaseCount(string shopId, string offerId)
        {
            return Current.GetPurchaseCount(shopId, offerId);
        }

        public void RecordPurchase(string shopId, string offerId)
        {
            Current.RecordPurchase(shopId, offerId);
            Save();
        }

        public IReadOnlyList<string> GetActiveBounties()
        {
            return Current.activeBountyIds;
        }

        public void ResetProgress()
        {
            Current = new ProfileState();
            Current.Normalize();
            Save();
        }
    }
}
