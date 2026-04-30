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
    public sealed class GateVS141E1RaisedFloorVisualAlignmentTests
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
        public void AdapterVisuals_StageRoomAndCorridorFloorsAsThinFlushVeneers()
        {
            GameObject root = Track(new GameObject("RaisedFloorAlignment"));

            DungeonBuildResult build = InvokeBuildFloorAttempt(root, DungeonShellVisualMode.AdapterVisuals, useFallback: false, floorIndex: 4, floorSeed: 1932105958);
            DungeonShellVisualTruthReport report = build.shellVisualReport;

            Assert.AreEqual(DungeonShellVisualMode.AdapterVisuals, report.activeMode, report.ToSummaryString());
            Assert.Greater(report.floorVeneerCount, 0, report.ToSummaryString());
            Assert.Greater(report.corridorVeneerCount, 0, report.ToSummaryString());
            Assert.AreEqual(0, report.raisedFloorViolations, report.ToSummaryString());
            Assert.LessOrEqual(report.maxFloorVisualHeightAboveSurface, 0.04f, report.ToSummaryString());

            foreach (DungeonShellVisualSpawnRecord visual in report.visuals.Where(IsFloorVeneer))
            {
                Assert.AreEqual(0, visual.visualObject.GetComponentsInChildren<Collider>(true).Length, visual.visualId);
                Assert.LessOrEqual(visual.bounds.size.y, 0.04f, visual.visualId);
                Assert.LessOrEqual(visual.floorVisualHeightAboveSurface, 0.04f, visual.visualId);
                Assert.LessOrEqual(visual.bounds.min.y, visual.sourceBounds.max.y + 0.005f, visual.visualId);
            }
        }

        [Test]
        public void RaisedFloorVeneerValidation_RejectsAnkleHighFakeFloors()
        {
            Bounds source = new Bounds(new Vector3(0f, -0.5f, 0f), new Vector3(8f, 1f, 8f));
            Bounds raised = new Bounds(new Vector3(0f, 0.55f, 0f), new Vector3(8f, 1f, 8f));

            Assert.IsFalse(DungeonSceneController.ValidateFloorVeneerAlignmentForTests(DungeonShellVisualKind.CorridorFloor, raised, source, out float heightAboveSurface));
            Assert.Greater(heightAboveSurface, 0.04f);
        }

        [Test]
        public void SafeGrayboxFallback_StillDestroysShellRootOnValidationFailure()
        {
            GameObject root = Track(new GameObject("RaisedFloorFallback"));

            DungeonBuildResult build = InvokeBuildFloorAttempt(root, DungeonShellVisualMode.AdapterVisuals, useFallback: true, floorIndex: 1, floorSeed: 4400, forceShellFailure: true);

            Assert.AreEqual(DungeonShellVisualMode.SafeGraybox, build.shellVisualReport.activeMode);
            Assert.IsTrue(build.shellVisualReport.fallbackTriggered);
            Assert.AreEqual(0, CountDescendantsNamed(root.transform, "DungeonShellVisuals"));
        }

        private static bool IsFloorVeneer(DungeonShellVisualSpawnRecord visual)
        {
            return visual.kind == DungeonShellVisualKind.RoomFloor ||
                   visual.kind == DungeonShellVisualKind.CorridorFloor ||
                   visual.kind == DungeonShellVisualKind.StairMarker ||
                   visual.kind == DungeonShellVisualKind.RoomPurposeFloorTint ||
                   visual.kind == DungeonShellVisualKind.RoomPurposeMarker;
        }

        private GameObject Track(GameObject gameObject)
        {
            cleanup.Add(gameObject);
            return gameObject;
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
            Assert.NotNull(method);
            RunState run = new RunState
            {
                isActive = true,
                seed = floorSeed,
                floorIndex = floorIndex,
                currentFloor = new FloorState
                {
                    floorIndex = floorIndex,
                    floorSeed = floorSeed
                }
            };
            run.Normalize();

            return (DungeonBuildResult)method.Invoke(
                controller,
                new object[]
                {
                    run,
                    useFallback,
                    1,
                    1
                });
        }

        private static int CountDescendantsNamed(Transform root, string name)
        {
            int count = 0;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Contains(name))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
