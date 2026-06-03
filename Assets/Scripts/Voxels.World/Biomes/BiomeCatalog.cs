using System.Collections.Generic;
using UnityEngine;

namespace Voxels.World
{
    [CreateAssetMenu(menuName = "Voxels/Biome Catalog", fileName = "BiomeCatalog_")]
    public sealed class BiomeCatalog : ScriptableObject
    {
        [SerializeField] BiomeDefinition terrainProfile;
        [SerializeField] BiomeDefinition fallbackBiome;
        [SerializeField] BiomeDefinition oceanBiome;
        [SerializeField] BiomeDefinition alpineBiome;
        [SerializeField] BiomeRule[] rules;

        public BiomeDefinition TerrainProfile => terrainProfile != null ? terrainProfile : fallbackBiome;
        public BiomeDefinition FallbackBiome => fallbackBiome;
        public BiomeDefinition OceanBiome => oceanBiome;
        public BiomeDefinition AlpineBiome => alpineBiome;
        public IReadOnlyList<BiomeRule> Rules => rules;

        public float MaxMountainAmplitude
        {
            get
            {
                float max = TerrainProfile != null ? TerrainProfile.MountainAmplitude : 18f;
                if (rules != null)
                {
                    for (int i = 0; i < rules.Length; i++)
                    {
                        BiomeDefinition biome = rules[i].Biome;
                        if (biome != null)
                        {
                            max = Mathf.Max(max, biome.MountainAmplitude);
                        }
                    }
                }

                if (alpineBiome != null)
                {
                    max = Mathf.Max(max, alpineBiome.MountainAmplitude);
                }

                return max;
            }
        }

        public List<BiomeDefinition> GetAllBiomesList()
        {
            var seen = new HashSet<BiomeDefinition>();
            var list = new List<BiomeDefinition>();
            TryAdd(TerrainProfile);
            TryAdd(FallbackBiome);
            TryAdd(OceanBiome);
            TryAdd(AlpineBiome);

            if (rules != null)
            {
                for (int i = 0; i < rules.Length; i++)
                {
                    TryAdd(rules[i].Biome);
                }
            }

            return list;

            void TryAdd(BiomeDefinition biome)
            {
                if (biome != null && seen.Add(biome))
                {
                    list.Add(biome);
                }
            }
        }

        public bool TryGetBiomeByAssetName(string assetName, out BiomeDefinition biome)
        {
            biome = null;
            if (string.IsNullOrEmpty(assetName))
            {
                return false;
            }

            List<BiomeDefinition> all = GetAllBiomesList();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].name == assetName)
                {
                    biome = all[i];
                    return true;
                }
            }

            return false;
        }
    }
}
