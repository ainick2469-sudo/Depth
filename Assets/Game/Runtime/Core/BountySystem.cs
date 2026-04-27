using System;
using System.Collections.Generic;

namespace FrontierDepths.Core
{
    public enum BountyState
    {
        Available,
        Accepted,
        Spawned,
        Killed,
        TurnedIn
    }

    [Serializable]
    public sealed class BountyDefinition
    {
        public string bountyId;
        public string title;
        public string targetName;
        public string targetArchetype;
        public int minFloor;
        public int maxFloor;
        public int goldReward;
        public int xpReward;
        public int reputationRequired;
        public string reason;
        public string markerColor;
        public float healthMultiplier = 1.4f;
        public float damageMultiplier = 1.15f;
        public float speedMultiplier = 1.05f;
        public float scaleMultiplier = 1.18f;

        public bool IsEligibleForFloor(int floorIndex)
        {
            return floorIndex >= Math.Max(1, minFloor) && floorIndex <= Math.Max(minFloor, maxFloor);
        }
    }

    [Serializable]
    public sealed class BountyRuntimeState
    {
        public string bountyId = string.Empty;
        public BountyState state = BountyState.Available;
        public int spawnedFloorIndex;
        public string targetRoomId = string.Empty;
        public string targetInstanceId = string.Empty;

        public void Normalize()
        {
            bountyId ??= string.Empty;
            targetRoomId ??= string.Empty;
            targetInstanceId ??= string.Empty;
            if (state < BountyState.Available || state > BountyState.TurnedIn)
            {
                state = BountyState.Available;
            }
        }
    }

    public static class BountyCatalog
    {
        private static readonly BountyDefinition[] Definitions =
        {
            new BountyDefinition
            {
                bountyId = "bounty.lantern_eater_slime",
                title = "Wanted: Lantern-Eater Slime",
                targetName = "Lantern-Eater Slime",
                targetArchetype = "Slime",
                minFloor = 2,
                maxFloor = 3,
                goldReward = 85,
                xpReward = 35,
                reputationRequired = 0,
                reason = "It has been swallowing survey lamps and leaving miners blind in the dark.",
                markerColor = "#88FF66"
            },
            new BountyDefinition
            {
                bountyId = "bounty.redfang_goblin_scout",
                title = "Wanted: Redfang Goblin Scout",
                targetName = "Redfang Goblin Scout",
                targetArchetype = "GoblinGrunt",
                minFloor = 3,
                maxFloor = 5,
                goldReward = 140,
                xpReward = 60,
                reputationRequired = 0,
                reason = "Tagged three caravans, stole the powder, and laughed about it.",
                markerColor = "#FF6538"
            },
            new BountyDefinition
            {
                bountyId = "bounty.hollow_brute",
                title = "Wanted: Hollow Brute",
                targetName = "Hollow Brute",
                targetArchetype = "GoblinBrute",
                minFloor = 5,
                maxFloor = 8,
                goldReward = 260,
                xpReward = 120,
                reputationRequired = ReputationService.KnownHandThreshold,
                reason = "Guarding the lower lift with a hammer made from a mine cart axle.",
                markerColor = "#C84535"
            }
        };

        public static IReadOnlyList<BountyDefinition> All => Definitions;

        public static BountyDefinition Get(string bountyId)
        {
            if (string.IsNullOrWhiteSpace(bountyId))
            {
                return null;
            }

            for (int i = 0; i < Definitions.Length; i++)
            {
                if (string.Equals(Definitions[i].bountyId, bountyId, StringComparison.Ordinal))
                {
                    return Definitions[i];
                }
            }

            return null;
        }

        public static bool IsVisible(ProfileState profile, BountyDefinition bounty)
        {
            if (bounty == null)
            {
                return false;
            }

            BountyRuntimeState state = profile != null ? BountyObjectiveTracker.GetOrCreate(profile, bounty.bountyId) : null;
            if (state != null && state.state != BountyState.Available)
            {
                return true;
            }

            return profile == null || profile.townReputation >= Math.Max(0, bounty.reputationRequired);
        }
    }

