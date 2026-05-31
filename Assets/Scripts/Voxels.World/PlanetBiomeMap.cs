using System.Collections.Generic;

namespace Voxels.World
{
    public sealed class PlanetBiomeMap
    {
        readonly byte[] biomeIndices;
        readonly List<BiomeDefinition> biomes;

        public int CellCount => biomeIndices.Length;
        public IReadOnlyList<BiomeDefinition> Biomes => biomes;

        public PlanetBiomeMap(int cellCount)
        {
            biomeIndices = new byte[cellCount];
            biomes = new List<BiomeDefinition>();
        }

        public void SetBiome(int cellIndex, BiomeDefinition biome)
        {
            if (cellIndex < 0 || cellIndex >= biomeIndices.Length)
            {
                return;
            }

            if (biome == null)
            {
                biomeIndices[cellIndex] = 0;
                return;
            }

            int index = biomes.IndexOf(biome);
            if (index < 0)
            {
                if (biomes.Count >= 255)
                {
                    biomeIndices[cellIndex] = 0;
                    return;
                }

                biomes.Add(biome);
                index = biomes.Count - 1;
            }

            biomeIndices[cellIndex] = (byte)(index + 1);
        }

        public BiomeDefinition GetBiome(int cellIndex)
        {
            if (cellIndex < 0 || cellIndex >= biomeIndices.Length)
            {
                return null;
            }

            byte stored = biomeIndices[cellIndex];
            if (stored == 0)
            {
                return null;
            }

            int biomeIndex = stored - 1;
            if (biomeIndex < 0 || biomeIndex >= biomes.Count)
            {
                return null;
            }

            return biomes[biomeIndex];
        }

        public byte GetBiomeIndex(int cellIndex) => biomeIndices[cellIndex];
    }
}
