using System.Collections.Generic;
using Voxels.Core.Sphere;

using Voxels.World;

namespace Voxels.World.Climate
{
    public sealed class OceanDistanceField
    {
        readonly int[] coastDistance;
        readonly int cellCount;

        public int CellCount => cellCount;

        public OceanDistanceField(IcosphereHexGrid grid, int seaLevel, PlanetColumnStorage columns)
        {
            cellCount = grid.CellCount;
            coastDistance = new int[cellCount];
            for (int i = 0; i < cellCount; i++)
            {
                coastDistance[i] = int.MaxValue / 4;
            }

            var queue = new Queue<int>();
            for (int i = 0; i < cellCount; i++)
            {
                if (columns.GetColumn(i).SurfaceHeight <= seaLevel)
                {
                    coastDistance[i] = 0;
                    queue.Enqueue(i);
                }
            }

            while (queue.Count > 0)
            {
                int cellIndex = queue.Dequeue();
                int nextDistance = coastDistance[cellIndex] + 1;
                ref readonly SphereHexCell cell = ref grid.GetCell(cellIndex);

                for (int n = 0; n < cell.NeighborCount; n++)
                {
                    int neighbor = cell.Neighbors[n];
                    if (nextDistance < coastDistance[neighbor])
                    {
                        coastDistance[neighbor] = nextDistance;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        public int GetCoastDistance(int cellIndex) => coastDistance[cellIndex];
    }
}
