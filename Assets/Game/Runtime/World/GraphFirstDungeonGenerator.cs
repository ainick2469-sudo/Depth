using System;
using System.Collections.Generic;
using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class GraphFirstDungeonGenerator : IDungeonGenerator
    {
        internal const int NormalGenerationAttemptsPerCall = 16;
        internal const int NormalGenerationSeedStep = 1543;

        private struct ExpansionCandidate
        {
            public string parentId;
            public Vector2Int position;
            public float score;
        }

        private struct LoopEdgeCandidate
        {
            public DungeonEdge edge;
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

        public bool TryGenerateNormal(FloorState floorState, out DungeonLayoutGraph graph, out GraphValidationReport report)
        {
            int floorIndex = Mathf.Max(1, floorState.floorIndex);
            int baseSeed = floorState.floorSeed == 0 ? 1000 + floorIndex * 977 : floorState.floorSeed;
            report = new GraphValidationReport
            {
                floorIndex = floorIndex,
                seed = baseSeed
            };

            GraphValidationAttemptResult bestAttempt = null;

            for (int attempt = 0; attempt < NormalGenerationAttemptsPerCall; attempt++)
            {
                int attemptSeed = GetNormalAttemptSeed(baseSeed, attempt);
                DungeonLayoutGraph candidate = BuildGraph(attemptSeed, floorIndex);
                GraphValidationAttemptResult attemptResult = ValidateGraph(candidate, floorIndex, attemptSeed, attempt + 1);
                report.attempts.Add(attemptResult);
                report.attemptCount = attempt + 1;

                if (attemptResult.IsValid)
                {
                    report.AdoptAttempt(attemptResult);
                    graph = candidate;
                    return true;
                }

                if (IsBetterAttempt(attemptResult, bestAttempt))
                {
                    bestAttempt = attemptResult;
                }
            }

            report.AdoptAttempt(bestAttempt);
            if (bestAttempt == null)
            {
                report.failures.Add("No normal generation attempts were evaluated.");
            }

            graph = bestAttempt != null ? bestAttempt.graph : null;
            return false;
        }

        public DungeonLayoutGraph GenerateFallback(FloorState floorState)
        {
            int floorIndex = Mathf.Max(1, floorState.floorIndex);
            int baseSeed = floorState.floorSeed == 0 ? 1000 + floorIndex * 977 : floorState.floorSeed;
            return BuildFallbackGraph(baseSeed, floorIndex);
        }

        internal static int GetNormalAttemptBaseSeed(int baseSeed, int outerAttemptIndex)
        {
            return baseSeed + outerAttemptIndex * NormalGenerationSeedStep * NormalGenerationAttemptsPerCall;
        }

        internal static int GetNormalAttemptSeed(int baseSeed, int attemptIndex)
        {
            return baseSeed + attemptIndex * NormalGenerationSeedStep;
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

            EnsureOuterCoverage(graph, occupied, random, floorIndex, targetRoomCount, maxRadius, ref nextOrdinaryId);
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
            AddNode(graph, occupied, "transit_up", floorIndex == 1 ? "Surface Lift" : "Upper Stairs", DungeonNodeKind.TransitUp, new Vector2Int(1, 0));
            AddNode(graph, occupied, "ordinary_0", ChooseLabel(OrdinaryLabels, random), DungeonNodeKind.Ordinary, new Vector2Int(-1, 0));
            AddNode(graph, occupied, "landmark", ChooseLabel(LandmarkLabels, random), DungeonNodeKind.Landmark, new Vector2Int(-2, 0));
            AddNode(graph, occupied, "secret_0", ChooseLabel(SecretLabels, random), DungeonNodeKind.Secret, new Vector2Int(0, -1));
            AddNode(graph, occupied, "transit_down", "Stair Chamber", DungeonNodeKind.TransitDown, new Vector2Int(-1, 1));

            AddEdge(graph, "entry_hub", "transit_up");
            AddEdge(graph, "entry_hub", "ordinary_0");
            AddEdge(graph, "entry_hub", "secret_0");
            AddEdge(graph, "ordinary_0", "landmark");
            AddEdge(graph, "ordinary_0", "transit_down");

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
            List<DungeonNode> orderedNodes = new List<DungeonNode>(graph.nodes);
            orderedNodes.Sort((left, right) =>
            {
                int leftDistance = distances.TryGetValue(left.nodeId, out int leftFound) ? leftFound : 0;
                int rightDistance = distances.TryGetValue(right.nodeId, out int rightFound) ? rightFound : 0;
                int distanceCompare = leftDistance.CompareTo(rightDistance);
                if (distanceCompare != 0)
                {
                    return distanceCompare;
                }

                return string.CompareOrdinal(left.nodeId, right.nodeId);
            });

            Dictionary<string, DungeonRoomTemplateKind> assignedTemplates = new Dictionary<string, DungeonRoomTemplateKind>();

            for (int i = 0; i < orderedNodes.Count; i++)
            {
                DungeonNode node = orderedNodes[i];
                int degree = graph.GetDegree(node.nodeId);
                int distance = distances.TryGetValue(node.nodeId, out int found) ? found : 0;
                DungeonExitMask requiredExits = GetRequiredExitMask(graph, node);
                List<DungeonRoomTemplateKind> candidates = GetTemplateCandidates(node.nodeKind, requiredExits, degree, distance);
                FilterTemplatesByRotationFit(candidates, requiredExits);
                ApplySizeTierPreference(node.nodeKind, candidates, random);
                ApplyAntiRepetitionRules(graph, node, assignedTemplates, candidates);

                if (candidates.Count == 0)
                {
                    candidates = GetFallbackTemplateCandidates(node.nodeKind, requiredExits);
                    FilterTemplatesByRotationFit(candidates, requiredExits);
                    ApplySizeTierPreference(node.nodeKind, candidates, random);
                }

                if (candidates.Count == 0)
                {
                    candidates = new List<DungeonRoomTemplateKind>
                    {
                        DungeonRoomTemplateKind.SquareChamber
                    };
                }

                node.roomTemplate = candidates[random.Next(candidates.Count)];
                List<int> validRotations = DungeonRoomTemplateLibrary.GetValidRotations(node.roomTemplate, requiredExits);
                node.rotationQuarterTurns = ChooseRotation(node.roomTemplate, requiredExits, validRotations, random);
                assignedTemplates[node.nodeId] = node.roomTemplate;
            }
        }

        private static List<DungeonRoomTemplateKind> GetTemplateCandidates(
            DungeonNodeKind kind,
            DungeonExitMask requiredExits,
            int degree,
            int distance)
        {
            if (kind == DungeonNodeKind.EntryHub)
            {
                return new List<DungeonRoomTemplateKind>
                {
                    DungeonRoomTemplateKind.BroadRectangle,
                    DungeonRoomTemplateKind.OctagonChamberSafe,
                    DungeonRoomTemplateKind.SquareChamber
                };
            }

            if (kind == DungeonNodeKind.TransitUp)
            {
                return new List<DungeonRoomTemplateKind>
                {
                    DungeonRoomTemplateKind.BroadRectangle,
                    DungeonRoomTemplateKind.SquareChamber
                };
            }

            if (kind == DungeonNodeKind.TransitDown)
            {
                return new List<DungeonRoomTemplateKind>
                {
                    DungeonRoomTemplateKind.BroadRectangle,
                    DungeonRoomTemplateKind.SquareChamber
                };
            }

            if (kind == DungeonNodeKind.Landmark)
            {
                return new List<DungeonRoomTemplateKind>
                {
                    DungeonRoomTemplateKind.OctagonChamberSafe,
                    DungeonRoomTemplateKind.BroadRectangle,
                    DungeonRoomTemplateKind.SquareChamber
                };
            }

            if (kind == DungeonNodeKind.Secret)
            {
                return new List<DungeonRoomTemplateKind>
                {
                    DungeonRoomTemplateKind.AlcoveRoomSafe,
                    DungeonRoomTemplateKind.SquareChamber
                };
            }

            if (degree <= 1)
            {
                return new List<DungeonRoomTemplateKind>
                {
                    DungeonRoomTemplateKind.AlcoveRoomSafe,
                    DungeonRoomTemplateKind.SquareChamber,
                    DungeonRoomTemplateKind.BroadRectangle,
                    DungeonRoomTemplateKind.OctagonChamberSafe
                };
            }

            if (degree == 2 && IsCornerMask(requiredExits))
            {
                return new List<DungeonRoomTemplateKind>
                {
                    DungeonRoomTemplateKind.LChamberSafe,
                    DungeonRoomTemplateKind.WideBendSafe
                };
            }

            if (degree == 2)
            {
                return new List<DungeonRoomTemplateKind>
                {
                    DungeonRoomTemplateKind.LongGallery,
                    DungeonRoomTemplateKind.PillarHallSafe,
                    DungeonRoomTemplateKind.BroadRectangle,
                };
            }

            if (degree == 3)
            {
                return new List<DungeonRoomTemplateKind>
                {
                    DungeonRoomTemplateKind.TChamberSafe,
                    DungeonRoomTemplateKind.ForkRoomSafe
                };
            }

            return new List<DungeonRoomTemplateKind>
            {
                DungeonRoomTemplateKind.CrossChamberSafe,
                DungeonRoomTemplateKind.SquareChamber,
                DungeonRoomTemplateKind.BroadRectangle
            };
        }

        private static List<DungeonRoomTemplateKind> GetFallbackTemplateCandidates(
            DungeonNodeKind kind,
            DungeonExitMask requiredExits)
        {
            DungeonRoomTemplateKind[] safeTemplates = DungeonRoomTemplateLibrary.GetGateOneSafeOrdinaryTemplates();
            List<DungeonRoomTemplateKind> fallback = new List<DungeonRoomTemplateKind>();
            for (int i = 0; i < safeTemplates.Length; i++)
            {
                if (kind != DungeonNodeKind.Ordinary && safeTemplates[i] == DungeonRoomTemplateKind.CrossChamberSafe)
                {
                    continue;
                }

                if (DungeonRoomTemplateLibrary.GetValidRotations(safeTemplates[i], requiredExits).Count > 0)
                {
                    fallback.Add(safeTemplates[i]);
                }
            }

            return fallback;
        }

        private static void ApplySizeTierPreference(
            DungeonNodeKind kind,
            List<DungeonRoomTemplateKind> candidates,
            System.Random random)
        {
            if (candidates.Count <= 1)
            {
                return;
            }

            DungeonRoomSizeTier desiredTier = ChooseDesiredSizeTier(kind, random);
            DungeonRoomSizeTier[] fallbackOrder = GetFallbackTierOrder(desiredTier);
            for (int orderIndex = 0; orderIndex < fallbackOrder.Length; orderIndex++)
            {
                List<DungeonRoomTemplateKind> matching = new List<DungeonRoomTemplateKind>();
                for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    if (DungeonRoomTemplateLibrary.GetSizeTier(candidates[candidateIndex]) == fallbackOrder[orderIndex])
                    {
                        matching.Add(candidates[candidateIndex]);
                    }
                }

                if (matching.Count == 0)
                {
                    continue;
                }

                candidates.Clear();
                candidates.AddRange(matching);
                return;
            }
        }

        private static DungeonRoomSizeTier ChooseDesiredSizeTier(DungeonNodeKind kind, System.Random random)
        {
            int roll = random.Next(100);
            return kind switch
            {
                DungeonNodeKind.EntryHub => DungeonRoomSizeTier.Large,
                DungeonNodeKind.Landmark => roll < 40 ? DungeonRoomSizeTier.Large : DungeonRoomSizeTier.Grand,
                DungeonNodeKind.TransitUp => roll < 45 ? DungeonRoomSizeTier.Medium : DungeonRoomSizeTier.Large,
                DungeonNodeKind.TransitDown => roll < 45 ? DungeonRoomSizeTier.Medium : DungeonRoomSizeTier.Large,
                DungeonNodeKind.Secret => roll < 45 ? DungeonRoomSizeTier.Small : DungeonRoomSizeTier.Medium,
                _ => roll switch
                {
                    < 10 => DungeonRoomSizeTier.Small,
                    < 55 => DungeonRoomSizeTier.Medium,
                    < 90 => DungeonRoomSizeTier.Large,
                    _ => DungeonRoomSizeTier.Grand
                }
            };
        }

        private static DungeonRoomSizeTier[] GetFallbackTierOrder(DungeonRoomSizeTier desiredTier)
        {
            return desiredTier switch
            {
                DungeonRoomSizeTier.Small => new[]
                {
                    DungeonRoomSizeTier.Small,
                    DungeonRoomSizeTier.Medium,
                    DungeonRoomSizeTier.Large,
                    DungeonRoomSizeTier.Grand
                },
                DungeonRoomSizeTier.Medium => new[]
                {
                    DungeonRoomSizeTier.Medium,
                    DungeonRoomSizeTier.Large,
                    DungeonRoomSizeTier.Small,
                    DungeonRoomSizeTier.Grand
                },
                DungeonRoomSizeTier.Large => new[]
                {
                    DungeonRoomSizeTier.Large,
                    DungeonRoomSizeTier.Grand,
                    DungeonRoomSizeTier.Medium,
                    DungeonRoomSizeTier.Small
                },
                _ => new[]
                {
                    DungeonRoomSizeTier.Grand,
                    DungeonRoomSizeTier.Large,
                    DungeonRoomSizeTier.Medium,
                    DungeonRoomSizeTier.Small
                }
            };
        }

        private static void ApplyAntiRepetitionRules(
            DungeonLayoutGraph graph,
            DungeonNode node,
            Dictionary<string, DungeonRoomTemplateKind> assignedTemplates,
            List<DungeonRoomTemplateKind> candidates)
        {
            if (node.nodeKind != DungeonNodeKind.Ordinary || candidates.Count <= 1)
            {
                return;
            }

            List<DungeonRoomTemplateKind> adjacencyFiltered = new List<DungeonRoomTemplateKind>();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (!UsesAdjacentTemplate(graph, node, candidates[i], assignedTemplates))
                {
                    adjacencyFiltered.Add(candidates[i]);
                }
            }

            if (adjacencyFiltered.Count > 0)
            {
                candidates.Clear();
                candidates.AddRange(adjacencyFiltered);
            }

            List<DungeonRoomTemplateKind> streakFiltered = new List<DungeonRoomTemplateKind>();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (!WouldCreateThreeRoomStreak(graph, node, candidates[i], assignedTemplates))
                {
                    streakFiltered.Add(candidates[i]);
                }
            }

            if (streakFiltered.Count > 0)
            {
                candidates.Clear();
                candidates.AddRange(streakFiltered);
            }
        }

        private static bool UsesAdjacentTemplate(
            DungeonLayoutGraph graph,
            DungeonNode node,
            DungeonRoomTemplateKind template,
            Dictionary<string, DungeonRoomTemplateKind> assignedTemplates)
        {
            List<DungeonNode> neighbors = graph.GetNeighbors(node.nodeId);
            for (int i = 0; i < neighbors.Count; i++)
            {
                DungeonNode neighbor = neighbors[i];
                if (neighbor.nodeKind != DungeonNodeKind.Ordinary)
                {
                    continue;
                }

                if (assignedTemplates.TryGetValue(neighbor.nodeId, out DungeonRoomTemplateKind assigned) && assigned == template)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool WouldCreateThreeRoomStreak(
            DungeonLayoutGraph graph,
            DungeonNode node,
            DungeonRoomTemplateKind template,
            Dictionary<string, DungeonRoomTemplateKind> assignedTemplates)
        {
            List<DungeonNode> neighbors = graph.GetNeighbors(node.nodeId);
            for (int i = 0; i < neighbors.Count; i++)
            {
                DungeonNode neighbor = neighbors[i];
                if (neighbor.nodeKind != DungeonNodeKind.Ordinary)
                {
                    continue;
                }

                if (!assignedTemplates.TryGetValue(neighbor.nodeId, out DungeonRoomTemplateKind assigned) || assigned != template)
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

                    if (assignedTemplates.TryGetValue(secondary.nodeId, out DungeonRoomTemplateKind secondaryTemplate) && secondaryTemplate == template)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void FilterTemplatesByRotationFit(List<DungeonRoomTemplateKind> candidates, DungeonExitMask requiredExits)
        {
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (DungeonRoomTemplateLibrary.GetValidRotations(candidates[i], requiredExits).Count == 0)
                {
                    candidates.RemoveAt(i);
                }
            }
        }

        private static int ChooseRotation(
            DungeonRoomTemplateKind template,
            DungeonExitMask requiredExits,
            List<int> validRotations,
            System.Random random)
        {
            if (validRotations == null || validRotations.Count == 0)
            {
                return 0;
            }

            if (template == DungeonRoomTemplateKind.LongGallery ||
                template == DungeonRoomTemplateKind.PillarHallSafe ||
                template == DungeonRoomTemplateKind.BroadRectangle)
            {
                if (IsVerticalMask(requiredExits) && validRotations.Contains(1))
                {
                    return 1;
                }

                if (IsHorizontalMask(requiredExits) && validRotations.Contains(0))
                {
                    return 0;
                }
            }

            return validRotations[random.Next(validRotations.Count)];
        }

        private static DungeonExitMask GetRequiredExitMask(DungeonLayoutGraph graph, DungeonNode node)
        {
            DungeonExitMask requiredExits = DungeonExitMask.None;
            List<DungeonNode> neighbors = graph.GetNeighbors(node.nodeId);
            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector2Int delta = ClampToCardinal(neighbors[i].gridPosition - node.gridPosition);
                requiredExits |= DungeonRoomTemplateLibrary.DirectionToMask(delta);
            }

            return requiredExits;
        }

        private static bool IsCornerMask(DungeonExitMask mask)
        {
            return mask == (DungeonExitMask.North | DungeonExitMask.East) ||
                   mask == (DungeonExitMask.North | DungeonExitMask.West) ||
                   mask == (DungeonExitMask.South | DungeonExitMask.East) ||
                   mask == (DungeonExitMask.South | DungeonExitMask.West);
        }

        private static bool IsHorizontalMask(DungeonExitMask mask)
        {
            return mask == (DungeonExitMask.East | DungeonExitMask.West);
        }

        private static bool IsVerticalMask(DungeonExitMask mask)
        {
            return mask == (DungeonExitMask.North | DungeonExitMask.South);
        }

        private static int CountCoveredSectors(int[] sectorCounts)
        {
            int covered = 0;
            for (int i = 0; i < sectorCounts.Length; i++)
            {
                if (sectorCounts[i] > 0)
                {
                    covered++;
                }
            }

            return covered;
        }

        private static Vector2 GetClusterCenter(DungeonLayoutGraph graph)
        {
            if (graph.nodes.Count == 0)
            {
                return Vector2.zero;
            }

            Vector2 sum = Vector2.zero;
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                sum += graph.nodes[i].gridPosition;
            }

            return sum / graph.nodes.Count;
        }

        private static void GetClusterBounds(DungeonLayoutGraph graph, out int minX, out int maxX, out int minY, out int maxY)
        {
            minX = int.MaxValue;
            maxX = int.MinValue;
            minY = int.MaxValue;
            maxY = int.MinValue;

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                Vector2Int position = graph.nodes[i].gridPosition;
                minX = Mathf.Min(minX, position.x);
                maxX = Mathf.Max(maxX, position.x);
                minY = Mathf.Min(minY, position.y);
                maxY = Mathf.Max(maxY, position.y);
            }
        }

        private static int GetBoundsExpansion(Vector2Int position, int minX, int maxX, int minY, int maxY)
        {
            int expansion = 0;
            if (position.x < minX)
            {
                expansion += minX - position.x;
            }
            else if (position.x > maxX)
            {
                expansion += position.x - maxX;
            }

            if (position.y < minY)
            {
                expansion += minY - position.y;
            }
            else if (position.y > maxY)
            {
                expansion += position.y - maxY;
            }

            return expansion;
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
            int coveredSectors = CountCoveredSectors(sectorCounts);
            Vector2 clusterCenter = GetClusterCenter(graph);
            GetClusterBounds(graph, out int minX, out int maxX, out int minY, out int maxY);

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
                    float centerDistance = Vector2.Distance(clusterCenter, candidatePosition);
                    int boundsExpansion = GetBoundsExpansion(candidatePosition, minX, maxX, minY, maxY);
                    float score = 0f;
                    if (coveredSectors < Mathf.Min(4, targetPerSector))
                    {
                        score += Mathf.Max(0, targetPerSector - sectorCounts[sector]) * 1.8f;
                    }

                    score += adjacentOccupied == 2 ? 7.25f : 0f;
                    score += adjacentOccupied >= 3 ? 5.75f : 0f;
                    score += adjacentOccupied == 1 ? -2.4f : 0f;
                    score += radius <= 2 ? 2.2f : 0f;
                    score += boundsExpansion == 0 && adjacentOccupied >= 2 ? 1.2f : 0f;
                    score -= centerDistance * 1.35f;
                    score -= boundsExpansion * 1.8f;
                    score += degree <= 2 ? 0.9f : -0.5f;
                    score -= Mathf.Max(0, openCount - 2) * 0.35f;
                    score -= radius >= 4 && adjacentOccupied <= 1 ? 3.25f : 0f;
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
            int floorIndex,
            int targetRoomCount,
            int maxRadius,
            ref int nextOrdinaryId)
        {
            int[] sectorCounts = GetSectorCounts(graph);
            int minimumSectors = floorIndex >= 4 ? 4 : 3;
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
            List<LoopEdgeCandidate> candidates = new List<LoopEdgeCandidate>();

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

                    int graphDistance = graph.GetGraphDistance(node.nodeId, neighbor.nodeId);
                    int combinedDegree = graph.GetDegree(node.nodeId) + graph.GetDegree(neighbor.nodeId);
                    float score = graphDistance * 2.6f;
                    score += combinedDegree <= 5 ? 1.8f : -1.6f;
                    score += node.nodeKind == DungeonNodeKind.EntryHub || neighbor.nodeKind == DungeonNodeKind.EntryHub ? -2.5f : 0f;
                    score += (float)random.NextDouble();

                    candidates.Add(new LoopEdgeCandidate
                    {
                        edge = new DungeonEdge { a = node.nodeId, b = neighbor.nodeId },
                        score = score
                    });
                }
            }

            candidates.Sort((left, right) => right.score.CompareTo(left.score));
            int loopCount = Mathf.Min(candidates.Count, Mathf.Clamp(2 + floorIndex / 5, 2, 5));
            for (int i = 0; i < loopCount; i++)
            {
                DungeonEdge edge = candidates[i].edge;
                if (graph.GetDegree(edge.a) >= 4 && graph.GetDegree(edge.b) >= 4)
                {
                    continue;
                }

                AddEdge(graph, edge.a, edge.b);
            }
        }

        private static GraphValidationAttemptResult ValidateGraph(DungeonLayoutGraph graph, int floorIndex, int attemptSeed, int attemptNumber)
        {
            GraphValidationAttemptResult result = new GraphValidationAttemptResult
            {
                attemptNumber = attemptNumber,
                attemptSeed = attemptSeed,
                graph = graph,
                layoutSignature = DungeonLayoutSignatureUtility.BuildSignature(graph, floorIndex, attemptSeed)
            };

            if (graph == null)
            {
                result.failures.Add("Graph was null.");
                return result;
            }

            DungeonNode entryHub = graph.GetNode(graph.entryHubNodeId);
            DungeonNode transitUp = graph.GetNode(graph.transitUpNodeId);
            DungeonNode transitDown = graph.GetNode(graph.transitDownNodeId);

            if (entryHub == null)
            {
                result.failures.Add("Missing entry hub node.");
            }

            if (transitUp == null)
            {
                result.failures.Add("Missing transit up node.");
            }

            if (transitDown == null)
            {
                result.failures.Add("Missing transit down node.");
            }

            if (entryHub != null && transitUp != null && !graph.HasPath(graph.entryHubNodeId, graph.transitUpNodeId))
            {
                result.failures.Add("No path from entry hub to transit up.");
            }

            if (entryHub != null && transitDown != null && !graph.HasPath(graph.entryHubNodeId, graph.transitDownNodeId))
            {
                result.failures.Add("No path from entry hub to transit down.");
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
                    result.failures.Add($"Duplicate room position at {node.gridPosition.x},{node.gridPosition.y}.");
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

            int minimumRoomCount = GetMinimumRoomCount(floorIndex);
            if (graph.nodes.Count < minimumRoomCount)
            {
                result.failures.Add($"Too few rooms for floor {floorIndex} ({graph.nodes.Count} < {minimumRoomCount}).");
            }

            int entryDegree = entryHub != null ? graph.GetDegree(graph.entryHubNodeId) : 0;
            int requiredEntryDegree = GetRequiredEntryDegree(floorIndex);
            if (entryDegree < requiredEntryDegree)
            {
                result.failures.Add($"Entry hub degree {entryDegree} below required {requiredEntryDegree}.");
            }

            int preferredEntryDegree = GetPreferredEntryDegree(floorIndex);
            if (preferredEntryDegree > requiredEntryDegree && entryDegree < preferredEntryDegree)
            {
                result.warnings.Add($"Entry hub degree {entryDegree} below preferred {preferredEntryDegree}.");
            }

            int coveredSectors = CountCoveredSectors(sectorCounts);
            int requiredCoveredSectors = GetRequiredCoveredSectors(floorIndex);
            if (coveredSectors < requiredCoveredSectors)
            {
                result.failures.Add($"Covered sectors {coveredSectors} below required {requiredCoveredSectors}.");
            }

            int preferredCoveredSectors = GetPreferredCoveredSectors(floorIndex);
            if (preferredCoveredSectors > requiredCoveredSectors && coveredSectors < preferredCoveredSectors)
            {
                result.warnings.Add($"Covered sectors {coveredSectors} below preferred {preferredCoveredSectors}.");
            }

            int minorAxisExtent = graph.nodes.Count > 0 ? Mathf.Min(maxX - minX, maxY - minY) : 0;
            int requiredMinorAxisExtent = GetRequiredMinorAxisExtent(floorIndex);
            if (minorAxisExtent < requiredMinorAxisExtent)
            {
                result.failures.Add($"Graph extents too narrow on minor axis ({minorAxisExtent} < {requiredMinorAxisExtent}).");
            }

            int requiredEdgeCount = GetRequiredEdgeCount(floorIndex, graph.nodes.Count);
            if (graph.edges.Count < requiredEdgeCount)
            {
                result.failures.Add($"Edge count {graph.edges.Count} below required {requiredEdgeCount}.");
            }
            else if (floorIndex <= 3 && graph.edges.Count < graph.nodes.Count)
            {
                result.warnings.Add("Layout has no loop edges; loops are preferred on early floors.");
            }

            DungeonNode landmark = FindFirstNodeByKind(graph, DungeonNodeKind.Landmark);
            DungeonNode secret = FindFirstNodeByKind(graph, DungeonNodeKind.Secret);
            int entryToDown = graph.GetGraphDistance(graph.entryHubNodeId, graph.transitDownNodeId);
            int upToDown = graph.GetGraphDistance(graph.transitUpNodeId, graph.transitDownNodeId);

            int requiredEntryToDownDistance = GetRequiredEntryToDownDistance(floorIndex);
            if (entryToDown < requiredEntryToDownDistance)
            {
                result.failures.Add($"Transit down is too close to entry ({entryToDown} < {requiredEntryToDownDistance}).");
            }

            int requiredUpToDownDistance = GetRequiredUpToDownDistance(floorIndex);
            if (upToDown < requiredUpToDownDistance)
            {
                result.failures.Add($"Transit up is too close to transit down ({upToDown} < {requiredUpToDownDistance}).");
            }

            if (landmark != null && graph.GetGraphDistance(landmark.nodeId, graph.transitDownNodeId) < GetRequiredLandmarkToDownDistance(floorIndex))
            {
                result.failures.Add($"Landmark room {landmark.nodeId} is too close to stairs down.");
            }

            if (secret != null)
            {
                if (graph.GetGraphDistance(secret.nodeId, graph.transitDownNodeId) < GetRequiredSecretToDownDistance(floorIndex))
                {
                    result.failures.Add($"Secret room {secret.nodeId} is too close to stairs down.");
                }

                if (landmark != null && graph.GetGraphDistance(secret.nodeId, landmark.nodeId) < GetRequiredSecretToLandmarkDistance(floorIndex))
                {
                    result.failures.Add($"Secret room {secret.nodeId} is too close to landmark room {landmark.nodeId}.");
                }
            }

            return result;
        }

        private static bool IsBetterAttempt(GraphValidationAttemptResult candidate, GraphValidationAttemptResult currentBest)
        {
            if (candidate == null)
            {
                return false;
            }

            if (currentBest == null)
            {
                return true;
            }

            int failureCompare = candidate.failures.Count.CompareTo(currentBest.failures.Count);
            if (failureCompare != 0)
            {
                return failureCompare < 0;
            }

            int warningCompare = candidate.warnings.Count.CompareTo(currentBest.warnings.Count);
            if (warningCompare != 0)
            {
                return warningCompare < 0;
            }

            int candidateNodeCount = candidate.graph != null ? candidate.graph.nodes.Count : 0;
            int currentNodeCount = currentBest.graph != null ? currentBest.graph.nodes.Count : 0;
            int nodeCompare = candidateNodeCount.CompareTo(currentNodeCount);
            if (nodeCompare != 0)
            {
                return nodeCompare > 0;
            }

            int candidateEdgeCount = candidate.graph != null ? candidate.graph.edges.Count : 0;
            int currentEdgeCount = currentBest.graph != null ? currentBest.graph.edges.Count : 0;
            int edgeCompare = candidateEdgeCount.CompareTo(currentEdgeCount);
            if (edgeCompare != 0)
            {
                return edgeCompare > 0;
            }

            return candidate.attemptSeed < currentBest.attemptSeed;
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
            if (floorIndex <= 1)
            {
                return random.Next(10, 15);
            }

            if (floorIndex <= 3)
            {
                return random.Next(12, 17);
            }

            if (floorIndex <= 8)
            {
                return random.Next(16, 23);
            }

            if (floorIndex <= 15)
            {
                return random.Next(22, 31);
            }

            int baseCount = 38 + Mathf.Min(12, floorIndex - 15);
            return random.Next(baseCount, baseCount + 10);
        }

        private static int GetMinimumRoomCount(int floorIndex)
        {
            if (floorIndex <= 1)
            {
                return 10;
            }

            if (floorIndex <= 3)
            {
                return 12;
            }

            if (floorIndex <= 8)
            {
                return 16;
            }

            if (floorIndex <= 15)
            {
                return 22;
            }

            return 38;
        }

        private static int GetRequiredEntryDegree(int floorIndex)
        {
            return floorIndex >= 4 ? 3 : 2;
        }

        private static int GetPreferredEntryDegree(int floorIndex)
        {
            return floorIndex <= 3 ? 3 : GetRequiredEntryDegree(floorIndex);
        }

        private static int GetRequiredCoveredSectors(int floorIndex)
        {
            if (floorIndex <= 1)
            {
                return 2;
            }

            return floorIndex <= 3 ? 3 : 4;
        }

        private static int GetPreferredCoveredSectors(int floorIndex)
        {
            if (floorIndex <= 1)
            {
                return 3;
            }

            return GetRequiredCoveredSectors(floorIndex);
        }

        private static int GetRequiredMinorAxisExtent(int floorIndex)
        {
            return floorIndex >= 4 ? 4 : 3;
        }

        private static int GetRequiredEdgeCount(int floorIndex, int nodeCount)
        {
            return floorIndex >= 4 ? nodeCount : Mathf.Max(0, nodeCount - 1);
        }

        private static int GetRequiredEntryToDownDistance(int floorIndex)
        {
            return floorIndex <= 3 ? 4 : Mathf.Clamp(5 + floorIndex / 4, 5, 12);
        }

        private static int GetRequiredUpToDownDistance(int floorIndex)
        {
            return floorIndex <= 3 ? 3 : 4;
        }

        private static int GetRequiredLandmarkToDownDistance(int floorIndex)
        {
            return floorIndex <= 1 ? 2 : 3;
        }

        private static int GetRequiredSecretToDownDistance(int floorIndex)
        {
            return floorIndex <= 1 ? 2 : 3;
        }

        private static int GetRequiredSecretToLandmarkDistance(int floorIndex)
        {
            return floorIndex <= 3 ? 2 : 3;
        }

        private static int GetMaxRadius(int floorIndex, int targetRoomCount)
        {
            return Mathf.Clamp(3 + floorIndex / 3 + targetRoomCount / 18, 5, 14);
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

        private static Vector2Int ClampToCardinal(Vector2Int delta)
        {
            delta.x = Mathf.Clamp(delta.x, -1, 1);
            delta.y = Mathf.Clamp(delta.y, -1, 1);
            return delta;
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
