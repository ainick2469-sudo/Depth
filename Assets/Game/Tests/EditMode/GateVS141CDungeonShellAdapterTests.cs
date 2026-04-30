using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FrontierDepths.Core;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS141CDungeonShellAdapterTests
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
        public void Catalog_DefinesEveryRequiredVisualKindWithDungeonVisualPaths()
        {
            DungeonShellVisualKind[] kinds = (DungeonShellVisualKind[])System.Enum.GetValues(typeof(DungeonShellVisualKind));

            Assert.AreEqual(kinds.Length, DungeonShellVisualCatalog.All.Count);
            foreach (DungeonShellVisualKind kind in kinds)
            {
                Assert.IsTrue(DungeonShellVisualCatalog.TryGet(kind, out DungeonShellVisualDefinition definition), kind.ToString());
                Assert.AreEqual(kind, definition.kind);
                Assert.That(definition.displayName, Is.Not.Empty);
                Assert.That(definition.preferredResourcePath, Does.StartWith("DungeonVisuals/"));
                Assert.IsTrue(definition.visualOnly);
                Assert.IsTrue(definition.stripPrefabColliders);
            }
        }

        [Test]
        public void WrapperPrefabs_ExistUnderDungeonVisualResourcesAndAreVisualOnly()
        {
            foreach (DungeonShellVisualDefinition definition in DungeonShellVisualCatalog.All)
            {
                GameObject prefab = Resources.Load<GameObject>(definition.preferredResourcePath);

                Assert.NotNull(prefab, definition.preferredResourcePath);
                Assert.Greater(prefab.GetComponentsInChildren<Renderer>(true).Length, 0, definition.displayName);
                Assert.AreEqual(0, prefab.GetComponentsInChildren<Collider>(true).Length, $"{definition.displayName} wrapper should not carry gameplay colliders.");
            }
        }

        [Test]
        public void Resolver_MissingResourceFallsBackAndWarnsOnce()
        {
            DungeonShellVisualResolver.ResetWarningsForTests();
            DungeonShellVisualDefinition missing = new DungeonShellVisualDefinition(
                DungeonShellVisualKind.RoomAccent,
                "Missing Test",
                "DungeonVisuals/__Missing",
                Color.white,
                Vector3.one,
                visualOnly: true,
                stripPrefabColliders: true,
                warningLabel: "Missing Test");
            GameObject parent = Track(new GameObject("MissingDungeonShellVisualParent"));

            LogAssert.Expect(LogType.Warning, new Regex("Dungeon shell visual resource missing: DungeonVisuals/__Missing"));
            GameObject first = DungeonShellVisualResolver.InstantiateVisual(missing, parent.transform, Vector3.zero, Vector3.one, Quaternion.identity, out bool firstUsedWrapper);
            GameObject second = DungeonShellVisualResolver.InstantiateVisual(missing, parent.transform, Vector3.one, Vector3.one, Quaternion.identity, out bool secondUsedWrapper);

            Assert.IsNull(first);
            Assert.IsNull(second);
            Assert.IsFalse(firstUsedWrapper);
            Assert.IsFalse(secondUsedWrapper);
            Assert.AreEqual(1, DungeonShellVisualResolver.MissingWarningCountForTests);
        }

        [Test]
        public void Resolver_LoadsPresentWrapperWithoutGameplayColliders()
        {
            GameObject parent = Track(new GameObject("LoadedDungeonShellVisualParent"));

            Assert.IsTrue(DungeonShellVisualResolver.TryInstantiateVisual(
                DungeonShellVisualKind.Floor,
                parent.transform,
                new Vector3(1f, 2f, 3f),
                new Vector3(4f, 0.5f, 6f),
                Quaternion.Euler(0f, 45f, 0f),
                out GameObject visual));

            Assert.NotNull(visual);
            Assert.AreEqual("ShellVisual_Floor", visual.name);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), visual.transform.localPosition);
            Assert.AreEqual(0, visual.GetComponentsInChildren<Collider>(true).Length);
            Assert.Greater(visual.GetComponentsInChildren<Renderer>(true).Length, 0);
        }

        [Test]
        public void DungeonBuild_WithShellVisualsPreservesGameplayRecords()
        {
            GameObject root = Track(new GameObject("DungeonShellAdapterBuildTest"));

            DungeonBuildResult build = InvokeBuildFloorAttempt(root, useFallback: false, floorSeed: 1932105958);
            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsTrue(report.IsValid, report.ToSummaryString(build, 10));
            Assert.Greater(build.rooms.Count, 0);
            Assert.Greater(build.corridors.Count, 0);
            Assert.Greater(build.doorOpenings.Count, 0);
            Assert.Greater(build.interactables.Count, 0);
            Assert.Greater(build.spawnPoints.Count, 0);
            Assert.Greater(CountDescendantsNamed(root.transform, "ShellVisual_"), 0, "Wrapper visuals should decorate the build without becoming gameplay records.");
            Assert.Greater(CountDescendantsNamed(root.transform, "FloorCollision"), 0, "Existing collision floors remain authoritative.");
            Assert.AreEqual(0, CountDescendantsNamed(root.transform, "DungeonShellVisualRoot"), "Adapter should not leave duplicate shell root containers.");
        }

        [Test]
        public void DungeonRebuild_DoesNotLeaveDuplicateShellRootsOrOldBuildVisuals()
        {
            GameObject root = Track(new GameObject("DungeonShellAdapterRebuildTest"));

            DungeonBuildResult first = InvokeBuildFloorAttempt(root, useFallback: true, floorSeed: 4400);
            int firstShellCount = CountDescendantsNamed(root.transform, "ShellVisual_");
            DungeonBuildResult second = InvokeBuildFloorAttempt(root, useFallback: true, floorSeed: 4400);
            int secondShellCount = CountDescendantsNamed(root.transform, "ShellVisual_");

            Assert.IsTrue(DungeonValidator.Validate(first).IsValid);
            Assert.IsTrue(DungeonValidator.Validate(second).IsValid);
            Assert.Greater(firstShellCount, 0);
            Assert.AreEqual(firstShellCount, secondShellCount, "Rebuild should clear previous shell visuals instead of accumulating duplicates.");
            Assert.AreEqual(0, CountDescendantsNamed(root.transform, "DungeonShellVisualRoot"));
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
                if (transforms[i].name.StartsWith(namePrefix))
                {
                    count++;
                }
            }

            return count;
        }

        private static DungeonBuildResult InvokeBuildFloorAttempt(GameObject root, bool useFallback, int floorSeed)
        {
            DungeonSceneController controller = root.GetComponent<DungeonSceneController>() ?? root.AddComponent<DungeonSceneController>();
            typeof(DungeonSceneController).GetField("runtimeRoot", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(controller, root.transform);
            typeof(DungeonSceneController).GetField("roomSpacing", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(controller, DungeonSceneController.NormalizeRoomSpacing(0f));

            MethodInfo method = typeof(DungeonSceneController).GetMethod("BuildFloorAttempt", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, "Expected DungeonSceneController.BuildFloorAttempt to exist.");

            FloorState state = new FloorState { floorIndex = 1, floorSeed = floorSeed };
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
