using System.Collections.Generic;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS131EnemyRosterTests
    {
        [Test]
        public void SlimesAreDebugOnlyAndExcludedFromNormalFloorCatalogs()
        {
            EnemyDefinition slime = EnemyCatalog.CreateDefinition(EnemyArchetype.Slime);
            EnemyDefinition spitter = EnemyCatalog.CreateDefinition(EnemyArchetype.SpitterSlime);
            try
            {
                Assert.AreEqual(EnemySpawnAvailability.DebugOnly, slime.spawnAvailability);
                Assert.AreEqual(EnemySpawnAvailability.DebugOnly, spitter.spawnAvailability);
                Assert.AreEqual(0f, slime.spawnWeight);
                Assert.AreEqual(0f, spitter.spawnWeight);
                Assert.IsTrue(slime.IsEligibleForFloor(1));
                Assert.IsFalse(slime.IsEligibleForNormalSpawn(1));

                for (int floor = 1; floor <= 12; floor++)
                {
                    List<EnemyDefinition> definitions = EnemyCatalog.CreateDefinitionsForFloor(floor);
                    Assert.IsFalse(definitions.Exists(definition => definition.archetype == EnemyArchetype.Slime), $"Slime appeared on floor {floor}.");
                    Assert.IsFalse(definitions.Exists(definition => definition.archetype == EnemyArchetype.SpitterSlime), $"Spitter Slime appeared on floor {floor}.");
                }
            }
            finally
            {
                Object.DestroyImmediate(slime);
                Object.DestroyImmediate(spitter);
            }
        }

        [Test]
        public void ActiveRosterDefinitionsHaveRequiredTaxonomyAndDesignNotes()
        {
            List<EnemyDefinition> definitions = EnemyCatalog.CreateDefaultDefinitions();
            int activeRosterCount = 0;
            try
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    EnemyDefinition definition = definitions[i];
                    if (definition == null || definition.spawnAvailability != EnemySpawnAvailability.Active)
                    {
                        continue;
                    }

                    activeRosterCount++;
                    Assert.IsFalse(string.IsNullOrWhiteSpace(definition.enemyId), definition.name);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(definition.displayName), definition.enemyId);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(definition.visualProfileId), definition.enemyId);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(definition.designNote), definition.enemyId);
                    StringAssert.Contains("Forces player", definition.designNote, definition.enemyId);
                    Assert.Greater(definition.maxHealth, 0f, definition.enemyId);
                    Assert.Greater(definition.attackDamage, 0f, definition.enemyId);
                    Assert.Greater(definition.moveSpeed, 0f, definition.enemyId);
                    Assert.Greater(definition.spawnWeight, 0f, definition.enemyId);
                    Assert.Greater(definition.masteryXpValue, 0f, definition.enemyId);
                    Assert.GreaterOrEqual(definition.maxFloor == 0 ? definition.minFloor : definition.maxFloor, definition.minFloor, definition.enemyId);
                    Assert.AreEqual(EnemyCatalog.GetFloorBand(definition.minFloor), definition.floorBand, definition.enemyId);
                }

                Assert.GreaterOrEqual(activeRosterCount, 24);
            }
            finally
            {
                DestroyDefinitions(definitions);
            }
        }

        [Test]
        public void FloorBandsExposeExpectedRosterProgression()
        {
            List<EnemyDefinition> floorOne = EnemyCatalog.CreateDefinitionsForFloor(1);
            List<EnemyDefinition> floorFive = EnemyCatalog.CreateDefinitionsForFloor(5);
            List<EnemyDefinition> floorTen = EnemyCatalog.CreateDefinitionsForFloor(10);
            try
            {
                Assert.GreaterOrEqual(floorOne.Count, 5);
                Assert.IsTrue(floorOne.Exists(definition => definition.archetype == EnemyArchetype.TorchlessPrisoner));
                Assert.IsTrue(floorOne.Exists(definition => definition.archetype == EnemyArchetype.MoldCoveredSkeleton));
                Assert.IsTrue(floorOne.Exists(definition => definition.archetype == EnemyArchetype.StarvedDungeonWolf));
                Assert.IsFalse(floorOne.Exists(definition => definition.archetype == EnemyArchetype.MossbackBearCub));
                Assert.IsFalse(floorOne.Exists(definition => definition.tier >= 3));

                Assert.IsTrue(floorFive.Exists(definition => definition.combatRole == EnemyCombatRole.Shield));
                Assert.IsTrue(floorFive.Exists(definition => definition.combatRole == EnemyCombatRole.Archer));
                Assert.IsTrue(floorFive.Exists(definition => definition.combatRole == EnemyCombatRole.Support));

                Assert.IsTrue(floorTen.Exists(definition => definition.combatRole == EnemyCombatRole.Trapper));
                Assert.IsTrue(floorTen.Exists(definition => definition.attackFamily == EnemyAttackFamily.RangedProjectile));
                Assert.IsTrue(floorTen.Exists(definition => definition.archetype == EnemyArchetype.MossbackBearCub));
                Assert.IsFalse(EnemyCatalog.CreateDefinitionsForFloor(7).Exists(definition => definition.archetype == EnemyArchetype.MossbackBearCub));
            }
            finally
            {
                DestroyDefinitions(floorOne);
                DestroyDefinitions(floorFive);
                DestroyDefinitions(floorTen);
            }
        }

        [Test]
        public void EncounterPacksExcludeRetiredEnemiesAndRespectFloorRanges()
        {
            List<EnemyPackDefinition> packs = DungeonEncounterDirector.CreateEnemyPacksForTests();
            Assert.GreaterOrEqual(packs.Count, 9);
            Assert.IsTrue(packs.Exists(pack => pack.packId == "BeginnerRoom"));
            Assert.IsTrue(packs.Exists(pack => pack.packId == "FirstTrapRoom"));

            for (int i = 0; i < packs.Count; i++)
            {
                EnemyPackDefinition pack = packs[i];
                Assert.Greater(pack.Count, 0, pack.packId);
                Assert.Greater(pack.weight, 0, pack.packId);
                Assert.LessOrEqual(pack.Count, 3, pack.packId);
                for (int archetypeIndex = 0; archetypeIndex < pack.archetypes.Length; archetypeIndex++)
                {
                    Assert.IsFalse(EnemyCatalog.IsRetiredFromNormalSpawns(pack.archetypes[archetypeIndex]), pack.packId);
                    EnemyDefinition definition = EnemyCatalog.CreateDefinition(pack.archetypes[archetypeIndex]);
                    try
                    {
                        Assert.IsTrue(definition.IsEligibleForFloor(pack.minFloor), pack.packId);
                        if (pack.maxFloor > 0)
                        {
                            Assert.IsTrue(definition.IsEligibleForFloor(pack.maxFloor), pack.packId);
                        }
                    }
                    finally
                    {
                        Object.DestroyImmediate(definition);
                    }
                }
            }
        }

        [Test]
        public void EncounterDirectorBuildsFloorPlansWithoutRetiredSlimes()
        {
            for (int floor = 1; floor <= 12; floor += 3)
            {
                DungeonBuildResult build = CreateEncounterBuild(floor);
                DungeonEncounterPlan plan = DungeonEncounterDirector.BuildPlan(build, null, 1300 + floor);

                Assert.Greater(plan.spawnedCount, 0, $"Floor {floor}");
                Assert.IsFalse(plan.archetypeCounts.ContainsKey(EnemyArchetype.Slime), $"Floor {floor}");
                Assert.IsFalse(plan.archetypeCounts.ContainsKey(EnemyArchetype.SpitterSlime), $"Floor {floor}");
                Assert.AreEqual(0, plan.retiredArchetypeViolationCount, $"Floor {floor}");
                Assert.IsFalse(plan.assignments.Exists(assignment =>
                    assignment.roomType == DungeonNodeKind.EntryHub ||
                    assignment.roomType == DungeonNodeKind.TransitUp ||
                    assignment.roomType == DungeonNodeKind.TransitDown), $"Floor {floor}");
                for (int i = 0; i < plan.assignments.Count; i++)
                {
                    Assert.LessOrEqual(plan.assignments[i].plannedCount, plan.assignments[i].roomCapacity, plan.assignments[i].templateId);
                }
            }
        }

        [Test]
        public void PrimitiveVisualsCreateDistinctBodyPlanParts()
        {
            Transform root = new GameObject("RosterVisualRoot").transform;
            EnemyDefinition humanoid = EnemyCatalog.CreateDefinition(EnemyArchetype.TorchlessPrisoner);
            EnemyDefinition quadruped = EnemyCatalog.CreateDefinition(EnemyArchetype.StarvedDungeonWolf);
            EnemyDefinition flyer = EnemyCatalog.CreateDefinition(EnemyArchetype.RustBellBat);
            try
            {
                GameObject humanoidEnemy = DungeonEncounterDirector.CreateEnemy(root, Vector3.zero, humanoid);
                GameObject quadrupedEnemy = DungeonEncounterDirector.CreateEnemy(root, Vector3.right * 4f, quadruped);
                GameObject flyerEnemy = DungeonEncounterDirector.CreateEnemy(root, Vector3.right * 8f, flyer);

                Assert.NotNull(humanoidEnemy.transform.Find("HumanoidHead"));
                Assert.NotNull(quadrupedEnemy.transform.Find("FrontLeftLeg"));
                Assert.NotNull(quadrupedEnemy.transform.Find("BackRightLeg"));
                Assert.NotNull(flyerEnemy.transform.Find("LeftWing"));
                Assert.NotNull(flyerEnemy.transform.Find("RightWing"));
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
                Object.DestroyImmediate(humanoid);
                Object.DestroyImmediate(quadruped);
                Object.DestroyImmediate(flyer);
            }
        }

        [Test]
        public void RetiredSlimeBountyCompatibilityTargetsNonSlimeArchetype()
        {
            BountyDefinition definition = BountyCatalog.Get("bounty.lantern_eater_slime");

            Assert.NotNull(definition);
            Assert.AreEqual("Lantern Thief Prisoner", definition.targetName);
            Assert.AreEqual(nameof(EnemyArchetype.TorchlessPrisoner), definition.targetArchetype);
        }

        private static DungeonBuildResult CreateEncounterBuild(int floor)
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = floor,
                seed = 3100 + floor,
                playerSpawn = Vector3.zero,
                playerSpawnNodeId = "entry"
            };
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "entry", roomType = DungeonNodeKind.EntryHub });
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "up", roomType = DungeonNodeKind.TransitUp });
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "down", roomType = DungeonNodeKind.TransitDown });
            AddEncounterRoom(build, "ordinary_a", DungeonNodeKind.Ordinary, new Vector3(28f, 0f, 0f), 1300f, 4);
            AddEncounterRoom(build, "ordinary_b", DungeonNodeKind.Ordinary, new Vector3(68f, 0f, 0f), 1300f, 4);
            AddEncounterRoom(build, "ordinary_c", DungeonNodeKind.Ordinary, new Vector3(108f, 0f, 0f), 1300f, 4);
            AddEncounterRoom(build, "landmark", DungeonNodeKind.Landmark, new Vector3(150f, 0f, 0f), 1800f, 5);
            AddEncounterRoom(build, "secret", DungeonNodeKind.Secret, new Vector3(190f, 0f, 0f), 900f, 2);
            return build;
        }

        private static void AddEncounterRoom(DungeonBuildResult build, string id, DungeonNodeKind kind, Vector3 center, float footprint, int spawnCount)
        {
            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = id,
                roomType = kind,
                footprintArea = footprint,
                bounds = new Bounds(center, new Vector3(28f, 8f, 28f))
            });

            for (int i = 0; i < spawnCount; i++)
            {
                build.spawnPoints.Add(new DungeonSpawnPointRecord
                {
                    nodeId = id,
                    category = DungeonSpawnPointCategory.EnemyMelee,
                    position = center + new Vector3((i % 2 == 0 ? -1f : 1f) * (4f + i * 1.5f), 3.5f, (i / 2) * 5f),
                    score = 10f - i
                });
            }
        }

        private static void DestroyDefinitions(List<EnemyDefinition> definitions)
        {
            if (definitions == null)
            {
                return;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                Object.DestroyImmediate(definitions[i]);
            }
        }
    }
}
