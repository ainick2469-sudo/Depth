using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class DungeonValidationReport
    {
        public readonly List<DungeonValidationIssue> failures = new List<DungeonValidationIssue>();
        public readonly List<DungeonValidationIssue> warnings = new List<DungeonValidationIssue>();

        public bool IsValid => failures.Count == 0;
        public bool HasWarnings => warnings.Count > 0;

        public void AddFailure(
            DungeonBuildResult buildResult,
            string nodeId,
            DungeonNodeKind roomType,
            DungeonRoomTemplateKind templateKind,
            string reason)
        {
            failures.Add(CreateIssue(buildResult, nodeId, roomType, templateKind, reason, "failed"));
        }

        public void AddWarning(
            DungeonBuildResult buildResult,
            string nodeId,
            DungeonNodeKind roomType,
            DungeonRoomTemplateKind templateKind,
            string reason)
        {
            warnings.Add(CreateIssue(buildResult, nodeId, roomType, templateKind, reason, "warning"));
        }

        public string ToSummaryString(DungeonBuildResult buildResult, int maxReasons = 5)
        {
            string state = IsValid ? "VALID" : "INVALID";
            if (buildResult == null)
            {
                return $"Dungeon build {state} | Failures {failures.Count}";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("Dungeon build ");
            builder.Append(state);
            builder.Append(" | Floor ");
            builder.Append(buildResult.floorIndex);
            builder.Append(" | Seed ");
            builder.Append(buildResult.seed);
            builder.Append(" | Attempt ");
            builder.Append(Mathf.Max(1, buildResult.attemptNumber));
            builder.Append("/");
            builder.Append(Mathf.Max(1, buildResult.attemptCount));
            builder.Append(" | RequestedFallback ");
            builder.Append(buildResult.requestedFallback ? "Yes" : "No");
            builder.Append(" | GeneratorFallback ");
            builder.Append(buildResult.generatorReturnedFallbackGraph ? "Yes" : "No");
            builder.Append(" | Emergency ");
            builder.Append(buildResult.isEmergencyDebugBuild ? "Yes" : "No");
            builder.Append(" | Rooms ");
            builder.Append(buildResult.rooms.Count);
            builder.Append(" | CorridorSegments ");
            builder.Append(buildResult.corridors.Count);
            builder.Append(" | Warnings ");
            builder.Append(warnings.Count);
            builder.Append(" | Failures ");
            builder.Append(failures.Count);

            if (failures.Count > 0)
            {
                builder.Append(" | Reasons ");
                builder.Append(GetGroupedSummary(failures, maxReasons));
            }

            if (warnings.Count > 0)
            {
                builder.Append(" | WarningReasons ");
                builder.Append(GetGroupedSummary(warnings, maxReasons));
            }

            return builder.ToString();
        }

        public void LogFailures(DungeonBuildResult buildResult, int maxReasons = 5)
        {
            Debug.LogError(ToSummaryString(buildResult, maxReasons));
            for (int i = 0; i < warnings.Count; i++)
            {
                Debug.LogWarning(warnings[i].ToLogString());
            }

            for (int i = 0; i < failures.Count; i++)
            {
                Debug.LogError(failures[i].ToLogString());
            }
        }

        private static DungeonValidationIssue CreateIssue(
            DungeonBuildResult buildResult,
            string nodeId,
            DungeonNodeKind roomType,
            DungeonRoomTemplateKind templateKind,
            string reason,
            string severity)
        {
            return new DungeonValidationIssue
            {
                seed = buildResult != null ? buildResult.seed : 0,
                floorIndex = buildResult != null ? buildResult.floorIndex : 0,
                nodeId = nodeId ?? string.Empty,
                roomType = roomType,
                templateKind = templateKind,
                reason = reason ?? "Unknown issue.",
                severity = severity
            };
        }

        private static string GetGroupedSummary(List<DungeonValidationIssue> issues, int maxReasons)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            for (int i = 0; i < issues.Count; i++)
            {
                string reason = string.IsNullOrWhiteSpace(issues[i].reason) ? "Unknown issue." : issues[i].reason;
                if (!counts.TryAdd(reason, 1))
                {
                    counts[reason]++;
                }
            }

            List<KeyValuePair<string, int>> grouped = new List<KeyValuePair<string, int>>(counts);
            grouped.Sort((left, right) =>
            {
                int countComparison = right.Value.CompareTo(left.Value);
                return countComparison != 0 ? countComparison : string.CompareOrdinal(left.Key, right.Key);
            });

            int limit = Mathf.Clamp(maxReasons, 1, grouped.Count);
            List<string> parts = new List<string>(limit);
            for (int i = 0; i < limit; i++)
            {
                KeyValuePair<string, int> entry = grouped[i];
                parts.Add(entry.Value > 1 ? $"{entry.Key} x{entry.Value}" : entry.Key);
            }

            return string.Join(" | ", parts);
        }
    }

    public sealed class DungeonValidationIssue
    {
        public int seed;
        public int floorIndex;
        public string nodeId;
        public DungeonNodeKind roomType;
        public DungeonRoomTemplateKind templateKind;
        public string reason;
        public string severity;

        public string ToLogString()
        {
            return $"Dungeon validation {severity}: seed={seed}, floor={floorIndex}, node={nodeId}, roomType={roomType}, template={templateKind}, reason={reason}";
        }
    }

    public static class DungeonValidator
    {
        private const float IntersectionPadding = 0.1f;
        private const float OpeningWallTolerance = 0.02f;
        private const float SeamTolerance = 0.1f;

        public static DungeonValidationReport Validate(DungeonBuildResult buildResult)
        {
            DungeonValidationReport report = new DungeonValidationReport();
            if (buildResult == null || buildResult.graph == null)
            {
                report.AddFailure(buildResult, string.Empty, DungeonNodeKind.Ordinary, DungeonRoomTemplateKind.SquareChamber, "Build result or graph was null.");
                return report;
            }

            ValidateGraphEdges(buildResult, report);
            ValidateRooms(buildResult, report);
            ValidateOpenings(buildResult, report);
            ValidateReachability(buildResult, report);
            ValidateSpawn(buildResult, report);
            ValidateInteractables(buildResult, report);
            return report;
        }

        private static void ValidateGraphEdges(DungeonBuildResult buildResult, DungeonValidationReport report)
        {
            for (int i = 0; i < buildResult.graphEdges.Count; i++)
            {
                DungeonGraphEdgeRecord edge = buildResult.graphEdges[i];
                List<DungeonCorridorBuildRecord> records = buildResult.GetCorridorsForEdge(edge.edgeKey);
                if (records.Count == 0)
                {
                    report.AddFailure(buildResult, edge.a, ResolveRoomType(buildResult, edge.a), ResolveTemplate(buildResult, edge.a), $"Missing rendered corridor for edge {edge.edgeKey}.");
                    continue;
                }

                DungeonNode aNode = buildResult.graph.GetNode(edge.a);
                DungeonNode bNode = buildResult.graph.GetNode(edge.b);
                if (aNode == null || bNode == null)
                {
                    report.AddFailure(buildResult, edge.a, DungeonNodeKind.Ordinary, DungeonRoomTemplateKind.SquareChamber, $"Missing nodes for edge {edge.edgeKey}.");
                    continue;
                }

                Vector2Int aDirection = ClampToCardinal(bNode.gridPosition - aNode.gridPosition);
                Vector2Int bDirection = -aDirection;
                DungeonDoorOpeningRecord aOpening = buildResult.FindDoorOpening(edge.a, aDirection);
                DungeonDoorOpeningRecord bOpening = buildResult.FindDoorOpening(edge.b, bDirection);
                if (aOpening == null)
                {
                    report.AddFailure(buildResult, edge.a, aNode.nodeKind, aNode.roomTemplate, $"Missing doorway opening for edge {edge.edgeKey}.");
                }

                if (bOpening == null)
                {
                    report.AddFailure(buildResult, edge.b, bNode.nodeKind, bNode.roomTemplate, $"Missing doorway opening for edge {edge.edgeKey}.");
                }

                if (aOpening == null || bOpening == null)
                {
                    continue;
                }

                DungeonCorridorBuildRecord first = records[0];
                DungeonCorridorBuildRecord last = records[records.Count - 1];
                if (!IntersectsExpanded(first.bounds, aOpening.bounds))
                {
                    report.AddFailure(buildResult, edge.a, aNode.nodeKind, aNode.roomTemplate, $"Corridor edge {edge.edgeKey} does not overlap doorway opening at node {edge.a}.");
                }

                if (!IntersectsExpanded(last.bounds, bOpening.bounds))
                {
                    report.AddFailure(buildResult, edge.b, bNode.nodeKind, bNode.roomTemplate, $"Corridor edge {edge.edgeKey} does not overlap doorway opening at node {edge.b}.");
                }

                ValidateOpeningAgainstWalls(buildResult, report, aNode, aOpening);
                ValidateOpeningAgainstWalls(buildResult, report, bNode, bOpening);
                WarnOnCorridorSeamMismatch(buildResult, report, aNode, aOpening, first);
                WarnOnCorridorSeamMismatch(buildResult, report, bNode, bOpening, last);
            }
        }

        private static void ValidateRooms(DungeonBuildResult buildResult, DungeonValidationReport report)
        {
            for (int i = 0; i < buildResult.graph.nodes.Count; i++)
            {
                DungeonNode node = buildResult.graph.nodes[i];
                DungeonRoomBuildRecord room = buildResult.FindRoom(node.nodeId);
                if (room == null)
                {
                    report.AddFailure(buildResult, node.nodeId, node.nodeKind, node.roomTemplate, "Missing rendered room.");
                    continue;
                }

                if (!room.hasFloor)
                {
                    report.AddFailure(buildResult, node.nodeId, node.nodeKind, node.roomTemplate, "Room has no floor.");
                }

                if (room.wallCount <= 0)
                {
                    report.AddFailure(buildResult, node.nodeId, node.nodeKind, node.roomTemplate, "Room has no walls.");
                }

                if (buildResult.graph.GetDegree(node.nodeId) > 0 && room.doorwayCount <= 0)
                {
                    report.AddFailure(buildResult, node.nodeId, node.nodeKind, node.roomTemplate, "Connected room has no valid doorway.");
                }
            }
        }

        private static void ValidateOpenings(DungeonBuildResult buildResult, DungeonValidationReport report)
        {
            for (int i = 0; i < buildResult.reservedZones.Count; i++)
            {
                DungeonReservedZoneRecord zone = buildResult.reservedZones[i];
                if (zone.kind != "Doorway")
                {
                    continue;
                }

                bool hasOpening = false;
                for (int openingIndex = 0; openingIndex < buildResult.doorOpenings.Count; openingIndex++)
                {
                    if (IntersectsExpanded(zone.bounds, buildResult.doorOpenings[openingIndex].bounds))
                    {
                        hasOpening = true;
                        break;
                    }
                }

                if (!hasOpening)
                {
                    report.AddFailure(buildResult, zone.ownerId, ResolveRoomType(buildResult, zone.ownerId), ResolveTemplate(buildResult, zone.ownerId), "Reserved doorway zone does not map to a doorway opening.");
                }
            }
        }

        private static void ValidateReachability(DungeonBuildResult buildResult, DungeonValidationReport report)
        {
            DungeonLayoutGraph graph = buildResult.graph;
            if (!graph.HasPath(buildResult.entryNodeId, buildResult.transitDownNodeId))
            {
                report.AddFailure(buildResult, buildResult.transitDownNodeId, ResolveRoomType(buildResult, buildResult.transitDownNodeId), ResolveTemplate(buildResult, buildResult.transitDownNodeId), "Stairs down are unreachable.");
            }

            if (!graph.HasPath(buildResult.entryNodeId, buildResult.transitUpNodeId))
            {
                report.AddFailure(buildResult, buildResult.transitUpNodeId, ResolveRoomType(buildResult, buildResult.transitUpNodeId), ResolveTemplate(buildResult, buildResult.transitUpNodeId), "Return route is unreachable.");
            }

            if (!string.IsNullOrWhiteSpace(buildResult.landmarkNodeId) && !graph.HasPath(buildResult.entryNodeId, buildResult.landmarkNodeId))
            {
                report.AddFailure(buildResult, buildResult.landmarkNodeId, ResolveRoomType(buildResult, buildResult.landmarkNodeId), ResolveTemplate(buildResult, buildResult.landmarkNodeId), "Landmark room is unreachable.");
            }

            if (!string.IsNullOrWhiteSpace(buildResult.secretNodeId) && !graph.HasPath(buildResult.entryNodeId, buildResult.secretNodeId))
            {
                report.AddFailure(buildResult, buildResult.secretNodeId, ResolveRoomType(buildResult, buildResult.secretNodeId), ResolveTemplate(buildResult, buildResult.secretNodeId), "Secret room is unreachable.");
            }
        }

        private static void ValidateSpawn(DungeonBuildResult buildResult, DungeonValidationReport report)
        {
            DungeonRoomBuildRecord entryRoom = buildResult.FindRoom(buildResult.entryNodeId);
            if (entryRoom == null)
            {
                return;
            }

            if (!ContainsXZ(entryRoom.bounds, buildResult.playerSpawn))
            {
                report.AddFailure(buildResult, buildResult.entryNodeId, entryRoom.roomType, entryRoom.templateKind, "Player spawn is outside the entry room.");
            }

            for (int i = 0; i < buildResult.wallSpans.Count; i++)
            {
                if (ContainsXZ(buildResult.wallSpans[i].bounds, buildResult.playerSpawn))
                {
                    report.AddFailure(buildResult, buildResult.entryNodeId, entryRoom.roomType, entryRoom.templateKind, "Player spawn overlaps wall geometry.");
                    break;
                }
            }
        }

        private static void ValidateInteractables(DungeonBuildResult buildResult, DungeonValidationReport report)
        {
            int requiredReturns = 0;

            for (int i = 0; i < buildResult.interactables.Count; i++)
            {
                DungeonInteractableBuildRecord interactable = buildResult.interactables[i];
                DungeonRoomBuildRecord room = buildResult.FindRoom(interactable.nodeId);
                if (room == null)
                {
                    report.AddFailure(buildResult, interactable.nodeId, ResolveRoomType(buildResult, interactable.nodeId), ResolveTemplate(buildResult, interactable.nodeId), $"Interactable {interactable.interactableType} has no room.");
                    continue;
                }

                if (!ContainsXZ(room.bounds, interactable.position))
                {
                    report.AddFailure(buildResult, interactable.nodeId, room.roomType, room.templateKind, $"Interactable {interactable.interactableType} is outside playable room bounds.");
                }

                if (interactable.isRequiredReturnRoute)
                {
                    requiredReturns++;
                    if (interactable.requiresTownSigil)
                    {
                        report.AddFailure(buildResult, interactable.nodeId, room.roomType, room.templateKind, "Floor 1 required return route requires a Town Sigil.");
                    }
                }
            }

            if (buildResult.floorIndex == 1)
            {
                if (requiredReturns == 0)
                {
                    report.AddFailure(buildResult, buildResult.transitUpNodeId, ResolveRoomType(buildResult, buildResult.transitUpNodeId), ResolveTemplate(buildResult, buildResult.transitUpNodeId), "Floor 1 required return route is missing.");
                }
                else if (requiredReturns > 1)
                {
                    report.AddFailure(buildResult, buildResult.transitUpNodeId, ResolveRoomType(buildResult, buildResult.transitUpNodeId), ResolveTemplate(buildResult, buildResult.transitUpNodeId), "Duplicate required return-to-town interactables found.");
                }
            }
        }

        private static void ValidateOpeningAgainstWalls(
            DungeonBuildResult buildResult,
            DungeonValidationReport report,
            DungeonNode node,
            DungeonDoorOpeningRecord opening)
        {
            Bounds visualOpening = opening.visualBounds.size == Vector3.zero ? opening.bounds : opening.visualBounds;
            for (int i = 0; i < buildResult.wallSpans.Count; i++)
            {
                DungeonWallSpanRecord wall = buildResult.wallSpans[i];
                if (wall.ownerId != node.nodeId || wall.isCorridorWall)
                {
                    continue;
                }

                if (IntersectsWithToleranceXZ(wall.bounds, visualOpening, OpeningWallTolerance))
                {
                    report.AddFailure(buildResult, node.nodeId, node.nodeKind, node.roomTemplate, $"Doorway opening {opening.openingId} is blocked by a wall span.");
                    return;
                }
            }
        }

        private static bool ContainsXZ(Bounds bounds, Vector3 position)
        {
            return position.x >= bounds.min.x - IntersectionPadding &&
                   position.x <= bounds.max.x + IntersectionPadding &&
                   position.z >= bounds.min.z - IntersectionPadding &&
                   position.z <= bounds.max.z + IntersectionPadding;
        }

        private static bool IntersectsExpanded(Bounds a, Bounds b)
        {
            a.Expand(IntersectionPadding);
            b.Expand(IntersectionPadding);
            return a.Intersects(b);
        }

        private static bool IntersectsWithToleranceXZ(Bounds a, Bounds b, float tolerance)
        {
            return a.min.x < b.max.x - tolerance &&
                   a.max.x > b.min.x + tolerance &&
                   a.min.z < b.max.z - tolerance &&
                   a.max.z > b.min.z + tolerance;
        }

        private static void WarnOnCorridorSeamMismatch(
            DungeonBuildResult buildResult,
            DungeonValidationReport report,
            DungeonNode node,
            DungeonDoorOpeningRecord opening,
            DungeonCorridorBuildRecord corridor)
        {
            Bounds visualOpening = opening.visualBounds.size == Vector3.zero ? opening.bounds : opening.visualBounds;
            Bounds corridorOuter = corridor.outerBounds.size == Vector3.zero ? corridor.bounds : corridor.outerBounds;
            float overlapDepth = GetDirectionalOverlap(corridorOuter, visualOpening, opening.direction);
            if (overlapDepth < SeamTolerance)
            {
                report.AddWarning(buildResult, node.nodeId, node.nodeKind, node.roomTemplate, $"Corridor mouth for edge {opening.edgeKey} overlaps visual doorway by {overlapDepth:0.###}, below seam tolerance {SeamTolerance:0.###}.");
            }

            float corridorOuterWidth = GetLateralSize(corridorOuter, opening.direction);
            float visualOpeningWidth = GetLateralSize(visualOpening, opening.direction);
            float widthGap = visualOpeningWidth - corridorOuterWidth;
            if (widthGap > SeamTolerance)
            {
                report.AddWarning(buildResult, node.nodeId, node.nodeKind, node.roomTemplate, $"Visual doorway opening {opening.openingId} is wider than corridor outer envelope by {widthGap:0.###}.");
            }
        }

        private static float GetDirectionalOverlap(Bounds corridorOuter, Bounds opening, Vector2Int direction)
        {
            if (direction.x == 0)
            {
                return Mathf.Max(0f, Mathf.Min(corridorOuter.max.z, opening.max.z) - Mathf.Max(corridorOuter.min.z, opening.min.z));
            }

            return Mathf.Max(0f, Mathf.Min(corridorOuter.max.x, opening.max.x) - Mathf.Max(corridorOuter.min.x, opening.min.x));
        }

        private static float GetLateralSize(Bounds bounds, Vector2Int direction)
        {
            return direction.x == 0 ? bounds.size.x : bounds.size.z;
        }

        private static Vector2Int ClampToCardinal(Vector2Int delta)
        {
            delta.x = Mathf.Clamp(delta.x, -1, 1);
            delta.y = Mathf.Clamp(delta.y, -1, 1);
            return delta;
        }

        private static DungeonNodeKind ResolveRoomType(DungeonBuildResult buildResult, string nodeId)
        {
            DungeonRoomBuildRecord room = buildResult != null ? buildResult.FindRoom(nodeId) : null;
            return room != null ? room.roomType : DungeonNodeKind.Ordinary;
        }

        private static DungeonRoomTemplateKind ResolveTemplate(DungeonBuildResult buildResult, string nodeId)
        {
            DungeonRoomBuildRecord room = buildResult != null ? buildResult.FindRoom(nodeId) : null;
            return room != null ? room.templateKind : DungeonRoomTemplateKind.SquareChamber;
        }
    }
}
