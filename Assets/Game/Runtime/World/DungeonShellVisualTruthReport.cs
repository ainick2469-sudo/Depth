using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class DungeonShellVisualTruthReport
    {
        public DungeonShellVisualMode requestedMode;
        public DungeonShellVisualMode activeMode;
        public string fallbackReason = string.Empty;
        public int spawnedVisualCount;
        public int spawnedWallVisualCount;
        public int spawnedFloorVisualCount;
        public int spawnedCorridorVisualCount;
        public int spawnedDoorwaySideTrimCount;
        public int spawnedPurposeVisualCount;
        public int floorVisualsChecked;
        public int corridorFloorVisualsChecked;
        public int floorVeneerCount;
        public int corridorVeneerCount;
        public int raisedFloorViolations;
        public int skippedRaisedFloorVisuals;
        public float maxFloorVisualHeightAboveSurface;
        public int doorwayClearanceCount;
        public int corridorClearanceCount;
        public int skippedDoorwayVisualCount;
        public int skippedRiskyVisualCount;
        public int skippedMismatchVisualCount;
        public int violationCount;
        public bool fallbackTriggered;

        public readonly List<DungeonShellVisualSpawnRecord> visuals = new List<DungeonShellVisualSpawnRecord>();
        public readonly List<string> failingVisualIds = new List<string>();

        public string ToSummaryString()
        {
            string reason = string.IsNullOrWhiteSpace(fallbackReason) ? "none" : fallbackReason;
            return $"Dungeon Visual Truth: mode={activeMode}, requested={requestedMode}, floors={spawnedFloorVisualCount}, walls={spawnedWallVisualCount}, corridors={spawnedCorridorVisualCount}, trims={spawnedDoorwaySideTrimCount}, purposeMarkers={spawnedPurposeVisualCount}, floorVeneers={floorVeneerCount}, corridorVeneers={corridorVeneerCount}, floorChecks={floorVisualsChecked}, corridorFloorChecks={corridorFloorVisualsChecked}, raisedFloorViolations={raisedFloorViolations}, skippedRaisedFloors={skippedRaisedFloorVisuals}, maxFloorOffset={maxFloorVisualHeightAboveSurface:0.###}, doorwayClearances={doorwayClearanceCount}, corridorClearances={corridorClearanceCount}, skippedDoorway={skippedDoorwayVisualCount}, skippedRisky={skippedRiskyVisualCount}, skippedMismatch={skippedMismatchVisualCount}, violations={violationCount}, fallback={(fallbackTriggered ? "Yes" : "No")}, fallbackReason={reason}";
        }
    }

    public sealed class DungeonShellVisualSpawnRecord
    {
        public string visualId = string.Empty;
        public string sourceId = string.Empty;
        public DungeonShellVisualKind kind;
        public Bounds bounds;
        public Bounds sourceBounds;
        public float sourceSurfaceY;
        public float floorVisualHeightAboveSurface;
        public bool sourceIsBlocking;
        public bool sourceOwned;
        public bool canHideSourceRenderer;
        public GameObject visualObject;
        public Renderer sourceRenderer;
    }
}
