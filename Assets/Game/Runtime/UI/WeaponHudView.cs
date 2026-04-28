using System.Collections.Generic;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class WeaponHudView : MonoBehaviour
    {
        private const int MaxIndividualPips = 8;

        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private Image backgroundFrameImage;
        [SerializeField] private RectTransform weaponIconRoot;
        [SerializeField] private Image weaponIconImage;
        [SerializeField] private Text weaponIconLabel;
        [SerializeField] private Text weaponNameText;
        [SerializeField] private Text ammoText;
        [SerializeField] private RectTransform ammoPipContainer;
        [SerializeField] private Text reloadText;
        [SerializeField] private Image hitMarkerImage;

        private readonly List<Image> ammoPips = new List<Image>();
        private PlayerWeaponController weapon;
        private float nextResolveTime;
        private float nextPollingRefreshTime;
        private float hitMarkerVisibleUntil;
        private string lastWeaponId = string.Empty;
        private int lastMagazineSize = -1;
        private int lastCurrentAmmo = -1;
        private int lastReserveAmmo = -1;
        private bool lastReloading;
        private int lastReloadProgressBucket = -1;
        private Color hitMarkerColor = new Color(1f, 0.95f, 0.62f, 0.95f);
        private Vector2 hitMarkerSize = new Vector2(22f, 22f);

        internal int AmmoPipCountForTests => ammoPips.Count;
        internal int FilledAmmoPipCountForTests => CountFilledPips();
        internal string AmmoTextForTests => ammoText != null ? ammoText.text : string.Empty;
        internal string WeaponNameTextForTests => weaponNameText != null ? weaponNameText.text : string.Empty;
        internal bool HasPanelRootForTests => panelRoot != null;
        internal bool HasBackgroundFrameForTests => backgroundFrameImage != null;
        internal bool HasAmmoPipContainerForTests => ammoPipContainer != null;
        internal bool IsIconFallbackVisibleForTests => weaponIconLabel != null && weaponIconLabel.enabled;

        private void Awake()
        {
            EnsureHudElements();
            HideWeaponHud();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            ResolveWeapon();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnsubscribeWeapon();
        }

        private void Update()
        {
            if (weapon == null && Time.unscaledTime >= nextResolveTime)
            {
                nextResolveTime = Time.unscaledTime + 0.5f;
                ResolveWeapon();
            }

            if (weapon == null)
            {
                HideWeaponHud();
                UpdateHitMarker();
                return;
            }

            int reloadBucket = weapon.IsReloading ? Mathf.RoundToInt(weapon.ReloadProgress * 100f) : -1;
            bool stateChanged =
                lastWeaponId != weapon.WeaponId ||
                lastMagazineSize != weapon.MagazineSize ||
                lastCurrentAmmo != weapon.CurrentAmmo ||
                lastReserveAmmo != weapon.ReserveAmmo ||
                lastReloading != weapon.IsReloading ||
                lastReloadProgressBucket != reloadBucket;

            if (stateChanged || Time.unscaledTime >= nextPollingRefreshTime)
            {
                nextPollingRefreshTime = Time.unscaledTime + 0.12f;
                RefreshFromWeaponState();
            }

            UpdateHitMarker();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureHudElements();
            ResolveWeapon();
        }

        private void ResolveWeapon()
        {
            PlayerWeaponController found = FindAnyObjectByType<PlayerWeaponController>();
            if (found == weapon)
            {
                RefreshFromWeaponState();
                return;
            }

            UnsubscribeWeapon();
            weapon = found;
            SubscribeWeapon();
            RefreshFromWeaponState();
        }

        private void SubscribeWeapon()
        {
            if (weapon == null)
            {
                return;
            }

            weapon.HitFeedbackReceived += HandleHitFeedbackReceived;
            weapon.WeaponFired += HandleWeaponStateEvent;
            weapon.ReloadStarted += HandleWeaponStateEvent;
            weapon.ReloadFinished += HandleWeaponStateEvent;
            weapon.DryFired += HandleWeaponStateEvent;
            weapon.WeaponEquipped += HandleWeaponStateEvent;
        }

        private void UnsubscribeWeapon()
        {
            if (weapon != null)
            {
                weapon.HitFeedbackReceived -= HandleHitFeedbackReceived;
                weapon.WeaponFired -= HandleWeaponStateEvent;
                weapon.ReloadStarted -= HandleWeaponStateEvent;
                weapon.ReloadFinished -= HandleWeaponStateEvent;
                weapon.DryFired -= HandleWeaponStateEvent;
                weapon.WeaponEquipped -= HandleWeaponStateEvent;
            }

            weapon = null;
        }

        private void HandleWeaponStateEvent(PlayerWeaponController _)
        {
            RefreshFromWeaponState();
        }

        internal void SetWeaponForTests(PlayerWeaponController testWeapon)
        {
            if (testWeapon == weapon)
            {
                return;
            }

            UnsubscribeWeapon();
            weapon = testWeapon;
            SubscribeWeapon();
            RefreshFromWeaponState();
        }

        internal void RefreshFromWeaponStateForTests()
        {
            RefreshFromWeaponState();
        }

        internal void ConfigureAmmoPipsForTests(int magazineSize)
        {
            ConfigureAmmoPips(magazineSize);
        }

        private void RefreshFromWeaponState()
        {
            EnsureHudElements();
            if (weapon == null)
            {
                HideWeaponHud();
                return;
            }

            ShowWeaponHud();
            lastWeaponId = weapon.WeaponId;
            lastMagazineSize = Mathf.Max(1, weapon.MagazineSize);
            lastCurrentAmmo = Mathf.Clamp(weapon.CurrentAmmo, 0, lastMagazineSize);
            lastReserveAmmo = Mathf.Max(0, weapon.ReserveAmmo);
            lastReloading = weapon.IsReloading;
            lastReloadProgressBucket = weapon.IsReloading ? Mathf.RoundToInt(weapon.ReloadProgress * 100f) : -1;

            if (weaponNameText != null)
            {
                weaponNameText.text = weapon.WeaponName;
            }

            if (ammoText != null)
            {
                ammoText.text = $"{lastCurrentAmmo} / {lastReserveAmmo}";
            }

            ConfigureWeaponIcon(lastWeaponId);
            ConfigureAmmoPips(lastMagazineSize);
            UpdateAmmoPips(lastCurrentAmmo, lastMagazineSize);

            if (reloadText != null)
            {
                reloadText.enabled = weapon.IsReloading;
                reloadText.text = weapon.IsReloading
                    ? $"RELOADING {lastReloadProgressBucket}%"
                    : string.Empty;
            }
        }

        private void ConfigureWeaponIcon(string weaponId)
        {
            if (weaponIconImage == null || weaponIconLabel == null)
            {
                return;
            }

            Sprite sprite = HudSpriteCatalog.TryGetWeaponIcon(weaponId);
            if (sprite != null)
            {
                weaponIconImage.sprite = sprite;
                weaponIconImage.type = Image.Type.Simple;
                weaponIconImage.preserveAspect = true;
                weaponIconImage.color = Color.white;
                weaponIconLabel.enabled = false;
                return;
            }

            weaponIconImage.sprite = null;
            weaponIconImage.color = new Color(0.12f, 0.1f, 0.07f, 0.78f);
            weaponIconLabel.enabled = true;
            weaponIconLabel.text = GetWeaponIconFallbackLabel(weaponId);
        }

        private static string GetWeaponIconFallbackLabel(string weaponId)
        {
            return weaponId switch
            {
                WeaponCatalog.FrontierRifleId => "RIF",
                WeaponCatalog.FrontierRevolverId => "REV",
                _ => "WEPN"
            };
        }

        private void ConfigureAmmoPips(int magazineSize)
        {
            int slotCount = magazineSize <= MaxIndividualPips ? Mathf.Max(1, magazineSize) : MaxIndividualPips;
            if (ammoPips.Count == slotCount)
            {
                return;
            }

            ClearAmmoPips();
            Font font = UiTheme.RuntimeFont;
            Sprite pipSprite = HudSpriteCatalog.TryGetAmmoPip();
            for (int i = 0; i < slotCount; i++)
            {
                GameObject pipObject = new GameObject($"AmmoPip_{i}", typeof(RectTransform), typeof(Image));
                pipObject.transform.SetParent(ammoPipContainer, false);
                Image pip = pipObject.GetComponent<Image>();
                pip.sprite = pipSprite;
                pip.type = Image.Type.Simple;
                pip.preserveAspect = pipSprite != null;
                pip.raycastTarget = false;
                RectTransform rect = pip.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(HudLayoutConstants.AmmoPipSize, HudLayoutConstants.AmmoPipSize);
                rect.anchoredPosition = new Vector2(i * (HudLayoutConstants.AmmoPipSize + HudLayoutConstants.AmmoPipSpacing), 0f);
                ammoPips.Add(pip);
            }

            if (magazineSize > MaxIndividualPips && ammoPipContainer.Find("AmmoPipCondensedLabel") == null)
            {
                GameObject labelObject = new GameObject("AmmoPipCondensedLabel", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(ammoPipContainer, false);
                Text label = labelObject.GetComponent<Text>();
                label.font = font;
                label.fontSize = 12;
                label.alignment = TextAnchor.MiddleLeft;
                label.color = new Color(UiTheme.MutedText.r, UiTheme.MutedText.g, UiTheme.MutedText.b, 0.88f);
                label.text = "SEG";
                label.raycastTarget = false;
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 0.5f);
                labelRect.pivot = new Vector2(0f, 0.5f);
                labelRect.sizeDelta = new Vector2(36f, 18f);
                labelRect.anchoredPosition = new Vector2(slotCount * (HudLayoutConstants.AmmoPipSize + HudLayoutConstants.AmmoPipSpacing) + 4f, 0f);
            }
        }

        private void UpdateAmmoPips(int currentMagazineAmmo, int magazineSize)
        {
            if (ammoPips.Count == 0)
            {
                return;
            }

            int filledSlots = magazineSize <= MaxIndividualPips
                ? Mathf.Clamp(currentMagazineAmmo, 0, ammoPips.Count)
                : Mathf.CeilToInt(Mathf.Clamp01(currentMagazineAmmo / (float)Mathf.Max(1, magazineSize)) * ammoPips.Count);
            for (int i = 0; i < ammoPips.Count; i++)
            {
                Image pip = ammoPips[i];
                bool filled = i < filledSlots;
                pip.enabled = true;
                pip.color = filled
                    ? new Color(0.94f, 0.78f, 0.36f, 0.98f)
                    : new Color(0.18f, 0.17f, 0.14f, 0.62f);
            }
        }

        private void ClearAmmoPips()
        {
            if (ammoPipContainer != null)
            {
                for (int i = ammoPipContainer.childCount - 1; i >= 0; i--)
                {
                    DestroyUiObject(ammoPipContainer.GetChild(i).gameObject);
                }
            }

            ammoPips.Clear();
        }

        private void HandleHitFeedbackReceived(WeaponHitFeedback feedback)
        {
            if (!feedback.IsDamageHit)
            {
                return;
            }

            hitMarkerVisibleUntil = Time.unscaledTime + 0.18f;
            hitMarkerColor = feedback.isChain
                ? new Color(0.42f, 0.85f, 1f, 0.98f)
                : feedback.kind switch
            {
                WeaponHitFeedbackKind.Reduced => new Color(1f, 0.56f, 0.18f, 0.98f),
                WeaponHitFeedbackKind.Kill => new Color(1f, 0.22f, 0.18f, 1f),
                _ => new Color(1f, 0.95f, 0.62f, 0.95f)
            };
            hitMarkerSize = feedback.kind == WeaponHitFeedbackKind.Kill || feedback.isCritical
                ? new Vector2(34f, 34f)
                : feedback.isChain
                    ? new Vector2(30f, 30f)
                : new Vector2(22f, 22f);
        }

        private void UpdateHitMarker()
        {
            if (hitMarkerImage == null)
            {
                return;
            }

            bool visible = Time.unscaledTime < hitMarkerVisibleUntil;
            hitMarkerImage.enabled = visible;
            if (visible)
            {
                float alpha = Mathf.InverseLerp(hitMarkerVisibleUntil - 0.18f, hitMarkerVisibleUntil, Time.unscaledTime);
                Color color = hitMarkerColor;
                color.a = Mathf.Lerp(hitMarkerColor.a, 0.2f, alpha);
                hitMarkerImage.color = color;
                hitMarkerImage.rectTransform.sizeDelta = hitMarkerSize;
            }
        }

        private void EnsureHudElements()
        {
            Font font = UiTheme.RuntimeFont;
            panelRoot ??= FindNamedComponent<RectTransform>("WeaponPanelRoot");
            backgroundFrameImage ??= FindNamedComponent<Image>("BackgroundFrameImage");
            weaponIconRoot ??= FindNamedComponent<RectTransform>("WeaponIconRoot");
            weaponIconImage ??= FindNamedComponent<Image>("WeaponIconImage");
            weaponIconLabel ??= FindNamedComponent<Text>("WeaponIconLabel");
            weaponNameText ??= FindNamedComponent<Text>("WeaponNameText");
            ammoText ??= FindNamedComponent<Text>("AmmoText");
            ammoPipContainer ??= FindNamedComponent<RectTransform>("AmmoPipContainer");
            reloadText ??= FindNamedComponent<Text>("ReloadStatusText");
            hitMarkerImage ??= FindNamedComponent<Image>("WeaponHitMarker");

            Transform panelParent = HudLayoutConstants.GetZoneOrRoot(transform, HudLayoutConstants.BottomRightZoneName);
            if (panelRoot == null)
            {
                GameObject panelObject = new GameObject("WeaponPanelRoot", typeof(RectTransform));
                panelObject.transform.SetParent(panelParent != null ? panelParent : transform, false);
                panelRoot = panelObject.GetComponent<RectTransform>();
            }

            panelRoot.anchorMin = panelRoot.anchorMax = new Vector2(1f, 0f);
            panelRoot.pivot = new Vector2(1f, 0f);
            panelRoot.sizeDelta = new Vector2(HudLayoutConstants.WeaponPanelWidth, HudLayoutConstants.WeaponPanelHeight);
            panelRoot.anchoredPosition = new Vector2(-HudLayoutConstants.HudMargin, HudLayoutConstants.HudMargin);

            if (backgroundFrameImage == null)
            {
                GameObject frameObject = new GameObject("BackgroundFrameImage", typeof(RectTransform), typeof(Image));
                frameObject.transform.SetParent(panelRoot, false);
                backgroundFrameImage = frameObject.GetComponent<Image>();
            }

            RectTransform frameRect = backgroundFrameImage.rectTransform;
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            backgroundFrameImage.raycastTarget = false;
            backgroundFrameImage.transform.SetAsFirstSibling();

            if (weaponNameText == null)
            {
                weaponNameText = CreateText("WeaponNameText", panelRoot, font, 19, TextAnchor.MiddleCenter);
            }

            RectTransform nameRect = weaponNameText.rectTransform;
            nameRect.anchorMin = new Vector2(0.08f, 1f);
            nameRect.anchorMax = new Vector2(0.92f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.sizeDelta = new Vector2(0f, 30f);
            nameRect.anchoredPosition = new Vector2(0f, -18f);

            if (weaponIconRoot == null)
            {
                GameObject iconRootObject = new GameObject("WeaponIconRoot", typeof(RectTransform), typeof(Image));
                iconRootObject.transform.SetParent(panelRoot, false);
                weaponIconRoot = iconRootObject.GetComponent<RectTransform>();
                Image iconBackground = iconRootObject.GetComponent<Image>();
                iconBackground.color = new Color(0.02f, 0.02f, 0.018f, 0.58f);
                iconBackground.raycastTarget = false;
            }

            weaponIconRoot.anchorMin = weaponIconRoot.anchorMax = new Vector2(0f, 0f);
            weaponIconRoot.pivot = new Vector2(0f, 0f);
            weaponIconRoot.sizeDelta = new Vector2(96f, 76f);
            weaponIconRoot.anchoredPosition = new Vector2(32f, 54f);

            if (weaponIconImage == null)
            {
                GameObject iconObject = new GameObject("WeaponIconImage", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(weaponIconRoot, false);
                weaponIconImage = iconObject.GetComponent<Image>();
            }

            RectTransform iconRect = weaponIconImage.rectTransform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(8f, 8f);
            iconRect.offsetMax = new Vector2(-8f, -8f);
            weaponIconImage.raycastTarget = false;

            if (weaponIconLabel == null)
            {
                weaponIconLabel = CreateText("WeaponIconLabel", weaponIconRoot, font, 18, TextAnchor.MiddleCenter);
            }

            RectTransform iconLabelRect = weaponIconLabel.rectTransform;
            iconLabelRect.anchorMin = Vector2.zero;
            iconLabelRect.anchorMax = Vector2.one;
            iconLabelRect.offsetMin = Vector2.zero;
            iconLabelRect.offsetMax = Vector2.zero;
            weaponIconLabel.color = new Color(0.98f, 0.82f, 0.36f, 0.94f);

            if (ammoText == null)
            {
                ammoText = CreateText("AmmoText", panelRoot, font, 32, TextAnchor.MiddleRight);
            }

            RectTransform ammoRect = ammoText.rectTransform;
            ammoRect.anchorMin = ammoRect.anchorMax = new Vector2(1f, 0f);
            ammoRect.pivot = new Vector2(1f, 0f);
            ammoRect.sizeDelta = new Vector2(190f, 48f);
            ammoRect.anchoredPosition = new Vector2(-32f, 52f);

            if (ammoPipContainer == null)
            {
                GameObject pipContainerObject = new GameObject("AmmoPipContainer", typeof(RectTransform));
                pipContainerObject.transform.SetParent(panelRoot, false);
                ammoPipContainer = pipContainerObject.GetComponent<RectTransform>();
            }

            ammoPipContainer.anchorMin = ammoPipContainer.anchorMax = new Vector2(0f, 0f);
            ammoPipContainer.pivot = new Vector2(0f, 0.5f);
            ammoPipContainer.sizeDelta = new Vector2(156f, 24f);
            ammoPipContainer.anchoredPosition = new Vector2(34f, 31f);

            if (reloadText == null)
            {
                reloadText = CreateText("ReloadStatusText", panelRoot, font, 15, TextAnchor.MiddleRight);
            }

            RectTransform reloadRect = reloadText.rectTransform;
            reloadRect.anchorMin = reloadRect.anchorMax = new Vector2(1f, 0f);
            reloadRect.pivot = new Vector2(1f, 0f);
            reloadRect.sizeDelta = new Vector2(210f, 24f);
            reloadRect.anchoredPosition = new Vector2(-32f, 27f);

            if (hitMarkerImage == null)
            {
                Transform centerParent = HudLayoutConstants.GetZoneOrRoot(transform, HudLayoutConstants.CenterZoneName);
                GameObject markerObject = new GameObject("WeaponHitMarker", typeof(RectTransform), typeof(Image));
                markerObject.transform.SetParent(centerParent != null ? centerParent : transform, false);
                hitMarkerImage = markerObject.GetComponent<Image>();
            }

            RectTransform hitRect = hitMarkerImage.rectTransform;
            hitRect.anchorMin = hitRect.anchorMax = new Vector2(0.5f, 0.5f);
            hitRect.pivot = new Vector2(0.5f, 0.5f);
            hitRect.sizeDelta = new Vector2(22f, 22f);
            hitRect.anchoredPosition = Vector2.zero;
            hitMarkerImage.raycastTarget = false;
            hitMarkerImage.color = new Color(1f, 0.95f, 0.62f, 0.95f);
            hitMarkerImage.enabled = false;

            ConfigureText(weaponNameText);
            ConfigureText(ammoText);
            ConfigureText(reloadText);
            ConfigureText(weaponIconLabel);
            ConfigurePanelFrame();
        }

        private Text CreateText(string name, Transform parent, Font font, int size, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(0.96f, 0.95f, 0.88f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private void ConfigureText(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.raycastTarget = false;
        }

        private void ConfigurePanelFrame()
        {
            if (backgroundFrameImage == null)
            {
                return;
            }

            Sprite panelSprite = HudSpriteCatalog.TryGetWeaponPanelFrame();
            backgroundFrameImage.sprite = panelSprite;
            backgroundFrameImage.type = Image.Type.Simple;
            backgroundFrameImage.preserveAspect = false;
            backgroundFrameImage.color = panelSprite != null
                ? new Color(1f, 1f, 1f, 0.92f)
                : new Color(UiTheme.Panel.r, UiTheme.Panel.g, UiTheme.Panel.b, 0.82f);
            backgroundFrameImage.enabled = true;
        }

        private void ShowWeaponHud()
        {
            if (panelRoot != null)
            {
                panelRoot.gameObject.SetActive(true);
            }
        }

        private void HideWeaponHud()
        {
            if (panelRoot != null)
            {
                panelRoot.gameObject.SetActive(false);
            }

            if (hitMarkerImage != null)
            {
                hitMarkerImage.enabled = false;
            }
        }

        private int CountFilledPips()
        {
            int count = 0;
            for (int i = 0; i < ammoPips.Count; i++)
            {
                if (ammoPips[i] != null && ammoPips[i].color.a > 0.8f)
                {
                    count++;
                }
            }

            return count;
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

        private static void DestroyUiObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
