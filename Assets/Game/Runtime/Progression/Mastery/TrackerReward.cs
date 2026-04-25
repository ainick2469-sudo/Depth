using System;

namespace FrontierDepths.Progression.Mastery
{
    [Serializable]
    public sealed class TrackerReward
    {
        public int level;
        public string rewardId;
        public string description;

        public TrackerReward(int level, string rewardId, string description)
        {
            this.level = level;
            this.rewardId = rewardId;
            this.description = description;
        }
    }
}
