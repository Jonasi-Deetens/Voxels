using Unity.Mathematics;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;

namespace Voxels.World.Generation
{
    public static class BlockLightSampler
    {
        static readonly HexCoord[] SampleOffsets =
        {
            HexCoord.Zero,
            new HexCoord(1, 0),
            new HexCoord(1, -1),
            new HexCoord(0, -1),
            new HexCoord(-1, 0),
            new HexCoord(-1, 1),
            new HexCoord(0, 1),
        };

        public static int SamplePlayerLightLevel(HexWorld world, in HexCoord playerHex, int sunLightLevel)
        {
            if (world == null || sunLightLevel >= 12)
            {
                return sunLightLevel;
            }

            int blockLight = 0;
            BlockRegistry registry = world.BlockRegistry;
            for (int i = 0; i < SampleOffsets.Length; i++)
            {
                HexCoord hex = playerHex.Add(SampleOffsets[i]);
                if (!world.TryGetColumn(hex, out BlockColumn column))
                {
                    continue;
                }

                int surface = column.SurfaceHeight;
                for (int layer = math.max(0, surface - 1); layer <= surface + 4; layer++)
                {
                    BlockId id = column.GetBlock(layer);
                    if (id.IsAir || !registry.TryGetDefinition(id, out BlockDefinition definition))
                    {
                        continue;
                    }

                    if (definition.IsEmissive)
                    {
                        blockLight = math.max(blockLight, definition.LightEmission);
                    }
                }
            }

            return math.max(sunLightLevel, blockLight);
        }
    }
}
