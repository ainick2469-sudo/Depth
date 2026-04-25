using System;
using System.Collections.Generic;

namespace FrontierDepths.Progression.Mastery
{
    [Serializable]
    public sealed class MasteryTrackerProgress
    {
        public string trackerId;
        public float xp;
        public int level;
        public int totalCount;
        public int lastMilestoneClaimed;
    }

    [Serializable]
    public sealed class MasteryProgressState
    {
        private readonly Dictionary<string, MasteryTrackerProgress> progressByTracker = new Dictionary<string, MasteryTrackerProgress>();

        public IEnumerable<MasteryTrackerProgress> AllProgress => progressByTracker.Values;

        public MasteryTrackerProgress GetOrCreate(string trackerId)
        {
            if (!progressByTracker.TryGetValue(trackerId, out MasteryTrackerProgress progress))
            {
                progress = new MasteryTrackerProgress { trackerId = trackerId };
                progressByTracker[trackerId] = progress;
            }

            return progress;
        }

        public bool TryGet(string trackerId, out MasteryTrackerProgress progress)
        {
            return progressByTracker.TryGetValue(trackerId, out progress);
        }
    }
}
