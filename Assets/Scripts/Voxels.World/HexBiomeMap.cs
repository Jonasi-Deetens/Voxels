using System.Collections.Generic;
using Voxels.Core.Hex;

namespace Voxels.World
{
    public sealed class HexBiomeMap
    {
        readonly Dictionary<HexCoord, BiomeDefinition> biomes = new Dictionary<HexCoord, BiomeDefinition>();

        public void SetBiome(in HexCoord hex, BiomeDefinition biome)
        {
            if (biome == null)
            {
                biomes.Remove(hex);
                return;
            }

            biomes[hex] = biome;
        }

        public BiomeDefinition GetBiome(in HexCoord hex)
        {
            biomes.TryGetValue(hex, out BiomeDefinition biome);
            return biome;
        }
    }
}
