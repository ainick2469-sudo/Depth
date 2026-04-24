using UnityEngine;

namespace FrontierDepths.Core
{
    public static class InputFrameGuard
    {
        private static int townServiceCloseConsumedFrame = -1;

        public static bool WasTownServiceCloseConsumedThisFrame => townServiceCloseConsumedFrame == Time.frameCount;

        public static void MarkTownServiceCloseConsumedThisFrame()
        {
            townServiceCloseConsumedFrame = Time.frameCount;
        }
    }
}
