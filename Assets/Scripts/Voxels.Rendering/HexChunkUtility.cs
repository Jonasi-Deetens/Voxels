using System.Collections.Generic;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Rendering
{
    public static class HexChunkUtility
    {
        public static List<HexCoord> CollectChunkHexes(HexWorld world, in ChunkCoord chunkCoord, int chunkSizeHex)
        {
            return CollectChunkHexesInternal(world, chunkCoord, chunkSizeHex, padding: 0);
        }

        /// <summary>
        /// Chunk hexes plus a padding ring for neighbor face culling at seams.
        /// </summary>
        public static List<HexCoord> CollectChunkMeshHexes(
            HexWorld world,
            in ChunkCoord chunkCoord,
            int chunkSizeHex,
            int padding)
        {
            return CollectChunkHexesInternal(world, chunkCoord, chunkSizeHex, padding);
        }

        static List<HexCoord> CollectChunkHexesInternal(
            HexWorld world,
            in ChunkCoord chunkCoord,
            int chunkSizeHex,
            int padding)
        {
            var hexes = new List<HexCoord>((chunkSizeHex + padding * 2) * (chunkSizeHex + padding * 2));
            HexCoord origin = chunkCoord.ToHexOrigin(chunkSizeHex);

            for (int dq = -padding; dq < chunkSizeHex + padding; dq++)
            {
                for (int dr = -padding; dr < chunkSizeHex + padding; dr++)
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

        public static void CollectProtectedHexes(
            HexWorld world,
            in HexCoord playerHex,
            int viewRadiusChunks,
            int chunkSizeHex,
            int padding,
            HashSet<HexCoord> output)
        {
            output.Clear();
            foreach (ChunkCoord chunk in EnumerateChunksAround(playerHex, viewRadiusChunks, chunkSizeHex))
            {
                List<HexCoord> hexes = CollectChunkHexesInternal(world, chunk, chunkSizeHex, padding);
                for (int i = 0; i < hexes.Count; i++)
                {
                    output.Add(hexes[i]);
                }
            }
        }
    }
}
