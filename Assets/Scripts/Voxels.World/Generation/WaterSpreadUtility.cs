using Voxels.Core.Blocks;
using Voxels.Core.Hex;

namespace Voxels.World.Generation
{
    /// <summary>
    /// Post-processes columns to extend shallow water one step along shorelines and fill surface depressions.
    /// </summary>
    public static class WaterSpreadUtility
    {
        public static void ApplyToCells(HexWorld world, System.Collections.Generic.IReadOnlyList<HexCoord> cells, WorldSettings settings)
        {
            int seaLevel = settings.SeaLevelLayer;
            BlockId water = ResolveWaterBlock(world);

            for (int i = 0; i < cells.Count; i++)
            {
                HexCoord hex = cells[i];
                if (world.IsColumnDirty(hex) || !world.TryGetColumn(hex, out BlockColumn column))
                {
                    continue;
                }

                int surface = column.SurfaceHeight;
                if (surface < seaLevel)
                {
                    continue;
                }

                TryFillDepression(world, hex, column, seaLevel, water, settings);
            }

            for (int i = 0; i < cells.Count; i++)
            {
                HexCoord hex = cells[i];
                if (world.IsColumnDirty(hex))
                {
                    continue;
                }

                SpreadShoreline(world, hex, seaLevel, water, settings);
            }
        }

        static void TryFillDepression(
            HexWorld world,
            in HexCoord hex,
            BlockColumn column,
            int seaLevel,
            BlockId water,
            WorldSettings settings)
        {
            int lowestNeighborSurface = column.SurfaceHeight;
            for (int n = 0; n < HexCoord.NeighborOffsets.Length; n++)
            {
                HexCoord neighbor = hex.Add(HexCoord.NeighborOffsets[n]);
                if (!world.TryGetColumn(neighbor, out BlockColumn neighborColumn))
                {
                    continue;
                }

                lowestNeighborSurface = System.Math.Min(lowestNeighborSurface, neighborColumn.SurfaceHeight);
            }

            if (lowestNeighborSurface >= seaLevel - 1 || column.SurfaceHeight > seaLevel)
            {
                return;
            }

            for (int layer = column.SurfaceHeight + 1; layer <= seaLevel; layer++)
            {
                if (layer >= settings.ColumnCapacity)
                {
                    break;
                }

                if (column.GetBlock(layer).IsAir)
                {
                    column.SetBlock(layer, water);
                }
            }
        }

        static void SpreadShoreline(HexWorld world, in HexCoord hex, int seaLevel, BlockId water, WorldSettings settings)
        {
            if (!world.TryGetColumn(hex, out BlockColumn column))
            {
                return;
            }

            if (column.SurfaceHeight >= seaLevel)
            {
                return;
            }

            for (int n = 0; n < HexCoord.NeighborOffsets.Length; n++)
            {
                HexCoord neighbor = hex.Add(HexCoord.NeighborOffsets[n]);
                if (!world.TryGetColumn(neighbor, out BlockColumn neighborColumn))
                {
                    continue;
                }

                if (neighborColumn.SurfaceHeight >= seaLevel)
                {
                    continue;
                }

                int layer = seaLevel;
                if (layer >= settings.ColumnCapacity || !neighborColumn.GetBlock(layer).IsAir)
                {
                    continue;
                }

                neighborColumn.SetBlock(layer, water);
            }
        }

        static BlockId ResolveWaterBlock(HexWorld world)
        {
            if (world.Settings.Biome?.WaterBlock != null)
            {
                return world.Settings.Biome.WaterBlock.BlockId;
            }

            if (world.BlockRegistry.TryGetByName("Water", out BlockDefinition water))
            {
                return water.BlockId;
            }

            return new BlockId(6);
        }
    }
}
