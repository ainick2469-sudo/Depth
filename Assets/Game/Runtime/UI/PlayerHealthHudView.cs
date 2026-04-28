using FrontierDepths.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class PlayerHealthHudView : MonoBehaviour
    {
        [SerializeField] private Text healthText;
        [SerializeField] private Text deathText;
        [SerializeField] private Image damageFlashImage;

        private PlayerHealth playerHealth;
        private float nextResolveTime;
        private float flashVisibleUntil;

        private void Awake()
        {
            EnsureHudElements();
            HideHud();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            ResolvePlayerHealth();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnsubscribePlayerHealth();
        }

        private void Update()
        {
            if (playerHealth == null && Time.unscaledTime >= nextResolveTime)
            {
                nextResolveTime = Time.unscaledTime + 0.5f;
                ResolvePlayerHealth();
            }

            if (playerHealth == null)
            {
                HideHud();
                return;
            }

            ShowHud();
            if (healthText != null)
            {
                healthText.enabled = false;
            }

            if (deathText != null)
            {
                deathText.enabled = playerHealth.IsDead;
                deathText.text = playerHealth.IsDead ? "YOU DIED\nPress R to return to town" : string.Empty;
            }

            UpdateDamageFlash();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureHudElements();
            ResolvePlayerHealth();
        }

        private void ResolvePlayerHealth()
        {
            PlayerHealth found = FindAnyObjectByType<PlayerHealth>();
            if (found == playerHealth)
            {
                return;
            }

            UnsubscribePlayerHealth();
            playerHealth = found;
            if (playerHealth != null)
            {
                playerHealth.Damaged += HandlePlayerDamaged;
            }
        }

        private void UnsubscribePlayerHealth()
        {
            if (playerHealth != null)
            {
                playerHealth.Damaged -= HandlePlayerDamaged;
            }

            playerHealth = null;
        }

        private void HandlePlayerDamaged(PlayerHealth health, DamageResult result)
        {
            if (result.applied)
            {
                flashVisibleUntil = Time.unscaledTime + 0.18f;
            }
        }

        private void UpdateDamageFlash()
        {
            if (damageFlashImage == null)
            {
                return;
            }

            bool visible = Time.unscaledTime < flashVisibleUntil;
            damageFlashImage.enabled = visible;
            if (!visible)
            {
                return;
            }

            float fade = Mathf.InverseLerp(flashVisibleUntil - 0.18f, flashVisibleUntil, Time.unscaledTime);
            damageFlashImage.color = new Color(1f, 0.08f, 0.03f, Mathf.Lerp(0.28f, 0.02f, fade));
        }

        private void EnsureHudElements()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            healthText ??= FindNamedComponent<Text>("PlayerHealth");
            deathText ??= FindNamedComponent<Text>("PlayerDeathMessage");
            damageFlashImage ??= FindNamedComponent<Image>("PlayerDamageFlash");

            if (healthText == null)
            {
                healthText = CreateText("PlayerHealth", font, 30, TextAnchor.LowerLeft);
                RectTransform rect = healthText.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.sizeDelta = new Vector2(260f, 44f);
                rect.anchoredPosition = new Vector2(28f, 158f);
            }

            if (deathText == null)
            {
                deathText = CreateText("PlayerDeathMessage", font, 42, TextAnchor.MiddleCenter);
                RectTransform rect = deathText.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(720f, 180f);
                rect.anchoredPosition = Vector2.zero;
            }

            if (damageFlashImage == null)
            {
                GameObject flashObject = new GameObject("PlayerDamageFlash", typeof(RectTransform), typeof(Image));
                flashObject.transform.SetParent(transform, false);
                damageFlashImage = flashObject.GetComponent<Image>();
                RectTransform rect = damageFlashImage.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            ConfigureText(healthText);
            ConfigureText(deathText);
            if (damageFlashImage != null)
            {
                damageFlashImage.raycastTarget = false;
                damageFlashImage.enabled = false;
            }
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

        private static void ConfigureText(Text text)
        {
            if (text != null)
            {
                text.raycastTarget = false;
            }
        }

        private void ShowHud()
        {
            if (healthText != null)
            {
                healthText.enabled = true;
            }
        }

        private void HideHud()
        {
            if (healthText != null)
            {
                healthText.enabled = false;
            }

            if (deathText != null)
            {
                deathText.enabled = false;
            }

            if (damageFlashImage != null)
            {
                damageFlashImage.enabled = false;
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
