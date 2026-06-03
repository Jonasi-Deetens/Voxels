using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    public enum WaterDepthTier
    {
        None = 0,
        Shallow = 1,
        Deep = 2,
    }

    public static class FluidHelper
    {
        public static bool IsFluidBlock(HexWorld world, BlockId blockId)
        {
            return world != null &&
                world.BlockRegistry.TryGetDefinition(blockId, out BlockDefinition definition) &&
                definition.IsFluid;
        }

        public static WaterDepthTier GetPlayerWaterDepth(
            HexWorld world,
            WorldSettings settings,
            WorldScroller scroller,
            Transform player)
        {
            if (world == null || settings == null || scroller == null || player == null)
            {
                return WaterDepthTier.None;
            }

            HexCoord hex = scroller.PlayerWorldHex;
            if (!world.TryGetColumn(hex, out BlockColumn column))
            {
                return WaterDepthTier.None;
            }

            int layer = Mathf.Clamp(
                Mathf.FloorToInt(player.position.y / settings.BlockSize),
                0,
                settings.ColumnCapacity - 1);

            if (!IsFluidBlock(world, column.GetBlock(layer)))
            {
                return WaterDepthTier.None;
            }

            int fluidLayers = 0;
            for (int l = layer; l >= 0; l--)
            {
                if (!IsFluidBlock(world, column.GetBlock(l)))
                {
                    break;
                }

                fluidLayers++;
            }

            return fluidLayers >= 2 ? WaterDepthTier.Deep : WaterDepthTier.Shallow;
        }

        public static bool IsPlayerInWater(
            HexWorld world,
            WorldSettings settings,
            WorldScroller scroller,
            Transform player) =>
            GetPlayerWaterDepth(world, settings, scroller, player) != WaterDepthTier.None;

        public static bool IsWaterAtLayer(HexWorld world, in HexCoord hex, int layer)
        {
            if (!world.TryGetColumn(hex, out BlockColumn column))
            {
                return false;
            }

            return IsFluidBlock(world, column.GetBlock(layer));
        }
    }
}
