using UnityEngine;

namespace FrontierDepths.Combat
{
    public enum EnemyArchetype
    {
        Slime,
        Bat,
        GoblinGrunt,
        GoblinBrute
    }

    public enum EnemyMobilityRole
    {
        RoomGuard,
        Roamer,
        Sleeper,
        Hunter,
        AmbusherPlaceholder
    }

    public enum EnemyAlertLevel
    {
        Passive,
        Suspicious,
        Investigating,
        Combat
    }

    [CreateAssetMenu(menuName = "FrontierDepths/Combat/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string enemyId = "enemy.mire_hound";
        public string displayName = "Mire Hound";
        public EnemyArchetype archetype = EnemyArchetype.GoblinGrunt;
        public int tier = 1;
        public float maxHealth = 55f;
        public float moveSpeed = 4.5f;
        public float attackDamage = 10f;
        public float attackRange = 2.2f;
        public float attackCooldown = 1.2f;
        public float attackWindupDuration = 0.25f;
        public float detectionRange = 45f;
        public float hearingRadiusMultiplier = 1f;
        public float groupAlertRadius = 30f;
        public EnemyAmbientBehavior ambientBehavior = EnemyAmbientBehavior.Patrol;
        public EnemyMobilityRole defaultMobilityRole = EnemyMobilityRole.RoomGuard;
        public float visionConeAngle = 120f;
        public float idleMoveSpeedMultiplier = 0.55f;
        public float patrolSpeedMultiplier = 0.55f;
        public float investigateSpeedMultiplier = 0.85f;
        public float chaseSpeedMultiplier = 1f;
        public float returnHomeSpeedMultiplier = 0.75f;
        public float patrolWaitSeconds = 1.1f;
        public float investigateDuration = 4f;
        public float lostSightGraceDuration = 1f;
        public float searchDuration = 3f;
        public float homeReturnStopDistance = 1.1f;
        public float stuckRecoverySeconds = 1.4f;
        public Vector3 visualScale = new Vector3(1.25f, 1.55f, 1.25f);
        public Color bodyColor = new Color(0.72f, 0.28f, 0.22f, 1f);
        public float spawnWeight = 1f;
        public int minFloor = 1;
        public int maxFloor = 5;
        public float masteryXpValue = 1f;
        public float goldDropChance = 0.2f;
        public int goldMin = 4;
        public int goldMax = 10;
        public float healthDropChance = 0.08f;
        public float healthAmount = 10f;
        public float ammoDropChance = 0.08f;
        public int ammoAmount = 2;

        public bool IsEligibleForFloor(int floorIndex)
        {
            int clampedFloor = Mathf.Max(1, floorIndex);
            return clampedFloor >= Mathf.Max(1, minFloor) &&
                   (maxFloor <= 0 || clampedFloor <= maxFloor);
        }
    }

    public sealed class EnemyVariantDefinition
    {
        public string variantId = string.Empty;
        public string displaySuffix = string.Empty;
        public float healthMultiplier = 1f;
        public float speedMultiplier = 1f;
        public float damageMultiplier = 1f;
        public float sizeMultiplier = 1f;
        public Color colorTint = Color.white;
        public float spawnWeight = 1f;
        public int minFloor = 1;
        public int maxFloor;

        public bool IsEligibleForFloor(int floorIndex)
        {
            int clampedFloor = Mathf.Max(1, floorIndex);
            return clampedFloor >= Mathf.Max(1, minFloor) &&
                   (maxFloor <= 0 || clampedFloor <= maxFloor);
        }
    }
}
