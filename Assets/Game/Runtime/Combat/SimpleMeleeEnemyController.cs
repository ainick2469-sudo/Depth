using System;
using System.Collections.Generic;
using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.Combat
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class SimpleMeleeEnemyController : MonoBehaviour
    {
        private static readonly Color AttackColor = new Color(1f, 0.12f, 0.08f, 1f);
        private static readonly Color InvestigateColor = new Color(1f, 0.72f, 0.18f, 1f);
        private static readonly Color ReturnColor = new Color(0.58f, 0.68f, 1f, 1f);
        private const float LastKnownPositionStopDistance = 1.2f;
        private const float AttackRangeForgiveness = 0.35f;
        private const float PatrolTargetStopDistance = 0.8f;
        private const float HomeBoundsInset = 3f;
        private const float PerceptionIntervalIdle = 0.18f;
        private const float PerceptionIntervalChase = 0.08f;
        private const float StuckProgressEpsilon = 0.035f;
        private const float DoorwayEdgeInset = 4f;
        private const float EnemySeparationRadius = 2.2f;
        private const float EnemySeparationStrength = 0.62f;
        private const float SlimeWanderMinRadius = 2.4f;
        private const float SlimeWanderMaxRadius = 7.2f;
        private static readonly List<SimpleMeleeEnemyController> ActiveEnemies = new List<SimpleMeleeEnemyController>();

        [SerializeField] private float moveSpeed = 4.6f;
        [SerializeField] private float detectionRange = 45f;
        [SerializeField] private float attackRange = 2.2f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private float attackWindupDuration = 0.25f;
        [SerializeField] private float alertMemoryDuration = 8f;
        [SerializeField] private float groupAlertRadius = 30f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private bool requireLineOfSightToAttack = true;
        [SerializeField] private LayerMask lineOfSightMask = -1;
        [SerializeField] private EnemyAmbientBehavior ambientBehavior = EnemyAmbientBehavior.Idle;
        [SerializeField] private EnemyMobilityRole mobilityRole = EnemyMobilityRole.RoomGuard;
        [SerializeField] private EnemyAlertLevel alertLevel = EnemyAlertLevel.Passive;
        [SerializeField] private float visionConeAngle = 120f;
        [SerializeField] private float idleMoveSpeedMultiplier = 0.55f;
        [SerializeField] private float patrolSpeedMultiplier = 0.55f;
        [SerializeField] private float investigateSpeedMultiplier = 0.85f;
        [SerializeField] private float chaseSpeedMultiplier = 1f;
        [SerializeField] private float returnHomeSpeedMultiplier = 0.75f;
        [SerializeField] private float patrolWaitSeconds = 1.1f;
        [SerializeField] private float investigateDuration = 4f;
        [SerializeField] private float lostSightGraceDuration = 1f;
        [SerializeField] private float searchDuration = 3f;
        [SerializeField] private float homeReturnStopDistance = 1.1f;
        [SerializeField] private float stuckRecoverySeconds = 1.4f;
        [SerializeField] private bool debugStateLabelsVisible;
        [SerializeField] private bool enableDebugStuckSnapRecovery;

        private readonly List<Vector3> patrolPoints = new List<Vector3>();
        private readonly List<Vector3> roamingRoutePoints = new List<Vector3>();
        private CharacterController controller;
        private EnemyHealth health;
        private PlayerHealth target;
        private float nextAttackTime;
        private float attackWindupCompleteTime;
        private float verticalVelocity;
        private float nextTargetResolveTime;
        private float nextPerceptionTime;
        private float alertUntilTime;
        private float lastSeenTargetTime = -999f;
        private float investigateUntilTime;
        private float patrolWaitUntilTime;
        private float stuckTimer;
        private Vector3 lastKnownTargetPosition;
        private Vector3 lastHeardPosition;
        private Vector3 currentMoveTarget;
        private Vector3 lastMoveSamplePosition;
        private Bounds homeRoomBounds;
        private SimpleMeleeEnemyState state = SimpleMeleeEnemyState.Idle;
        private bool subscribedToHealth;
        private bool subscribedToGameplayEvents;
        private bool registeredActive;
        private bool hasLastKnownTargetPosition;
        private bool hasLastHeardPosition;
        private bool hasMoveTarget;
        private bool hasHomeRoom;
        private bool isAttackWindingUp;
        private Color baseBodyColor = new Color(0.72f, 0.28f, 0.22f, 1f);
        private float hearingRadiusMultiplier = 1f;
        private EnemyArchetype archetype = EnemyArchetype.GoblinGrunt;
        private PlayerHealth windupTarget;
        private int patrolCursor;
        private int roamingCursor;
        private int stuckRecoveryCount;
        private int behaviorSeed;
        private int behaviorPickCounter;
        private int roamingDirection = 1;
        private float behaviorPhase01;
        private float patrolWaitJitter = 1f;
        private float slimeWanderRadius = 4f;
        private float batBobPhase;

        public SimpleMeleeEnemyState State => state;
        public float NextAttackTime => nextAttackTime;
        public bool IsAlerted => IsAlertedAt(Time.time);
        public float MoveSpeed => moveSpeed;
        public float AttackDamage => attackDamage;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public float AttackWindupDuration => attackWindupDuration;
        public bool IsAttackWindingUp => isAttackWindingUp;
        public float AttackWindupCompleteTime => attackWindupCompleteTime;
        public float DetectionRange => detectionRange;
        public float HearingRadiusMultiplier => hearingRadiusMultiplier;
        public float GroupAlertRadius => groupAlertRadius;
        public EnemyAmbientBehavior AmbientBehavior => ambientBehavior;
        public EnemyMobilityRole MobilityRole => mobilityRole;
        public EnemyAlertLevel AlertLevel => alertLevel;
        public float PatrolSpeedMultiplier => patrolSpeedMultiplier;
        public float InvestigateSpeedMultiplier => investigateSpeedMultiplier;
        public float ChaseSpeedMultiplier => chaseSpeedMultiplier;
        public float ReturnHomeSpeedMultiplier => returnHomeSpeedMultiplier;
        public string HomeRoomId { get; private set; } = string.Empty;
        public Bounds HomeRoomBounds => homeRoomBounds;
        public bool HasHomeRoom => hasHomeRoom;
        public Vector3 LastKnownTargetPosition => lastKnownTargetPosition;
        public Vector3 LastHeardPosition => lastHeardPosition;
        public bool IsInvestigating => state == SimpleMeleeEnemyState.Investigate;
        public int StuckRecoveryCount => stuckRecoveryCount;
        public int BehaviorSeed => behaviorSeed;
        public float BehaviorPhase01 => behaviorPhase01;
        public float BatBobPhase => batBobPhase;
        public float SlimeWanderRadius => slimeWanderRadius;

        public void Configure(EnemyDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            moveSpeed = Mathf.Max(0.1f, definition.moveSpeed);
            detectionRange = Mathf.Max(0f, definition.detectionRange);
            attackRange = Mathf.Max(0.1f, definition.attackRange);
            attackDamage = Mathf.Max(0f, definition.attackDamage);
            attackCooldown = Mathf.Max(0.05f, definition.attackCooldown);
            attackWindupDuration = Mathf.Max(0f, definition.attackWindupDuration);
            hearingRadiusMultiplier = Mathf.Max(0f, definition.hearingRadiusMultiplier);
            groupAlertRadius = Mathf.Max(0f, definition.groupAlertRadius);
            ambientBehavior = definition.ambientBehavior;
            mobilityRole = definition.defaultMobilityRole;
            visionConeAngle = Mathf.Clamp(definition.visionConeAngle, 1f, 360f);
            idleMoveSpeedMultiplier = Mathf.Clamp(definition.idleMoveSpeedMultiplier, 0.05f, 1f);
            patrolSpeedMultiplier = Mathf.Clamp(definition.patrolSpeedMultiplier > 0f ? definition.patrolSpeedMultiplier : idleMoveSpeedMultiplier, 0.05f, 2f);
            investigateSpeedMultiplier = Mathf.Clamp(definition.investigateSpeedMultiplier, 0.05f, 2f);
            chaseSpeedMultiplier = Mathf.Clamp(definition.chaseSpeedMultiplier, 0.05f, 2f);
            returnHomeSpeedMultiplier = Mathf.Clamp(definition.returnHomeSpeedMultiplier, 0.05f, 2f);
            patrolWaitSeconds = Mathf.Max(0f, definition.patrolWaitSeconds);
            investigateDuration = Mathf.Max(0.1f, definition.investigateDuration);
            lostSightGraceDuration = Mathf.Max(0f, definition.lostSightGraceDuration);
            searchDuration = Mathf.Max(0.1f, definition.searchDuration);
            homeReturnStopDistance = Mathf.Max(0.2f, definition.homeReturnStopDistance);
            stuckRecoverySeconds = Mathf.Max(0.25f, definition.stuckRecoverySeconds);
            baseBodyColor = definition.bodyColor;
            archetype = definition.archetype;
            ConfigureBehaviorSeed(behaviorSeed != 0 ? behaviorSeed : gameObject.GetInstanceID());

            if (!IsCombatState(state) && state != SimpleMeleeEnemyState.Dead)
            {
                SetState(GetAmbientState());
            }

            ApplyStateColor(state);
        }

        public void ConfigureHomeRoom(string roomId, Bounds bounds, IReadOnlyList<Vector3> roomPatrolPoints)
        {
            HomeRoomId = roomId ?? string.Empty;
            homeRoomBounds = bounds;
            hasHomeRoom = bounds.size.sqrMagnitude > 0.01f;
            patrolPoints.Clear();

            if (hasHomeRoom)
            {
                AddPatrolPoint(bounds.center);
                if (roomPatrolPoints != null)
                {
                    for (int i = 0; i < roomPatrolPoints.Count; i++)
                    {
                        AddPatrolPoint(roomPatrolPoints[i]);
                    }
                }

                AddPatrolPoint(new Vector3(bounds.min.x + HomeBoundsInset, bounds.center.y, bounds.min.z + HomeBoundsInset));
                AddPatrolPoint(new Vector3(bounds.max.x - HomeBoundsInset, bounds.center.y, bounds.min.z + HomeBoundsInset));
                AddPatrolPoint(new Vector3(bounds.min.x + HomeBoundsInset, bounds.center.y, bounds.max.z - HomeBoundsInset));
                AddPatrolPoint(new Vector3(bounds.max.x - HomeBoundsInset, bounds.center.y, bounds.max.z - HomeBoundsInset));
            }
            else
            {
                AddPatrolPoint(transform.position);
            }

            patrolCursor = Mathf.Clamp(patrolCursor, 0, Mathf.Max(0, patrolPoints.Count - 1));
            ConfigureBehaviorSeed(behaviorSeed != 0 ? behaviorSeed : gameObject.GetInstanceID());
            hasMoveTarget = false;
        }

        public void ConfigureMobilityRole(EnemyMobilityRole role)
        {
            mobilityRole = role;
            if (role == EnemyMobilityRole.Sleeper)
            {
                ambientBehavior = EnemyAmbientBehavior.SleepGuard;
            }
        }

        public void ConfigureRoamingRoute(IReadOnlyList<Vector3> routePoints)
        {
            roamingRoutePoints.Clear();
            if (routePoints != null)
            {
                for (int i = 0; i < routePoints.Count; i++)
                {
                    Vector3 point = routePoints[i];
                    if (roamingRoutePoints.Count == 0 ||
                        (Planar(roamingRoutePoints[roamingRoutePoints.Count - 1]) - Planar(point)).sqrMagnitude > 1f)
                    {
                        roamingRoutePoints.Add(new Vector3(point.x, transform.position.y, point.z));
                    }
                }
            }

            roamingCursor = Mathf.Clamp(roamingCursor, 0, Mathf.Max(0, roamingRoutePoints.Count - 1));
            ConfigureBehaviorSeed(behaviorSeed != 0 ? behaviorSeed : gameObject.GetInstanceID());
            if (CanRoamPassively() && roamingRoutePoints.Count > 0)
            {
                hasMoveTarget = false;
                SetState(SimpleMeleeEnemyState.Patrol);
            }
        }

        public void ConfigureBehaviorSeed(int seed)
        {
            behaviorSeed = seed == 0 ? gameObject.GetInstanceID() : seed;
            behaviorPickCounter = 0;
            behaviorPhase01 = Seeded01(17);
            patrolWaitJitter = Mathf.Lerp(0.55f, 1.45f, Seeded01(23));
            slimeWanderRadius = Mathf.Lerp(SlimeWanderMinRadius, SlimeWanderMaxRadius, Seeded01(29));
            batBobPhase = Seeded01(31) * Mathf.PI * 2f;
            roamingDirection = Seeded01(37) < 0.5f ? -1 : 1;
            if (patrolPoints.Count > 0)
            {
                patrolCursor = Mathf.Clamp(Mathf.FloorToInt(Seeded01(41) * patrolPoints.Count), 0, patrolPoints.Count - 1);
            }

            if (roamingRoutePoints.Count > 0)
            {
                roamingCursor = Mathf.Clamp(Mathf.FloorToInt(Seeded01(43) * roamingRoutePoints.Count), 0, roamingRoutePoints.Count - 1);
            }

            patrolWaitUntilTime = Time.time + GetPatrolWaitDuration();
        }

        internal void SetHomeRoomForTests(string roomId, Bounds bounds, IReadOnlyList<Vector3> roomPatrolPoints)
        {
            ConfigureHomeRoom(roomId, bounds, roomPatrolPoints);
        }

        internal IReadOnlyList<Vector3> GetPatrolPointsForTests()
        {
            return new List<Vector3>(patrolPoints);
        }

        internal IReadOnlyList<Vector3> GetRoamingRouteForTests()
        {
            return new List<Vector3>(roamingRoutePoints);
        }

        internal void ConfigureBehaviorSeedForTests(int seed)
        {
            ConfigureBehaviorSeed(seed);
        }

        internal Vector3 ChooseNextPatrolTargetForTests()
        {
            ChooseNextPatrolTarget();
            return currentMoveTarget;
        }

        internal float GetEffectiveMoveSpeedForTests(SimpleMeleeEnemyState testState)
        {
            return GetSpeedForState(testState);
        }

        internal void TickForTests(float currentTime, float deltaTime)
        {
            patrolWaitUntilTime = Mathf.Min(patrolWaitUntilTime, currentTime);
            Tick(currentTime, Mathf.Max(0f, deltaTime), false);
        }

        internal bool CanSeePlayerForTests(PlayerHealth playerHealth)
        {
            return CanSeePlayer(playerHealth, Time.time, true);
        }

        internal bool IsPointInsideHomeForTests(Vector3 point)
        {
            return IsInsideHomeRoom(point, 0f);
        }

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
            CancelAttackWindup(false);
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
            if (Time.timeScale <= 0f)
            {
                return;
            }

            Tick(Time.time, Time.deltaTime, true);
        }

        private void Tick(float currentTime, float deltaTime, bool resolveTarget)
        {
            EnsureComponents();
            if (health == null || health.IsDead)
            {
                CancelAttackWindup(false);
                SetState(SimpleMeleeEnemyState.Dead);
                return;
            }

            if (TickAttackWindup(currentTime))
            {
                return;
            }

            if (isAttackWindingUp)
            {
                ApplyGravityOnly(deltaTime);
                return;
            }

            if (resolveTarget)
            {
                ResolveTarget();
            }

            TickPerception(currentTime, resolveTarget);

            switch (state)
            {
                case SimpleMeleeEnemyState.Chase:
                    TickChase(currentTime, deltaTime);
                    break;
                case SimpleMeleeEnemyState.Investigate:
                    TickInvestigate(currentTime, deltaTime);
                    break;
                case SimpleMeleeEnemyState.ReturnToRoom:
                    TickReturnToRoom(deltaTime);
                    break;
                case SimpleMeleeEnemyState.Sleep:
                    ApplyGravityOnly(deltaTime);
                    break;
                case SimpleMeleeEnemyState.Patrol:
                    TickPatrol(currentTime, deltaTime);
                    break;
                case SimpleMeleeEnemyState.AttackRecover:
                    ApplyGravityOnly(deltaTime);
                    if (currentTime >= nextAttackTime)
                    {
                        SetState(IsAlertedAt(currentTime) ? SimpleMeleeEnemyState.Chase : GetAmbientState());
                    }
                    break;
                case SimpleMeleeEnemyState.Dead:
                    break;
                default:
                    TickIdle(deltaTime);
                    break;
            }
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
            DamageInfo damageInfo = CreateMeleeDamageInfo(playerHealth);
            DamageResult result = playerHealth.ApplyDamage(damageInfo, currentTime);
            SetState(SimpleMeleeEnemyState.AttackRecover);
            return result.applied;
        }

        internal bool TryStartAttackWindup(PlayerHealth playerHealth, float currentTime)
        {
            EnsureComponents();
            if (!CanAttack(playerHealth, currentTime))
            {
                return false;
            }

            nextAttackTime = currentTime + Mathf.Max(0.05f, attackCooldown);
            if (attackWindupDuration <= 0.001f)
            {
                return ApplyWindupDamage(playerHealth, currentTime);
            }

            windupTarget = playerHealth;
            isAttackWindingUp = true;
            attackWindupCompleteTime = currentTime + attackWindupDuration;
            SetState(SimpleMeleeEnemyState.AttackWindup);
            health?.Flash(GetWindupColor(), Mathf.Max(0.12f, Mathf.Min(attackWindupDuration, 0.35f)));
            return true;
        }

        internal bool TickAttackWindup(float currentTime)
        {
            if (!isAttackWindingUp)
            {
                return false;
            }

            if (health == null || health.IsDead || windupTarget == null || windupTarget.IsDead)
            {
                CancelAttackWindup(false);
                return false;
            }

            if (!IsTargetInAttackRange(windupTarget, AttackRangeForgiveness) || !HasLineOfSightTo(windupTarget))
            {
                CancelAttackWindup(true);
                return false;
            }

            if (currentTime < attackWindupCompleteTime)
            {
                return false;
            }

            PlayerHealth resolvedTarget = windupTarget;
            CancelAttackWindup(false);
            return ApplyWindupDamage(resolvedTarget, currentTime);
        }

        internal bool CanAttack(PlayerHealth playerHealth, float currentTime)
        {
            EnsureComponents();
            if (isAttackWindingUp || health == null || health.IsDead || playerHealth == null || playerHealth.IsDead)
            {
                return false;
            }

            return IsTargetInAttackRange(playerHealth, 0f) && currentTime >= nextAttackTime && HasLineOfSightTo(playerHealth);
        }

        internal bool IsAlertedAt(float currentTime)
        {
            return alertUntilTime > 0f && currentTime <= alertUntilTime;
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
            lastSeenTargetTime = currentTime;
            alertUntilTime = Mathf.Max(alertUntilTime, currentTime + Mathf.Max(0.1f, alertMemoryDuration));
            hasMoveTarget = false;
            if (CanEnterActiveCombat())
            {
                SetState(SimpleMeleeEnemyState.Chase);
            }
            else
            {
                StartInvestigatingPosition(lastKnownTargetPosition, currentTime, Mathf.Max(searchDuration, investigateDuration));
            }
        }

        internal void HandleDamagedForTests(DamageInfo damageInfo, DamageResult result, float currentTime)
        {
            HandleDamageAggro(damageInfo, result, currentTime);
        }

        internal bool HandleWeaponFiredForTests(GameplayEvent gameplayEvent, float currentTime)
        {
            return HandleWeaponFired(gameplayEvent, currentTime);
        }

        internal static Dictionary<SimpleMeleeEnemyState, int> GetActiveStateCounts()
        {
            Dictionary<SimpleMeleeEnemyState, int> counts = new Dictionary<SimpleMeleeEnemyState, int>();
            List<SimpleMeleeEnemyController> snapshot = new List<SimpleMeleeEnemyController>(ActiveEnemies);
            for (int i = snapshot.Count - 1; i >= 0; i--)
            {
                SimpleMeleeEnemyController enemy = snapshot[i];
                if (enemy == null || enemy.health == null || enemy.health.IsDead)
                {
                    continue;
                }

                counts.TryGetValue(enemy.state, out int count);
                counts[enemy.state] = count + 1;
            }

            return counts;
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
                if (health.Definition != null)
                {
                    Configure(health.Definition);
                }
                else
                {
                    health.SetStateColor(GetIdleColor());
                }

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

        private void TickPerception(float currentTime, bool resolveTarget)
        {
            if (health == null || health.IsDead || state == SimpleMeleeEnemyState.Dead || isAttackWindingUp)
            {
                return;
            }

            if (currentTime < nextPerceptionTime)
            {
                return;
            }

            float interval = state == SimpleMeleeEnemyState.Chase || state == SimpleMeleeEnemyState.Investigate
                ? PerceptionIntervalChase
                : PerceptionIntervalIdle;
            nextPerceptionTime = currentTime + interval;

            if (resolveTarget)
            {
                ResolveTarget();
            }

            if (target == null || target.IsDead)
            {
                return;
            }

            bool useCone = state == SimpleMeleeEnemyState.Sleep ||
                           state == SimpleMeleeEnemyState.Idle ||
                           state == SimpleMeleeEnemyState.Patrol;
            if (!CanSeePlayer(target, currentTime, useCone))
            {
                return;
            }

            lastKnownTargetPosition = target.transform.position;
            hasLastKnownTargetPosition = true;
            lastSeenTargetTime = currentTime;
            alertUntilTime = Mathf.Max(alertUntilTime, currentTime + Mathf.Max(0.1f, alertMemoryDuration));
            if (!IsAttackState(state))
            {
                if (CanEnterActiveCombat())
                {
                    SetState(SimpleMeleeEnemyState.Chase);
                }
                else
                {
                    StartInvestigatingPosition(target.transform.position, currentTime, Mathf.Max(searchDuration, investigateDuration));
                }
            }
        }

        private void TickChase(float currentTime, float deltaTime)
        {
            if (target == null || target.IsDead)
            {
                StartSearchOrReturn(currentTime);
                return;
            }

            bool canSee = CanSeePlayer(target, currentTime, false);
            if (canSee)
            {
                lastKnownTargetPosition = target.transform.position;
                hasLastKnownTargetPosition = true;
                lastSeenTargetTime = currentTime;
                alertUntilTime = Mathf.Max(alertUntilTime, currentTime + Mathf.Max(0.1f, alertMemoryDuration));
                if (IsTargetInAttackRange(target, 0f) && HasLineOfSightTo(target))
                {
                    TryStartAttackWindup(target, currentTime);
                    ApplyGravityOnly(deltaTime);
                    return;
                }
            }
            else if (currentTime - lastSeenTargetTime > lostSightGraceDuration)
            {
                StartInvestigatingPosition(lastKnownTargetPosition, currentTime, searchDuration);
                return;
            }

            Vector3 chaseTarget = hasLastKnownTargetPosition ? lastKnownTargetPosition : target.transform.position;
            MoveTowardPoint(chaseTarget, GetSpeedForState(SimpleMeleeEnemyState.Chase), deltaTime, false, true);
        }

        private void TickInvestigate(float currentTime, float deltaTime)
        {
            if (target != null && !target.IsDead && CanSeePlayer(target, currentTime, false))
            {
                Alert(target, target.transform.position, currentTime);
                return;
            }

            if (currentTime >= investigateUntilTime)
            {
                StartReturnToRoom();
                return;
            }

            Vector3 destination = hasMoveTarget ? currentMoveTarget : (hasLastHeardPosition ? lastHeardPosition : lastKnownTargetPosition);
            if (Vector3.Distance(Planar(transform.position), Planar(destination)) <= LastKnownPositionStopDistance)
            {
                ApplyGravityOnly(deltaTime);
                return;
            }

            MoveTowardPoint(destination, GetSpeedForState(SimpleMeleeEnemyState.Investigate), deltaTime, false, true);
        }

        private void TickReturnToRoom(float deltaTime)
        {
            Vector3 destination = GetNearestHomePoint(transform.position);
            if (!hasHomeRoom || Vector3.Distance(Planar(transform.position), Planar(destination)) <= homeReturnStopDistance)
            {
                hasMoveTarget = false;
                SetState(GetAmbientState());
                ApplyGravityOnly(deltaTime);
                return;
            }

            MoveTowardPoint(destination, GetSpeedForState(SimpleMeleeEnemyState.ReturnToRoom), deltaTime, true, true);
        }

        private void TickIdle(float deltaTime)
        {
            if (ambientBehavior == EnemyAmbientBehavior.Wander || ambientBehavior == EnemyAmbientBehavior.Patrol)
            {
                SetState(SimpleMeleeEnemyState.Patrol);
                TickPatrol(Time.time, deltaTime);
                return;
            }

            ApplyGravityOnly(deltaTime);
        }

        private void TickPatrol(float currentTime, float deltaTime)
        {
            if (patrolPoints.Count == 0)
            {
                AddPatrolPoint(hasHomeRoom ? homeRoomBounds.center : transform.position);
            }

            if (currentTime < patrolWaitUntilTime)
            {
                ApplyGravityOnly(deltaTime);
                return;
            }

            bool passiveRoam = CanRoamPassively() && roamingRoutePoints.Count > 0;
            if (!hasMoveTarget || (!passiveRoam && !IsSafeRoomLocalTarget(currentMoveTarget)))
            {
                ChooseNextPatrolTarget();
            }

            if (Vector3.Distance(Planar(transform.position), Planar(currentMoveTarget)) <= PatrolTargetStopDistance)
            {
                patrolWaitUntilTime = currentTime + GetPatrolWaitDuration();
                ChooseNextPatrolTarget();
                ApplyGravityOnly(deltaTime);
                return;
            }

            MoveTowardPoint(currentMoveTarget, GetSpeedForState(SimpleMeleeEnemyState.Patrol), deltaTime, !passiveRoam, true);
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

            float radius = Mathf.Max(0f, gameplayEvent.radius) * Mathf.Max(0f, hearingRadiusMultiplier);
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
            float planarDistance = Vector3.Distance(Planar(transform.position), Planar(eventPosition));
            bool eventInHomeRoom = hasHomeRoom && IsInsideHomeRoom(eventPosition, 0f);
            bool repeatedNearbyGunfire = hasLastHeardPosition &&
                                         Vector3.Distance(Planar(lastHeardPosition), Planar(eventPosition)) <= 8f &&
                                         state == SimpleMeleeEnemyState.Investigate;

            if (sourcePlayer != null && !sourcePlayer.IsDead && CanSeePlayer(sourcePlayer, currentTime, false))
            {
                Alert(sourcePlayer, sourcePlayer.transform.position, currentTime);
                return true;
            }

            float duration = investigateDuration;
            if (eventInHomeRoom || planarDistance <= radius * 0.35f)
            {
                duration = Mathf.Max(duration, searchDuration);
            }

            if (repeatedNearbyGunfire)
            {
                duration += Mathf.Max(1f, investigateDuration * 0.5f);
            }

            StartInvestigatingPosition(eventPosition, currentTime, duration);
            return true;
        }

        private void MoveTowardPoint(Vector3 worldTarget, float speed, float deltaTime, bool restrictToHomeRoom, bool allowStuckRecovery)
        {
            if (controller == null || deltaTime <= 0f)
            {
                return;
            }

            if (restrictToHomeRoom && !IsSafeRoomLocalTarget(worldTarget))
            {
                worldTarget = GetNearestHomePoint(worldTarget);
            }

            Vector3 before = transform.position;
            Vector3 planarDirection = worldTarget - before;
            planarDirection.y = 0f;
            Vector3 direction = planarDirection.sqrMagnitude > 0.001f ? planarDirection.normalized : Vector3.zero;
            Vector3 separationDirection = ComputeEnemySeparationDirection(restrictToHomeRoom);
            if (separationDirection.sqrMagnitude > 0.001f)
            {
                direction = (direction + separationDirection * EnemySeparationStrength).normalized;
            }

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            verticalVelocity += gravity * deltaTime;
            Vector3 velocity = direction * Mathf.Max(0f, speed) + Vector3.up * verticalVelocity;
            controller.Move(velocity * deltaTime);

            if (restrictToHomeRoom && hasHomeRoom && !IsInsideHomeRoom(transform.position, 0f))
            {
                transform.position = ClampToHomeRoom(transform.position, 0f);
                ChooseNextPatrolTarget();
            }

            TrackStuckProgress(before, direction, deltaTime, allowStuckRecovery);
        }

        private void ApplyGravityOnly(float deltaTime)
        {
            if (controller == null || deltaTime <= 0f)
            {
                return;
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            verticalVelocity += gravity * deltaTime;
            controller.Move(Vector3.up * verticalVelocity * deltaTime);
        }

        private bool ApplyWindupDamage(PlayerHealth playerHealth, float currentTime)
        {
            if (health == null || health.IsDead || playerHealth == null || playerHealth.IsDead)
            {
                return false;
            }

            if (!IsTargetInAttackRange(playerHealth, AttackRangeForgiveness) || !HasLineOfSightTo(playerHealth))
            {
                return false;
            }

            health?.Flash(AttackColor, 0.12f);
            DamageInfo damageInfo = CreateMeleeDamageInfo(playerHealth);
            DamageResult result = playerHealth.ApplyDamage(damageInfo, currentTime);
            SetState(SimpleMeleeEnemyState.AttackRecover);
            return result.applied;
        }

        private DamageInfo CreateMeleeDamageInfo(PlayerHealth playerHealth)
        {
            return new DamageInfo
            {
                amount = attackDamage,
                source = gameObject,
                hitPoint = playerHealth.transform.position,
                hitNormal = (playerHealth.transform.position - transform.position).normalized,
                damageType = DamageType.Physical,
                deliveryType = DamageDeliveryType.Melee,
                tags = new[] { GameplayTag.Melee, GameplayTag.OnHit }
            };
        }

        private bool IsTargetInAttackRange(PlayerHealth playerHealth, float forgiveness)
        {
            if (playerHealth == null)
            {
                return false;
            }

            Vector3 toTarget = playerHealth.transform.position - transform.position;
            toTarget.y = 0f;
            return toTarget.magnitude <= attackRange + Mathf.Max(0f, forgiveness);
        }

        private void CancelAttackWindup(bool enterRecover)
        {
            bool wasWindingUp = isAttackWindingUp;
            isAttackWindingUp = false;
            attackWindupCompleteTime = 0f;
            windupTarget = null;
            if (wasWindingUp && enterRecover && state == SimpleMeleeEnemyState.AttackWindup)
            {
                SetState(SimpleMeleeEnemyState.AttackRecover);
            }
        }

        private bool CanSeePlayer(PlayerHealth playerHealth, float currentTime, bool requireCone)
        {
            if (playerHealth == null || playerHealth.IsDead)
            {
                return false;
            }

            Vector3 toPlayer = playerHealth.transform.position - transform.position;
            toPlayer.y = 0f;
            float rangeMultiplier = state == SimpleMeleeEnemyState.Investigate ? 1.12f : 1f;
            if (toPlayer.magnitude > detectionRange * rangeMultiplier)
            {
                return false;
            }

            if (requireCone && !IsInsideVisionCone(toPlayer))
            {
                return false;
            }

            return HasLineOfSightTo(playerHealth);
        }

        private bool IsInsideVisionCone(Vector3 planarDirection)
        {
            if (visionConeAngle >= 359f || planarDirection.sqrMagnitude <= 0.001f)
            {
                return true;
            }

            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.001f)
            {
                return true;
            }

            float angle = Vector3.Angle(forward.normalized, planarDirection.normalized);
            return angle <= visionConeAngle * 0.5f;
        }

        private bool HasLineOfSightTo(PlayerHealth playerHealth)
        {
            if (!requireLineOfSightToAttack || playerHealth == null)
            {
                return true;
            }

            Vector3 start = transform.position + Vector3.up * 0.85f;
            Vector3 end = playerHealth.transform.position + Vector3.up * 0.85f;
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

        private void StartInvestigatingPosition(Vector3 position, float currentTime, float duration)
        {
            if (health == null || health.IsDead)
            {
                return;
            }

            lastHeardPosition = position;
            hasLastHeardPosition = true;
            currentMoveTarget = position;
            hasMoveTarget = true;
            investigateUntilTime = currentTime + Mathf.Max(0.1f, duration);
            alertUntilTime = Mathf.Max(alertUntilTime, currentTime + Mathf.Max(0.1f, alertMemoryDuration));
            SetState(SimpleMeleeEnemyState.Investigate);
        }

        private void StartSearchOrReturn(float currentTime)
        {
            if (hasLastKnownTargetPosition)
            {
                StartInvestigatingPosition(lastKnownTargetPosition, currentTime, searchDuration);
                return;
            }

            StartReturnToRoom();
        }

        private void StartReturnToRoom()
        {
            hasMoveTarget = false;
            target = null;
            hasLastHeardPosition = false;
            alertUntilTime = 0f;
            SetState(hasHomeRoom ? SimpleMeleeEnemyState.ReturnToRoom : GetAmbientState());
        }

        private SimpleMeleeEnemyState GetAmbientState()
        {
            return ambientBehavior switch
            {
                EnemyAmbientBehavior.SleepGuard => SimpleMeleeEnemyState.Sleep,
                EnemyAmbientBehavior.Wander => SimpleMeleeEnemyState.Patrol,
                EnemyAmbientBehavior.Patrol => SimpleMeleeEnemyState.Patrol,
                _ => SimpleMeleeEnemyState.Idle
            };
        }

        private void ChooseNextPatrolTarget()
        {
            if (CanRoamPassively() && roamingRoutePoints.Count > 0)
            {
                roamingCursor = (roamingCursor + roamingDirection + roamingRoutePoints.Count) % roamingRoutePoints.Count;
                currentMoveTarget = roamingRoutePoints[roamingCursor];
                hasMoveTarget = true;
                return;
            }

            if (patrolPoints.Count == 0)
            {
                currentMoveTarget = transform.position;
                hasMoveTarget = true;
                return;
            }

            if (archetype == EnemyArchetype.Slime && TryChooseSlimeWanderTarget(out Vector3 slimeTarget))
            {
                currentMoveTarget = slimeTarget;
                hasMoveTarget = true;
                return;
            }

            int attempts = Mathf.Max(1, patrolPoints.Count);
            for (int i = 0; i < attempts; i++)
            {
                patrolCursor = PickPatrolIndex();
                Vector3 candidate = patrolPoints[patrolCursor];
                if (IsSafeRoomLocalTarget(candidate))
                {
                    currentMoveTarget = candidate;
                    hasMoveTarget = true;
                    return;
                }
            }

            currentMoveTarget = GetNearestHomePoint(transform.position);
            hasMoveTarget = true;
        }

        private int PickPatrolIndex()
        {
            if (patrolPoints.Count <= 1)
            {
                return 0;
            }

            int next = Mathf.Clamp(Mathf.FloorToInt(Seeded01(1000 + behaviorPickCounter++) * patrolPoints.Count), 0, patrolPoints.Count - 1);
            if (next == patrolCursor)
            {
                next = (next + 1) % patrolPoints.Count;
            }

            return next;
        }

        private bool TryChooseSlimeWanderTarget(out Vector3 targetPoint)
        {
            targetPoint = transform.position;
            Vector3 origin = hasHomeRoom ? ClampToHomeRoom(transform.position, DoorwayEdgeInset) : transform.position;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float angle = Seeded01(2000 + behaviorPickCounter++) * Mathf.PI * 2f;
                float radius = Mathf.Lerp(SlimeWanderMinRadius, slimeWanderRadius, Seeded01(3000 + behaviorPickCounter++));
                Vector3 candidate = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                candidate.y = transform.position.y;
                if (IsSafeRoomLocalTarget(candidate))
                {
                    targetPoint = candidate;
                    return true;
                }
            }

            return false;
        }

        private void AddPatrolPoint(Vector3 point)
        {
            if (hasHomeRoom && !IsSafeRoomLocalTarget(point))
            {
                return;
            }

            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if ((Planar(patrolPoints[i]) - Planar(point)).sqrMagnitude < 1f)
                {
                    return;
                }
            }

            patrolPoints.Add(new Vector3(point.x, transform.position.y, point.z));
        }

        private bool CanRoamPassively()
        {
            return mobilityRole == EnemyMobilityRole.Roamer || mobilityRole == EnemyMobilityRole.Hunter;
        }

        private float GetSpeedForState(SimpleMeleeEnemyState nextState)
        {
            float multiplier = nextState switch
            {
                SimpleMeleeEnemyState.Chase => chaseSpeedMultiplier,
                SimpleMeleeEnemyState.Investigate => investigateSpeedMultiplier,
                SimpleMeleeEnemyState.ReturnToRoom => returnHomeSpeedMultiplier,
                SimpleMeleeEnemyState.Patrol => patrolSpeedMultiplier,
                _ => idleMoveSpeedMultiplier
            };
            return moveSpeed * Mathf.Max(0.05f, multiplier);
        }

        private bool IsSafeRoomLocalTarget(Vector3 point)
        {
            return !hasHomeRoom || IsInsideHomeRoom(point, DoorwayEdgeInset);
        }

        private bool IsInsideHomeRoom(Vector3 point, float inset)
        {
            if (!hasHomeRoom)
            {
                return true;
            }

            float safeInset = Mathf.Max(0f, inset);
            return point.x >= homeRoomBounds.min.x + safeInset &&
                   point.x <= homeRoomBounds.max.x - safeInset &&
                   point.z >= homeRoomBounds.min.z + safeInset &&
                   point.z <= homeRoomBounds.max.z - safeInset;
        }

        private Vector3 ClampToHomeRoom(Vector3 point, float inset)
        {
            if (!hasHomeRoom)
            {
                return point;
            }

            float safeInset = Mathf.Max(0f, inset);
            return new Vector3(
                Mathf.Clamp(point.x, homeRoomBounds.min.x + safeInset, homeRoomBounds.max.x - safeInset),
                point.y,
                Mathf.Clamp(point.z, homeRoomBounds.min.z + safeInset, homeRoomBounds.max.z - safeInset));
        }

        private Vector3 GetNearestHomePoint(Vector3 fromPosition)
        {
            if (patrolPoints.Count == 0)
            {
                return hasHomeRoom ? ClampToHomeRoom(homeRoomBounds.center, HomeBoundsInset) : fromPosition;
            }

            Vector3 best = patrolPoints[0];
            float bestDistance = (Planar(best) - Planar(fromPosition)).sqrMagnitude;
            for (int i = 1; i < patrolPoints.Count; i++)
            {
                float distance = (Planar(patrolPoints[i]) - Planar(fromPosition)).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = patrolPoints[i];
                }
            }

            return best;
        }

        private void TrackStuckProgress(Vector3 before, Vector3 direction, float deltaTime, bool allowStuckRecovery)
        {
            if (!allowStuckRecovery || direction.sqrMagnitude <= 0.001f)
            {
                stuckTimer = 0f;
                lastMoveSamplePosition = transform.position;
                return;
            }

            float moved = (Planar(transform.position) - Planar(before)).magnitude;
            if (moved <= StuckProgressEpsilon)
            {
                stuckTimer += deltaTime;
            }
            else
            {
                stuckTimer = 0f;
            }

            lastMoveSamplePosition = transform.position;
            if (stuckTimer < stuckRecoverySeconds)
            {
                return;
            }

            stuckTimer = 0f;
            stuckRecoveryCount++;
            if (debugStateLabelsVisible)
            {
                Debug.Log($"Enemy stuck recovery {name}: State={state} Home={HomeRoomId} Count={stuckRecoveryCount}");
            }

            if (state == SimpleMeleeEnemyState.Investigate)
            {
                currentMoveTarget = hasLastHeardPosition ? lastHeardPosition : GetNearestHomePoint(transform.position);
                if (hasHomeRoom && !IsInsideHomeRoom(currentMoveTarget, 0f))
                {
                    currentMoveTarget = GetNearestHomePoint(transform.position);
                }
            }
            else if (state == SimpleMeleeEnemyState.ReturnToRoom && enableDebugStuckSnapRecovery && hasHomeRoom)
            {
                transform.position = GetNearestHomePoint(transform.position);
            }
            else
            {
                ChooseNextPatrolTarget();
            }
        }

        private void SetState(SimpleMeleeEnemyState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            alertLevel = GetAlertLevelForState(nextState);
            ApplyStateColor(state);
        }

        private static EnemyAlertLevel GetAlertLevelForState(SimpleMeleeEnemyState nextState)
        {
            return nextState switch
            {
                SimpleMeleeEnemyState.Chase => EnemyAlertLevel.Combat,
                SimpleMeleeEnemyState.AttackWindup => EnemyAlertLevel.Combat,
                SimpleMeleeEnemyState.AttackRecover => EnemyAlertLevel.Combat,
                SimpleMeleeEnemyState.Investigate => EnemyAlertLevel.Investigating,
                SimpleMeleeEnemyState.ReturnToRoom => EnemyAlertLevel.Suspicious,
                _ => EnemyAlertLevel.Passive
            };
        }

        private void ApplyStateColor(SimpleMeleeEnemyState nextState)
        {
            if (health == null)
            {
                return;
            }

            switch (nextState)
            {
                case SimpleMeleeEnemyState.Chase:
                    health.SetStateColor(GetChaseColor());
                    break;
                case SimpleMeleeEnemyState.Investigate:
                    health.SetStateColor(Color.Lerp(baseBodyColor, InvestigateColor, 0.42f));
                    break;
                case SimpleMeleeEnemyState.ReturnToRoom:
                    health.SetStateColor(Color.Lerp(baseBodyColor, ReturnColor, 0.28f));
                    break;
                case SimpleMeleeEnemyState.AttackWindup:
                case SimpleMeleeEnemyState.AttackRecover:
                    health.SetStateColor(AttackColor);
                    break;
                case SimpleMeleeEnemyState.Sleep:
                    health.SetStateColor(Color.Lerp(baseBodyColor, Color.black, 0.18f));
                    break;
                case SimpleMeleeEnemyState.Dead:
                    break;
                default:
                    health.SetStateColor(GetIdleColor());
                    break;
            }
        }

        private Color GetIdleColor()
        {
            return baseBodyColor;
        }

        private Color GetChaseColor()
        {
            return Color.Lerp(baseBodyColor, new Color(1f, 0.48f, 0.16f, 1f), 0.35f);
        }

        private Color GetWindupColor()
        {
            return archetype switch
            {
                EnemyArchetype.Slime => Color.Lerp(baseBodyColor, new Color(0.75f, 1f, 0.42f, 1f), 0.55f),
                EnemyArchetype.Bat => Color.Lerp(baseBodyColor, Color.white, 0.62f),
                EnemyArchetype.GoblinBrute => Color.Lerp(baseBodyColor, new Color(1f, 0.05f, 0.02f, 1f), 0.78f),
                _ => Color.Lerp(baseBodyColor, new Color(1f, 0.28f, 0.04f, 1f), 0.65f)
            };
        }

        private void HandleDied(EnemyHealth enemyHealth)
        {
            CancelAttackWindup(false);
            target = null;
            alertUntilTime = 0f;
            hasLastKnownTargetPosition = false;
            hasLastHeardPosition = false;
            hasMoveTarget = false;
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

        private static bool IsCombatState(SimpleMeleeEnemyState nextState)
        {
            return nextState == SimpleMeleeEnemyState.Investigate ||
                   nextState == SimpleMeleeEnemyState.Chase ||
                   nextState == SimpleMeleeEnemyState.AttackWindup ||
                   nextState == SimpleMeleeEnemyState.AttackRecover ||
                   nextState == SimpleMeleeEnemyState.ReturnToRoom;
        }

        private static bool IsAttackState(SimpleMeleeEnemyState nextState)
        {
            return nextState == SimpleMeleeEnemyState.AttackWindup ||
                   nextState == SimpleMeleeEnemyState.AttackRecover;
        }

        private bool CanEnterActiveCombat()
        {
            if (IsActiveCombatState(state))
            {
                return true;
            }

            return CountActiveCombatEnemies() < GetActiveCombatCapForCurrentFloor();
        }

        private static bool IsActiveCombatState(SimpleMeleeEnemyState nextState)
        {
            return nextState == SimpleMeleeEnemyState.Chase ||
                   nextState == SimpleMeleeEnemyState.AttackWindup ||
                   nextState == SimpleMeleeEnemyState.AttackRecover;
        }

        private static int CountActiveCombatEnemies()
        {
            int count = 0;
            List<SimpleMeleeEnemyController> snapshot = new List<SimpleMeleeEnemyController>(ActiveEnemies);
            for (int i = 0; i < snapshot.Count; i++)
            {
                SimpleMeleeEnemyController enemy = snapshot[i];
                if (enemy == null || enemy.health == null || enemy.health.IsDead)
                {
                    continue;
                }

                if (IsActiveCombatState(enemy.state))
                {
                    count++;
                }
            }

            return count;
        }

        private static int GetActiveCombatCapForCurrentFloor()
        {
            RunState run = GameBootstrap.Instance != null && GameBootstrap.Instance.RunService != null
                ? GameBootstrap.Instance.RunService.Current
                : null;
            int floorIndex = run != null ? run.floorIndex : 1;
            if (floorIndex <= 2)
            {
                return 4;
            }

            if (floorIndex <= 5)
            {
                return 6;
            }

            if (floorIndex <= 8)
            {
                return 8;
            }

            return 10;
        }

        private float GetPatrolWaitDuration()
        {
            float noise = Mathf.Lerp(0.72f, 1.38f, Seeded01(4000 + behaviorPickCounter++));
            return Mathf.Max(0.05f, patrolWaitSeconds * patrolWaitJitter * noise);
        }

        private Vector3 ComputeEnemySeparationDirection(bool restrictToHomeRoom)
        {
            Vector3 separation = Vector3.zero;
            List<SimpleMeleeEnemyController> snapshot = new List<SimpleMeleeEnemyController>(ActiveEnemies);
            for (int i = 0; i < snapshot.Count; i++)
            {
                SimpleMeleeEnemyController other = snapshot[i];
                if (other == null || other == this || other.health == null || other.health.IsDead)
                {
                    continue;
                }

                if (restrictToHomeRoom && hasHomeRoom && !string.Equals(HomeRoomId, other.HomeRoomId, StringComparison.Ordinal))
                {
                    continue;
                }

                Vector3 offset = Planar(transform.position - other.transform.position);
                float distance = offset.magnitude;
                if (distance <= 0.001f || distance >= EnemySeparationRadius)
                {
                    continue;
                }

                separation += offset.normalized * (1f - distance / EnemySeparationRadius);
            }

            if (restrictToHomeRoom && hasHomeRoom)
            {
                Vector3 candidate = transform.position + separation;
                if (!IsInsideHomeRoom(candidate, 0f))
                {
                    separation = Planar(ClampToHomeRoom(candidate, 0f) - transform.position);
                }
            }

            return separation.sqrMagnitude > 0.001f ? separation.normalized : Vector3.zero;
        }

        private float Seeded01(int salt)
        {
            unchecked
            {
                int hash = behaviorSeed;
                hash = (hash * 397) ^ salt;
                hash = (hash * 397) ^ DeterministicStringHash(HomeRoomId);
                hash ^= hash << 13;
                hash ^= hash >> 17;
                hash ^= hash << 5;
                return (hash & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        private static int DeterministicStringHash(string value)
        {
            unchecked
            {
                int hash = 23;
                if (!string.IsNullOrEmpty(value))
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        hash = hash * 31 + value[i];
                    }
                }

                return hash;
            }
        }

        private static Vector3 Planar(Vector3 value)
        {
            return new Vector3(value.x, 0f, value.z);
        }
    }

    public enum EnemyAmbientBehavior
    {
        Idle,
        Wander,
        Patrol,
        SleepGuard
    }

    public enum SimpleMeleeEnemyState
    {
        Idle,
        Sleep,
        Patrol,
        Investigate,
        Chase,
        AttackWindup,
        AttackRecover,
        ReturnToRoom,
        Dead
    }
}
