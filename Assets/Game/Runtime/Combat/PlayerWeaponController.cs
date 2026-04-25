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

        [SerializeField] private Camera weaponCamera;
        [SerializeField] private WeaponDefinition weaponDefinition;
        [SerializeField] private LayerMask weaponRaycastMask = ~(1 << 2);
        [SerializeField] private float muzzleFlashDuration = 0.045f;
        [SerializeField] private float impactMarkerDuration = 1.25f;
        [SerializeField] private float weaponVolume = 0.28f;

        private readonly ImpactMarker[] impactMarkers = new ImpactMarker[ImpactPoolSize];

        private FirstPersonController playerController;
        private WeaponRuntimeState weaponState;
        private AudioSource weaponAudioSource;
        private AudioClip gunshotClip;
        private AudioClip reloadClip;
        private GameObject muzzleFlash;
        private float muzzleFlashHideTime;
        private int nextImpactMarker;

        public event Action<PlayerWeaponController> WeaponFired;
        public event Action<PlayerWeaponController> ReloadStarted;
        public event Action<PlayerWeaponController> ReloadFinished;
        public event Action<DamageResult> DamageHitConfirmed;

        public string WeaponId => GetWeaponId();
        public string WeaponName => GetWeaponName();
        public int CurrentAmmo => weaponState != null ? weaponState.CurrentAmmo : GetMagazineSize();
        public int MagazineSize => weaponState != null ? weaponState.MagazineSize : GetMagazineSize();
        public bool IsReloading => weaponState != null && weaponState.IsReloading;
        public float ReloadDuration => GetReloadDuration();

        private void Awake()
        {
            playerController = GetComponent<FirstPersonController>();
            weaponCamera ??= GetComponentInChildren<Camera>();
            ResolveWeaponDefinition();
            weaponState = new WeaponRuntimeState(GetMagazineSize());
            EnsureAudio();
            EnsureMuzzleFlash();
            EnsureImpactPool();
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
                    TryStartReload(currentTime);
                }

                return false;
            }

            PlayFireFeedback(currentTime);
            FireRaycast();
            WeaponFired?.Invoke(this);

            if (weaponState.CurrentAmmo <= 0)
            {
                TryStartReload(currentTime);
            }

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
                        DamageHitConfirmed?.Invoke(result);
                    }
                }
                else
                {
                    ShowImpactMarker(hit.point, hit.normal, Color.white);
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
        }

        private void EnsureMuzzleFlash()
        {
            if (weaponCamera == null)
            {
                return;
            }

            Transform existing = weaponCamera.transform.Find("WeaponMuzzleFlash");
            if (existing != null)
            {
                muzzleFlash = existing.gameObject;
                muzzleFlash.SetActive(false);
                return;
            }

            muzzleFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            muzzleFlash.name = "WeaponMuzzleFlash";
            muzzleFlash.transform.SetParent(weaponCamera.transform, false);
            muzzleFlash.transform.localPosition = new Vector3(0.18f, -0.14f, 0.48f);
            muzzleFlash.transform.localScale = Vector3.one * 0.12f;
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
        }

        private void PlayReloadFeedback(bool started)
        {
            if (started && weaponAudioSource != null && reloadClip != null)
            {
                weaponAudioSource.PlayOneShot(reloadClip, 0.8f);
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

        private void UpdateImpactMarkers(float currentTime)
        {
            for (int i = 0; i < impactMarkers.Length; i++)
            {
                impactMarkers[i]?.Tick(currentTime);
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
    }
}
