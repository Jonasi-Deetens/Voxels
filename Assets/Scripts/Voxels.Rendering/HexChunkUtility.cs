using System.Collections.Generic;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Rendering
{
    public static class HexChunkUtility
    {
        public static List<HexCoord> CollectChunkHexes(HexWorld world, in ChunkCoord chunkCoord, int chunkSizeHex)
        {
            var hexes = new List<HexCoord>(chunkSizeHex * chunkSizeHex);
            HexCoord origin = chunkCoord.ToHexOrigin(chunkSizeHex);

            for (int dq = 0; dq < chunkSizeHex; dq++)
            {
                for (int dr = 0; dr < chunkSizeHex; dr++)
                {
                    HexCoord worldHex = origin.Add(new HexCoord(dq, dr));
                    if (world.IsInsideWorld(worldHex))
                    {
                        hexes.Add(worldHex);
                    }
                }
            }

            return hexes;
        }

        public static IEnumerable<ChunkCoord> EnumerateChunksAround(in HexCoord worldHex, int viewRadiusChunks, int chunkSizeHex)
        {
            ChunkCoord center = ChunkCoord.FromHex(worldHex, chunkSizeHex);
            for (int dq = -viewRadiusChunks; dq <= viewRadiusChunks; dq++)
            {
                for (int dr = -viewRadiusChunks; dr <= viewRadiusChunks; dr++)
                {
                    yield return new ChunkCoord(center.Q + dq, center.R + dr);
                }
            }
        }
    }
}
