using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.Combat
{
    public static class EnemyCatalog
    {
        public static EnemyDefinition CreateDefinition(EnemyArchetype archetype)
        {
            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            Configure(definition, archetype);
            return definition;
        }

        public static List<EnemyDefinition> CreateDefaultDefinitions()
        {
            return new List<EnemyDefinition>
            {
                CreateDefinition(EnemyArchetype.Slime),
                CreateDefinition(EnemyArchetype.Bat),
                CreateDefinition(EnemyArchetype.GoblinGrunt),
                CreateDefinition(EnemyArchetype.GoblinBrute)
            };
        }

        public static List<EnemyDefinition> CreateDefinitionsForFloor(int floorIndex)
        {
            List<EnemyDefinition> definitions = CreateDefaultDefinitions();
            for (int i = definitions.Count - 1; i >= 0; i--)
            {
                if (definitions[i] == null || !definitions[i].IsEligibleForFloor(floorIndex))
                {
                    definitions.RemoveAt(i);
                }
            }

            return definitions;
        }

        private static void Configure(EnemyDefinition definition, EnemyArchetype archetype)
        {
            definition.archetype = archetype;
            switch (archetype)
            {
                case EnemyArchetype.Slime:
                    definition.enemyId = "enemy.slime";
                    definition.displayName = "Slime";
                    definition.tier = 1;
                    definition.maxHealth = 36f;
                    definition.moveSpeed = 3.6f;
                    definition.attackDamage = 6f;
                    definition.attackRange = 1.8f;
                    definition.attackCooldown = 1.35f;
                    definition.attackWindupDuration = 0.22f;
                    definition.detectionRange = 32f;
                    definition.hearingRadiusMultiplier = 0.8f;
                    definition.groupAlertRadius = 28f;
                    definition.ambientBehavior = EnemyAmbientBehavior.Wander;
                    definition.defaultMobilityRole = EnemyMobilityRole.RoomGuard;
                    definition.visionConeAngle = 90f;
                    definition.idleMoveSpeedMultiplier = 0.65f;
                    definition.patrolSpeedMultiplier = 0.65f;
                    definition.investigateSpeedMultiplier = 0.9f;
                    definition.chaseSpeedMultiplier = 1.05f;
                    definition.returnHomeSpeedMultiplier = 0.75f;
                    definition.patrolWaitSeconds = 1.6f;
                    definition.investigateDuration = 3.2f;
                    definition.lostSightGraceDuration = 0.7f;
                    definition.searchDuration = 2.4f;
                    definition.homeReturnStopDistance = 1.1f;
                    definition.stuckRecoverySeconds = 1.2f;
                    definition.visualScale = new Vector3(1.45f, 0.85f, 1.45f);
                    definition.bodyColor = new Color(0.24f, 0.72f, 0.32f, 1f);
                    definition.spawnWeight = 55f;
                    definition.minFloor = 1;
                    definition.maxFloor = 0;
                    definition.masteryXpValue = 0.75f;
                    definition.goldDropChance = 0.16f;
                    definition.goldMin = 3;
                    definition.goldMax = 8;
                    definition.healthDropChance = 0.12f;
                    definition.healthAmount = 8f;
                    definition.ammoDropChance = 0.08f;
                    definition.ammoAmount = 6;
                    break;
                case EnemyArchetype.Bat:
                    definition.enemyId = "enemy.bat";
                    definition.displayName = "Bat";
                    definition.tier = 1;
                    definition.maxHealth = 24f;
                    definition.moveSpeed = 7.5f;
                    definition.attackDamage = 6f;
                    definition.attackRange = 1.7f;
                    definition.attackCooldown = 0.95f;
                    definition.attackWindupDuration = 0.14f;
                    definition.detectionRange = 42f;
                    definition.hearingRadiusMultiplier = 1.2f;
                    definition.groupAlertRadius = 38f;
                    definition.ambientBehavior = EnemyAmbientBehavior.Patrol;
                    definition.defaultMobilityRole = EnemyMobilityRole.Roamer;
                    definition.visionConeAngle = 150f;
                    definition.idleMoveSpeedMultiplier = 0.9f;
                    definition.patrolSpeedMultiplier = 0.9f;
                    definition.investigateSpeedMultiplier = 1.1f;
                    definition.chaseSpeedMultiplier = 1.25f;
                    definition.returnHomeSpeedMultiplier = 1f;
                    definition.patrolWaitSeconds = 0.65f;
                    definition.investigateDuration = 4.2f;
                    definition.lostSightGraceDuration = 1.1f;
                    definition.searchDuration = 3.2f;
                    definition.homeReturnStopDistance = 1f;
                    definition.stuckRecoverySeconds = 1f;
                    definition.visualScale = new Vector3(0.85f, 0.95f, 0.85f);
                    definition.bodyColor = new Color(0.18f, 0.17f, 0.28f, 1f);
                    definition.spawnWeight = 25f;
                    definition.minFloor = 1;
                    definition.maxFloor = 0;
                    definition.masteryXpValue = 0.85f;
                    definition.goldDropChance = 0.12f;
                    definition.goldMin = 3;
                    definition.goldMax = 7;
                    definition.healthDropChance = 0.05f;
                    definition.healthAmount = 6f;
                    definition.ammoDropChance = 0.16f;
                    definition.ammoAmount = 6;
                    break;
                case EnemyArchetype.GoblinBrute:
                    definition.enemyId = "enemy.goblin_brute";
                    definition.displayName = "Goblin Brute";
                    definition.tier = 3;
                    definition.maxHealth = 126f;
                    definition.moveSpeed = 3.9f;
                    definition.attackDamage = 20f;
                    definition.attackRange = 2.6f;
                    definition.attackCooldown = 1.65f;
                    definition.attackWindupDuration = 0.45f;
                    definition.detectionRange = 45f;
                    definition.hearingRadiusMultiplier = 0.9f;
                    definition.groupAlertRadius = 36f;
                    definition.ambientBehavior = EnemyAmbientBehavior.SleepGuard;
                    definition.defaultMobilityRole = EnemyMobilityRole.Sleeper;
                    definition.visionConeAngle = 100f;
                    definition.idleMoveSpeedMultiplier = 0.5f;
                    definition.patrolSpeedMultiplier = 0.5f;
                    definition.investigateSpeedMultiplier = 0.85f;
                    definition.chaseSpeedMultiplier = 1.15f;
                    definition.returnHomeSpeedMultiplier = 0.7f;
                    definition.patrolWaitSeconds = 2.1f;
                    definition.investigateDuration = 4.8f;
                    definition.lostSightGraceDuration = 1.5f;
                    definition.searchDuration = 5f;
                    definition.homeReturnStopDistance = 1.35f;
                    definition.stuckRecoverySeconds = 1.5f;
                    definition.visualScale = new Vector3(1.65f, 2.05f, 1.65f);
                    definition.bodyColor = new Color(0.42f, 0.15f, 0.12f, 1f);
                    definition.spawnWeight = 0f;
                    definition.minFloor = 3;
                    definition.maxFloor = 0;
                    definition.masteryXpValue = 2.25f;
                    definition.goldDropChance = 1f;
                    definition.goldMin = 12;
                    definition.goldMax = 24;
                    definition.healthDropChance = 0.18f;
                    definition.healthAmount = 14f;
                    definition.ammoDropChance = 0.22f;
                    definition.ammoAmount = 9;
                    break;
                default:
                    definition.enemyId = "enemy.goblin_grunt";
                    definition.displayName = "Goblin Grunt";
                    definition.tier = 2;
                    definition.maxHealth = 72f;
                    definition.moveSpeed = 5.4f;
                    definition.attackDamage = 11f;
                    definition.attackRange = 2.2f;
                    definition.attackCooldown = 1.15f;
                    definition.attackWindupDuration = 0.28f;
                    definition.detectionRange = 48f;
                    definition.hearingRadiusMultiplier = 1f;
                    definition.groupAlertRadius = 34f;
                    definition.ambientBehavior = EnemyAmbientBehavior.Patrol;
                    definition.defaultMobilityRole = EnemyMobilityRole.RoomGuard;
                    definition.visionConeAngle = 120f;
                    definition.idleMoveSpeedMultiplier = 0.8f;
                    definition.patrolSpeedMultiplier = 0.8f;
                    definition.investigateSpeedMultiplier = 1.05f;
                    definition.chaseSpeedMultiplier = 1.2f;
                    definition.returnHomeSpeedMultiplier = 0.9f;
                    definition.patrolWaitSeconds = 1f;
                    definition.investigateDuration = 4f;
                    definition.lostSightGraceDuration = 1f;
                    definition.searchDuration = 3.2f;
                    definition.homeReturnStopDistance = 1.1f;
                    definition.stuckRecoverySeconds = 1.25f;
                    definition.visualScale = new Vector3(1.25f, 1.55f, 1.25f);
                    definition.bodyColor = new Color(0.76f, 0.32f, 0.17f, 1f);
                    definition.spawnWeight = 20f;
                    definition.minFloor = 1;
                    definition.maxFloor = 0;
                    definition.masteryXpValue = 1.25f;
                    definition.goldDropChance = 0.3f;
                    definition.goldMin = 6;
                    definition.goldMax = 14;
                    definition.healthDropChance = 0.08f;
                    definition.healthAmount = 10f;
                    definition.ammoDropChance = 0.14f;
                    definition.ammoAmount = 6;
                    break;
            }
        }
    }
}
