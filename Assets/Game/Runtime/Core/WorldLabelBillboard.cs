using UnityEngine;

namespace FrontierDepths.Core
{
    [RequireComponent(typeof(TextMesh))]
    public sealed class WorldLabelBillboard : MonoBehaviour
    {
        [SerializeField] private float maxVisibleDistance = 28f;
        [SerializeField] private float minScale = 0.75f;
        [SerializeField] private float maxScale = 1.35f;
        [SerializeField] private bool useOcclusion = true;
        [SerializeField] private LayerMask occlusionMask = ~0;
        [SerializeField] private Transform occlusionRoot;

        private TextMesh label;
        private Vector3 baseScale = Vector3.one;
        private Renderer cachedRenderer;

        public float MaxVisibleDistance => maxVisibleDistance;
        public bool UseOcclusion => useOcclusion;
        public Transform OcclusionRoot => occlusionRoot;

        private void Awake()
        {
            label = GetComponent<TextMesh>();
            cachedRenderer = GetComponent<Renderer>();
            baseScale = transform.localScale;
            ConfigureTextDefaults();
        }

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                SetVisible(false);
                return;
            }

            Vector3 toLabel = transform.position - camera.transform.position;
            float distance = toLabel.magnitude;
            if (distance > maxVisibleDistance || distance <= 0.01f || IsOccluded(camera, distance))
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            transform.rotation = GetBillboardRotation(camera.transform.position, transform.position);
            float scale = Mathf.Clamp(distance / 14f, minScale, maxScale);
            transform.localScale = baseScale * scale;
        }

        public void Configure(string text, Color color, float distance = 28f, bool occlude = true)
        {
            label ??= GetComponent<TextMesh>();
            label.text = text ?? string.Empty;
            label.color = color;
            maxVisibleDistance = Mathf.Max(2f, distance);
            useOcclusion = occlude;
            ConfigureTextDefaults();
        }

        public void ConfigureOcclusionRoot(Transform root)
        {
            occlusionRoot = root;
        }

        public static WorldLabelBillboard Create(Transform parent, string name, string text, Vector3 localPosition, Color color, float distance = 28f, bool occlude = true)
        {
            GameObject labelObject = new GameObject(name, typeof(TextMesh), typeof(WorldLabelBillboard));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localScale = Vector3.one;
            WorldLabelBillboard billboard = labelObject.GetComponent<WorldLabelBillboard>();
            billboard.Configure(text, color, distance, occlude);
            billboard.ConfigureOcclusionRoot(parent);
            return billboard;
        }

        public static Quaternion GetBillboardRotation(Vector3 cameraPosition, Vector3 labelPosition)
        {
            Vector3 awayFromCamera = labelPosition - cameraPosition;
            awayFromCamera.y = 0f;
            if (awayFromCamera.sqrMagnitude <= 0.0001f)
            {
                awayFromCamera = Vector3.forward;
            }

            return Quaternion.LookRotation(awayFromCamera.normalized, Vector3.up);
        }

        private bool IsOccluded(Camera camera, float distance)
        {
            if (!useOcclusion)
            {
                return false;
            }

            Vector3 origin = camera.transform.position;
            Vector3 target = transform.position;
            Vector3 direction = target - origin;
            float rayDistance = Mathf.Max(0f, distance - 0.2f);
            if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit, rayDistance, occlusionMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return hit.collider != null && !IsIgnoredOccluder(hit.collider.transform);
        }

        private bool IsIgnoredOccluder(Transform hitTransform)
        {
            if (hitTransform == null)
            {
                return false;
            }

            if (hitTransform == transform ||
                hitTransform.IsChildOf(transform) ||
                transform.IsChildOf(hitTransform))
            {
                return true;
            }

            return occlusionRoot != null &&
                   (hitTransform == occlusionRoot ||
                    hitTransform.IsChildOf(occlusionRoot) ||
                    occlusionRoot.IsChildOf(hitTransform));
        }

        private void ConfigureTextDefaults()
        {
            if (label == null)
            {
                return;
            }

            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.22f;
            label.fontSize = 40;
            label.richText = false;
        }

        private void SetVisible(bool visible)
        {
            if (cachedRenderer == null)
            {
                cachedRenderer = GetComponent<Renderer>();
            }

            if (cachedRenderer != null)
            {
                cachedRenderer.enabled = visible;
            }
        }
    }
}
