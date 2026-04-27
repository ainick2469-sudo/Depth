using UnityEngine;

namespace FrontierDepths.Progression
{
    public sealed class TownRuntimeKioskBuilder : MonoBehaviour
    {
        public const string RootName = "RuntimeTownKiosks";

        private static readonly KioskDefinition[] Kiosks =
        {
            new KioskDefinition("Blacksmith", "shop.blacksmith", "Talk to the Blacksmith", new Vector3(-18f, 0f, 14f), 32f, new Color(0.55f, 0.28f, 0.18f)),
            new KioskDefinition("Quartermaster", "shop.quartermaster", "Browse the General Store", new Vector3(18f, 0f, 14f), -32f, new Color(0.22f, 0.42f, 0.62f)),
            new KioskDefinition("Saloon / Inn", "shop.curio", "Visit the Saloon", new Vector3(-18f, 0f, -12f), 145f, new Color(0.56f, 0.38f, 0.16f)),
            new KioskDefinition("Bounty Board", "shop.bounty_board", "Read the Bounty Board", new Vector3(18f, 0f, -12f), -145f, new Color(0.25f, 0.5f, 0.28f)),
            new KioskDefinition("Dungeon Gate", string.Empty, "Enter the Dungeon", new Vector3(0f, 0f, 30f), 180f, new Color(0.58f, 0.2f, 0.16f))
        };

        private void Start()
        {
            EnsureRuntimeKiosks(transform);
        }

        public static Transform EnsureRuntimeKiosks(Transform parent)
        {
            Transform safeParent = parent != null ? parent : FindAnyObjectByType<TownHubController>()?.transform;
            if (safeParent == null)
            {
                return null;
            }

            Transform existing = safeParent.Find(RootName);
            if (existing != null)
            {
                return existing;
            }

            GameObject rootObject = new GameObject(RootName);
            rootObject.transform.SetParent(safeParent, false);
            Transform root = rootObject.transform;
            for (int i = 0; i < Kiosks.Length; i++)
            {
                CreateKiosk(root, Kiosks[i]);
            }

            return root;
        }

        private static void CreateKiosk(Transform root, KioskDefinition definition)
        {
            GameObject kiosk = new GameObject($"Kiosk_{definition.label}");
            kiosk.transform.SetParent(root, false);
            kiosk.transform.localPosition = definition.position;
            kiosk.transform.localRotation = Quaternion.Euler(0f, definition.yaw, 0f);

            CreateBox(kiosk.transform, "BackWall", new Vector3(0f, 2f, 1.2f), new Vector3(5.8f, 3.4f, 0.35f), definition.color);
            CreateBox(kiosk.transform, "LeftPost", new Vector3(-2.8f, 1.5f, -0.8f), new Vector3(0.35f, 3f, 2.4f), definition.color);
            CreateBox(kiosk.transform, "RightPost", new Vector3(2.8f, 1.5f, -0.8f), new Vector3(0.35f, 3f, 2.4f), definition.color);
            CreateBox(kiosk.transform, "Counter", new Vector3(0f, 0.75f, -1.45f), new Vector3(4.8f, 1f, 0.8f), Color.Lerp(definition.color, Color.white, 0.18f));
            CreateLabel(kiosk.transform, definition.label, new Vector3(0f, 3.7f, -1.35f));

            if (!string.IsNullOrWhiteSpace(definition.shopId))
            {
                GameObject station = new GameObject("ServiceStation", typeof(BoxCollider), typeof(TownServiceStation));
                station.transform.SetParent(kiosk.transform, false);
                station.transform.localPosition = new Vector3(0f, 1.4f, -2.35f);
                station.transform.localScale = new Vector3(4.8f, 2.2f, 1.5f);
                BoxCollider collider = station.GetComponent<BoxCollider>();
                collider.isTrigger = true;
                station.GetComponent<TownServiceStation>().Configure(definition.shopId, definition.prompt);
            }
        }

        private static void CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = localScale;
            Renderer renderer = box.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard")) { color = color };
            }
        }

        private static void CreateLabel(Transform parent, string text, Vector3 localPosition)
        {
            GameObject labelObject = new GameObject("SignLabel", typeof(TextMesh));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            TextMesh label = labelObject.GetComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.34f;
            label.fontSize = 42;
            label.color = Color.white;
        }

        private readonly struct KioskDefinition
        {
            public readonly string label;
            public readonly string shopId;
            public readonly string prompt;
            public readonly Vector3 position;
            public readonly float yaw;
            public readonly Color color;

            public KioskDefinition(string label, string shopId, string prompt, Vector3 position, float yaw, Color color)
            {
                this.label = label;
                this.shopId = shopId;
                this.prompt = prompt;
                this.position = position;
                this.yaw = yaw;
                this.color = color;
            }
        }
    }
}
