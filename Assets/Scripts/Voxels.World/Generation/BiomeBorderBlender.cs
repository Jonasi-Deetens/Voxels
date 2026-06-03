using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.World.Generation
{
    public static class BiomeBorderBlender
    {
        public static BlockId ResolveSurfaceBlock(HexWorld world, in HexCoord worldHex, BiomeDefinition primary)
        {
            if (primary == null)
            {
                return new BlockId(1);
            }

            BlockId surface = primary.SurfaceBlock != null ? primary.SurfaceBlock.BlockId : new BlockId(1);
            int differentNeighbors = 0;

            for (int i = 0; i < HexCoord.NeighborOffsets.Length; i++)
            {
                HexCoord neighbor = worldHex.Add(HexCoord.NeighborOffsets[i]);
                BiomeDefinition neighborBiome = world.GetBiome(neighbor);
                if (neighborBiome != null && neighborBiome != primary)
                {
                    differentNeighbors++;
                }
            }

            if (differentNeighbors < 2)
            {
                return surface;
            }

            if (primary.UnderwaterSurfaceBlock != null)
            {
                return primary.UnderwaterSurfaceBlock.BlockId;
            }

            return surface;
        }
    }
}
