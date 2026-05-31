using System;
using UnityEngine;

namespace Voxels.World
{
    [Serializable]
    public sealed class BiomeRule
    {
        [SerializeField] BiomeDefinition biome;
        [SerializeField] int priority = 0;
        [SerializeField] float minTemperature = -2f;
        [SerializeField] float maxTemperature = 2f;
        [SerializeField] float minHumidity;
        [SerializeField] float maxHumidity = 1f;
        [SerializeField] float minLeyLine;
        [SerializeField] float maxLeyLine = 1f;
        [SerializeField] int minElevationAboveSea;
        [SerializeField] int maxElevationAboveSea = 9999;
        [SerializeField] int maxCoastDistance = 9999;
        [SerializeField] int minCoastDistance;
        [SerializeField] bool requiresLand = true;

        public BiomeDefinition Biome => biome;
        public int Priority => priority;
        public float MinTemperature => minTemperature;
        public float MaxTemperature => maxTemperature;
        public float MinHumidity => minHumidity;
        public float MaxHumidity => maxHumidity;
        public float MinLeyLine => minLeyLine;
        public float MaxLeyLine => maxLeyLine;
        public int MinElevationAboveSea => minElevationAboveSea;
        public int MaxElevationAboveSea => maxElevationAboveSea;
        public int MaxCoastDistance => maxCoastDistance;
        public int MinCoastDistance => minCoastDistance;
        public bool RequiresLand => requiresLand;
    }
}
