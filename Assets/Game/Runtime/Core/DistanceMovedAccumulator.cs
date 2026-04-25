namespace FrontierDepths.Core
{
    public static class DistanceMovedAccumulator
    {
        public static bool TryAccumulate(ref float accumulatedDistance, float deltaDistance, float threshold, out float emittedDistance)
        {
            emittedDistance = 0f;
            if (deltaDistance <= 0f)
            {
                return false;
            }

            accumulatedDistance += deltaDistance;
            if (accumulatedDistance < threshold)
            {
                return false;
            }

            emittedDistance = accumulatedDistance;
            accumulatedDistance = 0f;
            return true;
        }
    }
}
