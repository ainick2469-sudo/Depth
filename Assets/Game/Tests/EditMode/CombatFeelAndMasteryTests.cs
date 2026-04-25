using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.Progression.Mastery;
using NUnit.Framework;
using UnityEngine;

namespace FrontierDepths.Tests.EditMode
{
    public class CombatFeelAndMasteryTests
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
        public void GameplayEventBus_PublishesSubscribedEvents()
        {
            int received = 0;
            GameplayEventType receivedType = default;
            GameplayEventBus.Subscribe(evt =>
            {
                received++;
                receivedType = evt.eventType;
            });

            GameplayEventBus.Publish(new GameplayEvent { eventType = GameplayEventType.WeaponFired });

            Assert.AreEqual(1, received);
            Assert.AreEqual(GameplayEventType.WeaponFired, receivedType);
        }

        [Test]
        public void WeaponFired_IncrementsBallisticMastery()
        {
            MasteryProgressService service = CreateListeningService();

            GameplayEventBus.Publish(CreateWeaponEvent(GameplayEventType.WeaponFired));

            Assert.Greater(service.GetProgress("mastery.ballistic").xp, 0f);
            service.StopListening();
        }

        [Test]
        public void StarterRevolverHit_IncrementsRevolverMastery()
        {
            MasteryProgressService service = CreateListeningService();

            GameplayEventBus.Publish(CreateWeaponEvent(GameplayEventType.WeaponHit));

            Assert.Greater(service.GetProgress("mastery.revolver").xp, 0f);
            service.StopListening();
        }

        [Test]
        public void PhysicalDamageDealt_IncrementsPhysicalMastery()
        {
            MasteryProgressService service = CreateListeningService();

            GameplayEvent gameplayEvent = CreateWeaponEvent(GameplayEventType.DamageDealt);
            gameplayEvent.finalAmount = 25f;
            GameplayEventBus.Publish(gameplayEvent);

            Assert.Greater(service.GetProgress("mastery.physical").xp, 0f);
            service.StopListening();
        }

        [Test]
        public void DistanceMovedAccumulator_EmitsOnlyAfterThreshold()
        {
            float accumulated = 0f;

            Assert.IsFalse(DistanceMovedAccumulator.TryAccumulate(ref accumulated, 3f, 10f, out _));
            Assert.IsFalse(DistanceMovedAccumulator.TryAccumulate(ref accumulated, 6f, 10f, out _));
            Assert.IsTrue(DistanceMovedAccumulator.TryAccumulate(ref accumulated, 1.5f, 10f, out float emittedDistance));
            Assert.AreEqual(10.5f, emittedDistance);
            Assert.AreEqual(0f, accumulated);
        }

        [Test]
        public void MasteryProgressService_LevelsAfterThreshold()
        {
            TrackerDefinition tracker = new TrackerDefinition("test.level", "Test Level", "Testing thresholds.");
            tracker.levelThresholds.Add(5f);
            tracker.rules.Add(new TrackerRule(GameplayEventType.WeaponFired, 6f));
            MasteryProgressService service = new MasteryProgressService(new[] { tracker });

            service.HandleEvent(new GameplayEvent { eventType = GameplayEventType.WeaponFired });

            Assert.AreEqual(1, service.GetProgress("test.level").level);
        }

        [Test]
        public void TrackerRules_IgnoreUnrelatedEvents()
        {
            MasteryProgressService service = new MasteryProgressService(MasteryTrackerCatalog.CreateStarterTrackers());

            service.HandleEvent(new GameplayEvent { eventType = GameplayEventType.ChestOpened });

            Assert.AreEqual(0f, service.GetProgress("mastery.ballistic").xp);
        }

        [Test]
        public void TargetDummyFlash_DoesNotMutateSharedMaterial()
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Material shared = new Material(Shader.Find("Standard")) { color = Color.green };
            target.GetComponent<Renderer>().sharedMaterial = shared;

            try
            {
                TargetDummyHealth dummy = target.AddComponent<TargetDummyHealth>();
                dummy.Configure(TargetDummyKind.Standard);
                dummy.ApplyDamage(new DamageInfo
                {
                    amount = 10f,
                    damageType = DamageType.Physical,
                    deliveryType = DamageDeliveryType.Raycast
                });

                Assert.AreEqual(Color.green, shared.color);
            }
            finally
            {
                Object.DestroyImmediate(shared);
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void LookRecoilState_RecoversOverTime()
        {
            LookRecoilState recoil = new LookRecoilState();
            recoil.AddImpulse(1.2f, 0.3f, 0.15f);
            float initialMagnitude = recoil.OffsetDegrees.magnitude;

            recoil.Tick(0.075f);

            Assert.Greater(initialMagnitude, recoil.OffsetDegrees.magnitude);
        }

        [Test]
        public void HitFeedbackClassification_DistinguishesNormalReducedAndKill()
        {
            Assert.AreEqual(
                WeaponHitFeedbackKind.Normal,
                PlayerWeaponController.ClassifyHitFeedback(25f, new DamageResult { applied = true, damageApplied = 25f }));
            Assert.AreEqual(
                WeaponHitFeedbackKind.Reduced,
                PlayerWeaponController.ClassifyHitFeedback(25f, new DamageResult { applied = true, damageApplied = 14f }));
            Assert.AreEqual(
                WeaponHitFeedbackKind.Kill,
                PlayerWeaponController.ClassifyHitFeedback(25f, new DamageResult { applied = true, damageApplied = 25f, killedTarget = true }));
        }

        private static MasteryProgressService CreateListeningService()
        {
            MasteryProgressService service = new MasteryProgressService(MasteryTrackerCatalog.CreateStarterTrackers());
            service.StartListening();
            return service;
        }

        private static GameplayEvent CreateWeaponEvent(GameplayEventType eventType)
        {
            return new GameplayEvent
            {
                eventType = eventType,
                weaponId = MasteryTrackerCatalog.StarterRevolverId,
                weaponArchetype = "Revolver",
                damageType = "Physical",
                deliveryType = "Raycast",
                amount = 25f,
                finalAmount = 25f
            };
        }
    }
}
