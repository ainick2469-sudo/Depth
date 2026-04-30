using FrontierDepths.Core;
using FrontierDepths.World;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class CompassHudView : MonoBehaviour
    {
        private static readonly string[] HeadingLabels = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        private readonly Text[] headingTexts = new Text[HeadingLabels.Length];
        private RectTransform stripRoot;
        private Text centerMarker;
        private Text floorText;
        private Transform player;
        private float nextResolveTime;
        private float smoothedYaw;
        private bool hasSmoothedYaw;

        internal string CenterHeadingForTests { get; private set; } = "N";
        internal string LocationLabelForTests => floorText != null ? floorText.text : string.Empty;
        internal bool LocationLabelVisibleForTests => floorText != null && floorText.enabled;

        private void Awake()
        {
            EnsureUi();
        }

        private void Update()
        {
            EnsureUi();
            ResolvePlayer();
            float yaw = player != null ? player.eulerAngles.y : 0f;
            if (!hasSmoothedYaw)
            {
                smoothedYaw = yaw;
                hasSmoothedYaw = true;
            }
            else
            {
                smoothedYaw = Mathf.LerpAngle(smoothedYaw, yaw, Mathf.Clamp01(Time.unscaledDeltaTime * 16f));
            }

            UpdateCompass(smoothedYaw);
            if (floorText != null)
            {
                ProfileState profile = GameBootstrap.Instance?.ProfileService?.Current;
                WorldFloorSceneContext context = WorldFloorSceneContext.ResolveCurrent(profile);
                floorText.enabled = context.ShouldShowHudLabel;
                floorText.text = context.FormatHudLabel();
            }
        }

        internal void UpdateCompassForTests(float yaw)
        {
            EnsureUi();
            UpdateCompass(yaw);
        }

        internal static float GetHeadingOffsetForYaw(float headingYaw, float playerYaw, float pixelsPerDegree)
        {
            return Mathf.DeltaAngle(playerYaw, headingYaw) * pixelsPerDegree;
        }

        internal static string GetLocationLabelForTests(WorldLocationKind context, int worldFloor)
        {
            return WorldFloorSceneContext.Create(context, worldFloor).FormatHudLabel();
        }

        internal static bool ShouldShowLocationLabelForTests(WorldLocationKind context)
        {
            return WorldFloorSceneContext.Create(context, 1).ShouldShowHudLabel;
        }

        private void UpdateCompass(float yaw)
        {
            CenterHeadingForTests = DungeonDirectionUtility.GetCardinalLabel(yaw);
            const float pixelsPerDegree = 4.2f;
            for (int i = 0; i < headingTexts.Length; i++)
            {
                Text text = headingTexts[i];
                if (text == null)
                {
                    continue;
                }

                float headingYaw = i * 45f;
                float offset = GetHeadingOffsetForYaw(headingYaw, yaw, pixelsPerDegree);
                RectTransform rect = text.rectTransform;
                rect.anchoredPosition = new Vector2(offset, 0f);
                bool centered = HeadingLabels[i] == CenterHeadingForTests;
                text.color = centered
                    ? new Color(1f, 0.84f, 0.28f, 1f)
                    : new Color(UiTheme.Text.r, UiTheme.Text.g, UiTheme.Text.b, Mathf.Lerp(0.25f, 0.82f, 1f - Mathf.Clamp01(Mathf.Abs(offset) / 210f)));
                text.fontSize = centered ? 20 : 15;
                text.enabled = Mathf.Abs(offset) <= 230f;
            }
        }

        private void ResolvePlayer()
        {
            if (player != null || Time.unscaledTime < nextResolveTime)
            {
                return;
            }

            nextResolveTime = Time.unscaledTime + 0.5f;
            FirstPersonController controller = FindAnyObjectByType<FirstPersonController>();
            player = controller != null ? controller.transform : null;
        }

        private void EnsureUi()
        {
            if (stripRoot != null)
            {
                return;
            }

            Font font = UiTheme.RuntimeFont;
            Transform parent = HudLayoutConstants.GetZoneOrRoot(transform, HudLayoutConstants.TopCenterZoneName);
            GameObject stripObject = new GameObject("CompassStrip", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            stripObject.transform.SetParent(parent != null ? parent : transform, false);
            stripRoot = stripObject.GetComponent<RectTransform>();
            stripRoot.anchorMin = stripRoot.anchorMax = new Vector2(0.5f, 1f);
            stripRoot.pivot = new Vector2(0.5f, 1f);
            stripRoot.sizeDelta = new Vector2(460f, 34f);
            stripRoot.anchoredPosition = new Vector2(0f, -18f);
            Image background = stripObject.GetComponent<Image>();
            background.color = new Color(0.02f, 0.02f, 0.025f, 0.42f);
            background.raycastTarget = false;

            for (int i = 0; i < HeadingLabels.Length; i++)
            {
                GameObject labelObject = new GameObject($"Compass_{HeadingLabels[i]}", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(stripRoot, false);
                Text text = labelObject.GetComponent<Text>();
                text.font = font;
                text.fontSize = 15;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = UiTheme.Text;
                text.raycastTarget = false;
                RectTransform rect = text.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(54f, 28f);
                text.text = HeadingLabels[i];
                headingTexts[i] = text;
            }

            GameObject markerObject = new GameObject("CompassCenterMarker", typeof(RectTransform), typeof(Text));
            markerObject.transform.SetParent(parent != null ? parent : transform, false);
            centerMarker = markerObject.GetComponent<Text>();
            centerMarker.font = font;
            centerMarker.fontSize = 18;
            centerMarker.alignment = TextAnchor.MiddleCenter;
            centerMarker.color = new Color(1f, 0.84f, 0.28f, 1f);
            centerMarker.text = "v";
            centerMarker.raycastTarget = false;
            RectTransform markerRect = centerMarker.rectTransform;
            markerRect.anchorMin = markerRect.anchorMax = new Vector2(0.5f, 1f);
            markerRect.pivot = new Vector2(0.5f, 1f);
            markerRect.sizeDelta = new Vector2(32f, 24f);
            markerRect.anchoredPosition = new Vector2(0f, -48f);

            GameObject floorObject = new GameObject("FloorLabel", typeof(RectTransform), typeof(Text));
            floorObject.transform.SetParent(parent != null ? parent : transform, false);
            floorText = floorObject.GetComponent<Text>();
            floorText.font = font;
            floorText.fontSize = 14;
            floorText.alignment = TextAnchor.UpperCenter;
            floorText.color = new Color(UiTheme.Text.r, UiTheme.Text.g, UiTheme.Text.b, 0.78f);
            floorText.raycastTarget = false;
            RectTransform floorRect = floorText.rectTransform;
            floorRect.anchorMin = floorRect.anchorMax = new Vector2(0.5f, 1f);
            floorRect.pivot = new Vector2(0.5f, 1f);
            floorRect.sizeDelta = new Vector2(460f, 42f);
            floorRect.anchoredPosition = new Vector2(0f, -70f);
        }
    }
}
