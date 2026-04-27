using System;
using UnityEngine;

namespace FrontierDepths.Combat
{
    public static class EnemyVariantCatalog
    {
        private static readonly EnemyVariantDefinition[] Variants =
        {
            new EnemyVariantDefinition
            {
                variantId = "variant.fast_slime",
                displaySuffix = "Fast",
                healthMultiplier = 0.8f,
                speedMultiplier = 1.25f,
                damageMultiplier = 0.9f,
                sizeMultiplier = 0.9f,
                colorTint = new Color(0.55f, 1f, 0.55f, 1f),
                spawnWeight = 6f,
                minFloor = 2
            },
            new EnemyVariantDefinition
            {
                variantId = "variant.heavy_slime",
                displaySuffix = "Heavy",
                healthMultiplier = 1.35f,
                speedMultiplier = 0.85f,
                damageMultiplier = 1.1f,
                sizeMultiplier = 1.15f,
                colorTint = new Color(0.28f, 0.85f, 0.38f, 1f),
                spawnWeight = 4f,
                minFloor = 3
            },
            new EnemyVariantDefinition
            {
                variantId = "variant.feral_bat",
                displaySuffix = "Feral",
                healthMultiplier = 0.85f,
                speedMultiplier = 1.2f,
                damageMultiplier = 1.1f,
                sizeMultiplier = 0.9f,
                colorTint = new Color(0.35f, 0.32f, 0.55f, 1f),
                spawnWeight = 6f,
                minFloor = 2
            },
            new EnemyVariantDefinition
            {
                variantId = "variant.scout_goblin",
                displaySuffix = "Scout",
                healthMultiplier = 0.85f,
                speedMultiplier = 1.18f,
                damageMultiplier = 1f,
                sizeMultiplier = 0.95f,
                colorTint = new Color(1f, 0.5f, 0.24f, 1f),
                spawnWeight = 5f,
                minFloor = 3
            },
            new EnemyVariantDefinition
            {
                variantId = "variant.guard_goblin",
                displaySuffix = "Guard",
                healthMultiplier = 1.3f,
                speedMultiplier = 0.9f,
                damageMultiplier = 1.15f,
                sizeMultiplier = 1.05f,
                colorTint = new Color(0.9f, 0.38f, 0.2f, 1f),
                spawnWeight = 4f,
                minFloor = 4
            },
            new EnemyVariantDefinition
            {
                variantId = "variant.brute_guardian",
                displaySuffix = "Guardian",
                healthMultiplier = 1.35f,
                speedMultiplier = 0.9f,
                damageMultiplier = 1.25f,
                sizeMultiplier = 1.1f,
                colorTint = new Color(0.58f, 0.18f, 0.14f, 1f),
                spawnWeight = 3f,
                minFloor = 5
            }
        };

        public static EnemyVariantDefinition ChooseVariant(EnemyArchetype archetype, int floorIndex, System.Random random)
        {
            if (floorIndex <= 1 || random == null)
            {
                return null;
            }

            float totalWeight = 0f;
            for (int i = 0; i < Variants.Length; i++)
            {
                if (IsVariantForArchetype(Variants[i], archetype) && Variants[i].IsEligibleForFloor(floorIndex))
                {
                    totalWeight += Mathf.Max(0f, Variants[i].spawnWeight);
                }
            }

            float noVariantWeight = floorIndex >= 6 ? 12f : 24f;
            double roll = random.NextDouble() * (totalWeight + noVariantWeight);
            if (roll < noVariantWeight || totalWeight <= 0f)
            {
                return null;
            }

            float cursor = noVariantWeight;
            for (int i = 0; i < Variants.Length; i++)
            {
                EnemyVariantDefinition variant = Variants[i];
                if (!IsVariantForArchetype(variant, archetype) || !variant.IsEligibleForFloor(floorIndex))
                {
                    continue;
                }

                cursor += Mathf.Max(0f, variant.spawnWeight);
                if (roll <= cursor)
                {
                    return variant;
                }
            }

            return null;
        }

