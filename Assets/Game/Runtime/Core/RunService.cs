using System;
using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.Core
{
    public sealed class RunService
    {
        private readonly SaveService saveService;
        private readonly ProfileService profileService;

        public RunService(SaveService saveService, ProfileService profileService)
        {
            this.saveService = saveService;
            this.profileService = profileService;

            Current = saveService.LoadRun() ?? new RunState();
            Current.Normalize();

            if (!Current.isActive)
            {
                Current.portalAnchor = PortalAnchorState.Invalid;
            }
        }

        public RunState Current { get; private set; }
        public bool HasActiveRun => Current != null && Current.isActive;

        public RunState StartNewRun()
        {
            int seed = Mathf.Abs(Guid.NewGuid().GetHashCode());
            FloorState floorOne = CreateFloorState(1, seed);
            profileService.Current.Normalize();
            string activeWeaponId = profileService.Current.GetActiveWeaponId();
            RunWeaponAmmoState activeAmmo = new RunWeaponAmmoState
            {
                weaponId = activeWeaponId,
                currentMagazineAmmo = 6,
                reserveAmmo = 36,
                maxReserveAmmo = 72
            };

            Current = new RunState
            {
                isActive = true,
                seed = seed,
                floorIndex = 1,
                equippedWeaponId = activeWeaponId,
                acceptedBountyIds = new List<string>(profileService.GetActiveBounties()),
                runUpgrades = new List<RunUpgradeRecord>(),
                weaponAmmo = activeAmmo.Clone(),
                weaponAmmoStates = new List<RunWeaponAmmoState> { activeAmmo.Clone() },
                currentFloor = floorOne,
                visitedFloors = new List<FloorState> { CloneFloor(floorOne) },
                portalAnchor = PortalAnchorState.Invalid,
                lastTransition = FloorTransitionKind.StartedRun
            };

            Save();
            return Current;
        }

        public RunState EnsureRun()
        {
            return HasActiveRun ? Current : StartNewRun();
        }

        public void DescendToNextFloor()
        {
            EnsureRun();
            SaveActiveFloorState();
            Current.floorIndex++;
            Current.currentFloor = GetOrCreateFloorState(Current.floorIndex);
            Current.portalAnchor = PortalAnchorState.Invalid;
            Current.lastTransition = FloorTransitionKind.Descended;
            Save();
        }

        public void AddRunUpgrade(string upgradeId)
        {
            EnsureRun();
            Current.AddOrStackUpgrade(upgradeId);
            Save();
        }

        public void MarkCurrentFloorRewardGranted()
        {
            EnsureRun();
            Current.currentFloor.rewardGranted = true;
            Save();
        }

        public void AscendToPreviousFloor()
        {
            EnsureRun();
            if (Current.floorIndex <= 1)
            {
                return;
            }

            SaveActiveFloorState();
            Current.floorIndex--;
            Current.currentFloor = GetOrCreateFloorState(Current.floorIndex);
            Current.portalAnchor = PortalAnchorState.Invalid;
            Current.lastTransition = FloorTransitionKind.Ascended;
            Save();
        }

        public void SetPortalAnchor(Vector3 worldPosition, string roomId)
        {
            EnsureRun();
            Current.portalAnchor = new PortalAnchorState
            {
                isValid = true,
                floorIndex = Current.floorIndex,
                roomId = roomId ?? string.Empty,
                worldPosition = new SerializableVector3(worldPosition)
            };
            Current.lastTransition = FloorTransitionKind.ReturnedByPortal;
            Save();
        }

        public void ClearPortalAnchor()
        {
            EnsureRun();
            Current.portalAnchor = PortalAnchorState.Invalid;
            Save();
        }

        public void PrepareTownReturnOnFoot()
        {
            EnsureRun();
            SaveActiveFloorState();
            Current.lastTransition = FloorTransitionKind.StartedRun;
            Save();
        }

        public void EndRun()
        {
            Current = new RunState();
            Current.Normalize();
            saveService.DeleteRun();
        }

        public void Save()
        {
            if (Current.currentFloor != null)
            {
                Current.SetVisitedFloor(Current.currentFloor);
            }

            Current.Normalize();
            saveService.SaveRun(Current);
        }

        public FloorState GetOrCreateFloorState(int floorIndex)
        {
            EnsureRun();
            FloorState existing = Current.GetVisitedFloor(floorIndex);
            if (existing != null)
            {
                return CloneFloor(existing);
            }

            FloorState created = CreateFloorState(floorIndex, Current.seed);
            Current.SetVisitedFloor(created);
            return CloneFloor(created);
        }

        public void SaveActiveFloorState()
        {
            if (Current == null || Current.currentFloor == null)
            {
                return;
            }

            Current.SetVisitedFloor(Current.currentFloor);
            saveService.SaveRun(Current);
        }

        public FloorState LoadFloorState(int floorIndex)
        {
            return GetOrCreateFloorState(floorIndex);
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
                rewardGranted = source.rewardGranted,
                graphLayoutSignature = source.graphLayoutSignature,
                layoutShapeSignature = source.layoutShapeSignature,
                visitedRoomIds = CopyStringList(source.visitedRoomIds),
                discoveredRoomIds = CopyStringList(source.discoveredRoomIds),
                discoveredCorridorIds = CopyStringList(source.discoveredCorridorIds),
                claimedRoomPurposeIds = CopyStringList(source.claimedRoomPurposeIds),
                lastKnownPlayerRoomId = source.lastKnownPlayerRoomId,
                knownStairRoomId = source.knownStairRoomId
            };
        }

        private static List<string> CopyStringList(List<string> source)
        {
            return source != null ? new List<string>(source) : new List<string>();
        }

        private static FloorState CreateFloorState(int floorIndex, int runSeed)
        {
            int chapterTier = Mathf.Max(0, (floorIndex - 1) / 20);
            string chapterId = chapterTier switch
            {
                0 => "chapter.frontier_descent",
                1 => "chapter.buried_temple",
                _ => "chapter.depths_unknown"
            };

            string floorBandId = floorIndex switch
            {
                <= 10 => "floorband.frontier_mine",
                <= 20 => "floorband.corrupted_quarry",
                _ => "floorband.buried_temple"
            };

            string themeId = floorIndex switch
            {
                <= 10 => "theme.frontier_town",
                <= 20 => "theme.corrupted_quarry",
                _ => "theme.buried_temple"
            };

            return new FloorState
            {
                floorIndex = floorIndex,
                floorSeed = runSeed + floorIndex * 977,
                floorBandId = floorBandId,
                chapterId = chapterId,
                themeKitId = themeId,
                stairDiscovered = false
            };
        }
    }
}
