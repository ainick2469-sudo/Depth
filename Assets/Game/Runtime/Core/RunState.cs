using System;
using System.Collections.Generic;

namespace FrontierDepths.Core
{
    [Serializable]
    public class RunState
    {
        public int version = 5;
        public bool isActive;
        public int seed;
        public int floorIndex = 1;
        public string equippedWeaponId = "weapon.frontier_revolver";
        public List<string> acceptedBountyIds = new List<string>();
        public List<RunUpgradeRecord> runUpgrades = new List<RunUpgradeRecord>();
        public RunWeaponAmmoState weaponAmmo = new RunWeaponAmmoState();
        public List<RunWeaponAmmoState> weaponAmmoStates = new List<RunWeaponAmmoState>();
        public FloorState currentFloor = new FloorState();
        public List<FloorState> visitedFloors = new List<FloorState>();
        public PortalAnchorState portalAnchor = PortalAnchorState.Invalid;
        public FloorTransitionKind lastTransition = FloorTransitionKind.StartedRun;

        public void Normalize()
        {
            acceptedBountyIds ??= new List<string>();
            runUpgrades ??= new List<RunUpgradeRecord>();
            bool legacyAmmoState = version < 3 || weaponAmmo == null;
            weaponAmmo ??= new RunWeaponAmmoState();
            weaponAmmo.Normalize(equippedWeaponId, legacyAmmoState);
            weaponAmmoStates ??= new List<RunWeaponAmmoState>();
            UpsertWeaponAmmoState(weaponAmmo);
            for (int i = weaponAmmoStates.Count - 1; i >= 0; i--)
            {
                if (weaponAmmoStates[i] == null)
                {
                    weaponAmmoStates.RemoveAt(i);
                    continue;
                }

                weaponAmmoStates[i].Normalize(weaponAmmoStates[i].weaponId);
            }
            version = 5;
            for (int i = runUpgrades.Count - 1; i >= 0; i--)
            {
                RunUpgradeRecord record = runUpgrades[i];
                if (string.IsNullOrWhiteSpace(record.upgradeId))
                {
                    runUpgrades.RemoveAt(i);
                    continue;
                }

                record.stackCount = Math.Max(1, record.stackCount);
                runUpgrades[i] = record;
            }

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

        public int GetUpgradeStackCount(string upgradeId)
        {
            if (string.IsNullOrWhiteSpace(upgradeId) || runUpgrades == null)
            {
                return 0;
            }

            for (int i = 0; i < runUpgrades.Count; i++)
            {
                if (string.Equals(runUpgrades[i].upgradeId, upgradeId, StringComparison.Ordinal))
                {
                    return Math.Max(0, runUpgrades[i].stackCount);
                }
            }

            return 0;
        }

        public void AddOrStackUpgrade(string upgradeId, int stackDelta = 1)
        {
            if (string.IsNullOrWhiteSpace(upgradeId))
            {
                throw new ArgumentException("Upgrade id is required.", nameof(upgradeId));
            }

            runUpgrades ??= new List<RunUpgradeRecord>();
            int clampedDelta = Math.Max(1, stackDelta);
            for (int i = 0; i < runUpgrades.Count; i++)
            {
                if (!string.Equals(runUpgrades[i].upgradeId, upgradeId, StringComparison.Ordinal))
                {
                    continue;
                }

                RunUpgradeRecord existing = runUpgrades[i];
                existing.stackCount = Math.Max(1, existing.stackCount) + clampedDelta;
                runUpgrades[i] = existing;
                return;
            }

            runUpgrades.Add(new RunUpgradeRecord
            {
                upgradeId = upgradeId,
                stackCount = clampedDelta
            });
        }

        public RunWeaponAmmoState GetWeaponAmmoState(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId) || weaponAmmoStates == null)
            {
                return null;
            }

            for (int i = 0; i < weaponAmmoStates.Count; i++)
            {
                RunWeaponAmmoState state = weaponAmmoStates[i];
                if (state != null && string.Equals(state.weaponId, weaponId, StringComparison.Ordinal))
                {
                    return state;
                }
            }

            return null;
        }

        public RunWeaponAmmoState GetOrCreateWeaponAmmoState(string weaponId, int magazineAmmo, int reserveAmmo, int maxReserveAmmo)
        {
            weaponId = string.IsNullOrWhiteSpace(weaponId) ? equippedWeaponId : weaponId;
            RunWeaponAmmoState existing = GetWeaponAmmoState(weaponId);
            if (existing != null)
            {
                existing.Normalize(weaponId, false, magazineAmmo, reserveAmmo, maxReserveAmmo);
                return existing;
            }

            RunWeaponAmmoState created = new RunWeaponAmmoState
            {
                weaponId = weaponId,
                currentMagazineAmmo = Math.Max(0, magazineAmmo),
                reserveAmmo = Math.Max(0, Math.Min(reserveAmmo, Math.Max(0, maxReserveAmmo))),
                maxReserveAmmo = Math.Max(0, maxReserveAmmo)
            };
            weaponAmmoStates ??= new List<RunWeaponAmmoState>();
            weaponAmmoStates.Add(created);
            return created;
        }

        public void UpsertWeaponAmmoState(RunWeaponAmmoState state)
        {
            if (state == null)
            {
                return;
            }

            weaponAmmoStates ??= new List<RunWeaponAmmoState>();
            state.Normalize(equippedWeaponId);
            for (int i = 0; i < weaponAmmoStates.Count; i++)
            {
                if (weaponAmmoStates[i] != null && string.Equals(weaponAmmoStates[i].weaponId, state.weaponId, StringComparison.Ordinal))
                {
                    weaponAmmoStates[i] = state.Clone();
                    return;
                }
            }

            weaponAmmoStates.Add(state.Clone());
        }

