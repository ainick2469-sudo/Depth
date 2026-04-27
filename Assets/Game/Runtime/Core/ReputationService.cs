using System;

namespace FrontierDepths.Core
{
    public enum ReputationTier
    {
        Stranger,
        KnownHand,
        TrustedDelver,
        TownChampion,
        FrontierLegend
    }

    public static class ReputationService
    {
        public const int KnownHandThreshold = 100;
        public const int TrustedDelverThreshold = 250;
        public const int TownChampionThreshold = 500;
        public const int FrontierLegendThreshold = 900;

        public static ReputationTier GetTier(int reputation)
        {
            int value = Math.Max(0, reputation);
            if (value >= FrontierLegendThreshold) return ReputationTier.FrontierLegend;
            if (value >= TownChampionThreshold) return ReputationTier.TownChampion;
            if (value >= TrustedDelverThreshold) return ReputationTier.TrustedDelver;
            if (value >= KnownHandThreshold) return ReputationTier.KnownHand;
            return ReputationTier.Stranger;
        }

        public static string GetTitle(int reputation)
        {
            return GetTier(reputation) switch
            {
                ReputationTier.KnownHand => "Known Hand",
                ReputationTier.TrustedDelver => "Trusted Delver",
                ReputationTier.TownChampion => "Town Champion",
                ReputationTier.FrontierLegend => "Frontier Legend",
                _ => "Stranger"
            };
        }

        public static float GetDiscountPercent(int reputation)
        {
            return GetTier(reputation) switch
            {
                ReputationTier.KnownHand => 0.05f,
                ReputationTier.TrustedDelver => 0.10f,
                ReputationTier.TownChampion => 0.15f,
                ReputationTier.FrontierLegend => 0.20f,
                _ => 0f
            };
        }

        public static int GetDiscountedCost(int baseCost, int reputation)
        {
            if (baseCost <= 0)
            {
                return 0;
            }

            float discount = GetDiscountPercent(reputation);
            return Math.Max(0, (int)Math.Round(baseCost * (1f - discount), MidpointRounding.AwayFromZero));
        }

        public static int GetBountyReputationReward(BountyDefinition bounty)
        {
            if (bounty == null)
            {
                return 0;
            }

            int floorScore = Math.Max(1, bounty.maxFloor) * 4;
            int rewardScore = Math.Max(0, bounty.goldReward / 20);
            return Math.Max(20, floorScore + rewardScore);
        }

        public static int AddReputation(ProfileState profile, int amount)
        {
            if (profile == null || amount <= 0)
            {
                return 0;
            }

            profile.townReputation = Math.Max(0, profile.townReputation + amount);
            return amount;
        }
    }
}
