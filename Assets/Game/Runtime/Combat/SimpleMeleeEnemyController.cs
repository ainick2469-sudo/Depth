using System.Collections.Generic;
using FrontierDepths.Core;
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
        private const float LastKnownPositionStopDistance = 1.2f;
        private static readonly List<SimpleMeleeEnemyController> ActiveEnemies = new List<SimpleMeleeEnemyController>();

        [SerializeField] private float moveSpeed = 4.6f;
        [SerializeField] private float detectionRange = 45f;
        [SerializeField] private float attackRange = 2.2f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private float alertMemoryDuration = 8f;
        [SerializeField] private float groupAlertRadius = 30f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private bool requireLineOfSightToAttack = true;
        [SerializeField] private LayerMask lineOfSightMask = -1;

        private CharacterController controller;
        private EnemyHealth health;
        private PlayerHealth target;
        private float nextAttackTime;
        private float verticalVelocity;
        private float nextTargetResolveTime;
        private float alertUntilTime;
        private Vector3 lastKnownTargetPosition;
        private SimpleMeleeEnemyState state;
        private bool subscribedToHealth;
        private bool subscribedToGameplayEvents;
        private bool registeredActive;
        private bool hasLastKnownTargetPosition;

        public SimpleMeleeEnemyState State => state;
        public float NextAttackTime => nextAttackTime;
        public bool IsAlerted => IsAlertedAt(Time.time);

        private void Awake()
        {
            EnsureComponents();
        }

        private void OnEnable()
        {
            RegisterActiveEnemy();
            SubscribeGameplayEvents();
            EnsureComponents();
        }

        private void OnDisable()
        {
            UnregisterActiveEnemy();
            UnsubscribeGameplayEvents();
            UnsubscribeHealthEvents();
        }

        private void OnDestroy()
        {
            OnDisable();
        }

        private void Update()
        {
            float currentTime = Time.time;
            EnsureComponents();
            if (health == null || health.IsDead)
            {
                SetState(SimpleMeleeEnemyState.Dead);
                return;
            }

            ResolveTarget();
            if (target == null || target.IsDead)
            {
                if (IsAlertedAt(currentTime) && hasLastKnownTargetPosition)
                {
                    Vector3 toLastKnown = lastKnownTargetPosition - transform.position;
                    toLastKnown.y = 0f;
                    if (toLastKnown.magnitude > LastKnownPositionStopDistance)
                    {
                        SetState(SimpleMeleeEnemyState.Chase);
                        MoveToward(toLastKnown);
                        return;
                    }
                }

                SetState(SimpleMeleeEnemyState.Idle);
                ApplyGravityOnly();
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            bool alerted = IsAlertedAt(currentTime);
            if (!alerted)
            {
                lastKnownTargetPosition = target.transform.position;
                hasLastKnownTargetPosition = true;
            }

            if (distance > detectionRange && !alerted)
            {
                SetState(SimpleMeleeEnemyState.Idle);
                ApplyGravityOnly();
                return;
            }

            if (distance <= attackRange && HasLineOfSightTo(target))
            {
                SetState(SimpleMeleeEnemyState.Attack);
                TryApplyAttack(target, currentTime);
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

        internal bool IsAlertedAt(float currentTime)
        {
            return currentTime <= alertUntilTime;
        }

        internal void Alert(PlayerHealth playerHealth, Vector3 knownPosition, float currentTime)
        {
            EnsureComponents();
            if (health == null || health.IsDead)
            {
                return;
            }

            target = playerHealth != null && !playerHealth.IsDead ? playerHealth : target;
            lastKnownTargetPosition = target != null ? target.transform.position : knownPosition;
            hasLastKnownTargetPosition = true;
            alertUntilTime = Mathf.Max(alertUntilTime, currentTime + Mathf.Max(0.1f, alertMemoryDuration));
            SetState(SimpleMeleeEnemyState.Chase);
        }

        internal void HandleDamagedForTests(DamageInfo damageInfo, DamageResult result, float currentTime)
        {
            HandleDamageAggro(damageInfo, result, currentTime);
        }

        internal bool HandleWeaponFiredForTests(GameplayEvent gameplayEvent, float currentTime)
        {
            return HandleWeaponFired(gameplayEvent, currentTime);
        }

        private void EnsureComponents()
        {
            controller ??= GetComponent<CharacterController>();
            health ??= GetComponent<EnemyHealth>();
            if (isActiveAndEnabled && (health == null || !health.IsDead))
            {
                RegisterActiveEnemy();
                SubscribeGameplayEvents();
            }

            if (health != null && !subscribedToHealth && !health.IsDead)
            {
                health.Died += HandleDied;
                health.Damaged += HandleDamaged;
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

        private void HandleDamaged(EnemyHealth enemyHealth, DamageInfo damageInfo, DamageResult result)
        {
            HandleDamageAggro(damageInfo, result, Time.time);
        }

        private void HandleDamageAggro(DamageInfo damageInfo, DamageResult result, float currentTime)
        {
            EnsureComponents();
            if (!result.applied || health == null || health.IsDead)
            {
                return;
            }

            PlayerHealth sourcePlayer = ResolvePlayerFromSource(damageInfo.source);
            Vector3 knownPosition = sourcePlayer != null
                ? sourcePlayer.transform.position
                : (damageInfo.source != null ? damageInfo.source.transform.position : damageInfo.hitPoint);

            if (!result.killedTarget)
            {
                Alert(sourcePlayer, knownPosition, currentTime);
            }

            AlertNearbyAllies(sourcePlayer, knownPosition, currentTime, this);
        }

        private void HandleGameplayEvent(GameplayEvent gameplayEvent)
        {
            if (gameplayEvent.eventType == GameplayEventType.WeaponFired)
            {
                HandleWeaponFired(gameplayEvent, Time.time);
            }
        }

        private bool HandleWeaponFired(GameplayEvent gameplayEvent, float currentTime)
        {
            EnsureComponents();
            if (health == null || health.IsDead || !IsEventOnCurrentFloor(gameplayEvent))
            {
                return false;
            }

            float radius = Mathf.Max(0f, gameplayEvent.radius);
            if (radius <= 0f)
            {
                return false;
            }

            Vector3 eventPosition = gameplayEvent.worldPosition;
            if ((transform.position - eventPosition).sqrMagnitude > radius * radius)
            {
                return false;
            }

            PlayerHealth sourcePlayer = ResolvePlayerFromSource(gameplayEvent.sourceObject);
            Alert(sourcePlayer, eventPosition, currentTime);
            return true;
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
            target = null;
            alertUntilTime = 0f;
            hasLastKnownTargetPosition = false;
            SetState(SimpleMeleeEnemyState.Dead);
            UnregisterActiveEnemy();
            UnsubscribeGameplayEvents();
            UnsubscribeHealthEvents();
            if (controller != null)
            {
                controller.enabled = false;
            }
        }

        private static PlayerHealth ResolvePlayerFromSource(GameObject source)
        {
            return source != null ? source.GetComponentInParent<PlayerHealth>() : null;
        }

        private static void AlertNearbyAllies(
            PlayerHealth sourcePlayer,
            Vector3 knownPosition,
            float currentTime,
            SimpleMeleeEnemyController sourceEnemy)
        {
            SimpleMeleeEnemyController[] sceneEnemies = FindObjectsByType<SimpleMeleeEnemyController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < sceneEnemies.Length; i++)
            {
                if (sceneEnemies[i] != null)
                {
                    sceneEnemies[i].EnsureComponents();
                }
            }

            List<SimpleMeleeEnemyController> snapshot = new List<SimpleMeleeEnemyController>(ActiveEnemies);
            for (int i = snapshot.Count - 1; i >= 0; i--)
            {
                SimpleMeleeEnemyController enemy = snapshot[i];
                if (enemy == null || enemy == sourceEnemy || enemy.health == null || enemy.health.IsDead)
                {
                    continue;
                }

                if ((enemy.transform.position - knownPosition).sqrMagnitude > enemy.groupAlertRadius * enemy.groupAlertRadius)
                {
                    continue;
                }

                enemy.Alert(sourcePlayer, knownPosition, currentTime);
            }
        }

        private void RegisterActiveEnemy()
        {
            if (!registeredActive)
            {
                ActiveEnemies.Add(this);
                registeredActive = true;
            }
        }

        private void UnregisterActiveEnemy()
        {
            if (registeredActive)
            {
                ActiveEnemies.Remove(this);
                registeredActive = false;
            }
        }

        private void SubscribeGameplayEvents()
        {
            if (!subscribedToGameplayEvents)
            {
                GameplayEventBus.Subscribe(HandleGameplayEvent);
                subscribedToGameplayEvents = true;
            }
        }

        private void UnsubscribeGameplayEvents()
        {
            if (subscribedToGameplayEvents)
            {
                GameplayEventBus.Unsubscribe(HandleGameplayEvent);
                subscribedToGameplayEvents = false;
            }
        }

        private void UnsubscribeHealthEvents()
        {
            if (health != null && subscribedToHealth)
            {
                health.Died -= HandleDied;
                health.Damaged -= HandleDamaged;
                subscribedToHealth = false;
            }
        }

        private static bool IsEventOnCurrentFloor(GameplayEvent gameplayEvent)
        {
            if (gameplayEvent.floorIndex <= 0)
            {
                return true;
            }

            RunState run = GameBootstrap.Instance != null && GameBootstrap.Instance.RunService != null
                ? GameBootstrap.Instance.RunService.Current
                : null;
            return run == null || run.floorIndex <= 0 || run.floorIndex == gameplayEvent.floorIndex;
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
