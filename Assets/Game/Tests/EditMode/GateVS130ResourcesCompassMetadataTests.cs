using FrontierDepths.Core;
using FrontierDepths.UI;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS130ResourcesCompassMetadataTests
    {
        [Test]
        public void PlayerResources_SpendRestoreAndRegenSafely()
        {
            GameObject player = new GameObject("Player");
            try
            {
                PlayerResourceController resources = player.AddComponent<PlayerResourceController>();

                Assert.IsTrue(resources.TrySpendStamina(35f, "Dash"));
                Assert.AreEqual(65f, resources.CurrentStamina, 0.01f);
                float now = Time.time;
                resources.TickForTests(1f, now + 0.1f);
                Assert.AreEqual(65f, resources.CurrentStamina, 0.01f, "Regen should wait for delay.");
                resources.TickForTests(1f, now + 2f);
                Assert.Greater(resources.CurrentStamina, 65f);

                Assert.IsTrue(resources.TrySpendFocus(25f, "Depth Sense"));
                Assert.AreEqual(75f, resources.CurrentFocus, 0.01f);
                resources.RestoreFocus(10f);
                Assert.AreEqual(85f, resources.CurrentFocus, 0.01f);

                Assert.IsTrue(resources.TrySpendMana(20f, "Future Spell"));
                Assert.AreEqual(80f, resources.CurrentMana, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PlayerResources_ExhaustionLocksStaminaUntilThresholdButAllowsHeldSprintResume()
        {
            GameObject player = new GameObject("PlayerResourcesExhaustion");
            try
            {
                PlayerResourceController resources = player.AddComponent<PlayerResourceController>();
                resources.SetResourceValuesForTests(5f, 100f);

                Assert.IsTrue(resources.TrySpendStamina(5f, "Dash"));
                Assert.IsTrue(resources.IsStaminaExhaustedForTests);
                resources.RestoreStamina(14f);
                Assert.IsFalse(resources.TrySpendStamina(1f, "Dash"), "Non-sprint actions stay locked below 20 stamina.");
                Assert.IsFalse(resources.TrySpendStamina(1f, "Sprint"), "Held sprint waits until 15 stamina.");
                resources.RestoreStamina(1f);
                Assert.IsTrue(resources.TrySpendStamina(1f, "Sprint"), "Held sprint can resume at 15 stamina.");
                Assert.IsFalse(resources.TrySpendStamina(1f, "Dash"), "Other stamina actions remain locked until 20 stamina.");
                resources.RestoreStamina(6f);
                Assert.IsTrue(resources.TrySpendStamina(1f, "Dash"));
                Assert.IsFalse(resources.IsStaminaExhaustedForTests);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void CompassUtility_MapsUnityYawToDungeonCardinals()
        {
            Assert.AreEqual("N", DungeonDirectionUtility.GetCardinalLabel(0f));
            Assert.AreEqual("NE", DungeonDirectionUtility.GetCardinalLabel(45f));
            Assert.AreEqual("E", DungeonDirectionUtility.GetCardinalLabel(90f));
            Assert.AreEqual("S", DungeonDirectionUtility.GetCardinalLabel(180f));
            Assert.AreEqual("W", DungeonDirectionUtility.GetCardinalLabel(270f));
            Assert.AreEqual("NW", DungeonDirectionUtility.GetCardinalLabel(-45f));
        }

        [TestCase(0f, "N")]
        [TestCase(45f, "NE")]
        [TestCase(90f, "E")]
        [TestCase(180f, "S")]
        [TestCase(270f, "W")]
        [TestCase(359f, "N")]
        public void CompassStrip_CentersExpectedHeadingAndWraps(float yaw, string expectedHeading)
        {
            GameObject hud = new GameObject("Hud", typeof(RectTransform));
            try
            {
                CompassHudView compass = hud.AddComponent<CompassHudView>();
                compass.UpdateCompassForTests(yaw);

                Assert.AreEqual(expectedHeading, compass.CenterHeadingForTests);
                Assert.AreEqual(0f, CompassHudView.GetHeadingOffsetForYaw(yaw, yaw, 4.2f), 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(hud);
            }
        }

        [Test]
        public void CompassLocationLabel_UsesSceneContextInsteadOfBlindRunFloor()
        {
            Assert.IsFalse(CompassHudView.ShouldShowLocationLabelForTests(WorldLocationKind.MainMenu));
            Assert.AreEqual(string.Empty, CompassHudView.GetLocationLabelForTests(WorldLocationKind.MainMenu, 1));
            Assert.IsTrue(CompassHudView.ShouldShowLocationLabelForTests(WorldLocationKind.Settlement));
            Assert.AreEqual("World Floor 1 - Frontier Outpost\nSettlement: Safe Zone", CompassHudView.GetLocationLabelForTests(WorldLocationKind.Settlement, 1));
            Assert.IsTrue(CompassHudView.ShouldShowLocationLabelForTests(WorldLocationKind.Labyrinth));
            Assert.AreEqual("World Floor 1 - Frontier Outpost\nTraining Labyrinth", CompassHudView.GetLocationLabelForTests(WorldLocationKind.Labyrinth, 1));
        }

        [Test]
        public void FirstPersonController_SprintDrainUsesTraversalFriendlyTuning()
        {
            GameObject player = new GameObject("Player");
            try
            {
                FirstPersonController controller = player.AddComponent<FirstPersonController>();

                Assert.AreEqual(6f, controller.SprintStaminaDrainPerSecond, 0.001f);
                Assert.AreEqual(35f, controller.DashStaminaCost, 0.001f);
                Assert.AreEqual(0f, controller.JumpStaminaCost, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void InputDefaults_AddFullMapAndDepthSenseBindingWithoutStealingMinimap()
        {
            Assert.AreEqual(KeyCode.M.ToString(), InputBindingService.GetDefaultRecord(GameplayInputAction.ToggleFullMap).primary);
            Assert.AreEqual(KeyCode.C.ToString(), InputBindingService.GetDefaultRecord(GameplayInputAction.ManaSense).primary);
            Assert.AreEqual(KeyCode.None.ToString(), InputBindingService.GetDefaultRecord(GameplayInputAction.Minimap).primary);
        }

        [Test]
        public void FirstPersonController_JumpStaminaCostDefaultsToZero()
        {
            GameObject player = new GameObject("Player");
            try
            {
                FirstPersonController controller = player.AddComponent<FirstPersonController>();

                Assert.AreEqual(0f, controller.JumpStaminaCost);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void FallbackGraph_AssignsDeterministicZoneAndRoleMetadata()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph graph = generator.GenerateFallback(new FloorState { floorIndex = 3, floorSeed = 123 });

            DungeonNode entry = graph.GetNode(graph.entryHubNodeId);
            DungeonNode down = graph.GetNode(graph.transitDownNodeId);
            DungeonNode secret = graph.GetNode("secret_0");

            Assert.AreEqual(DungeonZoneType.Entrance, entry.zoneType);
            Assert.AreEqual(DungeonRoomRole.Start, entry.roomRole);
            Assert.AreEqual("entrance_0", entry.zoneId);
            Assert.AreEqual(DungeonRoomRole.Exit, down.roomRole);
            Assert.AreEqual(DungeonRoomRole.Secret, secret.roomRole);
            Assert.AreEqual("secret_network_0", secret.zoneId);
        }

        [Test]
        public void MetadataCopyAndPurposeOverride_DoNotOverwriteProtectedStructure()
        {
            DungeonNode transitDown = new DungeonNode { nodeKind = DungeonNodeKind.TransitDown, nodeId = "down" };
            DungeonLayoutGraph graph = new DungeonLayoutGraph
            {
                entryHubNodeId = "entry",
                nodes =
                {
                    new DungeonNode { nodeId = "entry", nodeKind = DungeonNodeKind.EntryHub },
                    transitDown
                },
                edges =
                {
                    new DungeonEdge { a = "entry", b = "down" }
                }
            };
            DungeonMetadataUtility.ApplyGraphDefaults(graph, 4, 123);
            DungeonRoomBuildRecord record = new DungeonRoomBuildRecord { roomType = DungeonNodeKind.TransitDown };
            DungeonMetadataUtility.CopyNodeMetadata(transitDown, record);
            DungeonMetadataUtility.ApplyPurposeMetadata(record, RoomPurposeCatalog.Get("gold_treasury"), 4);

            Assert.AreEqual(DungeonRoomRole.Exit, record.roomRole);
            Assert.AreEqual(DungeonZoneType.BossWing, record.zoneType);
        }

        [Test]
        public void PurposeOverride_OrdinaryTreasuryBecomesTreasureMetadata()
        {
            DungeonRoomBuildRecord record = new DungeonRoomBuildRecord
            {
                roomType = DungeonNodeKind.Ordinary,
                roomRole = DungeonRoomRole.Combat,
                zoneType = DungeonZoneType.ForgottenHalls
            };

            DungeonMetadataUtility.ApplyPurposeMetadata(record, RoomPurposeCatalog.Get("gold_treasury"), 6);

            Assert.AreEqual(DungeonRoomRole.Treasure, record.roomRole);
            Assert.AreEqual(DungeonZoneType.Treasury, record.zoneType);
            Assert.GreaterOrEqual(record.dangerTier, 0);
        }
    }
}
