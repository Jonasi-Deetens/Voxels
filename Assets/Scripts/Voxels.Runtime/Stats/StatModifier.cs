using System;

namespace Voxels.Runtime
{
    [Serializable]
    public struct StatModifier
    {
        public string source;
        public float defensePercent;
        public float miningMultiplier;
        public float duration;

        public StatModifier(string sourceId, float defense, float mining, float seconds = -1f)
        {
            source = sourceId;
            defensePercent = defense;
            miningMultiplier = mining;
            duration = seconds;
        }
    }
}
