using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.World
{
    public static class DungeonLabyrinthObjectiveUtility
    {
        private const float MinimumObjectiveArea = 100f;

        public static DungeonLabyrinthObjectivePlan BuildObjectivePlan(DungeonLayoutGraph graph, int floorIndex)
        {
            DungeonLabyrinthObjectivePlan plan = new DungeonLabyrinthObjectivePlan();
            if (graph == null)
            {
                plan.warnings.Add("Objective plan skipped because graph is missing.");
                return plan;
            }

            plan.entranceRoomId = !string.IsNullOrWhiteSpace(graph.transitUpNodeId)
                ? graph.transitUpNodeId
                : graph.entryHubNodeId;
            plan.exitStairsRoomId = graph.transitDownNodeId ?? string.Empty;
            plan.lockedExitUntilObjectiveComplete = false;
            plan.lockedExitUntilBossDefeated = false;
            plan.nextDepthUnlocked = true;

            List<string> mainPath = BuildShortestPath(graph, plan.entranceRoomId, plan.exitStairsRoomId);
            if (mainPath.Count == 0)
            {
                mainPath = BuildShortestPath(graph, graph.entryHubNodeId, plan.exitStairsRoomId);
            }

            plan.mainPathRoomIds.AddRange(mainPath);
            plan.objectiveRoomId = SelectObjectiveRoom(graph, plan, floorIndex);
            plan.bossRoomId = SelectBossRoomPlaceholder(graph, plan, floorIndex);
            plan.bossApproachRoomId = SelectBossApproachRoom(graph, plan, floorIndex);
            if (string.IsNullOrWhiteSpace(plan.objectiveRoomId))
            {
                plan.objectiveRequired = false;
                plan.warnings.Add("No suitable objective room found; objective requirement disabled for this layout.");
            }

            if (string.IsNullOrWhiteSpace(plan.bossRoomId))
            {
                plan.warnings.Add("No suitable boss placeholder found; using exit metadata only.");
            }

            if (string.IsNullOrWhiteSpace(plan.bossApproachRoomId))
            {
                plan.warnings.Add("No suitable boss approach room found.");
            }

            return plan;
        }

        public static void ApplyObjectivePlan(DungeonBuildResult build)
        {
            if (build == null)
            {
                return;
            }

            build.labyrinthObjectivePlan ??= BuildObjectivePlan(build.graph, build.floorIndex);
            DungeonLabyrinthObjectivePlan plan = build.labyrinthObjectivePlan;
            for (int i = 0; i < build.rooms.Count; i++)
            {
                ApplyObjectivePlanToRoom(build.rooms[i], plan);
            }
        }

        public static void ApplyObjectivePlanToRoom(DungeonRoomBuildRecord room, DungeonLabyrinthObjectivePlan plan)
        {
            if (room == null || plan == null)
            {
                return;
            }

            room.objectivePathIndex = plan.mainPathRoomIds.IndexOf(room.nodeId);
            room.isObjectiveRoom = string.Equals(room.nodeId, plan.objectiveRoomId, System.StringComparison.Ordinal);
            room.isBossApproachRoom = string.Equals(room.nodeId, plan.bossApproachRoomId, System.StringComparison.Ordinal);
            room.isBossRoomPlaceholder = string.Equals(room.nodeId, plan.bossRoomId, System.StringComparison.Ordinal);
            room.isExitStairsRoom = string.Equals(room.nodeId, plan.exitStairsRoomId, System.StringComparison.Ordinal);

            if (room.isObjectiveRoom)
            {
                room.objectiveRole = DungeonRoomRole.Objective;
            }
            else if (room.isBossApproachRoom)
            {
                room.objectiveRole = DungeonRoomRole.BossApproach;
            }
            else if (room.isBossRoomPlaceholder)
            {
                room.objectiveRole = DungeonRoomRole.BossPlaceholder;
            }
            else if (room.isExitStairsRoom)
            {
                room.objectiveRole = DungeonRoomRole.Exit;
            }
            else
            {
                room.objectiveRole = DungeonRoomRole.None;
            }
        }

        public static bool IsReservedObjectiveRoom(string nodeId, DungeonLabyrinthObjectivePlan plan)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || plan == null)
            {
                return false;
            }

            return string.Equals(nodeId, plan.objectiveRoomId, System.StringComparison.Ordinal) ||
                   string.Equals(nodeId, plan.bossApproachRoomId, System.StringComparison.Ordinal) ||
                   string.Equals(nodeId, plan.bossRoomId, System.StringComparison.Ordinal);
        }

        public static bool IsObjectiveProtectedRoom(DungeonNode node)
        {
            if (node == null)
            {
                return true;
            }

            return node.nodeKind == DungeonNodeKind.EntryHub ||
                   node.nodeKind == DungeonNodeKind.TransitUp ||
                   node.nodeKind == DungeonNodeKind.TransitDown ||
                   node.nodeKind == DungeonNodeKind.Secret ||
                   node.roomRole == DungeonRoomRole.Start ||
                   node.roomRole == DungeonRoomRole.Return ||
                   node.roomRole == DungeonRoomRole.Exit ||
                   node.roomRole == DungeonRoomRole.Secret ||
                   node.roomRole == DungeonRoomRole.Safe ||
                   node.roomRole == DungeonRoomRole.Protected;
        }

        public static string SelectObjectiveRoom(DungeonLayoutGraph graph, DungeonLabyrinthObjectivePlan plan, int floorIndex)
        {
            DungeonNode best = null;
            float bestScore = float.MinValue;
            Dictionary<string, int> entryDistances = graph.BuildDistanceMap(string.IsNullOrWhiteSpace(graph.entryHubNodeId) ? plan.entranceRoomId : graph.entryHubNodeId);
            HashSet<string> mainPath = new HashSet<string>(plan.mainPathRoomIds);
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                if (IsObjectiveProtectedRoom(node) || node.nodeId == plan.exitStairsRoomId)
                {
                    continue;
                }

                float area = EstimateTemplateArea(node.roomTemplate);
                if (area < MinimumObjectiveArea)
                {
                    continue;
                }

                int distance = entryDistances.TryGetValue(node.nodeId, out int found) ? found : 0;
                int exitDistance = graph.GetGraphDistance(node.nodeId, plan.exitStairsRoomId);
                float score = distance * 4f;
                score += !mainPath.Contains(node.nodeId) ? 28f : -8f;
                score += graph.GetDegree(node.nodeId) <= 1 ? 14f : 0f;
                score += node.nodeKind == DungeonNodeKind.Landmark ? 16f : 0f;
                score += Mathf.Clamp(area / 100f, 0f, 16f);
                score -= exitDistance <= 1 ? 18f : 0f;
                score -= distance <= 1 ? 20f : 0f;

                if (score > bestScore || (Mathf.Approximately(score, bestScore) && string.CompareOrdinal(node.nodeId, best?.nodeId) < 0))
                {
                    best = node;
                    bestScore = score;
                }
            }

            if (best != null)
            {
                return best.nodeId;
            }

            plan.warnings.Add("Objective room used fallback selection.");
            return SelectFallbackOrdinaryRoom(graph, plan, avoidNodeId: plan.exitStairsRoomId);
        }

        public static string SelectBossRoomPlaceholder(DungeonLayoutGraph graph, DungeonLabyrinthObjectivePlan plan, int floorIndex)
        {
            DungeonNode best = null;
            float bestScore = float.MinValue;
            Dictionary<string, int> entryDistances = graph.BuildDistanceMap(string.IsNullOrWhiteSpace(graph.entryHubNodeId) ? plan.entranceRoomId : graph.entryHubNodeId);
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                if (IsObjectiveProtectedRoom(node) || node.nodeId == plan.objectiveRoomId)
                {
                    continue;
                }

                float area = EstimateTemplateArea(node.roomTemplate);
                if (area < MinimumObjectiveArea)
                {
                    continue;
                }

                int distance = entryDistances.TryGetValue(node.nodeId, out int found) ? found : 0;
                int exitDistance = graph.GetGraphDistance(node.nodeId, plan.exitStairsRoomId);
                float score = distance * 7f;
                score += exitDistance >= 0 ? Mathf.Max(0, 5 - exitDistance) * 5f : 0f;
                score += node.nodeKind == DungeonNodeKind.Landmark ? 16f : 0f;
                score += Mathf.Clamp(area / 100f, 0f, 18f);

                if (score > bestScore || (Mathf.Approximately(score, bestScore) && string.CompareOrdinal(node.nodeId, best?.nodeId) < 0))
                {
                    best = node;
                    bestScore = score;
                }
            }

            if (best != null)
            {
                return best.nodeId;
            }

            plan.warnings.Add("Boss placeholder used fallback selection.");
            return SelectFallbackOrdinaryRoom(graph, plan, avoidNodeId: plan.objectiveRoomId);
        }

        public static string SelectBossApproachRoom(DungeonLayoutGraph graph, DungeonLabyrinthObjectivePlan plan, int floorIndex)
        {
            if (string.IsNullOrWhiteSpace(plan.bossRoomId))
            {
                return string.Empty;
            }

            DungeonNode best = null;
            float bestScore = float.MinValue;
            HashSet<string> mainPath = new HashSet<string>(plan.mainPathRoomIds);
            List<DungeonNode> bossNeighbors = graph.GetNeighbors(plan.bossRoomId);
            for (int i = 0; i < bossNeighbors.Count; i++)
            {
                DungeonNode node = bossNeighbors[i];
                if (IsObjectiveProtectedRoom(node) || node.nodeId == plan.objectiveRoomId || node.nodeId == plan.exitStairsRoomId)
                {
                    continue;
                }

                float score = mainPath.Contains(node.nodeId) ? 20f : 8f;
                score += Mathf.Clamp(EstimateTemplateArea(node.roomTemplate) / 120f, 0f, 14f);
                if (score > bestScore)
                {
                    best = node;
                    bestScore = score;
                }
            }

            if (best != null)
            {
                return best.nodeId;
            }

            for (int i = plan.mainPathRoomIds.Count - 1; i >= 0; i--)
            {
                DungeonNode node = graph.GetNode(plan.mainPathRoomIds[i]);
                if (!IsObjectiveProtectedRoom(node) && node.nodeId != plan.objectiveRoomId && node.nodeId != plan.bossRoomId)
                {
                    return node.nodeId;
                }
            }

            return string.Empty;
        }

        private static string SelectFallbackOrdinaryRoom(DungeonLayoutGraph graph, DungeonLabyrinthObjectivePlan plan, string avoidNodeId)
        {
            DungeonNode best = null;
            int bestDistance = int.MinValue;
            Dictionary<string, int> distances = graph.BuildDistanceMap(graph.entryHubNodeId);
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                if (IsObjectiveProtectedRoom(node) || node.nodeId == avoidNodeId)
                {
                    continue;
                }

                int distance = distances.TryGetValue(node.nodeId, out int found) ? found : 0;
                if (distance > bestDistance)
                {
                    best = node;
                    bestDistance = distance;
                }
            }

            return best != null ? best.nodeId : string.Empty;
        }

        private static List<string> BuildShortestPath(DungeonLayoutGraph graph, string startId, string goalId)
        {
            List<string> path = new List<string>();
            if (graph == null || string.IsNullOrWhiteSpace(startId) || string.IsNullOrWhiteSpace(goalId))
            {
                return path;
            }

            Queue<string> frontier = new Queue<string>();
            Dictionary<string, string> previous = new Dictionary<string, string>();
            frontier.Enqueue(startId);
            previous[startId] = string.Empty;
            while (frontier.Count > 0)
            {
                string current = frontier.Dequeue();
                if (current == goalId)
                {
                    break;
                }

                List<DungeonNode> neighbors = graph.GetNeighbors(current);
                for (int i = 0; i < neighbors.Count; i++)
                {
                    string next = neighbors[i].nodeId;
                    if (previous.ContainsKey(next))
                    {
                        continue;
                    }

                    previous[next] = current;
                    frontier.Enqueue(next);
                }
            }

            if (!previous.ContainsKey(goalId))
            {
                return path;
            }

            string cursor = goalId;
            while (!string.IsNullOrWhiteSpace(cursor))
            {
                path.Add(cursor);
                cursor = previous.TryGetValue(cursor, out string parent) ? parent : string.Empty;
            }

            path.Reverse();
            return path;
        }

        private static float EstimateTemplateArea(DungeonRoomTemplateKind template)
        {
            HashSet<Vector2Int> cells = DungeonRoomTemplateLibrary.GetCells(new DungeonNode { roomTemplate = template });
            return Mathf.Max(1, cells.Count) * 36f;
        }
    }
}
