namespace FrontierDepths.Combat
{
    internal sealed class WeaponRuntimeState
    {
        private float nextFireTime;
        private float reloadCompleteTime;

        public WeaponRuntimeState(int magazineSize)
        {
            MagazineSize = magazineSize < 1 ? 1 : magazineSize;
            CurrentAmmo = MagazineSize;
        }

        public int CurrentAmmo { get; private set; }
        public int MagazineSize { get; private set; }
        public bool IsReloading { get; private set; }

        public bool CanFire(float currentTime)
        {
            return !IsReloading && CurrentAmmo > 0 && currentTime >= nextFireTime;
        }

        public bool TryFire(float currentTime, float fireCooldown)
        {
            if (!CanFire(currentTime))
            {
                return false;
            }

            CurrentAmmo--;
            nextFireTime = currentTime + fireCooldown;
            return true;
        }

        public bool TryStartReload(float currentTime, float reloadDuration)
        {
            if (IsReloading || CurrentAmmo >= MagazineSize)
            {
                return false;
            }

            IsReloading = true;
            reloadCompleteTime = currentTime + reloadDuration;
            return true;
        }

        public bool Tick(float currentTime)
        {
            if (!IsReloading || currentTime < reloadCompleteTime)
            {
                return false;
            }

            IsReloading = false;
            CurrentAmmo = MagazineSize;
            return true;
        }

        public void ResetMagazine(int magazineSize)
        {
            MagazineSize = magazineSize < 1 ? 1 : magazineSize;
            CurrentAmmo = MagazineSize;
            IsReloading = false;
            nextFireTime = 0f;
            reloadCompleteTime = 0f;
        }
    }
}
