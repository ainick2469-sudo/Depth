using UnityEngine;

namespace FrontierDepths.Combat
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class SimpleMeleeEnemyController : MonoBehaviour
    {
        private static readonly Color IdleColor = new Color(0.72f, 0.28f, 0.22f, 1f);
        private static readonly Color ChaseColor = new Color(0.92f, 0.42f, 0.18f, 1f);
        private static readonly Color AttackColor = new Color(1f, 0.12f, 0.08f, 1f);

        [SerializeField] private float moveSpeed = 4.6f;
        [SerializeField] private float detectionRange = 45f;
        [SerializeField] private float attackRange = 2.2f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private bool requireLineOfSightToAttack = true;
        [SerializeField] private LayerMask lineOfSightMask = -1;

        private CharacterController controller;
        private EnemyHealth health;
        private PlayerHealth target;
        private float nextAttackTime;
        private float verticalVelocity;
        private float nextTargetResolveTime;
        private SimpleMeleeEnemyState state;
        private bool subscribedToHealth;

        public SimpleMeleeEnemyState State => state;
        public float NextAttackTime => nextAttackTime;

        private void Awake()
        {
            EnsureComponents();
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        private void Update()
        {
            EnsureComponents();
            if (health == null || health.IsDead)
            {
                SetState(SimpleMeleeEnemyState.Dead);
                return;
            }

            ResolveTarget();
            if (target == null || target.IsDead)
            {
                SetState(SimpleMeleeEnemyState.Idle);
                ApplyGravityOnly();
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance > detectionRange)
            {
                SetState(SimpleMeleeEnemyState.Idle);
                ApplyGravityOnly();
                return;
            }

            if (distance <= attackRange && HasLineOfSightTo(target))
            {
                SetState(SimpleMeleeEnemyState.Attack);
                TryApplyAttack(target, Time.time);
                ApplyGravityOnly();
                return;
            }

            SetState(SimpleMeleeEnemyState.Chase);
            MoveToward(toTarget);
        }

        internal bool TryApplyAttack(PlayerHealth playerHealth, float currentTime)
        {
            EnsureComponents();
            if (!CanAttack(playerHealth, currentTime))
            {
                return false;
            }

            nextAttackTime = currentTime + Mathf.Max(0.05f, attackCooldown);
            health?.Flash(AttackColor, 0.16f);
            DamageInfo damageInfo = new DamageInfo
            {
                amount = attackDamage,
                source = gameObject,
                hitPoint = playerHealth.transform.position,
                hitNormal = (playerHealth.transform.position - transform.position).normalized,
                damageType = DamageType.Physical,
                deliveryType = DamageDeliveryType.Melee,
                tags = new[] { GameplayTag.Melee, GameplayTag.OnHit }
            };

            DamageResult result = playerHealth.ApplyDamage(damageInfo, currentTime);
            return result.applied;
        }

        internal bool CanAttack(PlayerHealth playerHealth, float currentTime)
        {
            EnsureComponents();
            if (health == null || health.IsDead || playerHealth == null || playerHealth.IsDead)
            {
                return false;
            }

            Vector3 toTarget = playerHealth.transform.position - transform.position;
            toTarget.y = 0f;
            return toTarget.magnitude <= attackRange && currentTime >= nextAttackTime && HasLineOfSightTo(playerHealth);
        }

        private void EnsureComponents()
        {
            controller ??= GetComponent<CharacterController>();
            health ??= GetComponent<EnemyHealth>();
            if (health != null && !subscribedToHealth)
            {
                health.Died += HandleDied;
                health.SetStateColor(IdleColor);
                subscribedToHealth = true;
            }

            if (lineOfSightMask.value == 0 || lineOfSightMask.value == -1)
            {
                lineOfSightMask = PlayerWeaponController.DefaultWeaponRaycastMask;
            }
        }

        private void ResolveTarget()
        {
            if (target != null || Time.unscaledTime < nextTargetResolveTime)
            {
                return;
            }

            target = FindAnyObjectByType<PlayerHealth>();
            nextTargetResolveTime = Time.unscaledTime + 0.35f;
        }

        private void MoveToward(Vector3 planarDirection)
        {
            if (controller == null)
            {
                return;
            }

            Vector3 direction = planarDirection.sqrMagnitude > 0.001f ? planarDirection.normalized : Vector3.zero;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 velocity = direction * moveSpeed + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        private void ApplyGravityOnly()
        {
            if (controller == null)
            {
                return;
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        private bool HasLineOfSightTo(PlayerHealth playerHealth)
        {
            if (!requireLineOfSightToAttack || playerHealth == null)
            {
                return true;
            }

            Vector3 start = transform.position + Vector3.up * 0.65f;
            Vector3 end = playerHealth.transform.position + Vector3.up * 0.65f;
            Vector3 direction = end - start;
            float distance = direction.magnitude;
            if (distance <= 0.01f)
            {
                return true;
            }

            RaycastHit[] hits = Physics.RaycastAll(start, direction.normalized, distance, lineOfSightMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (hitCollider.GetComponentInParent<PlayerHealth>() == playerHealth)
                {
                    return true;
                }

                return false;
            }

            return true;
        }

        private void SetState(SimpleMeleeEnemyState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            if (health == null)
            {
                return;
            }

            switch (state)
            {
                case SimpleMeleeEnemyState.Chase:
                    health.SetStateColor(ChaseColor);
                    break;
                case SimpleMeleeEnemyState.Attack:
                    health.SetStateColor(AttackColor);
                    break;
                case SimpleMeleeEnemyState.Dead:
                    break;
                default:
                    health.SetStateColor(IdleColor);
                    break;
            }
        }

        private void HandleDied(EnemyHealth enemyHealth)
        {
            SetState(SimpleMeleeEnemyState.Dead);
            if (controller != null)
            {
                controller.enabled = false;
            }
        }
    }

    public enum SimpleMeleeEnemyState
    {
        Idle,
        Chase,
        Attack,
        Dead
    }
}
