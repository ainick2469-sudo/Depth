using System.Collections.Generic;
using FrontierDepths.Core;
using FrontierDepths.World;
using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.UI
{
    public sealed class DungeonMinimapController : MonoBehaviour
    {
        private const string PanelName = "DungeonMinimap";
        private const float DefaultSize = 190f;
        private const float DefaultPadding = 14f;
        public const float PlayerArrowIconOffsetDegrees = 90f; // The current ">" glyph points right at 0 degrees.

        private readonly Dictionary<string, RoomElement> roomElements = new Dictionary<string, RoomElement>();
        private readonly List<CorridorElement> corridorElements = new List<CorridorElement>();
        private readonly HashSet<string> visitedRooms = new HashSet<string>();
        private readonly HashSet<string> discoveredRooms = new HashSet<string>();
        private readonly HashSet<string> discoveredCorridors = new HashSet<string>();

        private RectTransform panelRect;
        private RectTransform contentRect;
        private CanvasGroup canvasGroup;
        private Text playerArrow;
        private DungeonBuildResult buildResult;
        private Transform player;
        private MinimapCoordinateMapping mapping;
        private bool configured;
        private bool revealAllDebug;
        private bool visible = true;
        private bool rotateWithPlayer;
        private bool debugLabelsVisible;
        private float minimapSize = DefaultSize;
        private float minimapOpacity = 0.9f;
        private float minimapZoom = 1f;
        private string currentRoomId = string.Empty;
        private int geometryBuildCount;

        public int GeometryBuildCount => geometryBuildCount;
        public int RoomElementCount => roomElements.Count;
        public int CorridorElementCount => corridorElements.Count;
        public string CurrentRoomId => currentRoomId;
        public bool IsVisible => visible;
        public bool IsConfigured => configured;
        public bool RevealAllDebug => revealAllDebug;
        public float MinimapSize => minimapSize;
        public float MinimapOpacity => minimapOpacity;
        public float MinimapZoom => minimapZoom;
        public MinimapCoordinateMapping Mapping => mapping;
        public float CurrentContentRotationZ => contentRect != null ? contentRect.localEulerAngles.z : 0f;
        public float CurrentPlayerArrowRotationZ => playerArrow != null ? playerArrow.rectTransform.localEulerAngles.z : 0f;
        public Vector2 CurrentPlayerArrowPosition => playerArrow != null ? playerArrow.rectTransform.anchoredPosition : Vector2.zero;

        private void Awake()
        {
            EnsureUi();
        }

        private void Update()
        {
            if (!configured || player == null)
            {
                return;
            }

            UpdatePlayerTracking();
            UpdatePlayerArrow();
        }

        public void Configure(DungeonBuildResult result, Transform playerTransform)
        {
            using (LoadTimingLogger.Measure("Minimap build"))
            {
            EnsureUi();
            buildResult = result;
            player = playerTransform;
            configured = buildResult != null;
            visitedRooms.Clear();
            discoveredRooms.Clear();
            discoveredCorridors.Clear();
            currentRoomId = string.Empty;
            ClearGeometry();

            if (!configured)
            {
                if (panelRect != null)
                {
                    panelRect.gameObject.SetActive(false);
                }

                return;
            }

            mapping = CalculateCoordinateMapping(buildResult, new Vector2(minimapSize, minimapSize), DefaultPadding, minimapZoom);
            BuildStaticGeometry();
            SetVisible(visible);
            ImportActiveFloorDiscovery();
            if (visitedRooms.Count == 0 && !string.IsNullOrWhiteSpace(buildResult.playerSpawnNodeId))
            {
                NotifyRoomEntered(buildResult.playerSpawnNodeId);
            }

            UpdatePlayerTracking(true);
            RefreshRevealState();
            UpdatePlayerArrow();
            }
        }

        public void ToggleVisibility()
        {
            SetVisible(!visible);
        }

        public void SetVisible(bool value)
        {
            visible = value;
            if (panelRect != null)
            {
                panelRect.gameObject.SetActive(value && configured);
            }
        }

        public void SetRevealAllDebug(bool value)
        {
            if (revealAllDebug == value)
            {
                return;
            }

            revealAllDebug = value;
            RefreshRevealState();
        }

        public void SetRotateWithPlayer(bool value)
        {
            rotateWithPlayer = value;
        }

        public void SetSize(float value)
        {
            minimapSize = Mathf.Clamp(value, 120f, 360f);
            if (panelRect != null)
            {
                panelRect.sizeDelta = new Vector2(minimapSize, minimapSize);
            }

            if (contentRect != null)
            {
                contentRect.sizeDelta = new Vector2(minimapSize, minimapSize);
            }

            RebuildConfiguredMap();
        }

        public void SetOpacity(float value)
        {
            minimapOpacity = Mathf.Clamp01(value);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = minimapOpacity;
            }
        }

        public void SetZoom(float value)
        {
            minimapZoom = Mathf.Clamp(value, 0.5f, 3f);
            RebuildConfiguredMap();
        }

        public void SetDebugLabelsVisible(bool value)
        {
            debugLabelsVisible = value;
            RefreshRevealState();
        }

        public void NotifyRoomEntered(string roomId)
        {
            if (buildResult == null || string.IsNullOrWhiteSpace(roomId) || buildResult.FindRoom(roomId) == null)
            {
                return;
            }

            currentRoomId = roomId;
            visitedRooms.Add(roomId);
            discoveredRooms.Add(roomId);
            RevealConnectedRoomsAndCorridors(roomId);
            RefreshRevealState();
            ExportActiveFloorDiscovery(true);
        }

        public bool IsRoomVisited(string roomId)
        {
            return visitedRooms.Contains(roomId);
        }

        public bool IsRoomDiscovered(string roomId)
        {
            return discoveredRooms.Contains(roomId);
        }

        public bool IsCorridorDiscovered(string edgeKey)
        {
            return discoveredCorridors.Contains(edgeKey);
        }

        public void ImportDiscoveryFrom(FloorState floorState)
        {
            visitedRooms.Clear();
            discoveredRooms.Clear();
            discoveredCorridors.Clear();
            currentRoomId = string.Empty;
            if (floorState == null)
            {
                return;
            }

            AddRange(visitedRooms, floorState.visitedRoomIds);
            AddRange(discoveredRooms, floorState.discoveredRoomIds);
            AddRange(discoveredCorridors, floorState.discoveredCorridorIds);
            currentRoomId = floorState.lastKnownPlayerRoomId ?? string.Empty;
            RefreshRevealState();
        }

        public void ExportDiscoveryTo(FloorState floorState)
        {
            if (floorState == null)
            {
                return;
            }

            floorState.visitedRoomIds = new List<string>(visitedRooms);
            floorState.discoveredRoomIds = new List<string>(discoveredRooms);
            floorState.discoveredCorridorIds = new List<string>(discoveredCorridors);
            floorState.lastKnownPlayerRoomId = currentRoomId ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(currentRoomId))
            {
                DungeonRoomBuildRecord currentRoom = buildResult != null ? buildResult.FindRoom(currentRoomId) : null;
                if (currentRoom != null && (currentRoom.roomType == DungeonNodeKind.TransitDown || currentRoom.roomType == DungeonNodeKind.TransitUp))
                {
                    floorState.knownStairRoomId = currentRoomId;
                    floorState.stairDiscovered = true;
                }
            }

            if (buildResult != null)
            {
                foreach (string discoveredRoomId in discoveredRooms)
                {
                    DungeonRoomBuildRecord room = buildResult.FindRoom(discoveredRoomId);
                    if (room != null && (room.roomType == DungeonNodeKind.TransitDown || room.roomType == DungeonNodeKind.TransitUp))
                    {
                        floorState.knownStairRoomId = room.nodeId;
                        floorState.stairDiscovered = true;
                    }
                }
            }
        }

        public void PersistCurrentDiscovery()
        {
            ExportActiveFloorDiscovery(true);
        }

        private void ImportActiveFloorDiscovery()
        {
            RunService runService = GameBootstrap.Instance != null ? GameBootstrap.Instance.RunService : null;
            FloorState floorState = runService != null && runService.Current != null ? runService.Current.currentFloor : null;
            ImportDiscoveryFrom(floorState);
        }

        private void ExportActiveFloorDiscovery(bool save)
        {
            RunService runService = GameBootstrap.Instance != null ? GameBootstrap.Instance.RunService : null;
            FloorState floorState = runService != null && runService.Current != null ? runService.Current.currentFloor : null;
            if (floorState == null)
            {
                return;
            }

            ExportDiscoveryTo(floorState);
            if (save)
            {
                runService.SaveActiveFloorState();
            }
        }

        private static void AddRange(HashSet<string> target, List<string> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(source[i]))
                {
                    target.Add(source[i]);
                }
            }
        }

        public static MinimapCoordinateMapping CalculateCoordinateMapping(DungeonBuildResult result, Vector2 mapSize, float padding, float zoom = 1f)
        {
            Bounds bounds = CalculateWorldBounds(result);
            Vector2 worldSize = new Vector2(Mathf.Max(1f, bounds.size.x), Mathf.Max(1f, bounds.size.z));
            Vector2 available = new Vector2(Mathf.Max(1f, mapSize.x - padding * 2f), Mathf.Max(1f, mapSize.y - padding * 2f));
            float scale = Mathf.Min(available.x / worldSize.x, available.y / worldSize.y) * Mathf.Clamp(zoom, 0.5f, 3f);
            return new MinimapCoordinateMapping(bounds, mapSize, scale);
        }

        public static Bounds CalculateWorldBounds(DungeonBuildResult result)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
            bool hasBounds = false;
            if (result != null)
            {
                for (int i = 0; i < result.rooms.Count; i++)
                {
                    EncapsulateXZ(ref bounds, ref hasBounds, result.rooms[i].bounds);
                }

                for (int i = 0; i < result.corridors.Count; i++)
                {
                    EncapsulateXZ(ref bounds, ref hasBounds, result.corridors[i].bounds);
                }
            }

            return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.one);
        }

        public static float GetNorthUpPlayerArrowZ(float playerYawDegrees)
        {
            return PlayerArrowIconOffsetDegrees - playerYawDegrees;
        }

        public static float GetRotatingMapContentZ(float playerYawDegrees)
        {
            return playerYawDegrees;
        }

        public static float GetRotatingMapPlayerArrowZ(float playerYawDegrees)
        {
            return PlayerArrowIconOffsetDegrees;
        }

        public static Vector2 RotateMapPointForContent(Vector2 mapPoint, float contentRotationZ)
        {
            float radians = contentRotationZ * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(
                mapPoint.x * cos - mapPoint.y * sin,
                mapPoint.x * sin + mapPoint.y * cos);
        }

        private void EnsureUi()
        {
            if (panelRect != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject panelObject = new GameObject(PanelName, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panelObject.transform.SetParent(transform, false);
            panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.sizeDelta = new Vector2(minimapSize, minimapSize);
            panelRect.anchoredPosition = new Vector2(-22f, -22f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.02f, 0.025f, 0.03f, 0.72f);
            panelImage.raycastTarget = false;
            canvasGroup = panelObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = minimapOpacity;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            GameObject contentObject = new GameObject("MinimapContent", typeof(RectTransform));
            contentObject.transform.SetParent(panelObject.transform, false);
            contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(minimapSize, minimapSize);
            contentRect.anchoredPosition = Vector2.zero;

            GameObject playerObject = new GameObject("PlayerArrow", typeof(RectTransform), typeof(Text));
            playerObject.transform.SetParent(panelObject.transform, false);
            playerArrow = playerObject.GetComponent<Text>();
            playerArrow.font = font;
            playerArrow.fontSize = 18;
            playerArrow.alignment = TextAnchor.MiddleCenter;
            playerArrow.color = new Color(1f, 0.92f, 0.42f, 1f);
            playerArrow.text = ">";
            playerArrow.raycastTarget = false;
            RectTransform arrowRect = playerArrow.rectTransform;
            arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.sizeDelta = new Vector2(24f, 24f);

            panelObject.SetActive(false);
        }

        private void BuildStaticGeometry()
        {
            geometryBuildCount++;
            for (int i = 0; i < buildResult.corridors.Count; i++)
            {
                DungeonCorridorBuildRecord corridor = buildResult.corridors[i];
                Image image = CreateMapImage($"Corridor_{i}", contentRect, new Color(0.42f, 0.42f, 0.45f, 0.45f));
                ApplyBoundsToRect(image.rectTransform, corridor.bounds);
                corridorElements.Add(new CorridorElement(corridor, image));
            }

            for (int i = 0; i < buildResult.rooms.Count; i++)
            {
                DungeonRoomBuildRecord room = buildResult.rooms[i];
                Image fill = CreateMapImage($"Room_{room.nodeId}", contentRect, GetRoomColor(room, false, false));
                ApplyBoundsToRect(fill.rectTransform, room.bounds);

                Image current = CreateMapImage($"RoomCurrent_{room.nodeId}", contentRect, new Color(1f, 0.95f, 0.55f, 0.32f));
                ApplyBoundsToRect(current.rectTransform, room.bounds);

                Text icon = CreateMapText($"RoomIcon_{room.nodeId}", contentRect, GetRoomIcon(room), GetRoomIconColor(room));
                icon.rectTransform.anchoredPosition = mapping.WorldToMap(room.bounds.center);
                roomElements[room.nodeId] = new RoomElement(room, fill, current, icon);
            }
        }

        private void ClearGeometry()
        {
            if (contentRect != null)
            {
                for (int i = contentRect.childCount - 1; i >= 0; i--)
                {
                    DestroyUiObject(contentRect.GetChild(i).gameObject);
                }
            }

            roomElements.Clear();
            corridorElements.Clear();
        }

        private void UpdatePlayerTracking(bool force = false)
        {
            string containingRoomId = FindContainingRoomId(player.position);
            if (force || (!string.IsNullOrWhiteSpace(containingRoomId) && containingRoomId != currentRoomId))
            {
                NotifyRoomEntered(containingRoomId);
            }
        }

        private void UpdatePlayerArrow()
        {
            if (playerArrow == null || player == null)
            {
                return;
            }

            Vector2 mapPosition = mapping.WorldToMap(player.position);
            if (rotateWithPlayer && contentRect != null)
            {
                float contentRotation = GetRotatingMapContentZ(player.eulerAngles.y);
                contentRect.localEulerAngles = new Vector3(0f, 0f, contentRotation);
                playerArrow.rectTransform.anchoredPosition = RotateMapPointForContent(mapPosition, contentRotation);
                playerArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, GetRotatingMapPlayerArrowZ(player.eulerAngles.y));
            }
            else
            {
                if (contentRect != null)
                {
                    contentRect.localEulerAngles = Vector3.zero;
                }

                playerArrow.rectTransform.anchoredPosition = mapPosition;
                playerArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, GetNorthUpPlayerArrowZ(player.eulerAngles.y));
            }
        }

        private void RefreshRevealState()
        {
            for (int i = 0; i < corridorElements.Count; i++)
            {
                CorridorElement element = corridorElements[i];
                bool shown = revealAllDebug || IsCorridorShown(element.record);
                element.image.enabled = shown;
                element.image.color = new Color(0.42f, 0.42f, 0.45f, shown ? 0.5f : 0f);
            }

            foreach (KeyValuePair<string, RoomElement> pair in roomElements)
            {
                RoomElement element = pair.Value;
                bool roomVisible = revealAllDebug || IsRoomShown(element.room);
                bool roomVisited = visitedRooms.Contains(element.room.nodeId);
                bool isCurrent = element.room.nodeId == currentRoomId;
                element.fill.enabled = roomVisible;
                element.fill.color = GetRoomColor(element.room, roomVisited, isCurrent);
                element.current.enabled = roomVisible && isCurrent;
                element.icon.text = debugLabelsVisible && !string.IsNullOrWhiteSpace(element.room.label)
                    ? element.room.label
                    : GetRoomIcon(element.room);
                element.icon.fontSize = debugLabelsVisible ? 9 : 14;
                element.icon.enabled = roomVisible && !string.IsNullOrWhiteSpace(element.icon.text);
            }
        }

        private void RebuildConfiguredMap()
        {
            if (buildResult == null || !configured)
            {
                return;
            }

            ClearGeometry();
            mapping = CalculateCoordinateMapping(buildResult, new Vector2(minimapSize, minimapSize), DefaultPadding, minimapZoom);
            BuildStaticGeometry();
            RefreshRevealState();
            UpdatePlayerArrow();
        }

        private string FindContainingRoomId(Vector3 worldPosition)
        {
            if (buildResult == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < buildResult.rooms.Count; i++)
            {
                DungeonRoomBuildRecord room = buildResult.rooms[i];
                Bounds bounds = room.bounds;
                bounds.Expand(new Vector3(0.2f, 0f, 0.2f));
                if (worldPosition.x >= bounds.min.x && worldPosition.x <= bounds.max.x &&
                    worldPosition.z >= bounds.min.z && worldPosition.z <= bounds.max.z)
                {
                    return room.nodeId;
                }
            }

            return string.Empty;
        }

        private void RevealConnectedRoomsAndCorridors(string roomId)
        {
            if (buildResult == null)
            {
                return;
            }

            for (int i = 0; i < buildResult.graphEdges.Count; i++)
            {
                DungeonGraphEdgeRecord edge = buildResult.graphEdges[i];
                if (edge.a != roomId && edge.b != roomId)
                {
                    continue;
                }

                string otherRoomId = edge.a == roomId ? edge.b : edge.a;
                DungeonRoomBuildRecord otherRoom = buildResult.FindRoom(otherRoomId);
                if (otherRoom != null && otherRoom.roomType != DungeonNodeKind.Secret)
                {
                    discoveredRooms.Add(otherRoomId);
                }

                if (!IsSecretEdge(edge.edgeKey))
                {
                    discoveredCorridors.Add(edge.edgeKey);
                }
            }
        }

        private bool IsRoomShown(DungeonRoomBuildRecord room)
        {
            if (room.roomType == DungeonNodeKind.Secret)
            {
                return visitedRooms.Contains(room.nodeId);
            }

            return discoveredRooms.Contains(room.nodeId) || visitedRooms.Contains(room.nodeId);
        }

        private bool IsCorridorShown(DungeonCorridorBuildRecord corridor)
        {
            if (corridor.isSecretCorridor)
            {
                return visitedRooms.Contains(corridor.fromNodeId) || visitedRooms.Contains(corridor.toNodeId);
            }

            return discoveredCorridors.Contains(corridor.edgeKey);
        }

        private bool IsSecretEdge(string edgeKey)
        {
            if (buildResult == null)
            {
                return false;
            }

            for (int i = 0; i < buildResult.corridors.Count; i++)
            {
                if (buildResult.corridors[i].edgeKey == edgeKey && buildResult.corridors[i].isSecretCorridor)
                {
                    return true;
                }
            }

            return false;
        }

        private Image CreateMapImage(string name, Transform parent, Color color)
        {
            GameObject elementObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            elementObject.transform.SetParent(parent, false);
            Image image = elementObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text CreateMapText(string name, Transform parent, string text, Color color)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text label = textObject.GetComponent<Text>();
            label.font = font;
            label.fontSize = 14;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.text = text;
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = new Vector2(24f, 24f);
            return label;
        }

        private void ApplyBoundsToRect(RectTransform rect, Bounds worldBounds)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = mapping.WorldToMap(worldBounds.center);
            rect.sizeDelta = mapping.WorldSizeToMap(worldBounds.size);
        }

        private Color GetRoomColor(DungeonRoomBuildRecord room, bool visited, bool current)
        {
            Color baseColor = GetPurposeMinimapColor(room);
            if (baseColor == default)
            {
                baseColor = room.roomType switch
                {
                    DungeonNodeKind.TransitDown => new Color(0.95f, 0.75f, 0.2f, 1f),
                    DungeonNodeKind.TransitUp => new Color(0.55f, 0.82f, 0.95f, 1f),
                    DungeonNodeKind.Landmark => new Color(0.35f, 0.85f, 0.58f, 1f),
                    DungeonNodeKind.Secret => new Color(0.62f, 0.38f, 0.82f, 1f),
                    DungeonNodeKind.EntryHub => new Color(0.45f, 0.58f, 0.72f, 1f),
                    _ => new Color(0.58f, 0.6f, 0.64f, 1f)
                };
            }

            float alpha = current ? 0.92f : visited ? 0.72f : 0.36f;
            return new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

        private static Color GetPurposeMinimapColor(DungeonRoomBuildRecord room)
        {
            if (room == null || string.IsNullOrWhiteSpace(room.purposeId))
            {
                return default;
            }

            return room.purposeId switch
            {
                "green_cache" => new Color(0.25f, 0.9f, 0.38f, 1f),
                "purple_shrine" => new Color(0.68f, 0.35f, 0.95f, 1f),
                "red_elite" => new Color(0.92f, 0.18f, 0.14f, 1f),
                "orange_ambush" => new Color(1f, 0.48f, 0.12f, 1f),
                "rainbow_wild" => new Color(1f, 0.72f, 0.95f, 1f),
                "blue_fountain" => new Color(0.25f, 0.65f, 1f, 1f),
                "gold_treasury" => new Color(1f, 0.78f, 0.18f, 1f),
                "cyan_armory" => new Color(0.22f, 0.92f, 0.95f, 1f),
                "white_sanctuary" => new Color(0.9f, 0.9f, 0.82f, 1f),
                "black_vault" => new Color(0.16f, 0.08f, 0.2f, 1f),
                "teal_scout" => new Color(0.22f, 0.78f, 0.68f, 1f),
                _ => default
            };
        }

        private static string GetRoomIcon(DungeonRoomBuildRecord room)
        {
            if (!string.IsNullOrWhiteSpace(room.purposeIcon))
            {
                return room.purposeIcon;
            }

            return room.roomType switch
            {
                DungeonNodeKind.TransitDown => "D",
                DungeonNodeKind.TransitUp => "U",
                DungeonNodeKind.Landmark => "*",
                DungeonNodeKind.Secret => "?",
                _ => string.Empty
            };
        }

        private static Color GetRoomIconColor(DungeonRoomBuildRecord room)
        {
            Color purposeColor = GetPurposeMinimapColor(room);
            if (purposeColor != default)
            {
                return purposeColor;
            }

            return room.roomType switch
            {
                DungeonNodeKind.TransitDown => new Color(1f, 0.9f, 0.35f, 1f),
                DungeonNodeKind.TransitUp => new Color(0.7f, 0.9f, 1f, 1f),
                DungeonNodeKind.Secret => new Color(0.85f, 0.6f, 1f, 1f),
                _ => Color.white
            };
        }

        private static void EncapsulateXZ(ref Bounds bounds, ref bool hasBounds, Bounds next)
        {
            Bounds xz = new Bounds(
                new Vector3(next.center.x, 0f, next.center.z),
                new Vector3(Mathf.Max(1f, next.size.x), 1f, Mathf.Max(1f, next.size.z)));
            if (!hasBounds)
            {
                bounds = xz;
                hasBounds = true;
                return;
            }

            bounds.Encapsulate(xz);
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

        private sealed class RoomElement
        {
            public readonly DungeonRoomBuildRecord room;
            public readonly Image fill;
            public readonly Image current;
            public readonly Text icon;

            public RoomElement(DungeonRoomBuildRecord room, Image fill, Image current, Text icon)
            {
                this.room = room;
                this.fill = fill;
                this.current = current;
                this.icon = icon;
            }
        }

        private readonly struct CorridorElement
        {
            public readonly DungeonCorridorBuildRecord record;
            public readonly Image image;

            public CorridorElement(DungeonCorridorBuildRecord record, Image image)
            {
                this.record = record;
                this.image = image;
            }
        }
    }

    public readonly struct MinimapCoordinateMapping
    {
        public MinimapCoordinateMapping(Bounds worldBounds, Vector2 mapSize, float scale)
        {
            this.worldBounds = worldBounds;
            this.mapSize = mapSize;
            this.scale = scale;
        }

        public readonly Bounds worldBounds;
        public readonly Vector2 mapSize;
        public readonly float scale;

        public Vector2 WorldToMap(Vector3 worldPosition)
        {
            return new Vector2(
                (worldPosition.x - worldBounds.center.x) * scale,
                (worldPosition.z - worldBounds.center.z) * scale);
        }

        public Vector2 WorldSizeToMap(Vector3 worldSize)
        {
            return new Vector2(Mathf.Max(1f, worldSize.x * scale), Mathf.Max(1f, worldSize.z * scale));
        }
    }
}
