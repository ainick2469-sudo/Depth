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
    public sealed class Gate3D5ReadabilityTests
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
        public void Minimap_CoordinateMapping_UsesWorldXZAndPreservesAspect()
        {
            DungeonBuildResult build = CreateMapBuildResult();

            MinimapCoordinateMapping mapping = DungeonMinimapController.CalculateCoordinateMapping(build, new Vector2(200f, 200f), 10f);
            Vector2 left = mapping.WorldToMap(new Vector3(-10f, 0f, 0f));
            Vector2 right = mapping.WorldToMap(new Vector3(72f, 0f, 0f));
            Vector2 up = mapping.WorldToMap(new Vector3(0f, 0f, 40f));

            Assert.Greater(right.x, left.x);
            Assert.AreEqual(mapping.WorldToMap(new Vector3(0f, 25f, 0f)).x, mapping.WorldToMap(new Vector3(0f, -10f, 0f)).x, 0.001f);
            Assert.AreEqual(mapping.WorldToMap(new Vector3(0f, 25f, 0f)).y, mapping.WorldToMap(new Vector3(0f, -10f, 0f)).y, 0.001f);
            Assert.Greater(up.y, mapping.WorldToMap(Vector3.zero).y);
            Assert.Greater(mapping.WorldToMap(new Vector3(20f, 0f, 0f)).x, mapping.WorldToMap(Vector3.zero).x);
            Assert.LessOrEqual(Mathf.Abs(left.x), 100f);
            Assert.LessOrEqual(Mathf.Abs(right.x), 100f);
        }

        [TestCase(0f, 90f)]
        [TestCase(90f, 0f)]
        [TestCase(180f, -90f)]
        [TestCase(270f, -180f)]
        public void Minimap_NorthUpArrowRotation_UsesRightFacingGlyphBasis(float yaw, float expectedArrowZ)
        {
            Assert.AreEqual(
                expectedArrowZ,
                NormalizeSignedDegrees(DungeonMinimapController.GetNorthUpPlayerArrowZ(yaw)),
                0.001f);
        }

        [Test]
        public void Minimap_RotateWithPlayer_KeepsArrowForwardAndRotatesContentOnlyOnce()
        {
            Assert.AreEqual(90f, DungeonMinimapController.PlayerArrowIconOffsetDegrees, 0.001f);
            Assert.AreEqual(90f, DungeonMinimapController.GetRotatingMapContentZ(90f), 0.001f);
            Assert.AreEqual(90f, DungeonMinimapController.GetRotatingMapPlayerArrowZ(90f), 0.001f);

            Vector2 rotatedEastPoint = DungeonMinimapController.RotateMapPointForContent(new Vector2(12f, 0f), 90f);
            Assert.AreEqual(0f, rotatedEastPoint.x, 0.001f);
            Assert.AreEqual(12f, rotatedEastPoint.y, 0.001f);
        }

        [Test]
        public void Minimap_ReconfigurePreservesArrowBasisAcrossFloorTransition()
        {
            GameObject hud = new GameObject("Hud");
            GameObject player = new GameObject("Player");
            try
            {
                player.transform.eulerAngles = new Vector3(0f, 90f, 0f);
                DungeonMinimapController minimap = hud.AddComponent<DungeonMinimapController>();
                minimap.Configure(CreateMapBuildResult(), player.transform);
                float before = NormalizeSignedDegrees(minimap.CurrentPlayerArrowRotationZ);

                DungeonBuildResult nextFloor = CreateMapBuildResult();
                nextFloor.floorIndex = 2;
                minimap.Configure(nextFloor, player.transform);

                Assert.AreEqual(before, NormalizeSignedDegrees(minimap.CurrentPlayerArrowRotationZ), 0.001f);
                Assert.AreEqual(0f, NormalizeSignedDegrees(minimap.CurrentContentRotationZ), 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void Minimap_RevealsVisitedRoomConnectedCorridorAndNonSecretNeighbor()
        {
            GameObject hud = new GameObject("Hud");
            GameObject player = new GameObject("Player");
            try
            {
                DungeonBuildResult build = CreateMapBuildResult();
                DungeonMinimapController minimap = hud.AddComponent<DungeonMinimapController>();
                minimap.Configure(build, player.transform);

                minimap.NotifyRoomEntered("room.a");

                Assert.IsTrue(minimap.IsRoomVisited("room.a"));
                Assert.IsTrue(minimap.IsRoomDiscovered("room.b"));
                Assert.IsFalse(minimap.IsRoomDiscovered("room.secret"));
                Assert.IsTrue(minimap.IsCorridorDiscovered(DungeonBuildResult.GetEdgeKey("room.a", "room.b")));
                Assert.IsFalse(minimap.IsCorridorDiscovered(DungeonBuildResult.GetEdgeKey("room.a", "room.secret")));
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void Minimap_StaticGeometryBuildsOncePerConfigure()
        {
            GameObject hud = new GameObject("Hud");
            GameObject player = new GameObject("Player");
            try
            {
                DungeonMinimapController minimap = hud.AddComponent<DungeonMinimapController>();
                minimap.Configure(CreateMapBuildResult(), player.transform);
                int buildCount = minimap.GeometryBuildCount;

                minimap.NotifyRoomEntered("room.a");
                minimap.NotifyRoomEntered("room.b");

                Assert.AreEqual(buildCount, minimap.GeometryBuildCount);
                Assert.AreEqual(3, minimap.RoomElementCount);
                Assert.AreEqual(2, minimap.CorridorElementCount);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void RewardChoiceLabel_ShowsOwnedStackPreview()
        {
            RunState run = CreateRunState();
            run.AddOrStackUpgrade(RunUpgradeCatalog.ChainHitUpgradeId);
            RunUpgradeCatalog.TryGet(RunUpgradeCatalog.ChainHitUpgradeId, out RunUpgradeDefinition chainSpark);

            string label = RunUpgradeCatalog.BuildRewardChoiceLabel(run, chainSpark);

            StringAssert.Contains("Chain Spark Lv. 1 -> Lv. 2", label);
            StringAssert.Contains("20", label);
            StringAssert.Contains("30", label);
        }

        [Test]
        public void StatAggregator_ChainSparkStacksDamageFraction()
        {
            RunState run = CreateRunState();
            run.AddOrStackUpgrade(RunUpgradeCatalog.ChainHitUpgradeId);
            run.AddOrStackUpgrade(RunUpgradeCatalog.ChainHitUpgradeId);
            run.AddOrStackUpgrade(RunUpgradeCatalog.ChainHitUpgradeId);

            RunStatSnapshot snapshot = RunStatAggregator.Build(run);

            Assert.AreEqual(1, snapshot.chainEveryNthHit);
            Assert.AreEqual(0.40f, snapshot.chainDamageFraction, 0.001f);
        }

        [Test]
        public void WeaponRangeFalloff_UsesFullRangeMaxRangeAndMinimumMultiplier()
        {
            Assert.AreEqual(1f, PlayerWeaponController.CalculateRangeDamageMultiplier(16f, 17f, 45f, 0.5f), 0.001f);
            Assert.AreEqual(1f, PlayerWeaponController.CalculateRangeDamageMultiplier(17f, 17f, 45f, 0.5f), 0.001f);
            Assert.AreEqual(0.5f, PlayerWeaponController.CalculateRangeDamageMultiplier(45f, 17f, 45f, 0.5f), 0.001f);
            Assert.AreEqual(0f, PlayerWeaponController.CalculateRangeDamageMultiplier(46f, 17f, 45f, 0.5f), 0.001f);
        }

        [Test]
        public void WeaponDamageInfo_AppliesFalloffBeforeCrit()
        {
            RunState run = CreateRunState();
            run.AddOrStackUpgrade(RunUpgradeCatalog.CritChanceUpgradeId);
            RunStatAggregator.SetOverrideForTests(RunStatAggregator.Build(run));
            PlayerWeaponController.CritRollProviderForTests = () => 0f;

            GameObject player = new GameObject("FalloffCritPlayer");
            try
            {
                PlayerWeaponController weapon = player.AddComponent<PlayerWeaponController>();
                DamageInfo info = weapon.CreateDamageInfoForTests(Vector3.zero, Vector3.up, 1f, 0.5f);

                Assert.IsTrue(info.isCritical);
                Assert.AreEqual(15f * 0.5f * RunStatAggregator.CriticalHitMultiplier, info.amount, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void RunInfoPanel_UsesFinalAggregatedWeaponStats()
        {
            RunState run = CreateRunState();
            run.AddOrStackUpgrade(RunUpgradeCatalog.RevolverDamageUpgradeId);
            RunStatAggregator.SetOverrideForTests(RunStatAggregator.Build(run));

            GameObject hud = new GameObject("Hud");
            GameObject player = new GameObject("Player");
            try
            {
                player.AddComponent<PlayerHealth>();
                player.AddComponent<PlayerWeaponController>();
                RunInfoPanelController panel = hud.AddComponent<RunInfoPanelController>();

                string text = panel.BuildInfoText();

                StringAssert.Contains("Damage 15 -> 16.5", text);
                StringAssert.Contains("Range 45", text);
                StringAssert.Contains("Reserve", text);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void CorridorVisualFloor_TrimsEndpointOverlapToNearZeroButKeepsLogicalOverlap()
        {
            Vector3 start = new Vector3(5.25f, 0f, 0f);
            Vector3 end = new Vector3(14.75f, 0f, 0f);

            DungeonSceneController.GetVisualCorridorFloor(start, end, true, true, out Vector3 midpoint, out float length);

            Assert.AreEqual(10f, midpoint.x, 0.001f);
            Assert.AreEqual(8.04f, length, 0.01f);
            Assert.AreEqual(0.75f, DungeonSceneController.CorridorRoomOverlap, 0.001f);
            Assert.AreEqual(0.02f, DungeonSceneController.CorridorVisualRoomOverlap, 0.001f);
            Assert.Greater(length, 8f);
        }

        [Test]
        public void CorridorSeamDebugSummary_DistinguishesLogicalVisualAndYOffset()
        {
            string summary = DungeonSceneController.BuildCorridorSeamDebugSummary(
                new Vector3(5.25f, 0f, 0f),
                new Vector3(14.75f, 0f, 0f),
                true,
                true);

            StringAssert.Contains("LogicalOverlap=0.75", summary);
            StringAssert.Contains("VisualOverlap=0.02", summary);
            StringAssert.Contains("VisualYOffset=-0.015", summary);
            Assert.Less(DungeonSceneController.CorridorVisualFloorYOffset, 0f);
            Assert.Greater(DungeonSceneController.CorridorVisualFloorYOffset, -0.03f);
        }

        private static DungeonBuildResult CreateMapBuildResult()
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = 1,
                playerSpawnNodeId = "room.a",
                transitDownNodeId = "room.b",
                secretNodeId = "room.secret"
            };

            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "room.a",
                roomType = DungeonNodeKind.EntryHub,
                bounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(20f, 4f, 20f))
            });
            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "room.b",
                roomType = DungeonNodeKind.TransitDown,
                bounds = new Bounds(new Vector3(60f, 0f, 0f), new Vector3(24f, 4f, 24f))
            });
            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "room.secret",
                roomType = DungeonNodeKind.Secret,
                bounds = new Bounds(new Vector3(0f, 0f, 34f), new Vector3(16f, 4f, 16f))
            });

            AddEdge(build, "room.a", "room.b", false);
            AddEdge(build, "room.a", "room.secret", true);
            return build;
        }

        private static void AddEdge(DungeonBuildResult build, string a, string b, bool secret)
        {
            string edgeKey = DungeonBuildResult.GetEdgeKey(a, b);
            build.graphEdges.Add(new DungeonGraphEdgeRecord { edgeKey = edgeKey, a = a, b = b });
            Bounds bounds = a == "room.a" && b == "room.b"
                ? new Bounds(new Vector3(30f, 0f, 0f), new Vector3(40f, 2f, 8f))
                : new Bounds(new Vector3(0f, 0f, 17f), new Vector3(8f, 2f, 18f));
            build.corridors.Add(new DungeonCorridorBuildRecord
            {
                edgeKey = edgeKey,
                fromNodeId = a,
                toNodeId = b,
                bounds = bounds,
                isSecretCorridor = secret
            });
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

        private static float NormalizeSignedDegrees(float degrees)
        {
            return Mathf.Repeat(degrees + 180f, 360f) - 180f;
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
