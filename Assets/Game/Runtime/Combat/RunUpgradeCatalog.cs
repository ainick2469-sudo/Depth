using System;
using System.Collections.Generic;
using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.Combat
{
    public static class RunUpgradeCatalog
    {
        public const string RevolverDamageUpgradeId = "upgrade.run.deadeye_rounds";
        public const string ReloadSpeedUpgradeId = "upgrade.run.quick_hands";
        public const string MaxHealthUpgradeId = "upgrade.run.iron_gut";
        public const string CritChanceUpgradeId = "upgrade.run.lucky_shot";
        public const string KillHealUpgradeId = "upgrade.run.vampire_lead";
        public const string FirstShotAfterReloadUpgradeId = "upgrade.run.first_shot";
        public const string AmmoPickupUpgradeId = "upgrade.run.ammo_scavenger";
        public const string ChainHitUpgradeId = "upgrade.run.chain_spark";
        public const float ChainHitBaseDamageFraction = 0.35f;
        public const float ChainHitDamageFractionPerExtraStack = 0.10f;
        public const float ChainHitMaxDamageFraction = 0.75f;
        public const float ChainHitSearchRadius = 14f;

        private static readonly RunUpgradeDefinition[] Definitions =
        {
            new RunUpgradeDefinition
            {
                upgradeId = RevolverDamageUpgradeId,
                displayName = "Deadeye Rounds",
                description = "+10% revolver damage this run.",
                effectKind = RunUpgradeEffectKind.RevolverDamagePercent,
                value = 0.10f
            },
            new RunUpgradeDefinition
            {
                upgradeId = ReloadSpeedUpgradeId,
                displayName = "Quick Hands",
                description = "+15% reload speed this run.",
                effectKind = RunUpgradeEffectKind.ReloadSpeedPercent,
                value = 0.15f
            },
            new RunUpgradeDefinition
            {
                upgradeId = MaxHealthUpgradeId,
                displayName = "Iron Gut",
                description = "+10 max health this run.",
                effectKind = RunUpgradeEffectKind.MaxHealthFlat,
                value = 10f
            },
            new RunUpgradeDefinition
            {
                upgradeId = CritChanceUpgradeId,
                displayName = "Lucky Shot",
                description = "+5% critical hit chance this run.",
                effectKind = RunUpgradeEffectKind.CritChanceFlat,
                value = 0.05f
            },
            new RunUpgradeDefinition
            {
                upgradeId = KillHealUpgradeId,
                displayName = "Vampire Lead",
                description = "Real enemy kills heal 3 HP.",
                effectKind = RunUpgradeEffectKind.KillHealFlat,
                value = 3f
            },
            new RunUpgradeDefinition
            {
                upgradeId = FirstShotAfterReloadUpgradeId,
                displayName = "First Shot",
                description = "First shot after reload deals +35% damage.",
                effectKind = RunUpgradeEffectKind.FirstShotAfterReloadPercent,
                value = 0.35f
            },
            new RunUpgradeDefinition
            {
                upgradeId = AmmoPickupUpgradeId,
                displayName = "Ammo Scavenger",
                description = "Ammo pickups add +50% more reserve ammo.",
                effectKind = RunUpgradeEffectKind.AmmoPickupPercent,
                value = 0.50f
            },
            new RunUpgradeDefinition
            {
                upgradeId = ChainHitUpgradeId,
                displayName = "Chain Spark",
                description = "Every 6th weapon hit chains 35% damage to one nearby enemy.",
                effectKind = RunUpgradeEffectKind.EveryNthHitChain,
                triggerEveryNthHit = 6,
                chainDamageFraction = ChainHitBaseDamageFraction
            }
        };

        public static IReadOnlyList<RunUpgradeDefinition> All => Definitions;

        public static bool TryGet(string upgradeId, out RunUpgradeDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(upgradeId))
            {
                return false;
            }

            for (int i = 0; i < Definitions.Length; i++)
            {
                if (string.Equals(Definitions[i].upgradeId, upgradeId, StringComparison.Ordinal))
                {
                    definition = Definitions[i];
                    return true;
                }
            }

            return false;
        }

        public static List<RunUpgradeDefinition> CreateRewardChoicesForFloor(RunState run, int floorIndex, int count = 3)
        {
            int seed = run != null
                ? run.seed ^ (floorIndex * 397) ^ ((run.runUpgrades != null ? run.runUpgrades.Count : 0) * 911)
                : floorIndex * 397;
            return CreateRewardChoices(run, count, seed);
        }

        public static List<RunUpgradeDefinition> CreateRewardChoices(RunState run, int count, int seed)
        {
            List<RunUpgradeDefinition> candidates = new List<RunUpgradeDefinition>(Definitions.Length);
            for (int i = 0; i < Definitions.Length; i++)
            {
                RunUpgradeDefinition definition = Definitions[i];
                if (definition.unique && run != null && run.GetUpgradeStackCount(definition.upgradeId) > 0)
                {
                    continue;
                }

                candidates.Add(definition);
            }

            Shuffle(candidates, seed);
            List<RunUpgradeDefinition> choices = new List<RunUpgradeDefinition>(Mathf.Max(0, count));
            for (int i = 0; i < candidates.Count && choices.Count < count; i++)
            {
                if (!ContainsUpgrade(choices, candidates[i].upgradeId))
                {
                    choices.Add(candidates[i]);
                }
            }

            for (int i = 0; i < Definitions.Length && choices.Count < count; i++)
            {
                if (!ContainsUpgrade(choices, Definitions[i].upgradeId))
                {
                    choices.Add(Definitions[i]);
                }
            }

            return choices;
        }

        public static float GetChainDamageFractionForStack(int stackCount)
        {
            int clampedStack = Mathf.Max(1, stackCount);
            return Mathf.Min(
                ChainHitMaxDamageFraction,
                ChainHitBaseDamageFraction + (clampedStack - 1) * ChainHitDamageFractionPerExtraStack);
        }

        public static string BuildRewardChoiceLabel(RunState run, RunUpgradeDefinition definition)
        {
            if (definition == null)
            {
                return "Unknown upgrade";
            }

            int currentStack = run != null ? run.GetUpgradeStackCount(definition.upgradeId) : 0;
            int nextStack = Mathf.Max(1, currentStack + 1);
            string header = currentStack > 0
                ? $"{definition.displayName} Lv. {currentStack} -> Lv. {nextStack}"
                : $"{definition.displayName} Lv. 1\nNew upgrade";
            return $"{header}\n{BuildStackPreview(definition, currentStack, nextStack)}";
        }

        public static string BuildOwnedUpgradeSummary(RunUpgradeRecord record)
        {
            if (!TryGet(record.upgradeId, out RunUpgradeDefinition definition))
            {
                return $"{record.upgradeId} x{Mathf.Max(1, record.stackCount)}";
            }

            int stackCount = Mathf.Max(1, record.stackCount);
            return $"{definition.displayName} Lv. {stackCount}: {BuildCurrentEffectSummary(definition, stackCount)}";
        }

        private static string BuildStackPreview(RunUpgradeDefinition definition, int currentStack, int nextStack)
        {
            return definition.effectKind switch
            {
                RunUpgradeEffectKind.RevolverDamagePercent => BuildPercentPreview("Revolver damage", definition.value, currentStack, nextStack),
                RunUpgradeEffectKind.ReloadSpeedPercent => BuildPercentPreview("Reload speed", definition.value, currentStack, nextStack),
                RunUpgradeEffectKind.MaxHealthFlat => BuildFlatPreview("Max health", definition.value, currentStack, nextStack),
                RunUpgradeEffectKind.CritChanceFlat => BuildPercentPreview("Crit chance", definition.value, currentStack, nextStack),
                RunUpgradeEffectKind.KillHealFlat => BuildFlatPreview("Kill heal", definition.value, currentStack, nextStack, " HP"),
                RunUpgradeEffectKind.FirstShotAfterReloadPercent => BuildPercentPreview("First shot after reload", definition.value, currentStack, nextStack),
                RunUpgradeEffectKind.AmmoPickupPercent => BuildPercentPreview("Ammo reserve pickup", definition.value, currentStack, nextStack),
                RunUpgradeEffectKind.EveryNthHitChain => BuildChainPreview(currentStack, nextStack),
                _ => definition.description ?? string.Empty
            };
        }

        private static string BuildCurrentEffectSummary(RunUpgradeDefinition definition, int stackCount)
        {
            return definition.effectKind switch
            {
                RunUpgradeEffectKind.RevolverDamagePercent => $"+{definition.value * stackCount * 100f:0.#}% revolver damage",
                RunUpgradeEffectKind.ReloadSpeedPercent => $"+{definition.value * stackCount * 100f:0.#}% reload speed",
                RunUpgradeEffectKind.MaxHealthFlat => $"+{definition.value * stackCount:0.#} max health",
                RunUpgradeEffectKind.CritChanceFlat => $"+{definition.value * stackCount * 100f:0.#}% crit chance",
                RunUpgradeEffectKind.KillHealFlat => $"kills heal {definition.value * stackCount:0.#} HP",
                RunUpgradeEffectKind.FirstShotAfterReloadPercent => $"+{definition.value * stackCount * 100f:0.#}% first shot after reload",
                RunUpgradeEffectKind.AmmoPickupPercent => $"+{definition.value * stackCount * 100f:0.#}% reserve ammo from pickups",
                RunUpgradeEffectKind.EveryNthHitChain => $"every {Mathf.Max(1, definition.triggerEveryNthHit)}th hit chains {GetChainDamageFractionForStack(stackCount) * 100f:0.#}% damage",
                _ => definition.description ?? string.Empty
            };
        }

        private static string BuildPercentPreview(string label, float value, int currentStack, int nextStack)
        {
            return $"{label}: +{value * currentStack * 100f:0.#}% -> +{value * nextStack * 100f:0.#}%";
        }

        private static string BuildFlatPreview(string label, float value, int currentStack, int nextStack, string suffix = "")
        {
            return $"{label}: +{value * currentStack:0.#}{suffix} -> +{value * nextStack:0.#}{suffix}";
        }

        private static string BuildChainPreview(int currentStack, int nextStack)
        {
            float current = currentStack <= 0 ? 0f : GetChainDamageFractionForStack(currentStack);
            float next = GetChainDamageFractionForStack(nextStack);
            return $"Chain damage: {current * 100f:0.#}% -> {next * 100f:0.#}%";
        }

        private static void Shuffle(List<RunUpgradeDefinition> values, int seed)
        {
            System.Random random = new System.Random(seed);
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                RunUpgradeDefinition temp = values[i];
                values[i] = values[swapIndex];
                values[swapIndex] = temp;
            }
        }

        private static bool ContainsUpgrade(List<RunUpgradeDefinition> definitions, string upgradeId)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                if (string.Equals(definitions[i].upgradeId, upgradeId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
