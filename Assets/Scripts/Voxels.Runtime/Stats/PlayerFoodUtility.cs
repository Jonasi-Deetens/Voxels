using Voxels.Core.Blocks;
using Voxels.World;
using Voxels.World.Modding;

namespace Voxels.Runtime
{
    public static class PlayerFoodUtility
    {
        public static bool TryEatFromHotbar(
            BlockHotbar hotbar,
            PlayerInventory inventory,
            BlockRegistry registry,
            PlayerStatsController stats,
            out string feedback)
        {
            feedback = string.Empty;
            if (hotbar == null || inventory == null || registry == null || stats == null)
            {
                return false;
            }

            if (stats.IsDead)
            {
                return false;
            }

            BlockId blockId = hotbar.SelectedBlock;
            if (blockId.IsAir)
            {
                return false;
            }

            if (!registry.TryGetDefinition(blockId, out BlockDefinition definition))
            {
                return false;
            }

            float hunger = definition.HungerRestore;
            float health = definition.HealthRestoreOnEat;
            if (VoxelsModConfig.TryGetFoodOverride(definition.DisplayName, out float modHunger, out float modHealth))
            {
                if (modHunger > 0f)
                {
                    hunger = modHunger;
                }

                if (modHealth > 0f)
                {
                    health = modHealth;
                }
            }

            if (hunger <= 0f && health <= 0f)
            {
                feedback = "Can't eat that";
                return false;
            }

            if (!inventory.TryConsume(blockId))
            {
                return false;
            }

            stats.Add(StatId.Hunger, hunger);
            stats.Add(StatId.Health, health);
            feedback = $"Ate {definition.DisplayName} (+{hunger:0} food)";
            return true;
        }
    }
}
