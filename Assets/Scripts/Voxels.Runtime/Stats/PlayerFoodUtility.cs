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
            PlayerStatsController stats)
        {
            if (hotbar == null || inventory == null || registry == null || stats == null)
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

            PlayerStatsProfile profile = stats.Profile;
            float restore = definition.DisplayName switch
            {
                "Fungus" => profile.fungusHungerRestore,
                "Yellow Grass" => profile.yellowGrassHungerRestore,
                "Grass" => profile.yellowGrassHungerRestore * 0.75f,
                _ => 0f,
            };

            if (restore <= 0f)
            {
                return false;
            }

            if (!inventory.TryConsume(blockId))
            {
                return false;
            }

            stats.Add(StatId.Hunger, restore);
            stats.Add(StatId.Health, restore * 0.15f);
            return true;
        }
    }
}
