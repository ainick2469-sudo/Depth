using System;
using FrontierDepths.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class SharedSettingsPanelController : MonoBehaviour
    {
        private Text titleText;
        private Text settingsText;
        private Text keybindingsText;
        private RectTransform settingsGroup;
        private RectTransform keybindingsGroup;
        private Action<string> onMessage;
        private Func<FirstPersonController> playerProvider;
        private GameplayInputAction? pendingRebind;

        public bool IsShowingKeybindings => keybindingsGroup != null && keybindingsGroup.gameObject.activeSelf;

        private void Update()
        {
            if (!pendingRebind.HasValue)
            {
                return;
            }

            if (InputBindingService.TryReadPressedCandidate(out KeyCode key))
            {
                GameplayInputAction action = pendingRebind.Value;
                InputBindingService.SetPrimaryBinding(action, key);
                pendingRebind = null;
                onMessage?.Invoke($"{FormatAction(action)} rebound to {InputBindingService.GetDisplay(action)}.");
                Refresh();
            }
        }

        public void Build(string title, Action<string> messageCallback, Func<FirstPersonController> provider, Action closeCallback)
        {
            if (titleText != null)
            {
                return;
            }

            onMessage = messageCallback;
            playerProvider = provider;

            RectTransform root = transform as RectTransform;
            CreateLabel(root, "SharedSettingsTitle", title, UiTheme.HeaderSize, new Vector2(0f, -22f), new Vector2(500f, 34f), TextAnchor.MiddleCenter, UiTheme.Accent, out titleText);
            CreateButton(root, "SettingsTab", "Settings", new Vector2(-92f, -64f), ShowSettings, 160f, 32f, UiTheme.SmallSize);
            CreateButton(root, "KeybindingsTab", "Keybindings", new Vector2(92f, -64f), ShowKeybindings, 160f, 32f, UiTheme.SmallSize);

            settingsGroup = CreateGroup(root, "SharedSettingsRows");
            keybindingsGroup = CreateGroup(root, "SharedKeybindingRows");

            CreateLabel(settingsGroup, "SettingsText", string.Empty, UiTheme.SmallSize, new Vector2(0f, -16f), new Vector2(480f, 205f), TextAnchor.UpperLeft, UiTheme.Text, out settingsText);
            CreateSettingsButtons(settingsGroup);

            CreateLabel(keybindingsGroup, "KeybindingsText", string.Empty, UiTheme.SmallSize, new Vector2(-112f, -16f), new Vector2(270f, 450f), TextAnchor.UpperLeft, UiTheme.Text, out keybindingsText);
            CreateBindingButtons(keybindingsGroup);

            if (closeCallback != null)
            {
                CreateButton(root, "CloseSharedSettings", "Close", new Vector2(0f, -600f), () => closeCallback(), 180f, 36f, UiTheme.SmallSize);
            }

            ShowSettings();
        }

        public void ShowSettings()
        {
            pendingRebind = null;
            if (settingsGroup != null) settingsGroup.gameObject.SetActive(true);
            if (keybindingsGroup != null) keybindingsGroup.gameObject.SetActive(false);
            Refresh();
        }

        public void ShowKeybindings()
        {
            pendingRebind = null;
            if (settingsGroup != null) settingsGroup.gameObject.SetActive(false);
            if (keybindingsGroup != null) keybindingsGroup.gameObject.SetActive(true);
            Refresh();
        }

        public void Refresh()
        {
            RefreshSettingsText();
            RefreshKeybindingsText();
        }

        private void CreateSettingsButtons(RectTransform parent)
        {
            float y = -242f;
            CreateButton(parent, "SensitivityMinus", "Sensitivity -", new Vector2(-122f, y), () => Adjust(s => s.mouseSensitivity -= 0.1f), 170f, 30f, UiTheme.SmallSize);
            CreateButton(parent, "SensitivityPlus", "Sensitivity +", new Vector2(122f, y), () => Adjust(s => s.mouseSensitivity += 0.1f), 170f, 30f, UiTheme.SmallSize);
            y -= 38f;
            CreateButton(parent, "FovMinus", "FOV -", new Vector2(-122f, y), () => Adjust(s => s.fov -= 5f), 170f, 30f, UiTheme.SmallSize);
            CreateButton(parent, "FovPlus", "FOV +", new Vector2(122f, y), () => Adjust(s => s.fov += 5f), 170f, 30f, UiTheme.SmallSize);
            y -= 38f;
            CreateButton(parent, "MasterVolumeMinus", "Master -", new Vector2(-122f, y), () => Adjust(s => s.masterVolume -= 0.1f), 170f, 30f, UiTheme.SmallSize);
            CreateButton(parent, "MasterVolumePlus", "Master +", new Vector2(122f, y), () => Adjust(s => s.masterVolume += 0.1f), 170f, 30f, UiTheme.SmallSize);
            y -= 38f;
            CreateButton(parent, "SfxVolumeMinus", "SFX -", new Vector2(-122f, y), () => Adjust(s => s.sfxVolume -= 0.1f), 170f, 30f, UiTheme.SmallSize);
            CreateButton(parent, "SfxVolumePlus", "SFX +", new Vector2(122f, y), () => Adjust(s => s.sfxVolume += 0.1f), 170f, 30f, UiTheme.SmallSize);
            y -= 38f;
            CreateButton(parent, "MusicVolumeMinus", "Music -", new Vector2(-122f, y), () => Adjust(s => s.musicVolume -= 0.1f), 170f, 30f, UiTheme.SmallSize);
            CreateButton(parent, "MusicVolumePlus", "Music +", new Vector2(122f, y), () => Adjust(s => s.musicVolume += 0.1f), 170f, 30f, UiTheme.SmallSize);
            y -= 38f;
            CreateButton(parent, "CrosshairMinus", "Crosshair -", new Vector2(-122f, y), () => Adjust(s => s.crosshairSize -= 2f), 170f, 30f, UiTheme.SmallSize);
            CreateButton(parent, "CrosshairPlus", "Crosshair +", new Vector2(122f, y), () => Adjust(s => s.crosshairSize += 2f), 170f, 30f, UiTheme.SmallSize);
            y -= 38f;
            CreateButton(parent, "MapSizeMinus", "Map Size -", new Vector2(-122f, y), () => Adjust(s => s.minimapSize -= 20f), 170f, 30f, UiTheme.SmallSize);
            CreateButton(parent, "MapSizePlus", "Map Size +", new Vector2(122f, y), () => Adjust(s => s.minimapSize += 20f), 170f, 30f, UiTheme.SmallSize);
            y -= 38f;
            CreateButton(parent, "MapOpacityMinus", "Opacity -", new Vector2(-122f, y), () => Adjust(s => s.minimapOpacity -= 0.1f), 170f, 30f, UiTheme.SmallSize);
            CreateButton(parent, "MapOpacityPlus", "Opacity +", new Vector2(122f, y), () => Adjust(s => s.minimapOpacity += 0.1f), 170f, 30f, UiTheme.SmallSize);
            y -= 38f;
            CreateButton(parent, "MapZoomMinus", "Map Zoom -", new Vector2(-122f, y), () => Adjust(s => s.minimapZoom -= 0.1f), 170f, 30f, UiTheme.SmallSize);
            CreateButton(parent, "MapZoomPlus", "Map Zoom +", new Vector2(122f, y), () => Adjust(s => s.minimapZoom += 0.1f), 170f, 30f, UiTheme.SmallSize);
            y -= 38f;
            CreateButton(parent, "InvertY", "Toggle Invert Y", new Vector2(0f, y), () => Adjust(s => s.invertY = !s.invertY), 220f, 30f, UiTheme.SmallSize);
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
                CreateButton(parent, $"Rebind_{action}", "Rebind", new Vector2(160f, -34f - i * 32f), () => BeginRebind(action), 120f, 26f, UiTheme.SmallSize);
            }

            CreateButton(parent, "ResetBindings", "Reset Defaults", new Vector2(150f, -472f), ResetBindings, 180f, 30f, UiTheme.SmallSize);
        }

        private void BeginRebind(GameplayInputAction action)
        {
            pendingRebind = action;
            onMessage?.Invoke($"Press a key/button for {FormatAction(action)}.");
        }

        private void ResetBindings()
        {
            InputBindingService.ResetToDefaults();
            pendingRebind = null;
            onMessage?.Invoke("Keybindings reset.");
            Refresh();
        }

        private void Adjust(Action<GameSettingsState> change)
        {
            GameSettingsState settings = GameSettingsService.Current;
            change?.Invoke(settings);
            settings.Clamp();
            GameSettingsService.Save(settings);
            GameSettingsService.ApplyRuntime(settings);
            FirstPersonController player = playerProvider?.Invoke() ?? FindAnyObjectByType<FirstPersonController>();
            player?.ApplyLookSettings(settings.mouseSensitivity, settings.invertY, settings.fov);
            DungeonMinimapController minimap = FindAnyObjectByType<DungeonMinimapController>();
            if (minimap != null)
            {
                minimap.SetSize(settings.minimapSize);
                minimap.SetOpacity(settings.minimapOpacity);
                minimap.SetZoom(settings.minimapZoom);
            }

            Refresh();
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

        private static RectTransform CreateGroup(Transform parent, string name)
        {
            GameObject group = new GameObject(name, typeof(RectTransform));
            group.transform.SetParent(parent, false);
            RectTransform rect = group.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(520f, 520f);
            rect.anchoredPosition = new Vector2(0f, -88f);
            return rect;
        }

        private static Text CreateLabel(Transform parent, string name, string label, int fontSize, Vector2 position, Vector2 size, TextAnchor alignment, Color color, out Text text)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            text = textObject.GetComponent<Text>();
            UiTheme.StyleText(text, fontSize, alignment, color);
            text.text = label;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action, float width, float height, int fontSize)
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
            CreateLabel(buttonObject.transform, "Label", label, fontSize, Vector2.zero, rect.sizeDelta, TextAnchor.MiddleCenter, UiTheme.Text, out Text text);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            return button;
        }
    }
}
