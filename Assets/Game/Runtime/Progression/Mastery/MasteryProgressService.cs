using System.Collections.Generic;
using System.Text;
using FrontierDepths.Core;
using UnityEngine;

namespace FrontierDepths.Progression.Mastery
{
    public sealed class MasteryProgressService
    {
        private readonly List<TrackerDefinition> definitions;
        private readonly MasteryProgressState state;
        private readonly bool debugLogProgress;
        private bool subscribed;

        public MasteryProgressService(IEnumerable<TrackerDefinition> definitions, MasteryProgressState state = null, bool debugLogProgress = false)
        {
            this.definitions = definitions != null ? new List<TrackerDefinition>(definitions) : new List<TrackerDefinition>();
            this.state = state ?? new MasteryProgressState();
            this.debugLogProgress = debugLogProgress;
        }

        public MasteryProgressState State => state;

        public void StartListening()
        {
            if (subscribed)
            {
                return;
            }

            GameplayEventBus.Subscribe(HandleEvent);
            subscribed = true;
        }

        public void StopListening()
        {
            if (!subscribed)
            {
                return;
            }

            GameplayEventBus.Unsubscribe(HandleEvent);
            subscribed = false;
        }

        public void HandleEvent(GameplayEvent gameplayEvent)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                TrackerDefinition definition = definitions[i];
                float gainedXp = GetXpForDefinition(definition, gameplayEvent);
                if (gainedXp <= 0f)
                {
                    continue;
                }

                MasteryTrackerProgress progress = state.GetOrCreate(definition.trackerId);
                progress.xp += gainedXp;
                progress.totalCount++;
                int previousLevel = progress.level;
                progress.level = CalculateLevel(definition, progress.xp);

                if (debugLogProgress && (Debug.isDebugBuild || Application.isEditor))
                {
                    string levelSuffix = progress.level > previousLevel ? $" LEVEL {progress.level}" : string.Empty;
                    Debug.Log($"Mastery +{gainedXp:0.#} XP: {definition.displayName} ({progress.xp:0.#} XP){levelSuffix}");
                }
            }
        }

        public MasteryTrackerProgress GetProgress(string trackerId)
        {
            return state.GetOrCreate(trackerId);
        }

        public string GetDebugSummary(int maxTrackers = 10)
        {
            StringBuilder builder = new StringBuilder("Mastery");
            int count = 0;
            foreach (MasteryTrackerProgress progress in state.AllProgress)
            {
                if (count >= maxTrackers)
                {
                    break;
                }

                builder.Append(" | ");
                builder.Append(progress.trackerId);
                builder.Append(": L");
                builder.Append(progress.level);
                builder.Append(" ");
                builder.Append(progress.xp.ToString("0.#"));
                builder.Append("xp");
                count++;
            }

            return builder.ToString();
        }

        private static float GetXpForDefinition(TrackerDefinition definition, GameplayEvent gameplayEvent)
        {
            float xp = 0f;
            for (int i = 0; i < definition.rules.Count; i++)
            {
                TrackerRule rule = definition.rules[i];
                if (rule.Matches(gameplayEvent))
                {
                    xp += rule.GetXp(gameplayEvent);
                }
            }

            return xp;
        }

        private static int CalculateLevel(TrackerDefinition definition, float xp)
        {
            int level = 0;
            for (int i = 0; i < definition.levelThresholds.Count; i++)
            {
                if (xp >= definition.levelThresholds[i])
                {
                    level = i + 1;
                }
            }

            return level;
        }
    }
}
