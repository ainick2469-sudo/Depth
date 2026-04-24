using FrontierDepths.Core;
using FrontierDepths.World;
using NUnit.Framework;
using System.Collections.Generic;

namespace FrontierDepths.Tests.EditMode
{
    public class DungeonGraphGeneratorTests
    {
        [Test]
        public void Generator_AlwaysBuildsPathsFromEntryToTransitRooms()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph graph = generator.Generate(new FloorState { floorIndex = 1, floorSeed = 1234 });

            Assert.IsTrue(graph.HasPath(graph.entryHubNodeId, graph.transitUpNodeId));
            Assert.IsTrue(graph.HasPath(graph.entryHubNodeId, graph.transitDownNodeId));
        }

        [Test]
        public void Generator_UsesThreeOrMoreSectorsOnEarlyFloor()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph graph = generator.Generate(new FloorState { floorIndex = 1, floorSeed = 6123 });

            Assert.GreaterOrEqual(GetNeighborCount(graph, graph.entryHubNodeId), 3);
            Assert.GreaterOrEqual(GetCoveredSectorCount(graph), 3);
        }

        [Test]
        public void Generator_UsesAllFourSectorsOnMidDepthFloor()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph graph = generator.Generate(new FloorState { floorIndex = 8, floorSeed = 8123 });

            Assert.AreEqual(4, GetCoveredSectorCount(graph));
        }

        [Test]
        public void Generator_ScalesDungeonSizeWithFloorDepth()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph shallow = generator.Generate(new FloorState { floorIndex = 1, floorSeed = 1234 });
            DungeonLayoutGraph deep = generator.Generate(new FloorState { floorIndex = 15, floorSeed = 1234 + 15 * 977 });

            Assert.Greater(deep.nodes.Count, shallow.nodes.Count);
            Assert.Greater(GetGraphExtent(deep), GetGraphExtent(shallow));
        }

        [Test]
        public void Generator_SpreadsSpecialRoomsApart()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph graph = generator.Generate(new FloorState { floorIndex = 10, floorSeed = 10123 });

            DungeonNode landmark = FindByKind(graph, DungeonNodeKind.Landmark);
            DungeonNode secret = FindByKind(graph, DungeonNodeKind.Secret);

            Assert.NotNull(landmark);
            Assert.NotNull(secret);
            Assert.GreaterOrEqual(graph.GetGraphDistance(graph.transitUpNodeId, graph.transitDownNodeId), 4);
            Assert.GreaterOrEqual(graph.GetGraphDistance(landmark.nodeId, graph.transitDownNodeId), 3);
            Assert.GreaterOrEqual(graph.GetGraphDistance(secret.nodeId, graph.transitDownNodeId), 3);
            Assert.GreaterOrEqual(graph.GetGraphDistance(secret.nodeId, landmark.nodeId), 3);
        }

        [Test]
        public void Generator_ProducesLayoutVarietyAcrossDifferentSeeds()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            HashSet<string> signatures = new HashSet<string>();

            for (int i = 0; i < 6; i++)
            {
                DungeonLayoutGraph graph = generator.Generate(new FloorState
                {
                    floorIndex = 6,
                    floorSeed = 3000 + i * 977
                });

                signatures.Add(GetGraphSignature(graph));
            }

            Assert.GreaterOrEqual(signatures.Count, 4);
        }

        [Test]
        public void Generator_UsesMultipleRoomTemplatesOnMidDepthFloor()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph graph = generator.Generate(new FloorState { floorIndex = 8, floorSeed = 8800 });
            HashSet<DungeonRoomTemplateKind> templates = new HashSet<DungeonRoomTemplateKind>();

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                templates.Add(graph.nodes[i].roomTemplate);
            }

            Assert.GreaterOrEqual(templates.Count, 4);
        }

        private static DungeonNode FindByKind(DungeonLayoutGraph graph, DungeonNodeKind kind)
        {
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                if (graph.nodes[i].nodeKind == kind)
                {
                    return graph.nodes[i];
                }
            }

            return null;
        }

        private static int GetGraphExtent(DungeonLayoutGraph graph)
        {
            int best = 0;
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                int distance = UnityEngine.Mathf.Abs(graph.nodes[i].gridPosition.x) + UnityEngine.Mathf.Abs(graph.nodes[i].gridPosition.y);
                if (distance > best)
                {
                    best = distance;
                }
            }

            return best;
        }

        private static string GetGraphSignature(DungeonLayoutGraph graph)
        {
            List<string> parts = new List<string>();
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                parts.Add($"{node.nodeKind}:{node.gridPosition.x},{node.gridPosition.y}:{node.roomTemplate}");
            }

            parts.Sort();
            return string.Join("|", parts);
        }

        private static int GetNeighborCount(DungeonLayoutGraph graph, string nodeId)
        {
            return graph.GetDegree(nodeId);
        }

        private static int GetCoveredSectorCount(DungeonLayoutGraph graph)
        {
            int[] sectors = new int[4];
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                if (node.nodeId == graph.entryHubNodeId)
                {
                    continue;
                }

                sectors[GetSector(node.gridPosition)]++;
            }

            int covered = 0;
            for (int i = 0; i < sectors.Length; i++)
            {
                if (sectors[i] > 0)
                {
                    covered++;
                }
            }

            return covered;
        }

        private static int GetSector(UnityEngine.Vector2Int position)
        {
            if (UnityEngine.Mathf.Abs(position.x) >= UnityEngine.Mathf.Abs(position.y))
            {
                return position.x >= 0 ? 0 : 1;
            }

            return position.y >= 0 ? 2 : 3;
        }
    }
}
