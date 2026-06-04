namespace Voxels.Runtime
{
    public static class PlayerStatsRules
    {
        public static float ApplyDefense(float rawDamage, float defensePercent, float maxDefense)
        {
            float reduction = UnityEngine.Mathf.Clamp(defensePercent, 0f, maxDefense);
            return rawDamage * (1f - reduction);
        }

        public static float ApplyMiningSpeed(float breakDuration, float miningMultiplier)
        {
            if (miningMultiplier <= 0.01f)
            {
                return breakDuration;
            }

            return breakDuration / miningMultiplier;
        }
    }
}
