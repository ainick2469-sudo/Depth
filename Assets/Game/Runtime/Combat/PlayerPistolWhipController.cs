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
        private float nextWhipTime;

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

            if (Input.GetKeyDown(KeyCode.V) || Input.GetMouseButtonDown(2))
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
            return TryResolveHit(out RaycastHit hit, out IDamageable damageable) &&
                   ApplyWhipDamage(hit, damageable);
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
            if (hitMask.value == 0 || hitMask.value == -1)
            {
                hitMask = PlayerWeaponController.DefaultWeaponRaycastMask;
            }
        }

        private static bool IsInDungeonRuntime()
        {
            return SceneManager.GetActiveScene().name == GameSceneId.DungeonRuntime.ToString();
        }
    }
}
