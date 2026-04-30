using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.World
{
    public static class DungeonLayoutQualityUtility
    {
        private const float LongCorridorLength = 54f;
        private const float MinimumLandmarkArea = 900f;

        public static DungeonLayoutQualityReport Analyze(DungeonBuildResult build)
        {
            DungeonLayoutQualityReport report = new DungeonLayoutQualityReport();
            if (build == null)
            {
                return report;
            }

            ClassifyRooms(build);
            SelectLandmarkRooms(build);
            BuildMergeCandidates(build);
            PopulateRoomMetrics(build, report);
            PopulateCorridorMetrics(build, report);
            PopulateSpecialRoomMetrics(build, report);
            build.layoutQualityReport = report;
            return report;
        }

        public static bool IsProtectedRoom(DungeonRoomBuildRecord room)
        {
            if (room == null)
            {
                return true;
            }

            return room.isProtected ||
                   room.roomType == DungeonNodeKind.EntryHub ||
                   room.roomType == DungeonNodeKind.TransitUp ||
                   room.roomType == DungeonNodeKind.TransitDown ||
                   room.roomType == DungeonNodeKind.Secret ||
                   room.roomRole == DungeonRoomRole.Start ||
                   room.roomRole == DungeonRoomRole.Return ||
                   room.roomRole == DungeonRoomRole.Exit ||
                   room.roomRole == DungeonRoomRole.Secret ||
                   room.roomRole == DungeonRoomRole.Safe ||
                   string.Equals(room.purposeId, "white_sanctuary", System.StringComparison.Ordinal);
        }

        public static bool IsSpecialRoom(DungeonRoomBuildRecord room)
        {
            if (room == null)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(room.purposeId) ||
                   room.roomType == DungeonNodeKind.Landmark ||
                   room.roomType == DungeonNodeKind.Secret ||
                   room.roomRole == DungeonRoomRole.Treasure ||
                   room.roomRole == DungeonRoomRole.Shrine ||
                   room.roomRole == DungeonRoomRole.Armory ||
                   room.roomRole == DungeonRoomRole.Bounty ||
                   room.roomRole == DungeonRoomRole.Elite ||
                   room.roomRole == DungeonRoomRole.MiniBoss ||
                   room.roomRole == DungeonRoomRole.Boss ||
                   room.roomRole == DungeonRoomRole.Scout;
        }

        private static void ClassifyRooms(DungeonBuildResult build)
        {
            HashSet<string> mainPath = BuildShortestPathIds(
                build.graph,
                string.IsNullOrWhiteSpace(build.transitUpNodeId) ? build.entryNodeId : build.transitUpNodeId,
                build.transitDownNodeId);
            if (mainPath.Count == 0)
            {
                mainPath = BuildShortestPathIds(build.graph, build.entryNodeId, build.transitDownNodeId);
            }

            string approachRoomId = FindBossApproachRoomId(build.graph, mainPath, build.transitDownNodeId);
            for (int i = 0; i < build.rooms.Count; i++)
            {
                DungeonRoomBuildRecord room = build.rooms[i];
                int degree = build.graph != null ? build.graph.GetDegree(room.nodeId) : 0;
                room.isProtected = IsProtectedRoom(room);
                room.isMainPath = mainPath.Contains(room.nodeId);
                room.isDeadEnd = !room.isProtected && !room.isMainPath && degree <= 1;
                room.isBranch = !room.isProtected && !room.isMainPath && !room.isDeadEnd;
                room.isFutureBossApproach = !room.isProtected && string.Equals(room.nodeId, approachRoomId, System.StringComparison.Ordinal);

                if (room.isProtected)
                {
                    room.layoutRole = DungeonRoomRole.Protected;
                }
                else if (room.isFutureBossApproach)
                {
                    room.layoutRole = DungeonRoomRole.FutureBossApproach;
                }
                else if (room.isDeadEnd)
                {
                    room.layoutRole = DungeonRoomRole.DeadEnd;
                }
                else if (room.isBranch)
                {
                    room.layoutRole = DungeonRoomRole.Branch;
                }
                else if (room.isMainPath)
                {
                    room.layoutRole = DungeonRoomRole.MainPath;
                }
                else
                {
                    room.layoutRole = DungeonRoomRole.None;
                }
            }
        }

        private static void SelectLandmarkRooms(DungeonBuildResult build)
        {
            int target = GetLandmarkTarget(build.floorIndex, build.rooms.Count);
            List<DungeonRoomBuildRecord> candidates = new List<DungeonRoomBuildRecord>();
            for (int i = 0; i < build.rooms.Count; i++)
            {
                DungeonRoomBuildRecord room = build.rooms[i];
                if (room.isProtected || room.footprintArea < MinimumLandmarkArea)
                {
                    continue;
                }

                candidates.Add(room);
            }

            candidates.Sort((left, right) =>
            {
                int typeCompare = GetLandmarkScore(right).CompareTo(GetLandmarkScore(left));
                if (typeCompare != 0)
                {
                    return typeCompare;
                }

                int areaCompare = right.footprintArea.CompareTo(left.footprintArea);
                return areaCompare != 0 ? areaCompare : string.CompareOrdinal(left.nodeId, right.nodeId);
            });

            for (int i = 0; i < build.rooms.Count; i++)
            {
                build.rooms[i].isLandmarkRoom = build.rooms[i].roomType == DungeonNodeKind.Landmark;
                if (build.rooms[i].isLandmarkRoom && !build.rooms[i].isProtected)
                {
                    build.rooms[i].layoutRole = DungeonRoomRole.Landmark;
                }
            }

            for (int i = 0; i < candidates.Count && CountLandmarks(build) < target; i++)
            {
                candidates[i].isLandmarkRoom = true;
                candidates[i].layoutRole = DungeonRoomRole.Landmark;
            }
        }

        private static void BuildMergeCandidates(DungeonBuildResult build)
        {
            build.mergeCandidates.Clear();
            if (build.graph == null || build.floorIndex <= 3)
            {
                return;
            }

            int safeCap = build.floorIndex < 10 ? 2 : 4;
            for (int i = 0; i < build.graphEdges.Count; i++)
            {
                DungeonGraphEdgeRecord edge = build.graphEdges[i];
                DungeonRoomBuildRecord a = build.FindRoom(edge.a);
                DungeonRoomBuildRecord b = build.FindRoom(edge.b);
                if (a == null || b == null)
                {
                    continue;
                }

                DungeonRoomMergeCandidateRecord candidate = CreateMergeCandidate(build, a, b);
                build.mergeCandidates.Add(candidate);
                if (CountSafeMergeCandidates(build) >= safeCap)
                {
                    break;
                }
            }
        }

        private static DungeonRoomMergeCandidateRecord CreateMergeCandidate(DungeonBuildResult build, DungeonRoomBuildRecord a, DungeonRoomBuildRecord b)
        {
            Bounds combined = a.bounds;
            combined.Encapsulate(b.bounds);
            DungeonRoomMergeCandidateRecord candidate = new DungeonRoomMergeCandidateRecord
            {
                roomA = a.nodeId,
                roomB = b.nodeId,
                combinedBounds = combined,
                floorDepth = build.floorIndex,
                reason = "Adjacent graph rooms with compatible metadata."
            };

            if (IsProtectedRoom(a) || IsProtectedRoom(b))
            {
                candidate.rejectionReason = "Protected rooms cannot be merged.";
                return candidate;
            }

            if (a.roomType != DungeonNodeKind.Ordinary || b.roomType != DungeonNodeKind.Ordinary)
            {
                candidate.rejectionReason = "Only ordinary rooms are merge candidates in this foundation gate.";
                return candidate;
            }

            if (build.graph == null || !build.graph.HasPath(build.entryNodeId, build.transitDownNodeId))
            {
                candidate.rejectionReason = "Graph reachability could not be proven.";
                return candidate;
            }

            candidate.isSafeToApply = true;
            return candidate;
        }

        private static void PopulateRoomMetrics(DungeonBuildResult build, DungeonLayoutQualityReport report)
        {
            report.roomCount = build.rooms.Count;
            report.smallestRoomArea = build.rooms.Count > 0 ? float.MaxValue : 0f;
            float areaTotal = 0f;
            for (int i = 0; i < build.rooms.Count; i++)
            {
                DungeonRoomBuildRecord room = build.rooms[i];
                areaTotal += room.footprintArea;
                report.smallestRoomArea = Mathf.Min(report.smallestRoomArea, room.footprintArea);
                report.largestRoomArea = Mathf.Max(report.largestRoomArea, room.footprintArea);
                report.mainPathRoomCount += room.isMainPath ? 1 : 0;
                report.branchRoomCount += room.isBranch ? 1 : 0;
                report.deadEndRoomCount += room.isDeadEnd ? 1 : 0;
                report.landmarkRoomCount += room.isLandmarkRoom ? 1 : 0;
                report.protectedRoomCount += room.isProtected ? 1 : 0;
            }

            report.averageRoomArea = build.rooms.Count > 0 ? areaTotal / build.rooms.Count : 0f;
            report.mergeCandidateCount = CountSafeMergeCandidates(build);
            if (report.branchRoomCount == 0 && build.rooms.Count > 8)
            {
                report.layoutWarnings.Add("No branch rooms classified on a multi-room layout.");
            }
        }

        private static void PopulateCorridorMetrics(DungeonBuildResult build, DungeonLayoutQualityReport report)
        {
            report.corridorCount = build.corridors.Count;
            report.corridorToRoomRatio = build.rooms.Count > 0 ? build.corridors.Count / (float)build.rooms.Count : 0f;
            float lengthTotal = 0f;
            for (int i = 0; i < build.corridors.Count; i++)
            {
                float length = build.corridors[i].length;
                lengthTotal += length;
                report.longestCorridorLength = Mathf.Max(report.longestCorridorLength, length);
                if (length > LongCorridorLength)
                {
                    report.veryLongCorridorCount++;
                }
            }

            report.averageCorridorLength = build.corridors.Count > 0 ? lengthTotal / build.corridors.Count : 0f;
            report.longCorridorWarnings = report.veryLongCorridorCount;
            report.straightCorridorChainCount = CountStraightChains(build.graph);
        }

        private static void PopulateSpecialRoomMetrics(DungeonBuildResult build, DungeonLayoutQualityReport report)
        {
            Dictionary<string, int> purposeCounts = new Dictionary<string, int>();
            int mainPathSpecials = 0;
            int branchSpecials = 0;
            for (int i = 0; i < build.rooms.Count; i++)
            {
                DungeonRoomBuildRecord room = build.rooms[i];
                if (!IsSpecialRoom(room))
                {
                    continue;
                }

                report.specialRoomCount++;
                mainPathSpecials += room.isMainPath ? 1 : 0;
                branchSpecials += room.isBranch || room.isDeadEnd ? 1 : 0;
                if (!string.IsNullOrWhiteSpace(room.purposeId))
                {
                    purposeCounts.TryGetValue(room.purposeId, out int count);
                    purposeCounts[room.purposeId] = count + 1;
                }
            }

            if (build.floorIndex < 10)
            {
                foreach (KeyValuePair<string, int> pair in purposeCounts)
                {
                    if (pair.Value > 1)
                    {
                        report.repeatedSpecialRoomWarnings++;
                        report.layoutWarnings.Add($"Repeated early special purpose: {pair.Key} x{pair.Value}.");
                    }
                }
            }

            if (mainPathSpecials > Mathf.Max(1, report.mainPathRoomCount / 3))
            {
                report.layoutWarnings.Add("Main path has a high special-room density.");
            }

            if (report.specialRoomCount > 1 && branchSpecials == 0)
            {
                report.layoutWarnings.Add("Special rooms are not using branches or dead ends.");
            }
        }

        private static HashSet<string> BuildShortestPathIds(DungeonLayoutGraph graph, string startId, string goalId)
        {
            HashSet<string> result = new HashSet<string>();
            if (graph == null || string.IsNullOrWhiteSpace(startId) || string.IsNullOrWhiteSpace(goalId))
            {
                return result;
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
                return result;
            }

            string cursor = goalId;
            while (!string.IsNullOrWhiteSpace(cursor))
            {
                result.Add(cursor);
                cursor = previous.TryGetValue(cursor, out string parent) ? parent : string.Empty;
            }

            return result;
        }

        private static string FindBossApproachRoomId(DungeonLayoutGraph graph, HashSet<string> mainPath, string transitDownId)
        {
            if (graph == null || mainPath.Count == 0 || string.IsNullOrWhiteSpace(transitDownId))
            {
                return string.Empty;
            }

            List<DungeonNode> neighbors = graph.GetNeighbors(transitDownId);
            for (int i = 0; i < neighbors.Count; i++)
            {
                if (mainPath.Contains(neighbors[i].nodeId) && neighbors[i].nodeKind == DungeonNodeKind.Ordinary)
                {
                    return neighbors[i].nodeId;
                }
            }

            return string.Empty;
        }

        private static int GetLandmarkTarget(int floorIndex, int roomCount)
        {
            if (roomCount < 10)
            {
                return 1;
            }

            if (floorIndex >= 10 && roomCount >= 18)
            {
                return 3;
            }

            return floorIndex >= 4 && roomCount >= 14 ? 2 : 1;
        }

        private static int GetLandmarkScore(DungeonRoomBuildRecord room)
        {
            int score = 0;
            score += room.roomType == DungeonNodeKind.Landmark ? 100 : 0;
            score += room.isDeadEnd ? 40 : 0;
            score += room.isBranch ? 25 : 0;
            score += room.isFutureBossApproach ? 10 : 0;
            return score;
        }

        private static int CountLandmarks(DungeonBuildResult build)
        {
            int count = 0;
            for (int i = 0; i < build.rooms.Count; i++)
            {
                count += build.rooms[i].isLandmarkRoom ? 1 : 0;
            }

            return count;
        }

        private static int CountSafeMergeCandidates(DungeonBuildResult build)
        {
            int count = 0;
            for (int i = 0; i < build.mergeCandidates.Count; i++)
            {
                count += build.mergeCandidates[i].isSafeToApply ? 1 : 0;
            }

            return count;
        }

        private static int CountStraightChains(DungeonLayoutGraph graph)
        {
            if (graph == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                DungeonNode node = graph.nodes[i];
                List<DungeonNode> neighbors = graph.GetNeighbors(node.nodeId);
                if (neighbors.Count != 2)
                {
                    continue;
                }

                Vector2Int a = neighbors[0].gridPosition - node.gridPosition;
                Vector2Int b = neighbors[1].gridPosition - node.gridPosition;
                if ((a.x != 0 && b.x != 0) || (a.y != 0 && b.y != 0))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
