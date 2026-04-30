using System.Linq;
using System.Reflection;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.UI;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
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
            Time.timeScale = 1f;
            GameplayEventBus.ClearForTests();
            HudSpriteCatalog.ClearCacheForTests();
            DestroyRuntimeFeedbackRoot();
            Object.DestroyImmediate(GameObject.Find("RuntimeEventSystem"));
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
        public void WeaponHud_CreatesOnePanelFrameAndCylinderChambers()
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
                Assert.IsTrue(view.HasChamberIndicatorForTests);
                Assert.IsFalse(view.HasOldAmmoPipStripForTests);
                Assert.AreEqual(1, CountNamedChildren(hud.transform, "WeaponPanelRoot"));
                Assert.AreEqual(1, CountNamedChildren(hud.transform, "BackgroundFrameImage"));
                Assert.AreEqual(1, CountNamedChildren(hud.transform, "CylinderChamberRoot"));
                Assert.AreEqual(6, view.ChamberCountForTests);
                Assert.AreEqual("Revolver", view.WeaponNameTextForTests);
                Assert.AreEqual("Gunslinger Sidearm", view.WeaponSubtitleTextForTests);
                Assert.IsFalse(view.HasLegacyWeaponIconBlockForTests);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void PauseMenu_CreatesEventSystemSoButtonsCanReceiveClicks()
        {
            Object.DestroyImmediate(GameObject.Find("RuntimeEventSystem"));
            GameObject hud = CreateRectObject("Hud");
            try
            {
                PauseMenuController pause = hud.AddComponent<PauseMenuController>();
                pause.Show(null);

                Assert.IsTrue(pause.IsVisible);
                Assert.IsNotNull(Object.FindAnyObjectByType<EventSystem>());
                Assert.IsTrue(pause.HasEventSystemForTests);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(GameObject.Find("RuntimeEventSystem"));
            }
        }

        [Test]
        public void PauseMenu_ShowFreezesGameplayAndHideRestoresCapture()
        {
            Object.DestroyImmediate(GameObject.Find("RuntimeEventSystem"));
            GameObject hud = CreateRectObject("Hud");
            GameObject player = new GameObject("Player", typeof(CharacterController), typeof(PlayerInteractor), typeof(PlayerResourceController), typeof(FirstPersonController));
            try
            {
                PauseMenuController pause = hud.AddComponent<PauseMenuController>();
                FirstPersonController controller = player.GetComponent<FirstPersonController>();

                pause.Show(controller);

                Assert.IsTrue(pause.IsVisible);
                Assert.IsTrue(controller.IsUiCaptured);
                Assert.IsTrue(controller.IsManualPauseActive);
                Assert.AreEqual(0f, Time.timeScale);
                Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
                Assert.IsTrue(Cursor.visible);

                pause.Hide();

                Assert.IsFalse(pause.IsVisible);
                Assert.IsFalse(controller.IsUiCaptured);
                Assert.AreEqual(1f, Time.timeScale);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(GameObject.Find("RuntimeEventSystem"));
                Time.timeScale = 1f;
            }
        }

        [Test]
        public void WeaponHud_CylinderChambersTrackLoadedMagazineShotsAndReload()
        {
            GameObject hud = CreateRectObject("Hud");
            GameObject player = CreateWeaponPlayer(out PlayerWeaponController weapon);
            try
            {
                WeaponHudView view = hud.AddComponent<WeaponHudView>();
                weapon.HandleWeaponInputFrame(0f, false, false, false);
                view.SetWeaponForTests(weapon);

                Assert.AreEqual("6 / 6", view.AmmoTextForTests);
                Assert.AreEqual(6, view.FilledChamberCountForTests);

                Assert.IsTrue(weapon.HandleWeaponInputFrame(0f, true, true, false).fired);
                view.RefreshFromWeaponStateForTests();

                Assert.AreEqual("5 / 6", view.AmmoTextForTests);
                Assert.AreEqual(5, view.FilledChamberCountForTests);

                Assert.IsTrue(weapon.HandleWeaponInputFrame(0.5f, false, false, true).reloadStarted);
                view.RefreshFromWeaponStateForTests();
                Assert.IsTrue(weapon.TickReloadCompletion(2.0f));
                view.RefreshFromWeaponStateForTests();

                Assert.AreEqual("6 / 6", view.AmmoTextForTests);
                Assert.AreEqual(6, view.FilledChamberCountForTests);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void WeaponHud_DryFireDoesNotCreateNegativeChambers()
        {
            GameObject hud = CreateRectObject("Hud");
            GameObject player = CreateWeaponPlayer(out PlayerWeaponController weapon);
            try
            {
                WeaponHudView view = hud.AddComponent<WeaponHudView>();
                weapon.HandleWeaponInputFrame(0f, false, false, false);
                view.SetWeaponForTests(weapon);
                for (int i = 0; i < 6; i++)
                {
                    Assert.IsTrue(weapon.HandleWeaponInputFrame(i * 0.5f, true, i == 0, false).fired);
                }

                weapon.HandleWeaponInputFrame(3.1f, true, true, false);
                view.RefreshFromWeaponStateForTests();

                Assert.AreEqual("0 / 6", view.AmmoTextForTests);
                Assert.AreEqual(0, view.FilledChamberCountForTests);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void WeaponHud_RemovesOldBulletStripAndUsesRuntimeCylinder()
        {
            GameObject hud = CreateRectObject("Hud");
            GameObject player = CreateWeaponPlayer(out PlayerWeaponController weapon);
            try
            {
                WeaponHudView view = hud.AddComponent<WeaponHudView>();
                view.SetWeaponForTests(weapon);

                Assert.AreEqual(6, view.ChamberCountForTests);
                Assert.IsFalse(view.HasOldAmmoPipStripForTests);
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

                Assert.AreEqual("FRONTIER RIFLE", view.WeaponNameTextForTests);
                Assert.AreEqual("5 / 5", view.AmmoTextForTests);
                Assert.AreEqual(0, view.ChamberCountForTests);
                Assert.AreEqual(0, view.FilledChamberCountForTests);
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
                Assert.IsTrue(minimap.HasCircularMaskForTests);
                Assert.IsTrue(minimap.RootBackgroundHiddenForTests);
                Assert.IsTrue(minimap.CircularBackgroundUnderMaskForTests);
                Assert.IsTrue(minimap.FrameOverlaysMaskForTests);
                Assert.IsFalse(minimap.ContentRootForTests.IsChildOf(minimap.FrameLayerForTests));
                Assert.IsTrue(minimap.ContentRootForTests.IsChildOf(minimap.ContentMaskForTests));
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
        public void Minimap_ZoomClampsAndKeepsPlayerMarkerCentered()
        {
            GameObject hud = CreateRectObject("Hud");
            GameObject player = new GameObject("Player");
            try
            {
                DungeonMinimapController minimap = hud.AddComponent<DungeonMinimapController>();
                DungeonBuildResult build = CreateMapBuildResult();
                minimap.Configure(build, player.transform);

                minimap.SetZoom(10f);
                Assert.AreEqual(2.5f, minimap.MinimapZoom, 0.001f);
                minimap.SetZoom(0.1f);
                Assert.AreEqual(0.75f, minimap.MinimapZoom, 0.001f);

                player.transform.position = new Vector3(12f, 0f, 0f);
                InvokePrivate(minimap, "Update");
                Assert.IsTrue(minimap.IsPlayerCenteredForTests);
                Assert.AreNotEqual(Vector2.zero, minimap.CurrentContentPositionForTests);
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
                Assert.Greater(view.StaminaFillColorForTests.g, view.StaminaFillColorForTests.r);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void WeaponModelView_LoadsRevolverPrefabAndDoesNotDuplicate()
        {
            GameObject root = new GameObject("WeaponBlockout");
            GameObject graybox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                graybox.name = "Frame";
                graybox.transform.SetParent(root.transform, false);
                WeaponModelView view = root.AddComponent<WeaponModelView>();

                view.Configure(root.transform, WeaponCatalog.FrontierRevolverId);
                view.Configure(root.transform, WeaponCatalog.FrontierRevolverId);

                Assert.IsTrue(view.ModelLoadedForTests, "FrontierRevolver_Model prefab should be loadable from Resources.");
                Assert.AreEqual(1, view.InstanceCountForTests);
                Assert.IsTrue(view.PoseAppliedForTests, "Imported revolver should receive the first-person visual pose.");
                Assert.IsTrue(view.FallbackMaterialsAppliedForTests, "Imported revolver should receive readable fallback materials when imported white.");
                Assert.IsFalse(graybox.GetComponent<Renderer>().enabled, "Graybox renderers should hide when the imported model loads.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void WeaponModelView_UsesGrayboxFallbackForNonRevolver()
        {
            GameObject root = new GameObject("WeaponBlockout");
            GameObject graybox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                graybox.name = "Frame";
                graybox.transform.SetParent(root.transform, false);
                WeaponModelView view = root.AddComponent<WeaponModelView>();

                view.Configure(root.transform, WeaponCatalog.FrontierRifleId);

                Assert.IsFalse(view.ModelLoadedForTests);
                Assert.IsTrue(view.IsGrayboxFallbackActiveForTests);
                Assert.IsTrue(graybox.GetComponent<Renderer>().enabled);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ResourceHud_ClassXpUsesSimpleGunslingerCurve()
        {
            Assert.AreEqual(150, HudResourceView.GetXpNeededForLevel(1));
            Assert.AreEqual(200, HudResourceView.GetXpNeededForLevel(2));
            Assert.AreEqual(1, HudResourceView.GetClassLevelForXp(149));
            Assert.AreEqual(2, HudResourceView.GetClassLevelForXp(150));
            Assert.AreEqual(25, HudResourceView.GetXpIntoCurrentLevel(175));
        }

        [Test]
        public void SharedSettingsPanel_BuildsOnceAndKeepsRowsInsidePanel()
        {
            GameObject panel = CreateRectObject("SettingsPanel");
            try
            {
                SharedSettingsPanelController settings = panel.AddComponent<SharedSettingsPanelController>();
                settings.Build("Settings", _ => { }, () => null, () => { });
                settings.Build("Settings", _ => { }, () => null, () => { });

                settings.ShowKeybindings();
                Assert.IsTrue(settings.IsShowingKeybindings);
                Assert.AreEqual(17, settings.BindingRowCountForTests);
                Assert.AreEqual(1, CountNamedChildren(panel.transform, "SharedSettingsRows"));
                Assert.AreEqual(1, CountNamedChildren(panel.transform, "SharedKeybindingRows"));
                Assert.GreaterOrEqual(settings.SettingsGroupHeightForTests, 500f);
            }
            finally
            {
                Object.DestroyImmediate(panel);
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
