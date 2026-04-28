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
    public sealed class GateVS1301FocusCleanupTests
    {
        [Test]
        public void DepthSense_DoesNotSpendFocus_WhenNoTargetExists()
        {
            GameObject player = new GameObject("Player");
            GameObject hud = new GameObject("Hud");
            try
            {
                PlayerResourceController resources = player.AddComponent<PlayerResourceController>();
                DungeonMinimapController minimap = hud.AddComponent<DungeonMinimapController>();
                DepthSenseController depthSense = hud.AddComponent<DepthSenseController>();
                DungeonBuildResult build = CreateEntryOnlyBuild();
                minimap.Configure(build, player.transform);

                bool used = InvokeDepthSense(depthSense, build, minimap, resources, player.transform.position);

                Assert.IsFalse(used);
                Assert.AreEqual(100f, resources.CurrentFocus, 0.01f);
                Assert.AreEqual("No clear signal.", resources.StatusMessage);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void DepthSense_UsesFocusAndPreservesMana_WhenTargetExists()
        {
            GameObject player = new GameObject("Player");
            GameObject hud = new GameObject("Hud");
            try
            {
                PlayerResourceController resources = player.AddComponent<PlayerResourceController>();
                DungeonMinimapController minimap = hud.AddComponent<DungeonMinimapController>();
                DepthSenseController depthSense = hud.AddComponent<DepthSenseController>();
                DungeonBuildResult build = CreateBountyBuild();
                minimap.Configure(build, player.transform);

                bool used = InvokeDepthSense(depthSense, build, minimap, resources, player.transform.position);

                Assert.IsTrue(used);
                Assert.AreEqual(75f, resources.CurrentFocus, 0.01f);
                Assert.AreEqual(100f, resources.CurrentMana, 0.01f);
                Assert.IsTrue(minimap.IsRoomDiscovered("room.bounty"));
                StringAssert.Contains("marked", resources.StatusMessage);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void DepthSense_ShowsLowFocusMessage_WithoutSpending()
        {
            GameObject player = new GameObject("Player");
            GameObject hud = new GameObject("Hud");
            try
            {
                PlayerResourceController resources = player.AddComponent<PlayerResourceController>();
                resources.SetResourceValuesForTests(100f, 10f, 100f);
                DungeonMinimapController minimap = hud.AddComponent<DungeonMinimapController>();
                DepthSenseController depthSense = hud.AddComponent<DepthSenseController>();
                DungeonBuildResult build = CreateBountyBuild();
                minimap.Configure(build, player.transform);

                bool used = InvokeDepthSense(depthSense, build, minimap, resources, player.transform.position);

                Assert.IsFalse(used);
                Assert.AreEqual(10f, resources.CurrentFocus, 0.01f);
                Assert.AreEqual("Not enough Focus for Depth Sense.", resources.StatusMessage);
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void HudResourceView_ShowsFocus_NotMana_AndDoesNotDuplicateBars()
        {
            GameObject player = new GameObject("Player");
            GameObject hud = new GameObject("Hud");
            try
            {
                player.AddComponent<PlayerHealth>();
                PlayerResourceController resources = player.AddComponent<PlayerResourceController>();
                resources.SetResourceValuesForTests(64f, 72f, 91f);

                HudResourceView view = hud.AddComponent<HudResourceView>();
                InvokeParameterlessInstanceMethod(view, "Update");
                InvokeParameterlessInstanceMethod(view, "Update");

                Text[] texts = hud.GetComponentsInChildren<Text>(true);
                string[] labels = texts.Select(text => text.text).ToArray();

                Assert.IsTrue(labels.Any(text => text.StartsWith("FOCUS 72/100")));
                Assert.IsFalse(labels.Any(text => text.Contains("MANA")));
                Assert.AreEqual(1, labels.Count(text => text.StartsWith("FOCUS ")));
            }
            finally
            {
                Object.DestroyImmediate(hud);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void ControlHints_UseDepthSensePlayerFacingName()
        {
            string dungeonText = InvokeBuildHintText(true);

            StringAssert.Contains("Depth Sense", dungeonText);
            StringAssert.DoesNotContain("Mana Sense", dungeonText);
        }

        [Test]
        public void DashFailure_FromLowStamina_DoesNotConsumeCooldown()
        {
            GameObject player = new GameObject("Player");
            try
            {
                PlayerResourceController resources = player.AddComponent<PlayerResourceController>();
                FirstPersonController controller = player.AddComponent<FirstPersonController>();
                resources.SetResourceValuesForTests(10f, 100f, 100f);

                bool dashed = InvokeDashAttempt(controller, Time.time);

                Assert.IsFalse(dashed);
                Assert.AreEqual(0f, controller.DashCooldownRemaining, 0.001f);
                Assert.AreEqual(10f, resources.CurrentStamina, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        private static DungeonBuildResult CreateEntryOnlyBuild()
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = 1,
                seed = 101,
                playerSpawnNodeId = "room.entry"
            };
            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "room.entry",
                roomType = DungeonNodeKind.EntryHub,
                roomRole = DungeonRoomRole.Start,
                zoneType = DungeonZoneType.Entrance,
                bounds = new Bounds(Vector3.zero, new Vector3(20f, 4f, 20f))
            });
            return build;
        }

        private static DungeonBuildResult CreateBountyBuild()
        {
            DungeonBuildResult build = CreateEntryOnlyBuild();
            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "room.bounty",
                roomType = DungeonNodeKind.Ordinary,
                roomRole = DungeonRoomRole.Bounty,
                zoneType = DungeonZoneType.ForgottenHalls,
                bountyId = "bounty.test",
                bounds = new Bounds(new Vector3(30f, 0f, 0f), new Vector3(20f, 4f, 20f))
            });
            return build;
        }

        private static bool InvokeDepthSense(DepthSenseController controller, DungeonBuildResult build, DungeonMinimapController minimap, PlayerResourceController resources, Vector3 origin)
        {
            MethodInfo method = typeof(DepthSenseController).GetMethod("TryUseDepthSenseForTests", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "DepthSenseController test helper should exist.");
            return (bool)method.Invoke(controller, new object[] { build, minimap, resources, origin });
        }

        private static string InvokeBuildHintText(bool dungeon)
        {
            MethodInfo method = typeof(ControlHintHudView).GetMethod("BuildHintText", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "ControlHintHudView.BuildHintText should exist.");
            return (string)method.Invoke(null, new object[] { dungeon });
        }

        private static bool InvokeDashAttempt(FirstPersonController controller, float currentTime)
        {
            MethodInfo method = typeof(FirstPersonController).GetMethod("TryStartDashForTests", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "FirstPersonController.TryStartDashForTests should exist.");
            return (bool)method.Invoke(controller, new object[] { currentTime });
        }

        private static void InvokeParameterlessInstanceMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"{target.GetType().Name}.{methodName} should exist.");
            method.Invoke(target, null);
        }
    }
}
