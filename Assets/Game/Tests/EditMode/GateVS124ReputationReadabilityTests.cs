using System.Collections.Generic;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS124ReputationReadabilityTests
    {
        [Test]
        public void ReputationService_ComputesTierTitlesAndDiscounts()
        {
            Assert.AreEqual("Stranger", ReputationService.GetTitle(0));
            Assert.AreEqual("Known Hand", ReputationService.GetTitle(100));
            Assert.AreEqual("Trusted Delver", ReputationService.GetTitle(250));
            Assert.AreEqual("Town Champion", ReputationService.GetTitle(500));
            Assert.AreEqual("Frontier Legend", ReputationService.GetTitle(900));
            Assert.AreEqual(90, ReputationService.GetDiscountedCost(100, 250));
        }

        [Test]
        public void BountyVisibility_UsesReputationGateButKeepsAcceptedBountiesVisible()
        {
            ProfileState profile = new ProfileState();
            profile.Normalize();
            BountyDefinition hollowBrute = BountyCatalog.Get("bounty.hollow_brute");

            Assert.IsFalse(BountyCatalog.IsVisible(profile, hollowBrute));
            profile.townReputation = ReputationService.KnownHandThreshold;
            Assert.IsTrue(BountyCatalog.IsVisible(profile, hollowBrute));

            profile.townReputation = 0;
            Assert.IsTrue(BountyObjectiveTracker.MarkAccepted(profile, hollowBrute.bountyId, out _));
            Assert.IsTrue(BountyCatalog.IsVisible(profile, hollowBrute));
        }

        [Test]
        public void RoomPurposeCatalog_RespectsEarlyFloorCaps()
        {
            RoomPurposeDefinition teal = RoomPurposeCatalog.Get("teal_scout");
            Dictionary<string, int> counts = new Dictionary<string, int>
            {
                { teal.purposeId, 1 }
            };

            Assert.IsFalse(RoomPurposeCatalog.IsUnderFloorCap(teal, 7, counts));
            Assert.IsFalse(RoomPurposeCatalog.IsUnderFloorCap(teal, 14, counts));
            Assert.IsTrue(RoomPurposeCatalog.IsUnderFloorCap(teal, 20, counts));
        }

        [Test]
        public void RunUpgradeCatalog_ChainSparkIsAlwaysOnAtTwentyPercent()
        {
            RunState run = new RunState();
            run.Normalize();
            run.AddOrStackUpgrade(RunUpgradeCatalog.ChainHitUpgradeId);

            RunStatSnapshot stats = RunStatAggregator.Build(run);

            Assert.IsTrue(stats.HasChainHit);
            Assert.AreEqual(1, stats.chainEveryNthHit);
            Assert.AreEqual(0.20f, stats.chainDamageFraction, 0.001f);
        }

        [Test]
        public void RunUpgradeCatalog_NewRewardsAllUseImplementedEffects()
        {
            Assert.GreaterOrEqual(RunUpgradeCatalog.All.Count, 18);
            for (int i = 0; i < RunUpgradeCatalog.All.Count; i++)
            {
                RunUpgradeDefinition definition = RunUpgradeCatalog.All[i];
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.upgradeId));
                Assert.IsFalse(string.IsNullOrWhiteSpace(definition.displayName));
                Assert.IsTrue(IsImplemented(definition.effectKind), definition.displayName);
            }
        }

        [Test]
        public void EnemyLevelDefinition_ScalesCloneWithoutMutatingBaseDefinition()
        {
            EnemyDefinition baseDefinition = EnemyCatalog.CreateDefinition(EnemyArchetype.Slime);
            float baseHealth = baseDefinition.maxHealth;
            float baseDamage = baseDefinition.attackDamage;

            EnemyDefinition leveled = EnemyVariantCatalog.CreateLeveledDefinition(baseDefinition, 8);

            Assert.Greater(leveled.maxHealth, baseHealth);
            Assert.Greater(leveled.attackDamage, baseDamage);
            Assert.AreEqual(baseHealth, baseDefinition.maxHealth);
            Assert.AreEqual(baseDamage, baseDefinition.attackDamage);
            Assert.IsTrue(leveled.displayName.Contains("Lv. 8"));
        }

        [Test]
        public void WorldLabelBillboard_StoresOwnerOcclusionRoot()
        {
            GameObject owner = new GameObject("Owner");
            try
            {
                WorldLabelBillboard label = WorldLabelBillboard.Create(owner.transform, "Label", "Shop", Vector3.up, Color.white, 12f, true);
                Assert.AreEqual(owner.transform, label.OcclusionRoot);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        private static bool IsImplemented(RunUpgradeEffectKind kind)
        {
            return kind == RunUpgradeEffectKind.RevolverDamagePercent ||
                   kind == RunUpgradeEffectKind.ReloadSpeedPercent ||
                   kind == RunUpgradeEffectKind.MaxHealthFlat ||
                   kind == RunUpgradeEffectKind.CritChanceFlat ||
                   kind == RunUpgradeEffectKind.KillHealFlat ||
                   kind == RunUpgradeEffectKind.FirstShotAfterReloadPercent ||
                   kind == RunUpgradeEffectKind.AmmoPickupPercent ||
                   kind == RunUpgradeEffectKind.EveryNthHitChain ||
                   kind == RunUpgradeEffectKind.PistolWhipDamagePercent ||
                   kind == RunUpgradeEffectKind.PistolWhipCooldownPercent;
        }
    }
}
