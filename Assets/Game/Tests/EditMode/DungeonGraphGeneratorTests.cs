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
        public void Generator_UsesOnlyGateOneSafeOrdinaryTemplates()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            for (int seedIndex = 0; seedIndex < 6; seedIndex++)
            {
                DungeonLayoutGraph graph = generator.Generate(new FloorState
                {
                    floorIndex = 8,
                    floorSeed = 8800 + seedIndex * 977
                });

                for (int i = 0; i < graph.nodes.Count; i++)
                {
                    DungeonNode node = graph.nodes[i];
                    if (node.nodeKind == DungeonNodeKind.Ordinary)
                    {
                        Assert.IsTrue(
                            DungeonRoomTemplateLibrary.IsGateOneSafeOrdinaryTemplate(node.roomTemplate),
                            $"Ordinary node {node.nodeId} used unsafe template {node.roomTemplate}.");
                    }

                    if (node.nodeKind == DungeonNodeKind.Landmark)
                    {
                        Assert.AreEqual(DungeonTemplateFeature.Flat, DungeonRoomTemplateLibrary.GetFeature(node));
                    }
                }
            }
        }

        [Test]
        public void Generator_UsesMultipleSafeTemplatesOnMidDepthFloor()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph graph = generator.Generate(new FloorState { floorIndex = 8, floorSeed = 8800 });
            HashSet<DungeonRoomTemplateKind> templates = new HashSet<DungeonRoomTemplateKind>();

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                if (graph.nodes[i].nodeKind == DungeonNodeKind.Ordinary)
                {
                    templates.Add(graph.nodes[i].roomTemplate);
                }
            }

            Assert.GreaterOrEqual(templates.Count, 2);
        }

        [Test]
        public void Generator_AvoidsThreeRoomOrdinaryTemplateStreaks()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            for (int seedIndex = 0; seedIndex < 6; seedIndex++)
            {
                DungeonLayoutGraph graph = generator.Generate(new FloorState
                {
                    floorIndex = 8,
                    floorSeed = 9600 + seedIndex * 977
                });

                Assert.IsFalse(HasThreeRoomOrdinaryTemplateStreak(graph), $"Found three-room ordinary template streak at seed {9600 + seedIndex * 977}.");
            }
        }

        [Test]
        public void Generator_FallbackLayoutIncludesRequiredGateOneRooms()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph graph = generator.GenerateFallback(new FloorState { floorIndex = 1, floorSeed = 4400 });

            Assert.NotNull(FindByKind(graph, DungeonNodeKind.Landmark));
            Assert.NotNull(FindByKind(graph, DungeonNodeKind.Secret));
            Assert.NotNull(FindByKind(graph, DungeonNodeKind.TransitUp));
            Assert.NotNull(FindByKind(graph, DungeonNodeKind.TransitDown));
            Assert.IsTrue(graph.HasPath(graph.entryHubNodeId, graph.transitDownNodeId));
            Assert.IsTrue(graph.HasPath(graph.entryHubNodeId, graph.transitUpNodeId));

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                if (node.nodeKind == DungeonNodeKind.Ordinary)
                {
                    Assert.IsTrue(
                        DungeonRoomTemplateLibrary.IsGateOneSafeOrdinaryTemplate(node.roomTemplate),
                        $"Fallback ordinary node {node.nodeId} used unsafe template {node.roomTemplate}.");
                }
            }
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

        private static bool HasThreeRoomOrdinaryTemplateStreak(DungeonLayoutGraph graph)
        {
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                if (node.nodeKind != DungeonNodeKind.Ordinary)
                {
                    continue;
                }

                List<DungeonNode> neighbors = graph.GetNeighbors(node.nodeId);
                for (int neighborIndex = 0; neighborIndex < neighbors.Count; neighborIndex++)
                {
                    DungeonNode neighbor = neighbors[neighborIndex];
                    if (neighbor.nodeKind != DungeonNodeKind.Ordinary || neighbor.roomTemplate != node.roomTemplate)
                    {
                        continue;
                    }

                    List<DungeonNode> secondaryNeighbors = graph.GetNeighbors(neighbor.nodeId);
                    for (int secondaryIndex = 0; secondaryIndex < secondaryNeighbors.Count; secondaryIndex++)
                    {
                        DungeonNode secondary = secondaryNeighbors[secondaryIndex];
                        if (secondary.nodeId == node.nodeId || secondary.nodeKind != DungeonNodeKind.Ordinary)
                        {
                            continue;
                        }

                        if (secondary.roomTemplate == node.roomTemplate)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
