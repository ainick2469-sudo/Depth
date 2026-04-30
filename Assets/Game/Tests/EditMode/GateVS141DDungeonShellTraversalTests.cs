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
    public sealed class GateVS141DDungeonShellTraversalTests
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
        public void AdapterVisuals_ValidateBeforeHidingGrayboxAndSkipRiskyDoorwayVisuals()
        {
            GameObject root = Track(new GameObject("ShellTruthDefaultAdapter"));

            DungeonBuildResult build = InvokeBuildFloorAttempt(root, DungeonShellVisualMode.AdapterVisuals, useFallback: false, floorIndex: 1, floorSeed: 1932105958);
            DungeonShellVisualTruthReport report = build.shellVisualReport;

            Assert.NotNull(report);
            Assert.AreEqual(DungeonShellVisualMode.AdapterVisuals, report.activeMode, report.ToSummaryString());
            Assert.IsFalse(report.fallbackTriggered, report.ToSummaryString());
            Assert.Greater(report.spawnedWallVisualCount, 0);
            Assert.Greater(report.spawnedFloorVisualCount + report.spawnedCorridorVisualCount, 0);
            Assert.Greater(report.skippedDoorwayVisualCount, 0, "Solid DoorwayVisual wrappers must be skipped for this gate.");
            Assert.AreEqual(0, report.violationCount, report.ToSummaryString());
            Assert.AreEqual(0, report.visuals.Count(visual => visual.kind == DungeonShellVisualKind.Doorway));
            Assert.Greater(CountDisabledSourceRenderers(report), 0, "Graybox renderers should hide only after AdapterVisuals passes validation.");
        }

        [Test]
        public void SafeGrayboxMode_DoesNotSpawnShellOrHideGrayboxRenderers()
        {
            GameObject root = Track(new GameObject("ShellTruthSafeGraybox"));

            DungeonBuildResult build = InvokeBuildFloorAttempt(root, DungeonShellVisualMode.SafeGraybox, useFallback: true, floorIndex: 1, floorSeed: 4400);

            Assert.AreEqual(DungeonShellVisualMode.SafeGraybox, build.shellVisualReport.activeMode);
            Assert.AreEqual(0, build.shellVisualReport.spawnedVisualCount);
            Assert.AreEqual(0, CountDescendantsNamed(root.transform, "ShellVisual_"));
            Assert.AreEqual(0, CountDisabledWallRenderers(root.transform));
        }

        [Test]
        public void ValidationFailure_DestroysShellRootAndLeavesGrayboxVisible()
        {
            GameObject root = Track(new GameObject("ShellTruthForcedFallback"));

            DungeonBuildResult build = InvokeBuildFloorAttempt(
                root,
                DungeonShellVisualMode.AdapterVisuals,
                useFallback: true,
                floorIndex: 1,
                floorSeed: 4400,
                forceShellFailure: true);

            Assert.AreEqual(DungeonShellVisualMode.SafeGraybox, build.shellVisualReport.activeMode);
            Assert.IsTrue(build.shellVisualReport.fallbackTriggered);
            Assert.Greater(build.shellVisualReport.violationCount, 0);
            Assert.AreEqual(0, CountDescendantsNamed(root.transform, "ShellVisual_"));
            Assert.AreEqual(0, CountDescendantsNamed(root.transform, "DungeonShellVisuals"));
            Assert.AreEqual(0, CountDisabledWallRenderers(root.transform));
        }

        [Test]
        public void WallVisuals_AreSourceOwnedNonCollidingAndClearOfDoorwaysAndCorridors()
        {
            GameObject root = Track(new GameObject("ShellTruthClearance"));

            DungeonBuildResult build = InvokeBuildFloorAttempt(root, DungeonShellVisualMode.AdapterVisuals, useFallback: false, floorIndex: 1, floorSeed: 778287037);

            foreach (DungeonShellVisualSpawnRecord visual in build.shellVisualReport.visuals)
            {
                Assert.AreEqual(0, visual.visualObject.GetComponentsInChildren<Collider>(true).Length, $"{visual.visualId} should remain visual-only.");
                if (visual.kind != DungeonShellVisualKind.Wall)
                {
                    continue;
                }

                Assert.IsTrue(visual.sourceOwned, visual.visualId);
                Assert.IsTrue(visual.sourceIsBlocking, visual.visualId);
                Assert.That(visual.sourceId, Is.Not.Empty, visual.visualId);
                AssertWallClearOfDoorwaysAndCorridors(visual, build);
            }
        }

        [Test]
        public void ProceduralCoverage_ValidatesShellTruthAcrossMultipleFloorsAndSeeds()
        {
            int[] floors = { 1, 3, 5, 8 };
            int[] seeds = { 4400, 778287037, 1932105958, 10477, 20901, 31415, 45001, 59000, 73123, 88001 };

            foreach (int floor in floors)
            {
                foreach (int seed in seeds)
                {
                    GameObject root = Track(new GameObject($"ShellTruthFloor{floor}Seed{seed}"));
                    DungeonBuildResult build = InvokeBuildFloorAttempt(root, DungeonShellVisualMode.AdapterVisuals, useFallback: false, floorIndex: floor, floorSeed: seed);
                    DungeonValidationReport validation = DungeonValidator.Validate(build);

                    Assert.IsTrue(validation.IsValid, validation.ToSummaryString(build, 10));
                    Assert.AreEqual(DungeonShellVisualMode.AdapterVisuals, build.shellVisualReport.activeMode, build.shellVisualReport.ToSummaryString());
                    Assert.AreEqual(0, build.shellVisualReport.violationCount, build.shellVisualReport.ToSummaryString());
                    Assert.AreEqual(1, CountDescendantsNamed(root.transform, "DungeonShellVisuals"), "Rebuild should own exactly one shell root.");
                    Assert.IsNotEmpty(build.GetSpawnPoints(build.playerSpawnNodeId, DungeonSpawnPointCategory.PlayerSpawn));
                    Assert.IsNotEmpty(build.interactables, "Stairs/pickups/room interactables should survive shell validation.");
                }
            }
        }

        [Test]
        public void WorldLabels_ClampScaleHideWhenTooCloseAndConfigureOcclusion()
        {
            Assert.AreEqual(0.55f, WorldLabelBillboard.CalculateScaleForDistance(0.5f, 0.55f, 0.95f, 18f), 0.001f);
            Assert.AreEqual(0.95f, WorldLabelBillboard.CalculateScaleForDistance(100f, 0.55f, 0.95f, 18f), 0.001f);

            GameObject root = Track(new GameObject("LabelClampRoot"));
            WorldLabelBillboard label = WorldLabelBillboard.Create(
                root.transform,
                "Label",
                "Readable",
                Vector3.up,
                Color.white,
                20f,
                true);

            Assert.LessOrEqual(label.MaxScale, 0.95f);
            Assert.GreaterOrEqual(label.MinVisibleDistance, 1.35f);
            Assert.IsTrue(label.UseOcclusion);
            Assert.AreEqual(root.transform, label.OcclusionRoot);
            Quaternion rotation = WorldLabelBillboard.GetBillboardRotation(new Vector3(0f, 1f, -4f), Vector3.up);
            Assert.Greater(Vector3.Dot(rotation * Vector3.forward, Vector3.forward), 0.95f);
        }

        private GameObject Track(GameObject gameObject)
        {
            cleanup.Add(gameObject);
            return gameObject;
        }

        private static void AssertWallClearOfDoorwaysAndCorridors(DungeonShellVisualSpawnRecord visual, DungeonBuildResult build)
        {
            for (int i = 0; i < build.doorOpenings.Count; i++)
            {
                Assert.IsFalse(
                    visual.bounds.Intersects(DungeonSceneController.GetDoorwayShellClearanceBounds(build.doorOpenings[i])),
                    $"{visual.visualId} intersects doorway {build.doorOpenings[i].openingId}");
            }

            for (int i = 0; i < build.corridors.Count; i++)
            {
                Assert.IsFalse(
                    visual.bounds.Intersects(DungeonSceneController.GetCorridorShellClearanceBounds(build.corridors[i])),
                    $"{visual.visualId} intersects corridor {build.corridors[i].edgeKey}:{build.corridors[i].segmentIndex}");
            }
        }

        private static int CountDisabledSourceRenderers(DungeonShellVisualTruthReport report)
        {
            int count = 0;
            for (int i = 0; i < report.visuals.Count; i++)
            {
                Renderer renderer = report.visuals[i].sourceRenderer;
                if (renderer != null && !renderer.enabled)
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
