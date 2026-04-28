using FrontierDepths.Core;
using FrontierDepths.World;
using UnityEngine;

namespace FrontierDepths.UI
{
    public sealed class DepthSenseController : MonoBehaviour
    {
        public const float ManaCost = 25f;

        private FirstPersonController playerController;
        private PlayerResourceController resources;
        private DungeonSceneController dungeonScene;
        private DungeonMinimapController minimap;
        private float nextResolveTime;

        private void Update()
        {
            Resolve();
            if (!InputBindingService.GetKeyDown(GameplayInputAction.ManaSense) ||
                playerController == null ||
                playerController.IsUiCaptured ||
                DungeonRewardChoiceController.IsRewardChoiceActive)
            {
                return;
            }

            TryUseDepthSense();
        }

        private bool TryUseDepthSense()
        {
            DungeonBuildResult build = dungeonScene != null ? dungeonScene.CurrentBuildResult : null;
            if (build == null || minimap == null || resources == null)
            {
                resources?.SetStatusMessage("Depth Sense: the depths are quiet.");
                return false;
            }

            DungeonRoomBuildRecord target = FindSenseTarget(build);
            if (target == null)
            {
                resources.SetStatusMessage("Depth Sense: the depths are quiet.");
                return false;
            }

            if (!resources.TrySpendMana(ManaCost, "Depth Sense"))
            {
                return false;
            }

            minimap.RevealRoom(target.nodeId, true);
            resources.SetStatusMessage($"Depth Sense: {DescribeTarget(target)} marked.");
            return true;
        }

        private DungeonRoomBuildRecord FindSenseTarget(DungeonBuildResult build)
        {
            DungeonRoomBuildRecord best = null;
            float bestDistance = float.MaxValue;
            Vector3 origin = playerController != null ? playerController.transform.position : Vector3.zero;

            for (int pass = 0; pass < 3; pass++)
            {
                best = null;
                bestDistance = float.MaxValue;
                for (int i = 0; i < build.rooms.Count; i++)
                {
                    DungeonRoomBuildRecord room = build.rooms[i];
                    if (room == null || minimap.IsRoomDiscovered(room.nodeId) || minimap.IsRoomVisited(room.nodeId))
                    {
                        continue;
                    }

                    bool eligible = pass switch
                    {
                        0 => room.roomRole == DungeonRoomRole.Exit || room.roomType == DungeonNodeKind.TransitDown,
                        1 => !string.IsNullOrWhiteSpace(room.bountyId) || room.roomRole == DungeonRoomRole.Bounty,
                        _ => !string.IsNullOrWhiteSpace(room.purposeId) || room.roomRole == DungeonRoomRole.Treasure ||
                             room.roomRole == DungeonRoomRole.Shrine || room.roomRole == DungeonRoomRole.Scout ||
                             room.roomRole == DungeonRoomRole.Armory || room.roomRole == DungeonRoomRole.Elite
                    };
                    if (!eligible)
                    {
                        continue;
                    }

                    float distance = Vector3.SqrMagnitude(room.bounds.center - origin);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = room;
                    }
                }

                if (best != null)
                {
                    return best;
                }
            }

            return null;
        }

        private static string DescribeTarget(DungeonRoomBuildRecord target)
        {
            if (!string.IsNullOrWhiteSpace(target.purposeDisplayName))
            {
                return target.purposeDisplayName;
            }

            return target.roomRole switch
            {
                DungeonRoomRole.Exit => "stair room",
                DungeonRoomRole.Bounty => "bounty trail",
                DungeonRoomRole.Secret => "hidden room",
                _ => "point of interest"
            };
        }

        private void Resolve()
        {
            if (Time.unscaledTime < nextResolveTime &&
                playerController != null &&
                resources != null &&
                dungeonScene != null &&
                minimap != null)
            {
                return;
            }

            nextResolveTime = Time.unscaledTime + 0.5f;
            playerController ??= FindAnyObjectByType<FirstPersonController>();
            resources ??= playerController != null ? playerController.GetComponent<PlayerResourceController>() : FindAnyObjectByType<PlayerResourceController>();
            dungeonScene ??= FindAnyObjectByType<DungeonSceneController>();
            minimap ??= GetComponent<DungeonMinimapController>();
        }
    }
}
