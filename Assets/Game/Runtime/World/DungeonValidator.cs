using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class DungeonValidationReport
    {
        public readonly List<DungeonValidationFailure> failures = new List<DungeonValidationFailure>();

        public bool IsValid => failures.Count == 0;

        public void AddFailure(
            DungeonBuildResult buildResult,
            string nodeId,
            DungeonNodeKind roomType,
            DungeonRoomTemplateKind templateKind,
            string reason)
        {
            failures.Add(new DungeonValidationFailure
            {
                seed = buildResult != null ? buildResult.seed : 0,
                floorIndex = buildResult != null ? buildResult.floorIndex : 0,
                nodeId = nodeId ?? string.Empty,
                roomType = roomType,
                templateKind = templateKind,
                reason = reason ?? "Unknown failure."
            });
        }

        public void LogFailures()
        {
            for (int i = 0; i < failures.Count; i++)
            {
                Debug.LogError(failures[i].ToLogString());
            }
        }
    }

    public sealed class DungeonValidationFailure
    {
        public int seed;
        public int floorIndex;
        public string nodeId;
        public DungeonNodeKind roomType;
        public DungeonRoomTemplateKind templateKind;
        public string reason;

        public string ToLogString()
        {
            return $"Dungeon validation failed: seed={seed}, floor={floorIndex}, node={nodeId}, roomType={roomType}, template={templateKind}, reason={reason}";
        }
    }

    public static class DungeonValidator
    {
        private const float IntersectionPadding = 0.1f;

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
            for (int i = 0; i < buildResult.wallSpans.Count; i++)
            {
                DungeonWallSpanRecord wall = buildResult.wallSpans[i];
                if (wall.ownerId != node.nodeId || wall.isCorridorWall)
                {
                    continue;
                }

                if (IntersectsExpanded(wall.bounds, opening.bounds))
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
