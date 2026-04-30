using System;
using System.Collections.Generic;

namespace FrontierDepths.Core
{
    [Serializable]
    public sealed class WorldFloorProgressionProfileState
    {
        public int currentWorldFloor = 1;
        public int highestUnlockedWorldFloor = 1;
        public int worldSeed;
        public List<WorldFloorProfileRecord> floorRecords = new List<WorldFloorProfileRecord>();

        public void Normalize()
        {
            currentWorldFloor = Math.Max(1, currentWorldFloor);
            highestUnlockedWorldFloor = Math.Max(1, highestUnlockedWorldFloor);
            worldSeed = worldSeed == 0 ? CreateWorldSeed() : Math.Abs(worldSeed);
            floorRecords ??= new List<WorldFloorProfileRecord>();

            WorldFloorProfileRecord floorOne = GetOrCreateFloorRecord(1);
            floorOne.isUnlocked = true;
            highestUnlockedWorldFloor = Math.Max(highestUnlockedWorldFloor, 1);

            for (int i = floorRecords.Count - 1; i >= 0; i--)
            {
                WorldFloorProfileRecord record = floorRecords[i];
                if (record == null)
                {
                    floorRecords.RemoveAt(i);
                    continue;
                }

                record.Normalize(record.floorNumber <= 0 ? i + 1 : record.floorNumber);
                if (record.isUnlocked)
                {
                    highestUnlockedWorldFloor = Math.Max(highestUnlockedWorldFloor, record.floorNumber);
                }
            }

            currentWorldFloor = Math.Min(currentWorldFloor, highestUnlockedWorldFloor);
        }

        public WorldFloorProfileRecord GetOrCreateFloorRecord(int floorNumber)
        {
            floorNumber = Math.Max(1, floorNumber);
            floorRecords ??= new List<WorldFloorProfileRecord>();
            WorldFloorProfileRecord existing = FindFloorRecord(floorNumber);
            if (existing != null)
            {
                existing.Normalize(floorNumber);
                return existing;
            }

            WorldFloorProfileRecord created = new WorldFloorProfileRecord
            {
                floorNumber = floorNumber,
                isUnlocked = floorNumber == 1
            };
            created.Normalize(floorNumber);
            floorRecords.Add(created);
            return created;
        }

        public WorldFloorProfileRecord FindFloorRecord(int floorNumber)
        {
            floorNumber = Math.Max(1, floorNumber);
            floorRecords ??= new List<WorldFloorProfileRecord>();
            for (int i = 0; i < floorRecords.Count; i++)
            {
                WorldFloorProfileRecord record = floorRecords[i];
                if (record != null && record.floorNumber == floorNumber)
                {
                    return record;
                }
            }

            return null;
        }

        private static int CreateWorldSeed()
        {
            int seed = Guid.NewGuid().GetHashCode();
            if (seed == int.MinValue)
            {
                return int.MaxValue;
            }

            seed = Math.Abs(seed);
            return seed == 0 ? 1 : seed;
        }
    }

    [Serializable]
    public sealed class WorldFloorProfileRecord
    {
        public int floorNumber;
        public bool isUnlocked;
        public bool isCleared;
        public bool bossDefeated;
        public string defeatedBossId = string.Empty;
        public bool labyrinthEntranceKnown;
        public int floorSeed;
        public List<string> visitedSettlementIds = new List<string>();
        public List<string> unlockedTeleportGateIds = new List<string>();

        public void Normalize(int fallbackFloorNumber)
        {
            floorNumber = floorNumber <= 0 ? Math.Max(1, fallbackFloorNumber) : floorNumber;
            defeatedBossId ??= string.Empty;
            visitedSettlementIds = NormalizeStringList(visitedSettlementIds);
            unlockedTeleportGateIds = NormalizeStringList(unlockedTeleportGateIds);
        }

        public bool HasVisitedSettlement(string settlementId)
        {
            return !string.IsNullOrWhiteSpace(settlementId) &&
                   visitedSettlementIds != null &&
                   visitedSettlementIds.Contains(settlementId);
        }

        public void MarkSettlementVisited(string settlementId)
        {
            if (string.IsNullOrWhiteSpace(settlementId))
            {
                return;
            }

            visitedSettlementIds ??= new List<string>();
            if (!visitedSettlementIds.Contains(settlementId))
            {
                visitedSettlementIds.Add(settlementId);
            }
        }

        public bool HasTeleportGate(string gateId)
        {
            return !string.IsNullOrWhiteSpace(gateId) &&
                   unlockedTeleportGateIds != null &&
                   unlockedTeleportGateIds.Contains(gateId);
        }

        public void UnlockTeleportGate(string gateId)
        {
            if (string.IsNullOrWhiteSpace(gateId))
            {
                return;
            }

            unlockedTeleportGateIds ??= new List<string>();
            if (!unlockedTeleportGateIds.Contains(gateId))
            {
                unlockedTeleportGateIds.Add(gateId);
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
}
