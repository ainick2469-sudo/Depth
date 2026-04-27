using FrontierDepths.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrontierDepths.Combat
{
    public sealed class PlayerPistolWhipController : MonoBehaviour
    {
        public const float DefaultDamage = 7f;
        public const float DefaultRange = 2f;
        public const float DefaultRadius = 0.35f;
        public const float DefaultCooldownSeconds = 0.9f;

        [SerializeField] private Camera aimCamera;
        [SerializeField] private float damage = DefaultDamage;
        [SerializeField] private float range = DefaultRange;
        [SerializeField] private float spherecastRadius = DefaultRadius;
        [SerializeField] private float cooldownSeconds = DefaultCooldownSeconds;
        [SerializeField] private LayerMask hitMask = -1;

        private FirstPersonController playerController;
        private PlayerHealth playerHealth;
        private AudioSource whipAudioSource;
        private AudioClip swingClip;
        private AudioClip hitClip;
        private AudioClip wallClip;
        private Transform weaponBlockout;
        private Vector3 weaponBlockoutRestPosition;
        private Vector3 weaponBlockoutRestEuler;
        private float nextWhipTime;
        private float feedbackEndTime;
        private bool feedbackHit;

        public float Damage => damage;
        public float Range => range;
        public float CooldownSeconds => cooldownSeconds;
        public float NextWhipTime => nextWhipTime;

        private void Awake()
        {
            ResolveComponents();
        }

        private void Update()
        {
            if (!IsInDungeonRuntime() || IsBlocked())
            {
                return;
            }

            TickFeedback();

            if (InputBindingService.GetKeyDown(GameplayInputAction.PistolWhip))
            {
                TryWhip(Time.time);
            }
        }

        public bool TryWhip(float currentTime)
        {
            ResolveComponents();
            if (IsBlocked() || currentTime < nextWhipTime)
            {
                return false;
            }

            nextWhipTime = currentTime + Mathf.Max(0.05f, cooldownSeconds);
            bool hitApplied = TryResolveHit(out RaycastHit hit, out IDamageable damageable) &&
                              ApplyWhipDamage(hit, damageable);
            PlayWhipFeedback(hitApplied, hit);
            return hitApplied;
        }

        internal bool TryWhipForTests(float currentTime)
        {
            return TryWhip(currentTime);
        }

        private bool ApplyWhipDamage(RaycastHit hit, IDamageable damageable)
        {
            if (damageable == null)
            {
                return false;
            }

            DamageInfo damageInfo = new DamageInfo
            {
                amount = Mathf.Max(0f, damage),
                source = gameObject,
                weaponId = "weapon.pistol_whip",
                hitPoint = hit.point,
                hitNormal = hit.normal,
                damageType = DamageType.Physical,
                deliveryType = DamageDeliveryType.Melee,
                canCrit = false,
                isCritical = false,
                knockbackForce = 1.5f,
                tags = new[] { GameplayTag.Melee }
            };

            DamageResult result = damageable.ApplyDamage(damageInfo);
            return result.applied;
        }

        private bool TryResolveHit(out RaycastHit hit, out IDamageable damageable)
        {
            hit = default;
            damageable = null;
            ResolveComponents();
            if (aimCamera == null)
            {
                return false;
            }

            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.SphereCastAll(
                ray,
                Mathf.Max(0.01f, spherecastRadius),
                Mathf.Max(0.01f, range),
                hitMask,
                QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null || collider.transform.IsChildOf(transform) || collider.gameObject.layer == 2)
                {
                    continue;
                }

                IDamageable candidate = collider.GetComponentInParent<IDamageable>();
                if (candidate == null)
                {
                    hit = hits[i];
                    return false;
                }

                hit = hits[i];
                damageable = candidate;
                return true;
            }

            return false;
        }

        private bool IsBlocked()
        {
            ResolveComponents();
            return Time.timeScale <= 0f ||
                   (playerController != null && playerController.IsUiCaptured) ||
                   (playerHealth != null && playerHealth.IsDead);
        }

        private void ResolveComponents()
        {
            playerController ??= GetComponent<FirstPersonController>();
            playerHealth ??= GetComponent<PlayerHealth>();
            aimCamera ??= playerController != null ? playerController.PlayerCamera : GetComponentInChildren<Camera>();
            EnsureFeedbackObjects();
            if (hitMask.value == 0 || hitMask.value == -1)
            {
                hitMask = PlayerWeaponController.DefaultWeaponRaycastMask;
            }
        }

        private void EnsureFeedbackObjects()
        {
            if (whipAudioSource == null)
            {
                whipAudioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
                whipAudioSource.playOnAwake = false;
                whipAudioSource.loop = false;
                whipAudioSource.spatialBlend = 0f;
                whipAudioSource.volume = 0.22f;
            }

            swingClip ??= CreateFeedbackClip("PistolWhipSwing", 0.08f, 520f, 0.12f, 0.04f);
            hitClip ??= CreateFeedbackClip("PistolWhipHit", 0.09f, 160f, 0.2f, 0.045f);
            wallClip ??= CreateFeedbackClip("PistolWhipWall", 0.06f, 260f, 0.12f, 0.035f);
            if (aimCamera == null)
            {
                return;
            }

            weaponBlockout ??= aimCamera.transform.Find("WeaponBlockout");
            if (weaponBlockout != null && weaponBlockoutRestPosition == Vector3.zero)
            {
                weaponBlockoutRestPosition = weaponBlockout.localPosition;
                weaponBlockoutRestEuler = weaponBlockout.localEulerAngles;
            }
        }

        private void PlayWhipFeedback(bool hitApplied, RaycastHit hit)
        {
            EnsureFeedbackObjects();
            feedbackHit = hitApplied;
            feedbackEndTime = Time.unscaledTime + 0.16f;
            if (whipAudioSource != null)
            {
                whipAudioSource.PlayOneShot(hitApplied ? hitClip : (hit.collider != null ? wallClip : swingClip), 1f);
            }

            playerController?.ApplyLookRecoil(hitApplied ? 0.8f : 0.35f, hitApplied ? 0.45f : 0.2f, 0.12f);
            ShowWhipMarker(hitApplied, hit);
        }

        private void TickFeedback()
        {
            if (weaponBlockout == null)
            {
                return;
            }

            float remaining = feedbackEndTime - Time.unscaledTime;
            if (remaining <= 0f)
            {
                weaponBlockout.localPosition = Vector3.Lerp(weaponBlockout.localPosition, weaponBlockoutRestPosition, Time.unscaledDeltaTime * 18f);
                weaponBlockout.localEulerAngles = Vector3.Lerp(weaponBlockout.localEulerAngles, weaponBlockoutRestEuler, Time.unscaledDeltaTime * 18f);
                return;
            }

            float t = 1f - Mathf.Clamp01(remaining / 0.16f);
            float punch = Mathf.Sin(t * Mathf.PI);
            weaponBlockout.localPosition = weaponBlockoutRestPosition + new Vector3(-0.08f, -0.02f, 0.18f) * punch;
            weaponBlockout.localEulerAngles = weaponBlockoutRestEuler + new Vector3(10f, -12f, feedbackHit ? -20f : -12f) * punch;
        }

        private static void ShowWhipMarker(bool hitApplied, RaycastHit hit)
        {
            if (hit.collider == null)
            {
                return;
            }

            Transform pool = PlayerWeaponController.GetOrCreateFeedbackPool("PistolWhipImpactPool");
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = hitApplied ? "PistolWhipHit" : "PistolWhipThud";
            marker.transform.SetParent(pool, true);
            marker.transform.position = hit.point;
            marker.transform.localScale = Vector3.one * (hitApplied ? 0.22f : 0.14f);
            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard"))
                {
                    color = hitApplied ? new Color(1f, 0.82f, 0.3f, 1f) : new Color(0.55f, 0.55f, 0.5f, 1f)
                };
            }

            Destroy(marker, 0.18f);
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

        private static bool IsInDungeonRuntime()
        {
            return SceneManager.GetActiveScene().name == GameSceneId.DungeonRuntime.ToString();
        }
    }
}
