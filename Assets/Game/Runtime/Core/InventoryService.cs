using System.Collections.Generic;

namespace FrontierDepths.Core
{
    public sealed class InventoryService
    {
        private readonly ProfileService profileService;

        public InventoryService(ProfileService profileService)
        {
            this.profileService = profileService;
            this.profileService?.Current?.Normalize();
        }

        public ProfileState Profile => profileService.Current;

        public IReadOnlyList<string> OwnedWeaponIds => profileService.Current.unlockedWeaponIds;

        public bool HasWeapon(string weaponId)
        {
            return profileService.Current.HasUnlockedWeapon(weaponId);
        }

        public bool AddWeapon(string weaponId)
        {
            return profileService.UnlockWeapon(weaponId);
        }

        public bool EquipWeapon(string weaponId, int preferredSlot = 0)
        {
            if (!HasWeapon(weaponId))
            {
                return false;
            }

            profileService.EquipWeapon(weaponId, preferredSlot);
            return true;
        }

        public string GetEquippedWeaponId()
        {
            profileService.Current.Normalize();
            return profileService.Current.GetActiveWeaponId();
        }

        public string GetWeaponInSlot(int slot)
        {
            profileService.Current.Normalize();
            return slot == 2
                ? profileService.Current.secondaryWeaponId
                : profileService.Current.primaryWeaponId;
        }
    }
}
