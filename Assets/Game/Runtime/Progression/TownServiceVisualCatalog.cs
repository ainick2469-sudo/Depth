using System.Collections.Generic;
using UnityEngine;

namespace FrontierDepths.Progression
{
    public static class TownServiceVisualCatalog
    {
        public const string BlacksmithVisualPath = "TownVisuals/BlacksmithVisual";
        public const string SaloonInnVisualPath = "TownVisuals/SaloonInnVisual";
        public const string QuartermasterVisualPath = "TownVisuals/QuartermasterVisual";
        public const string BountyBoardVisualPath = "TownVisuals/BountyBoardVisual";

        private static readonly TownServiceVisualDefinition[] Definitions =
        {
            new TownServiceVisualDefinition(
                "shop.blacksmith",
                "Blacksmith",
                "Press E to visit Blacksmith",
                BlacksmithVisualPath,
                new Vector3(-16f, 0f, 13f),
                new Color(0.62f, 0.33f, 0.19f),
                new Vector2(8f, 7f),
                new Vector3(0f, 1.35f, 4.55f),
                0f,
                1f,
                new Vector3(0f, 0f, 3.65f),
                new Vector3(0f, 4.25f, 2.15f),
                true),

            new TownServiceVisualDefinition(
                "shop.quartermaster",
                "Quartermaster",
                "Press E to visit Quartermaster",
                QuartermasterVisualPath,
                new Vector3(16f, 0f, 13f),
                new Color(0.25f, 0.44f, 0.63f),
                new Vector2(7.5f, 6.5f),
                new Vector3(0f, 1.35f, 4.2f),
                0f,
                1f,
                new Vector3(0f, 0f, 3.35f),
                new Vector3(0f, 4.05f, 2f),
                true),

            new TownServiceVisualDefinition(
                "shop.saloon",
                "Saloon / Inn",
                "Press E to visit Saloon / Inn",
                SaloonInnVisualPath,
                new Vector3(-17f, 0f, -11f),
                new Color(0.58f, 0.38f, 0.17f),
                new Vector2(8f, 7f),
                new Vector3(0f, 1.35f, 4.4f),
                0f,
                1f,
                new Vector3(0f, 0f, 3.55f),
                new Vector3(0f, 4.1f, 2.05f),
                true),

            new TownServiceVisualDefinition(
                "shop.bounty_board",
                "Bounty Board",
                "Press E to view Bounty Board",
                BountyBoardVisualPath,
                new Vector3(8f, 0f, -5f),
                new Color(0.27f, 0.49f, 0.27f),
                new Vector2(4.5f, 3f),
                new Vector3(0f, 1.25f, 2.5f),
                0f,
                1f,
                new Vector3(0f, 0f, 2f),
                new Vector3(0f, 3.35f, 1.2f),
                true)
        };

        public static IReadOnlyList<TownServiceVisualDefinition> All => Definitions;

        public static bool TryGet(string serviceId, out TownServiceVisualDefinition definition)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].serviceId == serviceId)
                {
                    definition = Definitions[i];
                    return true;
                }
            }

            definition = default;
            return false;
        }
    }
}
