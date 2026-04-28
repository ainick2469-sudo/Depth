using System.Linq;
using FrontierDepths.Core;
using FrontierDepths.Progression;
using FrontierDepths.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS1304BReadabilityTests
    {
        private ProfileService profileService;
        private ClassExperienceRuntime experienceRuntime;

        [SetUp]
        public void SetUp()
        {
            GameplayEventBus.ClearForTests();
            profileService = new ProfileService(new SaveService());
            profileService.ResetProgress();
            experienceRuntime = new ClassExperienceRuntime(profileService);
        }

        [TearDown]
        public void TearDown()
        {
            experienceRuntime?.Unsubscribe();
            profileService?.ResetProgress();
            GameplayEventBus.ClearForTests();
        }

        [Test]
        public void EnemyKilled_GrantsClassXpThroughProfileService()
        {
            GameObject enemyObject = new GameObject("Enemy");
            try
            {
                GameplayEventBus.Publish(new GameplayEvent
                {
                    eventType = GameplayEventType.EnemyKilled,
                    targetObject = enemyObject,
                    amount = 1f,
                    tags = new[] { "Slime", "enemy.slime" }
                });

                Assert.AreEqual(10, profileService.Current.classXp);
            }
            finally
            {
                Object.DestroyImmediate(enemyObject);
            }
        }

        [Test]
        public void ClassXpChangedEvent_FiresOncePerGrant()
        {
            int calls = 0;
            int delta = 0;
            profileService.ClassExperienceChanged += (_, amount, reason) =>
            {
                calls++;
                delta = amount;
                Assert.AreEqual("Test", reason);
            };

            profileService.AddClassXp(25, "Test");

            Assert.AreEqual(1, calls);
            Assert.AreEqual(25, delta);
            Assert.AreEqual(25, profileService.Current.classXp);
        }

        [Test]
        public void HudResourceView_UpdatesXpBarFromProfileClassXp()
        {
            GameObject hud = new GameObject("Hud", typeof(RectTransform));
            try
            {
                HudLayoutConstants.EnsureZone(hud.transform, HudLayoutConstants.BottomLeftZoneName);
                HudResourceView view = hud.AddComponent<HudResourceView>();
                profileService.AddClassXp(75, "Test");
                view.SetProfileForTests(profileService.Current);

                InvokeUpdate(view);

                StringAssert.Contains("Gunslinger XP L1 75/150", view.XpLabelForTests);
                Assert.AreEqual(0.5f, view.XpNormalizedForTests, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(hud);
            }
        }

        [Test]
        public void TownRuntimeKiosks_CreateExpectedReadableLabelsOnce()
        {
            GameObject town = new GameObject("Town");
            try
            {
                Transform root = TownRuntimeKioskBuilder.EnsureRuntimeKiosks(town.transform);
                TownRuntimeKioskBuilder.EnsureRuntimeKioskLabels(root);

                string[] expected = { "Blacksmith", "Quartermaster", "Saloon / Inn", "Bounty Board" };
                foreach (string label in expected)
                {
                    TextMesh[] labels = root.GetComponentsInChildren<TextMesh>(true);
                    Assert.AreEqual(1, labels.Count(text => text.text == label), $"{label} should have one runtime label.");
                }
            }
            finally
            {
                Object.DestroyImmediate(town);
            }
        }

        private static void InvokeUpdate(HudResourceView view)
        {
            typeof(HudResourceView)
                .GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(view, null);
        }
    }
}
