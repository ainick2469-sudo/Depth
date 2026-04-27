using System.Collections.Generic;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS12EnemyLifeTests
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
        public void ArchetypeDefaults_StartInDistinctAmbientStatesAndPreserveBruteFloorBand()
        {
            using EnemyFixture slime = CreateEnemy(EnemyCatalog.CreateDefinition(EnemyArchetype.Slime), Vector3.zero);
            using EnemyFixture bat = CreateEnemy(EnemyCatalog.CreateDefinition(EnemyArchetype.Bat), Vector3.zero);
            using EnemyFixture goblin = CreateEnemy(EnemyCatalog.CreateDefinition(EnemyArchetype.GoblinGrunt), Vector3.zero);
            using EnemyFixture brute = CreateEnemy(EnemyCatalog.CreateDefinition(EnemyArchetype.GoblinBrute), Vector3.zero);

            Assert.AreEqual(SimpleMeleeEnemyState.Patrol, slime.Controller.State);
            Assert.AreEqual(SimpleMeleeEnemyState.Patrol, bat.Controller.State);
            Assert.AreEqual(SimpleMeleeEnemyState.Patrol, goblin.Controller.State);
            Assert.AreEqual(SimpleMeleeEnemyState.Sleep, brute.Controller.State);
            Assert.IsFalse(brute.Definition.IsEligibleForFloor(1));
            Assert.IsTrue(brute.Definition.IsEligibleForFloor(3));
        }

        [Test]
        public void HomeRoomPatrolPoints_AreInsetInsideRoomBounds()
        {
            using EnemyFixture enemy = CreateEnemy(EnemyCatalog.CreateDefinition(EnemyArchetype.GoblinGrunt), Vector3.zero);
            Bounds room = new Bounds(Vector3.zero, new Vector3(30f, 6f, 30f));

            enemy.Controller.SetHomeRoomForTests(
                "room.test",
                room,
                new List<Vector3>
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(14.5f, 0f, 0f),
                    new Vector3(-5f, 0f, -5f)
                });

            IReadOnlyList<Vector3> patrolPoints = enemy.Controller.GetPatrolPointsForTests();
            Assert.GreaterOrEqual(patrolPoints.Count, 2);
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                Assert.IsTrue(enemy.Controller.IsPointInsideHomeForTests(patrolPoints[i]), $"Point {patrolPoints[i]} should remain inside home room.");
                Assert.Less(Mathf.Abs(patrolPoints[i].x), 15f);
                Assert.Less(Mathf.Abs(patrolPoints[i].z), 15f);
            }
        }

        [Test]
        public void PatrolTick_RemainsInsideAssignedHomeRoom()
        {
            using EnemyFixture enemy = CreateEnemy(EnemyCatalog.CreateDefinition(EnemyArchetype.Slime), Vector3.zero);
            Bounds room = new Bounds(Vector3.zero, new Vector3(24f, 6f, 24f));
            enemy.Controller.SetHomeRoomForTests("room.slime", room, new[] { new Vector3(6f, 0f, 6f), new Vector3(-6f, 0f, -6f) });

            for (int i = 0; i < 80; i++)
            {
                enemy.Controller.TickForTests(i * 0.1f, 0.1f);
                Assert.IsTrue(enemy.Controller.IsPointInsideHomeForTests(enemy.GameObject.transform.position));
            }
        }

        [Test]
        public void StuckRecovery_PicksNewTargetInsteadOfPushingForever()
        {
            EnemyDefinition definition = EnemyCatalog.CreateDefinition(EnemyArchetype.GoblinGrunt);
            definition.moveSpeed = 6f;
            definition.stuckRecoverySeconds = 0.2f;
            using EnemyFixture enemy = CreateEnemy(definition, Vector3.zero);
            GameObject blocker = null;
            try
            {
                Bounds room = new Bounds(Vector3.zero, new Vector3(24f, 6f, 24f));
                enemy.Controller.SetHomeRoomForTests("room.stuck", room, new[] { new Vector3(8f, 0f, 0f), new Vector3(-8f, 0f, 0f) });
                enemy.Controller.ConfigureBehaviorSeedForTests(3);

                blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blocker.name = "Patrol_Stuck_Blocker";
                blocker.transform.position = new Vector3(0.95f, 0.85f, 0f);
                blocker.transform.localScale = new Vector3(0.5f, 3f, 8f);
                Physics.SyncTransforms();

                for (int i = 0; i < 8; i++)
                {
                    enemy.Controller.TickForTests(i * 0.1f, 0.1f);
                }

                Assert.GreaterOrEqual(enemy.Controller.StuckRecoveryCount, 1);
            }
            finally
            {
                if (blocker != null)
                {
                    Object.DestroyImmediate(blocker);
                }
            }
        }

        [Test]
        public void GunfireWithinRadius_InvestigatesInsteadOfWholeFloorChase()
        {
            using EnemyFixture enemy = CreateEnemy(EnemyCatalog.CreateDefinition(EnemyArchetype.GoblinGrunt), new Vector3(20f, 0f, 0f));

            bool heard = enemy.Controller.HandleWeaponFiredForTests(new GameplayEvent
            {
                eventType = GameplayEventType.WeaponFired,
                worldPosition = Vector3.zero,
                radius = 40f
            }, 2f);

            Assert.IsTrue(heard);
            Assert.AreEqual(SimpleMeleeEnemyState.Investigate, enemy.Controller.State);
            Assert.AreEqual(Vector3.zero, enemy.Controller.LastHeardPosition);
        }

        [Test]
        public void GunfireOutsideRadius_DoesNotWakeEnemy()
        {
            using EnemyFixture enemy = CreateEnemy(EnemyCatalog.CreateDefinition(EnemyArchetype.GoblinGrunt), new Vector3(70f, 0f, 0f));

            bool heard = enemy.Controller.HandleWeaponFiredForTests(new GameplayEvent
            {
                eventType = GameplayEventType.WeaponFired,
                worldPosition = Vector3.zero,
                radius = 30f
            }, 2f);

            Assert.IsFalse(heard);
            Assert.AreEqual(SimpleMeleeEnemyState.Patrol, enemy.Controller.State);
        }

        [Test]
        public void DamageAggro_ImmediatelyChasesAndGroupAlerts()
        {
            GameObject player = new GameObject("DamageAggroPlayer");
            using EnemyFixture first = CreateEnemy(EnemyCatalog.CreateDefinition(EnemyArchetype.GoblinGrunt), Vector3.zero);
            using EnemyFixture ally = CreateEnemy(EnemyCatalog.CreateDefinition(EnemyArchetype.GoblinGrunt), new Vector3(8f, 0f, 0f));
            try
            {
                player.transform.position = new Vector3(30f, 0f, 0f);
                player.AddComponent<PlayerHealth>();
                first.Controller.HandleDamagedForTests(
                    CreateDamageInfo(12f, player),
                    new DamageResult { applied = true, damageApplied = 12f },
                    4f);

                Assert.AreEqual(SimpleMeleeEnemyState.Chase, first.Controller.State);
                Assert.AreEqual(SimpleMeleeEnemyState.Chase, ally.Controller.State);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void LineOfSight_DetectsClearViewAndWallBlocksCorners()
        {
            GameObject player = new GameObject("LosPlayer");
            GameObject wall = null;
            using EnemyFixture enemy = CreateEnemy(EnemyCatalog.CreateDefinition(EnemyArchetype.GoblinGrunt), Vector3.zero);
            try
            {
                enemy.GameObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                player.transform.position = new Vector3(0f, 0f, 12f);
                PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();

                Assert.IsTrue(enemy.Controller.CanSeePlayerForTests(playerHealth));

                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "LOS_Blocker";
                wall.transform.position = new Vector3(0f, 0.85f, 6f);
                wall.transform.localScale = new Vector3(6f, 3f, 0.5f);
                Physics.SyncTransforms();

                Assert.IsFalse(enemy.Controller.CanSeePlayerForTests(playerHealth));
            }
            finally
            {
                Object.DestroyImmediate(player);
                if (wall != null)
                {
                    Object.DestroyImmediate(wall);
                }
            }
        }

        [Test]
        public void LostTarget_SearchesLastKnownThenReturnsHome()
        {
            GameObject player = new GameObject("LostTargetPlayer");
            using EnemyFixture enemy = CreateEnemy(EnemyCatalog.CreateDefinition(EnemyArchetype.GoblinGrunt), Vector3.zero);
            try
            {
                Bounds room = new Bounds(Vector3.zero, new Vector3(24f, 6f, 24f));
                enemy.Controller.SetHomeRoomForTests("room.goblin", room, new[] { Vector3.zero });
                player.transform.position = new Vector3(16f, 0f, 0f);
                PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();

                enemy.Controller.Alert(playerHealth, player.transform.position, 1f);
                Object.DestroyImmediate(player);

                enemy.Controller.TickForTests(3f, 0.1f);
                Assert.AreEqual(SimpleMeleeEnemyState.Investigate, enemy.Controller.State);

                enemy.Controller.TickForTests(10f, 0.1f);
                Assert.AreEqual(SimpleMeleeEnemyState.ReturnToRoom, enemy.Controller.State);
            }
            finally
            {
                if (player != null)
                {
                    Object.DestroyImmediate(player);
                }
            }
        }

        [Test]
        public void DeadEnemy_DoesNotHearPatrolInvestigateOrRemainSubscribed()
        {
            GameObject player = new GameObject("DeadEnemyPlayer");
            using EnemyFixture enemy = CreateEnemy(EnemyCatalog.CreateDefinition(EnemyArchetype.GoblinGrunt), Vector3.zero);
            try
            {
                player.AddComponent<PlayerHealth>();
                enemy.Controller.TickForTests(0f, 0f);
                Assert.GreaterOrEqual(GameplayEventBus.SubscriberCount, 1);

                enemy.Health.ApplyDamage(CreateDamageInfo(999f, player));
                bool heard = enemy.Controller.HandleWeaponFiredForTests(new GameplayEvent
                {
                    eventType = GameplayEventType.WeaponFired,
                    worldPosition = Vector3.zero,
                    radius = 60f
                }, 5f);
                enemy.Controller.TickForTests(6f, 0.1f);

                Assert.IsFalse(heard);
                Assert.AreEqual(SimpleMeleeEnemyState.Dead, enemy.Controller.State);
                Assert.AreEqual(0, GameplayEventBus.SubscriberCount);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        private static EnemyFixture CreateEnemy(EnemyDefinition definition, Vector3 position)
        {
            GameObject gameObject = new GameObject(definition.displayName);
            gameObject.transform.position = position;
            CharacterController controller = gameObject.AddComponent<CharacterController>();
            controller.height = Mathf.Max(1.1f, definition.visualScale.y * 2f);
            controller.radius = 0.5f;
            EnemyHealth health = gameObject.AddComponent<EnemyHealth>();
            health.Configure(definition);
            SimpleMeleeEnemyController melee = gameObject.AddComponent<SimpleMeleeEnemyController>();
            melee.Configure(definition);
            melee.ConfigureHomeRoom("room.fixture", new Bounds(position, new Vector3(28f, 6f, 28f)), new[] { position });
            return new EnemyFixture(gameObject, health, melee, definition);
        }

        private static DamageInfo CreateDamageInfo(float amount, GameObject source)
        {
            return new DamageInfo
            {
                amount = amount,
                source = source,
                hitPoint = source != null ? source.transform.position : Vector3.zero,
                damageType = DamageType.Physical,
                deliveryType = DamageDeliveryType.Raycast,
                tags = new[] { GameplayTag.Projectile, GameplayTag.OnHit }
            };
        }

        private sealed class EnemyFixture : System.IDisposable
        {
            public EnemyFixture(GameObject gameObject, EnemyHealth health, SimpleMeleeEnemyController controller, EnemyDefinition definition)
            {
                GameObject = gameObject;
                Health = health;
                Controller = controller;
                Definition = definition;
            }

            public GameObject GameObject { get; }
            public EnemyHealth Health { get; }
            public SimpleMeleeEnemyController Controller { get; }
            public EnemyDefinition Definition { get; }

            public void Dispose()
            {
                if (GameObject != null)
                {
                    Object.DestroyImmediate(GameObject);
                }

                if (Definition != null)
                {
                    Object.DestroyImmediate(Definition);
                }
            }
        }
    }
}
