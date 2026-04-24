using System.Collections.Generic;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;

namespace FrontierDepths.Tests.EditMode
{
    public class DungeonBuildValidationTests
    {
        [Test]
        public void Validator_DetectsMissingCorridorRecord()
        {
            DungeonBuildResult build = CreateValidBuildResult();
            build.corridors.Clear();

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsFalse(report.IsValid);
        }

        [Test]
        public void Validator_DetectsMissingDoorwayOpening()
        {
            DungeonBuildResult build = CreateValidBuildResult();
            build.doorOpenings.RemoveAt(0);

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsFalse(report.IsValid);
        }

        [Test]
        public void Validator_DetectsBlockedDoorway()
        {
            DungeonBuildResult build = CreateValidBuildResult();
            DungeonDoorOpeningRecord opening = build.doorOpenings[0];
            build.wallSpans.Add(new DungeonWallSpanRecord
            {
                ownerId = opening.nodeId,
                direction = opening.direction,
                bounds = opening.bounds,
                isCorridorWall = false
            });

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsFalse(report.IsValid);
        }

        [Test]
        public void Validator_DetectsDuplicateFloorOneReturnInteractables()
        {
            DungeonBuildResult build = CreateValidBuildResult();
            build.interactables.Add(new DungeonInteractableBuildRecord
            {
                nodeId = build.transitUpNodeId,
                interactableType = "ReturnLiftDuplicate",
                requiresTownSigil = false,
                returnsToTown = true,
                isRequiredReturnRoute = true,
                position = new Vector3(20f, 1f, 1f),
                bounds = new Bounds(new Vector3(20f, 1f, 1f), new Vector3(2f, 2f, 2f))
            });

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsFalse(report.IsValid);
        }

        [Test]
        public void Validator_DetectsFloorOneRequiredReturnThatNeedsTownSigil()
        {
            DungeonBuildResult build = CreateValidBuildResult();
            build.interactables[0].requiresTownSigil = true;

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsFalse(report.IsValid);
        }

        [Test]
        public void Validator_AcceptsFallbackLikeValidBuild()
        {
            DungeonBuildResult build = CreateValidBuildResult();

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsTrue(report.IsValid);
        }

        private static DungeonBuildResult CreateValidBuildResult()
        {
            DungeonLayoutGraph graph = new DungeonLayoutGraph
            {
                entryHubNodeId = "entry_hub",
                transitUpNodeId = "transit_up",
                transitDownNodeId = "transit_down",
                nodes = new List<DungeonNode>
                {
                    new DungeonNode { nodeId = "entry_hub", nodeKind = DungeonNodeKind.EntryHub, gridPosition = new Vector2Int(0, 0), roomTemplate = DungeonRoomTemplateKind.SquareChamber },
                    new DungeonNode { nodeId = "transit_up", nodeKind = DungeonNodeKind.TransitUp, gridPosition = new Vector2Int(1, 0), roomTemplate = DungeonRoomTemplateKind.BroadRectangle },
                    new DungeonNode { nodeId = "ordinary_0", nodeKind = DungeonNodeKind.Ordinary, gridPosition = new Vector2Int(-1, 0), roomTemplate = DungeonRoomTemplateKind.SquareChamber },
                    new DungeonNode { nodeId = "landmark", nodeKind = DungeonNodeKind.Landmark, gridPosition = new Vector2Int(-2, 0), roomTemplate = DungeonRoomTemplateKind.BroadRectangle },
                    new DungeonNode { nodeId = "secret_0", nodeKind = DungeonNodeKind.Secret, gridPosition = new Vector2Int(0, -1), roomTemplate = DungeonRoomTemplateKind.SquareChamber },
                    new DungeonNode { nodeId = "transit_down", nodeKind = DungeonNodeKind.TransitDown, gridPosition = new Vector2Int(-1, 1), roomTemplate = DungeonRoomTemplateKind.BroadRectangle }
                },
                edges = new List<DungeonEdge>
                {
                    new DungeonEdge { a = "entry_hub", b = "transit_up" },
                    new DungeonEdge { a = "entry_hub", b = "ordinary_0" },
                    new DungeonEdge { a = "entry_hub", b = "secret_0" },
                    new DungeonEdge { a = "ordinary_0", b = "landmark" },
                    new DungeonEdge { a = "ordinary_0", b = "transit_down" }
                }
            };
            graph.entryNodeId = graph.entryHubNodeId;
            graph.stairsNodeId = graph.transitDownNodeId;
            graph.returnAnchorNodeId = graph.transitUpNodeId;

            DungeonBuildResult build = new DungeonBuildResult
            {
                graph = graph,
                floorIndex = 1,
                seed = 4400,
                entryNodeId = graph.entryHubNodeId,
                transitUpNodeId = graph.transitUpNodeId,
                transitDownNodeId = graph.transitDownNodeId,
                landmarkNodeId = "landmark",
                secretNodeId = "secret_0",
                playerSpawn = new Vector3(0f, 3.5f, 0f)
            };

            AddRoom(build, "entry_hub", DungeonNodeKind.EntryHub, DungeonRoomTemplateKind.SquareChamber, Vector3.zero, 2);
            AddRoom(build, "transit_up", DungeonNodeKind.TransitUp, DungeonRoomTemplateKind.BroadRectangle, new Vector3(20f, 0f, 0f), 1);
            AddRoom(build, "ordinary_0", DungeonNodeKind.Ordinary, DungeonRoomTemplateKind.SquareChamber, new Vector3(-20f, 0f, 0f), 3);
            AddRoom(build, "landmark", DungeonNodeKind.Landmark, DungeonRoomTemplateKind.BroadRectangle, new Vector3(-40f, 0f, 0f), 1);
            AddRoom(build, "secret_0", DungeonNodeKind.Secret, DungeonRoomTemplateKind.SquareChamber, new Vector3(0f, 0f, -20f), 1);
            AddRoom(build, "transit_down", DungeonNodeKind.TransitDown, DungeonRoomTemplateKind.BroadRectangle, new Vector3(-20f, 0f, 20f), 1);

            AddEdge(build, "entry_hub", "transit_up", Vector3.zero, new Vector3(20f, 0f, 0f));
            AddEdge(build, "entry_hub", "ordinary_0", Vector3.zero, new Vector3(-20f, 0f, 0f));
            AddEdge(build, "entry_hub", "secret_0", Vector3.zero, new Vector3(0f, 0f, -20f));
            AddEdge(build, "ordinary_0", "landmark", new Vector3(-20f, 0f, 0f), new Vector3(-40f, 0f, 0f));
            AddEdge(build, "ordinary_0", "transit_down", new Vector3(-20f, 0f, 0f), new Vector3(-20f, 0f, 20f));

            build.interactables.Add(new DungeonInteractableBuildRecord
            {
                nodeId = "transit_up",
                interactableType = "ReturnLift",
                requiresTownSigil = false,
                returnsToTown = true,
                isRequiredReturnRoute = true,
                position = new Vector3(20f, 1f, 0f),
                bounds = new Bounds(new Vector3(20f, 1f, 0f), new Vector3(4f, 2f, 4f))
            });
            build.interactables.Add(new DungeonInteractableBuildRecord
            {
                nodeId = "transit_down",
                interactableType = "StairsDown",
                requiresTownSigil = false,
                returnsToTown = false,
                isRequiredReturnRoute = false,
                position = new Vector3(-20f, 1f, 20f),
                bounds = new Bounds(new Vector3(-20f, 1f, 20f), new Vector3(4f, 2f, 4f))
            });

            return build;
        }

