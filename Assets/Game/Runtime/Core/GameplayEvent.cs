using System;
using UnityEngine;

namespace FrontierDepths.Core
{
    [Serializable]
    public struct GameplayEvent
    {
        public GameplayEventType eventType;
        public GameObject sourceObject;
        public GameObject targetObject;
        public string weaponId;
        public string weaponArchetype;
        public string damageType;
        public string deliveryType;
        public float amount;
        public float finalAmount;
        public float distance;
        public Vector3 worldPosition;
        public float radius;
        public string[] tags;
        public bool wasCritical;
        public bool killedTarget;
        public int floorIndex;
        public string roomId;
        public float timestamp;

        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || tags == null)
            {
                return false;
            }

            for (int i = 0; i < tags.Length; i++)
            {
                if (string.Equals(tags[i], tag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
