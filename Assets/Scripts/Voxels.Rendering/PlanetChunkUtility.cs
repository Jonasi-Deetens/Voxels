using System.Collections.Generic;

namespace Voxels.Rendering
{
    public static class PlanetChunkUtility
    {
        public static List<int>[] BuildChunkCellGroups(int cellCount, int cellsPerChunk)
        {
            int chunkCount = (cellCount + cellsPerChunk - 1) / cellsPerChunk;
            var groups = new List<int>[chunkCount];

            for (int i = 0; i < chunkCount; i++)
            {
                groups[i] = new List<int>(cellsPerChunk);
            }

            for (int cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                int chunkIndex = cellIndex / cellsPerChunk;
                groups[chunkIndex].Add(cellIndex);
            }

            return groups;
        }
    }
}
