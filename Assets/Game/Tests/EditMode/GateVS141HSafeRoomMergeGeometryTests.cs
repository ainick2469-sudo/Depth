using System.Collections.Generic;
using System.Linq;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS141HSafeRoomMergeGeometryTests
    {
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            DungeonRewardChoiceController controller = Object.FindAnyObjectByType<DungeonRewardChoiceController>();
            if (controller != null)
            {
                Object.DestroyImmediate(controller.gameObject);
            }
        }

        [Test]
        public void ShapePlan_PrioritizesLargeOrIrregularRoomsForObjectiveAndBossMetadata()
        {
            DungeonLayoutGraph graph = CreateShapeGraph(6);
            DungeonLabyrinthObjectivePlan plan = DungeonLabyrinthObjectiveUtility.BuildObjectivePlan(graph, 6);

            DungeonRoomShapeUtility.ApplyTemplateShapePlan(graph, plan, 6, 4242);
            plan = DungeonLabyrinthObjectiveUtility.BuildObjectivePlan(graph, 6);

            AssertLargeOrIrregular(graph.GetNode(plan.objectiveRoomId));
            AssertLargeOrIrregular(graph.GetNode(plan.bossRoomId));
            AssertLargeOrIrregular(graph.GetNode(plan.bossApproachRoomId));
        }

        [Test]
        public void CompoundPlan_SuppressesFloorOneMergesAndExcludesProtectedRooms()
        {
            DungeonBuildResult floorOne = CreateMergeBuild(1);
            DungeonRoomShapeUtility.BuildCompoundPlan(floorOne);

            Assert.AreEqual(0, floorOne.compoundRooms.Count);

            DungeonBuildResult floorFive = CreateMergeBuild(5);
            DungeonRoomShapeUtility.BuildCompoundPlan(floorFive);

            foreach (DungeonRoomCompoundRecord compound in floorFive.compoundRooms)
            {
                Assert.IsFalse(compound.sourceRoomIds.Contains("entry"));
                Assert.IsFalse(compound.sourceRoomIds.Contains("up"));
                Assert.IsFalse(compound.sourceRoomIds.Contains("down"));
                Assert.IsFalse(compound.sourceRoomIds.Contains(floorFive.labyrinthObjectivePlan.objectiveRoomId));
                Assert.IsFalse(compound.sourceRoomIds.Contains(floorFive.labyrinthObjectivePlan.bossRoomId));
            }
        }

        [Test]
        public void SafeCompoundConnector_MarksSourceRoomsAndCorridorWithoutBreakingObjectiveMetadata()
        {
            DungeonBuildResult build = CreateMergeBuild(6);
            DungeonRoomShapeUtility.BuildCompoundPlan(build);

            Assert.Greater(build.compoundRooms.Count, 0);
            DungeonRoomCompoundRecord compound = build.compoundRooms[0];
            build.corridors.Add(new DungeonCorridorBuildRecord
            {
                edgeKey = compound.connectorEdgeKey,
                fromNodeId = compound.sourceRoomIds[0],
                toNodeId = compound.sourceRoomIds[1],
                length = 30f,
                width = DungeonRoomShapeUtility.GetCompoundConnectorWidth(12f),
                bounds = new Bounds(Vector3.zero, new Vector3(30f, 1f, 22f))
            });

            DungeonLayoutQualityReport report = DungeonLayoutQualityUtility.Analyze(build);

            Assert.GreaterOrEqual(report.mergeAppliedCount, 1);
            Assert.GreaterOrEqual(report.mergedRoomCount, 2);
            Assert.Greater(report.largestMergedRoomArea, 0f);
            Assert.IsTrue(build.FindRoom(compound.sourceRoomIds[0]).isMergedRoom);
            Assert.IsTrue(build.FindRoom(compound.sourceRoomIds[1]).isMergedRoom);
            Assert.IsTrue(build.corridors.Any(corridor => corridor.isCompoundConnector));
            Assert.IsTrue(build.FindRoom(build.labyrinthObjectivePlan.objectiveRoomId).isObjectiveRoom);
            Assert.IsTrue(build.FindRoom(build.labyrinthObjectivePlan.bossRoomId).isBossRoomPlaceholder);
        }

        [Test]
        public void LayoutReport_CountsExpandedIrregularRoomsAndMergeRejections()
        {
            DungeonBuildResult build = CreateMergeBuild(8);
            build.FindRoom("objective").templateKind = DungeonRoomTemplateKind.CrossChamberSafe;
            build.FindRoom("objective").isExpandedRoom = true;
            build.FindRoom("merge_a").templateKind = DungeonRoomTemplateKind.AlcoveRoomSafe;
            build.FindRoom("merge_a").isExpandedRoom = true;
            DungeonRoomShapeUtility.BuildCompoundPlan(build);

            DungeonLayoutQualityReport report = DungeonLayoutQualityUtility.Analyze(build);

            Assert.GreaterOrEqual(report.expandedRoomCount, 2);
            Assert.GreaterOrEqual(report.irregularRoomCount, 2);
            Assert.GreaterOrEqual(report.mergeAppliedCount, 1);
            Assert.GreaterOrEqual(report.mergeRejectedCount, 1);
            Assert.IsTrue(report.ToSummaryString().Contains("irregular="));
        }

        [Test]
        public void RewardModal_MousePathSelectsOnceUnlocksCursorAndAppliesReward()
        {
            RunState run = CreateRunState();
            List<RunUpgradeDefinition> choices = RunUpgradeCatalog.CreateRewardChoicesForFloor(run, 1, 3);
            int descendCount = 0;
            GameObject host = new GameObject("RewardModalTest");
            DungeonRewardChoiceController controller = host.AddComponent<DungeonRewardChoiceController>();

            controller.BeginForTests(choices, () => descendCount++);

            Assert.IsTrue(DungeonRewardChoiceController.IsRewardChoiceActive);
            Assert.IsTrue(controller.ActiveForTests);
            Assert.AreEqual(3, controller.ChoiceCountForTests);
            Assert.IsTrue(controller.CursorUnlockedForTests);

            Assert.IsTrue(controller.TrySelectChoiceForTests(0, run));
            Assert.AreEqual(1, descendCount);
            Assert.IsFalse(controller.ActiveForTests);
            Assert.IsTrue(run.currentFloor.rewardGranted);
            Assert.AreEqual(1, run.GetUpgradeStackCount(choices[0].upgradeId));
            Assert.IsFalse(controller.TrySelectChoiceForTests(1, run));
        }

        private static void AssertLargeOrIrregular(DungeonNode node)
        {
            Assert.NotNull(node);
            DungeonRoomSizeTier tier = DungeonRoomTemplateLibrary.GetSizeTier(node.roomTemplate);
            Assert.IsTrue(
                tier == DungeonRoomSizeTier.Large ||
                tier == DungeonRoomSizeTier.Grand ||
                DungeonRoomShapeUtility.IsIrregularTemplate(node.roomTemplate),
                node.roomTemplate.ToString());
        }

        private static DungeonLayoutGraph CreateShapeGraph(int floor)
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph
            {
                entryHubNodeId = "entry",
                entryNodeId = "entry",
                transitUpNodeId = "up",
                returnAnchorNodeId = "up",
                transitDownNodeId = "down",
                stairsNodeId = "down"
            };
            AddNode(graph, "entry", DungeonNodeKind.EntryHub, Vector2Int.zero);
            AddNode(graph, "up", DungeonNodeKind.TransitUp, new Vector2Int(1, 0));
            AddNode(graph, "main_a", DungeonNodeKind.Ordinary, new Vector2Int(2, 0));
            AddNode(graph, "approach", DungeonNodeKind.Ordinary, new Vector2Int(3, 0));
            AddNode(graph, "boss", DungeonNodeKind.Ordinary, new Vector2Int(4, 0));
            AddNode(graph, "down", DungeonNodeKind.TransitDown, new Vector2Int(5, 0));
            AddNode(graph, "branch", DungeonNodeKind.Ordinary, new Vector2Int(3, 1));
            AddNode(graph, "objective", DungeonNodeKind.Ordinary, new Vector2Int(3, 2));
            AddEdge(graph, "entry", "up");
            AddEdge(graph, "up", "main_a");
            AddEdge(graph, "main_a", "approach");
            AddEdge(graph, "approach", "boss");
            AddEdge(graph, "boss", "down");
            AddEdge(graph, "approach", "branch");
            AddEdge(graph, "branch", "objective");
            DungeonMetadataUtility.ApplyGraphDefaults(graph, floor, 4242);
            return graph;
        }

        private static DungeonBuildResult CreateMergeBuild(int floor)
        {
            DungeonLayoutGraph graph = CreateShapeGraph(floor);
            AddNode(graph, "merge_a", DungeonNodeKind.Ordinary, new Vector2Int(2, -1));
            AddNode(graph, "merge_b", DungeonNodeKind.Ordinary, new Vector2Int(2, -2));
            AddEdge(graph, "main_a", "merge_a");
            AddEdge(graph, "merge_a", "merge_b");
            DungeonMetadataUtility.ApplyGraphDefaults(graph, floor, 1234);
            DungeonLabyrinthObjectivePlan plan = DungeonLabyrinthObjectiveUtility.BuildObjectivePlan(graph, floor);
            plan.objectiveRoomId = "objective";
            plan.bossApproachRoomId = "approach";
            plan.bossRoomId = "boss";
            plan.exitStairsRoomId = "down";

            DungeonBuildResult build = new DungeonBuildResult
            {
                graph = graph,
                floorIndex = floor,
                seed = 1234,
                entryNodeId = "entry",
                transitUpNodeId = "up",
                transitDownNodeId = "down",
                labyrinthObjectivePlan = plan
            };

            for (int i = 0; i < graph.edges.Count; i++)
            {
                DungeonEdge edge = graph.edges[i];
                build.graphEdges.Add(new DungeonGraphEdgeRecord
                {
                    edgeKey = DungeonBuildResult.GetEdgeKey(edge.a, edge.b),
                    a = edge.a,
                    b = edge.b
                });
            }

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                DungeonRoomBuildRecord room = new DungeonRoomBuildRecord
                {
                    nodeId = node.nodeId,
                    roomType = node.nodeKind,
                    roomRole = node.roomRole,
                    templateKind = node.roomTemplate,
                    bounds = new Bounds(new Vector3(node.gridPosition.x * 26f, 0f, node.gridPosition.y * 26f), GetRoomSize(node.nodeId))
                };
                DungeonLabyrinthObjectiveUtility.ApplyObjectivePlanToRoom(room, plan);
                room.footprintArea = room.bounds.size.x * room.bounds.size.z;
                build.rooms.Add(room);
            }

            return build;
        }

        private static Vector3 GetRoomSize(string nodeId)
        {
            return nodeId == "objective" || nodeId == "boss"
                ? new Vector3(42f, 8f, 42f)
                : new Vector3(28f, 8f, 28f);
        }

        private static void AddNode(DungeonLayoutGraph graph, string id, DungeonNodeKind kind, Vector2Int position)
        {
            graph.nodes.Add(new DungeonNode
            {
                nodeId = id,
                label = id,
                nodeKind = kind,
                gridPosition = position,
                roomTemplate = DungeonRoomTemplateKind.SquareChamber
            });
        }

        private static void AddEdge(DungeonLayoutGraph graph, string a, string b)
        {
            graph.edges.Add(new DungeonEdge { a = a, b = b });
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
    }
}
