using System;
using System.Collections.Generic;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.Progression;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FrontierDepths.Tests.EditMode
{
    public sealed class GateVS123CleanTownDungeonIdentityTests
    {
        [Test]
        public void BountyTracker_AllowsThreeActiveAndRewardsOnce()
        {
            ProfileState profile = new ProfileState();
            profile.Normalize();

            Assert.IsTrue(BountyObjectiveTracker.MarkAccepted(profile, "bounty.lantern_eater_slime", out _));
            Assert.IsTrue(BountyObjectiveTracker.MarkAccepted(profile, "bounty.redfang_goblin_scout", out _));
            Assert.IsTrue(BountyObjectiveTracker.MarkAccepted(profile, "bounty.hollow_brute", out _));
            Assert.AreEqual(3, BountyObjectiveTracker.CountActive(profile));
            Assert.IsFalse(BountyObjectiveTracker.CanAccept(profile, "bounty.lantern_eater_slime", out _));

            Assert.IsTrue(BountyObjectiveTracker.MarkKilled(profile, "bounty.lantern_eater_slime"));
            Assert.IsTrue(BountyObjectiveTracker.TryTurnIn(profile, "bounty.lantern_eater_slime", out BountyDefinition definition, out _));
            Assert.AreEqual("Lantern-Eater Slime", definition.targetName);
            Assert.IsFalse(BountyObjectiveTracker.TryTurnIn(profile, "bounty.lantern_eater_slime", out _, out _));
        }

        [Test]
        public void RoomPurposeCatalog_DefinesDistinctPlayerFacingPurposes()
        {
            Assert.GreaterOrEqual(RoomPurposeCatalog.All.Length, 11);

            RoomPurposeDefinition green = RoomPurposeCatalog.Get("green_cache");
            RoomPurposeDefinition purple = RoomPurposeCatalog.Get("purple_shrine");
            RoomPurposeDefinition red = RoomPurposeCatalog.Get("red_elite");
            RoomPurposeDefinition teal = RoomPurposeCatalog.Get("teal_scout");

            Assert.NotNull(green);
            Assert.NotNull(purple);
            Assert.NotNull(red);
            Assert.NotNull(teal);
            Assert.AreNotEqual(green.effect, purple.effect);
            Assert.Greater(green.heal + green.ammo, 0f);
            Assert.Greater(purple.healthRisk, 0f);
            Assert.AreEqual(RoomPurposeEffect.Elite, red.effect);
            Assert.AreEqual(RoomPurposeEffect.Scout, teal.effect);

            for (int i = 0; i < RoomPurposeCatalog.All.Length; i++)
            {
                RoomPurposeDefinition purpose = RoomPurposeCatalog.All[i];
                Assert.IsFalse(string.IsNullOrWhiteSpace(purpose.displayName), purpose.purposeId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(purpose.prompt), purpose.purposeId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(purpose.resultText), purpose.purposeId);
                Assert.IsFalse(string.IsNullOrWhiteSpace(purpose.minimapIcon), purpose.purposeId);
            }
        }

        [Test]
        public void EnemyCatalog_AddsDataDrivenDepthGatedDiversity()
        {
            List<EnemyDefinition> floorOne = EnemyCatalog.CreateDefinitionsForFloor(1);
            List<EnemyDefinition> floorTwelve = EnemyCatalog.CreateDefinitionsForFloor(12);

            Assert.GreaterOrEqual(Enum.GetValues(typeof(EnemyArchetype)).Length, 16);
            Assert.IsFalse(floorOne.Exists(definition => definition.archetype == EnemyArchetype.GoblinBrute));
            Assert.IsFalse(floorOne.Exists(definition => definition.archetype == EnemyArchetype.IronOgre));
            Assert.IsTrue(floorTwelve.Exists(definition => definition.archetype == EnemyArchetype.IronOgre));
            Assert.IsTrue(floorTwelve.Exists(definition => definition.attackFamily == EnemyAttackFamily.CasterSupport));
            Assert.IsTrue(floorTwelve.Exists(definition => definition.attackFamily == EnemyAttackFamily.RangedSpit));

            for (int i = 0; i < floorTwelve.Count; i++)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(floorTwelve[i].displayName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(floorTwelve[i].visualProfileId));
            }
        }

        [Test]
        public void TownRuntimeKiosks_DoNotCreateDungeonGateService()
        {
            GameObject town = new GameObject("Town");
            try
            {
                TownRuntimeKioskBuilder.EnsureRuntimeKiosks(town.transform);
                Transform root = town.transform.Find("RuntimeTownKiosks");

                Assert.NotNull(root);
                Assert.IsFalse(HasDirectChild(root, "Kiosk_Dungeon Gate"));
                Assert.IsTrue(HasDirectChild(root, "Kiosk_Blacksmith"));
                Assert.IsTrue(HasDirectChild(root, "Kiosk_Quartermaster"));
                Assert.IsTrue(HasDirectChild(root, "Kiosk_Saloon / Inn"));
                Assert.IsTrue(HasDirectChild(root, "Kiosk_Bounty Board"));
            }
            finally
            {
                Object.DestroyImmediate(town);
            }
        }

        [Test]
        public void InputBindingDefaults_PreserveCriticalOldInputShortcuts()
        {
            Assert.AreEqual(KeyCode.W.ToString(), InputBindingService.GetDefaultRecord(GameplayInputAction.MoveForward).primary);
            Assert.AreEqual(KeyCode.Mouse0.ToString(), InputBindingService.GetDefaultRecord(GameplayInputAction.Fire).primary);
            Assert.AreEqual(KeyCode.V.ToString(), InputBindingService.GetDefaultRecord(GameplayInputAction.PistolWhip).primary);
            Assert.AreEqual(KeyCode.Mouse2.ToString(), InputBindingService.GetDefaultRecord(GameplayInputAction.PistolWhip).secondary);
            Assert.AreEqual(KeyCode.G.ToString(), InputBindingService.GetDefaultRecord(GameplayInputAction.RunInfo).primary);
            Assert.AreEqual(KeyCode.Escape.ToString(), InputBindingService.GetDefaultRecord(GameplayInputAction.Pause).primary);
        }

        [Test]
        public void PickupDropLanding_FindsGroundBelowDrop()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                floor.transform.position = Vector3.zero;
                floor.transform.localScale = new Vector3(20f, 1f, 20f);

                Assert.IsTrue(PickupDropLandingController.TryFindGroundedPosition(new Vector3(0f, 5f, 0f), out Vector3 grounded));
                Assert.That(grounded.y, Is.InRange(0.45f, 0.55f));
            }
            finally
            {
                Object.DestroyImmediate(floor);
            }
        }

        private static bool HasDirectChild(Transform root, string childName)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                if (root.GetChild(i).name == childName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
