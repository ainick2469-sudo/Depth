using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class DungeonStairsInteractable : MonoBehaviour, IInteractable
    {
        public string DisplayName => "Lower Stairs";
        public string Prompt => $"Descend to floor {GameBootstrap.Instance.RunService.Current.floorIndex + 1}";

        public bool CanInteract(PlayerInteractor interactor, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public void Interact(PlayerInteractor interactor)
        {
            GameplayEventBus.Publish(new GameplayEvent
            {
                eventType = GameplayEventType.StairsUsed,
                sourceObject = gameObject,
                floorIndex = GameBootstrap.Instance.RunService.Current.floorIndex,
                timestamp = Time.unscaledTime
            });
            GameBootstrap.Instance.RunService.DescendToNextFloor();
            GameBootstrap.Instance.SceneFlowService.ReloadCurrentScene();
        }
    }
}
