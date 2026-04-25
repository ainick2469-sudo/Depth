using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.World;
using NUnit.Framework;
using UnityEngine;

namespace FrontierDepths.Tests.EditMode
{
    public class CombatGate3ATests
    {
        [Test]
        public void WeaponRuntimeState_BlocksFireUntilCooldownExpires()
        {
            WeaponRuntimeState state = new WeaponRuntimeState(6);

            Assert.IsTrue(state.TryFire(0f, 0.35f));
            Assert.AreEqual(5, state.CurrentAmmo);
            Assert.IsFalse(state.TryFire(0.2f, 0.35f));
            Assert.AreEqual(5, state.CurrentAmmo);
            Assert.IsTrue(state.TryFire(0.35f, 0.35f));
            Assert.AreEqual(4, state.CurrentAmmo);
        }

        [Test]
        public void WeaponRuntimeState_ReloadRestoresAmmoAfterManualTimeAdvance()
        {
            WeaponRuntimeState state = new WeaponRuntimeState(6);
            Assert.IsTrue(state.TryFire(0f, 0.35f));

            Assert.IsTrue(state.TryStartReload(0.1f, 1.4f));
            Assert.IsFalse(state.Tick(1.49f));
            Assert.AreEqual(5, state.CurrentAmmo);
            Assert.IsTrue(state.Tick(1.5f));
            Assert.AreEqual(6, state.CurrentAmmo);
            Assert.IsFalse(state.IsReloading);
        }

        [Test]
        public void WeaponRuntimeState_CannotFireWhileReloading()
        {
            WeaponRuntimeState state = new WeaponRuntimeState(6);
            Assert.IsTrue(state.TryFire(0f, 0.35f));
            Assert.IsTrue(state.TryStartReload(0.1f, 1.4f));

            Assert.IsFalse(state.TryFire(0.5f, 0.35f));
            Assert.AreEqual(5, state.CurrentAmmo);
        }

        [Test]
        public void WeaponRuntimeState_FinalRoundQueuesAutoReloadAfterDelay()
        {
            WeaponRuntimeState state = new WeaponRuntimeState(1);

            Assert.IsTrue(state.TryFire(0f, 0.35f));
            Assert.AreEqual(0, state.CurrentAmmo);
            Assert.IsTrue(state.TryQueueAutoReload(0f, 0.12f));
            Assert.IsTrue(state.IsAutoReloadQueued);
            Assert.IsFalse(state.TryStartQueuedAutoReload(0.119f, 1.4f));

            Assert.IsTrue(state.TryStartQueuedAutoReload(0.12f, 1.4f));
            Assert.IsTrue(state.IsReloading);
            Assert.IsFalse(state.IsAutoReloadQueued);
        }

        [Test]
        public void WeaponRuntimeState_ManualReloadClearsPendingAutoReload()
        {
            WeaponRuntimeState state = new WeaponRuntimeState(1);

            Assert.IsTrue(state.TryFire(0f, 0.35f));
            Assert.IsTrue(state.TryQueueAutoReload(0f, 0.12f));
            Assert.IsTrue(state.TryStartReload(0.05f, 1.4f));

            Assert.IsFalse(state.IsAutoReloadQueued);
            Assert.IsTrue(state.IsReloading);
        }

        [Test]
        public void WeaponRuntimeState_DoesNotQueueAutoReloadWhileReloading()
        {
            WeaponRuntimeState state = new WeaponRuntimeState(1);

            Assert.IsTrue(state.TryFire(0f, 0.35f));
            Assert.IsTrue(state.TryStartReload(0.05f, 1.4f));

            Assert.IsFalse(state.TryQueueAutoReload(0.1f, 0.12f));
            Assert.IsFalse(state.IsAutoReloadQueued);
        }

