using Unity.Mathematics;
using Voxels.Core.Hex;
using Voxels.World.Modding;

namespace Voxels.World.Generation
{
    public static class StructurePlacementPlanner
    {
        public static bool CanPlaceStructure(
            HexWorld world,
            in HexCoord worldHex,
            BiomeDefinition biome,
            uint seed,
            WorldSettings settings)
        {
            int spacing = biome != null ? biome.StructureSpacing : 5;
            spacing = math.max(3, spacing);

            for (int dq = -spacing; dq <= spacing; dq++)
            {
                for (int dr = -spacing; dr <= spacing; dr++)
                {
                    if (dq == 0 && dr == 0)
                    {
                        continue;
                    }

                    HexCoord neighbor = worldHex.Add(new HexCoord(dq, dr));
                    if (HasStructureMarker(world, neighbor))
                    {
                        return false;
                    }
                }
            }

            float roll = Hash01(worldHex, seed, 701);
            return roll <= VoxelsModConfig.ResolveStructureDensity(settings);
        }

        public static bool HasStructureMarker(HexWorld world, in HexCoord hex)
        {
            if (!world.TryGetColumn(hex, out BlockColumn column))
            {
                return false;
            }

            int surface = column.SurfaceHeight;
            for (int layer = surface + 1; layer <= surface + 4; layer++)
            {
                if (layer >= world.Settings.ColumnCapacity)
                {
                    break;
                }

                if (!column.GetBlock(layer).IsAir)
                {
                    return true;
                }
            }

            return false;
        }

        static float Hash01(in HexCoord hex, uint seed, int salt)
        {
            uint hash = math.hash(new int3(hex.Q, hex.R, (int)seed + salt));
            return (hash & 0xFFFF) / 65535f;
        }
    }
}
