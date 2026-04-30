using FrontierDepths.Core;
using UnityEngine.SceneManagement;

namespace FrontierDepths.World
{
    public readonly struct WorldFloorSceneContext
    {
        public readonly WorldLocationKind locationKind;
        public readonly int worldFloor;
        public readonly string floorName;
        public readonly string areaId;
        public readonly string areaName;
        public readonly string secondaryLabel;

        public WorldFloorSceneContext(
            WorldLocationKind locationKind,
            int worldFloor,
            string floorName,
            string areaId,
            string areaName,
            string secondaryLabel)
        {
            this.locationKind = locationKind;
            this.worldFloor = worldFloor <= 0 ? 1 : worldFloor;
            this.floorName = floorName ?? string.Empty;
            this.areaId = areaId ?? string.Empty;
            this.areaName = areaName ?? string.Empty;
            this.secondaryLabel = secondaryLabel ?? string.Empty;
        }

        public bool ShouldShowHudLabel => locationKind != WorldLocationKind.MainMenu;

        public string PrimaryLabel => ShouldShowHudLabel
            ? $"World Floor {worldFloor} - {WorldFloorCatalog.GetFloorDisplayName(worldFloor)}"
            : string.Empty;

        public string SecondaryLabel => ShouldShowHudLabel ? secondaryLabel : string.Empty;

        public string FormatHudLabel()
        {
            if (!ShouldShowHudLabel)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(SecondaryLabel)
                ? PrimaryLabel
                : $"{PrimaryLabel}\n{SecondaryLabel}";
        }

        public static WorldFloorSceneContext ResolveCurrent(ProfileState profile)
        {
            return ResolveForScene(SceneManager.GetActiveScene().name, profile);
        }

        public static WorldFloorSceneContext ResolveForScene(string sceneName, ProfileState profile)
        {
            int floor = GetCurrentWorldFloor(profile);
            if (sceneName == GameSceneCatalog.MainMenu)
            {
                return Create(WorldLocationKind.MainMenu, floor);
            }

            if (sceneName == GameSceneCatalog.TownHub)
            {
                return Create(WorldLocationKind.Settlement, floor);
            }

            if (sceneName == GameSceneCatalog.DungeonRuntime)
            {
                return Create(WorldLocationKind.Labyrinth, floor);
            }

            return Create(WorldLocationKind.OuterField, floor);
        }

        public static WorldFloorSceneContext Create(WorldLocationKind kind, int floor)
        {
            floor = floor <= 0 ? 1 : floor;
            WorldFloorCatalog.TryGet(floor, out WorldFloorDefinition definition);
            string floorName = definition != null ? definition.floorName : WorldFloorCatalog.GetFloorDisplayName(floor);

            return kind switch
            {
                WorldLocationKind.MainMenu => new WorldFloorSceneContext(kind, floor, floorName, string.Empty, string.Empty, string.Empty),
                WorldLocationKind.Settlement => new WorldFloorSceneContext(
                    kind,
                    floor,
                    floorName,
                    definition != null ? definition.PrimarySettlementId : string.Empty,
                    definition != null ? definition.PrimarySettlementName : string.Empty,
                    "Settlement: Safe Zone"),
                WorldLocationKind.Labyrinth => new WorldFloorSceneContext(
                    kind,
                    floor,
                    floorName,
                    definition != null ? definition.labyrinthId : string.Empty,
                    definition != null ? definition.labyrinthName : string.Empty,
                    definition != null && !string.IsNullOrWhiteSpace(definition.labyrinthName) ? definition.labyrinthName : "Labyrinth"),
                WorldLocationKind.SafeRoom => new WorldFloorSceneContext(kind, floor, floorName, "safe_room", "Safe Room", "Safe Room"),
                WorldLocationKind.BossRoom => new WorldFloorSceneContext(kind, floor, floorName, "boss_room", "Boss Room", "Boss Room"),
                _ => new WorldFloorSceneContext(kind, floor, floorName, "outer_field", "Outer Field", "Outer Field")
            };
        }

        private static int GetCurrentWorldFloor(ProfileState profile)
        {
            if (profile == null)
            {
                return 1;
            }

            profile.worldFloorProgression ??= new WorldFloorProgressionProfileState();
            profile.worldFloorProgression.Normalize();
            return profile.worldFloorProgression.currentWorldFloor;
        }
    }
}
