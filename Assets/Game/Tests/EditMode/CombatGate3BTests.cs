using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.World;
using NUnit.Framework;
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
                position = new Vector3(36f, 3.5f, 0f),
                score = 10f
            });

            DungeonSpawnPointRecord selected = DungeonSceneController.SelectCombatTestEnemySpawn(
                build,
                new[] { new Vector3(24.5f, 3.5f, 0f) });

            Assert.NotNull(selected);
            Assert.AreEqual("ordinary_far", selected.nodeId);
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

        private static DungeonBuildResult CreateBuildForSpawnSelection()
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = 1,
                playerSpawn = Vector3.zero
            };
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "entry", roomType = DungeonNodeKind.EntryHub });
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "ordinary_near", roomType = DungeonNodeKind.Ordinary });
            build.rooms.Add(new DungeonRoomBuildRecord { nodeId = "ordinary_far", roomType = DungeonNodeKind.Ordinary });
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
