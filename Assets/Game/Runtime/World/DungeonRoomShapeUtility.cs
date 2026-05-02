using System;
using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.World
{
    public static class DungeonRoomShapeUtility
    {
        private const float CompoundConnectorWidth = 22f;

        private static readonly DungeonRoomTemplateKind[] GrandObjectiveTemplates =
        {
            DungeonRoomTemplateKind.CrossChamberSafe,
            DungeonRoomTemplateKind.OctagonChamberSafe,
            DungeonRoomTemplateKind.PillarHallSafe,
            DungeonRoomTemplateKind.BroadRectangle
        };

        private static readonly DungeonRoomTemplateKind[] BranchTemplates =
        {
            DungeonRoomTemplateKind.AlcoveRoomSafe,
            DungeonRoomTemplateKind.LChamberSafe,
            DungeonRoomTemplateKind.WideBendSafe,
            DungeonRoomTemplateKind.ForkRoomSafe,
            DungeonRoomTemplateKind.BroadRectangle
        };

        private static readonly DungeonRoomTemplateKind[] MainPathTemplates =
        {
            DungeonRoomTemplateKind.BroadRectangle,
            DungeonRoomTemplateKind.LongGallery,
            DungeonRoomTemplateKind.PillarHallSafe
        };

        public static float GetCompoundConnectorWidth(float normalWidth)
        {
            return Mathf.Max(normalWidth, CompoundConnectorWidth);
        }

        public static void ApplyTemplateShapePlan(DungeonLayoutGraph graph, DungeonLabyrinthObjectivePlan plan, int floorIndex, int seed)
        {
            if (graph == null)
            {
                return;
            }

            plan ??= DungeonLabyrinthObjectiveUtility.BuildObjectivePlan(graph, floorIndex);
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                if (!CanReceiveShapeTemplate(node))
                {
                    continue;
                }

                DungeonExitMask requiredExits = GetRequiredExitMask(graph, node);
                DungeonRoomTemplateKind[] candidates = GetPreferredTemplates(graph, plan, node, floorIndex);
                if (candidates.Length == 0)
                {
                    continue;
                }

                int offset = PositiveHash(seed, floorIndex, node.nodeId) % candidates.Length;
                for (int attempt = 0; attempt < candidates.Length; attempt++)
                {
                    DungeonRoomTemplateKind candidate = candidates[(attempt + offset) % candidates.Length];
                    List<int> rotations = DungeonRoomTemplateLibrary.GetValidRotations(candidate, requiredExits);
                    if (rotations.Count == 0)
                    {
                        continue;
                    }

                    node.roomTemplate = candidate;
                    node.rotationQuarterTurns = rotations[PositiveHash(seed, (int)candidate, node.nodeId) % rotations.Count];
                    break;
                }
            }
        }

        public static void BuildCompoundPlan(DungeonBuildResult build)
        {
            if (build == null || build.graph == null)
            {
                return;
            }

            build.compoundRooms.Clear();
            int cap = GetMergeCap(build.floorIndex);
            if (cap <= 0)
            {
                return;
            }

            List<DungeonEdge> candidates = new List<DungeonEdge>(build.graph.edges);
            candidates.Sort((left, right) => ScoreMergeEdge(build, right).CompareTo(ScoreMergeEdge(build, left)));
            HashSet<string> usedRooms = new HashSet<string>();

            for (int i = 0; i < candidates.Count && build.compoundRooms.Count < cap; i++)
            {
                DungeonEdge edge = candidates[i];
                if (usedRooms.Contains(edge.a) || usedRooms.Contains(edge.b))
                {
                    continue;
                }

                string rejection = GetMergeRejectionReason(build, edge.a, edge.b);
                if (!string.IsNullOrWhiteSpace(rejection))
                {
                    continue;
                }

                string edgeKey = DungeonBuildResult.GetEdgeKey(edge.a, edge.b);
                DungeonRoomCompoundRecord compound = new DungeonRoomCompoundRecord
                {
                    compoundRoomId = $"compound_{build.floorIndex}_{build.compoundRooms.Count + 1}",
                    connectorEdgeKey = edgeKey,
                    compoundShapeClass = "WideConnectorCompound",
                    compoundScaleClass = build.floorIndex >= 8 ? "Grand" : "Large",
                    isLandmarkCompound = IsLandmarkOrObjective(build, edge.a) || IsLandmarkOrObjective(build, edge.b),
                    mergeReason = "Safe ordinary rooms joined by widened source-level connector.",
                    applied = true
                };
                compound.sourceRoomIds.Add(edge.a);
                compound.sourceRoomIds.Add(edge.b);
                build.compoundRooms.Add(compound);
                usedRooms.Add(edge.a);
                usedRooms.Add(edge.b);
            }
        }

        public static bool TryGetCompoundForEdge(DungeonBuildResult build, string edgeKey, out DungeonRoomCompoundRecord compound)
        {
            compound = null;
            if (build == null || string.IsNullOrWhiteSpace(edgeKey))
            {
                return false;
            }

            for (int i = 0; i < build.compoundRooms.Count; i++)
            {
                DungeonRoomCompoundRecord candidate = build.compoundRooms[i];
                if (candidate.applied && string.Equals(candidate.connectorEdgeKey, edgeKey, StringComparison.Ordinal))
                {
                    compound = candidate;
                    return true;
                }
            }

            return false;
        }

        public static void ApplyRoomShapeMetadata(DungeonRoomBuildRecord room, DungeonNode node)
        {
            if (room == null || node == null)
            {
                return;
            }

            room.shapeClass = GetShapeClass(node.roomTemplate);
            room.isExpandedRoom = DungeonRoomTemplateLibrary.GetSizeTier(node.roomTemplate) == DungeonRoomSizeTier.Large ||
                                  DungeonRoomTemplateLibrary.GetSizeTier(node.roomTemplate) == DungeonRoomSizeTier.Grand;
        }

        public static void ApplyPostBuildCompoundMetadata(DungeonBuildResult build)
        {
            if (build == null)
            {
                return;
            }

            for (int i = 0; i < build.compoundRooms.Count; i++)
            {
                DungeonRoomCompoundRecord compound = build.compoundRooms[i];
                Bounds bounds = new Bounds();
                bool hasBounds = false;
                for (int roomIndex = 0; roomIndex < compound.sourceRoomIds.Count; roomIndex++)
                {
                    DungeonRoomBuildRecord room = build.FindRoom(compound.sourceRoomIds[roomIndex]);
                    if (room == null)
                    {
                        continue;
                    }

                    room.isMergedRoom = true;
                    room.compoundRoomId = compound.compoundRoomId;
                    room.sourceCompoundId = compound.compoundRoomId;
                    if (!hasBounds)
                    {
                        bounds = room.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(room.bounds);
                    }
                }

                List<DungeonCorridorBuildRecord> connectorSegments = build.GetCorridorsForEdge(compound.connectorEdgeKey);
                for (int segmentIndex = 0; segmentIndex < connectorSegments.Count; segmentIndex++)
                {
                    DungeonCorridorBuildRecord corridor = connectorSegments[segmentIndex];
                    corridor.isCompoundConnector = true;
                    corridor.compoundRoomId = compound.compoundRoomId;
                    if (!hasBounds)
                    {
                        bounds = corridor.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(corridor.bounds);
                    }
                }

                compound.compoundBounds = hasBounds ? bounds : compound.compoundBounds;
                compound.preservedDoorOpeningCount = CountDoorOpeningsForEdge(build, compound.connectorEdgeKey);
                compound.removedInteriorWallCount = 0;
            }
        }

        public static bool IsIrregularTemplate(DungeonRoomTemplateKind template)
        {
            return template == DungeonRoomTemplateKind.LChamberSafe ||
                   template == DungeonRoomTemplateKind.TChamberSafe ||
                   template == DungeonRoomTemplateKind.CrossChamberSafe ||
                   template == DungeonRoomTemplateKind.OctagonChamberSafe ||
                   template == DungeonRoomTemplateKind.PillarHallSafe ||
                   template == DungeonRoomTemplateKind.AlcoveRoomSafe ||
                   template == DungeonRoomTemplateKind.WideBendSafe ||
                   template == DungeonRoomTemplateKind.ForkRoomSafe;
        }

        public static string GetShapeClass(DungeonRoomTemplateKind template)
        {
            return template switch
            {
                DungeonRoomTemplateKind.BroadRectangle => "BroadRectangle",
                DungeonRoomTemplateKind.LongGallery => "LongGallery",
                DungeonRoomTemplateKind.LChamberSafe => "LChamberSafe",
                DungeonRoomTemplateKind.TChamberSafe => "TChamberSafe",
                DungeonRoomTemplateKind.CrossChamberSafe => "CrossChamberSafe",
                DungeonRoomTemplateKind.OctagonChamberSafe => "OctagonChamberSafe",
                DungeonRoomTemplateKind.PillarHallSafe => "PillarHallSafe",
                DungeonRoomTemplateKind.AlcoveRoomSafe => "AlcoveRoomSafe",
                DungeonRoomTemplateKind.WideBendSafe => "WideBendSafe",
                DungeonRoomTemplateKind.ForkRoomSafe => "ForkRoomSafe",
                _ => "SquareChamber"
            };
        }

        private static DungeonRoomTemplateKind[] GetPreferredTemplates(
            DungeonLayoutGraph graph,
            DungeonLabyrinthObjectivePlan plan,
            DungeonNode node,
            int floorIndex)
        {
            bool objectiveCritical = IsObjectiveCritical(plan, node.nodeId) || node.nodeKind == DungeonNodeKind.Landmark;
            if (objectiveCritical)
            {
                return GrandObjectiveTemplates;
            }

            if (floorIndex <= 1)
            {
                return graph.GetDegree(node.nodeId) <= 1
                    ? new[] { DungeonRoomTemplateKind.BroadRectangle }
                    : Array.Empty<DungeonRoomTemplateKind>();
            }

            if (graph.GetDegree(node.nodeId) <= 1 || !IsMainPath(plan, node.nodeId))
            {
                return floorIndex >= 4 ? BranchTemplates : new[] { DungeonRoomTemplateKind.BroadRectangle, DungeonRoomTemplateKind.AlcoveRoomSafe };
            }

            return floorIndex >= 8 ? MainPathTemplates : Array.Empty<DungeonRoomTemplateKind>();
        }

        private static bool CanReceiveShapeTemplate(DungeonNode node)
        {
            if (node == null)
            {
                return false;
            }

            return node.nodeKind == DungeonNodeKind.Ordinary || node.nodeKind == DungeonNodeKind.Landmark;
        }

        private static int GetMergeCap(int floorIndex)
        {
            if (floorIndex <= 1)
            {
                return 0;
            }

            if (floorIndex <= 3)
            {
                return 1;
            }

            if (floorIndex <= 7)
            {
                return 2;
            }

            return 3;
        }

        private static float ScoreMergeEdge(DungeonBuildResult build, DungeonEdge edge)
        {
            DungeonNode a = build.graph.GetNode(edge.a);
            DungeonNode b = build.graph.GetNode(edge.b);
            float score = 0f;
            score += IsSideRoute(build.labyrinthObjectivePlan, edge.a) ? 12f : 0f;
            score += IsSideRoute(build.labyrinthObjectivePlan, edge.b) ? 12f : 0f;
            score += IsLandmarkOrObjective(build, edge.a) ? 4f : 0f;
            score += IsLandmarkOrObjective(build, edge.b) ? 4f : 0f;
            score -= a != null ? build.graph.GetDegree(a.nodeId) : 0;
            score -= b != null ? build.graph.GetDegree(b.nodeId) : 0;
            return score;
        }

        private static string GetMergeRejectionReason(DungeonBuildResult build, string aId, string bId)
        {
            DungeonNode a = build.graph.GetNode(aId);
            DungeonNode b = build.graph.GetNode(bId);
            if (a == null || b == null)
            {
                return "Missing source node.";
            }

            if (IsProtectedNode(a) || IsProtectedNode(b))
            {
                return "Protected rooms cannot be physically merged.";
            }

            if (IsObjectiveCritical(build.labyrinthObjectivePlan, aId) || IsObjectiveCritical(build.labyrinthObjectivePlan, bId))
            {
                return "Objective, boss, and exit rooms are shaped but not physically merged in this gate.";
            }

            if (a.nodeKind != DungeonNodeKind.Ordinary || b.nodeKind != DungeonNodeKind.Ordinary)
            {
                return "Only ordinary rooms can use first-pass physical compound connectors.";
            }

            if (!AreAdjacentGridRooms(a, b))
            {
                return "Rooms are not adjacent in graph grid space.";
            }

            if (!build.graph.HasPath(build.entryNodeId, build.transitDownNodeId))
            {
                return "Reachability could not be proven.";
            }

            return string.Empty;
        }

        private static bool IsProtectedNode(DungeonNode node)
        {
            return DungeonLabyrinthObjectiveUtility.IsObjectiveProtectedRoom(node);
        }

        private static bool AreAdjacentGridRooms(DungeonNode a, DungeonNode b)
        {
            Vector2Int delta = a.gridPosition - b.gridPosition;
            return Mathf.Abs(delta.x) + Mathf.Abs(delta.y) == 1;
        }

        private static bool IsObjectiveCritical(DungeonLabyrinthObjectivePlan plan, string nodeId)
        {
            if (plan == null || string.IsNullOrWhiteSpace(nodeId))
            {
                return false;
            }

            return string.Equals(nodeId, plan.objectiveRoomId, StringComparison.Ordinal) ||
                   string.Equals(nodeId, plan.bossApproachRoomId, StringComparison.Ordinal) ||
                   string.Equals(nodeId, plan.bossRoomId, StringComparison.Ordinal) ||
                   string.Equals(nodeId, plan.exitStairsRoomId, StringComparison.Ordinal);
        }

        private static bool IsLandmarkOrObjective(DungeonBuildResult build, string nodeId)
        {
            DungeonNode node = build.graph.GetNode(nodeId);
            return node != null && node.nodeKind == DungeonNodeKind.Landmark || IsObjectiveCritical(build.labyrinthObjectivePlan, nodeId);
        }

        private static bool IsSideRoute(DungeonLabyrinthObjectivePlan plan, string nodeId)
        {
            return plan == null || !plan.mainPathRoomIds.Contains(nodeId);
        }

        private static bool IsMainPath(DungeonLabyrinthObjectivePlan plan, string nodeId)
        {
            return plan != null && plan.mainPathRoomIds.Contains(nodeId);
        }

        private static int CountDoorOpeningsForEdge(DungeonBuildResult build, string edgeKey)
        {
            int count = 0;
            for (int i = 0; i < build.doorOpenings.Count; i++)
            {
                if (string.Equals(build.doorOpenings[i].edgeKey, edgeKey, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
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

        private static int PositiveHash(int seed, int salt, string value)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + seed;
                hash = hash * 31 + salt;
                if (!string.IsNullOrEmpty(value))
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        hash = hash * 31 + value[i];
                    }
                }

                return hash == int.MinValue ? int.MaxValue : Mathf.Abs(hash);
            }
        }
    }
}
