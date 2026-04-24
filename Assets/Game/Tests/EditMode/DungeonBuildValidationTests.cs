using System.Collections.Generic;
using System.Reflection;
using FrontierDepths.Core;
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
                bounds = opening.visualBounds,
                isCorridorWall = false
            });

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsFalse(report.IsValid);
        }

        [Test]
        public void Validator_AllowsTrimmedWallSpanBesideDoorwayOpening()
        {
            DungeonBuildResult build = CreateValidBuildResult();
            DungeonDoorOpeningRecord opening = build.doorOpenings[0];
            build.wallSpans.Add(new DungeonWallSpanRecord
            {
                ownerId = opening.nodeId,
                direction = opening.direction,
                bounds = CreateTouchingWallBounds(opening),
                isCorridorWall = false
            });

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsTrue(report.IsValid, report.ToSummaryString(build));
            Assert.IsFalse(report.HasWarnings, report.ToSummaryString(build));
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
        public void Validator_DetectsCorridorMissingDoorwayOverlap()
        {
            DungeonBuildResult build = CreateValidBuildResult();
            DungeonCorridorBuildRecord corridor = build.corridors[0];
            corridor.bounds = new Bounds(corridor.bounds.center + new Vector3(0f, 0f, 30f), corridor.bounds.size);

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsFalse(report.IsValid);
        }

        [Test]
        public void Validator_DetectsRoomBoundsOverlap()
        {
            DungeonBuildResult build = CreateValidBuildResult();
            DungeonRoomBuildRecord overlapRoom = build.FindRoom("ordinary_0");
            overlapRoom.bounds = new Bounds(build.FindRoom("entry_hub").bounds.center, overlapRoom.bounds.size);

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsFalse(report.IsValid);
            StringAssert.Contains("Room bounds overlap room ordinary_0.", report.ToSummaryString(build, 10));
        }

        [Test]
        public void Validator_DetectsCorridorOverlapIntoUnrelatedRoom()
        {
            DungeonBuildResult build = CreateValidBuildResult();
            DungeonCorridorBuildRecord corridor = build.corridors[0];
            DungeonRoomBuildRecord landmarkRoom = build.FindRoom("landmark");
            corridor.outerBounds = landmarkRoom.bounds;

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsFalse(report.IsValid);
            StringAssert.Contains("overlaps room landmark", report.ToSummaryString(build, 10));
        }

        [Test]
        public void Validator_WarnsWhenVisualDoorwayExceedsCorridorOuterEnvelope()
        {
            DungeonBuildResult build = CreateValidBuildResult();
            DungeonDoorOpeningRecord opening = build.doorOpenings[0];
            opening.visualBounds = ExpandOpeningWidth(opening.visualBounds, opening.direction, 0.5f);
            opening.visualOpeningWidth += 0.5f;

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsTrue(report.IsValid, report.ToSummaryString(build));
            Assert.IsTrue(report.HasWarnings, report.ToSummaryString(build));
        }

        [Test]
        public void Validator_WarnsWhenCorridorOuterBoundsDoNotReachVisualDoorway()
        {
            DungeonBuildResult build = CreateValidBuildResult();
            DungeonCorridorBuildRecord corridor = build.corridors[0];
            corridor.outerBounds = new Bounds(corridor.outerBounds.center + new Vector3(7f, 0f, 0f), corridor.outerBounds.size);

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsTrue(report.IsValid, report.ToSummaryString(build));
            Assert.IsTrue(report.HasWarnings, report.ToSummaryString(build));
        }

        [Test]
        public void Validator_DetectsEnemySpawnPointInTransitRoom()
        {
            DungeonBuildResult build = CreateValidBuildResult();
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = build.transitUpNodeId,
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(20f, 3.5f, -4f),
                bounds = new Bounds(new Vector3(20f, 3.5f, -4f), new Vector3(2f, 6f, 2f)),
                score = 50f
            });

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsFalse(report.IsValid);
            StringAssert.Contains("non-combat room", report.ToSummaryString(build, 10));
        }

        [Test]
        public void Controller_BuildProducesMetricsAndSpawnCandidates()
        {
            GameObject root = new GameObject("DungeonSceneControllerMetricsTest");

            try
            {
                DungeonBuildResult build = InvokeBuildFloorAttempt(root, useFallback: false, floorSeed: 1932105958);
                DungeonValidationReport report = DungeonValidator.Validate(build);

                Assert.IsTrue(report.IsValid, report.ToSummaryString(build, 10));
                Assert.Greater(build.averageRoomFootprint, 0f);
                Assert.Greater(build.largestRoomFootprint, build.averageRoomFootprint * 0.75f);
                Assert.Greater(build.averageCorridorLength, 0f);
                Assert.Greater(build.maxCorridorLength, 0f);
                Assert.IsNotEmpty(build.GetSpawnPoints(build.playerSpawnNodeId, DungeonSpawnPointCategory.PlayerSpawn));
                Assert.Greater(build.GetSpawnPointCount(DungeonSpawnPointCategory.EnemyMelee), 0);
                Assert.Greater(build.GetSpawnPointCount(DungeonSpawnPointCategory.EnemyRanged), 0);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Controller_FallbackBuild_PassesValidation()
        {
            GameObject root = new GameObject("DungeonSceneControllerValidationTest");

            try
            {
                DungeonBuildResult build = InvokeBuildFloorAttempt(root, useFallback: true);
                DungeonValidationReport report = DungeonValidator.Validate(build);

                Assert.IsTrue(report.IsValid, report.ToSummaryString(build));
                Assert.IsTrue(build.usedFallback);
                Assert.IsTrue(build.requestedFallback);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Validator_AcceptsFallbackLikeValidBuild()
        {
            DungeonBuildResult build = CreateValidBuildResult();

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsTrue(report.IsValid);
            Assert.IsFalse(report.HasWarnings, report.ToSummaryString(build));
        }

        [Test]
        public void Validator_AcceptsSpawnInsideSelectedTransitUpRoom()
        {
            DungeonBuildResult build = CreateValidBuildResult();
            build.playerSpawnNodeId = build.transitUpNodeId;
            build.playerSpawnNodeKind = DungeonNodeKind.TransitUp.ToString();
            build.playerSpawn = new Vector3(20f, 3.5f, 0f);

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsTrue(report.IsValid, report.ToSummaryString(build));
        }

        [Test]
        public void Validator_FailsWhenSelectedSpawnRoomWasNotRendered()
        {
            DungeonBuildResult build = CreateValidBuildResult();
            build.playerSpawnNodeId = "missing_spawn_room";
            build.playerSpawnNodeKind = DungeonNodeKind.TransitUp.ToString();

            DungeonValidationReport report = DungeonValidator.Validate(build);

            Assert.IsFalse(report.IsValid);
            StringAssert.Contains("Selected spawn room missing_spawn_room was not rendered.", report.ToSummaryString(build, 10));
        }

        [Test]
        public void Seed_778287037_NormalOrFallbackBuild_PassesValidation()
        {
            GameObject root = new GameObject("DungeonSceneControllerSeedRegression");

            try
            {
                DungeonBuildResult normalBuild = InvokeBuildFloorAttempt(root, useFallback: false, floorSeed: 778287037);
                DungeonValidationReport normalReport = DungeonValidator.Validate(normalBuild);
                if (normalReport.IsValid)
                {
                    Assert.Pass();
                }

                DungeonBuildResult fallbackBuild = InvokeBuildFloorAttempt(root, useFallback: true, floorSeed: 778287037);
                DungeonValidationReport fallbackReport = DungeonValidator.Validate(fallbackBuild);

                Assert.IsTrue(fallbackReport.IsValid, fallbackReport.ToSummaryString(fallbackBuild, 10));
                Assert.IsFalse(HasSpawnRoomMismatchFailure(fallbackReport), fallbackReport.ToSummaryString(fallbackBuild, 10));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
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
                playerSpawnNodeId = graph.entryHubNodeId,
                playerSpawnNodeKind = DungeonNodeKind.EntryHub.ToString(),
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

            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "entry_hub",
                category = DungeonSpawnPointCategory.PlayerSpawn,
                position = new Vector3(0f, 3.5f, 0f),
                bounds = new Bounds(new Vector3(0f, 3.5f, 0f), new Vector3(2f, 6f, 2f)),
                score = 100f
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "ordinary_0",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(-20f, 3.5f, 0f),
                bounds = new Bounds(new Vector3(-20f, 3.5f, 0f), new Vector3(2f, 6f, 2f)),
                score = 90f
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "landmark",
                category = DungeonSpawnPointCategory.EnemyRanged,
                position = new Vector3(-40f, 3.5f, 0f),
                bounds = new Bounds(new Vector3(-40f, 3.5f, 0f), new Vector3(2f, 6f, 2f)),
                score = 88f
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "landmark",
                category = DungeonSpawnPointCategory.EliteEnemy,
                position = new Vector3(-40f, 3.5f, 2f),
                bounds = new Bounds(new Vector3(-40f, 3.5f, 2f), new Vector3(2f, 6f, 2f)),
                score = 86f
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "secret_0",
                category = DungeonSpawnPointCategory.Chest,
                position = new Vector3(0f, 3.5f, -20f),
                bounds = new Bounds(new Vector3(0f, 3.5f, -20f), new Vector3(2f, 6f, 2f)),
                score = 70f
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "ordinary_0",
                category = DungeonSpawnPointCategory.Shrine,
                position = new Vector3(-22f, 3.5f, 0f),
                bounds = new Bounds(new Vector3(-22f, 3.5f, 0f), new Vector3(2f, 6f, 2f)),
                score = 68f
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "landmark",
                category = DungeonSpawnPointCategory.Reward,
                position = new Vector3(-42f, 3.5f, 0f),
                bounds = new Bounds(new Vector3(-42f, 3.5f, 0f), new Vector3(2f, 6f, 2f)),
                score = 66f
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "entry_hub",
                category = DungeonSpawnPointCategory.Interactable,
                position = new Vector3(0f, 3.5f, 2f),
                bounds = new Bounds(new Vector3(0f, 3.5f, 2f), new Vector3(2f, 6f, 2f)),
                score = 64f
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
                origin = roomCenter,
                bounds = new Bounds(roomCenter + new Vector3(0f, 6f, 0f), new Vector3(12f, 12f, 12f)),
                hasFloor = true,
                wallCount = 4,
                doorwayCount = doorwayCount,
                footprintArea = 144f,
                centerCell = Vector2Int.zero
            });
            build.rooms[build.rooms.Count - 1].floorCells.Add(Vector2Int.zero);
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
            float corridorWidth = 16f;
            float corridorOuterWidth = DungeonSceneController.GetCorridorOuterWidth(corridorWidth);
            Vector3 size = horizontal
                ? new Vector3(Mathf.Abs(end.x - start.x), 2f, corridorWidth)
                : new Vector3(corridorWidth, 2f, Mathf.Abs(end.z - start.z));
            Vector3 outerSize = horizontal
                ? new Vector3(Mathf.Abs(end.x - start.x), 12f, corridorOuterWidth)
                : new Vector3(corridorOuterWidth, 12f, Mathf.Abs(end.z - start.z));

            build.corridors.Add(new DungeonCorridorBuildRecord
            {
                edgeKey = edgeKey,
                fromNodeId = from,
                toNodeId = to,
                segmentIndex = 0,
                start = start,
                end = end,
                bounds = new Bounds(midpoint, size),
                outerBounds = new Bounds(midpoint + new Vector3(0f, 5.5f, 0f), outerSize),
                horizontal = horizontal,
                length = horizontal ? Mathf.Abs(end.x - start.x) : Mathf.Abs(end.z - start.z),
                width = corridorWidth,
                isSecretCorridor = build.graph.GetNode(from).nodeKind == DungeonNodeKind.Secret || build.graph.GetNode(to).nodeKind == DungeonNodeKind.Secret
            });

            Vector2Int fromDirection = ToDirection(toCenter - fromCenter);
            Vector2Int toDirection = -fromDirection;
            AddOpening(build, from, to, fromCenter, fromDirection, edgeKey);
            AddOpening(build, to, from, toCenter, toDirection, edgeKey);
        }

        private static void AddOpening(DungeonBuildResult build, string nodeId, string neighborNodeId, Vector3 roomCenter, Vector2Int direction, string edgeKey)
        {
            Vector3 center = roomCenter + new Vector3(direction.x * 6f, 6f, direction.y * 6f);
            float visualOpeningWidth = DungeonSceneController.GetVisualDoorwayWidth(16f);
            float validationOpeningWidth = DungeonSceneController.GetValidationDoorwayWidth(visualOpeningWidth);
            Vector3 visualSize = direction.x == 0
                ? new Vector3(visualOpeningWidth, 12f, 2f)
                : new Vector3(2f, 12f, visualOpeningWidth);
            Vector3 validationSize = direction.x == 0
                ? new Vector3(validationOpeningWidth, 12f, 2f)
                : new Vector3(2f, 12f, validationOpeningWidth);

            build.doorOpenings.Add(new DungeonDoorOpeningRecord
            {
                openingId = $"DoorOpening_{nodeId}_{direction.x}_{direction.y}",
                nodeId = nodeId,
                neighborNodeId = neighborNodeId,
                direction = direction,
                edgeKey = edgeKey,
                openingWidth = validationOpeningWidth,
                visualOpeningWidth = visualOpeningWidth,
                validationOpeningWidth = validationOpeningWidth,
                center = center,
                visualBounds = new Bounds(center, visualSize),
                bounds = new Bounds(center, validationSize)
            });
            build.reservedZones.Add(new DungeonReservedZoneRecord
            {
                ownerId = nodeId,
                kind = "Doorway",
                bounds = new Bounds(center, new Vector3(Mathf.Max(validationSize.x, 18f), 6f, Mathf.Max(validationSize.z, 18f)))
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

        private static Bounds CreateTouchingWallBounds(DungeonDoorOpeningRecord opening)
        {
            const float spanLength = 4f;
            Bounds referenceBounds = opening.visualBounds;

            if (opening.direction.x == 0)
            {
                float centerX = referenceBounds.max.x + spanLength * 0.5f;
                return new Bounds(
                    new Vector3(centerX, referenceBounds.center.y, referenceBounds.center.z),
                    new Vector3(spanLength, referenceBounds.size.y, referenceBounds.size.z));
            }

            float centerZ = referenceBounds.max.z + spanLength * 0.5f;
            return new Bounds(
                new Vector3(referenceBounds.center.x, referenceBounds.center.y, centerZ),
                new Vector3(referenceBounds.size.x, referenceBounds.size.y, spanLength));
        }

        private static Bounds ExpandOpeningWidth(Bounds bounds, Vector2Int direction, float extraWidth)
        {
            Vector3 size = bounds.size;
            if (direction.x == 0)
            {
                size.x += extraWidth;
            }
            else
            {
                size.z += extraWidth;
            }

            return new Bounds(bounds.center, size);
        }

        private static bool HasSpawnRoomMismatchFailure(DungeonValidationReport report)
        {
            for (int i = 0; i < report.failures.Count; i++)
            {
                if (report.failures[i].reason.Contains("selected spawn room"))
                {
                    return true;
                }
            }

            return false;
        }

        private static DungeonBuildResult InvokeBuildFloorAttempt(GameObject root, bool useFallback, int floorSeed = 4400)
        {
            DungeonSceneController controller = root.AddComponent<DungeonSceneController>();
            typeof(DungeonSceneController).GetField("runtimeRoot", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(controller, root.transform);
            typeof(DungeonSceneController).GetField("roomSpacing", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(controller, DungeonSceneController.NormalizeRoomSpacing(0f));

            MethodInfo method = typeof(DungeonSceneController).GetMethod("BuildFloorAttempt", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, "Expected DungeonSceneController.BuildFloorAttempt to exist.");

            FloorState state = new FloorState { floorIndex = 1, floorSeed = floorSeed };
            state.Normalize(state.floorIndex, state.floorSeed);
            return (DungeonBuildResult)method.Invoke(controller, new object[] { state, useFallback, 1, 1 });
        }
    }
}
