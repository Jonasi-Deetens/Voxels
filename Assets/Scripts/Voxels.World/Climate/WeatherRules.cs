using Unity.Mathematics;

namespace Voxels.World.Climate
{
    public static class WeatherRules
    {
        public static WeatherKind PickTarget(BiomeDefinition biome, in ClimateSample climate, ref Random rng)
        {
            float roll = rng.NextFloat();
            bool cold = climate.Temperature < -0.12f;
            bool wet = climate.Humidity > 0.62f;
            bool dry = climate.Humidity < 0.32f;
            bool coastal = climate.CoastDistance <= 2;
            string biomeName = biome != null ? biome.name.ToLowerInvariant() : string.Empty;

            if (climate.IsOcean || coastal && wet)
            {
                if (roll < 0.35f)
                {
                    return WeatherKind.Fog;
                }

                if (roll < 0.7f)
                {
                    return WeatherKind.Rain;
                }

                return WeatherKind.Cloudy;
            }

            if (biomeName.Contains("desert") || biomeName.Contains("savanna"))
            {
                if (roll < 0.72f)
                {
                    return WeatherKind.Clear;
                }

                if (roll < 0.9f)
                {
                    return WeatherKind.Cloudy;
                }

                return WeatherKind.Storm;
            }

            if (biomeName.Contains("tundra") || biomeName.Contains("taiga") || biomeName.Contains("alpine"))
            {
                if (cold && roll < 0.58f)
                {
                    return WeatherKind.Snow;
                }

                if (roll < 0.45f)
                {
                    return WeatherKind.Cloudy;
                }

                return WeatherKind.Clear;
            }

            if (biomeName.Contains("swamp"))
            {
                if (roll < 0.35f)
                {
                    return WeatherKind.Fog;
                }

                if (roll < 0.75f)
                {
                    return WeatherKind.Rain;
                }

                return WeatherKind.Cloudy;
            }

            if (biomeName.Contains("forest") || biomeName.Contains("fungal"))
            {
                if (wet && roll < 0.4f)
                {
                    return WeatherKind.Rain;
                }

                if (roll < 0.7f)
                {
                    return WeatherKind.Cloudy;
                }

                return WeatherKind.Clear;
            }

            if (dry && roll < 0.55f)
            {
                return WeatherKind.Clear;
            }

            if (wet && cold && roll < 0.45f)
            {
                return WeatherKind.Snow;
            }

            if (wet && roll < 0.5f)
            {
                return WeatherKind.Rain;
            }

            if (wet && roll < 0.75f)
            {
                return WeatherKind.Cloudy;
            }

            if (roll < 0.25f)
            {
                return WeatherKind.Fog;
            }

            if (roll < 0.55f)
            {
                return WeatherKind.Cloudy;
            }

            return WeatherKind.Clear;
        }

        public static WeatherProfile GetProfile(WeatherKind kind)
        {
            return kind switch
            {
                WeatherKind.Cloudy => new WeatherProfile(0.55f, 0f, 1.15f, 1.1f, 0.12f, 0.9f),
                WeatherKind.Rain => new WeatherProfile(0.75f, 0.85f, 1.45f, 1.25f, 0.28f, 0.75f),
                WeatherKind.Snow => new WeatherProfile(0.7f, 0.75f, 1.35f, 0.85f, 0.22f, 0.82f),
                WeatherKind.Storm => new WeatherProfile(0.9f, 1f, 1.75f, 1.65f, 0.42f, 0.55f),
                WeatherKind.Fog => new WeatherProfile(0.35f, 0f, 2.2f, 0.7f, 0.35f, 0.7f),
                _ => new WeatherProfile(0.2f, 0f, 1f, 1f, 0f, 1f),
            };
        }
    }

    public readonly struct WeatherProfile
    {
        public readonly float CloudCoverage;
        public readonly float Precipitation;
        public readonly float FogMultiplier;
        public readonly float WindMultiplier;
        public readonly float SkyDimming;
        public readonly float AmbientVolumeMultiplier;

        public WeatherProfile(
            float cloudCoverage,
            float precipitation,
            float fogMultiplier,
            float windMultiplier,
            float skyDimming,
            float ambientVolumeMultiplier)
        {
            CloudCoverage = cloudCoverage;
            Precipitation = precipitation;
            FogMultiplier = fogMultiplier;
            WindMultiplier = windMultiplier;
            SkyDimming = skyDimming;
            AmbientVolumeMultiplier = ambientVolumeMultiplier;
        }
    }
}
