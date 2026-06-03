using Unity.Mathematics;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.World.Generation
{
    public static class StructureGenerator
    {
        public static void TryPlaceStructure(
            HexWorld world,
            in HexCoord worldHex,
            BlockColumn column,
            WorldSettings settings,
            uint seed)
        {
            if (settings.StructureDensity <= 0f)
            {
                return;
            }

            int seaLevel = settings.SeaLevelLayer;
            int surfaceHeight = column.SurfaceHeight;
            if (surfaceHeight < seaLevel)
            {
                return;
            }

            float roll = Hash01(worldHex, seed, 701);
            if (roll > settings.StructureDensity)
            {
                return;
            }

            BiomeDefinition biome = world.GetBiome(worldHex);
            BlockId trunk = biome?.SubsoilBlock != null ? biome.SubsoilBlock.BlockId : new BlockId(2);
            BlockId leaves = biome?.SurfaceBlock != null ? biome.SurfaceBlock.BlockId : new BlockId(1);

            int trunkHeight = math.clamp(settings.TreeTrunkHeight, 2, settings.MaxHeightAboveSurface - 2);
            for (int layer = surfaceHeight + 1; layer <= surfaceHeight + trunkHeight; layer++)
            {
                if (layer >= settings.ColumnCapacity)
                {
                    break;
                }

                column.SetBlock(layer, trunk);
            }

            int crownBase = surfaceHeight + trunkHeight;
            for (int layer = crownBase; layer <= crownBase + 2; layer++)
            {
                if (layer >= settings.ColumnCapacity)
                {
                    break;
                }

                column.SetBlock(layer, leaves);
            }
        }

        static float Hash01(in HexCoord hex, uint seed, int salt)
        {
            uint hash = math.hash(new int3(hex.Q, hex.R, (int)seed + salt));
            return (hash & 0xFFFF) / 65535f;
        }
    }
}
