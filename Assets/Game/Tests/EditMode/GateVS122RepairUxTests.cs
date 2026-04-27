using System.Collections.Generic;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.UI;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS122RepairUxTests
    {
        [Test]
        public void RunState_FloorDiscoveryPersistsPerFloorIndex()
        {
            RunState run = new RunState { seed = 123, floorIndex = 1 };
            run.currentFloor = new FloorState { floorIndex = 1, floorSeed = 1001 };
            run.currentFloor.visitedRoomIds.Add("floor1.a");
            run.currentFloor.discoveredCorridorIds.Add("floor1.a_floor1.b");
            run.SetVisitedFloor(run.currentFloor);

            FloorState floorTwo = new FloorState { floorIndex = 2, floorSeed = 2002 };
            floorTwo.visitedRoomIds.Add("floor2.a");
            run.SetVisitedFloor(floorTwo);

            Assert.IsTrue(run.GetVisitedFloor(1).visitedRoomIds.Contains("floor1.a"));
            Assert.IsFalse(run.GetVisitedFloor(1).visitedRoomIds.Contains("floor2.a"));
            Assert.IsTrue(run.GetVisitedFloor(2).visitedRoomIds.Contains("floor2.a"));
        }

        [Test]
        public void RunWeaponAmmoState_LegacyNormalizeUsesNewSafetyDefaults()
        {
            RunWeaponAmmoState ammo = new RunWeaponAmmoState
            {
                weaponId = string.Empty,
                currentMagazineAmmo = 0,
                reserveAmmo = 0,
                maxReserveAmmo = 0
            };

            ammo.Normalize("weapon.frontier_revolver", true);

            Assert.AreEqual(6, ammo.currentMagazineAmmo);
            Assert.AreEqual(36, ammo.reserveAmmo);
            Assert.AreEqual(72, ammo.maxReserveAmmo);
        }

        [Test]
        public void EnemyBehaviorSeeds_DeSyncPatrolTargetsForSameRoomEnemies()
        {
            EnemyDefinition slime = EnemyCatalog.CreateDefinition(EnemyArchetype.Slime);
            GameObject first = new GameObject("SeededSlimeA");
            GameObject second = new GameObject("SeededSlimeB");
            try
            {
                SimpleMeleeEnemyController a = first.AddComponent<SimpleMeleeEnemyController>();
                SimpleMeleeEnemyController b = second.AddComponent<SimpleMeleeEnemyController>();
                Bounds room = new Bounds(Vector3.zero, new Vector3(40f, 6f, 40f));
                Vector3[] patrol = { new Vector3(-8f, 0f, -8f), new Vector3(8f, 0f, -8f), new Vector3(8f, 0f, 8f), new Vector3(-8f, 0f, 8f) };

                a.Configure(slime);
                b.Configure(slime);
                a.SetHomeRoomForTests("room.seed", room, patrol);
                b.SetHomeRoomForTests("room.seed", room, patrol);
                a.ConfigureBehaviorSeedForTests(101);
                b.ConfigureBehaviorSeedForTests(909);

                Vector3 aTarget = a.ChooseNextPatrolTargetForTests();
                Vector3 bTarget = b.ChooseNextPatrolTargetForTests();

                Assert.AreNotEqual(a.BehaviorSeed, b.BehaviorSeed);
                Assert.Greater((aTarget - bTarget).sqrMagnitude, 0.5f);
                Assert.AreNotEqual(a.BatBobPhase, b.BatBobPhase);
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(slime);
            }
        }

        [Test]
        public void DungeonMinimap_ExportsAndImportsFloorDiscovery()
        {
            GameObject hud = new GameObject("Hud");
            GameObject player = new GameObject("Player");
            try
            {
                DungeonBuildResult build = CreateBuild();
                DungeonMinimapController minimap = hud.AddComponent<DungeonMinimapController>();
                minimap.Configure(build, player.transform);
                minimap.NotifyRoomEntered("room.a");

                FloorState floor = new FloorState { floorIndex = 1, floorSeed = 1001 };
                minimap.ExportDiscoveryTo(floor);

                DungeonMinimapController restored = new GameObject("RestoredHud").AddComponent<DungeonMinimapController>();
                restored.Configure(build, player.transform);
                restored.ImportDiscoveryFrom(floor);

                Assert.IsTrue(restored.IsRoomVisited("room.a"));
                Assert.IsTrue(restored.IsRoomDiscovered("room.b"));
                Assert.IsTrue(restored.IsCorridorDiscovered(DungeonBuildResult.GetEdgeKey("room.a", "room.b")));

                Object.DestroyImmediate(restored.gameObject);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void GameSettingsState_ClampsUnsafeValues()
        {
            GameSettingsState settings = new GameSettingsState
            {
                mouseSensitivity = 99f,
                fov = 140f,
                masterVolume = -2f,
                minimapOpacity = 0f,
                minimapZoom = 10f
            };

            settings.Clamp();

            Assert.AreEqual(10f, settings.mouseSensitivity);
            Assert.AreEqual(100f, settings.fov);
            Assert.AreEqual(0f, settings.masterVolume);
            Assert.AreEqual(0.1f, settings.minimapOpacity);
            Assert.AreEqual(3f, settings.minimapZoom);
        }

        private static DungeonBuildResult CreateBuild()
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = 1,
                seed = 111,
                playerSpawnNodeId = "room.a"
            };
            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "room.a",
                roomType = DungeonNodeKind.EntryHub,
                bounds = new Bounds(Vector3.zero, new Vector3(20f, 4f, 20f))
            });
            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "room.b",
                roomType = DungeonNodeKind.Landmark,
                bounds = new Bounds(new Vector3(30f, 0f, 0f), new Vector3(20f, 4f, 20f))
            });
            string edge = DungeonBuildResult.GetEdgeKey("room.a", "room.b");
            build.graphEdges.Add(new DungeonGraphEdgeRecord { a = "room.a", b = "room.b", edgeKey = edge });
            build.corridors.Add(new DungeonCorridorBuildRecord
            {
                edgeKey = edge,
                fromNodeId = "room.a",
                toNodeId = "room.b",
                bounds = new Bounds(new Vector3(15f, 0f, 0f), new Vector3(10f, 2f, 6f))
            });
            return build;
        }
    }
}
