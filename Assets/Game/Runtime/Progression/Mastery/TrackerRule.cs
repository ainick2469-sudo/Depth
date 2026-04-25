using System;
using FrontierDepths.Core;

namespace FrontierDepths.Progression.Mastery
{
    [Serializable]
    public sealed class TrackerRule
    {
        public GameplayEventType eventType;
        public string weaponId;
        public string weaponArchetype;
        public string damageType;
        public string deliveryType;
        public bool requireKilledTarget;
        public float xp;
        public float xpPerFinalAmount;
        public float xpPerDistance;

        public TrackerRule(GameplayEventType eventType, float xp)
        {
            this.eventType = eventType;
            this.xp = xp;
        }

        public bool Matches(GameplayEvent gameplayEvent)
        {
            if (gameplayEvent.eventType != eventType)
            {
                return false;
            }

            if (!MatchesOptional(weaponId, gameplayEvent.weaponId) ||
                !MatchesOptional(weaponArchetype, gameplayEvent.weaponArchetype) ||
                !MatchesOptional(damageType, gameplayEvent.damageType) ||
                !MatchesOptional(deliveryType, gameplayEvent.deliveryType))
            {
                return false;
            }

            return !requireKilledTarget || gameplayEvent.killedTarget;
        }

        public float GetXp(GameplayEvent gameplayEvent)
        {
            float total = xp;
            if (xpPerFinalAmount > 0f)
            {
                total += Math.Max(0f, gameplayEvent.finalAmount) * xpPerFinalAmount;
            }

            if (xpPerDistance > 0f)
            {
                total += Math.Max(0f, gameplayEvent.distance) * xpPerDistance;
            }

            return Math.Max(0f, total);
        }

        private static bool MatchesOptional(string expected, string actual)
        {
            return string.IsNullOrWhiteSpace(expected) ||
                   string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
        }
    }
}
