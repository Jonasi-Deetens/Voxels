using Unity.Mathematics;
using Voxels.Core.Sphere;

namespace Voxels.Runtime
{
    static class PlanetSurfaceLocator
    {
        public static int RefineNearestCell(IcosphereHexGrid grid, int startCellIndex, float3 direction)
        {
            direction = math.normalize(direction);
            if (startCellIndex < 0 || startCellIndex >= grid.CellCount)
            {
                return FindNearestCellBruteForce(grid, direction);
            }

            int current = startCellIndex;
            bool improved = true;
            while (improved)
            {
                improved = false;
                ref readonly SphereHexCell cell = ref grid.GetCell(current);
                float bestDot = math.dot(cell.Normal, direction);

                for (int i = 0; i < cell.NeighborCount; i++)
                {
                    int neighborIndex = cell.Neighbors[i];
                    ref readonly SphereHexCell neighbor = ref grid.GetCell(neighborIndex);
                    float neighborDot = math.dot(neighbor.Normal, direction);
                    if (neighborDot > bestDot + 0.00001f)
                    {
                        bestDot = neighborDot;
                        current = neighborIndex;
                        improved = true;
                    }
                }
            }

            return current;
        }

        static int FindNearestCellBruteForce(IcosphereHexGrid grid, float3 direction)
        {
            int bestCell = 0;
            float bestDot = -1f;

            for (int i = 0; i < grid.CellCount; i++)
            {
                float dot = math.dot(grid.GetCell(i).Normal, direction);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestCell = i;
                }
            }

            return bestCell;
        }
    }
}
