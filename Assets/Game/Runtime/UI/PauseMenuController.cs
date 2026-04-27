using FrontierDepths.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        public const string RootName = "RuntimePauseUI";

        private RectTransform panel;
        private RectTransform settingsPanel;
        private Text messageText;
        private Text settingsText;
        private FirstPersonController playerController;
        private int returnConfirmFrame = -999;
        private int quitConfirmFrame = -999;

        public bool IsVisible => panel != null && panel.gameObject.activeSelf;

        private void Awake()
        {
            EnsureUi();
            Hide();
        }

        public void Show(FirstPersonController controller)
        {
            EnsureUi();
            playerController = controller ?? FindAnyObjectByType<FirstPersonController>();
            panel.gameObject.SetActive(true);
            settingsPanel.gameObject.SetActive(false);
            messageText.text = "Paused";
            playerController?.SetUiCaptured(true);
            RefreshSettingsText();
        }

        public void Hide()
        {
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.gameObject.SetActive(false);
            }

            playerController ??= FindAnyObjectByType<FirstPersonController>();
            playerController?.SetUiCaptured(false);
        }

        private void EnsureUi()
        {
            if (panel != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject panelObject = new GameObject(RootName, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(transform, false);
            panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(420f, 430f);
            panel.anchoredPosition = Vector2.zero;
            panelObject.GetComponent<Image>().color = new Color(0.02f, 0.018f, 0.018f, 0.94f);

            CreateLabel(panel, "Title", "PAUSED", font, 30, new Vector2(0f, -36f), new Vector2(380f, 44f));
            messageText = CreateLabel(panel, "Message", "Paused", font, 15, new Vector2(0f, -82f), new Vector2(380f, 42f));
            CreateButton(panel, "Resume", "Resume", new Vector2(0f, -138f), Resume);
            CreateButton(panel, "Settings", "Settings", new Vector2(0f, -190f), ToggleSettings);
            CreateButton(panel, "ReturnTown", "Return To Town", new Vector2(0f, -242f), ReturnToTown);
            CreateButton(panel, "QuitMain", "Quit To Main Menu", new Vector2(0f, -294f), QuitToMainMenu);

            GameObject settingsObject = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            settingsObject.transform.SetParent(panel, false);
            settingsPanel = settingsObject.GetComponent<RectTransform>();
            settingsPanel.anchorMin = settingsPanel.anchorMax = new Vector2(1f, 0.5f);
            settingsPanel.pivot = new Vector2(0f, 0.5f);
            settingsPanel.sizeDelta = new Vector2(430f, 620f);
            settingsPanel.anchoredPosition = new Vector2(18f, 0f);
            settingsObject.GetComponent<Image>().color = new Color(0.035f, 0.032f, 0.028f, 0.94f);
            settingsText = CreateLabel(settingsPanel, "SettingsText", string.Empty, font, 16, new Vector2(0f, -36f), new Vector2(390f, 300f));
            CreateButton(settingsPanel, "SensitivityMinus", "Sensitivity -", new Vector2(-105f, -250f), () => AdjustSensitivity(-0.1f));
            CreateButton(settingsPanel, "SensitivityPlus", "Sensitivity +", new Vector2(105f, -250f), () => AdjustSensitivity(0.1f));
            CreateButton(settingsPanel, "FovMinus", "FOV -", new Vector2(-105f, -302f), () => AdjustFov(-5f));
            CreateButton(settingsPanel, "FovPlus", "FOV +", new Vector2(105f, -302f), () => AdjustFov(5f));
            CreateButton(settingsPanel, "MapSizeMinus", "Map Size -", new Vector2(-105f, -354f), () => AdjustMinimapSize(-20f));
            CreateButton(settingsPanel, "MapSizePlus", "Map Size +", new Vector2(105f, -354f), () => AdjustMinimapSize(20f));
            CreateButton(settingsPanel, "MapOpacityMinus", "Opacity -", new Vector2(-105f, -406f), () => AdjustMinimapOpacity(-0.1f));
            CreateButton(settingsPanel, "MapOpacityPlus", "Opacity +", new Vector2(105f, -406f), () => AdjustMinimapOpacity(0.1f));
            CreateButton(settingsPanel, "MapZoomMinus", "Map Zoom -", new Vector2(-105f, -458f), () => AdjustMinimapZoom(-0.1f));
            CreateButton(settingsPanel, "MapZoomPlus", "Map Zoom +", new Vector2(105f, -458f), () => AdjustMinimapZoom(0.1f));
            CreateButton(settingsPanel, "VolumeMinus", "Volume -", new Vector2(-105f, -510f), () => AdjustMasterVolume(-0.1f));
            CreateButton(settingsPanel, "VolumePlus", "Volume +", new Vector2(105f, -510f), () => AdjustMasterVolume(0.1f));
            CreateButton(settingsPanel, "InvertY", "Invert Y", new Vector2(0f, -562f), ToggleInvertY);
        }

        private void Resume()
        {
            Hide();
        }

        private void ToggleSettings()
        {
            settingsPanel.gameObject.SetActive(!settingsPanel.gameObject.activeSelf);
            RefreshSettingsText();
        }

        private void ReturnToTown()
        {
            if (Time.frameCount - returnConfirmFrame > 180)
            {
                returnConfirmFrame = Time.frameCount;
                messageText.text = "Click Return To Town again to confirm.";
                return;
            }

            GameBootstrap bootstrap = GameBootstrap.Instance;
            if (bootstrap == null)
            {
                return;
            }

            Time.timeScale = 1f;
            bootstrap.RunService?.PrepareTownReturnOnFoot();
            bootstrap.SceneFlowService?.SetPendingTownHubLoadReason(TownHubLoadReason.DungeonEntranceReturn);
            bootstrap.SceneFlowService?.LoadScene(GameSceneId.TownHub);
        }

        private void QuitToMainMenu()
        {
            if (Time.frameCount - quitConfirmFrame > 180)
            {
                quitConfirmFrame = Time.frameCount;
                messageText.text = "Click Quit To Main Menu again to confirm.";
                return;
            }

            Time.timeScale = 1f;
            GameBootstrap.Instance?.RunService?.SaveActiveFloorState();
            SceneManager.LoadScene(GameSceneCatalog.MainMenu, LoadSceneMode.Single);
        }

        private void AdjustSensitivity(float delta)
        {
            GameSettingsState settings = GameSettingsService.Current;
            settings.mouseSensitivity += delta;
            SaveApplySettings(settings);
        }

        private void AdjustFov(float delta)
        {
            GameSettingsState settings = GameSettingsService.Current;
            settings.fov += delta;
            SaveApplySettings(settings);
        }

        private void AdjustMinimapSize(float delta)
        {
            GameSettingsState settings = GameSettingsService.Current;
            settings.minimapSize += delta;
            SaveApplySettings(settings);
        }

        private void AdjustMinimapOpacity(float delta)
        {
            GameSettingsState settings = GameSettingsService.Current;
            settings.minimapOpacity += delta;
            SaveApplySettings(settings);
        }

        private void AdjustMinimapZoom(float delta)
        {
            GameSettingsState settings = GameSettingsService.Current;
            settings.minimapZoom += delta;
            SaveApplySettings(settings);
        }

        private void AdjustMasterVolume(float delta)
        {
            GameSettingsState settings = GameSettingsService.Current;
            settings.masterVolume += delta;
            settings.sfxVolume += delta;
            SaveApplySettings(settings);
        }

        private void ToggleInvertY()
        {
            GameSettingsState settings = GameSettingsService.Current;
            settings.invertY = !settings.invertY;
            SaveApplySettings(settings);
        }

        private void SaveApplySettings(GameSettingsState settings)
        {
            settings.Clamp();
            GameSettingsService.Save(settings);
            GameSettingsService.ApplyRuntime(settings);
            playerController ??= FindAnyObjectByType<FirstPersonController>();
            playerController?.ApplyLookSettings(settings.mouseSensitivity, settings.invertY, settings.fov);
            DungeonMinimapController minimap = FindAnyObjectByType<DungeonMinimapController>();
            if (minimap != null)
            {
                minimap.SetSize(settings.minimapSize);
                minimap.SetOpacity(settings.minimapOpacity);
                minimap.SetZoom(settings.minimapZoom);
            }
            RefreshSettingsText();
        }

        private void RefreshSettingsText()
        {
            if (settingsText == null)
            {
                return;
            }

            GameSettingsState settings = GameSettingsService.Current;
            settingsText.text =
                $"Mouse Sensitivity: {settings.mouseSensitivity:0.0}\n" +
                $"FOV: {settings.fov:0}\n" +
                $"Master Volume: {settings.masterVolume:0.00}\n" +
                $"SFX Volume: {settings.sfxVolume:0.00}\n" +
                $"Music Volume: {settings.musicVolume:0.00}\n" +
                $"Invert Y: {(settings.invertY ? "On" : "Off")}\n" +
                $"Crosshair Size: {settings.crosshairSize:0}\n" +
                $"Minimap Size: {settings.minimapSize:0}\n" +
                $"Minimap Opacity: {settings.minimapOpacity:0.00}\n" +
                $"Minimap Zoom: {settings.minimapZoom:0.00}";
        }

        private static Text CreateLabel(Transform parent, string name, string label, Font font, int fontSize, Vector2 position, Vector2 size)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = new Color(0.16f, 0.12f, 0.09f, 0.96f);
            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(action);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(220f, 40f);
            rect.anchoredPosition = position;
            Text text = CreateLabel(buttonObject.transform, "Label", label, font, 16, Vector2.zero, rect.sizeDelta);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            return button;
        }
    }
}
