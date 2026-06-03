using System.Collections.Generic;
using Voxels.Core.Hex;

namespace Voxels.World
{
    public sealed class HexColumnStorage
    {
        readonly Dictionary<HexCoord, BlockColumn> columns = new Dictionary<HexCoord, BlockColumn>();
        readonly int columnCapacity;

        public int ColumnCapacity => columnCapacity;

        public HexColumnStorage(int columnCapacity)
        {
            this.columnCapacity = columnCapacity;
        }

        public bool TryGetColumn(in HexCoord hex, out BlockColumn column) => columns.TryGetValue(hex, out column);

        public BlockColumn GetOrCreateColumn(in HexCoord hex)
        {
            if (!columns.TryGetValue(hex, out BlockColumn column))
            {
                column = new BlockColumn(columnCapacity);
                columns[hex] = column;
            }

            return column;
        }

        public bool RemoveColumn(in HexCoord hex) => columns.Remove(hex);

        public IEnumerable<KeyValuePair<HexCoord, BlockColumn>> Columns => columns;
    }
}
