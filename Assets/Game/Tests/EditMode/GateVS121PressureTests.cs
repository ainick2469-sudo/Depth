using System.Collections.Generic;
using FrontierDepths.Combat;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS121PressureTests
    {
        [Test]
        public void WeaponRuntimeState_ReloadConsumesReserveAndAllowsPartialReload()
        {
            WeaponRuntimeState state = new WeaponRuntimeState(6, 2, 60, 1);

            Assert.IsTrue(state.TryStartReload(0f, 0.1f));
            Assert.IsTrue(state.Tick(0.2f));

            Assert.AreEqual(3, state.CurrentAmmo);
            Assert.AreEqual(0, state.ReserveAmmo);
            Assert.IsFalse(state.TryStartReload(0.3f, 0.1f));
        }

        [Test]
        public void WeaponRuntimeState_ReservePickupRespectsCapAndAutoReloadRequiresReserve()
        {
            WeaponRuntimeState empty = new WeaponRuntimeState(6, 0, 12, 0);

            Assert.IsFalse(empty.TryQueueAutoReload(0f, 0f));
            Assert.AreEqual(5, empty.TryAddAmmoToReserve(5, true));
            Assert.IsTrue(empty.TryQueueAutoReload(0f, 0f));
            Assert.IsTrue(empty.TryStartQueuedAutoReload(0f, 0.1f));
            Assert.IsTrue(empty.Tick(0.2f));

            Assert.AreEqual(5, empty.CurrentAmmo);
            Assert.AreEqual(0, empty.ReserveAmmo);
            Assert.AreEqual(12, empty.TryAddAmmoToReserve(99, true));
            Assert.AreEqual(12, empty.ReserveAmmo);
            Assert.AreEqual(0, empty.TryAddAmmoToReserve(1, true));
        }

        [Test]
        public void EnemyMovementMultipliers_ApplyByStateAndMakeChaseFasterThanPatrol()
        {
            EnemyDefinition bat = EnemyCatalog.CreateDefinition(EnemyArchetype.Bat);
            GameObject enemy = new GameObject("MultiplierBat");
            try
            {
                SimpleMeleeEnemyController controller = enemy.AddComponent<SimpleMeleeEnemyController>();
                controller.Configure(bat);

                Assert.AreEqual(EnemyMobilityRole.Roamer, bat.defaultMobilityRole);
                Assert.Greater(controller.GetEffectiveMoveSpeedForTests(SimpleMeleeEnemyState.Chase), controller.GetEffectiveMoveSpeedForTests(SimpleMeleeEnemyState.Patrol));
                Assert.Greater(controller.GetEffectiveMoveSpeedForTests(SimpleMeleeEnemyState.Investigate), controller.GetEffectiveMoveSpeedForTests(SimpleMeleeEnemyState.Patrol));
            }
            finally
            {
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(bat);
            }
        }

        [Test]
        public void EnemyController_RoamerRouteIsConfiguredWithoutLeavingMathToWalls()
        {
            EnemyDefinition bat = EnemyCatalog.CreateDefinition(EnemyArchetype.Bat);
            GameObject enemy = new GameObject("RouteBat");
            try
            {
                SimpleMeleeEnemyController controller = enemy.AddComponent<SimpleMeleeEnemyController>();
                controller.Configure(bat);
                controller.ConfigureMobilityRole(EnemyMobilityRole.Roamer);
                controller.ConfigureHomeRoom("room.home", new Bounds(Vector3.zero, new Vector3(30f, 6f, 30f)), new[] { Vector3.zero });
                controller.ConfigureRoamingRoute(new[]
                {
                    Vector3.zero,
                    new Vector3(12f, 0f, 0f),
                    new Vector3(24f, 0f, 0f),
                    new Vector3(36f, 0f, 0f)
                });

                IReadOnlyList<Vector3> route = controller.GetRoamingRouteForTests();
                Assert.AreEqual(EnemyMobilityRole.Roamer, controller.MobilityRole);
                Assert.GreaterOrEqual(route.Count, 4);
                Assert.AreEqual(Vector3.zero, route[0]);
            }
            finally
            {
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(bat);
            }
        }

        [Test]
        public void EncounterDirector_FloorSixUsesDeepBandBudgetAndActiveCap()
        {
            DungeonBuildResult build = CreatePressureBuild(6);

            DungeonEncounterPlan plan = DungeonEncounterDirector.BuildPlan(build, null, 606);

            Assert.AreEqual(6, plan.floorIndex);
            Assert.AreEqual("DungeonPushesBack", plan.difficultyBand);
            Assert.AreEqual(8, plan.activeCombatCap);
            Assert.GreaterOrEqual(plan.requestedBudget, 24);
            Assert.LessOrEqual(plan.requestedBudget, 32);
            Assert.GreaterOrEqual(plan.spawnedCount, 18);
            Assert.IsFalse(plan.warning.Contains("floor > 5"));
        }

        [Test]
        public void EncounterDirector_FloorSixCanAssignRoamersWithGraphRoutes()
        {
            DungeonBuildResult build = CreatePressureBuild(6);
            DungeonEncounterPlan roamerPlan = null;

            for (int seed = 1; seed <= 80; seed++)
            {
                DungeonEncounterPlan plan = DungeonEncounterDirector.BuildPlan(build, null, seed);
                if (plan.roamerCount > 0)
                {
                    roamerPlan = plan;
                    break;
                }
            }

            Assert.NotNull(roamerPlan);
            Assert.Greater(roamerPlan.roamerCount, 0);
            Assert.IsTrue(roamerPlan.spawns.Exists(spawn =>
                (spawn.mobilityRole == EnemyMobilityRole.Roamer || spawn.mobilityRole == EnemyMobilityRole.Hunter) &&
                spawn.roamingRoute != null &&
                spawn.roamingRoute.Count >= 3 &&
                spawn.roomId != "entry" &&
                spawn.roomId != "transit_down" &&
                spawn.roomId != "reward"));
        }

        [Test]
        public void EnemyVariantCatalog_ClonesStatsWithoutMutatingBaseDefinition()
        {
            EnemyDefinition baseSlime = EnemyCatalog.CreateDefinition(EnemyArchetype.Slime);
            EnemyVariantDefinition variant = new EnemyVariantDefinition
            {
                variantId = "test.fast_slime",
                displaySuffix = "Fast",
                healthMultiplier = 0.8f,
                speedMultiplier = 1.4f,
                damageMultiplier = 1.2f,
                sizeMultiplier = 0.9f,
                colorTint = Color.cyan,
                minFloor = 1,
                maxFloor = 0
            };

            EnemyDefinition clone = EnemyVariantCatalog.CreateVariantDefinition(baseSlime, variant);
            try
            {
                Assert.AreEqual(36f, baseSlime.maxHealth);
                Assert.AreEqual(3.6f, baseSlime.moveSpeed);
                Assert.AreEqual(36f * 0.8f, clone.maxHealth);
                Assert.AreEqual(3.6f * 1.4f, clone.moveSpeed);
                Assert.IsTrue(clone.displayName.Contains("Fast"));
                Assert.AreEqual(Color.Lerp(baseSlime.bodyColor, Color.cyan, 0.35f), clone.bodyColor);
            }
            finally
            {
                Object.DestroyImmediate(baseSlime);
                Object.DestroyImmediate(clone);
            }
        }

        private static DungeonBuildResult CreatePressureBuild(int floorIndex)
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = floorIndex,
                seed = 909,
                playerSpawn = Vector3.zero,
                playerSpawnNodeId = "entry"
            };

            AddRoom(build, "entry", DungeonNodeKind.EntryHub, 0f, 900f, 0);
            AddRoom(build, "ordinary_a", DungeonNodeKind.Ordinary, 42f, 1800f, 6);
            AddRoom(build, "ordinary_b", DungeonNodeKind.Ordinary, 84f, 1800f, 6);
            AddRoom(build, "ordinary_c", DungeonNodeKind.Ordinary, 126f, 1800f, 6);
            AddRoom(build, "ordinary_d", DungeonNodeKind.Ordinary, 168f, 1800f, 6);
            AddRoom(build, "ordinary_e", DungeonNodeKind.Ordinary, 210f, 1800f, 6);
            AddRoom(build, "landmark", DungeonNodeKind.Landmark, 252f, 2400f, 8);
            AddRoom(build, "reward", DungeonNodeKind.Landmark, 294f, 1800f, 4);
            AddRoom(build, "transit_down", DungeonNodeKind.TransitDown, 336f, 900f, 0);

            Connect(build, "entry", "ordinary_a", 21f);
            Connect(build, "ordinary_a", "ordinary_b", 63f);
            Connect(build, "ordinary_b", "ordinary_c", 105f);
            Connect(build, "ordinary_c", "ordinary_d", 147f);
            Connect(build, "ordinary_d", "ordinary_e", 189f);
            Connect(build, "ordinary_e", "landmark", 231f);
            Connect(build, "landmark", "reward", 273f);
            Connect(build, "reward", "transit_down", 315f);
            return build;
        }

        private static void AddRoom(DungeonBuildResult build, string nodeId, DungeonNodeKind kind, float x, float footprint, int spawnCount)
        {
            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = nodeId,
                roomType = kind,
                footprintArea = footprint,
                bounds = new Bounds(new Vector3(x, 0f, 0f), new Vector3(36f, 8f, 36f))
            });

            for (int i = 0; i < spawnCount; i++)
            {
                build.spawnPoints.Add(new DungeonSpawnPointRecord
                {
                    nodeId = nodeId,
                    category = DungeonSpawnPointCategory.EnemyMelee,
                    position = new Vector3(x, 3.5f, -20f + i * 8f),
                    score = 100f - i
                });
            }
        }

        private static void Connect(DungeonBuildResult build, string a, string b, float x)
        {
            string edgeKey = DungeonBuildResult.GetEdgeKey(a, b);
            build.graphEdges.Add(new DungeonGraphEdgeRecord { a = a, b = b, edgeKey = edgeKey });
            build.corridors.Add(new DungeonCorridorBuildRecord
            {
                edgeKey = edgeKey,
                fromNodeId = a,
                toNodeId = b,
                start = new Vector3(x - 8f, 0f, 0f),
                end = new Vector3(x + 8f, 0f, 0f),
                length = 16f,
                width = 8f
            });
            build.doorOpenings.Add(new DungeonDoorOpeningRecord
            {
                nodeId = a,
                neighborNodeId = b,
                edgeKey = edgeKey,
                center = new Vector3(x - 10f, 0f, 0f)
            });
            build.doorOpenings.Add(new DungeonDoorOpeningRecord
            {
                nodeId = b,
                neighborNodeId = a,
                edgeKey = edgeKey,
                center = new Vector3(x + 10f, 0f, 0f)
            });
        }
    }
}
