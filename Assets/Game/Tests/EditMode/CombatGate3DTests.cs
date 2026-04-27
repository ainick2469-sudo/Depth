using System.Collections.Generic;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class CombatGate3DTests
    {
        [SetUp]
        public void SetUp()
        {
            GameplayEventBus.ClearForTests();
            RunStatAggregator.ClearOverrideForTests();
            PlayerWeaponController.CritRollProviderForTests = null;
        }

        [TearDown]
        public void TearDown()
        {
            GameplayEventBus.ClearForTests();
            RunStatAggregator.ClearOverrideForTests();
            PlayerWeaponController.CritRollProviderForTests = null;
            DestroyRuntimeFeedbackRoot();
        }

        [Test]
        public void RunState_StacksUpgradesAndPersistsFloorRewardFlag()
        {
            RunState run = CreateRunState();

            run.AddOrStackUpgrade(RunUpgradeCatalog.RevolverDamageUpgradeId);
            run.AddOrStackUpgrade(RunUpgradeCatalog.RevolverDamageUpgradeId);
            run.currentFloor.rewardGranted = true;
            run.Normalize();

            Assert.AreEqual(2, run.GetUpgradeStackCount(RunUpgradeCatalog.RevolverDamageUpgradeId));
            FloorState visited = run.GetVisitedFloor(1);
            Assert.NotNull(visited);
            Assert.IsTrue(visited.rewardGranted);
        }

        [Test]
        public void RewardCatalog_CreatesThreeNonDuplicateChoices()
        {
            RunState run = CreateRunState();

            List<RunUpgradeDefinition> choices = RunUpgradeCatalog.CreateRewardChoicesForFloor(run, 1);

            Assert.AreEqual(3, choices.Count);
            Assert.GreaterOrEqual(RunUpgradeCatalog.All.Count, 18);
            Assert.AreEqual(3, new HashSet<string>(choices.ConvertAll(choice => choice.upgradeId)).Count);
        }

        [Test]
        public void RewardChoice_TriggersOnlyForUnrewardedNormalDescent()
        {
            RunState run = CreateRunState();

            Assert.IsTrue(DungeonRewardChoiceController.ShouldOfferDescentReward(run, true));
            Assert.IsFalse(DungeonRewardChoiceController.ShouldOfferDescentReward(run, false));

            run.currentFloor.rewardGranted = true;
            Assert.IsFalse(DungeonRewardChoiceController.ShouldOfferDescentReward(run, true));

            run.currentFloor.rewardGranted = false;
            run.isActive = false;
            Assert.IsFalse(DungeonRewardChoiceController.ShouldOfferDescentReward(run, true));
        }

        [Test]
        public void RewardChoice_SelectionAddsUpgradeAndMarksFloorRewarded()
        {
            RunState run = CreateRunState();
            RunUpgradeDefinition selected = RunUpgradeCatalog.All[0];

            Assert.IsTrue(DungeonRewardChoiceController.TryApplySelectionForTests(run, selected));

            Assert.AreEqual(1, run.GetUpgradeStackCount(selected.upgradeId));
            Assert.IsTrue(run.currentFloor.rewardGranted);
            Assert.IsTrue(run.GetVisitedFloor(1).rewardGranted);
        }

        [Test]
        public void StatAggregator_StacksPercentBonusesAdditivelyWithSourceIds()
        {
            RunState run = CreateRunState();
            run.AddOrStackUpgrade(RunUpgradeCatalog.RevolverDamageUpgradeId);
            run.AddOrStackUpgrade(RunUpgradeCatalog.RevolverDamageUpgradeId);
            run.AddOrStackUpgrade(RunUpgradeCatalog.ReloadSpeedUpgradeId);

            RunStatSnapshot snapshot = RunStatAggregator.Build(run);

            Assert.AreEqual(0.20f, snapshot.revolverDamagePercent, 0.0001f);
            Assert.AreEqual(0.15f, snapshot.reloadSpeedPercent, 0.0001f);
            Assert.AreEqual(1.20f, snapshot.RevolverDamageMultiplier, 0.0001f);
            Assert.AreEqual(2, snapshot.modifiers.Count);
            Assert.IsTrue(ContainsSource(snapshot.modifiers, RunUpgradeCatalog.RevolverDamageUpgradeId));
            Assert.AreEqual(2, FindContribution(snapshot.modifiers, RunUpgradeCatalog.RevolverDamageUpgradeId).stackCount);
        }

        [Test]
        public void PlayerWeapon_DamageUpgradeAndCritUseDeterministicRoll()
        {
            RunState run = CreateRunState();
            run.AddOrStackUpgrade(RunUpgradeCatalog.RevolverDamageUpgradeId);
            run.AddOrStackUpgrade(RunUpgradeCatalog.CritChanceUpgradeId);
            RunStatAggregator.SetOverrideForTests(RunStatAggregator.Build(run));
            PlayerWeaponController.CritRollProviderForTests = () => 0f;

            GameObject player = new GameObject("CritWeaponPlayer");
            try
            {
                PlayerWeaponController weapon = player.AddComponent<PlayerWeaponController>();
                DamageInfo damageInfo = weapon.CreateDamageInfoForTests(Vector3.zero, Vector3.up);

                Assert.IsTrue(damageInfo.isCritical);
                Assert.AreEqual(15f * 1.10f * RunStatAggregator.CriticalHitMultiplier, damageInfo.amount, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PlayerWeapon_FirstShotAfterReloadConsumesOnFire()
        {
            RunState run = CreateRunState();
            run.AddOrStackUpgrade(RunUpgradeCatalog.FirstShotAfterReloadUpgradeId);
            RunStatAggregator.SetOverrideForTests(RunStatAggregator.Build(run));

            GameObject player = new GameObject("FirstShotPlayer");
            try
            {
                player.AddComponent<PlayerHealth>();
                PlayerWeaponController weapon = player.AddComponent<PlayerWeaponController>();
                Assert.IsTrue(weapon.TryFire(0f));
                Assert.IsTrue(weapon.TryStartReload(1f));
                Assert.IsTrue(weapon.TickReloadCompletion(3f));
                Assert.IsTrue(weapon.IsFirstShotAfterReloadReadyForTests);

                Assert.IsTrue(weapon.TryFire(3.2f));
                Assert.IsFalse(weapon.IsFirstShotAfterReloadReadyForTests);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void AmmoPickup_UsesRunAmmoMultiplierWithoutOverfilling()
        {
            RunState run = CreateRunState();
            run.AddOrStackUpgrade(RunUpgradeCatalog.AmmoPickupUpgradeId);
            RunStatAggregator.SetOverrideForTests(RunStatAggregator.Build(run));

            GameObject player = new GameObject("AmmoMultiplierPlayer");
            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                player.AddComponent<PlayerHealth>();
                PlayerWeaponController weapon = player.AddComponent<PlayerWeaponController>();
                for (int i = 0; i < weapon.MagazineSize; i++)
                {
                    Assert.IsTrue(weapon.TryFire(i * 0.5f));
                }

                int reserveBefore = weapon.ReserveAmmo;
                AmmoPickup pickup = pickupObject.AddComponent<AmmoPickup>();
                pickup.Configure(2);

                Assert.IsTrue(pickup.ApplyToPlayer(player));
                Assert.AreEqual(0, weapon.CurrentAmmo);
                Assert.AreEqual(Mathf.Min(weapon.MaxReserveAmmo, reserveBefore + 3), weapon.ReserveAmmo);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(pickupObject);
            }
        }

        [Test]
        public void KillHeal_HealsOnlyRealPlayerEnemyKills()
        {
            RunState run = CreateRunState();
            run.AddOrStackUpgrade(RunUpgradeCatalog.KillHealUpgradeId);
            RunStatAggregator.SetOverrideForTests(RunStatAggregator.Build(run));
            Assert.AreEqual(3, RunStatAggregator.Current.KillHealAmount);

            GameObject player = new GameObject("KillHealPlayer");
            GameObject enemy = new GameObject("KillHealEnemy");
            try
            {
                PlayerHealth health = player.AddComponent<PlayerHealth>();
                PlayerWeaponController weapon = player.AddComponent<PlayerWeaponController>();
                health.ApplyDamage(new DamageInfo { amount = 20f, source = enemy, damageType = DamageType.Physical }, 1f);

                weapon.HandleGameplayEventForTests(new GameplayEvent
                {
                    eventType = GameplayEventType.EnemyKilled,
                    sourceObject = weapon.gameObject,
                    targetObject = enemy,
                    floorIndex = 1
                });

                Assert.AreEqual(83f, health.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void ChainHit_EverySixthEnemyHitDamagesOneNearbyLivingEnemy()
        {
            RunState run = CreateRunState();
            run.AddOrStackUpgrade(RunUpgradeCatalog.ChainHitUpgradeId);
            RunStatAggregator.SetOverrideForTests(RunStatAggregator.Build(run));

            GameObject player = new GameObject("ChainPlayer");
            GameObject original = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            GameObject nearby = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            GameObject distant = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                PlayerWeaponController weapon = player.AddComponent<PlayerWeaponController>();
                EnemyHealth originalHealth = original.AddComponent<EnemyHealth>();
                originalHealth.Configure(100f, Color.red);
                EnemyHealth nearbyHealth = nearby.AddComponent<EnemyHealth>();
                nearbyHealth.Configure(100f, Color.blue);
                EnemyHealth distantHealth = distant.AddComponent<EnemyHealth>();
                distantHealth.Configure(100f, Color.gray);
                original.transform.position = Vector3.zero;
                nearby.transform.position = Vector3.right * 6f;
                distant.transform.position = Vector3.right * 30f;

                DamageInfo damageInfo = new DamageInfo
                {
                    amount = 20f,
                    source = player,
                    weaponId = "weapon.frontier_revolver",
                    hitPoint = original.transform.position,
                    damageType = DamageType.Physical,
                    deliveryType = DamageDeliveryType.Raycast
                };
                DamageResult result = new DamageResult { applied = true, damageApplied = 20f };

                for (int i = 0; i < 5; i++)
                {
                    Assert.IsFalse(weapon.TryApplyChainHit(damageInfo, result, original));
                }

                Assert.IsTrue(weapon.TryApplyChainHit(damageInfo, result, original));
                Assert.AreEqual(93f, nearbyHealth.CurrentHealth);
                Assert.AreEqual(100f, originalHealth.CurrentHealth);
                Assert.AreEqual(100f, distantHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(original);
                Object.DestroyImmediate(nearby);
                Object.DestroyImmediate(distant);
            }
        }

        private static RunState CreateRunState()
        {
            RunState run = new RunState
            {
                isActive = true,
                seed = 1234,
                floorIndex = 1,
                currentFloor = new FloorState { floorIndex = 1, floorSeed = 2211 },
                visitedFloors = new List<FloorState>()
            };
            run.Normalize();
            return run;
        }

        private static bool ContainsSource(IReadOnlyList<RunStatModifierContribution> modifiers, string sourceId)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].sourceId == sourceId)
                {
                    return true;
                }
            }

            return false;
        }

        private static RunStatModifierContribution FindContribution(IReadOnlyList<RunStatModifierContribution> modifiers, string sourceId)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i].sourceId == sourceId)
                {
                    return modifiers[i];
                }
            }

            Assert.Fail($"Missing contribution from {sourceId}.");
            return default;
        }

        private static void DestroyRuntimeFeedbackRoot()
        {
            Transform root = PlayerWeaponController.GetOrCreateRuntimeFeedbackRoot();
            if (root != null)
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }
    }
}
