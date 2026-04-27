using System.Collections.Generic;
using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.Combat
{
    public readonly struct RunStatSnapshot
    {
        public RunStatSnapshot(
            float revolverDamagePercent,
            float reloadSpeedPercent,
            float maxHealthFlat,
            float critChanceFlat,
            float killHealFlat,
            float firstShotAfterReloadPercent,
            float ammoPickupPercent,
            float pistolWhipDamagePercent,
            float pistolWhipCooldownPercent,
            float reserveAmmoCapacityFlat,
            float dashCooldownPercent,
            float lastRoundDamagePercent,
            float consecutiveShotDamagePercent,
            float lowHealthReloadPercent,
            float eliteBountyRewardFlat,
            bool hasLethalSavePerFloor,
            int scoutRevealBonus,
            float chainRangeFlat,
            float moveSpeedAfterKillPercent,
            int chainEveryNthHit,
            float chainDamageFraction,
            IReadOnlyList<RunStatModifierContribution> modifiers)
        {
            this.revolverDamagePercent = revolverDamagePercent;
            this.reloadSpeedPercent = reloadSpeedPercent;
            this.maxHealthFlat = maxHealthFlat;
            this.critChanceFlat = critChanceFlat;
            this.killHealFlat = killHealFlat;
            this.firstShotAfterReloadPercent = firstShotAfterReloadPercent;
            this.ammoPickupPercent = ammoPickupPercent;
            this.pistolWhipDamagePercent = pistolWhipDamagePercent;
            this.pistolWhipCooldownPercent = pistolWhipCooldownPercent;
            this.reserveAmmoCapacityFlat = reserveAmmoCapacityFlat;
            this.dashCooldownPercent = dashCooldownPercent;
            this.lastRoundDamagePercent = lastRoundDamagePercent;
            this.consecutiveShotDamagePercent = consecutiveShotDamagePercent;
            this.lowHealthReloadPercent = lowHealthReloadPercent;
            this.eliteBountyRewardFlat = eliteBountyRewardFlat;
            this.hasLethalSavePerFloor = hasLethalSavePerFloor;
            this.scoutRevealBonus = scoutRevealBonus;
            this.chainRangeFlat = chainRangeFlat;
            this.moveSpeedAfterKillPercent = moveSpeedAfterKillPercent;
            this.chainEveryNthHit = chainEveryNthHit;
            this.chainDamageFraction = chainDamageFraction;
            this.modifiers = modifiers;
        }

        public readonly float revolverDamagePercent;
        public readonly float reloadSpeedPercent;
        public readonly float maxHealthFlat;
        public readonly float critChanceFlat;
        public readonly float killHealFlat;
        public readonly float firstShotAfterReloadPercent;
        public readonly float ammoPickupPercent;
        public readonly float pistolWhipDamagePercent;
        public readonly float pistolWhipCooldownPercent;
        public readonly float reserveAmmoCapacityFlat;
        public readonly float dashCooldownPercent;
        public readonly float lastRoundDamagePercent;
        public readonly float consecutiveShotDamagePercent;
        public readonly float lowHealthReloadPercent;
        public readonly float eliteBountyRewardFlat;
        public readonly bool hasLethalSavePerFloor;
        public readonly int scoutRevealBonus;
        public readonly float chainRangeFlat;
        public readonly float moveSpeedAfterKillPercent;
        public readonly int chainEveryNthHit;
        public readonly float chainDamageFraction;
        public readonly IReadOnlyList<RunStatModifierContribution> modifiers;

        public float RevolverDamageMultiplier => 1f + Mathf.Max(0f, revolverDamagePercent);
        public float ReloadSpeedMultiplier => 1f + Mathf.Max(0f, reloadSpeedPercent);
        public float CritChanceBonus => Mathf.Max(0f, critChanceFlat);
        public float FirstShotAfterReloadMultiplier => 1f + Mathf.Max(0f, firstShotAfterReloadPercent);
        public float AmmoPickupMultiplier => 1f + Mathf.Max(0f, ammoPickupPercent);
        public float PistolWhipDamageMultiplier => 1f + Mathf.Max(0f, pistolWhipDamagePercent);
        public float PistolWhipCooldownMultiplier => Mathf.Max(0.25f, 1f - Mathf.Max(0f, pistolWhipCooldownPercent));
        public float DashCooldownMultiplier => Mathf.Max(0.25f, 1f - Mathf.Max(0f, dashCooldownPercent));
        public float LowHealthReloadMultiplier => 1f + Mathf.Max(0f, lowHealthReloadPercent);
        public int KillHealAmount => Mathf.Max(0, Mathf.RoundToInt(killHealFlat));
        public bool HasFirstShotAfterReloadBonus => firstShotAfterReloadPercent > 0f;
        public bool HasChainHit => chainEveryNthHit > 0 && chainDamageFraction > 0f;
    }

    public static class RunStatAggregator
    {
        public const float CriticalHitMultiplier = 1.5f;

        private static bool hasOverrideForTests;
        private static RunStatSnapshot overrideForTests;

        public static RunStatSnapshot Current => hasOverrideForTests
            ? overrideForTests
            : Build(GameBootstrap.Instance != null && GameBootstrap.Instance.RunService != null
                ? GameBootstrap.Instance.RunService.Current
                : null);

        internal static void SetOverrideForTests(RunStatSnapshot snapshot)
        {
            hasOverrideForTests = true;
            overrideForTests = snapshot;
        }

        internal static void ClearOverrideForTests()
        {
            hasOverrideForTests = false;
            overrideForTests = default;
        }

        public static RunStatSnapshot Build(RunState run)
        {
            float revolverDamagePercent = 0f;
            float reloadSpeedPercent = 0f;
            float maxHealthFlat = 0f;
            float critChanceFlat = 0f;
            float killHealFlat = 0f;
            float firstShotAfterReloadPercent = 0f;
            float ammoPickupPercent = 0f;
            float pistolWhipDamagePercent = 0f;
            float pistolWhipCooldownPercent = 0f;
            float reserveAmmoCapacityFlat = 0f;
            float dashCooldownPercent = 0f;
            float lastRoundDamagePercent = 0f;
            float consecutiveShotDamagePercent = 0f;
            float lowHealthReloadPercent = 0f;
            float eliteBountyRewardFlat = 0f;
            bool hasLethalSavePerFloor = false;
            int scoutRevealBonus = 0;
            float chainRangeFlat = 0f;
            float moveSpeedAfterKillPercent = 0f;
            int chainEveryNthHit = 0;
            float chainDamageFraction = 0f;
            List<RunStatModifierContribution> modifiers = new List<RunStatModifierContribution>();

            if (run != null && run.runUpgrades != null)
            {
                for (int i = 0; i < run.runUpgrades.Count; i++)
                {
                    RunUpgradeRecord record = run.runUpgrades[i];
                    if (!RunUpgradeCatalog.TryGet(record.upgradeId, out RunUpgradeDefinition definition))
                    {
                        continue;
                    }

                    int stackCount = Mathf.Max(1, record.stackCount);
                    float stackedValue = definition.value * stackCount;
                    switch (definition.effectKind)
                    {
                        case RunUpgradeEffectKind.RevolverDamagePercent:
                            revolverDamagePercent += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.RevolverDamage, 0f, stackedValue, stackCount));
                            break;
                        case RunUpgradeEffectKind.ReloadSpeedPercent:
                            reloadSpeedPercent += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.ReloadSpeed, 0f, stackedValue, stackCount));
                            break;
                        case RunUpgradeEffectKind.MaxHealthFlat:
                            maxHealthFlat += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.MaxHealth, stackedValue, 0f, stackCount));
                            break;
                        case RunUpgradeEffectKind.CritChanceFlat:
                            critChanceFlat += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.CritChance, stackedValue, 0f, stackCount));
                            break;
                        case RunUpgradeEffectKind.KillHealFlat:
                            killHealFlat += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.KillHeal, stackedValue, 0f, stackCount));
                            break;
                        case RunUpgradeEffectKind.FirstShotAfterReloadPercent:
                            firstShotAfterReloadPercent += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.FirstShotAfterReload, 0f, stackedValue, stackCount));
                            break;
                        case RunUpgradeEffectKind.AmmoPickupPercent:
                            ammoPickupPercent += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.AmmoPickup, 0f, stackedValue, stackCount));
                            break;
                        case RunUpgradeEffectKind.EveryNthHitChain:
                            chainEveryNthHit = 1;
                            float stackedChainFraction = RunUpgradeCatalog.GetChainDamageFractionForStack(stackCount);
                            chainDamageFraction = Mathf.Max(chainDamageFraction, stackedChainFraction);
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.ChainHit, 0f, stackedChainFraction, stackCount));
                            break;
                        case RunUpgradeEffectKind.PistolWhipDamagePercent:
                            pistolWhipDamagePercent += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.PistolWhipDamage, 0f, stackedValue, stackCount));
                            break;
                        case RunUpgradeEffectKind.PistolWhipCooldownPercent:
                            pistolWhipCooldownPercent += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.PistolWhipCooldown, 0f, stackedValue, stackCount));
                            break;
                        case RunUpgradeEffectKind.ReserveAmmoCapacityFlat:
                            reserveAmmoCapacityFlat += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.ReserveAmmoCapacity, stackedValue, 0f, stackCount));
                            break;
                        case RunUpgradeEffectKind.DashCooldownPercent:
                            dashCooldownPercent += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.DashCooldown, 0f, stackedValue, stackCount));
                            break;
                        case RunUpgradeEffectKind.LastRoundDamagePercent:
                            lastRoundDamagePercent += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.LastRoundDamage, 0f, stackedValue, stackCount));
                            break;
                        case RunUpgradeEffectKind.ConsecutiveShotDamagePercent:
                            consecutiveShotDamagePercent += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.ConsecutiveShotDamage, 0f, stackedValue, stackCount));
                            break;
                        case RunUpgradeEffectKind.LowHealthReloadPercent:
                            lowHealthReloadPercent += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.LowHealthReload, 0f, stackedValue, stackCount));
                            break;
                        case RunUpgradeEffectKind.EliteBountyRewardFlat:
                            eliteBountyRewardFlat += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.EliteBountyReward, stackedValue, 0f, stackCount));
                            break;
                        case RunUpgradeEffectKind.LethalSavePerFloor:
                            hasLethalSavePerFloor = true;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.LethalSave, 1f, 0f, stackCount));
                            break;
                        case RunUpgradeEffectKind.ScoutRevealBonus:
                            scoutRevealBonus += Mathf.RoundToInt(stackedValue);
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.ScoutReveal, stackedValue, 0f, stackCount));
                            break;
                        case RunUpgradeEffectKind.ChainRangeFlat:
                            chainRangeFlat += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.ChainRange, stackedValue, 0f, stackCount));
                            break;
                        case RunUpgradeEffectKind.MoveSpeedAfterKillPercent:
                            moveSpeedAfterKillPercent += stackedValue;
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.MoveSpeedAfterKill, 0f, stackedValue, stackCount));
                            break;
                    }
                }
            }

            return new RunStatSnapshot(
                revolverDamagePercent,
                reloadSpeedPercent,
                maxHealthFlat,
                critChanceFlat,
                killHealFlat,
                firstShotAfterReloadPercent,
                ammoPickupPercent,
                pistolWhipDamagePercent,
                pistolWhipCooldownPercent,
                reserveAmmoCapacityFlat,
                dashCooldownPercent,
                lastRoundDamagePercent,
                consecutiveShotDamagePercent,
                lowHealthReloadPercent,
                eliteBountyRewardFlat,
                hasLethalSavePerFloor,
                scoutRevealBonus,
                chainRangeFlat,
                moveSpeedAfterKillPercent,
                chainEveryNthHit,
                chainDamageFraction,
                modifiers);
        }
    }
}
