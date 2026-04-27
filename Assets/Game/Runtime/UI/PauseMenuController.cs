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
        private RectTransform keybindingsPanel;
        private Text messageText;
        private Text settingsText;
        private Text keybindingsText;
        private FirstPersonController playerController;
        private int returnConfirmFrame = -999;
        private int quitConfirmFrame = -999;
        private GameplayInputAction? pendingRebind;

        public bool IsVisible => panel != null && panel.gameObject.activeSelf;

        private void Awake()
        {
            EnsureUi();
            Hide();
        }

        private void Update()
        {
            if (!IsVisible || !pendingRebind.HasValue)
            {
                return;
            }

            if (InputBindingService.TryReadPressedCandidate(out KeyCode key))
            {
                InputBindingService.SetPrimaryBinding(pendingRebind.Value, key);
                messageText.text = $"{FormatAction(pendingRebind.Value)} rebound to {InputBindingService.GetDisplay(pendingRebind.Value)}.";
                pendingRebind = null;
                RefreshKeybindingsText();
            }
        }

        public void Show(FirstPersonController controller)
        {
            EnsureUi();
            playerController = controller ?? FindAnyObjectByType<FirstPersonController>();
            panel.gameObject.SetActive(true);
            settingsPanel.gameObject.SetActive(false);
            keybindingsPanel.gameObject.SetActive(false);
            pendingRebind = null;
            messageText.text = "Paused";
            playerController?.SetUiCaptured(true);
            RefreshSettingsText();
            RefreshKeybindingsText();
        }

        public void Hide()
        {
            pendingRebind = null;
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.gameObject.SetActive(false);
            }

            if (keybindingsPanel != null)
            {
                keybindingsPanel.gameObject.SetActive(false);
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

            GameObject panelObject = new GameObject(RootName, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(transform, false);
            panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(460f, 520f);
            panel.anchoredPosition = Vector2.zero;
            panelObject.GetComponent<Image>().color = UiTheme.Panel;

            CreateLabel(panel, "Title", "FRONTIER PAUSED", UiTheme.TitleSize, new Vector2(0f, -32f), new Vector2(410f, 44f), TextAnchor.MiddleCenter, UiTheme.Text);
            messageText = CreateLabel(panel, "Message", "Paused", UiTheme.BodySize, new Vector2(0f, -78f), new Vector2(410f, 44f), TextAnchor.MiddleCenter, UiTheme.MutedText);
            CreateButton(panel, "Resume", "Resume", new Vector2(0f, -138f), Resume);
            CreateButton(panel, "Settings", "Settings", new Vector2(0f, -190f), ToggleSettings);
            CreateButton(panel, "Keybindings", "Keybindings", new Vector2(0f, -242f), ToggleKeybindings);
            CreateButton(panel, "ReturnTown", "Return To Town", new Vector2(0f, -294f), ReturnToTown);
            CreateButton(panel, "QuitMain", "Quit To Main Menu", new Vector2(0f, -346f), QuitToMainMenu);

            settingsPanel = CreateSidePanel("SettingsPanel", new Vector2(500f, 640f), new Vector2(24f, 0f));
            CreateLabel(settingsPanel, "SettingsTitle", "SETTINGS", UiTheme.HeaderSize, new Vector2(0f, -24f), new Vector2(450f, 34f), TextAnchor.MiddleCenter, UiTheme.Accent);
            settingsText = CreateLabel(settingsPanel, "SettingsText", string.Empty, UiTheme.BodySize, new Vector2(0f, -64f), new Vector2(440f, 190f), TextAnchor.UpperLeft, UiTheme.Text);
            CreateSettingsButtons(settingsPanel);

            keybindingsPanel = CreateSidePanel("KeybindingsPanel", new Vector2(560f, 640f), new Vector2(24f, 0f));
            CreateLabel(keybindingsPanel, "BindingsTitle", "KEYBINDINGS", UiTheme.HeaderSize, new Vector2(0f, -24f), new Vector2(500f, 34f), TextAnchor.MiddleCenter, UiTheme.Accent);
            keybindingsText = CreateLabel(keybindingsPanel, "BindingsText", string.Empty, UiTheme.SmallSize, new Vector2(-120f, -64f), new Vector2(280f, 510f), TextAnchor.UpperLeft, UiTheme.Text);
            CreateBindingButtons(keybindingsPanel);
        }

        private RectTransform CreateSidePanel(string name, Vector2 size, Vector2 offset)
        {
            GameObject sideObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            sideObject.transform.SetParent(panel, false);
            RectTransform rect = sideObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
            sideObject.GetComponent<Image>().color = UiTheme.PanelAlt;
            sideObject.SetActive(false);
            return rect;
        }

        private void CreateSettingsButtons(RectTransform parent)
        {
            CreateLabel(parent, "SettingsTabs", "GAMEPLAY     VIDEO     AUDIO     UI", UiTheme.SmallSize, new Vector2(0f, -250f), new Vector2(440f, 28f), TextAnchor.MiddleCenter, UiTheme.MutedText);
            CreateButton(parent, "SensitivityMinus", "Sensitivity -", new Vector2(-120f, -292f), () => AdjustSensitivity(-0.1f), 170f);
            CreateButton(parent, "SensitivityPlus", "Sensitivity +", new Vector2(120f, -292f), () => AdjustSensitivity(0.1f), 170f);
            CreateButton(parent, "FovMinus", "FOV -", new Vector2(-120f, -340f), () => AdjustFov(-5f), 170f);
            CreateButton(parent, "FovPlus", "FOV +", new Vector2(120f, -340f), () => AdjustFov(5f), 170f);
            CreateButton(parent, "MapSizeMinus", "Map Size -", new Vector2(-120f, -388f), () => AdjustMinimapSize(-20f), 170f);
            CreateButton(parent, "MapSizePlus", "Map Size +", new Vector2(120f, -388f), () => AdjustMinimapSize(20f), 170f);
            CreateButton(parent, "MapOpacityMinus", "Opacity -", new Vector2(-120f, -436f), () => AdjustMinimapOpacity(-0.1f), 170f);
            CreateButton(parent, "MapOpacityPlus", "Opacity +", new Vector2(120f, -436f), () => AdjustMinimapOpacity(0.1f), 170f);
            CreateButton(parent, "MapZoomMinus", "Map Zoom -", new Vector2(-120f, -484f), () => AdjustMinimapZoom(-0.1f), 170f);
            CreateButton(parent, "MapZoomPlus", "Map Zoom +", new Vector2(120f, -484f), () => AdjustMinimapZoom(0.1f), 170f);
            CreateButton(parent, "VolumeMinus", "Volume -", new Vector2(-120f, -532f), () => AdjustMasterVolume(-0.1f), 170f);
            CreateButton(parent, "VolumePlus", "Volume +", new Vector2(120f, -532f), () => AdjustMasterVolume(0.1f), 170f);
            CreateButton(parent, "InvertY", "Invert Y", new Vector2(0f, -580f), ToggleInvertY, 220f);
        }

        private void CreateBindingButtons(RectTransform parent)
        {
            GameplayInputAction[] actions =
            {
                GameplayInputAction.MoveForward,
                GameplayInputAction.MoveBack,
                GameplayInputAction.MoveLeft,
                GameplayInputAction.MoveRight,
                GameplayInputAction.Jump,
                GameplayInputAction.Sprint,
                GameplayInputAction.Interact,
                GameplayInputAction.Fire,
                GameplayInputAction.Reload,
                GameplayInputAction.PistolWhip,
                GameplayInputAction.RunInfo,
                GameplayInputAction.Minimap,
                GameplayInputAction.Pause
            };

            for (int i = 0; i < actions.Length; i++)
            {
                GameplayInputAction action = actions[i];
                CreateButton(parent, $"Bind_{action}", "Rebind", new Vector2(160f, -82f - i * 34f), () => BeginRebind(action), 120f, 28f, UiTheme.SmallSize);
            }

            CreateButton(parent, "ResetBindings", "Reset Defaults", new Vector2(150f, -560f), ResetBindings, 180f);
        }

        private void Resume()
        {
            Hide();
        }

        private void ToggleSettings()
        {
            settingsPanel.gameObject.SetActive(!settingsPanel.gameObject.activeSelf);
            keybindingsPanel.gameObject.SetActive(false);
            pendingRebind = null;
            RefreshSettingsText();
        }

        private void ToggleKeybindings()
        {
            keybindingsPanel.gameObject.SetActive(!keybindingsPanel.gameObject.activeSelf);
            settingsPanel.gameObject.SetActive(false);
            pendingRebind = null;
            RefreshKeybindingsText();
        }

        private void BeginRebind(GameplayInputAction action)
        {
            pendingRebind = action;
            messageText.text = $"Press a key/button for {FormatAction(action)}.";
        }

        private void ResetBindings()
        {
            InputBindingService.ResetToDefaults();
            pendingRebind = null;
            messageText.text = "Keybindings reset.";
            RefreshKeybindingsText();
        }

        private void ReturnToTown()
        {
            if (Time.frameCount - returnConfirmFrame > 180)
            {
                returnConfirmFrame = Time.frameCount;
                messageText.text = "Click Return To Town again to confirm.";
                return;
            }

            Time.timeScale = 1f;
            GameBootstrap bootstrap = GameBootstrap.Instance;
            bootstrap?.RunService?.PrepareTownReturnOnFoot();
            bootstrap?.SceneFlowService?.SetPendingTownHubLoadReason(TownHubLoadReason.DungeonEntranceReturn);
            bootstrap?.SceneFlowService?.LoadScene(GameSceneId.TownHub);
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
                $"Mouse Sensitivity    {settings.mouseSensitivity:0.0}\n" +
                $"FOV                  {settings.fov:0}\n" +
                $"Master Volume        {settings.masterVolume:0.00}\n" +
                $"SFX Volume           {settings.sfxVolume:0.00}\n" +
                $"Music Volume         {settings.musicVolume:0.00}\n" +
                $"Invert Y             {(settings.invertY ? "On" : "Off")}\n" +
                $"Crosshair Size       {settings.crosshairSize:0}\n" +
                $"Minimap Size         {settings.minimapSize:0}\n" +
                $"Minimap Opacity      {settings.minimapOpacity:0.00}\n" +
                $"Minimap Zoom         {settings.minimapZoom:0.00}";
        }

        private void RefreshKeybindingsText()
        {
            if (keybindingsText == null)
            {
                return;
            }

            keybindingsText.text =
                $"{FormatAction(GameplayInputAction.MoveForward),-18} {InputBindingService.GetDisplay(GameplayInputAction.MoveForward)}\n" +
                $"{FormatAction(GameplayInputAction.MoveBack),-18} {InputBindingService.GetDisplay(GameplayInputAction.MoveBack)}\n" +
                $"{FormatAction(GameplayInputAction.MoveLeft),-18} {InputBindingService.GetDisplay(GameplayInputAction.MoveLeft)}\n" +
                $"{FormatAction(GameplayInputAction.MoveRight),-18} {InputBindingService.GetDisplay(GameplayInputAction.MoveRight)}\n" +
                $"{FormatAction(GameplayInputAction.Jump),-18} {InputBindingService.GetDisplay(GameplayInputAction.Jump)}\n" +
                $"{FormatAction(GameplayInputAction.Sprint),-18} {InputBindingService.GetDisplay(GameplayInputAction.Sprint)}\n" +
                $"{FormatAction(GameplayInputAction.Interact),-18} {InputBindingService.GetDisplay(GameplayInputAction.Interact)}\n" +
                $"{FormatAction(GameplayInputAction.Fire),-18} {InputBindingService.GetDisplay(GameplayInputAction.Fire)}\n" +
                $"{FormatAction(GameplayInputAction.Reload),-18} {InputBindingService.GetDisplay(GameplayInputAction.Reload)}\n" +
                $"{FormatAction(GameplayInputAction.PistolWhip),-18} {InputBindingService.GetDisplay(GameplayInputAction.PistolWhip)}\n" +
                $"{FormatAction(GameplayInputAction.RunInfo),-18} {InputBindingService.GetDisplay(GameplayInputAction.RunInfo)}\n" +
                $"{FormatAction(GameplayInputAction.Minimap),-18} {InputBindingService.GetDisplay(GameplayInputAction.Minimap)}\n" +
                $"{FormatAction(GameplayInputAction.Pause),-18} {InputBindingService.GetDisplay(GameplayInputAction.Pause)}";
        }

        private static string FormatAction(GameplayInputAction action)
        {
            return action switch
            {
                GameplayInputAction.MoveForward => "Move Forward",
                GameplayInputAction.MoveBack => "Move Back",
                GameplayInputAction.MoveLeft => "Move Left",
                GameplayInputAction.MoveRight => "Move Right",
                GameplayInputAction.PistolWhip => "Pistol Whip",
                GameplayInputAction.RunInfo => "Run Info",
                _ => action.ToString()
            };
        }

        private static Text CreateLabel(Transform parent, string name, string label, int fontSize, Vector2 position, Vector2 size, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            UiTheme.StyleText(text, fontSize, alignment, color);
            text.text = label;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action, float width = 240f, float height = 40f, int fontSize = UiTheme.BodySize)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(action);
            UiTheme.StyleButton(button);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = position;
            Text text = CreateLabel(buttonObject.transform, "Label", label, fontSize, Vector2.zero, rect.sizeDelta, TextAnchor.MiddleCenter, UiTheme.Text);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            return button;
        }
    }
}
