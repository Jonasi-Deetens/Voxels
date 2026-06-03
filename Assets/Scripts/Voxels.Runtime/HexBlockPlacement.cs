using Unity.Mathematics;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    public static class HexBlockPlacement
    {
        public static bool CanPlace(
            HexWorld world,
            WorldSettings settings,
            Transform player,
            in HexBlockTarget target,
            bool creative)
        {
            if (!target.IsValid || !world.IsInsideWorld(target.WorldHex))
            {
                return false;
            }

            BlockColumn column = world.GetOrCreateColumn(target.WorldHex);
            BlockId existing = column.GetBlock(target.Layer);
            if (!existing.IsAir)
            {
                return false;
            }

            if (FluidHelper.IsFluidBlock(world, existing))
            {
                return false;
            }

            if (target.Layer > column.SurfaceHeight + settings.MaxHeightAboveSurface)
            {
                return false;
            }

            if (!creative && IsFluidNeighbor(world, target))
            {
                return false;
            }

            if (!HasSupport(world, target))
            {
                return false;
            }

            if (OverlapsPlayer(player, settings, target))
            {
                return false;
            }

            return true;
        }

        static bool IsFluidNeighbor(HexWorld world, in HexBlockTarget target)
        {
            if (target.Layer > 0 && FluidHelper.IsFluidBlock(world, world.GetOrCreateColumn(target.WorldHex).GetBlock(target.Layer - 1)))
            {
                return true;
            }

            for (int i = 0; i < HexCoord.NeighborOffsets.Length; i++)
            {
                HexCoord neighborHex = target.WorldHex.Add(HexCoord.NeighborOffsets[i]);
                if (!world.TryGetColumn(neighborHex, out BlockColumn neighborColumn))
                {
                    continue;
                }

                if (FluidHelper.IsFluidBlock(world, neighborColumn.GetBlock(target.Layer)))
                {
                    return true;
                }
            }

            return false;
        }

        static bool HasSupport(HexWorld world, in HexBlockTarget target)
        {
            if (target.Layer > 0)
            {
                BlockId below = world.GetOrCreateColumn(target.WorldHex).GetBlock(target.Layer - 1);
                if (!below.IsAir && IsSolid(world, below))
                {
                    return true;
                }
            }

            for (int i = 0; i < HexCoord.NeighborOffsets.Length; i++)
            {
                HexCoord neighborHex = target.WorldHex.Add(HexCoord.NeighborOffsets[i]);
                if (!world.TryGetColumn(neighborHex, out BlockColumn neighborColumn))
                {
                    continue;
                }

                BlockId neighborBlock = neighborColumn.GetBlock(target.Layer);
                if (!neighborBlock.IsAir && IsSolid(world, neighborBlock))
                {
                    return true;
                }
            }

            return false;
        }

        static bool OverlapsPlayer(Transform player, WorldSettings settings, in HexBlockTarget target)
        {
            if (player == null)
            {
                return false;
            }

            float blockSize = settings.BlockSize;
            float3 blockCenter = FlatHexGrid.AxialToWorld(target.WorldHex, blockSize);
            blockCenter.y = (target.Layer + 0.5f) * blockSize;

            Vector3 playerPosition = player.position;
            float halfHeight = settings.PlayerHeight * 0.5f;
            float halfExtent = blockSize * 0.55f;

            return math.abs(playerPosition.x - blockCenter.x) < halfExtent &&
                math.abs(playerPosition.z - blockCenter.z) < halfExtent &&
                playerPosition.y + halfHeight > blockCenter.y - blockSize * 0.5f &&
                playerPosition.y - halfHeight < blockCenter.y + blockSize * 0.5f;
        }

        static bool IsSolid(HexWorld world, BlockId blockId)
        {
            return world.BlockRegistry.TryGetDefinition(blockId, out BlockDefinition definition) &&
                definition.IsSolid;
        }
    }
}