        private static void AddRoom(
            DungeonBuildResult build,
            string nodeId,
            DungeonNodeKind kind,
            DungeonRoomTemplateKind template,
            Vector3 roomCenter,
            int doorwayCount)
        {
            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = nodeId,
                roomType = kind,
                templateKind = template,
                bounds = new Bounds(roomCenter + new Vector3(0f, 6f, 0f), new Vector3(12f, 12f, 12f)),
                hasFloor = true,
                wallCount = 4,
                doorwayCount = doorwayCount
            });
        }

        private static void AddEdge(DungeonBuildResult build, string from, string to, Vector3 fromCenter, Vector3 toCenter)
        {
            string edgeKey = DungeonBuildResult.GetEdgeKey(from, to);
            build.graphEdges.Add(new DungeonGraphEdgeRecord { edgeKey = edgeKey, a = from, b = to });

            Vector3 direction = (toCenter - fromCenter).normalized;
            Vector3 start = fromCenter + direction * 6f;
            Vector3 end = toCenter - direction * 6f;
            Vector3 midpoint = (start + end) * 0.5f;
            bool horizontal = Mathf.Abs(end.x - start.x) >= Mathf.Abs(end.z - start.z);
            Vector3 size = horizontal
                ? new Vector3(Mathf.Abs(end.x - start.x), 2f, 16f)
                : new Vector3(16f, 2f, Mathf.Abs(end.z - start.z));

            build.corridors.Add(new DungeonCorridorBuildRecord
            {
                edgeKey = edgeKey,
                fromNodeId = from,
                toNodeId = to,
                segmentIndex = 0,
                start = start,
                end = end,
                bounds = new Bounds(midpoint, size),
                horizontal = horizontal
            });

            Vector2Int fromDirection = ToDirection(toCenter - fromCenter);
            Vector2Int toDirection = -fromDirection;
            AddOpening(build, from, to, fromCenter, fromDirection, edgeKey);
            AddOpening(build, to, from, toCenter, toDirection, edgeKey);
        }

        private static void AddOpening(DungeonBuildResult build, string nodeId, string neighborNodeId, Vector3 roomCenter, Vector2Int direction, string edgeKey)
        {
            Vector3 center = roomCenter + new Vector3(direction.x * 6f, 6f, direction.y * 6f);
            Vector3 size = direction.x == 0
                ? new Vector3(18f, 12f, 2f)
                : new Vector3(2f, 12f, 18f);

            build.doorOpenings.Add(new DungeonDoorOpeningRecord
            {
                openingId = $"DoorOpening_{nodeId}_{direction.x}_{direction.y}",
                nodeId = nodeId,
                neighborNodeId = neighborNodeId,
                direction = direction,
                edgeKey = edgeKey,
                openingWidth = 18f,
                center = center,
                bounds = new Bounds(center, size)
            });
            build.reservedZones.Add(new DungeonReservedZoneRecord
            {
                ownerId = nodeId,
                kind = "Doorway",
                bounds = new Bounds(center, new Vector3(Mathf.Max(size.x, 18f), 6f, Mathf.Max(size.z, 18f)))
            });
        }

        private static Vector2Int ToDirection(Vector3 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.z))
            {
                return delta.x >= 0f ? new Vector2Int(1, 0) : new Vector2Int(-1, 0);
            }

            return delta.z >= 0f ? new Vector2Int(0, 1) : new Vector2Int(0, -1);
        }
    }
}
