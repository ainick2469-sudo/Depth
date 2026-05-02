using System.Linq;
using FrontierDepths.Core;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS141FLabyrinthLayoutQualityTests
    {
        [Test]
        public void LayoutQualityReport_ClassifiesMainPathBranchesDeadEndsAndProtectedRooms()
        {
            DungeonBuildResult build = CreateBranchedBuild(floor: 5);

            DungeonLayoutQualityReport report = DungeonLayoutQualityUtility.Analyze(build);

            Assert.AreEqual(7, report.roomCount);
            Assert.GreaterOrEqual(report.mainPathRoomCount, 3);
            Assert.AreEqual(1, report.branchRoomCount);
            Assert.AreEqual(1, report.deadEndRoomCount);
            Assert.GreaterOrEqual(report.protectedRoomCount, 3);
            Assert.IsTrue(build.FindRoom("entry").isProtected);
            Assert.IsTrue(build.FindRoom("up").isProtected);
            Assert.IsTrue(build.FindRoom("down").isProtected);
            Assert.AreEqual(DungeonRoomRole.FutureBossApproach, build.FindRoom("approach").layoutRole);
            Assert.AreEqual(DungeonRoomRole.DeadEnd, build.FindRoom("dead_end").layoutRole);
        }

        [Test]
        public void LandmarkSelection_PrefersLargeUnprotectedSideRooms()
        {
            DungeonBuildResult build = CreateBranchedBuild(floor: 8);

            DungeonLayoutQualityReport report = DungeonLayoutQualityUtility.Analyze(build);

            Assert.GreaterOrEqual(report.landmarkRoomCount, 1);
            Assert.IsTrue(build.FindRoom("dead_end").isLandmarkRoom || build.FindRoom("branch").isLandmarkRoom);
            Assert.IsFalse(build.FindRoom("entry").isLandmarkRoom);
            Assert.IsFalse(build.FindRoom("down").isLandmarkRoom);
        }

        [Test]
        public void MergeCandidates_AreMetadataOnlyAndNeverMarkProtectedRoomsSafe()
        {
            DungeonBuildResult build = CreateBranchedBuild(floor: 6);

            DungeonLayoutQualityReport report = DungeonLayoutQualityUtility.Analyze(build);

            Assert.GreaterOrEqual(build.mergeCandidates.Count, report.mergeCandidateCount);
            Assert.Greater(report.mergeCandidateCount, 0);
            foreach (DungeonRoomMergeCandidateRecord candidate in build.mergeCandidates.Where(candidate => candidate.isSafeToApply))
            {
                Assert.IsFalse(DungeonLayoutQualityUtility.IsProtectedRoom(build.FindRoom(candidate.roomA)));
                Assert.IsFalse(DungeonLayoutQualityUtility.IsProtectedRoom(build.FindRoom(candidate.roomB)));
                Assert.IsTrue(build.graph.HasPath(build.entryNodeId, build.transitDownNodeId));
            }
        }

        [Test]
        public void MergeCandidates_AreSuppressedOnlyOnFirstTrainingFloor()
        {
            DungeonBuildResult build = CreateBranchedBuild(floor: 1);

            DungeonLayoutQualityReport report = DungeonLayoutQualityUtility.Analyze(build);

            Assert.AreEqual(0, report.mergeCandidateCount);
            Assert.AreEqual(0, build.mergeCandidates.Count);
        }

        [Test]
        public void SpecialRoomDistribution_WarnsAboutEarlyDuplicatePurposesAndMainPathOverload()
        {
            DungeonBuildResult build = CreateBranchedBuild(floor: 4);
            build.FindRoom("main").purposeId = "blue_fountain";
            build.FindRoom("approach").purposeId = "blue_fountain";
            build.FindRoom("main").roomRole = DungeonRoomRole.Shrine;
            build.FindRoom("approach").roomRole = DungeonRoomRole.Shrine;

            DungeonLayoutQualityReport report = DungeonLayoutQualityUtility.Analyze(build);

            Assert.AreEqual(2, report.specialRoomCount);
            Assert.GreaterOrEqual(report.repeatedSpecialRoomWarnings, 1);
            Assert.IsTrue(report.layoutWarnings.Any(warning => warning.Contains("Repeated early special purpose")));
        }

        [Test]
        public void CorridorMetrics_ReportLongCorridorsAndStraightChains()
        {
            DungeonBuildResult build = CreateBranchedBuild(floor: 5);
            build.corridors.Add(new DungeonCorridorBuildRecord { edgeKey = "extra", fromNodeId = "main", toNodeId = "approach", length = 80f });

            DungeonLayoutQualityReport report = DungeonLayoutQualityUtility.Analyze(build);

            Assert.Greater(report.averageCorridorLength, 0f);
            Assert.GreaterOrEqual(report.longestCorridorLength, 80f);
            Assert.GreaterOrEqual(report.longCorridorWarnings, 1);
            Assert.GreaterOrEqual(report.corridorToRoomRatio, 1f);
        }

        [Test]
        public void GeneratedGraphs_KeepExistingMetadataAndAllowDepthScaledLargeTemplates()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph shallow = GenerateNormalGraph(generator, 1, 778287037);
            DungeonLayoutGraph deep = GenerateNormalGraph(generator, 10, 778287037 + 10 * 977);

            Assert.NotNull(shallow.GetNode(shallow.entryHubNodeId));
            Assert.AreEqual(DungeonRoomRole.Start, shallow.GetNode(shallow.entryHubNodeId).roomRole);
            Assert.AreEqual(DungeonRoomRole.Exit, shallow.GetNode(shallow.transitDownNodeId).roomRole);
            Assert.GreaterOrEqual(CountLargeOrGrandTemplates(deep), CountLargeOrGrandTemplates(shallow));
        }

        private static DungeonLayoutGraph GenerateNormalGraph(GraphFirstDungeonGenerator generator, int floor, int seed)
        {
            bool success = generator.TryGenerateNormal(
                new FloorState { floorIndex = floor, floorSeed = seed },
                out DungeonLayoutGraph graph,
                out GraphValidationReport report);
            Assert.IsTrue(success, report.ToSummaryString(10));
            return graph;
        }

        private static int CountLargeOrGrandTemplates(DungeonLayoutGraph graph)
        {
            int count = 0;
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonRoomSizeTier tier = DungeonRoomTemplateLibrary.GetSizeTier(graph.nodes[i].roomTemplate);
                if (tier == DungeonRoomSizeTier.Large || tier == DungeonRoomSizeTier.Grand)
                {
                    count++;
                }
            }

            return count;
        }

        private static DungeonBuildResult CreateBranchedBuild(int floor)
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
            AddNode(graph, "main", DungeonNodeKind.Ordinary, new Vector2Int(2, 0));
            AddNode(graph, "approach", DungeonNodeKind.Ordinary, new Vector2Int(3, 0));
            AddNode(graph, "down", DungeonNodeKind.TransitDown, new Vector2Int(4, 0));
            AddNode(graph, "branch", DungeonNodeKind.Ordinary, new Vector2Int(2, 1));
            AddNode(graph, "dead_end", DungeonNodeKind.Ordinary, new Vector2Int(2, 2));
            AddEdge(graph, "entry", "up");
            AddEdge(graph, "up", "main");
            AddEdge(graph, "main", "approach");
            AddEdge(graph, "approach", "down");
            AddEdge(graph, "main", "branch");
            AddEdge(graph, "branch", "dead_end");
            DungeonMetadataUtility.ApplyGraphDefaults(graph, floor, 1234);

            DungeonBuildResult build = new DungeonBuildResult
            {
                graph = graph,
                floorIndex = floor,
                seed = 1234,
                entryNodeId = "entry",
                transitUpNodeId = "up",
                transitDownNodeId = "down"
            };

            for (int i = 0; i < graph.edges.Count; i++)
            {
                DungeonEdge edge = graph.edges[i];
                string key = DungeonBuildResult.GetEdgeKey(edge.a, edge.b);
                build.graphEdges.Add(new DungeonGraphEdgeRecord { edgeKey = key, a = edge.a, b = edge.b });
                build.corridors.Add(new DungeonCorridorBuildRecord
                {
                    edgeKey = key,
                    fromNodeId = edge.a,
                    toNodeId = edge.b,
                    length = 36f
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
                    criticalPathIndex = node.criticalPathIndex,
                    bounds = new Bounds(new Vector3(node.gridPosition.x * 20f, 0f, node.gridPosition.y * 20f), GetRoomSize(node.nodeId)),
                };
                room.footprintArea = room.bounds.size.x * room.bounds.size.z;
                build.rooms.Add(room);
            }

            return build;
        }

        private static Vector3 GetRoomSize(string nodeId)
        {
            return nodeId == "branch" || nodeId == "dead_end"
                ? new Vector3(36f, 8f, 36f)
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
                roomTemplate = DungeonRoomTemplateKind.SquareChamber
            });
        }

        private static void AddEdge(DungeonLayoutGraph graph, string a, string b)
        {
            graph.edges.Add(new DungeonEdge { a = a, b = b });
        }
    }
}
