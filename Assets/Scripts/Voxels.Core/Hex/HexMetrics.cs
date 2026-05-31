using Unity.Mathematics;

namespace Voxels.Core.Hex
{
    /// <summary>
    /// Hex block footprint: flat-to-flat width equals neighbor center distance for seamless tiling.
    /// Radial height is the same value (1 block = 1 unit cube equivalent, hexagonal top face).
    /// </summary>
    public static class HexMetrics
    {
        public const float Sqrt3 = 1.7320508075688772f;

        /// <summary>
        /// Flat-to-flat width and block height both equal neighbor center distance.
        /// </summary>
        public static float ApothemFromNeighborDistance(float neighborCenterDistance)
        {
            return neighborCenterDistance * 0.5f;
        }

        public static float FlatToFlat(float apothem) => apothem * 2f;

        public static float BlockHeight(float apothem) => FlatToFlat(apothem);

        public static float Circumradius(float apothem) => apothem / math.cos(math.radians(30f));
    }
}
