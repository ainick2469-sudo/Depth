using FrontierDepths.Core;
using FrontierDepths.World;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.Tests.EditMode
{
    public class DungeonGraphGeneratorTests
    {
        private static readonly int[] FloorOneCuratedSeeds =
        {
            778287037,
            1932105958,
            1155232724,
            1246244744,
            1246245721,
            6123
        };

        [Test]
        public void Generator_AlwaysBuildsPathsFromEntryToTransitRooms()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph graph = GenerateNormalGraph(generator, 1, 1234, out _);

            Assert.IsTrue(graph.HasPath(graph.entryHubNodeId, graph.transitUpNodeId));
            Assert.IsTrue(graph.HasPath(graph.entryHubNodeId, graph.transitDownNodeId));
        }

        [Test]
        public void Generator_UsesAtLeastTwoSectorsOnEarlyFloor()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph graph = GenerateNormalGraph(generator, 1, 6123, out GraphValidationReport report);

            Assert.GreaterOrEqual(GetNeighborCount(graph, graph.entryHubNodeId), 2, report.ToSummaryString(10));
            Assert.GreaterOrEqual(GetCoveredSectorCount(graph), 2, report.ToSummaryString(10));
        }

        [Test]
        public void Generator_UsesAllFourSectorsOnMidDepthFloor()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph graph = GenerateNormalGraph(generator, 8, 8123, out GraphValidationReport report);

            Assert.AreEqual(4, GetCoveredSectorCount(graph), report.ToSummaryString(10));
        }

        [Test]
        public void Generator_ScalesDungeonSizeWithFloorDepth()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph shallow = GenerateNormalGraph(generator, 1, 1234, out _);
            DungeonLayoutGraph deep = GenerateNormalGraph(generator, 15, 1234 + 15 * 977, out _);

            Assert.Greater(deep.nodes.Count, shallow.nodes.Count);
            Assert.Greater(GetGraphExtent(deep), GetGraphExtent(shallow));
        }

        [Test]
        public void Generator_NormalFloorOne_CuratedSeedSet_ProducesNonFallbackLayouts()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            for (int i = 0; i < FloorOneCuratedSeeds.Length; i++)
            {
                DungeonLayoutGraph graph = GenerateNormalGraph(generator, 1, FloorOneCuratedSeeds[i], out GraphValidationReport report);

                Assert.That(graph.nodes.Count, Is.InRange(10, 14), report.ToSummaryString(10));
                Assert.Greater(graph.nodes.Count, 6, report.ToSummaryString(10));
            }
        }

        [Test]
        public void Generator_NormalFloorThree_CuratedSeedSet_ProducesNonFallbackLayouts()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            for (int seedIndex = 0; seedIndex < 6; seedIndex++)
            {
                int seed = 5100 + seedIndex * 977;
                DungeonLayoutGraph graph = GenerateNormalGraph(generator, 3, seed, out GraphValidationReport report);

                Assert.That(graph.nodes.Count, Is.InRange(12, 16), report.ToSummaryString(10));
            }
        }

        [Test]
        public void Generator_SpreadsSpecialRoomsApart()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            DungeonLayoutGraph graph = GenerateNormalGraph(generator, 10, 10123, out GraphValidationReport report);

            DungeonNode landmark = FindByKind(graph, DungeonNodeKind.Landmark);
            DungeonNode secret = FindByKind(graph, DungeonNodeKind.Secret);

            Assert.NotNull(landmark, report.ToSummaryString(10));
            Assert.NotNull(secret, report.ToSummaryString(10));
            Assert.GreaterOrEqual(graph.GetGraphDistance(graph.transitUpNodeId, graph.transitDownNodeId), 4, report.ToSummaryString(10));
            Assert.GreaterOrEqual(graph.GetGraphDistance(landmark.nodeId, graph.transitDownNodeId), 3, report.ToSummaryString(10));
            Assert.GreaterOrEqual(graph.GetGraphDistance(secret.nodeId, graph.transitDownNodeId), 3, report.ToSummaryString(10));
            Assert.GreaterOrEqual(graph.GetGraphDistance(secret.nodeId, landmark.nodeId), 3, report.ToSummaryString(10));
        }

        [Test]
        public void Generator_NormalFloorOne_ProducesAtLeastFourDifferentSignaturesAcrossSeedSet()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            HashSet<string> signatures = new HashSet<string>();

            for (int i = 0; i < FloorOneCuratedSeeds.Length; i++)
            {
                DungeonLayoutGraph graph = GenerateNormalGraph(generator, 1, FloorOneCuratedSeeds[i], out _);
                signatures.Add(DungeonLayoutSignatureUtility.BuildSignature(graph, 1, FloorOneCuratedSeeds[i]));
            }

            Assert.GreaterOrEqual(signatures.Count, 4);
        }

        [Test]
        public void Generator_FloorTwoSignatureDiffersFromFloorOneForSameSeedBase()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            const int baseSeed = 778287037;

            DungeonLayoutGraph floorOne = GenerateNormalGraph(generator, 1, baseSeed, out _);
            DungeonLayoutGraph floorTwo = GenerateNormalGraph(generator, 2, baseSeed, out _);

            string floorOneSignature = DungeonLayoutSignatureUtility.BuildSignature(floorOne, 1, baseSeed);
            string floorTwoSignature = DungeonLayoutSignatureUtility.BuildSignature(floorTwo, 2, baseSeed);

            Assert.AreNotEqual(floorOneSignature, floorTwoSignature);
        }

        [Test]
        public void Generator_UsesOnlyGateOneSafeOrdinaryTemplates()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            for (int seedIndex = 0; seedIndex < 6; seedIndex++)
            {
                int seed = 8800 + seedIndex * 977;
                DungeonLayoutGraph graph = GenerateNormalGraph(generator, 8, seed, out GraphValidationReport report);

                for (int i = 0; i < graph.nodes.Count; i++)
                {
                    DungeonNode node = graph.nodes[i];
                    if (node.nodeKind == DungeonNodeKind.Ordinary)
                    {
                        Assert.IsTrue(
                            DungeonRoomTemplateLibrary.IsGateOneSafeOrdinaryTemplate(node.roomTemplate),
                            $"Ordinary node {node.nodeId} used unsafe template {node.roomTemplate}. {report.ToSummaryString(10)}");
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
            DungeonLayoutGraph graph = GenerateNormalGraph(generator, 8, 8800, out GraphValidationReport report);
            HashSet<DungeonRoomTemplateKind> templates = new HashSet<DungeonRoomTemplateKind>();

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                if (graph.nodes[i].nodeKind == DungeonNodeKind.Ordinary)
                {
                    templates.Add(graph.nodes[i].roomTemplate);
                }
            }

            Assert.GreaterOrEqual(templates.Count, 2, report.ToSummaryString(10));
        }

        [Test]
        public void Generator_UsesExpandedSafeTemplateVarietyAcrossSeedSet()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            HashSet<DungeonRoomTemplateKind> templates = new HashSet<DungeonRoomTemplateKind>();

            for (int seedIndex = 0; seedIndex < 12; seedIndex++)
            {
                int seed = 14000 + seedIndex * 977;
                DungeonLayoutGraph graph = GenerateNormalGraph(generator, 8, seed, out _);

                for (int nodeIndex = 0; nodeIndex < graph.nodes.Count; nodeIndex++)
                {
                    if (graph.nodes[nodeIndex].nodeKind == DungeonNodeKind.Ordinary)
                    {
                        templates.Add(graph.nodes[nodeIndex].roomTemplate);
                    }
                }
            }

            Assert.GreaterOrEqual(templates.Count, 5);
        }

        [Test]
        public void Generator_UsesRoleMatchedCornerJunctionAndCrossTemplatesAcrossSeedSet()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            bool foundCornerTemplate = false;
            bool foundJunctionTemplate = false;
            bool foundCrossTemplate = false;

            for (int seedIndex = 0; seedIndex < 20; seedIndex++)
            {
                int seed = 18000 + seedIndex * 977;
                DungeonLayoutGraph graph = GenerateNormalGraph(generator, 10, seed, out _);

                for (int nodeIndex = 0; nodeIndex < graph.nodes.Count; nodeIndex++)
                {
                    DungeonNode node = graph.nodes[nodeIndex];
                    if (node.nodeKind != DungeonNodeKind.Ordinary)
                    {
                        continue;
                    }

                    int degree = graph.GetDegree(node.nodeId);
                    DungeonExitMask mask = GetRequiredExitMask(graph, node);
                    if (degree == 2 && IsCornerMask(mask) &&
                        (node.roomTemplate == DungeonRoomTemplateKind.LChamberSafe || node.roomTemplate == DungeonRoomTemplateKind.WideBendSafe))
                    {
                        foundCornerTemplate = true;
                    }

                    if (degree == 3 &&
                        (node.roomTemplate == DungeonRoomTemplateKind.TChamberSafe || node.roomTemplate == DungeonRoomTemplateKind.ForkRoomSafe))
                    {
                        foundJunctionTemplate = true;
                    }

                    if (degree >= 4 && node.roomTemplate == DungeonRoomTemplateKind.CrossChamberSafe)
                    {
                        foundCrossTemplate = true;
                    }
                }

                if (foundCornerTemplate && foundJunctionTemplate && foundCrossTemplate)
                {
                    break;
                }
            }

            Assert.IsTrue(foundCornerTemplate, "Expected at least one corner ordinary room to use LChamberSafe or WideBendSafe.");
            Assert.IsTrue(foundJunctionTemplate, "Expected at least one three-way ordinary room to use TChamberSafe or ForkRoomSafe.");
            Assert.IsTrue(foundCrossTemplate, "Expected at least one four-way ordinary room to use CrossChamberSafe.");
        }

        [Test]
        public void Generator_AvoidsThreeRoomOrdinaryTemplateStreaks()
        {
            GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
            for (int seedIndex = 0; seedIndex < 6; seedIndex++)
            {
                int seed = 9600 + seedIndex * 977;
                DungeonLayoutGraph graph = GenerateNormalGraph(generator, 8, seed, out _);

                Assert.IsFalse(HasThreeRoomOrdinaryTemplateStreak(graph), $"Found three-room ordinary template streak at seed {seed}.");
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

        private static DungeonLayoutGraph GenerateNormalGraph(GraphFirstDungeonGenerator generator, int floorIndex, int seed, out GraphValidationReport report)
        {
            FloorState floorState = new FloorState
            {
                floorIndex = floorIndex,
                floorSeed = seed
            };

            bool success = generator.TryGenerateNormal(floorState, out DungeonLayoutGraph graph, out report);
            Assert.IsTrue(success, report.ToSummaryString(10));
            Assert.NotNull(graph, report.ToSummaryString(10));
            return graph;
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
                int distance = Mathf.Abs(graph.nodes[i].gridPosition.x) + Mathf.Abs(graph.nodes[i].gridPosition.y);
                if (distance > best)
                {
                    best = distance;
                }
            }

            return best;
        }

        private static int GetNeighborCount(DungeonLayoutGraph graph, string nodeId)
        {
            return graph.GetDegree(nodeId);
        }

        private static DungeonExitMask GetRequiredExitMask(DungeonLayoutGraph graph, DungeonNode node)
        {
            DungeonExitMask mask = DungeonExitMask.None;
            List<DungeonNode> neighbors = graph.GetNeighbors(node.nodeId);
            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector2Int delta = neighbors[i].gridPosition - node.gridPosition;
                delta.x = Mathf.Clamp(delta.x, -1, 1);
                delta.y = Mathf.Clamp(delta.y, -1, 1);
                mask |= DungeonRoomTemplateLibrary.DirectionToMask(delta);
            }

            return mask;
        }

        private static bool IsCornerMask(DungeonExitMask mask)
        {
            return mask == (DungeonExitMask.North | DungeonExitMask.East) ||
                   mask == (DungeonExitMask.North | DungeonExitMask.West) ||
                   mask == (DungeonExitMask.South | DungeonExitMask.East) ||
                   mask == (DungeonExitMask.South | DungeonExitMask.West);
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

        private static int GetSector(Vector2Int position)
        {
            if (Mathf.Abs(position.x) >= Mathf.Abs(position.y))
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
