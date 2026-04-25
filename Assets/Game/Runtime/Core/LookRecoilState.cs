using UnityEngine;

namespace FrontierDepths.Core
{
    public struct LookRecoilState
    {
        private Vector2 offsetDegrees;
        private float recoverySeconds;

        public Vector2 OffsetDegrees => offsetDegrees;

        public void AddImpulse(float pitchUpDegrees, float yawDegrees, float recoveryDuration)
        {
            offsetDegrees += new Vector2(-Mathf.Abs(pitchUpDegrees), yawDegrees);
            recoverySeconds = Mathf.Max(0.01f, recoveryDuration);
        }

        public void Tick(float deltaTime)
        {
            if (offsetDegrees.sqrMagnitude <= 0.0001f)
            {
                offsetDegrees = Vector2.zero;
                return;
            }

            float t = Mathf.Clamp01(Mathf.Max(0f, deltaTime) / Mathf.Max(0.01f, recoverySeconds));
            offsetDegrees = Vector2.Lerp(offsetDegrees, Vector2.zero, t);
            if (offsetDegrees.sqrMagnitude <= 0.0001f)
            {
                offsetDegrees = Vector2.zero;
            }
        }
    }
}
