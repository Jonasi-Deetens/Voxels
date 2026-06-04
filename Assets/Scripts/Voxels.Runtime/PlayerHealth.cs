using UnityEngine;

namespace Voxels.Runtime
{
    /// <summary>Compatibility facade; health is owned by <see cref="PlayerStatsController"/>.</summary>
    public sealed class PlayerHealth : MonoBehaviour
    {
        PlayerStatsController stats;

        void Awake() => stats = GetComponent<PlayerStatsController>();

        public float Health => stats != null ? stats.GetCurrent(StatId.Health) : 0f;
        public float MaxHealth => stats != null ? stats.GetMax(StatId.Health) : 20f;
        public float Normalized => stats != null ? stats.GetNormalized(StatId.Health) : 0f;

        public void Restore(float amount) => stats?.Add(StatId.Health, amount);

        public void SetHealth(float value)
        {
            if (stats == null)
            {
                return;
            }

            stats.RestoreFromSave(value, stats.GetCurrent(StatId.Stamina), stats.GetCurrent(StatId.Hunger), stats.GetCurrent(StatId.Breath));
        }

        public void TakeDamage(float amount) => stats?.ApplyDamage(amount);
    }
}
