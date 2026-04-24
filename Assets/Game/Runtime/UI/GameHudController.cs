using FrontierDepths.Core;
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
        }

        private void HandleEscapeAndResume()
        {
            if (InputFrameGuard.WasTownServiceCloseConsumedThisFrame)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (townHub != null && townHub.IsPanelOpen)
                {
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

                bool panelOpen = townHub.IsPanelOpen;
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
