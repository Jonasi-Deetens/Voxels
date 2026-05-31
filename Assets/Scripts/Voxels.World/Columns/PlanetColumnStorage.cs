using Voxels.Core.Sphere;

namespace Voxels.World
{
    public sealed class PlanetColumnStorage
    {
        readonly BlockColumn[] columns;

        public int CellCount => columns.Length;

        public PlanetColumnStorage(IcosphereHexGrid grid, int initialColumnCapacity)
        {
            columns = new BlockColumn[grid.CellCount];
            for (int i = 0; i < columns.Length; i++)
            {
                columns[i] = new BlockColumn(initialColumnCapacity);
            }
        }

        public BlockColumn GetColumn(int cellIndex) => columns[cellIndex];
    }
}
