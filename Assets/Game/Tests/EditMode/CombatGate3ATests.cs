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

        [Test]
        public void WeaponRaycast_AgainstDummyCollider_AppliesDamage()
        {
            GameObject dummy = DungeonSceneController.CreateTargetDummy(null, new Vector3(0f, 40f, 10f), TargetDummyKind.Standard);
            try
            {
                Physics.SyncTransforms();
                Ray ray = new Ray(new Vector3(0f, 40f, 0f), Vector3.forward);
                RaycastHit[] hits = Physics.RaycastAll(ray, 20f, PlayerWeaponController.DefaultWeaponRaycastMask, QueryTriggerInteraction.Ignore);

                Assert.IsTrue(PlayerWeaponController.TryResolveShotHit(hits, null, out WeaponShotHit hit, out _));
                Assert.AreEqual(WeaponShotHitKind.Damageable, hit.kind);
                DamageResult result = hit.damageable.ApplyDamage(CreateDamageInfo(25f, DamageType.Physical));

                Assert.IsTrue(result.applied);
                Assert.AreEqual(75f, dummy.GetComponent<TargetDummyHealth>().CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(dummy);
            }
        }

        [Test]
        public void WeaponRaycast_IgnoresPlayerChildColliders()
        {
            GameObject player = new GameObject("PlayerRoot");
            GameObject playerChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject dummy = DungeonSceneController.CreateTargetDummy(null, new Vector3(0f, 50f, 12f), TargetDummyKind.Standard);
            try
            {
                playerChild.name = "WeaponHelperCollider";
                playerChild.transform.SetParent(player.transform, true);
                playerChild.transform.position = new Vector3(0f, 50f, 5f);
                playerChild.transform.localScale = Vector3.one * 2f;
                Physics.SyncTransforms();

                Ray ray = new Ray(new Vector3(0f, 50f, 0f), Vector3.forward);
                RaycastHit[] hits = Physics.RaycastAll(ray, 25f, PlayerWeaponController.DefaultWeaponRaycastMask, QueryTriggerInteraction.Ignore);

                Assert.IsTrue(PlayerWeaponController.TryResolveShotHit(hits, player.transform, out WeaponShotHit hit, out int ignored));
                Assert.AreEqual(WeaponShotHitKind.Damageable, hit.kind);
                Assert.GreaterOrEqual(ignored, 1);
                Assert.AreEqual(dummy, hit.hit.collider.gameObject);
            }
            finally
            {
                Object.DestroyImmediate(dummy);
                Object.DestroyImmediate(playerChild);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void WeaponRaycast_StopsOnEnvironmentBeforeDummy()
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject dummy = DungeonSceneController.CreateTargetDummy(null, new Vector3(0f, 60f, 12f), TargetDummyKind.Standard);
            try
            {
                wall.name = "EnvironmentWall";
                wall.transform.position = new Vector3(0f, 60f, 5f);
                wall.transform.localScale = new Vector3(4f, 4f, 1f);
                Physics.SyncTransforms();

                Ray ray = new Ray(new Vector3(0f, 60f, 0f), Vector3.forward);
                RaycastHit[] hits = Physics.RaycastAll(ray, 25f, PlayerWeaponController.DefaultWeaponRaycastMask, QueryTriggerInteraction.Ignore);

                Assert.IsTrue(PlayerWeaponController.TryResolveShotHit(hits, null, out WeaponShotHit hit, out _));
                Assert.AreEqual(WeaponShotHitKind.Environment, hit.kind);
                Assert.AreEqual(wall, hit.hit.collider.gameObject);
                Assert.IsNull(hit.damageable);
                Assert.AreEqual(dummy.GetComponent<TargetDummyHealth>().MaxHealth, dummy.GetComponent<TargetDummyHealth>().CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(dummy);
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void DefaultWeaponRaycastMask_IncludesDefaultAndExcludesIgnoreRaycast()
        {
            int mask = PlayerWeaponController.DefaultWeaponRaycastMask;

            Assert.AreNotEqual(0, mask & (1 << LayerMask.NameToLayer("Default")));
            Assert.AreEqual(0, mask & (1 << LayerMask.NameToLayer("Ignore Raycast")));
        }

        [Test]
        public void CreatedTargetDummy_UsesDefaultLayerAndEnabledNonTriggerCollider()
        {
            GameObject dummy = DungeonSceneController.CreateTargetDummy(null, Vector3.zero, TargetDummyKind.Standard);
            try
            {
                int defaultLayer = LayerMask.NameToLayer("Default");
                Transform[] transforms = dummy.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    Assert.AreEqual(defaultLayer, transforms[i].gameObject.layer);
                }

                Collider[] colliders = dummy.GetComponentsInChildren<Collider>(true);
                Assert.IsNotEmpty(colliders);
                for (int i = 0; i < colliders.Length; i++)
                {
                    Assert.IsTrue(colliders[i].enabled);
                    Assert.IsFalse(colliders[i].isTrigger);
                }
            }
            finally
            {
                Object.DestroyImmediate(dummy);
            }
        }

        [Test]
        public void CombatTestStation_RejectsBlockedLineOfSightCandidate()
        {
            DungeonBuildResult build = CreateLineOfSightBuildResult();
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                blocker.name = "LineOfSightBlocker";
                blocker.transform.position = new Vector3(5f, 4.1f, 0f);
                blocker.transform.localScale = new Vector3(1f, 4f, 4f);
                Physics.SyncTransforms();

                var selected = DungeonSceneController.SelectCombatTestStationSpawns(build, 3, true);

                Assert.AreEqual(3, selected.Count);
                for (int i = 0; i < selected.Count; i++)
                {
                    Assert.AreNotEqual(new Vector3(10f, 3.5f, 0f), selected[i].position);
                }
            }
            finally
            {
                Object.DestroyImmediate(blocker);
            }
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

        private static DungeonBuildResult CreateLineOfSightBuildResult()
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = 1,
                playerSpawn = new Vector3(-20f, 3.5f, -20f),
                playerSpawnNodeId = "ordinary_0"
            };

            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "ordinary_0",
                roomType = DungeonNodeKind.Ordinary,
                bounds = new Bounds(Vector3.zero, new Vector3(48f, 8f, 48f))
            });

            Vector3[] positions =
            {
                new Vector3(10f, 3.5f, 0f),
                new Vector3(0f, 3.5f, 10f),
                new Vector3(-10f, 3.5f, 0f),
                new Vector3(0f, 3.5f, -10f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                build.spawnPoints.Add(new DungeonSpawnPointRecord
                {
                    nodeId = "ordinary_0",
                    category = DungeonSpawnPointCategory.TargetDummy,
                    position = positions[i],
                    bounds = new Bounds(positions[i], new Vector3(2f, 6f, 2f)),
                    score = 100f - i
                });
            }

            return build;
        }
    }
}
