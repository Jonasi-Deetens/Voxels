using Unity.Mathematics;

namespace Voxels.Core.Hex
{
    public static class FlatHexGeometry
    {
        static readonly float3[] CornerScratch = new float3[6];
        static readonly HexCoord[] NeighborScratch = new HexCoord[6];
        static readonly float[] AngleScratch = new float[6];
        static readonly int[] IndexScratch = new int[6];

        public static int GetSortedNeighborIndices(in HexCoord hex, int[] sortedNeighborIndices)
        {
            for (int i = 0; i < 6; i++)
            {
                NeighborScratch[i] = hex.Add(HexCoord.NeighborOffsets[i]);
                sortedNeighborIndices[i] = i;
            }

            SortNeighborsByAngle(NeighborScratch, sortedNeighborIndices);
            return 6;
        }

        public static void GetBlockCorners(
            float3 center,
            float blockSize,
            float3[] corners)
        {
            FlatHexGrid.GetCornerOffsets(blockSize, CornerScratch);
            for (int i = 0; i < 6; i++)
            {
                corners[i] = center + CornerScratch[i];
            }
        }

        public static float3 GetNeighborCenter(in HexCoord hex, int neighborDirection, float blockSize)
        {
            HexCoord neighbor = hex.Add(HexCoord.NeighborOffsets[neighborDirection]);
            return FlatHexGrid.AxialToWorld(neighbor, blockSize);
        }

        static void SortNeighborsByAngle(HexCoord[] neighbors, int[] indices)
        {
            float3 reference = FlatHexGrid.AxialToWorld(neighbors[0], 1f) - float3.zero;
            reference.y = 0f;
            reference = math.normalizesafe(reference, new float3(1f, 0f, 0f));

            for (int i = 0; i < 6; i++)
            {
                IndexScratch[i] = indices[i];
                float3 dir = FlatHexGrid.AxialToWorld(neighbors[i], 1f);
                dir.y = 0f;
                dir = math.normalizesafe(dir, reference);
                AngleScratch[i] = math.atan2(
                    math.cross(reference, dir).y,
                    math.dot(reference, dir));
            }

            for (int i = 1; i < 6; i++)
            {
                int index = IndexScratch[i];
                float angle = AngleScratch[i];
                int j = i - 1;
                while (j >= 0 && AngleScratch[j] > angle)
                {
                    IndexScratch[j + 1] = IndexScratch[j];
                    AngleScratch[j + 1] = AngleScratch[j];
                    j--;
                }

                IndexScratch[j + 1] = index;
                AngleScratch[j + 1] = angle;
            }

            for (int i = 0; i < 6; i++)
            {
                indices[i] = IndexScratch[i];
            }
        }
    }
}
