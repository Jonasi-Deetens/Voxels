using Voxels.World;

namespace Voxels.Runtime
{
    public static class BlockBreakCalculator
    {
        public static float GetBreakDuration(
            BlockDefinition block,
            PlayerToolMode tool,
            bool creative,
            float miningMultiplier = 1f)
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
            float multiplier = 1f;

            switch (block.MaterialCategory)
            {
                case BlockMaterialCategory.Stone:
                    multiplier = tool switch
                    {
                        PlayerToolMode.IronPickaxe => 0.22f,
                        PlayerToolMode.StonePickaxe => 0.32f,
                        PlayerToolMode.WoodenPickaxe => 0.5f,
                        _ => 1f,
                    };
                    break;
                case BlockMaterialCategory.Soil:
                    multiplier = tool == PlayerToolMode.Shovel ? 0.32f : 1f;
                    break;
                case BlockMaterialCategory.Soft:
                    multiplier = tool switch
                    {
                        PlayerToolMode.Shovel => 0.4f,
                        PlayerToolMode.Hand => 0.55f,
                        _ => 0.85f,
                    };
                    break;
                case BlockMaterialCategory.Wood:
                    multiplier = tool switch
                    {
                        PlayerToolMode.IronPickaxe => 0.4f,
                        PlayerToolMode.StonePickaxe => 0.55f,
                        PlayerToolMode.WoodenPickaxe => 0.65f,
                        _ => 1f,
                    };
                    break;
            }

            return PlayerStatsRules.ApplyMiningSpeed(baseTime * multiplier, miningMultiplier);
        }
    }
}
