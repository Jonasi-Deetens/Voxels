using Voxels.World;

namespace Voxels.Runtime
{
    public enum PlayerToolMode
    {
        Hand = 0,
        Pickaxe = 1,
        Shovel = 2,
    }

    public static class BlockBreakCalculator
    {
        public static float GetBreakDuration(BlockDefinition block, PlayerToolMode tool, bool creative)
        {
            if (creative || block == null)
            {
                return 0f;
            }

            if (block.IsFluid)
            {
                return 0.2f;
            }

            float baseTime = block.BreakTime;
            float multiplier = tool switch
            {
                PlayerToolMode.Pickaxe when block.MaterialCategory == BlockMaterialCategory.Stone => 0.35f,
                PlayerToolMode.Pickaxe when block.MaterialCategory == BlockMaterialCategory.Wood => 0.55f,
                PlayerToolMode.Shovel when block.MaterialCategory == BlockMaterialCategory.Soil => 0.35f,
                PlayerToolMode.Shovel when block.MaterialCategory == BlockMaterialCategory.Soft => 0.45f,
                PlayerToolMode.Hand when block.MaterialCategory == BlockMaterialCategory.Soft => 0.55f,
                _ => 1f,
            };

            return baseTime * multiplier;
        }
    }
}
