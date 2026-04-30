using System.Linq;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.Progression;
using FrontierDepths.UI;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS1305CReadabilityCleanupTests
    {
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(GameObject.Find("RuntimeCombatFeedbackService"));
            Object.DestroyImmediate(GameObject.Find("Main Camera"));
        }

        [Test]
        public void RoomPurposeCatalog_CurrentPurposesDoNotGrantBasicAmmo()
        {
            foreach (RoomPurposeDefinition purpose in RoomPurposeCatalog.All)
            {
                Assert.AreEqual(0, purpose.ammo, purpose.purposeId);
                StringAssert.DoesNotContain("ammo", purpose.resultText.ToLowerInvariant(), purpose.purposeId);
            }
        }

        [Test]
        public void RoomPurposeInteractable_IgnoresDormantAmmoAndDoesNotReportAmmo()
        {
            GameObject player = new GameObject("Player", typeof(PlayerInteractor));
            GameObject purposeObject = new GameObject("Purpose");
            try
            {
                RoomPurposeInteractable purpose = purposeObject.AddComponent<RoomPurposeInteractable>();
                purpose.Configure("claim.test", "Test Cache", "Open", 0, 99, 0f, 0f, "Checked.", RoomPurposeEffect.Cache);

                purpose.Interact(player.GetComponent<PlayerInteractor>());

                StringAssert.DoesNotContain("ammo", purpose.LastResultMessage.ToLowerInvariant());
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(purposeObject);
            }
        }

        [Test]
        public void RewardChoices_DoNotExposeDormantBasicAmmoUpgrades()
        {
            RunState run = new RunState();
            run.Normalize();

            for (int seed = 0; seed < 40; seed++)
            {
                foreach (RunUpgradeDefinition choice in RunUpgradeCatalog.CreateRewardChoices(run, 4, seed))
                {
                    Assert.AreNotEqual(RunUpgradeEffectKind.AmmoPickupPercent, choice.effectKind, choice.displayName);
                    Assert.AreNotEqual(RunUpgradeEffectKind.ReserveAmmoCapacityFlat, choice.effectKind, choice.displayName);
                    Assert.AreNotEqual(RunUpgradeCategory.Ammo, choice.category, choice.displayName);
                }
            }
        }

        [Test]
        public void WeaponHud_ChamberPipsStayInsideCylinderRoot()
        {
            GameObject hud = new GameObject("Hud", typeof(RectTransform));
            GameObject player = CreateWeaponPlayer(out PlayerWeaponController weapon);
            try
            {
                WeaponHudView view = hud.AddComponent<WeaponHudView>();
                view.SetWeaponForTests(weapon);

                Assert.AreEqual("6 / 6", view.AmmoTextForTests);
                Assert.AreEqual("Revolver", view.WeaponNameTextForTests);
                Assert.AreEqual("Gunslinger Sidearm", view.WeaponSubtitleTextForTests);
                Assert.AreEqual(6, view.ChamberCountForTests);
                Assert.IsTrue(view.AreChambersParentedToRootForTests);
                Assert.IsTrue(view.AreChambersInsideRootForTests);
                Assert.IsFalse(view.HasOldAmmoPipStripForTests);
                Assert.IsFalse(view.HasLegacyWeaponIconBlockForTests);
                CollectionAssert.AreEquivalent(
                    new[]
                    {
                        new Vector2(0f, 14f),
                        new Vector2(13f, 7f),
                        new Vector2(13f, -7f),
                        new Vector2(0f, -14f),
                        new Vector2(-13f, -7f),
                        new Vector2(-13f, 7f)
                    },
                    view.ChamberLocalPositionsForTests);
                Assert.IsTrue(view.ChamberLocalPositionsForTests.All(position => position.magnitude < 18f));
                Assert.IsTrue(view.IsWeaponPanelInsideSafeAreaForTests);
                Assert.IsTrue(view.IsWeaponTextInsidePanelForTests);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void CombatFeedback_DebugAndDirectMarkersUseScreenSpaceCanvas()
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1.5f, -8f);
            cameraObject.transform.rotation = Quaternion.identity;

            CombatFeedbackService service = CombatFeedbackService.GetOrCreate();
            Assert.IsTrue(CombatFeedbackService.SpawnDebugDamageNumberAtCrosshair());
            CombatFeedbackService.ShowDamageNumber(Vector3.zero, 17f, Color.white, false, "HIT");
            InvokeLateUpdate(service);

            Assert.IsTrue(service.HasScreenSpaceCanvasForTests);
            Assert.Greater(service.ActiveMarkerCountForTests, 0);
            Assert.AreEqual("visible", service.LastSpawnResultForTests);
            Assert.Greater(service.LastScreenPositionForTests.z, 0f);
        }

        [Test]
        public void WeaponModelView_FallbackMaterialColorsAreReadableAndNonWhite()
        {
            GameObject root = new GameObject("WeaponBlockout");
            try
            {
                WeaponModelView view = root.AddComponent<WeaponModelView>();
                view.Configure(root.transform, WeaponCatalog.FrontierRevolverId);

                if (!view.ModelLoadedForTests)
                {
                    Assert.IsTrue(view.IsGrayboxFallbackActiveForTests);
                    return;
                }

                Assert.IsTrue(view.PoseAppliedForTests);
                Assert.IsTrue(view.FallbackMaterialsAppliedForTests);
                Assert.IsTrue(view.FallbackMaterialColorsForTests.Any(color => color.r < 0.8f && color.g < 0.8f && color.b < 0.8f));
                Assert.IsFalse(view.FallbackMaterialColorsForTests.Any(color => color.r > 0.9f && color.g > 0.9f && color.b > 0.9f));
                Assert.IsFalse(view.FallbackMaterialColorsForTests.Any(IsNearBlack));
                Assert.Greater(AverageBrightness(view.FallbackMaterialColorsForTests), 0.42f);
                Assert.IsNotEmpty(view.MaterialDebugLinesForTests);
                Assert.IsTrue(view.MaterialDebugLinesForTests.Any(line => line.Contains("BodyGunmetal") || line.Contains("Steel")));
                if (view.FallbackMaterialColorsForTests.Length > 1)
                {
                    int distinctReadableColors = view.FallbackMaterialColorsForTests
                        .Select(color => $"{Mathf.RoundToInt(color.r * 100f)}:{Mathf.RoundToInt(color.g * 100f)}:{Mathf.RoundToInt(color.b * 100f)}")
                        .Distinct()
                        .Count();
                    Assert.GreaterOrEqual(distinctReadableColors, 2);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static bool IsNearBlack(Color color)
        {
            return color.r < 0.08f && color.g < 0.08f && color.b < 0.08f;
        }

        private static float AverageBrightness(Color[] colors)
        {
            if (colors == null || colors.Length == 0)
            {
                return 0f;
            }

            return colors.Average(color => (color.r + color.g + color.b) / 3f);
        }

        [Test]
        public void WeaponModelView_RuntimeMaterialsDoNotMutatePrefabSharedMaterials()
        {
            GameObject prefab = Resources.Load<GameObject>("Weapons/FrontierRevolver_Model");
            if (prefab == null)
            {
                Assert.Pass("Revolver prefab is optional in missing-LFS environments.");
                return;
            }

            Renderer[] prefabRenderers = prefab.GetComponentsInChildren<Renderer>(true);
            Material[][] originalMaterials = prefabRenderers
                .Select(renderer => renderer.sharedMaterials != null ? renderer.sharedMaterials.ToArray() : System.Array.Empty<Material>())
                .ToArray();
            Color[][] originalColors = originalMaterials
                .Select(materials => materials.Select(material => material != null ? material.color : Color.clear).ToArray())
                .ToArray();

            GameObject root = new GameObject("WeaponBlockout");
            try
            {
                WeaponModelView view = root.AddComponent<WeaponModelView>();
                view.Configure(root.transform, WeaponCatalog.FrontierRevolverId);

                for (int rendererIndex = 0; rendererIndex < prefabRenderers.Length; rendererIndex++)
                {
                    Material[] currentMaterials = prefabRenderers[rendererIndex].sharedMaterials;
                    Assert.AreEqual(originalMaterials[rendererIndex].Length, currentMaterials.Length);
                    for (int slot = 0; slot < currentMaterials.Length; slot++)
                    {
                        Assert.AreSame(originalMaterials[rendererIndex][slot], currentMaterials[slot]);
                        Color currentColor = currentMaterials[slot] != null ? currentMaterials[slot].color : Color.clear;
                        Assert.AreEqual(originalColors[rendererIndex][slot], currentColor);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MinimapHierarchy_KeepsVisibleContentUnderCircularMask()
        {
            GameObject hud = new GameObject("Hud", typeof(RectTransform));
            GameObject player = new GameObject("Player");
            try
            {
                DungeonMinimapController minimap = hud.AddComponent<DungeonMinimapController>();
                minimap.Configure(CreateMapBuildResult(), player.transform);

                Assert.IsTrue(minimap.RootBackgroundHiddenForTests);
                Assert.IsTrue(minimap.HasCircularMaskForTests);
                Assert.IsTrue(minimap.CircularBackgroundUnderMaskForTests);
                Assert.IsTrue(minimap.ContentRootForTests.IsChildOf(minimap.ContentMaskForTests));
                Assert.IsTrue(minimap.PlayerMarkerUnderMaskForTests);
                Assert.IsTrue(minimap.FrameOverlaysMaskForTests);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        private static GameObject CreateWeaponPlayer(out PlayerWeaponController weapon)
        {
            GameObject player = new GameObject("WeaponPlayer");
            GameObject cameraObject = new GameObject("WeaponCamera", typeof(Camera));
            cameraObject.transform.SetParent(player.transform, false);
            weapon = player.AddComponent<PlayerWeaponController>();
            return player;
        }

        private static DungeonBuildResult CreateMapBuildResult()
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = 1,
                playerSpawnNodeId = "room.entry"
            };
            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "room.entry",
                roomType = DungeonNodeKind.EntryHub,
                bounds = new Bounds(Vector3.zero, new Vector3(20f, 4f, 20f))
            });
            return build;
        }

        private static void InvokeLateUpdate(CombatFeedbackService service)
        {
            typeof(CombatFeedbackService)
                .GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(service, null);
        }
    }
}
