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
        [SerializeField] private Text weaponNameText;
        [SerializeField] private Text weaponSubtitleText;
        [SerializeField] private Text ammoText;
        [SerializeField] private RectTransform chamberRoot;
        [SerializeField] private Text reloadText;
        [SerializeField] private Image hitMarkerImage;

        private readonly List<Image> chamberFills = new List<Image>();
        private PlayerWeaponController weapon;
        private float nextResolveTime;
        private float nextPollingRefreshTime;
        private float hitMarkerVisibleUntil;
        private string lastWeaponId = string.Empty;
        private int lastMagazineSize = -1;
        private int lastCurrentAmmo = -1;
        private bool lastReloading;
        private int lastReloadProgressBucket = -1;
        private Color hitMarkerColor = new Color(1f, 0.95f, 0.62f, 0.95f);
        private Vector2 hitMarkerSize = new Vector2(22f, 22f);
        private static readonly RevolverChamberLayout ChamberLayout = RevolverChamberLayout.Default;

        internal int ChamberCountForTests => chamberFills.Count;
        internal int FilledChamberCountForTests => CountFilledChambers();
        internal int AmmoPipCountForTests => ChamberCountForTests;
        internal int FilledAmmoPipCountForTests => FilledChamberCountForTests;
        internal string AmmoTextForTests => ammoText != null ? ammoText.text : string.Empty;
        internal string WeaponNameTextForTests => weaponNameText != null ? weaponNameText.text : string.Empty;
        internal string WeaponSubtitleTextForTests => weaponSubtitleText != null ? weaponSubtitleText.text : string.Empty;
        internal Vector2[] ChamberLocalPositionsForTests => GetChamberLocalPositions();
        internal bool AreChambersInsideRootForTests => AreChambersInsideRoot();
        internal bool HasPanelRootForTests => panelRoot != null;
        internal bool HasBackgroundFrameForTests => backgroundFrameImage != null;
        internal bool HasAmmoPipContainerForTests => false;
        internal bool HasChamberIndicatorForTests => chamberRoot != null;
        internal bool AreChambersParentedToRootForTests => AreChambersParentedToRoot();
        internal bool IsWeaponPanelInsideSafeAreaForTests => IsPanelInsideSafeArea();
        internal bool IsWeaponTextInsidePanelForTests => IsRectInsidePanel(weaponNameText != null ? weaponNameText.rectTransform : null) &&
                                                         IsRectInsidePanel(ammoText != null ? ammoText.rectTransform : null);
        internal bool HasOldAmmoPipStripForTests => FindNamedTransform(transform, "AmmoPipContainer") != null;
        internal bool HasLegacyWeaponIconBlockForTests => FindNamedTransform(transform, "WeaponIconRoot") != null ||
                                                          FindNamedTransform(transform, "WeaponIconImage") != null ||
                                                          FindNamedTransform(transform, "WeaponIconLabel") != null;
        internal bool HasOpaqueChamberBackingForTests => HasOpaqueChamberBacking();
        internal bool IsIconFallbackVisibleForTests => false;
        internal bool UsesAmmoBulletFallbackForTests => chamberFills.Count > 0 && chamberFills[0] != null && chamberFills[0].sprite == null;

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
            ConfigureChambers(WeaponCatalog.FrontierRevolverId, magazineSize);
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
            lastReloading = weapon.IsReloading;
            lastReloadProgressBucket = weapon.IsReloading ? Mathf.RoundToInt(weapon.ReloadProgress * 100f) : -1;

            if (weaponNameText != null)
            {
                weaponNameText.text = GetDisplayWeaponName(lastWeaponId, weapon.WeaponName);
            }

            if (weaponSubtitleText != null)
            {
                weaponSubtitleText.text = lastWeaponId == WeaponCatalog.FrontierRevolverId
                    ? "Gunslinger Sidearm"
                    : "Frontier Longarm";
            }

            if (ammoText != null)
            {
                ammoText.text = $"{lastCurrentAmmo} / {lastMagazineSize}";
            }

            ConfigureChambers(lastWeaponId, lastMagazineSize);
            UpdateChambers(lastCurrentAmmo, lastMagazineSize);

            if (reloadText != null)
            {
                reloadText.enabled = weapon.IsReloading;
                reloadText.text = weapon.IsReloading
                    ? $"RELOADING {lastReloadProgressBucket}%"
                    : string.Empty;
            }
        }

        private static string GetDisplayWeaponName(string weaponId, string weaponName)
        {
            if (weaponId == WeaponCatalog.FrontierRevolverId)
            {
                return "Revolver";
            }

            return string.IsNullOrWhiteSpace(weaponName)
                ? "FRONTIER REVOLVER"
                : weaponName.ToUpperInvariant();
        }

        private void ConfigureChambers(string weaponId, int magazineSize)
        {
            bool revolver = string.Equals(weaponId, WeaponCatalog.FrontierRevolverId, System.StringComparison.Ordinal) && magazineSize == 6;
            int slotCount = revolver ? 6 : 0;
            if (chamberFills.Count == slotCount)
            {
                ApplyChamberLayout();
                return;
            }

            ClearChambers();
            if (chamberRoot != null)
            {
                chamberRoot.gameObject.SetActive(slotCount > 0);
            }

            for (int i = 0; i < slotCount; i++)
            {
                GameObject chamberObject = new GameObject($"CylinderChamber_{i}", typeof(RectTransform), typeof(Image));
                chamberObject.transform.SetParent(chamberRoot, false);
                Image chamber = chamberObject.GetComponent<Image>();
                chamber.sprite = HudRuntimeSpriteFactory.GetFilledCircleSprite();
                chamber.type = Image.Type.Simple;
                chamber.raycastTarget = false;
                RectTransform rect = chamber.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                chamberFills.Add(chamber);
            }

            ApplyChamberLayout();
        }

        private void UpdateChambers(int currentMagazineAmmo, int magazineSize)
        {
            if (chamberFills.Count == 0)
            {
                return;
            }

            int filledSlots = Mathf.Clamp(currentMagazineAmmo, 0, chamberFills.Count);
            for (int i = 0; i < chamberFills.Count; i++)
            {
                Image pip = chamberFills[i];
                bool filled = i < filledSlots;
                pip.enabled = true;
                pip.color = filled
                    ? new Color(1f, 0.76f, 0.28f, 0.98f)
                    : new Color(0.06f, 0.055f, 0.045f, 0.5f);
            }
        }

        private void ClearChambers()
        {
            if (chamberRoot != null)
            {
                for (int i = chamberRoot.childCount - 1; i >= 0; i--)
                {
                    Transform child = chamberRoot.GetChild(i);
                    if (!child.name.StartsWith("CylinderChamber_"))
                    {
                        continue;
                    }

                    DestroyUiObject(child.gameObject);
                }
            }

            chamberFills.Clear();
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
            weaponNameText ??= FindNamedComponent<Text>("WeaponNameText");
            weaponSubtitleText ??= FindNamedComponent<Text>("WeaponSubtitleText");
            ammoText ??= FindNamedComponent<Text>("AmmoText");
            chamberRoot ??= FindNamedComponent<RectTransform>("CylinderChamberRoot");
            Transform oldPipStrip = FindNamedTransform(transform, "AmmoPipContainer");
            if (oldPipStrip != null)
            {
                DestroyUiObject(oldPipStrip.gameObject);
            }
            DestroyLegacyWeaponIconBlock();
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
                weaponNameText = CreateText("WeaponNameText", panelRoot, font, 18, TextAnchor.MiddleCenter);
            }

            RectTransform nameRect = weaponNameText.rectTransform;
            nameRect.anchorMin = new Vector2(0.12f, 1f);
            nameRect.anchorMax = new Vector2(0.7f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.sizeDelta = new Vector2(0f, 24f);
            nameRect.anchoredPosition = new Vector2(0f, -58f);
            weaponNameText.fontSize = 16;
            weaponNameText.alignment = TextAnchor.MiddleCenter;
            weaponNameText.color = new Color(0.96f, 0.86f, 0.55f, 0.98f);

            if (weaponSubtitleText == null)
            {
                weaponSubtitleText = CreateText("WeaponSubtitleText", panelRoot, font, 13, TextAnchor.MiddleCenter);
            }

            RectTransform subtitleRect = weaponSubtitleText.rectTransform;
            subtitleRect.anchorMin = new Vector2(0.12f, 1f);
            subtitleRect.anchorMax = new Vector2(0.7f, 1f);
            subtitleRect.pivot = new Vector2(0.5f, 1f);
            subtitleRect.sizeDelta = new Vector2(0f, 20f);
            subtitleRect.anchoredPosition = new Vector2(0f, -36f);
            weaponSubtitleText.fontSize = 13;
            weaponSubtitleText.alignment = TextAnchor.MiddleCenter;
            weaponSubtitleText.color = new Color(0.82f, 0.72f, 0.5f, 0.92f);

            if (ammoText == null)
            {
                ammoText = CreateText("AmmoText", panelRoot, font, 30, TextAnchor.MiddleRight);
            }

            RectTransform ammoRect = ammoText.rectTransform;
            ammoRect.anchorMin = ammoRect.anchorMax = new Vector2(1f, 0f);
            ammoRect.pivot = new Vector2(1f, 0f);
            ammoRect.sizeDelta = new Vector2(180f, 42f);
            ammoRect.anchoredPosition = new Vector2(-28f, 48f);

            if (chamberRoot == null)
            {
                GameObject chamberRootObject = new GameObject("CylinderChamberRoot", typeof(RectTransform));
                chamberRootObject.transform.SetParent(panelRoot, false);
                chamberRoot = chamberRootObject.GetComponent<RectTransform>();
            }
            RemoveChamberBackingImage();

            chamberRoot.anchorMin = chamberRoot.anchorMax = new Vector2(1f, 0f);
            chamberRoot.pivot = new Vector2(0.5f, 0.5f);
            chamberRoot.sizeDelta = ChamberLayout.rootSize;
            chamberRoot.anchoredPosition = ChamberLayout.rootAnchoredPosition;

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
            ConfigureText(weaponSubtitleText);
            ConfigureText(ammoText);
            ConfigureText(reloadText);
            ConfigurePanelFrame();
            ApplyChamberLayout();
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

        private int CountFilledChambers()
        {
            int count = 0;
            for (int i = 0; i < chamberFills.Count; i++)
            {
                if (chamberFills[i] != null && chamberFills[i].color.a > 0.8f)
                {
                    count++;
                }
            }

            return count;
        }

        private static Vector2 GetChamberLocalPosition(int index)
        {
            int safeIndex = Mathf.Clamp(index, 0, ChamberLayout.localPositions.Length - 1);
            return ChamberLayout.localPositions[safeIndex];
        }

        private void ApplyChamberLayout()
        {
            if (chamberRoot != null)
            {
                chamberRoot.sizeDelta = ChamberLayout.rootSize;
                chamberRoot.anchoredPosition = ChamberLayout.rootAnchoredPosition;
            }

            for (int i = 0; i < chamberFills.Count; i++)
            {
                Image chamber = chamberFills[i];
                if (chamber == null)
                {
                    continue;
                }

                RectTransform rect = chamber.rectTransform;
                rect.sizeDelta = Vector2.one * ChamberLayout.pipSize;
                rect.anchoredPosition = GetChamberLocalPosition(i);
            }
        }

        private Vector2[] GetChamberLocalPositions()
        {
            Vector2[] positions = new Vector2[chamberFills.Count];
            for (int i = 0; i < chamberFills.Count; i++)
            {
                positions[i] = chamberFills[i] != null ? chamberFills[i].rectTransform.anchoredPosition : Vector2.zero;
            }

            return positions;
        }

        private bool AreChambersInsideRoot()
        {
            if (chamberRoot == null)
            {
                return chamberFills.Count == 0;
            }

            Vector2 halfSize = chamberRoot.rect.size * 0.5f;
            if (halfSize.x <= 0.001f || halfSize.y <= 0.001f)
            {
                halfSize = chamberRoot.sizeDelta * 0.5f;
            }

            for (int i = 0; i < chamberFills.Count; i++)
            {
                Image chamber = chamberFills[i];
                if (chamber == null)
                {
                    continue;
                }

                RectTransform rect = chamber.rectTransform;
                Vector2 halfPip = rect.sizeDelta * 0.5f;
                Vector2 position = rect.anchoredPosition;
                if (Mathf.Abs(position.x) + halfPip.x > halfSize.x ||
                    Mathf.Abs(position.y) + halfPip.y > halfSize.y)
                {
                    return false;
                }
            }

            return true;
        }

        private void RemoveChamberBackingImage()
        {
            if (chamberRoot == null)
            {
                return;
            }

            Image backing = chamberRoot.GetComponent<Image>();
            if (backing != null)
            {
                backing.enabled = false;
                backing.color = Color.clear;
                backing.raycastTarget = false;
            }
        }

        private bool HasOpaqueChamberBacking()
        {
            Image backing = chamberRoot != null ? chamberRoot.GetComponent<Image>() : null;
            return backing != null && backing.enabled && backing.color.a > 0.01f;
        }

        private bool AreChambersParentedToRoot()
        {
            if (chamberRoot == null)
            {
                return chamberFills.Count == 0;
            }

            for (int i = 0; i < chamberFills.Count; i++)
            {
                if (chamberFills[i] == null || chamberFills[i].transform.parent != chamberRoot)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsPanelInsideSafeArea()
        {
            if (panelRoot == null)
            {
                return false;
            }

            Vector2 size = panelRoot.sizeDelta;
            Vector2 position = panelRoot.anchoredPosition;
            return position.x <= -HudLayoutConstants.HudMargin + 0.001f &&
                   position.y >= HudLayoutConstants.HudMargin - 0.001f &&
                   size.x <= HudLayoutConstants.WeaponPanelWidth + 0.001f &&
                   size.y <= HudLayoutConstants.WeaponPanelHeight + 0.001f;
        }

        private bool IsRectInsidePanel(RectTransform rect)
        {
            if (panelRoot == null || rect == null)
            {
                return false;
            }

            Rect panelRect = panelRoot.rect;
            Vector3[] corners = new Vector3[4];
            rect.GetLocalCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 panelSpace = panelRoot.InverseTransformPoint(rect.TransformPoint(corners[i]));
                if (!panelRect.Contains(panelSpace))
                {
                    return false;
                }
            }

            return true;
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

        private void DestroyLegacyWeaponIconBlock()
        {
            Transform iconRoot = FindNamedTransform(transform, "WeaponIconRoot");
            if (iconRoot != null)
            {
                DestroyUiObject(iconRoot.gameObject);
            }
        }

        private readonly struct RevolverChamberLayout
        {
            public static readonly RevolverChamberLayout Default = new RevolverChamberLayout(
                new Vector2(-55f, 116f),
                new Vector2(58f, 58f),
                8.5f,
                new[]
                {
                    new Vector2(0f, 14f),
                    new Vector2(13f, 7f),
                    new Vector2(13f, -7f),
                    new Vector2(0f, -14f),
                    new Vector2(-13f, -7f),
                    new Vector2(-13f, 7f)
                });

            public readonly Vector2 rootAnchoredPosition;
            public readonly Vector2 rootSize;
            public readonly float pipSize;
            public readonly Vector2[] localPositions;

            private RevolverChamberLayout(Vector2 rootAnchoredPosition, Vector2 rootSize, float pipSize, Vector2[] localPositions)
            {
                this.rootAnchoredPosition = rootAnchoredPosition;
                this.rootSize = rootSize;
                this.pipSize = pipSize;
                this.localPositions = localPositions;
            }
        }
    }
}
