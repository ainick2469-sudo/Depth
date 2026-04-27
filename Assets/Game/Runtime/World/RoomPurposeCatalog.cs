using System;
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
            Definition("green_cache", "Cache Room", "Open Cache", "Useful supplies recovered.", new Color(0.25f, 0.9f, 0.38f), 1, 0, 8, 14, 10f, 0f, RoomPurposeEffect.Cache, "C"),
            Definition("purple_shrine", "Strange Shrine", "Touch Strange Shrine", "The shrine answers with a costly blessing.", new Color(0.7f, 0.3f, 0.95f), 1, 0, 28, 8, 0f, 8f, RoomPurposeEffect.Shrine, "S"),
            Definition("red_elite", "Elite Den", "Claim War Cache", "A dangerous cache from a harder fight.", new Color(0.95f, 0.22f, 0.16f), 3, 0, 38, 8, 0f, 0f, RoomPurposeEffect.Elite, "E"),
            Definition("orange_ambush", "Ambush Room", "Spring Ambush Cache", "You grab the bait before the dungeon notices.", new Color(1f, 0.48f, 0.12f), 2, 0, 22, 10, 0f, 4f, RoomPurposeEffect.Ambush, "!"),
            Definition("rainbow_wild", "Wild Room", "Open Wild Cache", "Something useful. Probably.", new Color(0.95f, 0.75f, 1f), 4, 0, 18, 18, 8f, 3f, RoomPurposeEffect.Wild, "?"),
            Definition("blue_fountain", "Fountain Room", "Drink From Fountain", "Clean water cuts through the dust.", new Color(0.25f, 0.58f, 1f), 2, 0, 0, 0, 22f, 0f, RoomPurposeEffect.Fountain, "+"),
            Definition("gold_treasury", "Treasury", "Open Treasury Cache", "Gold. Finally, a room with manners.", new Color(1f, 0.78f, 0.22f), 3, 0, 55, 0, 0f, 0f, RoomPurposeEffect.Treasury, "$"),
            Definition("cyan_armory", "Armory", "Open Armory Crate", "Ammunition and weapon supplies.", new Color(0.35f, 0.9f, 0.95f), 2, 0, 8, 26, 0f, 0f, RoomPurposeEffect.Armory, "A"),
            Definition("white_sanctuary", "Sanctuary", "Rest At Sanctuary", "A quiet room that does not immediately hate you.", new Color(0.95f, 0.92f, 0.82f), 4, 0, 0, 6, 18f, 0f, RoomPurposeEffect.Sanctuary, "W"),
            Definition("black_vault", "Cursed Vault", "Open Cursed Vault", "A richer reward with a shadow attached.", new Color(0.08f, 0.07f, 0.09f), 5, 0, 70, 18, 0f, 14f, RoomPurposeEffect.CursedVault, "V"),
            Definition("teal_scout", "Scout Room", "Read Survey Map", "Nearby routes are marked on your map.", new Color(0.2f, 0.75f, 0.68f), 2, 0, 6, 8, 0f, 0f, RoomPurposeEffect.Scout, "M")
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
            if (nodeKind == DungeonNodeKind.Landmark)
            {
                return PickFrom(floorIndex, floorSeed, nodeId, "green_cache", "red_elite", "gold_treasury", "cyan_armory");
            }

            if (nodeKind == DungeonNodeKind.Secret)
            {
                return PickFrom(floorIndex, floorSeed, nodeId, "purple_shrine", "black_vault", "rainbow_wild", "teal_scout");
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

            return PickFrom(floorIndex, floorSeed, nodeId, "blue_fountain", "orange_ambush", "white_sanctuary", "teal_scout");
        }

        private static RoomPurposeDefinition PickFrom(int floorIndex, int floorSeed, string nodeId, params string[] purposeIds)
        {
            RoomPurposeDefinition fallback = null;
            int roll = StableRoll(floorSeed, nodeId, Mathf.Max(1, purposeIds.Length));
            for (int offset = 0; offset < purposeIds.Length; offset++)
            {
                RoomPurposeDefinition definition = Get(purposeIds[(roll + offset) % purposeIds.Length]);
                fallback ??= definition;
                if (definition != null && definition.IsEligible(floorIndex))
                {
                    return definition;
                }
            }

            return fallback;
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
