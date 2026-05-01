using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class DungeonBuildResult
    {
        public DungeonLayoutGraph graph;
        public int floorIndex;
        public int seed;
        public bool usedFallback;
        public bool requestedFallback;
        public bool generatorReturnedFallbackGraph;
        public bool validationPassed;
        public bool isEmergencyDebugBuild;
        public int validationFailureCount;
        public int validationWarningCount;
        public int attemptNumber;
        public int attemptCount;
        public string graphLayoutSignature;
        public string layoutShapeSignature;
        public string validationSummary;
        public Vector3 playerSpawn;
        public string playerSpawnNodeId;
        public string playerSpawnNodeKind;
        public string entryNodeId;
        public string transitUpNodeId;
        public string transitDownNodeId;
        public string landmarkNodeId;
        public string secretNodeId;
        public float averageRoomFootprint;
        public float largestRoomFootprint;
        public float averageCorridorLength;
        public float maxCorridorLength;
        public float percentCorridorsOverTarget;
        public DungeonLayoutQualityReport layoutQualityReport;
        public DungeonLabyrinthObjectivePlan labyrinthObjectivePlan;
        public DungeonShellVisualTruthReport shellVisualReport;

        public readonly List<DungeonGraphEdgeRecord> graphEdges = new List<DungeonGraphEdgeRecord>();
        public readonly List<DungeonRoomBuildRecord> rooms = new List<DungeonRoomBuildRecord>();
        public readonly List<DungeonCorridorBuildRecord> corridors = new List<DungeonCorridorBuildRecord>();
        public readonly List<DungeonDoorOpeningRecord> doorOpenings = new List<DungeonDoorOpeningRecord>();
        public readonly List<DungeonWallSpanRecord> wallSpans = new List<DungeonWallSpanRecord>();
        public readonly List<DungeonReservedZoneRecord> reservedZones = new List<DungeonReservedZoneRecord>();
        public readonly List<DungeonInteractableBuildRecord> interactables = new List<DungeonInteractableBuildRecord>();
        public readonly List<DungeonSpawnPointRecord> spawnPoints = new List<DungeonSpawnPointRecord>();
        public readonly List<RoomClusterRecord> roomClusters = new List<RoomClusterRecord>();
        public readonly List<DungeonRoomMergeCandidateRecord> mergeCandidates = new List<DungeonRoomMergeCandidateRecord>();

        public static string GetEdgeKey(string a, string b)
        {
            return string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";
        }

        public DungeonRoomBuildRecord FindRoom(string nodeId)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].nodeId == nodeId)
                {
                    return rooms[i];
                }
            }

            return null;
        }

        public DungeonDoorOpeningRecord FindDoorOpening(string nodeId, Vector2Int direction)
        {
            for (int i = 0; i < doorOpenings.Count; i++)
            {
                if (doorOpenings[i].nodeId == nodeId && doorOpenings[i].direction == direction)
                {
                    return doorOpenings[i];
                }
            }

            return null;
        }

        public List<DungeonCorridorBuildRecord> GetCorridorsForEdge(string edgeKey)
        {
            List<DungeonCorridorBuildRecord> matches = new List<DungeonCorridorBuildRecord>();
            for (int i = 0; i < corridors.Count; i++)
            {
                if (corridors[i].edgeKey == edgeKey)
                {
                    matches.Add(corridors[i]);
                }
            }

            matches.Sort((left, right) => left.segmentIndex.CompareTo(right.segmentIndex));
            return matches;
        }

        public List<DungeonSpawnPointRecord> GetSpawnPoints(string nodeId, DungeonSpawnPointCategory category)
        {
            List<DungeonSpawnPointRecord> matches = new List<DungeonSpawnPointRecord>();
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i].nodeId == nodeId && spawnPoints[i].category == category)
                {
                    matches.Add(spawnPoints[i]);
                }
            }

            matches.Sort((left, right) => right.score.CompareTo(left.score));
            return matches;
        }

        public int GetSpawnPointCount(DungeonSpawnPointCategory category)
        {
            int count = 0;
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i].category == category)
                {
                    count++;
                }
            }

            return count;
        }

        public string GetBuildModeLabel()
        {
            if (isEmergencyDebugBuild)
            {
                return "EMERGENCY FALLBACK";
            }

            if (requestedFallback)
            {
                return "REQUESTED FALLBACK";
            }

            if (generatorReturnedFallbackGraph || usedFallback)
            {
                return "GENERATOR FALLBACK";
            }

            return "NORMAL BUILD";
        }
    }

    public sealed class DungeonGraphEdgeRecord
    {
        public string edgeKey;
        public string a;
        public string b;
    }

    public sealed class DungeonRoomBuildRecord
    {
        public string nodeId;
        public string label;
        public DungeonNodeKind roomType;
        public DungeonRoomTemplateKind templateKind;
        public string zoneId = string.Empty;
        public DungeonZoneType zoneType = DungeonZoneType.None;
        public DungeonRoomRole roomRole = DungeonRoomRole.None;
        public DungeonRoomRole layoutRole = DungeonRoomRole.None;
        public int criticalPathIndex = -1;
        public bool isOptional;
        public bool isProtected;
        public bool isMainPath;
        public bool isBranch;
        public bool isDeadEnd;
        public bool isLandmarkRoom;
        public bool isFutureBossApproach;
        public bool isObjectiveRoom;
        public bool isBossApproachRoom;
        public bool isBossRoomPlaceholder;
        public bool isExitStairsRoom;
        public int objectivePathIndex = -1;
        public DungeonRoomRole objectiveRole = DungeonRoomRole.None;
        public int dangerTier;
        public string lockId = string.Empty;
        public string requiredKeyId = string.Empty;
        public string grantsKeyId = string.Empty;
        public string bountyId = string.Empty;
        public string questId = string.Empty;
        public string purposeId = string.Empty;
        public string purposeDisplayName = string.Empty;
        public string purposeIcon = string.Empty;
        public string clusterId = string.Empty;
        public GameObject rootObject;
        public Vector3 origin;
        public Bounds bounds;
        public bool hasFloor;
        public int wallCount;
        public int doorwayCount;
        public float footprintArea;
        public Vector2Int centerCell;
        public readonly List<Vector2Int> floorCells = new List<Vector2Int>();
    }

    public sealed class DungeonRoomMergeCandidateRecord
    {
        public string roomA = string.Empty;
        public string roomB = string.Empty;
        public Bounds combinedBounds;
        public string reason = string.Empty;
        public int floorDepth;
        public bool isSafeToApply;
        public string rejectionReason = string.Empty;
    }

    public sealed class DungeonLabyrinthObjectivePlan
    {
        public string entranceRoomId = string.Empty;
        public readonly List<string> mainPathRoomIds = new List<string>();
        public string objectiveRoomId = string.Empty;
        public string bossApproachRoomId = string.Empty;
        public string bossRoomId = string.Empty;
        public string exitStairsRoomId = string.Empty;
        public bool objectiveRequired = true;
        public bool objectiveCompleted;
        public bool bossDefeated;
        public bool lockedExitUntilObjectiveComplete;
        public bool lockedExitUntilBossDefeated;
        public bool nextDepthUnlocked = true;
        public readonly List<string> warnings = new List<string>();

        public bool HasObjectiveStructure =>
            !string.IsNullOrWhiteSpace(entranceRoomId) &&
            !string.IsNullOrWhiteSpace(objectiveRoomId) &&
            !string.IsNullOrWhiteSpace(bossApproachRoomId) &&
            !string.IsNullOrWhiteSpace(bossRoomId) &&
            !string.IsNullOrWhiteSpace(exitStairsRoomId);
    }

    public sealed class DungeonLayoutQualityReport
    {
        public int roomCount;
        public int corridorCount;
        public float averageRoomArea;
        public float smallestRoomArea;
        public float largestRoomArea;
        public float averageCorridorLength;
        public float longestCorridorLength;
        public int veryLongCorridorCount;
        public int straightCorridorChainCount;
        public float corridorToRoomRatio;
        public int mainPathRoomCount;
        public int branchRoomCount;
        public int deadEndRoomCount;
        public int landmarkRoomCount;
        public int mergeCandidateCount;
        public int specialRoomCount;
        public int protectedRoomCount;
        public int repeatedSpecialRoomWarnings;
        public int longCorridorWarnings;
        public int objectivePathRoomCount;
        public bool hasObjectiveRoom;
        public bool hasBossApproachRoom;
        public bool hasBossRoomPlaceholder;
        public bool exitLockMetadataPrepared;
        public readonly List<string> objectivePlanWarnings = new List<string>();
        public readonly List<string> layoutWarnings = new List<string>();

        public int warningCount => layoutWarnings.Count + objectivePlanWarnings.Count + longCorridorWarnings;

        public string ToSummaryString()
        {
            return "Dungeon Layout Quality: " +
                   $"rooms={roomCount}, avgArea={averageRoomArea:0.0}, largest={largestRoomArea:0.0}, " +
                   $"corridors={corridorCount}, avgCorridor={averageCorridorLength:0.0}, longest={longestCorridorLength:0.0}, " +
                   $"mainPath={mainPathRoomCount}, branches={branchRoomCount}, deadEnds={deadEndRoomCount}, " +
                   $"landmarks={landmarkRoomCount}, objective={hasObjectiveRoom}, bossApproach={hasBossApproachRoom}, " +
                   $"bossRoom={hasBossRoomPlaceholder}, mergeCandidates={mergeCandidateCount}, warnings={warningCount}";
        }
    }

    public sealed class DungeonCorridorBuildRecord
    {
        public string edgeKey;
        public string fromNodeId;
        public string toNodeId;
        public int segmentIndex;
        public Vector3 start;
        public Vector3 end;
        public Bounds bounds;
        public Bounds outerBounds;
        public bool horizontal;
        public float length;
        public float width;
        public bool isSecretCorridor;
    }

    public sealed class DungeonDoorOpeningRecord
    {
        public string openingId;
        public string nodeId;
        public Vector2Int direction;
        public string neighborNodeId;
        public string edgeKey;
        public float openingWidth;
        public float visualOpeningWidth;
        public float validationOpeningWidth;
        public Vector3 center;
        public Bounds visualBounds;
        public Bounds bounds;
    }

    public sealed class DungeonWallSpanRecord
    {
        public string ownerId;
        public string edgeKey;
        public Vector2Int direction;
        public Bounds bounds;
        public bool isCorridorWall;
    }

    public sealed class DungeonReservedZoneRecord
    {
        public string ownerId;
        public string kind;
        public Bounds bounds;
    }

    public sealed class DungeonInteractableBuildRecord
    {
        public string nodeId;
        public string interactableType;
        public bool requiresTownSigil;
        public bool returnsToTown;
        public bool isRequiredReturnRoute;
        public Vector3 position;
        public Bounds bounds;
    }

    public enum DungeonSpawnPointCategory
    {
        PlayerSpawn,
        EnemyMelee,
        EnemyRanged,
        EliteEnemy,
        TargetDummy,
        Chest,
        Shrine,
        Reward,
        Interactable
    }

    public sealed class DungeonSpawnPointRecord
    {
        public string nodeId;
        public DungeonSpawnPointCategory category;
        public Vector3 position;
        public Bounds bounds;
        public float score;
    }

    public sealed class RoomClusterRecord
    {
        public string clusterId = string.Empty;
        public string purposeId = string.Empty;
        public int floorIndex;
        public Bounds combinedBounds;
        public readonly List<string> roomIds = new List<string>();
    }
}
