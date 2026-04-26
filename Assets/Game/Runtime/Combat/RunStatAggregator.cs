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
        public readonly int chainEveryNthHit;
        public readonly float chainDamageFraction;
        public readonly IReadOnlyList<RunStatModifierContribution> modifiers;

        public float RevolverDamageMultiplier => 1f + Mathf.Max(0f, revolverDamagePercent);
        public float ReloadSpeedMultiplier => 1f + Mathf.Max(0f, reloadSpeedPercent);
        public float CritChanceBonus => Mathf.Max(0f, critChanceFlat);
        public float FirstShotAfterReloadMultiplier => 1f + Mathf.Max(0f, firstShotAfterReloadPercent);
        public float AmmoPickupMultiplier => 1f + Mathf.Max(0f, ammoPickupPercent);
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
                            chainEveryNthHit = chainEveryNthHit == 0
                                ? definition.triggerEveryNthHit
                                : Mathf.Min(chainEveryNthHit, definition.triggerEveryNthHit);
                            chainDamageFraction = Mathf.Max(chainDamageFraction, definition.chainDamageFraction);
                            modifiers.Add(new RunStatModifierContribution(definition.SourceId, RunStatId.ChainHit, 0f, definition.chainDamageFraction, stackCount));
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
                chainEveryNthHit,
                chainDamageFraction,
                modifiers);
        }
    }
}
