using Unity.Mathematics;

namespace Voxels.Core.Hex
{
    public static class HexagonMask
    {
        public static bool IsInsideWorld(in HexCoord hex, int worldHexRadius)
        {
            return DistanceFromCenter(hex) <= worldHexRadius;
        }

        public static int DistanceFromCenter(in HexCoord hex)
        {
            int q = hex.Q;
            int r = hex.R;
            int s = hex.S;
            return math.max(math.abs(q), math.max(math.abs(r), math.abs(s)));
        }

        public static HexCoord ClampToWorld(in HexCoord hex, int worldHexRadius)
        {
            if (IsInsideWorld(hex, worldHexRadius))
            {
                return hex;
            }

            int q = hex.Q;
            int r = hex.R;
            int s = hex.S;

            int dq = math.abs(q) - worldHexRadius;
            int dr = math.abs(r) - worldHexRadius;
            int ds = math.abs(s) - worldHexRadius;

            if (dq > dr && dq > ds)
            {
                q -= (int)math.sign(q) * dq;
            }
            else if (dr > ds)
            {
                r -= (int)math.sign(r) * dr;
            }
            else
            {
                s -= (int)math.sign(s) * ds;
            }

            return new HexCoord(q, r);
        }
    }
}
