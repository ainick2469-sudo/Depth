using System.Linq;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS141GLabyrinthObjectivePathTests
    {
        [Test]
        public void ObjectivePlan_SelectsReachableObjectiveBossApproachBossAndExitRooms()
        {
            DungeonBuildResult build = CreateObjectiveBuild();

            DungeonLayoutQualityReport report = DungeonLayoutQualityUtility.Analyze(build);
            DungeonLabyrinthObjectivePlan plan = build.labyrinthObjectivePlan;

            Assert.IsTrue(plan.HasObjectiveStructure, string.Join(", ", plan.warnings));
            Assert.IsTrue(report.hasObjectiveRoom);
            Assert.IsTrue(report.hasBossApproachRoom);
            Assert.IsTrue(report.hasBossRoomPlaceholder);
            Assert.IsTrue(report.exitLockMetadataPrepared);
            Assert.AreEqual("up", plan.entranceRoomId);
            Assert.AreEqual("down", plan.exitStairsRoomId);
            Assert.IsTrue(build.graph.HasPath(plan.entranceRoomId, plan.objectiveRoomId));
            Assert.IsTrue(build.graph.HasPath(plan.entranceRoomId, plan.bossRoomId));
            Assert.IsTrue(build.graph.HasPath(plan.entranceRoomId, plan.exitStairsRoomId));
        }

        [Test]
        public void ObjectiveRooms_AvoidProtectedTransitEntrySecretAndStairRooms()
        {
            DungeonBuildResult build = CreateObjectiveBuild();

            DungeonLayoutQualityUtility.Analyze(build);

            AssertObjectiveRoomIsNotProtected(build, build.labyrinthObjectivePlan.objectiveRoomId);
            AssertObjectiveRoomIsNotProtected(build, build.labyrinthObjectivePlan.bossApproachRoomId);
            AssertObjectiveRoomIsNotProtected(build, build.labyrinthObjectivePlan.bossRoomId);
            Assert.AreNotEqual("entry", build.labyrinthObjectivePlan.objectiveRoomId);
            Assert.AreNotEqual("up", build.labyrinthObjectivePlan.objectiveRoomId);
            Assert.AreNotEqual("down", build.labyrinthObjectivePlan.objectiveRoomId);
            Assert.AreNotEqual("secret", build.labyrinthObjectivePlan.objectiveRoomId);
        }

        [Test]
        public void ObjectiveMetadata_IsAppliedToRoomRecordsWithoutOverwritingRoomRole()
        {
            DungeonBuildResult build = CreateObjectiveBuild();
            DungeonLayoutQualityUtility.Analyze(build);

            DungeonRoomBuildRecord objective = build.FindRoom(build.labyrinthObjectivePlan.objectiveRoomId);
            DungeonRoomBuildRecord approach = build.FindRoom(build.labyrinthObjectivePlan.bossApproachRoomId);
            DungeonRoomBuildRecord boss = build.FindRoom(build.labyrinthObjectivePlan.bossRoomId);
            DungeonRoomBuildRecord exit = build.FindRoom(build.labyrinthObjectivePlan.exitStairsRoomId);

            Assert.IsTrue(objective.isObjectiveRoom);
            Assert.AreEqual(DungeonRoomRole.Objective, objective.objectiveRole);
            Assert.IsTrue(approach.isBossApproachRoom);
            Assert.AreEqual(DungeonRoomRole.BossApproach, approach.objectiveRole);
            Assert.IsTrue(boss.isBossRoomPlaceholder);
            Assert.AreEqual(DungeonRoomRole.BossPlaceholder, boss.objectiveRole);
            Assert.IsTrue(exit.isExitStairsRoom);
            Assert.AreEqual(DungeonRoomRole.Exit, exit.roomRole);
            Assert.AreEqual(DungeonRoomRole.Exit, exit.objectiveRole);
        }

        [Test]
        public void ReservedObjectiveRooms_DoNotReceiveRandomPurposeMetadata()
        {
            DungeonBuildResult build = CreateObjectiveBuild();
            DungeonLayoutQualityUtility.Analyze(build);

            DungeonRoomBuildRecord objective = build.FindRoom(build.labyrinthObjectivePlan.objectiveRoomId);
            objective.purposeId = string.Empty;
            objective.purposeDisplayName = string.Empty;

            Assert.IsTrue(DungeonLabyrinthObjectiveUtility.IsReservedObjectiveRoom(objective.nodeId, build.labyrinthObjectivePlan));
            Assert.IsTrue(string.IsNullOrWhiteSpace(objective.purposeId));
        }

        [Test]
        public void FallbackPlan_WarnsButKeepsExitMetadataWhenNoIdealCandidateExists()
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph
            {
                entryHubNodeId = "entry",
                transitUpNodeId = "up",
                transitDownNodeId = "down"
            };
            AddNode(graph, "entry", DungeonNodeKind.EntryHub, Vector2Int.zero);
            AddNode(graph, "up", DungeonNodeKind.TransitUp, Vector2Int.right);
            AddNode(graph, "down", DungeonNodeKind.TransitDown, Vector2Int.left);
            AddEdge(graph, "entry", "up");
            AddEdge(graph, "entry", "down");
            DungeonMetadataUtility.ApplyGraphDefaults(graph, 1, 123);

            DungeonLabyrinthObjectivePlan plan = DungeonLabyrinthObjectiveUtility.BuildObjectivePlan(graph, 1);

            Assert.AreEqual("down", plan.exitStairsRoomId);
            Assert.IsFalse(plan.objectiveRequired);
            Assert.IsTrue(plan.warnings.Count > 0);
        }

        [Test]
        public void ObjectivePlan_IsDeterministicForSameGraph()
        {
            DungeonBuildResult first = CreateObjectiveBuild();
            DungeonBuildResult second = CreateObjectiveBuild();

            DungeonLayoutQualityUtility.Analyze(first);
            DungeonLayoutQualityUtility.Analyze(second);

            Assert.AreEqual(first.labyrinthObjectivePlan.objectiveRoomId, second.labyrinthObjectivePlan.objectiveRoomId);
            Assert.AreEqual(first.labyrinthObjectivePlan.bossApproachRoomId, second.labyrinthObjectivePlan.bossApproachRoomId);
            Assert.AreEqual(first.labyrinthObjectivePlan.bossRoomId, second.labyrinthObjectivePlan.bossRoomId);
        }

        [Test]
        public void GeneratedGraph_ObjectivePlanUsesExistingMetadataAndKeepsStairsUnlockedByDefault()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            Assert.IsTrue(generator.TryGenerateNormal(
                new FrontierDepths.Core.FloorState { floorIndex = 6, floorSeed = 1932105958 },
                out DungeonLayoutGraph graph,
                out GraphValidationReport report),
                report.ToSummaryString(10));

            DungeonLabyrinthObjectivePlan plan = DungeonLabyrinthObjectiveUtility.BuildObjectivePlan(graph, 6);

            Assert.IsFalse(plan.lockedExitUntilObjectiveComplete);
            Assert.IsFalse(plan.lockedExitUntilBossDefeated);
            Assert.IsTrue(plan.nextDepthUnlocked);
            Assert.IsFalse(string.IsNullOrWhiteSpace(plan.exitStairsRoomId));
            Assert.AreEqual(graph.transitDownNodeId, plan.exitStairsRoomId);
        }

        private static void AssertObjectiveRoomIsNotProtected(DungeonBuildResult build, string nodeId)
        {
            DungeonRoomBuildRecord room = build.FindRoom(nodeId);
            Assert.NotNull(room);
            Assert.IsFalse(DungeonLayoutQualityUtility.IsProtectedRoom(room), nodeId);
            Assert.AreNotEqual(DungeonNodeKind.EntryHub, room.roomType);
            Assert.AreNotEqual(DungeonNodeKind.TransitUp, room.roomType);
            Assert.AreNotEqual(DungeonNodeKind.TransitDown, room.roomType);
            Assert.AreNotEqual(DungeonNodeKind.Secret, room.roomType);
        }

        private static DungeonBuildResult CreateObjectiveBuild()
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
            AddNode(graph, "main_b", DungeonNodeKind.Ordinary, new Vector2Int(3, 0));
            AddNode(graph, "boss", DungeonNodeKind.Ordinary, new Vector2Int(4, 0));
            AddNode(graph, "down", DungeonNodeKind.TransitDown, new Vector2Int(5, 0));
            AddNode(graph, "branch", DungeonNodeKind.Ordinary, new Vector2Int(3, 1));
            AddNode(graph, "objective", DungeonNodeKind.Ordinary, new Vector2Int(3, 2));
            AddNode(graph, "secret", DungeonNodeKind.Secret, new Vector2Int(2, -1));
            AddEdge(graph, "entry", "up");
            AddEdge(graph, "up", "main_a");
            AddEdge(graph, "main_a", "main_b");
            AddEdge(graph, "main_b", "boss");
            AddEdge(graph, "boss", "down");
            AddEdge(graph, "main_b", "branch");
            AddEdge(graph, "branch", "objective");
            AddEdge(graph, "main_a", "secret");
            DungeonMetadataUtility.ApplyGraphDefaults(graph, 6, 1234);

            DungeonBuildResult build = new DungeonBuildResult
            {
                graph = graph,
                floorIndex = 6,
                seed = 1234,
                entryNodeId = "entry",
                transitUpNodeId = "up",
                transitDownNodeId = "down",
                labyrinthObjectivePlan = DungeonLabyrinthObjectiveUtility.BuildObjectivePlan(graph, 6)
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
                    zoneType = node.zoneType,
                    templateKind = node.roomTemplate,
                    criticalPathIndex = node.criticalPathIndex,
                    bounds = new Bounds(new Vector3(node.gridPosition.x * 20f, 0f, node.gridPosition.y * 20f), GetRoomSize(node.nodeId))
                };
                room.footprintArea = room.bounds.size.x * room.bounds.size.z;
                build.rooms.Add(room);
            }

            return build;
        }

        private static Vector3 GetRoomSize(string nodeId)
        {
            return nodeId == "objective" || nodeId == "boss"
                ? new Vector3(42f, 8f, 42f)
                : new Vector3(24f, 8f, 24f);
        }

        private static void AddNode(DungeonLayoutGraph graph, string id, DungeonNodeKind kind, Vector2Int position)
        {
            graph.nodes.Add(new DungeonNode
            {
                nodeId = id,
                label = id,
                nodeKind = kind,
                gridPosition = position,
                roomTemplate = DungeonRoomTemplateKind.BroadRectangle
            });
        }

        private static void AddEdge(DungeonLayoutGraph graph, string a, string b)
        {
            graph.edges.Add(new DungeonEdge { a = a, b = b });
        }
    }
}
