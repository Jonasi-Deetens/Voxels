using Unity.Mathematics;

namespace Voxels.Core.Hex
{
    public static class HexagonMask
    {
        public static bool IsInsideWorld(in HexCoord hex, int worldHexRadius)
        {
            int q = hex.Q;
            int r = hex.R;
            int s = hex.S;
            return math.abs(q) <= worldHexRadius
                && math.abs(r) <= worldHexRadius
                && math.abs(s) <= worldHexRadius;
        }
    }
}
