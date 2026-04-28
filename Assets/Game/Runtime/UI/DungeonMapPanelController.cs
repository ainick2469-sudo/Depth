using FrontierDepths.Core;
using FrontierDepths.World;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class DungeonMapPanelController : MonoBehaviour
    {
        private const float MapWidth = 760f;
        private const float MapHeight = 560f;
        private const float MapPadding = 34f;

        private RectTransform panelRect;
        private RectTransform contentRect;
        private Text titleText;
        private Text legendText;
        private Text northText;
        private DungeonBuildResult buildResult;
        private Transform player;
        private DungeonMinimapController minimap;
        private MinimapCoordinateMapping mapping;
        private bool visible;

        public bool IsVisible => visible;

        private void Awake()
        {
            EnsureUi();
            SetVisible(false, null);
        }

        public void Configure(DungeonBuildResult result, Transform playerTransform, DungeonMinimapController minimapController)
        {
            buildResult = result;
            player = playerTransform;
            minimap = minimapController;
            if (visible)
            {
                Rebuild();
            }
        }

        public void Toggle(FirstPersonController owner)
        {
            SetVisible(!visible, owner);
        }

        public void SetVisible(bool value, FirstPersonController owner)
        {
            EnsureUi();
            visible = value && buildResult != null;
            if (panelRect != null)
            {
                panelRect.gameObject.SetActive(visible);
            }

            if (visible)
            {
                Rebuild();
            }

            owner?.SetUiCaptured(visible);
        }

        private void Rebuild()
        {
            EnsureUi();
            ClearContent();
            if (buildResult == null || contentRect == null)
            {
                return;
            }

            mapping = DungeonMinimapController.CalculateCoordinateMapping(buildResult, new Vector2(MapWidth, MapHeight), MapPadding, 1f);
            if (titleText != null)
            {
                titleText.text = $"Floor {Mathf.Max(1, buildResult.floorIndex)} Map";
            }

            for (int i = 0; i < buildResult.corridors.Count; i++)
            {
                DungeonCorridorBuildRecord corridor = buildResult.corridors[i];
                if (!ShouldShowCorridor(corridor))
                {
                    continue;
                }

                Image image = CreateImage($"MapCorridor_{i}", new Color(0.45f, 0.43f, 0.38f, 0.56f));
                ApplyBounds(image.rectTransform, corridor.bounds);
            }

            for (int i = 0; i < buildResult.rooms.Count; i++)
            {
                DungeonRoomBuildRecord room = buildResult.rooms[i];
                if (!ShouldShowRoom(room))
                {
                    continue;
                }

                bool visited = minimap != null && minimap.IsRoomVisited(room.nodeId);
                bool current = minimap != null && minimap.CurrentRoomId == room.nodeId;
                Image fill = CreateImage($"MapRoom_{room.nodeId}", GetRoomColor(room, visited, current));
                ApplyBounds(fill.rectTransform, room.bounds);
                Text icon = CreateText($"MapIcon_{room.nodeId}", GetRoomIcon(room), GetRoomIconColor(room), 16);
                icon.rectTransform.anchoredPosition = mapping.WorldToMap(room.bounds.center);
            }

            if (player != null)
            {
                Text playerText = CreateText("MapPlayer", ">", new Color(1f, 0.92f, 0.34f, 1f), 22);
                playerText.rectTransform.anchoredPosition = mapping.WorldToMap(player.position);
                playerText.rectTransform.localEulerAngles = new Vector3(0f, 0f, DungeonMinimapController.GetNorthUpPlayerArrowZ(player.eulerAngles.y));
            }
        }

        private bool ShouldShowRoom(DungeonRoomBuildRecord room)
        {
            if (room == null)
            {
                return false;
            }

            if (minimap == null)
            {
                return room.roomType == DungeonNodeKind.EntryHub;
            }

            if (room.nodeId == minimap.CurrentRoomId)
            {
                return true;
            }

            if (room.roomType == DungeonNodeKind.Secret || room.roomRole == DungeonRoomRole.Secret)
            {
                return minimap.IsRoomVisited(room.nodeId);
            }

            return minimap.IsRoomDiscovered(room.nodeId) || minimap.IsRoomVisited(room.nodeId);
        }

        private bool ShouldShowCorridor(DungeonCorridorBuildRecord corridor)
        {
            if (corridor == null)
            {
                return false;
            }

            if (minimap == null)
            {
                return false;
            }

            if (corridor.isSecretCorridor)
            {
                return minimap.IsRoomVisited(corridor.fromNodeId) || minimap.IsRoomVisited(corridor.toNodeId);
            }

            return minimap.IsCorridorDiscovered(corridor.edgeKey);
        }

        private void EnsureUi()
        {
            if (panelRect != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject panelObject = new GameObject("DungeonFullMap", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(transform, false);
            panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(900f, 660f);
            panelRect.anchoredPosition = Vector2.zero;
            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.025f, 0.022f, 0.02f, 0.94f);
            panelImage.raycastTarget = true;

            titleText = CreatePanelText(panelObject.transform, "MapTitle", font, 24, TextAnchor.UpperCenter, new Vector2(0f, 300f), new Vector2(820f, 38f));
            northText = CreatePanelText(panelObject.transform, "MapNorth", font, 18, TextAnchor.UpperCenter, new Vector2(0f, 252f), new Vector2(260f, 28f));
            northText.text = "NORTH (+Z)";
            legendText = CreatePanelText(panelObject.transform, "MapLegend", font, 14, TextAnchor.LowerLeft, new Vector2(-410f, -304f), new Vector2(820f, 42f));
            legendText.text = "Legend: > You | U Up | D Down | S Shrine | T Treasure | A Armory | E Elite | ? Secret | M Scout";

            GameObject contentObject = new GameObject("FullMapContent", typeof(RectTransform));
            contentObject.transform.SetParent(panelObject.transform, false);
            contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(MapWidth, MapHeight);
            contentRect.anchoredPosition = new Vector2(0f, 0f);
        }

        private Text CreatePanelText(Transform parent, string name, Font font, int size, TextAnchor alignment, Vector2 position, Vector2 sizeDelta)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = UiTheme.Text;
            text.raycastTarget = false;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = position;
            return text;
        }

        private Image CreateImage(string name, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(contentRect, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text CreateText(string name, string value, Color color, int size)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(contentRect, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = value;
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(36f, 28f);
            return text;
        }

        private void ApplyBounds(RectTransform rect, Bounds worldBounds)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = mapping.WorldToMap(worldBounds.center);
            rect.sizeDelta = mapping.WorldSizeToMap(worldBounds.size);
        }

        private void ClearContent()
        {
            if (contentRect == null)
            {
                return;
            }

            for (int i = contentRect.childCount - 1; i >= 0; i--)
            {
                GameObject child = contentRect.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private static Color GetRoomColor(DungeonRoomBuildRecord room, bool visited, bool current)
        {
            Color color = room.roomRole switch
            {
                DungeonRoomRole.Start => new Color(0.45f, 0.58f, 0.72f, 1f),
                DungeonRoomRole.Return => new Color(0.55f, 0.82f, 0.95f, 1f),
                DungeonRoomRole.Exit => new Color(0.95f, 0.75f, 0.2f, 1f),
                DungeonRoomRole.Treasure => new Color(1f, 0.78f, 0.2f, 1f),
                DungeonRoomRole.Shrine => new Color(0.64f, 0.38f, 0.86f, 1f),
                DungeonRoomRole.Armory => new Color(0.22f, 0.92f, 0.95f, 1f),
                DungeonRoomRole.Elite => new Color(0.94f, 0.2f, 0.16f, 1f),
                DungeonRoomRole.Secret => new Color(0.55f, 0.32f, 0.72f, 1f),
                DungeonRoomRole.Scout => new Color(0.22f, 0.78f, 0.68f, 1f),
                _ => new Color(0.58f, 0.6f, 0.64f, 1f)
            };
            float alpha = current ? 0.95f : visited ? 0.76f : 0.38f;
            return new Color(color.r, color.g, color.b, alpha);
        }

        private static string GetRoomIcon(DungeonRoomBuildRecord room)
        {
            if (!string.IsNullOrWhiteSpace(room.purposeIcon))
            {
                return room.purposeIcon;
            }

            return room.roomRole switch
            {
                DungeonRoomRole.Start => "S",
                DungeonRoomRole.Return => "U",
                DungeonRoomRole.Exit => "D",
                DungeonRoomRole.Treasure => "T",
                DungeonRoomRole.Shrine => "S",
                DungeonRoomRole.Armory => "A",
                DungeonRoomRole.Elite => "E",
                DungeonRoomRole.Secret => "?",
                DungeonRoomRole.Scout => "M",
                _ => string.Empty
            };
        }

        private static Color GetRoomIconColor(DungeonRoomBuildRecord room)
        {
            return room.roomRole == DungeonRoomRole.Exit
                ? new Color(1f, 0.9f, 0.35f, 1f)
                : Color.white;
        }
    }
}
