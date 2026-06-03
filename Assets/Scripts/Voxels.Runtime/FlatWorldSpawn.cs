using System.Collections.Generic;
using Unity.Mathematics;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
    public static class FlatWorldSpawn
    {
        public static HexCoord FindSpawnHex(HexWorld world, WorldSettings settings)
        {
            int radius = math.min(16, settings.WorldHexRadius);
            var candidates = new List<(HexCoord hex, int score)>();

            for (int q = -radius; q <= radius; q++)
            {
                for (int r = -radius; r <= radius; r++)
                {
                    var hex = new HexCoord(q, r);
                    if (!world.IsInsideWorld(hex) || !world.TryGetColumn(hex, out BlockColumn column))
                    {
                        continue;
                    }

                    if (column.SurfaceHeight < settings.SeaLevelLayer)
                    {
                        continue;
                    }

                    BiomeDefinition biome = world.GetBiome(hex);
                    int score = biome != null ? biome.SpawnPreference : 0;
                    score -= world.DistanceToEdge(hex) * 2;
                    candidates.Add((hex, score));
                }
            }

            if (candidates.Count == 0)
            {
                return HexCoord.Zero;
            }

            candidates.Sort((a, b) => b.score.CompareTo(a.score));
            return candidates[0].hex;
        }
    }
}
