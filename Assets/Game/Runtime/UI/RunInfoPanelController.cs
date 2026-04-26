using System.Text;
using FrontierDepths.Combat;
using FrontierDepths.Core;
using FrontierDepths.Progression.Mastery;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class RunInfoPanelController : MonoBehaviour
    {
        private const string PanelName = "RunInfoPanel";

        private RectTransform panelRect;
        private Image panelBackground;
        private Text panelText;
        private bool visible;
        private float nextRefreshTime;

        public bool IsVisible => visible;

        private void Awake()
        {
            EnsureUi();
            SetVisible(false);
        }

        private void Update()
        {
            if (!visible || Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            RefreshText();
            nextRefreshTime = Time.unscaledTime + 0.2f;
        }

        public void Toggle()
        {
            SetVisible(!visible);
        }

        public void SetVisible(bool value)
        {
            visible = value;
            EnsureUi();
            if (panelRect != null)
            {
                panelRect.gameObject.SetActive(value);
            }

            if (value)
            {
                RefreshText();
            }
        }

        public string BuildInfoText()
        {
            StringBuilder builder = new StringBuilder(1024);
            GameBootstrap bootstrap = GameBootstrap.Instance;
            RunState run = bootstrap != null && bootstrap.RunService != null ? bootstrap.RunService.Current : null;
            ProfileState profile = bootstrap != null && bootstrap.ProfileService != null ? bootstrap.ProfileService.Current : null;
            PlayerWeaponController weapon = FindAnyObjectByType<PlayerWeaponController>();
            PlayerHealth health = FindAnyObjectByType<PlayerHealth>();
            RunStatSnapshot stats = RunStatAggregator.Current;

            builder.AppendLine("RUN INFO");
            builder.AppendLine(run != null ? $"Floor {run.floorIndex}" : "Floor -");
            builder.AppendLine(profile != null ? $"Gold {profile.gold}" : "Gold -");
            if (health != null)
            {
                builder.AppendLine($"HP {health.CurrentHealth:0}/{health.MaxHealth:0}");
            }

            builder.AppendLine();
            builder.AppendLine("WEAPON");
            if (weapon != null)
            {
                builder.AppendLine(weapon.WeaponName);
                builder.AppendLine($"Damage {weapon.BaseDamage:0.#} -> {weapon.EffectiveDamage:0.#} ({stats.revolverDamagePercent * 100f:+0.#;-0.#;0}%)");
                builder.AppendLine($"Reload {weapon.BaseReloadDuration:0.00}s -> {weapon.EffectiveReloadDuration:0.00}s (+{stats.reloadSpeedPercent * 100f:0.#}%)");
                builder.AppendLine($"Crit {weapon.CritChance * 100f:0.#}%");
                builder.AppendLine($"Range {weapon.MaxRange:0.#}m | full {weapon.FullDamageRange:0.#}m | max falloff {weapon.DamageMultiplierAtMaxRange * 100f:0.#}%");
            }
            else
            {
                builder.AppendLine("No weapon found.");
            }

            builder.AppendLine();
            builder.AppendLine("RUN UPGRADES");
            if (run != null && run.runUpgrades != null && run.runUpgrades.Count > 0)
            {
                for (int i = 0; i < run.runUpgrades.Count; i++)
                {
                    builder.AppendLine($"- {RunUpgradeCatalog.BuildOwnedUpgradeSummary(run.runUpgrades[i])}");
                }
            }
            else
            {
                builder.AppendLine("- None yet");
            }

            builder.AppendLine();
            builder.AppendLine("SPECIALS");
            builder.AppendLine(stats.HasChainHit
                ? $"- Chain Spark: every {stats.chainEveryNthHit}th hit chains {stats.chainDamageFraction * 100f:0.#}% damage within {RunUpgradeCatalog.ChainHitSearchRadius:0.#}m"
                : "- Chain Spark: inactive");
            builder.AppendLine(stats.HasFirstShotAfterReloadBonus
                ? $"- First Shot: +{stats.firstShotAfterReloadPercent * 100f:0.#}% after reload"
                : "- First Shot: inactive");

            builder.AppendLine();
            builder.AppendLine("MASTERY");
            MasteryProgressService mastery = MasteryProgressRuntime.Service;
            builder.AppendLine(mastery != null ? mastery.GetDebugSummary(8) : "Mastery runtime not ready");
            builder.AppendLine();
            builder.AppendLine("Press G to close.");
            return builder.ToString();
        }

        private void EnsureUi()
        {
            if (panelRect != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject panelObject = new GameObject(PanelName, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(transform, false);
            panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(720f, 620f);
            panelRect.anchoredPosition = Vector2.zero;

            panelBackground = panelObject.GetComponent<Image>();
            panelBackground.color = new Color(0.018f, 0.02f, 0.026f, 0.92f);
            panelBackground.raycastTarget = true;

            GameObject textObject = new GameObject("RunInfoText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panelObject.transform, false);
            panelText = textObject.GetComponent<Text>();
            panelText.font = font;
            panelText.fontSize = 18;
            panelText.alignment = TextAnchor.UpperLeft;
            panelText.color = new Color(0.94f, 0.93f, 0.86f, 1f);
            panelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            panelText.verticalOverflow = VerticalWrapMode.Truncate;
            panelText.raycastTarget = false;
            RectTransform textRect = panelText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(28f, 24f);
            textRect.offsetMax = new Vector2(-28f, -24f);
        }

        private void RefreshText()
        {
            if (panelText != null)
            {
                panelText.text = BuildInfoText();
            }
        }
    }
}
