using Voxels.Core.Hex;

namespace Voxels.Runtime
{
    public readonly struct HexBlockTarget
    {
        public HexBlockTarget(HexCoord worldHex, int layer, bool valid)
        {
            WorldHex = worldHex;
            Layer = layer;
            IsValid = valid;
        }

        public HexCoord WorldHex { get; }
        public int Layer { get; }
        public bool IsValid { get; }
    }
}
