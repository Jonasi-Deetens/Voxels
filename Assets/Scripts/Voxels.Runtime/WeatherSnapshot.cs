using Voxels.World.Climate;

namespace Voxels.Runtime
{
    public readonly struct WeatherSnapshot
    {
        public WeatherKind Kind { get; }
        public float CloudCoverage { get; }
        public float Precipitation { get; }
        public float FogMultiplier { get; }
        public float WindMultiplier { get; }
        public float SkyDimming { get; }
        public float AmbientVolumeMultiplier { get; }

        public WeatherSnapshot(
            WeatherKind kind,
            float cloudCoverage,
            float precipitation,
            float fogMultiplier,
            float windMultiplier,
            float skyDimming,
            float ambientVolumeMultiplier)
        {
            Kind = kind;
            CloudCoverage = cloudCoverage;
            Precipitation = precipitation;
            FogMultiplier = fogMultiplier;
            WindMultiplier = windMultiplier;
            SkyDimming = skyDimming;
            AmbientVolumeMultiplier = ambientVolumeMultiplier;
        }

        public static WeatherSnapshot Clear => new WeatherSnapshot(
            WeatherKind.Clear,
            0.2f,
            0f,
            1f,
            1f,
            0f,
            1f);
    }
}
