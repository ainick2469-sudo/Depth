using System.Linq;
using System.Reflection;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.UI;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS1302AHudPresentationTests
    {
        [SetUp]
        public void SetUp()
        {
            GameplayEventBus.ClearForTests();
            HudSpriteCatalog.ClearCacheForTests();
        }

        [TearDown]
        public void TearDown()
        {
            GameplayEventBus.ClearForTests();
            HudSpriteCatalog.ClearCacheForTests();
            DestroyRuntimeFeedbackRoot();
        }

        [Test]
        public void GameHudController_CreatesHudZonesWithoutDuplicating()
        {
            GameObject hud = CreateRectObject("Hud");
            try
            {
                GameHudController controller = hud.AddComponent<GameHudController>();
                InvokePrivate(controller, "EnsureHudElements");
                InvokePrivate(controller, "EnsureHudElements");

                Assert.AreEqual(1, CountNamedChildren(hud.transform, HudLayoutConstants.TopCenterZoneName));
                Assert.AreEqual(1, CountNamedChildren(hud.transform, HudLayoutConstants.TopRightZoneName));
                Assert.AreEqual(1, CountNamedChildren(hud.transform, HudLayoutConstants.BottomLeftZoneName));
                Assert.AreEqual(1, CountNamedChildren(hud.transform, HudLayoutConstants.BottomCenterZoneName));
                Assert.AreEqual(1, CountNamedChildren(hud.transform, HudLayoutConstants.BottomRightZoneName));
                Assert.AreEqual(1, CountNamedChildren(hud.transform, HudLayoutConstants.CenterZoneName));
                Assert.AreEqual(1, CountNamedChildren(hud.transform, HudLayoutConstants.OverlayZoneName));
            }
            finally
            {
                Object.DestroyImmediate(hud);
            }
        }

        [Test]
        public void WeaponHud_CreatesOnePanelFrameAndPipContainer()
        {
            GameObject hud = CreateRectObject("Hud");
            GameObject player = CreateWeaponPlayer(out PlayerWeaponController weapon);
            try
            {
                HudLayoutConstants.EnsureZone(hud.transform, HudLayoutConstants.BottomRightZoneName);
                HudLayoutConstants.EnsureZone(hud.transform, HudLayoutConstants.CenterZoneName);
                WeaponHudView view = hud.AddComponent<WeaponHudView>();
                view.SetWeaponForTests(weapon);
                view.RefreshFromWeaponStateForTests();
                view.RefreshFromWeaponStateForTests();

                Assert.IsTrue(view.HasPanelRootForTests);
                Assert.IsTrue(view.HasBackgroundFrameForTests);
                Assert.IsTrue(view.HasAmmoPipContainerForTests);
                Assert.AreEqual(1, CountNamedChildren(hud.transform, "WeaponPanelRoot"));
                Assert.AreEqual(1, CountNamedChildren(hud.transform, "BackgroundFrameImage"));
                Assert.AreEqual(1, CountNamedChildren(hud.transform, "AmmoPipContainer"));
                Assert.AreEqual(6, view.AmmoPipCountForTests);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void WeaponHud_AmmoPipsTrackLoadedMagazineShotsAndReload()
        {
            GameObject hud = CreateRectObject("Hud");
            GameObject player = CreateWeaponPlayer(out PlayerWeaponController weapon);
            try
            {
                WeaponHudView view = hud.AddComponent<WeaponHudView>();
                weapon.HandleWeaponInputFrame(0f, false, false, false);
                view.SetWeaponForTests(weapon);
                int initialReserve = weapon.ReserveAmmo;

                Assert.AreEqual($"6 / {initialReserve}", view.AmmoTextForTests);
                Assert.AreEqual(6, view.FilledAmmoPipCountForTests);

                Assert.IsTrue(weapon.HandleWeaponInputFrame(0f, true, true, false).fired);
                view.RefreshFromWeaponStateForTests();

                Assert.AreEqual($"5 / {initialReserve}", view.AmmoTextForTests);
                Assert.AreEqual(5, view.FilledAmmoPipCountForTests);

                Assert.IsTrue(weapon.HandleWeaponInputFrame(0.5f, false, false, true).reloadStarted);
                view.RefreshFromWeaponStateForTests();
                Assert.IsTrue(weapon.TickReloadCompletion(2.0f));
                view.RefreshFromWeaponStateForTests();

                Assert.AreEqual($"6 / {initialReserve - 1}", view.AmmoTextForTests);
                Assert.AreEqual(6, view.FilledAmmoPipCountForTests);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void WeaponHud_DryFireDoesNotCreateNegativePips()
        {
            GameObject hud = CreateRectObject("Hud");
            GameObject player = CreateWeaponPlayer(out PlayerWeaponController weapon);
            try
            {
                WeaponHudView view = hud.AddComponent<WeaponHudView>();
                weapon.HandleWeaponInputFrame(0f, false, false, false);
                view.SetWeaponForTests(weapon);
                int initialReserve = weapon.ReserveAmmo;
                for (int i = 0; i < 6; i++)
                {
                    Assert.IsTrue(weapon.HandleWeaponInputFrame(i * 0.5f, true, i == 0, false).fired);
                }

                weapon.HandleWeaponInputFrame(3.1f, true, true, false);
                view.RefreshFromWeaponStateForTests();

                Assert.AreEqual($"0 / {initialReserve}", view.AmmoTextForTests);
                Assert.AreEqual(0, view.FilledAmmoPipCountForTests);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void WeaponHud_WeaponSwapUpdatesNameAmmoAndPipCount()
        {
            GameObject hud = CreateRectObject("Hud");
            GameObject player = CreateWeaponPlayer(out PlayerWeaponController weapon);
            try
            {
                WeaponHudView view = hud.AddComponent<WeaponHudView>();
                view.SetWeaponForTests(weapon);

                Assert.IsTrue(weapon.EquipWeapon(WeaponCatalog.FrontierRifleId));
                view.RefreshFromWeaponStateForTests();

                Assert.AreEqual("Frontier Rifle", view.WeaponNameTextForTests);
                Assert.AreEqual("5 / 24", view.AmmoTextForTests);
                Assert.AreEqual(5, view.AmmoPipCountForTests);
                Assert.AreEqual(5, view.FilledAmmoPipCountForTests);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void HudSpriteCatalog_CachesMissingSpritesAndFailsSafely()
        {
            Sprite first = HudSpriteCatalog.TryGetSpriteForTests("Icons/Weapons/definitely_missing_test_icon");
            Sprite second = HudSpriteCatalog.TryGetSpriteForTests("Icons/Weapons/definitely_missing_test_icon");

            Assert.IsNull(first);
            Assert.IsNull(second);
            Assert.AreEqual(1, HudSpriteCatalog.CachedSpriteCountForTests);
            Assert.AreEqual(1, HudSpriteCatalog.MissingSpriteWarningCountForTests);
        }

        [Test]
        public void Minimap_CreatesOneFrameAndSeparatesContentRoot()
        {
            GameObject hud = CreateRectObject("Hud");
            GameObject player = new GameObject("Player");
            try
            {
                HudLayoutConstants.EnsureZone(hud.transform, HudLayoutConstants.TopRightZoneName);
                DungeonMinimapController minimap = hud.AddComponent<DungeonMinimapController>();
                DungeonBuildResult build = CreateMapBuildResult();

                minimap.Configure(build, player.transform);
                minimap.Configure(build, player.transform);

                Assert.AreEqual(1, minimap.FrameCountForTests);
                Assert.IsNotNull(minimap.FrameLayerForTests);
                Assert.IsNotNull(minimap.ContentMaskForTests);
                Assert.IsNotNull(minimap.ContentRootForTests);
                Assert.IsFalse(minimap.ContentRootForTests.IsChildOf(minimap.FrameLayerForTests));
                Assert.AreEqual(2, minimap.RoomElementCount);
                Assert.AreEqual(1, minimap.CorridorElementCount);
                Assert.IsTrue(minimap.ContentRootForTests.GetComponentsInChildren<Text>(true).Any(text => text.name.StartsWith("RoomIcon_")));
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void ResourceHud_ShowsHpFocusStamNotManaAndDoesNotDuplicate()
        {
            GameObject hud = CreateRectObject("Hud");
            GameObject player = new GameObject("Player");
            try
            {
                HudLayoutConstants.EnsureZone(hud.transform, HudLayoutConstants.BottomLeftZoneName);
                player.AddComponent<PlayerHealth>();
                PlayerResourceController resources = player.AddComponent<PlayerResourceController>();
                resources.SetResourceValuesForTests(82f, 61f, 44f);

                HudResourceView view = hud.AddComponent<HudResourceView>();
                InvokePrivate(view, "Update");
                InvokePrivate(view, "Update");

                string allText = string.Join("\n", hud.GetComponentsInChildren<Text>(true).Select(text => text.text));
                StringAssert.Contains("HP", allText);
                StringAssert.Contains("FOCUS", allText);
                StringAssert.Contains("STAM", allText);
                StringAssert.DoesNotContain("MANA", allText);
                Assert.IsTrue(view.HasResourcePanelForTests);
                Assert.AreEqual(3, view.ResourceBarCountForTests);
                Assert.AreEqual(1, CountNamedChildren(hud.transform, "HudFocusBar"));
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        private static GameObject CreateRectObject(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1920f, 1080f);
            return go;
        }

        private static GameObject CreateWeaponPlayer(out PlayerWeaponController weapon)
        {
            GameObject player = new GameObject("WeaponPlayer");
            GameObject cameraObject = new GameObject("WeaponCamera", typeof(Camera));
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = Vector3.zero;
            cameraObject.transform.localRotation = Quaternion.identity;
            weapon = player.AddComponent<PlayerWeaponController>();
            return player;
        }

        private static DungeonBuildResult CreateMapBuildResult()
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = 1,
                playerSpawnNodeId = "room.entry",
                transitDownNodeId = "room.exit"
            };

            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "room.entry",
                roomType = DungeonNodeKind.EntryHub,
                roomRole = DungeonRoomRole.Start,
                zoneType = DungeonZoneType.Entrance,
                bounds = new Bounds(Vector3.zero, new Vector3(20f, 4f, 20f))
            });
            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "room.exit",
                roomType = DungeonNodeKind.TransitDown,
                roomRole = DungeonRoomRole.Exit,
                zoneType = DungeonZoneType.BossWing,
                bounds = new Bounds(new Vector3(38f, 0f, 0f), new Vector3(20f, 4f, 20f))
            });

            string edgeKey = DungeonBuildResult.GetEdgeKey("room.entry", "room.exit");
            build.graphEdges.Add(new DungeonGraphEdgeRecord { edgeKey = edgeKey, a = "room.entry", b = "room.exit" });
            build.corridors.Add(new DungeonCorridorBuildRecord
            {
                edgeKey = edgeKey,
                fromNodeId = "room.entry",
                toNodeId = "room.exit",
                bounds = new Bounds(new Vector3(19f, 0f, 0f), new Vector3(18f, 2f, 7f))
            });
            return build;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"{target.GetType().Name}.{methodName} should exist.");
            method.Invoke(target, null);
        }

        private static int CountNamedChildren(Transform root, string objectName)
        {
            if (root == null)
            {
                return 0;
            }

            int count = root.name == objectName ? 1 : 0;
            for (int i = 0; i < root.childCount; i++)
            {
                count += CountNamedChildren(root.GetChild(i), objectName);
            }

            return count;
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
