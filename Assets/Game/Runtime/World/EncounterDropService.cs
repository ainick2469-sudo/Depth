using System.Collections.Generic;
using FrontierDepths.Combat;
using UnityEngine;

namespace FrontierDepths.World
{
    public sealed class EncounterDropService
    {
        public const string DropRootName = "EncounterDrops";
        public const int MaxActiveDropsPerFloor = 40;
        public const int MaxDropsPerEnemyDeath = 2;

        private readonly Transform parentRoot;
        private readonly List<EnemyHealth> registeredEnemies = new List<EnemyHealth>();
        private int activeDropCount;
        private bool suppressDrops;

        public EncounterDropService(Transform parentRoot)
        {
            this.parentRoot = parentRoot;
        }

        public Transform DropRoot => GetOrCreateDropRoot(parentRoot);
        public int ActiveDropCount => activeDropCount;
        public bool SuppressDrops
        {
            get => suppressDrops;
            set => suppressDrops = value;
        }

        public void RegisterEnemy(EnemyHealth enemyHealth)
        {
            if (enemyHealth == null || registeredEnemies.Contains(enemyHealth))
            {
                return;
            }

            registeredEnemies.Add(enemyHealth);
            enemyHealth.Died += HandleEnemyDied;
        }

        public void UnregisterEnemy(EnemyHealth enemyHealth)
        {
            if (enemyHealth == null)
            {
                return;
            }

            enemyHealth.Died -= HandleEnemyDied;
            registeredEnemies.Remove(enemyHealth);
        }

        public void Clear(bool immediate)
        {
            for (int i = registeredEnemies.Count - 1; i >= 0; i--)
            {
                if (registeredEnemies[i] != null)
                {
                    registeredEnemies[i].Died -= HandleEnemyDied;
                }
            }

            registeredEnemies.Clear();
            Transform root = GetOrCreateDropRoot(parentRoot);
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;
                if (immediate)
                {
                    Object.DestroyImmediate(child);
                }
                else
                {
                    Object.Destroy(child);
                }
            }

            activeDropCount = 0;
        }

        public static Transform GetOrCreateDropRoot(Transform parentRoot)
        {
            if (parentRoot == null)
            {
                return null;
            }

            Transform existing = parentRoot.Find(DropRootName);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject(DropRootName);
            root.transform.SetParent(parentRoot, false);
            return root.transform;
        }

        public static List<EncounterDropKind> RollDropsForTests(EnemyDefinition definition, int seed)
        {
            Random.State previousState = Random.state;
            Random.InitState(seed);
            List<EncounterDropKind> drops = RollDrops(definition);
            Random.state = previousState;
            return drops;
        }

        private void HandleEnemyDied(EnemyHealth enemyHealth)
        {
            if (enemyHealth == null)
            {
                return;
            }

            enemyHealth.Died -= HandleEnemyDied;
            registeredEnemies.Remove(enemyHealth);
            if (suppressDrops || !WasKilledByPlayer(enemyHealth.LastDamageInfo))
            {
                return;
            }

            SpawnDrops(enemyHealth);
        }

        private void SpawnDrops(EnemyHealth enemyHealth)
        {
            EnemyDefinition definition = enemyHealth.Definition;
            if (definition == null || parentRoot == null)
            {
                return;
            }

            List<EncounterDropKind> drops = RollDrops(definition);
            for (int i = 0; i < drops.Count; i++)
            {
                if (activeDropCount >= MaxActiveDropsPerFloor)
                {
                    Debug.LogWarning("Encounter drop cap reached; skipping extra prototype drops.");
                    return;
                }

                Vector3 offset = new Vector3((i - 0.5f) * 0.65f, 0f, 0f);
                if (CreatePickup(drops[i], definition, enemyHealth.transform.position + Vector3.up * 0.45f + offset) != null)
                {
                    activeDropCount++;
                }
            }
        }

        private static List<EncounterDropKind> RollDrops(EnemyDefinition definition)
        {
            List<EncounterDropKind> drops = new List<EncounterDropKind>(MaxDropsPerEnemyDeath);
            if (definition == null)
            {
                return drops;
            }

            if (definition.goldDropChance >= 1f || Random.value <= Mathf.Max(0f, definition.goldDropChance))
            {
                drops.Add(EncounterDropKind.Gold);
            }

            if (drops.Count < MaxDropsPerEnemyDeath &&
                (definition.healthDropChance >= 1f || Random.value <= Mathf.Max(0f, definition.healthDropChance)))
            {
                drops.Add(EncounterDropKind.Health);
            }

            if (drops.Count < MaxDropsPerEnemyDeath &&
                (definition.ammoDropChance >= 1f || Random.value <= Mathf.Max(0f, definition.ammoDropChance)))
            {
                drops.Add(EncounterDropKind.Ammo);
            }

            return drops;
        }

        private GameObject CreatePickup(EncounterDropKind kind, EnemyDefinition definition, Vector3 position)
        {
            Transform root = GetOrCreateDropRoot(parentRoot);
            if (root == null)
            {
                return null;
            }

            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickup.name = $"{kind}Pickup";
            pickup.transform.SetParent(root, true);
            pickup.transform.position = position;
            pickup.transform.localScale = Vector3.one * 0.65f;

            Collider collider = pickup.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            Renderer renderer = pickup.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard"))
                {
                    color = GetPickupColor(kind)
                };
            }

            switch (kind)
            {
                case EncounterDropKind.Health:
                    pickup.AddComponent<HealthPickup>().Configure(definition.healthAmount);
                    break;
                case EncounterDropKind.Ammo:
                    pickup.AddComponent<AmmoPickup>().Configure(definition.ammoAmount);
                    break;
                default:
                    int goldAmount = Random.Range(Mathf.Max(1, definition.goldMin), Mathf.Max(Mathf.Max(1, definition.goldMin) + 1, definition.goldMax + 1));
                    pickup.AddComponent<GoldPickup>().Configure(goldAmount);
                    break;
            }

            pickup.AddComponent<PickupDropLandingController>().BeginLanding(position);
            int defaultLayer = LayerMask.NameToLayer("Default");
            DungeonSceneController.SetLayerRecursively(pickup, defaultLayer >= 0 ? defaultLayer : 0);
            return pickup;
        }

        private static bool WasKilledByPlayer(DamageInfo damageInfo)
        {
            return damageInfo.source != null &&
                   (damageInfo.source.GetComponentInParent<PlayerWeaponController>() != null ||
                    damageInfo.source.GetComponentInParent<PlayerHealth>() != null);
        }

        private static Color GetPickupColor(EncounterDropKind kind)
        {
            return kind switch
            {
                EncounterDropKind.Health => new Color(0.25f, 0.9f, 0.38f, 1f),
                EncounterDropKind.Ammo => new Color(0.28f, 0.58f, 1f, 1f),
                _ => new Color(1f, 0.78f, 0.22f, 1f)
            };
        }
    }

    public enum EncounterDropKind
    {
        Gold,
        Health,
        Ammo
    }
}
