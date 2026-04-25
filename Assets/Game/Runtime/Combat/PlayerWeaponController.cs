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
        private const float DefaultDamage = 25f;
        private const float DefaultFireRate = 2.857f;
        private const int DefaultMagazineSize = 6;
        private const float DefaultReloadDuration = 1.4f;
        private const float DefaultRange = 100f;
        private const int ImpactPoolSize = 12;
        private const int DamageNumberPoolSize = 12;

        [SerializeField] private Camera weaponCamera;
        [SerializeField] private WeaponDefinition weaponDefinition;
        [SerializeField] private LayerMask weaponRaycastMask = ~(1 << 2);
        [SerializeField] private float muzzleFlashDuration = 0.045f;
        [SerializeField] private float impactMarkerDuration = 1.25f;
        [SerializeField] private float damageNumberDuration = 0.75f;
        [SerializeField] private float weaponVolume = 0.28f;
        [SerializeField] private float recoilPitchKick = 1.2f;
        [SerializeField] private float recoilYawKick = 0.35f;
        [SerializeField] private float recoilRecoverySeconds = 0.15f;

        private readonly ImpactMarker[] impactMarkers = new ImpactMarker[ImpactPoolSize];
        private readonly DamageNumberMarker[] damageNumbers = new DamageNumberMarker[DamageNumberPoolSize];

        private FirstPersonController playerController;
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

        public event Action<PlayerWeaponController> WeaponFired;
        public event Action<PlayerWeaponController> ReloadStarted;
        public event Action<PlayerWeaponController> ReloadFinished;
        public event Action<PlayerWeaponController> DryFired;
        public event Action<DamageResult> DamageHitConfirmed;
        public event Action<WeaponHitFeedback> HitFeedbackReceived;

        public string WeaponId => GetWeaponId();
        public string WeaponName => GetWeaponName();
        public int CurrentAmmo => weaponState != null ? weaponState.CurrentAmmo : GetMagazineSize();
        public int MagazineSize => weaponState != null ? weaponState.MagazineSize : GetMagazineSize();
        public bool IsReloading => weaponState != null && weaponState.IsReloading;
        public float ReloadProgress => weaponState != null ? weaponState.GetReloadProgress(Time.time) : 1f;
        public float ReloadDuration => GetReloadDuration();

        private void Awake()
        {
            playerController = GetComponent<FirstPersonController>();
            weaponCamera ??= GetComponentInChildren<Camera>();
            ResolveWeaponDefinition();
            weaponState = new WeaponRuntimeState(GetMagazineSize());
            EnsureAudio();
            EnsureWeaponBlockout();
            EnsureMuzzleFlash();
            EnsureImpactPool();
            EnsureDamageNumberPool();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            float currentTime = Time.time;
            if (weaponState != null && weaponState.Tick(currentTime))
            {
                PlayReloadFeedback(false);
                ReloadFinished?.Invoke(this);
                PublishWeaponEvent(GameplayEventType.ReloadFinished);
            }

            UpdateMuzzleFlash(currentTime);
            UpdateImpactMarkers(currentTime);

            if (!IsInDungeonRuntime() || playerController == null || playerController.IsUiCaptured)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                TryStartReload(currentTime);
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryFire(currentTime);
            }
        }

        public bool TryFire(float currentTime)
        {
            if (weaponState == null)
            {
                weaponState = new WeaponRuntimeState(GetMagazineSize());
            }

            if (!weaponState.TryFire(currentTime, GetFireCooldown()))
            {
                if (!weaponState.IsReloading && weaponState.CurrentAmmo <= 0)
                {
                    PlayDryFireFeedback();
                    DryFired?.Invoke(this);
                    PublishWeaponEvent(GameplayEventType.DryFire);
                }

                return false;
            }

            PlayFireFeedback(currentTime);
            WeaponFired?.Invoke(this);
            PublishWeaponEvent(GameplayEventType.WeaponFired, amount: GetBaseDamage());
            FireRaycast();

            return true;
        }

        public bool TryStartReload(float currentTime)
        {
            if (weaponState == null)
            {
                weaponState = new WeaponRuntimeState(GetMagazineSize());
            }

            if (!weaponState.TryStartReload(currentTime, GetReloadDuration()))
            {
                return false;
            }

            PlayReloadFeedback(true);
            ReloadStarted?.Invoke(this);
            PublishWeaponEvent(GameplayEventType.ReloadStarted);
            return true;
        }

        public static bool IsWeaponHitAllowed(Collider hitCollider, Transform playerRoot)
        {
            if (hitCollider == null)
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

        private void FireRaycast()
        {
            if (weaponCamera == null)
            {
                return;
            }

            Ray ray = weaponCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, GetRange(), weaponRaycastMask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (!IsWeaponHitAllowed(hit.collider, transform))
                {
                    continue;
                }

                DamageInfo damageInfo = CreateDamageInfo(hit.point, hit.normal);
                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    DamageResult result = damageable.ApplyDamage(damageInfo);
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
                            targetObject = hit.collider.gameObject,
                            hitPoint = hit.point,
                            hitNormal = hit.normal,
                            requestedDamage = damageInfo.amount,
                            finalDamage = result.damageApplied
                        };
                        DamageHitConfirmed?.Invoke(result);
                        HitFeedbackReceived?.Invoke(feedback);
                        ShowImpactMarker(hit.point, hit.normal, impactColor);
                        ShowDamageNumber(hit.point, result.damageApplied, impactColor, result.killedTarget);
                        PublishWeaponHitEvents(damageInfo, result, hit.collider.gameObject);
                    }
                }
                else
                {
                    WeaponHitFeedback feedback = new WeaponHitFeedback
                    {
                        kind = WeaponHitFeedbackKind.Environment,
                        hitPoint = hit.point,
                        hitNormal = hit.normal
                    };
                    HitFeedbackReceived?.Invoke(feedback);
                    ShowImpactMarker(hit.point, hit.normal, new Color(0.85f, 0.88f, 0.78f, 1f));
                }

                return;
            }
        }

        private DamageInfo CreateDamageInfo(Vector3 hitPoint, Vector3 hitNormal)
        {
            return new DamageInfo
            {
                amount = GetBaseDamage(),
                source = gameObject,
                weaponId = GetWeaponId(),
                hitPoint = hitPoint,
                hitNormal = hitNormal,
                damageType = GetDamageType(),
                deliveryType = GetDeliveryType(),
                tags = Array.Empty<GameplayTag>(),
                canCrit = GetCritChance() > 0f,
                isCritical = false,
                knockbackForce = GetKnockbackForce(),
                statusChance = GetStatusChance()
            };
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
            weaponCamera ??= GetComponentInChildren<Camera>();
            ResolveWeaponDefinition();
        }

        private bool IsInDungeonRuntime()
        {
            return SceneManager.GetActiveScene().name == GameSceneCatalog.DungeonRuntime;
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
                Destroy(collider);
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
                Destroy(collider);
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

        private void EnsureImpactPool()
        {
            Transform poolTransform = transform.Find("WeaponImpactPool");
            if (poolTransform == null)
            {
                GameObject poolObject = new GameObject("WeaponImpactPool");
                poolObject.transform.SetParent(transform, false);
                poolTransform = poolObject.transform;
            }

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
                        Destroy(collider);
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
            Transform poolTransform = transform.Find("WeaponDamageNumberPool");
            if (poolTransform == null)
            {
                GameObject poolObject = new GameObject("WeaponDamageNumberPool");
                poolObject.transform.SetParent(transform, false);
                poolTransform = poolObject.transform;
            }

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

        private void ShowDamageNumber(Vector3 point, float amount, Color color, bool killedTarget)
        {
            if (damageNumbers.Length == 0)
            {
                return;
            }

            DamageNumberMarker number = damageNumbers[nextDamageNumber];
            nextDamageNumber = (nextDamageNumber + 1) % damageNumbers.Length;
            number.Show(point + Vector3.up * 1.25f, Time.time + damageNumberDuration, amount, color, killedTarget);
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

        private float GetReloadDuration()
        {
            return weaponDefinition != null && weaponDefinition.reloadDuration > 0f ? weaponDefinition.reloadDuration : DefaultReloadDuration;
        }

        private float GetRange()
        {
            return weaponDefinition != null && weaponDefinition.maxRange > 0f ? weaponDefinition.maxRange : DefaultRange;
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
            return weaponDefinition != null ? Mathf.Max(0f, weaponDefinition.critChance) : 0f;
        }

        private float GetKnockbackForce()
        {
            return weaponDefinition != null ? Mathf.Max(0f, weaponDefinition.knockbackForce) : 0f;
        }

        private float GetStatusChance()
        {
            return weaponDefinition != null ? Mathf.Max(0f, weaponDefinition.statusChance) : 0f;
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

        private sealed class ImpactMarker
        {
            private readonly GameObject markerObject;
            private Renderer renderer;
            private float hideTime;

            public ImpactMarker(GameObject markerObject)
            {
                this.markerObject = markerObject;
                renderer = markerObject != null ? markerObject.GetComponent<Renderer>() : null;
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
                    renderer.sharedMaterial.color = color;
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

            public void Show(Vector3 position, float hideTime, float amount, Color color, bool killedTarget)
            {
                if (markerObject == null || text == null)
                {
                    return;
                }

                startPosition = position;
                showTime = Time.time;
                this.hideTime = hideTime;
                markerObject.transform.position = position;
                text.text = killedTarget ? $"{amount:0}\nDOWN" : amount.ToString("0");
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
        }
    }
}
