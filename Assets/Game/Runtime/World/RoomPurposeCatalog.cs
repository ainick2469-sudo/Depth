using System;
using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.World
{
    public enum RoomPurposeEffect
    {
        None,
        Cache,
        Shrine,
        Elite,
        Ambush,
        Wild,
        Fountain,
        Treasury,
        Armory,
        Sanctuary,
        CursedVault,
        Scout
    }

    public sealed class RoomPurposeDefinition
    {
        public string purposeId;
        public string displayName;
        public string prompt;
        public string resultText;
        public Color color;
        public int minFloor;
        public int maxFloor;
        public int gold;
        public int ammo;
        public float heal;
        public float healthRisk;
        public float temporaryBuffValue;
        public int reputation;
        public bool placeholder;
        public RoomPurposeEffect effect;
        public string minimapIcon;

        public bool IsEligible(int floorIndex)
        {
            return floorIndex >= Mathf.Max(1, minFloor) && (maxFloor <= 0 || floorIndex <= maxFloor);
        }
    }

    public static class RoomPurposeCatalog
    {
        private static readonly RoomPurposeDefinition[] Definitions =
        {
            Definition("green_cache", "Cache Room", "Open Cache", "Reliable supplies recovered.", new Color(0.25f, 0.9f, 0.38f), 1, 0, 8, 14, 10f, 0f, RoomPurposeEffect.Cache, "C"),
            Definition("purple_shrine", "Strange Shrine", "Make Sacrifice", "You trade blood for a strange blessing.", new Color(0.7f, 0.3f, 0.95f), 1, 0, 18, 4, 0f, 8f, RoomPurposeEffect.Shrine, "S"),
            Definition("red_elite", "Elite Den", "Claim War Trophy", "A battle trophy converts danger into reputation.", new Color(0.95f, 0.22f, 0.16f), 3, 0, 28, 0, 0f, 0f, RoomPurposeEffect.Elite, "E"),
            Definition("orange_ambush", "Ambush Room", "Spring Trap Cache", "You spring the trap and learn from the ambush.", new Color(1f, 0.48f, 0.12f), 2, 0, 12, 8, 0f, 4f, RoomPurposeEffect.Ambush, "!"),
            Definition("rainbow_wild", "Wild Room", "Open Wild Cache", "The room rolls a wildcard outcome.", new Color(0.95f, 0.75f, 1f), 4, 0, 8, 8, 0f, 0f, RoomPurposeEffect.Wild, "?"),
            Definition("blue_fountain", "Fountain Room", "Drink From Fountain", "Clean water restores and steadies you.", new Color(0.25f, 0.58f, 1f), 2, 0, 0, 0, 26f, 0f, RoomPurposeEffect.Fountain, "+"),
            Definition("gold_treasury", "Treasury", "Open Treasury Cache", "Gold. Finally, a room with manners.", new Color(1f, 0.78f, 0.22f), 3, 0, 65, 0, 0f, 0f, RoomPurposeEffect.Treasury, "$"),
            Definition("cyan_armory", "Armory", "Open Armory Crate", "Weapon supplies improve your next stretch.", new Color(0.35f, 0.9f, 0.95f), 2, 0, 4, 30, 0f, 0f, RoomPurposeEffect.Armory, "A"),
            Definition("white_sanctuary", "Sanctuary", "Rest At Sanctuary", "A safe blessing, no teeth attached.", new Color(0.95f, 0.92f, 0.82f), 4, 0, 0, 6, 18f, 0f, RoomPurposeEffect.Sanctuary, "W"),
            Definition("black_vault", "Cursed Vault", "Open Cursed Vault", "High reward, real cost, no polite lies.", new Color(0.08f, 0.07f, 0.09f), 5, 0, 80, 18, 0f, 14f, RoomPurposeEffect.CursedVault, "V"),
            Definition("teal_scout", "Scout Room", "Read Survey Map", "Nearby routes and the stair are marked.", new Color(0.2f, 0.75f, 0.68f), 2, 0, 0, 0, 0f, 0f, RoomPurposeEffect.Scout, "M")
        };

        public static RoomPurposeDefinition[] All => Definitions;

        public static RoomPurposeDefinition Get(string purposeId)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (string.Equals(Definitions[i].purposeId, purposeId, StringComparison.Ordinal))
                {
                    return Definitions[i];
                }
            }

            return null;
        }

        public static RoomPurposeDefinition Choose(DungeonNodeKind nodeKind, int floorIndex, int floorSeed, string nodeId)
        {
            return Choose(nodeKind, floorIndex, floorSeed, nodeId, null);
        }

        public static RoomPurposeDefinition Choose(DungeonNodeKind nodeKind, int floorIndex, int floorSeed, string nodeId, IReadOnlyDictionary<string, int> purposeUsageCounts)
        {
            if (nodeKind == DungeonNodeKind.Landmark)
            {
                return PickFrom(floorIndex, floorSeed, nodeId, purposeUsageCounts, "green_cache", "red_elite", "gold_treasury", "cyan_armory");
            }

            if (nodeKind == DungeonNodeKind.Secret)
            {
                return PickFrom(floorIndex, floorSeed, nodeId, purposeUsageCounts, "purple_shrine", "black_vault", "rainbow_wild", "teal_scout");
            }

            if (nodeKind != DungeonNodeKind.Ordinary)
            {
                return null;
            }

            int roll = StableRoll(floorSeed, nodeId, 100);
            int chance = floorIndex <= 1 ? 8 : (floorIndex <= 3 ? 14 : (floorIndex <= 8 ? 20 : 26));
            if (roll >= chance)
            {
                return null;
            }

            return PickFrom(floorIndex, floorSeed, nodeId, purposeUsageCounts, "blue_fountain", "orange_ambush", "white_sanctuary", "teal_scout");
        }

        public static int GetMaxPerFloor(RoomPurposeDefinition definition, int floorIndex)
        {
            if (definition == null)
            {
                return 0;
            }

            if (definition.effect == RoomPurposeEffect.Scout && floorIndex < 15)
            {
                return 1;
            }

            if (floorIndex < 10)
            {
                return 1;
            }

            if (floorIndex < 20)
            {
                return 2;
            }

            return definition.effect == RoomPurposeEffect.Scout ? 2 : 3;
        }

        public static bool IsUnderFloorCap(RoomPurposeDefinition definition, int floorIndex, IReadOnlyDictionary<string, int> purposeUsageCounts)
        {
            if (definition == null)
            {
                return false;
            }

            int count = 0;
            purposeUsageCounts?.TryGetValue(definition.purposeId, out count);
            return count < GetMaxPerFloor(definition, floorIndex);
        }

        private static RoomPurposeDefinition PickFrom(int floorIndex, int floorSeed, string nodeId, IReadOnlyDictionary<string, int> purposeUsageCounts, params string[] purposeIds)
        {
            RoomPurposeDefinition fallback = null;
            int roll = StableRoll(floorSeed, nodeId, Mathf.Max(1, purposeIds.Length));
            for (int offset = 0; offset < purposeIds.Length; offset++)
            {
                RoomPurposeDefinition definition = Get(purposeIds[(roll + offset) % purposeIds.Length]);
                fallback ??= definition;
                if (definition != null && definition.IsEligible(floorIndex) && IsUnderFloorCap(definition, floorIndex, purposeUsageCounts))
                {
                    return definition;
                }
            }

            return fallback != null && fallback.IsEligible(floorIndex) && IsUnderFloorCap(fallback, floorIndex, purposeUsageCounts)
                ? fallback
                : null;
        }

        private static RoomPurposeDefinition Definition(
            string id,
            string name,
            string prompt,
            string result,
            Color color,
            int minFloor,
            int maxFloor,
            int gold,
            int ammo,
            float heal,
            float risk,
            RoomPurposeEffect effect,
            string icon)
        {
            return new RoomPurposeDefinition
            {
                purposeId = id,
                displayName = name,
                prompt = prompt,
                resultText = result,
                color = color,
                minFloor = minFloor,
                maxFloor = maxFloor,
                gold = gold,
                ammo = ammo,
                heal = heal,
                healthRisk = risk,
                effect = effect,
                minimapIcon = icon
            };
        }

        private static int StableRoll(int seed, string nodeId, int modulo)
        {
            unchecked
            {
                int hash = seed == 0 ? 17 : seed;
                if (!string.IsNullOrWhiteSpace(nodeId))
                {
                    for (int i = 0; i < nodeId.Length; i++)
                    {
                        hash = hash * 31 + nodeId[i];
                    }
                }

                return Mathf.Abs(hash) % Mathf.Max(1, modulo);
            }
        }
    }
}
