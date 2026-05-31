using System;

namespace Voxels.Core.Blocks
{
    public readonly struct BlockId : IEquatable<BlockId>
    {
        public static readonly BlockId Air = new(0);

        public readonly ushort Value;

        public BlockId(ushort value)
        {
            Value = value;
        }

        public bool IsAir => Value == 0;

        public bool Equals(BlockId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is BlockId other && Equals(other);

        public override int GetHashCode() => Value;

        public static bool operator ==(BlockId left, BlockId right) => left.Value == right.Value;

        public static bool operator !=(BlockId left, BlockId right) => left.Value != right.Value;
    }
}
