using System;

namespace FrontierDepths.Combat
{
    [Serializable]
    public struct StatModifier
    {
        public string statId;
        public float additive;
        public float multiplier;
    }
}
