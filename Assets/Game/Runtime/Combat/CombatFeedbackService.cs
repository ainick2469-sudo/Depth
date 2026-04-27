using UnityEngine;

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
        private bool warnedMissingCamera;

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
            DontDestroyOnLoad(root);
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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureInitialized();
        }

        private void Update()
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
            if (poolTransform == null)
            {
                GameObject poolObject = new GameObject("SharedCombatDamageNumberPool");
                poolObject.transform.SetParent(transform, false);
                poolTransform = poolObject.transform;
            }

            for (int i = 0; i < damageNumbers.Length; i++)
            {
                Transform existing = poolTransform.Find($"DamageNumber_{i}");
                GameObject numberObject = existing != null
                    ? existing.gameObject
                    : new GameObject($"DamageNumber_{i}", typeof(TextMesh));
                numberObject.transform.SetParent(poolTransform, false);
                TextMesh text = numberObject.GetComponent<TextMesh>();
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.characterSize = 0.24f;
                text.fontSize = 52;
                numberObject.SetActive(false);
                damageNumbers[i] = new DamageNumberMarker(numberObject, text);
            }

            initialized = true;
        }

        private bool TryShow(Vector3 point, float amount, Color color, bool killedTarget, string label)
        {
            EnsureInitialized();
            if (!warnedMissingCamera && Camera.main == null)
            {
                warnedMissingCamera = true;
                Debug.LogWarning("CombatFeedbackService has no Camera.main; damage numbers will not billboard until a camera is available.");
            }

            DamageNumberMarker marker = damageNumbers[nextDamageNumber];
            nextDamageNumber = (nextDamageNumber + 1) % damageNumbers.Length;
            return marker != null &&
                   marker.Show(point + Vector3.up * 1.25f, Time.time + DamageNumberDuration, amount, color, killedTarget, label);
        }

        private sealed class DamageNumberMarker
        {
            private readonly GameObject markerObject;
            private readonly TextMesh text;
            private Vector3 startPosition;
            private float showTime;
            private float hideTime;

            public DamageNumberMarker(GameObject markerObject, TextMesh text)
            {
                this.markerObject = markerObject;
                this.text = text;
            }

            public bool IsValid => markerObject != null && text != null;

            public bool Show(Vector3 position, float hideTime, float amount, Color color, bool killedTarget, string label)
            {
                if (markerObject == null || text == null)
                {
                    return false;
                }

                startPosition = position;
                showTime = Time.time;
                this.hideTime = hideTime;
                string prefix = string.IsNullOrWhiteSpace(label) ? string.Empty : $"{label} ";
                text.text = $"{prefix}{amount:0}";
                text.color = killedTarget ? Color.red : color;
                markerObject.transform.position = position;
                markerObject.transform.localScale = Vector3.one * (killedTarget ? 1.22f : 1f);
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
                markerObject.transform.position = startPosition + Vector3.up * (0.35f + t * 0.55f);
                if (camera != null)
                {
                    Vector3 away = markerObject.transform.position - camera.transform.position;
                    away.y = 0f;
                    if (away.sqrMagnitude > 0.0001f)
                    {
                        markerObject.transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
                    }
                }
            }
        }
    }
}
