using System;
using Voxels.Core.Blocks;

namespace Voxels.World
{
    public sealed class BlockColumn
    {
        BlockId[] layers;
        int surfaceHeight;

        public int SurfaceHeight => surfaceHeight;

        public BlockColumn(int capacity)
        {
            layers = new BlockId[Math.Max(capacity, 1)];
        }

        public void SetSurfaceHeight(int height)
        {
            surfaceHeight = height;
            EnsureCapacity(height + 1);
        }

        public void SetBlock(int layer, BlockId blockId)
        {
            EnsureCapacity(layer + 1);
            layers[layer] = blockId;
        }

        public BlockId GetBlock(int layer)
        {
            if (layer < 0 || layer >= layers.Length)
            {
                return BlockId.Air;
            }

            return layers[layer];
        }

        public bool IsAir(int layer) => GetBlock(layer).IsAir;

        void EnsureCapacity(int required)
        {
            if (layers.Length < required)
            {
                Array.Resize(ref layers, Math.Max(required, layers.Length * 2));
            }
        }
    }
}
