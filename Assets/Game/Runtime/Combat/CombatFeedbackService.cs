using UnityEngine;
using UnityEngine.UI;

namespace FrontierDepths.Combat
{
    public sealed class CombatFeedbackService : MonoBehaviour
    {
        private const string RootName = "RuntimeCombatFeedbackService";
        private const int DamageNumberPoolSize = 32;
        private const float DamageNumberDuration = 0.75f;

        private readonly DamageNumberMarker[] damageNumbers = new DamageNumberMarker[DamageNumberPoolSize];
        private int nextDamageNumber;
        private bool initialized;
        private Canvas damageCanvas;

        public static CombatFeedbackService Instance { get; private set; }

        public static CombatFeedbackService GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameObject existing = GameObject.Find(RootName);
            if (existing != null && existing.TryGetComponent(out CombatFeedbackService existingService))
            {
                Instance = existingService;
                existingService.EnsureInitialized();
                return Instance;
            }

            GameObject root = new GameObject(RootName, typeof(CombatFeedbackService));
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(root);
            }

            Instance = root.GetComponent<CombatFeedbackService>();
            Instance.EnsureInitialized();
            return Instance;
        }

        public static void ShowDamageNumber(Vector3 point, float amount, Color color, bool killedTarget, string label = "")
        {
            if (!GetOrCreate().TryShow(point, amount, color, killedTarget, label))
            {
                Debug.LogWarning("CombatFeedbackService received damage feedback but could not display it.");
            }
        }

        internal int PoolSizeForTests => damageNumbers.Length;
        internal int ActiveMarkerCountForTests => CountActiveMarkers();

        internal static Quaternion GetDamageNumberBillboardRotationForTests(Vector3 markerPosition, Camera camera)
        {
            if (camera == null)
            {
                return Quaternion.identity;
            }

            Vector3 toCamera = camera.transform.position - markerPosition;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude <= 0.0001f)
            {
                toCamera = -camera.transform.forward;
                toCamera.y = 0f;
            }

            return toCamera.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(toCamera.normalized, Vector3.up)
                : Quaternion.identity;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                DestroyServiceObject(gameObject);
                return;
            }

            Instance = this;
            EnsureInitialized();
        }

        private static void DestroyServiceObject(GameObject target)
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

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            for (int i = 0; i < damageNumbers.Length; i++)
            {
                damageNumbers[i]?.Tick(Time.time, camera);
            }
        }

        private void EnsureInitialized()
        {
            if (initialized && damageNumbers[0] != null && damageNumbers[0].IsValid)
            {
                return;
            }

            initialized = false;
            Transform poolTransform = transform.Find("SharedCombatDamageNumberPool");
            EnsureDamageCanvas(ref poolTransform);
            if (poolTransform == null && damageCanvas != null)
            {
                poolTransform = damageCanvas.transform.Find("SharedCombatDamageNumberPool");
            }

            if (poolTransform == null)
            {
                GameObject poolObject = new GameObject("SharedCombatDamageNumberPool");
                poolObject.transform.SetParent(damageCanvas != null ? damageCanvas.transform : transform, false);
                poolTransform = poolObject.transform;
            }

            for (int i = 0; i < damageNumbers.Length; i++)
            {
                Transform existing = poolTransform.Find($"DamageNumber_{i}");
                GameObject numberObject = existing != null
                    ? existing.gameObject
                    : new GameObject($"DamageNumber_{i}", typeof(RectTransform), typeof(CanvasGroup), typeof(Text));
                numberObject.transform.SetParent(poolTransform, false);
                RectTransform rect = numberObject.GetComponent<RectTransform>() ?? numberObject.AddComponent<RectTransform>();
                CanvasGroup group = numberObject.GetComponent<CanvasGroup>() ?? numberObject.AddComponent<CanvasGroup>();
                Text text = numberObject.GetComponent<Text>() ?? numberObject.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = 28;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.raycastTarget = false;
                rect.sizeDelta = new Vector2(120f, 42f);
                numberObject.SetActive(false);
                damageNumbers[i] = new DamageNumberMarker(numberObject, rect, group, text);
            }

            initialized = true;
        }

        private bool TryShow(Vector3 point, float amount, Color color, bool killedTarget, string label)
        {
            EnsureInitialized();
            DamageNumberMarker marker = damageNumbers[nextDamageNumber];
            nextDamageNumber = (nextDamageNumber + 1) % damageNumbers.Length;
            return marker != null &&
                   marker.Show(point + Vector3.up * 1.25f, Time.time + DamageNumberDuration, amount, color, killedTarget, label);
        }

        private void EnsureDamageCanvas(ref Transform poolTransform)
        {
            if (damageCanvas == null)
            {
                Transform canvasTransform = transform.Find("ScreenSpaceDamageNumberCanvas");
                if (canvasTransform == null)
                {
                    GameObject canvasObject = new GameObject("ScreenSpaceDamageNumberCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                    canvasObject.transform.SetParent(transform, false);
                    canvasTransform = canvasObject.transform;
                }

                damageCanvas = canvasTransform.GetComponent<Canvas>();
                damageCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                damageCanvas.sortingOrder = 1200;
                CanvasScaler scaler = canvasTransform.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            if (poolTransform != null && poolTransform.parent != damageCanvas.transform)
            {
                poolTransform.SetParent(damageCanvas.transform, false);
            }
        }

        private int CountActiveMarkers()
        {
            int count = 0;
            for (int i = 0; i < damageNumbers.Length; i++)
            {
                if (damageNumbers[i] != null && damageNumbers[i].IsActive)
                {
                    count++;
                }
            }

            return count;
        }

        private sealed class DamageNumberMarker
        {
            private readonly GameObject markerObject;
            private readonly RectTransform rectTransform;
            private readonly CanvasGroup canvasGroup;
            private readonly Text text;
            private Vector3 startPosition;
            private float showTime;
            private float hideTime;

            public DamageNumberMarker(GameObject markerObject, RectTransform rectTransform, CanvasGroup canvasGroup, Text text)
            {
                this.markerObject = markerObject;
                this.rectTransform = rectTransform;
                this.canvasGroup = canvasGroup;
                this.text = text;
            }

            public bool IsValid => markerObject != null && rectTransform != null && text != null;
            public bool IsActive => markerObject != null && markerObject.activeSelf;

            public bool Show(Vector3 position, float hideTime, float amount, Color color, bool killedTarget, string label)
            {
                if (markerObject == null || rectTransform == null || text == null)
                {
                    return false;
                }

                startPosition = position;
                showTime = Time.time;
                this.hideTime = hideTime;
                string prefix = string.IsNullOrWhiteSpace(label) ? string.Empty : $"{label} ";
                text.text = $"{prefix}{amount:0}";
                text.color = killedTarget ? Color.red : color;
                rectTransform.localScale = Vector3.one * (killedTarget ? 1.22f : 1f);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }
                markerObject.SetActive(true);
                return true;
            }

            public void Tick(float currentTime, Camera camera)
            {
                if (markerObject == null || !markerObject.activeSelf)
                {
                    return;
                }

                if (currentTime >= hideTime)
                {
                    markerObject.SetActive(false);
                    return;
                }

                float t = Mathf.Clamp01((currentTime - showTime) / Mathf.Max(0.01f, hideTime - showTime));
                if (camera == null)
                {
                    markerObject.SetActive(false);
                    return;
                }

                Vector3 screenPosition = camera.WorldToScreenPoint(startPosition);
                if (screenPosition.z <= 0f)
                {
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = 0f;
                    }

                    return;
                }

                rectTransform.position = screenPosition + Vector3.up * Mathf.Lerp(16f, 58f, t);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                }
            }
        }
    }
}
