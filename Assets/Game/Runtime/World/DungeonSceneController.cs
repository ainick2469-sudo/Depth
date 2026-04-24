using System;
using System.Collections.Generic;
using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class DungeonSceneController : MonoBehaviour
    {
        private const float CellSize = 8f;
        private const float FloorThickness = 1f;
        private const float WallHeight = 12f;
        private const float WallThickness = 1f;
        private const float PrimaryCorridorWidth = 16f;
        private const float SecretCorridorWidth = 16f;
        private const int MaxBuildAttempts = 3;
        private const float RoomBoundsHeight = WallHeight + FloorThickness;
        private const float CorridorZoneHeight = 6f;

        private sealed class BoundarySpan
        {
            public Vector2Int direction;
            public float fixedCoord;
            public float start;
            public float end;
        }

        private static readonly Dictionary<int, Material> MaterialCache = new Dictionary<int, Material>();

        [SerializeField] private Transform runtimeRoot;
        [SerializeField] private float roomSpacing = 56f;

        private readonly GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
        private DungeonBuildResult activeBuildResult;
        private DungeonBuildResult currentBuildResult;
        private bool debugOverlayVisible;
        private string statusMessage = string.Empty;

        public string GetStatusLine()
        {
            return debugOverlayVisible ? statusMessage : string.Empty;
        }

        private void Start()
        {
            runtimeRoot ??= transform;
            roomSpacing = Mathf.Max(150f, roomSpacing);
            BuildFloor();
        }

        private void Update()
        {
            HandleDebugInput();
        }

        private void OnDrawGizmos()
        {
            if (!debugOverlayVisible || currentBuildResult == null)
            {
                return;
            }

            DrawBuildGizmos();
        }

        private void BuildFloor()
        {
            GameBootstrap bootstrap = GameBootstrap.Instance;
            if (bootstrap == null || bootstrap.RunService == null)
            {
                statusMessage = "Dungeon bootstrap not ready yet.";
                return;
            }

            RunState run = bootstrap.RunService.EnsureRun();
            FloorState currentFloor = run.currentFloor ?? new FloorState();
            int baseSeed = currentFloor.floorSeed == 0
                ? 1000 + Mathf.Max(1, currentFloor.floorIndex) * 977
                : currentFloor.floorSeed;

            DungeonValidationReport latestReport = null;
            for (int attempt = 0; attempt < MaxBuildAttempts; attempt++)
            {
                int attemptSeed = baseSeed + attempt * 1543;
                DungeonBuildResult attemptBuild = BuildFloorAttempt(CloneFloorState(currentFloor, run.floorIndex, attemptSeed), false);
                latestReport = DungeonValidator.Validate(attemptBuild);
                if (latestReport.IsValid)
                {
                    ApplySuccessfulBuild(run, attemptBuild);
                    return;
                }

                latestReport.LogFailures();
                ClearRuntimeRoot(true);
            }

            DungeonBuildResult fallbackBuild = BuildFloorAttempt(CloneFloorState(currentFloor, run.floorIndex, baseSeed), true);
            latestReport = DungeonValidator.Validate(fallbackBuild);
            currentBuildResult = fallbackBuild;
            if (latestReport.IsValid)
            {
                ApplySuccessfulBuild(run, fallbackBuild);
                return;
            }

            latestReport.LogFailures();
            Debug.LogError($"Dungeon fallback validation failed for floor {fallbackBuild.floorIndex} seed {fallbackBuild.seed}.");
            statusMessage = "Dungeon validation failed. Check console for details.";
        }

        private DungeonBuildResult BuildFloorAttempt(FloorState floorState, bool useFallback)
        {
            ClearRuntimeRoot(true);

            DungeonLayoutGraph graph = useFallback
                ? generator.GenerateFallback(floorState)
                : generator.Generate(floorState);

            activeBuildResult = new DungeonBuildResult
            {
                graph = graph,
                floorIndex = Mathf.Max(1, floorState.floorIndex),
                seed = floorState.floorSeed,
                usedFallback = useFallback,
                entryNodeId = graph.entryHubNodeId,
                transitUpNodeId = graph.transitUpNodeId,
                transitDownNodeId = graph.transitDownNodeId,
                landmarkNodeId = GetNodeIdByKind(graph, DungeonNodeKind.Landmark),
                secretNodeId = GetNodeIdByKind(graph, DungeonNodeKind.Secret)
            };

            for (int edgeIndex = 0; edgeIndex < graph.edges.Count; edgeIndex++)
            {
                DungeonEdge edge = graph.edges[edgeIndex];
                activeBuildResult.graphEdges.Add(new DungeonGraphEdgeRecord
                {
                    edgeKey = DungeonBuildResult.GetEdgeKey(edge.a, edge.b),
                    a = edge.a,
                    b = edge.b
                });

                CreateCorridor(graph.GetNode(edge.a), graph.GetNode(edge.b));
            }

            for (int nodeIndex = 0; nodeIndex < graph.nodes.Count; nodeIndex++)
            {
                CreateRoom(graph.nodes[nodeIndex], graph);
            }

            activeBuildResult.playerSpawn = GetSpawnPosition(graph);
            DungeonBuildResult completed = activeBuildResult;
            activeBuildResult = null;
            return completed;
        }

        private void ApplySuccessfulBuild(RunState run, DungeonBuildResult buildResult)
        {
            currentBuildResult = buildResult;
            run.currentFloor.floorSeed = buildResult.seed;
            GameBootstrap.Instance.RunService.Save();

            FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
            if (player != null)
            {
                player.WarpTo(buildResult.playerSpawn);
            }

            RefreshStatusMessage();
        }

        private Vector3 GetSpawnPosition(DungeonLayoutGraph graph)
        {
            DungeonNode spawnNode = graph.GetNode(graph.entryHubNodeId);
            return GridToWorld(spawnNode.gridPosition) + Vector3.up * 3.5f;
        }

        private void CreateRoom(DungeonNode node, DungeonLayoutGraph graph)
        {
            HashSet<Vector2Int> floorCells = DungeonRoomTemplateLibrary.GetCells(node);
            GameObject roomRoot = new GameObject($"Room_{node.nodeId}_{node.nodeKind}_{node.roomTemplate}");
            roomRoot.transform.SetParent(runtimeRoot, false);
            roomRoot.transform.position = GridToWorld(node.gridPosition);

            DungeonRoomBuildRecord roomRecord = new DungeonRoomBuildRecord
            {
                nodeId = node.nodeId,
                label = node.label,
                roomType = node.nodeKind,
                templateKind = node.roomTemplate,
                rootObject = roomRoot,
                bounds = GetRoomBounds(roomRoot.transform.position, floorCells)
            };
            activeBuildResult?.rooms.Add(roomRecord);

            Dictionary<Vector2Int, float> doorwayWidths = GetDoorwayWidths(node, graph);
            CreateMergedFloors(roomRoot.transform, floorCells, GetFloorColor(node.nodeKind));
            roomRecord.hasFloor = floorCells.Count > 0;
            CreateRoomWalls(roomRoot.transform, node, graph, floorCells, doorwayWidths, roomRecord);

            CreateInteriorFeature(roomRoot.transform, node, floorCells);
            CreateNodeInteractables(roomRoot.transform, node);
        }

        private void CreateCorridor(DungeonNode a, DungeonNode b)
        {
            if (a == null || b == null)
            {
                return;
            }

            Vector2Int direction2D = b.gridPosition - a.gridPosition;
            direction2D.x = Mathf.Clamp(direction2D.x, -1, 1);
            direction2D.y = Mathf.Clamp(direction2D.y, -1, 1);
            Vector2Int reverseDirection = new Vector2Int(-direction2D.x, -direction2D.y);
            float corridorWidth = a.nodeKind == DungeonNodeKind.Secret || b.nodeKind == DungeonNodeKind.Secret
                ? SecretCorridorWidth
                : PrimaryCorridorWidth;

            Vector3 start = GetDoorWorldPosition(a, direction2D);
            Vector3 end = GetDoorWorldPosition(b, reverseDirection);
            List<Vector3> routePoints = BuildCorridorRoute(start, end, direction2D);
            string edgeKey = DungeonBuildResult.GetEdgeKey(a.nodeId, b.nodeId);
            GameObject corridorRoot = new GameObject($"Corridor_{a.nodeId}_To_{b.nodeId}");
            corridorRoot.transform.SetParent(runtimeRoot, false);

            int segmentIndex = 0;
            for (int i = 1; i < routePoints.Count; i++)
            {
                Vector3 segmentStart = routePoints[i - 1];
                Vector3 segmentEnd = routePoints[i];
                if (Vector3.Distance(segmentStart, segmentEnd) <= 0.05f)
                {
                    continue;
                }

                CreateCorridorSegment(corridorRoot.transform, edgeKey, a.nodeId, b.nodeId, segmentIndex++, corridorWidth, segmentStart, segmentEnd);
            }
        }

        private void CreateCorridorSegment(
            Transform corridorRoot,
            string edgeKey,
            string fromNodeId,
            string toNodeId,
            int segmentIndex,
            float corridorWidth,
            Vector3 start,
            Vector3 end)
        {
            Vector3 midpoint = (start + end) * 0.5f;
            Vector3 delta = end - start;
            bool horizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.z);
            float corridorLength = Mathf.Max(CellSize * 0.9f, horizontal ? Mathf.Abs(delta.x) : Mathf.Abs(delta.z));

            GameObject segmentRoot = new GameObject($"Corridor_{fromNodeId}_To_{toNodeId}_Segment_{segmentIndex}");
            segmentRoot.transform.SetParent(corridorRoot, false);

            Vector3 floorScale = horizontal
                ? new Vector3(corridorLength, FloorThickness, corridorWidth)
                : new Vector3(corridorWidth, FloorThickness, corridorLength);

            GameObject floor = CreatePrimitive("Floor", segmentRoot.transform, midpoint + Vector3.down * (FloorThickness * 0.5f), floorScale, new Color(0.19f, 0.18f, 0.17f));
            activeBuildResult?.corridors.Add(new DungeonCorridorBuildRecord
            {
                edgeKey = edgeKey,
                fromNodeId = fromNodeId,
                toNodeId = toNodeId,
                segmentIndex = segmentIndex,
                start = start,
                end = end,
                bounds = GetBounds(floor.transform.position, floorScale),
                horizontal = horizontal
            });
            activeBuildResult?.reservedZones.Add(new DungeonReservedZoneRecord
            {
                ownerId = edgeKey,
                kind = "Corridor",
                bounds = GetBounds(midpoint + Vector3.up * (CorridorZoneHeight * 0.5f), new Vector3(floorScale.x, CorridorZoneHeight, floorScale.z))
            });

            CreateCorridorWalls(segmentRoot.transform, edgeKey, midpoint, corridorLength, corridorWidth, horizontal);
        }

        private void CreateCorridorWalls(Transform parent, string edgeKey, Vector3 midpoint, float corridorLength, float corridorWidth, bool horizontal)
        {
            Vector3 leftOffset = horizontal
                ? new Vector3(0f, WallHeight * 0.5f - FloorThickness * 0.5f, corridorWidth * 0.5f)
                : new Vector3(corridorWidth * 0.5f, WallHeight * 0.5f - FloorThickness * 0.5f, 0f);

            Vector3 rightOffset = -leftOffset;
            leftOffset.y = WallHeight * 0.5f - FloorThickness * 0.5f;
            rightOffset.y = WallHeight * 0.5f - FloorThickness * 0.5f;

            float trimmedLength = Mathf.Max(CellSize, corridorLength + WallThickness * 0.1f);
            Vector3 wallScale = horizontal
                ? new Vector3(trimmedLength, WallHeight, WallThickness)
                : new Vector3(WallThickness, WallHeight, trimmedLength);

            GameObject leftWall = CreatePrimitive("LeftWall", parent, midpoint + leftOffset, wallScale, new Color(0.16f, 0.17f, 0.2f));
            GameObject rightWall = CreatePrimitive("RightWall", parent, midpoint + rightOffset, wallScale, new Color(0.16f, 0.17f, 0.2f));
            RecordCorridorWall(edgeKey, GetBounds(leftWall.transform.position, wallScale));
            RecordCorridorWall(edgeKey, GetBounds(rightWall.transform.position, wallScale));
        }

        private void HandleDebugInput()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                debugOverlayVisible = !debugOverlayVisible;
                RefreshStatusMessage();
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                RefreshStatusMessage();
                Debug.Log(string.IsNullOrWhiteSpace(statusMessage) ? "Dungeon build not ready." : statusMessage);
            }

            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (shiftHeld)
                {
                    RegenerateFloorWithNewSeed();
                }
                else
                {
                    BuildFloor();
                }
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                TryTeleportToNode(currentBuildResult != null ? currentBuildResult.entryNodeId : string.Empty);
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                TryTeleportToNode(currentBuildResult != null ? currentBuildResult.transitDownNodeId : string.Empty);
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                TryTeleportToNode(currentBuildResult != null ? currentBuildResult.landmarkNodeId : string.Empty);
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                TryTeleportToNode(currentBuildResult != null ? currentBuildResult.secretNodeId : string.Empty);
            }

            if (Input.GetKeyDown(KeyCode.F10) && GameBootstrap.Instance != null)
            {
                GameBootstrap.Instance.SceneFlowService.LoadScene(GameSceneId.TownHub);
            }
        }

        private void RegenerateFloorWithNewSeed()
        {
            if (GameBootstrap.Instance == null || GameBootstrap.Instance.RunService == null)
            {
                return;
            }

            RunState run = GameBootstrap.Instance.RunService.EnsureRun();
            run.currentFloor.floorSeed = Mathf.Abs(Guid.NewGuid().GetHashCode()) + Mathf.Max(1, run.floorIndex) * 977;
            GameBootstrap.Instance.RunService.Save();
            BuildFloor();
        }

        private void TryTeleportToNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || currentBuildResult == null)
            {
                return;
            }

            DungeonRoomBuildRecord room = currentBuildResult.FindRoom(nodeId);
            FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
            if (room == null || player == null)
            {
                return;
            }

            Vector3 target = new Vector3(room.bounds.center.x, 3.5f, room.bounds.center.z);
            player.WarpTo(target);
        }

        private void DrawBuildGizmos()
        {
            for (int i = 0; i < currentBuildResult.rooms.Count; i++)
            {
                Gizmos.color = GetGizmoColor(currentBuildResult.rooms[i].roomType);
                Gizmos.DrawWireCube(currentBuildResult.rooms[i].bounds.center, currentBuildResult.rooms[i].bounds.size);
            }

            Gizmos.color = Color.yellow;
            for (int i = 0; i < currentBuildResult.corridors.Count; i++)
            {
                Gizmos.DrawWireCube(currentBuildResult.corridors[i].bounds.center, currentBuildResult.corridors[i].bounds.size);
                Gizmos.DrawLine(currentBuildResult.corridors[i].start + Vector3.up, currentBuildResult.corridors[i].end + Vector3.up);
            }

            Gizmos.color = new Color(1f, 0.5f, 0.1f);
            for (int i = 0; i < currentBuildResult.reservedZones.Count; i++)
            {
                Gizmos.DrawWireCube(currentBuildResult.reservedZones[i].bounds.center, currentBuildResult.reservedZones[i].bounds.size);
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(currentBuildResult.playerSpawn, 1.1f);
        }

        private static FloorState CloneFloorState(FloorState source, int fallbackFloorIndex, int floorSeed)
        {
            FloorState clone = new FloorState
            {
                floorIndex = source != null && source.floorIndex > 0 ? source.floorIndex : fallbackFloorIndex,
                floorSeed = floorSeed,
                floorBandId = source != null ? source.floorBandId : "floorband.frontier_mine",
                chapterId = source != null ? source.chapterId : "chapter.frontier_descent",
                themeKitId = source != null ? source.themeKitId : "theme.frontier_town",
                stairDiscovered = source != null && source.stairDiscovered
            };
            clone.Normalize(clone.floorIndex, floorSeed);
            return clone;
        }

        private void ClearRuntimeRoot(bool immediate)
        {
            if (runtimeRoot == null)
            {
                return;
            }

            for (int i = runtimeRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = runtimeRoot.GetChild(i).gameObject;
                if (immediate)
                {
                    DestroyImmediate(child);
                }
                else
                {
                    Destroy(child);
                }
            }
        }

        private static string GetNodeIdByKind(DungeonLayoutGraph graph, DungeonNodeKind kind)
        {
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                if (graph.nodes[i].nodeKind == kind)
                {
                    return graph.nodes[i].nodeId;
                }
            }

            return string.Empty;
        }

        private void RefreshStatusMessage()
        {
            if (currentBuildResult == null)
            {
                statusMessage = "Dungeon build not ready.";
                return;
            }

            statusMessage = $"Floor {currentBuildResult.floorIndex} | Seed {currentBuildResult.seed} | Rooms {currentBuildResult.rooms.Count} | Corridor Segments {currentBuildResult.corridors.Count} | Fallback {(currentBuildResult.usedFallback ? "Yes" : "No")}";
        }

        private static List<Vector3> BuildCorridorRoute(Vector3 start, Vector3 end, Vector2Int direction)
        {
            List<Vector3> points = new List<Vector3> { start };
            bool primaryHorizontal = direction.x != 0;

            if (primaryHorizontal)
            {
                float midX = (start.x + end.x) * 0.5f;
                AddRoutePoint(points, new Vector3(midX, start.y, start.z));
                AddRoutePoint(points, new Vector3(midX, end.y, end.z));
            }
            else
            {
                float midZ = (start.z + end.z) * 0.5f;
                AddRoutePoint(points, new Vector3(start.x, start.y, midZ));
                AddRoutePoint(points, new Vector3(end.x, end.y, midZ));
            }

            AddRoutePoint(points, end);
            return points;
        }

        private static void AddRoutePoint(List<Vector3> points, Vector3 point)
        {
            if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], point) > 0.05f)
            {
                points.Add(point);
            }
        }

        private Bounds GetRoomBounds(Vector3 roomPosition, HashSet<Vector2Int> floorCells)
        {
            GetLocalBounds(floorCells, out float minX, out float maxX, out float minZ, out float maxZ);
            Vector3 center = roomPosition + new Vector3((minX + maxX) * 0.5f, RoomBoundsHeight * 0.5f - FloorThickness * 0.5f, (minZ + maxZ) * 0.5f);
            Vector3 size = new Vector3(maxX - minX, RoomBoundsHeight, maxZ - minZ);
            return new Bounds(center, size);
        }

        private void RecordCorridorWall(string edgeKey, Bounds bounds)
        {
            activeBuildResult?.wallSpans.Add(new DungeonWallSpanRecord
            {
                ownerId = edgeKey,
                edgeKey = edgeKey,
                bounds = bounds,
                isCorridorWall = true
            });
        }

        private void RecordDoorOpening(
            Transform roomRoot,
            DungeonNode node,
            DungeonLayoutGraph graph,
            Vector2Int direction,
            float openingWidth,
            DungeonRoomBuildRecord roomRecord)
        {
            if (activeBuildResult == null || activeBuildResult.FindDoorOpening(node.nodeId, direction) != null)
            {
                return;
            }

            DungeonNode neighbor = GetNeighborForDirection(node, graph, direction);
            string edgeKey = neighbor != null ? DungeonBuildResult.GetEdgeKey(node.nodeId, neighbor.nodeId) : string.Empty;
            Vector3 doorwayCenter = roomRoot.position + GetDoorLocalPosition(node, direction);
            Vector3 openingSize = direction.x == 0
                ? new Vector3(openingWidth, WallHeight, WallThickness * 2f)
                : new Vector3(WallThickness * 2f, WallHeight, openingWidth);

            GameObject openingMarker = new GameObject($"DoorOpening_{node.nodeId}_{DirectionToToken(direction)}");
            openingMarker.transform.SetParent(roomRoot, false);
            openingMarker.transform.localPosition = GetDoorLocalPosition(node, direction);

            activeBuildResult.doorOpenings.Add(new DungeonDoorOpeningRecord
            {
                openingId = openingMarker.name,
                nodeId = node.nodeId,
                direction = direction,
                neighborNodeId = neighbor != null ? neighbor.nodeId : string.Empty,
                edgeKey = edgeKey,
                openingWidth = openingWidth,
                center = doorwayCenter,
                bounds = GetBounds(doorwayCenter + Vector3.up * (WallHeight * 0.5f - FloorThickness * 0.5f), openingSize)
            });
            activeBuildResult.reservedZones.Add(new DungeonReservedZoneRecord
            {
                ownerId = node.nodeId,
                kind = "Doorway",
                bounds = GetDoorwayReservedZone(doorwayCenter, direction, openingWidth)
            });
            roomRecord.doorwayCount++;
        }

        private void RecordInteractable(
            string nodeId,
            string interactableType,
            GameObject interactableObject,
            bool requiresTownSigil,
            bool returnsToTown,
            bool isRequiredReturnRoute)
        {
            if (activeBuildResult == null || interactableObject == null)
            {
                return;
            }

            Collider collider = interactableObject.GetComponent<Collider>();
            Bounds bounds = collider != null
                ? collider.bounds
                : new Bounds(interactableObject.transform.position, Vector3.one);

            activeBuildResult.interactables.Add(new DungeonInteractableBuildRecord
            {
                nodeId = nodeId,
                interactableType = interactableType,
                requiresTownSigil = requiresTownSigil,
                returnsToTown = returnsToTown,
                isRequiredReturnRoute = isRequiredReturnRoute,
                position = interactableObject.transform.position,
                bounds = bounds
            });
        }

        private static DungeonNode GetNeighborForDirection(DungeonNode node, DungeonLayoutGraph graph, Vector2Int direction)
        {
            List<DungeonNode> neighbors = graph.GetNeighbors(node.nodeId);
            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector2Int delta = neighbors[i].gridPosition - node.gridPosition;
                delta.x = Mathf.Clamp(delta.x, -1, 1);
                delta.y = Mathf.Clamp(delta.y, -1, 1);
                if (delta == direction)
                {
                    return neighbors[i];
                }
            }

            return null;
        }

        private static Bounds GetDoorwayReservedZone(Vector3 doorwayCenter, Vector2Int direction, float openingWidth)
        {
            Vector3 size = direction.x == 0
                ? new Vector3(openingWidth, CorridorZoneHeight, CellSize * 1.75f)
                : new Vector3(CellSize * 1.75f, CorridorZoneHeight, openingWidth);
            Vector3 center = doorwayCenter + new Vector3(direction.x * CellSize * 0.5f, CorridorZoneHeight * 0.5f, direction.y * CellSize * 0.5f);
            return new Bounds(center, size);
        }

        private static Bounds GetBounds(Vector3 center, Vector3 size)
        {
            return new Bounds(center, size);
        }

        private static string DirectionToToken(Vector2Int direction)
        {
            if (direction == new Vector2Int(0, 1))
            {
                return "North";
            }

            if (direction == new Vector2Int(0, -1))
            {
                return "South";
            }

            if (direction == new Vector2Int(1, 0))
            {
                return "East";
            }

            return "West";
        }

        private static Color GetGizmoColor(DungeonNodeKind kind)
        {
            return kind switch
            {
                DungeonNodeKind.EntryHub => Color.cyan,
                DungeonNodeKind.TransitUp => new Color(0.5f, 0.8f, 1f),
                DungeonNodeKind.TransitDown => Color.red,
                DungeonNodeKind.Landmark => new Color(0.3f, 0.85f, 0.4f),
                DungeonNodeKind.Secret => new Color(0.7f, 0.3f, 0.95f),
                _ => Color.white
            };
        }

        private static void CreateMergedFloors(Transform roomRoot, HashSet<Vector2Int> floorCells, Color color)
        {
            HashSet<Vector2Int> remaining = new HashSet<Vector2Int>(floorCells);
            while (remaining.Count > 0)
            {
                Vector2Int origin = SelectLowestCell(remaining);
                int width = 1;
                while (remaining.Contains(new Vector2Int(origin.x + width, origin.y)))
                {
                    width++;
                }

                int height = 1;
                bool canGrow = true;
                while (canGrow)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (!remaining.Contains(new Vector2Int(origin.x + x, origin.y + height)))
                        {
                            canGrow = false;
                            break;
                        }
                    }

                    if (canGrow)
                    {
                        height++;
                    }
                }

                Vector3 localPosition = new Vector3(
                    (origin.x + (width - 1) * 0.5f) * CellSize,
                    -FloorThickness * 0.5f,
                    (origin.y + (height - 1) * 0.5f) * CellSize);
                Vector3 localScale = new Vector3(width * CellSize, FloorThickness, height * CellSize);
                CreatePrimitive($"Floor_{origin.x}_{origin.y}_{width}_{height}", roomRoot, localPosition, localScale, color);

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        remaining.Remove(new Vector2Int(origin.x + x, origin.y + y));
                    }
                }
            }
        }

        private static Vector2Int SelectLowestCell(HashSet<Vector2Int> cells)
        {
            Vector2Int best = default;
            bool hasValue = false;

            foreach (Vector2Int cell in cells)
            {
                if (!hasValue || cell.y < best.y || (cell.y == best.y && cell.x < best.x))
                {
                    best = cell;
                    hasValue = true;
                }
            }

            return best;
        }

        private void CreateRoomWalls(
            Transform roomRoot,
            DungeonNode node,
            DungeonLayoutGraph graph,
            HashSet<Vector2Int> floorCells,
            Dictionary<Vector2Int, float> doorwayWidths,
            DungeonRoomBuildRecord roomRecord)
        {
            List<BoundarySpan> spans = CollectBoundarySpans(floorCells);
            for (int i = 0; i < spans.Count; i++)
            {
                BoundarySpan span = spans[i];
                if (doorwayWidths.TryGetValue(span.direction, out float openingWidth))
                {
                    CreateWallSpanWithOpening(roomRoot, node, graph, span, openingWidth, node.nodeKind, roomRecord);
                }
                else
                {
                    CreateWallSpan(roomRoot, node.nodeId, span.direction, span.fixedCoord, span.start, span.end, node.nodeKind, roomRecord);
                }
            }
        }

        private static List<BoundarySpan> CollectBoundarySpans(HashSet<Vector2Int> floorCells)
        {
            List<BoundarySpan> spans = new List<BoundarySpan>();
            for (int dirIndex = 0; dirIndex < 4; dirIndex++)
            {
                Vector2Int direction = CardinalDirection(dirIndex);
                Dictionary<int, List<int>> lineGroups = new Dictionary<int, List<int>>();

                foreach (Vector2Int cell in floorCells)
                {
                    if (floorCells.Contains(cell + direction))
                    {
                        continue;
                    }

                    int lineKey;
                    int axisIndex;
                    if (direction.x == 0)
                    {
                        lineKey = cell.y * 2 + direction.y;
                        axisIndex = cell.x;
                    }
                    else
                    {
                        lineKey = cell.x * 2 + direction.x;
                        axisIndex = cell.y;
                    }

                    if (!lineGroups.TryGetValue(lineKey, out List<int> indices))
                    {
                        indices = new List<int>();
                        lineGroups.Add(lineKey, indices);
                    }

                    indices.Add(axisIndex);
                }

                foreach (KeyValuePair<int, List<int>> entry in lineGroups)
                {
                    List<int> indices = entry.Value;
                    indices.Sort();
                    int runStart = indices[0];
                    int runEnd = indices[0];

                    for (int i = 1; i < indices.Count; i++)
                    {
                        if (indices[i] == runEnd + 1)
                        {
                            runEnd = indices[i];
                            continue;
                        }

                        spans.Add(CreateBoundarySpan(direction, entry.Key, runStart, runEnd));
                        runStart = indices[i];
                        runEnd = indices[i];
                    }

                    spans.Add(CreateBoundarySpan(direction, entry.Key, runStart, runEnd));
                }
            }

            return spans;
        }

        private static BoundarySpan CreateBoundarySpan(Vector2Int direction, int lineKey, int runStart, int runEnd)
        {
            return new BoundarySpan
            {
                direction = direction,
                fixedCoord = lineKey * CellSize * 0.5f,
                start = (runStart - 0.5f) * CellSize,
                end = (runEnd + 0.5f) * CellSize
            };
        }

        private void CreateWallSpanWithOpening(
            Transform roomRoot,
            DungeonNode node,
            DungeonLayoutGraph graph,
            BoundarySpan span,
            float openingWidth,
            DungeonNodeKind kind,
            DungeonRoomBuildRecord roomRecord)
        {
            Vector3 doorwayLocal = GetDoorLocalPosition(node, span.direction);
            float doorwayFixed = span.direction.x == 0 ? doorwayLocal.z : doorwayLocal.x;
            if (Mathf.Abs(doorwayFixed - span.fixedCoord) > 0.01f)
            {
                CreateWallSpan(roomRoot, node.nodeId, span.direction, span.fixedCoord, span.start, span.end, kind, roomRecord);
                return;
            }

            float openingCenter = span.direction.x == 0 ? doorwayLocal.x : doorwayLocal.z;
            float openingStart = Mathf.Max(span.start, openingCenter - openingWidth * 0.5f);
            float openingEnd = Mathf.Min(span.end, openingCenter + openingWidth * 0.5f);
            RecordDoorOpening(roomRoot, node, graph, span.direction, openingWidth, roomRecord);

            CreateWallSpan(roomRoot, node.nodeId, span.direction, span.fixedCoord, span.start, openingStart, kind, roomRecord);
            CreateWallSpan(roomRoot, node.nodeId, span.direction, span.fixedCoord, openingEnd, span.end, kind, roomRecord);
        }

        private void CreateWallSpan(
            Transform roomRoot,
            string ownerNodeId,
            Vector2Int direction,
            float fixedCoord,
            float start,
            float end,
            DungeonNodeKind kind,
            DungeonRoomBuildRecord roomRecord)
        {
            float length = end - start;
            if (length <= 0.05f)
            {
                return;
            }

            Vector3 localPosition;
            Vector3 scale;
            if (direction.x == 0)
            {
                localPosition = new Vector3((start + end) * 0.5f, WallHeight * 0.5f - FloorThickness * 0.5f, fixedCoord);
                scale = new Vector3(length + WallThickness, WallHeight, WallThickness);
            }
            else
            {
                localPosition = new Vector3(fixedCoord, WallHeight * 0.5f - FloorThickness * 0.5f, (start + end) * 0.5f);
                scale = new Vector3(WallThickness, WallHeight, length + WallThickness);
            }

            GameObject wall = CreatePrimitive(
                $"Wall_{ownerNodeId}_{DirectionToToken(direction)}_{roomRecord.wallCount}",
                roomRoot,
                localPosition,
                scale,
                GetWallColor(kind));
            roomRecord.wallCount++;
            activeBuildResult?.wallSpans.Add(new DungeonWallSpanRecord
            {
                ownerId = ownerNodeId,
                direction = direction,
                bounds = GetBounds(wall.transform.position, scale),
                isCorridorWall = false
            });
        }

        private static bool IsDoorOpeningCell(DungeonNode node, Vector2Int direction, Vector2Int cell, HashSet<Vector2Int> floorCells)
        {
            Vector2Int socket = DungeonRoomTemplateLibrary.GetDoorSocket(node, direction);
            Vector2Int lateral = direction.x == 0 ? new Vector2Int(1, 0) : new Vector2Int(0, 1);
            for (int offset = -1; offset <= 1; offset++)
            {
                Vector2Int candidate = socket + lateral * offset;
                if (floorCells.Contains(candidate) && cell == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private static void CreateWall(Transform roomRoot, Vector2Int cell, Vector2Int direction, DungeonNodeKind kind)
        {
            Vector3 offset = direction switch
            {
                { x: 0, y: 1 } => new Vector3(0f, WallHeight * 0.5f - FloorThickness * 0.5f, CellSize * 0.5f),
                { x: 0, y: -1 } => new Vector3(0f, WallHeight * 0.5f - FloorThickness * 0.5f, -CellSize * 0.5f),
                { x: 1, y: 0 } => new Vector3(CellSize * 0.5f, WallHeight * 0.5f - FloorThickness * 0.5f, 0f),
                _ => new Vector3(-CellSize * 0.5f, WallHeight * 0.5f - FloorThickness * 0.5f, 0f)
            };

            Vector3 scale = direction.x == 0
                ? new Vector3(CellSize + WallThickness, WallHeight, WallThickness)
                : new Vector3(WallThickness, WallHeight, CellSize + WallThickness);

            CreatePrimitive(
                $"Wall_{cell.x}_{cell.y}_{direction.x}_{direction.y}",
                roomRoot,
                CellToLocalPosition(cell) + offset,
                scale,
                GetWallColor(kind));
        }

        private static void CreateDoorwayCheeks(Transform roomRoot, DungeonNode node, Vector2Int direction, float corridorWidth, DungeonNodeKind kind)
        {
            Vector2Int socket = DungeonRoomTemplateLibrary.GetDoorSocket(node, direction);
            Vector3 doorwayCenter = CellToLocalPosition(socket) + new Vector3(
                direction.x * CellSize * 0.5f,
                WallHeight * 0.5f - FloorThickness * 0.5f,
                direction.y * CellSize * 0.5f);

            float cheekDepth = CellSize * 1.1f;
            Color color = GetWallColor(kind);

            if (direction.x == 0)
            {
                float z = doorwayCenter.z - direction.y * cheekDepth * 0.5f;
                float halfWidth = corridorWidth * 0.5f;
                CreatePrimitive(
                    $"DoorCheekL_{socket.x}_{socket.y}_{direction.x}_{direction.y}",
                    roomRoot,
                    new Vector3(doorwayCenter.x - halfWidth, doorwayCenter.y, z),
                    new Vector3(WallThickness, WallHeight, cheekDepth),
                    color);
                CreatePrimitive(
                    $"DoorCheekR_{socket.x}_{socket.y}_{direction.x}_{direction.y}",
                    roomRoot,
                    new Vector3(doorwayCenter.x + halfWidth, doorwayCenter.y, z),
                    new Vector3(WallThickness, WallHeight, cheekDepth),
                    color);
            }
            else
            {
                float x = doorwayCenter.x - direction.x * cheekDepth * 0.5f;
                float halfWidth = corridorWidth * 0.5f;
                CreatePrimitive(
                    $"DoorCheekL_{socket.x}_{socket.y}_{direction.x}_{direction.y}",
                    roomRoot,
                    new Vector3(x, doorwayCenter.y, doorwayCenter.z - halfWidth),
                    new Vector3(cheekDepth, WallHeight, WallThickness),
                    color);
                CreatePrimitive(
                    $"DoorCheekR_{socket.x}_{socket.y}_{direction.x}_{direction.y}",
                    roomRoot,
                    new Vector3(x, doorwayCenter.y, doorwayCenter.z + halfWidth),
                    new Vector3(cheekDepth, WallHeight, WallThickness),
                    color);
            }
        }

        private static void CreateInteriorFeature(Transform roomRoot, DungeonNode node, HashSet<Vector2Int> floorCells)
        {
            DungeonTemplateFeature feature = DungeonRoomTemplateLibrary.GetFeature(node);
            if (feature == DungeonTemplateFeature.Flat)
            {
                return;
            }

            GetLocalBounds(floorCells, out float minX, out float maxX, out float minZ, out float maxZ);
            Vector3 center = new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);
            float width = maxX - minX;
            float length = maxZ - minZ;
            Color accent = Color.Lerp(GetFloorColor(node.nodeKind), Color.white, 0.14f);

            switch (feature)
            {
                case DungeonTemplateFeature.RaisedDais:
                    CreateRaisedDaisFeature(roomRoot, center, width, length, accent);
                    break;
                case DungeonTemplateFeature.SunkenPit:
                    CreateSunkenPitFeature(roomRoot, center, width, length, accent);
                    break;
                case DungeonTemplateFeature.SplitDivider:
                    CreateSplitDividerFeature(roomRoot, center, width, length, accent, node.rotationQuarterTurns % 2 == 0);
                    break;
                case DungeonTemplateFeature.BalconyBridge:
                    CreateBalconyBridgeFeature(roomRoot, center, width, length, accent, node.rotationQuarterTurns % 2 == 0);
                    break;
            }
        }

        private static void CreateRaisedDaisFeature(Transform parent, Vector3 center, float width, float length, Color accent)
        {
            float platformWidth = Mathf.Max(CellSize * 2.2f, width * 0.42f);
            float platformLength = Mathf.Max(CellSize * 2.2f, length * 0.42f);
            CreatePrimitive("RaisedDais", parent, center + Vector3.up * 0.85f, new Vector3(platformWidth, 1.7f, platformLength), accent);
            CreateRamp(parent, center + new Vector3(0f, 0.35f, platformLength * 0.5f + CellSize * 0.45f), new Vector3(platformWidth * 0.55f, 0.8f, CellSize * 1.6f), new Vector3(-16f, 0f, 0f), accent);
            CreateRamp(parent, center + new Vector3(0f, 0.35f, -platformLength * 0.5f - CellSize * 0.45f), new Vector3(platformWidth * 0.55f, 0.8f, CellSize * 1.6f), new Vector3(16f, 0f, 0f), accent);
        }

        private static void CreateSunkenPitFeature(Transform parent, Vector3 center, float width, float length, Color accent)
        {
            float pitWidth = Mathf.Max(CellSize * 2.3f, width * 0.5f);
            float pitLength = Mathf.Max(CellSize * 2.3f, length * 0.5f);
            CreatePrimitive("SunkenPit", parent, center + Vector3.down * 0.75f, new Vector3(pitWidth, 1.25f, pitLength), Color.Lerp(accent, Color.black, 0.18f));
            CreateRamp(parent, center + new Vector3(0f, -0.2f, pitLength * 0.5f + CellSize * 0.38f), new Vector3(pitWidth * 0.45f, 0.75f, CellSize * 1.5f), new Vector3(15f, 0f, 0f), accent);
            CreateRamp(parent, center + new Vector3(0f, -0.2f, -pitLength * 0.5f - CellSize * 0.38f), new Vector3(pitWidth * 0.45f, 0.75f, CellSize * 1.5f), new Vector3(-15f, 0f, 0f), accent);
            CreateRamp(parent, center + new Vector3(pitWidth * 0.5f + CellSize * 0.38f, -0.2f, 0f), new Vector3(CellSize * 1.5f, 0.75f, pitLength * 0.4f), new Vector3(0f, 0f, 15f), accent);
            CreateRamp(parent, center + new Vector3(-pitWidth * 0.5f - CellSize * 0.38f, -0.2f, 0f), new Vector3(CellSize * 1.5f, 0.75f, pitLength * 0.4f), new Vector3(0f, 0f, -15f), accent);
        }

        private static void CreateSplitDividerFeature(Transform parent, Vector3 center, float width, float length, Color accent, bool horizontal)
        {
            Vector3 segmentScale = horizontal
                ? new Vector3(width * 0.32f, 4.2f, WallThickness)
                : new Vector3(WallThickness, 4.2f, length * 0.32f);
            Vector3 offset = horizontal ? new Vector3(width * 0.22f, 2.1f, 0f) : new Vector3(0f, 2.1f, length * 0.22f);

            CreatePrimitive("DividerA", parent, center - offset, segmentScale, accent);
            CreatePrimitive("DividerB", parent, center + offset, segmentScale, accent);
        }

        private static void CreateBalconyBridgeFeature(Transform parent, Vector3 center, float width, float length, Color accent, bool horizontal)
        {
            float balconyThickness = horizontal ? length * 0.32f : width * 0.32f;
            float span = horizontal ? width : length;
            float sideSpan = horizontal ? length : width;

            CreatePrimitive("LowerWell", parent, center + Vector3.down * 0.85f, horizontal
                ? new Vector3(width * 0.65f, 1.2f, length * 0.42f)
                : new Vector3(width * 0.42f, 1.2f, length * 0.65f), Color.Lerp(accent, Color.black, 0.14f));

            if (horizontal)
            {
                CreatePrimitive("BalconyNorth", parent, center + new Vector3(0f, 0.85f, sideSpan * 0.24f), new Vector3(span * 0.72f, 1.7f, balconyThickness), accent);
                CreatePrimitive("BalconySouth", parent, center + new Vector3(0f, 0.85f, -sideSpan * 0.24f), new Vector3(span * 0.72f, 1.7f, balconyThickness), accent);
                CreatePrimitive("Bridge", parent, center + Vector3.up * 1.2f, new Vector3(CellSize * 1.8f, 0.7f, length * 0.82f), Color.Lerp(accent, Color.white, 0.08f));
                CreateRamp(parent, center + new Vector3(-span * 0.28f, 0.4f, 0f), new Vector3(CellSize * 1.8f, 0.7f, CellSize * 1.7f), new Vector3(0f, 0f, -15f), accent);
                CreateRamp(parent, center + new Vector3(span * 0.28f, 0.4f, 0f), new Vector3(CellSize * 1.8f, 0.7f, CellSize * 1.7f), new Vector3(0f, 0f, 15f), accent);
            }
            else
            {
                CreatePrimitive("BalconyEast", parent, center + new Vector3(sideSpan * 0.24f, 0.85f, 0f), new Vector3(balconyThickness, 1.7f, span * 0.72f), accent);
                CreatePrimitive("BalconyWest", parent, center + new Vector3(-sideSpan * 0.24f, 0.85f, 0f), new Vector3(balconyThickness, 1.7f, span * 0.72f), accent);
                CreatePrimitive("Bridge", parent, center + Vector3.up * 1.2f, new Vector3(width * 0.82f, 0.7f, CellSize * 1.8f), Color.Lerp(accent, Color.white, 0.08f));
                CreateRamp(parent, center + new Vector3(0f, 0.4f, -span * 0.28f), new Vector3(CellSize * 1.7f, 0.7f, CellSize * 1.8f), new Vector3(15f, 0f, 0f), accent);
                CreateRamp(parent, center + new Vector3(0f, 0.4f, span * 0.28f), new Vector3(CellSize * 1.7f, 0.7f, CellSize * 1.8f), new Vector3(-15f, 0f, 0f), accent);
            }
        }

        private static void CreateRamp(Transform parent, Vector3 localPosition, Vector3 localScale, Vector3 localEulerAngles, Color color)
        {
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "RoomRamp";
            ramp.transform.SetParent(parent, false);
            ramp.transform.localPosition = localPosition;
            ramp.transform.localRotation = Quaternion.Euler(localEulerAngles);
            ramp.transform.localScale = localScale;
            ApplyColor(ramp.GetComponent<Renderer>(), color);
        }

        private void CreateNodeInteractables(Transform roomRoot, DungeonNode node)
        {
            if (node.nodeKind == DungeonNodeKind.TransitDown)
            {
                GameObject stairs = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stairs.name = $"Interactable_{node.nodeId}_StairsDown";
                stairs.transform.SetParent(roomRoot, false);
                stairs.transform.localPosition = new Vector3(0f, 1f, 0f);
                stairs.transform.localScale = new Vector3(6f, 0.75f, 6f);
                ApplyColor(stairs.GetComponent<Renderer>(), GetFloorColor(node.nodeKind));
                stairs.AddComponent<DungeonStairsInteractable>();
                RecordInteractable(node.nodeId, "StairsDown", stairs, false, false, false);
            }
            else if (node.nodeKind == DungeonNodeKind.TransitUp)
            {
                GameObject stairs = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stairs.name = $"Interactable_{node.nodeId}_ReturnLift";
                stairs.transform.SetParent(roomRoot, false);
                stairs.transform.localPosition = new Vector3(0f, 1f, 0f);
                stairs.transform.localScale = new Vector3(5.6f, 0.75f, 5.6f);
                ApplyColor(stairs.GetComponent<Renderer>(), GetFloorColor(node.nodeKind));
                stairs.AddComponent<DungeonAscendInteractable>();
                bool requiredReturnRoute = activeBuildResult != null && activeBuildResult.floorIndex == 1;
                RecordInteractable(node.nodeId, "ReturnLift", stairs, false, true, requiredReturnRoute);
            }
        }

        private static Dictionary<Vector2Int, Vector2Int> GetConnectionDirections(DungeonNode node, DungeonLayoutGraph graph)
        {
            Dictionary<Vector2Int, Vector2Int> directions = new Dictionary<Vector2Int, Vector2Int>();
            List<DungeonNode> neighbors = graph.GetNeighbors(node.nodeId);
            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector2Int delta = neighbors[i].gridPosition - node.gridPosition;
                delta.x = Mathf.Clamp(delta.x, -1, 1);
                delta.y = Mathf.Clamp(delta.y, -1, 1);
                directions[delta] = delta;
            }

            return directions;
        }

        private Vector3 GridToWorld(Vector2Int gridPosition)
        {
            return new Vector3(gridPosition.x * roomSpacing, 0f, gridPosition.y * roomSpacing);
        }

        private static Vector3 CellToLocalPosition(Vector2Int cell)
        {
            return new Vector3(cell.x * CellSize, 0f, cell.y * CellSize);
        }

        private Vector3 GetDoorWorldPosition(DungeonNode node, Vector2Int direction)
        {
            return GridToWorld(node.gridPosition) + GetDoorLocalPosition(node, direction);
        }

        private static Vector3 GetDoorLocalPosition(DungeonNode node, Vector2Int direction)
        {
            Vector2Int socket = DungeonRoomTemplateLibrary.GetDoorSocket(node, direction);
            return CellToLocalPosition(socket) + new Vector3(direction.x * CellSize * 0.5f, 0f, direction.y * CellSize * 0.5f);
        }

        private static Dictionary<Vector2Int, float> GetDoorwayWidths(DungeonNode node, DungeonLayoutGraph graph)
        {
            Dictionary<Vector2Int, float> widths = new Dictionary<Vector2Int, float>();
            List<DungeonNode> neighbors = graph.GetNeighbors(node.nodeId);
            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector2Int delta = neighbors[i].gridPosition - node.gridPosition;
                delta.x = Mathf.Clamp(delta.x, -1, 1);
                delta.y = Mathf.Clamp(delta.y, -1, 1);
                float corridorWidth = node.nodeKind == DungeonNodeKind.Secret || neighbors[i].nodeKind == DungeonNodeKind.Secret
                    ? SecretCorridorWidth
                    : PrimaryCorridorWidth;
                widths[delta] = corridorWidth + WallThickness * 2f;
            }

            return widths;
        }

        private static void GetLocalBounds(HashSet<Vector2Int> floorCells, out float minX, out float maxX, out float minZ, out float maxZ)
        {
            minX = float.MaxValue;
            maxX = float.MinValue;
            minZ = float.MaxValue;
            maxZ = float.MinValue;

            foreach (Vector2Int cell in floorCells)
            {
                Vector3 local = CellToLocalPosition(cell);
                minX = Mathf.Min(minX, local.x - CellSize * 0.5f);
                maxX = Mathf.Max(maxX, local.x + CellSize * 0.5f);
                minZ = Mathf.Min(minZ, local.z - CellSize * 0.5f);
                maxZ = Mathf.Max(maxZ, local.z + CellSize * 0.5f);
            }
        }

        private static Vector2Int CardinalDirection(int index)
        {
            return index switch
            {
                0 => new Vector2Int(0, 1),
                1 => new Vector2Int(0, -1),
                2 => new Vector2Int(1, 0),
                _ => new Vector2Int(-1, 0)
            };
        }

        private static GameObject CreatePrimitive(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;
            ApplyColor(primitive.GetComponent<Renderer>(), color);
            return primitive;
        }

        private static void ApplyColor(Renderer renderer, Color color)
        {
            renderer.sharedMaterial = GetSharedMaterial(color);
        }

        private static Material GetSharedMaterial(Color color)
        {
            Color32 color32 = color;
            int key = color32.r | (color32.g << 8) | (color32.b << 16) | (color32.a << 24);
            if (MaterialCache.TryGetValue(key, out Material cached))
            {
                return cached;
            }

            Material material = new Material(Shader.Find("Standard"))
            {
                color = color
            };
            MaterialCache.Add(key, material);
            return material;
        }

        private static Color GetFloorColor(DungeonNodeKind kind)
        {
            return kind switch
            {
                DungeonNodeKind.EntryHub => new Color(0.31f, 0.46f, 0.66f),
                DungeonNodeKind.TransitUp => new Color(0.29f, 0.58f, 0.68f),
                DungeonNodeKind.TransitDown => new Color(0.8f, 0.66f, 0.22f),
                DungeonNodeKind.Landmark => new Color(0.38f, 0.63f, 0.5f),
                DungeonNodeKind.Secret => new Color(0.56f, 0.42f, 0.72f),
                _ => new Color(0.34f, 0.35f, 0.38f)
            };
        }

        private static Color GetWallColor(DungeonNodeKind kind)
        {
            return kind switch
            {
                DungeonNodeKind.EntryHub => new Color(0.25f, 0.35f, 0.52f),
                DungeonNodeKind.TransitUp => new Color(0.25f, 0.43f, 0.5f),
                DungeonNodeKind.TransitDown => new Color(0.58f, 0.48f, 0.16f),
                DungeonNodeKind.Landmark => new Color(0.28f, 0.44f, 0.35f),
                DungeonNodeKind.Secret => new Color(0.44f, 0.33f, 0.58f),
                _ => new Color(0.2f, 0.22f, 0.26f)
            };
        }
    }
}
