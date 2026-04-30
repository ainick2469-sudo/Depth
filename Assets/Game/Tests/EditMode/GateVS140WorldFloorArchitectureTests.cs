using FrontierDepths.Core;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS140WorldFloorArchitectureTests
    {
        [Test]
        public void WorldFloorCatalog_SeedsExactlyFirstFiveDefinitions()
        {
            Assert.AreEqual(5, WorldFloorCatalog.All.Count);

            WorldFloorDefinition floorOne = WorldFloorCatalog.Get(1);
            WorldFloorDefinition floorTwo = WorldFloorCatalog.Get(2);
            WorldFloorDefinition floorFive = WorldFloorCatalog.Get(5);

            Assert.NotNull(floorOne);
            Assert.NotNull(floorTwo);
            Assert.NotNull(floorFive);
            Assert.AreEqual("Frontier Outpost", floorOne.floorName);
            Assert.AreEqual("Training Labyrinth", floorOne.labyrinthName);
            Assert.AreEqual("boss.candle_bound_warden", floorOne.bossId);
            Assert.AreEqual("Wolfroot Fields", floorTwo.floorName);
            Assert.AreEqual("Rootcellar Den", floorTwo.labyrinthName);
            Assert.AreEqual("Ironbell Crossing", floorFive.floorName);
            Assert.IsTrue(floorOne.hasMajorTown);
            Assert.IsTrue(floorFive.hasMajorTown);
            Assert.IsFalse(floorTwo.hasMajorTown);
            Assert.IsTrue(floorTwo.hasMinorCamp);
            Assert.IsFalse(WorldFloorCatalog.TryGet(6, out _), "Only the first five definitions should be seeded in this gate.");
        }

        [Test]
        public void WorldFloorProgression_DefaultsOlderProfilesToFloorOneOnly()
        {
            ProfileState profile = new ProfileState
            {
                worldFloorProgression = null
            };
            profile.Normalize();

            WorldFloorProgressionService service = new WorldFloorProgressionService(profile);

            Assert.AreEqual(1, service.CurrentWorldFloor);
            Assert.AreEqual(1, service.HighestUnlockedWorldFloor);
            Assert.IsTrue(service.IsFloorUnlocked(1));
            Assert.IsFalse(service.IsFloorUnlocked(2));
            Assert.IsFalse(service.IsFloorCleared(1));
        }

        [Test]
        public void WorldFloorProgression_UnlockBossDefeatAndSeedsPersistThroughProfileJson()
        {
            ProfileState profile = new ProfileState();
            profile.Normalize();
            profile.worldFloorProgression.worldSeed = 12345;
            profile.worldFloorProgression.Normalize();

            WorldFloorProgressionService service = new WorldFloorProgressionService(profile);
            int seedA = service.GetOrCreateFloorSeed(1);
            int seedB = service.GetOrCreateFloorSeed(1);

            Assert.AreEqual(seedA, seedB);
            Assert.IsFalse(service.IsFloorUnlocked(2));

            service.MarkBossDefeated(1, "boss.candle_bound_warden");

            Assert.IsTrue(service.IsBossDefeated(1));
            Assert.IsTrue(service.IsFloorCleared(1));
            Assert.IsTrue(service.IsFloorUnlocked(2));

            string json = JsonUtility.ToJson(profile);
            ProfileState loaded = JsonUtility.FromJson<ProfileState>(json);
            loaded.Normalize();
            WorldFloorProgressionService loadedService = new WorldFloorProgressionService(loaded);

            Assert.IsTrue(loadedService.IsBossDefeated(1));
            Assert.IsTrue(loadedService.IsFloorUnlocked(2));
            Assert.AreEqual(seedA, loadedService.GetOrCreateFloorSeed(1));
        }

        [Test]
        public void WorldFloorProgression_TracksKnownEntranceSettlementAndTeleportGate()
        {
            ProfileState profile = new ProfileState();
            profile.Normalize();
            WorldFloorProgressionService service = new WorldFloorProgressionService(profile);

            service.MarkLabyrinthEntranceKnown(1);
            service.MarkSettlementVisited(1, "settlement.frontier_outpost");
            service.UnlockTeleportGate(1, "gate.frontier_outpost");

            Assert.IsTrue(service.IsLabyrinthEntranceKnown(1));
            Assert.IsTrue(service.IsSettlementVisited(1, "settlement.frontier_outpost"));
            Assert.IsTrue(service.IsTeleportGateUnlocked(1, "gate.frontier_outpost"));

            WorldFloorState state = service.GetOrCreateState(1);
            Assert.IsTrue(state.labyrinthEntranceKnown);
            Assert.Contains("settlement.frontier_outpost", state.visitedSettlementIds);
            Assert.Contains("gate.frontier_outpost", state.unlockedTeleportGateIds);
        }

        [Test]
        public void WorldFloorSceneContext_FormatsMainMenuSettlementAndLabyrinthLabels()
        {
            ProfileState profile = new ProfileState();
            profile.Normalize();

            WorldFloorSceneContext mainMenu = WorldFloorSceneContext.ResolveForScene(GameSceneCatalog.MainMenu, profile);
            WorldFloorSceneContext town = WorldFloorSceneContext.ResolveForScene(GameSceneCatalog.TownHub, profile);
            WorldFloorSceneContext dungeon = WorldFloorSceneContext.ResolveForScene(GameSceneCatalog.DungeonRuntime, profile);

            Assert.IsFalse(mainMenu.ShouldShowHudLabel);
            Assert.AreEqual(string.Empty, mainMenu.FormatHudLabel());
            Assert.AreEqual(WorldLocationKind.Settlement, town.locationKind);
            Assert.AreEqual("World Floor 1 - Frontier Outpost\nSettlement: Safe Zone", town.FormatHudLabel());
            Assert.AreEqual(WorldLocationKind.Labyrinth, dungeon.locationKind);
            Assert.AreEqual("training_labyrinth", dungeon.areaId);
            Assert.AreEqual("World Floor 1 - Frontier Outpost\nTraining Labyrinth - Depth 1", dungeon.FormatHudLabel());
            Assert.AreEqual("World Floor 1 - Frontier Outpost\nTraining Labyrinth - Depth 4", WorldFloorSceneContext.Create(WorldLocationKind.Labyrinth, 1, 4).FormatHudLabel());
        }
    }
}
