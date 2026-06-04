using System.Collections.Generic;
using UnityEngine;
using Voxels.World;
using Voxels.World.Climate;

namespace Voxels.Runtime
{
    public sealed class PlayerStatsController : MonoBehaviour
    {
        [SerializeField] PlayerStatsProfile profile;

        readonly List<StatModifier> modifiers = new List<StatModifier>();
        readonly Dictionary<StatId, float> current = new Dictionary<StatId, float>();

        float lastDamageTime = -999f;
        bool isDead;
        float hungerDrainMultiplier = 1f;
        bool isUnderwater;

        public bool IsDead => isDead;
        public PlayerStatsProfile Profile => profile;

        public void Configure(PlayerStatsProfile newProfile)
        {
            if (newProfile != null)
            {
                profile = newProfile;
            }

            ResetToFull();
        }

        public event System.Action Died;

        void Awake()
        {
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PlayerStatsProfile>();
            }

            ResetToFull();
            isDead = false;
        }

        void Update()
        {
            if (IsCreative())
            {
                ResetToFull();
                isDead = false;
                return;
            }

            if (isDead)
            {
                return;
            }

            float dt = Time.deltaTime;
            TickModifiers(dt);
            TickHunger(dt);
            TickStaminaRegen(dt);
            TickHealthRegen(dt);
            TickBreath(dt);
            TickStarvation(dt);

            if (!isDead && GetCurrent(StatId.Health) <= 0f)
            {
                isDead = true;
                Died?.Invoke();
            }
        }

        public void RespawnAfterDeath(float healthFraction)
        {
            isDead = false;
            lastDamageTime = Time.time;
            current[StatId.Health] = profile.maxHealth * Mathf.Clamp01(healthFraction);
            current[StatId.Stamina] = profile.maxStamina;
            current[StatId.Hunger] = profile.maxHunger * 0.5f;
            current[StatId.Breath] = profile.maxBreath;
        }

        public void ResetToFull()
        {
            current[StatId.Health] = profile.maxHealth;
            current[StatId.Stamina] = profile.maxStamina;
            current[StatId.Hunger] = profile.maxHunger;
            current[StatId.Breath] = profile.maxBreath;
        }

        public void RestoreFromSave(float health, float stamina, float hunger, float breath)
        {
            current[StatId.Health] = Mathf.Clamp(health, 0f, profile.maxHealth);
            current[StatId.Stamina] = Mathf.Clamp(stamina, 0f, profile.maxStamina);
            current[StatId.Hunger] = Mathf.Clamp(hunger, 0f, profile.maxHunger);
            current[StatId.Breath] = Mathf.Clamp(breath, 0f, profile.maxBreath);
        }

        public void ExportSave(out float health, out float stamina, out float hunger, out float breath)
        {
            health = GetCurrent(StatId.Health);
            stamina = GetCurrent(StatId.Stamina);
            hunger = GetCurrent(StatId.Hunger);
            breath = GetCurrent(StatId.Breath);
        }

        public float GetCurrent(StatId id) => current.TryGetValue(id, out float value) ? value : 0f;

        public float GetMax(StatId id) => id switch
        {
            StatId.Health => profile.maxHealth,
            StatId.Stamina => profile.maxStamina,
            StatId.Hunger => profile.maxHunger,
            StatId.Breath => profile.maxBreath,
            _ => 1f,
        };

        public float GetNormalized(StatId id)
        {
            float max = GetMax(id);
            return max > 0f ? GetCurrent(id) / max : 0f;
        }

        public float GetDefensePercent()
        {
            float total = 0f;
            for (int i = 0; i < modifiers.Count; i++)
            {
                total += modifiers[i].defensePercent;
            }

            return Mathf.Clamp(total, 0f, profile.maxDefensePercent);
        }

        public float GetMiningMultiplier()
        {
            float mult = profile.baseMiningMultiplier;
            for (int i = 0; i < modifiers.Count; i++)
            {
                mult *= modifiers[i].miningMultiplier;
            }

            return Mathf.Max(0.1f, mult);
        }

        public void SetHungerDrainMultiplier(float multiplier) => hungerDrainMultiplier = Mathf.Max(0f, multiplier);

        public void SetUnderwater(bool underwater) => isUnderwater = underwater;

        public bool TrySpend(StatId id, float amount)
        {
            if (IsCreative() || amount <= 0f)
            {
                return true;
            }

            float value = GetCurrent(id);
            if (value < amount)
            {
                return false;
            }

            current[id] = value - amount;
            return true;
        }

