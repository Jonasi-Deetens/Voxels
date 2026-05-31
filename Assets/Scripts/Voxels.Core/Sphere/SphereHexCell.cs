using Unity.Mathematics;

namespace Voxels.Core.Sphere
{
    public readonly struct SphereHexCell
    {
        public readonly int Index;
        public readonly float3 Normal;
        public readonly int[] Neighbors;
        public readonly bool IsPentagon;

        public SphereHexCell(int index, float3 normal, int[] neighbors, bool isPentagon)
        {
            Index = index;
            Normal = normal;
            Neighbors = neighbors;
            IsPentagon = isPentagon;
        }

        public int NeighborCount => Neighbors.Length;
    }
}
