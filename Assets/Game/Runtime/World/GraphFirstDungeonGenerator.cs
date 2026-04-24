using System;
using System.Collections.Generic;
using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class GraphFirstDungeonGenerator : IDungeonGenerator
    {
        private struct ExpansionCandidate
        {
            public string parentId;
            public Vector2Int position;
            public float score;
        }

        private static readonly Vector2Int[] CardinalDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        private static readonly Vector2Int[] RingPositions =
        {
            new Vector2Int(1, 1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1),
            new Vector2Int(1, -1)
        };

        private static readonly string[] OrdinaryLabels =
        {
            "Ore Gallery",
            "Dust Hall",
            "Timber Route",
            "Broken Crossing",
            "Lantern Drift",
            "Collapsed Spur",
            "Echo Chamber",
            "Shale Passage",
            "Hollow Bend",
            "Miner Junction"
        };

        private static readonly string[] LandmarkLabels =
        {
            "Lantern Shrine",
            "Sunken Lift",
            "Surveyor Rotunda",
            "Whispering Well"
        };

        private static readonly string[] SecretLabels =
        {
            "Smuggler Niche",
            "Hidden Cache",
            "Collapsed Reliquary",
            "Buried Survey Vault"
        };

        public DungeonLayoutGraph Generate(FloorState floorState)
        {
            int floorIndex = Mathf.Max(1, floorState.floorIndex);
            int baseSeed = floorState.floorSeed == 0 ? 1000 + floorIndex * 977 : floorState.floorSeed;

            for (int attempt = 0; attempt < 16; attempt++)
            {
                DungeonLayoutGraph graph = BuildGraph(baseSeed + attempt * 1543, floorIndex);
                if (IsValidGraph(graph, floorIndex))
                {
                    return graph;
                }
            }

            return BuildFallbackGraph(baseSeed, floorIndex);
        }

        private static DungeonLayoutGraph BuildGraph(int seed, int floorIndex)
        {
            System.Random random = new System.Random(seed);
            DungeonLayoutGraph graph = new DungeonLayoutGraph();
            Dictionary<Vector2Int, string> occupied = new Dictionary<Vector2Int, string>();
            int nextOrdinaryId = 0;
            int nextSecretId = 0;

            AddNode(graph, occupied, "entry_hub", "Entry Hub", DungeonNodeKind.EntryHub, Vector2Int.zero);
            graph.entryHubNodeId = "entry_hub";

            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int position = CardinalDirections[i];
                string nodeId = $"room_{nextOrdinaryId++}";
                AddNode(graph, occupied, nodeId, ChooseLabel(OrdinaryLabels, random), DungeonNodeKind.Ordinary, position);
                AddEdge(graph, graph.entryHubNodeId, nodeId);
            }

            for (int i = 0; i < RingPositions.Length; i++)
            {
                Vector2Int position = RingPositions[i];
                string nodeId = $"room_{nextOrdinaryId++}";
                AddNode(graph, occupied, nodeId, ChooseLabel(OrdinaryLabels, random), DungeonNodeKind.Ordinary, position);
                ConnectToAdjacentOccupied(graph, occupied, nodeId, position, random, true);
            }

            int targetRoomCount = GetTargetRoomCount(floorIndex, random);
            int maxRadius = GetMaxRadius(floorIndex, targetRoomCount);
            int guard = targetRoomCount * 48;

            while (graph.nodes.Count < targetRoomCount && guard-- > 0)
            {
                List<ExpansionCandidate> candidates = CollectExpansionCandidates(graph, occupied, random, targetRoomCount, maxRadius);
                if (candidates.Count == 0)
                {
                    break;
                }

                candidates.Sort((left, right) => right.score.CompareTo(left.score));
                ExpansionCandidate choice = candidates[random.Next(Mathf.Min(8, candidates.Count))];
                string nodeId = $"room_{nextOrdinaryId++}";
                AddNode(graph, occupied, nodeId, ChooseLabel(OrdinaryLabels, random), DungeonNodeKind.Ordinary, choice.position);
                ConnectToAdjacentOccupied(graph, occupied, nodeId, choice.position, random, Manhattan(choice.position) <= 2);
            }

            EnsureOuterCoverage(graph, occupied, random, targetRoomCount, maxRadius, ref nextOrdinaryId);
            AddLoopEdges(graph, occupied, random, floorIndex);
            AssignSpecialRooms(graph, occupied, random, floorIndex, ref nextSecretId);
            AssignTemplates(graph, random);
            SyncLegacyIds(graph);
            return graph;
        }

        private static DungeonLayoutGraph BuildFallbackGraph(int seed, int floorIndex)
        {
            System.Random random = new System.Random(seed + floorIndex * 67);
            DungeonLayoutGraph graph = new DungeonLayoutGraph();
            Dictionary<Vector2Int, string> occupied = new Dictionary<Vector2Int, string>();

            AddNode(graph, occupied, "entry_hub", "Entry Hub", DungeonNodeKind.EntryHub, Vector2Int.zero);
            AddNode(graph, occupied, "transit_up", "Lift Chamber", DungeonNodeKind.TransitUp, new Vector2Int(1, 0));
            AddNode(graph, occupied, "north_room", ChooseLabel(OrdinaryLabels, random), DungeonNodeKind.Ordinary, new Vector2Int(0, 1));
            AddNode(graph, occupied, "south_room", ChooseLabel(OrdinaryLabels, random), DungeonNodeKind.Ordinary, new Vector2Int(0, -1));
            AddNode(graph, occupied, "west_room", ChooseLabel(OrdinaryLabels, random), DungeonNodeKind.Ordinary, new Vector2Int(-1, 0));
            AddNode(graph, occupied, "ring_ne", ChooseLabel(OrdinaryLabels, random), DungeonNodeKind.Ordinary, new Vector2Int(1, 1));
            AddNode(graph, occupied, "ring_nw", ChooseLabel(OrdinaryLabels, random), DungeonNodeKind.Ordinary, new Vector2Int(-1, 1));
            AddNode(graph, occupied, "ring_sw", ChooseLabel(OrdinaryLabels, random), DungeonNodeKind.Ordinary, new Vector2Int(-1, -1));
            AddNode(graph, occupied, "landmark", ChooseLabel(LandmarkLabels, random), DungeonNodeKind.Landmark, new Vector2Int(-2, 1));
            AddNode(graph, occupied, "secret_0", ChooseLabel(SecretLabels, random), DungeonNodeKind.Secret, new Vector2Int(1, -2));
            AddNode(graph, occupied, "transit_down", "Stair Chamber", DungeonNodeKind.TransitDown, new Vector2Int(-3, 0));

            AddEdge(graph, "entry_hub", "transit_up");
            AddEdge(graph, "entry_hub", "north_room");
            AddEdge(graph, "entry_hub", "south_room");
            AddEdge(graph, "entry_hub", "west_room");
            AddEdge(graph, "north_room", "ring_ne");
            AddEdge(graph, "north_room", "ring_nw");
            AddEdge(graph, "west_room", "ring_nw");
            AddEdge(graph, "west_room", "ring_sw");
            AddEdge(graph, "south_room", "ring_sw");
            AddEdge(graph, "south_room", "secret_0");
            AddEdge(graph, "west_room", "transit_down");
            AddEdge(graph, "ring_nw", "landmark");

            graph.entryHubNodeId = "entry_hub";
            graph.transitUpNodeId = "transit_up";
            graph.transitDownNodeId = "transit_down";
            SyncLegacyIds(graph);
            AssignTemplates(graph, random);
            return graph;
        }

        private static void AssignSpecialRooms(
            DungeonLayoutGraph graph,
            Dictionary<Vector2Int, string> occupied,
            System.Random random,
            int floorIndex,
            ref int nextSecretId)
        {
            Dictionary<string, int> distances = graph.BuildDistanceMap(graph.entryHubNodeId);
            DungeonNode transitUp = SelectTransitUp(graph, distances, random);
            transitUp.nodeKind = DungeonNodeKind.TransitUp;
            transitUp.label = floorIndex == 1 ? "Surface Lift" : "Upper Stairs";
            graph.transitUpNodeId = transitUp.nodeId;

            int upSector = GetSector(transitUp.gridPosition);
            DungeonNode transitDown = SelectTransitDown(graph, distances, upSector, transitUp.nodeId);
            transitDown.nodeKind = DungeonNodeKind.TransitDown;
            transitDown.label = "Stair Chamber";
            graph.transitDownNodeId = transitDown.nodeId;

            int downSector = GetSector(transitDown.gridPosition);
            DungeonNode landmark = SelectLandmark(graph, distances, upSector, downSector, transitUp.nodeId, transitDown.nodeId);
            if (landmark != null)
            {
                landmark.nodeKind = DungeonNodeKind.Landmark;
                landmark.label = ChooseLabel(LandmarkLabels, random);
            }

            HashSet<string> blocked = new HashSet<string> { graph.entryHubNodeId, transitUp.nodeId, transitDown.nodeId };
            if (landmark != null)
            {
                blocked.Add(landmark.nodeId);
            }

            if (!TryAttachSecret(graph, occupied, distances, random, blocked, ref nextSecretId))
            {
                DungeonNode fallbackSecret = SelectFallbackSecret(graph, distances, blocked);
                if (fallbackSecret != null)
                {
                    fallbackSecret.nodeKind = DungeonNodeKind.Secret;
                    fallbackSecret.label = ChooseLabel(SecretLabels, random);
                }
            }
        }

        private static DungeonNode SelectTransitUp(DungeonLayoutGraph graph, Dictionary<string, int> distances, System.Random random)
        {
            DungeonNode best = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                if (node.nodeId == graph.entryHubNodeId || node.nodeKind != DungeonNodeKind.Ordinary)
                {
                    continue;
                }

                int distance = distances.TryGetValue(node.nodeId, out int found) ? found : 99;
                if (distance > 2)
                {
                    continue;
                }

                float score = distance == 1 ? 12f : 8f;
                score += graph.GetDegree(node.nodeId) <= 2 ? 1.5f : 0f;
                score += (float)random.NextDouble();
                if (score > bestScore)
                {
                    best = node;
                    bestScore = score;
                }
            }

            return best ?? graph.GetNeighbors(graph.entryHubNodeId)[0];
        }

        private static DungeonNode SelectTransitDown(
            DungeonLayoutGraph graph,
            Dictionary<string, int> distances,
            int excludedSector,
            string transitUpId)
        {
            DungeonNode best = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                if (node.nodeId == graph.entryHubNodeId || node.nodeId == transitUpId || node.nodeKind != DungeonNodeKind.Ordinary)
                {
                    continue;
                }

                int distance = distances.TryGetValue(node.nodeId, out int found) ? found : 0;
                int degree = graph.GetDegree(node.nodeId);
                int sector = GetSector(node.gridPosition);
                float score = distance * 10f - degree * 1.5f;
                score += sector != excludedSector ? 8f : 0f;
                score += Mathf.Max(Mathf.Abs(node.gridPosition.x), Mathf.Abs(node.gridPosition.y));

                if (score > bestScore)
                {
                    best = node;
                    bestScore = score;
                }
            }

            return best ?? graph.GetNode(transitUpId);
        }

        private static DungeonNode SelectLandmark(
            DungeonLayoutGraph graph,
            Dictionary<string, int> distances,
            int transitUpSector,
            int transitDownSector,
            string transitUpId,
            string transitDownId)
        {
            DungeonNode best = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                if (node.nodeId == graph.entryHubNodeId || node.nodeId == transitUpId || node.nodeId == transitDownId || node.nodeKind != DungeonNodeKind.Ordinary)
                {
                    continue;
                }

                int distance = distances.TryGetValue(node.nodeId, out int found) ? found : 0;
                int degree = graph.GetDegree(node.nodeId);
                int sector = GetSector(node.gridPosition);
                int distanceFromTransitDown = graph.GetGraphDistance(node.nodeId, transitDownId);
                float score = distance * 7f + degree * 3f;
                score += sector != transitUpSector && sector != transitDownSector ? 8f : 0f;
                score += distanceFromTransitDown >= 4 ? 5f : 0f;

                if (score > bestScore)
                {
                    best = node;
                    bestScore = score;
                }
            }

            return best;
        }

        private static bool TryAttachSecret(
            DungeonLayoutGraph graph,
            Dictionary<Vector2Int, string> occupied,
            Dictionary<string, int> distances,
            System.Random random,
            HashSet<string> blocked,
            ref int nextSecretId)
        {
            List<DungeonNode> candidates = new List<DungeonNode>(graph.nodes);
            candidates.Sort((left, right) =>
            {
                int leftDistance = distances.TryGetValue(left.nodeId, out int leftFound) ? leftFound : 0;
                int rightDistance = distances.TryGetValue(right.nodeId, out int rightFound) ? rightFound : 0;
                return rightDistance.CompareTo(leftDistance);
            });

            for (int i = 0; i < candidates.Count; i++)
            {
                DungeonNode anchor = candidates[i];
                if (blocked.Contains(anchor.nodeId) || anchor.nodeKind != DungeonNodeKind.Ordinary)
                {
                    continue;
                }

                if (graph.GetDegree(anchor.nodeId) > 2)
                {
                    continue;
                }

                List<Vector2Int> positions = GetFreeAdjacentPositions(anchor.gridPosition, occupied);
                Shuffle(positions, random);

                for (int j = 0; j < positions.Count; j++)
                {
                    Vector2Int position = positions[j];
                    if (CountAdjacentOccupied(position, occupied) != 1)
                    {
                        continue;
                    }

                    int positionSector = GetSector(position);
                    bool sectorConflict = false;
                    foreach (string blockedId in blocked)
                    {
                        DungeonNode blockedNode = graph.GetNode(blockedId);
                        if (blockedNode != null && GetSector(blockedNode.gridPosition) == positionSector)
                        {
                            sectorConflict = true;
                            break;
                        }
                    }

                    if (sectorConflict)
                    {
                        continue;
                    }

                    string secretId = $"secret_{nextSecretId++}";
                    AddNode(graph, occupied, secretId, ChooseLabel(SecretLabels, random), DungeonNodeKind.Secret, position);
                    AddEdge(graph, anchor.nodeId, secretId);
                    return true;
                }
            }

            return false;
        }

        private static DungeonNode SelectFallbackSecret(DungeonLayoutGraph graph, Dictionary<string, int> distances, HashSet<string> blocked)
        {
            DungeonNode best = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                if (blocked.Contains(node.nodeId) || node.nodeKind != DungeonNodeKind.Ordinary)
                {
                    continue;
                }

                int distance = distances.TryGetValue(node.nodeId, out int found) ? found : 0;
                float score = distance * 6f - graph.GetDegree(node.nodeId) * 1.5f;
                if (score > bestScore)
                {
                    best = node;
                    bestScore = score;
                }
            }

            return best;
        }

        private static void AssignTemplates(DungeonLayoutGraph graph, System.Random random)
        {
            Dictionary<string, int> distances = graph.BuildDistanceMap(graph.entryHubNodeId);

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                int degree = graph.GetDegree(node.nodeId);
                int distance = distances.TryGetValue(node.nodeId, out int found) ? found : 0;
                int horizontalLinks = 0;
                int verticalLinks = 0;
                List<DungeonNode> neighbors = graph.GetNeighbors(node.nodeId);
                for (int neighborIndex = 0; neighborIndex < neighbors.Count; neighborIndex++)
                {
                    Vector2Int delta = neighbors[neighborIndex].gridPosition - node.gridPosition;
                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    {
                        horizontalLinks++;
                    }
                    else
                    {
                        verticalLinks++;
                    }
                }

                node.roomTemplate = ChooseTemplate(node.nodeKind, degree, distance, random);
                node.rotationQuarterTurns = ChooseRotation(node.roomTemplate, horizontalLinks, verticalLinks, random);
            }
        }

        private static DungeonRoomTemplateKind ChooseTemplate(
            DungeonNodeKind kind,
            int degree,
            int distance,
            System.Random random)
        {
            if (kind == DungeonNodeKind.EntryHub)
            {
                return random.NextDouble() > 0.45d
                    ? DungeonRoomTemplateKind.SquareChamber
                    : DungeonRoomTemplateKind.BroadRectangle;
            }

            if (kind == DungeonNodeKind.TransitUp)
            {
                return random.NextDouble() < 0.55d
                    ? DungeonRoomTemplateKind.BroadRectangle
                    : DungeonRoomTemplateKind.SquareChamber;
            }

            if (kind == DungeonNodeKind.TransitDown)
            {
                return random.NextDouble() < 0.55d
                    ? DungeonRoomTemplateKind.BroadRectangle
                    : DungeonRoomTemplateKind.SquareChamber;
            }

            if (kind == DungeonNodeKind.Landmark)
            {
                double landmarkRoll = random.NextDouble();
                if (landmarkRoll > 0.78d)
                {
                    return DungeonRoomTemplateKind.LongGallery;
                }

                if (landmarkRoll > 0.52d)
                {
                    return DungeonRoomTemplateKind.SplitChamber;
                }

                if (landmarkRoll > 0.26d)
                {
                    return DungeonRoomTemplateKind.BroadRectangle;
                }

                return DungeonRoomTemplateKind.SquareChamber;
            }

            if (kind == DungeonNodeKind.Secret)
            {
                double secretRoll = random.NextDouble();
                if (secretRoll > 0.72d)
                {
                    return DungeonRoomTemplateKind.LChamber;
                }

                if (secretRoll > 0.38d)
                {
                    return DungeonRoomTemplateKind.SquareChamber;
                }

                return DungeonRoomTemplateKind.BroadRectangle;
            }

            if (degree >= 4)
            {
                double denseRoll = random.NextDouble();
                if (denseRoll > 0.82d)
                {
                    return DungeonRoomTemplateKind.CruciformChamber;
                }

                if (denseRoll > 0.58d)
                {
                    return DungeonRoomTemplateKind.SplitChamber;
                }

                if (denseRoll > 0.28d)
                {
                    return DungeonRoomTemplateKind.BroadRectangle;
                }

                return DungeonRoomTemplateKind.SquareChamber;
            }

            if (degree == 3)
            {
                double junctionRoll = random.NextDouble();
                if (junctionRoll > 0.78d)
                {
                    return DungeonRoomTemplateKind.SplitChamber;
                }

                if (junctionRoll > 0.56d)
                {
                    return DungeonRoomTemplateKind.LChamber;
                }

                if (junctionRoll > 0.24d)
                {
                    return DungeonRoomTemplateKind.BroadRectangle;
                }

                return DungeonRoomTemplateKind.SquareChamber;
            }

            if (distance >= 5 && random.NextDouble() > 0.7d)
            {
                return DungeonRoomTemplateKind.LongGallery;
            }

            double ordinaryRoll = random.NextDouble();
            if (ordinaryRoll > 0.88d)
            {
                return DungeonRoomTemplateKind.SquareChamber;
            }

            if (ordinaryRoll > 0.68d)
            {
                return DungeonRoomTemplateKind.LongGallery;
            }

            if (ordinaryRoll > 0.42d)
            {
                return DungeonRoomTemplateKind.LChamber;
            }

            if (ordinaryRoll > 0.18d)
            {
                return DungeonRoomTemplateKind.BroadRectangle;
            }

            return DungeonRoomTemplateKind.CruciformChamber;
        }

        private static int ChooseRotation(DungeonRoomTemplateKind template, int horizontalLinks, int verticalLinks, System.Random random)
        {
            if (template == DungeonRoomTemplateKind.LongGallery ||
                template == DungeonRoomTemplateKind.BroadRectangle ||
                template == DungeonRoomTemplateKind.SplitChamber ||
                template == DungeonRoomTemplateKind.BalconyBridgeChamber)
            {
                if (horizontalLinks > verticalLinks)
                {
                    return 0;
                }

                if (verticalLinks > horizontalLinks)
                {
                    return 1;
                }
            }

            return random.Next(4);
        }

        private static List<ExpansionCandidate> CollectExpansionCandidates(
            DungeonLayoutGraph graph,
            Dictionary<Vector2Int, string> occupied,
            System.Random random,
            int targetRoomCount,
            int maxRadius)
        {
            Dictionary<Vector2Int, ExpansionCandidate> bestByPosition = new Dictionary<Vector2Int, ExpansionCandidate>();
            int[] sectorCounts = GetSectorCounts(graph);
            int targetPerSector = Mathf.CeilToInt((targetRoomCount - 1) / 4f);

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                if (node.nodeKind == DungeonNodeKind.Secret)
                {
                    continue;
                }

                int degree = graph.GetDegree(node.nodeId);
                if (node.nodeKind != DungeonNodeKind.EntryHub && degree >= 4)
                {
                    continue;
                }

                for (int dirIndex = 0; dirIndex < CardinalDirections.Length; dirIndex++)
                {
                    Vector2Int candidatePosition = node.gridPosition + CardinalDirections[dirIndex];
                    if (occupied.ContainsKey(candidatePosition))
                    {
                        continue;
                    }

                    int radius = Mathf.Max(Mathf.Abs(candidatePosition.x), Mathf.Abs(candidatePosition.y));
                    if (radius > maxRadius)
                    {
                        continue;
                    }

                    int adjacentOccupied = CountAdjacentOccupied(candidatePosition, occupied);
                    if (adjacentOccupied == 0)
                    {
                        continue;
                    }

                    int openCount = CountFreeAdjacentPositions(candidatePosition, occupied);
                    int sector = GetSector(candidatePosition);
                    float score = 0f;
                    score += Mathf.Max(0, targetPerSector - sectorCounts[sector]) * 2.4f;
                    score += Mathf.Min(2, adjacentOccupied - 1) * 3.2f;
                    score += radius <= 2 ? 2.1f : 0f;
                    score += radius >= 3 ? 1.2f : 0f;
                    score += degree <= 2 ? 0.7f : -0.4f;
                    score += openCount * 0.25f;
                    score += (float)random.NextDouble();

                    ExpansionCandidate candidate = new ExpansionCandidate
                    {
                        parentId = node.nodeId,
                        position = candidatePosition,
                        score = score
                    };

                    if (!bestByPosition.TryGetValue(candidatePosition, out ExpansionCandidate currentBest) || candidate.score > currentBest.score)
                    {
                        bestByPosition[candidatePosition] = candidate;
                    }
                }
            }

            return new List<ExpansionCandidate>(bestByPosition.Values);
        }

        private static void EnsureOuterCoverage(
            DungeonLayoutGraph graph,
            Dictionary<Vector2Int, string> occupied,
            System.Random random,
            int targetRoomCount,
            int maxRadius,
            ref int nextOrdinaryId)
        {
            int[] sectorCounts = GetSectorCounts(graph);
            int minimumSectors = targetRoomCount >= 24 ? 4 : 3;
            int covered = 0;
            for (int i = 0; i < sectorCounts.Length; i++)
            {
                if (sectorCounts[i] > 0)
                {
                    covered++;
                }
            }

            if (covered >= minimumSectors)
            {
                return;
            }

            for (int sector = 0; sector < 4; sector++)
            {
                if (sectorCounts[sector] > 0)
                {
                    continue;
                }

                Vector2Int direction = sector switch
                {
                    0 => new Vector2Int(1, 0),
                    1 => new Vector2Int(-1, 0),
                    2 => new Vector2Int(0, 1),
                    _ => new Vector2Int(0, -1)
                };

                for (int step = 2; step <= maxRadius; step++)
                {
                    Vector2Int position = direction * step;
                    if (occupied.ContainsKey(position))
                    {
                        continue;
                    }

                    string nodeId = $"room_{nextOrdinaryId++}";
                    AddNode(graph, occupied, nodeId, ChooseLabel(OrdinaryLabels, random), DungeonNodeKind.Ordinary, position);
                    ConnectToAdjacentOccupied(graph, occupied, nodeId, position, random, false);
                    break;
                }
            }
        }

        private static void ConnectToAdjacentOccupied(
            DungeonLayoutGraph graph,
            Dictionary<Vector2Int, string> occupied,
            string nodeId,
            Vector2Int position,
            System.Random random,
            bool forceAllAdjacent)
        {
            List<string> adjacentIds = GetAdjacentOccupiedNodeIds(position, occupied);
            Shuffle(adjacentIds, random);

            bool hasConnected = false;
            for (int i = 0; i < adjacentIds.Count; i++)
            {
                string otherId = adjacentIds[i];
                if (string.IsNullOrWhiteSpace(otherId) || otherId == nodeId)
                {
                    continue;
                }

                bool shouldConnect = !hasConnected || forceAllAdjacent || random.NextDouble() > 0.55d;
                if (!shouldConnect)
                {
                    continue;
                }

                AddEdge(graph, nodeId, otherId);
                hasConnected = true;
            }

            if (!hasConnected && adjacentIds.Count > 0)
            {
                AddEdge(graph, nodeId, adjacentIds[0]);
            }
        }

        private static void AddLoopEdges(
            DungeonLayoutGraph graph,
            Dictionary<Vector2Int, string> occupied,
            System.Random random,
            int floorIndex)
        {
            List<DungeonEdge> candidates = new List<DungeonEdge>();

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                if (node.nodeKind == DungeonNodeKind.Secret)
                {
                    continue;
                }

                for (int dirIndex = 0; dirIndex < CardinalDirections.Length; dirIndex++)
                {
                    Vector2Int neighborPosition = node.gridPosition + CardinalDirections[dirIndex];
                    if (!occupied.TryGetValue(neighborPosition, out string neighborId))
                    {
                        continue;
                    }

                    DungeonNode neighbor = graph.GetNode(neighborId);
                    if (neighbor == null || neighbor.nodeKind == DungeonNodeKind.Secret)
                    {
                        continue;
                    }

                    if (neighbor.gridPosition.x < node.gridPosition.x ||
                        (neighbor.gridPosition.x == node.gridPosition.x && neighbor.gridPosition.y < node.gridPosition.y))
                    {
                        continue;
                    }

                    if (HasEdge(graph, node.nodeId, neighbor.nodeId))
                    {
                        continue;
                    }

                    candidates.Add(new DungeonEdge { a = node.nodeId, b = neighbor.nodeId });
                }
            }

            Shuffle(candidates, random);
            int loopCount = Mathf.Min(candidates.Count, Mathf.Clamp(3 + floorIndex / 3, 3, 10));
            for (int i = 0; i < loopCount; i++)
            {
                AddEdge(graph, candidates[i].a, candidates[i].b);
            }
        }

        private static bool IsValidGraph(DungeonLayoutGraph graph, int floorIndex)
        {
            if (graph == null)
            {
                return false;
            }

            DungeonNode entryHub = graph.GetNode(graph.entryHubNodeId);
            DungeonNode transitUp = graph.GetNode(graph.transitUpNodeId);
            DungeonNode transitDown = graph.GetNode(graph.transitDownNodeId);

            if (entryHub == null || transitUp == null || transitDown == null)
            {
                return false;
            }

            if (!graph.HasPath(graph.entryHubNodeId, graph.transitUpNodeId) || !graph.HasPath(graph.entryHubNodeId, graph.transitDownNodeId))
            {
                return false;
            }

            HashSet<Vector2Int> seenPositions = new HashSet<Vector2Int>();
            int[] sectorCounts = new int[4];
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                if (!seenPositions.Add(node.gridPosition))
                {
                    return false;
                }

                minX = Mathf.Min(minX, node.gridPosition.x);
                maxX = Mathf.Max(maxX, node.gridPosition.x);
                minY = Mathf.Min(minY, node.gridPosition.y);
                maxY = Mathf.Max(maxY, node.gridPosition.y);

                if (node.nodeId != graph.entryHubNodeId)
                {
                    sectorCounts[GetSector(node.gridPosition)]++;
                }
            }

            if (graph.nodes.Count < GetMinimumRoomCount(floorIndex) || graph.GetDegree(graph.entryHubNodeId) < 3)
            {
                return false;
            }

            int coveredSectors = 0;
            for (int i = 0; i < sectorCounts.Length; i++)
            {
                if (sectorCounts[i] > 0)
                {
                    coveredSectors++;
                }
            }

            if (coveredSectors < (floorIndex >= 4 ? 4 : 3))
            {
                return false;
            }

            if (Mathf.Min(maxX - minX, maxY - minY) < 4 || graph.edges.Count < graph.nodes.Count)
            {
                return false;
            }

            DungeonNode landmark = FindFirstNodeByKind(graph, DungeonNodeKind.Landmark);
            DungeonNode secret = FindFirstNodeByKind(graph, DungeonNodeKind.Secret);
            int entryToDown = graph.GetGraphDistance(graph.entryHubNodeId, graph.transitDownNodeId);
            int upToDown = graph.GetGraphDistance(graph.transitUpNodeId, graph.transitDownNodeId);
            if (entryToDown < Mathf.Clamp(5 + floorIndex / 4, 5, 12) || upToDown < 4)
            {
                return false;
            }

            if (landmark != null && graph.GetGraphDistance(landmark.nodeId, graph.transitDownNodeId) < 3)
            {
                return false;
            }

            if (secret != null)
            {
                if (graph.GetGraphDistance(secret.nodeId, graph.transitDownNodeId) < 3)
                {
                    return false;
                }

                if (landmark != null && graph.GetGraphDistance(secret.nodeId, landmark.nodeId) < 3)
                {
                    return false;
                }
            }

            return true;
        }

        private static DungeonNode FindFirstNodeByKind(DungeonLayoutGraph graph, DungeonNodeKind kind)
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

        private static void SyncLegacyIds(DungeonLayoutGraph graph)
        {
            graph.entryNodeId = graph.entryHubNodeId;
            graph.stairsNodeId = graph.transitDownNodeId;
            graph.returnAnchorNodeId = graph.transitUpNodeId;
        }

        private static int GetTargetRoomCount(int floorIndex, System.Random random)
        {
            if (floorIndex <= 3)
            {
                return random.Next(16, 23);
            }

            if (floorIndex <= 8)
            {
                return random.Next(22, 31);
            }

            if (floorIndex <= 15)
            {
                return random.Next(30, 41);
            }

            int baseCount = 38 + Mathf.Min(12, floorIndex - 15);
            return random.Next(baseCount, baseCount + 10);
        }

        private static int GetMinimumRoomCount(int floorIndex)
        {
            if (floorIndex <= 3)
            {
                return 16;
            }

            if (floorIndex <= 8)
            {
                return 22;
            }

            if (floorIndex <= 15)
            {
                return 30;
            }

            return 38;
        }

        private static int GetMaxRadius(int floorIndex, int targetRoomCount)
        {
            return Mathf.Clamp(4 + floorIndex / 2 + targetRoomCount / 18, 6, 16);
        }

        private static void AddNode(
            DungeonLayoutGraph graph,
            Dictionary<Vector2Int, string> occupied,
            string id,
            string label,
            DungeonNodeKind kind,
            Vector2Int position)
        {
            graph.nodes.Add(new DungeonNode
            {
                nodeId = id,
                label = label,
                nodeKind = kind,
                gridPosition = position
            });

            occupied[position] = id;
        }

        private static void AddEdge(DungeonLayoutGraph graph, string a, string b)
        {
            if (!HasEdge(graph, a, b))
            {
                graph.edges.Add(new DungeonEdge { a = a, b = b });
            }
        }

        private static bool HasEdge(DungeonLayoutGraph graph, string a, string b)
        {
            for (int i = 0; i < graph.edges.Count; i++)
            {
                if ((graph.edges[i].a == a && graph.edges[i].b == b) ||
                    (graph.edges[i].a == b && graph.edges[i].b == a))
                {
                    return true;
                }
            }

            return false;
        }

        private static int[] GetSectorCounts(DungeonLayoutGraph graph)
        {
            int[] counts = new int[4];
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                if (node.nodeId == graph.entryHubNodeId)
                {
                    continue;
                }

                counts[GetSector(node.gridPosition)]++;
            }

            return counts;
        }

        private static int GetSector(Vector2Int position)
        {
            if (Mathf.Abs(position.x) >= Mathf.Abs(position.y))
            {
                return position.x >= 0 ? 0 : 1;
            }

            return position.y >= 0 ? 2 : 3;
        }

        private static int Manhattan(Vector2Int position)
        {
            return Mathf.Abs(position.x) + Mathf.Abs(position.y);
        }

        private static int CountAdjacentOccupied(Vector2Int position, Dictionary<Vector2Int, string> occupied)
        {
            int count = 0;
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                if (occupied.ContainsKey(position + CardinalDirections[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountFreeAdjacentPositions(Vector2Int position, Dictionary<Vector2Int, string> occupied)
        {
            int count = 0;
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                if (!occupied.ContainsKey(position + CardinalDirections[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static List<string> GetAdjacentOccupiedNodeIds(Vector2Int position, Dictionary<Vector2Int, string> occupied)
        {
            List<string> ids = new List<string>();
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                if (occupied.TryGetValue(position + CardinalDirections[i], out string nodeId))
                {
                    ids.Add(nodeId);
                }
            }

            return ids;
        }

        private static List<Vector2Int> GetFreeAdjacentPositions(Vector2Int position, Dictionary<Vector2Int, string> occupied)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int candidate = position + CardinalDirections[i];
                if (!occupied.ContainsKey(candidate))
                {
                    positions.Add(candidate);
                }
            }

            return positions;
        }

        private static void Shuffle<T>(List<T> values, System.Random random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
            }
        }

        private static string ChooseLabel(string[] source, System.Random random)
        {
            return source[random.Next(source.Length)];
        }
    }
}
