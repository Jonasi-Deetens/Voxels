using Voxels.Core.Blocks;
using Voxels.World;
using Voxels.World.Modding;

namespace Voxels.Runtime
{
    public static class VoxelsModConfig
    {
        public static bool TryApplyHotbar(BlockHotbar hotbar, BlockRegistry registry)
        {
            VoxelsModConfigData config = Voxels.World.Modding.VoxelsModConfig.Load();
            if (config.hotbarBlockNames == null || config.hotbarBlockNames.Length == 0 || registry == null)
            {
                return false;
            }

            var slots = new BlockId[9];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = BlockId.Air;
                if (i < config.hotbarBlockNames.Length &&
                    registry.TryGetByName(config.hotbarBlockNames[i], out BlockDefinition definition))
                {
                    slots[i] = definition.BlockId;
                }
            }

            hotbar.SetSlots(slots);
            return true;
        }
    }
}
