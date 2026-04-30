using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FrontierDepths.Core;
using FrontierDepths.Progression;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS141BTownVisualLayoutTests
    {
        private readonly List<GameObject> cleanup = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = cleanup.Count - 1; i >= 0; i--)
            {
                if (cleanup[i] != null)
                {
                    Object.DestroyImmediate(cleanup[i]);
                }
            }

            cleanup.Clear();
            TownServiceVisualResolver.ResetWarningsForTests();
        }

        [Test]
        public void VisualCatalog_DefinesWrapperOnlyResourcesAndExactPrompts()
        {
            Assert.AreEqual(4, TownServiceVisualCatalog.All.Count);

            AssertVisualDefinition("shop.blacksmith", "Blacksmith", "Press E to visit Blacksmith", TownServiceVisualCatalog.BlacksmithVisualPath);
            AssertVisualDefinition("shop.quartermaster", "Quartermaster", "Press E to visit Quartermaster", TownServiceVisualCatalog.QuartermasterVisualPath);
            AssertVisualDefinition("shop.saloon", "Saloon / Inn", "Press E to visit Saloon / Inn", TownServiceVisualCatalog.SaloonInnVisualPath);
            AssertVisualDefinition("shop.bounty_board", "Bounty Board", "Press E to view Bounty Board", TownServiceVisualCatalog.BountyBoardVisualPath);
        }

        [Test]
        public void WrapperPrefabs_ExistUnderTownVisualResources()
        {
            foreach (TownServiceVisualDefinition definition in TownServiceVisualCatalog.All)
            {
                Assert.That(definition.preferredResourcePath, Does.StartWith("TownVisuals/"), definition.displayName);
                Assert.NotNull(Resources.Load<GameObject>(definition.preferredResourcePath), definition.preferredResourcePath);
            }
        }

        [Test]
        public void RuntimeLayout_CreatesOneStationPerServiceAndNoRuntimeDungeonGate()
        {
            Transform root = BuildTownRoot();

            Assert.IsNull(root.Find("Kiosk_Dungeon Gate"), "The scene DungeonGate remains authoritative; runtime kiosks must not duplicate it.");

            TownServiceStation[] stations = root.GetComponentsInChildren<TownServiceStation>(true);
            Assert.AreEqual(4, stations.Length);
            CollectionAssert.AreEquivalent(
                new[] { "Blacksmith", "Quartermaster", "Saloon / Inn", "Bounty Board" },
                stations.Select(station => station.DisplayName).ToArray());
            Assert.AreEqual(1, stations.Count(station => station.DisplayName == "Bounty Board"), "Bounty Board should not be duplicated.");
        }

        [Test]
        public void RuntimeLayout_UsesDeterministicFrontFacingPositionsAndClearFootprints()
        {
            Transform root = BuildTownRoot();

            foreach (TownServiceVisualDefinition definition in TownServiceVisualCatalog.All)
            {
                Transform kiosk = FindDirectChild(root, $"Kiosk_{definition.displayName}");
                Assert.NotNull(kiosk, definition.displayName);
                Assert.AreEqual(definition.layoutPosition, kiosk.localPosition, $"{definition.displayName} should use deterministic layout coordinates.");
                Assert.IsTrue(TownRuntimeKioskBuilder.DoesKioskFrontFaceTownCenterForTests(kiosk), $"{definition.displayName} front should face the town center/path.");
                Assert.IsTrue(TownRuntimeKioskBuilder.IsInteractionPointInFrontForTests(kiosk), $"{definition.displayName} interaction zone should be in front.");
            }

            IReadOnlyList<TownServiceVisualDefinition> definitions = TownServiceVisualCatalog.All;
            for (int i = 0; i < definitions.Count; i++)
            {
                for (int j = i + 1; j < definitions.Count; j++)
                {
                    Assert.IsFalse(
                        TownRuntimeKioskBuilder.DoServiceFootprintsOverlapForTests(definitions[i], definitions[j]),
                        $"{definitions[i].displayName} and {definitions[j].displayName} footprints should not overlap.");
                }
            }
        }

        [Test]
        public void RuntimeLayout_AddsReadableLabelsAndExactPlayerPrompts()
        {
            Transform root = BuildTownRoot();

            foreach (TownServiceVisualDefinition definition in TownServiceVisualCatalog.All)
            {
                Transform kiosk = FindDirectChild(root, $"Kiosk_{definition.displayName}");
                Assert.NotNull(kiosk, definition.displayName);

                TextMesh[] labels = kiosk.GetComponentsInChildren<TextMesh>(true);
                Assert.AreEqual(1, labels.Count(label => label.text == definition.displayName), definition.displayName);
                Assert.NotNull(labels.First(label => label.text == definition.displayName).GetComponent<WorldLabelBillboard>());

                TownServiceStation station = kiosk.GetComponentInChildren<TownServiceStation>(true);
                Assert.NotNull(station);
                Assert.AreEqual(definition.prompt, station.Prompt);
                Assert.AreEqual(definition.displayName, station.DisplayName);
            }
        }

        [Test]
        public void RuntimeLayout_UsesNonBlockingPathDressing()
        {
            Transform root = BuildTownRoot();
            Transform pathRoot = root.Find(TownRuntimeKioskBuilder.PathRootName);

            Assert.NotNull(pathRoot);
            Assert.NotNull(pathRoot.Find("MainRoad_ToDungeonGate"));
            Assert.NotNull(pathRoot.Find("LeftServicePath"));
            Assert.NotNull(pathRoot.Find("RightServicePath"));
            Assert.NotNull(pathRoot.Find("RearServicePath"));
            Assert.AreEqual(0, pathRoot.GetComponentsInChildren<Collider>(true).Length, "Town road dressing should not block player paths or prompts.");
        }

        [Test]
        public void MissingWrapperResource_FallsBackAndWarnsOnlyOnce()
        {
            TownServiceVisualResolver.ResetWarningsForTests();
            LogAssert.Expect(LogType.Warning, new Regex("Town visual resource missing: TownVisuals/__Missing"));

            Assert.IsNull(TownServiceVisualResolver.LoadVisualForTests("TownVisuals/__Missing"));
            Assert.IsNull(TownServiceVisualResolver.LoadVisualForTests("TownVisuals/__Missing"));

            Assert.AreEqual(1, TownServiceVisualResolver.MissingWarningCountForTests);
        }

        [Test]
        public void DungeonGatePrompt_UsesTrainingLabyrinthLanguage()
        {
            GameObject gate = new GameObject("DungeonGate", typeof(DungeonGateInteractable));
            cleanup.Add(gate);

            DungeonGateInteractable interactable = gate.GetComponent<DungeonGateInteractable>();

            Assert.AreEqual("Dungeon Gate", interactable.DisplayName);
            Assert.AreEqual("Press E to enter Training Labyrinth", interactable.Prompt);
        }

        private Transform BuildTownRoot()
        {
            GameObject town = new GameObject("Town");
            cleanup.Add(town);
            Transform root = TownRuntimeKioskBuilder.EnsureRuntimeKiosks(town.transform);
            Assert.NotNull(root);
            return root;
        }

        private static void AssertVisualDefinition(string serviceId, string displayName, string prompt, string resourcePath)
        {
            Assert.IsTrue(TownServiceVisualCatalog.TryGet(serviceId, out TownServiceVisualDefinition definition), serviceId);
            Assert.AreEqual(displayName, definition.displayName);
            Assert.AreEqual(prompt, definition.prompt);
            Assert.AreEqual(resourcePath, definition.preferredResourcePath);
            Assert.IsTrue(definition.usesAssetVisual);
            Assert.Greater(definition.footprintSize.x, 0f);
            Assert.Greater(definition.footprintSize.y, 0f);
            Assert.Greater(definition.interactionOffset.z, 0f);
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
