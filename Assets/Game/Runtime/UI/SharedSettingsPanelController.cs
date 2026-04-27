using System;
using FrontierDepths.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class SharedSettingsPanelController : MonoBehaviour
    {
        private readonly System.Collections.Generic.List<BindingRow> bindingRows = new System.Collections.Generic.List<BindingRow>();
        private Text titleText;
        private Text settingsText;
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
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameplayInputAction action = pendingRebind.Value;
                pendingRebind = null;
                onMessage?.Invoke($"{FormatAction(action)} rebind cancelled.");
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

            CreateBindingRows(keybindingsGroup);

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

        private void CreateBindingRows(RectTransform parent)
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
                GameplayInputAction.Inventory,
                GameplayInputAction.Dash,
                GameplayInputAction.EquipPrimary,
                GameplayInputAction.EquipSecondary,
                GameplayInputAction.RunInfo,
                GameplayInputAction.Minimap,
                GameplayInputAction.Pause
            };

            bindingRows.Clear();
            for (int i = 0; i < actions.Length; i++)
            {
                GameplayInputAction action = actions[i];
                RectTransform row = CreateRow(parent, $"BindingRow_{action}", new Vector2(0f, -16f - i * 28f));
                CreateLabel(row, "Action", FormatAction(action), UiTheme.SmallSize, new Vector2(-170f, 0f), new Vector2(180f, 26f), TextAnchor.MiddleLeft, UiTheme.Text, out Text actionText);
                CreateLabel(row, "Binding", InputBindingService.GetDisplay(action), UiTheme.SmallSize, new Vector2(34f, 0f), new Vector2(190f, 26f), TextAnchor.MiddleLeft, UiTheme.Accent, out Text bindingText);
                Button button = CreateButton(row, $"Rebind_{action}", "Rebind", new Vector2(194f, 0f), () => BeginRebind(action), 110f, 24f, UiTheme.SmallSize);
                bindingRows.Add(new BindingRow(action, row, bindingText, button));
            }

            CreateButton(parent, "ResetBindings", "Reset Defaults", new Vector2(150f, -492f), ResetBindings, 180f, 30f, UiTheme.SmallSize);
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
            for (int i = 0; i < bindingRows.Count; i++)
            {
                BindingRow row = bindingRows[i];
                bool waiting = pendingRebind.HasValue && pendingRebind.Value == row.action;
                row.bindingText.text = waiting ? "Press key/button..." : InputBindingService.GetDisplay(row.action);
                row.button.GetComponentInChildren<Text>().text = waiting ? "Cancel Esc" : "Rebind";
                Image image = row.row.GetComponent<Image>();
                if (image != null)
                {
                    image.color = waiting ? new Color(0.36f, 0.26f, 0.12f, 0.92f) : new Color(0f, 0f, 0f, 0.18f);
                }
            }
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
                GameplayInputAction.EquipPrimary => "Equip Slot 1",
                GameplayInputAction.EquipSecondary => "Equip Slot 2",
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

        private static RectTransform CreateRow(Transform parent, string name, Vector2 position)
        {
            GameObject rowObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            rowObject.transform.SetParent(parent, false);
            RectTransform rect = rowObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(500f, 26f);
            rect.anchoredPosition = position;
            rowObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.18f);
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

        private readonly struct BindingRow
        {
            public BindingRow(GameplayInputAction action, RectTransform row, Text bindingText, Button button)
            {
                this.action = action;
                this.row = row;
                this.bindingText = bindingText;
                this.button = button;
            }

            public readonly GameplayInputAction action;
            public readonly RectTransform row;
            public readonly Text bindingText;
            public readonly Button button;
        }
    }
}
