using System;
using System.Collections.Generic;

namespace FrontierDepths.Core
{
    [Serializable]
    public class RunState
    {
        public int version = 2;
        public bool isActive;
        public int seed;
        public int floorIndex = 1;
        public string equippedWeaponId = "weapon.frontier_revolver";
        public List<string> acceptedBountyIds = new List<string>();
        public FloorState currentFloor = new FloorState();
        public List<FloorState> visitedFloors = new List<FloorState>();
        public PortalAnchorState portalAnchor = PortalAnchorState.Invalid;
        public FloorTransitionKind lastTransition = FloorTransitionKind.StartedRun;

        public void Normalize()
        {
            acceptedBountyIds ??= new List<string>();
            if (currentFloor == null)
            {
                currentFloor = new FloorState();
            }
            currentFloor.Normalize(floorIndex, seed);

            visitedFloors ??= new List<FloorState>();
            if (visitedFloors.Count == 0)
            {
                visitedFloors.Add(CloneFloor(currentFloor));
            }

            bool foundCurrent = false;
            for (int i = 0; i < visitedFloors.Count; i++)
            {
                if (visitedFloors[i] == null)
                {
                    visitedFloors[i] = new FloorState();
                }
                visitedFloors[i].Normalize(visitedFloors[i].floorIndex <= 0 ? i + 1 : visitedFloors[i].floorIndex, seed);

                if (visitedFloors[i].floorIndex == floorIndex)
                {
                    visitedFloors[i] = CloneFloor(currentFloor);
                    foundCurrent = true;
                }
            }

            if (!foundCurrent)
            {
                visitedFloors.Add(CloneFloor(currentFloor));
            }
        }

        public FloorState GetVisitedFloor(int targetFloorIndex)
        {
            for (int i = 0; i < visitedFloors.Count; i++)
            {
                if (visitedFloors[i].floorIndex == targetFloorIndex)
                {
                    return visitedFloors[i];
                }
            }

            return null;
        }

        public void SetVisitedFloor(FloorState floorState)
        {
            if (floorState == null)
            {
                return;
            }

            floorState.Normalize(floorState.floorIndex, seed);
            for (int i = 0; i < visitedFloors.Count; i++)
            {
                if (visitedFloors[i].floorIndex == floorState.floorIndex)
                {
                    visitedFloors[i] = CloneFloor(floorState);
                    return;
                }
            }

            visitedFloors.Add(CloneFloor(floorState));
        }

        private static FloorState CloneFloor(FloorState source)
        {
            return new FloorState
            {
                floorIndex = source.floorIndex,
                floorSeed = source.floorSeed,
                floorBandId = source.floorBandId,
                chapterId = source.chapterId,
                themeKitId = source.themeKitId,
                stairDiscovered = source.stairDiscovered,
                graphLayoutSignature = source.graphLayoutSignature,
                layoutShapeSignature = source.layoutShapeSignature
            };
        }
    }

    [Serializable]
    public class FloorState
    {
        public int floorIndex;
        public int floorSeed;
        public string floorBandId = "floorband.frontier_mine";
        public string chapterId = "chapter.frontier_descent";
        public string themeKitId = "theme.frontier_town";
        public bool stairDiscovered;
        public string graphLayoutSignature = string.Empty;
        public string layoutShapeSignature = string.Empty;

        public void Normalize(int fallbackFloorIndex, int fallbackSeed)
        {
            floorIndex = floorIndex <= 0 ? fallbackFloorIndex : floorIndex;
            floorSeed = floorSeed == 0 ? fallbackSeed + fallbackFloorIndex * 31 : floorSeed;
            floorBandId = string.IsNullOrWhiteSpace(floorBandId) ? "floorband.frontier_mine" : floorBandId;
            chapterId = string.IsNullOrWhiteSpace(chapterId) ? "chapter.frontier_descent" : chapterId;
            themeKitId = string.IsNullOrWhiteSpace(themeKitId) ? "theme.frontier_town" : themeKitId;
            graphLayoutSignature ??= string.Empty;
            layoutShapeSignature ??= string.Empty;
        }
    }

    [Serializable]
    public struct PortalAnchorState
    {
        public bool isValid;
        public int floorIndex;
        public string roomId;
        public SerializableVector3 worldPosition;

        public static PortalAnchorState Invalid => new PortalAnchorState
        {
            isValid = false,
            floorIndex = 0,
            roomId = string.Empty,
            worldPosition = default
        };
    }

    public enum FloorTransitionKind
    {
        StartedRun,
        Descended,
        Ascended,
        ReturnedByPortal
    }
}
