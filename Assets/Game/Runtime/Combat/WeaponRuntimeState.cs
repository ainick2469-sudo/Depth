namespace FrontierDepths.Combat
{
    internal sealed class WeaponRuntimeState
    {
        private float nextFireTime;
        private float reloadStartTime;
        private float reloadCompleteTime;
        private bool autoReloadQueued;
        private float autoReloadReadyTime;

        public WeaponRuntimeState(int magazineSize)
            : this(magazineSize, 30, 60, -1)
        {
        }

        public WeaponRuntimeState(int magazineSize, int reserveAmmo, int maxReserveAmmo, int currentAmmo = -1)
        {
            MagazineSize = magazineSize < 1 ? 1 : magazineSize;
            MaxReserveAmmo = UnityEngine.Mathf.Max(0, maxReserveAmmo);
            ReserveAmmo = UnityEngine.Mathf.Clamp(reserveAmmo, 0, MaxReserveAmmo);
            CurrentAmmo = currentAmmo < 0 ? MagazineSize : UnityEngine.Mathf.Clamp(currentAmmo, 0, MagazineSize);
        }

        public int CurrentAmmo { get; private set; }
        public int MagazineSize { get; private set; }
        public int ReserveAmmo { get; private set; }
        public int MaxReserveAmmo { get; private set; }
        public bool IsReloading { get; private set; }
        public float ReloadStartTime => reloadStartTime;
        public float ReloadCompleteTime => reloadCompleteTime;
        public bool IsAutoReloadQueued => autoReloadQueued;
        public float AutoReloadReadyTime => autoReloadReadyTime;

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
            if (IsReloading || CurrentAmmo >= MagazineSize || ReserveAmmo <= 0)
            {
                return false;
            }

            ClearPendingAutoReload();
            IsReloading = true;
            reloadStartTime = currentTime;
            reloadCompleteTime = currentTime + reloadDuration;
            return true;
        }

        public bool TryQueueAutoReload(float currentTime, float delay)
        {
            if (IsReloading || CurrentAmmo >= MagazineSize || ReserveAmmo <= 0 || autoReloadQueued)
            {
                return false;
            }

            autoReloadQueued = true;
            autoReloadReadyTime = currentTime + UnityEngine.Mathf.Max(0f, delay);
            return true;
        }

        public bool TryStartQueuedAutoReload(float currentTime, float reloadDuration)
        {
            if (!autoReloadQueued || currentTime < autoReloadReadyTime)
            {
                return false;
            }

            ClearPendingAutoReload();
            return TryStartReload(currentTime, reloadDuration);
        }

        public void ClearPendingAutoReload()
        {
            autoReloadQueued = false;
            autoReloadReadyTime = 0f;
        }

        public int TryAddAmmoToMagazine(int amount, bool cancelReloadIfNeeded)
        {
            if (amount <= 0 || CurrentAmmo >= MagazineSize)
            {
                return 0;
            }

            int before = CurrentAmmo;
            CurrentAmmo = UnityEngine.Mathf.Min(MagazineSize, CurrentAmmo + amount);
            int added = CurrentAmmo - before;
            if (added > 0)
            {
                ClearPendingAutoReload();
                if (cancelReloadIfNeeded && IsReloading)
                {
                    IsReloading = false;
                    reloadStartTime = 0f;
                    reloadCompleteTime = 0f;
                }
            }

            return added;
        }

        public int TryAddAmmoToReserve(int amount, bool cancelReloadIfNeeded)
        {
            if (amount <= 0 || ReserveAmmo >= MaxReserveAmmo)
            {
                return 0;
            }

            int before = ReserveAmmo;
            ReserveAmmo = UnityEngine.Mathf.Min(MaxReserveAmmo, ReserveAmmo + amount);
            int added = ReserveAmmo - before;
            if (added > 0)
            {
                ClearPendingAutoReload();
                if (cancelReloadIfNeeded && IsReloading && CurrentAmmo >= MagazineSize)
                {
                    IsReloading = false;
                    reloadStartTime = 0f;
                    reloadCompleteTime = 0f;
                }
            }

            return added;
        }

        public bool Tick(float currentTime)
        {
            if (!IsReloading || currentTime < reloadCompleteTime)
            {
                return false;
            }

            IsReloading = false;
            int missingAmmo = UnityEngine.Mathf.Max(0, MagazineSize - CurrentAmmo);
            int loadedAmmo = UnityEngine.Mathf.Min(missingAmmo, ReserveAmmo);
            CurrentAmmo += loadedAmmo;
            ReserveAmmo -= loadedAmmo;
            reloadStartTime = 0f;
            reloadCompleteTime = 0f;
            ClearPendingAutoReload();
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
            CurrentAmmo = UnityEngine.Mathf.Min(CurrentAmmo <= 0 ? MagazineSize : CurrentAmmo, MagazineSize);
            IsReloading = false;
            nextFireTime = 0f;
            reloadStartTime = 0f;
            reloadCompleteTime = 0f;
            ClearPendingAutoReload();
        }

        public void ResetAmmo(int magazineSize, int currentAmmo, int reserveAmmo, int maxReserveAmmo)
        {
            MagazineSize = magazineSize < 1 ? 1 : magazineSize;
            MaxReserveAmmo = UnityEngine.Mathf.Max(0, maxReserveAmmo);
            CurrentAmmo = UnityEngine.Mathf.Clamp(currentAmmo, 0, MagazineSize);
            ReserveAmmo = UnityEngine.Mathf.Clamp(reserveAmmo, 0, MaxReserveAmmo);
            IsReloading = false;
            nextFireTime = 0f;
            reloadStartTime = 0f;
            reloadCompleteTime = 0f;
            ClearPendingAutoReload();
        }
    }
}
