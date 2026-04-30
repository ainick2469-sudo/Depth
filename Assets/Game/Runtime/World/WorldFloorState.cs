using System.Collections.Generic;
using FrontierDepths.Core;

namespace FrontierDepths.World
{
    public sealed class WorldFloorState
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

        public static WorldFloorState FromProfileRecord(WorldFloorProfileRecord record)
        {
            if (record == null)
            {
                return new WorldFloorState { floorNumber = 1, isUnlocked = true };
            }

            return new WorldFloorState
            {
                floorNumber = record.floorNumber,
                isUnlocked = record.isUnlocked,
                isCleared = record.isCleared,
                bossDefeated = record.bossDefeated,
                defeatedBossId = record.defeatedBossId ?? string.Empty,
                labyrinthEntranceKnown = record.labyrinthEntranceKnown,
                floorSeed = record.floorSeed,
                visitedSettlementIds = record.visitedSettlementIds != null
                    ? new List<string>(record.visitedSettlementIds)
                    : new List<string>(),
                unlockedTeleportGateIds = record.unlockedTeleportGateIds != null
                    ? new List<string>(record.unlockedTeleportGateIds)
                    : new List<string>()
            };
        }
    }
}