        [Test]
        public void StandardDummy_TakesDamage_DiesOnce_AndResets()
        {
            GameObject target = new GameObject("StandardDummyTest");
            try
            {
                TargetDummyHealth dummy = target.AddComponent<TargetDummyHealth>();
                dummy.Configure(TargetDummyKind.Standard);
                int deathCount = 0;
                dummy.Died += _ => deathCount++;

                DamageResult firstHit = dummy.ApplyDamage(CreateDamageInfo(25f, DamageType.Physical));
                Assert.IsTrue(firstHit.applied);
                Assert.AreEqual(75f, dummy.CurrentHealth);

                DamageResult killingHit = dummy.ApplyDamage(CreateDamageInfo(100f, DamageType.Physical));
                Assert.IsTrue(killingHit.killedTarget);
                Assert.AreEqual(1, deathCount);

                dummy.ApplyDamage(CreateDamageInfo(100f, DamageType.Physical));
                Assert.AreEqual(1, deathCount);
                Assert.IsTrue(dummy.IsDead);

                dummy.AdvanceReset(1.9f);
                Assert.IsTrue(dummy.IsDead);
                dummy.AdvanceReset(0.2f);
                Assert.IsFalse(dummy.IsDead);
                Assert.AreEqual(dummy.MaxHealth, dummy.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ArmoredDummy_ReducesPhysicalDamageInResult()
        {
            GameObject target = new GameObject("ArmoredDummyTest");
            try
            {
                TargetDummyHealth dummy = target.AddComponent<TargetDummyHealth>();
                dummy.Configure(TargetDummyKind.Armored);

                DamageResult result = dummy.ApplyDamage(CreateDamageInfo(100f, DamageType.Physical));

                Assert.IsTrue(result.applied);
                Assert.Greater(result.damageApplied, 0f);
                Assert.Less(result.damageApplied, 100f);
                Assert.AreEqual(dummy.MaxHealth - result.damageApplied, dummy.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void StatusDummy_RecordsDamageTagsAndStatusMetadata()
        {
            GameObject target = new GameObject("StatusDummyTest");
            try
            {
                TargetDummyHealth dummy = target.AddComponent<TargetDummyHealth>();
                dummy.Configure(TargetDummyKind.StatusTest);

                DamageInfo damageInfo = CreateDamageInfo(15f, DamageType.Fire);
                damageInfo.tags = new[] { GameplayTag.Fire, GameplayTag.OnHit };
                damageInfo.statusChance = 0.5f;
                DamageResult result = dummy.ApplyDamage(damageInfo);

                Assert.IsTrue(result.applied);
                StringAssert.Contains("Fire", dummy.LastStatusText);
                StringAssert.Contains("OnHit", dummy.LastStatusText);
                StringAssert.Contains("Status 50%", dummy.LastStatusText);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void CombatTestStation_UsesTargetDummySpawnPointsOnly()
        {
            DungeonBuildResult build = CreateStationBuildResult();
            build.spawnPoints.Add(new DungeonSpawnPointRecord
            {
                nodeId = "ordinary_0",
                category = DungeonSpawnPointCategory.Interactable,
                position = new Vector3(12f, 3.5f, 0f),
                bounds = new Bounds(new Vector3(12f, 3.5f, 0f), new Vector3(2f, 6f, 2f)),
                score = 999f
            });

            var selected = DungeonSceneController.SelectCombatTestStationSpawns(build, 3);

            Assert.AreEqual(3, selected.Count);
            for (int i = 0; i < selected.Count; i++)
            {
                Assert.AreEqual(DungeonSpawnPointCategory.TargetDummy, selected[i].category);
                Assert.GreaterOrEqual(Vector3.Distance(build.playerSpawn, selected[i].position), 12f);
            }
        }

        [Test]
        public void CombatTestStation_DoesNotSelectOnNonFloorOne()
        {
            DungeonBuildResult build = CreateStationBuildResult();
            build.floorIndex = 2;

            var selected = DungeonSceneController.SelectCombatTestStationSpawns(build, 3);

            Assert.IsEmpty(selected);
        }

        [Test]
        public void WeaponRaycast_AgainstDummyCollider_AppliesDamage()
        {
            GameObject dummy = DungeonSceneController.CreateTargetDummy(null, new Vector3(0f, 40f, 10f), TargetDummyKind.Standard);
            try
            {
                Physics.SyncTransforms();
                Ray ray = new Ray(new Vector3(0f, 40f, 0f), Vector3.forward);
                RaycastHit[] hits = Physics.RaycastAll(ray, 20f, PlayerWeaponController.DefaultWeaponRaycastMask, QueryTriggerInteraction.Ignore);

                Assert.IsTrue(PlayerWeaponController.TryResolveShotHit(hits, null, out WeaponShotHit hit, out _));
                Assert.AreEqual(WeaponShotHitKind.Damageable, hit.kind);
                DamageResult result = hit.damageable.ApplyDamage(CreateDamageInfo(25f, DamageType.Physical));

                Assert.IsTrue(result.applied);
                Assert.AreEqual(75f, dummy.GetComponent<TargetDummyHealth>().CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(dummy);
            }
        }

        [Test]
        public void WeaponRaycast_IgnoresPlayerChildColliders()
        {
            GameObject player = new GameObject("PlayerRoot");
            GameObject playerChild = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject dummy = DungeonSceneController.CreateTargetDummy(null, new Vector3(0f, 50f, 12f), TargetDummyKind.Standard);
            try
            {
                playerChild.name = "WeaponHelperCollider";
                playerChild.transform.SetParent(player.transform, true);
                playerChild.transform.position = new Vector3(0f, 50f, 5f);
                playerChild.transform.localScale = Vector3.one * 2f;
                Physics.SyncTransforms();

                Ray ray = new Ray(new Vector3(0f, 50f, 0f), Vector3.forward);
                RaycastHit[] hits = Physics.RaycastAll(ray, 25f, PlayerWeaponController.DefaultWeaponRaycastMask, QueryTriggerInteraction.Ignore);

                Assert.IsTrue(PlayerWeaponController.TryResolveShotHit(hits, player.transform, out WeaponShotHit hit, out int ignored));
                Assert.AreEqual(WeaponShotHitKind.Damageable, hit.kind);
                Assert.GreaterOrEqual(ignored, 1);
                Assert.AreEqual(dummy, hit.hit.collider.gameObject);
            }
            finally
            {
                Object.DestroyImmediate(dummy);
                Object.DestroyImmediate(playerChild);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void WeaponRaycast_StopsOnEnvironmentBeforeDummy()
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject dummy = DungeonSceneController.CreateTargetDummy(null, new Vector3(0f, 60f, 12f), TargetDummyKind.Standard);
            try
            {
                wall.name = "EnvironmentWall";
                wall.transform.position = new Vector3(0f, 60f, 5f);
                wall.transform.localScale = new Vector3(4f, 4f, 1f);
                Physics.SyncTransforms();

                Ray ray = new Ray(new Vector3(0f, 60f, 0f), Vector3.forward);
                RaycastHit[] hits = Physics.RaycastAll(ray, 25f, PlayerWeaponController.DefaultWeaponRaycastMask, QueryTriggerInteraction.Ignore);

                Assert.IsTrue(PlayerWeaponController.TryResolveShotHit(hits, null, out WeaponShotHit hit, out _));
                Assert.AreEqual(WeaponShotHitKind.Environment, hit.kind);
                Assert.AreEqual(wall, hit.hit.collider.gameObject);
                Assert.IsNull(hit.damageable);
                Assert.AreEqual(dummy.GetComponent<TargetDummyHealth>().MaxHealth, dummy.GetComponent<TargetDummyHealth>().CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(dummy);
                Object.DestroyImmediate(wall);
            }
        }

        [Test]
        public void ShotResolution_NoHit_UsesCameraTargetAndMuzzleTracer()
        {
            Ray aimRay = new Ray(new Vector3(0f, 80f, 0f), Vector3.forward);
            Vector3 muzzle = new Vector3(0.4f, 79.8f, 0.7f);

            WeaponShotResolution resolution = PlayerWeaponController.ResolveShot(
                aimRay,
                25f,
                new RaycastHit[0],
                null,
                muzzle,
                PlayerWeaponController.DefaultWeaponRaycastMask);

            Assert.AreEqual(WeaponShotHitKind.None, resolution.kind);
            Assert.AreEqual(muzzle, resolution.muzzleStart);
            Assert.AreEqual(aimRay.origin + aimRay.direction * 25f, resolution.cameraTargetPoint);
            Assert.AreEqual(resolution.cameraTargetPoint, resolution.finalTracerEnd);
        }

        [Test]
        public void ShotResolution_DamageHit_TracerEndsAtCameraRayHitPoint()
        {
            GameObject dummy = DungeonSceneController.CreateTargetDummy(null, new Vector3(0f, 90f, 12f), TargetDummyKind.Standard);
            try
            {
                Physics.SyncTransforms();
                Ray aimRay = new Ray(new Vector3(0f, 90f, 0f), Vector3.forward);
                RaycastHit[] hits = Physics.RaycastAll(aimRay, 25f, PlayerWeaponController.DefaultWeaponRaycastMask, QueryTriggerInteraction.Ignore);
                Vector3 muzzle = new Vector3(0.45f, 89.75f, 0.7f);

                WeaponShotResolution resolution = PlayerWeaponController.ResolveShot(
                    aimRay,
                    25f,
                    hits,
                    null,
                    muzzle,
                    PlayerWeaponController.DefaultWeaponRaycastMask);

                Assert.AreEqual(WeaponShotHitKind.Damageable, resolution.kind);
                Assert.AreEqual(muzzle, resolution.muzzleStart);
                Assert.AreEqual(resolution.hitPoint, resolution.cameraTargetPoint);
                Assert.AreEqual(resolution.hitPoint, resolution.finalTracerEnd);
                Assert.IsFalse(resolution.muzzleObstructed);
            }
            finally
            {
                Object.DestroyImmediate(dummy);
            }
        }

        [Test]
        public void ShotResolution_MuzzleObstruction_UsesEnvironmentEndpoint()
        {
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject dummy = DungeonSceneController.CreateTargetDummy(null, new Vector3(0f, 100f, 14f), TargetDummyKind.Standard);
            try
            {
                blocker.name = "MuzzleObstruction";
                blocker.transform.position = new Vector3(0.9f, 100f, 7f);
                blocker.transform.localScale = new Vector3(0.6f, 3f, 1.4f);
                Physics.SyncTransforms();

                Ray aimRay = new Ray(new Vector3(0f, 100f, 0f), Vector3.forward);
                RaycastHit[] hits = Physics.RaycastAll(aimRay, 30f, PlayerWeaponController.DefaultWeaponRaycastMask, QueryTriggerInteraction.Ignore);
                Vector3 muzzle = new Vector3(1.8f, 100f, 0f);

                WeaponShotResolution resolution = PlayerWeaponController.ResolveShot(
                    aimRay,
                    30f,
                    hits,
                    null,
                    muzzle,
                    PlayerWeaponController.DefaultWeaponRaycastMask);

                Assert.AreEqual(WeaponShotHitKind.Environment, resolution.kind);
                Assert.IsTrue(resolution.muzzleObstructed);
                Assert.AreEqual(blocker.GetComponent<Collider>(), resolution.hitCollider);
                Assert.IsNull(resolution.damageable);
                Assert.AreEqual(resolution.hitPoint, resolution.finalTracerEnd);
                Assert.Less(Vector3.Distance(resolution.finalTracerEnd, muzzle), Vector3.Distance(resolution.cameraTargetPoint, muzzle));
            }
            finally
            {
                Object.DestroyImmediate(dummy);
                Object.DestroyImmediate(blocker);
            }
        }

        [Test]
        public void TryFire_AppliesDamageBeforeWeaponFiredEvent()
        {
            GameObject player = new GameObject("WeaponOrderPlayer");
            GameObject cameraObject = new GameObject("WeaponOrderCamera", typeof(Camera));
            GameObject dummy = DungeonSceneController.CreateTargetDummy(null, new Vector3(0f, 110f, 12f), TargetDummyKind.Standard);
            try
            {
                player.transform.position = new Vector3(0f, 110f, 0f);
                cameraObject.transform.SetParent(player.transform, true);
                cameraObject.transform.position = new Vector3(0f, 110f, 0f);
                cameraObject.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                PlayerWeaponController weapon = player.AddComponent<PlayerWeaponController>();
                Physics.SyncTransforms();

                string order = string.Empty;
                weapon.DamageHitConfirmed += _ => order += "D";
                weapon.WeaponFired += _ => order += "F";

                Assert.IsTrue(weapon.TryFire(0f));
                Assert.AreEqual("DF", order);
                Assert.Less(dummy.GetComponent<TargetDummyHealth>().CurrentHealth, dummy.GetComponent<TargetDummyHealth>().MaxHealth);
            }
            finally
            {
                DestroyRuntimeFeedbackRoot();
                Object.DestroyImmediate(dummy);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void DefaultWeaponRaycastMask_IncludesDefaultAndExcludesIgnoreRaycast()
        {
            int mask = PlayerWeaponController.DefaultWeaponRaycastMask;

            Assert.AreNotEqual(0, mask & (1 << LayerMask.NameToLayer("Default")));
            Assert.AreEqual(0, mask & (1 << LayerMask.NameToLayer("Ignore Raycast")));
        }

        [Test]
        public void CreatedTargetDummy_UsesDefaultLayerAndEnabledNonTriggerCollider()
        {
            GameObject dummy = DungeonSceneController.CreateTargetDummy(null, Vector3.zero, TargetDummyKind.Standard);
            try
            {
                int defaultLayer = LayerMask.NameToLayer("Default");
                Transform[] transforms = dummy.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    Assert.AreEqual(defaultLayer, transforms[i].gameObject.layer);
                }

                Collider[] colliders = dummy.GetComponentsInChildren<Collider>(true);
                Assert.IsNotEmpty(colliders);
                for (int i = 0; i < colliders.Length; i++)
                {
                    Assert.IsTrue(colliders[i].enabled);
                    Assert.IsFalse(colliders[i].isTrigger);
                }
            }
            finally
            {
                Object.DestroyImmediate(dummy);
            }
        }

        [Test]
        public void RuntimeFeedbackPools_AreSceneRootedAndDoNotMoveWithPlayer()
        {
            GameObject player = new GameObject("FeedbackParentPlayer");
            Transform root = null;
            GameObject marker = null;
            try
            {
                Transform impactPool = PlayerWeaponController.GetOrCreateFeedbackPool("WeaponImpactPool");
                root = impactPool.parent;
                marker = new GameObject("WorldFixedImpact");
                marker.transform.SetParent(impactPool, true);
                marker.transform.position = new Vector3(4f, 5f, 6f);
                Vector3 initialPosition = marker.transform.position;

                player.transform.position = new Vector3(10f, 0f, 0f);
                player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                Assert.AreEqual("RuntimeFeedbackRoot", root.name);
                Assert.IsFalse(impactPool.IsChildOf(player.transform));
                Assert.AreEqual(initialPosition, marker.transform.position);
                Assert.AreSame(root, PlayerWeaponController.GetOrCreateRuntimeFeedbackRoot());
            }
            finally
            {
                if (marker != null)
                {
                    Object.DestroyImmediate(marker);
                }

                if (root != null)
                {
                    Object.DestroyImmediate(root.gameObject);
                }

                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void ImpactMarkerColor_UsesMaterialPropertyBlock()
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Material shared = new Material(Shader.Find("Standard")) { color = Color.green };
            try
            {
                Renderer renderer = marker.GetComponent<Renderer>();
                renderer.sharedMaterial = shared;
                MaterialPropertyBlock block = new MaterialPropertyBlock();

                PlayerWeaponController.ApplyImpactMarkerColor(renderer, block, Color.red);
                renderer.GetPropertyBlock(block);

                Assert.AreEqual(Color.green, shared.color);
                Assert.AreEqual(Color.red, block.GetColor("_Color"));
            }
            finally
            {
                Object.DestroyImmediate(shared);
                Object.DestroyImmediate(marker);
            }
        }

        [Test]
        public void TracerMarker_ExpiresAndResetsOnReuse()
        {
            GameObject tracerObject = new GameObject("TracerTest", typeof(LineRenderer));
            try
            {
                LineRenderer line = tracerObject.GetComponent<LineRenderer>();
                line.positionCount = 2;
                PlayerWeaponController.TracerMarker tracer = new PlayerWeaponController.TracerMarker(tracerObject, line);

                tracer.Show(Vector3.zero, Vector3.forward * 10f, 1f, Color.yellow);
                Assert.IsTrue(tracerObject.activeSelf);
                Assert.IsTrue(line.enabled);
                Assert.AreEqual(Vector3.forward * 10f, line.GetPosition(1));

                tracer.Tick(1.01f);
                Assert.IsFalse(tracerObject.activeSelf);
                Assert.IsFalse(line.enabled);
                Assert.AreEqual(Vector3.zero, line.GetPosition(0));
                Assert.AreEqual(Vector3.zero, line.GetPosition(1));

                tracer.Show(Vector3.right, Vector3.right * 3f, 2f, Color.cyan);
                Assert.IsTrue(tracerObject.activeSelf);
                Assert.IsTrue(line.enabled);
                Assert.AreEqual(Vector3.right, line.GetPosition(0));
                Assert.AreEqual(Vector3.right * 3f, line.GetPosition(1));
                Assert.AreEqual(Color.cyan, line.startColor);
            }
            finally
            {
                Object.DestroyImmediate(tracerObject);
            }
        }

        [Test]
        public void TryFire_BlockedByUiCaptureDoesNotConsumeAmmo()
        {
            GameObject player = new GameObject("BlockedWeaponPlayer");
            GameObject cameraObject = new GameObject("BlockedWeaponCamera", typeof(Camera));
            try
            {
                player.AddComponent<CharacterController>();
                player.AddComponent<PlayerInteractor>();
                cameraObject.transform.SetParent(player.transform, false);
                FirstPersonController controller = player.AddComponent<FirstPersonController>();
                PlayerWeaponController weapon = player.AddComponent<PlayerWeaponController>();
                controller.SetUiCaptured(true);
                int initialAmmo = weapon.CurrentAmmo;
                int fireEvents = 0;
                weapon.WeaponFired += _ => fireEvents++;

                Assert.IsFalse(weapon.TryFire(0f));
                Assert.AreEqual(initialAmmo, weapon.CurrentAmmo);
                Assert.AreEqual(0, fireEvents);
            }
            finally
            {
                DestroyRuntimeFeedbackRoot();
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void HeldFire_FiresRepeatedlyOnlyAfterCooldown()
        {
            PlayerWeaponController weapon = CreateWeaponController(out GameObject player);
            try
            {
                int firedCount = 0;
                weapon.WeaponFired += _ => firedCount++;

                WeaponInputFrameResult first = weapon.HandleWeaponInputFrame(0f, true, true, false);
                WeaponInputFrameResult tooSoon = weapon.HandleWeaponInputFrame(0.1f, true, false, false);
                WeaponInputFrameResult afterCooldown = weapon.HandleWeaponInputFrame(0.36f, true, false, false);

                Assert.IsTrue(first.fireAttempted);
                Assert.IsTrue(first.fired);
                Assert.IsTrue(tooSoon.fireAttempted);
                Assert.IsFalse(tooSoon.fired);
                Assert.IsTrue(afterCooldown.fired);
                Assert.AreEqual(2, firedCount);
                Assert.AreEqual(4, weapon.CurrentAmmo);
            }
            finally
            {
                DestroyRuntimeFeedbackRoot();
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void HeldFire_StopsDuringReloadAndResumesAfterReloadCompletes()
        {
            PlayerWeaponController weapon = CreateWeaponController(out GameObject player);
            try
            {
                int firedCount = 0;
                weapon.WeaponFired += _ => firedCount++;

                Assert.IsTrue(weapon.HandleWeaponInputFrame(0f, true, true, false).fired);
                WeaponInputFrameResult reload = weapon.HandleWeaponInputFrame(0.1f, true, false, true);
                WeaponInputFrameResult duringReload = weapon.HandleWeaponInputFrame(0.5f, true, false, false);
                bool reloadFinished = weapon.TickReloadCompletion(1.51f);
                WeaponInputFrameResult afterReload = weapon.HandleWeaponInputFrame(1.51f, true, false, false);

                Assert.IsTrue(reload.reloadRequested);
                Assert.IsTrue(reload.reloadStarted);
                Assert.IsFalse(reload.fired);
                Assert.IsFalse(duringReload.fireAttempted);
                Assert.IsTrue(reloadFinished);
                Assert.IsTrue(afterReload.fired);
                Assert.AreEqual(2, firedCount);
            }
            finally
            {
                DestroyRuntimeFeedbackRoot();
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void HeldFire_ReleasedDuringReloadDoesNotResumeUntilHeldAgain()
        {
            PlayerWeaponController weapon = CreateWeaponController(out GameObject player);
            try
            {
                int firedCount = 0;
                weapon.WeaponFired += _ => firedCount++;

                Assert.IsTrue(weapon.HandleWeaponInputFrame(0f, true, true, false).fired);
                Assert.IsTrue(weapon.HandleWeaponInputFrame(0.1f, false, false, true).reloadStarted);
                Assert.IsTrue(weapon.TickReloadCompletion(1.51f));
                WeaponInputFrameResult releasedFrame = weapon.HandleWeaponInputFrame(1.51f, false, false, false);
                WeaponInputFrameResult heldAgain = weapon.HandleWeaponInputFrame(1.86f, true, true, false);

                Assert.IsFalse(releasedFrame.fireAttempted);
                Assert.IsFalse(releasedFrame.fired);
                Assert.IsTrue(heldAgain.fired);
                Assert.AreEqual(2, firedCount);
            }
            finally
            {
                DestroyRuntimeFeedbackRoot();
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void HeldFire_AutoReloadsAtEmptyAndResumesWhenHeld()
        {
            PlayerWeaponController weapon = CreateWeaponController(out GameObject player);
            try
            {
                int firedCount = 0;
                int reloadStarted = 0;
                int dryFired = 0;
                weapon.WeaponFired += _ => firedCount++;
                weapon.ReloadStarted += _ => reloadStarted++;
                weapon.DryFired += _ => dryFired++;

                for (int i = 0; i < 6; i++)
                {
                    Assert.IsTrue(weapon.HandleWeaponInputFrame(i * 0.36f, true, i == 0, false).fired);
                }

                Assert.AreEqual(0, weapon.CurrentAmmo);
                WeaponInputFrameResult pendingFrame = weapon.HandleWeaponInputFrame(1.85f, true, false, false);
                WeaponInputFrameResult reloadFrame = weapon.HandleWeaponInputFrame(1.93f, true, false, false);
                WeaponInputFrameResult duringReload = weapon.HandleWeaponInputFrame(2.1f, true, false, false);
                Assert.IsTrue(weapon.TickReloadCompletion(3.34f));
                WeaponInputFrameResult afterReload = weapon.HandleWeaponInputFrame(3.34f, true, false, false);

                Assert.IsFalse(pendingFrame.fireAttempted);
                Assert.IsFalse(pendingFrame.reloadStarted);
                Assert.IsTrue(reloadFrame.autoReloadStarted);
                Assert.IsTrue(reloadFrame.reloadStarted);
                Assert.IsFalse(duringReload.fireAttempted);
                Assert.IsTrue(afterReload.fired);
                Assert.AreEqual(7, firedCount);
                Assert.AreEqual(1, reloadStarted);
                Assert.AreEqual(0, dryFired);
            }
            finally
            {
                DestroyRuntimeFeedbackRoot();
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void ManualReloadWhileAutoReloadPendingStartsImmediately()
        {
            PlayerWeaponController weapon = CreateWeaponController(out GameObject player);
            try
            {
                for (int i = 0; i < 6; i++)
                {
                    Assert.IsTrue(weapon.HandleWeaponInputFrame(i * 0.36f, true, i == 0, false).fired);
                }

                WeaponInputFrameResult manualReload = weapon.HandleWeaponInputFrame(1.85f, true, false, true);

                Assert.IsTrue(manualReload.reloadRequested);
                Assert.IsTrue(manualReload.reloadStarted);
                Assert.IsFalse(manualReload.autoReloadStarted);
                Assert.IsFalse(manualReload.fired);
                Assert.IsTrue(weapon.IsReloading);
            }
            finally
            {
                DestroyRuntimeFeedbackRoot();
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void HeldFire_BlockedByUiCaptureDoesNotConsumeAmmoOrFire()
        {
            GameObject player = new GameObject("BlockedHeldWeaponPlayer");
            GameObject cameraObject = new GameObject("BlockedHeldWeaponCamera", typeof(Camera));
            try
            {
                player.AddComponent<CharacterController>();
                player.AddComponent<PlayerInteractor>();
                cameraObject.transform.SetParent(player.transform, false);
                FirstPersonController controller = player.AddComponent<FirstPersonController>();
                PlayerWeaponController weapon = player.AddComponent<PlayerWeaponController>();
                controller.SetUiCaptured(true);
                int initialAmmo = weapon.CurrentAmmo;
                int fireEvents = 0;
                weapon.WeaponFired += _ => fireEvents++;

                WeaponInputFrameResult result = weapon.HandleWeaponInputFrame(0f, true, true, false);

                Assert.IsTrue(result.inputBlocked);
                Assert.IsFalse(result.fireAttempted);
                Assert.IsFalse(result.fired);
                Assert.AreEqual(initialAmmo, weapon.CurrentAmmo);
                Assert.AreEqual(0, fireEvents);
            }
            finally
            {
                DestroyRuntimeFeedbackRoot();
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void CombatTestStation_RejectsBlockedLineOfSightCandidate()
        {
            DungeonBuildResult build = CreateLineOfSightBuildResult();
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                blocker.name = "LineOfSightBlocker";
                blocker.transform.position = new Vector3(5f, 4.1f, 0f);
                blocker.transform.localScale = new Vector3(1f, 4f, 4f);
                Physics.SyncTransforms();

                var selected = DungeonSceneController.SelectCombatTestStationSpawns(build, 3, true);

                Assert.AreEqual(3, selected.Count);
                for (int i = 0; i < selected.Count; i++)
                {
                    Assert.AreNotEqual(new Vector3(10f, 3.5f, 0f), selected[i].position);
                }
            }
            finally
            {
                Object.DestroyImmediate(blocker);
            }
        }

        private static PlayerWeaponController CreateWeaponController(out GameObject player)
        {
            player = new GameObject("TestWeaponPlayer");
            GameObject cameraObject = new GameObject("TestWeaponCamera", typeof(Camera));
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = Vector3.zero;
            cameraObject.transform.localRotation = Quaternion.identity;
            return player.AddComponent<PlayerWeaponController>();
        }

        private static DamageInfo CreateDamageInfo(float amount, DamageType damageType)
        {
            return new DamageInfo
            {
                amount = amount,
                source = null,
                weaponId = "weapon.frontier_revolver",
                hitPoint = Vector3.zero,
                hitNormal = Vector3.up,
                damageType = damageType,
                deliveryType = DamageDeliveryType.Raycast,
                tags = new GameplayTag[0],
                canCrit = false,
                isCritical = false,
                knockbackForce = 0f,
                statusChance = 0f
            };
        }

        private static DungeonBuildResult CreateStationBuildResult()
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = 1,
                playerSpawn = Vector3.zero,
                playerSpawnNodeId = "transit_up"
            };

            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "transit_up",
                roomType = DungeonNodeKind.TransitUp,
                bounds = new Bounds(Vector3.zero, new Vector3(24f, 8f, 24f))
            });

            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "ordinary_0",
                roomType = DungeonNodeKind.Ordinary,
                bounds = new Bounds(new Vector3(24f, 0f, 0f), new Vector3(36f, 8f, 36f))
            });

            for (int i = 0; i < 3; i++)
            {
                Vector3 position = new Vector3(20f + i * 3f, 3.5f, i * 2f);
                build.spawnPoints.Add(new DungeonSpawnPointRecord
                {
                    nodeId = "ordinary_0",
                    category = DungeonSpawnPointCategory.TargetDummy,
                    position = position,
                    bounds = new Bounds(position, new Vector3(2f, 6f, 2f)),
                    score = 100f - i
                });
            }

            return build;
        }

        private static DungeonBuildResult CreateLineOfSightBuildResult()
        {
            DungeonBuildResult build = new DungeonBuildResult
            {
                floorIndex = 1,
                playerSpawn = new Vector3(-20f, 3.5f, -20f),
                playerSpawnNodeId = "ordinary_0"
            };

            build.rooms.Add(new DungeonRoomBuildRecord
            {
                nodeId = "ordinary_0",
                roomType = DungeonNodeKind.Ordinary,
                bounds = new Bounds(Vector3.zero, new Vector3(48f, 8f, 48f))
            });

            Vector3[] positions =
            {
                new Vector3(10f, 3.5f, 0f),
                new Vector3(0f, 3.5f, 10f),
                new Vector3(-10f, 3.5f, 0f),
                new Vector3(0f, 3.5f, -10f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                build.spawnPoints.Add(new DungeonSpawnPointRecord
                {
                    nodeId = "ordinary_0",
                    category = DungeonSpawnPointCategory.TargetDummy,
                    position = positions[i],
                    bounds = new Bounds(positions[i], new Vector3(2f, 6f, 2f)),
                    score = 100f - i
                });
            }

            return build;
        }

        private static void DestroyRuntimeFeedbackRoot()
        {
            GameObject root = GameObject.Find("RuntimeFeedbackRoot");
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
