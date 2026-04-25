using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.World;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.Tests.EditMode
{
    public class CombatGate3BTests
    {
        [SetUp]
        public void SetUp()
        {
            GameplayEventBus.ClearForTests();
        }

        [TearDown]
        public void TearDown()
        {
            GameplayEventBus.ClearForTests();
        }

        [Test]
        public void PlayerHealth_TakesDamageAndEmitsDamageTaken()
        {
            GameObject player = new GameObject("PlayerHealthTest");
            try
            {
                PlayerHealth health = player.AddComponent<PlayerHealth>();
                int events = 0;
                GameplayEventBus.Subscribe(evt =>
                {
                    if (evt.eventType == GameplayEventType.DamageTaken)
                    {
                        events++;
                    }
                });

                DamageResult result = health.ApplyDamage(CreateDamageInfo(25f, player), 1f);

                Assert.IsTrue(result.applied);
                Assert.AreEqual(75f, health.CurrentHealth);
                Assert.AreEqual(1, events);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PlayerHealth_InvulnerabilityBlocksRepeatedDamage()
        {
            GameObject player = new GameObject("PlayerInvulnerabilityTest");
            try
            {
                PlayerHealth health = player.AddComponent<PlayerHealth>();

                Assert.IsTrue(health.ApplyDamage(CreateDamageInfo(10f, player), 1f).applied);
                Assert.IsFalse(health.ApplyDamage(CreateDamageInfo(10f, player), 1.1f).applied);
                Assert.IsTrue(health.ApplyDamage(CreateDamageInfo(10f, player), 1.3f).applied);

                Assert.AreEqual(80f, health.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PlayerHealth_DeathEventFiresOnceAndResetRestores()
        {
            GameObject player = new GameObject("PlayerDeathOnceTest");
            try
            {
                PlayerHealth health = player.AddComponent<PlayerHealth>();
                int deaths = 0;
                health.Died += _ => deaths++;

                DamageResult first = health.ApplyDamage(CreateDamageInfo(999f, player), 1f);
                DamageResult second = health.ApplyDamage(CreateDamageInfo(999f, player), 2f);

                Assert.IsTrue(first.killedTarget);
                Assert.IsFalse(second.applied);
                Assert.AreEqual(1, deaths);
                health.ResetHealth();
                Assert.IsFalse(health.IsDead);
                Assert.AreEqual(health.MaxHealth, health.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void EnemyHealth_TakesDamageAndPublishesEnemyKilledOnce()
        {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                EnemyHealth health = enemy.AddComponent<EnemyHealth>();
                health.Configure(50f, Color.red);
                int killedEvents = 0;
                int diedEvents = 0;
                health.Died += _ => diedEvents++;
                GameplayEventBus.Subscribe(evt =>
                {
                    if (evt.eventType == GameplayEventType.EnemyKilled)
                    {
                        killedEvents++;
                    }
                });

                Assert.IsTrue(health.ApplyDamage(CreateDamageInfo(25f, enemy)).applied);
                Assert.AreEqual(25f, health.CurrentHealth);
                DamageResult lethal = health.ApplyDamage(CreateDamageInfo(25f, enemy));
                DamageResult ignored = health.ApplyDamage(CreateDamageInfo(25f, enemy));

                Assert.IsTrue(lethal.killedTarget);
                Assert.IsFalse(ignored.applied);
                Assert.AreEqual(1, diedEvents);
                Assert.AreEqual(1, killedEvents);
                Assert.IsFalse(enemy.GetComponent<Collider>().enabled);
            }
            finally
            {
                Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void EnemyHealth_UsesPropertyBlockWithoutMutatingSharedMaterial()
        {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Material shared = new Material(Shader.Find("Standard")) { color = Color.green };
            try
            {
                Renderer renderer = enemy.GetComponent<Renderer>();
                renderer.sharedMaterial = shared;
                EnemyHealth health = enemy.AddComponent<EnemyHealth>();
                health.Configure(50f, Color.red);

                health.ApplyDamage(CreateDamageInfo(5f, enemy));

                Assert.AreEqual(Color.green, shared.color);
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Assert.AreEqual(Color.white, block.GetColor("_Color"));
            }
            finally
            {
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(shared);
            }
        }

        [Test]
        public void SimpleMeleeEnemy_RespectsAttackCooldown()
        {
            GameObject player = new GameObject("MeleePlayer");
            GameObject enemy = new GameObject("MeleeEnemy");
            try
            {
                player.transform.position = Vector3.forward * 1.5f;
                PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();
                enemy.AddComponent<CharacterController>();
                EnemyHealth enemyHealth = enemy.AddComponent<EnemyHealth>();
                enemyHealth.Configure(50f, Color.red);
                SimpleMeleeEnemyController melee = enemy.AddComponent<SimpleMeleeEnemyController>();

                Assert.IsTrue(melee.TryApplyAttack(playerHealth, 1f));
                Assert.IsFalse(melee.TryApplyAttack(playerHealth, 1.5f));
                Assert.IsTrue(melee.TryApplyAttack(playerHealth, 2.3f));
                Assert.AreEqual(80f, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void SimpleMeleeEnemy_DeadEnemyCannotAttack()
        {
            GameObject player = new GameObject("DeadEnemyPlayer");
            GameObject enemy = new GameObject("DeadEnemy");
            try
            {
                player.transform.position = Vector3.forward * 1.5f;
                PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();
                enemy.AddComponent<CharacterController>();
                EnemyHealth enemyHealth = enemy.AddComponent<EnemyHealth>();
                enemyHealth.Configure(50f, Color.red);
                SimpleMeleeEnemyController melee = enemy.AddComponent<SimpleMeleeEnemyController>();
                enemyHealth.ApplyDamage(CreateDamageInfo(999f, player));

                Assert.IsFalse(melee.TryApplyAttack(playerHealth, 1f));
                Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void SimpleMeleeEnemy_DamageAggroChasesImmediatelyOutsideDetection()
        {
            GameObject player = new GameObject("DamageAggroPlayer");
            GameObject enemy = new GameObject("DamageAggroEnemy");
            try
            {
                player.transform.position = new Vector3(80f, 0f, 0f);
                player.AddComponent<PlayerHealth>();
                enemy.AddComponent<CharacterController>();
                enemy.AddComponent<EnemyHealth>().Configure(50f, Color.red);
                SimpleMeleeEnemyController melee = enemy.AddComponent<SimpleMeleeEnemyController>();

                DamageInfo damageInfo = CreateDamageInfo(25f, player);
                melee.HandleDamagedForTests(damageInfo, new DamageResult { applied = true, damageApplied = 25f }, 1f);

                Assert.AreEqual(SimpleMeleeEnemyState.Chase, melee.State);
                Assert.IsTrue(melee.IsAlertedAt(8.9f));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void SimpleMeleeEnemy_HearsGunfireWithinRadiusWithoutLineOfSight()
        {
            GameObject player = new GameObject("GunfirePlayer");
            GameObject enemy = new GameObject("GunfireEnemy");
            try
            {
                player.AddComponent<PlayerHealth>();
                enemy.transform.position = new Vector3(40f, 0f, 0f);
                enemy.AddComponent<CharacterController>();
                enemy.AddComponent<EnemyHealth>().Configure(50f, Color.red);
                SimpleMeleeEnemyController melee = enemy.AddComponent<SimpleMeleeEnemyController>();

                bool alerted = melee.HandleWeaponFiredForTests(new GameplayEvent
                {
                    eventType = GameplayEventType.WeaponFired,
                    sourceObject = player,
                    worldPosition = Vector3.zero,
                    radius = 55f
                }, 2f);

                Assert.IsTrue(alerted);
                Assert.AreEqual(SimpleMeleeEnemyState.Chase, melee.State);
                Assert.IsTrue(melee.IsAlertedAt(9.9f));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void SimpleMeleeEnemy_IgnoresGunfireOutsideRadius()
        {
            GameObject enemy = new GameObject("DistantGunfireEnemy");
            try
            {
                enemy.transform.position = new Vector3(70f, 0f, 0f);
                enemy.AddComponent<CharacterController>();
                enemy.AddComponent<EnemyHealth>().Configure(50f, Color.red);
                SimpleMeleeEnemyController melee = enemy.AddComponent<SimpleMeleeEnemyController>();

                bool alerted = melee.HandleWeaponFiredForTests(new GameplayEvent
                {
                    eventType = GameplayEventType.WeaponFired,
                    worldPosition = Vector3.zero,
                    radius = 55f
                }, 2f);

                Assert.IsFalse(alerted);
                Assert.IsFalse(melee.IsAlertedAt(2f));
            }
            finally
            {
                Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void SimpleMeleeEnemy_DamagedEnemyAlertsNearbyAllies()
        {
            GameObject player = new GameObject("GroupAggroPlayer");
            GameObject firstEnemy = new GameObject("GroupAggroFirst");
            GameObject allyEnemy = new GameObject("GroupAggroAlly");
            try
            {
                player.AddComponent<PlayerHealth>();
                firstEnemy.AddComponent<CharacterController>();
                firstEnemy.AddComponent<EnemyHealth>().Configure(50f, Color.red);
                SimpleMeleeEnemyController first = firstEnemy.AddComponent<SimpleMeleeEnemyController>();

                allyEnemy.transform.position = new Vector3(10f, 0f, 0f);
                allyEnemy.AddComponent<CharacterController>();
                allyEnemy.AddComponent<EnemyHealth>().Configure(50f, Color.red);
                SimpleMeleeEnemyController ally = allyEnemy.AddComponent<SimpleMeleeEnemyController>();

                first.HandleDamagedForTests(
                    CreateDamageInfo(25f, player),
                    new DamageResult { applied = true, damageApplied = 25f },
                    3f);

                Assert.IsTrue(ally.IsAlertedAt(10.9f));
                Assert.AreEqual(SimpleMeleeEnemyState.Chase, ally.State);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(firstEnemy);
                Object.DestroyImmediate(allyEnemy);
            }
        }

        [Test]
        public void SimpleMeleeEnemy_DeadEnemyIgnoresHearingAndUnsubscribes()
        {
            GameObject player = new GameObject("DeadHearingPlayer");
            GameObject enemy = new GameObject("DeadHearingEnemy");
            try
            {
                player.AddComponent<PlayerHealth>();
                enemy.AddComponent<CharacterController>();
                EnemyHealth enemyHealth = enemy.AddComponent<EnemyHealth>();
                enemyHealth.Configure(50f, Color.red);
                SimpleMeleeEnemyController melee = enemy.AddComponent<SimpleMeleeEnemyController>();

                melee.HandleWeaponFiredForTests(new GameplayEvent
                {
                    eventType = GameplayEventType.WeaponFired,
                    sourceObject = player,
                    worldPosition = Vector3.zero,
                    radius = 0f
                }, 3f);
                Assert.GreaterOrEqual(GameplayEventBus.SubscriberCount, 1);
                enemyHealth.ApplyDamage(CreateDamageInfo(999f, player));

                bool alerted = melee.HandleWeaponFiredForTests(new GameplayEvent
                {
                    eventType = GameplayEventType.WeaponFired,
                    sourceObject = player,
                    worldPosition = Vector3.zero,
                    radius = 55f
                }, 4f);

                Assert.IsFalse(alerted);
                Assert.IsFalse(melee.IsAlertedAt(4f));
                Assert.AreEqual(0, GameplayEventBus.SubscriberCount);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void DeadPlayerCannotFire()
        {
            GameObject player = new GameObject("DeadPlayerWeapon");
            GameObject cameraObject = new GameObject("DeadPlayerCamera", typeof(Camera));
            try
            {
                cameraObject.transform.SetParent(player.transform, false);
                PlayerHealth health = player.AddComponent<PlayerHealth>();
                PlayerWeaponController weapon = player.AddComponent<PlayerWeaponController>();
                health.ApplyDamage(CreateDamageInfo(999f, player), 1f);
                int ammo = weapon.CurrentAmmo;

                WeaponInputFrameResult result = weapon.HandleWeaponInputFrame(1.5f, true, true, false);

                Assert.IsTrue(result.inputBlocked);
                Assert.IsFalse(result.fired);
                Assert.AreEqual(ammo, weapon.CurrentAmmo);
            }
            finally
            {
                DestroyRuntimeFeedbackRoot();
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void CombatTestEnemySpawn_ChoosesNearbySafeOrdinaryEnemyPoint()
        {
            DungeonBuildResult build = CreateBuildForSpawnSelection();
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "entry",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(18f, 3.5f, 0f),
                score = 100f
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "ordinary_near",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(24f, 3.5f, 0f),
                score = 50f
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "ordinary_far",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(60f, 3.5f, 0f),
                score = 200f
            });

            DungeonSpawnPointRecord selected = DungeonSceneController.SelectCombatTestEnemySpawn(build);

            Assert.NotNull(selected);
            Assert.AreEqual("ordinary_near", selected.nodeId);
        }

        [Test]
        public void CombatTestEnemySpawn_AvoidsDummyOccupiedPoints()
        {
            DungeonBuildResult build = CreateBuildForSpawnSelection();
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "ordinary_near",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(24f, 3.5f, 0f),
                score = 200f
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "ordinary_far",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(42f, 3.5f, 0f),
                score = 10f
            });

            DungeonSpawnPointRecord selected = DungeonSceneController.SelectCombatTestEnemySpawn(
                build,
                new[] { new Vector3(24.5f, 3.5f, 0f) });

            Assert.NotNull(selected);
            Assert.AreEqual("ordinary_far", selected.nodeId);
        }

        [Test]
        public void CombatTestEnemySpawn_SelectsBestEffortPopulationWithSpacing()
        {
            DungeonBuildResult build = CreateBuildForSpawnSelection();
            build.floorIndex = 1;
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "ordinary_near",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(24f, 3.5f, 0f),
                score = 100f
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "ordinary_far",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(42f, 3.5f, 0f),
                score = 90f
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "landmark",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(70f, 3.5f, 0f),
                score = 200f
            });

            List<DungeonSpawnPointRecord> selected = DungeonSceneController.SelectCombatTestEnemySpawns(build, 3);

            Assert.AreEqual(3, selected.Count);
            Assert.AreEqual("ordinary_near", selected[0].nodeId);
            Assert.AreEqual("ordinary_far", selected[1].nodeId);
            Assert.AreEqual("landmark", selected[2].nodeId);
        }

        [Test]
        public void CombatTestEnemySpawn_BestEffortUnderfillsInsteadOfFailing()
        {
            DungeonBuildResult build = CreateBuildForSpawnSelection();
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "ordinary_near",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(24f, 3.5f, 0f),
                score = 100f
            });

            List<DungeonSpawnPointRecord> selected = DungeonSceneController.SelectCombatTestEnemySpawns(build, 3);

            Assert.AreEqual(1, selected.Count);
        }

        [Test]
        public void CombatTestEnemySpawn_AvoidsEntryTransitAndBlockedGeometry()
        {
            DungeonBuildResult build = CreateBuildForSpawnSelection();
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "transit", roomType = DungeonNodeKind.TransitDown });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "entry",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(30f, 3.5f, 0f),
                score = 500f
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "transit",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(45f, 3.5f, 0f),
                score = 400f
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "ordinary_near",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(60f, 3.5f, 0f),
                bounds = new Bounds(new Vector3(60f, 3.5f, 0f), new Vector3(3f, 6f, 3f)),
                score = 300f
            });
            build.interactables.Add(new DungeonInteractableBuildRecord
            {
                nodeId = "ordinary_near",
                bounds = new Bounds(new Vector3(60f, 3.5f, 0f), new Vector3(6f, 6f, 6f))
            });
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "ordinary_far",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(80f, 3.5f, 0f),
                score = 1f
            });

            List<DungeonSpawnPointRecord> selected = DungeonSceneController.SelectCombatTestEnemySpawns(build, 3);

            Assert.AreEqual(1, selected.Count);
            Assert.AreEqual("ordinary_far", selected[0].nodeId);
        }

        [Test]
        public void CombatTestEnemySpawn_ReachabilityRejectsDisconnectedGraphRoom()
        {
            DungeonBuildResult build = CreateBuildForSpawnSelection();
            build.graph = new DungeonLayoutGraph();
            build.graph.nodes.Add(new DungeonNode { nodeId = "entry", nodeKind = DungeonNodeKind.EntryHub, gridPosition = Vector2Int.zero });
            build.graph.nodes.Add(new DungeonNode { nodeId = "ordinary_near", nodeKind = DungeonNodeKind.Ordinary, gridPosition = Vector2Int.right });
            build.graph.entryHubNodeId = "entry";
            build.playerSpawnNodeId = "entry";
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "ordinary_near",
                category = DungeonSpawnPointCategory.EnemyMelee,
                position = new Vector3(24f, 3.5f, 0f),
                score = 100f
            });

            List<DungeonSpawnPointRecord> selected = DungeonSceneController.SelectCombatTestEnemySpawns(build, 1, null, true);

            Assert.AreEqual(0, selected.Count);
        }

        [Test]
        public void TargetDummiesDoNotEmitEnemyKilled()
        {
            GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                TargetDummyHealth health = dummy.AddComponent<TargetDummyHealth>();
                health.Configure(TargetDummyKind.Standard);
                int enemyKilled = 0;
                GameplayEventBus.Subscribe(evt =>
                {
                    if (evt.eventType == GameplayEventType.EnemyKilled)
                    {
                        enemyKilled++;
                    }
                });

                health.ApplyDamage(CreateDamageInfo(999f, dummy));

                Assert.AreEqual(0, enemyKilled);
            }
            finally
            {
                Object.DestroyImmediate(dummy);
            }
        }

        [Test]
        public void CombatTestDummiesAreNotCountedAsLivingEnemies()
        {
            GameObject root = new GameObject("CombatRoots");
            try
            {
                Transform dummyRoot = new GameObject("CombatTestStation").transform;
                dummyRoot.SetParent(root.transform, false);
                GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                dummy.transform.SetParent(dummyRoot, false);
                dummy.AddComponent<TargetDummyHealth>().Configure(TargetDummyKind.Standard);

                Transform enemyRoot = new GameObject("CombatTestEnemies").transform;
                enemyRoot.SetParent(root.transform, false);
                GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemy.transform.SetParent(enemyRoot, false);
                enemy.AddComponent<EnemyHealth>().Configure(50f, Color.red);

                Assert.AreEqual(1, enemyRoot.GetComponentsInChildren<EnemyHealth>(true).Length);
                Assert.AreEqual(0, dummyRoot.GetComponentsInChildren<EnemyHealth>(true).Length);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MultipleEnemiesRespectPlayerInvulnerabilityWindow()
        {
            GameObject player = new GameObject("MultiEnemyPlayer");
            GameObject firstEnemy = new GameObject("FirstEnemy");
            GameObject secondEnemy = new GameObject("SecondEnemy");
            try
            {
                PlayerHealth health = player.AddComponent<PlayerHealth>();
                firstEnemy.AddComponent<CharacterController>();
                firstEnemy.AddComponent<EnemyHealth>().Configure(50f, Color.red);
                SimpleMeleeEnemyController first = firstEnemy.AddComponent<SimpleMeleeEnemyController>();
                secondEnemy.AddComponent<CharacterController>();
                secondEnemy.AddComponent<EnemyHealth>().Configure(50f, Color.red);
                SimpleMeleeEnemyController second = secondEnemy.AddComponent<SimpleMeleeEnemyController>();

                Assert.IsTrue(first.TryApplyAttack(health, 1f));
                Assert.IsFalse(second.TryApplyAttack(health, 1f));

                Assert.AreEqual(90f, health.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(firstEnemy);
                Object.DestroyImmediate(secondEnemy);
            }
        }

        private static DungeonBuildResult CreateBuildForSpawnSelection()
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = 1,
                playerSpawn = Vector3.zero,
                playerSpawnNodeId = "entry"
            };
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "entry", roomType = DungeonNodeKind.EntryHub });
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "ordinary_near", roomType = DungeonNodeKind.Ordinary });
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "ordinary_far", roomType = DungeonNodeKind.Ordinary });
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "landmark", roomType = DungeonNodeKind.Landmark });
            return build;
        }

        private static DamageInfo CreateDamageInfo(float amount, GameObject source)
        {
            return new DamageInfo
            {
                amount = amount,
                source = source,
                weaponId = "test.weapon",
                damageType = DamageType.Physical,
                deliveryType = DamageDeliveryType.Raycast,
                hitPoint = Vector3.zero,
                hitNormal = Vector3.up
            };
        }

        private static void DestroyRuntimeFeedbackRoot()
        {
            Transform root = PlayerWeaponController.GetOrCreateRuntimeFeedbackRoot();
            if (root != null)
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }
    }
}
