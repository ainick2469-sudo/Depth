using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class HudDebugBoundsController : MonoBehaviour
    {
        private static readonly string[] TrackedNames =
        {
            HudLayoutConstants.TopCenterZoneName,
            HudLayoutConstants.TopRightZoneName,
            HudLayoutConstants.BottomLeftZoneName,
            HudLayoutConstants.BottomCenterZoneName,
            HudLayoutConstants.BottomRightZoneName,
            "WeaponPanelRoot",
            "DungeonMinimap",
            "MinimapContentRoot",
            "HudResourcePanel",
            "AmmoPipContainer"
        };

        private readonly List<Image> boundsImages = new List<Image>();
        private readonly List<Text> labels = new List<Text>();
        private RectTransform debugRoot;
        private bool visible;

        internal bool IsVisibleForTests => visible;
        internal int DebugElementCountForTests => boundsImages.Count;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                SetVisible(!visible);
            }

            if (visible)
            {
                RefreshBounds();
            }
        }

        internal void SetVisibleForTests(bool value)
        {
            SetVisible(value);
        }

        private void SetVisible(bool value)
        {
            visible = value;
            EnsureUi();
            if (debugRoot != null)
            {
                debugRoot.gameObject.SetActive(visible);
            }

            if (visible)
            {
                RefreshBounds();
            }
        }

        private void EnsureUi()
        {
            if (debugRoot != null)
            {
                return;
            }

            Transform parent = HudLayoutConstants.GetZoneOrRoot(transform, HudLayoutConstants.OverlayZoneName);
            GameObject rootObject = new GameObject("HudDebugLayoutBounds", typeof(RectTransform));
            rootObject.transform.SetParent(parent != null ? parent : transform, false);
            debugRoot = rootObject.GetComponent<RectTransform>();
            debugRoot.anchorMin = Vector2.zero;
            debugRoot.anchorMax = Vector2.one;
            debugRoot.offsetMin = Vector2.zero;
            debugRoot.offsetMax = Vector2.zero;
            debugRoot.gameObject.SetActive(false);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < TrackedNames.Length; i++)
            {
                GameObject boxObject = new GameObject($"DebugBounds_{TrackedNames[i]}", typeof(RectTransform), typeof(Image));
                boxObject.transform.SetParent(debugRoot, false);
                Image image = boxObject.GetComponent<Image>();
                image.color = new Color(0.2f, 0.85f, 1f, 0.08f);
                image.raycastTarget = false;
                boundsImages.Add(image);

                GameObject labelObject = new GameObject($"DebugLabel_{TrackedNames[i]}", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(boxObject.transform, false);
                Text label = labelObject.GetComponent<Text>();
                label.font = font;
                label.fontSize = 11;
                label.alignment = TextAnchor.UpperLeft;
                label.color = new Color(0.6f, 0.95f, 1f, 0.92f);
                label.text = TrackedNames[i];
                label.raycastTarget = false;
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(3f, 2f);
                labelRect.offsetMax = new Vector2(-3f, -2f);
                labels.Add(label);
            }
        }

        private void RefreshBounds()
        {
            EnsureUi();
            RectTransform rootRect = transform as RectTransform;
            for (int i = 0; i < TrackedNames.Length; i++)
            {
                RectTransform target = FindNamedTransform(transform, TrackedNames[i]) as RectTransform;
                bool hasTarget = target != null && rootRect != null;
                boundsImages[i].enabled = hasTarget;
                labels[i].enabled = hasTarget;
                if (!hasTarget)
                {
                    continue;
                }

                RectTransform boxRect = boundsImages[i].rectTransform;
                Vector3[] corners = new Vector3[4];
                target.GetWorldCorners(corners);
                Vector2 min = rootRect.InverseTransformPoint(corners[0]);
                Vector2 max = rootRect.InverseTransformPoint(corners[2]);
                boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
                boxRect.pivot = new Vector2(0.5f, 0.5f);
                boxRect.anchoredPosition = (min + max) * 0.5f;
                boxRect.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
            }
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
