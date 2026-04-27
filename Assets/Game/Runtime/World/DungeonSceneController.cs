using System;
using System.Collections.Generic;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class DungeonSceneController : MonoBehaviour
    {
        private const float CellSize = 6f;
        private const float FloorThickness = 1f;
        private const float WallHeight = 12f;
        private const float WallThickness = 1f;
        private const float PrimaryCorridorWidth = 12f;
        private const float SecretCorridorWidth = 10f;
        private const int MaxBuildAttempts = 3;
        private const float RoomBoundsHeight = WallHeight + FloorThickness;
        private const float CorridorZoneHeight = 6f;
        private const float DoorwayClearance = 0.25f;
        internal const float CorridorRoomOverlap = 0.75f;
        internal const float CorridorVisualRoomOverlap = 0.02f;
        internal const float CorridorVisualFloorYOffset = -0.015f;
        private const float DoorwayAlignmentEpsilon = 0.05f;
        private const float PlayerSpawnHeight = 3.5f;
        private const float SpawnWallMargin = 6f;
        private const float SpawnInteractableClearance = 3f;
        private const float SpawnDoorwayClearance = 4f;
        private const float SpawnCandidateRadius = 4f;
        private const float SpawnCandidateHeight = 3.5f;
        private const float SpawnCandidateClearanceRadius = 3f;
        private const float EnemyMeleeSpawnMinimumDistance = 20f;
        private const float CombatTestEnemyMinimumSeparation = 14f;
        private const float EnemyRangedSpawnMinimumDistance = 30f;
        private const float EliteEnemySpawnMinimumDistance = 24f;
        private const float TargetDummySpawnMinimumDistance = 12f;
        private const int MaxCategorySpawnPointsPerRoom = 4;
        private const float DefaultRoomSpacing = 78f;
        private const float MinimumRecommendedRoomSpacing = 74f;
        private const float MaximumRecommendedRoomSpacing = 82f;
        private const float MinimumRoomGap = 12f;
        private const float CorridorTargetLength = 36f;
        private const string CombatTestStationName = "CombatTestStation";
        private const string CombatTestEnemiesName = "CombatTestEnemies";

        private sealed class BoundarySpan
        {
            public Vector2Int direction;
            public float fixedCoord;
            public float start;
            public float end;
        }

        internal readonly struct DungeonSpawnRoutingResult
        {
            public readonly string selectedNodeId;
            public readonly string selectedNodeKind;
            public readonly FloorTransitionKind transitionKind;
            public readonly bool useExplicitWorldPosition;
            public readonly Vector3 explicitWorldPosition;
            public readonly bool usedEntryFallback;
            public readonly string warningMessage;

            public DungeonSpawnRoutingResult(
                string selectedNodeId,
                string selectedNodeKind,
                FloorTransitionKind transitionKind,
                bool useExplicitWorldPosition,
                Vector3 explicitWorldPosition,
                bool usedEntryFallback,
                string warningMessage)
            {
                this.selectedNodeId = selectedNodeId;
                this.selectedNodeKind = selectedNodeKind;
                this.transitionKind = transitionKind;
                this.useExplicitWorldPosition = useExplicitWorldPosition;
                this.explicitWorldPosition = explicitWorldPosition;
                this.usedEntryFallback = usedEntryFallback;
                this.warningMessage = warningMessage;
            }
        }

        private static readonly Dictionary<int, Material> MaterialCache = new Dictionary<int, Material>();

        [SerializeField] private Transform runtimeRoot;
        [SerializeField] private float roomSpacing = DefaultRoomSpacing;
        [SerializeField] private bool enableCombatTestStation = true;
        [SerializeField] private bool enableEncounterDirector = true;
        [SerializeField] private bool enableCombatTestEnemies;
        [SerializeField] private bool allowCombatTestEnemiesWithEncounters;

        private readonly GraphFirstDungeonGenerator generator = new GraphFirstDungeonGenerator();
        private DungeonEncounterDirector encounterDirector;
        private DungeonBuildResult activeBuildResult;
        private DungeonBuildResult currentBuildResult;
        private DungeonBuildResult emergencyDebugBuildResult;
        private bool debugOverlayVisible;
        private string statusMessage = string.Empty;

        public DungeonBuildResult CurrentBuildResult => GetVisibleBuildResult();

        public string GetStatusLine()
        {
            return debugOverlayVisible ? statusMessage : string.Empty;
        }

        private void Start()
        {
            runtimeRoot ??= transform;
            roomSpacing = NormalizeRoomSpacing(roomSpacing);
            BuildFloor();
        }

        private void Update()
        {
            HandleDebugInput();
        }

        private void OnDrawGizmos()
        {
            if (!debugOverlayVisible || GetVisibleBuildResult() == null)
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

            currentBuildResult = null;
            emergencyDebugBuildResult = null;
            RunState run = bootstrap.RunService.EnsureRun();
            FloorState currentFloor = run.currentFloor ?? new FloorState();
            int baseSeed = currentFloor.floorSeed == 0
                ? 1000 + Mathf.Max(1, currentFloor.floorIndex) * 977
                : currentFloor.floorSeed;

            DungeonValidationReport latestReport = null;
            List<GraphValidationReport> failedGraphReports = new List<GraphValidationReport>();
            List<string> renderedValidationFailures = new List<string>();
            FloorState repeatedFloorState = null;
            DungeonLayoutGraph repeatedGraph = null;
            GraphValidationReport repeatedGraphReport = null;
            int repeatedAttemptNumber = 0;
            for (int attempt = 0; attempt < MaxBuildAttempts; attempt++)
            {
                int attemptSeed = GraphFirstDungeonGenerator.GetNormalAttemptBaseSeed(baseSeed, attempt);
                FloorState attemptFloorState = CloneFloorState(currentFloor, run.floorIndex, attemptSeed);
                if (!generator.TryGenerateNormal(attemptFloorState, out DungeonLayoutGraph graph, out GraphValidationReport graphReport))
                {
                    failedGraphReports.Add(graphReport);
                    graphReport.Log("Normal dungeon graph generation failed.");
                    continue;
                }

                failedGraphReports.Add(graphReport);
                graphReport.Log("Normal dungeon graph generation succeeded.");
                DungeonBuildResult attemptBuild = BuildFloorAttempt(attemptFloorState, graph, graphReport, false, attempt + 1, MaxBuildAttempts);
                latestReport = DungeonValidator.Validate(attemptBuild);
                FinalizeValidation(attemptBuild, latestReport, false);
                Debug.Log(attemptBuild.validationSummary);
                if (latestReport.IsValid)
                {
                    bool canTryAnotherNormalAttempt = attempt < MaxBuildAttempts - 1;
                    if (ShouldRejectRepeatedLayout(run, attemptBuild, canTryAnotherNormalAttempt))
                    {
                        if (repeatedGraph == null)
                        {
                            repeatedFloorState = attemptFloorState;
                            repeatedGraph = graph;
                            repeatedGraphReport = graphReport;
                            repeatedAttemptNumber = attempt + 1;
                        }

                        Debug.LogWarning(
                            $"Normal dungeon layout shape repeated a recent floor on floor {attemptBuild.floorIndex}; " +
                            "rerolling while normal attempts remain. Reliability still wins over anti-repetition.");
                        ClearRuntimeRoot(true);
                        continue;
                    }

                    ApplySuccessfulBuild(run, attemptBuild);
                    return;
                }

                renderedValidationFailures.Add(latestReport.ToSummaryString(attemptBuild, 5));
                latestReport.LogFailures(attemptBuild);
                ClearRuntimeRoot(true);
            }

            if (repeatedGraph != null)
            {
                Debug.LogWarning("Anti-repetition found only repeated valid normal layouts; accepting best valid normal layout instead of falling back.");
                DungeonBuildResult repeatedBuild = BuildFloorAttempt(
                    repeatedFloorState,
                    repeatedGraph,
                    repeatedGraphReport,
                    false,
                    repeatedAttemptNumber,
                    MaxBuildAttempts);
                latestReport = DungeonValidator.Validate(repeatedBuild);
                FinalizeValidation(repeatedBuild, latestReport, false);
                Debug.Log(repeatedBuild.validationSummary);
                if (latestReport.IsValid)
                {
                    ApplySuccessfulBuild(run, repeatedBuild);
                    return;
                }

                renderedValidationFailures.Add(latestReport.ToSummaryString(repeatedBuild, 5));
                latestReport.LogFailures(repeatedBuild);
                ClearRuntimeRoot(true);
            }

            FloorState fallbackFloorState = CloneFloorState(currentFloor, run.floorIndex, baseSeed);
            DungeonLayoutGraph fallbackGraph = generator.GenerateFallback(fallbackFloorState);
            GraphValidationReport fallbackGraphReport = CreateFallbackGraphReport(fallbackFloorState, fallbackGraph);
            LogFallbackDiagnostics(fallbackFloorState, failedGraphReports, renderedValidationFailures);
            Debug.Log($"Explicit dungeon fallback requested. {fallbackGraphReport.ToSummaryString()}");
            DungeonBuildResult fallbackBuild = BuildFloorAttempt(fallbackFloorState, fallbackGraph, fallbackGraphReport, true, 1, 1);
            latestReport = DungeonValidator.Validate(fallbackBuild);
            FinalizeValidation(fallbackBuild, latestReport, !latestReport.IsValid);
            fallbackBuild.validationSummary = $"{fallbackBuild.validationSummary} | NORMAL GENERATION FAILED";
            Debug.Log(fallbackBuild.validationSummary);
            if (latestReport.IsValid)
            {
                ApplySuccessfulBuild(run, fallbackBuild);
                return;
            }

            emergencyDebugBuildResult = fallbackBuild;
            currentBuildResult = null;
            SetVisibleInteractablesEnabled(false);
            latestReport.LogFailures(fallbackBuild);
            Debug.LogError($"Dungeon fallback validation failed for floor {fallbackBuild.floorIndex} seed {fallbackBuild.seed}.");
            statusMessage = $"{fallbackBuild.validationSummary} | Emergency debug layout left visible.";
        }

        private DungeonBuildResult BuildFloorAttempt(
            FloorState floorState,
            DungeonLayoutGraph graph,
            GraphValidationReport graphValidationReport,
            bool useFallback,
            int attemptNumber,
            int attemptCount)
        {
            ClearRuntimeRoot(true);

            activeBuildResult = new DungeonBuildResult
            {
                graph = graph,
                floorIndex = Mathf.Max(1, floorState.floorIndex),
                seed = floorState.floorSeed,
                usedFallback = useFallback,
                requestedFallback = useFallback,
                generatorReturnedFallbackGraph = useFallback,
                attemptNumber = Mathf.Max(1, attemptNumber),
                attemptCount = Mathf.Max(1, attemptCount),
                graphLayoutSignature = graphValidationReport != null
                    ? graphValidationReport.layoutSignature
                    : DungeonLayoutSignatureUtility.BuildSignature(graph, floorState),
                layoutShapeSignature = graphValidationReport != null && !string.IsNullOrWhiteSpace(graphValidationReport.layoutShapeSignature)
                    ? graphValidationReport.layoutShapeSignature
                    : DungeonLayoutSignatureUtility.BuildShapeSignature(graph),
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

            SampleStaticSpawnCandidates();
            activeBuildResult.playerSpawn = GetSpawnPosition(graph);
            SampleCombatSpawnCandidates(activeBuildResult.playerSpawn);
            UpdateBuildMetrics(activeBuildResult);
            DungeonBuildResult completed = activeBuildResult;
            activeBuildResult = null;
            return completed;
        }

        private void ApplySuccessfulBuild(RunState run, DungeonBuildResult buildResult)
        {
            buildResult.isEmergencyDebugBuild = false;
            emergencyDebugBuildResult = null;
            currentBuildResult = buildResult;
            SetVisibleInteractablesEnabled(true);
            run.currentFloor.floorSeed = buildResult.seed;
            run.currentFloor.graphLayoutSignature = buildResult.graphLayoutSignature ?? string.Empty;
            run.currentFloor.layoutShapeSignature = buildResult.layoutShapeSignature ?? string.Empty;
            GameBootstrap.Instance.RunService.Save();

            FirstPersonController player = FindAnyObjectByType<FirstPersonController>();
            if (player != null)
            {
                EnsurePlayerCombatComponents(player);
                player.WarpTo(buildResult.playerSpawn);
            }

            SpawnCombatTestStation(buildResult);
            SpawnEncounterEnemies(buildResult);
            if (enableCombatTestEnemies && (!enableEncounterDirector || allowCombatTestEnemiesWithEncounters))
            {
                SpawnCombatTestEnemies(buildResult);
            }
            RefreshStatusMessage();
        }

        private Vector3 GetSpawnPosition(DungeonLayoutGraph graph)
        {
            RunState run = GameBootstrap.Instance != null ? GameBootstrap.Instance.RunService.Current : null;
            int floorIndex = activeBuildResult != null
                ? activeBuildResult.floorIndex
                : (run != null && run.floorIndex > 0 ? run.floorIndex : 1);
            FloorTransitionKind transitionKind = run != null ? run.lastTransition : FloorTransitionKind.StartedRun;
            PortalAnchorState portalAnchor = run != null ? run.portalAnchor : PortalAnchorState.Invalid;
            DungeonSpawnRoutingResult routing = ResolveSpawnRouting(graph, transitionKind, portalAnchor, floorIndex);
            if (activeBuildResult != null)
            {
                activeBuildResult.playerSpawnNodeId = routing.selectedNodeId;
                activeBuildResult.playerSpawnNodeKind = routing.selectedNodeKind;
            }

            if (!string.IsNullOrWhiteSpace(routing.warningMessage))
            {
                Debug.LogWarning($"Dungeon spawn routing fallback on floor {floorIndex}: {routing.warningMessage}");
            }

            Vector3 finalPosition;
            if (routing.useExplicitWorldPosition)
            {
                finalPosition = routing.explicitWorldPosition;
                finalPosition.y = Mathf.Max(finalPosition.y, PlayerSpawnHeight);
            }
            else
            {
                finalPosition = GetSpawnPositionForNode(graph, routing.selectedNodeId);
            }

            if (Debug.isDebugBuild || Application.isEditor)
            {
                Debug.Log(
                    $"Dungeon spawn routed. Floor={floorIndex} Transition={routing.transitionKind} " +
                    $"NodeId={routing.selectedNodeId} NodeKind={routing.selectedNodeKind} Position={finalPosition}");
            }

            return finalPosition;
        }

        internal static DungeonSpawnRoutingResult ResolveSpawnRouting(
            DungeonLayoutGraph graph,
            FloorTransitionKind transitionKind,
            PortalAnchorState portalAnchor,
            int floorIndex)
        {
            if (graph == null)
            {
                return new DungeonSpawnRoutingResult(
                    string.Empty,
                    "MissingGraph",
                    transitionKind,
                    false,
                    Vector3.up * PlayerSpawnHeight,
                    true,
                    "Dungeon graph missing during spawn selection.");
            }

            if (transitionKind == FloorTransitionKind.ReturnedByPortal &&
                portalAnchor.isValid &&
                portalAnchor.floorIndex == floorIndex)
            {
                DungeonNode portalNode = !string.IsNullOrWhiteSpace(portalAnchor.roomId)
                    ? graph.GetNode(portalAnchor.roomId)
                    : null;

                return new DungeonSpawnRoutingResult(
                    portalNode != null ? portalNode.nodeId : portalAnchor.roomId,
                    portalNode != null ? portalNode.nodeKind.ToString() : "PortalAnchor",
                    transitionKind,
                    true,
                    portalAnchor.worldPosition.ToVector3(),
                    false,
                    string.Empty);
            }

            string requestedNodeId = transitionKind switch
            {
                FloorTransitionKind.Ascended => graph.transitDownNodeId,
                FloorTransitionKind.Descended => graph.transitUpNodeId,
                FloorTransitionKind.ReturnedByPortal => graph.transitUpNodeId,
                _ => graph.transitUpNodeId
            };

            DungeonNode requestedNode = graph.GetNode(requestedNodeId);
            if (requestedNode != null)
            {
                return new DungeonSpawnRoutingResult(
                    requestedNode.nodeId,
                    requestedNode.nodeKind.ToString(),
                    transitionKind,
                    false,
                    Vector3.zero,
                    false,
                    string.Empty);
            }

            DungeonNode fallbackNode = graph.GetNode(graph.entryHubNodeId);
            if (fallbackNode != null)
            {
                return new DungeonSpawnRoutingResult(
                    fallbackNode.nodeId,
                    fallbackNode.nodeKind.ToString(),
                    transitionKind,
                    false,
                    Vector3.zero,
                    true,
                    $"Requested spawn node '{requestedNodeId}' missing for transition {transitionKind}; falling back to entry hub '{fallbackNode.nodeId}'.");
            }

            DungeonNode anyNode = graph.nodes.Count > 0 ? graph.nodes[0] : null;
            return new DungeonSpawnRoutingResult(
                anyNode != null ? anyNode.nodeId : string.Empty,
                anyNode != null ? anyNode.nodeKind.ToString() : "MissingNode",
                transitionKind,
                false,
                Vector3.up * PlayerSpawnHeight,
                true,
                $"Requested spawn node '{requestedNodeId}' missing and entry hub '{graph.entryHubNodeId}' was unavailable.");
        }

        private Vector3 GetSpawnPositionForNode(DungeonLayoutGraph graph, string nodeId)
        {
            DungeonRoomBuildRecord room = activeBuildResult != null ? activeBuildResult.FindRoom(nodeId) : null;
            if (room != null)
            {
                List<DungeonSpawnPointRecord> sampledPlayerSpawns = activeBuildResult.GetSpawnPoints(nodeId, DungeonSpawnPointCategory.PlayerSpawn);
                if (sampledPlayerSpawns.Count > 0)
                {
                    return sampledPlayerSpawns[0].position;
                }

                if (TryFindSafeSpawnPosition(room, nodeId, out Vector3 safeSpawn))
                {
                    return safeSpawn;
                }

                return new Vector3(room.bounds.center.x, PlayerSpawnHeight, room.bounds.center.z);
            }

            DungeonNode node = graph != null ? graph.GetNode(nodeId) : null;
            if (node == null && graph != null)
            {
                node = graph.GetNode(graph.entryHubNodeId);
            }

            return node != null
                ? GridToWorld(node.gridPosition) + Vector3.up * PlayerSpawnHeight
                : Vector3.up * PlayerSpawnHeight;
        }

        private void SampleStaticSpawnCandidates()
        {
            if (activeBuildResult == null)
            {
                return;
            }

            for (int i = 0; i < activeBuildResult.rooms.Count; i++)
            {
                DungeonRoomBuildRecord room = activeBuildResult.rooms[i];
                AddSpawnCandidates(room, DungeonSpawnPointCategory.PlayerSpawn, MaxCategorySpawnPointsPerRoom, 0f, true, true, false);
                AddSpawnCandidates(room, DungeonSpawnPointCategory.Interactable, 2, 0f, true, true, false);

                if (room.roomType == DungeonNodeKind.EntryHub || room.roomType == DungeonNodeKind.TransitUp || room.roomType == DungeonNodeKind.TransitDown)
                {
                    continue;
                }

                AddSpawnCandidates(room, DungeonSpawnPointCategory.Chest, 2, 0f, true, true, false);
                AddSpawnCandidates(room, DungeonSpawnPointCategory.Shrine, 1, 0f, true, true, false);
                AddSpawnCandidates(room, DungeonSpawnPointCategory.Reward, 1, 0f, true, true, false);
            }
        }

        private void SampleCombatSpawnCandidates(Vector3 playerSpawn)
        {
            if (activeBuildResult == null)
            {
                return;
            }

            for (int i = 0; i < activeBuildResult.rooms.Count; i++)
            {
                DungeonRoomBuildRecord room = activeBuildResult.rooms[i];
                if (room.roomType != DungeonNodeKind.Ordinary && room.roomType != DungeonNodeKind.Landmark)
                {
                    continue;
                }

                AddSpawnCandidates(room, DungeonSpawnPointCategory.EnemyMelee, GetEnemyMeleeSpawnCandidateCount(room, activeBuildResult.floorIndex), EnemyMeleeSpawnMinimumDistance, true, true, true, playerSpawn);
                AddSpawnCandidates(room, DungeonSpawnPointCategory.EnemyRanged, 2, EnemyRangedSpawnMinimumDistance, true, true, true, playerSpawn);
                AddSpawnCandidates(room, DungeonSpawnPointCategory.EliteEnemy, 1, EliteEnemySpawnMinimumDistance, true, true, true, playerSpawn);
                AddSpawnCandidates(room, DungeonSpawnPointCategory.TargetDummy, 4, TargetDummySpawnMinimumDistance, true, true, true, playerSpawn);
            }
        }

        private static int GetEnemyMeleeSpawnCandidateCount(DungeonRoomBuildRecord room, int floorIndex)
        {
            if (room == null)
            {
                return 3;
            }

            int count = room.footprintArea >= 1600f ? 6 : (room.footprintArea >= 900f ? 4 : 3);
            if (room.roomType == DungeonNodeKind.Landmark)
            {
                count += floorIndex >= 6 ? 4 : 2;
            }

            if (floorIndex >= 6)
            {
                count += 2;
            }

            return Mathf.Clamp(count, 3, room.roomType == DungeonNodeKind.Landmark ? 10 : 7);
        }

        private void AddSpawnCandidates(
            DungeonRoomBuildRecord room,
            DungeonSpawnPointCategory category,
            int maxCount,
            float minimumDistanceFromPlayer,
            bool avoidDoorways,
            bool avoidInteractables,
            bool avoidPlayerSpawn,
            Vector3? playerSpawn = null)
        {
            if (activeBuildResult == null || room == null || maxCount <= 0)
            {
                return;
            }

            List<DungeonSpawnPointRecord> candidates = new List<DungeonSpawnPointRecord>();
            Vector3 roomCenter = new Vector3(room.bounds.center.x, SpawnCandidateHeight, room.bounds.center.z);

            for (int cellIndex = 0; cellIndex < room.floorCells.Count; cellIndex++)
            {
                Vector3 candidatePosition = room.origin + CellToLocalPosition(room.floorCells[cellIndex]) + Vector3.up * SpawnCandidateHeight;
                if (!IsSpawnCellConnectedToCenter(room, room.floorCells[cellIndex]))
                {
                    continue;
                }

                if (!IsSpawnPointValid(room, candidatePosition, minimumDistanceFromPlayer, avoidDoorways, avoidInteractables, avoidPlayerSpawn, playerSpawn))
                {
                    continue;
                }

                candidates.Add(new DungeonSpawnPointRecord
                {
                    nodeId = room.nodeId,
                    category = category,
                    position = candidatePosition,
                    bounds = new Bounds(candidatePosition, new Vector3(SpawnCandidateClearanceRadius, 6f, SpawnCandidateClearanceRadius)),
                    score = ScoreSpawnCandidate(category, candidatePosition, roomCenter, playerSpawn)
                });
            }

            candidates.Sort((left, right) => right.score.CompareTo(left.score));
            int count = Mathf.Min(maxCount, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                activeBuildResult.spawnPoints.Add(candidates[i]);
            }
        }

        private static float ScoreSpawnCandidate(
            DungeonSpawnPointCategory category,
            Vector3 candidatePosition,
            Vector3 roomCenter,
            Vector3? playerSpawn)
        {
            float distanceFromCenter = Vector3.Distance(candidatePosition, roomCenter);
            float playerDistance = playerSpawn.HasValue ? Vector3.Distance(candidatePosition, playerSpawn.Value) : 0f;

            return category switch
            {
                DungeonSpawnPointCategory.PlayerSpawn => 100f - distanceFromCenter,
                DungeonSpawnPointCategory.Interactable => 90f - distanceFromCenter,
                DungeonSpawnPointCategory.Chest => 80f - distanceFromCenter,
                DungeonSpawnPointCategory.Shrine => 78f - distanceFromCenter,
                DungeonSpawnPointCategory.Reward => 76f - distanceFromCenter,
                DungeonSpawnPointCategory.EnemyMelee => playerDistance - distanceFromCenter * 0.4f,
                DungeonSpawnPointCategory.EnemyRanged => playerDistance * 1.2f - distanceFromCenter * 0.2f,
                DungeonSpawnPointCategory.EliteEnemy => playerDistance * 1.1f - distanceFromCenter * 0.3f,
                DungeonSpawnPointCategory.TargetDummy => playerDistance - distanceFromCenter * 0.1f,
                _ => 0f
            };
        }

        private void SpawnCombatTestStation(DungeonBuildResult buildResult)
        {
            if (!enableCombatTestStation ||
                buildResult == null ||
                buildResult.floorIndex != 1 ||
                buildResult.isEmergencyDebugBuild ||
                runtimeRoot == null)
            {
                return;
            }

            if (runtimeRoot.Find(CombatTestStationName) != null)
            {
                return;
            }

            List<DungeonSpawnPointRecord> stationSpawns = SelectCombatTestStationSpawns(buildResult, 3, true);
            if (stationSpawns.Count < 3)
            {
                Debug.LogWarning($"Combat test station skipped on floor {buildResult.floorIndex}: only {stationSpawns.Count} valid target dummy spawn points were available.");
                return;
            }

            GameObject stationRoot = new GameObject(CombatTestStationName);
            stationRoot.transform.SetParent(runtimeRoot, false);
            CreateTargetDummy(stationRoot.transform, stationSpawns[0].position, TargetDummyKind.Standard);
            CreateTargetDummy(stationRoot.transform, stationSpawns[1].position, TargetDummyKind.Armored);
            CreateTargetDummy(stationRoot.transform, stationSpawns[2].position, TargetDummyKind.StatusTest);
        }

        private void SpawnEncounterEnemies(DungeonBuildResult buildResult)
        {
            if (!enableEncounterDirector ||
                buildResult == null ||
                buildResult.isEmergencyDebugBuild ||
                runtimeRoot == null)
            {
                return;
            }

            DungeonEncounterDirector director = GetOrCreateEncounterDirector();
            List<Vector3> occupiedPositions = GetCombatTestStationOccupiedPositions();
            DungeonEncounterSummary encounterSummary = director.Spawn(buildResult, occupiedPositions, true);
            if (!string.IsNullOrWhiteSpace(encounterSummary.warning))
            {
                Debug.LogWarning(encounterSummary.warning);
            }

            Debug.Log(encounterSummary.ToDebugString());
        }

        private void SpawnCombatTestEnemies(DungeonBuildResult buildResult)
        {
            if (!enableCombatTestEnemies ||
                buildResult == null ||
                buildResult.floorIndex > 5 ||
                buildResult.isEmergencyDebugBuild ||
                runtimeRoot == null)
            {
                return;
            }

            Transform enemyRoot = GetOrCreateCombatTestEnemiesRoot();
            ClearCombatTestEnemies(enemyRoot, true);
            int requestedCount = GetCombatTestEnemyCount(buildResult.floorIndex);
            List<Vector3> occupiedPositions = GetCombatTestStationOccupiedPositions();
            List<DungeonSpawnPointRecord> spawnPoints = SelectCombatTestEnemySpawns(buildResult, requestedCount, occupiedPositions, true);
            if (spawnPoints.Count == 0)
            {
                Debug.LogWarning($"Combat test enemy skipped on floor {buildResult.floorIndex}: no safe EnemyMelee spawn point was available.");
                return;
            }

            for (int i = 0; i < spawnPoints.Count; i++)
            {
                CreateCombatTestEnemy(enemyRoot, spawnPoints[i].position);
            }

            if (spawnPoints.Count < requestedCount)
            {
                Debug.LogWarning($"Combat test enemy population underfilled on floor {buildResult.floorIndex}: spawned {spawnPoints.Count}/{requestedCount} safe enemies.");
            }

            Debug.Log(GetCombatTestEnemySummary(buildResult, spawnPoints.Count, requestedCount));
        }

        private DungeonEncounterDirector GetOrCreateEncounterDirector()
        {
            if (encounterDirector == null)
            {
                encounterDirector = new DungeonEncounterDirector(runtimeRoot);
            }

            return encounterDirector;
        }

        internal static DungeonSpawnPointRecord SelectCombatTestEnemySpawn(DungeonBuildResult buildResult, IList<Vector3> occupiedPositions = null)
        {
            List<DungeonSpawnPointRecord> selected = SelectCombatTestEnemySpawns(buildResult, 1, occupiedPositions, false);
            return selected.Count > 0 ? selected[0] : null;
        }

        internal static List<DungeonSpawnPointRecord> SelectCombatTestEnemySpawns(
            DungeonBuildResult buildResult,
            int requestedCount,
            IList<Vector3> occupiedPositions = null,
            bool requireReachability = false)
        {
            List<DungeonSpawnPointRecord> selected = new List<DungeonSpawnPointRecord>();
            if (buildResult == null || buildResult.floorIndex > 5 || requestedCount <= 0)
            {
                return selected;
            }

            List<DungeonSpawnPointRecord> candidates = new List<DungeonSpawnPointRecord>();
            for (int i = 0; i < buildResult.spawnPoints.Count; i++)
            {
                DungeonSpawnPointRecord spawnPoint = buildResult.spawnPoints[i];
                if (spawnPoint.category != DungeonSpawnPointCategory.EnemyMelee)
                {
                    continue;
                }

                if (!IsCombatTestEnemySpawnSafe(buildResult, spawnPoint, occupiedPositions, selected, requireReachability))
                {
                    continue;
                }

                candidates.Add(spawnPoint);
            }

            candidates.Sort((left, right) =>
            {
                DungeonRoomBuildRecord leftRoom = buildResult.FindRoom(left.nodeId);
                DungeonRoomBuildRecord rightRoom = buildResult.FindRoom(right.nodeId);
                int leftRank = leftRoom != null && leftRoom.roomType == DungeonNodeKind.Ordinary ? 0 : 1;
                int rightRank = rightRoom != null && rightRoom.roomType == DungeonNodeKind.Ordinary ? 0 : 1;
                int rankCompare = leftRank.CompareTo(rightRank);
                if (rankCompare != 0)
                {
                    return rankCompare;
                }

                int distanceCompare = Vector3.Distance(left.position, buildResult.playerSpawn)
                    .CompareTo(Vector3.Distance(right.position, buildResult.playerSpawn));
                return distanceCompare != 0 ? distanceCompare : right.score.CompareTo(left.score);
            });

            for (int i = 0; i < candidates.Count && selected.Count < requestedCount; i++)
            {
                DungeonSpawnPointRecord candidate = candidates[i];
                if (!IsCombatTestEnemySpawnSafe(buildResult, candidate, occupiedPositions, selected, requireReachability))
                {
                    continue;
                }

                selected.Add(candidate);
            }

            return selected;
        }

        internal static bool IsCombatTestEnemySpawnSafe(
            DungeonBuildResult buildResult,
            DungeonSpawnPointRecord spawnPoint,
            IList<Vector3> occupiedPositions,
            IList<DungeonSpawnPointRecord> selectedSpawnPoints,
            bool requireReachability = false)
        {
            if (buildResult == null || spawnPoint == null || spawnPoint.category != DungeonSpawnPointCategory.EnemyMelee)
            {
                return false;
            }

            DungeonRoomBuildRecord room = buildResult.FindRoom(spawnPoint.nodeId);
            if (room == null || (room.roomType != DungeonNodeKind.Ordinary && room.roomType != DungeonNodeKind.Landmark))
            {
                return false;
            }

            if (Vector3.Distance(spawnPoint.position, buildResult.playerSpawn) < EnemyMeleeSpawnMinimumDistance)
            {
                return false;
            }

            if (IsNearOccupiedCombatTestPosition(spawnPoint.position, occupiedPositions, CombatTestEnemyMinimumSeparation))
            {
                return false;
            }

            if (selectedSpawnPoints != null)
            {
                for (int i = 0; i < selectedSpawnPoints.Count; i++)
                {
                    if (Vector3.Distance(spawnPoint.position, selectedSpawnPoints[i].position) < CombatTestEnemyMinimumSeparation)
                    {
                        return false;
                    }
                }
            }

            Bounds candidateBounds = GetSpawnPointBounds(spawnPoint);
            candidateBounds.Expand(new Vector3(2f, 0f, 2f));
            if (IntersectsBlockedSpawnGeometry(buildResult, room, candidateBounds))
            {
                return false;
            }

            return !requireReachability || HasCombatTestEnemyReachability(buildResult, spawnPoint);
        }

        private static int GetCombatTestEnemyCount(int floorIndex)
        {
            return floorIndex <= 1 ? 3 : 4;
        }

        private static string GetCombatTestEnemySummary(DungeonBuildResult buildResult, int spawnedCount, int requestedCount)
        {
            if (buildResult == null)
            {
                return "Combat test enemy summary unavailable.";
            }

            return
                $"Combat test enemies | Floor {buildResult.floorIndex} | " +
                $"Spawned {spawnedCount}/{requestedCount} | EnemyMelee spawn points {buildResult.GetSpawnPointCount(DungeonSpawnPointCategory.EnemyMelee)} | " +
                $"PlayerNode {buildResult.playerSpawnNodeId}";
        }

        private Transform GetOrCreateCombatTestEnemiesRoot()
        {
            Transform existing = runtimeRoot != null ? runtimeRoot.Find(CombatTestEnemiesName) : null;
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject(CombatTestEnemiesName);
            root.transform.SetParent(runtimeRoot, false);
            return root.transform;
        }

        private static void ClearCombatTestEnemies(Transform enemyRoot, bool immediate)
        {
            if (enemyRoot == null)
            {
                return;
            }

            for (int i = enemyRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = enemyRoot.GetChild(i).gameObject;
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

        private List<Vector3> GetCombatTestStationOccupiedPositions()
        {
            List<Vector3> positions = new List<Vector3>();
            if (runtimeRoot == null)
            {
                return positions;
            }

            Transform stationRoot = runtimeRoot.Find(CombatTestStationName);
            if (stationRoot == null)
            {
                return positions;
            }

            TargetDummyHealth[] dummies = stationRoot.GetComponentsInChildren<TargetDummyHealth>(true);
            for (int i = 0; i < dummies.Length; i++)
            {
                if (dummies[i] != null)
                {
                    positions.Add(dummies[i].transform.position);
                }
            }

            return positions;
        }

        private static bool IsNearOccupiedCombatTestPosition(Vector3 position, IList<Vector3> occupiedPositions, float minimumDistance = 6f)
        {
            if (occupiedPositions == null)
            {
                return false;
            }

            for (int i = 0; i < occupiedPositions.Count; i++)
            {
                if (Vector3.Distance(position, occupiedPositions[i]) < minimumDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private static Bounds GetSpawnPointBounds(DungeonSpawnPointRecord spawnPoint)
        {
            if (spawnPoint.bounds.size.sqrMagnitude > 0.01f)
            {
                return spawnPoint.bounds;
            }

            return new Bounds(spawnPoint.position, new Vector3(SpawnCandidateClearanceRadius, 6f, SpawnCandidateClearanceRadius));
        }

        private static bool IntersectsBlockedSpawnGeometry(DungeonBuildResult buildResult, DungeonRoomBuildRecord room, Bounds candidateBounds)
        {
            for (int i = 0; i < buildResult.interactables.Count; i++)
            {
                DungeonInteractableBuildRecord interactable = buildResult.interactables[i];
                if (interactable.nodeId != room.nodeId)
                {
                    continue;
                }

                Bounds bounds = interactable.bounds;
                bounds.Expand(new Vector3(SpawnInteractableClearance, 0f, SpawnInteractableClearance));
                if (bounds.Intersects(candidateBounds))
                {
                    return true;
                }
            }

            for (int i = 0; i < buildResult.reservedZones.Count; i++)
            {
                DungeonReservedZoneRecord zone = buildResult.reservedZones[i];
                if (zone.ownerId != room.nodeId || zone.kind != "Doorway")
                {
                    continue;
                }

                Bounds bounds = zone.bounds;
                bounds.Expand(new Vector3(SpawnDoorwayClearance, 0f, SpawnDoorwayClearance));
                if (bounds.Intersects(candidateBounds))
                {
                    return true;
                }
            }

            for (int i = 0; i < buildResult.wallSpans.Count; i++)
            {
                DungeonWallSpanRecord wall = buildResult.wallSpans[i];
                if (wall.ownerId != room.nodeId)
                {
                    continue;
                }

                Bounds bounds = wall.bounds;
                bounds.Expand(new Vector3(0.25f, 0f, 0.25f));
                if (bounds.Intersects(candidateBounds))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasCombatTestEnemyReachability(DungeonBuildResult buildResult, DungeonSpawnPointRecord spawnPoint)
        {
            if (buildResult == null || spawnPoint == null)
            {
                return false;
            }

            if (buildResult.graph != null &&
                !string.IsNullOrWhiteSpace(buildResult.playerSpawnNodeId) &&
                !string.IsNullOrWhiteSpace(spawnPoint.nodeId) &&
                !buildResult.graph.HasPath(buildResult.playerSpawnNodeId, spawnPoint.nodeId))
            {
                return false;
            }

            DungeonRoomBuildRecord room = buildResult.FindRoom(spawnPoint.nodeId);
            Vector3 start = room != null
                ? new Vector3(room.bounds.center.x, spawnPoint.position.y + 0.85f, room.bounds.center.z)
                : buildResult.playerSpawn + Vector3.up * 0.85f;
            Vector3 target = spawnPoint.position + Vector3.up * 0.85f;
            if ((target - start).sqrMagnitude <= 0.01f)
            {
                return true;
            }

            return !Physics.Linecast(start, target, out _, PlayerWeaponController.DefaultWeaponRaycastMask, QueryTriggerInteraction.Ignore);
        }

        internal static List<DungeonSpawnPointRecord> SelectCombatTestStationSpawns(DungeonBuildResult buildResult, int requestedCount, bool requireLineOfSight = false)
        {
            List<DungeonSpawnPointRecord> selected = new List<DungeonSpawnPointRecord>();
            if (buildResult == null || buildResult.floorIndex != 1 || requestedCount <= 0)
            {
                return selected;
            }

            if (TryCollectStationSpawnsForRoom(buildResult, buildResult.playerSpawnNodeId, requestedCount, selected, requireLineOfSight))
            {
                return selected;
            }

            List<DungeonRoomBuildRecord> rooms = new List<DungeonRoomBuildRecord>();
            for (int i = 0; i < buildResult.rooms.Count; i++)
            {
                DungeonRoomBuildRecord room = buildResult.rooms[i];
                if (room.roomType == DungeonNodeKind.Ordinary || room.roomType == DungeonNodeKind.Landmark)
                {
                    rooms.Add(room);
                }
            }

            rooms.Sort((left, right) =>
                Vector3.Distance(left.bounds.center, buildResult.playerSpawn)
                    .CompareTo(Vector3.Distance(right.bounds.center, buildResult.playerSpawn)));

            for (int i = 0; i < rooms.Count; i++)
            {
                selected.Clear();
                if (TryCollectStationSpawnsForRoom(buildResult, rooms[i].nodeId, requestedCount, selected, requireLineOfSight))
                {
                    return selected;
                }
            }

            selected.Clear();
            List<DungeonSpawnPointRecord> fallbackSpawns = new List<DungeonSpawnPointRecord>();
            for (int i = 0; i < buildResult.spawnPoints.Count; i++)
            {
                DungeonSpawnPointRecord spawnPoint = buildResult.spawnPoints[i];
                if (spawnPoint.category != DungeonSpawnPointCategory.TargetDummy ||
                    Vector3.Distance(spawnPoint.position, buildResult.playerSpawn) < TargetDummySpawnMinimumDistance ||
                    (requireLineOfSight && !HasCombatTestStationLineOfSight(buildResult, spawnPoint)))
                {
                    continue;
                }

                fallbackSpawns.Add(spawnPoint);
            }

            fallbackSpawns.Sort((left, right) =>
            {
                int distanceCompare = Vector3.Distance(left.position, buildResult.playerSpawn)
                    .CompareTo(Vector3.Distance(right.position, buildResult.playerSpawn));
                return distanceCompare != 0 ? distanceCompare : right.score.CompareTo(left.score);
            });

            int count = Mathf.Min(requestedCount, fallbackSpawns.Count);
            for (int i = 0; i < count; i++)
            {
                selected.Add(fallbackSpawns[i]);
            }

            return selected;
        }

        private static bool TryCollectStationSpawnsForRoom(
            DungeonBuildResult buildResult,
            string roomId,
            int requestedCount,
            List<DungeonSpawnPointRecord> selected,
            bool requireLineOfSight)
        {
            if (buildResult == null || selected == null || string.IsNullOrWhiteSpace(roomId))
            {
                return false;
            }

            selected.Clear();
            List<DungeonSpawnPointRecord> roomSpawns = buildResult.GetSpawnPoints(roomId, DungeonSpawnPointCategory.TargetDummy);
            for (int i = 0; i < roomSpawns.Count; i++)
            {
                DungeonSpawnPointRecord spawnPoint = roomSpawns[i];
                if (Vector3.Distance(spawnPoint.position, buildResult.playerSpawn) < TargetDummySpawnMinimumDistance)
                {
                    continue;
                }

                if (requireLineOfSight && !HasCombatTestStationLineOfSight(buildResult, spawnPoint))
                {
                    continue;
                }

                selected.Add(spawnPoint);
                if (selected.Count >= requestedCount)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasCombatTestStationLineOfSight(DungeonBuildResult buildResult, DungeonSpawnPointRecord spawnPoint)
        {
            if (buildResult == null || spawnPoint == null)
            {
                return false;
            }

            Vector3 start = GetCombatTestStationApproachPoint(buildResult, spawnPoint);
            Vector3 target = spawnPoint.position + Vector3.up * 0.35f;
            if ((target - start).sqrMagnitude <= 0.01f)
            {
                return true;
            }

            return !Physics.Linecast(start, target, out _, PlayerWeaponController.DefaultWeaponRaycastMask, QueryTriggerInteraction.Ignore);
        }

        internal static Vector3 GetCombatTestStationApproachPoint(DungeonBuildResult buildResult, DungeonSpawnPointRecord spawnPoint)
        {
            DungeonRoomBuildRecord room = buildResult != null && spawnPoint != null ? buildResult.FindRoom(spawnPoint.nodeId) : null;
            if (room != null)
            {
                return new Vector3(room.bounds.center.x, spawnPoint.position.y + 0.85f, room.bounds.center.z);
            }

            return buildResult != null ? buildResult.playerSpawn + Vector3.up * 0.85f : Vector3.up * PlayerSpawnHeight;
        }

        internal static GameObject CreateTargetDummy(Transform parent, Vector3 position, TargetDummyKind kind)
        {
            GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = $"{kind}Dummy";
            dummy.transform.SetParent(parent, true);
            dummy.transform.position = position;
            dummy.transform.localScale = new Vector3(1.4f, 1.7f, 1.4f);

            TargetDummyHealth health = dummy.AddComponent<TargetDummyHealth>();
            health.Configure(kind);

            GameObject labelObject = new GameObject("Label", typeof(TextMesh));
            labelObject.transform.SetParent(dummy.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            TextMesh label = labelObject.GetComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.22f;
            label.fontSize = 42;
            label.color = Color.white;
            health.SetStatusText(label);
            int defaultLayer = LayerMask.NameToLayer("Default");
            SetLayerRecursively(dummy, defaultLayer >= 0 ? defaultLayer : 0);
            EnsureTargetDummyColliderSanity(dummy);
            return dummy;
        }

        internal static GameObject CreateCombatTestEnemy(Transform parent, Vector3 position)
        {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "SandboxMeleeEnemy";
            enemy.transform.SetParent(parent, true);
            enemy.transform.position = position;
            enemy.transform.localScale = new Vector3(1.25f, 1.55f, 1.25f);

            CapsuleCollider capsuleCollider = enemy.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(capsuleCollider);
                }
                else
                {
                    DestroyImmediate(capsuleCollider);
                }
            }

            CharacterController characterController = enemy.AddComponent<CharacterController>();
            characterController.height = 3.1f;
            characterController.radius = 0.55f;
            characterController.center = Vector3.zero;

            EnemyHealth health = enemy.AddComponent<EnemyHealth>();
            health.Configure(50f, new Color(0.72f, 0.28f, 0.22f, 1f));
            SimpleMeleeEnemyController melee = enemy.AddComponent<SimpleMeleeEnemyController>();
            melee.ConfigureHomeRoom(string.Empty, new Bounds(position, new Vector3(12f, 4f, 12f)), new[] { position });

            int defaultLayer = LayerMask.NameToLayer("Default");
            SetLayerRecursively(enemy, defaultLayer >= 0 ? defaultLayer : 0);
            return enemy;
        }

        internal static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.layer = layer;
            Transform rootTransform = root.transform;
            for (int i = 0; i < rootTransform.childCount; i++)
            {
                SetLayerRecursively(rootTransform.GetChild(i).gameObject, layer);
            }
        }

        private static void EnsureTargetDummyColliderSanity(GameObject dummy)
        {
            if (dummy == null)
            {
                return;
            }

            Collider[] colliders = dummy.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null)
                {
                    continue;
                }

                colliders[i].enabled = true;
                colliders[i].isTrigger = false;
            }
        }

        private static void EnsurePlayerCombatComponents(FirstPersonController player)
        {
            if (player == null)
            {
                return;
            }

            if (player.GetComponent<PlayerHealth>() == null)
            {
                player.gameObject.AddComponent<PlayerHealth>();
            }

            if (player.GetComponent<PlayerDeathReturnController>() == null)
            {
                player.gameObject.AddComponent<PlayerDeathReturnController>();
            }

            if (player.GetComponent<PlayerWeaponController>() == null)
            {
                player.gameObject.AddComponent<PlayerWeaponController>();
            }

            if (player.GetComponent<PlayerPistolWhipController>() == null)
            {
                player.gameObject.AddComponent<PlayerPistolWhipController>();
            }
        }

        private bool IsSpawnPointValid(
            DungeonRoomBuildRecord room,
            Vector3 candidatePosition,
            float minimumDistanceFromPlayer,
            bool avoidDoorways,
            bool avoidInteractables,
            bool avoidPlayerSpawn,
            Vector3? playerSpawn)
        {
            if (!IsWithinRoomInterior(room.bounds, candidatePosition, SpawnWallMargin))
            {
                return false;
            }

            Bounds candidateBounds = new Bounds(candidatePosition, new Vector3(SpawnCandidateClearanceRadius, 6f, SpawnCandidateClearanceRadius));

            if (avoidPlayerSpawn && playerSpawn.HasValue && Vector3.Distance(candidatePosition, playerSpawn.Value) < minimumDistanceFromPlayer)
            {
                return false;
            }

            if (avoidInteractables)
            {
                for (int interactableIndex = 0; interactableIndex < activeBuildResult.interactables.Count; interactableIndex++)
                {
                    DungeonInteractableBuildRecord interactable = activeBuildResult.interactables[interactableIndex];
                    if (interactable.nodeId != room.nodeId)
                    {
                        continue;
                    }

                    Bounds interactableBounds = interactable.bounds;
                    interactableBounds.Expand(new Vector3(SpawnInteractableClearance, 0f, SpawnInteractableClearance));
                    if (interactableBounds.Intersects(candidateBounds))
                    {
                        return false;
                    }
                }
            }

            if (avoidDoorways)
            {
                for (int zoneIndex = 0; zoneIndex < activeBuildResult.reservedZones.Count; zoneIndex++)
                {
                    DungeonReservedZoneRecord zone = activeBuildResult.reservedZones[zoneIndex];
                    if (zone.ownerId != room.nodeId || zone.kind != "Doorway")
                    {
                        continue;
                    }

                    Bounds zoneBounds = zone.bounds;
                    zoneBounds.Expand(new Vector3(SpawnDoorwayClearance, 0f, SpawnDoorwayClearance));
                    if (zoneBounds.Intersects(candidateBounds))
                    {
                        return false;
                    }
                }
            }

            for (int wallIndex = 0; wallIndex < activeBuildResult.wallSpans.Count; wallIndex++)
            {
                DungeonWallSpanRecord wall = activeBuildResult.wallSpans[wallIndex];
                if (wall.ownerId != room.nodeId)
                {
                    continue;
                }

                Bounds wallBounds = wall.bounds;
                wallBounds.Expand(new Vector3(0.25f, 0f, 0.25f));
                if (wallBounds.Intersects(candidateBounds))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSpawnCellConnectedToCenter(DungeonRoomBuildRecord room, Vector2Int startCell)
        {
            HashSet<Vector2Int> floorCells = new HashSet<Vector2Int>(room.floorCells);
            if (!floorCells.Contains(startCell) || !floorCells.Contains(room.centerCell))
            {
                return false;
            }

            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int> { startCell };
            frontier.Enqueue(startCell);

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                if (current == room.centerCell)
                {
                    return true;
                }

                for (int i = 0; i < 4; i++)
                {
                    Vector2Int next = current + CardinalDirection(i);
                    if (!floorCells.Contains(next) || !visited.Add(next))
                    {
                        continue;
                    }

                    frontier.Enqueue(next);
                }
            }

            return false;
        }

        private static void UpdateBuildMetrics(DungeonBuildResult buildResult)
        {
            if (buildResult == null)
            {
                return;
            }

            float roomFootprintTotal = 0f;
            float largestRoomFootprint = 0f;
            for (int i = 0; i < buildResult.rooms.Count; i++)
            {
                roomFootprintTotal += buildResult.rooms[i].footprintArea;
                largestRoomFootprint = Mathf.Max(largestRoomFootprint, buildResult.rooms[i].footprintArea);
            }

            float corridorLengthTotal = 0f;
            float maxCorridorLength = 0f;
            int corridorsOverTarget = 0;
            for (int i = 0; i < buildResult.corridors.Count; i++)
            {
                float length = buildResult.corridors[i].length;
                corridorLengthTotal += length;
                maxCorridorLength = Mathf.Max(maxCorridorLength, length);
                if (length > CorridorTargetLength)
                {
                    corridorsOverTarget++;
                }
            }

            buildResult.averageRoomFootprint = buildResult.rooms.Count > 0 ? roomFootprintTotal / buildResult.rooms.Count : 0f;
            buildResult.largestRoomFootprint = largestRoomFootprint;
            buildResult.averageCorridorLength = buildResult.corridors.Count > 0 ? corridorLengthTotal / buildResult.corridors.Count : 0f;
            buildResult.maxCorridorLength = maxCorridorLength;
            buildResult.percentCorridorsOverTarget = buildResult.corridors.Count > 0
                ? corridorsOverTarget * 100f / buildResult.corridors.Count
                : 0f;
        }

        private bool TryFindSafeSpawnPosition(DungeonRoomBuildRecord room, string nodeId, out Vector3 spawnPosition)
        {
            Vector3 roomCenter = new Vector3(room.bounds.center.x, PlayerSpawnHeight, room.bounds.center.z);
            float offsetDistance = Mathf.Clamp(Mathf.Min(room.bounds.extents.x, room.bounds.extents.z) * 0.35f, 8f, 14f);
            Vector3 diagonalOffset = new Vector3(offsetDistance, 0f, offsetDistance);

            Vector3[] candidateOffsets =
            {
                new Vector3(0f, 0f, -offsetDistance),
                new Vector3(offsetDistance, 0f, 0f),
                new Vector3(0f, 0f, offsetDistance),
                new Vector3(-offsetDistance, 0f, 0f),
                diagonalOffset,
                new Vector3(-diagonalOffset.x, 0f, diagonalOffset.z),
                new Vector3(diagonalOffset.x, 0f, -diagonalOffset.z),
                -diagonalOffset,
                Vector3.zero
            };

            for (int i = 0; i < candidateOffsets.Length; i++)
            {
                Vector3 candidate = roomCenter + candidateOffsets[i];
                if (IsSpawnCandidateClear(room, nodeId, candidate))
                {
                    spawnPosition = candidate;
                    return true;
                }
            }

            spawnPosition = roomCenter;
            return false;
        }

        private bool IsSpawnCandidateClear(DungeonRoomBuildRecord room, string nodeId, Vector3 candidate)
        {
            if (!IsWithinRoomInterior(room.bounds, candidate, SpawnWallMargin))
            {
                return false;
            }

            Bounds candidateBounds = new Bounds(candidate, new Vector3(SpawnCandidateRadius, 6f, SpawnCandidateRadius));
            if (activeBuildResult == null)
            {
                return true;
            }

            for (int i = 0; i < activeBuildResult.interactables.Count; i++)
            {
                DungeonInteractableBuildRecord interactable = activeBuildResult.interactables[i];
                if (interactable.nodeId != nodeId)
                {
                    continue;
                }

                Bounds interactableBounds = interactable.bounds;
                interactableBounds.Expand(new Vector3(SpawnInteractableClearance, 0f, SpawnInteractableClearance));
                if (interactableBounds.Intersects(candidateBounds))
                {
                    return false;
                }
            }

            for (int i = 0; i < activeBuildResult.reservedZones.Count; i++)
            {
                DungeonReservedZoneRecord zone = activeBuildResult.reservedZones[i];
                if (zone.ownerId != nodeId || zone.kind != "Doorway")
                {
                    continue;
                }

                Bounds zoneBounds = zone.bounds;
                zoneBounds.Expand(new Vector3(SpawnDoorwayClearance, 0f, SpawnDoorwayClearance));
                if (zoneBounds.Intersects(candidateBounds))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsWithinRoomInterior(Bounds roomBounds, Vector3 candidate, float margin)
        {
            return candidate.x >= roomBounds.min.x + margin &&
                   candidate.x <= roomBounds.max.x - margin &&
                   candidate.z >= roomBounds.min.z + margin &&
                   candidate.z <= roomBounds.max.z - margin;
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
                origin = roomRoot.transform.position,
                centerCell = DungeonRoomTemplateLibrary.GetCenterCell(node.roomTemplate, node.rotationQuarterTurns),
                bounds = GetRoomBounds(roomRoot.transform.position, floorCells)
            };
            roomRecord.footprintArea = roomRecord.bounds.size.x * roomRecord.bounds.size.z;
            roomRecord.floorCells.AddRange(floorCells);
            RoomPurposeDefinition purpose = RoomPurposeCatalog.Choose(node.nodeKind, activeBuildResult != null ? activeBuildResult.floorIndex : 1, activeBuildResult != null ? activeBuildResult.seed : 0, node.nodeId);
            if (purpose != null)
            {
                roomRecord.purposeId = purpose.purposeId;
                roomRecord.purposeDisplayName = purpose.displayName;
                roomRecord.purposeIcon = purpose.minimapIcon;
            }
            activeBuildResult?.rooms.Add(roomRecord);

            Dictionary<Vector2Int, float> doorwayWidths = GetDoorwayWidths(node, graph);
            CreateMergedFloors(roomRoot.transform, floorCells, purpose != null ? purpose.color : GetFloorColor(node.nodeKind));
            roomRecord.hasFloor = floorCells.Count > 0;
            CreateRoomWalls(roomRoot.transform, node, graph, floorCells, doorwayWidths, roomRecord);

            CreateInteriorFeature(roomRoot.transform, node, floorCells);
            CreateNodeInteractables(roomRoot.transform, node, purpose);
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
            routePoints = ExpandRouteEndpointsIntoRooms(routePoints, direction2D, CorridorRoomOverlap);
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

                bool trimStart = i == 1;
                bool trimEnd = i == routePoints.Count - 1;
                CreateCorridorSegment(corridorRoot.transform, edgeKey, a.nodeId, b.nodeId, segmentIndex++, corridorWidth, segmentStart, segmentEnd, trimStart, trimEnd);
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
            Vector3 end,
            bool trimVisualStart,
            bool trimVisualEnd)
        {
            Vector3 midpoint = (start + end) * 0.5f;
            Vector3 delta = end - start;
            bool horizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.z);
            float corridorLength = Mathf.Max(CellSize * 0.9f, horizontal ? Mathf.Abs(delta.x) : Mathf.Abs(delta.z));
            GetVisualCorridorFloor(start, end, trimVisualStart, trimVisualEnd, out Vector3 visualMidpoint, out float visualLength);

            GameObject segmentRoot = new GameObject($"Corridor_{fromNodeId}_To_{toNodeId}_Segment_{segmentIndex}");
            segmentRoot.transform.SetParent(corridorRoot, false);

            Vector3 visualFloorScale = horizontal
                ? new Vector3(visualLength, FloorThickness, corridorWidth)
                : new Vector3(corridorWidth, FloorThickness, visualLength);
            Vector3 collisionFloorScale = horizontal
                ? new Vector3(corridorLength, FloorThickness, corridorWidth)
                : new Vector3(corridorWidth, FloorThickness, corridorLength);
            float corridorOuterWidth = GetCorridorOuterWidth(corridorWidth);
            Vector3 outerBoundsSize = horizontal
                ? new Vector3(corridorLength, WallHeight, corridorOuterWidth)
                : new Vector3(corridorOuterWidth, WallHeight, corridorLength);
            Vector3 outerBoundsCenter = midpoint + Vector3.up * (WallHeight * 0.5f - FloorThickness * 0.5f);

            GameObject visualFloor = CreatePrimitive(
                "Floor",
                segmentRoot.transform,
                visualMidpoint + Vector3.down * (FloorThickness * 0.5f) + Vector3.up * CorridorVisualFloorYOffset,
                visualFloorScale,
                new Color(0.19f, 0.18f, 0.17f));
            SetColliderEnabled(visualFloor, false);

            GameObject collisionFloor = CreatePrimitive(
                "FloorCollision",
                segmentRoot.transform,
                midpoint + Vector3.down * (FloorThickness * 0.5f),
                collisionFloorScale,
                new Color(0.19f, 0.18f, 0.17f));
            SetRendererEnabled(collisionFloor, false);
            Bounds collisionBounds = GetBounds(collisionFloor.transform.position, collisionFloorScale);
            activeBuildResult?.corridors.Add(new DungeonCorridorBuildRecord
            {
                edgeKey = edgeKey,
                fromNodeId = fromNodeId,
                toNodeId = toNodeId,
                segmentIndex = segmentIndex,
                start = start,
                end = end,
                bounds = collisionBounds,
                outerBounds = GetBounds(outerBoundsCenter, outerBoundsSize),
                horizontal = horizontal,
                length = corridorLength,
                width = corridorWidth,
                isSecretCorridor = Mathf.Abs(corridorWidth - SecretCorridorWidth) <= 0.01f
            });
            activeBuildResult?.reservedZones.Add(new DungeonReservedZoneRecord
            {
                ownerId = edgeKey,
                kind = "Corridor",
                bounds = GetBounds(midpoint + Vector3.up * (CorridorZoneHeight * 0.5f), new Vector3(collisionFloorScale.x, CorridorZoneHeight, collisionFloorScale.z))
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
            bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
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

            if (ctrlHeld && Input.GetKeyDown(KeyCode.F7))
            {
                if (shiftHeld)
                {
                    KillEncounterOrTestEnemies();
                }
                else
                {
                    RespawnEncounterOrTestEnemies();
                }

                return;
            }

            if (ctrlHeld && Input.GetKeyDown(KeyCode.F8))
            {
                PrintEncounterOrTestEnemySummary();
                return;
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                DungeonBuildResult visibleBuild = GetVisibleBuildResult();
                TryTeleportToNode(visibleBuild != null ? visibleBuild.entryNodeId : string.Empty);
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                DungeonBuildResult visibleBuild = GetVisibleBuildResult();
                TryTeleportToNode(visibleBuild != null ? visibleBuild.transitDownNodeId : string.Empty);
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                DungeonBuildResult visibleBuild = GetVisibleBuildResult();
                TryTeleportToNode(visibleBuild != null ? visibleBuild.landmarkNodeId : string.Empty);
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                DungeonBuildResult visibleBuild = GetVisibleBuildResult();
                TryTeleportToNode(visibleBuild != null ? visibleBuild.secretNodeId : string.Empty);
            }

            if (Input.GetKeyDown(KeyCode.F10) && GameBootstrap.Instance != null)
            {
                GameBootstrap.Instance.RunService?.SaveActiveFloorState();
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

        private void RespawnEncounterOrTestEnemies()
        {
            if (enableEncounterDirector)
            {
                RespawnEncounterEnemies();
                if (enableCombatTestEnemies && allowCombatTestEnemiesWithEncounters)
                {
                    RespawnCombatTestEnemies();
                }

                return;
            }

            RespawnCombatTestEnemies();
        }

        private void KillEncounterOrTestEnemies()
        {
            if (enableEncounterDirector)
            {
                KillEncounterEnemies();
                if (enableCombatTestEnemies && allowCombatTestEnemiesWithEncounters)
                {
                    KillCombatTestEnemies();
                }

                return;
            }

            KillCombatTestEnemies();
        }

        private void PrintEncounterOrTestEnemySummary()
        {
            if (enableEncounterDirector && encounterDirector != null)
            {
                Debug.Log(encounterDirector.Summary.ToDebugString());
                return;
            }

            PrintCombatTestEnemySummary();
        }

        private void RespawnEncounterEnemies()
        {
            DungeonBuildResult visibleBuild = GetVisibleBuildResult();
            if (visibleBuild == null)
            {
                Debug.LogWarning("Cannot respawn encounter enemies: dungeon build not ready.");
                return;
            }

            SpawnEncounterEnemies(visibleBuild);
        }

        private void KillEncounterEnemies()
        {
            if (encounterDirector == null)
            {
                Debug.Log("No encounter enemies to clear.");
                return;
            }

            int activeCount = encounterDirector.Summary.livingEnemyCount;
            encounterDirector.Clear(true);
            Debug.Log($"Cleared {activeCount} encounter enemies without drops or progression.");
        }

        private void RespawnCombatTestEnemies()
        {
            DungeonBuildResult visibleBuild = GetVisibleBuildResult();
            if (visibleBuild == null)
            {
                Debug.LogWarning("Cannot respawn combat test enemies: dungeon build not ready.");
                return;
            }

            SpawnCombatTestEnemies(visibleBuild);
        }

        private void KillCombatTestEnemies()
        {
            Transform enemyRoot = runtimeRoot != null ? runtimeRoot.Find(CombatTestEnemiesName) : null;
            if (enemyRoot == null)
            {
                Debug.Log("No combat test enemies to kill.");
                return;
            }

            EnemyHealth[] enemies = enemyRoot.GetComponentsInChildren<EnemyHealth>(true);
            ClearCombatTestEnemies(enemyRoot, true);
            Debug.Log($"Cleared {enemies.Length} combat test enemies without drops or progression.");
        }

        private void PrintCombatTestEnemySummary()
        {
            DungeonBuildResult visibleBuild = GetVisibleBuildResult();
            int activeCount = CountCombatTestEnemies();
            int requested = visibleBuild != null && visibleBuild.floorIndex <= 5 ? GetCombatTestEnemyCount(visibleBuild.floorIndex) : 0;
            Debug.Log(visibleBuild != null
                ? GetCombatTestEnemySummary(visibleBuild, activeCount, requested)
                : $"Combat test enemy summary unavailable. Active={activeCount}");
        }

        private int CountCombatTestEnemies()
        {
            Transform enemyRoot = runtimeRoot != null ? runtimeRoot.Find(CombatTestEnemiesName) : null;
            return enemyRoot != null ? enemyRoot.GetComponentsInChildren<EnemyHealth>(true).Length : 0;
        }

        private void TryTeleportToNode(string nodeId)
        {
            DungeonBuildResult visibleBuild = GetVisibleBuildResult();
            if (string.IsNullOrWhiteSpace(nodeId) || visibleBuild == null)
            {
                return;
            }

            DungeonRoomBuildRecord room = visibleBuild.FindRoom(nodeId);
            FirstPersonController player = FindAnyObjectByType<FirstPersonController>();
            if (room == null || player == null)
            {
                return;
            }

            Vector3 target = new Vector3(room.bounds.center.x, 3.5f, room.bounds.center.z);
            player.WarpTo(target);
        }

        private void DrawBuildGizmos()
        {
            DungeonBuildResult visibleBuild = GetVisibleBuildResult();
            if (visibleBuild == null)
            {
                return;
            }

            for (int i = 0; i < visibleBuild.rooms.Count; i++)
            {
                Gizmos.color = GetGizmoColor(visibleBuild.rooms[i].roomType);
                Gizmos.DrawWireCube(visibleBuild.rooms[i].bounds.center, visibleBuild.rooms[i].bounds.size);
            }

            Gizmos.color = Color.yellow;
            for (int i = 0; i < visibleBuild.corridors.Count; i++)
            {
                Gizmos.DrawWireCube(visibleBuild.corridors[i].bounds.center, visibleBuild.corridors[i].bounds.size);
                Gizmos.DrawLine(visibleBuild.corridors[i].start + Vector3.up, visibleBuild.corridors[i].end + Vector3.up);
                Gizmos.DrawSphere(visibleBuild.corridors[i].start + Vector3.up, 0.35f);
                Gizmos.DrawSphere(visibleBuild.corridors[i].end + Vector3.up, 0.35f);
            }

            Gizmos.color = new Color(0.2f, 0.75f, 1f);
            for (int i = 0; i < visibleBuild.corridors.Count; i++)
            {
                Gizmos.DrawWireCube(visibleBuild.corridors[i].outerBounds.center, visibleBuild.corridors[i].outerBounds.size);
            }

            Gizmos.color = new Color(0.25f, 1f, 0.35f);
            for (int i = 0; i < visibleBuild.doorOpenings.Count; i++)
            {
                Gizmos.DrawWireCube(visibleBuild.doorOpenings[i].visualBounds.center, visibleBuild.doorOpenings[i].visualBounds.size);
            }

            Gizmos.color = new Color(0.85f, 0.25f, 1f);
            for (int i = 0; i < visibleBuild.doorOpenings.Count; i++)
            {
                Gizmos.DrawWireCube(visibleBuild.doorOpenings[i].bounds.center, visibleBuild.doorOpenings[i].bounds.size);
            }

            Gizmos.color = new Color(1f, 0.5f, 0.1f);
            for (int i = 0; i < visibleBuild.reservedZones.Count; i++)
            {
                Gizmos.DrawWireCube(visibleBuild.reservedZones[i].bounds.center, visibleBuild.reservedZones[i].bounds.size);
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(visibleBuild.playerSpawn, 1.1f);

            for (int i = 0; i < visibleBuild.spawnPoints.Count; i++)
            {
                DungeonSpawnPointRecord spawnPoint = visibleBuild.spawnPoints[i];
                Gizmos.color = GetSpawnPointGizmoColor(spawnPoint.category);
                Gizmos.DrawSphere(spawnPoint.position, 0.6f);
            }
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
                stairDiscovered = source != null && source.stairDiscovered,
                graphLayoutSignature = source != null ? source.graphLayoutSignature : string.Empty,
                layoutShapeSignature = source != null ? source.layoutShapeSignature : string.Empty,
                visitedRoomIds = source != null && source.visitedRoomIds != null ? new List<string>(source.visitedRoomIds) : new List<string>(),
                discoveredRoomIds = source != null && source.discoveredRoomIds != null ? new List<string>(source.discoveredRoomIds) : new List<string>(),
                discoveredCorridorIds = source != null && source.discoveredCorridorIds != null ? new List<string>(source.discoveredCorridorIds) : new List<string>(),
                claimedRoomPurposeIds = source != null && source.claimedRoomPurposeIds != null ? new List<string>(source.claimedRoomPurposeIds) : new List<string>(),
                lastKnownPlayerRoomId = source != null ? source.lastKnownPlayerRoomId : string.Empty,
                knownStairRoomId = source != null ? source.knownStairRoomId : string.Empty
            };
            clone.Normalize(clone.floorIndex, floorSeed);
            return clone;
        }

        internal static bool ShouldRejectRepeatedLayout(RunState run, DungeonBuildResult buildResult, bool canTryAnotherNormalAttempt)
        {
            if (!canTryAnotherNormalAttempt ||
                run == null ||
                buildResult == null ||
                buildResult.requestedFallback ||
                string.IsNullOrWhiteSpace(buildResult.layoutShapeSignature) ||
                run.visitedFloors == null)
            {
                return false;
            }

            int currentFloor = Mathf.Max(1, buildResult.floorIndex);
            for (int i = 0; i < run.visitedFloors.Count; i++)
            {
                FloorState visited = run.visitedFloors[i];
                if (visited == null ||
                    visited.floorIndex >= currentFloor ||
                    currentFloor - visited.floorIndex > 3 ||
                    string.IsNullOrWhiteSpace(visited.layoutShapeSignature))
                {
                    continue;
                }

                if (string.Equals(visited.layoutShapeSignature, buildResult.layoutShapeSignature, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearRuntimeRoot(bool immediate)
        {
            if (runtimeRoot == null)
            {
                return;
            }

            encounterDirector?.Clear(immediate);

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

        private DungeonBuildResult GetVisibleBuildResult()
        {
            return emergencyDebugBuildResult ?? currentBuildResult;
        }

        private static void FinalizeValidation(DungeonBuildResult buildResult, DungeonValidationReport report, bool isEmergencyDebugBuild)
        {
            if (buildResult == null)
            {
                return;
            }

            buildResult.validationPassed = report != null && report.IsValid;
            buildResult.validationFailureCount = report != null ? report.failures.Count : 0;
            buildResult.validationWarningCount = report != null ? report.warnings.Count : 0;
            buildResult.isEmergencyDebugBuild = isEmergencyDebugBuild;
            buildResult.validationSummary = report != null
                ? report.ToSummaryString(buildResult)
                : ComposeBuildSummary(buildResult);
        }

        private static GraphValidationReport CreateFallbackGraphReport(FloorState floorState, DungeonLayoutGraph graph)
        {
            int floorIndex = floorState != null ? Mathf.Max(1, floorState.floorIndex) : 1;
            int seed = floorState != null ? floorState.floorSeed : 0;
            return new GraphValidationReport
            {
                floorIndex = floorIndex,
                seed = seed,
                attemptCount = 1,
                layoutSignature = DungeonLayoutSignatureUtility.BuildSignature(graph, floorIndex, seed),
                layoutShapeSignature = DungeonLayoutSignatureUtility.BuildShapeSignature(graph)
            };
        }

        private static void LogFallbackDiagnostics(
            FloorState fallbackFloorState,
            List<GraphValidationReport> failedGraphReports,
            List<string> renderedValidationFailures)
        {
            int floorIndex = fallbackFloorState != null ? Mathf.Max(1, fallbackFloorState.floorIndex) : 1;
            int seed = fallbackFloorState != null ? fallbackFloorState.floorSeed : 0;
            string graphReasons = BuildTopGraphFailureSummary(failedGraphReports, 3);
            string attemptSeeds = BuildAttemptSeedSummary(failedGraphReports);
            string bestFailed = BuildBestFailedGraphSummary(failedGraphReports);
            string renderedReasons = renderedValidationFailures != null && renderedValidationFailures.Count > 0
                ? string.Join(" || ", renderedValidationFailures)
                : "None";

            Debug.LogError(
                $"REQUESTED FALLBACK - NORMAL GENERATION FAILED | Floor {floorIndex} | Seed {seed} | " +
                $"NormalAttemptSeeds {attemptSeeds} | TopGraphFailures {graphReasons} | BestFailedGraph {bestFailed} | " +
                $"RenderedValidationFailures {renderedReasons}");
        }

        private static string BuildAttemptSeedSummary(List<GraphValidationReport> reports)
        {
            if (reports == null || reports.Count == 0)
            {
                return "None";
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < reports.Count; i++)
            {
                parts.Add(reports[i].GetAttemptSeedsSummary());
            }

            return string.Join(" | ", parts);
        }

        private static string BuildBestFailedGraphSummary(List<GraphValidationReport> reports)
        {
            if (reports == null || reports.Count == 0)
            {
                return "None";
            }

            for (int i = reports.Count - 1; i >= 0; i--)
            {
                string summary = reports[i].GetBestFailedAttemptSummary();
                if (!string.IsNullOrWhiteSpace(summary) && summary != "None")
                {
                    return summary;
                }
            }

            return "None";
        }

        private static string BuildTopGraphFailureSummary(List<GraphValidationReport> reports, int maxReasons)
        {
            if (reports == null || reports.Count == 0)
            {
                return "None";
            }

            Dictionary<string, int> counts = new Dictionary<string, int>();
            for (int reportIndex = 0; reportIndex < reports.Count; reportIndex++)
            {
                GraphValidationReport report = reports[reportIndex];
                for (int attemptIndex = 0; attemptIndex < report.attempts.Count; attemptIndex++)
                {
                    List<string> failures = report.attempts[attemptIndex].failures;
                    for (int failureIndex = 0; failureIndex < failures.Count; failureIndex++)
                    {
                        string reason = string.IsNullOrWhiteSpace(failures[failureIndex]) ? "Unknown reason." : failures[failureIndex];
                        if (!counts.TryAdd(reason, 1))
                        {
                            counts[reason]++;
                        }
                    }
                }
            }

            if (counts.Count == 0)
            {
                return "None";
            }

            List<KeyValuePair<string, int>> ordered = new List<KeyValuePair<string, int>>(counts);
            ordered.Sort((left, right) =>
            {
                int countCompare = right.Value.CompareTo(left.Value);
                return countCompare != 0 ? countCompare : string.CompareOrdinal(left.Key, right.Key);
            });

            List<string> parts = new List<string>();
            int limit = Mathf.Min(maxReasons, ordered.Count);
            for (int i = 0; i < limit; i++)
            {
                parts.Add($"{ordered[i].Key} x{ordered[i].Value}");
            }

            return string.Join("; ", parts);
        }

        private static string ComposeBuildSummary(DungeonBuildResult buildResult)
        {
            if (buildResult == null)
            {
                return "Dungeon build not ready.";
            }

            string validationState = buildResult.validationPassed ? "VALID" : "UNKNOWN";
            return $"{buildResult.GetBuildModeLabel()} | Dungeon build {validationState} | Floor {buildResult.floorIndex} | Seed {buildResult.seed} | Attempt {Mathf.Max(1, buildResult.attemptNumber)}/{Mathf.Max(1, buildResult.attemptCount)} | RequestedFallback {(buildResult.requestedFallback ? "Yes" : "No")} | GeneratorFallback {(buildResult.generatorReturnedFallbackGraph ? "Yes" : "No")} | Emergency {(buildResult.isEmergencyDebugBuild ? "Yes" : "No")} | Rooms {buildResult.rooms.Count} | AvgRoom {buildResult.averageRoomFootprint:0.#} | LargestRoom {buildResult.largestRoomFootprint:0.#} | Corridor Segments {buildResult.corridors.Count} | AvgCorridor {buildResult.averageCorridorLength:0.#} | MaxCorridor {buildResult.maxCorridorLength:0.#} | CorridorOver36 {buildResult.percentCorridorsOverTarget:0.#}% | SpawnPts P{buildResult.GetSpawnPointCount(DungeonSpawnPointCategory.PlayerSpawn)} M{buildResult.GetSpawnPointCount(DungeonSpawnPointCategory.EnemyMelee)} R{buildResult.GetSpawnPointCount(DungeonSpawnPointCategory.EnemyRanged)} E{buildResult.GetSpawnPointCount(DungeonSpawnPointCategory.EliteEnemy)} T{buildResult.GetSpawnPointCount(DungeonSpawnPointCategory.TargetDummy)} C{buildResult.GetSpawnPointCount(DungeonSpawnPointCategory.Chest)} S{buildResult.GetSpawnPointCount(DungeonSpawnPointCategory.Shrine)} W{buildResult.GetSpawnPointCount(DungeonSpawnPointCategory.Reward)} I{buildResult.GetSpawnPointCount(DungeonSpawnPointCategory.Interactable)} | Warnings {buildResult.validationWarningCount} | Failures {buildResult.validationFailureCount}";
        }

        private void SetVisibleInteractablesEnabled(bool enabled)
        {
            if (runtimeRoot == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = runtimeRoot.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IInteractable)
                {
                    behaviours[i].enabled = enabled;
                }
            }

            Collider[] colliders = runtimeRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].name.StartsWith("Interactable_", StringComparison.Ordinal))
                {
                    colliders[i].enabled = enabled;
                }
            }
        }

        private void RefreshStatusMessage()
        {
            DungeonBuildResult visibleBuild = GetVisibleBuildResult();
            if (visibleBuild == null)
            {
                statusMessage = "Dungeon build not ready.";
                return;
            }

            statusMessage = string.IsNullOrWhiteSpace(visibleBuild.validationSummary)
                ? ComposeBuildSummary(visibleBuild)
                : visibleBuild.validationSummary;
            if (encounterDirector != null && encounterDirector.Summary.spawnSource != "None")
            {
                statusMessage = $"{statusMessage} | {encounterDirector.Summary.ToDebugString()}";
            }
        }

        internal static List<Vector3> BuildCorridorRoute(Vector3 start, Vector3 end, Vector2Int direction)
        {
            List<Vector3> points = new List<Vector3> { start };
            bool primaryHorizontal = direction.x != 0;

            if (primaryHorizontal)
            {
                if (Mathf.Abs(start.z - end.z) <= DoorwayAlignmentEpsilon)
                {
                    AddRoutePoint(points, end);
                    return points;
                }

                float midX = (start.x + end.x) * 0.5f;
                AddRoutePoint(points, new Vector3(midX, start.y, start.z));
                AddRoutePoint(points, new Vector3(midX, end.y, end.z));
            }
            else
            {
                if (Mathf.Abs(start.x - end.x) <= DoorwayAlignmentEpsilon)
                {
                    AddRoutePoint(points, end);
                    return points;
                }

                float midZ = (start.z + end.z) * 0.5f;
                AddRoutePoint(points, new Vector3(start.x, start.y, midZ));
                AddRoutePoint(points, new Vector3(end.x, end.y, midZ));
            }

            AddRoutePoint(points, end);
            return points;
        }

        internal static List<Vector3> ExpandRouteEndpointsIntoRooms(List<Vector3> routePoints, Vector2Int direction, float overlap)
        {
            List<Vector3> expanded = routePoints != null ? new List<Vector3>(routePoints) : new List<Vector3>();
            if (expanded.Count < 2 || overlap <= 0f)
            {
                return expanded;
            }

            Vector3 primaryDirection = new Vector3(direction.x, 0f, direction.y);
            if (primaryDirection.sqrMagnitude <= 0f)
            {
                return expanded;
            }

            primaryDirection.Normalize();
            expanded[0] -= primaryDirection * overlap;
            expanded[expanded.Count - 1] += primaryDirection * overlap;
            return expanded;
        }

        internal static void GetVisualCorridorFloor(Vector3 start, Vector3 end, bool trimStart, bool trimEnd, out Vector3 midpoint, out float length)
        {
            Vector3 delta = end - start;
            bool horizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.z);
            float rawLength = horizontal ? Mathf.Abs(delta.x) : Mathf.Abs(delta.z);
            Vector3 direction = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.forward;
            float trimDistance = Mathf.Max(0f, CorridorRoomOverlap - CorridorVisualRoomOverlap);
            float totalTrim = (trimStart ? trimDistance : 0f) + (trimEnd ? trimDistance : 0f);
            float safeLength = Mathf.Max(CellSize * 0.9f, rawLength - totalTrim);
            float appliedTrim = Mathf.Min(totalTrim, Mathf.Max(0f, rawLength - safeLength));
            float startTrim = trimStart && totalTrim > 0f ? appliedTrim * ((trimDistance) / totalTrim) : 0f;
            float endTrim = trimEnd && totalTrim > 0f ? appliedTrim * ((trimDistance) / totalTrim) : 0f;
            Vector3 visualStart = start + direction * startTrim;
            Vector3 visualEnd = end - direction * endTrim;
            midpoint = (visualStart + visualEnd) * 0.5f;
            length = Mathf.Max(CellSize * 0.9f, horizontal ? Mathf.Abs(visualEnd.x - visualStart.x) : Mathf.Abs(visualEnd.z - visualStart.z));
        }

        internal static string BuildCorridorSeamDebugSummary(Vector3 start, Vector3 end, bool trimStart, bool trimEnd)
        {
            GetVisualCorridorFloor(start, end, trimStart, trimEnd, out Vector3 visualMidpoint, out float visualLength);
            float logicalLength = Mathf.Abs(end.x - start.x) >= Mathf.Abs(end.z - start.z)
                ? Mathf.Abs(end.x - start.x)
                : Mathf.Abs(end.z - start.z);
            return
                $"Corridor seam | LogicalOverlap={CorridorRoomOverlap:0.###} " +
                $"VisualOverlap={CorridorVisualRoomOverlap:0.###} " +
                $"VisualYOffset={CorridorVisualFloorYOffset:0.###} " +
                $"LogicalLength={logicalLength:0.###} " +
                $"VisualLength={visualLength:0.###} " +
                $"VisualMidpoint={visualMidpoint}";
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
            float visualOpeningWidth,
            DungeonRoomBuildRecord roomRecord)
        {
            if (activeBuildResult == null || activeBuildResult.FindDoorOpening(node.nodeId, direction) != null)
            {
                return;
            }

            DungeonNode neighbor = GetNeighborForDirection(node, graph, direction);
            string edgeKey = neighbor != null ? DungeonBuildResult.GetEdgeKey(node.nodeId, neighbor.nodeId) : string.Empty;
            Vector3 doorwayCenter = roomRoot.position + GetDoorLocalPosition(node, direction);
            float validationOpeningWidth = GetValidationDoorwayWidth(visualOpeningWidth);
            Vector3 visualOpeningSize = direction.x == 0
                ? new Vector3(visualOpeningWidth, WallHeight, WallThickness * 2f)
                : new Vector3(WallThickness * 2f, WallHeight, visualOpeningWidth);
            Vector3 validationOpeningSize = direction.x == 0
                ? new Vector3(validationOpeningWidth, WallHeight, WallThickness * 2f)
                : new Vector3(WallThickness * 2f, WallHeight, validationOpeningWidth);
            Vector3 openingBoundsCenter = doorwayCenter + Vector3.up * (WallHeight * 0.5f - FloorThickness * 0.5f);

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
                openingWidth = validationOpeningWidth,
                visualOpeningWidth = visualOpeningWidth,
                validationOpeningWidth = validationOpeningWidth,
                center = doorwayCenter,
                visualBounds = GetBounds(openingBoundsCenter, visualOpeningSize),
                bounds = GetBounds(openingBoundsCenter, validationOpeningSize)
            });
            activeBuildResult.reservedZones.Add(new DungeonReservedZoneRecord
            {
                ownerId = node.nodeId,
                kind = "Doorway",
                bounds = GetDoorwayReservedZone(doorwayCenter, direction, validationOpeningWidth)
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

        private static Color GetSpawnPointGizmoColor(DungeonSpawnPointCategory category)
        {
            return category switch
            {
                DungeonSpawnPointCategory.PlayerSpawn => new Color(0.2f, 1f, 1f),
                DungeonSpawnPointCategory.EnemyMelee => new Color(1f, 0.35f, 0.35f),
                DungeonSpawnPointCategory.EnemyRanged => new Color(1f, 0.8f, 0.25f),
                DungeonSpawnPointCategory.EliteEnemy => new Color(1f, 0.2f, 0.85f),
                DungeonSpawnPointCategory.TargetDummy => new Color(0.4f, 1f, 0.75f),
                DungeonSpawnPointCategory.Chest => new Color(0.95f, 0.65f, 0.15f),
                DungeonSpawnPointCategory.Shrine => new Color(0.6f, 0.95f, 0.35f),
                DungeonSpawnPointCategory.Reward => new Color(0.45f, 0.9f, 0.45f),
                _ => new Color(0.8f, 0.8f, 0.8f)
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
                scale = new Vector3(length, WallHeight, WallThickness);
            }
            else
            {
                localPosition = new Vector3(fixedCoord, WallHeight * 0.5f - FloorThickness * 0.5f, (start + end) * 0.5f);
                scale = new Vector3(WallThickness, WallHeight, length);
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

        private void CreateNodeInteractables(Transform roomRoot, DungeonNode node, RoomPurposeDefinition purpose)
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
            else if (purpose != null)
            {
                CreateRoomPurposeInteractable(
                    roomRoot,
                    node.nodeId,
                    purpose.purposeId,
                    purpose.displayName,
                    purpose.prompt,
                    new Vector3(0f, 1f, 0f),
                    purpose.color,
                    gold: purpose.gold,
                    ammo: purpose.ammo,
                    heal: purpose.heal,
                    healthRisk: purpose.healthRisk,
                    resultText: purpose.resultText,
                    effect: purpose.effect);
            }
        }

        private void CreateRoomPurposeInteractable(
            Transform roomRoot,
            string nodeId,
            string purposeType,
            string displayName,
            string prompt,
            Vector3 localPosition,
            Color color,
            int gold,
            int ammo,
            float heal,
            float healthRisk,
            string resultText,
            RoomPurposeEffect effect)
        {
            Transform purposeRoot = GetOrCreateRuntimeRootChild("RuntimeRoomPurposeInteractables");
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = $"Interactable_{nodeId}_{purposeType}";
            marker.transform.SetParent(purposeRoot, true);
            marker.transform.position = roomRoot.TransformPoint(localPosition);
            marker.transform.localScale = new Vector3(1.5f, 0.42f, 1.5f);
            ApplyColor(marker.GetComponent<Renderer>(), color);

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            string claimId = BuildRoomPurposeClaimId(nodeId, purposeType);
            marker.AddComponent<RoomPurposeInteractable>().Configure(claimId, displayName, prompt, gold, ammo, heal, healthRisk, resultText, effect);
            CreateFloatingLabel(marker.transform, displayName, color);
            RecordInteractable(nodeId, purposeType, marker, false, false, false);
        }

        private Transform GetOrCreateRuntimeRootChild(string childName)
        {
            Transform parent = runtimeRoot != null ? runtimeRoot : transform;
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject(childName);
            root.transform.SetParent(parent, false);
            return root.transform;
        }

        private string BuildRoomPurposeClaimId(string nodeId, string purposeType)
        {
            int floorIndex = activeBuildResult != null ? activeBuildResult.floorIndex : 0;
            int seed = activeBuildResult != null ? activeBuildResult.seed : 0;
            return $"floor_{floorIndex}_seed_{seed}_room_{nodeId}_purpose_{purposeType}";
        }

        private static void CreateFloatingLabel(Transform parent, string label, Color color)
        {
            FrontierDepths.Core.WorldLabelBillboard.Create(parent, "PurposeLabel", label, new Vector3(0f, 1.5f, 0f), color, 22f, true);
        }

        private void CreatePurposePickup(
            Transform roomRoot,
            string nodeId,
            string interactableType,
            EncounterDropKind kind,
            Vector3 localPosition,
            int amount)
        {
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickup.name = $"Interactable_{nodeId}_{interactableType}";
            pickup.transform.SetParent(roomRoot, false);
            pickup.transform.localPosition = localPosition;
            pickup.transform.localScale = Vector3.one * 0.8f;

            Collider collider = pickup.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            ApplyColor(pickup.GetComponent<Renderer>(), GetPickupPurposeColor(kind));
            switch (kind)
            {
                case EncounterDropKind.Health:
                    pickup.AddComponent<HealthPickup>().Configure(amount);
                    break;
                case EncounterDropKind.Ammo:
                    pickup.AddComponent<AmmoPickup>().Configure(amount);
                    break;
                default:
                    pickup.AddComponent<GoldPickup>().Configure(amount);
                    break;
            }

            pickup.AddComponent<PickupDropLandingController>().BeginLanding(pickup.transform.position);
            RecordInteractable(nodeId, interactableType, pickup, false, false, false);
        }

        private void CreatePurposeMarker(Transform roomRoot, string nodeId, string interactableType, Vector3 localPosition, Color color)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = $"Interactable_{nodeId}_{interactableType}";
            marker.transform.SetParent(roomRoot, false);
            marker.transform.localPosition = localPosition;
            marker.transform.localScale = new Vector3(1.4f, 0.35f, 1.4f);
            ApplyColor(marker.GetComponent<Renderer>(), color);
            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            RecordInteractable(nodeId, interactableType, marker, false, false, false);
        }

        private static Color GetPickupPurposeColor(EncounterDropKind kind)
        {
            return kind switch
            {
                EncounterDropKind.Health => new Color(0.25f, 0.9f, 0.38f, 1f),
                EncounterDropKind.Ammo => new Color(0.28f, 0.58f, 1f, 1f),
                _ => new Color(1f, 0.78f, 0.22f, 1f)
            };
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

        internal static float GetCorridorOuterWidth(float corridorWidth)
        {
            return corridorWidth + WallThickness;
        }

        internal static float GetMinimumSafeRoomSpacing()
        {
            float maxFootprint = 0f;
            DungeonRoomTemplateKind[] templates = DungeonRoomTemplateLibrary.GetGateOneSafeOrdinaryTemplates();
            for (int i = 0; i < templates.Length; i++)
            {
                for (int rotation = 0; rotation < 4; rotation++)
                {
                    Vector2 footprint = DungeonRoomTemplateLibrary.GetFootprintSize(templates[i], rotation, CellSize);
                    maxFootprint = Mathf.Max(maxFootprint, footprint.x, footprint.y);
                }
            }

            return maxFootprint + MinimumRoomGap;
        }

        internal static float NormalizeRoomSpacing(float configuredSpacing)
        {
            float normalized = configuredSpacing <= 0f ? DefaultRoomSpacing : configuredSpacing;
            if (normalized < MinimumRecommendedRoomSpacing)
            {
                normalized = DefaultRoomSpacing;
            }
            else if (normalized > MaximumRecommendedRoomSpacing)
            {
                normalized = MaximumRecommendedRoomSpacing;
            }

            return Mathf.Max(normalized, GetMinimumSafeRoomSpacing());
        }

        internal static float GetVisualDoorwayWidth(float corridorWidth)
        {
            return GetCorridorOuterWidth(corridorWidth);
        }

        internal static float GetValidationDoorwayWidth(float visualOpeningWidth)
        {
            return visualOpeningWidth + DoorwayClearance * 2f;
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
                widths[delta] = GetVisualDoorwayWidth(corridorWidth);
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

        private static void SetColliderEnabled(GameObject target, bool enabled)
        {
            Collider collider = target != null ? target.GetComponent<Collider>() : null;
            if (collider != null)
            {
                collider.enabled = enabled;
            }
        }

        private static void SetRendererEnabled(GameObject target, bool enabled)
        {
            Renderer renderer = target != null ? target.GetComponent<Renderer>() : null;
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
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
            return GetGameplayRoomFloorColor(kind);
        }

        internal static Color GetGameplayRoomFloorColor(DungeonNodeKind kind)
        {
            return kind switch
            {
                DungeonNodeKind.EntryHub => new Color(0.36f, 0.43f, 0.52f),
                DungeonNodeKind.TransitUp => new Color(0.32f, 0.52f, 0.62f),
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
