using System.Collections.Generic;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.World.Climate
{
    public sealed class FlatOceanDistanceField
    {
        readonly Dictionary<HexCoord, int> coastDistance = new Dictionary<HexCoord, int>();

        public FlatOceanDistanceField(
            IEnumerable<HexCoord> cells,
            int seaLevel,
            HexWorldDataCache cache)
        {
            var queue = new Queue<HexCoord>();
            foreach (HexCoord hex in cells)
            {
                if (!cache.TryGetColumn(hex, out BlockColumn column))
                {
                    continue;
                }

                if (column.SurfaceHeight < seaLevel)
                {
                    coastDistance[hex] = 0;
                    queue.Enqueue(hex);
                }
                else
                {
                    coastDistance[hex] = int.MaxValue / 4;
                }
            }

            while (queue.Count > 0)
            {
                HexCoord hex = queue.Dequeue();
                int nextDistance = coastDistance[hex] + 1;

                for (int i = 0; i < HexCoord.NeighborOffsets.Length; i++)
                {
                    HexCoord neighbor = hex.Add(HexCoord.NeighborOffsets[i]);
                    if (!coastDistance.TryGetValue(neighbor, out int existing) || nextDistance < existing)
                    {
                        coastDistance[neighbor] = nextDistance;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        public int GetCoastDistance(in HexCoord hex)
        {
            return coastDistance.TryGetValue(hex, out int distance) ? distance : 9999;
        }
    }
}
