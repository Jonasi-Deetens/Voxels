using System;

namespace Voxels.Core.Hex
{
    public readonly struct ChunkCoord : IEquatable<ChunkCoord>
    {
        public readonly int Q;
        public readonly int R;

        public ChunkCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public static ChunkCoord FromHex(in HexCoord hex, int chunkSizeHex)
        {
            return new ChunkCoord(
                FloorDiv(hex.Q, chunkSizeHex),
                FloorDiv(hex.R, chunkSizeHex));
        }

        public HexCoord ToHexOrigin(int chunkSizeHex) => new HexCoord(Q * chunkSizeHex, R * chunkSizeHex);

        public bool Equals(ChunkCoord other) => Q == other.Q && R == other.R;

        public override bool Equals(object obj) => obj is ChunkCoord other && Equals(other);

        public override int GetHashCode() => (Q * 397) ^ R;

        public override string ToString() => $"Chunk({Q},{R})";

        static int FloorDiv(int value, int divisor)
        {
            if (divisor <= 0)
            {
                return 0;
            }

            return value >= 0 ? value / divisor : (value - divisor + 1) / divisor;
        }
    }
}
