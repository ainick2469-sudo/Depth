using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.World
{
    public static class WorldFloorCatalog
    {
        private static readonly WorldFloorDefinition[] Definitions =
        {
            new WorldFloorDefinition
            {
                floorNumber = 1,
                floorName = "Frontier Outpost",
                biomeTheme = "Grassland ruins / training plains",
                dangerTier = 1,
                hasMajorTown = true,
                hasMinorCamp = false,
                hasLabyrinth = true,
                majorSettlementId = "settlement.frontier_outpost",
                majorSettlementName = "Frontier Outpost",
                labyrinthId = "training_labyrinth",
                labyrinthName = "Training Labyrinth",
                bossId = "boss.candle_bound_warden",
                bossName = "Candle-Bound Warden",
                fieldEnemyPool = new[]
                {
                    "enemy.torchless_prisoner",
                    "enemy.candle_goblin",
                    "enemy.mold_covered_skeleton",
                    "enemy.rusty_dagger_ratfolk",
                    "enemy.starved_dungeon_wolf",
                    "enemy.coal_eyed_alley_cat",
                    "enemy.rust_bell_bat"
                },
                labyrinthEnemyPool = new[]
                {
                    "enemy.torchless_prisoner",
                    "enemy.candle_goblin",
                    "enemy.mold_covered_skeleton",
                    "enemy.dungeon_janitor_ghoul",
                    "enemy.rust_bell_bat"
                },
                specialRoomPool = new[] { "green_cache", "teal_scout", "gold_treasury", "white_safe" },
                worldSize = new Vector2Int(900, 900),
                townCount = 1,
                landmarkCount = 4,
                roadStyle = "Dust road training loop",
                weatherProfile = "Clear prairie winds",
                musicProfile = "music.frontier_outpost",
                visualPalette = "gold grass, gray ruin stone, warm lantern light"
            },
            new WorldFloorDefinition
            {
                floorNumber = 2,
                floorName = "Wolfroot Fields",
                biomeTheme = "Forest fields / ruined pasture",
                dangerTier = 2,
                hasMajorTown = false,
                hasMinorCamp = true,
                hasLabyrinth = true,
                minorCampId = "camp.wolfroot_field_camp",
                minorCampName = "Wolfroot Field Camp",
                labyrinthId = "rootcellar_den",
                labyrinthName = "Rootcellar Den",
                bossId = "boss.bone_mane_alpha",
                bossName = "Bone-Mane Alpha",
                fieldEnemyPool = new[]
                {
                    "enemy.starved_dungeon_wolf",
                    "enemy.candle_goblin",
                    "enemy.rusty_dagger_ratfolk",
                    "enemy.coal_eyed_alley_cat",
                    "enemy.rust_bell_bat"
                },
                labyrinthEnemyPool = new[]
                {
                    "enemy.starved_dungeon_wolf",
                    "enemy.mold_covered_skeleton",
                    "enemy.dungeon_janitor_ghoul",
                    "enemy.candle_goblin"
                },
                specialRoomPool = new[] { "green_cache", "purple_shrine", "teal_scout", "orange_trap" },
                worldSize = new Vector2Int(1100, 950),
                townCount = 0,
                landmarkCount = 5,
                roadStyle = "Overgrown wagon track",
                weatherProfile = "Clouded field wind",
                musicProfile = "music.wolfroot_fields",
                visualPalette = "green fields, dark roots, bone-white ruins"
            },
            new WorldFloorDefinition
            {
                floorNumber = 3,
                floorName = "Candlewood Swamp",
                biomeTheme = "Swamp / candle marsh",
                dangerTier = 3,
                hasMajorTown = false,
                hasMinorCamp = true,
                hasLabyrinth = true,
                minorCampId = "camp.candlewood_shrine",
                minorCampName = "Candlewood Shrine Camp",
                labyrinthId = "wickwater_labyrinth",
                labyrinthName = "Wickwater Labyrinth",
                bossId = "boss.lantern_hag",
                bossName = "Lantern Hag",
                fieldEnemyPool = new[]
                {
                    "enemy.candle_goblin",
                    "enemy.rust_bell_bat",
                    "enemy.crypt_lynx",
                    "enemy.lantern_cultist",
                    "enemy.mold_covered_skeleton"
                },
                labyrinthEnemyPool = new[]
                {
                    "enemy.lantern_cultist",
                    "enemy.bone_archer_initiate",
                    "enemy.candle_goblin",
                    "enemy.crypt_lynx"
                },
                specialRoomPool = new[] { "purple_shrine", "teal_scout", "black_curse", "white_safe" },
                worldSize = new Vector2Int(1050, 1050),
                townCount = 0,
                landmarkCount = 6,
                roadStyle = "Half-sunk plank road",
                weatherProfile = "Misty candle marsh",
                musicProfile = "music.candlewood_swamp",
                visualPalette = "mud green, candle gold, black water"
            },
            new WorldFloorDefinition
            {
                floorNumber = 4,
                floorName = "Prison Road",
                biomeTheme = "Bandit hills / broken prison road",
                dangerTier = 4,
                hasMajorTown = false,
                hasMinorCamp = true,
                hasLabyrinth = true,
                minorCampId = "camp.abandoned_checkpoint",
                minorCampName = "Abandoned Checkpoint",
                labyrinthId = "chainlock_hold",
                labyrinthName = "Chainlock Hold",
                bossId = "boss.shackled_jailer",
                bossName = "Shackled Jailer",
                fieldEnemyPool = new[]
                {
                    "enemy.chain_bound_thief",
                    "enemy.ash_eaten_prison_guard",
                    "enemy.rusty_dagger_ratfolk",
                    "enemy.candle_goblin",
                    "enemy.goblin_shield_rat"
                },
                labyrinthEnemyPool = new[]
                {
                    "enemy.chain_bound_thief",
                    "enemy.goblin_shield_rat",
                    "enemy.bone_archer_initiate",
                    "enemy.pickaxe_skeleton_miner",
                    "enemy.ash_eaten_prison_guard"
                },
                specialRoomPool = new[] { "red_elite", "orange_trap", "gold_treasury", "teal_scout" },
                worldSize = new Vector2Int(1200, 900),
                townCount = 0,
                landmarkCount = 5,
                roadStyle = "Broken prison road",
                weatherProfile = "Dry hill gusts",
                musicProfile = "music.prison_road",
                visualPalette = "dust brown, rust iron, prison stone"
            },
            new WorldFloorDefinition
            {
                floorNumber = 5,
                floorName = "Ironbell Crossing",
                biomeTheme = "Canyon / mining settlement",
                dangerTier = 5,
                hasMajorTown = true,
                hasMinorCamp = false,
                hasLabyrinth = true,
                majorSettlementId = "settlement.ironbell_crossing",
                majorSettlementName = "Ironbell Crossing",
                labyrinthId = "bellforge_labyrinth",
                labyrinthName = "Bellforge Labyrinth",
                bossId = "boss.rust_bell_executioner",
                bossName = "Rust Bell Executioner",
                fieldEnemyPool = new[]
                {
                    "enemy.pickaxe_skeleton_miner",
                    "enemy.ash_eaten_prison_guard",
                    "enemy.cursed_kennel_wolf",
                    "enemy.bone_archer_initiate",
                    "enemy.lantern_cultist"
                },
                labyrinthEnemyPool = new[]
                {
                    "enemy.pickaxe_skeleton_miner",
                    "enemy.goblin_shield_rat",
                    "enemy.bone_archer_initiate",
                    "enemy.cursed_kennel_wolf",
                    "enemy.chain_bound_thief"
                },
                specialRoomPool = new[] { "red_elite", "gold_treasury", "purple_shrine", "orange_trap", "white_safe" },
                worldSize = new Vector2Int(1300, 1000),
                townCount = 1,
                landmarkCount = 7,
                roadStyle = "Canyon switchback road",
                weatherProfile = "Dry canyon updrafts",
                musicProfile = "music.ironbell_crossing",
                visualPalette = "canyon red, iron gray, brass bell highlights"
            }
        };

        public static IReadOnlyList<WorldFloorDefinition> All => Definitions;

        public static WorldFloorDefinition Get(int floorNumber)
        {
            TryGet(floorNumber, out WorldFloorDefinition definition);
            return definition;
        }

        public static bool TryGet(int floorNumber, out WorldFloorDefinition definition)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].floorNumber == floorNumber)
                {
                    definition = Definitions[i];
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public static string GetFloorDisplayName(int floorNumber)
        {
            return TryGet(floorNumber, out WorldFloorDefinition definition)
                ? definition.floorName
                : $"Unknown Floor {Mathf.Max(1, floorNumber)}";
        }

        public static string GetLabyrinthDisplayName(int floorNumber)
        {
            return TryGet(floorNumber, out WorldFloorDefinition definition) && !string.IsNullOrWhiteSpace(definition.labyrinthName)
                ? definition.labyrinthName
                : "Unknown Labyrinth";
        }

        public static string GetDefaultSettlementId(int floorNumber)
        {
            return TryGet(floorNumber, out WorldFloorDefinition definition)
                ? definition.PrimarySettlementId
                : string.Empty;
        }
    }
}
