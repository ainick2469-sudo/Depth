using System.Collections.Generic;
using FrontierDepths.Combat;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class CombatGate3CTests
    {
        [Test]
        public void EnemyCatalog_DefaultArchetypesHaveDistinctCombatProfiles()
        {
            List<EnemyDefinition> definitions = EnemyCatalog.CreateDefaultDefinitions();

            EnemyDefinition slime = definitions.Find(definition => definition.archetype == EnemyArchetype.Slime);
            EnemyDefinition bat = definitions.Find(definition => definition.archetype == EnemyArchetype.Bat);
            EnemyDefinition grunt = definitions.Find(definition => definition.archetype == EnemyArchetype.GoblinGrunt);
            EnemyDefinition brute = definitions.Find(definition => definition.archetype == EnemyArchetype.GoblinBrute);

            Assert.NotNull(slime);
            Assert.NotNull(bat);
            Assert.NotNull(grunt);
            Assert.NotNull(brute);
            Assert.AreEqual(36f, slime.maxHealth);
            Assert.AreEqual(24f, bat.maxHealth);
            Assert.AreEqual(72f, grunt.maxHealth);
            Assert.AreEqual(126f, brute.maxHealth);
            Assert.Greater(bat.moveSpeed, slime.moveSpeed);
            Assert.Greater(brute.attackDamage, grunt.attackDamage);
            Assert.Greater(brute.attackWindupDuration, grunt.attackWindupDuration);
            Assert.Greater(grunt.attackWindupDuration, bat.attackWindupDuration);
            Assert.IsTrue(slime.IsEligibleForFloor(1));
            Assert.IsFalse(brute.IsEligibleForFloor(1));
            Assert.IsTrue(brute.IsEligibleForFloor(3));
        }

        [Test]
        public void FrontierRevolverUsesGate3CRetunedDamage()
        {
            WeaponDefinition revolver = Resources.Load<WeaponDefinition>("Definitions/Combat/Weapon_FrontierRevolver");
            WeaponDefinition runtimeFallback = ScriptableObject.CreateInstance<WeaponDefinition>();
            try
            {
                Assert.NotNull(revolver);
                Assert.AreEqual(15f, revolver.baseDamage);
                Assert.AreEqual(15f, runtimeFallback.baseDamage);
            }
            finally
            {
                Object.DestroyImmediate(runtimeFallback);
            }
        }

        [Test]
        public void EnemyDefinition_ConfiguresHealthControllerAndColliderScale()
        {
            Transform root = new GameObject("EnemyCreateRoot").transform;
            EnemyDefinition definition = EnemyCatalog.CreateDefinition(EnemyArchetype.Bat);
            try
            {
                GameObject enemy = DungeonEncounterDirector.CreateEnemy(root, Vector3.up * 3.5f, definition);

                EnemyHealth health = enemy.GetComponent<EnemyHealth>();
                SimpleMeleeEnemyController melee = enemy.GetComponent<SimpleMeleeEnemyController>();
                CharacterController characterController = enemy.GetComponent<CharacterController>();

                Assert.NotNull(health);
                Assert.NotNull(melee);
                Assert.NotNull(characterController);
                Assert.AreEqual(definition.maxHealth, health.MaxHealth);
                Assert.AreEqual(definition.archetype, health.Archetype);
                Assert.AreEqual(definition.moveSpeed, melee.MoveSpeed);
                Assert.AreEqual(definition.attackDamage, melee.AttackDamage);
                Assert.AreEqual(definition.attackRange, melee.AttackRange);
                Assert.AreEqual(definition.attackWindupDuration, melee.AttackWindupDuration);
                Assert.AreEqual(definition.hearingRadiusMultiplier, melee.HearingRadiusMultiplier);
                Assert.AreEqual(definition.groupAlertRadius, melee.GroupAlertRadius);
                Assert.AreEqual(definition.visualScale, enemy.transform.localScale);
                Assert.LessOrEqual(characterController.radius, 0.82f);
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void EncounterDirector_FloorOneBudgetUsesGroupsAndExcludesBrutes()
        {
            DungeonBuildResult build = CreateEncounterBuild(1);

            DungeonEncounterPlan plan = DungeonEncounterDirector.BuildPlan(build, new[] { new Vector3(40f, 3.5f, 18f) }, 12345);

            Assert.GreaterOrEqual(plan.spawnedCount, 8);
            Assert.LessOrEqual(plan.spawnedCount, 12);
            Assert.IsFalse(plan.archetypeCounts.ContainsKey(EnemyArchetype.GoblinBrute));
            Assert.IsTrue(plan.assignments.Exists(assignment => assignment.plannedCount == 2));
            Assert.IsTrue(plan.assignments.Exists(assignment => assignment.plannedCount == 3));
            Assert.GreaterOrEqual(plan.groupFightCount, 2);
            Assert.LessOrEqual(plan.soloRoomCount, 1);
            Assert.GreaterOrEqual(plan.emptyEligibleRoomCount, 1);
            Assert.IsFalse(plan.assignments.Exists(assignment =>
                assignment.roomType == DungeonNodeKind.EntryHub ||
                assignment.roomType == DungeonNodeKind.TransitUp ||
                assignment.roomType == DungeonNodeKind.TransitDown));
            Assert.Less(plan.assignments.Count, CountEligibleRooms(build));
        }

        [Test]
        public void EncounterDirector_FloorOneBatCapsPreventMosquitoHell()
        {
            DungeonBuildResult build = CreateEncounterBuild(1);

            DungeonEncounterPlan plan = DungeonEncounterDirector.BuildPlan(build, null, 5678);

            plan.archetypeCounts.TryGetValue(EnemyArchetype.Bat, out int totalBats);
            Assert.LessOrEqual(totalBats, 3);
            Assert.IsFalse(plan.archetypeCounts.Count == 1 && plan.archetypeCounts.ContainsKey(EnemyArchetype.Bat));
            for (int i = 0; i < plan.assignments.Count; i++)
            {
                Assert.LessOrEqual(CountArchetypes(plan.assignments[i], EnemyArchetype.Bat), 2);
            }
        }

        [Test]
        public void EncounterDirector_FloorTwoOrHigherIncludesThreePackWhenSafe()
        {
            DungeonBuildResult build = CreateEncounterBuild(2);

            DungeonEncounterPlan plan = DungeonEncounterDirector.BuildPlan(build, null, 2468);

            Assert.GreaterOrEqual(plan.spawnedCount, 10);
            Assert.LessOrEqual(plan.spawnedCount, 15);
            Assert.IsTrue(plan.assignments.Exists(assignment => assignment.plannedCount == 3));
            Assert.GreaterOrEqual(plan.groupFightCount, 2);
        }

        [Test]
        public void EncounterDirector_LandmarkRoomsPreferGroupFights()
        {
            DungeonBuildResult build = CreateEncounterBuild(1);

            DungeonEncounterPlan plan = DungeonEncounterDirector.BuildPlan(build, null, 12345);
            DungeonEncounterRoomAssignment landmark = plan.assignments.Find(assignment => assignment.roomType == DungeonNodeKind.Landmark);

            Assert.NotNull(landmark);
            Assert.GreaterOrEqual(landmark.plannedCount, 2);
            Assert.AreEqual(DungeonEncounterRoomRole.LandmarkFight, landmark.role);
        }

        [Test]
        public void EncounterDirector_FloorThreeCatalogCanSelectBrutes()
        {
            List<EnemyDefinition> definitions = EnemyCatalog.CreateDefinitionsForFloor(3);

            Assert.IsTrue(definitions.Exists(definition => definition.archetype == EnemyArchetype.GoblinBrute));
            Assert.IsFalse(EnemyCatalog.CreateDefinitionsForFloor(1).Exists(definition => definition.archetype == EnemyArchetype.GoblinBrute));
        }

        [Test]
        public void EncounterDirector_UnderfillsBestEffortWithWarning()
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = 1,
                seed = 7,
                playerSpawn = Vector3.zero,
                playerSpawnNodeId = "entry"
            };
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "entry", roomType = DungeonNodeKind.EntryHub });
            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "ordinary",
                roomType = DungeonNodeKind.Ordinary,
                footprintArea = 1300f,
                bounds = new Bounds(new Vector3(30f, 0f, 0f), new Vector3(24f, 8f, 24f))
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "ordinary",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(30f, 3.5f, 0f),
                score = 1f
            });

            DungeonEncounterPlan plan = DungeonEncounterDirector.BuildPlan(build, null, 7);

            Assert.AreEqual(1, plan.spawnedCount);
            Assert.IsTrue(plan.warning.Contains("underfilled"));
        }

        [Test]
        public void EncounterDirector_GroupTemplatesRespectBudgetCapacityAndSpacing()
        {
            DungeonBuildResult build = CreateEncounterBuild(1);

            DungeonEncounterPlan plan = DungeonEncounterDirector.BuildPlan(build, null, 12345);

            Assert.LessOrEqual(plan.spawnedCount, plan.requestedBudget);
            for (int assignmentIndex = 0; assignmentIndex < plan.assignments.Count; assignmentIndex++)
            {
                DungeonEncounterRoomAssignment assignment = plan.assignments[assignmentIndex];
                Assert.LessOrEqual(assignment.plannedCount, assignment.roomCapacity);
                Assert.IsFalse(string.IsNullOrWhiteSpace(assignment.templateId));

                List<DungeonEncounterSpawn> roomSpawns = plan.spawns.FindAll(spawn => spawn.roomId == assignment.roomId);
                for (int i = 0; i < roomSpawns.Count; i++)
                {
                    for (int j = i + 1; j < roomSpawns.Count; j++)
                    {
                        Assert.GreaterOrEqual(Vector3.Distance(roomSpawns[i].position, roomSpawns[j].position), 14f);
                    }
                }
            }
        }

        [Test]
        public void EncounterDirector_BrutesDoNotSpawnInSmallRooms()
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = 3,
                seed = 77,
                playerSpawn = Vector3.zero,
                playerSpawnNodeId = "entry"
            };
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "entry", roomType = DungeonNodeKind.EntryHub });
            AddEncounterRoom(build, "small_a", DungeonNodeKind.Ordinary, 36f, 700f, 3);
            AddEncounterRoom(build, "small_b", DungeonNodeKind.Ordinary, 76f, 700f, 3);
            AddEncounterRoom(build, "small_c", DungeonNodeKind.Ordinary, 116f, 700f, 3);

            DungeonEncounterPlan plan = DungeonEncounterDirector.BuildPlan(build, null, 77);

            for (int i = 0; i < plan.assignments.Count; i++)
            {
                if (plan.assignments[i].archetypes.Contains(EnemyArchetype.GoblinBrute))
                {
                    Assert.GreaterOrEqual(plan.assignments[i].roomCapacity, 2);
                    Assert.Fail("Brute spawned in a small-room assignment.");
                }
            }
        }

        [Test]
        public void SimpleMeleeEnemy_AttackWindupDelaysDamageAndAppliesOnce()
        {
            GameObject player = new GameObject("WindupPlayer");
            GameObject enemy = new GameObject("WindupEnemy");
            try
            {
                player.transform.position = Vector3.forward * 1.5f;
                PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();
                enemy.AddComponent<CharacterController>();
                enemy.AddComponent<EnemyHealth>().Configure(50f, Color.red);
                SimpleMeleeEnemyController melee = enemy.AddComponent<SimpleMeleeEnemyController>();

                Assert.IsTrue(melee.TryStartAttackWindup(playerHealth, 1f));
                Assert.IsTrue(melee.IsAttackWindingUp);
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
                Assert.IsFalse(melee.TickAttackWindup(1f + melee.AttackWindupDuration * 0.5f));
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
                Assert.IsTrue(melee.TickAttackWindup(1f + melee.AttackWindupDuration + 0.01f));
                Assert.IsFalse(melee.IsAttackWindingUp);
                Assert.AreEqual(90f, playerHealth.CurrentHealth);
                Assert.IsFalse(melee.TickAttackWindup(1f + melee.AttackWindupDuration + 0.02f));
                Assert.AreEqual(90f, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void SimpleMeleeEnemy_AttackWindupCancelsOnRangeExitAndKeepsCooldown()
        {
            GameObject player = new GameObject("WindupExitPlayer");
            GameObject enemy = new GameObject("WindupExitEnemy");
            try
            {
                player.transform.position = Vector3.forward * 1.5f;
                PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();
                enemy.AddComponent<CharacterController>();
                enemy.AddComponent<EnemyHealth>().Configure(50f, Color.red);
                SimpleMeleeEnemyController melee = enemy.AddComponent<SimpleMeleeEnemyController>();

                Assert.IsTrue(melee.TryStartAttackWindup(playerHealth, 2f));
                player.transform.position = Vector3.forward * 9f;

                Assert.IsFalse(melee.TickAttackWindup(2f + melee.AttackWindupDuration + 0.01f));
                Assert.IsFalse(melee.IsAttackWindingUp);
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
                Assert.Greater(melee.NextAttackTime, 2f + melee.AttackWindupDuration);
                Assert.IsFalse(melee.CanAttack(playerHealth, 2f + melee.AttackWindupDuration + 0.02f));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void EncounterDirector_ClearOnlyEncounterRootsAndSuppressesDrops()
        {
            GameObject runtimeRoot = new GameObject("RuntimeRoot");
            try
            {
                Transform dummyRoot = new GameObject("CombatTestStation").transform;
                dummyRoot.SetParent(runtimeRoot.transform, false);
                new GameObject("Dummy").transform.SetParent(dummyRoot, false);

                Transform enemyRoot = new GameObject(DungeonEncounterDirector.EncounterEnemiesRootName).transform;
                enemyRoot.SetParent(runtimeRoot.transform, false);
                new GameObject("EncounterEnemy").transform.SetParent(enemyRoot, false);

                Transform dropRoot = new GameObject(EncounterDropService.DropRootName).transform;
                dropRoot.SetParent(runtimeRoot.transform, false);
                new GameObject("GoldPickup").transform.SetParent(dropRoot, false);

                DungeonEncounterDirector director = new DungeonEncounterDirector(runtimeRoot.transform);
                director.Clear(true);

                Assert.NotNull(runtimeRoot.transform.Find("CombatTestStation/Dummy"));
                Assert.AreEqual(0, runtimeRoot.transform.Find(DungeonEncounterDirector.EncounterEnemiesRootName).childCount);
                Assert.AreEqual(0, runtimeRoot.transform.Find(EncounterDropService.DropRootName).childCount);
            }
            finally
            {
                Object.DestroyImmediate(runtimeRoot);
            }
        }

        [Test]
        public void EncounterDropRollsRespectPerEnemyCap()
        {
            EnemyDefinition definition = EnemyCatalog.CreateDefinition(EnemyArchetype.GoblinBrute);
            try
            {
                definition.goldDropChance = 1f;
                definition.healthDropChance = 1f;
                definition.ammoDropChance = 1f;

                List<EncounterDropKind> drops = EncounterDropService.RollDropsForTests(definition, 99);

                Assert.AreEqual(EncounterDropService.MaxDropsPerEnemyDeath, drops.Count);
                Assert.Contains(EncounterDropKind.Gold, drops);
                Assert.Contains(EncounterDropKind.Health, drops);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void HealthPickupAppliesOnlyToPlayer()
        {
            GameObject player = new GameObject("PickupPlayer");
            GameObject nonPlayer = new GameObject("PickupEnemy");
            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();
                playerHealth.ApplyDamage(new DamageInfo { amount = 25f, source = nonPlayer, damageType = DamageType.Physical }, 1f);
                HealthPickup pickup = pickupObject.AddComponent<HealthPickup>();
                pickup.Configure(10f);

                Assert.IsFalse(pickup.ApplyToPlayer(nonPlayer));
                Assert.IsTrue(pickup.ApplyToPlayer(player));
                Assert.AreEqual(85f, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(nonPlayer);
                Object.DestroyImmediate(pickupObject);
            }
        }

        [Test]
        public void AmmoPickupAddsReserveAndDoesNotOverfill()
        {
            GameObject player = new GameObject("AmmoPickupPlayer");
            GameObject cameraObject = new GameObject("AmmoPickupCamera", typeof(Camera));
            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                cameraObject.transform.SetParent(player.transform, false);
                player.AddComponent<PlayerHealth>();
                PlayerWeaponController weapon = player.AddComponent<PlayerWeaponController>();
                for (int i = 0; i < weapon.MagazineSize; i++)
                {
                    Assert.IsTrue(weapon.TryFire(i * 0.5f));
                }

                Assert.AreEqual(0, weapon.CurrentAmmo);
                int reserveBefore = weapon.ReserveAmmo;

                AmmoPickup pickup = pickupObject.AddComponent<AmmoPickup>();
                pickup.Configure(3);

                Assert.IsTrue(pickup.ApplyToPlayer(player));
                Assert.AreEqual(0, weapon.CurrentAmmo);
                Assert.AreEqual(Mathf.Min(weapon.MaxReserveAmmo, reserveBefore + 3), weapon.ReserveAmmo);
                Assert.LessOrEqual(weapon.ReserveAmmo, weapon.MaxReserveAmmo);
            }
            finally
            {
                DestroyRuntimeFeedbackRoot();
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(pickupObject);
            }
        }

        private static DungeonBuildResult CreateEncounterBuild(int floorIndex)
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = floorIndex,
                seed = 103,
                playerSpawn = Vector3.zero,
                playerSpawnNodeId = "entry"
            };
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "entry", roomType = DungeonNodeKind.EntryHub });
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "transit_up", roomType = DungeonNodeKind.TransitUp });
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "transit_down", roomType = DungeonNodeKind.TransitDown });
            AddEncounterRoom(build, "ordinary_a", DungeonNodeKind.Ordinary, 36f, 1450f, 3);
            AddEncounterRoom(build, "ordinary_b", DungeonNodeKind.Ordinary, 76f, 1450f, 3);
            AddEncounterRoom(build, "ordinary_c", DungeonNodeKind.Ordinary, 116f, 1450f, 3);
            AddEncounterRoom(build, "ordinary_d", DungeonNodeKind.Ordinary, 156f, 1450f, 3);
            AddEncounterRoom(build, "landmark", DungeonNodeKind.Landmark, 198f, 2100f, 4);
            return build;
        }

        private static void AddEncounterRoom(DungeonBuildResult build, string nodeId, DungeonNodeKind roomType, float x, float footprint, int spawnCount)
        {
            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = nodeId,
                roomType = roomType,
                footprintArea = footprint,
                bounds = new Bounds(new Vector3(x, 0f, 0f), new Vector3(30f, 8f, 30f))
            });

            for (int i = 0; i < spawnCount; i++)
            {
                build.spawnPoints.Add(new DungeonSpawnPointRecord
                {
                    nodeId = nodeId,
                    category = DungeonSpawnPointCategory.EnemyMelee,
                    position = new Vector3(x, 3.5f, -18f + i * 18f),
                    score = 100f - i
                });
            }
        }

        private static int CountEligibleRooms(DungeonBuildResult build)
        {
            int count = 0;
            for (int i = 0; i < build.rooms.Count; i++)
            {
                if (!DungeonEncounterDirector.IsSafeRoomType(build.rooms[i].roomType) &&
                    build.GetSpawnPoints(build.rooms[i].nodeId, DungeonSpawnPointCategory.EnemyMelee).Count > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountArchetypes(DungeonEncounterRoomAssignment assignment, EnemyArchetype archetype)
        {
            int count = 0;
            for (int i = 0; i < assignment.archetypes.Count; i++)
            {
                if (assignment.archetypes[i] == archetype)
                {
                    count++;
                }
            }

            return count;
        }

        private static void DestroyRuntimeFeedbackRoot()
        {
            Transform feedbackRoot = PlayerWeaponController.GetOrCreateRuntimeFeedbackRoot();
            if (feedbackRoot != null)
            {
                Object.DestroyImmediate(feedbackRoot.gameObject);
            }
        }
    }
}
