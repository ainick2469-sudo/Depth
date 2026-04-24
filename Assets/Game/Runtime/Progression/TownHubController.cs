using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.Progression
{
    public sealed class TownHubController : MonoBehaviour
    {
        private const string DefaultSpawnAnchorName = "TownSpawn_Default";
        private const string DungeonEntranceSpawnAnchorName = "TownSpawn_DungeonEntranceReturn";
        private const string PortalReturnSpawnAnchorName = "TownSpawn_PortalReturn";
        private const float SpawnHeight = 2f;
        private const float DungeonEntranceOffsetDistance = 8f;
        private const float PortalReturnOffsetDistance = 8f;

        private TownShopService shopService;
        private ShopDefinition activeShop;
        private string lastMessage = "Welcome back to the frontier.";
        private FirstPersonController playerController;
        private Transform defaultSpawnAnchor;
        private Transform dungeonEntranceSpawnAnchor;
        private Transform portalReturnSpawnAnchor;

        public bool IsPanelOpen => activeShop != null;
        public bool WasPanelOpenInputConsumedThisFrame => InputFrameGuard.WasTownServiceOpenConsumedThisFrame;
        public bool WasPanelCloseInputConsumedThisFrame => InputFrameGuard.WasTownServiceCloseConsumedThisFrame;

        public string BuildPanelText()
        {
            if (activeShop == null)
            {
                return string.Empty;
            }

            ProfileState profile = GameBootstrap.Instance.ProfileService.Current;
            string text = $"{activeShop.displayName}\n{activeShop.greeting}\n\nGold: {profile.gold} | Sigils: {profile.townSigils}\n\n";
            for (int i = 0; i < activeShop.offers.Length; i++)
            {
                ShopOffer offer = activeShop.offers[i];
                text += $"{i + 1}. {offer.displayName} [{offer.cost}g]\n{offer.description}\n\n";
            }

            text += "Press 1-9 to take an offer. Press E or Escape to close.";
            if (!string.IsNullOrWhiteSpace(lastMessage))
            {
                text += $"\n\n{lastMessage}";
            }

            return text;
        }

        public string GetStatusLine()
        {
            ProfileState profile = GameBootstrap.Instance.ProfileService.Current;
            return $"Town Hub\nGold: {profile.gold}\nSigils: {profile.townSigils}\nWeapon: {profile.equippedWeaponId}";
        }

        private void Start()
        {
            shopService = new TownShopService(GameBootstrap.Instance.ProfileService);
            playerController = FindAnyObjectByType<FirstPersonController>();
            EnsureSpawnAnchors();
            PlacePlayerAtTownSpawn();
        }

        private void Update()
        {
            if (!IsPanelOpen)
            {
                return;
            }

            if (HandlePanelInput())
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) TrySelect(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) TrySelect(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) TrySelect(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) TrySelect(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) TrySelect(4);
            if (Input.GetKeyDown(KeyCode.Alpha6)) TrySelect(5);
            if (Input.GetKeyDown(KeyCode.Alpha7)) TrySelect(6);
            if (Input.GetKeyDown(KeyCode.Alpha8)) TrySelect(7);
            if (Input.GetKeyDown(KeyCode.Alpha9)) TrySelect(8);
        }

        public void OpenService(ShopDefinition definition)
        {
            activeShop = definition;
            lastMessage = definition != null ? definition.greeting : string.Empty;
            playerController ??= FindAnyObjectByType<FirstPersonController>();
            playerController?.SetUiCaptured(definition != null);
            if (definition != null)
            {
                InputFrameGuard.MarkTownServiceOpenConsumedThisFrame();
            }
        }

        public void CloseService()
        {
            activeShop = null;
            playerController ??= FindAnyObjectByType<FirstPersonController>();
            playerController?.SetUiCaptured(false);
        }

        public bool HandlePanelInput()
        {
            bool eDown = Input.GetKeyDown(KeyCode.E);
            bool escapeDown = Input.GetKeyDown(KeyCode.Escape);
            if (!ShouldClosePanelFromInput(IsPanelOpen, eDown, escapeDown, InputFrameGuard.WasTownServiceOpenConsumedThisFrame))
            {
                return false;
            }

            CloseServiceFromInput();
            return true;
        }

        public void CloseServiceFromInput()
        {
            InputFrameGuard.MarkTownServiceCloseConsumedThisFrame();
            CloseService();
            playerController?.ResumeGameplayCapture();
        }

        internal static bool ShouldClosePanelFromInput(bool isPanelOpen, bool eDown, bool escapeDown, bool openedThisFrame)
        {
            if (!isPanelOpen || openedThisFrame)
            {
                return false;
            }

            return eDown || escapeDown;
        }

        private void TrySelect(int index)
        {
            if (activeShop == null)
            {
                return;
            }

            shopService.TryExecuteOffer(activeShop, index, out lastMessage);
        }

        private void PlacePlayerAtTownSpawn()
        {
            playerController ??= FindAnyObjectByType<FirstPersonController>();
            if (playerController == null || GameBootstrap.Instance == null || GameBootstrap.Instance.SceneFlowService == null)
            {
                return;
            }

            TownHubLoadReason reason = GameBootstrap.Instance.SceneFlowService.ConsumePendingTownHubLoadReason();
            Transform selectedAnchor = GetSpawnAnchor(reason);
            Vector3 finalPosition = selectedAnchor != null ? selectedAnchor.position : playerController.transform.position;
            playerController.WarpTo(finalPosition);

            if (Debug.isDebugBuild || Application.isEditor)
            {
                string anchorName = selectedAnchor != null ? selectedAnchor.name : "PlayerExistingPosition";
                Debug.Log($"TownHub spawn routed. Reason={reason} Anchor={anchorName} Position={finalPosition}");
            }
        }

        private Transform GetSpawnAnchor(TownHubLoadReason reason)
        {
            EnsureSpawnAnchors();
            return reason switch
            {
                TownHubLoadReason.DungeonEntranceReturn => dungeonEntranceSpawnAnchor ?? defaultSpawnAnchor,
                TownHubLoadReason.DungeonPortalReturn => portalReturnSpawnAnchor ?? dungeonEntranceSpawnAnchor ?? defaultSpawnAnchor,
                _ => defaultSpawnAnchor ?? dungeonEntranceSpawnAnchor ?? portalReturnSpawnAnchor
            };
        }

        private void EnsureSpawnAnchors()
        {
            defaultSpawnAnchor ??= FindSceneTransform(DefaultSpawnAnchorName);
            dungeonEntranceSpawnAnchor ??= FindSceneTransform(DungeonEntranceSpawnAnchorName);
            portalReturnSpawnAnchor ??= FindSceneTransform(PortalReturnSpawnAnchorName);

            playerController ??= FindAnyObjectByType<FirstPersonController>();
            Vector3 fallbackPlayerPosition = playerController != null ? playerController.transform.position : new Vector3(0f, SpawnHeight, -28f);

            if (defaultSpawnAnchor == null)
            {
                defaultSpawnAnchor = CreateRuntimeSpawnAnchor(DefaultSpawnAnchorName, fallbackPlayerPosition);
            }

            GameObject dungeonGate = GameObject.Find("DungeonGate");
            Vector3 dungeonEntrancePosition = dungeonGate != null
                ? dungeonGate.transform.position - dungeonGate.transform.forward * DungeonEntranceOffsetDistance + Vector3.up * (SpawnHeight - dungeonGate.transform.position.y)
                : defaultSpawnAnchor.position + new Vector3(0f, 0f, 60f);

            if (dungeonEntranceSpawnAnchor == null)
            {
                dungeonEntranceSpawnAnchor = CreateRuntimeSpawnAnchor(DungeonEntranceSpawnAnchorName, dungeonEntrancePosition);
            }

            Vector3 portalReturnPosition = dungeonEntranceSpawnAnchor.position + Vector3.right * PortalReturnOffsetDistance;
            if (portalReturnSpawnAnchor == null)
            {
                portalReturnSpawnAnchor = CreateRuntimeSpawnAnchor(PortalReturnSpawnAnchorName, portalReturnPosition);
            }
        }

        private static Transform FindSceneTransform(string objectName)
        {
            GameObject sceneObject = GameObject.Find(objectName);
            return sceneObject != null ? sceneObject.transform : null;
        }

        private Transform CreateRuntimeSpawnAnchor(string objectName, Vector3 worldPosition)
        {
            GameObject anchorObject = new GameObject(objectName);
            anchorObject.transform.SetParent(transform, true);
            anchorObject.transform.position = worldPosition;
            return anchorObject.transform;
        }
    }
}
