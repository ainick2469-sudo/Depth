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

                Assert.IsTrue(resources.TrySpendMana(25f, "Depth Sense"));
                Assert.AreEqual(75f, resources.CurrentMana, 0.01f);
                resources.RestoreMana(10f);
                Assert.AreEqual(85f, resources.CurrentMana, 0.01f);
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

        [Test]
        public void InputDefaults_AddFullMapAndManaSenseWithoutStealingMinimap()
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