        public int TryAddReserveAmmoToActiveWeapon(int amount)
        {
            weaponAmmo ??= new RunWeaponAmmoState();
            RunWeaponAmmoState active = GetOrCreateWeaponAmmoState(
                string.IsNullOrWhiteSpace(equippedWeaponId) ? weaponAmmo.weaponId : equippedWeaponId,
                weaponAmmo.currentMagazineAmmo,
                weaponAmmo.reserveAmmo,
                weaponAmmo.maxReserveAmmo);
            int added = active.TryAddReserveAmmo(amount);
            weaponAmmo = active.Clone();
            return added;
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
    }

    [Serializable]
    public struct RunUpgradeRecord
    {
        public string upgradeId;
        public int stackCount;
    }

    [Serializable]
    public class RunWeaponAmmoState
    {
        public string weaponId = "weapon.frontier_revolver";
        public int currentMagazineAmmo = 6;
        public int reserveAmmo = 36;
        public int maxReserveAmmo = 72;

        public void Normalize(string fallbackWeaponId, bool useDefaultsForMissingValues = false)
        {
            Normalize(fallbackWeaponId, useDefaultsForMissingValues, 6, 36, 72);
        }

        public void Normalize(
            string fallbackWeaponId,
            bool useDefaultsForMissingValues,
            int defaultMagazineAmmo,
            int defaultReserveAmmo,
            int defaultMaxReserveAmmo)
        {
            weaponId = string.IsNullOrWhiteSpace(weaponId)
                ? (string.IsNullOrWhiteSpace(fallbackWeaponId) ? "weapon.frontier_revolver" : fallbackWeaponId)
                : weaponId;
            maxReserveAmmo = Math.Max(0, maxReserveAmmo <= 0 ? defaultMaxReserveAmmo : maxReserveAmmo);
            if (useDefaultsForMissingValues)
            {
                currentMagazineAmmo = currentMagazineAmmo <= 0 ? defaultMagazineAmmo : currentMagazineAmmo;
                reserveAmmo = reserveAmmo <= 0 ? defaultReserveAmmo : reserveAmmo;
            }

            currentMagazineAmmo = Math.Max(0, currentMagazineAmmo);
            reserveAmmo = Math.Max(0, Math.Min(reserveAmmo, maxReserveAmmo));
        }

        public int TryAddReserveAmmo(int amount)
        {
            if (amount <= 0 || reserveAmmo >= maxReserveAmmo)
            {
                return 0;
            }

            int before = reserveAmmo;
            reserveAmmo = Math.Min(maxReserveAmmo, reserveAmmo + amount);
            return reserveAmmo - before;
        }

        public RunWeaponAmmoState Clone()
        {
            return new RunWeaponAmmoState
            {
                weaponId = weaponId,
                currentMagazineAmmo = currentMagazineAmmo,
                reserveAmmo = reserveAmmo,
                maxReserveAmmo = maxReserveAmmo
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
        public bool rewardGranted;
        public string graphLayoutSignature = string.Empty;
        public string layoutShapeSignature = string.Empty;
        public List<string> visitedRoomIds = new List<string>();
        public List<string> discoveredRoomIds = new List<string>();
        public List<string> discoveredCorridorIds = new List<string>();
        public List<string> claimedRoomPurposeIds = new List<string>();
        public string lastKnownPlayerRoomId = string.Empty;
        public string knownStairRoomId = string.Empty;

        public void Normalize(int fallbackFloorIndex, int fallbackSeed)
        {
            floorIndex = floorIndex <= 0 ? fallbackFloorIndex : floorIndex;
            floorSeed = floorSeed == 0 ? fallbackSeed + fallbackFloorIndex * 31 : floorSeed;
            floorBandId = string.IsNullOrWhiteSpace(floorBandId) ? "floorband.frontier_mine" : floorBandId;
            chapterId = string.IsNullOrWhiteSpace(chapterId) ? "chapter.frontier_descent" : chapterId;
            themeKitId = string.IsNullOrWhiteSpace(themeKitId) ? "theme.frontier_town" : themeKitId;
            graphLayoutSignature ??= string.Empty;
            layoutShapeSignature ??= string.Empty;
            visitedRoomIds = NormalizeStringList(visitedRoomIds);
            discoveredRoomIds = NormalizeStringList(discoveredRoomIds);
            discoveredCorridorIds = NormalizeStringList(discoveredCorridorIds);
            claimedRoomPurposeIds = NormalizeStringList(claimedRoomPurposeIds);
            lastKnownPlayerRoomId ??= string.Empty;
            knownStairRoomId ??= string.Empty;
        }

        public bool HasClaimedRoomPurpose(string claimId)
        {
            return !string.IsNullOrWhiteSpace(claimId) &&
                   claimedRoomPurposeIds != null &&
                   claimedRoomPurposeIds.Contains(claimId);
        }

        public void MarkRoomPurposeClaimed(string claimId)
        {
            if (string.IsNullOrWhiteSpace(claimId))
            {
                return;
            }

            claimedRoomPurposeIds ??= new List<string>();
            if (!claimedRoomPurposeIds.Contains(claimId))
            {
                claimedRoomPurposeIds.Add(claimId);
            }
        }

        private static List<string> NormalizeStringList(List<string> source)
        {
            List<string> normalized = new List<string>();
            if (source == null)
            {
                return normalized;
            }

            for (int i = 0; i < source.Count; i++)
            {
                string value = source[i];
                if (!string.IsNullOrWhiteSpace(value) && !normalized.Contains(value))
                {
                    normalized.Add(value);
                }
            }

            return normalized;
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
