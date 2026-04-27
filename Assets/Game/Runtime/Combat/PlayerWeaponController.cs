using System;
using FrontierDepths.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrontierDepths.Combat
{
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        private const string DefaultWeaponId = "weapon.frontier_revolver";
        private const string DefaultWeaponName = "Frontier Revolver";
        private const float DefaultDamage = 15f;
        private const float DefaultFireRate = 2.857f;
        private const int DefaultMagazineSize = 6;
        private const int DefaultStartingReserveAmmo = 36;
        private const int DefaultMaxReserveAmmo = 72;
        private const float DefaultReloadDuration = 1.4f;
        private const float DefaultRange = 45f;
        private const float DefaultFullDamageRange = 17f;
        private const float DefaultDamageMultiplierAtMaxRange = 0.50f;
        private const float DefaultRevolverHearingRadius = 55f;
        private const int ImpactPoolSize = 12;
        private const int DamageNumberPoolSize = 12;
        private const int TracerPoolSize = 8;
        private const float ShotDebugDrawDuration = 0.5f;
        private const string RuntimeFeedbackRootName = "RuntimeFeedbackRoot";

        [SerializeField] private Camera weaponCamera;
        [SerializeField] private WeaponDefinition weaponDefinition;
        [SerializeField] private LayerMask weaponRaycastMask = DefaultWeaponRaycastMask;
        [SerializeField] private float muzzleFlashDuration = 0.045f;
        [SerializeField] private float impactMarkerDuration = 1.25f;
        [SerializeField] private float damageNumberDuration = 0.75f;
        [SerializeField] private float tracerDuration = 0.05f;
        [SerializeField] private float weaponVolume = 0.28f;
        [SerializeField] private float recoilPitchKick = 1.2f;
        [SerializeField] private float recoilYawKick = 0.35f;
        [SerializeField] private float recoilRecoverySeconds = 0.15f;
        [SerializeField] private bool autoReloadOnEmpty = true;
        [SerializeField] private float autoReloadDelay = 0.12f;
        [SerializeField] private float weaponHearingRadius = DefaultRevolverHearingRadius;
        [SerializeField] private bool enableShotDebugLogging = true;
        [SerializeField] private bool enableShotDebugDraw = true;

        private readonly ImpactMarker[] impactMarkers = new ImpactMarker[ImpactPoolSize];
        private readonly DamageNumberMarker[] damageNumbers = new DamageNumberMarker[DamageNumberPoolSize];
        private readonly TracerMarker[] tracers = new TracerMarker[TracerPoolSize];

        private FirstPersonController playerController;
        private PlayerHealth playerHealth;
        private WeaponRuntimeState weaponState;
        private AudioSource weaponAudioSource;
        private AudioClip gunshotClip;
        private AudioClip reloadClip;
        private AudioClip reloadFinishClip;
        private AudioClip dryFireClip;
        private GameObject muzzleFlash;
        private float muzzleFlashHideTime;
        private int nextImpactMarker;
        private int nextDamageNumber;
        private int nextTracer;
        private bool runtimeReady;
        private bool firstShotAfterReloadReady;
        private float pendingShotDamageMultiplier = 1f;
        private int runUpgradeWeaponHitCounter;
        private bool applyingChainHit;

        public static int DefaultWeaponRaycastMask => ~(1 << 2);
        public static Func<float> CritRollProviderForTests { get; set; }

        public event Action<PlayerWeaponController> WeaponFired;
        public event Action<PlayerWeaponController> ReloadStarted;
        public event Action<PlayerWeaponController> ReloadFinished;
        public event Action<PlayerWeaponController> DryFired;
        public event Action<DamageResult> DamageHitConfirmed;
        public event Action<WeaponHitFeedback> HitFeedbackReceived;

        public string WeaponId => GetWeaponId();
        public string WeaponName => GetWeaponName();
        public float BaseDamage => GetBaseWeaponDamage();
        public float EffectiveDamage => GetBaseDamage();
        public float MaxRange => GetRange();
        public float FullDamageRange => GetFullDamageRange();
        public float DamageMultiplierAtMaxRange => GetDamageMultiplierAtMaxRange();
        public float EffectiveReloadDuration => GetReloadDuration();
        public float BaseReloadDuration => GetBaseReloadDuration();
        public float CritChance => GetCritChance();
        public int CurrentAmmo => weaponState != null ? weaponState.CurrentAmmo : GetMagazineSize();
        public int MagazineSize => weaponState != null ? weaponState.MagazineSize : GetMagazineSize();
        public int ReserveAmmo => weaponState != null ? weaponState.ReserveAmmo : GetStartingReserveAmmo();
        public int MaxReserveAmmo => weaponState != null ? weaponState.MaxReserveAmmo : GetMaxReserveAmmo();
        public bool IsReloading => weaponState != null && weaponState.IsReloading;
        public float ReloadProgress => weaponState != null ? weaponState.GetReloadProgress(Time.time) : 1f;
        public float ReloadDuration => GetReloadDuration();
        internal bool IsFirstShotAfterReloadReadyForTests => firstShotAfterReloadReady;

        private void Awake()
        {
            EnsureRuntimeReady();
            SubscribeGameplayEvents();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SubscribeGameplayEvents();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            GameplayEventBus.Unsubscribe(HandleGameplayEvent);
            HideAllFeedback();
        }

        private void SubscribeGameplayEvents()
        {
            GameplayEventBus.Unsubscribe(HandleGameplayEvent);
            GameplayEventBus.Subscribe(HandleGameplayEvent);
        }

        private void Update()
        {
            float currentTime = Time.time;
            TickReloadCompletion(currentTime);

            UpdateMuzzleFlash(currentTime);
            UpdateImpactMarkers(currentTime);

            if (!IsInDungeonRuntime() || playerController == null || playerController.IsUiCaptured)
            {
                HideAllTracers();
                return;
            }

            HandleWeaponInputFrame(
                currentTime,
                InputBindingService.GetKey(GameplayInputAction.Fire),
                InputBindingService.GetKeyDown(GameplayInputAction.Fire),
                InputBindingService.GetKeyDown(GameplayInputAction.Reload));
        }

        internal WeaponInputFrameResult HandleWeaponInputFrame(
            float currentTime,
            bool wantsToFire,
            bool firePressedThisFrame,
            bool reloadPressedThisFrame)
        {
            EnsureRuntimeReady();

            WeaponInputFrameResult result = new WeaponInputFrameResult();
            if (IsGameplayInputBlocked())
            {
                result.inputBlocked = true;
                return result;
            }

            if (weaponState == null)
            {
                weaponState = CreateWeaponRuntimeState();
            }

            if (weaponState.TryStartQueuedAutoReload(currentTime, GetReloadDuration()))
            {
                BeginReloadFeedback();
                result.reloadStarted = true;
                result.autoReloadStarted = true;
            }

            if (reloadPressedThisFrame)
            {
                result.reloadRequested = true;
                result.reloadStarted |= TryStartReload(currentTime);
                return result;
            }

            if (!ShouldAttemptFireInput(wantsToFire, firePressedThisFrame))
            {
                return result;
            }

            result.fireAttempted = true;
            result.fired = TryFire(currentTime);
            return result;
        }

        internal bool ShouldAttemptFireInput(bool wantsToFire, bool firePressedThisFrame)
        {
            EnsureRuntimeReady();
            if (!wantsToFire)
            {
                return false;
            }

            if (weaponState == null)
            {
                return true;
            }

            if (weaponState.IsReloading)
            {
                return false;
            }

            if (weaponState.CurrentAmmo > 0)
            {
                return true;
            }

            if (weaponState.IsAutoReloadQueued)
            {
                return false;
            }

            return firePressedThisFrame;
        }

        internal bool TickReloadCompletion(float currentTime)
        {
            EnsureRuntimeReady();
            if (weaponState == null || !weaponState.Tick(currentTime))
            {
                return false;
            }

            PlayReloadFeedback(false);
            firstShotAfterReloadReady = RunStatAggregator.Current.HasFirstShotAfterReloadBonus;
            ReloadFinished?.Invoke(this);
            PublishWeaponEvent(GameplayEventType.ReloadFinished);
            SyncAmmoToRun(true);
            return true;
        }

        public bool TryFire(float currentTime)
        {
            EnsureRuntimeReady();
            if (IsGameplayInputBlocked())
            {
                return false;
            }

            if (weaponState == null)
            {
                weaponState = CreateWeaponRuntimeState();
            }

            if (!weaponState.TryFire(currentTime, GetFireCooldown()))
            {
                if (!weaponState.IsReloading && weaponState.CurrentAmmo <= 0)
                {
                    bool alreadyQueued = weaponState.IsAutoReloadQueued;
                    if (!alreadyQueued)
                    {
                        PlayDryFireFeedback();
                        DryFired?.Invoke(this);
                        PublishWeaponEvent(GameplayEventType.DryFire);
                        QueueAutoReload(currentTime, 0f);
                    }

                    if (alreadyQueued)
                    {
                        weaponState.ClearPendingAutoReload();
                        if (weaponState.TryStartReload(currentTime, GetReloadDuration()))
                        {
                            BeginReloadFeedback();
                        }
                    }
                    else if (weaponState.TryStartQueuedAutoReload(currentTime, GetReloadDuration()))
                    {
                        BeginReloadFeedback();
                    }
                }

                return false;
            }

            SyncAmmoToRun(false);
            pendingShotDamageMultiplier = firstShotAfterReloadReady
                ? RunStatAggregator.Current.FirstShotAfterReloadMultiplier
                : 1f;
            firstShotAfterReloadReady = false;
            WeaponShotResolution resolution = ResolveShot();
            ApplyShotResolution(resolution);
            pendingShotDamageMultiplier = 1f;
            WeaponFired?.Invoke(this);
            PublishWeaponEvent(GameplayEventType.WeaponFired, amount: GetBaseDamage());
            PlayFireFeedback(currentTime);
            if (weaponState.CurrentAmmo <= 0)
            {
                QueueAutoReload(currentTime, autoReloadDelay);
            }

            return true;
        }

        public bool TryStartReload(float currentTime)
        {
            EnsureRuntimeReady();
            if (IsGameplayInputBlocked())
            {
                return false;
            }

            if (weaponState == null)
            {
                weaponState = CreateWeaponRuntimeState();
            }

            if (!weaponState.TryStartReload(currentTime, GetReloadDuration()))
            {
                return false;
            }

            BeginReloadFeedback();
            return true;
        }

        public int TryAddAmmoToMagazine(int amount, bool cancelReloadIfNeeded = true)
        {
            EnsureRuntimeReady();
            if (weaponState == null)
            {
                weaponState = CreateWeaponRuntimeState();
            }

            bool wasReloading = weaponState.IsReloading;
            int added = weaponState.TryAddAmmoToMagazine(amount, cancelReloadIfNeeded);
            if (added > 0 && wasReloading && cancelReloadIfNeeded)
            {
                firstShotAfterReloadReady = RunStatAggregator.Current.HasFirstShotAfterReloadBonus;
                ReloadFinished?.Invoke(this);
                PublishWeaponEvent(GameplayEventType.ReloadFinished);
            }

            if (added > 0)
            {
                SyncAmmoToRun(true);
            }

            return added;
        }

        public int TryAddAmmoToReserve(int amount, bool cancelReloadIfNeeded = true)
        {
            EnsureRuntimeReady();
            if (weaponState == null)
            {
                weaponState = CreateWeaponRuntimeState();
            }

            int added = weaponState.TryAddAmmoToReserve(amount, cancelReloadIfNeeded);
            if (added > 0)
            {
                SyncAmmoToRun(true);
            }

            return added;
        }

        public static bool IsWeaponHitAllowed(Collider hitCollider, Transform playerRoot)
        {
            if (hitCollider == null)
            {
                return false;
            }

            if (hitCollider.isTrigger || hitCollider.gameObject.layer == 2)
            {
                return false;
            }

            return playerRoot == null || !hitCollider.transform.IsChildOf(playerRoot);
        }

        internal static WeaponHitFeedbackKind ClassifyHitFeedback(float requestedDamage, DamageResult result)
        {
            if (result.killedTarget)
            {
                return WeaponHitFeedbackKind.Kill;
            }

            return result.damageApplied < requestedDamage - 0.01f
                ? WeaponHitFeedbackKind.Reduced
                : WeaponHitFeedbackKind.Normal;
        }

        internal static bool TryResolveShotHit(RaycastHit[] hits, Transform playerRoot, out WeaponShotHit hitInfo, out int ignoredHitCount)
        {
            hitInfo = default;
            ignoredHitCount = 0;

            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (!IsWeaponHitAllowed(hit.collider, playerRoot))
                {
                    ignoredHitCount++;
                    continue;
                }

                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                hitInfo = new WeaponShotHit
                {
                    kind = damageable != null ? WeaponShotHitKind.Damageable : WeaponShotHitKind.Environment,
                    hit = hit,
                    damageable = damageable
                };
                return true;
            }

            return false;
        }

        internal WeaponShotResolution ResolveShot()
        {
            ResolveWeaponCamera();
            if (weaponCamera == null)
            {
                LogShotDebug("camera=null; no raycast fired.");
                return default;
            }

            Ray ray = weaponCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float range = GetRange();
            RaycastHit[] hits = Physics.RaycastAll(ray, range, weaponRaycastMask, QueryTriggerInteraction.Ignore);
            Vector3 muzzleStart = GetMuzzleWorldPosition(ray.origin);
            WeaponShotResolution resolution = ResolveShot(ray, range, hits, transform, muzzleStart, weaponRaycastMask);
            LogShotDebug(
                $"camera={weaponCamera.name}; aimOrigin={ray.origin}; aimDirection={ray.direction}; muzzle={muzzleStart}; range={range:0.##}; mask={weaponRaycastMask.value}; hits={resolution.raycastHitCount}; ignored={resolution.ignoredHitCount}; target={resolution.cameraTargetPoint}; tracerStart={resolution.muzzleStart}; tracerEnd={resolution.finalTracerEnd}; kind={resolution.kind}; hit={(resolution.hitCollider != null ? resolution.hitCollider.name : "none")}; muzzleObstructed={resolution.muzzleObstructed}");
            return resolution;
        }

        internal static WeaponShotResolution ResolveShot(Ray aimRay, float range, RaycastHit[] hits, Transform playerRoot, Vector3 muzzleStart, int raycastMask)
        {
            WeaponShotResolution resolution = new WeaponShotResolution
            {
                aimRay = aimRay,
                cameraTargetPoint = aimRay.origin + aimRay.direction * range,
                muzzleStart = muzzleStart,
                finalTracerEnd = aimRay.origin + aimRay.direction * range,
                hitPoint = aimRay.origin + aimRay.direction * range,
                hitNormal = -aimRay.direction,
                maxRange = range,
                hitDistance = range,
                raycastHitCount = hits != null ? hits.Length : 0
            };

            if (TryResolveShotHit(hits, playerRoot, out WeaponShotHit shotHit, out int ignoredHitCount))
            {
                RaycastHit hit = shotHit.hit;
                resolution.kind = shotHit.kind;
                resolution.hitCollider = hit.collider;
                resolution.damageable = shotHit.damageable;
                resolution.cameraTargetPoint = hit.point;
                resolution.finalTracerEnd = hit.point;
                resolution.hitPoint = hit.point;
                resolution.hitNormal = hit.normal;
                resolution.hitDistance = hit.distance;
            }

            resolution.ignoredHitCount = ignoredHitCount;
            if (TryResolveMuzzleObstruction(muzzleStart, resolution.cameraTargetPoint, resolution.hitCollider, resolution.damageable, raycastMask, out RaycastHit obstruction))
            {
                resolution.kind = WeaponShotHitKind.Environment;
                resolution.hitCollider = obstruction.collider;
                resolution.damageable = null;
                resolution.muzzleObstructed = true;
                resolution.muzzleObstructionCollider = obstruction.collider;
                resolution.finalTracerEnd = obstruction.point;
                resolution.hitPoint = obstruction.point;
                resolution.hitNormal = obstruction.normal;
                resolution.hitDistance = obstruction.distance;
            }

            return resolution;
        }

        private void ApplyShotResolution(WeaponShotResolution resolution)
        {
            if (resolution.aimRay.direction == Vector3.zero)
            {
                return;
            }

            if (resolution.kind == WeaponShotHitKind.None)
            {
                DrawShotRay(resolution.aimRay.origin, resolution.cameraTargetPoint, Color.red);
                DrawShotRay(resolution.muzzleStart, resolution.finalTracerEnd, Color.red);
                ShowTracer(resolution.muzzleStart, resolution.finalTracerEnd, Color.red);
                return;
            }

            if (resolution.damageable != null)
            {
                float rangeMultiplier = CalculateRangeDamageMultiplier(
                    resolution.hitDistance,
                    GetFullDamageRange(),
                    GetRange(),
                    GetDamageMultiplierAtMaxRange());
                DamageInfo damageInfo = CreateDamageInfo(resolution.hitPoint, resolution.hitNormal, rangeMultiplier);
                DamageResult result = resolution.damageable.ApplyDamage(damageInfo);
                LogShotDebug($"damageable={resolution.hitCollider.name}; applied={result.applied}; damage={result.damageApplied:0.##}; recoilAppliedAfterResolution=true");

                if (result.applied)
                {
                    WeaponHitFeedbackKind kind = ClassifyHitFeedback(damageInfo.amount, result);
                    Color impactColor = kind == WeaponHitFeedbackKind.Reduced
                        ? new Color(1f, 0.56f, 0.18f, 1f)
                        : (kind == WeaponHitFeedbackKind.Kill ? new Color(1f, 0.22f, 0.18f, 1f) : new Color(1f, 0.9f, 0.35f, 1f));
                    WeaponHitFeedback feedback = new WeaponHitFeedback
                    {
                        kind = kind,
                        damageResult = result,
                        targetObject = resolution.hitCollider.gameObject,
                        hitPoint = resolution.hitPoint,
                        hitNormal = resolution.hitNormal,
                        requestedDamage = damageInfo.amount,
                        finalDamage = result.damageApplied,
                        isCritical = damageInfo.isCritical,
                        label = damageInfo.isCritical ? "CRIT" : string.Empty
                    };
                    DamageHitConfirmed?.Invoke(result);
                    HitFeedbackReceived?.Invoke(feedback);
                    ShowImpactMarker(resolution.hitPoint, resolution.hitNormal, impactColor);
                    ShowDamageNumber(resolution.hitPoint, result.damageApplied, impactColor, result.killedTarget, damageInfo.isCritical ? "CRIT" : string.Empty);
                    ShowTracer(resolution.muzzleStart, resolution.finalTracerEnd, impactColor);
                    DrawShotRay(resolution.aimRay.origin, resolution.cameraTargetPoint, Color.green);
                    DrawShotRay(resolution.muzzleStart, resolution.finalTracerEnd, impactColor);
                    PublishWeaponHitEvents(damageInfo, result, resolution.hitCollider.gameObject);
                }

                return;
            }

            WeaponHitFeedback environmentFeedback = new WeaponHitFeedback
            {
                kind = WeaponHitFeedbackKind.Environment,
                targetObject = resolution.hitCollider != null ? resolution.hitCollider.gameObject : null,
                hitPoint = resolution.hitPoint,
                hitNormal = resolution.hitNormal
            };
            HitFeedbackReceived?.Invoke(environmentFeedback);
            Color environmentColor = new Color(0.85f, 0.88f, 0.78f, 1f);
            ShowImpactMarker(resolution.hitPoint, resolution.hitNormal, environmentColor);
            ShowTracer(resolution.muzzleStart, resolution.finalTracerEnd, environmentColor);
            DrawShotRay(resolution.aimRay.origin, resolution.cameraTargetPoint, environmentColor);
            DrawShotRay(resolution.muzzleStart, resolution.finalTracerEnd, environmentColor);
        }

        private static bool TryResolveMuzzleObstruction(Vector3 muzzleStart, Vector3 targetPoint, Collider targetCollider, IDamageable targetDamageable, int raycastMask, out RaycastHit obstruction)
        {
            obstruction = default;
            Vector3 toTarget = targetPoint - muzzleStart;
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            RaycastHit[] hits = Physics.RaycastAll(new Ray(muzzleStart, toTarget.normalized), toTarget.magnitude, raycastMask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (!IsWeaponHitAllowed(hit.collider, null))
                {
                    continue;
                }

                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (hit.collider == targetCollider || (damageable != null && damageable == targetDamageable))
                {
                    return false;
                }

                obstruction = hit;
                return true;
            }

            return false;
        }

        private DamageInfo CreateDamageInfo(Vector3 hitPoint, Vector3 hitNormal, float rangeDamageMultiplier = 1f)
        {
            float critChance = GetCritChance();
            bool isCritical = critChance > 0f && RollCriticalHit(critChance);
            float amount = GetBaseDamage() *
                           Mathf.Max(0.01f, pendingShotDamageMultiplier) *
                           Mathf.Clamp01(rangeDamageMultiplier);
            GameplayTag[] tags = isCritical ? new[] { GameplayTag.OnCrit } : Array.Empty<GameplayTag>();
            if (isCritical)
            {
                amount *= RunStatAggregator.CriticalHitMultiplier;
            }

            return new DamageInfo
            {
                amount = amount,
                source = gameObject,
                weaponId = GetWeaponId(),
                hitPoint = hitPoint,
                hitNormal = hitNormal,
                damageType = GetDamageType(),
                deliveryType = GetDeliveryType(),
                tags = tags,
                canCrit = critChance > 0f,
                isCritical = isCritical,
                knockbackForce = GetKnockbackForce(),
                statusChance = GetStatusChance()
            };
        }

        internal DamageInfo CreateDamageInfoForTests(Vector3 hitPoint, Vector3 hitNormal, float shotMultiplier = 1f, float rangeDamageMultiplier = 1f)
        {
            float previousMultiplier = pendingShotDamageMultiplier;
            pendingShotDamageMultiplier = shotMultiplier;
            DamageInfo damageInfo = CreateDamageInfo(hitPoint, hitNormal, rangeDamageMultiplier);
            pendingShotDamageMultiplier = previousMultiplier;
            return damageInfo;
        }

        private void ResolveWeaponCamera()
        {
            playerController ??= GetComponent<FirstPersonController>();
            if (playerController != null && playerController.PlayerCamera != null)
            {
                weaponCamera = playerController.PlayerCamera;
                return;
            }

            Camera[] activeCameras = GetComponentsInChildren<Camera>(false);
            for (int i = 0; i < activeCameras.Length; i++)
            {
                if (activeCameras[i] != null && activeCameras[i].isActiveAndEnabled)
                {
                    weaponCamera = activeCameras[i];
                    return;
                }
            }

            if (weaponCamera == null)
            {
                weaponCamera = GetComponentInChildren<Camera>(true);
            }
        }

        private void ResolveWeaponDefinition()
        {
            if (weaponDefinition != null)
            {
                return;
            }

            string equippedId = DefaultWeaponId;
            if (GameBootstrap.Instance != null && GameBootstrap.Instance.RunService != null)
            {
                equippedId = string.IsNullOrWhiteSpace(GameBootstrap.Instance.RunService.Current.equippedWeaponId)
                    ? DefaultWeaponId
                    : GameBootstrap.Instance.RunService.Current.equippedWeaponId;
            }

            WeaponDefinition[] definitions = Resources.LoadAll<WeaponDefinition>("Definitions/Combat");
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] != null && definitions[i].weaponId == equippedId)
                {
                    weaponDefinition = definitions[i];
                    return;
                }
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HideAllFeedback();
            ResolveWeaponCamera();
            ResolveWeaponDefinition();
        }

        private void EnsureRuntimeReady()
        {
            if (runtimeReady)
            {
                return;
            }

            playerController = GetComponent<FirstPersonController>();
            playerHealth = GetComponent<PlayerHealth>();
            ResolveWeaponCamera();
            ResolveWeaponDefinition();
            weaponState ??= CreateWeaponRuntimeState();
            EnsureAudio();
            EnsureWeaponBlockout();
            EnsureMuzzleFlash();
            EnsureImpactPool();
            EnsureDamageNumberPool();
            EnsureTracerPool();
            runtimeReady = true;
        }

        private WeaponRuntimeState CreateWeaponRuntimeState()
        {
            int magazineSize = GetMagazineSize();
            int maxReserve = GetMaxReserveAmmo();
            int startingReserve = GetStartingReserveAmmo();
            RunWeaponAmmoState savedAmmo = GetRunAmmoState();
            if (savedAmmo != null && string.Equals(savedAmmo.weaponId, GetWeaponId(), StringComparison.Ordinal))
            {
                savedAmmo.Normalize(GetWeaponId());
                return new WeaponRuntimeState(
                    magazineSize,
                    Mathf.Clamp(savedAmmo.reserveAmmo, 0, maxReserve),
                    maxReserve,
                    Mathf.Clamp(savedAmmo.currentMagazineAmmo, 0, magazineSize));
            }

            return new WeaponRuntimeState(magazineSize, startingReserve, maxReserve);
        }

        private RunWeaponAmmoState GetRunAmmoState()
        {
            return GameBootstrap.Instance != null && GameBootstrap.Instance.RunService != null
                ? GameBootstrap.Instance.RunService.Current.weaponAmmo
                : null;
        }

        private void SyncAmmoToRun(bool save)
        {
            if (weaponState == null || GameBootstrap.Instance == null || GameBootstrap.Instance.RunService == null)
            {
                return;
            }

            RunState run = GameBootstrap.Instance.RunService.Current;
            run.weaponAmmo ??= new RunWeaponAmmoState();
            run.weaponAmmo.weaponId = GetWeaponId();
            run.weaponAmmo.currentMagazineAmmo = weaponState.CurrentAmmo;
            run.weaponAmmo.reserveAmmo = weaponState.ReserveAmmo;
            run.weaponAmmo.maxReserveAmmo = weaponState.MaxReserveAmmo;
            if (save)
            {
                GameBootstrap.Instance.RunService.Save();
            }
        }

        private bool IsInDungeonRuntime()
        {
            return SceneManager.GetActiveScene().name == GameSceneCatalog.DungeonRuntime;
        }

        private bool IsGameplayInputBlocked()
        {
            if (playerHealth == null)
            {
                playerHealth = GetComponent<PlayerHealth>();
            }

            return (playerController != null && playerController.IsUiCaptured) ||
                   (playerHealth != null && playerHealth.IsDead);
        }

        private void BeginReloadFeedback()
        {
            HideAllTracers();
            PlayReloadFeedback(true);
            ReloadStarted?.Invoke(this);
            PublishWeaponEvent(GameplayEventType.ReloadStarted);
        }

        private void QueueAutoReload(float currentTime, float delay)
        {
            if (!autoReloadOnEmpty || weaponState == null || weaponState.ReserveAmmo <= 0)
            {
                return;
            }

            bool queued = weaponState.TryQueueAutoReload(currentTime, delay);
            if (queued)
            {
                LogShotDebug($"autoReloadQueued=true; readyTime={weaponState.AutoReloadReadyTime:0.###}; delay={delay:0.###}");
            }
        }

        private void EnsureAudio()
        {
            Transform audioTransform = transform.Find("WeaponAudio");
            if (audioTransform == null)
            {
                GameObject audioObject = new GameObject("WeaponAudio", typeof(AudioSource));
                audioObject.transform.SetParent(transform, false);
                audioTransform = audioObject.transform;
            }

            weaponAudioSource = audioTransform.GetComponent<AudioSource>();
            if (weaponAudioSource == null)
            {
                weaponAudioSource = audioTransform.gameObject.AddComponent<AudioSource>();
            }

            weaponAudioSource.playOnAwake = false;
            weaponAudioSource.loop = false;
            weaponAudioSource.spatialBlend = 0f;
            weaponAudioSource.volume = weaponVolume;
            gunshotClip ??= CreateFeedbackClip("FrontierRevolverShot", 0.075f, 920f, 0.24f, 0.04f);
            reloadClip ??= CreateFeedbackClip("FrontierRevolverReload", 0.18f, 380f, 0.16f, 0.09f);
            reloadFinishClip ??= CreateFeedbackClip("FrontierRevolverReloadFinish", 0.09f, 620f, 0.14f, 0.04f);
            dryFireClip ??= CreateFeedbackClip("FrontierRevolverDryFire", 0.045f, 1800f, 0.08f, 0.018f);
        }

        private void EnsureWeaponBlockout()
        {
            if (weaponCamera == null || weaponCamera.transform.Find("WeaponBlockout") != null)
            {
                return;
            }

            Transform root = new GameObject("WeaponBlockout").transform;
            root.SetParent(weaponCamera.transform, false);
            root.localPosition = new Vector3(0.25f, -0.28f, 0.56f);
            root.localRotation = Quaternion.Euler(0f, -4f, 0f);

            Material metal = CreateMaterial(new Color(0.22f, 0.22f, 0.24f, 1f));
            Material grip = CreateMaterial(new Color(0.12f, 0.1f, 0.08f, 1f));
            CreateBlockoutPart("Grip", root, PrimitiveType.Cube, new Vector3(-0.07f, -0.13f, -0.05f), new Vector3(0.12f, 0.24f, 0.12f), Quaternion.Euler(12f, 0f, -12f), grip);
            CreateBlockoutPart("Frame", root, PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.02f), new Vector3(0.18f, 0.12f, 0.18f), Quaternion.identity, metal);
            CreateBlockoutPart("Cylinder", root, PrimitiveType.Cylinder, new Vector3(0f, -0.01f, 0.12f), new Vector3(0.13f, 0.09f, 0.13f), Quaternion.Euler(90f, 0f, 0f), metal);
            CreateBlockoutPart("Barrel", root, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0.32f), new Vector3(0.055f, 0.26f, 0.055f), Quaternion.Euler(90f, 0f, 0f), metal);

            Transform muzzlePoint = new GameObject("MuzzlePoint").transform;
            muzzlePoint.SetParent(root, false);
            muzzlePoint.localPosition = new Vector3(0f, 0.02f, 0.47f);
        }

        private static void CreateBlockoutPart(string name, Transform parent, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = localRotation;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyRuntimeObject(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private void EnsureMuzzleFlash()
        {
            if (weaponCamera == null)
            {
                return;
            }

            Transform muzzleParent = GetMuzzlePoint() ?? weaponCamera.transform;
            Transform existing = muzzleParent.Find("WeaponMuzzleFlash") ?? weaponCamera.transform.Find("WeaponMuzzleFlash");
            if (existing != null)
            {
                muzzleFlash = existing.gameObject;
                muzzleFlash.transform.SetParent(muzzleParent, false);
                muzzleFlash.SetActive(false);
                return;
            }

            muzzleFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            muzzleFlash.name = "WeaponMuzzleFlash";
            muzzleFlash.transform.SetParent(muzzleParent, false);
            muzzleFlash.transform.localPosition = Vector3.zero;
            muzzleFlash.transform.localScale = Vector3.one * 0.14f;
            Collider collider = muzzleFlash.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyRuntimeObject(collider);
            }

            Renderer renderer = muzzleFlash.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial(new Color(1f, 0.82f, 0.28f, 1f));
            }

            muzzleFlash.SetActive(false);
        }

        private Transform GetMuzzlePoint()
        {
            return weaponCamera != null ? weaponCamera.transform.Find("WeaponBlockout/MuzzlePoint") : null;
        }

        private Vector3 GetMuzzleWorldPosition(Vector3 fallback)
        {
            Transform muzzlePoint = GetMuzzlePoint();
            return muzzlePoint != null ? muzzlePoint.position : fallback;
        }

        internal static Transform GetOrCreateRuntimeFeedbackRoot()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] roots = activeScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == RuntimeFeedbackRootName)
                {
                    return roots[i].transform;
                }
            }

            GameObject rootObject = new GameObject(RuntimeFeedbackRootName);
            if (activeScene.IsValid())
            {
                SceneManager.MoveGameObjectToScene(rootObject, activeScene);
            }

            return rootObject.transform;
        }

        internal static Transform GetOrCreateFeedbackPool(string poolName)
        {
            Transform root = GetOrCreateRuntimeFeedbackRoot();
            Transform pool = root.Find(poolName);
            if (pool != null)
            {
                return pool;
            }

            GameObject poolObject = new GameObject(poolName);
            poolObject.transform.SetParent(root, false);
            return poolObject.transform;
        }

        internal static void ApplyImpactMarkerColor(Renderer renderer, MaterialPropertyBlock propertyBlock, Color color)
        {
            if (renderer == null || propertyBlock == null)
            {
                return;
            }

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(propertyBlock);
        }

        private void EnsureImpactPool()
        {
            Transform poolTransform = GetOrCreateFeedbackPool("WeaponImpactPool");

            for (int i = 0; i < impactMarkers.Length; i++)
            {
                Transform markerTransform = poolTransform.Find($"ImpactMarker_{i}");
                GameObject markerObject;
                if (markerTransform != null)
                {
                    markerObject = markerTransform.gameObject;
                }
                else
                {
                    markerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    markerObject.name = $"ImpactMarker_{i}";
                    markerObject.transform.SetParent(poolTransform, false);
                    markerObject.transform.localScale = Vector3.one * 0.18f;
                    Collider collider = markerObject.GetComponent<Collider>();
                    if (collider != null)
                    {
                        DestroyRuntimeObject(collider);
                    }
                }

                Renderer renderer = markerObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = CreateMaterial(new Color(1f, 0.95f, 0.72f, 0.95f));
                }

                markerObject.SetActive(false);
                impactMarkers[i] = new ImpactMarker(markerObject);
            }
        }

        private void EnsureDamageNumberPool()
        {
            Transform poolTransform = GetOrCreateFeedbackPool("WeaponDamageNumberPool");

            for (int i = 0; i < damageNumbers.Length; i++)
            {
                Transform numberTransform = poolTransform.Find($"DamageNumber_{i}");
                GameObject numberObject;
                if (numberTransform != null)
                {
                    numberObject = numberTransform.gameObject;
                }
                else
                {
                    numberObject = new GameObject($"DamageNumber_{i}", typeof(TextMesh));
                    numberObject.transform.SetParent(poolTransform, false);
                }

                TextMesh text = numberObject.GetComponent<TextMesh>();
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.characterSize = 0.24f;
                text.fontSize = 52;
                numberObject.SetActive(false);
                damageNumbers[i] = new DamageNumberMarker(numberObject, text);
            }
        }

        private void EnsureTracerPool()
        {
            Transform poolTransform = GetOrCreateFeedbackPool("WeaponTracerPool");

            for (int i = 0; i < tracers.Length; i++)
            {
                Transform tracerTransform = poolTransform.Find($"Tracer_{i}");
                GameObject tracerObject;
                if (tracerTransform != null)
                {
                    tracerObject = tracerTransform.gameObject;
                }
                else
                {
                    tracerObject = new GameObject($"Tracer_{i}", typeof(LineRenderer));
                    tracerObject.transform.SetParent(poolTransform, false);
                }

                LineRenderer line = tracerObject.GetComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.035f;
                line.endWidth = 0.006f;
                line.material = CreateMaterial(new Color(1f, 0.95f, 0.55f, 0.8f));
                tracerObject.SetActive(false);
                tracers[i] = new TracerMarker(tracerObject, line);
            }
        }

        private void PlayFireFeedback(float currentTime)
        {
            if (weaponAudioSource != null && gunshotClip != null)
            {
                weaponAudioSource.PlayOneShot(gunshotClip, 1f);
            }

            if (muzzleFlash != null)
            {
                muzzleFlash.SetActive(true);
                muzzleFlashHideTime = currentTime + muzzleFlashDuration;
            }

            playerController?.ApplyLookRecoil(recoilPitchKick, UnityEngine.Random.Range(-recoilYawKick, recoilYawKick), recoilRecoverySeconds);
        }

        private void PlayReloadFeedback(bool started)
        {
            if (weaponAudioSource == null)
            {
                return;
            }

            if (started && reloadClip != null)
            {
                weaponAudioSource.PlayOneShot(reloadClip, 0.8f);
            }
            else if (!started && reloadFinishClip != null)
            {
                weaponAudioSource.PlayOneShot(reloadFinishClip, 0.75f);
            }
        }

        private void PlayDryFireFeedback()
        {
            if (weaponAudioSource != null && dryFireClip != null)
            {
                weaponAudioSource.PlayOneShot(dryFireClip, 0.85f);
            }
        }

        private void UpdateMuzzleFlash(float currentTime)
        {
            if (muzzleFlash != null && muzzleFlash.activeSelf && currentTime >= muzzleFlashHideTime)
            {
                muzzleFlash.SetActive(false);
            }
        }

        private void ShowImpactMarker(Vector3 point, Vector3 normal, Color color)
        {
            if (impactMarkers.Length == 0)
            {
                return;
            }

            ImpactMarker marker = impactMarkers[nextImpactMarker];
            nextImpactMarker = (nextImpactMarker + 1) % impactMarkers.Length;
            marker.Show(point + normal.normalized * 0.04f, Time.time + impactMarkerDuration, color);
        }

        private void ShowDamageNumber(Vector3 point, float amount, Color color, bool killedTarget, string label = "")
        {
            CombatFeedbackService.ShowDamageNumber(point, amount, color, killedTarget, label);
        }

        private void ShowTracer(Vector3 start, Vector3 end, Color color)
        {
            if (tracers.Length == 0)
            {
                return;
            }

            TracerMarker tracer = tracers[nextTracer];
            nextTracer = (nextTracer + 1) % tracers.Length;
            float lifetime = Mathf.Clamp(tracerDuration, 0.035f, 0.07f);
            tracer.Show(start, end, Time.time + lifetime, color);
        }

        private void HideAllTracers()
        {
            for (int i = 0; i < tracers.Length; i++)
            {
                tracers[i]?.Hide();
            }
        }

        private void HideAllFeedback()
        {
            HideAllTracers();
            for (int i = 0; i < impactMarkers.Length; i++)
            {
                impactMarkers[i]?.Hide();
            }

            for (int i = 0; i < damageNumbers.Length; i++)
            {
                damageNumbers[i]?.Hide();
            }
        }

        private void DrawShotRay(Vector3 start, Vector3 end, Color color)
        {
            if (enableShotDebugDraw && IsDevelopmentDebugEnabled())
            {
                Debug.DrawLine(start, end, color, ShotDebugDrawDuration);
            }
        }

        private void LogShotDebug(string message)
        {
            if (enableShotDebugLogging && IsDevelopmentDebugEnabled())
            {
                Debug.Log($"[WeaponShotDebug] {message}", this);
            }
        }

        private static bool IsDevelopmentDebugEnabled()
        {
            return Application.isEditor || Debug.isDebugBuild;
        }

        private void UpdateImpactMarkers(float currentTime)
        {
            for (int i = 0; i < impactMarkers.Length; i++)
            {
                impactMarkers[i]?.Tick(currentTime);
            }

            for (int i = 0; i < damageNumbers.Length; i++)
            {
                damageNumbers[i]?.Tick(currentTime, weaponCamera);
            }

            for (int i = 0; i < tracers.Length; i++)
            {
                tracers[i]?.Tick(currentTime);
            }
        }

        private string GetWeaponId()
        {
            return weaponDefinition != null && !string.IsNullOrWhiteSpace(weaponDefinition.weaponId)
                ? weaponDefinition.weaponId
                : DefaultWeaponId;
        }

        private string GetWeaponName()
        {
            return weaponDefinition != null && !string.IsNullOrWhiteSpace(weaponDefinition.displayName)
                ? weaponDefinition.displayName
                : DefaultWeaponName;
        }

        private float GetBaseDamage()
        {
            float baseDamage = GetBaseWeaponDamage();
            return GetWeaponArchetype() == WeaponArchetype.Revolver
                ? baseDamage * RunStatAggregator.Current.RevolverDamageMultiplier
                : baseDamage;
        }

        private float GetBaseWeaponDamage()
        {
            return weaponDefinition != null && weaponDefinition.baseDamage > 0f ? weaponDefinition.baseDamage : DefaultDamage;
        }

        private float GetFireCooldown()
        {
            float fireRate = weaponDefinition != null && weaponDefinition.fireRate > 0f ? weaponDefinition.fireRate : DefaultFireRate;
            return 1f / Mathf.Max(0.01f, fireRate);
        }

        private int GetMagazineSize()
        {
            return weaponDefinition != null && weaponDefinition.magazineSize > 0 ? weaponDefinition.magazineSize : DefaultMagazineSize;
        }

        private int GetStartingReserveAmmo()
        {
            return weaponDefinition != null && weaponDefinition.startingReserveAmmo >= 0
                ? weaponDefinition.startingReserveAmmo
                : DefaultStartingReserveAmmo;
        }

        private int GetMaxReserveAmmo()
        {
            int configured = weaponDefinition != null && weaponDefinition.maxReserveAmmo >= 0
                ? weaponDefinition.maxReserveAmmo
                : DefaultMaxReserveAmmo;
            return Mathf.Max(0, configured);
        }

        private float GetReloadDuration()
        {
            return GetBaseReloadDuration() / Mathf.Max(0.1f, RunStatAggregator.Current.ReloadSpeedMultiplier);
        }

        private float GetBaseReloadDuration()
        {
            return weaponDefinition != null && weaponDefinition.reloadDuration > 0f ? weaponDefinition.reloadDuration : DefaultReloadDuration;
        }

        private float GetRange()
        {
            return weaponDefinition != null && weaponDefinition.maxRange > 0f ? weaponDefinition.maxRange : DefaultRange;
        }

        private float GetFullDamageRange()
        {
            float range = GetRange();
            float configured = weaponDefinition != null && weaponDefinition.fullDamageRange > 0f
                ? weaponDefinition.fullDamageRange
                : DefaultFullDamageRange;
            return Mathf.Clamp(configured, 0f, range);
        }

        private float GetDamageMultiplierAtMaxRange()
        {
            float configured = weaponDefinition != null && weaponDefinition.damageMultiplierAtMaxRange > 0f
                ? weaponDefinition.damageMultiplierAtMaxRange
                : DefaultDamageMultiplierAtMaxRange;
            return Mathf.Clamp01(configured);
        }

        internal static float CalculateRangeDamageMultiplier(float distance, float fullDamageRange, float maxRange, float multiplierAtMaxRange)
        {
            float clampedMaxRange = Mathf.Max(0.01f, maxRange);
            if (distance > clampedMaxRange)
            {
                return 0f;
            }

            float clampedFullRange = Mathf.Clamp(fullDamageRange, 0f, clampedMaxRange);
            if (distance <= clampedFullRange)
            {
                return 1f;
            }

            float t = Mathf.InverseLerp(clampedFullRange, clampedMaxRange, distance);
            return Mathf.Lerp(1f, Mathf.Clamp01(multiplierAtMaxRange), t);
        }

        private DamageType GetDamageType()
        {
            return weaponDefinition != null ? weaponDefinition.damageType : DamageType.Physical;
        }

        private DamageDeliveryType GetDeliveryType()
        {
            return weaponDefinition != null ? weaponDefinition.deliveryType : DamageDeliveryType.Raycast;
        }

        private WeaponArchetype GetWeaponArchetype()
        {
            return weaponDefinition != null ? weaponDefinition.weaponArchetype : WeaponArchetype.Revolver;
        }

        private float GetCritChance()
        {
            float baseCritChance = weaponDefinition != null ? Mathf.Max(0f, weaponDefinition.critChance) : 0f;
            return Mathf.Clamp01(baseCritChance + RunStatAggregator.Current.CritChanceBonus);
        }

        private static bool RollCriticalHit(float critChance)
        {
            float roll = CritRollProviderForTests != null ? CritRollProviderForTests.Invoke() : UnityEngine.Random.value;
            return roll < Mathf.Clamp01(critChance);
        }

        private float GetKnockbackForce()
        {
            return weaponDefinition != null ? Mathf.Max(0f, weaponDefinition.knockbackForce) : 0f;
        }

        private float GetStatusChance()
        {
            return weaponDefinition != null ? Mathf.Max(0f, weaponDefinition.statusChance) : 0f;
        }

        private float GetWeaponHearingRadius()
        {
            return Mathf.Max(0f, weaponHearingRadius);
        }

        private int GetFloorIndex()
        {
            return GameBootstrap.Instance != null && GameBootstrap.Instance.RunService != null
                ? GameBootstrap.Instance.RunService.Current.floorIndex
                : 0;
        }

        private void PublishWeaponEvent(GameplayEventType eventType, float amount = 0f)
        {
            GameplayEventBus.Publish(new GameplayEvent
            {
                eventType = eventType,
                sourceObject = gameObject,
                weaponId = GetWeaponId(),
                weaponArchetype = GetWeaponArchetype().ToString(),
                damageType = GetDamageType().ToString(),
                deliveryType = GetDeliveryType().ToString(),
                amount = amount,
                worldPosition = transform.position,
                radius = eventType == GameplayEventType.WeaponFired ? GetWeaponHearingRadius() : 0f,
                floorIndex = GetFloorIndex(),
                timestamp = Time.unscaledTime
            });
        }

        private void PublishWeaponHitEvents(DamageInfo damageInfo, DamageResult result, GameObject targetObject)
        {
            GameplayEvent hitEvent = CreateGameplayEvent(GameplayEventType.WeaponHit, damageInfo, result, targetObject);
            GameplayEvent damageEvent = CreateGameplayEvent(GameplayEventType.DamageDealt, damageInfo, result, targetObject);
            GameplayEventBus.Publish(hitEvent);
            GameplayEventBus.Publish(damageEvent);
            if (damageInfo.isCritical)
            {
                GameplayEventBus.Publish(CreateGameplayEvent(GameplayEventType.CriticalHit, damageInfo, result, targetObject));
            }

            TryApplyChainHit(damageInfo, result, targetObject);
        }

        private void HandleGameplayEvent(GameplayEvent gameplayEvent)
        {
            playerHealth ??= GetComponent<PlayerHealth>();
            if (gameplayEvent.eventType != GameplayEventType.EnemyKilled ||
                gameplayEvent.sourceObject == null ||
                playerHealth == null ||
                playerHealth.IsDead)
            {
                return;
            }

            PlayerWeaponController sourceWeapon = gameplayEvent.sourceObject.GetComponentInParent<PlayerWeaponController>();
            if (sourceWeapon != this)
            {
                return;
            }

            int healAmount = RunStatAggregator.Current.KillHealAmount;
            if (healAmount > 0)
            {
                playerHealth.Heal(healAmount);
            }
        }

        internal void HandleGameplayEventForTests(GameplayEvent gameplayEvent)
        {
            HandleGameplayEvent(gameplayEvent);
        }

        internal bool TryApplyChainHit(DamageInfo damageInfo, DamageResult result, GameObject targetObject)
        {
            if (applyingChainHit || !result.applied || targetObject == null)
            {
                return false;
            }

            EnemyHealth originalEnemy = targetObject.GetComponentInParent<EnemyHealth>();
            if (originalEnemy == null)
            {
                return false;
            }

            RunStatSnapshot stats = RunStatAggregator.Current;
            if (!stats.HasChainHit)
            {
                return false;
            }

            EnemyHealth chainTarget = FindNearestChainTarget(originalEnemy, damageInfo.hitPoint, RunUpgradeCatalog.ChainHitSearchRadius);
            if (chainTarget == null)
            {
                return false;
            }

            applyingChainHit = true;
            DamageInfo chainDamage = new DamageInfo
            {
                amount = Mathf.Max(0f, result.damageApplied * stats.chainDamageFraction),
                source = gameObject,
                weaponId = GetWeaponId(),
                hitPoint = chainTarget.transform.position + Vector3.up,
                hitNormal = Vector3.up,
                damageType = DamageType.Shock,
                deliveryType = DamageDeliveryType.Area,
                tags = new[] { GameplayTag.Shock, GameplayTag.OnHit },
                canCrit = false,
                isCritical = false
            };

            DamageResult chainResult = chainTarget.ApplyDamage(chainDamage);
            if (chainResult.applied)
            {
                Color chainColor = new Color(0.42f, 0.85f, 1f, 1f);
                if (runtimeReady)
                {
                    ShowTracer(damageInfo.hitPoint, chainDamage.hitPoint, chainColor);
                    ShowDamageNumber(chainDamage.hitPoint, chainResult.damageApplied, chainColor, chainResult.killedTarget, "CHAIN");
                }

                HitFeedbackReceived?.Invoke(new WeaponHitFeedback
                {
                    kind = WeaponHitFeedbackKind.Chain,
                    damageResult = chainResult,
                    targetObject = chainTarget.gameObject,
                    hitPoint = damageInfo.hitPoint,
                    hitNormal = Vector3.up,
                    secondaryPoint = chainDamage.hitPoint,
                    requestedDamage = chainDamage.amount,
                    finalDamage = chainResult.damageApplied,
                    isChain = true,
                    label = "CHAIN"
                });
                GameplayEventBus.Publish(CreateGameplayEvent(GameplayEventType.DamageDealt, chainDamage, chainResult, chainTarget.gameObject));
            }

            applyingChainHit = false;
            return chainResult.applied;
        }

        private static EnemyHealth FindNearestChainTarget(EnemyHealth originalEnemy, Vector3 origin, float radius)
        {
            EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();
            EnemyHealth nearest = null;
            float nearestDistanceSquared = radius * radius;
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyHealth candidate = enemies[i];
                if (candidate == null || candidate == originalEnemy || candidate.IsDead)
                {
                    continue;
                }

                float distanceSquared = (candidate.transform.position - origin).sqrMagnitude;
                if (distanceSquared <= nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearest = candidate;
                }
            }

            return nearest;
        }

        private GameplayEvent CreateGameplayEvent(GameplayEventType eventType, DamageInfo damageInfo, DamageResult result, GameObject targetObject)
        {
            return new GameplayEvent
            {
                eventType = eventType,
                sourceObject = gameObject,
                targetObject = targetObject,
                weaponId = damageInfo.weaponId,
                weaponArchetype = GetWeaponArchetype().ToString(),
                damageType = damageInfo.damageType.ToString(),
                deliveryType = damageInfo.deliveryType.ToString(),
                amount = damageInfo.amount,
                finalAmount = result.damageApplied,
                worldPosition = damageInfo.hitPoint,
                tags = ConvertTags(damageInfo.tags),
                wasCritical = damageInfo.isCritical,
                killedTarget = result.killedTarget,
                floorIndex = GetFloorIndex(),
                timestamp = Time.unscaledTime
            };
        }

        private static string[] ConvertTags(GameplayTag[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return Array.Empty<string>();
            }

            string[] converted = new string[tags.Length];
            for (int i = 0; i < tags.Length; i++)
            {
                converted[i] = tags[i].ToString();
            }

            return converted;
        }

        private static AudioClip CreateFeedbackClip(string name, float durationSeconds, float frequency, float amplitude, float decaySeconds)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.CeilToInt(durationSeconds * sampleRate);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float envelope = Mathf.Exp(-time / Mathf.Max(0.01f, decaySeconds));
                samples[i] = Mathf.Sin(time * frequency * Mathf.PI * 2f) * amplitude * envelope;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static Material CreateMaterial(Color color)
        {
            return new Material(Shader.Find("Standard"))
            {
                color = color
            };
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private sealed class ImpactMarker
        {
            private readonly GameObject markerObject;
            private Renderer renderer;
            private MaterialPropertyBlock propertyBlock;
            private float hideTime;

            public ImpactMarker(GameObject markerObject)
            {
                this.markerObject = markerObject;
                renderer = markerObject != null ? markerObject.GetComponent<Renderer>() : null;
                propertyBlock = new MaterialPropertyBlock();
            }

            public void Show(Vector3 position, float hideTime, Color color)
            {
                if (markerObject == null)
                {
                    return;
                }

                this.hideTime = hideTime;
                markerObject.transform.position = position;
                if (renderer != null)
                {
                    ApplyImpactMarkerColor(renderer, propertyBlock, color);
                }

                markerObject.SetActive(true);
            }

            public void Tick(float currentTime)
            {
                if (markerObject != null && markerObject.activeSelf && currentTime >= hideTime)
                {
                    markerObject.SetActive(false);
                }
            }

            public void Hide()
            {
                if (markerObject != null)
                {
                    markerObject.SetActive(false);
                }

                hideTime = 0f;
            }
        }

        internal sealed class TracerMarker
        {
            private readonly GameObject tracerObject;
            private readonly LineRenderer lineRenderer;
            private float hideTime;

            public TracerMarker(GameObject tracerObject, LineRenderer lineRenderer)
            {
                this.tracerObject = tracerObject;
                this.lineRenderer = lineRenderer;
            }

            public void Show(Vector3 start, Vector3 end, float hideTime, Color color)
            {
                if (tracerObject == null || lineRenderer == null)
                {
                    return;
                }

                tracerObject.SetActive(false);
                this.hideTime = hideTime;
                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, start);
                lineRenderer.SetPosition(1, end);
                lineRenderer.startColor = color;
                Color endColor = color;
                endColor.a = 0f;
                lineRenderer.endColor = endColor;
                tracerObject.SetActive(true);
            }

            public void Tick(float currentTime)
            {
                if (tracerObject != null && tracerObject.activeSelf && currentTime >= hideTime)
                {
                    Hide();
                }
            }

            public void Hide()
            {
                if (lineRenderer != null)
                {
                    lineRenderer.SetPosition(0, Vector3.zero);
                    lineRenderer.SetPosition(1, Vector3.zero);
                    lineRenderer.startColor = Color.clear;
                    lineRenderer.endColor = Color.clear;
                    lineRenderer.enabled = false;
                }

                if (tracerObject != null)
                {
                    tracerObject.SetActive(false);
                }

                hideTime = 0f;
            }
        }

        private sealed class DamageNumberMarker
        {
            private readonly GameObject markerObject;
            private readonly TextMesh text;
            private Vector3 startPosition;
            private float showTime;
            private float hideTime;

            public DamageNumberMarker(GameObject markerObject, TextMesh text)
            {
                this.markerObject = markerObject;
                this.text = text;
            }

            public void Show(Vector3 position, float hideTime, float amount, Color color, bool killedTarget, string label)
            {
                if (markerObject == null || text == null)
                {
                    return;
                }

                startPosition = position;
                showTime = Time.time;
                this.hideTime = hideTime;
                markerObject.transform.position = position;
                if (!string.IsNullOrWhiteSpace(label))
                {
                    text.text = killedTarget ? $"{amount:0}\n{label}\nDOWN" : $"{amount:0}\n{label}";
                }
                else
                {
                    text.text = killedTarget ? $"{amount:0}\nDOWN" : amount.ToString("0");
                }

                text.color = color;
                markerObject.SetActive(true);
            }

            public void Tick(float currentTime, Camera camera)
            {
                if (markerObject == null || !markerObject.activeSelf)
                {
                    return;
                }

                if (currentTime >= hideTime)
                {
                    markerObject.SetActive(false);
                    return;
                }

                float t = Mathf.InverseLerp(showTime, hideTime, currentTime);
                markerObject.transform.position = startPosition + Vector3.up * (0.65f * t);
                if (camera != null)
                {
                    Vector3 awayFromCamera = markerObject.transform.position - camera.transform.position;
                    if (awayFromCamera.sqrMagnitude > 0.001f)
                    {
                        markerObject.transform.rotation = Quaternion.LookRotation(awayFromCamera.normalized, Vector3.up);
                    }
                }

                Color color = text.color;
                color.a = 1f - t;
                text.color = color;
            }

            public void Hide()
            {
                if (markerObject != null)
                {
                    markerObject.SetActive(false);
                }

                hideTime = 0f;
            }
        }
    }
}
