using FrontierDepths.Combat;
using FrontierDepths.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class WeaponHudView : MonoBehaviour
    {
        [SerializeField] private Text weaponNameText;
        [SerializeField] private Text ammoText;
        [SerializeField] private Text reloadText;
        [SerializeField] private Image hitMarkerImage;
        [SerializeField] private Image panelFrameImage;

        private PlayerWeaponController weapon;
        private float nextResolveTime;
        private float hitMarkerVisibleUntil;
        private Color hitMarkerColor = new Color(1f, 0.95f, 0.62f, 0.95f);
        private Vector2 hitMarkerSize = new Vector2(22f, 22f);

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
                return;
            }

            ShowWeaponHud();
            if (weaponNameText != null)
            {
                weaponNameText.text = weapon.WeaponName;
            }

            if (ammoText != null)
            {
                ammoText.text = $"{weapon.CurrentAmmo}/{weapon.ReserveAmmo}";
            }

            if (reloadText != null)
            {
                reloadText.enabled = weapon.IsReloading;
                reloadText.text = weapon.IsReloading ? $"RELOADING {Mathf.RoundToInt(weapon.ReloadProgress * 100f)}%" : string.Empty;
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
                return;
            }

            UnsubscribeWeapon();
            weapon = found;
            if (weapon != null)
            {
                weapon.HitFeedbackReceived += HandleHitFeedbackReceived;
            }
        }

        private void UnsubscribeWeapon()
        {
            if (weapon != null)
            {
                weapon.HitFeedbackReceived -= HandleHitFeedbackReceived;
            }

            weapon = null;
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
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            weaponNameText ??= FindNamedComponent<Text>("WeaponName");
            ammoText ??= FindNamedComponent<Text>("WeaponAmmo");
            reloadText ??= FindNamedComponent<Text>("WeaponReload");
            hitMarkerImage ??= FindNamedComponent<Image>("WeaponHitMarker");
            panelFrameImage ??= FindNamedComponent<Image>("WeaponPanelFrame");

            if (panelFrameImage == null)
            {
                GameObject frameObject = new GameObject("WeaponPanelFrame", typeof(RectTransform), typeof(Image));
                frameObject.transform.SetParent(transform, false);
                panelFrameImage = frameObject.GetComponent<Image>();
                RectTransform rect = panelFrameImage.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.sizeDelta = new Vector2(316f, 204f);
                rect.anchoredPosition = new Vector2(-18f, 12f);
                panelFrameImage.raycastTarget = false;
                panelFrameImage.transform.SetAsFirstSibling();
            }

            if (weaponNameText == null)
            {
                weaponNameText = CreateText("WeaponName", font, 18, TextAnchor.LowerRight);
                RectTransform rect = weaponNameText.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.sizeDelta = new Vector2(280f, 30f);
                rect.anchoredPosition = new Vector2(-48f, 106f);
            }

            if (ammoText == null)
            {
                ammoText = CreateText("WeaponAmmo", font, 34, TextAnchor.LowerRight);
                RectTransform rect = ammoText.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.sizeDelta = new Vector2(220f, 48f);
                rect.anchoredPosition = new Vector2(-54f, 46f);
            }

            if (reloadText == null)
            {
                reloadText = CreateText("WeaponReload", font, 22, TextAnchor.LowerRight);
                RectTransform rect = reloadText.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.sizeDelta = new Vector2(220f, 36f);
                rect.anchoredPosition = new Vector2(-48f, 140f);
            }

            if (hitMarkerImage == null)
            {
                GameObject markerObject = new GameObject("WeaponHitMarker", typeof(RectTransform), typeof(Image));
                markerObject.transform.SetParent(transform, false);
                hitMarkerImage = markerObject.GetComponent<Image>();
                RectTransform rect = hitMarkerImage.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(22f, 22f);
                rect.anchoredPosition = Vector2.zero;
            }

            ConfigureText(weaponNameText);
            ConfigureText(ammoText);
            ConfigureText(reloadText);
            if (hitMarkerImage != null)
            {
                hitMarkerImage.raycastTarget = false;
                hitMarkerImage.color = new Color(1f, 0.95f, 0.62f, 0.95f);
                hitMarkerImage.enabled = false;
            }

            ConfigurePanelFrame();
        }

        private Text CreateText(string name, Font font, int size, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(transform, false);
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
            if (panelFrameImage == null)
            {
                return;
            }

            Sprite panelSprite = HudSpriteCatalog.GetWeaponPanelSprite();
            panelFrameImage.sprite = panelSprite;
            panelFrameImage.type = Image.Type.Simple;
            panelFrameImage.preserveAspect = panelSprite != null;
            panelFrameImage.color = panelSprite != null
                ? new Color(1f, 1f, 1f, 0.92f)
                : new Color(UiTheme.Panel.r, UiTheme.Panel.g, UiTheme.Panel.b, 0.76f);
            panelFrameImage.enabled = true;
        }

        private void ShowWeaponHud()
        {
            if (panelFrameImage != null)
            {
                panelFrameImage.enabled = true;
            }

            SetTextVisible(weaponNameText, true);
            SetTextVisible(ammoText, true);
        }

        private void HideWeaponHud()
        {
            SetTextVisible(weaponNameText, false);
            SetTextVisible(ammoText, false);
            SetTextVisible(reloadText, false);
            if (panelFrameImage != null)
            {
                panelFrameImage.enabled = false;
            }

            if (hitMarkerImage != null)
            {
                hitMarkerImage.enabled = false;
            }
        }

        private static void SetTextVisible(Text text, bool visible)
        {
            if (text != null)
            {
                text.enabled = visible;
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
    }
}
