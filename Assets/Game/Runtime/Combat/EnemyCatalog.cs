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
                CreateDefinition(EnemyArchetype.GoblinBrute),
                CreateDefinition(EnemyArchetype.CaveRat),
                CreateDefinition(EnemyArchetype.DustMite),
                CreateDefinition(EnemyArchetype.TrainingSkeleton),
                CreateDefinition(EnemyArchetype.Skitter),
                CreateDefinition(EnemyArchetype.SpitterSlime),
                CreateDefinition(EnemyArchetype.BoneArcher),
                CreateDefinition(EnemyArchetype.ShieldGoblin),
                CreateDefinition(EnemyArchetype.BombBeetle),
                CreateDefinition(EnemyArchetype.OrcGrunt),
                CreateDefinition(EnemyArchetype.RiftHound),
                CreateDefinition(EnemyArchetype.Stoneback),
                CreateDefinition(EnemyArchetype.HexCaster),
                CreateDefinition(EnemyArchetype.IronOgre),
                CreateDefinition(EnemyArchetype.HexWitch),
                CreateDefinition(EnemyArchetype.GraveKnight),
                CreateDefinition(EnemyArchetype.RiftStalker)
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
                    definition.attackFamily = EnemyAttackFamily.MeleeRush;
                    definition.visualProfileId = "squat_blob";
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
                    definition.ammoAmount = 10;
                    break;
                case EnemyArchetype.Bat:
                    definition.enemyId = "enemy.bat";
                    definition.displayName = "Bat";
                    definition.attackFamily = EnemyAttackFamily.FastSkirmisher;
                    definition.visualProfileId = "winged_small";
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
                    definition.ammoAmount = 10;
                    break;
                case EnemyArchetype.CaveRat:
                    ConfigureDefinition(definition, "enemy.cave_rat", "Cave Rat", 1, 20f, 6.4f, 5f, 1.55f, 0.85f, 0.11f, 34f,
                        EnemyAmbientBehavior.Wander, EnemyMobilityRole.Roamer, EnemyAttackFamily.FastSkirmisher, "low_quadruped",
                        new Color(0.45f, 0.38f, 0.28f, 1f), new Vector3(0.7f, 0.55f, 1.05f), 18f, 1, 0,
                        1f, 24f, 120f, 0.9f, 1.1f, 1.18f, 1f, 0.7f, 0.12f, 2, 7, 0.05f, 6f, 0.1f, 8);
                    break;
                case EnemyArchetype.DustMite:
                    ConfigureDefinition(definition, "enemy.dust_mite", "Dust Mite", 1, 14f, 5.8f, 4f, 1.35f, 0.72f, 0.08f, 30f,
                        EnemyAmbientBehavior.Wander, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.FastSkirmisher, "tiny_swarm",
                        new Color(0.72f, 0.65f, 0.46f, 1f), new Vector3(0.5f, 0.45f, 0.5f), 14f, 1, 0,
                        0.85f, 22f, 145f, 0.95f, 1.08f, 1.16f, 0.95f, 0.55f, 0.1f, 1, 5, 0.04f, 5f, 0.08f, 6);
                    break;
                case EnemyArchetype.TrainingSkeleton:
                    ConfigureDefinition(definition, "enemy.training_skeleton", "Training Skeleton", 1, 44f, 3.2f, 8f, 2f, 1.35f, 0.35f, 36f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.MeleeRush, "bone_stack",
                        new Color(0.78f, 0.76f, 0.66f, 1f), new Vector3(1.0f, 1.65f, 0.8f), 12f, 1, 0,
                        0.75f, 25f, 100f, 0.62f, 0.82f, 1f, 0.72f, 0.95f, 0.18f, 4, 10, 0.08f, 8f, 0.08f, 8);
                    break;
                case EnemyArchetype.Skitter:
                    ConfigureDefinition(definition, "enemy.skitter", "Skitter", 1, 26f, 6.8f, 6f, 1.65f, 0.82f, 0.1f, 38f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Roamer, EnemyAttackFamily.FastSkirmisher, "spider_low",
                        new Color(0.22f, 0.23f, 0.18f, 1f), new Vector3(0.95f, 0.55f, 0.95f), 13f, 2, 0,
                        1.15f, 30f, 150f, 0.9f, 1.1f, 1.22f, 1f, 0.8f, 0.12f, 3, 8, 0.05f, 6f, 0.12f, 8);
                    break;
                case EnemyArchetype.SpitterSlime:
                    ConfigureDefinition(definition, "enemy.spitter_slime", "Spitter Slime", 2, 50f, 3.2f, 8f, 2.35f, 1.45f, 0.32f, 44f,
                        EnemyAmbientBehavior.Wander, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.RangedSpit, "spitter_blob",
                        new Color(0.18f, 0.74f, 0.56f, 1f), new Vector3(1.3f, 1.05f, 1.3f), 16f, 3, 0,
                        1f, 30f, 120f, 0.58f, 0.82f, 1f, 0.72f, 1.25f, 0.24f, 6, 14, 0.1f, 8f, 0.16f, 10);
                    break;
                case EnemyArchetype.BoneArcher:
                    ConfigureDefinition(definition, "enemy.bone_archer", "Bone Archer", 2, 58f, 4.1f, 10f, 2.55f, 1.35f, 0.24f, 54f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.RangedSpit, "thin_ranged",
                        new Color(0.74f, 0.72f, 0.64f, 1f), new Vector3(0.9f, 1.75f, 0.8f), 14f, 3, 0,
                        1f, 32f, 130f, 0.72f, 1.02f, 1.08f, 0.86f, 1.35f, 0.25f, 7, 15, 0.06f, 8f, 0.18f, 10);
                    break;
                case EnemyArchetype.ShieldGoblin:
                    ConfigureDefinition(definition, "enemy.shield_goblin", "Shield Goblin", 2, 92f, 3.8f, 12f, 2.15f, 1.4f, 0.36f, 44f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.TankBruiser, "shielded",
                        new Color(0.55f, 0.35f, 0.18f, 1f), new Vector3(1.25f, 1.55f, 1.15f), 14f, 3, 0,
                        0.9f, 32f, 105f, 0.58f, 0.85f, 1.02f, 0.75f, 1.45f, 0.35f, 8, 18, 0.1f, 10f, 0.12f, 10);
                    break;
                case EnemyArchetype.BombBeetle:
                    ConfigureDefinition(definition, "enemy.bomb_beetle", "Bomb Beetle", 2, 42f, 5.4f, 16f, 1.75f, 1.8f, 0.55f, 38f,
                        EnemyAmbientBehavior.Wander, EnemyMobilityRole.Roamer, EnemyAttackFamily.Charger, "round_charger",
                        new Color(0.9f, 0.42f, 0.12f, 1f), new Vector3(1.1f, 0.8f, 1.1f), 10f, 4, 0,
                        0.95f, 28f, 140f, 0.72f, 1f, 1.22f, 0.85f, 1.4f, 0.28f, 7, 16, 0.05f, 6f, 0.16f, 10);
                    break;
                case EnemyArchetype.OrcGrunt:
                    ConfigureDefinition(definition, "enemy.orc_grunt", "Orc Grunt", 3, 118f, 4.2f, 17f, 2.55f, 1.45f, 0.4f, 48f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.TankBruiser, "heavy_melee",
                        new Color(0.25f, 0.52f, 0.25f, 1f), new Vector3(1.5f, 1.95f, 1.35f), 17f, 6, 0,
                        1f, 36f, 110f, 0.56f, 0.86f, 1.08f, 0.78f, 2f, 0.45f, 12, 26, 0.12f, 12f, 0.18f, 12);
                    break;
                case EnemyArchetype.RiftHound:
                    ConfigureDefinition(definition, "enemy.rift_hound", "Rift Hound", 3, 72f, 7.1f, 13f, 2.05f, 0.95f, 0.18f, 52f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Hunter, EnemyAttackFamily.Charger, "rift_hound",
                        new Color(0.32f, 0.2f, 0.7f, 1f), new Vector3(1.15f, 0.95f, 1.55f), 16f, 6, 0,
                        1.25f, 42f, 155f, 0.86f, 1.15f, 1.28f, 1.05f, 1.75f, 0.22f, 10, 22, 0.08f, 8f, 0.22f, 12);
                    break;
                case EnemyArchetype.Stoneback:
                    ConfigureDefinition(definition, "enemy.stoneback", "Stoneback", 3, 160f, 2.8f, 19f, 2.65f, 1.75f, 0.5f, 42f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.TankBruiser, "stone_tank",
                        new Color(0.46f, 0.48f, 0.44f, 1f), new Vector3(1.75f, 1.5f, 1.75f), 13f, 6, 0,
                        0.75f, 30f, 95f, 0.42f, 0.7f, 0.92f, 0.66f, 2.2f, 0.5f, 14, 30, 0.16f, 14f, 0.14f, 12);
                    break;
                case EnemyArchetype.HexCaster:
                    ConfigureDefinition(definition, "enemy.hex_caster", "Hex Caster", 3, 82f, 3.9f, 14f, 2.7f, 1.55f, 0.38f, 58f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.CasterSupport, "caster",
                        new Color(0.48f, 0.16f, 0.72f, 1f), new Vector3(1.05f, 1.75f, 1.05f), 12f, 7, 0,
                        1.15f, 38f, 150f, 0.64f, 1f, 1.08f, 0.86f, 2f, 0.32f, 12, 24, 0.08f, 8f, 0.2f, 12);
                    break;
                case EnemyArchetype.IronOgre:
                    ConfigureDefinition(definition, "enemy.iron_ogre", "Iron Ogre", 4, 260f, 3.2f, 26f, 3f, 1.95f, 0.65f, 52f,
                        EnemyAmbientBehavior.SleepGuard, EnemyMobilityRole.Sleeper, EnemyAttackFamily.TankBruiser, "ogre",
                        new Color(0.55f, 0.52f, 0.48f, 1f), new Vector3(2.05f, 2.45f, 1.95f), 13f, 11, 0,
                        0.9f, 42f, 105f, 0.38f, 0.74f, 1.02f, 0.7f, 3.6f, 0.65f, 20, 45, 0.18f, 18f, 0.2f, 14);
                    break;
                case EnemyArchetype.HexWitch:
                    ConfigureDefinition(definition, "enemy.hex_witch", "Hex Witch", 4, 130f, 4.4f, 18f, 2.75f, 1.45f, 0.32f, 62f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Hunter, EnemyAttackFamily.CasterSupport, "witch",
                        new Color(0.35f, 0.08f, 0.42f, 1f), new Vector3(1.15f, 1.95f, 1.15f), 12f, 11, 0,
                        1.25f, 45f, 165f, 0.72f, 1.08f, 1.18f, 0.95f, 3.1f, 0.42f, 18, 38, 0.1f, 12f, 0.22f, 14);
                    break;
                case EnemyArchetype.GraveKnight:
                    ConfigureDefinition(definition, "enemy.grave_knight", "Grave Knight", 4, 210f, 4.0f, 23f, 2.75f, 1.55f, 0.48f, 56f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.TankBruiser, "grave_knight",
                        new Color(0.28f, 0.32f, 0.38f, 1f), new Vector3(1.55f, 2.1f, 1.35f), 14f, 11, 0,
                        1f, 42f, 115f, 0.55f, 0.86f, 1.08f, 0.8f, 3.3f, 0.58f, 20, 42, 0.12f, 14f, 0.2f, 14);
                    break;
                case EnemyArchetype.RiftStalker:
                    ConfigureDefinition(definition, "enemy.rift_stalker", "Rift Stalker", 4, 150f, 6.8f, 20f, 2.35f, 1.12f, 0.2f, 64f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Hunter, EnemyAttackFamily.Ambusher, "stalker",
                        new Color(0.12f, 0.12f, 0.32f, 1f), new Vector3(1.2f, 1.75f, 1.2f), 11f, 12, 0,
                        1.35f, 48f, 170f, 0.85f, 1.16f, 1.26f, 1.02f, 3.2f, 0.36f, 18, 40, 0.08f, 10f, 0.25f, 14);
                    break;
                case EnemyArchetype.GoblinBrute:
                    definition.enemyId = "enemy.goblin_brute";
                    definition.displayName = "Goblin Brute";
                    definition.attackFamily = EnemyAttackFamily.TankBruiser;
                    definition.visualProfileId = "brute";
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
                    definition.ammoAmount = 12;
                    break;
                default:
                    definition.enemyId = "enemy.goblin_grunt";
                    definition.displayName = "Goblin Grunt";
                    definition.attackFamily = EnemyAttackFamily.MeleeRush;
                    definition.visualProfileId = "goblin";
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
                    definition.ammoAmount = 10;
                    break;
            }
        }

        private static void ConfigureDefinition(
            EnemyDefinition definition,
            string enemyId,
            string displayName,
            int tier,
            float maxHealth,
            float moveSpeed,
            float attackDamage,
            float attackRange,
            float attackCooldown,
            float attackWindupDuration,
            float detectionRange,
            EnemyAmbientBehavior ambientBehavior,
            EnemyMobilityRole mobilityRole,
            EnemyAttackFamily attackFamily,
            string visualProfileId,
            Color bodyColor,
            Vector3 visualScale,
            float spawnWeight,
            int minFloor,
            int maxFloor,
            float hearingRadiusMultiplier,
            float groupAlertRadius,
            float visionConeAngle,
            float patrolSpeedMultiplier,
            float investigateSpeedMultiplier,
            float chaseSpeedMultiplier,
            float returnHomeSpeedMultiplier,
            float masteryXpValue,
            float goldDropChance,
            int goldMin,
            int goldMax,
            float healthDropChance,
            float healthAmount,
            float ammoDropChance,
            int ammoAmount)
        {
            definition.enemyId = enemyId;
            definition.displayName = displayName;
            definition.tier = tier;
            definition.maxHealth = maxHealth;
            definition.moveSpeed = moveSpeed;
            definition.attackDamage = attackDamage;
            definition.attackRange = attackRange;
            definition.attackCooldown = attackCooldown;
            definition.attackWindupDuration = attackWindupDuration;
            definition.detectionRange = detectionRange;
            definition.hearingRadiusMultiplier = hearingRadiusMultiplier;
            definition.groupAlertRadius = groupAlertRadius;
            definition.ambientBehavior = ambientBehavior;
            definition.defaultMobilityRole = mobilityRole;
            definition.attackFamily = attackFamily;
            definition.visualProfileId = visualProfileId;
            definition.visionConeAngle = visionConeAngle;
            definition.idleMoveSpeedMultiplier = patrolSpeedMultiplier;
            definition.patrolSpeedMultiplier = patrolSpeedMultiplier;
            definition.investigateSpeedMultiplier = investigateSpeedMultiplier;
            definition.chaseSpeedMultiplier = chaseSpeedMultiplier;
            definition.returnHomeSpeedMultiplier = returnHomeSpeedMultiplier;
            definition.patrolWaitSeconds = Mathf.Lerp(1.45f, 0.75f, Mathf.InverseLerp(1f, 4f, tier));
            definition.investigateDuration = Mathf.Lerp(3.2f, 5.2f, Mathf.InverseLerp(1f, 4f, tier));
            definition.lostSightGraceDuration = Mathf.Lerp(0.75f, 1.5f, Mathf.InverseLerp(1f, 4f, tier));
            definition.searchDuration = Mathf.Lerp(2.5f, 5.2f, Mathf.InverseLerp(1f, 4f, tier));
            definition.homeReturnStopDistance = 1.1f;
            definition.stuckRecoverySeconds = 1.2f;
            definition.visualScale = visualScale;
            definition.bodyColor = bodyColor;
            definition.spawnWeight = spawnWeight;
            definition.minFloor = minFloor;
            definition.maxFloor = maxFloor;
            definition.masteryXpValue = masteryXpValue;
            definition.goldDropChance = goldDropChance;
            definition.goldMin = goldMin;
            definition.goldMax = goldMax;
            definition.healthDropChance = healthDropChance;
            definition.healthAmount = healthAmount;
            definition.ammoDropChance = ammoDropChance;
            definition.ammoAmount = ammoAmount;
        }
    }
}
