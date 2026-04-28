using FrontierDepths.Combat;
using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class RoomPurposeInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string purposeId = string.Empty;
        [SerializeField] private string displayName = "Cache";
        [SerializeField] private string prompt = "Open Cache";
        [SerializeField] private int goldAmount;
        [SerializeField] private int ammoAmount;
        [SerializeField] private float healAmount;
        [SerializeField] private float healthRiskAmount;
        [SerializeField] private string resultPrefix = string.Empty;
        [SerializeField] private RoomPurposeEffect effect = RoomPurposeEffect.Cache;

        private bool claimed;
        private string lastResultMessage = string.Empty;

        public string PurposeId => purposeId;
        public string DisplayName => displayName;
        public string Prompt => claimed ? "Already claimed" : prompt;
        public string LastResultMessage => lastResultMessage;

        public void Configure(
            string claimId,
            string title,
            string promptText,
            int gold,
            int ammo,
            float heal,
            float healthRisk)
        {
            Configure(claimId, title, promptText, gold, ammo, heal, healthRisk, string.Empty, RoomPurposeEffect.Cache);
        }

        public void Configure(
            string claimId,
            string title,
            string promptText,
            int gold,
            int ammo,
            float heal,
            float healthRisk,
            string resultText,
            RoomPurposeEffect purposeEffect)
        {
            purposeId = claimId ?? string.Empty;
            displayName = string.IsNullOrWhiteSpace(title) ? "Cache" : title;
            prompt = string.IsNullOrWhiteSpace(promptText) ? displayName : promptText;
            goldAmount = Mathf.Max(0, gold);
            ammoAmount = Mathf.Max(0, ammo);
            healAmount = Mathf.Max(0f, heal);
            healthRiskAmount = Mathf.Max(0f, healthRisk);
            resultPrefix = resultText ?? string.Empty;
            effect = purposeEffect;
            claimed = IsClaimed();
        }

        public bool CanInteract(PlayerInteractor interactor, out string reason)
        {
            claimed = IsClaimed();
            if (claimed)
            {
                reason = "Already claimed.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!CanInteract(interactor, out _))
            {
                return;
            }

            GameObject playerObject = interactor != null ? interactor.gameObject : null;
            PlayerHealth playerHealth = playerObject != null ? playerObject.GetComponentInParent<PlayerHealth>() : null;
            PlayerWeaponController weapon = playerObject != null ? playerObject.GetComponentInParent<PlayerWeaponController>() : null;
            PlayerResourceController resources = playerObject != null ? playerObject.GetComponentInParent<PlayerResourceController>() : null;

            float hpLost = ApplyNonLethalRisk(playerHealth);
            float healed = playerHealth != null ? playerHealth.Heal(healAmount) : 0f;
            int ammoAdded = weapon != null ? weapon.TryAddAmmoToReserve(ammoAmount, true) : 0;
            if (goldAmount > 0 && GameBootstrap.Instance != null && GameBootstrap.Instance.ProfileService != null)
            {
                GameBootstrap.Instance.ProfileService.AddGold(goldAmount);
            }

            string sideEffect = ApplyPurposeSideEffect(playerHealth, weapon, resources);
            MarkClaimed();
            claimed = true;
            lastResultMessage = BuildResultMessage(goldAmount, ammoAdded, healed, hpLost, sideEffect);
            if (!string.IsNullOrWhiteSpace(resultPrefix))
            {
                lastResultMessage = $"{resultPrefix} {lastResultMessage}";
            }
            Debug.Log($"{displayName}: {lastResultMessage}");
        }

        private string ApplyPurposeSideEffect(PlayerHealth playerHealth, PlayerWeaponController weapon, PlayerResourceController resources)
        {
            ProfileService profileService = GameBootstrap.Instance != null ? GameBootstrap.Instance.ProfileService : null;
            switch (effect)
            {
                case RoomPurposeEffect.Shrine:
                    AddProfileProgress(profileService, classXp: 18, reputation: 5);
                    float shrineFocus = resources != null ? resources.RestoreFocus(20f) : 0f;
                    return shrineFocus > 0f ? $"+18 class XP, +5 reputation, +{shrineFocus:0} focus" : "+18 class XP, +5 reputation";
                case RoomPurposeEffect.Elite:
                    AddProfileProgress(profileService, classXp: 16, reputation: 12);
                    return "+16 class XP, +12 reputation";
                case RoomPurposeEffect.Ambush:
                    AddProfileProgress(profileService, classXp: 12, reputation: 3);
                    return "+12 class XP, trap marker logged";
                case RoomPurposeEffect.Wild:
                    return ApplyWildOutcome(playerHealth, weapon, profileService);
                case RoomPurposeEffect.Fountain:
                    float fountainFocus = resources != null ? resources.RestoreFocus(35f) : 0f;
                    float fountainStamina = resources != null ? resources.RestoreStamina(35f) : 0f;
                    return $"fountain recovery +{fountainFocus:0} focus, +{fountainStamina:0} stamina";
                case RoomPurposeEffect.Armory:
                    int armoryAmmo = weapon != null ? weapon.TryAddAmmoToReserve(8, true) : 0;
                    return armoryAmmo > 0 ? $"+{armoryAmmo} armory ammo" : "weapon cache checked";
                case RoomPurposeEffect.Sanctuary:
                    AddProfileProgress(profileService, classXp: 8, reputation: 4);
                    return "+8 class XP, sanctuary blessing";
                case RoomPurposeEffect.CursedVault:
                    AddProfileProgress(profileService, classXp: 24, reputation: 10);
                    return "+24 class XP, +10 reputation";
                case RoomPurposeEffect.Scout:
                    return ApplyScoutSurvey(profileService);
                case RoomPurposeEffect.Treasury:
                    return "treasury payout";
                case RoomPurposeEffect.Cache:
                    return "safe supplies recovered";
                default:
                    return string.Empty;
            }
        }

        private string ApplyWildOutcome(PlayerHealth playerHealth, PlayerWeaponController weapon, ProfileService profileService)
        {
            int roll = Mathf.Abs(purposeId.GetHashCode()) % 4;
            switch (roll)
            {
                case 0:
                    profileService?.AddGold(22);
                    return "wild gold surge +22g";
                case 1:
                    int ammo = weapon != null ? weapon.TryAddAmmoToReserve(14, true) : 0;
                    return ammo > 0 ? $"wild ammo surge +{ammo}" : "wild ammo fizzled";
                case 2:
                    float heal = playerHealth != null ? playerHealth.Heal(14f) : 0f;
                    return heal > 0f ? $"wild heal +{heal:0} HP" : "wild heal found no wound";
                default:
                    AddProfileProgress(profileService, classXp: 20, reputation: 6);
                    return "wild insight +20 class XP, +6 reputation";
            }
        }

        private static void AddProfileProgress(ProfileService profileService, int classXp, int reputation)
        {
            if (profileService == null)
            {
                return;
            }

            if (classXp > 0)
            {
                profileService.Current.classXp += classXp;
                while (profileService.Current.classXp >= (profileService.Current.skillPoints + 1) * 100)
                {
                    profileService.Current.skillPoints++;
                }
            }

            ReputationService.AddReputation(profileService.Current, reputation);
            profileService.Save();
        }

        private static string ApplyScoutSurvey(ProfileService profileService)
        {
            FloorState floor = GetCurrentFloor();
            DungeonBuildResult build = FindAnyObjectByType<DungeonSceneController>()?.CurrentBuildResult;
            if (floor != null && build != null)
            {
                DungeonRoomBuildRecord stair = FindStairRoom(build);
                if (stair != null && string.IsNullOrWhiteSpace(floor.knownStairRoomId))
                {
                    MarkRoomDiscovered(floor, stair.nodeId);
                    floor.knownStairRoomId = stair.nodeId;
                    floor.stairDiscovered = true;
                    GameBootstrap.Instance?.RunService?.SaveActiveFloorState();
                    return "Stair room marked.";
                }

                DungeonRoomBuildRecord special = FindNearestHiddenSpecialRoom(build, floor);
                if (special != null)
                {
                    MarkRoomDiscovered(floor, special.nodeId);
                    int extra = RevealExtraSpecialRooms(build, floor, special.nodeId, RunStatAggregator.Current.scoutRevealBonus);
                    GameBootstrap.Instance?.RunService?.SaveActiveFloorState();
                    return extra > 0
                        ? $"Nearest special room marked: {special.purposeDisplayName}. +{extra} extra scout reveal."
                        : $"Nearest special room marked: {special.purposeDisplayName}.";
                }

                DungeonRoomBuildRecord bounty = FindHiddenBountyRoom(build, floor, profileService?.Current);
                if (bounty != null)
                {
                    MarkRoomDiscovered(floor, bounty.nodeId);
                    GameBootstrap.Instance?.RunService?.SaveActiveFloorState();
                    return "Bounty trail marked.";
                }
            }

            AddProfileProgress(profileService, classXp: 6, reputation: 2);
            return "Already mapped. +6 class XP, +2 reputation.";
        }

        private static DungeonRoomBuildRecord FindStairRoom(DungeonBuildResult build)
        {
            for (int i = 0; i < build.rooms.Count; i++)
            {
                DungeonRoomBuildRecord room = build.rooms[i];
                if (room.roomType == DungeonNodeKind.TransitDown || room.roomType == DungeonNodeKind.TransitUp)
                {
                    return room;
                }
            }

            return null;
        }

        private static DungeonRoomBuildRecord FindNearestHiddenSpecialRoom(DungeonBuildResult build, FloorState floor)
        {
            for (int i = 0; i < build.rooms.Count; i++)
            {
                DungeonRoomBuildRecord room = build.rooms[i];
                if (!string.IsNullOrWhiteSpace(room.purposeId) && !IsRoomKnown(floor, room.nodeId))
                {
                    return room;
                }
            }

            return null;
        }

        private static DungeonRoomBuildRecord FindHiddenBountyRoom(DungeonBuildResult build, FloorState floor, ProfileState profile)
        {
            if (profile == null || profile.bounties == null)
            {
                return null;
            }

            for (int i = 0; i < profile.bounties.Count; i++)
            {
                BountyRuntimeState bounty = profile.bounties[i];
                if (bounty == null ||
                    bounty.spawnedFloorIndex != floor.floorIndex ||
                    string.IsNullOrWhiteSpace(bounty.targetRoomId) ||
                    IsRoomKnown(floor, bounty.targetRoomId))
                {
                    continue;
                }

                return build.FindRoom(bounty.targetRoomId);
            }

            return null;
        }

        private static int RevealExtraSpecialRooms(DungeonBuildResult build, FloorState floor, string excludeRoomId, int count)
        {
            int revealed = 0;
            for (int i = 0; i < build.rooms.Count && revealed < count; i++)
            {
                DungeonRoomBuildRecord room = build.rooms[i];
                if (room.nodeId == excludeRoomId || string.IsNullOrWhiteSpace(room.purposeId) || IsRoomKnown(floor, room.nodeId))
                {
                    continue;
                }

                MarkRoomDiscovered(floor, room.nodeId);
                revealed++;
            }

            return revealed;
        }

        private static void MarkRoomDiscovered(FloorState floor, string roomId)
        {
            if (floor == null || string.IsNullOrWhiteSpace(roomId))
            {
                return;
            }

            floor.discoveredRoomIds ??= new System.Collections.Generic.List<string>();
            if (!floor.discoveredRoomIds.Contains(roomId))
            {
                floor.discoveredRoomIds.Add(roomId);
            }

            GameplayEventBus.Publish(new GameplayEvent
            {
                eventType = GameplayEventType.RoomDiscovered,
                roomId = roomId,
                floorIndex = floor.floorIndex,
                timestamp = Time.unscaledTime
            });
        }

        private static bool IsRoomKnown(FloorState floor, string roomId)
        {
            return floor != null &&
                   !string.IsNullOrWhiteSpace(roomId) &&
                   ((floor.discoveredRoomIds != null && floor.discoveredRoomIds.Contains(roomId)) ||
                    (floor.visitedRoomIds != null && floor.visitedRoomIds.Contains(roomId)));
        }

        private bool IsClaimed()
        {
            FloorState floor = GetCurrentFloor();
            return floor != null && floor.HasClaimedRoomPurpose(purposeId);
        }

        private void MarkClaimed()
        {
            FloorState floor = GetCurrentFloor();
            if (floor == null)
            {
                return;
            }

            floor.MarkRoomPurposeClaimed(purposeId);
            GameBootstrap.Instance?.RunService?.SaveActiveFloorState();
        }

        private float ApplyNonLethalRisk(PlayerHealth playerHealth)
        {
            if (playerHealth == null || healthRiskAmount <= 0f || playerHealth.CurrentHealth <= 1f)
            {
                return 0f;
            }

            float amount = Mathf.Min(healthRiskAmount, Mathf.Max(0f, playerHealth.CurrentHealth - 1f));
            DamageResult result = playerHealth.ApplyDamage(new DamageInfo
            {
                amount = amount,
                source = gameObject,
                hitPoint = playerHealth.transform.position,
                hitNormal = Vector3.up,
                damageType = DamageType.Void,
                deliveryType = DamageDeliveryType.StatusTick
            });
            return result.applied ? result.damageApplied : 0f;
        }

        private static FloorState GetCurrentFloor()
        {
            return GameBootstrap.Instance != null && GameBootstrap.Instance.RunService != null
                ? GameBootstrap.Instance.RunService.Current?.currentFloor
                : null;
        }

        private static string BuildResultMessage(int gold, int ammo, float healed, float hpLost, string sideEffect)
        {
            string message = string.Empty;
            if (gold > 0)
            {
                message += $"+{gold} gold";
            }

            if (ammo > 0)
            {
                message += (message.Length > 0 ? ", " : string.Empty) + $"+{ammo} ammo";
            }

            if (healed > 0f)
            {
                message += (message.Length > 0 ? ", " : string.Empty) + $"+{healed:0} HP";
            }

            if (hpLost > 0f)
            {
                message += (message.Length > 0 ? ", " : string.Empty) + $"-{hpLost:0} HP";
            }

            if (!string.IsNullOrWhiteSpace(sideEffect))
            {
                message += (message.Length > 0 ? ", " : string.Empty) + sideEffect;
            }

            return string.IsNullOrWhiteSpace(message) ? "Nothing happened." : message;
        }
    }
}
