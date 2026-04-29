using System.IO;
using System.Linq;
using System.Reflection;
using FrontierDepths.Combat;
using FrontierDepths.Progression;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS1305AStabilityTests
    {
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(GameObject.Find("RuntimeCombatFeedbackService"));
            GameObject camera = GameObject.Find("Main Camera");
            if (camera != null)
            {
                Object.DestroyImmediate(camera);
            }
        }

        [Test]
        public void InfiniteBasicAmmo_ReloadRefillsMagazineWithoutConsumingReserve()
        {
            WeaponRuntimeState state = new WeaponRuntimeState(6, 0, 72, 0);

            Assert.IsTrue(state.TryStartReload(0f, 0.1f));
            Assert.IsTrue(state.Tick(0.2f));

            Assert.AreEqual(6, state.CurrentAmmo);
            Assert.AreEqual(0, state.ReserveAmmo);
            Assert.IsTrue(state.InfiniteReserveAmmo);
        }

        [Test]
        public void CurrentDropAndShopTables_DoNotExposeBasicAmmo()
        {
            EnemyDefinition slime = EnemyCatalog.CreateDefinition(EnemyArchetype.Slime);
            try
            {
                slime.goldDropChance = 0f;
                slime.healthDropChance = 0f;
                slime.ammoDropChance = 1f;

                CollectionAssert.DoesNotContain(EncounterDropService.RollDropsForTests(slime, 123), EncounterDropKind.Ammo);

                ShopDefinition quartermaster = TownShopCatalog.GetShop("shop.quartermaster");
                Assert.NotNull(quartermaster);
                Assert.IsFalse(quartermaster.offers.Any(offer => offer.action == ShopOfferAction.RestockAmmo));
                Assert.IsFalse(quartermaster.offers.Any(offer => offer.rewardId == "ammo.reserve"));
            }
            finally
            {
                Object.DestroyImmediate(slime);
            }
        }

        [Test]
        public void CombatFeedback_UsesScreenSpaceCanvasPool()
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1.5f, -8f);
            cameraObject.transform.rotation = Quaternion.identity;

            CombatFeedbackService.ShowDamageNumber(Vector3.zero, 12f, Color.white, false, "HIT");
            CombatFeedbackService service = CombatFeedbackService.GetOrCreate();
            InvokePrivate(service, "LateUpdate");

            Transform canvas = service.transform.Find("ScreenSpaceDamageNumberCanvas");
            Assert.NotNull(canvas);
            Assert.NotNull(canvas.GetComponent<Canvas>());
            Assert.NotNull(canvas.Find("SharedCombatDamageNumberPool"));
            Assert.Greater(service.ActiveMarkerCountForTests, 0);
            Assert.Greater(canvas.GetComponentsInChildren<Text>(true).Length, 0);
        }

        [Test]
        public void WeaponModelView_AppliesRevolverPoseAndDoesNotDuplicate()
        {
            GameObject root = new GameObject("WeaponBlockout");
            GameObject graybox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                graybox.transform.SetParent(root.transform, false);
                WeaponModelView view = root.AddComponent<WeaponModelView>();

                view.Configure(root.transform, WeaponCatalog.FrontierRevolverId);
                view.Configure(root.transform, WeaponCatalog.FrontierRevolverId);

                if (view.ModelLoadedForTests)
                {
                    Assert.IsTrue(view.PoseAppliedForTests);
                    Assert.AreEqual(1, view.InstanceCountForTests);
                }
                else
                {
                    Assert.IsTrue(view.IsGrayboxFallbackActiveForTests);
                    Assert.AreEqual(0, view.InstanceCountForTests);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TownRuntimeKiosks_FaceTownCenterAndDoNotCreateGate()
        {
            GameObject town = new GameObject("Town");
            try
            {
                Transform root = TownRuntimeKioskBuilder.EnsureRuntimeKiosks(town.transform);

                Assert.IsNull(root.Find("Kiosk_Dungeon Gate"));
                string[] ids = { "Blacksmith", "Quartermaster", "Saloon / Inn", "Bounty Board" };
                foreach (string id in ids)
                {
                    Transform kiosk = FindDirectChild(root, $"Kiosk_{id}");
                    Assert.NotNull(kiosk, id);
                    Assert.IsTrue(TownRuntimeKioskBuilder.DoesKioskFrontFaceTownCenterForTests(kiosk), id);
                }
            }
            finally
            {
                Object.DestroyImmediate(town);
            }
        }

        [Test]
        public void ProjectSnapshot_DocsAndExporterExist()
        {
            string root = Directory.GetCurrentDirectory();
            string snapshot = Path.Combine(root, "ProjectSnapshot");

            Assert.IsTrue(File.Exists(Path.Combine(snapshot, "README_FOR_CHATGPT.md")));
            Assert.IsTrue(File.Exists(Path.Combine(snapshot, "PROJECT_MAP.md")));
            Assert.IsTrue(File.Exists(Path.Combine(snapshot, "KNOWN_ISSUES.md")));
            Assert.IsTrue(File.Exists(Path.Combine(root, "Tools", "GenerateProjectSnapshot.ps1")));
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method.Invoke(target, null);
        }

        private static Transform FindDirectChild(Transform root, string childName)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
