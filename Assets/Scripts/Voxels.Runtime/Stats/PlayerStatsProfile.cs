using UnityEngine;

namespace Voxels.Runtime
{
    [CreateAssetMenu(menuName = "Voxels/Player Stats Profile", fileName = "PlayerStats_")]
    public sealed class PlayerStatsProfile : ScriptableObject
    {
        [Header("Maximums")]
        public float maxHealth = 20f;
        public float maxStamina = 100f;
        public float maxHunger = 100f;
        public float maxBreath = 100f;

        [Header("Regeneration (per second)")]
        public float healthRegenPerSecond = 1f;
        public float staminaRegenPerSecond = 20f;
        public float breathRegenPerSecond = 40f;

        [Header("Drain (per second)")]
        public float hungerDrainPerSecond = 0.08f;
        public float sprintStaminaDrainPerSecond = 25f;
        public float swimStaminaDrainPerSecond = 15f;
        public float deepBreathDrainPerSecond = 12f;

        [Header("Costs")]
        public float jumpStaminaCost = 15f;

        [Header("Thresholds")]
        [Range(0f, 1f)] public float hungerRegenHealthThreshold = 0.3f;
        public float healthRegenCombatDelaySeconds = 3f;
        [Range(0f, 0.85f)] public float maxDefensePercent = 0.8f;
        public float baseMiningMultiplier = 1f;

        [Header("Food")]
        public float fungusHungerRestore = 35f;
        public float yellowGrassHungerRestore = 20f;
    }
}
