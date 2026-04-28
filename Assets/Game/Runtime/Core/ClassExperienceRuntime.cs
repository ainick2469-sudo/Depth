using UnityEngine;

namespace FrontierDepths.Core
{
    public sealed class ClassExperienceRuntime
    {
        private readonly ProfileService profileService;
        private bool subscribed;

        public ClassExperienceRuntime(ProfileService profileService)
        {
            this.profileService = profileService;
            Subscribe();
        }

        public bool IsSubscribedForTests => subscribed;

        public void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            GameplayEventBus.Subscribe(HandleGameplayEvent);
            subscribed = true;
        }

        public void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            GameplayEventBus.Unsubscribe(HandleGameplayEvent);
            subscribed = false;
        }

        internal void HandleGameplayEventForTests(GameplayEvent gameplayEvent)
        {
            HandleGameplayEvent(gameplayEvent);
        }

        internal static int CalculateEnemyKillClassXpForTests(GameplayEvent gameplayEvent)
        {
            return CalculateEnemyKillClassXp(gameplayEvent);
        }

        private void HandleGameplayEvent(GameplayEvent gameplayEvent)
        {
            if (gameplayEvent.eventType != GameplayEventType.EnemyKilled || profileService == null)
            {
                return;
            }

            int xp = CalculateEnemyKillClassXp(gameplayEvent);
            profileService.AddClassXp(xp, "Enemy Kill");
        }

        private static int CalculateEnemyKillClassXp(GameplayEvent gameplayEvent)
        {
            string archetype = GetFallbackArchetype(gameplayEvent.tags);
            int baseXp = archetype switch
            {
                "Slime" => 8,
                "Bat" => 10,
                "GoblinGrunt" => 16,
                "GoblinBrute" => 32,
                _ => 12
            };

            int levelOrTier = Mathf.Max(1, Mathf.RoundToInt(gameplayEvent.amount));

            return Mathf.Max(1, baseXp + levelOrTier * 2);
        }

        private static string GetFallbackArchetype(string[] tags)
        {
            if (tags != null)
            {
                for (int i = 0; i < tags.Length; i++)
                {
                    string tag = tags[i];
                    if (tag == "Slime" || tag == "Bat" || tag == "GoblinGrunt" || tag == "GoblinBrute")
                    {
                        return tag;
                    }
                }
            }

            return "GoblinGrunt";
        }
    }
}
