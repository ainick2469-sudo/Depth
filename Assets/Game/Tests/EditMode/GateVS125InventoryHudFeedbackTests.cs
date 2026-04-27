using System.Collections.Generic;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS125InventoryHudFeedbackTests
    {
        [Test]
        public void ProfileNormalize_DefaultsRevolverAndPromotesSecondOwnedWeapon()
        {
            ProfileState profile = new ProfileState
            {
                equippedWeaponId = WeaponCatalog.FrontierRifleId,
                unlockedWeaponIds = new List<string> { WeaponCatalog.FrontierRevolverId, WeaponCatalog.FrontierRifleId }
            };

            profile.Normalize();

            Assert.AreEqual(WeaponCatalog.FrontierRevolverId, profile.primaryWeaponId);
            Assert.AreEqual(WeaponCatalog.FrontierRifleId, profile.secondaryWeaponId);
            Assert.IsTrue(profile.HasUnlockedWeapon(WeaponCatalog.FrontierRevolverId));
        }

        [Test]
        public void RunState_StoresPerWeaponAmmoAndMirrorsActiveLegacyAmmo()
        {
            RunState run = new RunState();
            run.Normalize();

            RunWeaponAmmoState rifle = run.GetOrCreateWeaponAmmoState(WeaponCatalog.FrontierRifleId, 5, 12, 60);
            rifle.reserveAmmo = 20;
            run.UpsertWeaponAmmoState(rifle);

            RunWeaponAmmoState loaded = run.GetWeaponAmmoState(WeaponCatalog.FrontierRifleId);

            Assert.NotNull(loaded);
            Assert.AreEqual(20, loaded.reserveAmmo);
            Assert.AreEqual(60, loaded.maxReserveAmmo);
        }

        [Test]
        public void InputBindingDefaults_AddInventoryDashAndWeaponSlots()
        {
            Assert.AreEqual(KeyCode.I.ToString(), InputBindingService.GetDefaultRecord(GameplayInputAction.Inventory).primary);
            Assert.AreEqual(KeyCode.Tab.ToString(), InputBindingService.GetDefaultRecord(GameplayInputAction.Inventory).secondary);
            Assert.AreEqual(KeyCode.LeftControl.ToString(), InputBindingService.GetDefaultRecord(GameplayInputAction.Dash).primary);
            Assert.AreEqual(KeyCode.LeftAlt.ToString(), InputBindingService.GetDefaultRecord(GameplayInputAction.Dash).secondary);
            Assert.AreEqual(KeyCode.Alpha1.ToString(), InputBindingService.GetDefaultRecord(GameplayInputAction.EquipPrimary).primary);
            Assert.AreEqual(KeyCode.Alpha2.ToString(), InputBindingService.GetDefaultRecord(GameplayInputAction.EquipSecondary).primary);
        }

        [Test]
        public void RunUpgradeChoices_AvoidDuplicatePrimaryCategoriesWhenPossible()
        {
            RunState run = new RunState();
            run.Normalize();

            List<RunUpgradeDefinition> choices = RunUpgradeCatalog.CreateRewardChoices(run, 3, 12345);
            HashSet<RunUpgradeCategory> categories = new HashSet<RunUpgradeCategory>();

            for (int i = 0; i < choices.Count; i++)
            {
                Assert.IsTrue(categories.Add(choices[i].category), $"Duplicate category {choices[i].category}");
            }
        }

        [Test]
        public void HuntersClaim_IsBountyRewardNotKillHealClone()
        {
            Assert.IsTrue(RunUpgradeCatalog.TryGet(RunUpgradeCatalog.HuntersClaimUpgradeId, out RunUpgradeDefinition huntersClaim));

            Assert.AreEqual(RunUpgradeEffectKind.EliteBountyRewardFlat, huntersClaim.effectKind);
            Assert.AreEqual(RunUpgradeCategory.Bounty, huntersClaim.category);
        }

        [Test]
        public void RunStatAggregator_BuildsNewMovementAndSurvivalEffects()
        {
            RunState run = new RunState();
            run.Normalize();
            run.AddOrStackUpgrade(RunUpgradeCatalog.DashSpursUpgradeId);
            run.AddOrStackUpgrade(RunUpgradeCatalog.GritUpgradeId);
            run.AddOrStackUpgrade(RunUpgradeCatalog.RoomReaderUpgradeId);

            RunStatSnapshot stats = RunStatAggregator.Build(run);

            Assert.Less(stats.DashCooldownMultiplier, 1f);
            Assert.IsTrue(stats.hasLethalSavePerFloor);
            Assert.AreEqual(1, stats.scoutRevealBonus);
        }

        [Test]
        public void CombatFeedbackService_RecreatesPersistentDamagePool()
        {
            CombatFeedbackService service = CombatFeedbackService.GetOrCreate();

            CombatFeedbackService.ShowDamageNumber(Vector3.zero, 7f, Color.white, false, "TEST");

            Assert.NotNull(service);
            Assert.AreEqual(32, service.PoolSizeForTests);
            Assert.NotNull(service.transform.Find("SharedCombatDamageNumberPool"));
            Object.DestroyImmediate(service.gameObject);
        }

        [Test]
        public void HealthPickupMagnet_CollectsByPollingRadius()
        {
            GameObject player = new GameObject("Player", typeof(PlayerHealth));
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                health.ResetHealth();
                health.ApplyDamage(new DamageInfo { amount = 25f, source = pickup, hitPoint = player.transform.position });
                HealthPickup healthPickup = pickup.AddComponent<HealthPickup>();
                healthPickup.Configure(10f);
                PickupMagnetController magnet = pickup.GetComponent<PickupMagnetController>();

                Assert.IsTrue(magnet.TryCollect(player));
                Assert.Greater(health.CurrentHealth, health.MaxHealth - 25f);
            }
            finally
            {
                Object.DestroyImmediate(player);
                if (pickup != null)
                {
                    Object.DestroyImmediate(pickup);
                }
            }
        }
    }
}
