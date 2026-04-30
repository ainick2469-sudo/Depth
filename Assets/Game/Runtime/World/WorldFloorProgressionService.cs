using System;
using FrontierDepths.Core;

namespace FrontierDepths.World
{
    public sealed class WorldFloorProgressionService
    {
        private readonly ProfileState profile;
        private readonly Action saveAction;

        public WorldFloorProgressionService(ProfileService profileService)
            : this(profileService != null ? profileService.Current : null, profileService != null ? profileService.Save : null)
        {
        }

        public WorldFloorProgressionService(ProfileState profileState, Action saveAction = null)
        {
            profile = profileState ?? throw new ArgumentNullException(nameof(profileState));
            this.saveAction = saveAction;
            EnsureState();
        }

        public int CurrentWorldFloor => EnsureState().currentWorldFloor;
        public int HighestUnlockedWorldFloor => EnsureState().highestUnlockedWorldFloor;

        public bool IsFloorUnlocked(int floor)
        {
            if (floor <= 0)
            {
                return false;
            }

            WorldFloorProgressionProfileState state = EnsureState();
            WorldFloorProfileRecord record = state.FindFloorRecord(floor);
            return floor <= state.highestUnlockedWorldFloor || (record != null && record.isUnlocked);
        }

        public bool IsFloorCleared(int floor)
        {
            WorldFloorProfileRecord record = floor > 0 ? EnsureState().FindFloorRecord(floor) : null;
            return record != null && record.isCleared;
        }

        public void UnlockFloor(int floor)
        {
            UnlockFloorInternal(floor);
        }

        public void MarkBossDefeated(int floor, string bossId)
        {
            if (floor <= 0)
            {
                return;
            }

            WorldFloorProgressionProfileState state = EnsureState();
            WorldFloorProfileRecord record = state.GetOrCreateFloorRecord(floor);
            record.bossDefeated = true;
            record.defeatedBossId = bossId ?? string.Empty;
            record.isCleared = true;

            if (WorldFloorCatalog.TryGet(floor, out WorldFloorDefinition definition) &&
                string.Equals(definition.bossId, bossId, StringComparison.Ordinal) &&
                WorldFloorCatalog.TryGet(floor + 1, out _))
            {
                UnlockFloorInternal(floor + 1, false);
            }

            Save();
        }

        public bool IsBossDefeated(int floor)
        {
            WorldFloorProfileRecord record = floor > 0 ? EnsureState().FindFloorRecord(floor) : null;
            return record != null && record.bossDefeated;
        }

        public void MarkLabyrinthEntranceKnown(int floor)
        {
            if (floor <= 0)
            {
                return;
            }

            EnsureState().GetOrCreateFloorRecord(floor).labyrinthEntranceKnown = true;
            Save();
        }

        public bool IsLabyrinthEntranceKnown(int floor)
        {
            WorldFloorProfileRecord record = floor > 0 ? EnsureState().FindFloorRecord(floor) : null;
            return record != null && record.labyrinthEntranceKnown;
        }

        public void MarkSettlementVisited(int floor, string settlementId)
        {
            if (floor <= 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(settlementId))
            {
                settlementId = WorldFloorCatalog.GetDefaultSettlementId(floor);
            }

            EnsureState().GetOrCreateFloorRecord(floor).MarkSettlementVisited(settlementId);
            Save();
        }

        public bool IsSettlementVisited(int floor, string settlementId)
        {
            if (floor <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(settlementId))
            {
                settlementId = WorldFloorCatalog.GetDefaultSettlementId(floor);
            }

            WorldFloorProfileRecord record = EnsureState().FindFloorRecord(floor);
            return record != null && record.HasVisitedSettlement(settlementId);
        }

        public void UnlockTeleportGate(int floor, string gateId)
        {
            if (floor <= 0)
            {
                return;
            }

            EnsureState().GetOrCreateFloorRecord(floor).UnlockTeleportGate(gateId);
            Save();
        }

        public bool IsTeleportGateUnlocked(int floor, string gateId)
        {
            WorldFloorProfileRecord record = floor > 0 ? EnsureState().FindFloorRecord(floor) : null;
            return record != null && record.HasTeleportGate(gateId);
        }

        public int GetOrCreateFloorSeed(int floor)
        {
            floor = Math.Max(1, floor);
            WorldFloorProgressionProfileState state = EnsureState();
            WorldFloorProfileRecord record = state.GetOrCreateFloorRecord(floor);
            if (record.floorSeed == 0)
            {
                record.floorSeed = BuildFloorSeed(state.worldSeed, floor);
                Save();
            }

            return record.floorSeed;
        }

        public WorldFloorState GetOrCreateState(int floor)
        {
            WorldFloorProfileRecord record = EnsureState().GetOrCreateFloorRecord(Math.Max(1, floor));
            return WorldFloorState.FromProfileRecord(record);
        }

        private void UnlockFloorInternal(int floor, bool save = true)
        {
            if (floor <= 0)
            {
                return;
            }

            WorldFloorProgressionProfileState state = EnsureState();
            WorldFloorProfileRecord record = state.GetOrCreateFloorRecord(floor);
            record.isUnlocked = true;
            state.highestUnlockedWorldFloor = Math.Max(state.highestUnlockedWorldFloor, floor);
            if (save)
            {
                Save();
            }
        }

        private WorldFloorProgressionProfileState EnsureState()
        {
            profile.worldFloorProgression ??= new WorldFloorProgressionProfileState();
            profile.worldFloorProgression.Normalize();
            return profile.worldFloorProgression;
        }

        private void Save()
        {
            EnsureState();
            saveAction?.Invoke();
        }

        private static int BuildFloorSeed(int worldSeed, int floor)
        {
            unchecked
            {
                int mixed = worldSeed ^ (floor * 1009) ^ 0x5bd1e995;
                if (mixed == int.MinValue)
                {
                    return int.MaxValue;
                }

                mixed = Math.Abs(mixed);
                return mixed == 0 ? floor * 1009 + 1 : mixed;
            }
        }
    }
}
