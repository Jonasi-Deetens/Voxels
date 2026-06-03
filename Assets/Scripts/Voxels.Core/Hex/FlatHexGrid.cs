using Unity.Mathematics;

namespace Voxels.Core.Hex
{
    /// <summary>
    /// Pointy-top axial hex layout on the XZ plane with Y up.
    /// </summary>
    public static class FlatHexGrid
    {
        public static float3 AxialToWorld(in HexCoord hex, float blockSize)
        {
            float x = blockSize * (HexMetrics.Sqrt3 * hex.Q + HexMetrics.Sqrt3 * 0.5f * hex.R);
            float z = blockSize * (1.5f * hex.R);
            return new float3(x, 0f, z);
        }

        public static HexCoord WorldToAxial(float3 worldPosition, float blockSize)
        {
            if (blockSize <= 0.0001f)
            {
                return HexCoord.Zero;
            }

            float qf = (HexMetrics.Sqrt3 / 3f * worldPosition.x - 1f / 3f * worldPosition.z) / blockSize;
            float rf = (2f / 3f * worldPosition.z) / blockSize;
            return CubeRound(qf, -qf - rf, rf);
        }

        public static HexCoord WorldToAxial(float x, float z, float blockSize)
        {
            return WorldToAxial(new float3(x, 0f, z), blockSize);
        }

        public static float Circumradius(float blockSize) => blockSize;

        public static float Apothem(float blockSize) => blockSize * HexMetrics.Sqrt3 * 0.5f;

        public static void GetCornerOffsets(float blockSize, float3[] corners)
        {
            float radius = Circumradius(blockSize);
            for (int i = 0; i < 6; i++)
            {
                float angle = math.radians(60f * i - 30f);
                corners[i] = new float3(math.cos(angle) * radius, 0f, math.sin(angle) * radius);
            }
        }

        static HexCoord CubeRound(float qf, float rf, float sf)
        {
            int q = (int)math.round(qf);
            int r = (int)math.round(rf);
            int s = (int)math.round(sf);

            float qDiff = math.abs(q - qf);
            float rDiff = math.abs(r - rf);
            float sDiff = math.abs(s - sf);

            if (qDiff > rDiff && qDiff > sDiff)
            {
                q = -r - s;
            }
            else if (rDiff > sDiff)
            {
                r = -q - s;
            }

            return new HexCoord(q, r);
        }
    }
}
