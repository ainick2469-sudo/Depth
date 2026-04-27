using System;

namespace FrontierDepths.Combat
{
    public enum RunUpgradeEffectKind
    {
        RevolverDamagePercent,
        ReloadSpeedPercent,
        MaxHealthFlat,
        CritChanceFlat,
        KillHealFlat,
        FirstShotAfterReloadPercent,
        AmmoPickupPercent,
        EveryNthHitChain,
        PistolWhipDamagePercent,
        PistolWhipCooldownPercent,
        ReserveAmmoCapacityFlat,
        DashCooldownPercent,
        LastRoundDamagePercent,
        ConsecutiveShotDamagePercent,
        LowHealthReloadPercent,
        EliteBountyRewardFlat,
        LethalSavePerFloor,
        ScoutRevealBonus,
        ChainRangeFlat,
        MoveSpeedAfterKillPercent
    }

    public enum RunUpgradeCategory
    {
        Damage,
        Ammo,
        Reload,
        Survival,
        Mobility,
        Melee,
        Bounty,
        RoomMap,
        Chain,
        Economy
    }

    public enum RunStatId
    {
        RevolverDamage,
        ReloadSpeed,
        MaxHealth,
        CritChance,
        KillHeal,
        FirstShotAfterReload,
        AmmoPickup,
        ChainHit,
        PistolWhipDamage,
        PistolWhipCooldown,
        ReserveAmmoCapacity,
        DashCooldown,
        LastRoundDamage,
        ConsecutiveShotDamage,
        LowHealthReload,
        EliteBountyReward,
        LethalSave,
        ScoutReveal,
        ChainRange,
        MoveSpeedAfterKill
    }

    [Serializable]
    public sealed class RunUpgradeDefinition
    {
        public string upgradeId;
        public string displayName;
        public string description;
        public RunUpgradeEffectKind effectKind;
        public RunUpgradeCategory category;
        public bool unique;
        public float value;
        public int triggerEveryNthHit;
        public float chainDamageFraction;

        public string SourceId => upgradeId;
    }

    public readonly struct RunStatModifierContribution
    {
        public RunStatModifierContribution(string sourceId, RunStatId statId, float flatBonus, float additivePercent, int stackCount)
        {
            this.sourceId = sourceId;
            this.statId = statId;
            this.flatBonus = flatBonus;
            this.additivePercent = additivePercent;
            this.stackCount = stackCount;
        }

        public readonly string sourceId;
        public readonly RunStatId statId;
        public readonly float flatBonus;
        public readonly float additivePercent;
        public readonly int stackCount;
    }
}
