using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    public static class FluidHelper
    {
        public static bool IsFluidBlock(HexWorld world, BlockId blockId)
        {
            return world != null &&
                world.BlockRegistry.TryGetDefinition(blockId, out BlockDefinition definition) &&
                definition.IsFluid;
        }

        public static bool IsPlayerInWater(
            HexWorld world,
            WorldSettings settings,
            WorldScroller scroller,
            Transform player)
        {
            if (world == null || settings == null || scroller == null || player == null)
            {
                return false;
            }

            HexCoord hex = scroller.PlayerWorldHex;
            if (!world.TryGetColumn(hex, out BlockColumn column))
            {
                return false;
            }

            int layer = Mathf.Clamp(
                Mathf.FloorToInt(player.position.y / settings.BlockSize),
                0,
                settings.ColumnCapacity - 1);

            return IsFluidBlock(world, column.GetBlock(layer));
        }

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
