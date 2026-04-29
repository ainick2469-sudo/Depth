using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.Combat
{
    public static class EnemyCatalog
    {
        public static EnemyDefinition CreateDefinition(EnemyArchetype archetype)
        {
            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            Configure(definition, archetype);
            return definition;
        }

        public static List<EnemyDefinition> CreateDefaultDefinitions()
        {
            List<EnemyDefinition> definitions = new List<EnemyDefinition>
            {
                CreateDefinition(EnemyArchetype.Slime),
                CreateDefinition(EnemyArchetype.SpitterSlime),
                CreateDefinition(EnemyArchetype.Bat),
                CreateDefinition(EnemyArchetype.GoblinGrunt),
                CreateDefinition(EnemyArchetype.GoblinBrute),
                CreateDefinition(EnemyArchetype.CaveRat),
                CreateDefinition(EnemyArchetype.DustMite),
                CreateDefinition(EnemyArchetype.TrainingSkeleton),
                CreateDefinition(EnemyArchetype.Skitter),
                CreateDefinition(EnemyArchetype.BoneArcher),
                CreateDefinition(EnemyArchetype.ShieldGoblin),
                CreateDefinition(EnemyArchetype.BombBeetle),
                CreateDefinition(EnemyArchetype.OrcGrunt),
                CreateDefinition(EnemyArchetype.RiftHound),
                CreateDefinition(EnemyArchetype.Stoneback),
                CreateDefinition(EnemyArchetype.HexCaster),
                CreateDefinition(EnemyArchetype.IronOgre),
                CreateDefinition(EnemyArchetype.HexWitch),
                CreateDefinition(EnemyArchetype.GraveKnight),
                CreateDefinition(EnemyArchetype.RiftStalker),
                CreateDefinition(EnemyArchetype.TorchlessPrisoner),
                CreateDefinition(EnemyArchetype.CandleGoblin),
                CreateDefinition(EnemyArchetype.MoldCoveredSkeleton),
                CreateDefinition(EnemyArchetype.RustyDaggerRatfolk),
                CreateDefinition(EnemyArchetype.DungeonJanitorGhoul),
                CreateDefinition(EnemyArchetype.StarvedDungeonWolf),
                CreateDefinition(EnemyArchetype.CoalEyedAlleyCat),
                CreateDefinition(EnemyArchetype.RustBellBat),
                CreateDefinition(EnemyArchetype.ChainBoundThief),
                CreateDefinition(EnemyArchetype.GoblinShieldRat),
                CreateDefinition(EnemyArchetype.BoneArcherInitiate),
                CreateDefinition(EnemyArchetype.LanternCultist),
                CreateDefinition(EnemyArchetype.PickaxeSkeletonMiner),
                CreateDefinition(EnemyArchetype.CursedKennelWolf),
                CreateDefinition(EnemyArchetype.CryptLynx),
                CreateDefinition(EnemyArchetype.AshEatenPrisonGuard),
                CreateDefinition(EnemyArchetype.GoblinTripwireTrapper),
                CreateDefinition(EnemyArchetype.BarrelHeadBandit),
                CreateDefinition(EnemyArchetype.SewerKnifeTwin),
                CreateDefinition(EnemyArchetype.RottenBellRinger),
                CreateDefinition(EnemyArchetype.CrossbowGoblin),
                CreateDefinition(EnemyArchetype.BoneManeWolf),
                CreateDefinition(EnemyArchetype.DungeonRam),
                CreateDefinition(EnemyArchetype.MossbackBearCub)
            };

            return definitions;
        }

        public static List<EnemyDefinition> CreateDebugDefinitions()
        {
            return CreateDefaultDefinitions();
        }

        public static List<EnemyDefinition> CreateDefinitionsForFloor(int floorIndex)
        {
            List<EnemyDefinition> definitions = CreateDefaultDefinitions();
            for (int i = definitions.Count - 1; i >= 0; i--)
            {
                if (definitions[i] == null || !definitions[i].IsEligibleForNormalSpawn(floorIndex))
                {
                    definitions.RemoveAt(i);
                }
            }

            return definitions;
        }

        public static EnemyFloorBand GetFloorBand(int floorIndex)
        {
            int floor = Mathf.Max(1, floorIndex);
            if (floor <= 3)
            {
                return EnemyFloorBand.RecruitDungeon;
            }

            if (floor <= 7)
            {
                return EnemyFloorBand.OrganizedDungeon;
            }

            if (floor <= 12)
            {
                return EnemyFloorBand.TacticalDungeon;
            }

            if (floor <= 18)
            {
                return EnemyFloorBand.CursedOrders;
            }

            if (floor <= 25)
            {
                return EnemyFloorBand.GothicHunters;
            }

            return EnemyFloorBand.DeepHorrors;
        }

        public static bool IsRetiredFromNormalSpawns(EnemyArchetype archetype)
        {
            return archetype == EnemyArchetype.Slime || archetype == EnemyArchetype.SpitterSlime;
        }

        private static void Configure(EnemyDefinition definition, EnemyArchetype archetype)
        {
            definition.archetype = archetype;
            definition.spawnAvailability = EnemySpawnAvailability.Active;
            definition.bountyEligible = true;
            definition.attackImplementationNote = "Uses existing simple enemy controller behavior.";
            definition.ammoDropChance = 0f;
            definition.ammoAmount = 0;

            switch (archetype)
            {
                case EnemyArchetype.Slime:
                    ConfigureDefinition(definition, "enemy.slime", "Slime", 1, 36f, 3.6f, 6f, 1.8f, 1.35f, 0.22f, 32f,
                        EnemyAmbientBehavior.Wander, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.BasicMelee, "debug_squat_blob",
                        new Color(0.24f, 0.72f, 0.32f, 1f), new Vector3(1.45f, 0.85f, 1.45f), 0f, 1, 0,
                        0.8f, 28f, 90f, 0.65f, 0.9f, 1.05f, 0.75f, 0.75f, 0.16f, 3, 8, 0.12f, 8f,
                        EnemyBodyPlan.Quadruped, EnemyFaction.DeepHorror, EnemyCombatRole.Swarmer, EnemySpawnAvailability.DebugOnly, false,
                        "Debug-only retired blob; preserved for compatibility tests.");
                    break;
                case EnemyArchetype.SpitterSlime:
                    ConfigureDefinition(definition, "enemy.spitter_slime", "Spitter Slime", 2, 50f, 3.2f, 8f, 2.35f, 1.45f, 0.32f, 44f,
                        EnemyAmbientBehavior.Wander, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.RangedProjectile, "debug_spitter_blob",
                        new Color(0.18f, 0.74f, 0.56f, 1f), new Vector3(1.3f, 1.05f, 1.3f), 0f, 3, 0,
                        1f, 30f, 120f, 0.58f, 0.82f, 1f, 0.72f, 1.25f, 0.1f, 6, 14, 0.1f, 8f,
                        EnemyBodyPlan.Quadruped, EnemyFaction.DeepHorror, EnemyCombatRole.Archer, EnemySpawnAvailability.DebugOnly, false,
                        "Debug-only retired ranged blob; normal gameplay must not spawn it.");
                    break;
                case EnemyArchetype.Bat:
                    ConfigureDefinition(definition, "enemy.bat", "Bat", 1, 24f, 7.5f, 6f, 1.7f, 0.95f, 0.14f, 42f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Roamer, EnemyAttackFamily.LeapingBite, "winged_small",
                        new Color(0.18f, 0.17f, 0.28f, 1f), new Vector3(0.85f, 0.95f, 0.85f), 12f, 1, 10,
                        1.2f, 38f, 150f, 0.9f, 1.1f, 1.25f, 1f, 0.85f, 0.12f, 3, 7, 0.05f, 6f,
                        EnemyBodyPlan.Flying, EnemyFaction.Beast, EnemyCombatRole.Swarmer, EnemySpawnAvailability.Active, true,
                        "Forces player to track vertical fast movement.");
                    break;
                case EnemyArchetype.GoblinGrunt:
                    ConfigureDefinition(definition, "enemy.goblin_grunt", "Goblin Grunt", 2, 72f, 5.4f, 11f, 2.2f, 1.15f, 0.28f, 48f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.BasicMelee, "goblin",
                        new Color(0.76f, 0.32f, 0.17f, 1f), new Vector3(1.25f, 1.55f, 1.25f), 8f, 1, 8,
                        1f, 34f, 120f, 0.8f, 1.05f, 1.2f, 0.9f, 1.25f, 0.3f, 6, 14, 0.08f, 10f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Goblin, EnemyCombatRole.Grunt, EnemySpawnAvailability.Active, true,
                        "Forces player to handle a standard humanoid rush.");
                    break;
                case EnemyArchetype.GoblinBrute:
                    ConfigureDefinition(definition, "enemy.goblin_brute", "Goblin Brute", 3, 126f, 3.9f, 20f, 2.6f, 1.65f, 0.45f, 45f,
                        EnemyAmbientBehavior.SleepGuard, EnemyMobilityRole.Sleeper, EnemyAttackFamily.Charge, "brute",
                        new Color(0.42f, 0.15f, 0.12f, 1f), new Vector3(1.65f, 2.05f, 1.65f), 4f, 3, 10,
                        0.9f, 36f, 100f, 0.5f, 0.85f, 1.15f, 0.7f, 2.25f, 1f, 12, 24, 0.18f, 14f,
                        EnemyBodyPlan.EliteHumanoid, EnemyFaction.Goblin, EnemyCombatRole.Brute, EnemySpawnAvailability.Active, true,
                        "Forces player to respect slow heavy windups.");
                    break;
                case EnemyArchetype.CaveRat:
                    ConfigureDefinition(definition, "enemy.cave_rat", "Cave Rat", 1, 20f, 6.4f, 5f, 1.55f, 0.85f, 0.11f, 34f,
                        EnemyAmbientBehavior.Wander, EnemyMobilityRole.Roamer, EnemyAttackFamily.LeapingBite, "low_quadruped",
                        new Color(0.45f, 0.38f, 0.28f, 1f), new Vector3(0.7f, 0.55f, 1.05f), 6f, 1, 5,
                        1f, 24f, 120f, 0.9f, 1.1f, 1.18f, 1f, 0.7f, 0.12f, 2, 7, 0.05f, 6f,
                        EnemyBodyPlan.Quadruped, EnemyFaction.Ratfolk, EnemyCombatRole.Swarmer, EnemySpawnAvailability.Active, true,
                        "Forces player to react to low fast pressure.");
                    break;
                case EnemyArchetype.DustMite:
                    ConfigureDefinition(definition, "enemy.dust_mite", "Dust Mite", 1, 14f, 5.8f, 4f, 1.35f, 0.72f, 0.08f, 30f,
                        EnemyAmbientBehavior.Wander, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.LeapingBite, "tiny_quadruped",
                        new Color(0.72f, 0.65f, 0.46f, 1f), new Vector3(0.5f, 0.45f, 0.5f), 4f, 1, 4,
                        0.85f, 22f, 145f, 0.95f, 1.08f, 1.16f, 0.95f, 0.55f, 0.04f, 1, 5, 0.04f, 5f,
                        EnemyBodyPlan.Quadruped, EnemyFaction.Beast, EnemyCombatRole.Swarmer, EnemySpawnAvailability.Active, false,
                        "Forces player to notice tiny skittering movement.");
                    break;
                case EnemyArchetype.TrainingSkeleton:
                    ConfigureDefinition(definition, "enemy.training_skeleton", "Training Skeleton", 1, 44f, 3.2f, 8f, 2f, 1.35f, 0.35f, 36f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.BasicMelee, "humanoid_bone",
                        new Color(0.78f, 0.76f, 0.66f, 1f), new Vector3(1.0f, 1.65f, 0.8f), 7f, 1, 8,
                        0.75f, 25f, 100f, 0.62f, 0.82f, 1f, 0.72f, 0.95f, 0.18f, 4, 10, 0.08f, 8f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Undead, EnemyCombatRole.Grunt, EnemySpawnAvailability.Active, true,
                        "Forces player to land repeated shots.");
                    break;
                case EnemyArchetype.Skitter:
                    ConfigureDefinition(definition, "enemy.skitter", "Skitter", 1, 26f, 6.8f, 6f, 1.65f, 0.82f, 0.1f, 38f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Roamer, EnemyAttackFamily.LeapingBite, "low_quadruped_spider",
                        new Color(0.22f, 0.23f, 0.18f, 1f), new Vector3(0.95f, 0.55f, 0.95f), 6f, 2, 8,
                        1.15f, 30f, 150f, 0.9f, 1.1f, 1.22f, 1f, 0.8f, 0.05f, 3, 8, 0.05f, 6f,
                        EnemyBodyPlan.Quadruped, EnemyFaction.Beast, EnemyCombatRole.Swarmer, EnemySpawnAvailability.Active, false,
                        "Forces player to track lateral low movement.");
                    break;
                case EnemyArchetype.BoneArcher:
                    ConfigureDefinition(definition, "enemy.bone_archer", "Bone Archer", 2, 58f, 4.1f, 10f, 2.55f, 1.35f, 0.24f, 54f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.RangedSpit, "humanoid_ranged",
                        new Color(0.74f, 0.72f, 0.64f, 1f), new Vector3(0.9f, 1.75f, 0.8f), 8f, 4, 12,
                        1f, 32f, 130f, 0.72f, 1.02f, 1.08f, 0.86f, 1.35f, 0.06f, 7, 15, 0.06f, 8f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Undead, EnemyCombatRole.Archer, EnemySpawnAvailability.Active, true,
                        "Forces player to prioritize ranged pressure.");
                    break;
                case EnemyArchetype.ShieldGoblin:
                    ConfigureDefinition(definition, "enemy.shield_goblin", "Shield Goblin", 2, 92f, 3.8f, 12f, 2.15f, 1.4f, 0.36f, 44f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.ShieldGuard, "humanoid_shielded",
                        new Color(0.55f, 0.35f, 0.18f, 1f), new Vector3(1.25f, 1.55f, 1.15f), 8f, 4, 12,
                        0.9f, 32f, 105f, 0.58f, 0.85f, 1.02f, 0.75f, 1.45f, 0.1f, 8, 18, 0.1f, 10f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Goblin, EnemyCombatRole.Shield, EnemySpawnAvailability.Active, true,
                        "Forces player to reposition around a blocker.");
                    break;
                case EnemyArchetype.BombBeetle:
                    ConfigureDefinition(definition, "enemy.bomb_beetle", "Bomb Beetle", 2, 42f, 5.4f, 16f, 1.75f, 1.8f, 0.55f, 38f,
                        EnemyAmbientBehavior.Wander, EnemyMobilityRole.Roamer, EnemyAttackFamily.Charge, "low_quadruped_charger",
                        new Color(0.9f, 0.42f, 0.12f, 1f), new Vector3(1.1f, 0.8f, 1.1f), 5f, 4, 10,
                        0.95f, 28f, 140f, 0.72f, 1f, 1.22f, 0.85f, 1.4f, 0.05f, 7, 16, 0.05f, 6f,
                        EnemyBodyPlan.Quadruped, EnemyFaction.Beast, EnemyCombatRole.Charger, EnemySpawnAvailability.Active, false,
                        "Forces player to dodge a charge-marked body.");
                    break;
                case EnemyArchetype.OrcGrunt:
                    ConfigureDefinition(definition, "enemy.orc_grunt", "Orc Grunt", 3, 118f, 4.2f, 17f, 2.55f, 1.45f, 0.4f, 48f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.BasicMelee, "humanoid_heavy",
                        new Color(0.25f, 0.52f, 0.25f, 1f), new Vector3(1.5f, 1.95f, 1.35f), 10f, 13, 0,
                        1f, 36f, 110f, 0.56f, 0.86f, 1.08f, 0.78f, 2f, 0.12f, 12, 26, 0.12f, 12f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Orc, EnemyCombatRole.Brute, EnemySpawnAvailability.Active, true,
                        "Forces player in later floors to manage a sturdier humanoid.");
                    break;
                case EnemyArchetype.RiftHound:
                    ConfigureDefinition(definition, "enemy.rift_hound", "Rift Hound", 3, 72f, 7.1f, 13f, 2.05f, 0.95f, 0.18f, 52f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Hunter, EnemyAttackFamily.Charge, "quadruped_hound",
                        new Color(0.32f, 0.2f, 0.7f, 1f), new Vector3(1.15f, 0.95f, 1.55f), 8f, 13, 0,
                        1.25f, 42f, 155f, 0.86f, 1.15f, 1.28f, 1.05f, 1.75f, 0.08f, 10, 22, 0.08f, 8f,
                        EnemyBodyPlan.Quadruped, EnemyFaction.DeepHorror, EnemyCombatRole.Hunter, EnemySpawnAvailability.Active, true,
                        "Forces player in later floors to manage deep-horror chase pressure.");
                    break;
                case EnemyArchetype.Stoneback:
                    ConfigureDefinition(definition, "enemy.stoneback", "Stoneback", 3, 160f, 2.8f, 19f, 2.65f, 1.75f, 0.5f, 42f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.ShieldGuard, "large_quadruped",
                        new Color(0.46f, 0.48f, 0.44f, 1f), new Vector3(1.75f, 1.5f, 1.75f), 6f, 13, 0,
                        0.75f, 30f, 95f, 0.42f, 0.7f, 0.92f, 0.66f, 2.2f, 0.16f, 14, 30, 0.16f, 14f,
                        EnemyBodyPlan.LargeQuadruped, EnemyFaction.DungeonConstruct, EnemyCombatRole.Shield, EnemySpawnAvailability.Active, true,
                        "Forces player in later floors to solve armored beast pressure.");
                    break;
                case EnemyArchetype.HexCaster:
                    ConfigureDefinition(definition, "enemy.hex_caster", "Hex Caster", 3, 82f, 3.9f, 14f, 2.7f, 1.55f, 0.38f, 58f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.CasterSupport, "humanoid_caster",
                        new Color(0.48f, 0.16f, 0.72f, 1f), new Vector3(1.05f, 1.75f, 1.05f), 7f, 12, 0,
                        1.15f, 38f, 150f, 0.64f, 1f, 1.08f, 0.86f, 2f, 0.08f, 12, 24, 0.08f, 8f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Cultist, EnemyCombatRole.Support, EnemySpawnAvailability.Active, true,
                        "Forces player in later floors to prioritize support/caster pressure.");
                    break;
                case EnemyArchetype.IronOgre:
                    ConfigureDefinition(definition, "enemy.iron_ogre", "Iron Ogre", 4, 260f, 3.2f, 26f, 3f, 1.95f, 0.65f, 52f,
                        EnemyAmbientBehavior.SleepGuard, EnemyMobilityRole.Sleeper, EnemyAttackFamily.Charge, "elite_humanoid_ogre",
                        new Color(0.55f, 0.52f, 0.48f, 1f), new Vector3(2.05f, 2.45f, 1.95f), 5f, 11, 0,
                        0.9f, 42f, 105f, 0.38f, 0.74f, 1.02f, 0.7f, 3.6f, 0.18f, 20, 45, 0.18f, 18f,
                        EnemyBodyPlan.EliteHumanoid, EnemyFaction.DungeonConstruct, EnemyCombatRole.Brute, EnemySpawnAvailability.Active, true,
                        "Forces player to dodge a very slow elite windup.");
                    break;
                case EnemyArchetype.HexWitch:
                    ConfigureDefinition(definition, "enemy.hex_witch", "Hex Witch", 4, 130f, 4.4f, 18f, 2.75f, 1.45f, 0.32f, 62f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Hunter, EnemyAttackFamily.DebuffPlayer, "humanoid_witch",
                        new Color(0.35f, 0.08f, 0.42f, 1f), new Vector3(1.15f, 1.95f, 1.15f), 5f, 11, 0,
                        1.25f, 45f, 165f, 0.72f, 1.08f, 1.18f, 0.95f, 3.1f, 0.1f, 18, 38, 0.1f, 12f,
                        EnemyBodyPlan.EliteHumanoid, EnemyFaction.Cultist, EnemyCombatRole.Support, EnemySpawnAvailability.Active, true,
                        "Forces player in later floors to prioritize curse/debuff pressure.");
                    break;
                case EnemyArchetype.GraveKnight:
                    ConfigureDefinition(definition, "enemy.grave_knight", "Grave Knight", 4, 210f, 4.0f, 23f, 2.75f, 1.55f, 0.48f, 56f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.ShieldGuard, "elite_humanoid_knight",
                        new Color(0.28f, 0.32f, 0.38f, 1f), new Vector3(1.55f, 2.1f, 1.35f), 5f, 11, 0,
                        1f, 42f, 115f, 0.55f, 0.86f, 1.08f, 0.8f, 3.3f, 0.12f, 20, 42, 0.12f, 14f,
                        EnemyBodyPlan.EliteHumanoid, EnemyFaction.Undead, EnemyCombatRole.EliteDuelist, EnemySpawnAvailability.Active, true,
                        "Forces player in later floors to respect elite duelist pressure.");
                    break;
                case EnemyArchetype.RiftStalker:
                    ConfigureDefinition(definition, "enemy.rift_stalker", "Rift Stalker", 4, 150f, 6.8f, 20f, 2.35f, 1.12f, 0.2f, 64f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Hunter, EnemyAttackFamily.TeleportStrike, "elite_humanoid_stalker",
                        new Color(0.12f, 0.12f, 0.32f, 1f), new Vector3(1.2f, 1.75f, 1.2f), 5f, 12, 0,
                        1.35f, 48f, 170f, 0.85f, 1.16f, 1.26f, 1.02f, 3.2f, 0.08f, 18, 40, 0.08f, 10f,
                        EnemyBodyPlan.EliteHumanoid, EnemyFaction.DeepHorror, EnemyCombatRole.Ambusher, EnemySpawnAvailability.Active, true,
                        "Forces player in later floors to respect ambush/teleport pressure.");
                    break;
                default:
                    ConfigureRosterDefinition(definition, archetype);
                    break;
            }

            definition.floorBand = GetFloorBand(definition.minFloor);
            ApplyAttackFamilyTuning(definition);
        }

        private static void ConfigureRosterDefinition(EnemyDefinition definition, EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.TorchlessPrisoner:
                    ConfigureDefinition(definition, "enemy.torchless_prisoner", "Torchless Prisoner", 1, 48f, 4.2f, 7f, 2.05f, 1.2f, 0.24f, 34f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.BasicMelee, "humanoid_prisoner",
                        new Color(0.42f, 0.36f, 0.32f, 1f), new Vector3(1f, 1.55f, 0.95f), 24f, 1, 3,
                        0.9f, 24f, 105f, 0.55f, 0.78f, 1f, 0.75f, 0.8f, 0.18f, 4, 9, 0.08f, 8f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Prisoner, EnemyCombatRole.Grunt, EnemySpawnAvailability.Active, true,
                        "Forces player to practice basic shooting and spacing.");
                    break;
                case EnemyArchetype.CandleGoblin:
                    ConfigureDefinition(definition, "enemy.candle_goblin", "Candle Goblin", 1, 38f, 5.1f, 6f, 1.9f, 1.05f, 0.18f, 42f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Roamer, EnemyAttackFamily.BuffAlly, "humanoid_goblin_candle",
                        new Color(0.86f, 0.42f, 0.12f, 1f), new Vector3(0.95f, 1.35f, 0.9f), 18f, 1, 7,
                        1.45f, 42f, 155f, 0.82f, 1.08f, 1.18f, 0.9f, 0.95f, 0.22f, 5, 11, 0.06f, 6f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Goblin, EnemyCombatRole.Support, EnemySpawnAvailability.Active, true,
                        "Forces player to kill the alert support before it wakes the room.");
                    break;
                case EnemyArchetype.MoldCoveredSkeleton:
                    ConfigureDefinition(definition, "enemy.mold_covered_skeleton", "Mold-Covered Skeleton", 1, 62f, 3.1f, 8f, 2.05f, 1.45f, 0.34f, 32f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.BasicMelee, "humanoid_bone_mold",
                        new Color(0.66f, 0.72f, 0.57f, 1f), new Vector3(1.0f, 1.7f, 0.86f), 20f, 1, 3,
                        0.75f, 25f, 100f, 0.55f, 0.75f, 0.95f, 0.72f, 1.05f, 0.18f, 4, 10, 0.08f, 8f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Undead, EnemyCombatRole.Grunt, EnemySpawnAvailability.Active, true,
                        "Forces player to commit repeated shots into a slow target.");
                    break;
                case EnemyArchetype.RustyDaggerRatfolk:
                    ConfigureDefinition(definition, "enemy.rusty_dagger_ratfolk", "Rusty Dagger Ratfolk", 1, 34f, 6.2f, 6f, 1.65f, 0.9f, 0.12f, 38f,
                        EnemyAmbientBehavior.Wander, EnemyMobilityRole.Roamer, EnemyAttackFamily.LeapingBite, "humanoid_ratfolk_fast",
                        new Color(0.46f, 0.34f, 0.24f, 1f), new Vector3(0.85f, 1.25f, 0.8f), 16f, 1, 3,
                        1.2f, 34f, 145f, 0.9f, 1.08f, 1.25f, 1f, 0.9f, 0.12f, 3, 8, 0.05f, 6f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Ratfolk, EnemyCombatRole.Swarmer, EnemySpawnAvailability.Active, true,
                        "Forces player to dash or track a fast side-angle attacker.");
                    break;
                case EnemyArchetype.DungeonJanitorGhoul:
                    ConfigureDefinition(definition, "enemy.dungeon_janitor_ghoul", "Dungeon Janitor Ghoul", 1, 78f, 3.4f, 10f, 2.25f, 1.55f, 0.42f, 34f,
                        EnemyAmbientBehavior.SleepGuard, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.HeavyWindup, "humanoid_ghoul_brute",
                        new Color(0.36f, 0.47f, 0.34f, 1f), new Vector3(1.2f, 1.75f, 1.05f), 10f, 1, 7,
                        0.85f, 28f, 95f, 0.48f, 0.72f, 0.95f, 0.7f, 1.1f, 0.22f, 5, 12, 0.1f, 10f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Undead, EnemyCombatRole.Brute, EnemySpawnAvailability.Active, true,
                        "Forces player to respect a slow windup even on early floors.");
                    break;
                case EnemyArchetype.StarvedDungeonWolf:
                    ConfigureDefinition(definition, "enemy.starved_dungeon_wolf", "Starved Dungeon Wolf", 1, 42f, 6.8f, 7f, 1.85f, 0.95f, 0.14f, 44f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Hunter, EnemyAttackFamily.LeapingBite, "quadruped_wolf",
                        new Color(0.34f, 0.31f, 0.27f, 1f), new Vector3(0.8f, 0.72f, 1.35f), 16f, 1, 3,
                        1.25f, 36f, 150f, 0.85f, 1.1f, 1.28f, 1f, 0.95f, 0.08f, 3, 8, 0.06f, 7f,
                        EnemyBodyPlan.Quadruped, EnemyFaction.Beast, EnemyCombatRole.Hunter, EnemySpawnAvailability.Active, true,
                        "Forces player to handle a fast quadruped closing distance.");
                    break;
                case EnemyArchetype.CoalEyedAlleyCat:
                    ConfigureDefinition(definition, "enemy.coal_eyed_alley_cat", "Coal-Eyed Alley Cat", 1, 24f, 7.2f, 5f, 1.45f, 0.75f, 0.08f, 34f,
                        EnemyAmbientBehavior.Wander, EnemyMobilityRole.Roamer, EnemyAttackFamily.LeapingBite, "quadruped_cat",
                        new Color(0.08f, 0.08f, 0.07f, 1f), new Vector3(0.55f, 0.48f, 0.85f), 12f, 1, 3,
                        1.1f, 26f, 170f, 1f, 1.15f, 1.32f, 1f, 0.7f, 0.05f, 2, 7, 0.04f, 5f,
                        EnemyBodyPlan.Quadruped, EnemyFaction.Beast, EnemyCombatRole.Ambusher, EnemySpawnAvailability.Active, false,
                        "Forces player to aim at a small evasive target.");
                    break;
                case EnemyArchetype.RustBellBat:
                    ConfigureDefinition(definition, "enemy.rust_bell_bat", "Rust Bell Bat", 1, 26f, 7.6f, 6f, 1.7f, 0.9f, 0.12f, 44f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Roamer, EnemyAttackFamily.LeapingBite, "flying_bell_bat",
                        new Color(0.28f, 0.2f, 0.18f, 1f), new Vector3(0.82f, 0.9f, 0.82f), 14f, 1, 12,
                        1.25f, 38f, 155f, 0.9f, 1.08f, 1.25f, 1f, 0.85f, 0.08f, 3, 7, 0.05f, 6f,
                        EnemyBodyPlan.Flying, EnemyFaction.Beast, EnemyCombatRole.Swarmer, EnemySpawnAvailability.Active, true,
                        "Forces player to track a fluttering flyer.");
                    break;
                case EnemyArchetype.ChainBoundThief:
                    ConfigureDefinition(definition, "enemy.chain_bound_thief", "Chain-Bound Thief", 2, 56f, 5.9f, 10f, 1.9f, 0.95f, 0.16f, 48f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Hunter, EnemyAttackFamily.PackTactics, "humanoid_thief_chain",
                        new Color(0.24f, 0.24f, 0.3f, 1f), new Vector3(0.9f, 1.55f, 0.85f), 14f, 4, 7,
                        1.1f, 34f, 145f, 0.75f, 1.02f, 1.22f, 1f, 1.15f, 0.08f, 6, 14, 0.06f, 8f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Prisoner, EnemyCombatRole.Ambusher, EnemySpawnAvailability.Active, true,
                        "Forces player to watch a hit-and-run humanoid.");
                    break;
                case EnemyArchetype.GoblinShieldRat:
                    ConfigureDefinition(definition, "enemy.goblin_shield_rat", "Goblin Shield Rat", 2, 88f, 3.6f, 10f, 2.0f, 1.4f, 0.34f, 42f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.ShieldGuard, "humanoid_goblin_shield",
                        new Color(0.48f, 0.34f, 0.18f, 1f), new Vector3(1.15f, 1.45f, 1.05f), 16f, 4, 7,
                        0.9f, 32f, 110f, 0.58f, 0.85f, 1.02f, 0.75f, 1.4f, 0.1f, 7, 16, 0.1f, 10f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Goblin, EnemyCombatRole.Shield, EnemySpawnAvailability.Active, true,
                        "Forces player to solve a blocker before backline pressure.");
                    break;
                case EnemyArchetype.BoneArcherInitiate:
                    ConfigureDefinition(definition, "enemy.bone_archer_initiate", "Bone Archer Initiate", 2, 52f, 4.0f, 9f, 2.55f, 1.3f, 0.22f, 56f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.RangedProjectile, "humanoid_bone_ranged",
                        new Color(0.72f, 0.7f, 0.61f, 1f), new Vector3(0.88f, 1.68f, 0.78f), 14f, 4, 12,
                        1f, 34f, 135f, 0.68f, 0.98f, 1.05f, 0.84f, 1.2f, 0.06f, 6, 14, 0.06f, 8f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Undead, EnemyCombatRole.Archer, EnemySpawnAvailability.Active, true,
                        "Forces player to prioritize ranged skeleton pressure.");
                    break;
                case EnemyArchetype.LanternCultist:
                    ConfigureDefinition(definition, "enemy.lantern_cultist", "Lantern Cultist", 2, 64f, 3.8f, 8f, 2.2f, 1.35f, 0.3f, 58f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.BuffAlly, "humanoid_cultist_lantern",
                        new Color(0.64f, 0.3f, 0.58f, 1f), new Vector3(1f, 1.7f, 0.95f), 12f, 4, 7,
                        1.35f, 46f, 160f, 0.58f, 0.9f, 1.02f, 0.78f, 1.25f, 0.08f, 7, 15, 0.08f, 8f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Cultist, EnemyCombatRole.Support, EnemySpawnAvailability.Active, true,
                        "Forces player to shut down support before it rallies beasts.");
                    break;
                case EnemyArchetype.PickaxeSkeletonMiner:
                    ConfigureDefinition(definition, "enemy.pickaxe_skeleton_miner", "Pickaxe Skeleton Miner", 2, 96f, 3.0f, 15f, 2.35f, 1.7f, 0.52f, 38f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.HeavyWindup, "humanoid_bone_pickaxe",
                        new Color(0.66f, 0.62f, 0.52f, 1f), new Vector3(1.15f, 1.75f, 1.0f), 12f, 4, 7,
                        0.82f, 30f, 100f, 0.48f, 0.72f, 0.92f, 0.68f, 1.45f, 0.12f, 8, 18, 0.1f, 10f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Undead, EnemyCombatRole.Brute, EnemySpawnAvailability.Active, true,
                        "Forces player to dodge a slow armor-breaker windup.");
                    break;
                case EnemyArchetype.CursedKennelWolf:
                    ConfigureDefinition(definition, "enemy.cursed_kennel_wolf", "Cursed Kennel Wolf", 2, 62f, 7.0f, 11f, 1.95f, 0.95f, 0.16f, 50f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Hunter, EnemyAttackFamily.LeapingBite, "quadruped_wolf_cursed",
                        new Color(0.23f, 0.25f, 0.34f, 1f), new Vector3(0.95f, 0.82f, 1.55f), 14f, 4, 12,
                        1.25f, 42f, 155f, 0.82f, 1.12f, 1.3f, 1.02f, 1.2f, 0.08f, 6, 14, 0.08f, 8f,
                        EnemyBodyPlan.Quadruped, EnemyFaction.Beast, EnemyCombatRole.Hunter, EnemySpawnAvailability.Active, true,
                        "Forces player to manage pack pressure and dash timing.");
                    break;
                case EnemyArchetype.CryptLynx:
                    ConfigureDefinition(definition, "enemy.crypt_lynx", "Crypt Lynx", 2, 46f, 7.4f, 9f, 1.65f, 0.82f, 0.12f, 44f,
                        EnemyAmbientBehavior.Wander, EnemyMobilityRole.Hunter, EnemyAttackFamily.LeapingBite, "quadruped_cat_crypt",
                        new Color(0.16f, 0.17f, 0.2f, 1f), new Vector3(0.65f, 0.58f, 1.05f), 12f, 4, 12,
                        1.18f, 34f, 170f, 0.95f, 1.15f, 1.34f, 1.05f, 1f, 0.05f, 5, 12, 0.05f, 6f,
                        EnemyBodyPlan.Quadruped, EnemyFaction.Beast, EnemyCombatRole.Ambusher, EnemySpawnAvailability.Active, true,
                        "Forces player to track a small evasive ambusher.");
                    break;
                case EnemyArchetype.AshEatenPrisonGuard:
                    ConfigureDefinition(definition, "enemy.ash_eaten_prison_guard", "Ash-Eaten Prison Guard", 2, 108f, 3.7f, 14f, 2.35f, 1.55f, 0.42f, 42f,
                        EnemyAmbientBehavior.SleepGuard, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.ShieldGuard, "humanoid_guard_ash",
                        new Color(0.36f, 0.31f, 0.26f, 1f), new Vector3(1.25f, 1.85f, 1.1f), 10f, 4, 7,
                        0.86f, 34f, 100f, 0.48f, 0.76f, 0.98f, 0.72f, 1.6f, 0.1f, 8, 20, 0.1f, 12f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Prisoner, EnemyCombatRole.Brute, EnemySpawnAvailability.Active, true,
                        "Forces player to respect a durable frontliner.");
                    break;
                case EnemyArchetype.GoblinTripwireTrapper:
                    ConfigureDefinition(definition, "enemy.goblin_tripwire_trapper", "Goblin Tripwire Trapper", 3, 58f, 4.8f, 10f, 2f, 1.15f, 0.22f, 52f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.TrapPlace, "humanoid_goblin_trapper",
                        new Color(0.72f, 0.45f, 0.16f, 1f), new Vector3(0.95f, 1.45f, 0.9f), 14f, 8, 12,
                        1.15f, 42f, 145f, 0.7f, 1.02f, 1.1f, 0.86f, 1.35f, 0.06f, 8, 16, 0.06f, 8f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Goblin, EnemyCombatRole.Trapper, EnemySpawnAvailability.Active, true,
                        "Forces player to read trap-placeholding pressure.");
                    break;
                case EnemyArchetype.BarrelHeadBandit:
                    ConfigureDefinition(definition, "enemy.barrel_head_bandit", "Barrel-Head Bandit", 3, 86f, 4.4f, 13f, 2.25f, 1.3f, 0.28f, 46f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.BasicMelee, "humanoid_bandit_barrel",
                        new Color(0.42f, 0.27f, 0.17f, 1f), new Vector3(1.2f, 1.75f, 1.05f), 12f, 8, 12,
                        1f, 36f, 120f, 0.62f, 0.92f, 1.08f, 0.8f, 1.45f, 0.08f, 8, 20, 0.08f, 10f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Prisoner, EnemyCombatRole.Grunt, EnemySpawnAvailability.Active, true,
                        "Forces player to burn down a mid-tier humanoid.");
                    break;
                case EnemyArchetype.SewerKnifeTwin:
                    ConfigureDefinition(definition, "enemy.sewer_knife_twin", "Sewer Knife Twin", 3, 54f, 6.4f, 12f, 1.75f, 0.82f, 0.12f, 50f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Hunter, EnemyAttackFamily.PackTactics, "humanoid_ratfolk_twin",
                        new Color(0.38f, 0.3f, 0.24f, 1f), new Vector3(0.9f, 1.35f, 0.85f), 14f, 8, 12,
                        1.25f, 44f, 155f, 0.92f, 1.12f, 1.28f, 1.02f, 1.35f, 0.05f, 7, 15, 0.06f, 8f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Ratfolk, EnemyCombatRole.Ambusher, EnemySpawnAvailability.Active, true,
                        "Forces player to manage side-pressure pairs.");
                    break;
                case EnemyArchetype.RottenBellRinger:
                    ConfigureDefinition(definition, "enemy.rotten_bell_ringer", "Rotten Bell Ringer", 3, 84f, 3.5f, 11f, 2.2f, 1.35f, 0.34f, 60f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.FearHowl, "humanoid_undead_bell",
                        new Color(0.42f, 0.48f, 0.39f, 1f), new Vector3(1.15f, 1.8f, 1.05f), 10f, 8, 12,
                        1.5f, 52f, 165f, 0.56f, 0.88f, 1.02f, 0.78f, 1.5f, 0.08f, 8, 18, 0.08f, 10f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Undead, EnemyCombatRole.Support, EnemySpawnAvailability.Active, true,
                        "Forces player to prioritize a room-alerting support.");
                    break;
                case EnemyArchetype.CrossbowGoblin:
                    ConfigureDefinition(definition, "enemy.crossbow_goblin", "Crossbow Goblin", 3, 62f, 4.2f, 13f, 2.65f, 1.25f, 0.22f, 58f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.RangedProjectile, "humanoid_goblin_crossbow",
                        new Color(0.66f, 0.36f, 0.13f, 1f), new Vector3(0.95f, 1.45f, 0.9f), 14f, 8, 12,
                        1.1f, 42f, 140f, 0.7f, 1.02f, 1.08f, 0.86f, 1.55f, 0.05f, 8, 18, 0.06f, 8f,
                        EnemyBodyPlan.Humanoid, EnemyFaction.Goblin, EnemyCombatRole.Archer, EnemySpawnAvailability.Active, true,
                        "Forces player to break line of sight and target ranged goblins.");
                    break;
                case EnemyArchetype.BoneManeWolf:
                    ConfigureDefinition(definition, "enemy.bone_mane_wolf", "Bone-Mane Wolf", 3, 82f, 7.2f, 14f, 2f, 0.95f, 0.16f, 54f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.Hunter, EnemyAttackFamily.LeapingBite, "quadruped_wolf_bone_mane",
                        new Color(0.42f, 0.43f, 0.4f, 1f), new Vector3(1.05f, 0.9f, 1.65f), 13f, 8, 12,
                        1.3f, 46f, 155f, 0.85f, 1.12f, 1.32f, 1.02f, 1.55f, 0.08f, 8, 18, 0.08f, 8f,
                        EnemyBodyPlan.Quadruped, EnemyFaction.Beast, EnemyCombatRole.Hunter, EnemySpawnAvailability.Active, true,
                        "Forces player to handle tougher fast beast pressure.");
                    break;
                case EnemyArchetype.DungeonRam:
                    ConfigureDefinition(definition, "enemy.dungeon_ram", "Dungeon Ram", 3, 118f, 4.9f, 18f, 2.45f, 1.65f, 0.46f, 46f,
                        EnemyAmbientBehavior.Patrol, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.Charge, "large_quadruped_ram",
                        new Color(0.48f, 0.44f, 0.36f, 1f), new Vector3(1.45f, 1.05f, 1.75f), 10f, 8, 12,
                        1f, 38f, 115f, 0.58f, 0.88f, 1.08f, 0.8f, 1.75f, 0.1f, 10, 22, 0.1f, 10f,
                        EnemyBodyPlan.LargeQuadruped, EnemyFaction.Beast, EnemyCombatRole.Charger, EnemySpawnAvailability.Active, true,
                        "Forces player to sidestep a heavy charge placeholder.");
                    break;
                case EnemyArchetype.MossbackBearCub:
                    ConfigureDefinition(definition, "enemy.mossback_bear_cub", "Mossback Bear Cub", 3, 150f, 3.7f, 20f, 2.65f, 1.8f, 0.58f, 42f,
                        EnemyAmbientBehavior.SleepGuard, EnemyMobilityRole.RoomGuard, EnemyAttackFamily.HeavyWindup, "large_quadruped_bear",
                        new Color(0.31f, 0.34f, 0.25f, 1f), new Vector3(1.6f, 1.2f, 1.85f), 7f, 8, 12,
                        0.9f, 36f, 105f, 0.42f, 0.72f, 0.92f, 0.68f, 2.1f, 0.14f, 12, 28, 0.12f, 12f,
                        EnemyBodyPlan.LargeQuadruped, EnemyFaction.Beast, EnemyCombatRole.Brute, EnemySpawnAvailability.Active, true,
                        "Forces player to dodge a huge HP windup check.");
                    break;
            }
        }

        private static void ConfigureDefinition(
            EnemyDefinition definition,
            string enemyId,
            string displayName,
            int tier,
            float maxHealth,
            float moveSpeed,
            float attackDamage,
            float attackRange,
            float attackCooldown,
            float attackWindupDuration,
            float detectionRange,
            EnemyAmbientBehavior ambientBehavior,
            EnemyMobilityRole mobilityRole,
            EnemyAttackFamily attackFamily,
            string visualProfileId,
            Color bodyColor,
            Vector3 visualScale,
            float spawnWeight,
            int minFloor,
            int maxFloor,
            float hearingRadiusMultiplier,
            float groupAlertRadius,
            float visionConeAngle,
            float patrolSpeedMultiplier,
            float investigateSpeedMultiplier,
            float chaseSpeedMultiplier,
            float returnHomeSpeedMultiplier,
            float masteryXpValue,
            float goldDropChance,
            int goldMin,
            int goldMax,
            float healthDropChance,
            float healthAmount,
            EnemyBodyPlan bodyPlan,
            EnemyFaction faction,
            EnemyCombatRole combatRole,
            EnemySpawnAvailability availability,
            bool bountyEligible,
            string designNote)
        {
            definition.enemyId = enemyId;
            definition.displayName = displayName;
            definition.tier = tier;
            definition.maxHealth = maxHealth;
            definition.moveSpeed = moveSpeed;
            definition.attackDamage = attackDamage;
            definition.attackRange = attackRange;
            definition.attackCooldown = attackCooldown;
            definition.attackWindupDuration = attackWindupDuration;
            definition.detectionRange = detectionRange;
            definition.hearingRadiusMultiplier = hearingRadiusMultiplier;
            definition.groupAlertRadius = groupAlertRadius;
            definition.ambientBehavior = ambientBehavior;
            definition.defaultMobilityRole = mobilityRole;
            definition.attackFamily = attackFamily;
            definition.visualProfileId = visualProfileId;
            definition.visionConeAngle = visionConeAngle;
            definition.idleMoveSpeedMultiplier = patrolSpeedMultiplier;
            definition.patrolSpeedMultiplier = patrolSpeedMultiplier;
            definition.investigateSpeedMultiplier = investigateSpeedMultiplier;
            definition.chaseSpeedMultiplier = chaseSpeedMultiplier;
            definition.returnHomeSpeedMultiplier = returnHomeSpeedMultiplier;
            definition.patrolWaitSeconds = Mathf.Lerp(1.45f, 0.75f, Mathf.InverseLerp(1f, 4f, tier));
            definition.investigateDuration = Mathf.Lerp(3.2f, 5.2f, Mathf.InverseLerp(1f, 4f, tier));
            definition.lostSightGraceDuration = Mathf.Lerp(0.75f, 1.5f, Mathf.InverseLerp(1f, 4f, tier));
            definition.searchDuration = Mathf.Lerp(2.5f, 5.2f, Mathf.InverseLerp(1f, 4f, tier));
            definition.homeReturnStopDistance = 1.1f;
            definition.stuckRecoverySeconds = 1.2f;
            definition.visualScale = visualScale;
            definition.bodyColor = bodyColor;
            definition.spawnWeight = spawnWeight;
            definition.minFloor = minFloor;
            definition.maxFloor = maxFloor;
            definition.masteryXpValue = masteryXpValue;
            definition.goldDropChance = goldDropChance;
            definition.goldMin = goldMin;
            definition.goldMax = goldMax;
            definition.healthDropChance = healthDropChance;
            definition.healthAmount = healthAmount;
            definition.bodyPlan = bodyPlan;
            definition.faction = faction;
            definition.combatRole = combatRole;
            definition.spawnAvailability = availability;
            definition.bountyEligible = bountyEligible;
            definition.designNote = designNote;
            definition.floorBand = GetFloorBand(minFloor);
        }

        private static void ApplyAttackFamilyTuning(EnemyDefinition definition)
        {
            switch (definition.attackFamily)
            {
                case EnemyAttackFamily.BasicMelee:
                case EnemyAttackFamily.MeleeRush:
                    definition.attackImplementationNote = "Implemented: existing melee behavior.";
                    break;
                case EnemyAttackFamily.LeapingBite:
                    definition.attackRange = Mathf.Max(definition.attackRange, 1.8f);
                    definition.chaseSpeedMultiplier = Mathf.Max(definition.chaseSpeedMultiplier, 1.18f);
                    definition.attackImplementationNote = "Placeholder: faster melee chase and bite range; full leap deferred.";
                    break;
                case EnemyAttackFamily.Charge:
                    definition.attackWindupDuration = Mathf.Max(definition.attackWindupDuration, 0.45f);
                    definition.attackImplementationNote = "Placeholder: heavy windup/charge metadata; full charge motion deferred.";
                    break;
                case EnemyAttackFamily.HeavyWindup:
                    definition.attackWindupDuration = Mathf.Max(definition.attackWindupDuration, 0.5f);
                    definition.attackImplementationNote = "Implemented as slow melee with longer windup.";
                    break;
                case EnemyAttackFamily.ShieldGuard:
                    definition.maxHealth *= 1.08f;
                    definition.moveSpeed *= 0.92f;
                    definition.attackImplementationNote = "Placeholder: shield/tank metadata; real frontal blocking deferred.";
                    break;
                case EnemyAttackFamily.RangedProjectile:
                case EnemyAttackFamily.RangedSpit:
                    definition.detectionRange += 4f;
                    definition.attackImplementationNote = "Placeholder: ranged metadata with safe melee fallback; projectile behavior deferred.";
                    break;
                case EnemyAttackFamily.TrapPlace:
                    definition.attackImplementationNote = "Placeholder: trap metadata with safe melee fallback; trap placement deferred.";
                    break;
                case EnemyAttackFamily.BuffAlly:
                case EnemyAttackFamily.Summon:
                case EnemyAttackFamily.DebuffPlayer:
                case EnemyAttackFamily.FearHowl:
                    definition.groupAlertRadius += 6f;
                    definition.attackImplementationNote = "Placeholder: support metadata increases alert footprint; active spell behavior deferred.";
                    break;
                case EnemyAttackFamily.PackTactics:
                    definition.groupAlertRadius += 4f;
                    definition.attackImplementationNote = "Placeholder: pack metadata with safe melee fallback.";
                    break;
                default:
                    definition.attackImplementationNote = "Placeholder: unsupported attack family falls back to safe melee behavior.";
                    break;
            }
        }
    }
}
