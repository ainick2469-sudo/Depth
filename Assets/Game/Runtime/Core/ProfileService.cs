using System.Collections.Generic;

namespace FrontierDepths.Core
{
    public sealed class ProfileService
    {
        private readonly SaveService saveService;

        public ProfileService(SaveService saveService)
        {
            this.saveService = saveService;
            Current = saveService.LoadProfile() ?? new ProfileState();
            Current.Normalize();
            Save();
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
            Save();
            return true;
        }

        public void EquipWeapon(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return;
            }

            if (!Current.unlockedWeaponIds.Contains(weaponId))
            {
                Current.unlockedWeaponIds.Add(weaponId);
            }

            Current.equippedWeaponId = weaponId;
            Save();
        }

        public bool AcceptBounty(string bountyId)
        {
            if (string.IsNullOrWhiteSpace(bountyId) || Current.activeBountyIds.Contains(bountyId))
            {
                return false;
            }

            Current.activeBountyIds.Add(bountyId);
            Save();
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
