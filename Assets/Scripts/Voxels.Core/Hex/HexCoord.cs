using System;
using Unity.Mathematics;

namespace Voxels.Core.Hex
{
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        public static readonly HexCoord Zero = new HexCoord(0, 0);

        public static readonly HexCoord[] NeighborOffsets =
        {
            new HexCoord(1, 0),
            new HexCoord(1, -1),
            new HexCoord(0, -1),
            new HexCoord(-1, 0),
            new HexCoord(-1, 1),
            new HexCoord(0, 1),
        };

        public readonly int Q;
        public readonly int R;

        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int S => -Q - R;

        public HexCoord Add(in HexCoord other) => new HexCoord(Q + other.Q, R + other.R);

        public HexCoord Subtract(in HexCoord other) => new HexCoord(Q - other.Q, R - other.R);

        public HexCoord Scale(int factor) => new HexCoord(Q * factor, R * factor);

        public int DistanceTo(in HexCoord other)
        {
            int dq = math.abs(Q - other.Q);
            int dr = math.abs(R - other.R);
            int ds = math.abs(S - other.S);
            return math.max(dq, math.max(dr, ds));
        }

        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;

        public override bool Equals(object obj) => obj is HexCoord other && Equals(other);

        public override int GetHashCode() => (Q * 397) ^ R;

        public static bool operator ==(HexCoord left, HexCoord right) => left.Equals(right);

        public static bool operator !=(HexCoord left, HexCoord right) => !left.Equals(right);

        public override string ToString() => $"({Q},{R})";
    }
}
