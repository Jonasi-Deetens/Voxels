using UnityEngine;

namespace Voxels.Runtime
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField] float maxHealth = 20f;
        float health;
        float invulnTimer;

        public float Health => health;
        public float MaxHealth => maxHealth;
        public float Normalized => maxHealth > 0f ? health / maxHealth : 0f;

        void Awake() => health = maxHealth;

        public void Restore(float amount) => health = Mathf.Min(maxHealth, health + amount);

        public void SetHealth(float value) => health = Mathf.Clamp(value, 0f, maxHealth);

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || invulnTimer > 0f)
            {
                return;
            }

            health = Mathf.Max(0f, health - amount);
            invulnTimer = 0.75f;
        }

        void Update()
        {
            if (invulnTimer > 0f)
            {
                invulnTimer -= Time.deltaTime;
            }
        }
    }
}
