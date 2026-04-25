using FrontierDepths.Combat;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;

namespace FrontierDepths.Tests.EditMode
{
    public class CombatGate3ATests
    {
        [Test]
        public void WeaponRuntimeState_BlocksFireUntilCooldownExpires()
        {
            WeaponRuntimeState state = new WeaponRuntimeState(6);

            Assert.IsTrue(state.TryFire(0f, 0.35f));
            Assert.AreEqual(5, state.CurrentAmmo);
            Assert.IsFalse(state.TryFire(0.2f, 0.35f));
            Assert.AreEqual(5, state.CurrentAmmo);
            Assert.IsTrue(state.TryFire(0.35f, 0.35f));
            Assert.AreEqual(4, state.CurrentAmmo);
        }

        [Test]
        public void WeaponRuntimeState_ReloadRestoresAmmoAfterManualTimeAdvance()
        {
            WeaponRuntimeState state = new WeaponRuntimeState(6);
            Assert.IsTrue(state.TryFire(0f, 0.35f));

            Assert.IsTrue(state.TryStartReload(0.1f, 1.4f));
            Assert.IsFalse(state.Tick(1.49f));
            Assert.AreEqual(5, state.CurrentAmmo);
            Assert.IsTrue(state.Tick(1.5f));
            Assert.AreEqual(6, state.CurrentAmmo);
            Assert.IsFalse(state.IsReloading);
        }

        [Test]
        public void WeaponRuntimeState_CannotFireWhileReloading()
        {
            WeaponRuntimeState state = new WeaponRuntimeState(6);
            Assert.IsTrue(state.TryFire(0f, 0.35f));
            Assert.IsTrue(state.TryStartReload(0.1f, 1.4f));

            Assert.IsFalse(state.TryFire(0.5f, 0.35f));
            Assert.AreEqual(5, state.CurrentAmmo);
        }

        [Test]
        public void StandardDummy_TakesDamage_DiesOnce_AndResets()
        {
            GameObject target = new GameObject("StandardDummyTest");
            try
            {
                TargetDummyHealth dummy = target.AddComponent<TargetDummyHealth>();
                dummy.Configure(TargetDummyKind.Standard);
                int deathCount = 0;
                dummy.Died += _ => deathCount++;

                DamageResult firstHit = dummy.ApplyDamage(CreateDamageInfo(25f, DamageType.Physical));
                Assert.IsTrue(firstHit.applied);
                Assert.AreEqual(75f, dummy.CurrentHealth);

                DamageResult killingHit = dummy.ApplyDamage(CreateDamageInfo(100f, DamageType.Physical));
                Assert.IsTrue(killingHit.killedTarget);
                Assert.AreEqual(1, deathCount);

                dummy.ApplyDamage(CreateDamageInfo(100f, DamageType.Physical));
                Assert.AreEqual(1, deathCount);
                Assert.IsTrue(dummy.IsDead);

                dummy.AdvanceReset(1.9f);
                Assert.IsTrue(dummy.IsDead);
                dummy.AdvanceReset(0.2f);
                Assert.IsFalse(dummy.IsDead);
                Assert.AreEqual(dummy.MaxHealth, dummy.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ArmoredDummy_ReducesPhysicalDamageInResult()
        {
            GameObject target = new GameObject("ArmoredDummyTest");
            try
            {
                TargetDummyHealth dummy = target.AddComponent<TargetDummyHealth>();
                dummy.Configure(TargetDummyKind.Armored);

                DamageResult result = dummy.ApplyDamage(CreateDamageInfo(100f, DamageType.Physical));

                Assert.IsTrue(result.applied);
                Assert.Greater(result.damageApplied, 0f);
                Assert.Less(result.damageApplied, 100f);
                Assert.AreEqual(dummy.MaxHealth - result.damageApplied, dummy.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void StatusDummy_RecordsDamageTagsAndStatusMetadata()
        {
            GameObject target = new GameObject("StatusDummyTest");
            try
            {
                TargetDummyHealth dummy = target.AddComponent<TargetDummyHealth>();
                dummy.Configure(TargetDummyKind.StatusTest);

                DamageInfo damageInfo = CreateDamageInfo(15f, DamageType.Fire);
                damageInfo.tags = new[] { GameplayTag.Fire, GameplayTag.OnHit };
                damageInfo.statusChance = 0.5f;
                DamageResult result = dummy.ApplyDamage(damageInfo);

                Assert.IsTrue(result.applied);
                StringAssert.Contains("Fire", dummy.LastStatusText);
                StringAssert.Contains("OnHit", dummy.LastStatusText);
                StringAssert.Contains("Status 50%", dummy.LastStatusText);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void CombatTestStation_UsesTargetDummySpawnPointsOnly()
        {
            DungeonBuildResult build = CreateStationBuildResult();
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "ordinary_0",
                category = DungeonSpawnPointCategory.Interactable,
                position = new Vector3(12f, 3.5f, 0f),
                bounds = new Bounds(new Vector3(12f, 3.5f, 0f), new Vector3(2f, 6f, 2f)),
                score = 999f
            });

            var selected = DungeonSceneController.SelectCombatTestStationSpawns(build, 3);

            Assert.AreEqual(3, selected.Count);
            for (int i = 0; i < selected.Count; i++)
            {
                Assert.AreEqual(DungeonSpawnPointCategory.TargetDummy, selected[i].category);
                Assert.GreaterOrEqual(Vector3.Distance(build.playerSpawn, selected[i].position), 12f);
            }
        }

        [Test]
        public void CombatTestStation_DoesNotSelectOnNonFloorOne()
        {
            DungeonBuildResult build = CreateStationBuildResult();
            build.floorIndex = 2;

            var selected = DungeonSceneController.SelectCombatTestStationSpawns(build, 3);

            Assert.IsEmpty(selected);
        }

        private static DamageInfo CreateDamageInfo(float amount, DamageType damageType)
        {
            return new DamageInfo
            {
                amount = amount,
                source = null,
                weaponId = "weapon.frontier_revolver",
                hitPoint = Vector3.zero,
                hitNormal = Vector3.up,
                damageType = damageType,
                deliveryType = DamageDeliveryType.Raycast,
                tags = new GameplayTag[0],
                canCrit = false,
                isCritical = false,
                knockbackForce = 0f,
                statusChance = 0f
            };
        }

        private static DungeonBuildResult CreateStationBuildResult()
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = 1,
                playerSpawn = Vector3.zero,
                playerSpawnNodeId = "transit_up"
            };

            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "transit_up",
                roomType = DungeonNodeKind.TransitUp,
                bounds = new Bounds(Vector3.zero, new Vector3(24f, 8f, 24f))
            });

            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "ordinary_0",
                roomType = DungeonNodeKind.Ordinary,
                bounds = new Bounds(new Vector3(24f, 0f, 0f), new Vector3(36f, 8f, 36f))
            });

            for (int i = 0; i < 3; i++)
            {
                Vector3 position = new Vector3(20f + i * 3f, 3.5f, i * 2f);
                build.spawnPoints.Add(new DungeonSpawnPointRecord
                {
                    nodeId = "ordinary_0",
                    category = DungeonSpawnPointCategory.TargetDummy,
                    position = position,
                    bounds = new Bounds(position, new Vector3(2f, 6f, 2f)),
                    score = 100f - i
                });
            }

            return build;
        }
    }
}
