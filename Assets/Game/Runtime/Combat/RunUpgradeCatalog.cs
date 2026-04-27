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
        public const string BulletPouchUpgradeId = "upgrade.run.bullet_pouch";
        public const string CloseQuartersUpgradeId = "upgrade.run.close_quarters";
        public const string QuickJabUpgradeId = "upgrade.run.quick_jab";
        public const string HotBarrelUpgradeId = "upgrade.run.hot_barrel";
        public const string RangersMarkUpgradeId = "upgrade.run.rangers_mark";
        public const string SupplyLuckUpgradeId = "upgrade.run.supply_luck";
        public const string SteadyAimUpgradeId = "upgrade.run.steady_aim";
        public const string AdrenalineReloadUpgradeId = "upgrade.run.adrenaline_reload";
        public const string HuntersClaimUpgradeId = "upgrade.run.hunters_claim";
        public const string GritUpgradeId = "upgrade.run.grit";
        public const string DashSpursUpgradeId = "upgrade.run.dash_spurs";
        public const string LastRoundUpgradeId = "upgrade.run.last_round";
        public const string TrailblazerUpgradeId = "upgrade.run.trailblazer";
        public const string RoomReaderUpgradeId = "upgrade.run.room_reader";
        public const string RicochetBrandUpgradeId = "upgrade.run.ricochet_brand";
        public const float ChainHitBaseDamageFraction = 0.20f;
        public const float ChainHitDamageFractionPerExtraStack = 0.10f;
        public const float ChainHitMaxDamageFraction = 0.80f;
        public const float ChainHitSearchRadius = 14f;

        private static readonly RunUpgradeDefinition[] Definitions =
        {
            new RunUpgradeDefinition
            {
                upgradeId = RevolverDamageUpgradeId,
                displayName = "Deadeye Rounds",
                description = "+10% revolver damage this run.",
                effectKind = RunUpgradeEffectKind.RevolverDamagePercent,
                category = RunUpgradeCategory.Damage,
                value = 0.10f
            },
            new RunUpgradeDefinition
            {
                upgradeId = ReloadSpeedUpgradeId,
                displayName = "Quick Hands",
                description = "+15% reload speed this run.",
                effectKind = RunUpgradeEffectKind.ReloadSpeedPercent,
                category = RunUpgradeCategory.Reload,
                value = 0.15f
            },
            new RunUpgradeDefinition
            {
                upgradeId = MaxHealthUpgradeId,
                displayName = "Iron Gut",
                description = "+10 max health this run.",
                effectKind = RunUpgradeEffectKind.MaxHealthFlat,
                category = RunUpgradeCategory.Survival,
                value = 10f
            },
            new RunUpgradeDefinition
            {
                upgradeId = CritChanceUpgradeId,
                displayName = "Lucky Shot",
                description = "+5% critical hit chance this run.",
                effectKind = RunUpgradeEffectKind.CritChanceFlat,
                category = RunUpgradeCategory.Damage,
                value = 0.05f
            },
            new RunUpgradeDefinition
            {
                upgradeId = KillHealUpgradeId,
                displayName = "Vampire Lead",
                description = "Real enemy kills heal 3 HP.",
                effectKind = RunUpgradeEffectKind.KillHealFlat,
                category = RunUpgradeCategory.Survival,
                value = 3f
            },
            new RunUpgradeDefinition
            {
                upgradeId = FirstShotAfterReloadUpgradeId,
                displayName = "First Shot",
                description = "First shot after reload deals +35% damage.",
                effectKind = RunUpgradeEffectKind.FirstShotAfterReloadPercent,
                category = RunUpgradeCategory.Damage,
                value = 0.35f
            },
            new RunUpgradeDefinition
            {
                upgradeId = AmmoPickupUpgradeId,
                displayName = "Ammo Scavenger",
                description = "Ammo pickups add +50% more reserve ammo.",
                effectKind = RunUpgradeEffectKind.AmmoPickupPercent,
                category = RunUpgradeCategory.Ammo,
                value = 0.50f
            },
            new RunUpgradeDefinition
            {
                upgradeId = ChainHitUpgradeId,
                displayName = "Chain Spark",
                description = "Every weapon hit chains 20% damage to one nearby enemy.",
                effectKind = RunUpgradeEffectKind.EveryNthHitChain,
                category = RunUpgradeCategory.Chain,
                triggerEveryNthHit = 1,
                chainDamageFraction = ChainHitBaseDamageFraction
            },
            new RunUpgradeDefinition
            {
                upgradeId = BulletPouchUpgradeId,
                displayName = "Bullet Pouch",
                description = "+12 reserve ammo capacity and refill headroom.",
                effectKind = RunUpgradeEffectKind.ReserveAmmoCapacityFlat,
                category = RunUpgradeCategory.Ammo,
                value = 12f
            },
            new RunUpgradeDefinition
            {
                upgradeId = CloseQuartersUpgradeId,
                displayName = "Close Quarters",
                description = "+25% Pistol Whip damage.",
                effectKind = RunUpgradeEffectKind.PistolWhipDamagePercent,
                category = RunUpgradeCategory.Melee,
                value = 0.25f
            },
            new RunUpgradeDefinition
            {
                upgradeId = QuickJabUpgradeId,
                displayName = "Quick Jab",
                description = "-10% Pistol Whip cooldown.",
                effectKind = RunUpgradeEffectKind.PistolWhipCooldownPercent,
                category = RunUpgradeCategory.Melee,
                value = 0.10f
            },
            new RunUpgradeDefinition
            {
                upgradeId = HotBarrelUpgradeId,
                displayName = "Hot Barrel",
                description = "Consecutive shots before reload gain +6% damage each.",
                effectKind = RunUpgradeEffectKind.ConsecutiveShotDamagePercent,
                category = RunUpgradeCategory.Damage,
                value = 0.06f
            },
            new RunUpgradeDefinition
            {
                upgradeId = RangersMarkUpgradeId,
                displayName = "Ranger's Mark",
                description = "+4% critical hit chance.",
                effectKind = RunUpgradeEffectKind.CritChanceFlat,
                category = RunUpgradeCategory.Damage,
                value = 0.04f
            },
            new RunUpgradeDefinition
            {
                upgradeId = SupplyLuckUpgradeId,
                displayName = "Supply Luck",
                description = "+35% reserve ammo from pickups.",
                effectKind = RunUpgradeEffectKind.AmmoPickupPercent,
                category = RunUpgradeCategory.Economy,
                value = 0.35f
            },
            new RunUpgradeDefinition
            {
                upgradeId = SteadyAimUpgradeId,
                displayName = "Steady Aim",
                description = "+3% critical hit chance.",
                effectKind = RunUpgradeEffectKind.CritChanceFlat,
                category = RunUpgradeCategory.Damage,
                value = 0.03f
            },
            new RunUpgradeDefinition
            {
                upgradeId = AdrenalineReloadUpgradeId,
                displayName = "Adrenaline Reload",
                description = "+25% reload speed while below 40% HP.",
                effectKind = RunUpgradeEffectKind.LowHealthReloadPercent,
                category = RunUpgradeCategory.Reload,
                value = 0.25f
            },
            new RunUpgradeDefinition
            {
                upgradeId = HuntersClaimUpgradeId,
                displayName = "Hunter's Claim",
                description = "Bounty and elite kills grant +10 gold and +3 reputation.",
                effectKind = RunUpgradeEffectKind.EliteBountyRewardFlat,
                category = RunUpgradeCategory.Bounty,
                value = 1f
            },
            new RunUpgradeDefinition
            {
                upgradeId = GritUpgradeId,
                displayName = "Grit",
                description = "Once per floor, lethal damage leaves you at 1 HP.",
                effectKind = RunUpgradeEffectKind.LethalSavePerFloor,
                category = RunUpgradeCategory.Survival,
                unique = true,
                value = 1f
            },
            new RunUpgradeDefinition
            {
                upgradeId = DashSpursUpgradeId,
                displayName = "Dash Spurs",
                description = "-15% dash cooldown.",
                effectKind = RunUpgradeEffectKind.DashCooldownPercent,
                category = RunUpgradeCategory.Mobility,
                value = 0.15f
            },
            new RunUpgradeDefinition
            {
                upgradeId = LastRoundUpgradeId,
                displayName = "Last Round",
                description = "Final bullet in the magazine deals +45% damage.",
                effectKind = RunUpgradeEffectKind.LastRoundDamagePercent,
                category = RunUpgradeCategory.Damage,
                value = 0.45f
            },
            new RunUpgradeDefinition
            {
                upgradeId = TrailblazerUpgradeId,
                displayName = "Trailblazer",
                description = "Enemy kills grant +20% movement speed for 2 seconds.",
                effectKind = RunUpgradeEffectKind.MoveSpeedAfterKillPercent,
                category = RunUpgradeCategory.Mobility,
                value = 0.20f
            },
            new RunUpgradeDefinition
            {
                upgradeId = RoomReaderUpgradeId,
                displayName = "Room Reader",
                description = "Scout rooms reveal one extra special room.",
                effectKind = RunUpgradeEffectKind.ScoutRevealBonus,
                category = RunUpgradeCategory.RoomMap,
                value = 1f
            },
            new RunUpgradeDefinition
            {
                upgradeId = RicochetBrandUpgradeId,
                displayName = "Ricochet Brand",
                description = "+2m Chain Spark search range.",
                effectKind = RunUpgradeEffectKind.ChainRangeFlat,
                category = RunUpgradeCategory.Chain,
                value = 2f
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
                if (!ContainsUpgrade(choices, candidates[i].upgradeId) && !ContainsCategory(choices, candidates[i].category))
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
                RunUpgradeEffectKind.PistolWhipDamagePercent => BuildPercentPreview("Pistol Whip damage", definition.value, currentStack, nextStack),
                RunUpgradeEffectKind.PistolWhipCooldownPercent => BuildPercentPreview("Pistol Whip cooldown reduction", definition.value, currentStack, nextStack),
                RunUpgradeEffectKind.ReserveAmmoCapacityFlat => BuildFlatPreview("Reserve ammo capacity", definition.value, currentStack, nextStack),
                RunUpgradeEffectKind.DashCooldownPercent => BuildPercentPreview("Dash cooldown reduction", definition.value, currentStack, nextStack),
                RunUpgradeEffectKind.LastRoundDamagePercent => BuildPercentPreview("Last round damage", definition.value, currentStack, nextStack),
                RunUpgradeEffectKind.ConsecutiveShotDamagePercent => BuildPercentPreview("Consecutive shot damage", definition.value, currentStack, nextStack),
                RunUpgradeEffectKind.LowHealthReloadPercent => BuildPercentPreview("Low-health reload speed", definition.value, currentStack, nextStack),
                RunUpgradeEffectKind.EliteBountyRewardFlat => BuildFlatPreview("Elite/bounty reward stack", definition.value, currentStack, nextStack),
                RunUpgradeEffectKind.LethalSavePerFloor => currentStack > 0 ? "Lethal save already unlocked" : "Unlock once-per-floor lethal save",
                RunUpgradeEffectKind.ScoutRevealBonus => BuildFlatPreview("Extra scout reveals", definition.value, currentStack, nextStack),
                RunUpgradeEffectKind.ChainRangeFlat => BuildFlatPreview("Chain Spark range", definition.value, currentStack, nextStack, "m"),
                RunUpgradeEffectKind.MoveSpeedAfterKillPercent => BuildPercentPreview("Move speed after kill", definition.value, currentStack, nextStack),
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
                RunUpgradeEffectKind.EveryNthHitChain => $"every hit chains {GetChainDamageFractionForStack(stackCount) * 100f:0.#}% damage",
                RunUpgradeEffectKind.PistolWhipDamagePercent => $"+{definition.value * stackCount * 100f:0.#}% Pistol Whip damage",
                RunUpgradeEffectKind.PistolWhipCooldownPercent => $"-{Mathf.Min(75f, definition.value * stackCount * 100f):0.#}% Pistol Whip cooldown",
                RunUpgradeEffectKind.ReserveAmmoCapacityFlat => $"+{definition.value * stackCount:0.#} reserve ammo capacity",
                RunUpgradeEffectKind.DashCooldownPercent => $"-{Mathf.Min(75f, definition.value * stackCount * 100f):0.#}% dash cooldown",
                RunUpgradeEffectKind.LastRoundDamagePercent => $"+{definition.value * stackCount * 100f:0.#}% final bullet damage",
                RunUpgradeEffectKind.ConsecutiveShotDamagePercent => $"+{definition.value * stackCount * 100f:0.#}% damage per consecutive shot",
                RunUpgradeEffectKind.LowHealthReloadPercent => $"+{definition.value * stackCount * 100f:0.#}% reload speed below 40% HP",
                RunUpgradeEffectKind.EliteBountyRewardFlat => $"+{10 * stackCount}g and +{3 * stackCount} rep from elite/bounty kills",
                RunUpgradeEffectKind.LethalSavePerFloor => "once-per-floor lethal save",
                RunUpgradeEffectKind.ScoutRevealBonus => $"+{stackCount} extra scout reveals",
                RunUpgradeEffectKind.ChainRangeFlat => $"+{definition.value * stackCount:0.#}m Chain Spark range",
                RunUpgradeEffectKind.MoveSpeedAfterKillPercent => $"+{definition.value * stackCount * 100f:0.#}% move speed after kill",
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

        private static bool ContainsCategory(List<RunUpgradeDefinition> definitions, RunUpgradeCategory category)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i].category == category)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
