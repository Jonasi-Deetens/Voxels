using System.Collections.Generic;
using Voxels.Core.Hex;

namespace Voxels.World
{
    /// <summary>
    /// Column + biome storage with LRU eviction. Loaded chunk hexes can be marked protected from eviction.
    /// </summary>
    public sealed class HexWorldDataCache
    {
        sealed class CacheEntry
        {
            public BlockColumn Column;
            public BiomeDefinition Biome;
            public LinkedListNode<HexCoord> LruNode;
        }

        readonly Dictionary<HexCoord, CacheEntry> entries = new Dictionary<HexCoord, CacheEntry>();
        readonly LinkedList<HexCoord> lruOrder = new LinkedList<HexCoord>();
        readonly int columnCapacity;
        readonly int maxCachedCells;

        public int ColumnCapacity => columnCapacity;
        public int CachedCellCount => entries.Count;

        public HexWorldDataCache(int columnCapacity, int maxCachedCells)
        {
            this.columnCapacity = columnCapacity;
            this.maxCachedCells = maxCachedCells > 0 ? maxCachedCells : 4096;
        }

        public bool TryGetColumn(in HexCoord hex, out BlockColumn column)
        {
            if (entries.TryGetValue(hex, out CacheEntry entry))
            {
                Touch(entry);
                column = entry.Column;
                return true;
            }

            column = null;
            return false;
        }

        public BlockColumn GetOrCreateColumn(in HexCoord hex)
        {
            if (entries.TryGetValue(hex, out CacheEntry entry))
            {
                Touch(entry);
                return entry.Column;
            }

            var column = new BlockColumn(columnCapacity);
            var node = lruOrder.AddFirst(hex);
            entries[hex] = new CacheEntry { Column = column, LruNode = node };
            TrimUnprotected(null);
            return column;
        }

        public void SetBiome(in HexCoord hex, BiomeDefinition biome)
        {
            if (!entries.TryGetValue(hex, out CacheEntry entry))
            {
                if (biome == null)
                {
                    return;
                }

                GetOrCreateColumn(hex);
                entry = entries[hex];
            }

            entry.Biome = biome;
            Touch(entry);
        }

        public BiomeDefinition GetBiome(in HexCoord hex)
        {
            if (entries.TryGetValue(hex, out CacheEntry entry))
            {
                Touch(entry);
                return entry.Biome;
            }

            return null;
        }

        public bool HasColumn(in HexCoord hex) => entries.ContainsKey(hex);

        public bool RemoveColumn(in HexCoord hex)
        {
            return RemoveEntry(hex);
        }

        public IEnumerable<KeyValuePair<HexCoord, BlockColumn>> EnumerateColumns()
        {
            foreach (KeyValuePair<HexCoord, CacheEntry> entry in entries)
            {
                yield return new KeyValuePair<HexCoord, BlockColumn>(entry.Key, entry.Value.Column);
            }
        }

        public void TrimUnprotected(HashSet<HexCoord> protectedHexes)
        {
            int evictionSafety = entries.Count + 8;
            while (entries.Count > maxCachedCells && evictionSafety-- > 0)
            {
                LinkedListNode<HexCoord> tail = lruOrder.Last;
                if (tail == null)
                {
                    break;
                }

                HexCoord hex = tail.Value;
                if (protectedHexes != null && protectedHexes.Contains(hex))
                {
                    Touch(entries[hex]);
                    continue;
                }

                RemoveEntry(hex);
            }
        }

        public void Clear()
        {
            entries.Clear();
            lruOrder.Clear();
        }

        void Touch(CacheEntry entry)
        {
            if (entry.LruNode == null)
            {
                return;
            }

            lruOrder.Remove(entry.LruNode);
            entry.LruNode = lruOrder.AddFirst(entry.LruNode.Value);
        }

        bool RemoveEntry(in HexCoord hex)
        {
            if (!entries.TryGetValue(hex, out CacheEntry entry))
            {
                return false;
            }

            if (entry.LruNode != null)
            {
                lruOrder.Remove(entry.LruNode);
            }

            entries.Remove(hex);
            return true;
        }
    }
}
