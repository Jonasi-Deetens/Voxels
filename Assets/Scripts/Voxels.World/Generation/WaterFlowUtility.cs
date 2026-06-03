using Voxels.Core.Blocks;
using Voxels.Core.Hex;

namespace Voxels.World.Generation
{
    /// <summary>
    /// Local water settle when blocks change near fluid.
    /// </summary>
    public static class WaterFlowUtility
    {
        public static void SettleAround(HexWorld world, in HexCoord originHex, WorldSettings settings, int radius = 1)
        {
            BlockId water = ResolveWater(world);
            int seaLevel = settings.SeaLevelLayer;

            for (int dq = -radius; dq <= radius; dq++)
            {
                for (int dr = -radius; dr <= radius; dr++)
                {
                    HexCoord hex = originHex.Add(new HexCoord(dq, dr));
                    if (!world.IsInsideWorld(hex) || world.IsColumnDirty(hex))
                    {
                        continue;
                    }

                    if (!world.TryGetColumn(hex, out BlockColumn column))
                    {
                        continue;
                    }

                    TryFillDownward(world, hex, column, seaLevel, water, settings);
                }
            }
        }

        static void TryFillDownward(
            HexWorld world,
            in HexCoord hex,
            BlockColumn column,
            int seaLevel,
            BlockId water,
            WorldSettings settings)
        {
            for (int layer = settings.ColumnCapacity - 2; layer >= 0; layer--)
            {
                BlockId current = column.GetBlock(layer);
                BlockId above = column.GetBlock(layer + 1);
                if (current.IsAir && !above.IsAir && world.BlockRegistry.TryGetDefinition(above, out BlockDefinition def) && def.IsFluid)
                {
                    column.SetBlock(layer, water);
                }
            }

            int surface = column.SurfaceHeight;
            if (surface < seaLevel)
            {
                for (int layer = surface + 1; layer <= seaLevel; layer++)
                {
                    if (column.GetBlock(layer).IsAir)
                    {
                        column.SetBlock(layer, water);
                    }
                }
            }
        }

        static BlockId ResolveWater(HexWorld world)
        {
            if (world.Settings.Biome?.WaterBlock != null)
            {
                return world.Settings.Biome.WaterBlock.BlockId;
            }

            return world.BlockRegistry.TryGetByName("Water", out BlockDefinition water) ? water.BlockId : new BlockId(6);
        }
    }
}
