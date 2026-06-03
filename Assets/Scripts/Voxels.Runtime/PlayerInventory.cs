using System.Collections.Generic;
using UnityEngine;
using Voxels.Core.Blocks;
using Voxels.World;

namespace Voxels.Runtime
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        const int MaxStack = 99;

        readonly Dictionary<ushort, int> counts = new Dictionary<ushort, int>();

        public int GetCount(BlockId blockId) =>
            counts.TryGetValue(blockId.Value, out int count) ? count : 0;

        public void Add(BlockId blockId, int amount = 1)
        {
            if (blockId.IsAir || amount <= 0)
            {
                return;
            }

            counts.TryGetValue(blockId.Value, out int existing);
            counts[blockId.Value] = Mathf.Min(MaxStack, existing + amount);
        }

        public bool TryConsume(BlockId blockId, int amount = 1)
        {
            if (blockId.IsAir)
            {
                return true;
            }

            if (!counts.TryGetValue(blockId.Value, out int existing) || existing < amount)
            {
                return false;
            }

            existing -= amount;
            if (existing <= 0)
            {
                counts.Remove(blockId.Value);
            }
            else
            {
                counts[blockId.Value] = existing;
            }

            return true;
        }

        public bool HasAtLeast(BlockId blockId, int amount = 1)
        {
            if (blockId.IsAir)
            {
                return true;
            }

            return GetCount(blockId) >= amount;
        }

        public string FormatCount(BlockId blockId)
        {
            int count = GetCount(blockId);
            return count <= 0 ? string.Empty : count.ToString();
        }
    }
}