        public static EnemyDefinition CreateVariantDefinition(EnemyDefinition baseDefinition, EnemyVariantDefinition variant)
        {
            if (baseDefinition == null || variant == null)
            {
                return baseDefinition;
            }

            EnemyDefinition clone = ScriptableObject.CreateInstance<EnemyDefinition>();
            CopyDefinition(baseDefinition, clone);
            clone.enemyId = $"{baseDefinition.enemyId}.{variant.variantId}";
            clone.displayName = string.IsNullOrWhiteSpace(variant.displaySuffix)
                ? baseDefinition.displayName
                : $"{variant.displaySuffix} {baseDefinition.displayName}";
            clone.maxHealth *= Mathf.Max(0.1f, variant.healthMultiplier);
            clone.moveSpeed *= Mathf.Max(0.1f, variant.speedMultiplier);
            clone.attackDamage *= Mathf.Max(0f, variant.damageMultiplier);
            clone.visualScale *= Mathf.Max(0.1f, variant.sizeMultiplier);
            clone.bodyColor = Color.Lerp(baseDefinition.bodyColor, variant.colorTint, 0.35f);
            return clone;
        }

        private static bool IsVariantForArchetype(EnemyVariantDefinition variant, EnemyArchetype archetype)
        {
            if (variant == null || string.IsNullOrWhiteSpace(variant.variantId))
            {
                return false;
            }

            return archetype switch
            {
                EnemyArchetype.Slime => variant.variantId.IndexOf("slime", StringComparison.Ordinal) >= 0,
                EnemyArchetype.Bat => variant.variantId.IndexOf("bat", StringComparison.Ordinal) >= 0,
                EnemyArchetype.GoblinBrute => variant.variantId.IndexOf("brute", StringComparison.Ordinal) >= 0,
                _ => variant.variantId.IndexOf("goblin", StringComparison.Ordinal) >= 0
            };
        }

        private static void CopyDefinition(EnemyDefinition source, EnemyDefinition target)
        {
            target.enemyId = source.enemyId;
            target.displayName = source.displayName;
            target.archetype = source.archetype;
            target.attackFamily = source.attackFamily;
            target.visualProfileId = source.visualProfileId;
            target.tier = source.tier;
            target.maxHealth = source.maxHealth;
            target.moveSpeed = source.moveSpeed;
            target.attackDamage = source.attackDamage;
            target.attackRange = source.attackRange;
            target.attackCooldown = source.attackCooldown;
            target.attackWindupDuration = source.attackWindupDuration;
            target.detectionRange = source.detectionRange;
            target.hearingRadiusMultiplier = source.hearingRadiusMultiplier;
            target.groupAlertRadius = source.groupAlertRadius;
            target.ambientBehavior = source.ambientBehavior;
            target.defaultMobilityRole = source.defaultMobilityRole;
            target.visionConeAngle = source.visionConeAngle;
            target.idleMoveSpeedMultiplier = source.idleMoveSpeedMultiplier;
            target.patrolSpeedMultiplier = source.patrolSpeedMultiplier;
            target.investigateSpeedMultiplier = source.investigateSpeedMultiplier;
            target.chaseSpeedMultiplier = source.chaseSpeedMultiplier;
            target.returnHomeSpeedMultiplier = source.returnHomeSpeedMultiplier;
            target.patrolWaitSeconds = source.patrolWaitSeconds;
            target.investigateDuration = source.investigateDuration;
            target.lostSightGraceDuration = source.lostSightGraceDuration;
            target.searchDuration = source.searchDuration;
            target.homeReturnStopDistance = source.homeReturnStopDistance;
            target.stuckRecoverySeconds = source.stuckRecoverySeconds;
            target.visualScale = source.visualScale;
            target.bodyColor = source.bodyColor;
            target.spawnWeight = source.spawnWeight;
            target.minFloor = source.minFloor;
            target.maxFloor = source.maxFloor;
            target.masteryXpValue = source.masteryXpValue;
            target.goldDropChance = source.goldDropChance;
            target.goldMin = source.goldMin;
            target.goldMax = source.goldMax;
            target.healthDropChance = source.healthDropChance;
            target.healthAmount = source.healthAmount;
            target.ammoDropChance = source.ammoDropChance;
            target.ammoAmount = source.ammoAmount;
        }
    }
}
