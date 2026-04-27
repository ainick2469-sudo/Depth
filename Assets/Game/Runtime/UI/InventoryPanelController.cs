using System.Collections.Generic;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.Progression;
using FrontierDepths.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class InventoryPanelController : MonoBehaviour
    {
        private readonly List<Button> weaponButtons = new List<Button>();
        private RectTransform panel;
        private Text titleText;
        private Text summaryText;
        private Text statsText;
        private FirstPersonController playerController;
        private PlayerWeaponController weaponController;
        private int selectedIndex;

        public bool IsVisible => panel != null && panel.gameObject.activeSelf;

        private void Awake()
        {
            EnsureUi();
            Hide();
        }

        private void Update()
        {
            ResolvePlayer();
            HandleWeaponSlotInput();
            HandleInventoryInput();
        }

        public void Show()
        {
            EnsureUi();
            Refresh();
            panel.gameObject.SetActive(true);
            playerController?.SetUiCaptured(true);
        }

        public void Hide()
        {
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }

            playerController?.SetUiCaptured(false);
        }

        private void HandleInventoryInput()
        {
            if (InputBindingService.GetKeyDown(GameplayInputAction.Inventory))
            {
                if (IsVisible)
                {
                    Hide();
                    return;
                }

                if (CanOpenInventory())
                {
                    Show();
                }
            }

            if (!IsVisible)
            {
                return;
            }

            if (InputBindingService.GetKeyDown(GameplayInputAction.Pause))
            {
                Hide();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                EquipSelected();
            }
        }

        private void HandleWeaponSlotInput()
        {
            if (IsVisible)
            {
                if (InputBindingService.GetKeyDown(GameplayInputAction.EquipPrimary))
                {
                    EquipSlot(1);
                }
                else if (InputBindingService.GetKeyDown(GameplayInputAction.EquipSecondary))
                {
                    EquipSlot(2);
                }

                return;
            }

            if (!CanUseGameplaySwap())
            {
                return;
            }

            if (InputBindingService.GetKeyDown(GameplayInputAction.EquipPrimary))
            {
                EquipSlot(1);
            }
            else if (InputBindingService.GetKeyDown(GameplayInputAction.EquipSecondary))
            {
                EquipSlot(2);
            }
        }

        private bool CanOpenInventory()
        {
            return !DungeonRewardChoiceController.IsRewardChoiceActive &&
                   !TownServicePanelController.IsAnyVisible &&
                   (playerController == null || !playerController.IsManualPauseActive);
        }

        private bool CanUseGameplaySwap()
        {
            return CanOpenInventory() &&
                   (playerController == null || !playerController.IsUiCaptured);
        }

        private void EquipSelected()
        {
            ProfileState profile = GameBootstrap.Instance?.ProfileService?.Current;
            if (profile == null || profile.unlockedWeaponIds == null || profile.unlockedWeaponIds.Count == 0)
            {
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, profile.unlockedWeaponIds.Count - 1);
            EquipWeapon(profile.unlockedWeaponIds[selectedIndex], selectedIndex == 0 ? 1 : 2);
        }

        private void EquipSlot(int slot)
        {
            InventoryService inventory = GameBootstrap.Instance?.InventoryService;
            string weaponId = inventory?.GetWeaponInSlot(slot);
            if (!string.IsNullOrWhiteSpace(weaponId))
            {
                EquipWeapon(weaponId, slot);
            }
        }

        private void EquipWeapon(string weaponId, int slot)
        {
            InventoryService inventory = GameBootstrap.Instance?.InventoryService;
            if (inventory == null || !inventory.EquipWeapon(weaponId, slot))
            {
                return;
            }

            ResolvePlayer();
            weaponController?.EquipWeapon(weaponId);
            Refresh();
        }

        private void Refresh()
        {
            if (panel == null)
            {
                return;
            }

            ProfileState profile = GameBootstrap.Instance?.ProfileService?.Current;
            RunState run = GameBootstrap.Instance?.RunService?.Current;
            if (profile == null)
            {
                return;
            }

            profile.Normalize();
            titleText.text = "Inventory";
            summaryText.text =
                $"Gold: {profile.gold}  |  Reputation: {ReputationService.GetTitle(profile.townReputation)} ({profile.townReputation})  |  Skill Points: {profile.skillPoints}\n" +
                $"Equipped: {WeaponCatalog.GetDisplayName(profile.GetActiveWeaponId())}\n" +
                $"{InputBindingService.GetDisplay(GameplayInputAction.EquipPrimary)} Primary  |  {InputBindingService.GetDisplay(GameplayInputAction.EquipSecondary)} Secondary  |  Enter/Click Equip";

            for (int i = 0; i < weaponButtons.Count; i++)
            {
                Destroy(weaponButtons[i].gameObject);
            }
            weaponButtons.Clear();

            for (int i = 0; i < profile.unlockedWeaponIds.Count; i++)
            {
                int index = i;
                string weaponId = profile.unlockedWeaponIds[i];
                Button button = CreateButton(panel, $"InventoryWeapon_{i}", BuildWeaponButtonLabel(profile, run, weaponId));
                button.onClick.AddListener(() =>
                {
                    selectedIndex = index;
                    EquipWeapon(weaponId, index == 0 ? 1 : 2);
                });
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(330f, 56f);
                rect.anchoredPosition = new Vector2(28f, -138f - i * 64f);
                weaponButtons.Add(button);
            }

            statsText.text = BuildStatsText(profile, run);
        }

        private static string BuildWeaponButtonLabel(ProfileState profile, RunState run, string weaponId)
        {
            string slot = profile.primaryWeaponId == weaponId ? "Primary" : profile.secondaryWeaponId == weaponId ? "Secondary" : "Owned";
            string active = profile.GetActiveWeaponId() == weaponId ? "EQUIPPED" : slot;
            RunWeaponAmmoState ammo = run?.GetWeaponAmmoState(weaponId);
            string ammoText = ammo != null ? $" {ammo.currentMagazineAmmo}/{ammo.reserveAmmo}" : string.Empty;
            return $"{WeaponCatalog.GetDisplayName(weaponId)}\n{active}{ammoText}";
        }

        private static string BuildStatsText(ProfileState profile, RunState run)
        {
            string weaponId = profile.GetActiveWeaponId();
            WeaponCatalog.TryGet(weaponId, out WeaponDefinition definition);
            RunWeaponAmmoState ammo = run?.GetWeaponAmmoState(weaponId) ?? run?.weaponAmmo;
            string stats = definition == null
                ? "No weapon stats available."
                : $"Damage {definition.baseDamage:0.#}\nRate {definition.fireRate:0.##}/s\nMagazine {definition.magazineSize}\nRange {definition.maxRange:0}m\nReload {definition.reloadDuration:0.##}s";
            string ammoText = ammo != null
                ? $"\n\nAmmo\nMagazine {ammo.currentMagazineAmmo}\nReserve {ammo.reserveAmmo}/{ammo.maxReserveAmmo}"
                : string.Empty;
            string upgrades = run != null && run.runUpgrades != null && run.runUpgrades.Count > 0
                ? $"\n\nRun Upgrades\n{FormatUpgrades(run)}"
                : "\n\nRun Upgrades\nNone yet.";
            return $"{stats}{ammoText}{upgrades}";
        }

        private static string FormatUpgrades(RunState run)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < run.runUpgrades.Count; i++)
            {
                builder.AppendLine(RunUpgradeCatalog.BuildOwnedUpgradeSummary(run.runUpgrades[i]));
            }

            return builder.ToString().TrimEnd();
        }

        private void ResolvePlayer()
        {
            playerController ??= FindAnyObjectByType<FirstPersonController>();
            weaponController ??= playerController != null
                ? playerController.GetComponent<PlayerWeaponController>()
                : FindAnyObjectByType<PlayerWeaponController>();
        }

        private void EnsureUi()
        {
            if (panel != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject root = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(transform, false);
            panel = root.GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(760f, 540f);
            panel.anchoredPosition = Vector2.zero;
            root.GetComponent<Image>().color = UiTheme.Panel;

            titleText = CreateText(panel, "Title", font, 32, TextAnchor.MiddleCenter, new Vector2(0f, -24f), new Vector2(700f, 42f), UiTheme.Accent);
            summaryText = CreateText(panel, "Summary", font, 17, TextAnchor.UpperLeft, new Vector2(28f, -70f), new Vector2(700f, 64f), UiTheme.Text);
            statsText = CreateText(panel, "Stats", font, 17, TextAnchor.UpperLeft, new Vector2(392f, -138f), new Vector2(330f, 340f), UiTheme.Text);
        }

        private static Text CreateText(Transform parent, string name, Font font, int size, TextAnchor alignment, Vector2 position, Vector2 sizeDelta, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = position;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = UiTheme.Button;
            Button button = buttonObject.GetComponent<Button>();
            Text text = CreateText(buttonObject.transform, "Label", font, 16, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(300f, 52f), UiTheme.Text);
            text.text = label;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            return button;
        }
    }
}
