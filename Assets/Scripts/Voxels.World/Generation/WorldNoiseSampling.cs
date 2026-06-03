using Unity.Mathematics;
using Voxels.Core.Hex;

namespace Voxels.World.Generation
{
    /// <summary>
    /// Stable noise sample coordinates for large world-hex positions (infinite worlds).
    /// </summary>
    public static class WorldNoiseSampling
    {
        const int WrapCells = 512;

        public static float2 SampleXZ(in HexCoord worldHex, float blockSize, in HexCoord originHex)
        {
            HexCoord relative = worldHex.Subtract(originHex);
            relative = WrapHex(relative);
            return FlatHexGrid.AxialToWorld(relative, blockSize).xz;
        }

        public static HexCoord WrapHex(in HexCoord hex)
        {
            return new HexCoord(WrapCoord(hex.Q), WrapCoord(hex.R));
        }

        static int WrapCoord(int value)
        {
            int wrapped = value % WrapCells;
            if (wrapped < 0)
            {
                wrapped += WrapCells;
            }

            return wrapped - WrapCells / 2;
        }
    }
}
