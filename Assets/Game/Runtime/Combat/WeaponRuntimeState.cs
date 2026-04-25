namespace FrontierDepths.Combat
{
    internal sealed class WeaponRuntimeState
    {
        private float nextFireTime;
        private float reloadStartTime;
        private float reloadCompleteTime;

        public WeaponRuntimeState(int magazineSize)
        {
            MagazineSize = magazineSize < 1 ? 1 : magazineSize;
            CurrentAmmo = MagazineSize;
        }

        public int CurrentAmmo { get; private set; }
        public int MagazineSize { get; private set; }
        public bool IsReloading { get; private set; }
        public float ReloadStartTime => reloadStartTime;
        public float ReloadCompleteTime => reloadCompleteTime;

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
            reloadStartTime = currentTime;
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
            reloadStartTime = 0f;
            reloadCompleteTime = 0f;
            return true;
        }

        public float GetReloadProgress(float currentTime)
        {
            if (!IsReloading)
            {
                return CurrentAmmo >= MagazineSize ? 1f : 0f;
            }

            float duration = reloadCompleteTime - reloadStartTime;
            if (duration <= 0.001f)
            {
                return 1f;
            }

            return UnityEngine.Mathf.Clamp01((currentTime - reloadStartTime) / duration);
        }

        public void ResetMagazine(int magazineSize)
        {
            MagazineSize = magazineSize < 1 ? 1 : magazineSize;
            CurrentAmmo = MagazineSize;
            IsReloading = false;
            nextFireTime = 0f;
            reloadStartTime = 0f;
            reloadCompleteTime = 0f;
        }
    }
}
