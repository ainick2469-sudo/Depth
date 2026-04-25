using System.Collections.Generic;
using FrontierDepths.Core;

namespace FrontierDepths.Progression.Mastery
{
    public static class MasteryTrackerCatalog
    {
        public const string StarterRevolverId = "weapon.frontier_revolver";

        public static List<TrackerDefinition> CreateStarterTrackers()
        {
            return new List<TrackerDefinition>
            {
                CreateBallistic(),
                CreateRevolver(),
                CreatePhysical(),
                CreateMarksman(),
                CreateReload(),
                CreateDryFire(),
                CreateExplorer(),
                CreateStair(),
                CreateMobility(),
                CreateFrontierLegend()
            };
        }

        private static TrackerDefinition CreateBallistic()
        {
            TrackerDefinition tracker = Create("mastery.ballistic", "Ballistic Mastery", "Practice with raycast/firearm weapons.");
            tracker.rules.Add(new TrackerRule(GameplayEventType.WeaponFired, 1f) { weaponArchetype = "Revolver" });
            tracker.rules.Add(new TrackerRule(GameplayEventType.WeaponHit, 5f) { deliveryType = "Raycast" });
            tracker.rules.Add(new TrackerRule(GameplayEventType.DamageDealt, 1f) { deliveryType = "Raycast", xpPerFinalAmount = 0.08f });
            return tracker;
        }

        private static TrackerDefinition CreateRevolver()
        {
            TrackerDefinition tracker = Create("mastery.revolver", "Revolver Mastery", "Familiarity with the starter revolver.");
            tracker.rules.Add(new TrackerRule(GameplayEventType.WeaponFired, 1f) { weaponId = StarterRevolverId });
            tracker.rules.Add(new TrackerRule(GameplayEventType.WeaponHit, 6f) { weaponId = StarterRevolverId });
            return tracker;
        }

        private static TrackerDefinition CreatePhysical()
        {
            TrackerDefinition tracker = Create("mastery.physical", "Physical Mastery", "Physical damage dealt through weapons and abilities.");
            tracker.rules.Add(new TrackerRule(GameplayEventType.DamageDealt, 1f) { damageType = "Physical", xpPerFinalAmount = 0.12f });
            return tracker;
        }

        private static TrackerDefinition CreateMarksman()
        {
            TrackerDefinition tracker = Create("mastery.marksman", "Marksman Mastery", "Accurate ranged hits.");
            tracker.rules.Add(new TrackerRule(GameplayEventType.WeaponHit, 5f));
            return tracker;
        }

        private static TrackerDefinition CreateReload()
        {
            TrackerDefinition tracker = Create("mastery.reload", "Reload Mastery", "Clean reload timing.");
            tracker.rules.Add(new TrackerRule(GameplayEventType.ReloadFinished, 3f));
            return tracker;
        }

        private static TrackerDefinition CreateDryFire()
        {
            TrackerDefinition tracker = Create("mastery.dry_fire", "Dry-Fire Discipline", "Debug tracker for empty trigger pulls.");
            tracker.rules.Add(new TrackerRule(GameplayEventType.DryFire, 1f));
            return tracker;
        }

        private static TrackerDefinition CreateExplorer()
        {
            TrackerDefinition tracker = Create("mastery.explorer", "Explorer Mastery", "Placeholder for discovering rooms and secrets.");
            tracker.rules.Add(new TrackerRule(GameplayEventType.RoomDiscovered, 8f));
            return tracker;
        }

        private static TrackerDefinition CreateStair()
        {
            TrackerDefinition tracker = Create("mastery.stairs", "Stair Mastery", "Using dungeon stairs and floor routes.");
            tracker.rules.Add(new TrackerRule(GameplayEventType.StairsUsed, 10f));
            return tracker;
        }

        private static TrackerDefinition CreateMobility()
        {
            TrackerDefinition tracker = Create("mastery.mobility", "Mobility Mastery", "Movement, jumps, and traversal.");
            tracker.rules.Add(new TrackerRule(GameplayEventType.PlayerJumped, 2f));
            tracker.rules.Add(new TrackerRule(GameplayEventType.DistanceMoved, 0f) { xpPerDistance = 0.2f });
            return tracker;
        }

        private static TrackerDefinition CreateFrontierLegend()
        {
            TrackerDefinition tracker = Create("mastery.frontier_legend", "Frontier Legend", "A tiny meta progression tracker for varied action.");
            tracker.rules.Add(new TrackerRule(GameplayEventType.WeaponFired, 0.2f));
            tracker.rules.Add(new TrackerRule(GameplayEventType.WeaponHit, 0.5f));
            tracker.rules.Add(new TrackerRule(GameplayEventType.DamageDealt, 0.2f));
            tracker.rules.Add(new TrackerRule(GameplayEventType.ReloadFinished, 0.4f));
            tracker.rules.Add(new TrackerRule(GameplayEventType.StairsUsed, 1f));
            tracker.rules.Add(new TrackerRule(GameplayEventType.DistanceMoved, 0f) { xpPerDistance = 0.04f });
            return tracker;
        }

        private static TrackerDefinition Create(string id, string name, string description)
        {
            TrackerDefinition tracker = new TrackerDefinition(id, name, description);
            tracker.levelThresholds.AddRange(new[] { 10f, 25f, 50f, 100f, 200f });
            return tracker;
        }
    }
}