    public static class BountyObjectiveTracker
    {
        public const int MaxActiveBounties = 3;

        public static BountyRuntimeState GetOrCreate(ProfileState profile, string bountyId)
        {
            if (profile == null || string.IsNullOrWhiteSpace(bountyId))
            {
                return null;
            }

            profile.bounties ??= new List<BountyRuntimeState>();
            for (int i = 0; i < profile.bounties.Count; i++)
            {
                if (profile.bounties[i] != null && string.Equals(profile.bounties[i].bountyId, bountyId, StringComparison.Ordinal))
                {
                    profile.bounties[i].Normalize();
                    return profile.bounties[i];
                }
            }

            BountyRuntimeState state = new BountyRuntimeState { bountyId = bountyId };
            profile.bounties.Add(state);
            return state;
        }

        public static int CountActive(ProfileState profile)
        {
            int count = 0;
            if (profile == null || profile.bounties == null)
            {
                return 0;
            }

            for (int i = 0; i < profile.bounties.Count; i++)
            {
                BountyRuntimeState state = profile.bounties[i];
                if (state != null && (state.state == BountyState.Accepted || state.state == BountyState.Spawned))
                {
                    count++;
                }
            }

            return count;
        }

        public static bool CanAccept(ProfileState profile, string bountyId, out string reason)
        {
            BountyDefinition definition = BountyCatalog.Get(bountyId);
            if (definition == null)
            {
                reason = "Unknown bounty.";
                return false;
            }

            BountyRuntimeState state = GetOrCreate(profile, bountyId);
            if (state.state == BountyState.TurnedIn)
            {
                reason = "Already completed.";
                return false;
            }

            if (state.state != BountyState.Available)
            {
                reason = "Already accepted.";
                return false;
            }

            if (CountActive(profile) >= MaxActiveBounties)
            {
                reason = "Too many active bounties.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public static bool MarkAccepted(ProfileState profile, string bountyId, out string reason)
        {
            if (!CanAccept(profile, bountyId, out reason))
            {
                return false;
            }

            BountyRuntimeState state = GetOrCreate(profile, bountyId);
            state.state = BountyState.Accepted;
            reason = "Bounty accepted.";
            return true;
        }

        public static void MarkSpawned(ProfileState profile, string bountyId, int floorIndex, string roomId, string instanceId)
        {
            BountyRuntimeState state = GetOrCreate(profile, bountyId);
            if (state == null || state.state == BountyState.Killed || state.state == BountyState.TurnedIn)
            {
                return;
            }

            state.state = BountyState.Spawned;
            state.spawnedFloorIndex = floorIndex;
            state.targetRoomId = roomId ?? string.Empty;
            state.targetInstanceId = instanceId ?? string.Empty;
        }

        public static bool MarkKilled(ProfileState profile, string bountyId)
        {
            BountyRuntimeState state = GetOrCreate(profile, bountyId);
            if (state == null || (state.state != BountyState.Accepted && state.state != BountyState.Spawned))
            {
                return false;
            }

            state.state = BountyState.Killed;
            return true;
        }

        public static bool TryTurnIn(ProfileState profile, string bountyId, out BountyDefinition definition, out string reason)
        {
            definition = BountyCatalog.Get(bountyId);
            BountyRuntimeState state = GetOrCreate(profile, bountyId);
            if (definition == null || state == null)
            {
                reason = "Unknown bounty.";
                return false;
            }

            if (state.state != BountyState.Killed)
            {
                reason = state.state == BountyState.TurnedIn ? "Already claimed." : "Target not slain yet.";
                return false;
            }

            state.state = BountyState.TurnedIn;
            reason = "Bounty reward claimed.";
            return true;
        }
    }
}
