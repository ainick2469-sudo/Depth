using UnityEngine;

namespace FrontierDepths.Core
{
    public static class InputFrameGuard
    {
        private static int townServiceOpenConsumedFrame = -1;
        private static int townServiceCloseConsumedFrame = -1;

        public static bool WasTownServiceOpenConsumedThisFrame => townServiceOpenConsumedFrame == Time.frameCount;
        public static bool WasTownServiceCloseConsumedThisFrame => townServiceCloseConsumedFrame == Time.frameCount;
        public static bool WasTownServiceInputConsumedThisFrame => WasTownServiceOpenConsumedThisFrame || WasTownServiceCloseConsumedThisFrame;

        public static void MarkTownServiceOpenConsumedThisFrame()
        {
            townServiceOpenConsumedFrame = Time.frameCount;
        }

        public static void MarkTownServiceCloseConsumedThisFrame()
        {
            townServiceCloseConsumedFrame = Time.frameCount;
        }
    }
}
