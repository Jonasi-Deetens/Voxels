using Voxels.Core.Blocks;
using Voxels.World;

namespace Voxels.Runtime
{
    public static class BlockHotbarPalette
    {
        static readonly string[] SlotNames =
        {
            "Grass",
            "Dirt",
            "Stone",
            "Sand",
            "Gravel",
            "Snow",
            "Dark Grass",
            "Fungus",
            "Crystal",
        };

        public static void ApplyToHotbar(BlockHotbar hotbar, BlockRegistry registry)
        {
            if (hotbar == null)
            {
                return;
            }

            var slots = new BlockId[9];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = BlockId.Air;
                if (registry != null && registry.TryGetByName(SlotNames[i], out BlockDefinition definition))
                {
                    slots[i] = definition.BlockId;
                }
            }

            if (slots[0].IsAir)
            {
                slots[0] = new BlockId(1);
            }

            hotbar.SetSlots(slots);
        }
    }
}
