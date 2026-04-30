using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FrontierDepths.Core;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS141ECuratedDungeonVisualTests
    {
        private readonly List<GameObject> cleanup = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = cleanup.Count - 1; i >= 0; i--)
            {
                if (cleanup[i] != null)
                {
                    Object.DestroyImmediate(cleanup[i]);
                }
            }

            cleanup.Clear();
            DungeonShellVisualResolver.ResetWarningsForTests();
        }

        [Test]
        public void Catalog_AddsCuratedSafeKindsAndTrainingProfileWithoutBreakingLegacyKinds()
        {
            Assert.AreEqual(DungeonShellVisualProfile.TrainingLabyrinth, DungeonShellVisualCatalog.GetActiveProfile());
            AssertCatalogKind(DungeonShellVisualKind.Floor);
            AssertCatalogKind(DungeonShellVisualKind.Wall);
            AssertCatalogKind(DungeonShellVisualKind.Corridor);
            AssertCatalogKind(DungeonShellVisualKind.RoomFloor);
            AssertCatalogKind(DungeonShellVisualKind.CorridorFloor);
            AssertCatalogKind(DungeonShellVisualKind.RoomWall);
            AssertCatalogKind(DungeonShellVisualKind.DoorwaySideTrim);
            AssertCatalogKind(DungeonShellVisualKind.StairMarker);
            AssertCatalogKind(DungeonShellVisualKind.RoomPurposeFloorTint);
        }

        [Test]
        public void AdapterVisuals_SpawnSafeFloorsTrimsAndPurposeMarkers()
        {
            GameObject root = Track(new GameObject("CuratedDungeonVisuals"));

            DungeonBuildResult build = InvokeBuildFloorAttempt(root, DungeonShellVisualMode.AdapterVisuals, useFallback: false, floorIndex: 4, floorSeed: 1932105958);
            DungeonShellVisualTruthReport report = build.shellVisualReport;

            Assert.NotNull(report);
            Assert.AreEqual(DungeonShellVisualMode.AdapterVisuals, report.activeMode, report.ToSummaryString());
            Assert.AreEqual(0, report.violationCount, report.ToSummaryString());
            Assert.Greater(report.spawnedFloorVisualCount, 0);
            Assert.Greater(report.spawnedCorridorVisualCount, 0);
            Assert.Greater(report.spawnedDoorwaySideTrimCount, 0);
            Assert.AreEqual(0, report.visuals.Count(visual => visual.kind == DungeonShellVisualKind.Doorway), "Solid doorway wrappers stay disabled.");
            Assert.AreEqual(0, report.visuals.Count(visual => visual.kind == DungeonShellVisualKind.CorridorWall), "Corridor wall wrappers stay disabled unless proven safe.");

            if (build.rooms.Any(room => !string.IsNullOrWhiteSpace(room.purposeId) && room.roomType != DungeonNodeKind.Secret))
            {
                Assert.Greater(report.spawnedPurposeVisualCount, 0, report.ToSummaryString());
            }
        }

        [Test]
        public void DoorwaySideTrim_StaysOutsideDoorwayAndCorridorClearance()
        {
            GameObject root = Track(new GameObject("CuratedDoorwayTrimClearance"));

            DungeonBuildResult build = InvokeBuildFloorAttempt(root, DungeonShellVisualMode.AdapterVisuals, useFallback: false, floorIndex: 3, floorSeed: 778287037);

            foreach (DungeonShellVisualSpawnRecord trim in build.shellVisualReport.visuals.Where(visual => visual.kind == DungeonShellVisualKind.DoorwaySideTrim))
            {
                Assert.AreEqual(0, trim.visualObject.GetComponentsInChildren<Collider>(true).Length, trim.visualId);
                AssertClearOfDoorwaysAndCorridors(trim, build);
            }
        }

        [Test]
        public void PurposeMarkers_AreNonCollidingSubtleAndDoNotRevealSecretRooms()
        {
            GameObject root = Track(new GameObject("CuratedPurposeMarkers"));

            DungeonBuildResult build = InvokeBuildFloorAttempt(root, DungeonShellVisualMode.AdapterVisuals, useFallback: false, floorIndex: 8, floorSeed: 31415);

            foreach (DungeonShellVisualSpawnRecord marker in build.shellVisualReport.visuals.Where(visual => visual.kind == DungeonShellVisualKind.RoomPurposeFloorTint || visual.kind == DungeonShellVisualKind.RoomPurposeMarker))
            {
                Assert.AreEqual(0, marker.visualObject.GetComponentsInChildren<Collider>(true).Length, marker.visualId);
                Assert.LessOrEqual(marker.bounds.size.y, 0.2f, marker.visualId);
                AssertClearOfDoorwaysAndCorridors(marker, build);
                if (!string.IsNullOrWhiteSpace(build.secretNodeId))
                {
                    Assert.That(marker.sourceId, Does.Not.Contain(build.secretNodeId), "Secret room purpose visuals must not be spawned before discovery.");
                }

                foreach (DungeonInteractableBuildRecord interactable in build.interactables)
                {
                    Assert.IsFalse(marker.bounds.Intersects(interactable.bounds), $"{marker.visualId} overlaps interactable {interactable.interactableType}");
                }
            }
        }

        [Test]
        public void SourceOwnedWrappers_PassScalePivotAuditAndStayColliderless()
        {
            GameObject root = Track(new GameObject("CuratedScalePivotAudit"));

            DungeonBuildResult build = InvokeBuildFloorAttempt(root, DungeonShellVisualMode.AdapterVisuals, useFallback: false, floorIndex: 5, floorSeed: 4400);

            foreach (DungeonShellVisualSpawnRecord visual in build.shellVisualReport.visuals.Where(visual => visual.sourceOwned))
            {
                Assert.AreEqual(0, visual.visualObject.GetComponentsInChildren<Collider>(true).Length, visual.visualId);
                if (visual.kind == DungeonShellVisualKind.RoomFloor || visual.kind == DungeonShellVisualKind.CorridorFloor)
                {
                    Assert.LessOrEqual(visual.bounds.size.y, 0.04f, visual.visualId);
                    Assert.LessOrEqual(visual.floorVisualHeightAboveSurface, 0.04f, visual.visualId);
                    Assert.LessOrEqual(Mathf.Abs(visual.bounds.size.x - visual.sourceBounds.size.x), 0.08f, visual.visualId);
                    Assert.LessOrEqual(Mathf.Abs(visual.bounds.size.z - visual.sourceBounds.size.z), 0.08f, visual.visualId);
                    continue;
                }

                Assert.LessOrEqual(Vector3.Distance(visual.bounds.center, visual.sourceBounds.center), 0.06f, visual.visualId);
                Assert.LessOrEqual(Vector3.Distance(visual.bounds.size, visual.sourceBounds.size), 0.06f, visual.visualId);
            }
        }

        [Test]
        public void SafeGrayboxFallback_RemovesCuratedShellAndLeavesGrayboxVisible()
        {
            GameObject root = Track(new GameObject("CuratedForcedFallback"));

            DungeonBuildResult build = InvokeBuildFloorAttempt(root, DungeonShellVisualMode.AdapterVisuals, useFallback: true, floorIndex: 1, floorSeed: 4400, forceShellFailure: true);

            Assert.AreEqual(DungeonShellVisualMode.SafeGraybox, build.shellVisualReport.activeMode);
            Assert.IsTrue(build.shellVisualReport.fallbackTriggered);
            Assert.AreEqual(0, CountDescendantsNamed(root.transform, "ShellVisual_"));
            Assert.AreEqual(0, CountDescendantsNamed(root.transform, "DungeonShellVisuals"));
            Assert.AreEqual(0, CountDisabledWallRenderers(root.transform));
        }

        private static void AssertCatalogKind(DungeonShellVisualKind kind)
        {
            Assert.IsTrue(DungeonShellVisualCatalog.TryGet(kind, out DungeonShellVisualDefinition definition), kind.ToString());
            Assert.That(definition.preferredResourcePath, Does.StartWith("DungeonVisuals/"));
            Assert.IsTrue(definition.visualOnly);
            Assert.IsTrue(definition.stripPrefabColliders);
        }

        private static void AssertClearOfDoorwaysAndCorridors(DungeonShellVisualSpawnRecord visual, DungeonBuildResult build)
        {
            for (int i = 0; i < build.doorOpenings.Count; i++)
            {
                Assert.IsFalse(visual.bounds.Intersects(DungeonSceneController.GetDoorwayShellClearanceBounds(build.doorOpenings[i])), $"{visual.visualId} intersects doorway {build.doorOpenings[i].openingId}");
            }

            for (int i = 0; i < build.corridors.Count; i++)
            {
                Assert.IsFalse(visual.bounds.Intersects(DungeonSceneController.GetCorridorShellClearanceBounds(build.corridors[i])), $"{visual.visualId} intersects corridor {build.corridors[i].edgeKey}:{build.corridors[i].segmentIndex}");
            }
        }

        private GameObject Track(GameObject gameObject)
        {
            cleanup.Add(gameObject);
            return gameObject;
        }

        private static int CountDescendantsNamed(Transform root, string namePrefix)
        {
            int count = 0;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name.StartsWith(namePrefix, System.StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountDisabledWallRenderers(Transform root)
        {
            int count = 0;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].name.StartsWith("Wall_", System.StringComparison.Ordinal) && !renderers[i].enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private static DungeonBuildResult InvokeBuildFloorAttempt(
            GameObject root,
            DungeonShellVisualMode mode,
            bool useFallback,
            int floorIndex,
            int floorSeed,
            bool forceShellFailure = false)
        {
            DungeonSceneController controller = root.GetComponent<DungeonSceneController>() ?? root.AddComponent<DungeonSceneController>();
            controller.ShellVisualModeForTests = mode;
            controller.ForceShellVisualValidationFailureForTests = forceShellFailure;
            typeof(DungeonSceneController).GetField("runtimeRoot", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(controller, root.transform);
            typeof(DungeonSceneController).GetField("roomSpacing", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(controller, DungeonSceneController.NormalizeRoomSpacing(0f));

            MethodInfo method = typeof(DungeonSceneController).GetMethod("BuildFloorAttempt", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, "Expected DungeonSceneController.BuildFloorAttempt to exist.");

            FloorState state = new FloorState { floorIndex = floorIndex, floorSeed = floorSeed };
            state.Normalize(state.floorIndex, state.floorSeed);
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph graph;
            GraphValidationReport graphReport;
            if (useFallback)
            {
                graph = generator.GenerateFallback(state);
                graphReport = new GraphValidationReport
                {
                    floorIndex = state.floorIndex,
                    seed = state.floorSeed,
                    attemptCount = 1,
                    layoutSignature = DungeonLayoutSignatureUtility.BuildSignature(graph, state)
                };
            }
            else
            {
                Assert.IsTrue(generator.TryGenerateNormal(state, out graph, out graphReport), graphReport.ToSummaryString(10));
            }

            return (DungeonBuildResult)method.Invoke(controller, new object[] { state, graph, graphReport, useFallback, 1, 1 });
        }
    }
}