        public void Add(StatId id, float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            current[id] = Mathf.Min(GetMax(id), GetCurrent(id) + amount);
        }

        public void ApplyDamage(float rawDamage)
        {
            if (IsCreative() || rawDamage <= 0f)
            {
                return;
            }

            float final = PlayerStatsRules.ApplyDefense(rawDamage, GetDefensePercent(), profile.maxDefensePercent);
            current[StatId.Health] = Mathf.Max(0f, GetCurrent(StatId.Health) - final);
            lastDamageTime = Time.time;
        }

        public void AddModifier(StatModifier modifier)
        {
            if (modifier.miningMultiplier <= 0f)
            {
                modifier.miningMultiplier = 1f;
            }

            modifiers.Add(modifier);
        }

        public void RemoveModifiersFromSource(string sourceId)
        {
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                if (modifiers[i].source == sourceId)
                {
                    modifiers.RemoveAt(i);
                }
            }
        }

        public bool CanSprint() => IsCreative() || GetCurrent(StatId.Stamina) > 1f;

        public void SpendSprint(float deltaTime)
        {
            if (!GameInput.IsSprintHeld())
            {
                return;
            }

            TrySpend(StatId.Stamina, profile.sprintStaminaDrainPerSecond * deltaTime);
        }

        public void SpendSwim(float deltaTime, bool sprinting)
        {
            float cost = profile.swimStaminaDrainPerSecond;
            if (sprinting)
            {
                cost += profile.sprintStaminaDrainPerSecond * 0.5f;
            }

            TrySpend(StatId.Stamina, cost * deltaTime);
        }

        public bool TryJump()
        {
            return TrySpend(StatId.Stamina, profile.jumpStaminaCost);
        }

        public void ApplyWeatherHungerMultiplier(WeatherSnapshot snapshot)
        {
            float mult = 1f;
            if (snapshot.Kind == WeatherKind.Storm || snapshot.Kind == WeatherKind.Rain)
            {
                mult = 1.15f;
            }
            else if (snapshot.Kind == WeatherKind.Snow)
            {
                mult = 1.25f;
            }

            SetHungerDrainMultiplier(mult);
        }

        void TickHunger(float dt)
        {
            float hunger = GetCurrent(StatId.Hunger);
            hunger -= profile.hungerDrainPerSecond * hungerDrainMultiplier * dt;
            current[StatId.Hunger] = Mathf.Max(0f, hunger);
        }

        void TickStaminaRegen(float dt)
        {
            if (GetCurrent(StatId.Stamina) >= profile.maxStamina)
            {
                return;
            }

            float hungerNorm = GetNormalized(StatId.Hunger);
            float regenMult = hungerNorm <= 0.1f ? 0.35f : 1f;
            Add(StatId.Stamina, profile.staminaRegenPerSecond * regenMult * dt);
        }

        void TickHealthRegen(float dt)
        {
            if (GetCurrent(StatId.Health) >= profile.maxHealth)
            {
                return;
            }

            if (GetNormalized(StatId.Hunger) < profile.hungerRegenHealthThreshold)
            {
                return;
            }

            if (Time.time - lastDamageTime < profile.healthRegenCombatDelaySeconds)
            {
                return;
            }

            Add(StatId.Health, profile.healthRegenPerSecond * dt);
        }

        void TickBreath(float dt)
        {
            if (isUnderwater)
            {
                TrySpend(StatId.Breath, profile.deepBreathDrainPerSecond * dt);
                if (GetCurrent(StatId.Breath) <= 0f)
                {
                    ApplyDamage(6f * dt);
                }
            }
            else if (GetCurrent(StatId.Breath) < profile.maxBreath)
            {
                Add(StatId.Breath, profile.breathRegenPerSecond * dt);
            }
        }

        void TickStarvation(float dt)
        {
            if (GetCurrent(StatId.Hunger) > 0f)
            {
                return;
            }

            ApplyDamage(4f * dt);
        }

        void TickModifiers(float dt)
        {
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                StatModifier mod = modifiers[i];
                if (mod.duration < 0f)
                {
                    continue;
                }

                mod.duration -= dt;
                if (mod.duration <= 0f)
                {
                    modifiers.RemoveAt(i);
                }
                else
                {
                    modifiers[i] = mod;
                }
            }
        }

        static bool IsCreative() =>
            PlayerGameplayState.Instance != null && PlayerGameplayState.Instance.CreativeMode;
    }
}
