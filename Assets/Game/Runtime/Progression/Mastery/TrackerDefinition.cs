using System;
using System.Collections.Generic;

namespace FrontierDepths.Progression.Mastery
{
    [Serializable]
    public sealed class TrackerDefinition
    {
        public string trackerId;
        public string displayName;
        public string description;
        public List<TrackerRule> rules = new List<TrackerRule>();
        public List<float> levelThresholds = new List<float>();
        public List<TrackerReward> rewards = new List<TrackerReward>();

        public TrackerDefinition(string trackerId, string displayName, string description)
        {
            this.trackerId = trackerId;
            this.displayName = displayName;
            this.description = description;
        }
    }
}
