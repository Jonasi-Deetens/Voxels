using Voxels.Core.Blocks;
using Voxels.World;

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

            if (!registry.TryGetDefinition(blockId, out BlockDefinition definition) || !definition.IsEdible)
            {
                feedback = "Can't eat that";
                return false;
            }

            if (!inventory.TryConsume(blockId))
            {
                return false;
            }

            stats.Add(StatId.Hunger, definition.HungerRestore);
            stats.Add(StatId.Health, definition.HealthRestoreOnEat);
            feedback = $"Ate {definition.DisplayName} (+{definition.HungerRestore:0} food)";
            return true;
        }
    }
}
