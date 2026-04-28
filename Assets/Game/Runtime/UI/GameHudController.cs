using FrontierDepths.Core;
using FrontierDepths.Combat;
using FrontierDepths.Progression;
using FrontierDepths.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class GameHudController : MonoBehaviour
    {
        private static readonly Color PromptReadyColor = new Color(0.96f, 0.95f, 0.9f, 1f);
        private static readonly Color PromptBlockedColor = new Color(1f, 0.72f, 0.48f, 1f);
        private static readonly Color PromptPausedColor = new Color(0.86f, 0.88f, 0.93f, 1f);

        [SerializeField] private Text promptText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text panelText;
        [SerializeField] private Image panelBackground;
        [SerializeField] private Image promptBackground;
        [SerializeField] private Image crosshairImage;
        [SerializeField] private Image crosshairCoreImage;

        private PlayerInteractor interactor;
        private FirstPersonController playerController;
        private TownHubController townHub;
        private DungeonSceneController dungeonScene;
        private DungeonMinimapController minimapController;
        private DungeonMapPanelController fullMapController;
        private RunInfoPanelController runInfoPanelController;
        private PauseMenuController pauseMenuController;
        private DungeonBuildResult configuredMinimapBuild;

        private void Awake()
        {
            EnsureHudElements();
            ResolveSceneReferences();
            HidePanel();
            HidePrompt();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            EnsureHudElements();
            ResolveSceneReferences();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Start()
        {
            ResolveSceneReferences();
        }

        private void Update()
        {
            RefreshDungeonHudBindings();
            HandleOverlayInput();
            HandleEscapeAndResume();
            RefreshStatusAndPanel();
            RefreshPromptAndCrosshair();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResolveSceneReferences();
            EnsureHudElements();
            HidePanel();
            HidePrompt();
            runInfoPanelController?.SetVisible(false);
            pauseMenuController?.Hide();
            fullMapController?.SetVisible(false, playerController);
            configuredMinimapBuild = null;
        }

        private void HandleEscapeAndResume()
        {
            if (InputFrameGuard.WasTownServiceInputConsumedThisFrame)
            {
                return;
            }

            if (DungeonRewardChoiceController.IsRewardChoiceActive)
            {
                return;
            }

            if (InputBindingService.GetKeyDown(GameplayInputAction.Pause))
            {
                if (pauseMenuController != null && pauseMenuController.IsVisible)
                {
                    pauseMenuController.Hide();
                    return;
                }

                if (runInfoPanelController != null && runInfoPanelController.IsVisible)
                {
                    SetRunInfoVisible(false);
                    return;
                }

                if (townHub != null && townHub.IsPanelOpen)
                {
                    return;
                }

                if (pauseMenuController != null)
                {
                    pauseMenuController.Show(playerController);
                    return;
                }

                playerController?.ToggleManualPause();
                return;
            }

            if (playerController == null || !playerController.IsManualPauseActive)
            {
                return;
            }

            if (townHub != null && townHub.IsPanelOpen)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) && !IsPointerOverBlockingUi())
            {
                playerController.ResumeGameplayCapture();
            }
        }

        private void RefreshStatusAndPanel()
        {
            if (townHub != null)
            {
                if (statusText != null)
                {
                    statusText.text = townHub.GetStatusLine();
                }

                bool panelOpen = townHub.IsPanelOpen && !TownServicePanelController.IsAnyVisible;
                if (panelBackground != null)
                {
                    panelBackground.enabled = panelOpen;
                }

                if (panelText != null)
                {
                    panelText.enabled = panelOpen;
                    panelText.text = panelOpen ? townHub.BuildPanelText() : string.Empty;
                }

                return;
            }

            if (statusText != null)
            {
                statusText.text = dungeonScene != null ? dungeonScene.GetStatusLine() : string.Empty;
            }

            HidePanel();
        }

        private void RefreshPromptAndCrosshair()
        {
            bool uiCaptured = playerController != null && playerController.IsUiCaptured;
            bool panelOpen = townHub != null && townHub.IsPanelOpen;
            SetCrosshairVisible(!uiCaptured);

            if (playerController != null && playerController.IsManualPauseActive && !panelOpen)
            {
                ShowPrompt("Paused - Press Escape or click to resume", PromptPausedColor);
                return;
            }

            if (panelOpen)
            {
                HidePrompt();
                return;
            }

            if (interactor == null || !interactor.HasFocusedInteractable)
            {
                PlayerWeaponController weapon = playerController != null ? playerController.GetComponent<PlayerWeaponController>() : null;
                if (weapon != null && (weapon.ReserveAmmo <= 0 || weapon.CurrentAmmo <= 0))
                {
                    ShowPrompt($"{InputBindingService.GetDisplay(GameplayInputAction.PistolWhip)}: Pistol Whip", PromptPausedColor);
                    return;
                }

                HidePrompt();
                return;
            }

            if (interactor.FocusedCanInteract && !string.IsNullOrWhiteSpace(interactor.FocusedPrompt))
            {
                ShowPrompt($"Press E to {interactor.FocusedPrompt}", PromptReadyColor);
                return;
            }

            if (!string.IsNullOrWhiteSpace(interactor.BlockedReason))
            {
                ShowPrompt(interactor.BlockedReason, PromptBlockedColor);
                return;
            }

            HidePrompt();
        }

        private void EnsureHudElements()
        {
            promptText ??= FindNamedComponent<Text>("Prompt");
            statusText ??= FindNamedComponent<Text>("Status");
            panelBackground ??= FindNamedComponent<Image>("PanelBackground");
            panelText ??= FindNamedComponent<Text>("PanelText");
            crosshairImage ??= FindNamedComponent<Image>("Crosshair");
            pauseMenuController ??= GetComponentInChildren<PauseMenuController>(true);
            if (pauseMenuController == null)
            {
                pauseMenuController = gameObject.AddComponent<PauseMenuController>();
            }

            if (promptText != null)
            {
                promptText.supportRichText = true;
                promptText.color = PromptReadyColor;
                promptText.raycastTarget = false;
            }

            if (statusText != null)
            {
                statusText.raycastTarget = false;
            }

            if (panelText != null)
            {
                panelText.raycastTarget = false;
            }

            if (panelBackground != null)
            {
                panelBackground.raycastTarget = false;
            }

            EnsurePromptBackground();
            EnsureCrosshairVisuals();
            EnsureWeaponHudView();
            EnsurePlayerHealthHudView();
            EnsureInventoryPanelController();
            EnsureHudResourceView();
            EnsureControlHintHudView();
            EnsureDashHudView();
            EnsureCompassHudView();
            EnsureDepthSenseController();
            EnsureDungeonMinimapController();
            EnsureDungeonMapPanelController();
            EnsureRunInfoPanelController();
        }

        private void EnsureWeaponHudView()
        {
            if (GetComponent<WeaponHudView>() == null)
            {
                gameObject.AddComponent<WeaponHudView>();
            }
        }

        private void EnsurePlayerHealthHudView()
        {
            if (GetComponent<PlayerHealthHudView>() == null)
            {
                gameObject.AddComponent<PlayerHealthHudView>();
            }
        }

        private void EnsureInventoryPanelController()
        {
            if (GetComponent<InventoryPanelController>() == null)
            {
                gameObject.AddComponent<InventoryPanelController>();
            }
        }

        private void EnsureHudResourceView()
        {
            if (GetComponent<HudResourceView>() == null)
            {
                gameObject.AddComponent<HudResourceView>();
            }
        }

        private void EnsureControlHintHudView()
        {
            if (GetComponent<ControlHintHudView>() == null)
            {
                gameObject.AddComponent<ControlHintHudView>();
            }
        }

        private void EnsureDashHudView()
        {
            if (GetComponent<DashHudView>() == null)
            {
                gameObject.AddComponent<DashHudView>();
            }
        }

        private void EnsureCompassHudView()
        {
            if (GetComponent<CompassHudView>() == null)
            {
                gameObject.AddComponent<CompassHudView>();
            }
        }

        private void EnsureDepthSenseController()
        {
            if (GetComponent<DepthSenseController>() == null)
            {
                gameObject.AddComponent<DepthSenseController>();
            }
        }

        private void EnsureDungeonMinimapController()
        {
            minimapController = GetComponent<DungeonMinimapController>();
            if (minimapController == null)
            {
                minimapController = gameObject.AddComponent<DungeonMinimapController>();
            }
        }

        private void EnsureDungeonMapPanelController()
        {
            fullMapController = GetComponent<DungeonMapPanelController>();
            if (fullMapController == null)
            {
                fullMapController = gameObject.AddComponent<DungeonMapPanelController>();
            }
        }

        private void EnsureRunInfoPanelController()
        {
            runInfoPanelController = GetComponent<RunInfoPanelController>();
            if (runInfoPanelController == null)
            {
                runInfoPanelController = gameObject.AddComponent<RunInfoPanelController>();
            }
        }

        private void EnsurePromptBackground()
        {
            if (promptText == null)
            {
                return;
            }

            promptBackground ??= FindNamedComponent<Image>("PromptBackground");
            if (promptBackground == null)
            {
                GameObject backgroundObject = new GameObject("PromptBackground", typeof(RectTransform), typeof(Image));
                backgroundObject.transform.SetParent(transform, false);
                promptBackground = backgroundObject.GetComponent<Image>();
            }

            promptBackground.color = new Color(0.02f, 0.02f, 0.03f, 0.68f);
            promptBackground.raycastTarget = false;
            RectTransform backgroundRect = promptBackground.rectTransform;
            backgroundRect.anchorMin = new Vector2(0.5f, 0f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(620f, 64f);
            backgroundRect.anchoredPosition = new Vector2(0f, 96f);
            promptBackground.transform.SetSiblingIndex(promptText.transform.GetSiblingIndex());
            promptBackground.enabled = false;

            RectTransform promptRect = promptText.rectTransform;
            promptRect.anchorMin = new Vector2(0.5f, 0f);
            promptRect.anchorMax = new Vector2(0.5f, 0f);
            promptRect.pivot = new Vector2(0.5f, 0.5f);
            promptRect.sizeDelta = new Vector2(560f, 56f);
            promptRect.anchoredPosition = new Vector2(0f, 96f);
            promptText.alignment = TextAnchor.MiddleCenter;
        }

        private void EnsureCrosshairVisuals()
        {
            if (crosshairImage == null)
            {
                return;
            }

            crosshairImage.color = new Color(0f, 0f, 0f, 0.45f);
            crosshairImage.raycastTarget = false;
            RectTransform crosshairRect = crosshairImage.rectTransform;
            crosshairRect.anchorMin = crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRect.sizeDelta = new Vector2(14f, 14f);
            crosshairRect.anchoredPosition = Vector2.zero;

            Transform coreTransform = crosshairImage.transform.Find("CrosshairCore");
            if (crosshairCoreImage == null && coreTransform != null)
            {
                crosshairCoreImage = coreTransform.GetComponent<Image>();
            }

            if (crosshairCoreImage == null)
            {
                GameObject coreObject = new GameObject("CrosshairCore", typeof(RectTransform), typeof(Image));
                coreObject.transform.SetParent(crosshairImage.transform, false);
                crosshairCoreImage = coreObject.GetComponent<Image>();
            }

            crosshairCoreImage.color = new Color(0.98f, 0.98f, 0.96f, 0.95f);
            crosshairCoreImage.raycastTarget = false;
            RectTransform coreRect = crosshairCoreImage.rectTransform;
            coreRect.anchorMin = coreRect.anchorMax = new Vector2(0.5f, 0.5f);
            coreRect.sizeDelta = new Vector2(4f, 4f);
            coreRect.anchoredPosition = Vector2.zero;
        }

        private void ResolveSceneReferences()
        {
            playerController = FindAnyObjectByType<FirstPersonController>();
            interactor = playerController != null ? playerController.Interactor : FindAnyObjectByType<PlayerInteractor>();
            townHub = FindAnyObjectByType<TownHubController>();
            dungeonScene = FindAnyObjectByType<DungeonSceneController>();
            configuredMinimapBuild = null;
            RefreshDungeonHudBindings();
        }

        private void RefreshDungeonHudBindings()
        {
            EnsureDungeonMinimapController();
            EnsureDungeonMapPanelController();
            EnsureRunInfoPanelController();
            if (dungeonScene == null)
            {
                dungeonScene = FindAnyObjectByType<DungeonSceneController>();
            }

            if (playerController == null)
            {
                playerController = FindAnyObjectByType<FirstPersonController>();
            }

            DungeonBuildResult build = dungeonScene != null ? dungeonScene.CurrentBuildResult : null;
            if (build != null && build != configuredMinimapBuild)
            {
                configuredMinimapBuild = build;
                ApplySettingsToMinimap();
                minimapController.Configure(build, playerController != null ? playerController.transform : null);
                fullMapController.Configure(build, playerController != null ? playerController.transform : null, minimapController);
            }
            else if (build == null && configuredMinimapBuild != null)
            {
                configuredMinimapBuild = null;
                minimapController.Configure(null, null);
                fullMapController.Configure(null, null, minimapController);
            }
            else if (build == null && minimapController != null && minimapController.IsConfigured)
            {
                minimapController.Configure(null, null);
                fullMapController.Configure(null, null, minimapController);
            }
        }

        private void ApplySettingsToMinimap()
        {
            if (minimapController == null)
            {
                return;
            }

            GameSettingsState settings = GameSettingsService.Current;
            minimapController.SetSize(settings.minimapSize);
            minimapController.SetOpacity(settings.minimapOpacity);
            minimapController.SetZoom(settings.minimapZoom);
        }

        private void HandleOverlayInput()
        {
            if (fullMapController != null && fullMapController.IsVisible)
            {
                if (InputBindingService.GetKeyDown(GameplayInputAction.ToggleFullMap) ||
                    InputBindingService.GetKeyDown(GameplayInputAction.Pause))
                {
                    fullMapController.SetVisible(false, playerController);
                }

                return;
            }

            if (InputFrameGuard.WasTownServiceInputConsumedThisFrame || !CanToggleGameplayOverlay())
            {
                return;
            }

            if (runInfoPanelController != null && runInfoPanelController.IsVisible)
            {
                if (InputBindingService.GetKeyDown(GameplayInputAction.RunInfo))
                {
                    SetRunInfoVisible(false);
                }

                return;
            }

            if (InputBindingService.GetKeyDown(GameplayInputAction.ToggleFullMap))
            {
                fullMapController?.Toggle(playerController);
                return;
            }

            if (InputBindingService.GetKeyDown(GameplayInputAction.Minimap))
            {
                minimapController?.ToggleVisibility();
            }

            if (InputBindingService.GetKeyDown(GameplayInputAction.RunInfo))
            {
                SetRunInfoVisible(true);
            }
        }

        private bool CanToggleGameplayOverlay()
        {
            if (DungeonRewardChoiceController.IsRewardChoiceActive)
            {
                CloseRunInfoIfOpen();
                return false;
            }

            if (pauseMenuController != null && pauseMenuController.IsVisible)
            {
                return false;
            }

            if (playerController != null && playerController.IsUiCaptured && (runInfoPanelController == null || !runInfoPanelController.IsVisible))
            {
                return false;
            }

            if (townHub != null && townHub.IsPanelOpen)
            {
                CloseRunInfoIfOpen();
                return false;
            }

            if (playerController != null && playerController.IsManualPauseActive)
            {
                CloseRunInfoIfOpen();
                return false;
            }

            return true;
        }

        private void SetRunInfoVisible(bool visible)
        {
            if (runInfoPanelController == null)
            {
                return;
            }

            runInfoPanelController.SetVisible(visible);
            playerController?.SetUiCaptured(visible);
        }

        private void CloseRunInfoIfOpen()
        {
            if (runInfoPanelController != null && runInfoPanelController.IsVisible)
            {
                runInfoPanelController.SetVisible(false);
            }
        }

        private T FindNamedComponent<T>(string objectName) where T : Component
        {
            Transform target = FindNamedTransform(transform, objectName);
            return target != null ? target.GetComponent<T>() : null;
        }

        private static Transform FindNamedTransform(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindNamedTransform(root.GetChild(i), objectName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static bool IsPointerOverBlockingUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void SetCrosshairVisible(bool visible)
        {
            if (crosshairImage != null)
            {
                crosshairImage.enabled = visible;
            }

            if (crosshairCoreImage != null)
            {
                crosshairCoreImage.enabled = visible;
            }
        }

        private void ShowPrompt(string text, Color color)
        {
            if (promptText == null)
            {
                return;
            }

            promptText.enabled = true;
            promptText.color = color;
            promptText.text = text;
            if (promptBackground != null)
            {
                promptBackground.enabled = true;
            }
        }

        private void HidePrompt()
        {
            if (promptText != null)
            {
                promptText.enabled = false;
                promptText.text = string.Empty;
            }

            if (promptBackground != null)
            {
                promptBackground.enabled = false;
            }
        }

        private void HidePanel()
        {
            if (panelBackground != null)
            {
                panelBackground.enabled = false;
            }

            if (panelText != null)
            {
                panelText.enabled = false;
                panelText.text = string.Empty;
            }
        }
    }
}
